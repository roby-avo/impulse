# UI and frame pacing upgrade — 0.8.0

Measured with the opt-in standalone `--pg-benchmark` mode on this Mac's Apple
M3 Pro at **3024 × 1842**, with the local Laya service running. Each stage has
an eight-second sample after warm-up. Raw records: `performance-before.json`
and `performance-after.json`.

| Stage | Before mean frame | After mean frame | Before p99 | After p99 | GC collections before → after |
|---|---:|---:|---:|---:|---:|
| Gameplay | 33.33 ms (~30 FPS) | 16.77 ms (~60 FPS) | 33.61 ms | 17.60 ms | 3 → 2 |
| Pause | 25.56 ms (~39 FPS) | 16.74 ms (~60 FPS) | 40.48 ms | 17.62 ms | 3 → 0 |
| Experiment lab | 16.67 ms (~60 FPS) | 16.74 ms (~60 FPS) | 18.21 ms | 17.60 ms | 24 → 0 |

This compares the previous shipping defaults against the new **Balanced**
default: 60 FPS cap, 85% 3D render scale and 2× MSAA. The retained UI remains at
native resolution. The **High** option uses 100% 3D scale, 4× MSAA and a 120 FPS
cap. These are short before/after smoke measurements, not a guaranteed frame
rate on all hardware or a controlled comparison of Laya's variable tactics.
`GC.GetAllocatedBytesForCurrentThread` returns zero on this Unity player runtime;
byte-allocation measurements are unavailable, not proof of zero allocations.
Collection counts are measured with `GC.CollectionCount(0)` across the process.

## Causes addressed

- Legacy OnGUI/GUILayout rebuilt UI and allocated arrays/strings on repeated GUI
  events. Gameplay, pause, results and experiment screens now use UI Toolkit.
  Controls persist, labels update only when their values change, HUD data refreshes
  at 10 Hz, and paused performance text at 2 Hz. The aiming marker still tracks
  every frame. F1 remains a developer-only immediate-mode execution diagnostic.
- Full Retina 3D rendering with 4× MSAA and display-driven VSync was costly.
  Balanced settings bound the workload and request a predictable 60 FPS cadence.
- Every telemetry sample previously flushed a file on the main thread.
  A dedicated ordered writer now batches disk flushes every 250 ms and drains
  on exit. Serialization of Unity state remains on the main thread; file I/O
  does not. Snapshot barriers preserve complete ordered records for export.
- Match exports previously read, parsed and wrote whole reports synchronously.
  Exports now take a consistent snapshot and run on a background task, with
  busy state and error feedback. Concurrent exports are serialized.
- Menus release/capture the pointer explicitly, pause on focus loss, and suppress
  resume-click input for one frame to avoid accidental attacks.

## Verification

Full M1–M7 Play Mode regression with real local Laya, plus retained-document
lifetime, no idle menu rebuilds, repeated open/close, background export while
paused, input/time restoration and icon-load checks. Offline report DOM tests
remain available in `scripts/test-inspector.cjs`.

Standalone visual checks cover HUD, pause menu, tabbed setup, action toggles,
observation groups; automated checks also exercise the results screen. The generated app icon is assigned in Player
Settings and embedded as `Contents/Resources/PlayerIcon.icns` in the Mac bundle.

To reproduce timing, quit other copies of the game, then run from the project:

```sh
'Builds/Physics Playground.app/Contents/MacOS/Physics Playground' --pg-benchmark
```

It records gameplay/pause/lab stages without scripting a human policy, writes
`~/Library/Application Support/Impulse/Physics Playground/performance.json`,
and quits. Do not use the computer during the sample. Background applications,
thermal state and Laya inference affect results.
