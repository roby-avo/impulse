"""Small real-model scenario probe, not a claim of general tactical strength."""
import copy,json,pathlib,time,urllib.request
root=pathlib.Path(__file__).resolve().parent.parent
baseline=json.loads((root/'validation/laya-benchmark-request.json').read_text())
questions=json.loads((root/'Assets/StreamingAssets/laya-question.json').read_text())
base=baseline['state'];base['self'].update(push_ready_in=0,dodge_ready_in=0);base['opponent'].update(winding_up=False,recovering=False);base['sudden_death']=False
cases=[]
def case(name,patch,expected):
 s=copy.deepcopy(base)
 for key,value in patch.items():
  if isinstance(value,dict):s[key].update(value)
  else:s[key]=value
 cases.append((name,s,expected))
case('edge pressure',{'self':{'near_edge':True,'distance_to_edge_m':.7},'opponent':{'distance_m':2}},['MOVE_TOWARD_SAFETY','DODGE_LEFT','DODGE_RIGHT'])
case('incoming throw',{'incoming_projectile':True,'opponent':{'distance_m':5,'holding_object':False}},['DODGE_LEFT','DODGE_RIGHT','TAKE_COVER','JUMP'])
case('close push windup',{'opponent':{'distance_m':1.7,'winding_up':True}},['DODGE_LEFT','DODGE_RIGHT','RETREAT_FROM_OPPONENT'])
case('punish recovery',{'opponent':{'distance_m':1.7,'recovering':True,'near_edge':True,'distance_to_edge_m':1}},['PUSH_OPPONENT'])
case('held barrel at range',{'self':{'holding_object':True,'held_object_type':'barrel'},'opponent':{'distance_m':5}},['THROW_HELD_OBJECT_AT_OPPONENT'])
case('sudden death outside boundary',{'self':{'near_edge':True,'distance_to_edge_m':-.2},'sudden_death':True},['MOVE_TOWARD_SAFETY'])
results=[]
for name,state,expected in cases:
 q=copy.deepcopy(questions);criteria=q['action']['criteria']
 if state['self']['holding_object']:criteria.pop('GRAB_NEAREST_OBJECT')
 else:criteria.pop('THROW_HELD_OBJECT_AT_OPPONENT')
 if state['opponent']['distance_m']>2.3:criteria.pop('PUSH_OPPONENT')
 payload=json.dumps({'model':'english','state':state,'questions':q}).encode();start=time.perf_counter()
 request=urllib.request.Request('http://127.0.0.1:8000/v1/systemone',data=payload,headers={'Content-Type':'application/json'})
 with urllib.request.urlopen(request,timeout=30) as response: result=json.load(response)
 action=result['answers']['action']['choice'];results.append({'scenario':name,'action':action,'reasonable_examples':expected,'matches_examples':action in expected,'latency_ms':round((time.perf_counter()-start)*1000),'state':state})
report={'note':'Six hand-authored states, one request each. Example actions are judgment calls; this is not a calibrated benchmark.','results':results}
(root/'validation/tactics-upgrade.json').write_text(json.dumps(report,indent=2)+'\n')
for r in results:print(r['scenario']+': '+r['action'])
