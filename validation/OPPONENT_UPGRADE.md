# Opponent upgrade (model-decided)

Goal: make the AI opponent more of a threat without any replacement policy. The
model still chooses every action; game code only executes the chosen action.

## Changes

- **`ATTACK_OPPONENT`** (default on): one decision closes the gap and lands the same
  shove as `PUSH_OPPONENT`. Previously the AI stopped at 2.05 m after an approach and
  needed a second model reply before it could push. Offered when the shove is ready
  (or ready within 0.5 s) and the hands are empty.
- **`CUT_OFF_OPPONENT`** (opt-in in the F2 lab): move to the roof-center side of the
  opponent so a shove points toward the nearer edge. In every probe wording the
  pretrained Laya checkpoint never chose it, and including it lowered probe scores,
  so it is not in the default profile. It is available for other models.
- **Observation `opponent.edge_behind_m`**: roof left behind the opponent along the
  line from me through them. The `situation` text states it when it is under 2.5 m.
- **Props return** 8 s after falling off the roof, dropping in at their spawn point.
- **Rival boost** (setup screen, off by default): a labelled rule change for the orange
  robot (+25% or +50% shove, 15% or 23% less knockback), shown on the HUD and
  recorded as `rival_boost` in match metadata.
- Previous untouched factory profiles migrate to the new default; edited profiles are
  left as saved.

## Criteria wording, probed on real Laya

Eight hand-authored situations, one request each (`scripts/probe-opponent-actions.py`).
The example sets are judgment calls, not a calibrated benchmark.

| Question | Matches example set |
|---|---|
| Before (10 actions) | 5/8 (`probes-before.txt`) |
| First draft: "Rush at the enemy and shove them in one move, especially when little roof is left behind them." | 5/8, but chose attack in 7/8 cases including incoming throws and the enemy's wind-up |
| **Shipped: "Close in and shove a distant enemy."** | **6/8** (`probes-after.txt`) |
| Shipped + cut-off enabled | 5/8 (`probes-after-with-cut-off.txt`) |

Both remaining misses (shoving while at the edge or during the enemy's wind-up) are
also misses before the change.

## Live matches against the scripted attacker

`LiveMatchLauncher.Run` (editor, real local Laya `english`, Foundry rotation, first to
5). The scripted human is a frame-perfect worst case: it pushes on the first frame the
opponent is within 2.2 m, with no reaction time. Three matches per condition.

| Condition | Score (human–Laya) | Mean round | Laya push hits | Push hits per minute | Cover choices |
|---|---|---|---|---|---|
| Before | 15–0 | 6.9 s | 8 | 2.8 | 31% |
| After | 15–0 | 4.6 s | 11 | 4.7 | 0% |
| After, rival boost +50% | 15–0 | 4.8 s | 4 | 1.7 | 0% |

Summaries: `opponent-upgrade/*-summary.txt`.

The model is clearly more aggressive (attack is 49% of executed choices; push hits
per minute up about 70%) and no longer hides behind cover, but it still loses every
round to the scripted attacker. Event logs show why: Laya began 50 shoves and 37 were
cancelled because the bot's shove, started earlier with zero latency, landed first.
Committing the attack at the full 2.3 m push range instead of 2.1 m did not change
this (8 push hits in three matches), so the validated 2.1 m reach is kept. The
rival boost does not help in this race, because the first shove decides it.

Decisions marked `unreachable` in these recordings were all made while Laya was
already falling off the roof (outside the edge, airborne), not navigation failures.

## Limits

These results do not measure play against people, who react in roughly 200–300 ms
and do not push on the first possible frame. Human playtests with and without the
boost are the next step. The probe set is small and hand-made.
