FROM ubuntu:22.04

# Install dependencies
RUN apt-get update && apt-get install -y \
    gforth \
    bash \
    && rm -rf /var/lib/apt/lists/*

# Create app directory
WORKDIR /opt/forthcoin

# Copy source code
COPY src/ ./src/
COPY explorer/ ./explorer/
COPY tests/ ./tests/
COPY start-node.sh ./

EXPOSE 8333 8765

# Start the blockchain node
CMD ["bash", "/opt/forthcoin/start-node.sh"]
