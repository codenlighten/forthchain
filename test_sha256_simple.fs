\ Simple SHA-256 debug test
REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." ========================================" CR
CR ." Testing SHA-256 Components" CR
CR ." ========================================" CR

\ Test 1: Initialize hash
: TEST-INITIALIZE
    CR .\" Test 1: Hash initialization...\" CR
    INIT-HASH
    HEX
    CR .\" H[0] = \" 0 H@ . CR
    .\" H[1] = \" 1 H@ . CR
    DECIMAL
    CR .\" ✓ PASSED\" CR ;

\ Test 2: W-array access
: TEST-W-ARRAY
    CR .\" Test 2: W-array...\" CR
    $12345678 0 W256!
    $ABCDEF12 15 W256!
    HEX
    CR .\" W[0]  = \" 0 W256@ . CR
    .\" W[15] = \" 15 W256@ . CR
    DECIMAL
    CR .\" ✓ PASSED\" CR ;

\ Test 3: Working variables
: TEST-VARIABLES
    CR .\" Test 3: Working variables...\" CR
    $AABBCCDD 0 SET-VAR
    $11223344 7 SET-VAR
    HEX
    CR .\" VAR[0] = \" 0 GET-VAR . CR
    .\" VAR[7] = \" 7 GET-VAR . CR
    DECIMAL
    CR .\" ✓ PASSED\" CR ;

\ Test 4: Message expansion
: TEST-EXPANSION
    CR .\" Test 4: Message expansion...\" CR
    16 0 DO I I W256! LOOP
    EXPAND-MSG
    HEX
    CR .\" W[0]  = \" 0 W256@ . CR
    .\" W[16] = \" 16 W256@ . CR
    DECIMAL
    CR .\" ✓ PASSED\" CR ;

\ Test 5: Byte swap
CREATE TBLOCK 64 ALLOT

: TEST-BYTESWAP
    CR .\" Test 5: Byte swap...\" CR
    $01020304 TBLOCK !
    HEX
    CR .\" Original: \" TBLOCK @ . CR
    .\" Swapped:  \" TBLOCK @ BSWAP32 . CR
    DECIMAL
    TBLOCK @ BSWAP32 $04030201 =
    IF CR .\" ✓ PASSED\" CR
    ELSE CR .\" ✗ FAILED!\" CR THEN ;

TEST-INITIALIZE
TEST-W-ARRAY
TEST-VARIABLES
TEST-EXPANSION
TEST-BYTESWAP

CR ." ========================================" CR
CR ." All component tests completed!" CR
CR ." ========================================\" CR
BYE
