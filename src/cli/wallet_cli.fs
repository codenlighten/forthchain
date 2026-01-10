\ =========================================================
\ FORTHCOIN WALLET CLI
\ =========================================================
\ Command-line interface for wallet operations

\ ---------------------------------------------------------
\ 1. Wallet State
\ ---------------------------------------------------------

CREATE CURRENT-WALLET WALLET-SIZE ALLOT
VARIABLE WALLET-LOADED

: INIT-CLI ( -- )
    CURRENT-WALLET WALLET-SIZE ERASE
    FALSE WALLET-LOADED ! ;

\ ---------------------------------------------------------
\ 2. Wallet Commands
\ ---------------------------------------------------------

\ Create new wallet
: CMD-NEW-WALLET ( -- )
    CURRENT-WALLET INIT-WALLET
    TRUE WALLET-LOADED !
    
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              NEW WALLET CREATED                        ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    \ Display address
    ." Address: "
    CURRENT-WALLET WALLET-ADDRESS 25 0 DO
        DUP I + C@ .BYTE
    LOOP
    DROP CR
    
    ." " CR
    ." Private key: "
    CURRENT-WALLET WALLET-PRIVKEY 32 0 DO
        DUP I + C@ .BYTE
    LOOP
    DROP CR
    
    ." " CR
    ." ⚠️  SAVE YOUR PRIVATE KEY! This cannot be recovered." CR
    ." " CR ;

\ Load existing wallet
: CMD-LOAD-WALLET ( filename-str filename-len -- )
    ." Loading wallet from file..." CR
    CURRENT-WALLET -ROT LOAD-WALLET IF
        TRUE WALLET-LOADED !
        ." ✓ Wallet loaded successfully!" CR
        ." Address: "
        CURRENT-WALLET WALLET-ADDRESS 25 0 DO
            DUP I + C@ .BYTE
        LOOP
        DROP CR
    ELSE
        ." ✗ Failed to load wallet!" CR
    THEN ;

\ Save wallet to file
: CMD-SAVE-WALLET ( filename-str filename-len -- )
    ." Saving wallet..." CR
    CURRENT-WALLET -ROT SAVE-WALLET
    ." ✓ Wallet saved successfully!" CR ;

\ Show balance
: CMD-BALANCE ( -- )
    WALLET-LOADED @ 0= IF
        ." ✗ No wallet loaded! Use 'new' or 'load' first." CR
        EXIT
    THEN
    
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║                  WALLET BALANCE                        ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    CURRENT-WALLET GET-BALANCE
    ." Balance: " U. ." satoshis" CR
    DUP 100000000 / ." (" U. ." FORTH)" CR
    ." " CR ;

\ Show address
: CMD-ADDRESS ( -- )
    WALLET-LOADED @ 0= IF
        ." ✗ No wallet loaded!" CR
        EXIT
    THEN
    
    ." " CR
    ." Your address:" CR
    CURRENT-WALLET WALLET-ADDRESS 25 0 DO
        DUP I + C@ .BYTE
    LOOP
    DROP CR
    ." " CR ;

\ List UTXOs
: CMD-LIST-UTXOS ( -- )
    WALLET-LOADED @ 0= IF
        ." ✗ No wallet loaded!" CR
        EXIT
    THEN
    
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║                  UNSPENT OUTPUTS                       ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    UTXO-COUNT @ 0= IF
        ." No UTXOs found." CR
        EXIT
    THEN
    
    UTXO-COUNT @ 0 DO
        I UTXO-ENTRY-SIZE * UTXO-SET +
        ." [" I . ." ] " 
        
        \ Show txhash (first 8 bytes)
        DUP 8 0 DO
            DUP I + C@ .BYTE
        LOOP
        ." ..."
        
        \ Show index
        ."  #" DUP 32 + @ .
        
        \ Show value
        ."  Value: " DUP 36 + @ U. ." satoshis" CR
        
        DROP
    LOOP
    ." " CR ;

\ Send transaction
: CMD-SEND ( dest-addr value -- )
    WALLET-LOADED @ 0= IF
        DROP DROP
        ." ✗ No wallet loaded!" CR
        EXIT
    THEN
    
    ." " CR
    ." Creating transaction..." CR
    
    \ Create transaction
    CURRENT-WALLET -ROT CREATE-SEND-TX
    DUP 0= IF
        DROP
        ." ✗ Failed to create transaction (insufficient funds?)" CR
        EXIT
    THEN
    
    ." ✓ Transaction created!" CR
    
    \ Sign transaction
    CURRENT-WALLET WALLET-SIGN-TX
    ." ✓ Transaction signed!" CR
    
    \ Add to mempool
    DUP MEMPOOL-ADD IF
        ." ✓ Added to mempool" CR
        
        \ Broadcast to network
        BROADCAST-TX
        ." ✓ Broadcast to network" CR
    ELSE
        DROP
        ." ✗ Mempool rejected transaction!" CR
    THEN
    
    ." " CR ;

\ ---------------------------------------------------------
\ 3. Mining Commands
\ ---------------------------------------------------------

\ Mine a block
: CMD-MINE ( -- )
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║                  MINING BLOCK                          ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    \ Create coinbase transaction
    WALLET-LOADED @ IF
        CURRENT-WALLET 5000000000 CREATE-COINBASE-TX
    ELSE
        \ Mine to a dummy address if no wallet
        CREATE-BLOCK-HEADER
    THEN
    
    ." Mining with difficulty target..." CR
    ." (This may take a while)" CR
    ." " CR
    
    \ TODO: Integrate with actual mining
    ." Press Ctrl+C to stop mining." CR ;

\ ---------------------------------------------------------
\ 4. Network Commands  
\ ---------------------------------------------------------

\ Connect to peer
: CMD-CONNECT ( ip-str port -- )
    ." Connecting to peer..." CR
    CONNECT-AND-ADD-PEER
    ." " CR ;

\ List peers
: CMD-PEERS ( -- )
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║                  CONNECTED PEERS                       ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    PEER-COUNT @ 0= IF
        ." No peers connected." CR
    ELSE
        ." Connected peers: " PEER-COUNT @ . CR
        PEER-COUNT @ 0 DO
            ." [" I . ." ] FD=" I CELLS PEER-FDS + @ . CR
        LOOP
    THEN
    ." " CR ;

\ ---------------------------------------------------------
\ 5. Blockchain Info Commands
\ ---------------------------------------------------------

\ Show blockchain status
: CMD-STATUS ( -- )
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              FORTHCOIN NODE STATUS                     ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    ." Blockchain Height: " CHAIN-HEIGHT @ . CR
    ." Mempool Size: " MEMPOOL-SIZE @ . ." transactions" CR
    ." UTXO Set Size: " UTXO-COUNT @ . ." outputs" CR
    ." Connected Peers: " PEER-COUNT @ . CR
    
    WALLET-LOADED @ IF
        ." Wallet: Loaded" CR
        ." Balance: " CURRENT-WALLET GET-BALANCE U. ." satoshis" CR
    ELSE
        ." Wallet: Not loaded" CR
    THEN
    
    ." " CR ;

\ Show mempool
: CMD-MEMPOOL ( -- )
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║                     MEMPOOL                            ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    
    MEMPOOL-STATS ;

\ ---------------------------------------------------------
\ 6. Help System
\ ---------------------------------------------------------

: CMD-HELP ( -- )
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║              FORTHCOIN WALLET COMMANDS                 ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    ." WALLET MANAGEMENT:" CR
    ."   new              Create new wallet" CR
    ."   load <file>      Load wallet from file" CR
    ."   save <file>      Save wallet to file" CR
    ." " CR
    ." TRANSACTIONS:" CR
    ."   balance          Show wallet balance" CR
    ."   address          Show your address" CR
    ."   utxos            List unspent outputs" CR
    ."   send <addr> <amount>  Send coins" CR
    ." " CR
    ." MINING:" CR
    ."   mine             Mine a new block" CR
    ." " CR
    ." NETWORK:" CR
    ."   connect <ip> <port>   Connect to peer" CR
    ."   peers            List connected peers" CR
    ." " CR
    ." BLOCKCHAIN:" CR
    ."   status           Show node status" CR
    ."   mempool          Show pending transactions" CR
    ." " CR
    ." SYSTEM:" CR
    ."   help             Show this help" CR
    ."   quit             Exit wallet" CR
    ." " CR ;

\ ---------------------------------------------------------
\ 7. Command Parser
\ ---------------------------------------------------------

CREATE CMD-BUFFER 256 ALLOT

: PARSE-CMD ( -- )
    CMD-BUFFER 256 ACCEPT
    CMD-BUFFER SWAP
    
    \ Parse command
    2DUP s" new" COMPARE 0= IF
        2DROP CMD-NEW-WALLET EXIT
    THEN
    
    2DUP s" balance" COMPARE 0= IF
        2DROP CMD-BALANCE EXIT
    THEN
    
    2DUP s" address" COMPARE 0= IF
        2DROP CMD-ADDRESS EXIT
    THEN
    
    2DUP s" utxos" COMPARE 0= IF
        2DROP CMD-LIST-UTXOS EXIT
    THEN
    
    2DUP s" peers" COMPARE 0= IF
        2DROP CMD-PEERS EXIT
    THEN
    
    2DUP s" status" COMPARE 0= IF
        2DROP CMD-STATUS EXIT
    THEN
    
    2DUP s" mempool" COMPARE 0= IF
        2DROP CMD-MEMPOOL EXIT
    THEN
    
    2DUP s" mine" COMPARE 0= IF
        2DROP CMD-MINE EXIT
    THEN
    
    2DUP s" help" COMPARE 0= IF
        2DROP CMD-HELP EXIT
    THEN
    
    2DUP s" quit" COMPARE 0= IF
        2DROP BYE
    THEN
    
    \ Unknown command
    2DROP
    ." ✗ Unknown command. Type 'help' for available commands." CR ;

\ ---------------------------------------------------------
\ 8. Main CLI Loop
\ ---------------------------------------------------------

: WALLET-CLI ( -- )
    INIT-CLI
    
    ." " CR
    ." ╔════════════════════════════════════════════════════════╗" CR
    ." ║                                                        ║" CR
    ." ║          ███████╗ ██████╗ ██████╗ ████████╗           ║" CR
    ." ║          ██╔════╝██╔═══██╗██╔══██╗╚══██╔══╝           ║" CR
    ." ║          █████╗  ██║   ██║██████╔╝   ██║              ║" CR
    ." ║          ██╔══╝  ██║   ██║██╔══██╗   ██║              ║" CR
    ." ║          ██║     ╚██████╔╝██║  ██║   ██║              ║" CR
    ." ║          ╚═╝      ╚═════╝ ╚═╝  ╚═╝   ╚═╝              ║" CR
    ." ║                                                        ║" CR
    ." ║                 FORTHCOIN WALLET                      ║" CR
    ." ║                  Version 1.0                          ║" CR
    ." ║                                                        ║" CR
    ." ╚════════════════════════════════════════════════════════╝" CR
    ." " CR
    ." Type 'help' for available commands." CR
    ." " CR
    
    BEGIN
        ." forth> " 
        PARSE-CMD
    AGAIN ;

\ Helper for hex byte display
: .BYTE ( c -- )
    BASE @ >R HEX
    DUP 4 RSHIFT 15 AND
    DUP 10 < IF 48 + ELSE 55 + THEN EMIT
    15 AND
    DUP 10 < IF 48 + ELSE 55 + THEN EMIT
    R> BASE ! ;

CR ." [CLI] Wallet CLI loaded. Type 'wallet-cli' to start." CR
