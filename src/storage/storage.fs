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

CR ." [STORAGE] Persistence layer loaded." CR
