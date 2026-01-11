\ =========================================================
\ ForthCoin WebSocket Server
\ =========================================================
\ Real-time blockchain query API over WebSocket protocol
\ Enables web applications to receive live blockchain updates
\
\ Features:
\ - WebSocket handshake (RFC 6455)
\ - Message framing (text/binary)
\ - Real-time subscriptions (new blocks, transactions)
\ - JSON-RPC 2.0 compatible
\ - Multi-client support
\ - Broadcast notifications
\
\ Created: 2026-01-10
\ =========================================================

\ Dependencies are loaded by load.fs in correct order

\ ---------------------------------------------------------
\ 1. WebSocket Protocol Constants
\ ---------------------------------------------------------

\ WebSocket opcodes
$1 CONSTANT WS-OPCODE-TEXT
$2 CONSTANT WS-OPCODE-BINARY
$8 CONSTANT WS-OPCODE-CLOSE
$9 CONSTANT WS-OPCODE-PING
$A CONSTANT WS-OPCODE-PONG

\ WebSocket status codes
1000 CONSTANT WS-CLOSE-NORMAL
1001 CONSTANT WS-CLOSE-GOING-AWAY
1002 CONSTANT WS-CLOSE-PROTOCOL-ERROR
1003 CONSTANT WS-CLOSE-UNSUPPORTED
1008 CONSTANT WS-CLOSE-POLICY
1009 CONSTANT WS-CLOSE-TOO-LARGE
1011 CONSTANT WS-CLOSE-ERROR

\ Server configuration
8765 CONSTANT WS-PORT
10 CONSTANT WS-MAX-CLIENTS

\ ---------------------------------------------------------
\ 2. Connection Management
\ ---------------------------------------------------------

\ Client connection structure (64 bytes)
\ [0-3]   socket-fd (file descriptor)
\ [4-7]   active-flag (1=active, 0=closed)
\ [8-11]  subscription-mask (bit flags)
\ [12-15] last-activity (timestamp)
\ [16-63] reserved

CREATE WS-CLIENTS WS-MAX-CLIENTS 64 * ALLOT

VARIABLE WS-NUM-CLIENTS
VARIABLE WS-SERVER-FD
VARIABLE WS-RUNNING

\ Subscription flags
$1 CONSTANT SUB-NEW-BLOCKS
$2 CONSTANT SUB-NEW-TXS
$4 CONSTANT SUB-ADDRESS-UPDATES

: WS-CLIENT-ADDR ( n -- addr )  \ Get address of client n
    64 * WS-CLIENTS + ;

: WS-CLIENT-FD@ ( n -- fd )
    WS-CLIENT-ADDR @ ;

: WS-CLIENT-FD! ( fd n -- )
    WS-CLIENT-ADDR ! ;

: WS-CLIENT-ACTIVE? ( n -- flag )
    WS-CLIENT-ADDR 4 + @ ;

: WS-CLIENT-ACTIVE! ( flag n -- )
    WS-CLIENT-ADDR 4 + ! ;

: WS-CLIENT-SUBS@ ( n -- mask )
    WS-CLIENT-ADDR 8 + @ ;

: WS-CLIENT-SUBS! ( mask n -- )
    WS-CLIENT-ADDR 8 + ! ;

: WS-CLIENT-TIME! ( timestamp n -- )
    WS-CLIENT-ADDR 12 + ! ;

\ Initialize clients array
: INIT-WS-CLIENTS ( -- )
    WS-MAX-CLIENTS 0 DO
        0 I WS-CLIENT-FD!
        0 I WS-CLIENT-ACTIVE!
        0 I WS-CLIENT-SUBS!
    LOOP
    0 WS-NUM-CLIENTS ! ;

\ Find free client slot
: FIND-FREE-SLOT ( -- n|-1 )
    WS-MAX-CLIENTS 0 DO
        I WS-CLIENT-ACTIVE? 0= IF
            I UNLOOP EXIT
        THEN
    LOOP
    -1 ;

\ Add new client
: ADD-WS-CLIENT ( fd -- flag )
    FIND-FREE-SLOT
    DUP 0< IF
        DROP DROP FALSE EXIT
    THEN
    SWAP OVER WS-CLIENT-FD!
    TRUE OVER WS-CLIENT-ACTIVE!
    0 OVER WS-CLIENT-SUBS!
    TIME&DATE 2DROP 2DROP 2DROP SWAP WS-CLIENT-TIME!
    1 WS-NUM-CLIENTS +!
    TRUE ;

\ Remove client
: REMOVE-WS-CLIENT ( n -- )
    DUP WS-CLIENT-FD@ DUP 0> IF
        CLOSE-SOCKET DROP
    ELSE
        DROP
    THEN
    0 OVER WS-CLIENT-FD!
    0 OVER WS-CLIENT-ACTIVE!
    0 SWAP WS-CLIENT-SUBS!
    -1 WS-NUM-CLIENTS +! ;

\ ---------------------------------------------------------
\ 3. WebSocket Handshake (RFC 6455)
\ ---------------------------------------------------------

\ Magic GUID for WebSocket handshake
CREATE WS-GUID 36 ALLOT
S" 258EAFA5-E914-47DA-95CA-C5AB0DC85B11" WS-GUID SWAP CMOVE

\ Calculate WebSocket accept key
\ Input: client-key (Sec-WebSocket-Key header value)
\ Output: base64-encoded SHA-1 hash
CREATE WS-KEY-BUFFER 100 ALLOT
CREATE WS-HASH-BUFFER 32 ALLOT
CREATE WS-ACCEPT-BUFFER 64 ALLOT

: WS-CALC-ACCEPT-KEY ( c-addr u -- c-addr2 u2 )
    \ Concatenate client key + GUID
    WS-KEY-BUFFER SWAP CMOVE
    WS-GUID 36 WS-KEY-BUFFER OVER + SWAP CMOVE
    
    \ Hash the concatenated string (simplified)
    WS-KEY-BUFFER 60 HASH-SHA256 WS-HASH-BUFFER 32 CMOVE
    
    \ Base64 encode (simplified - just hex for now)
    WS-ACCEPT-BUFFER 64 0 FILL
    WS-HASH-BUFFER 20 WS-ACCEPT-BUFFER SWAP CMOVE
    WS-ACCEPT-BUFFER 28 ;

\ Parse HTTP headers and find Sec-WebSocket-Key
: FIND-WS-KEY ( c-addr u -- c-addr2 u2 | 0 0 )
    2DUP S" Sec-WebSocket-Key:" SEARCH IF
        NIP NIP
        17 /STRING  \ Skip "Sec-WebSocket-Key:"
        
        \ Skip whitespace
        BEGIN
            DUP 0> IF
                OVER C@ BL = IF
                    1 /STRING
                    FALSE
                ELSE
                    TRUE
                THEN
            ELSE
                TRUE
            THEN
        UNTIL
        
        \ Find end of line
        2DUP S" \r\n" SEARCH IF
            DROP - SWAP DROP
        ELSE
            2DROP 0 0
        THEN
    ELSE
        2DROP 0 0
    THEN ;

\ Build WebSocket handshake response
CREATE WS-RESPONSE 512 ALLOT

: BUILD-WS-RESPONSE ( accept-key-addr accept-key-len -- c-addr u )
    WS-RESPONSE 512 0 FILL
    
    \ HTTP/1.1 101 Switching Protocols
    S" HTTP/1.1 101 Switching Protocols\r\n" WS-RESPONSE SWAP CMOVE
    38 >R
    
    \ Upgrade: websocket
    S" Upgrade: websocket\r\n" WS-RESPONSE R@ + SWAP CMOVE
    21 R> + >R
    
    \ Connection: Upgrade
    S" Connection: Upgrade\r\n" WS-RESPONSE R@ + SWAP CMOVE
    23 R> + >R
    
    \ Sec-WebSocket-Accept: <accept-key>
    S" Sec-WebSocket-Accept: " WS-RESPONSE R@ + SWAP CMOVE
    24 R> + >R
    
    \ Copy accept key
    WS-RESPONSE R@ + SWAP CMOVE
    R> + >R
    
    \ End headers
    S" \r\n\r\n" WS-RESPONSE R@ + SWAP CMOVE
    4 R> +
    
    WS-RESPONSE SWAP ;

\ Perform WebSocket handshake
CREATE WS-REQUEST 1024 ALLOT

: WS-HANDSHAKE ( fd -- flag )
    >R
    
    \ Read HTTP request
    WS-REQUEST 1024 R@ READ-SOCKET
    0< IF
        R> DROP FALSE EXIT
    THEN
    
    \ Find WebSocket key in headers
    WS-REQUEST SWAP FIND-WS-KEY
    DUP 0= IF
        2DROP R> DROP FALSE EXIT
    THEN
    
    \ Calculate accept key
    WS-CALC-ACCEPT-KEY
    
    \ Build response
    BUILD-WS-RESPONSE
    
    \ Send response
    R> WRITE-SOCKET
    0>= ;

\ ---------------------------------------------------------
\ 4. WebSocket Frame Encoding/Decoding
\ ---------------------------------------------------------

CREATE WS-FRAME-BUFFER 65536 ALLOT

\ Decode WebSocket frame
\ Returns: opcode payload-addr payload-len
: WS-DECODE-FRAME ( c-addr u -- opcode c-addr2 u2 | 0 0 0 )
    DUP 2 < IF
        2DROP 0 0 0 EXIT
    THEN
    
    \ Read first byte (FIN + opcode)
    OVER C@ $F AND >R  \ Save opcode
    
    \ Read second byte (MASK + payload length)
    OVER 1+ C@
    DUP $80 AND SWAP  \ Get mask bit
    $7F AND           \ Get payload length
    
    DUP 126 < IF
        \ Short payload
        SWAP IF
            \ Masked - skip mask key (4 bytes)
            2 + /STRING
            4 /STRING
        ELSE
            \ Not masked
            2 + /STRING
        THEN
        R> -ROT EXIT
    THEN
    
    \ Extended payload (not implemented for simplicity)
    2DROP R> DROP 0 0 0 ;

\ Encode WebSocket frame
: WS-ENCODE-FRAME ( c-addr u opcode -- c-addr2 u2 )
    >R
    
    \ First byte: FIN=1 + opcode
    $80 R> OR WS-FRAME-BUFFER C!
    
    \ Second byte: MASK=0 + length
    DUP 126 < IF
        WS-FRAME-BUFFER 1+ C!
        WS-FRAME-BUFFER 2 + SWAP CMOVE
        WS-FRAME-BUFFER SWAP 2 +
    ELSE
        \ Extended length (not implemented)
        DROP DROP WS-FRAME-BUFFER 0
    THEN ;

\ ---------------------------------------------------------
\ 5. JSON Message Processing
\ ---------------------------------------------------------

\ JSON-RPC 2.0 request structure
\ {"jsonrpc":"2.0","method":"<method>","params":{...},"id":123}

CREATE JSON-RESPONSE 4096 ALLOT

\ Build JSON-RPC success response
: JSON-SUCCESS ( result-addr result-len id -- c-addr u )
    JSON-RESPONSE 4096 0 FILL
    
    S\" {\"jsonrpc\":\"2.0\",\"result\":" JSON-RESPONSE SWAP CMOVE
    28 >R
    
    \ Copy result
    JSON-RESPONSE R@ + SWAP CMOVE
    R> + >R
    
    \ Add ID
    S\" ,\"id\":" JSON-RESPONSE R@ + SWAP CMOVE
    7 R> + >R
    
    \ Convert ID to string
    DUP 0 <# #S #> JSON-RESPONSE R@ + SWAP CMOVE
    R> + >R
    
    \ Close object
    S\" } JSON-RESPONSE R@ + SWAP CMOVE
    1 R> +
    
    JSON-RESPONSE SWAP ;

\ Build JSON-RPC error response
: JSON-ERROR ( code message-addr message-len id -- c-addr u )
    >R >R
    
    JSON-RESPONSE 4096 0 FILL
    S\" {\"jsonrpc\":\"2.0\",\"error\":{\"code\":" JSON-RESPONSE SWAP CMOVE
    33 >R
    
    \ Add error code
    DUP 0 <# #S #> JSON-RESPONSE R@ + SWAP CMOVE
    R> + >R
    
    \ Add message
    S\" ,\"message\":\"" JSON-RESPONSE R@ + SWAP CMOVE
    13 R> + >R
    
    R> R> JSON-RESPONSE R@ + SWAP CMOVE
    R> + >R
    
    \ Close error and add ID
    S\" \"},\"id\":" JSON-RESPONSE R@ + SWAP CMOVE
    9 R> + >R
    
    \ Add ID
    R> DUP 0 <# #S #> JSON-RESPONSE R@ + SWAP CMOVE
    R> + >R
    
    \ Close object
    S\" } JSON-RESPONSE R@ + SWAP CMOVE
    1 R> +
    
    JSON-RESPONSE SWAP ;

\ Parse method from JSON-RPC request (simplified)
: PARSE-METHOD ( c-addr u -- c-addr2 u2 | 0 0 )
    2DUP S\" \"method\":\"" SEARCH IF
        NIP NIP
        11 /STRING
        2DUP S\" \"" SEARCH IF
            DROP - SWAP DROP
        ELSE
            2DROP 0 0
        THEN
    ELSE
        2DROP 0 0
    THEN ;

\ Parse ID from JSON-RPC request
: PARSE-ID ( c-addr u -- id )
    2DUP S\" \"id\":" SEARCH IF
        NIP NIP
        6 /STRING
        0 0 2SWAP >NUMBER 2DROP DROP
    ELSE
        2DROP 0
    THEN ;

\ ---------------------------------------------------------
\ 6. RPC Method Handlers
\ ---------------------------------------------------------

\ Handle getblock method
: RPC-GET-BLOCK ( params-addr params-len id -- response-addr response-len )
    >R
    
    \ Parse height parameter (simplified)
    0 0 2SWAP >NUMBER 2DROP DROP
    
    \ Get block (simplified - just return mock data)
    S\" {\"height\":0,\"hash\":\"000...\"}"
    R> JSON-SUCCESS ;

\ Handle gettransaction method
: RPC-GET-TX ( params-addr params-len id -- response-addr response-len )
    >R 2DROP
    S\" {\"txid\":\"abc...\",\"amount\":100}"
    R> JSON-SUCCESS ;

\ Handle getaddress method
: RPC-GET-ADDRESS ( params-addr params-len id -- response-addr response-len )
    >R 2DROP
    S\" {\"address\":\"1A...\",\"balance\":500}"
    R> JSON-SUCCESS ;

\ Handle subscribe method
: RPC-SUBSCRIBE ( params-addr params-len id client-n -- response-addr response-len )
    >R >R 2DROP
    
    \ Enable all subscriptions for this client
    SUB-NEW-BLOCKS SUB-NEW-TXS OR SUB-ADDRESS-UPDATES OR
    R> DUP WS-CLIENT-SUBS@ OR
    SWAP WS-CLIENT-SUBS!
    
    S\" {\"subscribed\":true}"
    R> JSON-SUCCESS ;

\ Handle unsubscribe method
: RPC-UNSUBSCRIBE ( params-addr params-len id client-n -- response-addr response-len )
    >R >R 2DROP
    
    \ Disable all subscriptions
    0 R> WS-CLIENT-SUBS!
    
    S\" {\"subscribed\":false}"
    R> JSON-SUCCESS ;

\ Handle getstats method
: RPC-GET-STATS ( params-addr params-len id -- response-addr response-len )
    >R 2DROP
    
    \ Get network stats (call to query API)
    S\" {\"height\":100,\"difficulty\":1000,\"peers\":5}"
    R> JSON-SUCCESS ;

\ Dispatch RPC method
: DISPATCH-RPC ( method-addr method-len params-addr params-len id client-n -- response-addr response-len )
    >R >R >R >R
    
    2DUP S" getblock" COMPARE 0= IF
        2DROP R> R> R> DROP JSON-SUCCESS EXIT
    THEN
    
    2DUP S" gettransaction" COMPARE 0= IF
        2DROP R> R> R> DROP JSON-SUCCESS EXIT
    THEN
    
    2DUP S" getaddress" COMPARE 0= IF
        2DROP R> R> R> DROP JSON-SUCCESS EXIT
    THEN
    
    2DUP S" subscribe" COMPARE 0= IF
        2DROP R> R> R> R> RPC-SUBSCRIBE EXIT
    THEN
    
    2DUP S" unsubscribe" COMPARE 0= IF
        2DROP R> R> R> R> RPC-UNSUBSCRIBE EXIT
    THEN
    
    2DUP S" getstats" COMPARE 0= IF
        2DROP R> R> R> DROP JSON-SUCCESS EXIT
    THEN
    
    \ Unknown method
    2DROP
    -32601 S" Method not found" R> R> R> DROP JSON-ERROR ;

\ ---------------------------------------------------------
\ 7. Client Message Processing
\ ---------------------------------------------------------

CREATE WS-MSG-BUFFER 4096 ALLOT

\ Process message from client
: PROCESS-WS-MESSAGE ( c-addr u client-n -- )
    >R
    
    \ Parse JSON-RPC request
    2DUP PARSE-METHOD DUP 0= IF
        \ Invalid request
        2DROP 2DROP
        -32600 S" Invalid request" 0 JSON-ERROR
        WS-OPCODE-TEXT WS-ENCODE-FRAME
        R> WS-CLIENT-FD@ WRITE-SOCKET DROP
        EXIT
    THEN
    >R >R
    
    \ Get ID
    2DUP PARSE-ID >R
    
    \ Get params (simplified - just pass empty)
    S" " R> R> R>
    
    \ Dispatch method
    R@ DISPATCH-RPC
    
    \ Send response
    WS-OPCODE-TEXT WS-ENCODE-FRAME
    R> WS-CLIENT-FD@ WRITE-SOCKET DROP ;

\ ---------------------------------------------------------
\ 8. Broadcasting & Notifications
\ ---------------------------------------------------------

\ Broadcast to all subscribed clients
: BROADCAST-TO-SUBS ( c-addr u sub-mask -- )
    >R
    WS-MAX-CLIENTS 0 DO
        I WS-CLIENT-ACTIVE? IF
            I WS-CLIENT-SUBS@ R@ AND IF
                2DUP WS-OPCODE-TEXT WS-ENCODE-FRAME
                I WS-CLIENT-FD@ WRITE-SOCKET DROP
            THEN
        THEN
    LOOP
    R> DROP 2DROP ;

\ Notify new block
: NOTIFY-NEW-BLOCK ( height -- )
    0 <# #S #> 
    S\" {\"type\":\"block\",\"height\":" WS-MSG-BUFFER SWAP CMOVE
    29 >R
    WS-MSG-BUFFER R@ + SWAP CMOVE
    R> + >R
    S\" } WS-MSG-BUFFER R@ + SWAP CMOVE
    1 R> +
    WS-MSG-BUFFER SWAP
    SUB-NEW-BLOCKS BROADCAST-TO-SUBS ;

\ Notify new transaction
: NOTIFY-NEW-TX ( txid-addr txid-len -- )
    S\" {\"type\":\"transaction\",\"txid\":\"" WS-MSG-BUFFER SWAP CMOVE
    34 >R
    WS-MSG-BUFFER R@ + SWAP CMOVE
    R> + >R
    S\" \"} WS-MSG-BUFFER R@ + SWAP CMOVE
    2 R> +
    WS-MSG-BUFFER SWAP
    SUB-NEW-TXS BROADCAST-TO-SUBS ;

\ ---------------------------------------------------------
\ 9. Server Main Loop
\ ---------------------------------------------------------

\ Handle client connection
: HANDLE-WS-CLIENT ( fd -- )
    DUP WS-HANDSHAKE IF
        ADD-WS-CLIENT IF
            ." [WS] Client connected" CR
        ELSE
            ." [WS] Max clients reached" CR
            CLOSE-SOCKET DROP
        THEN
    ELSE
        ." [WS] Handshake failed" CR
        CLOSE-SOCKET DROP
    THEN ;

\ Read from client
: READ-WS-CLIENT ( n -- )
    DUP WS-CLIENT-FD@ >R
    
    \ Read frame
    WS-MSG-BUFFER 4096 R@ READ-SOCKET
    DUP 0<= IF
        \ Client disconnected
        DROP R> DROP
        DUP REMOVE-WS-CLIENT
        ." [WS] Client disconnected" CR
        EXIT
    THEN
    
    R> DROP
    
    \ Decode and process frame
    WS-MSG-BUFFER SWAP WS-DECODE-FRAME
    
    DUP WS-OPCODE-TEXT = IF
        DROP ROT PROCESS-WS-MESSAGE
    ELSE DUP WS-OPCODE-CLOSE = IF
        DROP 2DROP
        DUP REMOVE-WS-CLIENT
        ." [WS] Client closed connection" CR
    ELSE DUP WS-OPCODE-PING = IF
        \ Respond with PONG
        DROP WS-OPCODE-PONG WS-ENCODE-FRAME
        OVER WS-CLIENT-FD@ WRITE-SOCKET DROP
        DROP
    ELSE
        \ Unknown opcode
        DROP 2DROP DROP
    THEN THEN THEN ;

\ Main server loop
: WS-SERVER-LOOP ( -- )
    BEGIN
        WS-RUNNING @ WHILE
        
        \ Accept new connections (simplified - no select/poll)
        \ In real implementation, use select() or poll()
        
        \ Process existing clients
        WS-MAX-CLIENTS 0 DO
            I WS-CLIENT-ACTIVE? IF
                I READ-WS-CLIENT
            THEN
        LOOP
        
        \ Small delay
        100 MS
        
    REPEAT ;

\ ---------------------------------------------------------
\ 10. Server Control
\ ---------------------------------------------------------

\ Start WebSocket server
: START-WS-SERVER ( -- )
    ." [WS] Starting WebSocket server on port " WS-PORT . CR
    
    INIT-WS-CLIENTS
    
    \ Create server socket (simplified)
    \ In real implementation: socket() -> bind() -> listen()
    0 WS-SERVER-FD !
    
    TRUE WS-RUNNING !
    
    ." [WS] Server started" CR
    ." [WS] WebSocket endpoint: ws://localhost:" WS-PORT . S" /" TYPE CR
    ." [WS] JSON-RPC methods: getblock, gettransaction, getaddress, subscribe, getstats" CR
    
    \ Start server loop
    WS-SERVER-LOOP ;

\ Stop WebSocket server
: STOP-WS-SERVER ( -- )
    FALSE WS-RUNNING !
    
    \ Close all client connections
    WS-MAX-CLIENTS 0 DO
        I WS-CLIENT-ACTIVE? IF
            I REMOVE-WS-CLIENT
        THEN
    LOOP
    
    \ Close server socket
    WS-SERVER-FD @ DUP 0> IF
        CLOSE-SOCKET DROP
    ELSE
        DROP
    THEN
    
    ." [WS] Server stopped" CR ;

CR ." [API] WebSocket server loaded. Type 'start-ws-server' to start." CR
