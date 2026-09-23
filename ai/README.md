# Local Laya service

Run `./ai/setup.sh` once to create the Python environment and cache weights.
Run `./ai/start.sh`, then `ai/.venv/bin/python ai/smoke_test.py`.
The service binds only `127.0.0.1:8000`. It uses `laya==0.3.7` and its actual
`laya.serve.build_router/create_app` API, preloads only `english`, and Unity
pins `model: english` on every request. That selects `convaiinnovations/laya`
(the repository root). No routing to other checkpoints occurs in game.

`HF_HUB_OFFLINE=1` is the runtime default. Setup may download model weights;
game inference is always local. `USE_TF=0` avoids the documented TF import
issue. CPU inference uses four threads by default. `LAYA_DEVICE` and
`LAYA_THREADS` can override the device and thread count.

The loaded agent is retained. Its supported `cfg` uses 1024 total tokens and
384 question tokens so all ten action descriptions survive tokenization.
The official protocol is `POST /v1/systemone` with `state`, `questions`,
and `model`. Readiness is `GET /health` with `english` in `loaded`.

The smoke test uses the same question file as Unity, calls the real model,
validates membership in the action space, and writes measured latency and
raw probabilities to `validation/laya-smoke.json`. Confidence is model output,
not a claim of calibration for this game. Low confidence does not trigger
another policy. Failures cause neutral wait and retry in Unity.

Inspected source: the installed `laya/agent.py`, `router.py`, and `serve.py`.
Upstream documentation: https://huggingface.co/convaiinnovations/laya
The full tested macOS/Python 3.13 dependency set is `requirements-lock.txt`.
