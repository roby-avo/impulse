#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ ! -x .venv/bin/python ]]; then
  echo 'Run ai/setup.sh first.' >&2
  exit 1
fi
exec "$PWD/.venv/bin/python" "$PWD/server.py"
