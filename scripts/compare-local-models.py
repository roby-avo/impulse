"""Compare installed local System One models through the running game service (127.0.0.1:8000).

For each model: load time (cold start through the service), median and p90 latency of an exact
recorded game request, resident memory of its server process (runtime models), and the score on
the hand-authored situations in probe-opponent-actions.py. Not a benchmark of general ability.

    python3 scripts/compare-local-models.py [model ...] > validation/local-models.json
"""
import json, pathlib, statistics, subprocess, sys, time, urllib.error, urllib.request

ROOT = pathlib.Path(__file__).resolve().parent.parent
BASE = 'http://127.0.0.1:8000'
REQUEST = json.loads((ROOT / 'validation/laya-benchmark-request.json').read_text())


def call(path, body=None, timeout=30):
    data = None if body is None else json.dumps(body).encode()
    request = urllib.request.Request(BASE + path, data=data, headers={'Content-Type': 'application/json'},
                                     method='GET' if body is None and not path.endswith(('/load', '/download')) else 'POST')
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return json.load(response), response.headers


def models():
    return {m['name']: m for m in call('/v1/models')[0]['models']}


def resident_mb(runtime):
    out = subprocess.run(['ps', '-axo', 'rss=,command='], capture_output=True, text=True).stdout
    return round(sum(int(line.split(None, 1)[0]) for line in out.splitlines()
                     if f'ai/runtimes/{runtime}/' in line) / 1024) or None


def measure(name, info):
    started = time.time()
    call(f'/v1/models/{name}/load', {})
    while True:
        state = models()[name]
        if state['loaded'] or state['error'] or time.time() - started > 600:
            break
        time.sleep(.5)
    if not state['loaded']:
        return {'model': name, 'error': state['error'] or 'load timed out'}
    load_s = round(time.time() - started, 1)
    latencies = []
    for _ in range(12):
        t = time.perf_counter()
        try:
            call('/v1/systemone', dict(REQUEST, model=name))
        except urllib.error.HTTPError as exc:
            return {'model': name, 'error': f'HTTP {exc.code}: {exc.read()[:200].decode(errors="replace")}'}
        latencies.append((time.perf_counter() - t) * 1000)
    latencies = sorted(latencies[2:])   # the first calls include one-off warm-up work
    probe = subprocess.run([sys.executable, str(ROOT / 'scripts/probe-opponent-actions.py'), f'--model={name}'],
                           capture_output=True, text=True, timeout=600).stdout.strip().splitlines()
    score = probe[-1].split()[0] if probe else '?'
    return {'model': name, 'family': info['family'], 'runtime': info['runtime'], 'load_seconds': load_s,
            'median_ms': round(statistics.median(latencies)), 'p90_ms': round(latencies[int(len(latencies) * .9) - 1]),
            'server_memory_mb': resident_mb(info['runtime']) if info['runtime'] != 'laya' else None,
            'probe_score': score, 'probe': probe[:-1]}


def main():
    catalog = models()
    names = sys.argv[1:] or [n for n, m in catalog.items() if m['installed']]
    results = []
    for name in names:
        result = measure(name, catalog[name])
        print(json.dumps({k: v for k, v in result.items() if k != 'probe'}), file=sys.stderr, flush=True)
        results.append(result)
    print(json.dumps({'machine': subprocess.run(['sysctl', '-n', 'machdep.cpu.brand_string'], capture_output=True, text=True).stdout.strip(),
                      'note': 'Latency is end-to-end through the game service; probes are 8 hand-authored situations, one request each.',
                      'results': results}, indent=2))


if __name__ == '__main__':
    main()
