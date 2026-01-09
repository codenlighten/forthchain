\ =========================================================
\ FORTHCOIN NODE - MAIN ENTRY POINT
\ =========================================================

\ Load all modules
INCLUDE src/load.fs

CR
CR ." ========================================" CR
CR ." FORTHCOIN - Blockchain in Pure Forth" CR
CR ." ========================================" CR
CR

\ Configuration
: SHOW-STATUS ( -- )
    CR ." [STATUS] Node Information:" CR
    ." Block Height: " GET-BLOCK-HEIGHT . CR
    ." Network Port: " CHAIN-PORT . CR
    ." Peer Count: " PEER-COUNT @ . CR ;

\ Initialize all subsystems
: INIT-NODE ( -- )
    CR ." [INIT] Initializing ForthCoin node..." CR
    
    \ 1. Initialize storage
    ." [INIT] Opening blockchain database..." CR
    OPEN-DB
    CLEAR-INDEX
    ." [INIT] Database ready." CR
    
    \ 2. Initialize network
    ." [INIT] Initializing network..." CR
    INIT-NETWORK
    ." [INIT] Network ready." CR
    
    \ 3. Show status
    SHOW-STATUS
    
    CR ." [INIT] Node initialization complete!" CR ;

\ Shutdown all subsystems
: SHUTDOWN-NODE ( -- )
    CR ." [SHUTDOWN] Shutting down node..." CR
    SHUTDOWN-NETWORK
    CLOSE-DB
    CR ." [SHUTDOWN] Goodbye!" CR ;

\ Main node loop (simplified - no actual mining/networking yet)
: NODE-LOOP ( -- )
    CR ." [NODE] Node is running. Press Ctrl+C to exit." CR
    CR ." [NODE] Available commands:" CR
    ."   SHOW-STATUS  - Display node status" CR
    ."   SHUTDOWN-NODE - Exit the node" CR
    CR
    
    \ Drop into interactive mode
    ." Type commands at the prompt below:" CR ;

\ Start the node (for interactive use)
: START-NODE ( -- )
    INIT-NODE
    NODE-LOOP ;

CR ." [MAIN] ForthCoin node loaded." CR
CR ." Type 'START-NODE' to begin, or run tests with 'make test'" CR
CR
