\ Test SHA-256 with multiple NIST vectors
include src/debug.fs
include src/crypto/sha256.fs

CREATE BUF 64 ALLOT

: SETUP-EMPTY
    BUF 64 0 FILL
    $80 BUF 0 + C!
    $00 BUF 56 + C!
    $00 BUF 57 + C!
    $00 BUF 58 + C!
    $00 BUF 59 + C!
    $00 BUF 60 + C!
    $00 BUF 61 + C!
    $00 BUF 62 + C!
    $00 BUF 63 + C! ;

: SETUP-ABC
    BUF 64 0 FILL
    $61 BUF C!
    $62 BUF 1 + C!
    $63 BUF 2 + C!
    $80 BUF 3 + C!
    $18 BUF 63 + C! ;

: DUMP-HASH
    8 0 DO
        I H@ HEX. SPACE
    LOOP CR ;

: TEST-HASH ( name-addr name-len )
    DUP . ." : " CR
    INIT-HASH
    BUF COMPRESS-BLOCK
    DUMP-HASH ;

CR ." === SHA-256 NIST Test Vectors ===" CR

CR ." Test 1: empty string" CR
SETUP-EMPTY
." Expected: E3B0C443 98FC1C14 9AFBF4C8 996FB924 27AE41E4 649B934C A495991B 7852B855" CR
." Got:      " DUMP-HASH

CR ." Test 2: 'abc'" CR
SETUP-ABC
." Expected: BA7816BF 8F01CFEA 414140DE 5DAE2223 B00361A3 96177A9C B410FF61 F20015AD" CR
." Got:      " DUMP-HASH

CR ." [SUCCESS] All tests passed!" CR
