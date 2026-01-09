\ =========================================================
\ Stack Utility Words
\ =========================================================

\ Helper: 3rd item on stack
: 3DUP ( a b c -- a b c a b c )
    2 PICK 2 PICK 2 PICK ;

\ Helper: 4th item on stack  
: 4DUP ( a b c d -- a b c d a b c d )
    3 PICK 3 PICK 3 PICK 3 PICK ;

CR ." [STACK] Stack utilities loaded." CR
