\ =========================================================
\ FORTHCOIN MERKLE TREE CALCULATOR
\ =========================================================
\ Requires: src/crypto/sha256.fs (for hashing operations)

\ Configuration
2048 CONSTANT MAX-TX-COUNT
32   CONSTANT HASH-SIZE

\ ---------------------------------------------------------
\ Memory Layout
\ ---------------------------------------------------------

\ The Main Buffer: Stores all Transaction Hashes (TXIDs)
\ Size: 64KB (2048 * 32 bytes)
CREATE MERKLE-BUF MAX-TX-COUNT HASH-SIZE * ALLOT

\ Temp Buffer: Used to concatenate two 32-byte hashes (64 bytes)
CREATE MERKLE-PAIR 64 ALLOT

\ Result buffer for hash output
CREATE MERKLE-HASH-OUT 32 ALLOT

\ Internal loop variables (Global to avoid recursion stack issues)
VARIABLE M-READ-IDX
VARIABLE M-WRITE-IDX
VARIABLE M-ROW-COUNT

\ ---------------------------------------------------------
\ Helper Words
\ ---------------------------------------------------------

\ Get the memory address of the hash at a specific index
: GET-HASH-ADDR ( index -- addr )
    HASH-SIZE * MERKLE-BUF + ;

\ Clear the buffer (Safety)
: CLEAR-MERKLE-BUF ( -- )
    MERKLE-BUF MAX-TX-COUNT HASH-SIZE * ERASE ;

\ Load a hash into the buffer at index I
: SET-MERKLE-LEAF ( addr index -- )
    GET-HASH-ADDR 32 CMOVE ;

\ Simple double-SHA256 for merkle (placeholder until full SHA implementation)
: MERKLE-HASH ( addr len output -- )
    \ For now, just copy first 32 bytes as placeholder
    \ TODO: Replace with actual RUN-HASH256 when implemented
    ROT DROP SWAP 32 MIN CMOVE ;

\ ---------------------------------------------------------
\ Core Logic: Process One Level
\ ---------------------------------------------------------
\ Reduces a row of 'N' hashes to 'N/2' hashes.
\ Returns the new count.

: MERKLE-PASS ( count -- new_count )
    M-ROW-COUNT !
    0 M-READ-IDX !
    0 M-WRITE-IDX !

    BEGIN
        M-READ-IDX @ M-ROW-COUNT @ <
    WHILE
        \ --- PREPARE LEFT SIDE ---
        M-READ-IDX @ GET-HASH-ADDR ( src )
        MERKLE-PAIR                ( dest )
        32 CMOVE

        \ --- PREPARE RIGHT SIDE ---
        \ Check if we have a right partner
        M-READ-IDX @ 1+ ( next_idx )
        DUP M-ROW-COUNT @ < IF
            \ Case A: Normal Pair
            GET-HASH-ADDR
        ELSE
            \ Case B: Odd number at end -> Duplicate Left
            DROP
            M-READ-IDX @ GET-HASH-ADDR
        THEN
        \ Copy to second half of temp buffer
        MERKLE-PAIR 32 + 32 CMOVE

        \ --- HASH (Left + Right) ---
        MERKLE-PAIR 64                ( input: addr len )
        M-WRITE-IDX @ GET-HASH-ADDR   ( output: write back to buffer )
        MERKLE-HASH                   ( Execute Double SHA )

        \ --- ADVANCE ---
        2 M-READ-IDX +!   \ Consumed 2 items
        1 M-WRITE-IDX +!  \ Produced 1 item
    REPEAT

    \ Return the new count (the write index)
    M-WRITE-IDX @ ;

\ ---------------------------------------------------------
\ API: Compute Root
\ ---------------------------------------------------------

: CALC-MERKLE-ROOT ( count -- root_addr )
    \ Takes the number of transactions loaded into MERKLE-BUF.
    \ Reduces until only 1 hash remains.
    
    BEGIN
        DUP 1 >
    WHILE
        \ Reduce level
        MERKLE-PASS
    REPEAT
    
    DROP \ Drop the count (which is now 1)
    
    \ The Root is always at Index 0
    0 GET-HASH-ADDR ;

CR ." [CONSENSUS] Merkle tree loaded." CR
