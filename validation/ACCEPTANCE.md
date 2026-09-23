# Acceptance evidence

Validated on macOS, Unity 6000.4.0f1, Python 3.13, Laya 0.3.7, local CPU inference.

| Milestone | Result | Evidence |
|---|---|---|
| M0 | PASS | Successful editor import, URP setup, final player compilation |
| M1 | PASS | Play Mode movement, grounded jump, dodge, barrel grab/throw, push impulse, fall/reset |
| M2 | PASS | Every semantic action terminates; navigation and physical interactions succeed |
| M3 | PASS | `laya-smoke.json`, `unity-laya-response.json`: real cached Laya responses |
| M4 | PASS | Live decision loop, eligibility, double-out draw, first-to-five, rematch, telemetry, offline neutral wait |
| M5 | PASS | Presentation/pause/resource checks, full physics match, universal Mac build, standalone visual inspection |

`latest-results.txt` is the most recent full regression acceptance output.
`results.txt` retains development runs, including early failures and their later
successful reruns. Full local editor logs (`*.log`) are available but excluded
from Git.

`live-match.txt` reports the final scripted-human vs real-Laya full physical match.
The test human supplies only movement and push inputs. It does not reposition
contestants during the fights; normal game round resets remain in effect.
Test scripts are guarded by UNITY_EDITOR and excluded from the standalone game.

`standalone-match.jsonl` preserves the real standalone seven-round match. The
corresponding `standalone-summary.json` derives 5–2, 118 executed choices and
522.4 ms mean request latency directly from that trace. Stale responses are
recorded separately and are not counted as executed decisions.

Visual QA used the actual Mac game window, including live grabbing, the paused
arena, score progression and the full results screen. Title clipping, reticle
placement and results-button text overlap were corrected after inspection.
Generated particles were corrected to stop before configuration and play on emit.

Reproduce with the commands in the root README; start the real Laya service first.

Final launcher verification: the local service remained alive with parent PID 1
in an independent session after the launcher exited; `/health` reported the
English model loaded, and the standalone game resumed valid model decisions.
The ready game was left open on a fresh paused match. Launcher shell syntax and
Git whitespace checks passed. Generated Unity YAML uses its standard empty-field
spacing, documented in `.gitattributes`.
