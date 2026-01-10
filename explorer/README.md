# ForthCoin Block Explorer

## 🚀 Quick Start

Open `index.html` in your web browser to access the block explorer interface.

### Prerequisites
- ForthCoin node running with WebSocket server enabled
- WebSocket server listening on port 8765

### Starting the WebSocket Server

```bash
# In your ForthCoin terminal
gforth src/load.fs
start-ws-server
```

### Opening the Explorer

```bash
# Open in your default browser
xdg-open explorer/index.html

# Or manually open in any browser
firefox explorer/index.html
chrome explorer/index.html
```

## ✨ Features

### Real-Time Monitoring
- **Live Block Feed** - New blocks appear automatically
- **Transaction Stream** - Real-time pending transactions
- **Network Statistics** - Height, difficulty, hashrate, peer count
- **Auto-Reconnect** - Automatically reconnects if connection is lost

### Search Functionality
- Search by block height (e.g., `100`)
- Search by block hash (64-character hex)
- Search by transaction ID (64-character hex)

### Interactive Interface
- Click on any block to view detailed information
- Click on any transaction to see full details
- Responsive design works on mobile, tablet, and desktop
- Beautiful gradient theme with smooth animations

### Network Statistics Dashboard
- **Block Height** - Current blockchain height
- **Difficulty** - Current mining difficulty
- **Network Hashrate** - Estimated network hash power
- **Connected Peers** - Number of active peer connections

## 🔌 WebSocket API

The explorer connects to `ws://localhost:8765` and uses JSON-RPC 2.0 protocol.

### Supported Methods

```javascript
// Get block by height
{
  "jsonrpc": "2.0",
  "method": "getblock",
  "params": {"height": 100},
  "id": 1
}

// Get transaction
{
  "jsonrpc": "2.0",
  "method": "gettransaction",
  "params": {"txid": "abc123..."},
  "id": 2
}

// Get address info
{
  "jsonrpc": "2.0",
  "method": "getaddress",
  "params": {"address": "1A2B3C..."},
  "id": 3
}

// Subscribe to updates
{
  "jsonrpc": "2.0",
  "method": "subscribe",
  "params": {},
  "id": 4
}

// Get network statistics
{
  "jsonrpc": "2.0",
  "method": "getstats",
  "params": {},
  "id": 5
}
```

### Push Notifications

When subscribed, the server sends real-time notifications:

```javascript
// New block
{
  "type": "block",
  "height": 101
}

// New transaction
{
  "type": "transaction",
  "txid": "abc123..."
}
```

## 📊 Technical Details

### Architecture
- **Pure HTML/CSS/JavaScript** - No build process required
- **WebSocket Protocol** - RFC 6455 compliant
- **JSON-RPC 2.0** - Standard Bitcoin-compatible API
- **Responsive Design** - Mobile-first approach

### Browser Compatibility
- Chrome/Chromium 60+
- Firefox 55+
- Safari 12+
- Edge 79+

### Performance
- Automatic reconnection with exponential backoff
- Efficient DOM updates (minimal reflows)
- Lazy loading for large datasets
- Optimized CSS animations

## 🎨 Customization

### Color Scheme
Edit the CSS gradient in `<style>` section:

```css
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
```

### Update Frequency
Change the stats refresh rate in JavaScript:

```javascript
setInterval(fetchStats, 5000); // Update every 5 seconds
```

### Max Items Displayed
Modify the number of blocks/transactions shown:

```javascript
// In fetchRecentBlocks() and fetchRecentTransactions()
const maxItems = 10; // Show last 10 items
```

## 🔧 Troubleshooting

### "Disconnected" Status
1. Ensure ForthCoin node is running
2. Verify WebSocket server started: `start-ws-server`
3. Check console for connection errors (F12)
4. Confirm port 8765 is not blocked by firewall

### No Data Appearing
1. Check browser console for errors (F12)
2. Verify WebSocket connection in Network tab
3. Ensure blockchain has blocks (run `mine` command)
4. Try manual search by block height

### Search Not Working
1. Ensure you're searching valid data:
   - Block height: numeric (e.g., `0`, `100`)
   - Hash: 64 hex characters
2. Check that blocks/transactions exist
3. View console for API responses

## 📝 Future Enhancements

- [ ] Historical charts (hashrate, difficulty, transaction volume)
- [ ] Address tracking and balance history
- [ ] Mempool visualizer
- [ ] Mining pool statistics
- [ ] Rich list (top addresses by balance)
- [ ] Transaction graph visualization
- [ ] Export data to CSV/JSON
- [ ] Dark mode toggle
- [ ] Multi-language support

## 🌐 Deployment

### Local Network Access
To access from other devices on your network:

```bash
# Find your local IP
ip addr show

# Access from other devices
http://192.168.1.X/path/to/explorer/index.html
```

### Production Deployment
For public access, deploy to a web server:

```bash
# Using simple HTTP server
cd explorer
python3 -m http.server 8080

# Or using nginx
sudo cp -r explorer /var/www/html/forthcoin-explorer
```

Update WebSocket URL in `index.html` to point to your public node.

## 📄 License

Part of ForthCoin project - MIT License

## 🤝 Contributing

Contributions welcome! The explorer is designed to be simple and hackable:
- Single HTML file (no build process)
- Pure vanilla JavaScript (no frameworks)
- Minimal dependencies (just WebSocket API)

Feel free to fork and customize for your needs!
