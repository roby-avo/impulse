# Local System One models

Every model in `ai/models.json` was installed with `ai/models.py download` on an
Apple M3 Pro (18 GB), then measured through the running game service with
`scripts/compare-local-models.py` (raw output: `local-models.json`).

| Model | Family | Load (cold, through the service) | Median decision | p90 | Probe score |
|---|---|---|---|---|---|
| `english` | Laya | 0.0 s | 82 ms | 83 ms | 6/8 |
| `multilingual` | Laya | 3.4 s | 35 ms | 36 ms | 5/8 |
| `typed-decisions` | Laya | 3.8 s | 88 ms | 90 ms | 4/8 |
| `von` | Von | 18.1 s | 69 ms | 74 ms | 4/8 |
| `kev-0.8b` | Kev | 16.0 s | 54 ms | 56 ms | 5/8 |
| `system-one-qwen3-0.6b` | System One | 7.0 s | 98 ms | 99 ms | 4/8 |
| `system-one-minicpm5-2b` | System One | 7.8 s | 340 ms | 347 ms | 3/8 |
| `openthai-systemone` | OpenThai | 15.0 s | 322 ms | 323 ms | 3/8 |
| `lafalce-system-one` | System One (lafalce) | 15.5 s | 163 ms | 172 ms | 4/8 |

- **Load** is the time from the game's load request until the model answered the real game
  question; for runtime models it includes starting the model's own server. The default
  `english` checkpoint was already resident.
- **Decision latency** is end to end through the service (HTTP forward included), for the exact
  recorded game request in `laya-benchmark-request.json`, after two warm-up calls.
- **Probe score**: eight hand-authored situations, one request each (`probe-opponent-actions.py`).
  The game's action criteria were worded and tuned for Laya English, so this mostly shows how
  each model reads *these* criteria, not its general ability. Per-situation answers are in
  `local-models.json`.

## What the probes show

- **Laya English** (6/8) is the most aggressive at range and still dodges a thrown prop.
- **Von** (4/8) leans defensive: it retreats or dodges even when a shove would win, but moves to
  safety at the edge and dodges an enemy's wind-up, which no other model did.
- **Kev 0.8B** (5/8) chooses push or attack sensibly at range and in reach, but attacks into an
  incoming throw.
- **System One 0.6B** (4/8) and **2B** (3/8) push well when in reach and throw held props; the 2B
  model prefers moving to safety over attacking at range.
- **OpenThai** (3/8) mostly moves toward safety or cover.
- **lafalce student** (4/8) answers `ATTACK_OPPONENT` in every situation; its probabilities are
  flat (0.25–0.39), so it is effectively a constant attacker here.

## Install notes

- Install times (runtime environment + weights + verified first start) were 1–2 minutes per
  family on this connection; a second model of an installed family skips the runtime step.
- `system-one-minicpm5-2b` fingerprints every `.json`/`.safetensors` file in its model folder
  and binds its temperature file to those hashes, so its catalog entry downloads only the six
  backbone files and uses the temperature file from the runtime's pinned repository. This is
  also why the manager keeps its bookkeeping outside model folders.
- The mpuig System One server accepts only the model id it advertises; the manager asks
  `GET /v1/models` and retries with that id.
- Not included: Kev 4B (its adapter path peaks near 16 GB while loading) and larger Kev models,
  which do not fit beside the game on 18 GB; `open-jev-deberta-v3-large` (no server, 256-token
  input limit) and `open-alternative-jev` (a library without a server).
