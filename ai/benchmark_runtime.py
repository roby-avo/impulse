"""Benchmark the installed checkpoint on exact recorded inputs, never a substitute policy."""
import os
os.environ.setdefault('USE_TF','0')
os.environ.setdefault('HF_HUB_OFFLINE','1')
import argparse,json,time,statistics
from pathlib import Path
import torch
from laya import Agent

parser=argparse.ArgumentParser()
parser.add_argument('--request',default='validation/laya-benchmark-request.json')
parser.add_argument('--output',default='validation/laya-runtime-benchmark.json')
args=parser.parse_args()
payload=json.loads(Path(args.request).read_text())
torch.set_num_threads(4)
torch.set_num_interop_threads(1)
a=Agent('convaiinnovations/laya',device='cpu');a.cfg.update(max_len=1024,head_max_len=384)
results=[];reference=None
for device,threads in [('cpu',1),('cpu',2),('cpu',4),('cpu',6),('mps',2)]:
 if device=='mps' and not torch.backends.mps.is_available():continue
 try:
  torch.set_num_threads(threads);a.device=torch.device(device);a.model.to(a.device)
  times=[];answers=[]
  for i in range(5):
   start=time.perf_counter()
   with torch.inference_mode(): r=a.system_one(payload['state'],payload['questions'])
   ms=(time.perf_counter()-start)*1000
   if i>0:times.append(ms)
   answers.append(r['answers']['action'])
  if reference is None:reference=answers[-1]['probabilities']
  report={'device':device,'threads':threads,'median_ms':statistics.median(times),'latency_ms':times,'choice':answers[-1]['choice'],'max_probability_delta':max(abs(v-reference[k]) for k,v in answers[-1]['probabilities'].items()),'input_tokens':r['usage']['input_tokens']}
 except Exception as exc:report={'device':device,'threads':threads,'error':str(exc)}
 results.append(report);print(json.dumps(report),flush=True)
 Path(args.output).write_text(json.dumps({'torch':torch.__version__,'results':results},indent=2))
