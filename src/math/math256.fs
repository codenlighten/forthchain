\ =========================================================
\ 256-bit Unsigned Integer Arithmetic for Blockchain
\ Assumes 64-bit cell size.
\ Representation: ( d c b a ) where 'a' is least significant
\ =========================================================

\ Define a "256-bit variable" (allocates 32 bytes)
: 256VAR CREATE 32 ALLOT ;

\ Helper: Print a 256-bit number in hex (for debugging)
: 256. ( d c b a -- )
    BASE @ >R HEX
    0 <# # # # # # # # # # # # # # # # # #> TYPE SPACE
    0 <# # # # # # # # # # # # # # # # # #> TYPE SPACE
    0 <# # # # # # # # # # # # # # # # # #> TYPE SPACE
    0 <# # # # # # # # # # # # # # # # # #> TYPE
    R> BASE ! CR ;

\ Helper: Zero a 256-bit number on the stack
: 256ZERO ( -- 0 0 0 0 )
    0 0 0 0 ;

\ Fetch 256-bit from memory to stack
: 256@ ( addr -- d c b a )
    DUP 24 + @ SWAP
    DUP 16 + @ SWAP
    DUP 8 + @ SWAP
    @ ;

\ Store 256-bit from stack to memory
: 256! ( d c b a addr -- )
    >R
    R@ !         \ Store a (LSB) 
    R@ 8 + !     \ Store b
    R@ 16 + !    \ Store c
    R> 24 + ! ;  \ Store d (MSB)

\ =========================================================
\ 256-bit Memory-Based Addition (Little Endian)
\ Usage: ( addr_dest addr_src -- )
\ Adds the value at src into dest.
\ =========================================================

: 256+! ( addr_dest addr_src -- )
    \ Save original addresses
    OVER OVER              ( dest src dest src )
    
    \ STEP 1: CELL A (Least Significant)
    @ SWAP @               ( dest src val_src val_dest )
    +                      ( dest src sum_a )
    DUP 2 PICK @ < IF 1 ELSE 0 THEN  ( dest src sum_a carry )
    ROT ROT               ( dest carry src sum_a )
    2 PICK !              ( dest carry src )  \ Store to dest
    
    \ Move to Cell B
    8 + SWAP 8 + SWAP     ( carry src+8 dest+8 )

    \ STEP 2: CELL B
    OVER OVER              ( carry src dest src dest )
    @ SWAP @               ( carry src dest val_dest val_src )
    + ROT +                ( src dest sum_b_total )
    DUP 2 PICK @ < IF 1 ELSE 0 THEN  ( src dest sum_b carry2 )
    ROT ROT                ( src carry2 dest sum_b )
    2 PICK !               ( src carry2 dest )
    
    \ Move to Cell C
    8 + SWAP 8 + SWAP      ( carry2 src+16 dest+16 )

    \ STEP 3: CELL C
    OVER OVER
    @ SWAP @
    + ROT +
    DUP 2 PICK @ < IF 1 ELSE 0 THEN
    ROT ROT
    2 PICK !
    
    \ Move to Cell D
    8 + SWAP 8 + SWAP      ( carry3 src+24 dest+24 )

    \ STEP 4: CELL D (Most Significant)
    OVER @ SWAP @ + ROT +  ( dest sum_d )
    SWAP ! ;

\ =========================================================
\ 256-bit Memory-Based Right Shift (>> 1)
\ Usage: ( addr -- )
\ Shifts the 256-bit number at addr right by 1 bit.
\ =========================================================

: 256RSHIFT1 ( addr -- )
    \ Process from Highest Cell (D) down to Lowest (A)
    
    \ 1. Fetch Cell D (MSB)
    DUP 24 +        ( addr addr_d )
    DUP @           ( addr addr_d val_d )
    
    \ Save the LSB of Cell D (carry to Cell C)
    DUP 1 AND       ( addr addr_d val_d carry_d )
    SWAP 1 RSHIFT   ( addr addr_d carry_d val_d_shifted )
    ROT !           ( addr carry_d )

    \ 2. Process Cell C
    OVER 16 +       ( addr carry_d addr_c )
    DUP @           ( addr carry_d addr_c val_c )
    DUP 1 AND       ( addr carry_d addr_c val_c carry_c )
    SWAP 1 RSHIFT   ( addr carry_d addr_c carry_c val_c_shifted )
    
    \ Inject carry from D into MSB of C (bit 63)
    ROT 63 LSHIFT OR  ( addr carry_d addr_c val_c_new )
    SWAP !            ( addr carry_d )

    \ 3. Process Cell B
    OVER 8 +        ( addr carry_c addr_b )
    DUP @ 
    DUP 1 AND       ( addr carry_c addr_b val_b carry_b )
    SWAP 1 RSHIFT   ( addr carry_c addr_b carry_b val_b_shifted )
    ROT 63 LSHIFT OR 
    SWAP !          ( addr carry_b )

    \ 4. Process Cell A (LSB)
    OVER            ( addr carry_b addr_a )
    DUP @
    1 RSHIFT        ( addr carry_b addr_a val_a_shifted )
    ROT 63 LSHIFT OR
    SWAP !          ( addr )
    DROP ;

\ =========================================================
\ 256-bit Memory-Based Left Shift (<< 1)
\ Usage: ( addr -- )
\ Shifts the 256-bit number at addr left by 1 bit.
\ =========================================================

: 256LSHIFT1 ( addr -- )
    \ Process from Lowest (Cell A) to Highest (Cell D) to carry up
    
    \ 1. Cell A
    DUP @           ( addr val_a )
    DUP 63 RSHIFT   ( addr val_a carry_out )
    SWAP 1 LSHIFT   ( addr carry_out val_a_new )
    OVER SWAP OVER ! ( addr carry_out )
    
    \ 2. Cell B
    OVER 8 +        ( addr carry_in addr_b )
    DUP @           ( addr carry_in addr_b val_b )
    DUP 63 RSHIFT   ( addr carry_in addr_b val_b carry_out_b )
    >R              ( Save new carry )
    1 LSHIFT OR     ( addr addr_b val_b_new )
    SWAP !          ( addr )
    R>              ( addr carry_out_b )

    \ 3. Cell C
    OVER 16 +       ( addr carry_in addr_c )
    DUP @           ( addr carry_in addr_c val_c )
    DUP 63 RSHIFT >R
    1 LSHIFT OR SWAP !
    R>              ( addr carry_out_c )
    
    \ 4. Cell D
    SWAP 24 +       ( carry_in addr_d )
    DUP @ 1 LSHIFT OR SWAP !
    ;

\ =========================================================
\ 256-bit Comparison (Unsigned)
\ Usage: ( addr1 addr2 -- flag )
\ Returns TRUE (-1) if [addr1] < [addr2], else FALSE (0)
\ =========================================================

: 256< ( addr1 addr2 -- flag )
    \ Compare from Most Significant Cell (Cell D) down to A
    
    \ 1. Compare Cell D (MSB)
    2DUP 24 + @ SWAP 24 + @  ( addr1 addr2 val_d1 val_d2 )
    2DUP < IF 2DROP 2DROP TRUE EXIT THEN
    > IF 2DROP FALSE EXIT THEN
    
    \ 2. Compare Cell C
    2DUP 16 + @ SWAP 16 + @
    2DUP < IF 2DROP 2DROP TRUE EXIT THEN
    > IF 2DROP FALSE EXIT THEN

    \ 3. Compare Cell B
    2DUP 8 + @ SWAP 8 + @
    2DUP < IF 2DROP 2DROP TRUE EXIT THEN
    > IF 2DROP FALSE EXIT THEN

    \ 4. Compare Cell A (LSB)
    @ SWAP @ < ; 

CR ." [MATH] 256-bit arithmetic loaded." CR
