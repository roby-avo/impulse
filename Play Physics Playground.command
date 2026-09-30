#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ -x ai/.venv/bin/python ]]; then
  ai/.venv/bin/python ai/ensure_service.py || true
fi
if [[ ! -d 'Builds/Physics Playground.app' ]]; then
  echo 'Building the game with the installed Unity editor…'
  scripts/unity-check.sh Playground.Editor.ProjectSetup.BuildMac -quit
fi
if [[ ! -x ai/.venv/bin/python ]]; then
  echo 'TypeSafe can play immediately with an API key. For local Laya, run ./ai/setup.sh once and relaunch.'
fi
open 'Builds/Physics Playground.app'
