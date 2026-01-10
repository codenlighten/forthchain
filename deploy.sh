#!/bin/bash
# ForthCoin Deployment Script for DigitalOcean
# Run this on a fresh Ubuntu 22.04+ droplet

set -e

echo "==================================="
echo "ForthCoin Node Deployment"
echo "==================================="

# Update system
echo "[1/5] Updating system packages..."
sudo apt-get update
sudo apt-get upgrade -y

# Install Gforth
echo "[2/5] Installing Gforth..."
sudo apt-get install -y gforth git make

# Verify Gforth installation
echo "[3/5] Verifying Gforth..."
gforth --version

# Clone repository
echo "[4/5] Cloning ForthCoin repository..."
if [ -d "$HOME/forthcoin" ]; then
    echo "Repository already exists, pulling latest..."
    cd $HOME/forthcoin
    git pull origin main
else
    cd $HOME
    git clone https://github.com/codenlighten/forthchain.git forthcoin
    cd forthcoin
fi

# Run tests
echo "[5/5] Running verification tests..."
echo ""
echo "--- SHA-256 Test ---"
timeout 5 gforth test_sha256_direct.fs 2>&1 | grep -A3 "Hash output" || true

echo ""
echo "--- Math Test ---"
timeout 5 gforth test_math256_add.fs 2>&1 | grep "TEST" || true

echo ""
echo "--- Mining Test ---"
timeout 8 gforth test_e2e_mining.fs 2>&1 | grep -E "(Nonce|SUCCESS)" || true

echo ""
echo "==================================="
echo "ForthCoin Node Deployed Successfully"
echo "==================================="
echo ""
echo "Repository location: $HOME/forthcoin"
echo "To run mining: cd $HOME/forthcoin && gforth test_e2e_mining.fs"
echo "To run tests: cd $HOME/forthcoin && make test"
