#!/bin/bash
PIDS=$(pgrep -f "TinCan")
if [ -n "$PIDS" ]; then
    echo "$PIDS" | xargs kill 2>/dev/null
    echo "TinCan stopped"
else
    echo "TinCan was not running"
fi
