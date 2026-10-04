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
The service binds only `127.0.0.1:8000` and preloads the default `english`
checkpoint. Each request names its model; the service answers with exactly that
checkpoint or returns an error. No other model or fallback decision policy ever
chooses actions.

## Models

Local System One checkpoints are managed from one catalog. `models.json` lists the
shipped Laya checkpoints, pinned to a repository revision; your own entries go in
`models.local.json` (untracked). Installed checkpoints live in `ai/models/<id>/`
(untracked). The game's setup screen lists the same catalog, shows install and
loading state, and can download a missing model with one click.

```sh
ai/.venv/bin/python ai/models.py list
ai/.venv/bin/python ai/models.py download multilingual
ai/.venv/bin/python ai/models.py verify multilingual        # real game question, legal answer?
ai/.venv/bin/python ai/models.py remove multilingual
```

| Model | Size | Notes |
|---|---|---|
| `english` | 804 MB | Default; the configuration validated for IMPULSE |
| `multilingual` | 614 MB | mmBERT-base, 100+ languages; experimental here (prompts are English) |
| `typed-decisions` | 804 MB | Fine-tuned on typed-decision workflows; experimental here |

Downloads fetch only the selected checkpoint's files. If the Hugging Face cache
already holds them (for example from an earlier `setup.sh`), installation is
offline and hardlinks the weights, so it costs no extra disk space.

**Adding a new model.** Any checkpoint in the Laya format (`rl_agent_config.json`,
`model.safetensors`, `tokenizer/`, `encoder/`) works:

```sh
ai/.venv/bin/python ai/models.py add my-laya org/repo --revision <commit> --size-mb 800
ai/.venv/bin/python ai/models.py add my-finetune ~/checkpoints/run-7   # a local folder
```

A Hugging Face entry then downloads from the game or with `download`; a folder entry
is used in place and never deleted by `remove`. Pin `--revision` for reproducible
experiments. Entries may set `agent_config` (default `max_len` 1024,
`head_max_len` 384). The `runtime` field is `laya` today; other runtimes would need
an adapter in `server.py`.

Every model is checked before it serves a match: loading runs a warm-up with the
real game question, and a model whose answer the game could not execute is
reported instead of used. Results are kept in `ai/models/.verified/`.

**Memory.** At most two models stay resident (`LAYA_MAX_LOADED`, default 2); loading
a third unloads the least recently used one. Two local AI players share one
inference worker, so in Laya-vs-Laya matches each model's decision rate is lower.

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
One inference may run at a time. An overlapping request waits up to 0.6 s
for the worker (enough for the other AI player's decision), then receives HTTP 429
and Retry-After instead of joining a stale-state queue. A request for a model that
is still loading receives 503 with `X-Laya-Status: loading`; the game retries each
second. All accelerator work (moving weights, warm-up, unloading) runs on the
inference thread, because Metal is not safe for concurrent use. Opening the setup
screen asks the service to load and warm the selected models, and re-warms them
after more than a minute idle: the first Apple GPU inference after a long idle can
take several seconds. Health checks run separately
from inference and remain responsive. A disconnected client does not release
the worker until its computation finishes. Accelerator recovery, if required,
uses the same model on CPU; `/health` reports its actual current device.

## Protocol and diagnostics

The pinned `laya==0.3.7` SDK's `Agent.system_one` still performs all
tokenization, inference and answer construction. A small FastAPI wrapper retains
the official `POST /v1/systemone` wire format (`model`, `state`, `questions`)
while moving inference off the HTTP event loop. Any installed catalog model is
accepted. Optional `LAYA_API_KEY` authentication is preserved.

Endpoints: `GET /v1/models` (catalog with install, load and download state),
`POST /v1/models/{id}/load`, `POST /v1/models/{id}/download` (runs `models.py` in a
separate process; the service itself stays offline), and `POST /v1/systemone`.
Responses report the serving model id in `model`. The launcher replaces an older
service it started when the runtime version changes.

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
