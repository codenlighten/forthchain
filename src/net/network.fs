\ =========================================================
\ FORTHCOIN NETWORK PROTOCOL
\ =========================================================
\ Basic P2P message structures and parsing

\ Configuration
8333 CONSTANT CHAIN-PORT
$D9B4BEF9 CONSTANT MAGIC-MAINNET

\ Message buffer
CREATE MSG-BUFFER 1024 ALLOT
CREATE HEADER-BUF 24 ALLOT

\ ---------------------------------------------------------
\ 1. Message Header Structure (24 Bytes)
\ ---------------------------------------------------------
\ [4: Magic] [12: Command] [4: Payload Length] [4: Checksum]

: PARSE-MAGIC ( addr -- magic )
    @ ;

: PARSE-COMMAND ( addr -- cmd_addr )
    4 + ;

: PARSE-LENGTH ( addr -- length )
    16 + @ ;

: PARSE-CHECKSUM ( addr -- checksum )
    20 + @ ;

\ ---------------------------------------------------------
\ 2. Version Message Construction (Simplified)
\ ---------------------------------------------------------

CREATE VER-BUF 256 ALLOT

: CONSTRUCT-VERSION ( -- addr len )
    VER-BUF 256 ERASE
    VER-BUF
    
    \ Version (70015)
    70015 OVER ! 4 +
    
    \ Services (NODE_NETWORK = 1)
    1 OVER ! 4 +
    0 OVER ! 4 +
    
    \ Timestamp (placeholder)
    0 OVER ! 4 +
    0 OVER ! 4 +
    
    \ Addr_Recv (26 bytes - simplified)
    26 + 
    
    \ Addr_From (26 bytes - simplified)
    26 +
    
    \ Nonce (8 bytes)
    $DEADBEEF OVER ! 4 +
    $CAFEBABE OVER ! 4 +
    
    \ User Agent (empty string = 1 byte)
    0 OVER C! 1+
    
    \ Start Height (4 bytes)
    0 OVER ! 4 +
    
    \ Relay flag (1 byte)
    1 OVER C! 1+
    
    \ Return: Start Address, Length
    VER-BUF - VER-BUF SWAP ;

\ ---------------------------------------------------------
\ 3. Message Validation
\ ---------------------------------------------------------

: VALIDATE-MAGIC ( addr -- flag )
    PARSE-MAGIC MAGIC-MAINNET = ;

: VALIDATE-COMMAND ( addr cmd_str -- flag )
    SWAP PARSE-COMMAND SWAP COMPARE 0= ;

\ ---------------------------------------------------------
\ 4. Protocol Constants
\ ---------------------------------------------------------

: CMD-VERSION s" version" ;
: CMD-VERACK  s" verack" ;
: CMD-BLOCK   s" block" ;
: CMD-TX      s" tx" ;
: CMD-INV     s" inv" ;
: CMD-GETDATA s" getdata" ;

\ ---------------------------------------------------------
\ 5. Socket Operations (Using Gforth Built-ins)
\ ---------------------------------------------------------

\ Socket file descriptors
VARIABLE LISTEN-FD
VARIABLE CURRENT-PEER-FD

\ Peer storage (32 max peers)
32 CONSTANT MAX-PEERS
CREATE PEER-FDS 32 CELLS ALLOT
CREATE PEER-IPS 32 16 * ALLOT  \ 16 bytes per IP (for IPv4 storage)
VARIABLE PEER-COUNT

: INIT-PEER-TABLE ( -- )
    0 PEER-COUNT !
    PEER-FDS 32 CELLS ERASE
    PEER-IPS 32 16 * ERASE ;

\ Add peer to table
: ADD-PEER ( fd ip-addr -- )
    PEER-COUNT @ MAX-PEERS < IF
        PEER-COUNT @ CELLS PEER-IPS + 16 CMOVE  \ Store IP
        PEER-COUNT @ CELLS PEER-FDS + !          \ Store FD
        1 PEER-COUNT +!
    ELSE
        DROP DROP
        ." [NETWORK] Max peers reached!" CR
    THEN ;

\ Remove peer from table
: REMOVE-PEER ( fd -- )
    0 MAX-PEERS 0 DO
        I CELLS PEER-FDS + @ OVER = IF
            DROP I LEAVE
        THEN
    LOOP
    \ Shift remaining peers
    DUP 1+ MAX-PEERS SWAP DO
        I CELLS PEER-FDS + @
        I 1- CELLS PEER-FDS + !
    LOOP
    -1 PEER-COUNT +! ;

\ ---------------------------------------------------------
\ 6. TCP Socket Primitives
\ ---------------------------------------------------------

\ Create listening socket
: CREATE-LISTEN-SOCKET ( port -- fd )
    \ AF_INET (2), SOCK_STREAM (1), PROTOCOL (0)
    2 1 0 socket
    DUP 0< IF
        DROP 
        ." [NETWORK] Socket creation failed!" CR
        0 EXIT
    THEN
    DUP LISTEN-FD !
    
    \ Set SO_REUSEADDR
    DUP 1 1 15 setsockopt DROP  \ SOL_SOCKET=1, SO_REUSEADDR=2
    
    \ Bind to port
    DROP CHAIN-PORT ;  \ Simplified - needs proper sockaddr setup

\ Accept incoming connection
: ACCEPT-CONNECTION ( listen-fd -- peer-fd )
    \ Simplified accept - needs sockaddr buffer
    0 0 accept
    DUP 0< IF
        DROP 0
        ." [NETWORK] Accept failed!" CR
    THEN ;

\ Connect to peer
: CONNECT-TO-PEER ( ip-str port -- fd )
    \ Create socket
    2 1 0 socket
    DUP 0< IF
        DROP DROP
        ." [NETWORK] Socket creation failed for outbound!" CR
        0 EXIT
    THEN
    \ Simplified - needs proper sockaddr setup and connect() call
    NIP ;

\ Send data to peer
: SEND-TO-PEER ( data-addr len fd -- bytes-sent )
    >R  \ Save fd
    2DUP  \ data-addr len data-addr len
    R> 0 send  \ fd flags=0
    DUP 0< IF
        DROP
        ." [NETWORK] Send failed!" CR
        0
    THEN ;

\ Receive data from peer
: RECV-FROM-PEER ( buffer-addr max-len fd -- bytes-received )
    >R  \ Save fd
    R> 0 recv  \ fd flags=0
    DUP 0< IF
        DROP
        ." [NETWORK] Recv failed!" CR
        0
    THEN ;

\ Close socket
: CLOSE-SOCKET ( fd -- )
    close-socket DROP ;

\ ---------------------------------------------------------
\ 7. Message Sending/Receiving
\ ---------------------------------------------------------

\ Send message with header
: SEND-MESSAGE ( payload-addr payload-len cmd-str cmd-len peer-fd -- )
    >R  \ Save peer-fd
    
    \ Build header in HEADER-BUF
    HEADER-BUF 24 ERASE
    
    \ Magic
    MAGIC-MAINNET HEADER-BUF !
    
    \ Command (12 bytes, null-padded)
    2OVER HEADER-BUF 4 + 12 MIN CMOVE
    
    \ Payload length
    2SWAP 2DUP 
    HEADER-BUF 16 + !
    
    \ Checksum (simplified - just first 4 bytes of SHA256)
    2DUP MSG-BUFFER SWAP CMOVE
    MSG-BUFFER SWAP SHA256-HASH
    HASH-RESULT @ HEADER-BUF 20 + !
    
    \ Send header
    HEADER-BUF 24 R@ SEND-TO-PEER DROP
    
    \ Send payload
    MSG-BUFFER SWAP R> SEND-TO-PEER DROP ;

\ Receive message (blocking)
: RECV-MESSAGE ( peer-fd -- payload-addr payload-len flag )
    \ Receive header (24 bytes)
    HEADER-BUF 24 ROT RECV-FROM-PEER
    24 < IF
        0 0 FALSE EXIT  \ Connection closed or error
    THEN
    
    \ Validate magic
    HEADER-BUF VALIDATE-MAGIC 0= IF
        ." [NETWORK] Invalid magic!" CR
        0 0 FALSE EXIT
    THEN
    
    \ Get payload length
    HEADER-BUF PARSE-LENGTH
    DUP 1024 > IF  \ Limit to buffer size
        DROP
        ." [NETWORK] Payload too large!" CR
        0 0 FALSE EXIT
    THEN
    
    \ Receive payload
    DUP >R MSG-BUFFER SWAP OVER RECV-FROM-PEER
    R> < IF
        ." [NETWORK] Incomplete payload!" CR
        0 0 FALSE EXIT
    THEN
    
    \ Return payload
    MSG-BUFFER R> TRUE ;

\ ---------------------------------------------------------
\ 8. Protocol Handshake
\ ---------------------------------------------------------

\ Send version message
: SEND-VERSION ( peer-fd -- )
    CONSTRUCT-VERSION
    CMD-VERSION OVER
    SEND-MESSAGE ;

\ Send verack
: SEND-VERACK ( peer-fd -- )
    0 0 CMD-VERACK OVER SEND-MESSAGE ;

\ Perform handshake as initiator
: DO-HANDSHAKE-INITIATOR ( peer-fd -- success-flag )
    DUP SEND-VERSION
    
    \ Wait for version
    DUP RECV-MESSAGE 0= IF
        DROP FALSE EXIT
    THEN
    DROP DROP  \ Drop payload
    
    \ Send verack
    DUP SEND-VERACK
    
    \ Wait for verack
    RECV-MESSAGE 0= IF
        FALSE EXIT
    THEN
    DROP DROP
    TRUE ;

\ Handle handshake as responder
: DO-HANDSHAKE-RESPONDER ( peer-fd -- success-flag )
    \ Wait for version
    DUP RECV-MESSAGE 0= IF
        DROP FALSE EXIT
    THEN
    DROP DROP
    
    \ Send version
    DUP SEND-VERSION
    
    \ Send verack
    DUP SEND-VERACK
    
    \ Wait for verack
    RECV-MESSAGE 0= IF
        FALSE EXIT
    THEN
    DROP DROP
    TRUE ;

\ ---------------------------------------------------------
\ 9. Connection Management
\ ---------------------------------------------------------

VARIABLE ACTIVE-PEERS

: INIT-NETWORK ( -- )
    0 ACTIVE-PEERS !
    INIT-PEER-TABLE
    ." [NETWORK] Network initialized." CR ;

: START-LISTENING ( -- )
    CHAIN-PORT CREATE-LISTEN-SOCKET
    DUP 0= IF
        ." [NETWORK] Failed to start listening!" CR
        EXIT
    THEN
    ." [NETWORK] Listening on port " CHAIN-PORT . CR ;

: ACCEPT-AND-ADD-PEER ( -- )
    LISTEN-FD @ ACCEPT-CONNECTION
    DUP 0> IF
        DUP ." [NETWORK] Accepted peer FD=" . CR
        DUP DO-HANDSHAKE-RESPONDER IF
            ." [NETWORK] Handshake complete!" CR
            0 ADD-PEER  \ IP is 0 for accepted connections
        ELSE
            ." [NETWORK] Handshake failed!" CR
            CLOSE-SOCKET
        THEN
    THEN ;

: CONNECT-AND-ADD-PEER ( ip-str port -- )
    CONNECT-TO-PEER
    DUP 0> IF
        DUP ." [NETWORK] Connected to peer FD=" . CR
        DUP DO-HANDSHAKE-INITIATOR IF
            ." [NETWORK] Handshake complete!" CR
            SWAP ADD-PEER
        ELSE
            ." [NETWORK] Handshake failed!" CR
            CLOSE-SOCKET
        THEN
    ELSE
        DROP
    THEN ;

: SHUTDOWN-NETWORK ( -- )
    ." [NETWORK] Shutting down..." CR
    \ Close all peer connections
    PEER-COUNT @ 0 DO
        I CELLS PEER-FDS + @
        DUP 0> IF CLOSE-SOCKET THEN
    LOOP
    \ Close listening socket
    LISTEN-FD @ DUP 0> IF CLOSE-SOCKET THEN
    0 PEER-COUNT !
    ." [NETWORK] Shutdown complete." CR ;

\ ---------------------------------------------------------
\ 10. Block & Transaction Broadcasting
\ ---------------------------------------------------------

\ Broadcast block to all peers
: BROADCAST-BLOCK ( block-addr -- )
    PEER-COUNT @ 0= IF
        DROP
        ." [NETWORK] No peers to broadcast to!" CR
        EXIT
    THEN
    
    PEER-COUNT @ 0 DO
        I CELLS PEER-FDS + @
        DUP 0> IF
            \ Send block (simplified - needs serialization)
            2DUP 80 CMD-BLOCK 5 ROT SEND-MESSAGE
        ELSE
            DROP
        THEN
    LOOP
    ." [NETWORK] Block broadcast to " PEER-COUNT @ . ." peers" CR ;

\ Broadcast transaction to all peers
: BROADCAST-TX ( tx-addr -- )
    PEER-COUNT @ 0= IF
        DROP
        ." [NETWORK] No peers to broadcast to!" CR
        EXIT
    THEN
    
    PEER-COUNT @ 0 DO
        I CELLS PEER-FDS + @
        DUP 0> IF
            \ Serialize and send transaction
            2DUP SERIALIZE-TX
            CMD-TX 2 ROT SEND-MESSAGE
        ELSE
            DROP
        THEN
    LOOP
    ." [NETWORK] Transaction broadcast to " PEER-COUNT @ . ." peers" CR ;

\ Request block from peer
: REQUEST-BLOCK ( block-hash peer-fd -- )
    >R
    \ Send getdata message with block hash
    32 CMD-GETDATA 7 R> SEND-MESSAGE ;

\ ---------------------------------------------------------
\ 11. Blockchain Synchronization
\ ---------------------------------------------------------

\ Sync state
VARIABLE SYNC-PEER-FD
VARIABLE SYNC-HEIGHT
VARIABLE SYNC-IN-PROGRESS

: START-SYNC ( peer-fd -- )
    SYNC-PEER-FD !
    0 SYNC-HEIGHT !
    TRUE SYNC-IN-PROGRESS !
    ." [NETWORK] Starting blockchain sync..." CR ;

\ Request headers for sync
: REQUEST-HEADERS ( start-height peer-fd -- )
    >R
    \ Create getheaders message (simplified)
    CREATE-GETHEADERS-MSG
    CMD-GETHEADERS 11 R> SEND-MESSAGE ;

: CREATE-GETHEADERS-MSG ( start-height -- addr len )
    MSG-BUFFER 256 ERASE
    MSG-BUFFER
    
    \ Version
    70015 OVER ! 4 +
    
    \ Hash count (1 for start)
    1 OVER C! 1+
    
    \ Block hash at start height (simplified - use genesis)
    GENESIS-HASH OVER 32 CMOVE 32 +
    
    \ Stop hash (all zeros for "give me everything")
    0 OVER ! 4 +
    0 OVER ! 4 +
    0 OVER ! 4 +
    0 OVER ! 4 +
    0 OVER ! 4 +
    0 OVER ! 4 +
    0 OVER ! 4 +
    0 OVER ! 4 +
    
    MSG-BUFFER - MSG-BUFFER SWAP ;

\ Process received headers and request blocks
: PROCESS-HEADERS ( headers-addr count -- )
    ." [NETWORK] Received " DUP . ." headers" CR
    0 DO
        \ Each header is 80 bytes
        DUP I 80 * +
        
        \ Request full block
        DUP SYNC-PEER-FD @ REQUEST-BLOCK
        
        1 SYNC-HEIGHT +!
    LOOP
    DROP ;

\ Process received block during sync
: PROCESS-SYNC-BLOCK ( block-addr -- )
    \ Validate block
    DUP VALIDATE-BLOCK IF
        \ Add to blockchain
        ADD-BLOCK
        ." [NETWORK] Sync block added, height=" SYNC-HEIGHT @ . CR
        
        \ Check if sync complete (simplified)
        SYNC-HEIGHT @ 1000 >= IF
            ." [NETWORK] Sync complete!" CR
            FALSE SYNC-IN-PROGRESS !
        THEN
    ELSE
        ." [NETWORK] Invalid block during sync!" CR
        FALSE SYNC-IN-PROGRESS !
    THEN ;

\ ---------------------------------------------------------
\ 12. Peer Message Loop (Background Processing)
\ ---------------------------------------------------------

VARIABLE NET-RUNNING

: PROCESS-PEER-MESSAGES ( -- )
    PEER-COUNT @ 0 DO
        I CELLS PEER-FDS + @
        DUP 0> IF
            \ Non-blocking check for messages (simplified)
            DUP RECV-MESSAGE IF
                \ Process message based on command
                HEADER-BUF PARSE-COMMAND
                
                \ Check command type
                2DUP CMD-BLOCK COMPARE 0= IF
                    ." [NETWORK] Received block from peer" CR
                    SYNC-IN-PROGRESS @ IF
                        \ During sync, process sequentially
                        MSG-BUFFER PROCESS-SYNC-BLOCK
                    ELSE
                        \ Normal operation, validate and maybe add
                        MSG-BUFFER DUP VALIDATE-BLOCK IF
                            ADD-BLOCK
                            ." [NETWORK] New block added to chain" CR
                        ELSE
                            DROP
                            ." [NETWORK] Invalid block rejected" CR
                        THEN
                    THEN
                THEN
                
                2DUP CMD-TX COMPARE 0= IF
                    ." [NETWORK] Received transaction from peer" CR
                    \ Add to mempool
                    MSG-BUFFER MEMPOOL-ADD
                THEN
                
                2DUP s" headers" COMPARE 0= IF
                    ." [NETWORK] Received headers" CR
                    \ Process headers during sync
                    MSG-BUFFER C@  \ Header count
                    MSG-BUFFER 1+ SWAP PROCESS-HEADERS
                THEN
                
                2DROP DROP DROP
            ELSE
                DROP DROP
            THEN
        ELSE
            DROP
        THEN
    LOOP ;

: RUN-NETWORK-LOOP ( -- )
    TRUE NET-RUNNING !
    BEGIN
        NET-RUNNING @ WHILE
        \ Accept new connections
        LISTEN-FD @ 0> IF
            ACCEPT-AND-ADD-PEER
        THEN
        
        \ Process messages from existing peers
        PROCESS-PEER-MESSAGES
        
        \ Small delay (10ms)
        10 MS
    REPEAT ;

: STOP-NETWORK-LOOP ( -- )
    FALSE NET-RUNNING ! ;

CR ." [NETWORK] P2P networking loaded (sockets + protocol + sync)." CR
