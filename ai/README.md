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

Local System One models are managed from one catalog. `models.json` lists the shipped
models, each pinned to a repository revision; your own entries (and runtimes) go in
`models.local.json` (untracked). The game's setup screen lists the same catalog, shows
install and loading state, and downloads a missing model with one click.

```sh
ai/.venv/bin/python ai/models.py list                 # models, install state, verification
ai/.venv/bin/python ai/models.py runtimes             # runtime recipes and install state
ai/.venv/bin/python ai/models.py download von         # runtime + weights + verified first start
ai/.venv/bin/python ai/models.py verify von           # real game question, legal answer?
ai/.venv/bin/python ai/models.py remove von           # weights; `remove-runtime von` for the environment
```

There are two kinds of model:

- **Laya checkpoints** (`runtime: "laya"`) run inside this service with the pinned
  Laya SDK, on the Apple GPU.
- **Other open System One models** run through a **runtime**: the project's own code in
  an isolated environment under `ai/runtimes/<runtime>/` (a pinned pip package, or a
  pinned git commit set up with `uv sync`), serving the TypeSafe `/v1/systemone`
  protocol. This service starts that server on a free `127.0.0.1` port when the model
  is selected, forwards requests for the model to it, and stops it when the model is
  unloaded. The game always talks to port 8000. Server output goes to
  `ai/.runtime/models/<model>.log`.

| Model | Family / runtime | Base | License |
|---|---|---|---|
| `english` | Laya (built in) | ModernBERT-large, 421M | Apache-2.0 |
| `multilingual` | Laya (built in) | mmBERT-base, 322M | Apache-2.0 |
| `typed-decisions` | Laya (built in) | ModernBERT-large, 421M | Apache-2.0 |
| `von` | Von · `von-sdk` 1.3.7 | ModernBERT-large, 395M | Apache-2.0 |
| `kev-0.8b` | Kev · git `kev-1.0` (MLX) | Qwen3.5-0.8B + LoRA | Apache-2.0 |
| `system-one-qwen3-0.6b` | System One · git (MLX) | Qwen3-0.6B + LoRA | MIT |
| `system-one-minicpm5-2b` | System One · git (MLX) | MiniCPM5-2B, 8-bit | MIT |
| `openthai-systemone` | OpenThai · `openthai-systemone` 0.1.0 | Qwen3.5-0.8B, Thai + English | Apache-2.0 |
| `lafalce-system-one` | lafalce · git (CPU) | ModernBERT-base + LoRA | Apache-2.0 |

Measured start times, latency and probe scores on this machine are in
[`validation/LOCAL_MODELS.md`](../validation/LOCAL_MODELS.md). Larger models of the same
families (Kev 4B/9B/27B) need more memory than an 18 GB Mac can spare beside the game.

**What `download` does.** It installs the runtime if it is missing, fetches the
pinned weights (reusing the Hugging Face cache offline, hardlinking large files), then
performs a **first start with network access** so the server can fetch any base model
it needs. That start must answer the real game question with a legal action and finite
confidence, or the model is not marked installed. Later starts are offline. The manager
keeps its bookkeeping in `ai/models/.meta/`, outside model folders, because some
runtimes fingerprint every file in a model folder.

**Adding a model of an existing family** (for example a fine-tuned Kev or Von):

```sh
ai/.venv/bin/python ai/models.py add my-kev org/kev-finetune --runtime kev --revision <commit>
ai/.venv/bin/python ai/models.py add my-laya org/repo --revision <commit> --size-mb 800
ai/.venv/bin/python ai/models.py add my-run ~/checkpoints/run-7      # Laya-format folder
```

**Adding a new family.** Add a runtime and a model to `models.local.json`:

```json
{
  "runtimes": {
    "myfamily": {
      "family": "MyFamily",
      "install": {"kind": "pip", "python": "3.13", "packages": ["myfamily-sdk==1.2.3"]},
      "serve": {"argv": ["{python}", "-m", "myfamily.serve", "--port", "{port}", "--model", "{weights}"],
                "env": {"MYFAMILY_DEVICE": "{device}"}}
    }
  },
  "models": [
    {"id": "myfamily-small", "label": "MyFamily Small", "runtime": "myfamily",
     "repo": "org/myfamily-small", "revision": "<commit>", "size_mb": 900,
     "request_model": "myfamily-small"}
  ]
}
```

`install.kind` is `pip` (packages) or `git` (`url`, a pinned `ref`, optional `sync`
extras and extra `packages`). Placeholders: `{python}`, `{bin}`, `{src}` (git checkout),
`{weights}` (the model folder), `{port}`, `{device}` and `{args}` (the model's
`serve_args`). Optional model fields: `files` (only these files), `ignore`,
`serve_args`, `serve_env`, `request_model` (the `model` value sent to the server; if
the server rejects it, the name it lists on `GET /v1/models` is used). The server must
implement `POST /v1/systemone` and answer `choice` questions with `choice` and a
`confidence`.

Installing a runtime runs that project's code from PyPI or GitHub, so pin exact
versions and review projects you add. `openthai-systemone` loads model code shipped in
its Hugging Face repository (`trust_remote_code`).

**Memory.** At most two models stay resident across all kinds (`LAYA_MAX_LOADED`,
default 2); loading a third unloads the least recently used one and stops its server.
Two local AI players share one request worker, so in local-vs-local matches each
model's decision rate is lower.

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
