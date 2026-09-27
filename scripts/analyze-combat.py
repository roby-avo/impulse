"""Summarize a recorded match without claiming that stationary time is indecision."""
import collections, json, math, pathlib, sys
rows=[json.loads(line) for line in pathlib.Path(sys.argv[1]).read_text().splitlines() if line.strip()]
starts={r['round_id']:r['game_time'] for r in rows if r['type']=='round_start'}
rounds=[round(r['game_time']-starts[r['round_id']],2) for r in rows if r['type']=='round_end' and r['round_id'] in starts]
actors={}
for actor in sorted({r.get('actor') for r in rows if r['type']=='decision'}):
    decisions=[r for r in rows if r['type']=='decision' and r.get('actor')==actor]
    executed=[r for r in decisions if r['execution_status']=='executed']
    overlap=sum(any(p['action_start']<=r['game_time']<p['action_end'] and p['decision_id']!=r['decision_id'] for p in executed) for r in decisions)
    sampled=still=0
    for r in rows:
        if r['type']!='frame' or r['phase']!='Fight': continue
        for b in r['bodies']:
            if b['kind']=='fighter' and b['id']==actor:
                sampled+=1
                still+=math.hypot(b['velocity']['x'],b['velocity']['z'])<.25
    actors[actor]={'requests':len(decisions),'executed':len(executed),'stale':sum(r['execution_status']=='stale' for r in decisions),'requests_while_previous_action_running':overlap,'choices':dict(collections.Counter(r['selected_action'] for r in executed)),'stationary_frame_percent':round(100*still/max(1,sampled),1),'note':'Stationary samples include attacking, recovery and intentional waiting.'}
print(json.dumps({'round_fight_seconds':rounds,'actors':actors,'summary':next((r for r in reversed(rows) if r['type']=='summary'),None)},indent=2))
