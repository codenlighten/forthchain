#!/bin/bash
# ForthCoin Complete Deployment Script
# Deploys blockchain + WebSocket API + Block Explorer

set -e

echo "🚀 ForthCoin Complete Deployment Starting..."
echo ""

# Update system
echo "📦 Updating system packages..."
apt-get update
apt-get upgrade -y

# Install dependencies
echo "📦 Installing dependencies..."
apt-get install -y gforth git nginx

# Clone/update repository
if [ -d "/opt/forthcoin" ]; then
    echo "🔄 Updating existing installation..."
    cd /opt/forthcoin
    git pull origin main
else
    echo "📥 Cloning ForthCoin repository..."
    git clone https://github.com/codenlighten/forthchain.git /opt/forthcoin
    cd /opt/forthcoin
fi

# Create systemd service for blockchain node
echo "⚙️  Creating blockchain service..."
cat > /etc/systemd/system/forthcoin.service <<'EOF'
[Unit]
Description=ForthCoin Blockchain Node
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/opt/forthcoin
ExecStart=/usr/bin/gforth /opt/forthcoin/src/load.fs -e "init-storage init-network start-listening start-ws-server"
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF

# Configure nginx for block explorer
echo "🌐 Configuring nginx for block explorer..."
cat > /etc/nginx/sites-available/forthcoin-explorer <<'EOF'
server {
    listen 80;
    server_name _;

    root /opt/forthcoin/explorer;
    index index.html;

    location / {
        try_files $uri $uri/ =404;
    }

    # WebSocket proxy
    location /ws {
        proxy_pass http://localhost:8765;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # API endpoint (optional)
    location /api {
        proxy_pass http://localhost:8765;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
EOF

# Enable nginx site
ln -sf /etc/nginx/sites-available/forthcoin-explorer /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

# Test nginx configuration
nginx -t

# Reload nginx
systemctl reload nginx

# Enable and start ForthCoin service
echo "🚀 Starting ForthCoin services..."
systemctl daemon-reload
systemctl enable forthcoin
systemctl restart forthcoin

# Configure firewall
echo "🔒 Configuring firewall..."
ufw allow 22/tcp      # SSH
ufw allow 80/tcp      # HTTP (Explorer)
ufw allow 8333/tcp    # P2P Blockchain
ufw allow 8765/tcp    # WebSocket API
echo "y" | ufw enable || true

# Wait for services to start
echo "⏳ Waiting for services to start..."
sleep 5

# Check status
echo ""
echo "✅ Deployment Complete!"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 Service Status:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
systemctl status forthcoin --no-pager | head -10
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🌐 Access Points:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
PUBLIC_IP=$(curl -s ifconfig.me)
echo "Block Explorer:  http://$PUBLIC_IP"
echo "WebSocket API:   ws://$PUBLIC_IP:8765"
echo "P2P Node:        $PUBLIC_IP:8333"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📝 Useful Commands:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "View logs:       journalctl -u forthcoin -f"
echo "Restart node:    systemctl restart forthcoin"
echo "Stop node:       systemctl stop forthcoin"
echo "Check status:    systemctl status forthcoin"
echo ""
echo "🎉 ForthCoin is now live!"
