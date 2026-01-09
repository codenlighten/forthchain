\ =========================================================
\ FORTHCOIN DEBUG TRACER
\ =========================================================

VARIABLE LOG-FILE-ID
VARIABLE DEBUG-ENABLED

\ Open/Create debug.log
: INIT-DEBUG ( -- )
    s" debug.log" R/W CREATE-FILE 
    IF 
        s" debug.log" R/W OPEN-FILE ABORT" Cannot open/create log"
    THEN
    LOG-FILE-ID !
    TRUE DEBUG-ENABLED ! ;

\ Write string to log file (and console)
: LOG-TYPE ( addr len -- )
    DEBUG-ENABLED @ IF
        2DUP TYPE       \ Print to console
        LOG-FILE-ID @ ?DUP IF
            >R 2DUP R> WRITE-FILE DROP 
            LOG-FILE-ID @ FLUSH-FILE DROP
        THEN
    ELSE
        2DROP
    THEN ;

\ Write newline
: LOG-CR ( -- )
    CR 
    LOG-FILE-ID @ ?DUP IF
        s" " SWAP WRITE-LINE DROP 
    THEN ;

\ ---------------------------------------------------------
\ The 256-bit Stack Dumper
\ ---------------------------------------------------------

\ Helper: Format one 64-bit cell as 16-char Hex string
: (.HEX16) ( n -- addr len )
    BASE @ >R HEX       \ Save current base, switch to Hex
    0 <# # # # # # # # # # # # # # # # # #> \ 16 hex digits
    R> BASE ! ;         \ Restore base

: .STACK-256 ( -- )
    DEPTH 0= IF s" <Empty>" LOG-TYPE EXIT THEN

    s" [ " LOG-TYPE
    
    \ Loop through stack from bottom to top
    DEPTH 0 ?DO
        I PICK 
        (.HEX16) LOG-TYPE s"  " LOG-TYPE
    LOOP
    
    s" ]" LOG-TYPE ;

\ ---------------------------------------------------------
\ The TRACE-POINT Word
\ ---------------------------------------------------------

: TRACE-POINT ( addr len -- )
    LOG-CR 
    s" === TRACE: " LOG-TYPE LOG-TYPE s"  ===" LOG-TYPE LOG-CR
    
    \ 1. Dump Stack
    s" Stack: " LOG-TYPE .STACK-256 LOG-CR
    
    s" ----------------------------------------" LOG-TYPE LOG-CR ;

\ Initialize debug on load
INIT-DEBUG

CR ." [DEBUG] Logging system initialized." CR
