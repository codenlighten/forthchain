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
    SWAP MASK32 SWAP        \ ensure unsigned 32-bit before shifting
    2DUP RSHIFT             \ x n -> x>>n
    SWAP 32 SWAP -          \ x>>n 32-n
    ROT SWAP LSHIFT         \ (x<< (32-n))
    OR MASK32 ;

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
\ Use VALUES to hold intermediate results for clarity
0 VALUE MAJ-X
0 VALUE MAJ-Y
0 VALUE MAJ-Z

: MAJ ( x y z -- res )
    TO MAJ-Z TO MAJ-Y TO MAJ-X
    MAJ-X MAJ-Y AND
    MAJ-X MAJ-Z AND XOR
    MAJ-Y MAJ-Z AND XOR
    MASK32 ;


\ Big Sigma 0: ROTR(2) ^ ROTR(13) ^ ROTR(22)
: BSIG0 ( x -- res )
    MASK32
    DUP 2 ROTR32
    OVER 13 ROTR32 XOR
    SWAP 22 ROTR32 XOR ;

\ Big Sigma 1: ROTR(6) ^ ROTR(11) ^ ROTR(25)
: BSIG1 ( x -- res )
    MASK32
    DUP 6 ROTR32
    OVER 11 ROTR32 XOR
    SWAP 25 ROTR32 XOR ;
    
\ Little Sigma 0: ROTR(7) ^ ROTR(18) ^ SHR(3)
: LSIG0 ( x -- res )
    MASK32
    DUP 7 ROTR32
    OVER 18 ROTR32 XOR
    SWAP 3 RSHIFT XOR
    MASK32 ;

\ Little Sigma 1: ROTR(17) ^ ROTR(19) ^ SHR(10)
: LSIG1 ( x -- res )
    MASK32
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

\ Allocate 64 words (stored as cells)
CREATE W-ARRAY 64 CELLS ALLOT

\ Helper to access W[i] with cell stride
: W256@ ( index -- val ) CELLS W-ARRAY + @ ;
: W256! ( val index -- ) CELLS W-ARRAY + ! ;

\ =========================================================
\ Message Expansion
\ Fills W[16]..W[63] based on W[0]..W[15]
\ =========================================================

: EXPAND-MSG ( -- )
    64 16 DO
        I 2 - W256@ LSIG1      ( term1 )
        I 7 - W256@ +          ( term1+term2 )
        I 15 - W256@ LSIG0 +   ( sum_so_far )
        I 16 - W256@ +         ( total_sum )
        MASK32 I W256!
    LOOP ;

\ =========================================================
\ Working Variables
\ =========================================================

CREATE WORK-VARS 64 ALLOT

VARIABLE VAR-I
0 VALUE VAR-T1
0 VALUE VAR-T2

: GET-VAR ( index -- val ) CELLS WORK-VARS + @ ;
: SET-VAR ( val index -- ) CELLS WORK-VARS + ! ;

\ Byte-swap a 32-bit number ( Little Endian <-> Big Endian )
: BSWAP32 ( n -- n' )
    DUP $FF AND 24 LSHIFT       ( byte0_shifted )
    OVER 8 RSHIFT $FF AND 16 LSHIFT OR
    OVER 16 RSHIFT $FF AND 8 LSHIFT OR
    SWAP 24 RSHIFT $FF AND OR ;

\ Read 4 bytes big-endian into a 32-bit cell
: FETCH32BE ( addr -- u )
    DUP C@ 24 LSHIFT            \ byte0 << 24
    OVER 1+ C@ 16 LSHIFT OR
    OVER 2 + C@ 8 LSHIFT OR
    SWAP 3 + C@ OR
    MASK32 ;

\ =========================================================
\ SHA-256 Initial Hash Values (H0..H7)
\ =========================================================

CREATE H-INIT
    $6a09e667 , $bb67ae85 , $3c6ef372 , $a54ff53a ,
    $510e527f , $9b05688c , $1f83d9ab , $5be0cd19 ,

CREATE H-STATE 64 ALLOT

: H@ ( index -- val ) CELLS H-STATE + @ ;
: H! ( val index -- ) CELLS H-STATE + ! ;

: INIT-HASH ( -- )
    8 0 DO
        I CELLS H-INIT + @ I H!
    LOOP ;

\ =========================================================
\ SHA-256 Compression Function (Main Loop)
\ =========================================================

\ One round of compression (safe: no return-stack use)
: COMPRESS-ROUND ( i -- )
    VAR-I !   \ save index
    
    \ Compute T1 = h + BSIG1(e) + CH(e,f,g) + K[i] + W[i]
    7 GET-VAR
    4 GET-VAR BSIG1 +
    4 GET-VAR 5 GET-VAR 6 GET-VAR CH +
    VAR-I @ GET-K +
    VAR-I @ W256@ +
    MASK32 TO VAR-T1

    \ Compute T2 = BSIG0(a) + MAJ(a,b,c)
    0 GET-VAR BSIG0
    0 GET-VAR 1 GET-VAR 2 GET-VAR MAJ +
    MASK32 TO VAR-T2

    \ Shift and update state
    6 GET-VAR 7 SET-VAR  \ h = g
    5 GET-VAR 6 SET-VAR  \ g = f
    4 GET-VAR 5 SET-VAR  \ f = e
    3 GET-VAR VAR-T1 + MASK32 4 SET-VAR  \ e = d + T1
    2 GET-VAR 3 SET-VAR  \ d = c
    1 GET-VAR 2 SET-VAR  \ c = b
    0 GET-VAR 1 SET-VAR  \ b = a
    VAR-T1 VAR-T2 + MASK32 0 SET-VAR ; \ a = T1 + T2

\ Process one 512-bit block
: COMPRESS-BLOCK ( addr -- )
    \ Load input block into W[0]..W[15] (big-endian bytes)
    16 0 DO
        DUP I 4 * + FETCH32BE
        I W256!
    LOOP DROP
    
    \ Expand message schedule
    EXPAND-MSG
    
    \ Initialize working variables a-h from current hash
    8 0 DO
        I H@ I SET-VAR
    LOOP
    
    \ Main compression loop (64 rounds)
    64 0 DO
        I COMPRESS-ROUND
    LOOP
    
    \ Add compressed chunk to hash values
    8 0 DO
        I H@ I GET-VAR + MASK32 I H!
    LOOP ;

\ =========================================================
\ Single SHA-256 Hash
\ =========================================================

\ Compute SHA-256 of a 512-bit (64-byte) block
\ Input: addr = address of 64-byte block
\ Output: 32-byte hash stored in H-STATE
: SHA256-BLOCK ( addr -- )
    INIT-HASH
    COMPRESS-BLOCK ;

\ =========================================================
\ Double SHA-256 (Bitcoin Standard)
\ =========================================================

\ Storage for intermediate hash result
CREATE TEMP-HASH 64 ALLOT

\ Compute SHA256(SHA256(data)) for 32-byte input
\ Input: addr = address of 32-byte data
\ Output: 32-byte hash stored in H-STATE
: HASH256 ( addr -- )
    \ First hash
    INIT-HASH
    COMPRESS-BLOCK
    
    \ Copy result to temp buffer
    8 0 DO
        I H@ I CELLS TEMP-HASH + !
    LOOP
    
    \ Second hash (of the temp result)
    INIT-HASH
    TEMP-HASH COMPRESS-BLOCK ;

\ =========================================================
\ Helper: Copy hash result to destination
\ =========================================================

: COPY-HASH ( dest-addr -- )
    8 0 DO
        I H@ OVER I CELLS + !
    LOOP DROP ;

CR ." [CRYPTO] SHA-256 compression function loaded." CR
