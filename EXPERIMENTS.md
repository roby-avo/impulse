# Experiments and match inspection

Launch with `Play Physics Playground.command`, then press **F2**. The game pauses
and releases the cursor. The lab has Setup, Actions, Observations and Recordings
tabs. Set an experiment ID and condition ID, choose a request
rate cap, toggle actions and observations, and choose **Apply & start new match**.
The validated profile is saved and used on the next launch. Use F2 or Escape to
resume without applying changes. **Restore defaults** changes the draft only.

The cap is request starts per **game second**, excluding pauses (0.25–10 Hz).
Each AI player has at most one request in flight and one committed action at a time.
In Laya-vs-TypeSafe mode the same cap and schema apply independently to both players. Inference and
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
sent to either model when omitted from the profile. Confidence is uncalibrated model
output; the raw response retains any returned probability distribution.

## Inspect and export

Press **F3**, or open F2 and choose a saved match then **Export & open inspector**.
The game pauses and opens a self-contained browser report. It works offline and
has no CDN, analytics, server or network dependencies. It supports:

- Top-down replay with play/pause, timeline scrubbing and round selection.
- Both players’ and props’ positions, velocity arrows and recent events.
- A decision table showing player, choice, latency and execution status.
- Exact request inputs, raw responses, action outcomes and resulting state.
- Experiment metadata, profile hash and summary statistics.
- Loading another JSONL file and downloading JSON or decision CSV.

The newest 12 logs appear in F2. For older logs use **Open telemetry folder** and
the report's **Load JSONL** control. Reports also read legacy M5 decision logs,
which do not contain spatial frames.

Each export folder contains `report.html`, `match.jsonl`, `match.json` and
`decisions.csv`, under:

`~/Library/Application Support/Impulse/Physics Playground/Telemetry/Exports/<match-id>/`

Completed matches export automatically in the background. Export status and
errors appear in the lab; repeated clicks do not start duplicate jobs. Mid-match exports are snapshots; export
again to include later decisions. Disk writes are ordered on a background thread and flushed in 250 ms batches.
Exports use a consistent snapshot after flushing queued records. JSONL is the
authoritative append-only record;
a response still in flight can arrive after an export or summary was written.
The report recomputes latency statistics from all decision records it receives.
Mean/median/p95 in the final JSON summary cover all completed requests (including
errors/stale responses). The gameplay HUD and `matches-v3.csv` mean cover
executed decisions only. Request and response timestamps are UTC; simulation
and action timestamps use game time.

Spatial replay samples at up to 10 Hz and records resets without interpolation.
It is a visual record, **not deterministic PhysX resimulation** or a full video.
Human event timestamps do not establish reaction times. No random seed makes
human/model timing and physics deterministic. The pretrained model is not
fine-tuned for the game; record model/service changes separately when comparing
runs made with different checkpoints or installations.

## Multi-model recordings (schema 3)

Match metadata includes the matchup and both participants' actor, controller,
requested model and endpoint. Decisions include `actor`, `provider`, `model`
(requested alias), and `response_model` (the version returned by the service).
Decision IDs include actor identity so the two independent loops cannot collide.
Each request captures its original match, actor and profile; switching matchups
aborts outstanding requests, and late results remain in the original recording.

Summaries have `left_actor`, `right_actor`, `left_wins`, `right_wins`, `left` and
`right` physical metrics, plus `ai_players` with separate executed counts, failure
counts, stale counts, total executed latency and action distributions. Frames
identify fighter `slot` as `left` or `right`. Historical `human`/`laya` and
`human_wins`/`laya_wins` JSON fields remain compatibility aliases for left/right;
use the explicit participant identities for new analysis. New aggregate CSVs
use `matches-v3.csv` to avoid changing the header of existing recordings.

Neither credentials nor authentication headers are recorded. HTTP error bodies
are discarded; raw successful model responses are retained with key redaction.
TypeSafe model latency includes the network round trip. No delay is added to
artificially equalize the local and cloud models.

## Laya decision update (0.12)

Fresh factory profiles now cap requests at 4 Hz. Only exact untouched 2 Hz factory
profiles from the 22- or 27-field versions migrate in memory; edited criteria,
custom IDs, notes, rates, action sets and observation sets are preserved.

The new `situation` observation describes visible geometry, weapons, attack
windups, recovery, cover and boundary danger in plain language. It contains no
chosen action. **Deselect `situation` as well as the corresponding numeric fields
when ablating that information**, because the description otherwise conveys it.
The complete numeric snapshot remains available for telemetry.

Repeated model selections can continue an existing movement without resetting
its deadline or destination. Such recorded movement segments end with
`continued_by_model`; `execution_success: true` means the segment was continued,
not that the overall destination was reached. A changed cover position starts a
new execution. Every continuation and every new action still requires a valid
provider response, and the configured request cap remains enforced.

The executor now leads observed opponent velocity for throws, advances within a
committed push's existing movement limits, clamps retreat destinations inside the
roof, and checks cover/object reachability. These execution semantics apply to
both AI providers. They do not grant different speed, damage, cooldowns, or
invulnerability, and no policy runs when the provider is unavailable.
