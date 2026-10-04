"""Hand-authored situations sent to real local Laya; example sets are judgment calls, not a benchmark.
Usage: python3 scripts/probe-opponent-actions.py [question.json] [--with-cut-off]"""
import copy,json,sys,urllib.request
args=[a for a in sys.argv[1:] if not a.startswith('--')]
q=json.load(open(args[0] if args else 'Assets/StreamingAssets/laya-question.json'))
if '--with-cut-off' not in sys.argv: q['action']['criteria'].pop('CUT_OFF_OPPONENT',None)  # opt-in, as in the default profile
def state(sit,self={},opp={},**kw):
    s={'situation':sit,'self':dict(distance_to_edge_m=5,speed=0,balance=1,push_ready_in=0,dodge_ready_in=0,near_edge=False,holding_object=False,grounded=True,held_object_type='none'),
       'opponent':dict(distance_m=4,distance_to_edge_m=5,speed=1,near_edge=False,holding_object=False,facing_self=True,held_object_type='none',winding_up=False,recovering=False,edge_behind_m=6),
       'nearest_object':dict(type='crate',id='crate-0',mass_class='light',distance_m=3),'incoming_projectile':False,'cover_available':True,'sudden_death':False,'previous_action':'none','previous_outcome':'idle'}
    s['self'].update(self);s['opponent'].update(opp);s.update(kw);return s
cases=[
 ('rush: enemy far, little roof behind',state("I am empty handed. The enemy is 5.0 meters away. The enemy is near an open edge. A shove now would push the enemy toward the edge, only 1.2 meters behind them.",opp=dict(distance_m=5,distance_to_edge_m=1.2,near_edge=True,edge_behind_m=1.2)),['ATTACK_OPPONENT','APPROACH_OPPONENT'],['PUSH_OPPONENT']),
 ('in reach, little roof behind',state("I am empty handed. The enemy is within pushing range. The enemy is near an open edge. A shove now would push the enemy toward the edge, only 1.0 meters behind them.",opp=dict(distance_m=1.9,distance_to_edge_m=1,near_edge=True,edge_behind_m=1)),['PUSH_OPPONENT','ATTACK_OPPONENT'],[]),
 ('enemy near edge but I am on edge side',state("I am empty handed. The enemy is 3.0 meters away. The enemy is near an open edge. I am not between the enemy and the roof center.",opp=dict(distance_m=3,distance_to_edge_m=1.5,near_edge=True,edge_behind_m=8)),['CUT_OFF_OPPONENT','ATTACK_OPPONENT','APPROACH_OPPONENT'],['PUSH_OPPONENT']),
 ('open center fight',state("I am empty handed. The enemy is 4.0 meters away. The nearest throwable weapon is 3.0 meters away.",opp=dict(edge_behind_m=6)),['ATTACK_OPPONENT','APPROACH_OPPONENT','GRAB_NEAREST_OBJECT','CUT_OFF_OPPONENT'],['PUSH_OPPONENT']),
 ('edge pressure',state("I am dangerously close to falling off the roof. I am empty handed. The enemy is within pushing range.",self=dict(near_edge=True,distance_to_edge_m=.7),opp=dict(distance_m=2,edge_behind_m=9)),['MOVE_TOWARD_SAFETY','DODGE_LEFT','DODGE_RIGHT'],[]),
 ('incoming throw',state("A fast thrown object is flying toward me and will hit me soon. I am empty handed. The enemy is 5.0 meters away.",opp=dict(distance_m=5),incoming_projectile=True),['DODGE_LEFT','DODGE_RIGHT','TAKE_COVER','JUMP'],['PUSH_OPPONENT']),
 ('enemy winding up close',state("The enemy is winding up an attack that will hit me soon. I am empty handed. The enemy is within pushing range.",opp=dict(distance_m=1.7,winding_up=True,edge_behind_m=6)),['DODGE_LEFT','DODGE_RIGHT','RETREAT_FROM_OPPONENT'],[]),
 ('held barrel at range',state("I am holding a barrel. The enemy is 5.0 meters away. There is a clear throwing lane.",self=dict(holding_object=True,held_object_type='barrel'),opp=dict(distance_m=5)),['THROW_HELD_OBJECT_AT_OPPONENT'],['GRAB_NEAREST_OBJECT','ATTACK_OPPONENT','PUSH_OPPONENT']),
]
good=0
for name,s,ok,remove in cases:
    qq=copy.deepcopy(q);c=qq['action']['criteria']
    for k in remove: c.pop(k,None)
    if s['self']['holding_object']: c.pop('GRAB_NEAREST_OBJECT',None)
    else: c.pop('THROW_HELD_OBJECT_AT_OPPONENT',None)
    r=json.load(urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:8000/v1/systemone',json.dumps({'model':'english','state':s,'questions':qq}).encode(),{'Content-Type':'application/json'}),timeout=30))
    a=r['answers']['action'];probs=sorted(a.get('probabilities',{}).items(),key=lambda x:-x[1])[:3]
    hit=a['choice'] in ok;good+=hit
    print(f"{'OK ' if hit else '-- '}{name:38s} -> {a['choice']:30s} top3={[(k[:12],round(v,2)) for k,v in probs]}")
print(f'{good}/{len(cases)} match the example sets')
