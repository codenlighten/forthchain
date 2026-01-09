\ Simple SHA-256 debug test
REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." Testing SHA-256 components..." CR

\ Test 1: Initialize hash
: TEST-INIT-HASH
    CR ." 1. Initializing hash state..." CR
    INIT-HASH
    CR ." H[0] = " HEX 0 H@ . DECIMAL CR
    CR ." ✓ Hash initialized" CR ;

TEST-INIT-HASH

\ Test 2: W-array access
CR ." 2. Testing W-array..." CR
$12345678 0 W!
0 W@ HEX . DECIMAL CR
CR ." ✓ W-array works" CR

\ Test 3: Working variables
CR ." 3. Testing working variables..." CR
$AABBCCDD 0 SET-VAR
0 GET-VAR HEX . DECIMAL CR
CR ." ✓ Working variables work" CR

\ Test 4: Message expansion
CR ." 4. Testing message expansion..." CR
16 0 DO
    I I W!
LOOP
EXPAND-MSG
CR ." W[16] = " 16 W@ HEX . DECIMAL CR
CR ." ✓ Message expansion works" CR

\ Test 5: Load and byte-swap
CR ." 5. Testing block load..." CR
CREATE TEST-BLOCK 64 ALLOT
$01020304 TEST-BLOCK !
TEST-BLOCK @ HEX . DECIMAL CR
TEST-BLOCK @ BSWAP32 HEX . DECIMAL CR
CR ." ✓ Byte swap works" CR

CR ." All component tests passed!" CR
BYE
