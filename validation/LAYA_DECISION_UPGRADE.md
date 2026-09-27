# Laya decision and execution update — 0.12.0

## What changed

- Factory request cap increased from 2 to 4 starts per game second, and navigation
  can plan its next step earlier. Each model still has only one request in flight.
- Added a selectable plain-language `situation` observation describing boundary
  danger, approaching projectiles, opponent windup/recovery, weapon possession,
  reach and line of sight. Existing numeric observations and exact request logs remain.
- Rewrote action criteria to explain when the action is useful. Eight hand-authored
  tuning scenarios matched example reasonable actions in 5/8 cases versus 3/8
  with the previous prompt/state. These examples were used for tuning, are not a
  held-out benchmark, and include judgment calls rather than definitive optimal labels.
- Repeated model selections continue the existing movement instead of braking,
  resetting its target and extending its timeout every time. Changed cover targets
  restart deliberately. Every continuation requires a new valid model answer.
- Entering melee range no longer cancels escape/cover movements. Approach stops
  within attack range rather than continuing into the opponent. Committed pushes
  may advance using the fighter's existing reduced windup movement speed.
- Cover observations require a complete navigation path and actual occlusion of
  enemy sight. Object observations omit unreachable targets. Retreat destinations
  stay inside the roof margin, and already-arrived retreat is excluded as a no-op.
- Projectile warnings use relative velocity and predicted closest approach;
  throws passing beside the robot do not trigger an immediate threat.
- Selected throws compensate gravity and lead visible lateral movement through
  windup and flight. Lead is bounded; attacks retain normal turn rate, windup,
  damage and cooldowns, and direction locks when committed.
- Only exact untouched legacy factory profiles migrate in memory. Custom criteria,
  observation/action subsets, rates, IDs and notes are preserved.

## Live comparison

Same editor Play Mode test: first to five, same deterministic scripted human input
that walks toward Laya and pushes whenever possible. The Laya opponent uses the
actual local checkpoint on Apple GPU. Physics and asynchronous timing are not
perfectly deterministic. This is one baseline and one final comparison match,
not an estimate of general win rate. The recorded candidate precedes the final
small cover-continuation guard and stricter migration validation; the final build
passed the regression and native checks below. Two earlier timing/range-only candidates and
one context candidate are also retained for inspection.

| Measure | Before | After |
|---|---:|---:|
| Human–Laya score | 5–0 | 5–0 |
| Mean active round duration | 3.70 s | 17.34 s |
| Executed decisions / continuations | 24 | 243 |
| Mean latency of executed requests | 141 ms | 122 ms |
| Stale responses / total requests | 9 / 33 | 37 / 280 |
| Actual grabs / throws / throw hits | 0 / 0 / 0 | 3 / 3 / 1 |
| Actual successful pushes | 3 | 5 |
| Actual dodges | 0 | 10 |
| Stationary sampled frames | 30.1% | 17.3% |
| Self-caused ring-outs | 0 | 0 |
| Request errors | 0 | 0 |

Stationary samples include legitimate attack/recovery time. Continued model
choices count as executed decision segments, not distinct completed maneuvers.
The longer last two rounds dominate the duration improvement; the first two
rounds still ended in under five seconds. Laya still overuses cover and remains
vulnerable to relentless melee pressure. The update improves execution and
variety; it does **not** demonstrate a higher win rate or reliable optimal tactics.
The original checkpoint is unchanged, and there is no scripted replacement policy.

## Validation

- 21 focused Unity Play Mode checks passed, including a real physical throw hitting
  a laterally moving opponent, collision-course versus near-miss projectiles,
  retreat continuity under melee pressure, actual cover occlusion and custom-profile
  migration protection.
- Full M1–M7 regression and retained UI/export checks passed, including request caps,
  exact provenance, restricted action sets and provider-offline neutral waiting.
- All 32 combat regression checks passed.
- Mac build 0.12.0 (14) succeeded. Native launch exercised a real local match,
  pause and return to setup; no runtime exceptions appeared in Player.log.
  See `laya-improvement/native-smoke.json` for the recorded request counts.
- `git diff --check` passed.

Re-run the focused suite:

```sh
scripts/unity-check.sh Playground.Editor.AIDecisionValidationLauncher.Run
```

Re-run the live match:

```sh
scripts/unity-check.sh Playground.Editor.LiveMatchLauncher.Run
```

Replay the saved prompt tuning examples while no match is using the local server:

```sh
python3 scripts/replay-decision-probes.py --variant old
python3 scripts/replay-decision-probes.py --variant both
```

Evidence: `laya-improvement/before-match.jsonl`, `after-match.jsonl`, corresponding
summary JSON, `checks.txt`, and saved prompt probes with exact requests/responses.
For observation ablations, deselect `situation` alongside the raw fields whose
information it describes; see `EXPERIMENTS.md`.
