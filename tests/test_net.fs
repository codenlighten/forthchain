\ =========================================================
\ FORTHCOIN NETWORK PARSER TEST
\ =========================================================
\ Tests message parsing with real Bitcoin protocol data

CR ." [TEST] Running Network Parser Suite..." CR

\ 1. Create the Mock Buffer (Real Bitcoin version message header)
CREATE MOCK-MSG
    \ Header (24 bytes)
    HEX
    F9 C, BE C, B4 C, D9 C,  \ Magic (Mainnet)
    76 C, 65 C, 72 C, 73 C, 69 C, 6F C, 6E C, 00 C,  \ "version" (8 bytes)
    00 C, 00 C, 00 C, 00 C,  \ padding (4 bytes)
    64 C, 00 C, 00 C, 00 C,  \ Length (100 bytes)
    3B C, 64 C, 8D C, 5A C,  \ Checksum
    
    \ Payload (partial - first 16 bytes)
    7F C, 11 C, 01 C, 00 C,  \ Version 70015
    0D C, 04 C, 00 C, 00 C,  \ Services (low)
    00 C, 00 C, 00 C, 00 C,  \ Services (high)
    00 C, 00 C, 00 C, 00 C,  \ Timestamp (low)
    DECIMAL

\ Mock reader pointer
VARIABLE MOCK-PTR

: INIT-MOCK  MOCK-MSG MOCK-PTR ! ;
: MOCK-READ-32 ( -- n ) 
    MOCK-PTR @ @ 
    4 MOCK-PTR +! ;
: MOCK-READ-BYTE ( -- b )
    MOCK-PTR @ C@
    1 MOCK-PTR +! ;

\ Test: Parse magic bytes
: TEST-PARSE-MAGIC ( -- )
    MOCK-MSG PARSE-MAGIC
    HEX $D9B4BEF9 = s" Magic bytes correct" ASSERT-TRUE
    DECIMAL ;

\ Test: Parse command
: TEST-PARSE-COMMAND ( -- )
    MOCK-MSG PARSE-COMMAND
    s" version" COMPARE 0= s" Command is 'version'" ASSERT-TRUE ;

\ Test: Parse length
: TEST-PARSE-LENGTH ( -- )
    MOCK-MSG PARSE-LENGTH
    100 = s" Payload length is 100" ASSERT-TRUE ;

\ Test: Validate magic
: TEST-VALIDATE-MAGIC ( -- )
    MOCK-MSG VALIDATE-MAGIC
    s" Magic validation works" ASSERT-TRUE ;

\ Test: Validate command
: TEST-VALIDATE-COMMAND ( -- )
    MOCK-MSG CMD-VERSION VALIDATE-COMMAND
    s" Command validation works" ASSERT-TRUE ;

\ Test: Version construction
: TEST-VERSION-CONSTRUCT ( -- )
    CONSTRUCT-VERSION
    0> s" Version message constructs" ASSERT-TRUE
    DROP ;

\ Test: Protocol constants
: TEST-PROTOCOL-CONSTANTS ( -- )
    CMD-VERSION NIP 7 = s" Version command length" ASSERT-TRUE
    CMD-VERACK NIP 6 = s" Verack command length" ASSERT-TRUE ;

\ Execute tests
TEST-PARSE-MAGIC
TEST-PARSE-COMMAND
TEST-PARSE-LENGTH
TEST-VALIDATE-MAGIC
TEST-VALIDATE-COMMAND
TEST-VERSION-CONSTRUCT
TEST-PROTOCOL-CONSTANTS

CR ." [TEST] Network parser tests complete." CR
