\ Unit tests for 256+!
include src/math/math256.fs

CREATE A 32 ALLOT
CREATE B 32 ALLOT

: SET256 ( d c b a addr -- ) 256! ;
: GET256 ( addr -- d c b a ) 256@ ;

: SHOW256 ( addr -- ) GET256 256. ;

: TEST1
    \ A = 1, B = 1 => A = 2
    0 0 0 1 A SET256
    0 0 0 1 B SET256
    A B 256+!
    CR ." TEST1: 1 + 1" CR
    A SHOW256 ;

: TEST2
    \ A = FFFF...FFFF, B = 1 => A = 0000...0000 with carry into next word
    0 0 $FFFFFFFFFFFFFFFF $FFFFFFFFFFFFFFFF A SET256
    0 0 0 1 B SET256
    A B 256+!
    CR ." TEST2: carry from low word" CR
    A SHOW256 ;

: TEST3
    \ A = all ones, B = 1 => wrap to zero with final carry drop
    $FFFFFFFFFFFFFFFF $FFFFFFFFFFFFFFFF $FFFFFFFFFFFFFFFF $FFFFFFFFFFFFFFFF A SET256
    0 0 0 1 B SET256
    A B 256+!
    CR ." TEST3: full wraparound" CR
    A SHOW256 ;

TEST1
TEST2
TEST3
