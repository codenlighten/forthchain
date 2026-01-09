#!/usr/bin/env python3
"""
Compute the exact values expected for round 0 of SHA-256("abc")
"""

def rotr32(n, d):
    """Rotate right 32-bit value"""
    return ((n >> d) | (n << (32 - d))) & 0xFFFFFFFF

def shr32(n, d):
    """Shift right 32-bit value"""
    return (n >> d) & 0xFFFFFFFF

def ch(x, y, z):
    """CH function"""
    return (x & y) ^ (~x & z)

def maj(x, y, z):
    """MAJ function"""
    return (x & y) ^ (x & z) ^ (y & z)

def bsig0(x):
    """Big Sigma 0"""
    return rotr32(x, 2) ^ rotr32(x, 13) ^ rotr32(x, 22)

def bsig1(x):
    """Big Sigma 1"""
    return rotr32(x, 6) ^ rotr32(x, 11) ^ rotr32(x, 25)

def lsig0(x):
    """Little Sigma 0"""
    return rotr32(x, 7) ^ rotr32(x, 18) ^ shr32(x, 3)

def lsig1(x):
    """Little Sigma 1"""
    return rotr32(x, 17) ^ rotr32(x, 19) ^ shr32(x, 10)

K0 = 0x428a2f98
W0 = 0x61626380

# Initial values
a = 0x6a09e667
b = 0xbb67ae85
c = 0x3c6ef372
d = 0xa54ff53a
e = 0x510e527f
f = 0x9b05688c
g = 0x1f83d9ab
h = 0x5be0cd19

print("=== Round 0 Computation ===\n")
print(f"Initial state:")
print(f"  a = 0x{a:08x}")
print(f"  b = 0x{b:08x}")
print(f"  c = 0x{c:08x}")
print(f"  d = 0x{d:08x}")
print(f"  e = 0x{e:08x}")
print(f"  f = 0x{f:08x}")
print(f"  g = 0x{g:08x}")
print(f"  h = 0x{h:08x}\n")

# T1 = h + BSIG1(e) + CH(e,f,g) + K[0] + W[0]
bsig1_e = bsig1(e)
ch_efg = ch(e, f, g)

print(f"BSIG1(e) = 0x{bsig1_e:08x}")
print(f"CH(e,f,g) = 0x{ch_efg:08x}")
print(f"K[0] = 0x{K0:08x}")
print(f"W[0] = 0x{W0:08x}\n")

T1 = (h + bsig1_e + ch_efg + K0 + W0) & 0xFFFFFFFF
print(f"T1 = h + BSIG1(e) + CH(e,f,g) + K[0] + W[0]")
print(f"T1 = 0x{h:08x} + 0x{bsig1_e:08x} + 0x{ch_efg:08x} + 0x{K0:08x} + 0x{W0:08x}")
print(f"T1 = 0x{T1:08x}\n")

# T2 = BSIG0(a) + MAJ(a,b,c)
bsig0_a = bsig0(a)
maj_abc = maj(a, b, c)

print(f"BSIG0(a) = 0x{bsig0_a:08x}")
print(f"MAJ(a,b,c) = 0x{maj_abc:08x}")

T2 = (bsig0_a + maj_abc) & 0xFFFFFFFF
print(f"T2 = BSIG0(a) + MAJ(a,b,c)")
print(f"T2 = 0x{bsig0_a:08x} + 0x{maj_abc:08x}")
print(f"T2 = 0x{T2:08x}\n")

# New values
h_new = g
g_new = f
f_new = e
e_new = (d + T1) & 0xFFFFFFFF
d_new = c
c_new = b
b_new = a
a_new = (T1 + T2) & 0xFFFFFFFF

print("After update:")
print(f"  a' = T1 + T2 = 0x{a_new:08x}")
print(f"  b' = a       = 0x{b_new:08x}")
print(f"  c' = b       = 0x{c_new:08x}")
print(f"  d' = c       = 0x{d_new:08x}")
print(f"  e' = d + T1  = 0x{e_new:08x}")
print(f"  f' = e       = 0x{f_new:08x}")
print(f"  g' = f       = 0x{g_new:08x}")
print(f"  h' = g       = 0x{h_new:08x}")

print("\n" + "="*50)
print("FORTH COMPUTED:")
print("  var[0]=$8CFCAB6B (should be 0x{:08x})".format(a_new))
print("  var[1]=$6A09E667 (should be 0x{:08x})".format(b_new))
print("  var[2]=$BB67AE85 (should be 0x{:08x})".format(c_new))
print("  var[3]=$3C6EF372 (should be 0x{:08x})".format(d_new))
print("  var[4]=$FA2A4622 (should be 0x{:08x})".format(e_new))
print("  var[5]=$510E527F (should be 0x{:08x})".format(f_new))
print("  var[6]=$9B05688C (should be 0x{:08x})".format(g_new))
print("  var[7]=$1F83D9AB (should be 0x{:08x})".format(h_new))
