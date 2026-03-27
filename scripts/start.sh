#!/bin/bash
# Start TinCan trading bot
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_DIR"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet/libexec"
export PATH="$DOTNET_ROOT:$PATH"

# Kill any existing instance
pkill -f "TinCan.dll" 2>/dev/null
sleep 1

# Start the app in background
nohup dotnet run > /tmp/tincan.log 2>&1 &
echo "TinCan started at $(date)"
