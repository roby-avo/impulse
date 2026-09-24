"""Compare CPU and Apple GPU answers on recorded Laya requests, with unchanged FP32 weights."""
import os
os.environ.setdefault('USE_TF','0');os.environ.setdefault('HF_HUB_OFFLINE','1')
import argparse,json
from pathlib import Path
import torch
from laya import Agent
p=argparse.ArgumentParser();p.add_argument('recording');p.add_argument('--output',default='validation/laya-device-parity.json');args=p.parse_args()
requests=[]
for line in Path(args.recording).read_text().splitlines():
 d=json.loads(line)
 if d.get('type')=='decision' and d.get('provider')=='Laya':requests.append((json.loads(d['request_state_json']),json.loads(d['questions_json'])))
requests=requests[:20];assert requests
torch.set_num_threads(4);torch.set_num_interop_threads(1)
a=Agent('convaiinnovations/laya',device='cpu');a.cfg.update(max_len=1024,head_max_len=384)
answers={}
for device in ['cpu','mps']:
 a.device=torch.device(device);a.model.to(a.device)
 with torch.inference_mode():answers[device]=[a.system_one(s,q)['answers']['action'] for s,q in requests]
report={'requests':len(requests),'same_choices':sum(c['choice']==g['choice'] for c,g in zip(answers['cpu'],answers['mps'])),'max_reported_probability_difference':max(abs(v-g['probabilities'][k]) for c,g in zip(answers['cpu'],answers['mps']) for k,v in c['probabilities'].items())}
Path(args.output).write_text(json.dumps(report,indent=2));print(json.dumps(report));assert report['same_choices']==len(requests) and report['max_reported_probability_difference']<=.001
