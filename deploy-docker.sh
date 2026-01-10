#!/bin/bash
# Deploy ForthCoin to DigitalOcean with Docker

SERVER="root@143.110.134.126"
REMOTE_DIR="/root/forthcoin"

echo "=== ForthCoin Docker Deployment ==="
echo ""

# Install Docker on server if needed
echo "[1/5] Ensuring Docker is installed on server..."
ssh $SERVER 'command -v docker >/dev/null 2>&1 || {
    echo "Installing Docker..."
    apt-get update
    apt-get install -y docker.io docker-compose
    systemctl start docker
    systemctl enable docker
}'

# Copy files
echo "[2/5] Copying files to server..."
ssh $SERVER "mkdir -p $REMOTE_DIR"
scp -r src/ Dockerfile docker-compose.yml test_*.fs $SERVER:$REMOTE_DIR/

# Build images
echo "[3/5] Building Docker images..."
ssh $SERVER "cd $REMOTE_DIR && docker-compose build"

# Stop existing containers
echo "[4/5] Stopping existing containers..."
ssh $SERVER "cd $REMOTE_DIR && docker-compose down || true"

# Start nodes
echo "[5/5] Starting ForthCoin nodes..."
ssh $SERVER "cd $REMOTE_DIR && docker-compose up -d"

echo ""
echo "=== Deployment Complete ==="
echo ""
echo "View logs:"
echo "  ssh $SERVER 'docker logs forthcoin-node1'"
echo "  ssh $SERVER 'docker logs forthcoin-node2'"
echo ""
echo "Check status:"
echo "  ssh $SERVER 'docker ps'"
echo ""
echo "Node 1: http://143.110.134.126:8333"
echo "Node 2: http://143.110.134.126:8334"
