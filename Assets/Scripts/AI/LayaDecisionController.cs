using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace Playground {
public class LayaDecisionController:MonoBehaviour {
 public bool Enabled=true;public int ExecutedDecisions,StaleResponses;public float RetryDelay=.7f,DecisionGap=.12f;
 Arena a;DecisionRecord active;int sequence;float nextRequest;bool nearEdge,danger,pushable;
 void Start(){a=Arena.Instance;a.Executor.Completed+=OnCompleted;StartCoroutine(Loop());}
 bool CanDecide=>Enabled&&!a.HUD.Paused&&a.Match.Phase==RoundPhase.Fight&&!a.Match.Practice&&!a.GetComponent<ExecutorDebugPanel>().Open;
 IEnumerator Loop(){while(true){if(!CanDecide||a.Executor.Running||Time.time<nextRequest||!a.Experiments.Ready){yield return null;continue;}
   var profile=a.Experiments.Current;nextRequest=Time.time+1/profile.max_decisions_per_second;
   int generation=a.Match.Generation;float requestedGameTime=Time.time;var context=a.Executor.ObserveContext();var snapshot=AIPerception.Capture(a.Executor,context);var allowed=ActionSchema.Available(a.Executor,context).Where(x=>profile.actions.Contains(x)).ToArray();int round=a.Match.Round;string match=a.Telemetry.MatchId;DecisionResult result=null;
   if(allowed.Length==0){a.Client.Status="No legal actions in this profile · neutral wait";a.Telemetry.LogEvent("no_legal_actions","Laya","neutral_wait");yield return new WaitForSeconds(RetryDelay);continue;}
   string stateJson=StateSchema.Project(snapshot,profile.state_fields),questionsJson=ActionSchema.Build(profile.questions,allowed);
   yield return a.Client.Decide(snapshot,r=>result=r,allowed,stateJson,questionsJson);
   var record=new DecisionRecord{experiment_id=profile.experiment_id,condition_id=profile.condition_id,profile_hash=profile.Hash(),request_state_json=stateJson,questions_json=questionsJson,match_id=match,round_id=round,decision_id=match+"-"+(++sequence),game_time=requestedGameTime,state_snapshot=snapshot,available_actions=allowed,request_timestamp=result.RequestedAt,response_timestamp=result.RespondedAt,latency_ms=result.LatencyMs,confidence=result.Confidence,selected_action=result.Valid?result.Action.ToString():null,raw_response=result.Raw};
   if(!CanDecide||generation!=a.Match.Generation||Time.time<a.AI.StunnedUntil||(!snapshot.self.near_edge&&a.AI.Edge<1.4f)||(!snapshot.incoming_projectile&&AIPerception.Danger(a.AI))){record.execution_status="stale";record.interruption_reason="stale_response";StaleResponses++;a.Telemetry.Write(record);yield return new WaitForSeconds(DecisionGap);continue;}
   if(!result.Valid){record.execution_status="request_error";record.interruption_reason=result.Error;record.resulting_state=AIPerception.Capture(a.Executor,a.Executor.ObserveContext());a.Telemetry.Write(record);yield return new WaitForSeconds(RetryDelay);continue;}
   // The only production Begin() call uses the validated Laya answer unchanged.
   record.execution_status="executed";a.Telemetry.Count(result);active=record;active.action_start=Time.time;ExecutedDecisions++;a.Executor.Begin(result.Action,context);
   yield return new WaitForSeconds(DecisionGap);
  }}
 void Update(){if(!a||!CanDecide)return;bool nowEdge=a.AI.Edge<1.4f;bool nowDanger=AIPerception.Danger(a.AI);bool nowPush=Vector3.Distance(a.AI.transform.position,a.Human.transform.position)<1.8f;
  if(a.Executor.Running){if(nowEdge&&!nearEdge)a.Executor.Finish(false,"sudden_near_edge");else if(nowDanger&&!danger)a.Executor.Finish(false,"incoming_projectile");else if(nowPush&&!pushable)a.Executor.Finish(false,"opponent_in_push_range");}
  nearEdge=nowEdge;danger=nowDanger;pushable=nowPush;
 }
 void OnCompleted(AIActionExecutor executor){if(active==null)return;active.action_end=Time.time;active.execution_success=executor.Success;active.interruption_reason=executor.Outcome;active.resulting_state=AIPerception.Capture(executor,executor.ObserveContext());a.Telemetry.Write(active);active=null;}
 void OnDestroy(){if(a&&a.Executor)a.Executor.Completed-=OnCompleted;}
}
}
