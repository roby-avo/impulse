"""Exercise the actual local model. No mock/stub action provider."""
import json, time, urllib.request
from pathlib import Path
root=Path(__file__).resolve().parent.parent
questions=json.loads((root/'Assets/StreamingAssets/laya-question.json').read_text())
state={'self':{'distance_to_edge_m':4,'holding_object':False,'speed':0,'balance':1},'opponent':{'distance_m':3,'near_edge':True,'holding_object':False},'nearest_object':{'type':'barrel','distance_m':1.5},'incoming_projectile':False,'previous_action':'none'}
with urllib.request.urlopen('http://127.0.0.1:8000/health',timeout=10) as r:
    health=json.load(r)
assert health['status']=='ok' and 'english' in health['loaded'], health
payload=json.dumps({'model':'english','state':state,'questions':questions}).encode()
t=time.perf_counter()
with urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:8000/v1/systemone',data=payload,headers={'Content-Type':'application/json'}),timeout=30) as r:
    result=json.load(r)
action=result['answers']['action']['choice']
assert action in questions['action']['criteria'],result
report={'health':health,'action':action,'latency_ms':round((time.perf_counter()-t)*1000,1),'response':result}
print(json.dumps(report,indent=2))
(root/'validation/laya-smoke.json').write_text(json.dumps(report,indent=2)+'\n')
