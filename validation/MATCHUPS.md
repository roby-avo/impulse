# Matchup validation — 0.9.0

2026-09-23, Unity 6000.4.0f1 on Apple M3 Pro.

The dedicated Play Mode suite passed 31 checks. Laya used the real local English
checkpoint. TypeSafe used a test-only HTTP server implementing the official
models and System One choice contracts; it is never a production fallback.

Coverage:

- Typed response validation, missing confidence, invalid confidence and illegal choices.
- Bearer authentication, model discovery, requested model and served model identity.
- Masked key entry and preventing an unverified cloud matchup from starting.
- No credential in match JSONL/CSV, even when a response echoes the fixture key.
- 401 stops retries; 429 respects timed backoff; no error response bodies retained.
- All three player mappings, spectator control isolation, independent decision loops.
- Separate decision IDs, actor/provider provenance, per-player counts and latency.
- Both AI-vs-AI winners, clean rematches, pending-request cancellation on mode switch.
- Late response isolation to its original match/actor, setup pause and exports.

Reproduce with the local Laya service running, and close the Unity editor:

```sh
python3 scripts/typesafe-fixture.py
# In a second terminal:
scripts/unity-check.sh Playground.Editor.MatchupValidationLauncher.Run
```

The fixture binds only to 127.0.0.1:8766 and accepts only its public dummy test key.
Stop it after testing. Detailed results are in `validation/matchups.txt`.
One initial local fixture connection timed out; rerunning after confirming its
health completed all checks successfully.

Live authenticated TypeSafe inference requires a user-provided account key and
was not performed. The game discovers account-specific models before enabling
cloud play; API availability, quota and actual cloud latency remain account-dependent.

The full existing M1–M7 acceptance suite and retained-UI/background-export checks
also passed after the change. The export inspector's JavaScript and the updated
launcher passed syntax checks. These checks include real local Laya inference.

The universal macOS 0.9.0 build succeeded and was launched. Its runtime log showed
successful startup and `DOCK ICON APPLIED`, without runtime exceptions. The final
interactive/visual UI check was blocked because the Mac remained locked; this
limitation is separate from the passing Play Mode UI-state checks.
