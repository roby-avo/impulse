# Experiments and match inspection

Launch with `Play Physics Playground.command`, then press **F2**. The game pauses
and releases the cursor. Set an experiment ID and condition ID, choose a request
rate cap, toggle actions and observations, and choose **Apply & start new match**.
The validated profile is saved and used on the next launch. Use F2 or Escape to
resume without applying changes. **Restore defaults** changes the draft only.

The cap is request starts per **game second**, excluding pauses (0.25–10 Hz).
There is one request in flight and one committed action at a time. Inference and
action duration can lower the actual rate. Neither inference latency nor model
mistakes are hidden by a replacement policy.

## Profiles and schemas

The active profile is copied into each match's metadata. Its SHA-256 hash and
experiment/condition IDs are also recorded on every decision, including late
responses from earlier matches. Profile changes in the panel start a fresh match.
IDs allow letters, digits, dots, underscores and hyphens. At least one supported
action and one known observation must be enabled.

**Edit profile file** opens the saved JSON. You can edit the per-action natural
language `questions.action.criteria` descriptions as well as the instructions.
Use **Reload profile.json**, review the draft, then apply. JSON settings live at:

`~/Library/Application Support/Impulse/Physics Playground/Experiments/profile.json`

Invalid saved settings disable autonomous requests and are reported in F2; they
never silently select a fallback tactic. Restore defaults and apply to recover.
Changing executable action names requires adding an executor, not just JSON.

The action subset is intersected with physical eligibility (range, cooldown,
grounded state and held/available objects). When that intersection is empty,
the AI waits and logs `no_legal_actions`; no model call or alternate choice is
made. Eligibility itself may disclose mechanical constraints even if you omit
related state fields. Account for this when designing observation ablations.

The exact projected observations are in `request_state_json`; the exact choice
question is in `questions_json`. `state_snapshot` and `resulting_state` retain
full diagnostic observations for offline analysis. Those extra fields are **not**
sent to Laya when omitted from the profile. Confidence is uncalibrated model
output; the raw response retains any returned probability distribution.

## Inspect and export

Press **F3**, or open F2 and choose a saved match then **Export & open inspector**.
The game pauses and opens a self-contained browser report. It works offline and
has no CDN, analytics, server or network dependencies. It supports:

- Top-down replay with play/pause, timeline scrubbing and round selection.
- Human/Laya/prop positions, velocity arrows and recent events.
- A decision table showing choice, latency and execution status.
- Exact request inputs, raw responses, action outcomes and resulting state.
- Experiment metadata, profile hash and summary statistics.
- Loading another JSONL file and downloading JSON or decision CSV.

The newest 12 logs appear in F2. For older logs use **Open telemetry folder** and
the report's **Load JSONL** control. Reports also read legacy M5 decision logs,
which do not contain spatial frames.

Each export folder contains `report.html`, `match.jsonl`, `match.json` and
`decisions.csv`, under:

`~/Library/Application Support/Impulse/Physics Playground/Telemetry/Exports/<match-id>/`

Completed matches export automatically. Mid-match exports are snapshots; export
again to include later decisions. JSONL is the authoritative append-only record;
a response still in flight can arrive after an export or summary was written.
The report recomputes latency statistics from all decision records it receives.
Mean/median/p95 in the final JSON summary cover all completed requests (including
errors/stale responses). The gameplay HUD and legacy `matches.csv` mean cover
executed decisions only. Request and response timestamps are UTC; simulation
and action timestamps use game time.

Spatial replay samples at up to 10 Hz and records resets without interpolation.
It is a visual record, **not deterministic PhysX resimulation** or a full video.
Human event timestamps do not establish reaction times. No random seed makes
human/model timing and physics deterministic. The pretrained model is not
fine-tuned for the game; record model/service changes separately when comparing
runs made with different checkpoints or installations.
