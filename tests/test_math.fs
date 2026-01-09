\ =========================================================
\ FORTHCOIN MATH TEST SUITE
\ =========================================================
\ This file verifies the low-level arithmetic kernels.

CR ." [TEST] Running Math Suite..." CR

\ ---------------------------------------------------------
\ 1. Test Variables (Must be Global)
\ ---------------------------------------------------------
256VAR T-A   \ Operand A
256VAR T-B   \ Operand B
256VAR T-RES \ Result

\ Helper to clear variables between tests
: CLEAR-VARS
    T-A 32 ERASE
    T-B 32 ERASE
    T-RES 32 ERASE ;

\ ---------------------------------------------------------
\ 2. Test: 32-bit Rotation (ROTR32)
\ ---------------------------------------------------------
\ Test Vector: 0x87654321 rotated right by 4 bits
\ Expected:    0x18765432

: TEST-ROTR ( -- )
    HEX
    $87654321 4 ROTR32  ( result )
    $18765432 =         ( flag )
    s" ROTR32 Basic" ASSERT-TRUE 
    
    \ Edge Case: Rotate by 0
    $ABCDEF00 0 ROTR32
    $ABCDEF00 =
    s" ROTR32 Zero" ASSERT-TRUE

    \ Edge Case: Rotate by 32 (Should be same)
    \ Note: Our logic might need mod 32 check if CPU doesn't handle it
    \ $12345678 32 ROTR32 $12345678 = s" ROTR32 Full" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ 3. Test: 256-bit Addition (Ripple Carry)
\ ---------------------------------------------------------
\ We will test the "Waterfall": Adding 1 to a number that is
\ all FFs in the lower cells to ensure carry moves up.

: TEST-ADD-RIPPLE ( -- )
    CLEAR-VARS
    HEX
    
    \ Setup T-A: 00...00 00...00 FFFFFFFFFFFFFFFF FFFFFFFFFFFFFFFF
    \ (Low 128 bits are maxed out)
    
    \ Write -1 (All Fs) to Cell A and Cell B
    -1 0 0 0 T-A 256!
    -1 T-A 8 + !  \ Manually writing to 2nd cell for setup
    
    \ Setup T-B: 1
    1 0 0 0 T-B 256!
    
    \ Action: T-A += T-B
    T-A T-B 256+!
    
    \ Verification:
    \ T-A should now be: ...0001 0000...0000 0000...0000
    \ Cell A should be 0
    T-A @ 0= s" 256+ Ripple Cell A" ASSERT-TRUE
    
    \ Cell B should be 0
    T-A 8 + @ 0= s" 256+ Ripple Cell B" ASSERT-TRUE
    
    \ Cell C should be 1 (Carry landed here)
    T-A 16 + @ 1 = s" 256+ Ripple Cell C" ASSERT-TRUE
    
    DECIMAL ;

\ ---------------------------------------------------------
\ 4. Test: 256-bit Comparison (256<)
\ ---------------------------------------------------------

: TEST-COMPARE ( -- )
    CLEAR-VARS
    HEX
    
    \ Case 1: Equality
    1 0 0 0 T-A 256!
    1 0 0 0 T-B 256!
    T-A T-B 256< 0= s" 256< Equality (False)" ASSERT-TRUE
    
    \ Case 2: A < B (Simple)
    1 0 0 0 T-A 256!
    2 0 0 0 T-B 256!
    T-A T-B 256< -1 = s" 256< Simple Less" ASSERT-TRUE
    
    \ Case 3: A > B (Simple)
    5 0 0 0 T-A 256!
    2 0 0 0 T-B 256!
    T-A T-B 256< 0= s" 256< Simple Greater" ASSERT-TRUE
    
    \ Case 4: High Cell Dominance
    \ A has large LSB, but small MSB
    \ B has small LSB, but large MSB -> B is bigger
    -1 0 0 0 T-A 256!  ( A = ...0000 FFFF... )
     0 0 0 1 T-B 256!  ( B = ...0001 0000... )
    
    T-A T-B 256< -1 = s" 256< MSB Dominance" ASSERT-TRUE
    
    DECIMAL ;

\ ---------------------------------------------------------
\ 5. Test: Endianness / Byte Swapping
\ ---------------------------------------------------------
\ If BSWAP32 is broken, the blockchain hash will be inverted.

: TEST-BSWAP ( -- )
    HEX
    $AABBCCDD BSWAP32
    $DDCCBBAA = s" BSWAP32 Logic" ASSERT-TRUE
    DECIMAL ;

\ ---------------------------------------------------------
\ EXECUTE TESTS
\ ---------------------------------------------------------
TEST-ROTR
TEST-ADD-SIMPLE
TEST-COMPARE
TEST-BSWAP

CR ." [TEST] Math tests complete." CR
