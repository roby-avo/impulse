#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ ! -x ai/.venv/bin/python ]]; then ./ai/setup.sh; fi
ready() {
  ai/.venv/bin/python - <<'PY' >/dev/null 2>&1
import json,urllib.request
with urllib.request.urlopen('http://127.0.0.1:8000/health',timeout=2) as r:
    h=json.load(r)
assert h.get('status')=='ok' and 'english' in h.get('loaded',[])
PY
}
if ! ready; then
  mkdir -p ai/.runtime
  laya_pid=$(ai/.venv/bin/python - <<'PYTHON'
import subprocess
from pathlib import Path
root=Path.cwd()
with (root/'ai/.runtime/service.log').open('a') as log:
    process=subprocess.Popen([str(root/'ai/start.sh')],stdin=subprocess.DEVNULL,
                             stdout=log,stderr=subprocess.STDOUT,start_new_session=True)
print(process.pid)
PYTHON
)
  echo "$laya_pid" > ai/.runtime/service.pid
  echo 'Loading local Laya…'
  for ((attempt=0; attempt<120; attempt++)); do
    if ready; then break; fi
    if ! kill -0 "$laya_pid" 2>/dev/null; then cat ai/.runtime/service.log; exit 1; fi
    sleep 1
  done
  if ! ready; then echo 'Laya is not ready. See ai/.runtime/service.log'; exit 1; fi
fi
if [[ ! -d 'Builds/Physics Playground.app' ]]; then
  echo 'Building the game with the installed Unity editor…'
  scripts/unity-check.sh Playground.Editor.ProjectSetup.BuildMac -quit
fi
open 'Builds/Physics Playground.app'
