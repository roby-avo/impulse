#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
ready() {
  ai/.venv/bin/python - <<'PY' >/dev/null 2>&1
import json,urllib.request
with urllib.request.urlopen('http://127.0.0.1:8000/health',timeout=2) as r:
    h=json.load(r)
assert h.get('status')=='ok' and 'english' in h.get('loaded',[])
PY
}
if [[ -x ai/.venv/bin/python ]] && ! ready; then
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
  echo 'Local Laya is loading in the background. Choose your matchup in the game.'
fi
if [[ ! -d 'Builds/Physics Playground.app' ]]; then
  echo 'Building the game with the installed Unity editor…'
  scripts/unity-check.sh Playground.Editor.ProjectSetup.BuildMac -quit
fi
if [[ ! -x ai/.venv/bin/python ]]; then
  echo 'TypeSafe can play immediately with an API key. For local Laya, run ./ai/setup.sh once and relaunch.'
fi
open 'Builds/Physics Playground.app'
