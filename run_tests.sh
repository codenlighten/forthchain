#!/bin/bash
cd "$(dirname "$0")"
gforth -m 128M << 'EOF'
\ Load modules with proper paths
include src/debug.fs
include src/math/stack.fs  
include src/math/math256.fs
include src/crypto/sha256.fs

CR ." [SYSTEM] Core Modules Loaded Successfully." CR

\ Test framework
VARIABLE PASS-COUNT
VARIABLE FAIL-COUNT
0 PASS-COUNT !
0 FAIL-COUNT !

: ASSERT-TRUE ( flag name_addr name_len -- )
    ROT IF 
        PASS-COUNT @ 1+ PASS-COUNT !
        ." ."
    ELSE
        FAIL-COUNT @ 1+ FAIL-COUNT !
        CR ." [FAIL] " TYPE CR
    THEN ;

\ Load the comprehensive test suites
include tests/test_math.fs
include tests/test_crypto.fs
include tests/test_merkle.fs

\ Summary
CR CR
." ======================================" CR
PASS-COUNT @ . ." tests passed. "
FAIL-COUNT @ . ." tests failed." CR

\ Use [IF] for conditional compilation instead of IF
FAIL-COUNT @ 0> [IF]
    CR ." !!! TESTS FAILED !!!" CR
    1 (bye)
[ELSE]
    CR ." [SUCCESS] All tests passed!" CR
    0 (bye)
[THEN]
EOF
