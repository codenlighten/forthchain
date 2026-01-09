\ Test MAJ and BSIG0
include src/crypto/sha256.fs

0 VALUE TEST-VAL

: TEST-SIG-FUNCTIONS
    CR ." Testing BSIG0..." CR
    
    \ BSIG0(0x6a09e667) should be 0xce20b47e
    $6A09E667 BSIG0 
    DUP HEX. CR
    DUP $CE20B47E = IF ." CORRECT" ELSE ." WRONG" THEN CR
    
    CR ." Testing MAJ..." CR
    \ MAJ(0x6a09e667, 0xbb67ae85, 0x3c6ef372) should be 0x3a6fe667
    $6A09E667 $BB67AE85 $3C6EF372 MAJ
    DUP HEX. CR
    DUP $3A6FE667 = IF ." CORRECT" ELSE ." WRONG" THEN CR
    ;

TEST-SIG-FUNCTIONS
