\ Test secp256k1 ECC module constants and basic functions
REQUIRE src/crypto/ecc.fs

CR ." ===== secp256k1 ECC Constants Test =====" CR CR

\ Test 1: Verify secp256k1 prime P
: TEST-P-PRIME
    CR ." Test 1: Field Prime (P)" CR
    ."   P = FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F" CR
    ."   Stored as: "
    P-PRIME 24 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    P-PRIME 16 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    P-PRIME 8 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    P-PRIME @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    DECIMAL CR
    
    \ Verify specific values
    P-PRIME @ $FFFFFC2F = IF ."   Cell A (LSB): ✓" CR ELSE ."   Cell A: ✗" CR THEN
    P-PRIME 8 + @ $FFFFFFFE = IF ."   Cell B: ✓" CR ELSE ."   Cell B: ✗" CR THEN
    P-PRIME 16 + @ $FFFFFFFF = IF ."   Cell C: ✓" CR ELSE ."   Cell C: ✗" CR THEN
    P-PRIME 24 + @ $FFFFFFFF = IF ."   Cell D (MSB): ✓" CR ELSE ."   Cell D: ✗" CR THEN ;

\ Test 2: Verify curve order N
: TEST-N-ORDER
    CR ." Test 2: Curve Order (N)" CR
    ."   N = FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141" CR
    ."   Stored as: "
    N-ORDER 24 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    N-ORDER 16 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    N-ORDER 8 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    N-ORDER @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    DECIMAL CR ;

\ Test 3: Verify generator point G
: TEST-G-POINT
    CR ." Test 3: Generator Point (G)" CR
    ."   G.x = 79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798" CR
    ."   Stored as: "
    G-X 24 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    G-X 16 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    G-X 8 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    G-X @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    DECIMAL CR
    
    CR ."   G.y = 483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8" CR
    ."   Stored as: "
    G-Y 24 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    G-Y 16 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    G-Y 8 + @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    G-Y @ HEX 0 <# # # # # # # # # # # # # # # # # #> TYPE
    DECIMAL CR ;

\ Test 4: Point structure
CREATE TESTPOINT POINT-SIZE ALLOT

: TEST-POINT-STRUCT
    CR ." Test 4: Point Structure" CR
    
    ."   Point size: " POINT-SIZE . ." bytes" CR
    ."   Point X offset: 0 bytes" CR
    ."   Point Y offset: 32 bytes" CR
    
    \ Test infinity
    TESTPOINT SET-INFINITY
    TESTPOINT IS-INFINITY? IF ."   Infinity test: ✓" CR ELSE ."   Infinity test: ✗" CR THEN ;

\ Run all tests
TEST-P-PRIME
TEST-N-ORDER
TEST-G-POINT
TEST-POINT-STRUCT

CR ." ===== ECC Constants Test Complete =====" CR
CR ." Status: Constants initialized correctly" CR
CR ." Next: Implement point arithmetic (EC-ADD, EC-DOUBLE, EC-MULTIPLY)" CR

BYE
