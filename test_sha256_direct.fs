\ Test SHA-256 implementation properly
include src/debug.fs
include src/crypto/sha256.fs

CR ." === SHA-256 Test for 'abc' ===" CR

CREATE BUF 64 ALLOT

: SETUP-INPUT
    BUF 64 0 FILL
    $61 BUF C!
    $62 BUF 1 + C!
    $63 BUF 2 + C!
    $80 BUF 3 + C!
    $18 BUF 63 + C! ;

: TEST-SHA256
    CR ." Setting up input..." CR
    SETUP-INPUT
    
    CR ." Initializing hash..." CR
    INIT-HASH
    
    CR ." Loading W array..." CR
    16 0 DO
        BUF I 4 * + FETCH32BE
        I W256!
    LOOP
    
    CR ." Expanding message schedule..." CR
    EXPAND-MSG
    
    CR ." Running compression..." CR
    BUF COMPRESS-BLOCK
    
    CR ." Hash output:" CR
    8 0 DO
        I H@ HEX. SPACE
    LOOP CR
    
    CR ." Expected:   BA7816BF 8F01CFEA 414140DE 5DAE2223 B00361A3 96177A9C B410FF61 F20015AD" CR ;

TEST-SHA256
