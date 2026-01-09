\ Test to show current SHA-256 output vs expected
require src/crypto/sha256.fs
require src/debug.fs

\ Create test input "abc"
CREATE TEST-INPUT 64 ALLOT

: PREPARE-TEST-INPUT ( -- )
    \ Clear buffer
    TEST-INPUT 64 0 FILL
    
    \ Set "abc" (0x616263)
    97 TEST-INPUT 0 + C!
    98 TEST-INPUT 1 + C!
    99 TEST-INPUT 2 + C!
    
    \ Set length in bits (24 bits = 3 bytes)
    \ Bitcoin format: bit length at offset 56-63 (big-endian)
    $00 TEST-INPUT 56 + C!
    $00 TEST-INPUT 57 + C!
    $00 TEST-INPUT 58 + C!
    $00 TEST-INPUT 59 + C!
    $00 TEST-INPUT 60 + C!
    $00 TEST-INPUT 61 + C!
    $00 TEST-INPUT 62 + C!
    $18 TEST-INPUT 63 + C!  \ 24 in big-endian
    ;

: DUMP-HASH ( -- )
    CR ." SHA-256 Output:" CR
    ." 0x" 0 H@ HEX. SPACE
    ." 0x" 1 H@ HEX. SPACE
    ." 0x" 2 H@ HEX. SPACE
    ." 0x" 3 H@ HEX. CR
    ." 0x" 4 H@ HEX. SPACE
    ." 0x" 5 H@ HEX. SPACE
    ." 0x" 6 H@ HEX. SPACE
    ." 0x" 7 H@ HEX. CR
    ;

: TEST-ABC
    PREPARE-TEST-INPUT
    TEST-INPUT SHA256-BLOCK
    DUMP-HASH
    CR ." Expected:" CR
    ." 0xBA7816BF 0x8F01CFEA 0x414140DE 0x5DAE2223" CR
    ." 0xB00361A3 0x96177A9C 0xB410FF61 0xF20015AD" CR
    ;

TEST-ABC
