"""Score, round length, Laya push hits and executed action mix across recorded matches."""
import json,sys,collections,statistics
tot=collections.Counter();rounds=[];pushes_laya=0;laya_hits=0;wins=collections.Counter()
for path in sys.argv[1:]:
    rows=[json.loads(l) for l in open(path) if l.strip()]
    starts={r['round_id']:r['game_time'] for r in rows if r['type']=='round_start'}
    rounds+=[r['game_time']-starts[r['round_id']] for r in rows if r['type']=='round_end' and r['round_id'] in starts]
    for r in rows:
        if r['type']=='decision' and r['execution_status']=='executed': tot[r['selected_action']]+=1
        if r['type'] in('avatar_event',) and r.get('actor')=='Laya' and r.get('action')=='push_hit': pushes_laya+=1
        if r['type']=='summary': wins['human']+=r['left_wins'];wins['laya']+=r['right_wins']
n=sum(tot.values())
print('matches',len(sys.argv)-1,'score',dict(wins),'mean round s',round(statistics.mean(rounds),1),'Laya push hits',pushes_laya,'executed',n)
for k,v in tot.most_common(): print(f'  {k:32s}{v:4d} {100*v/n:5.1f}%')
