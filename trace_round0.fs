\ Trace first round of compression
include src/debug.fs
include src/crypto/sha256.fs

CR ." === Tracing Round 0 of SHA-256 ===" CR

CREATE BUF 64 ALLOT

: SETUP-INPUT
    BUF 64 0 FILL
    $61 BUF C!
    $62 BUF 1 + C!
    $63 BUF 2 + C!
    $80 BUF 3 + C!
    $18 BUF 63 + C! ;

: TRACE-ROUND-0
    SETUP-INPUT
    INIT-HASH
    
    CR ." Loading W[0..15]..." CR
    16 0 DO
        BUF I 4 * + FETCH32BE
        I W256!
    LOOP
    
    CR ." Expanding message schedule..." CR
    EXPAND-MSG
    
    CR ." Initial values (a..h):" CR
    8 0 DO
        ." var[" I . ." ]=" I H@ HEX. CR
    LOOP
    
    CR ." Setting a..h from hash..." CR
    8 0 DO
        I H@ I SET-VAR
    LOOP
    
    CR ." Value of W[0]:" CR
    0 W256@ HEX. CR
    
    CR ." Value of K[0]:" CR
    0 GET-K HEX. CR
    
    CR ." Computing one round..." CR
    0 COMPRESS-ROUND
    
    CR ." Values after round 0:" CR
    8 0 DO
        ." var[" I . ." ]=" I GET-VAR HEX. CR
    LOOP ;

TRACE-ROUND-0
