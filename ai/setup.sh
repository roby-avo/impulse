#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
python3 -m venv .venv
.venv/bin/pip install -r requirements-lock.txt
# Installs the default checkpoint into ai/models/ (reusing the Hugging Face cache when present).
# More models: .venv/bin/python models.py list / download <id>, or the game's setup screen.
USE_TF=0 HF_HUB_OFFLINE=0 .venv/bin/python models.py download english
echo 'Model installed; future launches are offline.'
