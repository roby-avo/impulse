# Gameplay upgrade — 0.10.0

## Implemented

- Shared bounded turning for human and AI; committed attacks lock facing.
- Push anticipation, contact and recovery; longer recovery after a miss.
- Obstruction checks for static cover and intervening props.
- Consistent action permissions during stun, dodge, wind-up and recovery.
- Throws interrupted by impact; scheduled attacks cleared on round reset.
- Chair: 21 m/s, 0.12 s wind-up, full carry speed. Crate: 18 m/s,
  0.22 s wind-up, 90% carry speed. Barrel: 14 m/s, 0.38 s wind-up,
  72% carry speed. Mass still determines impact strength.
- Mouse pitch controls throw elevation. Preview and release share velocity
  calculation, including inherited movement. The arc stops at a sphere-cast
  collision estimate; rotating non-spherical props can contact differently.
- Exact nearest-grab marker, attached fighter markers, attack warnings,
  compact score/cooldowns and optional help (H) / model diagnostics (F4).
- AI can request its next model decision while a movement action runs.
  Returned actions are revalidated before execution; no substitute policy.
- Additional observations for wind-up, recovery, cooldowns and sudden death;
  retained concise action descriptions after evaluating longer alternatives.
  Only an unchanged original baseline
  is upgraded in memory automatically; custom profiles remain unchanged.
- Guided unscored tutorial: grab, physical throw hit, evade a training shove,
  physical ring-out. Its explicitly scripted instructor never runs in matches.
- Three symmetric prop layouts and round rotation; selectable first to 3/5/7.
- Optional sudden death after 60 s: orange safe boundary shrinks, with 0.75 s
  grace outside it. Physical roof stays intact. Simultaneous exits draw.
- Match metadata includes selected layout/rotation and timed-round rule.
- Revised locomotion, anticipation, follow-through, stagger, carrying, airborne
  and landing poses; contact-position VFX; pooled positional audio voices and
  damped metallic impact/footstep synthesis.
- Gradient sky, layered city silhouettes, rooftop wear, material roughness and
  metallic differentiation. Blender source regenerated without static spawn rings.

## Validation

Recorded results are in `combat-upgrade.txt`; the full existing M1–M7 suite,
matchup checks, live-model match and standalone inspection are recorded with
this change. TypeSafe contract checks use a localhost fixture, not paid cloud calls.

Reproduce focused physics/tutorial checks:

```sh
scripts/unity-check.sh Playground.Editor.CombatValidationLauncher.Run
```

Analyze a match:

```sh
python3 scripts/analyze-combat.py /path/to/match.jsonl
```

## Limits and next playtest

No claim of competitive balance or improved human enjoyment is made from scripted
checks. The pretrained local model can still choose poor tactics. Compare several
human matches across layouts and inspect action mix, round lengths, failed
executions, stale replies and reported readability. Stationary frame percentage
includes attack/recovery and is not a direct measurement of indecision.

For controlled experiments select Foundry and disable sudden death. Updated combat
rules change the environment; compare versioned recordings, not unlabelled scores.

## Results from this implementation

- 31 focused combat/tutorial checks pass, including real collision-driven throw,
  dodge and ring-out progression; see `combat-upgrade.txt`.
- Final M1–M7 regression and retained UI/export suite pass with real local Laya.
- 34 matchup checks pass (real Laya + localhost TypeSafe fixture).
- Final standalone visual check confirmed the setup rule label, guided-training
  transition, held-prop trajectory/reticle, compact HUD and return to setup.
  The rebuilt Mac app is left open at the matchup menu.
- Standalone M3 Pro benchmark at 3024×1842: gameplay averaged 16.736 ms
  (59.75 FPS), p95 16.672 ms, p99 17.584 ms over an eight-second sample.
  One gameplay GC collection; none during pause or experiments. Allocation
  counters were unavailable. No runtime exceptions appeared in the benchmark.
  See `upgrade-performance.json`; this short sample is not a long-session soak.
- Final scripted-human match: Human 5–0 Laya, 29 executed decisions, approximately
  127 ms mean executed request latency, zero request errors, 44 s total.
  Five requests overlapped an executing movement. See `upgrade-live-summary.json`.
- Six hand-authored tactical probes and rejected wording variants are preserved
  in `tactics-upgrade.json` and `tactics-variants.json`. The model still makes
  weak edge/defense choices; longer instructions regressed its behavior and were
  not shipped. Concise original action descriptions are retained.
- One final editor launch crashed inside Unity's native Burst compiler before
  tests began. Retrying completed all 31 checks; the final regression and build
  subsequently succeeded. The crash log is `upgrade-editor-startup-crash.log`.

The scripted attacker still wins reliably. These results validate execution and
regression behavior, not a solved competitive opponent or human enjoyment.
