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
INCLUDE src/crypto/ecc.fs

\ 3. Consensus (Depends on Crypto & Math)
INCLUDE src/consensus/merkle.fs
INCLUDE src/consensus/mining.fs
INCLUDE src/consensus/transactions.fs
INCLUDE src/consensus/wallet.fs
INCLUDE src/consensus/mempool.fs

\ 4. Storage & Networking
INCLUDE src/storage/storage.fs
INCLUDE src/net/network.fs

\ 5. CLI Interface
INCLUDE src/cli/wallet_cli.fs

CR ." [SYSTEM] Core Modules Loaded Successfully." CR
CR ." ════════════════════════════════════════════════════════" CR
CR ." 🚀 FORTHCOIN v1.0 - Full Node Ready" CR
CR ." ════════════════════════════════════════════════════════" CR
CR ." Available commands:" CR
CR ."   wallet-cli       Start interactive wallet" CR
CR ."   init-storage     Initialize persistent storage" CR
CR ."   init-network     Initialize P2P networking" CR
CR ."   start-listening  Start accepting peer connections" CR
CR ."   run-network-loop Start network message processing" CR
CR ." ════════════════════════════════════════════════════════" CR
CR

