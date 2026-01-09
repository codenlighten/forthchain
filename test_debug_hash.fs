\ Debug SHA-256 compression step by step
REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." ========== SHA-256 Debug Test ==========" CR

CREATE TESTBLOCK 64 ALLOT

: INIT-TEST
    TESTBLOCK 64 0 FILL
    INIT-HASH ;

: TEST-LOAD-BLOCK
    CR ." Step 1: Loading block into W-array..." CR
    16 0 DO
        TESTBLOCK I CELLS + @ BSWAP32 I W256!
    LOOP
    CR ." W-array loaded" CR ;

: TEST-EXPAND
    CR ." Step 2: Expanding message schedule..." CR
    EXPAND-MSG
    CR ." Message expanded" CR ;

: TEST-INIT-VARS
    CR ." Step 3: Initializing working variables..." CR
    8 0 DO
        I H@ I SET-VAR
    LOOP
    CR ." Variables initialized" CR
    HEX
    CR ." a = " 0 GET-VAR . CR
    ." e = " 4 GET-VAR . CR
    DECIMAL ;

: TEST-ONE-ROUND
    CR ." Step 4: Testing one compression round..." CR
    \ T1 calculation
    7 GET-VAR DUP . CR
    4 GET-VAR BSIG1 + DUP . CR
    4 GET-VAR 5 GET-VAR 6 GET-VAR CH + DUP . CR
    0 GET-K + DUP . CR
    0 W256@ + DUP . CR
    MASK32
    CR ." T1 = " HEX OVER . DECIMAL CR
    
    \ T2 calculation
    0 GET-VAR BSIG0
    0 GET-VAR 1 GET-VAR 2 GET-VAR MAJ +
    MASK32
    CR ." T2 = " HEX DUP . DECIMAL CR
    
    \ Now have T1 T2 on stack
    CR ." About to rotate variables..." CR
    2DROP  \ Don't actually do it yet
    CR ." Round test complete" CR ;

INIT-TEST
TEST-LOAD-BLOCK
TEST-EXPAND
TEST-INIT-VARS
TEST-ONE-ROUND

CR ." ========== Debug Complete ==========" CR
BYE
