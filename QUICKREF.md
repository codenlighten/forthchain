# ForthCoin - Quick Reference Guide

## Getting Started

### Build & Run
```bash
cd /home/greg/dev/forthcoin
make clean          # Clean artifacts
make test           # Run tests (2 passing)
make shell          # Interactive Gforth shell
make run            # Run main node
```

### Project Structure
```
src/
├── debug.fs              # Logging & tracing
├── load.fs               # Module loader
├── main.fs               # Node entry point
├── math/
│   ├── stack.fs          # Stack utilities
│   └── math256.fs        # 256-bit arithmetic
├── crypto/
│   └── sha256.fs         # SHA-256 (85% complete)
├── consensus/
│   ├── merkle.fs         # Merkle trees (needs SHA-256)
│   └── mining.fs         # Proof-of-work
├── storage/
│   └── storage.fs        # Persistence layer
└── net/
    └── network.fs        # P2P protocol

tests/
├── run_tests.fs          # Test framework
├── test_math.fs          # Math tests
├── test_crypto.fs        # Crypto tests
├── test_merkle.fs        # Merkle tests
├── test_storage.fs       # Storage tests
└── test_net.fs           # Network tests

Documentation/
├── README.md             # Project overview
├── technical.md          # Technical specifications
├── overview.md           # Architecture docs (3,488 lines)
├── IMPLEMENTATION_SUMMARY.md  # This summary
├── PROGRESS.md           # Progress report
└── STATUS.md             # Current status
```

## Key Data Structures

### 256-bit Integer
```forth
256VAR my-hash
$6a09e667 my-hash !      \ Store value
my-hash @ .              \ Read value
```

### Hash State (SHA-256)
```forth
INIT-HASH                \ Initialize H[0..7]
0 H@ . \ Read first hash word
```

### Block Header
```forth
CREATE BLOCK-HDR 80 ALLOT
BLOCK-HDR SHA256-BLOCK   \ Hash one block
```

### Database
```forth
OPEN-DB                  \ Open blockchain.dat
WRITE-BLOCK addr         \ Store block with hash index
READ-BLOCK hash-addr addr \ Retrieve by hash
```

## Core Operations

### 256-bit Math
```forth
h1 h2 256<       \ Compare (less than)
h1 256LSHIFT1    \ Left shift by 1
3DUP 4DUP        \ Stack duplication
```

### SHA-256 Primitives
```forth
ROTR32       \ 32-bit rotate right
BSIG0 BSIG1  \ Big sigma functions
LSIG0 LSIG1  \ Little sigma functions
CH MAJ       \ Boolean operations
```

### Hashing
```forth
COMPRESS-BLOCK       \ Process one 512-bit block
INIT-HASH            \ Start new hash
0 H@ 1 H@ ... 7 H@  \ Read 8 hash words
```

### Mining
```forth
SET-TARGET-FROM-BITS   \ Expand difficulty
MINE-BLOCK             \ Execute mining loop
```

## Testing

### Run Tests
```forth
make test           # Automated test suite
gforth test_sha256.fs       # SHA-256 test
gforth verify_sha256.fs     # Component verification
```

### Component Tests
```forth
CREATE TESTBLOCK 64 ALLOT
TESTBLOCK 64 0 FILL         \ Clear block
TESTBLOCK SHA256-BLOCK      \ Hash it
0 H@ . \ Inspect first word
```

## Status & Known Issues

✅ **Working**:
- 256-bit math and comparisons
- SHA-256 primitives (ROTR32, BSIG, CH, MAJ)
- Message schedule expansion
- Block storage with hash indexing
- Network protocol structures
- Mining loop framework

⚠️ **Needs Work**:
- SHA-256 final output (hash mismatch on NIST vectors)
- 256-bit addition carry propagation
- Socket layer for actual P2P connections

## Next Steps

1. **Debug SHA-256** - Compare output against reference
2. **Fix 256+!** - Implement proper ripple carry
3. **Integrate Merkle** - Connect to SHA-256
4. **Test Mining** - Run with real hashes
5. **Add Networking** - Socket integration

## Performance Notes

- **Block lookup**: O(1) with 65,536 hash buckets
- **Merkle trees**: O(n) for n transactions
- **Mining loop**: ~100k iterations tested
- **Storage**: Append-only for crash recovery
- **Memory**: ~1MB for blockchain buffers

## Resources

- **Repository**: https://github.com/codenlighten/forthchain
- **Gforth**: http://www.complang.tuwien.ac.at/forth/gforth/
- **Bitcoin specs**: https://developer.bitcoin.org/

## Troubleshooting

### "Invalid memory address"
Usually means array access out of bounds. Check:
- W-ARRAY is 512 bytes (indices 0-63 valid)
- WORK-VARS is 64 bytes (indices 0-7 valid)
- TX-BUFFER size for your block count

### "Stack underflow"
Check for mismatched stack effects in words.
All loops use VARIABLE storage (no >R/R>).

### SHA-256 hash mismatch
Components verified working. Issue in final output.
Next: Trace round-by-round against reference.

---

**Last Updated**: January 9, 2026
**Status**: Foundation complete, integration phase
