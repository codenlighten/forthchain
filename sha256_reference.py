#!/usr/bin/env python3
"""
SHA-256 reference implementation for comparing against ForthCoin.
Used for debugging the Forth implementation.
"""

import hashlib
import struct

# Test vectors
test_cases = [
    (b"abc", "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"),
    (b"", "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855"),
    (b"abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", 
     "248D6A61D20638B8E5C026930C3E6039A33CE45964FF2167F6ECEDD419DB06C1"),
]

def sha256_reference(data):
    """Get the official SHA-256 hash."""
    return hashlib.sha256(data).hexdigest().upper()

print("SHA-256 Reference Test Vectors")
print("=" * 60)

for data, expected in test_cases:
    result = sha256_reference(data)
    status = "✓" if result == expected else "✗"
    
    print(f"{status} Input: {data!r}")
    print(f"  Got:      {result}")
    print(f"  Expected: {expected}")
    if result != expected:
        print(f"  MISMATCH!")
    print()

# For the "abc" case, show step-by-step
print("\nDetailed trace for 'abc':")
print("=" * 60)

data = b"abc"
h = hashlib.sha256(data)

print(f"Input: {data!r}")
print(f"Input hex: {data.hex().upper()}")
print(f"\nSHA-256 Processing:")

# This is just the final output
final = h.hexdigest().upper()
print(f"Final hash: {final}")

# Break into 32-bit words
print(f"\nAs 32-bit words:")
for i in range(0, len(final), 8):
    word = final[i:i+8]
    print(f"  Word {i//8}: 0x{word}")
