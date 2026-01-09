\ Debug message schedule expansion
include src/debug.fs
include src/crypto/sha256.fs

CR ." === Debugging Message Schedule ===" CR

CREATE BUF 64 ALLOT

: SETUP-INPUT
    BUF 64 0 FILL
    $61 BUF C!
    $62 BUF 1 + C!
    $63 BUF 2 + C!
    $80 BUF 3 + C!
    $18 BUF 63 + C! ;

: TEST-MSG-SCHEDULE
    SETUP-INPUT
    
    CR ." Loading W[0..15]..." CR
    16 0 DO
        BUF I 4 * + FETCH32BE
        I W256!
    LOOP
    
    CR ." W array before expansion:" CR
    16 0 DO
        ." W[" I . ." ]=" I W256@ HEX. CR
    LOOP
    
    CR ." Expanding W[16..63]..." CR
    EXPAND-MSG
    
    CR ." First few expanded values:" CR
    20 16 DO
        ." W[" I . ." ]=" I W256@ HEX. CR
    LOOP ;

TEST-MSG-SCHEDULE
