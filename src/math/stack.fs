\ =========================================================
\ Stack Utility Words
\ =========================================================

\ Helper: 3rd item on stack
: 3DUP ( a b c -- a b c a b c )
    2 PICK 2 PICK 2 PICK ;

\ Helper: 4th item on stack  
: 4DUP ( a b c d -- a b c d a b c d )
    3 PICK 3 PICK 3 PICK 3 PICK ;

\ Helper: drop 3 items
: 3DROP ( a b c -- )
    DROP DROP DROP ;

\ Helper: drop 4 items
: 4DROP ( a b c d -- )
    DROP DROP DROP DROP ;

CR ." [STACK] Stack utilities loaded." CR
