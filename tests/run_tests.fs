\ =========================================================
\ FORTHCOIN TEST RUNNER
\ =========================================================

\ Load the core system first (using relative path from project root)
INCLUDE src/load.fs

CR ." [TEST] Starting Unit Tests..." CR
CR ." ======================================" CR

\ Simple Test Framework
VARIABLE PASS-COUNT
VARIABLE FAIL-COUNT

0 PASS-COUNT !
0 FAIL-COUNT !

: ASSERT-TRUE ( flag name_addr name_len -- )
    ROT IF 
        PASS-COUNT @ 1+ PASS-COUNT !
        ." ."  \ Print dot for pass
    ELSE
        FAIL-COUNT @ 1+ FAIL-COUNT !
        CR ." [FAIL] " TYPE CR
    THEN ;

\ Load Test Definitions
INCLUDE tests/test_math.fs
INCLUDE tests/test_crypto.fs

\ Summary
CR CR
." ======================================" CR
PASS-COUNT @ . ." tests passed. " 
FAIL-COUNT @ . ." tests failed." CR

FAIL-COUNT @ 0> IF
    CR ." !!! TESTS FAILED !!!" CR
    1 (bye)  \ Exit with Error Code 1
ELSE
    CR ." [SUCCESS] All tests passed!" CR
    0 (bye)  \ Exit with Success Code 0
THEN
