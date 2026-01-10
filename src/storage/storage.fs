\ =========================================================
\ FORTHCOIN STORAGE ENGINE
\ =========================================================
\ Requires: math256.fs, crypto.fs

\ Configuration
65536 CONSTANT BUCKETS  \ Index size (2^16)

\ Global File Handle
VARIABLE DB-FILE-ID

\ Temp buffer for hash operations
CREATE TEST-HASH-OUT 32 ALLOT

\ ---------------------------------------------------------
\ 1. Low-Level File I/O
\ ---------------------------------------------------------

: OPEN-DB ( -- )
    s" blockchain.dat" R/W OPEN-FILE 
    IF DROP 
        \ File missing? Create it.
        s" blockchain.dat" R/W CREATE-FILE ABORT" Failed to create DB"
    THEN
    DB-FILE-ID ! ;

: CLOSE-DB ( -- )
    DB-FILE-ID @ ?DUP IF CLOSE-FILE DROP THEN ;

\ Write 32-bit integer (Little Endian)
: WRITE-32 ( n -- )
    HERE ! 
    HERE 4 DB-FILE-ID @ WRITE-FILE ABORT" Disk Write Error" ;

\ Read 32-bit integer
: READ-32 ( -- n )
    HERE 4 DB-FILE-ID @ READ-FILE DROP
    HERE @ ;

\ ---------------------------------------------------------
\ 2. The Index (Chained Hash Table)
\ ---------------------------------------------------------
\ Memory Layout: [Buckets Array (64K cells)] -> [Node] -> [Node]
\ Node Structure: [Next_Ptr] [File_Offset] [32-byte Hash]

CREATE INDEX-TABLE BUCKETS CELLS ALLOT

: CLEAR-INDEX ( -- )
    INDEX-TABLE BUCKETS CELLS ERASE ;

: HASH->BUCKET ( hash_addr -- bucket_addr )
    W@ CELLS INDEX-TABLE + ;

: ADD-TO-INDEX ( hash_addr offset -- )
    \ 1. Allocate Node (Cell + Cell + 32)
    ALIGN HERE >R    ( Save Node Address )
    2 CELLS 32 + ALLOT
    
    \ 2. Fill Node
    SWAP R@ CELL+ !         ( Store Offset )
    R@ 2 CELLS + 32 CMOVE   ( Copy Hash )
    
    \ 3. Link into Bucket
    R@ 2 CELLS + HASH->BUCKET ( bucket_addr )
    DUP @ R@ !              ( Node.Next = Bucket.Head )
    R@ SWAP !               ( Bucket.Head = Node )
    
    R> DROP ;

: FIND-OFFSET ( hash_addr -- offset -1 | 0 )
    \ 1. Get Bucket
    HASH->BUCKET @ ( node_ptr )
    
    \ 2. Walk the Linked List
    BEGIN
        DUP 0<>  \ While node is not null
    WHILE
        \ Compare Hash (Node+8 vs SearchHash)
        DUP 2 CELLS + OVER 32 COMPARE 0= IF
            \ MATCH FOUND!
            CELL+ @ TRUE EXIT
        THEN
        \ Next Node
        @ 
    REPEAT
    
    \ Not Found
    DROP FALSE ;

\ ---------------------------------------------------------
\ 3. Block Manager (Public API)
\ ---------------------------------------------------------

: WRITE-BLOCK ( block_addr block_len -- )
    \ 1. Get Write Position (End of File)
    DB-FILE-ID @ FILE-SIZE DROP ( offset_low )
    >R  \ Save offset
    DB-FILE-ID @ FILE-SIZE DROP DB-FILE-ID @ REPOSITION-FILE DROP
    
    \ 2. Calculate Hash (for Index) - simplified for now
    \ TODO: Replace with actual RUN-HASH256 when implemented
    OVER TEST-HASH-OUT 32 CMOVE  ( Copy first 32 bytes as "hash" )
    TEST-HASH-OUT R@ ADD-TO-INDEX ( Add offset to Index )
    
    \ 3. Write Envelope
    $D9B4BEF9 WRITE-32  ( Magic )
    DUP       WRITE-32  ( Size )
    
    \ 4. Write Data
    DB-FILE-ID @ WRITE-FILE ABORT" Block Data Write Error" 
    
    \ 5. Sync to Disk
    DB-FILE-ID @ FLUSH-FILE DROP 
    R> DROP ;

: READ-BLOCK ( hash_addr buffer -- success? )
    SWAP FIND-OFFSET 0= IF 
        DROP FALSE EXIT \ Unknown Block
    THEN
    
    ( buffer offset )
    
    \ 1. Seek to Offset
    0 DB-FILE-ID @ REPOSITION-FILE ABORT" Seek Error"
    
    \ 2. Read Envelope (Magic+Size = 8 bytes)
    HERE 8 DB-FILE-ID @ READ-FILE DROP
    
    \ 3. Get size from envelope
    HERE 4 + @ ( size )
    
    \ 4. Read Block Data
    ROT SWAP DB-FILE-ID @ READ-FILE DROP
    TRUE ;

: GET-BLOCK-HEIGHT ( -- height )
    \ TODO: Track height in index
    \ For now return 0
    0 ;

\ ---------------------------------------------------------
\ 4. UTXO Set Persistence
\ ---------------------------------------------------------

\ UTXO file for persistent state
VARIABLE UTXO-FILE-ID

: OPEN-UTXO-DB ( -- )
    s" utxo.dat" R/W OPEN-FILE
    IF DROP
        s" utxo.dat" R/W CREATE-FILE ABORT" Failed to create UTXO DB"
    THEN
    UTXO-FILE-ID ! ;

: CLOSE-UTXO-DB ( -- )
    UTXO-FILE-ID @ ?DUP IF CLOSE-FILE DROP THEN ;

\ Save current UTXO set to disk
: SAVE-UTXO-SET ( -- )
    OPEN-UTXO-DB
    
    \ Write count
    UTXO-COUNT @ HERE ! 
    HERE 4 UTXO-FILE-ID @ WRITE-FILE DROP
    
    \ Write each UTXO (69 bytes each)
    UTXO-COUNT @ 0 ?DO
        I UTXO-ENTRY-SIZE * UTXO-SET +
        UTXO-ENTRY-SIZE UTXO-FILE-ID @ WRITE-FILE DROP
    LOOP
    
    UTXO-FILE-ID @ FLUSH-FILE DROP
    CLOSE-UTXO-DB
    ." [STORAGE] UTXO set saved (" UTXO-COUNT @ . ." entries)" CR ;

\ Load UTXO set from disk
: LOAD-UTXO-SET ( -- )
    s" utxo.dat" FILE-STATUS NIP IF
        ." [STORAGE] No UTXO file found, starting fresh" CR
        EXIT
    THEN
    
    OPEN-UTXO-DB
    
    \ Read count
    HERE 4 UTXO-FILE-ID @ READ-FILE IF
        DROP CLOSE-UTXO-DB EXIT
    THEN
    DROP
    HERE @ UTXO-COUNT !
    
    \ Read each UTXO
    UTXO-COUNT @ 0 ?DO
        I UTXO-ENTRY-SIZE * UTXO-SET +
        UTXO-ENTRY-SIZE UTXO-FILE-ID @ READ-FILE DROP
        DROP
    LOOP
    
    CLOSE-UTXO-DB
    ." [STORAGE] UTXO set loaded (" UTXO-COUNT @ . ." entries)" CR ;

\ ---------------------------------------------------------
\ 5. Mempool Persistence
\ ---------------------------------------------------------

VARIABLE MEMPOOL-FILE-ID

: OPEN-MEMPOOL-DB ( -- )
    s" mempool.dat" R/W OPEN-FILE
    IF DROP
        s" mempool.dat" R/W CREATE-FILE ABORT" Failed to create mempool DB"
    THEN
    MEMPOOL-FILE-ID ! ;

: CLOSE-MEMPOOL-DB ( -- )
    MEMPOOL-FILE-ID @ ?DUP IF CLOSE-FILE DROP THEN ;

\ Save mempool to disk
: SAVE-MEMPOOL ( -- )
    OPEN-MEMPOOL-DB
    
    \ Write count
    MEMPOOL-SIZE @ HERE !
    HERE 4 MEMPOOL-FILE-ID @ WRITE-FILE DROP
    
    \ Write each transaction
    MEMPOOL-SIZE @ 0 ?DO
        I TX-SIZE * MEMPOOL +
        TX-SIZE MEMPOOL-FILE-ID @ WRITE-FILE DROP
    LOOP
    
    MEMPOOL-FILE-ID @ FLUSH-FILE DROP
    CLOSE-MEMPOOL-DB
    ." [STORAGE] Mempool saved (" MEMPOOL-SIZE @ . ." transactions)" CR ;

\ Load mempool from disk
: LOAD-MEMPOOL ( -- )
    s" mempool.dat" FILE-STATUS NIP IF
        ." [STORAGE] No mempool file found, starting fresh" CR
        EXIT
    THEN
    
    OPEN-MEMPOOL-DB
    
    \ Read count
    HERE 4 MEMPOOL-FILE-ID @ READ-FILE IF
        DROP CLOSE-MEMPOOL-DB EXIT
    THEN
    DROP
    HERE @ MEMPOOL-SIZE !
    
    \ Read each transaction
    MEMPOOL-SIZE @ 0 ?DO
        I TX-SIZE * MEMPOOL +
        TX-SIZE MEMPOOL-FILE-ID @ READ-FILE DROP
        DROP
    LOOP
    
    CLOSE-MEMPOOL-DB
    ." [STORAGE] Mempool loaded (" MEMPOOL-SIZE @ . ." transactions)" CR ;

\ ---------------------------------------------------------
\ 6. Blockchain Metadata
\ ---------------------------------------------------------

VARIABLE META-FILE-ID
VARIABLE CHAIN-HEIGHT
VARIABLE CHAIN-WORK

: OPEN-META-DB ( -- )
    s" meta.dat" R/W OPEN-FILE
    IF DROP
        s" meta.dat" R/W CREATE-FILE ABORT" Failed to create meta DB"
        \ Initialize with genesis
        0 CHAIN-HEIGHT !
        0 0 CHAIN-WORK 2!
    THEN
    META-FILE-ID ! ;

: CLOSE-META-DB ( -- )
    META-FILE-ID @ ?DUP IF CLOSE-FILE DROP THEN ;

: SAVE-METADATA ( -- )
    OPEN-META-DB
    
    \ Write height (4 bytes)
    CHAIN-HEIGHT @ HERE !
    HERE 4 META-FILE-ID @ WRITE-FILE DROP
    
    \ Write total work (8 bytes)
    CHAIN-WORK 2@ HERE 2!
    HERE 8 META-FILE-ID @ WRITE-FILE DROP
    
    META-FILE-ID @ FLUSH-FILE DROP
    CLOSE-META-DB ;

: LOAD-METADATA ( -- )
    s" meta.dat" FILE-STATUS NIP IF
        ." [STORAGE] No metadata found, starting at genesis" CR
        0 CHAIN-HEIGHT !
        0 0 CHAIN-WORK 2!
        EXIT
    THEN
    
    OPEN-META-DB
    
    \ Read height
    HERE 4 META-FILE-ID @ READ-FILE IF
        DROP CLOSE-META-DB EXIT
    THEN
    DROP
    HERE @ CHAIN-HEIGHT !
    
    \ Read work
    HERE 8 META-FILE-ID @ READ-FILE IF
        DROP CLOSE-META-DB EXIT
    THEN
    DROP
    HERE 2@ CHAIN-WORK 2!
    
    CLOSE-META-DB
    ." [STORAGE] Blockchain height: " CHAIN-HEIGHT @ . CR ;

\ ---------------------------------------------------------
\ 7. High-Level Storage Management
\ ---------------------------------------------------------

: INIT-STORAGE ( -- )
    CLEAR-INDEX
    OPEN-DB
    LOAD-METADATA
    LOAD-UTXO-SET
    LOAD-MEMPOOL
    ." [STORAGE] Storage system initialized." CR ;

: SHUTDOWN-STORAGE ( -- )
    SAVE-METADATA
    SAVE-UTXO-SET
    SAVE-MEMPOOL
    CLOSE-DB
    ." [STORAGE] Storage system shutdown." CR ;

\ Periodic save (call from main loop)
: SAVE-STATE ( -- )
    SAVE-METADATA
    SAVE-UTXO-SET
    SAVE-MEMPOOL ;

\ Add block and update metadata
: STORE-BLOCK ( block-addr block-len -- )
    \ Write block to disk
    2DUP WRITE-BLOCK
    
    \ Update height
    1 CHAIN-HEIGHT +!
    
    \ Update work (simplified - add difficulty)
    1 0 CHAIN-WORK 2@ D+ CHAIN-WORK 2!
    
    \ Save metadata
    SAVE-METADATA
    
    ." [STORAGE] Block stored at height " CHAIN-HEIGHT @ . CR ;

\ Retrieve block by height (simplified)
: FETCH-BLOCK-BY-HEIGHT ( height buffer -- success? )
    \ TODO: Implement height->hash mapping
    \ For now, only works if we build an index
    2DROP FALSE ;

\ ---------------------------------------------------------
\ 8. Wallet Persistence
\ ---------------------------------------------------------

VARIABLE WALLET-FILE-ID

: SAVE-WALLET ( wallet-addr filename-str filename-len -- )
    R/W CREATE-FILE ABORT" Failed to create wallet file"
    WALLET-FILE-ID !
    
    \ Write wallet (121 bytes)
    WALLET-SIZE WALLET-FILE-ID @ WRITE-FILE DROP
    
    WALLET-FILE-ID @ FLUSH-FILE DROP
    WALLET-FILE-ID @ CLOSE-FILE DROP
    ." [STORAGE] Wallet saved." CR ;

: LOAD-WALLET ( wallet-addr filename-str filename-len -- success? )
    R/O OPEN-FILE IF
        DROP FALSE EXIT
    THEN
    WALLET-FILE-ID !
    
    \ Read wallet
    WALLET-SIZE WALLET-FILE-ID @ READ-FILE IF
        DROP WALLET-FILE-ID @ CLOSE-FILE DROP
        FALSE EXIT
    THEN
    DROP
    
    WALLET-FILE-ID @ CLOSE-FILE DROP
    ." [STORAGE] Wallet loaded." CR
    TRUE ;

CR ." [STORAGE] Full persistence layer loaded." CR
