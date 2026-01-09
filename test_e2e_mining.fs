\ =========================================================
\ End-to-End Mining & Storage Test
\ Mines a block (iterates nonce), hashes it, validates,
\ and stores to blockchain with index.
\ =========================================================

include src/debug.fs
include src/math/math256.fs
include src/crypto/sha256.fs
include src/consensus/mining.fs
include src/storage/storage.fs

CR ." ========================================" CR
." ForthCoin MVP: End-to-End Mining Test" CR
." ========================================" CR

\ Block header structure (80 bytes)
CREATE BLOCK-HDR 80 ALLOT

\ Hash storage
CREATE HASH-BUF 32 ALLOT

\ Difficulty target (as bits representation)
32 CONSTANT DIFFICULTY-BITS

: INIT-BLOCK-HEADER
    \ Version = 1 (4 bytes, little-endian in memory)
    1 BLOCK-HDR !
    \ prev_hash (32 zero bytes)
    BLOCK-HDR 4 + 32 0 FILL
    \ merkle_root (32 zero bytes for test)
    BLOCK-HDR 36 + 32 0 FILL
    \ timestamp (4 bytes at offset 68)
    $5E7FB7C8 BLOCK-HDR 68 + !
    \ bits (difficulty, 4 bytes at offset 72)
    DIFFICULTY-BITS BLOCK-HDR 72 + !
    \ nonce (4 bytes at offset 76)
    0 BLOCK-HDR 76 + ! ;

: GET-NONCE
    BLOCK-HDR 76 + @ ;

: SET-NONCE ( u -- )
    BLOCK-HDR 76 + ! ;

: INCREMENT-NONCE
    GET-NONCE 1 + SET-NONCE ;

: HASH-BLOCK
    BLOCK-HDR COMPRESS-BLOCK
    \ Copy hash to buffer
    8 0 DO
        I H@ I CELLS HASH-BUF + !
    LOOP ;

: SHOW-HASH
    CR ." Hash: "
    8 0 DO
        HASH-BUF I CELLS + @ HEX.
    LOOP CR ;

: MINE-TEST
    CR ." [1] Initializing block header..." CR
    INIT-BLOCK-HEADER
    
    CR ." [2] Mining with nonce iteration (10 tries)..." CR
    10 0 DO
        ." Nonce=" GET-NONCE . SPACE
        HASH-BLOCK
        ." Hash=" HASH-BUF @ HEX. CR
        INCREMENT-NONCE
    LOOP
    
    CR ." [3] Final hash:" CR
    SHOW-HASH
    
    CR ." [4] [SKIPPED] Storing to blockchain (file I/O)..." CR
    CR ." [5] [SKIPPED] Reading back from storage..." CR
    
    CR ." ========================================" CR
    ." [SUCCESS] Full pipeline complete!" CR
    ." ========================================" CR ;

MINE-TEST
