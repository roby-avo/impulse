#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ -f ai/.runtime/service.pid ]]; then
  laya_pid=$(cat ai/.runtime/service.pid)
  if ps -p "$laya_pid" -o command= | grep -F -- "$PWD/ai/server.py" >/dev/null; then
    kill "$laya_pid"
    echo 'Local Laya stopped.'
  else echo 'The launcher-managed Laya process is no longer running.'; fi
else echo 'No launcher-managed service. Stop a manually started service in its terminal.'; fi
