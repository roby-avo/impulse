# Laya latency investigation — 0.9.1

2026-09-24, Apple M3 Pro. Battery power and Low Power Mode were active during
inspection; no system power preferences were changed.

## Reported match

Recording `20260924-123635-568fef.jsonl` showed:

| Player | Replies | Median latency | Range | Executed | Outdated |
| --- | ---: | ---: | ---: | ---: | ---: |
| Laya | 20 | 3,123 ms | 1,684–4,281 ms | 10 | 10 |
| TypeSafe | 99 | 265 ms | 232–349 ms | 98 | 1 |

This was slow inference, not a permanently stuck client. The request wait occupied
most of Laya's time, and changing world state invalidated half its replies.
A previous-day match had 447 ms median Laya latency. Power state and competing
render/CPU load can affect timing; the recordings do not isolate their individual
contributions to the slowdown.

The service explicitly forced CPU. Its SDK HTTP endpoint also performed the
synchronous inference call on the async event loop, preventing responsiveness
while computing. Cancelled Unity requests could leave a misleading deciding
status until the next status update.

## Changes

- Use Apple MPS when available, retain CPU override and the same FP32 weights.
- Warm up the retained model on its serving worker before reporting readiness.
- Run inference in one bounded worker; reject overload rather than queue old states.
- Keep health responsive and report actual device and inference timing.
- Show elapsed wait, last latency, executed/outdated counts, and actual backend.
- Clear cancelled status and record specific reasons for discarded replies.
- Preserve mechanical eligibility, input schemas and stale-response safety checks.

## Measurements

Same recorded request, four warm samples per configuration:

| Runtime | Median |
| --- | ---: |
| CPU, 1 thread | 618 ms |
| CPU, 2 threads | 516 ms |
| CPU, 4 threads | 466 ms |
| CPU, 6 threads | 458 ms |
| Apple MPS | 102 ms |

The MPS choice and reported probabilities matched CPU for this request.
A separate check of all 20 Laya requests in the reported match produced the same
20 choices and zero difference in reported probabilities. This is sampled
agreement, not a claim of bitwise equivalence for every possible input.

Updated live HTTP service: ten valid responses, **147 ms median**, **191 ms max**.
A health request during inference took **1.2 ms**. An overlapping inference
request was rejected with 429 in **1.2 ms** instead of queued.
The earlier live CPU service measured 472 ms median on the same standalone
request. These controlled measurements and the user's busy-game match have
different workloads and should not be interpreted as an exact same-load speedup.

Evidence: `laya-runtime-benchmark.json`, `laya-device-parity.json`,
`laya-http-before.json`, `laya-http-after.json` and the benchmark request JSON.
The full M1–M7 and retained UI/export acceptance suite passed with real GPU Laya.

## Rendered standalone check

The rebuilt universal macOS 0.9.1 app ran its opt-in rendering benchmark at
3024×1898 with real GPU Laya. Across 25 recorded requests: **132 ms median**,
**421 ms maximum**, 24 executed and one outdated. This was Human-vs-Laya with
an idle human and benchmark-controlled pauses, not a repeat of the user's
TypeSafe match. Live UI inspection showed `LAYA / Apple GPU`, `132 ms`, and
completed moves.

During the eight-second gameplay frame sample: mean **16.94 ms (~59 FPS)**,
p95 **17.50 ms**, p99 **20.95 ms**, max **73.76 ms**. Pause/lab averaged ~16.76 ms.
This is a short sample and not a claim that every frame is hitch-free.
Evidence: `laya-render-inference.json` and `laya-render-performance.json`.

All 34 matchup checks passed, including device/server timing records, elapsed
request age and cancellation status. The new build was relaunched normally and
visually confirmed at matchup setup. The local service remains on MPS.
