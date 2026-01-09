\ Test SHA-256 full compression
REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." ========== SHA-256 Full Hash Test ==========" CR

\ Test block - all zeros (simplest test)
CREATE ZEROBLOCK 64 ALLOT

: INIT-ZERO
    ZEROBLOCK 64 0 FILL ;

: TEST-ZERO-HASH
    CR ." Testing hash of zero block..." CR
    INIT-ZERO
    ZEROBLOCK SHA256-BLOCK
    
    \ Print result
    CR ." Hash result:" CR
    HEX
    8 0 DO
        CR ." H[" I . ." ] = " I H@ . 
    LOOP
    DECIMAL
    CR ." Test completed!" CR ;

TEST-ZERO-HASH

CR ." ========== Test Complete ==========" CR
BYE
