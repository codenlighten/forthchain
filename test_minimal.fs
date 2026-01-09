\ Minimal SHA-256 test
REQUIRE src/debug.fs
REQUIRE src/crypto/sha256.fs

CR ." Testing minimal compression..." CR

CREATE TBLOCK 64 ALLOT

: TEST
    TBLOCK 64 0 FILL
    INIT-HASH
    CR ." About to load block..." CR
    16 0 DO
        TBLOCK I CELLS + @ BSWAP32 I W256!
    LOOP
    CR ." Block loaded. Expanding..." CR
    EXPAND-MSG
    CR ." Expanded. Init vars..." CR
    8 0 DO
        I H@ I SET-VAR
    LOOP
    CR ." Vars initialized. Entering loop..." CR
    
    \ Just do ONE round manually
    CR ." Computing T1..." CR
    7 GET-VAR DUP .S CR
    4 GET-VAR BSIG1 + DUP .S CR
    4 GET-VAR 5 GET-VAR 6 GET-VAR CH + DUP .S CR
    0 GET-K + DUP .S CR
    0 W256@ + DUP .S CR
    MASK32 T1 ! .S CR
    
    CR ." Computing T2..." CR
    0 GET-VAR BSIG0 DUP .S CR
    0 GET-VAR 1 GET-VAR 2 GET-VAR MAJ + DUP .S CR
    MASK32 T2 ! .S CR
    
    CR ." Done! T1=" T1 @ HEX . CR
    ."       T2=" T2 @ . DECIMAL CR ;

TEST

CR ." Test complete" CR
BYE
