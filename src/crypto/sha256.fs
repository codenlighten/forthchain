\ =========================================================
\ SHA-256 Implementation for ForthCoin
\ =========================================================

\ Helper: Mask to ensure we stay within 32 bits
: MASK32 ( n -- n' ) 
    $FFFFFFFF AND ;

\ =========================================================
\ 32-bit Bitwise Rotation (Required for SHA-256)
\ =========================================================

\ Circular Rotate Right for 32-bit values
\ Formula: (x >> n) | (x << (32 - n))
: ROTR32 ( x n -- x' )
    2DUP RSHIFT         ( x n x_shifted_right )
    SWAP 32 SWAP -      ( x x_shifted_right 32-n )
    ROT SWAP LSHIFT     ( x_shifted_right x_shifted_left )
    OR                  ( combined )
    MASK32 ;

\ =========================================================
\ SHA-256 Logical Primitives (The "Sigmas")
\ =========================================================

\ Standard choice function: Ch(x, y, z) = (x AND y) XOR ((NOT x) AND z)
: CH ( x y z -- res )
    >R OVER AND         ( x x&y )
    SWAP INVERT         ( x&y ~x )
    R> AND XOR          ( res ) 
    MASK32 ;

\ Majority function: Maj(x, y, z) = (x AND y) XOR (x AND z) XOR (y AND z)
: MAJ ( x y z -- res )
    DUP >R              ( x y z )
    OVER AND            ( x y x&y )
    >R                  ( x y )
    OVER R@ AND         ( x y x&z )
    ROT R> AND          ( x x&z y&z )
    XOR XOR             ( res )
    R> DROP             ( res )
    MASK32 ;

\ Big Sigma 0: ROTR(2) ^ ROTR(13) ^ ROTR(22)
: BSIG0 ( x -- res )
    DUP 2 ROTR32
    OVER 13 ROTR32 XOR
    SWAP 22 ROTR32 XOR ;

\ Big Sigma 1: ROTR(6) ^ ROTR(11) ^ ROTR(25)
: BSIG1 ( x -- res )
    DUP 6 ROTR32
    OVER 11 ROTR32 XOR
    SWAP 25 ROTR32 XOR ;
    
\ Little Sigma 0: ROTR(7) ^ ROTR(18) ^ SHR(3)
: LSIG0 ( x -- res )
    DUP 7 ROTR32
    OVER 18 ROTR32 XOR
    SWAP 3 RSHIFT XOR
    MASK32 ;

\ Little Sigma 1: ROTR(17) ^ ROTR(19) ^ SHR(10)
: LSIG1 ( x -- res )
    DUP 17 ROTR32
    OVER 19 ROTR32 XOR
    SWAP 10 RSHIFT XOR
    MASK32 ;

\ =========================================================
\ SHA-256 Constants (K)
\ =========================================================

CREATE K-TABLE
    $428a2f98 , $71374491 , $b5c0fbcf , $e9b5dba5 ,
    $3956c25b , $59f111f1 , $923f82a4 , $ab1c5ed5 ,
    $d807aa98 , $12835b01 , $243185be , $550c7dc3 ,
    $72be5d74 , $80deb1fe , $9bdc06a7 , $c19bf174 ,
    $e49b69c1 , $efbe4786 , $0fc19dc6 , $240ca1cc ,
    $2de92c6f , $4a7484aa , $5cb0a9dc , $76f988da ,
    $983e5152 , $a831c66d , $b00327c8 , $bf597fc7 ,
    $c6e00bf3 , $d5a79147 , $06ca6351 , $14292967 ,
    $27b70a85 , $2e1b2138 , $4d2c6dfc , $53380d13 ,
    $650a7354 , $766a0abb , $81c2c92e , $92722c85 ,
    $a2bfe8a1 , $a81a664b , $c24b8b70 , $c76c51a3 ,
    $d192e819 , $d6990624 , $f40e3585 , $106aa070 ,
    $19a4c116 , $1e376c08 , $2748774c , $34b0bcb5 ,
    $391c0cb3 , $4ed8aa4a , $5b9cca4f , $682e6ff3 ,
    $748f82ee , $78a5636f , $84c87814 , $8cc70208 ,
    $90befffa , $a4506ceb , $bef9a3f7 , $c67178f2 ,

\ Word to fetch K[i]
: GET-K ( index -- k )
    CELLS K-TABLE + @ ;

\ =========================================================
\ Message Schedule Array
\ =========================================================

\ Allocate 64 cells (4 bytes each = 256 bytes total)
CREATE W-ARRAY 256 ALLOT

\ Helper to access W[i]
: W@ ( index -- val ) CELLS W-ARRAY + @ ;
: W! ( val index -- ) CELLS W-ARRAY + ! ;

\ =========================================================
\ Message Expansion
\ Fills W[16]..W[63] based on W[0]..W[15]
\ =========================================================

: EXPAND-MSG ( -- )
    64 16 DO
        I 2 - W@ LSIG1      ( term1 )
        I 7 - W@ +          ( term1+term2 )
        I 15 - W@ LSIG0 +   ( sum_so_far )
        I 16 - W@ +         ( total_sum )
        MASK32 I W!
    LOOP ;

\ =========================================================
\ Working Variables
\ =========================================================

CREATE WORK-VARS 32 ALLOT

: GET-VAR ( index -- val ) CELLS WORK-VARS + @ ;
: SET-VAR ( val index -- ) CELLS WORK-VARS + ! ;

\ Byte-swap a 32-bit number ( Little Endian <-> Big Endian )
: BSWAP32 ( n -- n' )
    DUP $FF AND 24 LSHIFT       ( byte0_shifted )
    OVER 8 RSHIFT $FF AND 16 LSHIFT OR
    OVER 16 RSHIFT $FF AND 8 LSHIFT OR
    SWAP 24 RSHIFT $FF AND OR ;

CR ." [CRYPTO] SHA-256 primitives loaded." CR
