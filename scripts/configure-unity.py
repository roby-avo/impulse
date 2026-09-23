#!/usr/bin/env python3
"""Print the editor required by this project. Unity's API updater runs on first import."""
from pathlib import Path
print((Path(__file__).resolve().parent.parent/'ProjectSettings/ProjectVersion.txt').read_text())
