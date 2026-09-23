#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
python3 -m venv .venv
.venv/bin/pip install -r requirements-lock.txt
USE_TF=0 HF_HUB_OFFLINE=0 .venv/bin/python -c 'import laya; laya.load("convaiinnovations/laya", device="cpu"); print("Model cached; future launches are offline.")'
