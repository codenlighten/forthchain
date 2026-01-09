\ Minimal SHA-256 test - step by step
include src/debug.fs
include src/crypto/sha256.fs

CR ." Testing SHA-256 components..." CR

\ Test 1: Hash init
CR ." Init hash test..." CR
INIT-HASH
0 H@ HEX. CR

\ Test 2: Simple message prep
CR ." Creating test input (abc)..." CR
CREATE TEST-BUF 64 ALLOT

\ Manually set up the buffer for "abc" with proper padding
: SETUP-ABC
    \ Clear
    TEST-BUF 64 0 FILL
    
    \ Message "abc" = 0x61 0x62 0x63
    $61 TEST-BUF 0 + C!
    $62 TEST-BUF 1 + C!
    $63 TEST-BUF 2 + C!
    
    \ Padding: append 0x80
    $80 TEST-BUF 3 + C!
    
    \ Length = 24 bits = 0x00000000 00000018 in big-endian
    \ at bytes 56-63
    TEST-BUF 56 + 8 0 FILL  \ Clear that area
    $18 TEST-BUF 63 + C!    \ Put 0x18 at the end
    
    ." Buffer set up" CR ;

SETUP-ABC

\ Test 3: Load into W array
CR ." Loading block into W array..." CR
: LOAD-BLOCK
    16 0 DO
        TEST-BUF I 4 * + FETCH32BE
        I W256!
        ." W[" I . ." ]=" HEX. SPACE
    LOOP CR ;

LOAD-BLOCK

\ Test 4: Try one round of compression
CR ." Testing one compress round..." CR
INIT-HASH
COMPRESS-ROUND 0

CR ." Values after round 0:" CR
8 0 DO
    ." var[" I . ." ]=" GET-VAR HEX. SPACE
LOOP CR

CR ." Done" CR
