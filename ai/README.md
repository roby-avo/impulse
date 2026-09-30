# Local Laya service

Run `./ai/setup.sh` once to create the Python environment and cache weights.
The game automatically starts the installed runtime when its local connection
fails, and reconnects after a stopped service. This works when opening the Mac
app directly from `Builds/`, as well as through the launcher. Startup is on demand;
it does not install a login daemon or download weights. Missing setup and port
conflicts appear in the model diagnostics.

Both game and launcher use `ensure_service.py`: a cross-process startup lock,
verified PID, detached process, and 15-second crash retry cooldown prevent
duplicate launches and rapid restart loops. Service output goes to
`ai/.runtime/service.log`. Keep the built app in this checkout's `Builds/` folder
so it can locate the Python environment. Stop the game before using the stop
command, otherwise active local play restarts the service.

For manual diagnostics, run `./ai/start.sh`, then
`ai/.venv/bin/python ai/smoke_test.py`.
The service binds only `127.0.0.1:8000`, preloads the `english` checkpoint from
`convaiinnovations/laya`, and pins every prediction to it. No other model or
fallback decision policy chooses actions.

## Runtime and acceleration

Version 0.9.1 defaults to PyTorch **MPS (Apple GPU)** when available, otherwise
CPU. `LAYA_DEVICE=cpu ./ai/start.sh` explicitly selects CPU. `LAYA_THREADS` sets
CPU intra-op threads (default 4); inter-op threads are capped at 1. The exact
same FP32 checkpoint, observations and question descriptions are used on both
devices—no quantization, action caching or state truncation was added.

`HF_HUB_OFFLINE=1` is the runtime default; `USE_TF=0` avoids the SDK's documented
TensorFlow import issue. The retained agent uses 1024 total tokens and 384
question tokens, preserving the existing ten-action setup.

The service warms inference on its dedicated worker before reporting ready.
One inference may run at a time. Overlapping requests receive HTTP 429 and
Retry-After instead of joining a stale-state queue. Health checks run separately
from inference and remain responsive. A disconnected client does not release
the worker until its computation finishes. Accelerator recovery, if required,
uses the same model on CPU; `/health` reports its actual current device.

## Protocol and diagnostics

The pinned `laya==0.3.7` SDK's `Router.predict` / `Agent.system_one` still performs
all tokenization, inference and answer construction. A small FastAPI wrapper
retains the official `POST /v1/systemone` wire format (`model`, `state`,
`questions`) while moving inference off the HTTP event loop. Only `english` is
accepted by this game service. Optional `LAYA_API_KEY` authentication is preserved.

`GET /health` reports loaded checkpoint, actual device, runtime version, busy
state, thread count, last inference time and completed requests. Responses also
include `Server-Timing`, `X-Laya-Device` and `X-Laya-Inference-Ms`. Unity records
the device and server inference time alongside its full request latency.

The HUD shows request age versus timeout, last latency, executed decisions and
outdated replies for each model. Cancellation clears the deciding status.
Stale telemetry distinguishes match/control changes, knockback, sudden edge
proximity and incoming projectiles. Safety checks still discard stale decisions.

## Validation

See [the latency investigation](../validation/LAYA_LATENCY.md) for measurements.
The smoke test calls the real model and validates legal action membership.
Additional tools:

```sh
ai/.venv/bin/python ai/benchmark_runtime.py
ai/.venv/bin/python ai/check_runtime.py
ai/.venv/bin/python ai/check_device_parity.py /path/to/recording.jsonl
```

The benchmark uses `validation/laya-benchmark-request.json`, an exact semantic
request from the reported slow match. Run benchmarks without other inference
clients for comparable timing. Device parity compares up to 20 recorded Laya
requests. Confidence is model output, not game-specific calibration.

Inspected source: installed `laya/agent.py`, `router.py`, `serve.py`, `common.py`.
Model documentation: https://huggingface.co/convaiinnovations/laya
MPS documentation: https://docs.pytorch.org/docs/stable/notes/mps.html
The validated dependency set remains `requirements-lock.txt`.
