#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
UNITY="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity}"
method="${1:-Playground.Editor.ProjectSetup.Prepare}"
mkdir -p validation
exec "$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod "$method" -logFile "$PWD/validation/unity.log" "${@:2}"
