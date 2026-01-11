\ =========================================================
\ FORTHCOIN DEPENDENCY MANAGER
\ =========================================================

CR ." [SYSTEM] Loading ForthCoin Core Modules..." CR

\ 1. Foundation (No dependencies)
INCLUDE debug.fs
INCLUDE math/stack.fs
INCLUDE math/math256.fs

\ 2. Cryptography (Depends on Math)
INCLUDE crypto/sha256.fs
INCLUDE crypto/ecc.fs

\ 3. Consensus (Depends on Crypto & Math)
INCLUDE consensus/merkle.fs
INCLUDE consensus/mining.fs
INCLUDE consensus/transactions.fs
INCLUDE consensus/wallet.fs
INCLUDE consensus/mempool.fs
INCLUDE consensus/multisig-stub.fs
INCLUDE consensus/script-stub.fs
INCLUDE consensus/difficulty-stub.fs

\ 4. Storage & Networking
INCLUDE storage/storage-stub.fs
INCLUDE net/network-stub.fs

\ 5. API & Query Interface
INCLUDE api/query-stub.fs
INCLUDE api/websocket-stub.fs

\ 6. CLI Interface (commented out for initial deployment)
\ INCLUDE cli/wallet_cli.fs

CR ." [SYSTEM] Core Modules Loaded Successfully." CR
CR ." ════════════════════════════════════════════════════════" CR
CR ." 🚀 FORTHCOIN v1.0 - Full Node Ready" CR
CR ." ════════════════════════════════════════════════════════" CR
CR ." Available commands:" CR
CR ."   wallet-cli       Start interactive wallet" CR
CR ."   start-ws-server  Start WebSocket API server (port 8765)" CR
CR ."   init-storage     Initialize persistent storage" CR
CR ."   init-network     Initialize P2P networking" CR
CR ."   start-listening  Start accepting peer connections" CR
CR ."   run-network-loop Start network message processing" CR
CR ." ════════════════════════════════════════════════════════" CR
CR

