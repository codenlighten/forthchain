\ Minimal tests for 256+!
include src/math/math256.fs

CREATE A 32 ALLOT
CREATE B 32 ALLOT

: SET256 ( d c b a addr -- ) 256! ;
: GET256 ( addr -- d c b a ) 256@ ;

: TEST1
    \ A = 0x00000000...00000001
    0 0 0 1 A SET256
    \ B = 0x00000000...00000001
    0 0 0 1 B SET256
    A B 256+!
    CR ." TEST1: 1 + 1 = 2" CR
    A GET256 256. ;

: TEST2
    \ A = 0x00000000...FFFFFFFFFFFFFFFF
    0 0 $FFFFFFFFFFFFFFFF $FFFFFFFFFFFFFFFF A SET256
    \ B = 1
    0 0 0 1 B SET256
    A B 256+!
    CR ." TEST2: carry into higher cell" CR
    A GET256 256. ;

TEST1
TEST2
