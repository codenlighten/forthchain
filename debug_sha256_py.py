#!/usr/bin/env python3
"""
Debug the message schedule and initial round values for "abc"
"""

import hashlib
import struct

def rotr32(n, d):
    """Rotate right 32-bit value"""
    return ((n >> d) | (n << (32 - d))) & 0xFFFFFFFF

def shr32(n, d):
    """Shift right 32-bit value"""
    return (n >> d) & 0xFFFFFFFF

# SHA-256 constants
K = [
    0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5,
    0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
    0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3,
    0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
    0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
    0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
    0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7,
    0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
    0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13,
    0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
    0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3,
    0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
    0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5,
    0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
    0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
    0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
]

# Initial hash values
H0 = 0x6a09e667
H1 = 0xbb67ae85
H2 = 0x3c6ef372
H3 = 0xa54ff53a
H4 = 0x510e527f
H5 = 0x9b05688c
H6 = 0x1f83d9ab
H7 = 0x5be0cd19

def _lsig0(x):
    """Lower sigma 0"""
    return rotr32(x, 7) ^ rotr32(x, 18) ^ shr32(x, 3)

def _lsig1(x):
    """Lower sigma 1"""
    return rotr32(x, 17) ^ rotr32(x, 19) ^ shr32(x, 10)

# Prepare message for "abc"
msg = b"abc"
msg_bits = len(msg) * 8

# Padding
padded = bytearray(msg)
padded.append(0x80)  # Add the 1 bit
while (len(padded) % 64) != 56:
    padded.append(0x00)
padded += struct.pack('>Q', msg_bits)  # 64-bit message length

print("=== SHA-256 Debug for 'abc' ===\n")

print(f"Message: {msg!r}")
print(f"Padded length: {len(padded)} bytes ({len(padded)//64} blocks)")
print(f"Padded (hex): {padded.hex()}\n")

# Extract W[0..15]
W = []
for i in range(16):
    word = struct.unpack('>I', padded[i*4:(i+1)*4])[0]
    W.append(word)

print("Initial W[0..15] (from message):")
for i in range(16):
    print(f"  W[{i:2}] = 0x{W[i]:08x}")

print("\nMessage Schedule Expansion W[16..63]:")
for i in range(16, 64):
    term1 = _lsig1(W[i-2])
    term2 = W[i-7]
    term3 = _lsig0(W[i-15])
    term4 = W[i-16]
    w = (term1 + term2 + term3 + term4) & 0xFFFFFFFF
    W.append(w)
    if i < 20 or i >= 60:  # Show first and last few
        print(f"  W[{i:2}] = LSIG1(W[{i-2:2}]) + W[{i-7:2}] + LSIG0(W[{i-15:2}]) + W[{i-16:2}]")
        print(f"         = 0x{term1:08x} + 0x{term2:08x} + 0x{term3:08x} + 0x{term4:08x}")
        print(f"         = 0x{w:08x}")

print("\nInitial hash values:")
print(f"  a = 0x{H0:08x}")
print(f"  b = 0x{H1:08x}")
print(f"  c = 0x{H2:08x}")
print(f"  d = 0x{H3:08x}")
print(f"  e = 0x{H4:08x}")
print(f"  f = 0x{H5:08x}")
print(f"  g = 0x{H6:08x}")
print(f"  h = 0x{H7:08x}")

# Test with standard library
h = hashlib.sha256(msg)
result = h.hexdigest().upper()
print(f"\nFinal SHA-256 hash: {result}")
print(f"As 32-bit words:")
for i in range(0, len(result), 8):
    print(f"  0x{result[i:i+8]}")
