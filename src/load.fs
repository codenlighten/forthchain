\ =========================================================
\ FORTHCOIN DEPENDENCY MANAGER
\ =========================================================

CR ." [SYSTEM] Loading ForthCoin Core Modules..." CR

\ 1. Foundation (No dependencies)
INCLUDE src/debug.fs
INCLUDE src/math/stack.fs
INCLUDE src/math/math256.fs

\ 2. Cryptography (Depends on Math)
INCLUDE src/crypto/sha256.fs

\ 3. Consensus (Depends on Crypto & Math)
INCLUDE src/consensus/merkle.fs
INCLUDE src/consensus/mining.fs

\ 4. Storage & Networking
INCLUDE src/storage/storage.fs
INCLUDE src/net/network.fs

\ 5. Stubs for modules under development
\ INCLUDE src/crypto/ecc.fs

CR ." [SYSTEM] Core Modules Loaded Successfully." CR
CR ." [READY] Type 'WORDS' to see available commands." CR
