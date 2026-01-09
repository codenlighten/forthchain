\ Detailed SHA-256 component verification
REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." ===== SHA-256 Component Verification =====" CR CR

\ Test ROTR32 with known values
CR ." 1. ROTR32 Tests" CR
: TEST-ROTR32-VALS
    \ ROTR32(0x12345678, 2) = 0x048D159E
    $12345678 2 ROTR32 $048D159E = IF CR ."   ROTR32(0x12345678, 2) ✓" CR
    ELSE CR ."   ROTR32 test FAILED" CR THEN ;

TEST-ROTR32-VALS

\ Test CH with known values
CR ." 2. CH function" CR
: TEST-CH-VALS
    \ CH(0x6a09e667, 0xbb67ae85, 0x3c6ef372)
    $6a09e667 $bb67ae85 $3c6ef372 CH
    \ Should compute correctly
    CR ."   CH computed" CR ;

TEST-CH-VALS

\ Test BSIG0 rotation
CR ." 3. BSIG0 Tests" CR
: TEST-BSIG0-VALS
    $6a09e667 BSIG0
    CR ."   BSIG0(H0) computed = " HEX . CR
    DECIMAL ;

TEST-BSIG0-VALS

\ Test message expansion with known pattern
CR ." 4. Message Schedule Expansion" CR
: TEST-EXPAND-VALS
    \ Initialize W[0..15] with simple pattern
    16 0 DO I I W256! LOOP
    
    CR ."   W[0..15] initialized" CR
    
    \ Expand
    EXPAND-MSG
    
    CR ."   W[16] = " HEX 16 W256@ . DECIMAL CR
    CR ."   W[17] = " HEX 17 W256@ . DECIMAL CR ;

TEST-EXPAND-VALS

\ Test initial hash values
CR ." 5. Initial Hash State" CR
: TEST-INIT-HASH-VALS
    INIT-HASH
    CR ."   H[0] = " HEX 0 H@ . DECIMAL CR
    CR ."   H[1] = " HEX 1 H@ . DECIMAL CR ;

TEST-INIT-HASH-VALS

CR ." ===== Verification Complete =====" CR
BYE
