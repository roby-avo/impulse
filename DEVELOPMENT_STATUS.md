# Development status — through Milestone 5

**Playable and complete through M5.** Unity 6000.4.0f1 / URP. A universal Mac
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

- Art remains deliberately stylized primitives; custom Blender assets are M6.
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

**Next milestone:** M6 visual upgrade, only after further human playtesting.

Final handoff: double-click launcher verified, service detached successfully and
healthy after launcher exit, final visual fixes inspected in the Mac build.
The game is open on a fresh paused match; Escape resumes. The local service
remains resident for play and can be stopped with Stop Local Laya.command.
