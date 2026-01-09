\ =========================================================
\ MERKLE TREE CALCULATOR
\ =========================================================

REQUIRE ../crypto/sha256.fs

CR ." [CONSENSUS] Loading Merkle tree module..." CR

\ Configuration
2048 CONSTANT MAX-TX-COUNT
32   CONSTANT HASH-SIZE

\ Memory for transaction hashes
CREATE TX-BUFFER MAX-TX-COUNT HASH-SIZE * ALLOT
CREATE PAIR-BUFFER 64 ALLOT
CREATE HASH-OUT 32 ALLOT

VARIABLE TX-COUNT
VARIABLE CALC-INDEX

\ =========================================================
\ Copy a 32-byte hash
\ =========================================================

: COPY-HASH ( src dest -- )
    32 0 DO
        OVER I + C@ OVER I + C!
    LOOP
    2DROP ;

\ =========================================================
\ Hash two 32-byte values together (Bitcoin merkle style)
\ =========================================================

: HASH-PAIR ( hash1 hash2 result -- )
    >R
    \ Concatenate hashes into PAIR-BUFFER
    PAIR-BUFFER COPY-HASH          \ Copy hash1
    PAIR-BUFFER 32 + COPY-HASH     \ Copy hash2
    
    \ Compute SHA256(pair)
    PAIR-BUFFER SHA256-BLOCK
    
    \ Copy result to output
    R@ 8 0 DO
        I H@ OVER I 4 * + !
    LOOP DROP ;

\ =========================================================
\ Calculate Merkle Root (Bitcoin compliant)
\ =========================================================

: CALC-MERKLE ( tx-count -- root-addr )
    TX-COUNT !
    
    \ Single tx: return it as root
    TX-COUNT @ 1 = IF TX-BUFFER EXIT THEN
    
    \ Multiple transactions: build tree iteratively
    CALC-INDEX !
    
    \ Process pairs until one remains
    BEGIN
        CALC-INDEX @ 1 >
    WHILE
        \ Number of hashes is even or odd?
        CALC-INDEX @ 2 /MOD SWAP  \ remainder -> quot
        
        DUP 0> IF
            \ Odd: duplicate last hash
            CALC-INDEX @ 32 * TX-BUFFER +
            DUP COPY-HASH
        THEN
        
        \ Process all pairs
        CALC-INDEX @ 2 / 0 DO
            I 2 * 32 * TX-BUFFER + >R   \ hash1
            I 2 * 1 + 32 * TX-BUFFER +  \ hash2
            HASH-OUT 
            HASH-PAIR
            
            R> DROP
        LOOP
        
        CALC-INDEX @ 2 / CALC-INDEX !
    REPEAT
    
    TX-BUFFER ;

CR ." [CONSENSUS] Merkle tree loaded." CR
