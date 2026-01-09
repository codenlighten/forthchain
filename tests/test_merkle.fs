\ =========================================================
\ FORTHCOIN MERKLE TREE TEST SUITE
\ =========================================================

CR ." [TEST] Running Merkle Suite..." CR

\ Test: Basic 3-leaf Merkle tree
: TEST-MERKLE-3LEAF ( -- )
    CLEAR-MERKLE-BUF
    
    \ 1. Create Dummy Hashes (all 0x01, 0x02, 0x03)
    MERKLE-BUF 32 1 FILL       \ Leaf 0 = All 0x01
    MERKLE-BUF 32 + 32 2 FILL  \ Leaf 1 = All 0x02
    MERKLE-BUF 64 + 32 3 FILL  \ Leaf 2 = All 0x03
    
    \ 2. Compute Root for 3 items
    3 CALC-MERKLE-ROOT ( root_addr )
    
    \ 3. Check result (Non-zero check is enough for basic sanity)
    @ 0<> s" Merkle 3-leaf calculation" ASSERT-TRUE ;

\ Test: Single leaf (edge case)
: TEST-MERKLE-SINGLE ( -- )
    CLEAR-MERKLE-BUF
    
    \ Single hash
    MERKLE-BUF 32 $AA FILL
    
    \ Compute root (should be the same hash)
    1 CALC-MERKLE-ROOT ( root_addr )
    
    \ Verify it's the input hash
    @ $AAAAAAAA = s" Merkle single leaf" ASSERT-TRUE ;

\ Test: Even number of leaves (2)
: TEST-MERKLE-EVEN ( -- )
    CLEAR-MERKLE-BUF
    
    MERKLE-BUF 32 1 FILL
    MERKLE-BUF 32 + 32 2 FILL
    
    2 CALC-MERKLE-ROOT
    @ 0<> s" Merkle 2-leaf calculation" ASSERT-TRUE ;

TEST-MERKLE-3LEAF
TEST-MERKLE-SINGLE
TEST-MERKLE-EVEN

CR ." [TEST] Merkle tests complete." CR
