"""Catalog, download and verification for local System One checkpoints.

`models.json` lists the checkpoints the game knows about; `models.local.json`
(untracked) adds your own. Installed checkpoints live in `ai/models/<id>/`, or in
an existing folder named by a `path` entry. The game service and this CLI share
the same code, so a model that installs here appears in the game's setup screen.

    ai/.venv/bin/python ai/models.py list
    ai/.venv/bin/python ai/models.py download multilingual
    ai/.venv/bin/python ai/models.py verify multilingual
    ai/.venv/bin/python ai/models.py add my-laya org/repo --revision <commit>
    ai/.venv/bin/python ai/models.py add my-finetune ~/checkpoints/run-7
    ai/.venv/bin/python ai/models.py remove multilingual

Only the selected model ever answers; there is no fallback between checkpoints.
"""
import argparse
from fnmatch import fnmatch
import json
import os
from pathlib import Path
import re
import shutil
import sys
import time

AI = Path(__file__).resolve().parent
ROOT = AI.parent
CATALOG = AI / 'models.json'
LOCAL_CATALOG = AI / 'models.local.json'
STORE = AI / 'models'
PARTIAL = STORE / '.partial'
VERIFIED = STORE / '.verified'
RUNTIME_VERSION = '0.10.0'
REQUIRED = ('rl_agent_config.json', 'model.safetensors')
PATTERNS = ('rl_agent_config.json', 'model.safetensors', 'tokenizer/*', 'encoder/*')
ID_PATTERN = re.compile(r'^[a-z0-9][a-z0-9._-]{0,63}$')
RUNTIMES = {'laya'}


class ModelError(Exception):
    """A user-facing problem with a catalog entry, download or checkpoint."""


def _read(path):
    try:
        return json.loads(path.read_text())
    except FileNotFoundError:
        return None
    except (OSError, ValueError) as exc:
        raise ModelError(f'{path.name} is not valid JSON: {exc}') from None


def _validate(entry, source):
    model_id = entry.get('id', '')
    if not ID_PATTERN.match(model_id):
        raise ModelError(f'{source}: invalid model id {model_id!r} (use lowercase letters, digits, . _ -)')
    if entry.get('runtime', 'laya') not in RUNTIMES:
        raise ModelError(f'{source}: {model_id} uses unsupported runtime {entry.get("runtime")!r}')
    if bool(entry.get('repo')) == bool(entry.get('path')):
        raise ModelError(f'{source}: {model_id} needs exactly one of "repo" or "path"')
    entry.setdefault('runtime', 'laya')
    entry.setdefault('label', model_id)
    entry.setdefault('description', '')
    entry.setdefault('agent_config', {'max_len': 1024, 'head_max_len': 384})
    return entry


def catalog():
    """All known models in display order; local entries extend or replace shipped ones."""
    shipped = _read(CATALOG) or {'models': []}
    models = {}
    for entry in shipped.get('models', []):
        models[entry['id']] = _validate(dict(entry), CATALOG.name)
    for entry in (_read(LOCAL_CATALOG) or {}).get('models', []):
        models[entry.get('id', '')] = _validate(dict(entry), LOCAL_CATALOG.name)
    return models


def default_model():
    return (_read(CATALOG) or {}).get('default', 'english')


def entry(model_id):
    models = catalog()
    if model_id not in models:
        raise ModelError(f'unknown model {model_id!r}; known: {", ".join(models) or "none"}')
    return models[model_id]


def model_dir(item):
    if item.get('path'):
        path = Path(os.path.expanduser(item['path']))
        return path if path.is_absolute() else ROOT / path
    return STORE / item['id']


def installed(item):
    directory = model_dir(item)
    return all((directory / name).is_file() for name in REQUIRED)


def disk_bytes(path):
    total = 0
    for root, _, files in os.walk(path):
        for name in files:
            try:
                total += os.lstat(os.path.join(root, name)).st_size
            except OSError:
                pass
    return total


def download_progress(item):
    """Fraction of the expected size present in the staging folder, or None."""
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
    return {'name': item['id'], 'label': item['label'], 'description': item['description'],
            'runtime': item['runtime'], 'source': source_string(item), 'size_mb': item.get('size_mb', 0),
            'installed': installed(item), 'local_path': bool(item.get('path')),
            'verified': bool(record and record.get('ok')),
            'download_progress': download_progress(item)}


def _patterns(item):
    prefix = f'{item["subfolder"]}/' if item.get('subfolder') else ''
    return [prefix + pattern for pattern in PATTERNS]


def _place(source, target):
    """Copy small files; hardlink weights when possible so shared caches cost no space.
    Small files are copied because the SDK may rewrite tokenizer config in place."""
    target.mkdir(parents=True, exist_ok=True)
    for path in source.rglob('*'):
        relative = path.relative_to(source)
        # Bundled repos keep sibling checkpoints in subfolders; take only this one's files.
        if not path.is_file() or not any(fnmatch(relative.as_posix(), pattern) for pattern in PATTERNS):
            continue
        destination = target / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        real = path.resolve()
        if path.name == 'model.safetensors':
            try:
                os.link(real, destination)
                continue
            except OSError:
                pass
        shutil.copy2(real, destination)


def download(model_id, allow_network=True):
    """Install a catalog model into ai/models/<id>. Reuses the Hugging Face cache offline when it
    already holds the pinned files; otherwise downloads only that checkpoint's files."""
    item = entry(model_id)
    if installed(item):
        return {'state': 'installed', 'name': model_id, 'message': f'{model_id} is already installed'}
    if item.get('path'):
        raise ModelError(f'{model_id} points to {model_dir(item)}, which does not contain {" and ".join(REQUIRED)}')
    from huggingface_hub import snapshot_download
    from huggingface_hub.errors import LocalEntryNotFoundError
    staging = PARTIAL / model_id
    shutil.rmtree(staging, ignore_errors=True)
    staging.mkdir(parents=True)
    options = {'repo_id': item['repo'], 'revision': item.get('revision'), 'allow_patterns': _patterns(item),
               'token': os.environ.get('HF_TOKEN')}
    reused = True
    try:
        cached = Path(snapshot_download(local_files_only=True, **options))
        source = cached / item['subfolder'] if item.get('subfolder') else cached
        if not all((source / name).is_file() for name in REQUIRED):
            raise LocalEntryNotFoundError('incomplete cache')
        _place(source, staging / 'checkpoint')
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
        os.replace(source, staging / 'checkpoint')
    checkpoint = staging / 'checkpoint'
    if not all((checkpoint / name).is_file() for name in REQUIRED):
        shutil.rmtree(staging, ignore_errors=True)
        raise ModelError(f'{source_string(item)} does not contain a Laya checkpoint ({", ".join(REQUIRED)})')
    (checkpoint / '.source.json').write_text(json.dumps({
        'source': source_string(item), 'reused_cache': reused, 'installed_at': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())}, indent=2))
    target = model_dir(item)
    shutil.rmtree(target, ignore_errors=True)
    os.replace(checkpoint, target)
    shutil.rmtree(staging, ignore_errors=True)
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
    (VERIFIED / f'{model_id}.json').unlink(missing_ok=True)
    return {'state': 'removed', 'name': model_id, 'freed_mb': round(freed / 1048576)}


def add(model_id, source, label=None, description=None, revision=None, subfolder=None, size_mb=None):
    """Register a Hugging Face repo or a local checkpoint folder in models.local.json."""
    local = Path(os.path.expanduser(source))
    item = {'id': model_id, 'label': label or model_id, 'runtime': 'laya',
            'description': description or 'Added with models.py add.'}
    if local.exists() or source.startswith(('/', '.', '~')):
        if not all((local / name).is_file() for name in REQUIRED):
            raise ModelError(f'{local} does not contain {" and ".join(REQUIRED)}')
        item['path'] = str(local.resolve())
        item['size_mb'] = size_mb or round(disk_bytes(local) / 1048576)
    else:
        item['repo'] = source
        if subfolder:
            item['subfolder'] = subfolder
        if revision:
            item['revision'] = revision
        item['size_mb'] = size_mb or 0
    _validate(item, 'add')
    data = _read(LOCAL_CATALOG) or {'schema': 1, 'models': []}
    data['models'] = [m for m in data['models'] if m.get('id') != model_id] + [item]
    LOCAL_CATALOG.write_text(json.dumps(data, indent=2) + '\n')
    return {'state': 'added', 'name': model_id, 'installed': installed(item)}


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
              'device': str(device), 'checked_at': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())}
    (VERIFIED / f'{item["id"]}.json').write_text(json.dumps(record, indent=2))
    return record


def verify(model_id, device=None):
    import torch
    item = entry(model_id)
    agent = load_agent(item, device)
    request = game_request()
    started = time.perf_counter()
    with torch.inference_mode():
        result = agent.system_one(request['state'], request['questions'])
    ok, answer = check_answer(result, request['questions'])
    return record_verification(item, ok, answer, (time.perf_counter() - started) * 1000, agent.device)


def main(argv=None):
    parser = argparse.ArgumentParser(description='Manage local System One checkpoints for IMPULSE.')
    parser.add_argument('--json', action='store_true', help='machine-readable output')
    commands = parser.add_subparsers(dest='command', required=True)
    commands.add_parser('list', help='show known and installed models')
    for name, text in [('download', 'install a catalog model'), ('remove', 'delete an installed model'),
                       ('verify', 'run the game question and check for a legal answer')]:
        command = commands.add_parser(name, help=text)
        command.add_argument('model')
        if name == 'verify':
            command.add_argument('--device', default=None, help='cpu or mps (default: SDK choice)')
    adding = commands.add_parser('add', help='register a Hugging Face repo or a local checkpoint folder')
    adding.add_argument('model')
    adding.add_argument('source', help='org/repo or a folder containing rl_agent_config.json')
    for option in ('label', 'description', 'revision', 'subfolder'):
        adding.add_argument('--' + option)
    adding.add_argument('--size-mb', type=int)
    args = parser.parse_args(argv)
    os.environ.setdefault('USE_TF', '0')
    try:
        if args.command == 'list':
            result = {'default': default_model(), 'models': [status(m) for m in catalog().values()]}
        elif args.command == 'download':
            result = download(args.model)
        elif args.command == 'remove':
            result = remove(args.model)
        elif args.command == 'verify':
            result = verify(args.model, args.device)
        else:
            result = add(args.model, args.source, args.label, args.description, args.revision, args.subfolder, args.size_mb)
    except ModelError as exc:
        result = {'state': 'error', 'message': str(exc)}
    if args.json:
        print(json.dumps(result), flush=True)
    elif args.command == 'list':
        for model in result['models']:
            mark = 'installed' if model['installed'] else f'{model["size_mb"]} MB download'
            check = ', verified' if model['verified'] else ''
            default = ' (default)' if model['name'] == result['default'] else ''
            print(f'{model["name"]:<18} {mark}{check}{default}\n{"":<18} {model["label"]} · {model["source"]}')
    else:
        print(result.get('message') or json.dumps(result, indent=2))
    return 1 if result.get('state') == 'error' or result.get('ok') is False else 0


if __name__ == '__main__':
    sys.exit(main())
