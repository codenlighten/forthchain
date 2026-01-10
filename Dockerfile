FROM ubuntu:22.04

# Install dependencies
RUN apt-get update && apt-get install -y \
    gforth \
    && rm -rf /var/lib/apt/lists/*

# Create app directory
WORKDIR /forthcoin

# Copy source code
COPY src/ ./src/
COPY test_e2e_mining.fs ./
COPY test_sha256_direct.fs ./
COPY test_math256_add.fs ./

# Default command: run mining test
CMD ["gforth", "test_e2e_mining.fs"]
