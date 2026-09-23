#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p Assets/Plugins/macOS
xcrun clang -dynamiclib -fobjc-arc -arch arm64 -arch x86_64 \
  -mmacosx-version-min=12.0 -framework Cocoa \
  -install_name @rpath/libPlaygroundDock.dylib \
  Native/DockIcon.m -o Assets/Plugins/macOS/libPlaygroundDock.dylib
codesign --force --sign - Assets/Plugins/macOS/libPlaygroundDock.dylib
