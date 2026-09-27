# Keyboard-only gameplay — 0.13.0

The match now uses a fixed 55° elevated orthographic camera looking over the entire
roof. Position, bearing and scale do not follow fighters or respond to mouse
movement, buttons or scrolling. Aspect-ratio fitting keeps the roof visible when
the window is resized. Impact effects remain on the fighters; camera shake is off.

| Key | Action |
|---|---|
| WASD or arrows | Move in consistent screen directions |
| F | Push with empty hands; throw when holding an object |
| E | Grab or drop |
| Either Shift | Dodge in movement direction; rightward if standing still |
| Space | Jump |
| Enter | Start the selected matchup from setup |
| Escape | Pause/resume |
| R | Start a fresh match during gameplay |

The human fighter automatically faces the opponent, independently of movement.
Throws use the same bounded lead/gravity calculation as the AI executor. The
trajectory preview uses that same aim. Normal turning speed, attack windup,
interruptions, cooldowns and recovery still apply; throws do not home after release.
A 0.4-second attack input buffer accommodates facing/recovery, and is cleared by
pausing, inactive rounds or dropping/grabbing. Attacks require pressing F.

Menus still support the pointer. Match gameplay needs no pointer, cursor lock,
manual camera control or manual aiming. Instructions in setup, HUD, pause, training
and the README have been updated. Spatial sound attenuation has been adjusted for
the more distant fixed listener.

## Verification

23 input checks passed in Unity Play Mode using synthetic Input System keyboard
and mouse devices through the real HumanInput component and normal player loop:

- Enter starts the matchup; WASD/arrows move correctly; diagonal movement is bounded.
- Facing tracks the enemy without changing screen-relative movement.
- Mouse clicks do not attack; mouse delta/scroll do not move the camera.
- The camera stays fixed as fighters move, uses orthographic projection, and fits
  all roof corners at fighter head height.
- F physically pushes and throws; E grabs and drops; Shift dodges; Space jumps.
- A keyboard-only throw physically hits the opponent.
- Paused keys are ignored and no queued attack leaks through resume.

The harness directs synthetic input to the game view in batch mode, primes button
edge tracking, and restores those test settings on completion.

```sh
scripts/unity-check.sh Playground.Editor.KeyboardGameplayValidationLauncher.Run
```

Detailed assertions are in `keyboard-gameplay.txt`.

- Full M1–M7 regression, provider-offline behavior, experiment provenance and
  retained UI/export checks passed.
- All 32 combat/tutorial checks passed; the previous mouse-pitch assertion now
  verifies that the arena camera keeps its fixed orientation.
- The final view uses an orthographic half-height of 13 m (with aspect fitting),
  leaving space for the scoreboard above fighters and the HUD below the roof.
- Mac build 0.13.0 (15) succeeded. Native Enter started a local match without
  a pointer click. The fixed whole-arena view and updated pause controls were
  visually checked at 3024 × 1842 content resolution.
- Native Player.log contained no runtime exceptions. `git diff --check` passed.
- The game was left open; the user paused it during the native check.
