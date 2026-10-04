"""Catalog, download and verification for local System One models.

`models.json` lists the models the game knows about; `models.local.json` (untracked)
adds your own. Two kinds of model are supported:

* `laya` checkpoints run inside the game service with the pinned Laya SDK.
* Any other open System One model runs through a **runtime**: an isolated Python
  environment (a pinned pip package or a pinned git checkout, set up with uv) whose
  own server speaks the TypeSafe `/v1/systemone` protocol. The game service starts
  that server on demand and forwards requests for the model to it.

Installed weights live in `ai/models/<id>/` (or an existing folder named by a `path`
entry); runtimes live in `ai/runtimes/<runtime>/`. The game's setup screen lists the
same catalog and can download any entry.

    ai/.venv/bin/python ai/models.py list
    ai/.venv/bin/python ai/models.py download von
    ai/.venv/bin/python ai/models.py verify von
    ai/.venv/bin/python ai/models.py add my-laya org/repo --revision <commit>
    ai/.venv/bin/python ai/models.py add my-kev org/kev-finetune --runtime kev --revision <commit>
    ai/.venv/bin/python ai/models.py remove von

Only the selected model ever answers; there is no fallback between models.
"""
import argparse
from fnmatch import fnmatch
import json
import os
from pathlib import Path
import re
import shutil
import signal
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request

AI = Path(__file__).resolve().parent
ROOT = AI.parent
CATALOG = AI / 'models.json'
LOCAL_CATALOG = AI / 'models.local.json'
STORE = AI / 'models'
PARTIAL = STORE / '.partial'
VERIFIED = STORE / '.verified'
META = STORE / '.meta'   # bookkeeping lives outside model folders: some runtimes fingerprint every file
RUNTIMES_DIR = AI / 'runtimes'
LOGS = AI / '.runtime' / 'models'
RUNTIME_VERSION = '0.11.0'
REQUIRED = ('rl_agent_config.json', 'model.safetensors')
PATTERNS = ('rl_agent_config.json', 'model.safetensors', 'tokenizer/*', 'encoder/*')
IGNORED = ('assets/*', '*.gif', '*.png', '*.jpg', '*.jpeg', '*.mp4', 'train.log', 'run.log')
ID_PATTERN = re.compile(r'^[a-z0-9][a-z0-9._-]{0,63}$')
BUILTIN = 'laya'
FIRST_START_TIMEOUT = 1200   # the first start may fetch base weights and compile kernels
START_TIMEOUT = 300


class ModelError(Exception):
    """A user-facing problem with a catalog entry, runtime, download or model."""


def _read(path):
    try:
        return json.loads(path.read_text())
    except FileNotFoundError:
        return None
    except (OSError, ValueError) as exc:
        raise ModelError(f'{path.name} is not valid JSON: {exc}') from None


def _now():
    return time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())


# ------------------------------------------------------------------ catalog

def runtimes():
    """Runtime recipes by id; local entries extend or replace shipped ones."""
    merged = {}
    for source in (CATALOG, LOCAL_CATALOG):
        for runtime_id, spec in ((_read(source) or {}).get('runtimes') or {}).items():
            if not ID_PATTERN.match(runtime_id) or runtime_id == BUILTIN:
                raise ModelError(f'{source.name}: invalid runtime id {runtime_id!r}')
            install = spec.get('install') or {}
            if install.get('kind') not in ('pip', 'git') or not spec.get('serve', {}).get('argv'):
                raise ModelError(f'{source.name}: runtime {runtime_id} needs install.kind "pip" or "git" and serve.argv')
            if install['kind'] == 'pip' and not install.get('packages'):
                raise ModelError(f'{source.name}: runtime {runtime_id} lists no packages')
            if install['kind'] == 'git' and not (install.get('url') and install.get('ref')):
                raise ModelError(f'{source.name}: runtime {runtime_id} needs a git url and a pinned ref')
            merged[runtime_id] = dict(spec, id=runtime_id)
    return merged


def _validate(entry, source, known_runtimes):
    model_id = entry.get('id', '')
    if not ID_PATTERN.match(model_id):
        raise ModelError(f'{source}: invalid model id {model_id!r} (use lowercase letters, digits, . _ -)')
    runtime = entry.setdefault('runtime', BUILTIN)
    if runtime != BUILTIN and runtime not in known_runtimes:
        raise ModelError(f'{source}: {model_id} uses unknown runtime {runtime!r}')
    if bool(entry.get('repo')) == bool(entry.get('path')):
        raise ModelError(f'{source}: {model_id} needs exactly one of "repo" or "path"')
    entry.setdefault('label', model_id)
    entry.setdefault('description', '')
    entry.setdefault('family', 'Laya' if runtime == BUILTIN else known_runtimes[runtime].get('family', runtime))
    if runtime == BUILTIN:
        entry.setdefault('agent_config', {'max_len': 1024, 'head_max_len': 384})
    return entry


def catalog():
    """All known models in display order; local entries extend or replace shipped ones."""
    known = runtimes()
    models = {}
    for entry in (_read(CATALOG) or {'models': []}).get('models', []):
        models[entry['id']] = _validate(dict(entry), CATALOG.name, known)
    for entry in (_read(LOCAL_CATALOG) or {}).get('models', []):
        models[entry.get('id', '')] = _validate(dict(entry), LOCAL_CATALOG.name, known)
    return models


def default_model():
    return (_read(CATALOG) or {}).get('default', 'english')


def entry(model_id):
    models = catalog()
    if model_id not in models:
        raise ModelError(f'unknown model {model_id!r}; known: {", ".join(models) or "none"}')
    return models[model_id]


def is_server(item):
    return item['runtime'] != BUILTIN


def model_dir(item):
    if item.get('path'):
        path = Path(os.path.expanduser(item['path']))
        return path if path.is_absolute() else ROOT / path
    return STORE / item['id']


def _ready_marker(item):
    return META / item['id'] / 'ready.json'


def _source_marker(item):
    return META / item['id'] / 'source.json'


def installed(item):
    directory = model_dir(item)
    if not is_server(item):
        return all((directory / name).is_file() for name in REQUIRED)
    record = _read(_ready_marker(item)) if _ready_marker(item).exists() else None
    return bool(directory.is_dir() and runtime_installed(item['runtime']) and record
                and record.get('source') == source_string(item) and record.get('runtime') == runtime_fingerprint(item['runtime']))


def disk_bytes(path):
    total = 0
    for root, _, files in os.walk(path):
        for name in files:
            try:
                total += os.lstat(os.path.join(root, name)).st_size
            except OSError:
                pass
    return total


def _phase_file(model_id):
    return PARTIAL / f'{model_id}.phase'


def _set_phase(model_id, phase):
    PARTIAL.mkdir(parents=True, exist_ok=True)
    _phase_file(model_id).write_text(phase)


def download_phase(item):
    try:
        return _phase_file(item['id']).read_text().strip() or None
    except OSError:
        return None


def download_progress(item):
    """Fraction of the expected weights present in the staging folder, or None."""
    staging = PARTIAL / item['id']
    if not staging.exists():
        return None
    expected = max(1, item.get('size_mb', 0) * 1024 * 1024)
    return min(.99, disk_bytes(staging) / expected)


def verification(item):
    record = _read(VERIFIED / f'{item["id"]}.json')
    return record if record and record.get('source') == source_string(item) else None


def source_string(item):
    if item.get('path'):
        return str(model_dir(item))
    return '/'.join(filter(None, [item['repo'], item.get('subfolder')])) + '@' + (item.get('revision') or 'main')


def status(item):
    record = verification(item)
    return {'name': item['id'], 'label': item['label'], 'family': item['family'], 'description': item['description'],
            'runtime': item['runtime'], 'source': source_string(item), 'size_mb': item.get('size_mb', 0),
            'installed': installed(item), 'local_path': bool(item.get('path')),
            'verified': bool(record and record.get('ok')),
            'download_progress': download_progress(item), 'download_phase': download_phase(item)}


# ------------------------------------------------------------------ runtimes

def runtime_spec(runtime_id):
    known = runtimes()
    if runtime_id not in known:
        raise ModelError(f'unknown runtime {runtime_id!r}; known: {", ".join(known) or "none"}')
    return known[runtime_id]


def runtime_dir(runtime_id):
    return RUNTIMES_DIR / runtime_id


def runtime_fingerprint(runtime_id):
    return json.dumps(runtime_spec(runtime_id).get('install'), sort_keys=True)


def runtime_python(runtime_id):
    spec = runtime_spec(runtime_id)
    base = runtime_dir(runtime_id) / ('src' if spec['install']['kind'] == 'git' else '')
    return base / '.venv' / 'bin' / 'python'


def runtime_installed(runtime_id):
    try:
        marker = _read(runtime_dir(runtime_id) / '.installed.json')
        return bool(marker and marker.get('install') == runtime_fingerprint(runtime_id) and runtime_python(runtime_id).exists())
    except ModelError:
        return False


def _uv():
    """uv installs the isolated runtimes; it is added to the Laya environment if missing."""
    for candidate in (shutil.which('uv'), AI / '.venv' / 'bin' / 'uv', Path.home() / '.local' / 'bin' / 'uv'):
        if candidate and Path(candidate).exists():
            return str(candidate)
    subprocess.run([sys.executable, '-m', 'pip', 'install', '--quiet', 'uv==0.9.5'], check=True)
    return str(AI / '.venv' / 'bin' / 'uv')


def _run(argv, log, cwd=None, env=None):
    with open(log, 'a') as out:
        out.write(f'\n$ {" ".join(map(str, argv))}\n')
        out.flush()
        result = subprocess.run([str(a) for a in argv], cwd=cwd, env=env, stdout=out, stderr=subprocess.STDOUT)
    if result.returncode != 0:
        raise ModelError(f'command failed ({result.returncode}): {" ".join(map(str, argv[:4]))}… see {log}')


def install_runtime(runtime_id):
    """Create the runtime's own environment exactly as pinned in the catalog. Idempotent."""
    if runtime_installed(runtime_id):
        return {'state': 'installed', 'runtime': runtime_id}
    spec = runtime_spec(runtime_id)
    install = spec['install']
    target = runtime_dir(runtime_id)
    LOGS.mkdir(parents=True, exist_ok=True)
    log = LOGS / f'runtime-{runtime_id}.log'
    uv = _uv()
    env = dict(os.environ, UV_PYTHON_PREFERENCE='managed', HF_HUB_OFFLINE='0')
    env.pop('VIRTUAL_ENV', None)
    shutil.rmtree(target, ignore_errors=True)
    target.mkdir(parents=True)
    python = install.get('python', '3.13')
    if install['kind'] == 'pip':
        _run([uv, 'venv', target / '.venv', '--python', python], log, env=env)
        _run([uv, 'pip', 'install', '--python', target / '.venv' / 'bin' / 'python', *install['packages']], log, env=env)
    else:
        src = target / 'src'
        _run(['git', 'clone', '--quiet', install['url'], src], log, env=env)
        _run(['git', '-C', src, 'checkout', '--quiet', install['ref']], log, env=env)
        _run([uv, 'sync', '--project', src, '--python', python, *install.get('sync', [])], log, cwd=src,
             env=dict(env, UV_PROJECT_ENVIRONMENT=str(src / '.venv')))
        for package in install.get('packages', []):
            _run([uv, 'pip', 'install', '--python', src / '.venv' / 'bin' / 'python', package], log, env=env)
    (target / '.installed.json').write_text(json.dumps({'install': runtime_fingerprint(runtime_id), 'installed_at': _now()}, indent=2))
    return {'state': 'installed', 'runtime': runtime_id}


def remove_runtime(runtime_id):
    users = [m['id'] for m in catalog().values() if m['runtime'] == runtime_id and model_dir(m).is_dir() and not m.get('path')]
    if users:
        raise ModelError(f'runtime {runtime_id} is used by installed models: {", ".join(users)}; remove them first')
    target = runtime_dir(runtime_spec(runtime_id)['id'])
    freed = disk_bytes(target) if target.exists() else 0
    shutil.rmtree(target, ignore_errors=True)
    return {'state': 'removed', 'runtime': runtime_id, 'freed_mb': round(freed / 1048576)}


# ------------------------------------------------------------------ model servers

def free_port():
    with socket.socket() as probe:
        probe.bind(('127.0.0.1', 0))
        return probe.getsockname()[1]


def serve_command(item, port):
    """argv, env and working directory for a model's own /v1/systemone server."""
    spec = runtime_spec(item['runtime'])
    python = runtime_python(item['runtime'])
    values = {'python': str(python), 'bin': str(python.parent), 'src': str(runtime_dir(item['runtime']) / 'src'),
              'weights': str(model_dir(item)), 'port': str(port), 'device': os.environ.get('LAYA_DEVICE') or 'mps'}
    values.update({key: str(value).format(**values) for key, value in (item.get('values') or {}).items()})

    def fill(text):
        return str(text).format(**values)
    argv = []
    for part in spec['serve']['argv']:
        if part == '{args}':
            argv += [fill(a) for a in item.get('serve_args', [])]
        else:
            argv.append(fill(part))
    env = dict(os.environ)
    env.pop('VIRTUAL_ENV', None)
    env.pop('PYTHONPATH', None)
    env.update({key: fill(value) for key, value in (spec['serve'].get('env') or {}).items()})
    env.update({key: fill(value) for key, value in (item.get('serve_env') or {}).items()})
    cwd = values['src'] if spec['install']['kind'] == 'git' else str(runtime_dir(item['runtime']))
    return argv, env, cwd


def start_server(item, port, online=False):
    argv, env, cwd = serve_command(item, port)
    env['HF_HUB_OFFLINE'] = '0' if online else '1'
    env['TRANSFORMERS_OFFLINE'] = '0' if online else '1'
    LOGS.mkdir(parents=True, exist_ok=True)
    log = open(LOGS / f'{item["id"]}.log', 'a')
    log.write(f'\n--- {_now()} start on port {port} ({"online" if online else "offline"})\n$ {" ".join(argv)}\n')
    log.flush()
    process = subprocess.Popen(argv, cwd=cwd, env=env, stdin=subprocess.DEVNULL, stdout=log, stderr=subprocess.STDOUT,
                               start_new_session=True)
    log.close()
    return process


def stop_server(process):
    if process is None or process.poll() is not None:
        return
    try:
        os.killpg(process.pid, signal.SIGTERM)
    except OSError:
        process.terminate()
    try:
        process.wait(10)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except OSError:
            process.kill()


def served_model_name(port):
    """The model id a server advertises on GET /v1/models (Jev and OpenAI list shapes)."""
    with urllib.request.urlopen(f'http://127.0.0.1:{port}/v1/models', timeout=10) as response:
        listing = json.load(response)
    entries = listing.get('models') or listing.get('data') or []
    names = [e.get('name') or e.get('id') for e in entries if isinstance(e, dict)]
    if not names or not names[0]:
        raise ModelError('the server lists no models on /v1/models')
    return names[0]


def post(port, body, item, timeout=30):
    """One System One request to a model server; returns (result, milliseconds)."""
    payload = dict(body, model=item.get('_served_model') or item.get('request_model') or item['id'])
    request = urllib.request.Request(f'http://127.0.0.1:{port}/v1/systemone', data=json.dumps(payload).encode(),
                                     headers={'Content-Type': 'application/json'})
    started = time.perf_counter()
    with urllib.request.urlopen(request, timeout=timeout) as response:
        result = json.load(response)
    return result, (time.perf_counter() - started) * 1000


def wait_ready(item, port, process, timeout):
    """Poll with the real game question until the server answers; returns (result, ms)."""
    request = game_request()
    deadline = time.time() + timeout
    last = None
    while time.time() < deadline:
        if process.poll() is not None:
            raise ModelError(f'{item["id"]} server exited ({process.returncode}); see {LOGS / (item["id"] + ".log")}')
        try:
            return post(port, request, item, timeout=120)
        except urllib.error.HTTPError as exc:
            detail = exc.read()[:300].decode(errors='replace')
            if exc.code in (404, 422) and 'model' in detail.lower() and not item.get('_served_model'):
                # Some servers accept only the exact id they advertise; ask, then retry once.
                item['_served_model'] = served_model_name(port)
                continue
            raise ModelError(f'{item["id"]} rejected the game request (HTTP {exc.code}): {detail}') from None
        except (OSError, ValueError) as exc:
            last = exc
            time.sleep(1)
    raise ModelError(f'{item["id"]} did not answer within {timeout:.0f} s ({type(last).__name__}); see {LOGS / (item["id"] + ".log")}')


# ------------------------------------------------------------------ weights

def _patterns(item):
    if not is_server(item):
        prefix = f'{item["subfolder"]}/' if item.get('subfolder') else ''
        return [prefix + pattern for pattern in PATTERNS]
    return item.get('files')


def _ignored(item):
    return None if not is_server(item) else list(IGNORED) + item.get('ignore', [])


def _wanted(relative, item):
    patterns, ignored = _patterns(item), _ignored(item)
    if patterns is not None and not any(fnmatch(relative, p) for p in patterns):
        return False
    return not (ignored and any(fnmatch(relative, p) for p in ignored))


def _place(source, target, item):
    """Copy small files; hardlink weights when possible so shared caches cost no space.
    Small files are copied because some SDKs rewrite tokenizer config in place."""
    target.mkdir(parents=True, exist_ok=True)
    for path in source.rglob('*'):
        relative = path.relative_to(source)
        # Bundled repos keep sibling checkpoints in subfolders; take only this one's files.
        if not path.is_file() or '.cache' in relative.parts or not _wanted(relative.as_posix(), item):
            continue
        destination = target / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        real = path.resolve()
        if path.suffix in ('.safetensors', '.bin', '.pt', '.gguf', '.npz') or real.stat().st_size > 32 * 1024 * 1024:
            try:
                os.link(real, destination)
                continue
            except OSError:
                pass
        shutil.copy2(real, destination)


def _fetch_weights(item, allow_network):
    from huggingface_hub import snapshot_download
    from huggingface_hub.errors import LocalEntryNotFoundError
    model_id = item['id']
    staging = PARTIAL / model_id
    shutil.rmtree(staging, ignore_errors=True)
    staging.mkdir(parents=True)
    options = {'repo_id': item['repo'], 'revision': item.get('revision'), 'allow_patterns': _patterns(item),
               'ignore_patterns': _ignored(item), 'token': os.environ.get('HF_TOKEN')}
    reused = True
    try:
        cached = Path(snapshot_download(local_files_only=True, **options))
        source = cached / item['subfolder'] if item.get('subfolder') else cached
        if not is_server(item) and not all((source / name).is_file() for name in REQUIRED):
            raise LocalEntryNotFoundError('incomplete cache')
        _place(source, staging / 'checkpoint', item)
    except (LocalEntryNotFoundError, FileNotFoundError, OSError):
        reused = False
        shutil.rmtree(staging, ignore_errors=True)
        staging.mkdir(parents=True)
        if not allow_network or os.environ.get('HF_HUB_OFFLINE') == '1':
            shutil.rmtree(staging, ignore_errors=True)
            raise ModelError(f'{model_id} is not cached and downloads are disabled (HF_HUB_OFFLINE=1)') from None
        try:
            snapshot_download(local_dir=staging / 'download', **options)
        except Exception as exc:
            shutil.rmtree(staging, ignore_errors=True)
            raise ModelError(f'download of {model_id} failed: {type(exc).__name__}: {exc}') from None
        source = staging / 'download'
        if item.get('subfolder'):
            source = source / item['subfolder']
        shutil.rmtree(source / '.cache', ignore_errors=True)
        os.replace(source, staging / 'checkpoint')
    checkpoint = staging / 'checkpoint'
    if not is_server(item) and not all((checkpoint / name).is_file() for name in REQUIRED):
        shutil.rmtree(staging, ignore_errors=True)
        raise ModelError(f'{source_string(item)} does not contain a Laya checkpoint ({", ".join(REQUIRED)})')
    target = model_dir(item)
    _source_marker(item).parent.mkdir(parents=True, exist_ok=True)
    _source_marker(item).write_text(json.dumps({'source': source_string(item), 'reused_cache': reused, 'installed_at': _now()}, indent=2))
    shutil.rmtree(target, ignore_errors=True)
    target.parent.mkdir(parents=True, exist_ok=True)
    os.replace(checkpoint, target)
    shutil.rmtree(staging, ignore_errors=True)
    return reused


def _first_start(item):
    """Start the model's server once with network access so it can fetch base weights it
    needs, check a legal answer to the real game question, then stop. Later starts are offline."""
    port = free_port()
    process = start_server(item, port, online=True)
    try:
        result, ms = wait_ready(item, port, process, FIRST_START_TIMEOUT)
    finally:
        stop_server(process)
    request = game_request()
    ok, answer = check_answer(result, request['questions'])
    record_verification(item, ok, answer, ms, item['runtime'])
    if not ok:
        raise ModelError(f'{item["id"]} answered the game question with something the game cannot execute: {answer}')
    marker = _ready_marker(item)
    marker.parent.mkdir(parents=True, exist_ok=True)
    marker.write_text(json.dumps({'source': source_string(item), 'runtime': runtime_fingerprint(item['runtime']),
                                  'first_answer': answer.get('choice'), 'checked_at': _now()}, indent=2))


def download(model_id, allow_network=True):
    """Install a catalog model: its runtime (if any), its pinned weights, and for server models
    a verified first start. Reuses the Hugging Face cache offline when it holds the files."""
    item = entry(model_id)
    if installed(item):
        return {'state': 'installed', 'name': model_id, 'message': f'{model_id} is already installed'}
    try:
        if is_server(item) and not allow_network and not runtime_installed(item['runtime']):
            raise ModelError(f'{model_id} needs its {item["runtime"]} runtime, which requires network access to install')
        if is_server(item):
            _set_phase(model_id, 'installing runtime')
            install_runtime(item['runtime'])
        reused = True
        if item.get('path'):
            if not is_server(item):
                raise ModelError(f'{model_id} points to {model_dir(item)}, which does not contain {" and ".join(REQUIRED)}')
            if not model_dir(item).is_dir():
                raise ModelError(f'{model_id} points to {model_dir(item)}, which does not exist')
        elif not model_dir(item).is_dir() or (_read(_source_marker(item)) or {}).get('source') != source_string(item):
            _set_phase(model_id, 'downloading weights')
            reused = _fetch_weights(item, allow_network)
        if is_server(item):
            _set_phase(model_id, 'first start')
            _first_start(item)
    finally:
        _phase_file(model_id).unlink(missing_ok=True)
    return {'state': 'installed', 'name': model_id, 'reused_cache': reused,
            'message': f'{model_id} installed' + (' from the local cache' if reused else '')}


def remove(model_id):
    item = entry(model_id)
    if item.get('path'):
        raise ModelError(f'{model_id} is your folder {model_dir(item)}; remove its catalog entry instead')
    target = model_dir(item)
    freed = disk_bytes(target) if target.exists() else 0
    shutil.rmtree(target, ignore_errors=True)
    shutil.rmtree(PARTIAL / model_id, ignore_errors=True)
    shutil.rmtree(META / model_id, ignore_errors=True)
    (VERIFIED / f'{model_id}.json').unlink(missing_ok=True)
    note = f'; its runtime stays in ai/runtimes/{item["runtime"]} (models.py remove-runtime {item["runtime"]})' if is_server(item) else ''
    return {'state': 'removed', 'name': model_id, 'freed_mb': round(freed / 1048576), 'message': f'{model_id} removed{note}'}


def add(model_id, source, label=None, description=None, revision=None, subfolder=None, size_mb=None, runtime=BUILTIN):
    """Register a Hugging Face repo or a local folder in models.local.json."""
    local = Path(os.path.expanduser(source))
    item = {'id': model_id, 'label': label or model_id, 'runtime': runtime,
            'description': description or 'Added with models.py add.'}
    if local.exists() or source.startswith(('/', '.', '~')):
        if runtime == BUILTIN and not all((local / name).is_file() for name in REQUIRED):
            raise ModelError(f'{local} does not contain {" and ".join(REQUIRED)}')
        if not local.is_dir():
            raise ModelError(f'{local} is not a folder')
        item['path'] = str(local.resolve())
        item['size_mb'] = size_mb or round(disk_bytes(local) / 1048576)
    else:
        item['repo'] = source
        if subfolder:
            item['subfolder'] = subfolder
        if revision:
            item['revision'] = revision
        item['size_mb'] = size_mb or 0
    _validate(item, 'add', runtimes())
    data = _read(LOCAL_CATALOG) or {'schema': 1, 'models': []}
    data['models'] = [m for m in data.get('models', []) if m.get('id') != model_id] + [item]
    LOCAL_CATALOG.write_text(json.dumps(data, indent=2) + '\n')
    return {'state': 'added', 'name': model_id, 'installed': installed(item)}


# ------------------------------------------------------------------ checks

def game_request():
    """A representative in-game request: the real action question and a typical state."""
    return {'state': {
        'self': {'distance_to_edge_m': 4, 'holding_object': False, 'speed': 0, 'balance': 1},
        'opponent': {'distance_m': 3, 'near_edge': True, 'holding_object': False},
        'nearest_object': {'type': 'barrel', 'distance_m': 1.5},
        'incoming_projectile': False, 'previous_action': 'none'},
        'questions': json.loads((ROOT / 'Assets/StreamingAssets/laya-question.json').read_text())}


def load_agent(item, device=None):
    if not installed(item):
        raise ModelError(f'{item["id"]} is not installed; run: ai/.venv/bin/python ai/models.py download {item["id"]}')
    from laya import Agent
    agent = Agent(str(model_dir(item)), device=device)
    agent.cfg.update(item['agent_config'])
    return agent


def check_answer(result, questions):
    """The game only executes a legal choice with finite confidence in [0, 1]."""
    answer = (result.get('answers') or {}).get('action') or {}
    confidence = answer.get('confidence')
    legal = answer.get('choice') in questions['action']['criteria']
    finite = isinstance(confidence, (int, float)) and 0 <= confidence <= 1
    return legal and finite, answer


def record_verification(item, ok, answer, inference_ms, device):
    VERIFIED.mkdir(parents=True, exist_ok=True)
    record = {'source': source_string(item), 'ok': ok, 'choice': answer.get('choice'),
              'confidence': answer.get('confidence'), 'inference_ms': round(inference_ms, 1),
              'device': str(device), 'checked_at': _now()}
    (VERIFIED / f'{item["id"]}.json').write_text(json.dumps(record, indent=2))
    return record


def verify(model_id, device=None):
    item = entry(model_id)
    request = game_request()
    if is_server(item):
        if not installed(item):
            raise ModelError(f'{model_id} is not installed; run: ai/.venv/bin/python ai/models.py download {model_id}')
        port = free_port()
        process = start_server(item, port)
        try:
            wait_ready(item, port, process, START_TIMEOUT)
            result, ms = post(port, request, item)
        finally:
            stop_server(process)
        ok, answer = check_answer(result, request['questions'])
        return record_verification(item, ok, answer, ms, item['runtime'])
    import torch
    agent = load_agent(item, device)
    started = time.perf_counter()
    with torch.inference_mode():
        result = agent.system_one(request['state'], request['questions'])
    ok, answer = check_answer(result, request['questions'])
    return record_verification(item, ok, answer, (time.perf_counter() - started) * 1000, agent.device)


def main(argv=None):
    parser = argparse.ArgumentParser(description='Manage local System One models for IMPULSE.')
    parser.add_argument('--json', action='store_true', help='machine-readable output')
    commands = parser.add_subparsers(dest='command', required=True)
    commands.add_parser('list', help='show known and installed models')
    commands.add_parser('runtimes', help='show runtime recipes and whether they are installed')
    for name, text in [('download', 'install a catalog model (and its runtime)'), ('remove', 'delete an installed model'),
                       ('verify', 'run the game question and check for a legal answer'),
                       ('install-runtime', 'install a runtime environment'), ('remove-runtime', 'delete an unused runtime environment')]:
        command = commands.add_parser(name, help=text)
        command.add_argument('runtime' if 'runtime' in name else 'model')
        if name == 'verify':
            command.add_argument('--device', default=None, help='Laya only: cpu or mps (default: SDK choice)')
    adding = commands.add_parser('add', help='register a Hugging Face repo or a local folder')
    adding.add_argument('model')
    adding.add_argument('source', help='org/repo or a local folder')
    for option in ('label', 'description', 'revision', 'subfolder'):
        adding.add_argument('--' + option)
    adding.add_argument('--runtime', default=BUILTIN, help='laya (default) or a runtime id from `runtimes`')
    adding.add_argument('--size-mb', type=int)
    args = parser.parse_args(argv)
    os.environ.setdefault('USE_TF', '0')
    try:
        if args.command == 'list':
            result = {'default': default_model(), 'models': [status(m) for m in catalog().values()]}
        elif args.command == 'runtimes':
            result = {'runtimes': [{'id': r, 'family': s.get('family', r), 'kind': s['install']['kind'],
                                    'installed': runtime_installed(r)} for r, s in runtimes().items()]}
        elif args.command == 'download':
            result = download(args.model)
        elif args.command == 'remove':
            result = remove(args.model)
        elif args.command == 'verify':
            result = verify(args.model, args.device)
        elif args.command == 'install-runtime':
            result = install_runtime(args.runtime)
        elif args.command == 'remove-runtime':
            result = remove_runtime(args.runtime)
        else:
            result = add(args.model, args.source, args.label, args.description, args.revision, args.subfolder,
                         args.size_mb, args.runtime)
    except ModelError as exc:
        result = {'state': 'error', 'message': str(exc)}
    if args.json:
        print(json.dumps(result), flush=True)
    elif args.command == 'list':
        for model in result['models']:
            mark = 'installed' if model['installed'] else f'{model["size_mb"]} MB download'
            check = ', verified' if model['verified'] else ''
            default = ' (default)' if model['name'] == result['default'] else ''
            print(f'{model["name"]:<26} {mark}{check}{default}\n{"":<26} {model["family"]} · {model["label"]} · {model["source"]}')
    elif args.command == 'runtimes':
        for runtime in result['runtimes']:
            print(f'{runtime["id"]:<14} {runtime["kind"]:<4} {"installed" if runtime["installed"] else "not installed"}  ({runtime["family"]})')
    else:
        print(result.get('message') or json.dumps(result, indent=2))
    return 1 if result.get('state') == 'error' or result.get('ok') is False else 0


if __name__ == '__main__':
    sys.exit(main())
