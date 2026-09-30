# Local Laya startup and recovery — 0.13.1

Previously, only the `.command` launcher started the Python service. Opening the
app directly after the service stopped left the game retrying a closed port.

The game now starts the installed local runtime after a connection failure and
retries every second while recovering. The helper runs off the Unity main thread,
spawns a detached service, and shares a cross-process startup lock with the
launcher. Startup attempts are throttled, and a failed server start has a
15-second cooldown. A verified PID prevents stale PID files from identifying
unrelated processes. Missing setup and port conflicts have actionable messages.
The game locates the checkout above its app directory; keep the app in `Builds/`.

## Verification on 2026-09-30

- Mac build 0.13.1 (16) succeeded.
- Four concurrent helper calls produced exactly one service PID. A later call
  reused the healthy service.
- Stopped the service and opened the built app directly, without the launcher.
  Starting Human vs Laya caused the game to launch the service. After resuming
  from normal focus-loss pause, real gameplay requests succeeded.
- Forcibly terminated that managed service during active gameplay. The game
  launched a new PID and resumed successful real-model decisions automatically.
  Recorded health evidence is in `laya-autostart.json`.
- Five isolated startup tests passed: reuse of a healthy service, missing Python
  setup, stale/reused PID, conflicting port, and rapid-failure cooldown.
- Launcher shell syntax and `git diff --check` passed.

Run the isolated checks without interrupting gameplay:

```sh
ai/.venv/bin/python -m unittest discover -s ai -p test_ensure_service.py -v
```

The service is started on demand, not installed as a login daemon. Model weights
still require the one-time setup. Quitting the game before using the stop command
prevents its active local controller from restarting the service.
