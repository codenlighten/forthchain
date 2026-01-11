\ =========================================================
\ Mempool (Memory Pool)
\ =========================================================
\ Hold pending transactions before inclusion in blocks
\
\ Features:
\   - Add transaction to pool
\   - Remove transaction from pool
\   - Get transactions for block building
\   - Validate transactions before adding

\ Dependencies are loaded by load.fs in correct order

\ =========================================================
\ Mempool Structure
\ =========================================================

256 CONSTANT MAX-MEMPOOL-TXS
CREATE MEMPOOL MAX-MEMPOOL-TXS CELLS ALLOT  \ Array of transaction pointers
VARIABLE MEMPOOL-COUNT
0 MEMPOOL-COUNT !

\ =========================================================
\ Mempool Operations
\ =========================================================

\ Add transaction to mempool
: MEMPOOL-ADD ( tx-addr -- success? )
    \ Check if mempool is full
    MEMPOOL-COUNT @ MAX-MEMPOOL-TXS >= IF
        DROP FALSE EXIT
    THEN
    
    \ TODO: Validate transaction
    \ - Check signatures
    \ - Verify inputs exist in UTXO set
    \ - Check no double spends
    \ - Verify amounts
    
    \ Add to mempool
    MEMPOOL-COUNT @ CELLS MEMPOOL + !
    1 MEMPOOL-COUNT +!
    TRUE ;

\ Remove transaction from mempool (after including in block)
: MEMPOOL-REMOVE ( tx-addr -- )
    0 SWAP                      \ index tx-addr
    MEMPOOL-COUNT @ 0 ?DO
        \ Find transaction in mempool
        DUP I CELLS MEMPOOL + @ = IF
            DROP I LEAVE
        THEN
    LOOP
    
    \ Shift remaining transactions down
    DUP MEMPOOL-COUNT @ 1- < IF
        DUP 1+ CELLS MEMPOOL +  \ Source
        OVER CELLS MEMPOOL +    \ Destination
        MEMPOOL-COUNT @ OVER - CELLS  \ Count
        CMOVE
    THEN
    DROP
    
    -1 MEMPOOL-COUNT +! ;

\ Get transaction by index
: MEMPOOL-GET ( index -- tx-addr )
    CELLS MEMPOOL + @ ;

\ Clear mempool
: MEMPOOL-CLEAR ( -- )
    0 MEMPOOL-COUNT !
    MEMPOOL MAX-MEMPOOL-TXS CELLS 0 FILL ;

\ =========================================================
\ Transaction Selection for Block Building
\ =========================================================

\ Get transactions for next block (simple: take first N)
: MEMPOOL-SELECT-TXS ( max-count -- tx-array count )
    MEMPOOL-COUNT @ MIN         \ Don't exceed available
    DUP 0= IF
        DROP MEMPOOL 0 EXIT
    THEN
    MEMPOOL SWAP ;

\ =========================================================
\ Transaction Validation
\ =========================================================

\ Validate transaction before adding to mempool
: VALIDATE-TX ( tx-addr -- valid? )
    \ 1. Check version
    DUP TX-VERSION @ 1 = 0= IF
        DROP FALSE EXIT
    THEN
    
    \ 2. Check has at least one input
    DUP TX-INPUTCOUNT C@ 0= IF
        DROP FALSE EXIT
    THEN
    
    \ 3. Check has at least one output
    DUP TX-OUTPUTCOUNT C@ 0= IF
        DROP FALSE EXIT
    THEN
    
    \ 4. TODO: Verify signatures on all inputs
    \ 5. TODO: Check inputs exist in UTXO set
    \ 6. TODO: Verify no double-spending
    \ 7. TODO: Check input sum >= output sum (with fees)
    
    DROP TRUE ;

\ =========================================================
\ Display Functions
\ =========================================================

: .MEMPOOL ( -- )
    CR ." Mempool:" CR
    ."   Count: " MEMPOOL-COUNT @ . CR
    MEMPOOL-COUNT @ 0 ?DO
        ."   TX[" I . ." ]: "
        I MEMPOOL-GET .TX
    LOOP ;

\ =========================================================
\ Mempool Statistics
\ =========================================================

VARIABLE TOTAL-MEMPOOL-SIZE
0 TOTAL-MEMPOOL-SIZE !

: MEMPOOL-STATS ( -- )
    0 TOTAL-MEMPOOL-SIZE !
    
    \ Calculate total size
    MEMPOOL-COUNT @ 0 ?DO
        I MEMPOOL-GET TX-SIZE + TOTAL-MEMPOOL-SIZE +!
    LOOP
    
    CR ." Mempool Statistics:" CR
    ."   Transactions: " MEMPOOL-COUNT @ . CR
    ."   Total Size: " TOTAL-MEMPOOL-SIZE @ . ." bytes" CR
    ."   Capacity: " MAX-MEMPOOL-TXS . ." transactions" CR
    ."   Used: " MEMPOOL-COUNT @ 100 * MAX-MEMPOOL-TXS / . ." %" CR ;

\ =========================================================
\ Initialization
\ =========================================================

CR ." =========================================" CR
CR ." Mempool loaded" CR
CR ." =========================================" CR
CR ." Capacity: " MAX-MEMPOOL-TXS . ." transactions" CR
CR ." =========================================" CR
CR ." Functions:" CR
CR ."   - MEMPOOL-ADD" CR
CR ."   - MEMPOOL-REMOVE" CR
CR ."   - MEMPOOL-SELECT-TXS" CR
CR ."   - VALIDATE-TX" CR
CR ." =========================================" CR
