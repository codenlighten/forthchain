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
\ Test: SHA-256 Hash of "abc" (NIST test vector)
\ ---------------------------------------------------------

\ Input block for "abc" (padded to 512 bits)
CREATE TEST-BLOCK-ABC 64 ALLOT

: INIT-TEST-BLOCK-ABC ( -- )
    \ Clear block
    TEST-BLOCK-ABC 64 0 FILL
    
    \ Write "abc" = 0x61626380 (with padding bit)
    HEX 61626380 TEST-BLOCK-ABC !
    
    \ Write length (24 bits = 0x18) at end (bit 448-511)
    18000000 TEST-BLOCK-ABC 60 + !
    DECIMAL ;

\ Expected hash for "abc":
\ ba7816bf 8f01cfea 414140de 5dae2223
\ b00361a3 96177a9c b410ff61 f20015ad

: TEST-SHA256-ABC ( -- )
    INIT-TEST-BLOCK-ABC
    TEST-BLOCK-ABC SHA256-BLOCK
    
    HEX
    \ Check all 8 words of the hash
    0 H@ BA7816BF = s" SHA256('abc') H0" ASSERT-TRUE
    1 H@ 8F01CFEA = s" SHA256('abc') H1" ASSERT-TRUE
    2 H@ 414140DE = s" SHA256('abc') H2" ASSERT-TRUE
    3 H@ 5DAE2223 = s" SHA256('abc') H3" ASSERT-TRUE
    4 H@ B00361A3 = s" SHA256('abc') H4" ASSERT-TRUE
    5 H@ 96177A9C = s" SHA256('abc') H5" ASSERT-TRUE
    6 H@ B410FF61 = s" SHA256('abc') H6" ASSERT-TRUE
    7 H@ F20015AD = s" SHA256('abc') H7" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ EXECUTE TESTS
\ ---------------------------------------------------------
TEST-ROTR-CRYPTO
TEST-MASK32-CRYPTO
TEST-CH-CRYPTO
TEST-MAJ-CRYPTO
TEST-BSWAP-CRYPTO
TEST-SHA256-ABC

CR ." [TEST] Crypto tests complete." CR
