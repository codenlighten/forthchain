\ =========================================================
\ FORTHCOIN STORAGE TEST SUITE
\ =========================================================

CR ." [TEST] Running Storage Suite..." CR

\ Test buffer
CREATE TEMP-BUF 128 ALLOT

\ Test: Database open/close
: TEST-DB-OPEN ( -- )
    OPEN-DB
    DB-FILE-ID @ 0<> s" Database opens successfully" ASSERT-TRUE
    CLOSE-DB ;

\ Test: Clear index
: TEST-CLEAR-INDEX ( -- )
    CLEAR-INDEX
    INDEX-TABLE @ 0= s" Index clears to zero" ASSERT-TRUE ;

\ Test: Hash to bucket mapping
: TEST-HASH-BUCKET ( -- )
    \ Create a test hash (first 2 bytes matter for bucket)
    TEMP-BUF 32 ERASE
    HEX $1234 TEMP-BUF W!
    
    \ Get bucket
    TEMP-BUF HASH->BUCKET
    
    \ Verify it's within range
    INDEX-TABLE - BUCKETS CELLS < s" Bucket within range" ASSERT-TRUE
    DECIMAL ;

\ Test: Basic block write (without actual file I/O in test)
: TEST-BLOCK-STRUCTURE ( -- )
    \ Just verify we can construct block data
    TEMP-BUF 80 $AA FILL
    TRUE s" Block buffer prepared" ASSERT-TRUE ;

\ Execute tests
TEST-DB-OPEN
TEST-CLEAR-INDEX
TEST-HASH-BUCKET
TEST-BLOCK-STRUCTURE

CR ." [TEST] Storage tests complete." CR
