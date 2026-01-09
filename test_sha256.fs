\ =========================================================
\ Standalone SHA-256 Test
\ =========================================================

REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." ======================================" CR
CR ." Testing SHA-256 Implementation" CR
CR ." ======================================" CR

\ ---------------------------------------------------------
\ Test: SHA-256 Hash of "abc" (NIST test vector)
\ ---------------------------------------------------------

\ Input block for "abc" (padded to 512 bits)
CREATE TEST-BLOCK-ABC 64 ALLOT

: INIT-TEST-BLOCK-ABC ( -- )
    \ Clear block
    TEST-BLOCK-ABC 64 0 FILL

    \ Write bytes for "abc" then padding bit
    $61 TEST-BLOCK-ABC       C!  \ 'a'
    $62 TEST-BLOCK-ABC 1 +  C!  \ 'b'
    $63 TEST-BLOCK-ABC 2 +  C!  \ 'c'
    $80 TEST-BLOCK-ABC 3 +  C!  \ 0x80 padding bit

    \ Write length (64-bit big-endian): 24 bits = 0x18
    $18 TEST-BLOCK-ABC 63 + C! ;

\ Expected hash for "abc":
\ ba7816bf 8f01cfea 414140de 5dae2223
\ b00361a3 96177a9c b410ff61 f20015ad

: TEST-SHA256-ABC ( -- )
    CR ." Computing SHA-256('abc')..." CR
    INIT-TEST-BLOCK-ABC
    TEST-BLOCK-ABC SHA256-BLOCK
    
    HEX
    CR ." Result: " CR
    8 0 DO
        ."   H" I . ." = " I H@ . CR
    LOOP
    
    CR ." Expected: " CR
    ."   H0 = BA7816BF" CR
    ."   H1 = 8F01CFEA" CR
    ."   H2 = 414140DE" CR
    ."   H3 = 5DAE2223" CR
    ."   H4 = B00361A3" CR
    ."   H5 = 96177A9C" CR
    ."   H6 = B410FF61" CR
    ."   H7 = F20015AD" CR
    DECIMAL
    
    CR ." Verification: "
    0 H@ $BA7816BF = 
    1 H@ $8F01CFEA = AND
    2 H@ $414140DE = AND
    3 H@ $5DAE2223 = AND
    4 H@ $B00361A3 = AND
    5 H@ $96177A9C = AND
    6 H@ $B410FF61 = AND
    7 H@ $F20015AD = AND
    
    IF
        CR ." ✓ TEST PASSED - SHA-256 is correct!" CR
    ELSE
        CR ." ✗ TEST FAILED - SHA-256 mismatch!" CR
    THEN ;

TEST-SHA256-ABC

CR ." ======================================" CR
BYE
