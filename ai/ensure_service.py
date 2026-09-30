"""Idempotent, detached startup shared by the game and desktop launcher."""
import fcntl
import json
from pathlib import Path
import socket
import subprocess
import time
import urllib.request

ROOT = Path(__file__).resolve().parent.parent
RUNTIME = ROOT / 'ai/.runtime'


def healthy():
    try:
        with urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=1) as response:
            state = json.load(response)
        return state.get('status') == 'ok' and 'english' in state.get('loaded', []) and 'runtime_version' in state
    except (OSError, ValueError, TypeError, AttributeError):
        return False


def managed_pid():
    try:
        pid = int((RUNTIME / 'service.pid').read_text())
        command = subprocess.run(['ps', '-p', str(pid), '-o', 'command='], capture_output=True, text=True, timeout=2)
        if command.returncode == 0 and str(ROOT / 'ai/server.py') in command.stdout:
            return pid
    except (OSError, ValueError, subprocess.TimeoutExpired):
        pass
    return None


def ensure():
    python = ROOT / 'ai/.venv/bin/python'
    if healthy():
        return {'state': 'ready', 'message': 'Local Laya ready'}
    if not python.is_file():
        return {'state': 'error', 'message': 'Laya setup missing · run ai/setup.sh in the project folder'}
    RUNTIME.mkdir(exist_ok=True)
    # Serialize launcher/game requests, including the window before Python imports finish.
    with (RUNTIME / 'startup.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        if healthy():
            return {'state': 'ready', 'message': 'Local Laya ready'}
        pid = managed_pid()
        if pid:
            return {'state': 'starting', 'message': 'Laya is loading · reconnecting automatically', 'pid': pid}
        with socket.socket() as probe:
            probe.settimeout(.3)
            if probe.connect_ex(('127.0.0.1', 8000)) == 0:
                return {'state': 'error', 'message': 'Port 8000 is in use · close the other local service'}
        attempt = RUNTIME / 'last-start'
        if attempt.exists() and time.time() - attempt.stat().st_mtime < 15:
            return {'state': 'starting', 'message': 'Laya startup retry pending · diagnostics: ai/.runtime/service.log'}
        attempt.touch()
        with (RUNTIME / 'service.log').open('a') as log:
            process = subprocess.Popen([str(python), str(ROOT / 'ai/server.py')], cwd=ROOT / 'ai',
                                       stdin=subprocess.DEVNULL, stdout=log, stderr=subprocess.STDOUT,
                                       start_new_session=True)
        (RUNTIME / 'service.pid').write_text(str(process.pid) + '\n')
        return {'state': 'starting', 'message': 'Starting local Laya · reconnecting automatically', 'pid': process.pid}


if __name__ == '__main__':
    try:
        result = ensure()
    except Exception as exc:
        result = {'state': 'error', 'message': 'Cannot start Laya · check ai/.runtime/service.log', 'error_type': type(exc).__name__}
    print(json.dumps(result), flush=True)
    raise SystemExit(1 if result['state'] == 'error' else 0)
