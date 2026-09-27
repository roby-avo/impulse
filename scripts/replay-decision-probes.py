"""Replay saved local-model probes. These are tuning examples, not a held-out benchmark."""
import argparse
import json
from pathlib import Path
import urllib.request

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--input', default='validation/laya-improvement/prompt-probes.json')
parser.add_argument('--variant', default='both')
parser.add_argument('--output', default='validation/laya-improvement/replayed-probes.json')
args = parser.parse_args()
rows = [r for r in json.loads(Path(args.input).read_text()) if str(r['variant']) == args.variant]
if not rows:
    parser.error('No examples match the requested variant')
results = []
for row in rows:
    request = urllib.request.Request('http://127.0.0.1:8000/v1/systemone',
        data=json.dumps(row['request']).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=30) as response:
        answer = json.load(response)
    choice = answer['answers']['action']['choice']
    result = dict(row, response=answer, choice=choice, matches=choice in row['expected_examples'])
    results.append(result)
    print(f"{row['scenario']}: {choice}", flush=True)
Path(args.output).write_text(json.dumps(results, indent=2) + '\n')
print(f"Matched example choices: {sum(r['matches'] for r in results)}/{len(results)}")
