# IMPULSE — interface and presentation pass (0.11.0)

This pass follows the gameplay upgrade and extends it to the UI, arena, robots,
and movement feel. It does not replace the local model or change its tactical policy.

## Interface

- Split match setup: IMPULSE identity and a resolution-independent rooftop
  illustration on the left; selectable opponent/arena cards and match rules on
  the right. Selected cards have a clear border and color treatment.
- Persistent launch action, separate scrollable cloud connection controls, masked
  session-only API key, and the existing verified-key requirement.
- Warm ivory typography, pale lime primary actions, cyan/orange player identities,
  consistent spacing, hover/focus states, and restrained panel borders.
- Centered scoreboard with separate round information and clock; compact loadout
  and ability bars; keycap hints; unified tutorial and warning treatments.
- Pause menu with a controls reference, larger result score, scrollable results,
  and matching experiment-lab navigation and forms.

## Arena, robots and feel

- Authored matte alternate floor panels replace reflective metal checker tiles.
  Court lines and end markings give the roof scale; facade ribs and light strips
  add detail below the playable boundary.
- Neutral tone mapping with restrained bloom and vignette improves highlight
  handling. No additional physical obstacles were introduced.
- Blender helmet mesh separated at its neck pivot, with cheek armor and rear
  vents. Helmets track the opponent within a limited angle. Arm and leg linkages
  connect the modular body parts visually without adding gameplay colliders.
- Movement-driven stride phase, smoothed boot poses, idle breathing and softened
  landing motion reduce visual snapping.
- Reset and bounded facing now synchronize Rigidbody rotation, so interpolation
  cannot undo a spawn-facing change. A focused physics regression covers this.
- Smoothed camera focus and zoom, and a small speed-dependent field-of-view change.
  Aim remains immediate. Ground acceleration is 40 m/s², braking is 48 m/s²,
  and air acceleration is 12 m/s²; both fighters use the same rules.

## Verification

- Blender export completed, including the new robot_head asset.
- All 32 focused combat/tutorial checks passed after the movement/presentation changes.
- Full M1–M7 regression and retained UI/export checks passed.
- macOS 0.11.0 build 13 succeeded.
- Visual inspection covered setup, tutorial/HUD, pause, results and the lab. At 1280×720,
  local rules fit without scrolling, cloud setup keeps the launch action visible,
  selection cards update the rooftop preview, and unverified cloud play is disabled.
  The final training view confirms that the rival faces the player after reset.

The rooftop illustration is schematic rather than an exact minimap. The local
model's tactical weaknesses described in GAMEPLAY_UPGRADE.md remain a separate
balancing task. Frame-time measurements are short samples, not long-session soaks.

## Performance sample

Final five-view benchmark on M3 Pro at 3024×1842, eight seconds per view:

| View | Mean frame time | Approx. FPS | p95 |
| --- | ---: | ---: | ---: |
| gameplay | 16.844 ms | 59.4 | 17.575 ms |
| pause | 16.771 ms | 59.6 | 17.474 ms |
| experiments | 16.736 ms | 59.8 | 17.112 ms |
| results | 16.736 ms | 59.8 | 16.840 ms |
| setup | 17.044 ms | 58.7 | 17.575 ms |

Gameplay p99 was 17.909 ms, with two GC collections. Setup had a single
139.484 ms maximum frame during a sample that included a native screenshot/app
inspection; average frame rate does not imply hitch-free rendering. Allocation
counters were unavailable. No runtime exceptions or shader errors were reported.
See `ui-performance.json` and `ui-player-benchmark.log`.
