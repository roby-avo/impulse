# Physics Playground

A rooftop ring-out game built with Unity 6.4: **Human vs Laya**,
**Human vs TypeSafe**, or **Laya vs TypeSafe**.

## Play on this Mac

Double-click **Play Physics Playground.command**. It starts the local Laya
service in the background if installed, and opens `Builds/Physics Playground.app`.
The built game and Python environment are already available on this workstation.
The launcher leaves Laya resident for quick rematches. Use
**Stop Local Laya.command** to stop a launcher-managed service when finished.

For local Laya on a fresh checkout, run `./ai/setup.sh` once.
TypeSafe-only play does not require the local Python environment. The launcher can build the Mac app
if it is missing; close the Unity editor before that first build. Python 3.13 and
Unity **6000.4.0f1** were used for the validated build. The app includes Apple
Silicon and Intel binaries; inference was tested on Apple Silicon CPU.

## Choose who plays

The game opens on **Choose your matchup**. Select a mode, then **Enter the arena**.

- **Human vs Laya:** play against the local model; no API key required.
- **Human vs TypeSafe:** enter your TypeSafe key, select **Check key & load models**,
  choose an available model, then start.
- **Laya vs TypeSafe:** watch local Laya (cyan) compete against TypeSafe (orange).
  Right-drag to orbit the spectator camera; Escape pauses the match.

Use **Escape → Change matchup / API key** to change players or credentials.
Keys are masked and kept in session memory only; they are excluded from settings,
recordings and exports. `TYPESAFE_API_KEY` is also read from the game's launch
process environment. TypeSafe sends arena observations and action instructions to
its cloud API and uses your account's request allowance. See [TYPESAFE.md](TYPESAFE.md).

## Smooth presentation

The HUD, pause screen, results and experiment lab use retained UI Toolkit
controls. **Escape → Graphics** switches between Balanced (default, 60 FPS cap,
85% 3D render scale / 2× MSAA) and High (120 FPS cap, full scale / 4× MSAA).
The UI stays crisp at native resolution. Losing window focus pauses the match.
The Mac app includes a custom robot icon for the Dock and Finder.
Version 0.8.1 also explicitly refreshes the running Dock tile through AppKit.

Telemetry writes and report exports run in the background. The new interface
uses tabs for Setup, Actions, Observations and Recordings. See
[performance evidence](validation/PERFORMANCE.md) for measured before/after
frame times and validation details.

## Controls

| Input | Action |
|---|---|
| WASD / mouse | Move / look |
| Space | Jump |
| Left Shift | Dodge in movement direction |
| E | Grab nearest reachable prop / drop held prop |
| Left click | Throw held prop; push when empty-handed |
| F / right click | Push |
| Mouse wheel | Camera distance |
| Tab (hold) | Match statistics and Laya action distribution |
| Escape | Pause / resume and unlock / capture cursor |
| R | Fresh match |
| F2 | Experiment settings, schemas and saved-match inspection |
| F3 | Export current/selected match and open offline inspector |
| F1 | Developer executor lab; practice, no scoring |

Knock the opponent off the roof. First to **five** round wins takes the match.
Simultaneous ring-outs draw. Players and props reset between rounds. Crates,
chairs, and barrels are throwable. Heavy boxes are pushable cover. Yellow
stripes mark open edges. The crosshair while holding a prop projects its initial
ballistic path approximately six metres ahead; it is not target lock-on.

Edit `Assets/StreamingAssets/game-config.json` and rebuild to change the target
number of wins (1–20). There is no health bar: impacts create knockback and a
brief loss of balance.

## Open in Unity

Open this folder in Unity Hub, open `Assets/Scenes/Rooftop.unity`, start
`./ai/start.sh` in a terminal, and press Play. The scene's Arena component builds
the same deterministic rooftop used by the app. URP, Input System, Cinemachine,
and AI Navigation are configured and pinned in `Packages/manifest.json`.

All ten actions can be exercised through F1. Opening the panel suspends the
model controller and marks practice. Closing it starts a fresh scored match.
This manual panel is an execution diagnostic, never a fallback policy.

## Laya and fairness

When Laya is selected, its tactical choices come **only** from locally running
`convaiinnovations/laya`. The installed `laya==0.3.7` SDK is preloaded once, and its
official HTTP interface binds to `127.0.0.1:8000`. Unity explicitly selects the
English root checkpoint. Runtime defaults to offline cached weights.

Mechanical action filtering excludes throwing without an object, grabbing while
holding one, pushes out of range/on cooldown, and unavailable jump/dodge actions.
It never ranks tactics. NavMesh pathfinding only executes the target associated
with Laya's selected action. Responses for old rounds or interrupted states are
discarded. Service errors/timeouts produce neutral wait and retries; there is
no random, rule-based, behavior-tree, or other-model replacement.

Human, Laya and TypeSafe use the same Fighter component, movement speed, jump, dodge,
interaction ranges, push cooldown, mass and impact response. Inference latency
is measured and shown; it is not concealed. This pretrained model has not been
fine-tuned for the game, and its tactical choices can be repetitive or weak.
Its confidence output is not validated game-specific calibration.

See [ai/README.md](ai/README.md) for setup, offline inference, API details and
`ai/.venv/bin/python ai/smoke_test.py`.

## Telemetry

On macOS, logs are saved under:

`~/Library/Application Support/Impulse/Physics Playground/Telemetry`

Each match has JSONL records with state, offered actions, raw model probabilities,
latency, execution times, outcome, interruptions and resulting state. Human
jump/grab/drop/throw/push/dodge/impact and round events are recorded too.
Completed matches append to `matches-v3.csv`, with explicit player identities. These event timestamps do **not**
claim to measure human reaction time. F1 practice is explicitly marked in logs.

## Experiment platform and upgraded art

Press **F2** to configure experiment IDs, request frequency, action/observation
schemas and question instructions. Press **F3** to export and inspect a match
with a sampled spatial replay and per-decision details. See
[EXPERIMENTS.md](EXPERIMENTS.md) for profiles, data definitions and limitations.

The robots, props, rooftop and skyline are original Blender meshes. Editable
source and regeneration instructions are in [ArtSource/README.md](ArtSource/README.md).
Physics continues to use the validated simple collision proxies.

## Reproduce validation and build

Close the editor first and leave `./ai/start.sh` running:

```sh
scripts/unity-check.sh Playground.Editor.Validation.M7
scripts/unity-check.sh Playground.Editor.LiveMatchLauncher.Run
scripts/unity-check.sh Playground.Editor.ProjectSetup.BuildMac -quit
```

`UNITY_EDITOR` overrides the Unity executable path. The default matches this Mac.
Mac builds require Xcode Command Line Tools (`xcode-select --install`) to compile
the small universal AppKit plugin from `Native/DockIcon.m`. The build invokes
`scripts/build-dock-plugin.sh` automatically.
M7 runs M1–M6 acceptance checks plus real-model experiment/schema/rate/export checks in actual Play Mode.
The live match test supplies scripted **human** inputs and keeps the Laya side
model-controlled. Test drivers are compiled only in the editor and are excluded
from the player. Latest logs are in `validation/unity.log`.

See [validation/ACCEPTANCE.md](validation/ACCEPTANCE.md) for evidence and
[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) for the milestone status.
Asset provenance is documented in [ASSETS.md](ASSETS.md).
