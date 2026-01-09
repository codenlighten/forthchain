# ForthCoin - Blockchain Implementation in Forth

A complete blockchain implementation built from scratch using Forth, with no external dependencies beyond the core language.

## Project Status

**Phase 1: Foundation** ✅
- 256-bit arithmetic (addition, comparison, bit shifts)
- Debug logging system
- SHA-256 primitives (rotations, logical functions)
- Test framework

**Phase 2: Cryptography** 🚧 In Progress
- SHA-256 compression function
- Elliptic Curve (secp256k1)
- ECDSA signatures

**Phase 3-6:** Planned
- Transaction structure & Merkle trees
- Mining & consensus
- P2P networking
- Persistent storage

## Requirements

- Gforth (GNU Forth) - Install via: `sudo apt install gforth` (Linux) or `brew install gforth` (macOS)
- 64-bit system (required for 256-bit arithmetic)

## Quick Start

```bash
# Run tests
make test

# Interactive development
make shell

# Clean data files
make clean
```

## Project Structure

```
forthcoin/
├── src/
│   ├── load.fs           # Dependency manager
│   ├── debug.fs          # Logging & tracing
│   ├── math/
│   │   ├── stack.fs      # Stack utilities
│   │   └── math256.fs    # 256-bit arithmetic
│   ├── crypto/
│   │   └── sha256.fs     # SHA-256 implementation
│   ├── consensus/        # Mining & Merkle trees (TODO)
│   ├── net/              # P2P networking (TODO)
│   └── storage/          # Blockchain storage (TODO)
└── tests/
    ├── run_tests.fs      # Test runner
    ├── test_math.fs      # Math tests
    └── test_crypto.fs    # Crypto tests
```

## Architecture

ForthCoin implements a Bitcoin-like blockchain with:

- **256-bit big integer math** for cryptographic operations
- **SHA-256** for hashing (mining, addresses, Merkle trees)
- **secp256k1** elliptic curve for signatures
- **Stack-based scripting** leveraging Forth's native VM
- **Proof of Work** consensus
- **Flat-file storage** with in-memory indexing

## Development

The build system enforces test-driven development:

1. Write tests in `tests/test_*.fs`
2. Implement features in `src/`
3. Run `make test` to verify
4. Use `make shell` for interactive debugging

## Testing

```bash
# Run all tests
make test

# Expected output:
# [TEST] Starting Unit Tests...
# ......................
# X tests passed. 0 tests failed.
# [SUCCESS] All tests passed!
```

## Documentation

- [overview.md](overview.md) - High-level architecture and feasibility
- [technical.md](technical.md) - Complete implementation roadmap

## Why Forth?

1. **Native stack VM** - Perfect for Bitcoin Script-like operations
2. **Minimal footprint** - Complete node in <100KB
3. **Direct hardware access** - Can run on embedded devices
4. **Deterministic execution** - Ideal for consensus systems

## Contributing

This is an educational project demonstrating blockchain internals. Key principles:

- ✅ No shortcuts - implement everything from scratch
- ✅ No external crypto libraries
- ✅ Readable, well-commented code
- ✅ Extensive test coverage

## License

MIT

## Author

Built following the technical specifications in `technical.md`
