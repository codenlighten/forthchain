\ Simple SHA-256 component test
REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." ========== SHA-256 Component Tests ==========" CR

\ Test 1: Initialize hash
: TEST1
    CR ." Test 1: Hash initialization" CR
    INIT-HASH
    0 H@ $6A09E667 = IF
        CR ." PASS" CR
    ELSE
        CR ." FAIL - Hash init broken" CR
    THEN ;

\ Test 2: W-array
: TEST2
    CR ." Test 2: W-array access" CR
    $12345678 0 W256!
    0 W256@ $12345678 = IF
        CR ." PASS" CR
    ELSE
        CR ." FAIL - W-array broken" CR
    THEN ;

\ Test 3: Working variables
: TEST3
    CR ." Test 3: Working variables" CR
    $AABBCCDD 0 SET-VAR
    0 GET-VAR $AABBCCDD = IF
        CR ." PASS" CR
    ELSE
        CR ." FAIL - Working vars broken" CR
    THEN ;

\ Test 4: Byte swap
: TEST4
    CR ." Test 4: Byte swap" CR
    $01020304 BSWAP32 $04030201 = IF
        CR ." PASS" CR
    ELSE
        CR ." FAIL - Byte swap broken" CR
    THEN ;

TEST1
TEST2
TEST3
TEST4

CR ." ========== All Tests Complete ==========" CR
BYE
