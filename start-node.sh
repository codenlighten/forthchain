#!/bin/bash

# Start the Forthcoin node
cd /opt/forthcoin/src

echo "Starting Forthcoin blockchain node..."

# Start the blockchain, initialize, and keep gforth running in interactive mode
exec gforth load.fs -e "init-storage init-network start-listening start-ws-server CR .\" [FORTHCOIN] Node initialized and running.\" CR CR"

