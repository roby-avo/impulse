"""HTTP timing/health check using real Laya and an exact recorded request."""
import argparse,json,statistics,time,urllib.request,urllib.error
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--request',default='validation/laya-benchmark-request.json');p.add_argument('--output',default='validation/laya-http-after.json');args=p.parse_args()
body=Path(args.request).read_bytes();allowed=json.loads(body)['questions']['action']['criteria']
def get(path):
 with urllib.request.urlopen('http://127.0.0.1:8000'+path,timeout=5) as r:return json.load(r)
def post():
 start=time.perf_counter()
 try:
  with urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:8000/v1/systemone',data=body,headers={'Content-Type':'application/json'}),timeout=15) as r:
   result=json.load(r);assert result['answers']['action']['choice'] in allowed
   return {'status':r.status,'ms':(time.perf_counter()-start)*1000,'timing':r.headers.get('Server-Timing'),'device':r.headers.get('X-Laya-Device')}
 except urllib.error.HTTPError as e:return {'status':e.code,'ms':(time.perf_counter()-start)*1000}
health=get('/health');runs=[post() for _ in range(10)];assert all(r['status']==200 for r in runs),runs
with ThreadPoolExecutor(2) as pool:
 running=pool.submit(post)
 deadline=time.perf_counter()+5
 while not get('/health')['busy']:
  assert time.perf_counter()<deadline
 start=time.perf_counter();during=get('/health');health_ms=(time.perf_counter()-start)*1000
 overlapping=post();first=running.result()
# Overlap waits briefly for the single worker (two AI players share it), never in an open-ended queue.
assert during['busy'] and first['status']==200 and (overlapping['status']==429 or (overlapping['status']==200 and overlapping['ms']<first['ms']+1000)),(during,overlapping,first)
result={'health':health,'runs':runs,'median_ms':statistics.median(r['ms'] for r in runs),'max_ms':max(r['ms'] for r in runs),'health_during_inference_ms':health_ms,'overlap':overlapping}
Path(args.output).write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
