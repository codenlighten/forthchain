\ =========================================================
\ FORTHCOIN MINING ENGINE (PROOF OF WORK)
\ =========================================================
\ Requires: math256.fs, sha256.fs

\ ---------------------------------------------------------
\ 1. Block Header Structure (80 Bytes)
\ ---------------------------------------------------------

CREATE BLOCK-HEADER 80 ALLOT

\ Offsets for specific fields
: HDR-VERSION   ( -- addr ) BLOCK-HEADER ;        \ 4 bytes
: HDR-PREVHASH  ( -- addr ) BLOCK-HEADER 4 + ;    \ 32 bytes
: HDR-MERKLE    ( -- addr ) BLOCK-HEADER 36 + ;   \ 32 bytes
: HDR-TIME      ( -- addr ) BLOCK-HEADER 68 + ;   \ 4 bytes
: HDR-BITS      ( -- addr ) BLOCK-HEADER 72 + ;   \ 4 bytes
: HDR-NONCE     ( -- addr ) BLOCK-HEADER 76 + ;   \ 4 bytes

\ ---------------------------------------------------------
\ 2. Difficulty: Compact -> 256-bit Target
\ ---------------------------------------------------------

256VAR TARGET-NUM
256VAR CURRENT-HASH

: SET-TARGET-FROM-BITS ( bits -- )
    \ 1. Extract Exponent (Top 8 bits)
    DUP 24 RSHIFT ( bits exp )
    
    \ 2. Extract Mantissa (Bottom 24 bits)
    SWAP $00FFFFFF AND ( exp mantissa )
    
    \ 3. Clear Target Variable
    0 0 0 0 TARGET-NUM 256!
    
    \ 4. Store Mantissa in Lowest Cell (Cell A)
    TARGET-NUM !  ( exp )
    
    \ 5. Shift Loop: Target << (8 * (exp - 3))
    3 - 8 * ( bits_to_shift )
    
    \ Shift left bit by bit
    DUP 0 > IF
        0 DO
            TARGET-NUM 256LSHIFT1
        LOOP
    ELSE
        DROP
    THEN ;

\ ---------------------------------------------------------
\ 3. The Mining Loop
\ ---------------------------------------------------------

: INC-NONCE ( -- )
    HDR-NONCE @ 1+ HDR-NONCE ! ;

: MINE-BLOCK ( bits -- success? )
    \ Setup Target
    SET-TARGET-FROM-BITS
    
    CR ." [MINER] Starting Loop..." CR
    ." Target: " TARGET-NUM 256@ 256. 
    
    \ Reset Nonce to 0
    0 HDR-NONCE !
    
    \ The Loop (Limit to 100k hashes for testing)
    100000 0 DO
        \ 1. Hash the Header (Placeholder - need full SHA256)
        \ BLOCK-HEADER 80 CURRENT-HASH RUN-HASH256
        
        \ For now, simulate with incrementing hash
        HDR-NONCE @ 0 0 0 CURRENT-HASH 256!
        
        \ 2. Compare Hash < Target
        CURRENT-HASH TARGET-NUM 256< IF
            CR ." [MINER] !!! BLOCK FOUND !!!" 
            CR ." Nonce: " HDR-NONCE @ .
            CR ." Hash: " CURRENT-HASH 256@ 256.
            TRUE UNLOOP EXIT
        THEN
        
        \ 3. Increment Nonce
        INC-NONCE
        
        \ Visualization (Dot every 10k hashes)
        I 10000 MOD 0= IF ." ." THEN
    LOOP
    
    CR ." [MINER] No block found in 100k attempts." FALSE ;

CR ." [CONSENSUS] Mining engine loaded." CR
