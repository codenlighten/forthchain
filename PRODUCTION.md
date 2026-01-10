# ForthCoin Production Operations Guide

## 🎯 Production Readiness Checklist

### Before Going Live

- [ ] **Security Audit** - Review all cryptographic implementations
- [ ] **Load Testing** - Test with 1000+ transactions
- [ ] **Multi-Node Testing** - Run 5+ node network for 24 hours
- [ ] **Backup Strategy** - Automated daily backups configured
- [ ] **Monitoring Setup** - Prometheus/Grafana dashboards active
- [ ] **Documentation Review** - All APIs and procedures documented
- [ ] **Disaster Recovery Plan** - Recovery procedures tested
- [ ] **Capacity Planning** - Storage and bandwidth projections completed

## 🏗️ Architecture Patterns

### Pattern 1: High Availability Cluster

```
┌─────────────────────────────────────┐
│         Load Balancer (HAProxy)     │
│         Primary: 192.168.1.10       │
│         Backup:  192.168.1.11       │
└──────────┬─────────────┬────────────┘
           │             │
    ┌──────▼─────┐ ┌────▼──────┐
    │  API Node1 │ │ API Node2 │
    │  WS: 8765  │ │ WS: 8765  │
    └──────┬─────┘ └────┬──────┘
           │             │
    ┌──────▼─────────────▼──────┐
    │    Bootstrap Node          │
    │    Full Blockchain         │
    │    P2P: 8333              │
    └──────┬─────────────────────┘
           │
    ┌──────┴──────┬──────┬──────┐
    │             │      │      │
┌───▼───┐  ┌─────▼──┐ ┌─▼────┐ │
│Mining1│  │Mining2 │ │Minin3│ │
└───────┘  └────────┘ └──────┘ │
```

**Specs:**
- Load Balancer: 2x VMs (active/passive)
- API Nodes: 2x 4GB RAM, 2 vCPU
- Bootstrap: 1x 8GB RAM, 4 vCPU, 100GB SSD
- Mining Nodes: 3x 4GB RAM, 4 vCPU

**Total Cost:** ~$100/month on DigitalOcean

### Pattern 2: Government Treasury Deployment

```
┌────────────────────────────────────┐
│   Public Explorer (DMZ)            │
│   Read-Only WebSocket API          │
└────────────┬───────────────────────┘
             │ (Firewall)
┌────────────▼───────────────────────┐
│   Internal Blockchain Network      │
│                                    │
│  ┌──────────┐  ┌──────────┐       │
│  │Treasury  │  │Audit     │       │
│  │Multi-Sig │  │Node      │       │
│  │3-of-5    │  │(Read-Only│       │
│  └────┬─────┘  └──────────┘       │
│       │                            │
│  ┌────▼──────────┐                │
│  │ Consensus Node│                │
│  │ (No Mining)   │                │
│  └───────────────┘                │
└────────────────────────────────────┘
```

**Security Features:**
- Air-gapped signing nodes
- Hardware security modules (HSM) for keys
- Immutable audit log
- 3-of-5 multi-signature for all transactions
- Geographic distribution of signers

### Pattern 3: Development/Testing Network

```
┌────────────┐  ┌────────────┐  ┌────────────┐
│   Node 1   │  │   Node 2   │  │   Node 3   │
│   Docker   │  │   Docker   │  │   Docker   │
│   +Mining  │  │   +Mining  │  │   +WebSock │
└─────┬──────┘  └─────┬──────┘  └─────┬──────┘
      │               │               │
      └───────────────┴───────────────┘
              Docker Network
            (forthcoin-testnet)
```

**Docker Compose:**
```yaml
version: '3.8'
services:
  node1:
    image: forthcoin:latest
    container_name: forthcoin-node1
    ports:
      - "8333:8333"
    networks:
      - forthcoin
    volumes:
      - node1-data:/var/lib/forthcoin
    command: ["testnet"]
    
  node2:
    image: forthcoin:latest
    container_name: forthcoin-node2
    networks:
      - forthcoin
    volumes:
      - node2-data:/var/lib/forthcoin
    command: ["testnet", "--peer=node1:8333"]
    
  node3:
    image: forthcoin:latest
    container_name: forthcoin-node3
    ports:
      - "8765:8765"
    networks:
      - forthcoin
    volumes:
      - node3-data:/var/lib/forthcoin
    command: ["testnet", "--peer=node1:8333", "--websocket"]
    
  explorer:
    image: nginx:alpine
    container_name: forthcoin-explorer
    ports:
      - "8080:80"
    networks:
      - forthcoin
    volumes:
      - ./explorer:/usr/share/nginx/html:ro

networks:
  forthcoin:
    driver: bridge

volumes:
  node1-data:
  node2-data:
  node3-data:
```

## 🔐 Security Best Practices

### 1. Key Management

**Never store private keys in plain text!**

```bash
# Generate secure wallet with encrypted backup
gforth src/load.fs
forth> new
forth> save-encrypted wallet.enc
Enter passphrase: ********
Confirm passphrase: ********

# Backup to secure location
gpg --encrypt --recipient admin@example.com wallet.enc
mv wallet.enc.gpg /secure/backup/
shred -u wallet.enc  # Securely delete original
```

### 2. Network Isolation

```bash
# Separate networks for different functions
iptables -N FORTHCOIN_P2P
iptables -N FORTHCOIN_API
iptables -N FORTHCOIN_INTERNAL

# P2P only from known peers
iptables -A FORTHCOIN_P2P -p tcp --dport 8333 \
    -s 192.168.1.0/24 -j ACCEPT
iptables -A FORTHCOIN_P2P -j DROP

# API from application servers only
iptables -A FORTHCOIN_API -p tcp --dport 8765 \
    -s 10.0.0.0/8 -j ACCEPT
iptables -A FORTHCOIN_API -j DROP
```

### 3. Audit Logging

```bash
# Enable comprehensive audit trail
auditctl -w /opt/forthcoin/src -p wa -k forthcoin_code
auditctl -w /var/lib/forthcoin -p wa -k forthcoin_data
auditctl -w /etc/systemd/system/forthcoin.service -p wa -k forthcoin_service

# View audit logs
ausearch -k forthcoin_data -ts today
```

### 4. TLS/SSL for WebSocket

```nginx
# nginx reverse proxy with SSL
upstream forthcoin_ws {
    server 127.0.0.1:8765;
}

server {
    listen 443 ssl http2;
    server_name api.forthcoin.example.com;
    
    ssl_certificate /etc/letsencrypt/live/api.forthcoin.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.forthcoin.example.com/privkey.pem;
    
    location / {
        proxy_pass http://forthcoin_ws;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## 📊 Performance Benchmarks

### Expected Performance Metrics

| Operation | Time | Notes |
|-----------|------|-------|
| SHA-256 hash | < 1ms | NIST-verified implementation |
| ECDSA sign | < 5ms | secp256k1 curve |
| ECDSA verify | < 10ms | Public key recovery |
| Transaction validation | < 2ms | Single-sig input |
| Block validation | < 100ms | 100 transactions |
| Mining (difficulty 1000) | ~10s | 1 GH/s expected |
| WebSocket message | < 5ms | JSON-RPC roundtrip |
| Blockchain sync | ~1 block/s | Network dependent |

### Load Testing

```bash
# Test transaction throughput
./tests/load_test.sh --txs=1000 --duration=60

# Test mining performance
./tests/mine_benchmark.sh --blocks=100

# Test WebSocket API
./tests/ws_stress.sh --clients=100 --duration=300

# Test network sync
./tests/sync_test.sh --nodes=10 --blocks=1000
```

## 🚀 Scaling Strategies

### Vertical Scaling

**When:** Single node reaching limits
**How:** Increase resources

```bash
# Current: 2GB RAM, 2 vCPU
# Upgrade to: 8GB RAM, 4 vCPU

# Resize droplet (DigitalOcean)
doctl compute droplet-action resize $DROPLET_ID --size s-4vcpu-8gb

# Or AWS
aws ec2 modify-instance-attribute \
    --instance-id $INSTANCE_ID \
    --instance-type t3.xlarge
```

### Horizontal Scaling

**When:** Need higher availability or throughput
**How:** Add more nodes

```bash
# Deploy additional nodes
for i in {4..10}; do
    doctl compute droplet create forthcoin-node$i \
        --image ubuntu-20-04-x64 \
        --size s-2vcpu-4gb \
        --region nyc1 \
        --user-data-file deploy.sh
done

# Connect to existing network
gforth src/load.fs -e "init-network connect 192.168.1.1 8333 bye"
```

### Read Replicas

**When:** Heavy query load on API
**How:** Deploy read-only nodes

```bash
# Read-only node (no mining)
gforth src/load.fs
forth> init-storage
forth> init-network
forth> connect-readonly 192.168.1.1 8333
forth> start-ws-server
```

## 📈 Monitoring Dashboards

### Grafana Panel Examples

#### Blockchain Health

```sql
-- Panel: Block Height Over Time
SELECT 
  time_bucket('1 hour', timestamp) AS hour,
  max(height) as max_height
FROM blockchain_metrics
WHERE timestamp > NOW() - INTERVAL '24 hours'
GROUP BY hour
ORDER BY hour
```

#### Network Statistics

```sql
-- Panel: Peer Distribution
SELECT 
  peer_ip,
  COUNT(*) as connection_count,
  AVG(latency_ms) as avg_latency
FROM peer_metrics
WHERE timestamp > NOW() - INTERVAL '1 hour'
GROUP BY peer_ip
ORDER BY connection_count DESC
```

#### Mining Performance

```sql
-- Panel: Hashrate History
SELECT 
  time_bucket('5 minutes', timestamp) AS interval,
  AVG(hashrate) as avg_hashrate,
  MAX(hashrate) as max_hashrate
FROM mining_metrics
WHERE timestamp > NOW() - INTERVAL '6 hours'
GROUP BY interval
ORDER BY interval
```

### Alert Rules

```yaml
# alerts.yml
groups:
  - name: forthcoin
    interval: 30s
    rules:
      - alert: NodeDown
        expr: up{job="forthcoin"} == 0
        for: 2m
        annotations:
          summary: "ForthCoin node is down"
          
      - alert: HighMemoryUsage
        expr: node_memory_usage > 90
        for: 5m
        annotations:
          summary: "Node memory usage above 90%"
          
      - alert: ChainStalled
        expr: increase(forthcoin_height[10m]) == 0
        for: 10m
        annotations:
          summary: "Blockchain height not increasing"
          
      - alert: PeerCountLow
        expr: forthcoin_peers < 3
        for: 5m
        annotations:
          summary: "Less than 3 peers connected"
```

## 🔄 Upgrade Procedures

### Rolling Update (Zero Downtime)

```bash
#!/bin/bash
# rolling_update.sh

NODES=("node1" "node2" "node3" "node4")

for node in "${NODES[@]}"; do
    echo "Updating $node..."
    
    # Stop node
    ssh $node "systemctl stop forthcoin"
    
    # Backup
    ssh $node "tar -czf /backup/pre-upgrade-$(date +%Y%m%d).tar.gz /var/lib/forthcoin"
    
    # Pull latest code
    ssh $node "cd /opt/forthcoin && git pull origin main"
    
    # Run migrations if needed
    ssh $node "gforth /opt/forthcoin/migrations/migrate.fs"
    
    # Start node
    ssh $node "systemctl start forthcoin"
    
    # Wait for sync
    sleep 30
    
    # Verify health
    ssh $node "gforth /opt/forthcoin/src/load.fs -e 'init-storage get-height . bye'"
    
    echo "$node updated successfully"
done
```

### Emergency Rollback

```bash
#!/bin/bash
# rollback.sh

# Stop all nodes
for node in node1 node2 node3 node4; do
    ssh $node "systemctl stop forthcoin"
done

# Restore from backup
for node in node1 node2 node3 node4; do
    ssh $node "cd /opt/forthcoin && git checkout $PREVIOUS_VERSION"
    ssh $node "tar -xzf /backup/pre-upgrade-$(date +%Y%m%d).tar.gz -C /"
done

# Restart nodes
for node in node1 node2 node3 node4; do
    ssh $node "systemctl start forthcoin"
done

# Verify
for node in node1 node2 node3 node4; do
    ssh $node "gforth /opt/forthcoin/src/load.fs -e 'init-storage get-height . bye'"
done
```

## 📞 On-Call Runbook

### Issue: High CPU Usage

**Symptoms:** CPU > 90% for extended period

**Investigation:**
```bash
# Check what's consuming CPU
top -b -n 1 | head -20

# Check Gforth processes
ps aux | grep gforth

# Check for mining loops
gforth src/load.fs -e "mining-status bye"
```

**Resolution:**
1. If mining: Normal behavior during block production
2. If stuck in loop: Restart service
3. If persistent: Review recent code changes

### Issue: Memory Leak

**Symptoms:** Memory usage growing unbounded

**Investigation:**
```bash
# Check memory usage trend
sar -r 1 10

# Check Gforth dictionary size
gforth src/load.fs -e "unused . bye"

# Check for orphaned data structures
gforth tests/memory_leak_test.fs
```

**Resolution:**
1. Restart service (clears memory)
2. Review recent allocations
3. Run memory profiler

### Issue: Network Partition

**Symptoms:** Peer count drops to 0

**Investigation:**
```bash
# Check network connectivity
ping 8.8.8.8
traceroute bootstrap.forthcoin.net

# Check firewall rules
iptables -L -n | grep 8333

# Check listening ports
netstat -tuln | grep 8333
```

**Resolution:**
1. Verify bootstrap nodes are reachable
2. Check firewall allows port 8333
3. Manually reconnect to known peers
4. Review network logs

## 📚 Additional Resources

### Documentation
- [Architecture Overview](./ARCHITECTURE.md)
- [API Reference](./API.md)
- [Security Audit Report](./SECURITY.md)

### Tools
- [Monitoring Scripts](./monitoring/)
- [Deployment Automation](./deploy/)
- [Load Testing Suite](./tests/load/)

### Community
- GitHub Issues
- Discord Server
- Weekly Dev Calls

---

**Last Updated:** 2026-01-10  
**Version:** 1.0  
**Maintained By:** ForthCoin Core Team
