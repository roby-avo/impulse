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
