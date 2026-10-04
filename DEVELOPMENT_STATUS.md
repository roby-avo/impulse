# Development status — through Milestone 7

**Playable and complete through M7.** Unity 6000.4.0f1 / URP. A universal Mac
standalone build is in `Builds/Physics Playground.app`; the double-click launcher
starts the real local Laya service and the game. No manual scene assembly is needed.

## Milestones and validation

- **M0 — Foundation:** Git initialized; Unity 6 project, URP, Input System,
  Cinemachine, AI Navigation, scene, shader resources and reproducible setup.
  Final editor/player compilations succeeded. Unity's initial Shader Graph API
  migration was completed; the batch editor search-index startup race was fixed.
- **M1 — Human physics sandbox:** Rooftop, mouse camera, move/jump/dodge,
  grab/drop/throw/push, crate/chair/barrel/heavy cover, knockout volume and resets.
  Play Mode checks verify movement, jump ascent, grab and throw velocity, push
  impulse, fall recovery and prop reset.
- **M2 — AI physical avatar:** Shared physical controller and all ten semantic
  actions; F1 manual executor lab. All actions return terminal outcomes, with
  successful approach/retreat/push/grab/throw/dodge/safety/cover/jump exercised.
- **M3 — Real local Laya:** Inspected installed Laya 0.3.7 API, preloaded cached
  `convaiinnovations/laya`, official local HTTP serving, structured choices,
  state snapshots, response validation and measured asynchronous Unity requests.
  Real Python smoke test and real Play Mode model request passed.
- **M4 — Human vs Laya:** Autonomous model-only loop, physical action eligibility,
  interruptions, stale-response rejection, countdown/fight/winner/reset,
  configurable first-to-five, simultaneous draws, rematches and JSONL/CSV logs.
  Tests cover five scored ring-outs, draw, rematch, and unavailable service with
  no substitute actions. Queued impacts are cleared across resets.
- **M5 — Game feel:** Tuned movement and shared knockback recovery; animated robot
  limbs, generated audio, sparks, modest camera impulse, improved aiming/camera,
  anti-aliasing, readable boundaries, styled HUD, pause, statistics/action
  distribution and rematch controls. Standalone visual inspection caught and
  corrected clipped title, aim marker and results-button overlap.

## Playtest evidence

A standalone seven-round match completed **Human 5–2 Laya**, with **118 executed
Laya decisions**, **nine AI throws**, six throw hits, and **522.4 ms** mean model
request latency. Its full trace and derived summary are in `validation/`.

A separate editor-only scripted-human test completed a physical first-to-five
match against live Laya without teleporting either contestant during fights.
The final run completed in about 41 seconds. The entire M1–M5 regression suite
also passed. See `validation/ACCEPTANCE.md`.

## Known limits

- Spatial replay is sampled at up to 10 Hz, not deterministic PhysX replay.
- Inspector DOM tests pass; automated browser preview of local HTML is blocked
  by the browser tool policy. F3 exports a self-contained report for local use.
- Laya is the pretrained English root checkpoint, without game-specific training.
  It can repeat tactics or make poor choices; no other policy corrects them.
  Aggressive scripted human play can beat it. Confidence is logged, not claimed
  to be calibrated for this game.
- Local CPU inference latency varies with machine load; observed requests were
  roughly 0.37–0.71 seconds. The HUD reports actual latency.
- Validation and launch were performed on macOS/Apple Silicon. The app also
  contains an Intel binary, but Intel inference was not tested.
- Enjoyment is subjective; repeated complete matches and mechanics are verified,
  rather than treating an automated test as proof of player preference.

## M6 — Visual upgrade complete

Original Blender 5.2.2 LTS production pipeline, ten modular FBX assets and editable
.blend gallery: armored robots with animated separate gloves/boots, crate,
barrel, chair, cargo box, fan vents, roof panels/markings and windowed skyline.
URP material roles and mesh batching preserve the original mechanics. M1–M6
regressions pass, including imported metre scale and unchanged collision proxies.
The standalone Mac build was visually inspected with the new art.

## M7 — Experimental platform complete

- F2 experiment lab with persistent IDs, notes, request-rate cap, enabled action
  subset, observation projection and editable model instructions/criteria.
- Invalid configurations stop requests; empty mechanical action intersections
  wait without a model call or fallback action.
- Versioned metadata, complete profile + hash, exact model request inputs,
  response status, raw answers, human metrics and latency percentiles.
- Spatial frames at up to 10 Hz for fighters and props, round/score markers.
- Offline event/decision inspector with replay controls and JSON/JSONL/CSV
  exports. Completed matches export automatically; F3 exports snapshots.
- Full M1–M7 regression passes with real Laya, including observation omission,
  restricted actions, request-rate enforcement, export, empty-action safety and
  old-experiment attribution for late responses during rematches.
- Inspector JavaScript/DOM tests cover decisions, sampled bodies, scrubber,
  round filter, play/pause, empty files and safe embedded data.

A new full physical match with upgraded art and telemetry completed Human 5–0,
nine executed Laya decisions, 648 ms mean latency for executed decisions, 402
spatial frames. All 22 requests averaged 688 ms; 13 stale responses were rejected.
See `validation/m7-live-match.jsonl` and `validation/m7-experiment.jsonl`.

**Specification complete through M7.** Further work would be driven by human
playtesting, model evaluation or additional maps/mechanics, rather than an
unfinished milestone. See `EXPERIMENTS.md` for measurement definitions and limits.


Final standalone verification: F2 opens and pauses the experiment lab; applying
the baseline profile saves all ten actions and 22 observation fields and starts
a fresh countdown. No runtime exceptions were present in Player.log. The final
panel uses an opaque background for readability over the arena.


## 0.8.0 — UI, responsiveness and app identity

Replaced the gameplay HUD, pause menu, results and experiment lab with retained
UI Toolkit controls and a shared visual style. The lab has four tabs, exact
numeric frequency input, readable toggle groups, background export feedback
and stable navigation. Disk logging uses an ordered background writer with
250 ms flush batches and consistent snapshot barriers; report generation runs
in a worker task. Menus restore input cleanly and pause on focus loss.

Balanced/High graphics presets are available from the pause screen. On the
Apple M3 Pro at 3024×1842, the short standalone benchmark improved gameplay from
33.33 ms (~30 FPS) to 16.77 ms (~60 FPS); pause and lab samples each had zero GC
collections, compared with 3 and 24 previously. See `validation/PERFORMANCE.md`
for methodology and limits. Real Laya inference remains asynchronous and visible.

Custom generated robot icon is included in the universal Mac app bundle and
HUD. Master image and exact generation prompt are preserved in the project.

## 0.8.1 — Running macOS Dock icon

The bundled ICNS was correct, but the user's running Dock tile remained generic.
A universal AppKit plugin now assigns `NSApplication.applicationIconImage` at
launch, after startup, and on focus. Mac builds compile and sign the plugin
automatically. See `validation/DOCK_ICON.md` for runtime verification and limits.

## 0.9.0 — TypeSafe and selectable matchups

Added a launch setup screen for Human vs Laya, Human vs TypeSafe, and Laya vs
TypeSafe. Cloud play authenticates via model discovery, with a masked API-key
field and account-specific model selection. Keys remain in session memory and
are excluded from saved configuration and recordings. Local Laya remains fully
local and does not require credentials.

Both AI players have independent clients, execution state and decision loops,
with symmetric observations/actions and a shared experiment profile. AI-vs-AI
uses an orbitable spectator camera and model-aware scores, winners and statistics.
Matchup changes cancel pending calls; failures never trigger a substitute policy.
Authentication/quota/request errors require reconfiguration; transient failures
and rate limits use bounded backoff.

Schema-3 recordings and CSV exports identify each actor, requested model and
served model, with per-player counts/latency/distributions. The launcher opens
the game while local Laya starts in the background, and permits cloud-only play
without installing the local inference environment.

Validation: 31 dedicated matchup checks passed with real local Laya and a
TypeSafe HTTP contract fixture. The existing M1–M7 and UI stability suite also
passed. Authenticated live TypeSafe inference remains untested because no account
key was supplied. See `TYPESAFE.md` and `validation/MATCHUPS.md`.

## 0.9.1 — Laya latency and honest runtime diagnostics

Investigated the reported AI-vs-AI match: Laya's median reply took 3,123 ms and
10/20 replies were outdated, versus TypeSafe's 265 ms and 1/99 outdated. The
service was forced to CPU; its synchronous SDK inference also blocked the HTTP
event loop. Cancellation could leave an old deciding status on screen.

Laya now defaults to Apple GPU/MPS where available, retains the same FP32
checkpoint and input schemas, warms up on a dedicated worker, and rejects
overload instead of queuing obsolete states. Health stays responsive and exposes
the actual device, busy state and inference timing. CPU override remains available.

The HUD shows each model's elapsed request time, last latency, executed/outdated
counts and backend. Cancelled status is cleared, and recordings include the
actual inference device/time and specific stale-response reasons.

Twenty requests from the user's recording produced identical choices and
reported probabilities on CPU and GPU. Real GPU Laya passed the full M1–M7/UI
suite and 34 matchup checks. See `validation/LAYA_LATENCY.md` for controlled
benchmarks and rendered-game measurements, including their limits.

## 0.10.0 — Combat, onboarding and arena presentation

Implemented telegraphed, interruptible attacks, consistent action gates, shared
turn rates, distinct prop handling, pitch-controlled trajectory preview, grab
highlighting, compact HUD and optional diagnostics. Added a physical tutorial,
three symmetric layouts, round rotation and optional shrinking-boundary sudden
death. AI requests can overlap movement and are revalidated before execution;
new observations expose combat timing. Presentation adds expressive procedural
poses, spatial audio, contact effects, sky/skyline depth and authored roof updates.
See `validation/GAMEPLAY_UPGRADE.md` for scope, evidence and playtest limits.

## Interface and presentation pass — 0.11.0

Redesigned match setup, HUD, pause, results and lab; added a vector rooftop
preview and selectable mode/layout cards. Blender now exports an articulated
helmet; robot strides and camera following are smoothed. The roof has matte
panels, court markings and facade detail, with restrained post-processing.
Both fighters share the revised acceleration/braking. Focused combat checks and
M1–M7 regressions pass. See `validation/UI_PRESENTATION_UPGRADE.md`.

## Laya decision pass — 0.12

Implemented clearer observations/criteria, 4 Hz factory default with strict legacy
profile migration, continuous model-authorized movement, reachable cover/objects,
trajectory-based threat detection and leading throws. No replacement tactical
policy; both AI providers share execution rules. Details and limitations are in
`validation/LAYA_DECISION_UPGRADE.md`.

## Keyboard gameplay — 0.13

Fixed arena camera and keyboard-only player input. WASD/arrows move in screen
directions, F attacks (push/throw), E grabs/drops, Shift dodges and Space jumps.
Opponent facing and throw aiming are automatic using the existing physical aim
solution. HUD, setup, pause controls and training instructions reflect the new
controls. Camera rotation/zoom/follow/shake and mouse attacks have been removed.

## 0.14.0 — Game experience pass

Six phases, each validated and committed separately on `gameplay-improvements`.

- **Fixes:** hold R to restart (it sits between E and F), a "FIGHT!" call each round,
  no winner celebration on draws, standing dodge sidesteps toward the safer side,
  persisted volume.
- **Persistence:** opt-in TypeSafe key in the macOS Keychain (native Security
  plugin, "Remember key on this Mac", "Forget saved key", auto-reconnect); setup
  choices persist in PlayerPrefs.
- **Local model manager:** `ai/models.json` catalog and `ai/models.py`
  (list/download/verify/add/remove); service 0.10.0 serves any installed checkpoint,
  lists models, loads and warms them, downloads in a separate process; Watch AI pairs
  any two models, including two local ones with no key. All Metal work runs on the
  inference thread (this fixed a crash when loading a model during inference).
- **Game feel (fixed camera):** hit-stop, a slow-motion beat when a robot drops off,
  ground rings, AI intent labels, synthesized music/wind/stinger, optional screen
  kick, pooled particles.
- **Opponent:** `ATTACK_OPPONENT` (one decision closes in and shoves), opt-in
  `CUT_OFF_OPPONENT`, `opponent.edge_behind_m`, props return, labelled Rival boost.
  See `validation/OPPONENT_UPGRADE.md` for probes and live measurements.
- **Depth and reach:** rising knockback, charged push, ledge save, launching fan
  vents, local two-player, gamepads, win/loss records across sessions.

All five Unity suites (M1–M7, keyboard, combat, AI decisions, matchups) and the
Python service tests pass; the macOS build was visually checked with the
`--pg-screenshot` hook. Human playtesting of the new mechanics and of the opponent
with and without Rival boost is the next step.

## 0.15.0 — Other open System One models

The local model manager now runs any open System One model, not only Laya checkpoints.
`ai/models.json` gains **runtimes**: pinned install recipes (pip package or git commit,
set up with uv in `ai/runtimes/<runtime>/`) plus a serve command for the project's own
`/v1/systemone` server. The local service (0.11.0) starts that server on demand, forwards
requests to it, counts it toward the two resident models and stops it when unloaded.
`download` installs the runtime and weights, then performs a verified first start.

Installed and measured on this Mac (M3 Pro, 18 GB): Von 1.3, Kev 0.8B, System One 0.6B
and 2B (MLX), OpenThai-SystemOne and the lafalce student, besides the three Laya
checkpoints. Laya vs Von plays in the game with per-player model provenance. Larger Kev
models do not fit beside the game on 18 GB. See `validation/LOCAL_MODELS.md`.
