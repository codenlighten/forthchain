\ =========================================================
\ FORTHCOIN CRYPTO TEST SUITE
\ =========================================================

CR ." [TEST] Running Crypto Suite..." CR

\ ---------------------------------------------------------
\ Test: 32-bit Rotation (ROTR32)
\ ---------------------------------------------------------

: TEST-ROTR-CRYPTO ( -- )
    HEX
    \ Basic rotation
    87654321 4 ROTR32 18765432 = s" ROTR32 Basic" ASSERT-TRUE
    
    \ Rotate by 0
    ABCDEF00 0 ROTR32 ABCDEF00 = s" ROTR32 Zero" ASSERT-TRUE
    
    \ High bit rotation
    80000000 1 ROTR32 40000000 = s" ROTR32 High Bit" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ Test: MASK32
\ ---------------------------------------------------------

: TEST-MASK32-CRYPTO ( -- )
    HEX
    123456789ABCDEF MASK32 89ABCDEF = s" MASK32 truncates" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ Test: CH function
\ ---------------------------------------------------------

: TEST-CH-CRYPTO ( -- )
    HEX
    F0F0F0F0 AAAAAAAA 55555555 CH 50505050 = s" CH function" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ Test: MAJ function
\ ---------------------------------------------------------

: TEST-MAJ-CRYPTO ( -- )
    HEX
    FFFFFFFF AAAAAAAA 55555555 MAJ FFFFFFFF = s" MAJ function" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ Test: Byte Swap (BSWAP32)
\ ---------------------------------------------------------

: TEST-BSWAP-CRYPTO ( -- )
    HEX
    AABBCCDD BSWAP32 DDCCBBAA = s" BSWAP32 function" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ EXECUTE TESTS
\ ---------------------------------------------------------
TEST-ROTR-CRYPTO
TEST-MASK32-CRYPTO
TEST-CH-CRYPTO
TEST-MAJ-CRYPTO
TEST-BSWAP-CRYPTO

CR ." [TEST] Crypto tests complete." CR
