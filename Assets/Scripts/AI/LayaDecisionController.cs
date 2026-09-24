using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace Playground {
public class LayaDecisionController:MonoBehaviour {
 public bool Enabled=true;public int ExecutedDecisions,StaleResponses;public float RetryDelay=.7f,DecisionGap=.12f;
 public AIActionExecutor Executor;public LayaClient Client;
 Arena a;DecisionRecord active;int sequence;float nextRequest;bool nearEdge,danger,pushable;
 void Start(){a=Arena.Instance;if(!Executor)Executor=a.Executor;if(!Client)Client=a.Client;Executor.Completed+=OnCompleted;StartCoroutine(Loop());}
 bool CanDecide=>Enabled&&Client.ReadyToRequest&&!a.HUD.Paused&&a.Match.Phase==RoundPhase.Fight&&!a.Match.Practice&&!a.GetComponent<ExecutorDebugPanel>().Open;
 IEnumerator Loop(){while(true){if(!CanDecide||Executor.Running||Time.time<nextRequest||!a.Experiments.Ready){yield return null;continue;}
   var profile=a.Experiments.Current;nextRequest=Time.time+1/profile.max_decisions_per_second;
   int generation=a.Match.Generation;float requestedGameTime=Time.time;var context=Executor.ObserveContext();var snapshot=AIPerception.Capture(Executor,context);var allowed=ActionSchema.Available(Executor,context).Where(x=>profile.actions.Contains(x)).ToArray();int round=a.Match.Round;string match=a.Telemetry.MatchId,actor=Executor.Self.Actor,provider=Client.Provider.ToString(),model=Client.Model;DecisionResult result=null;
   if(allowed.Length==0){Client.Status="No legal actions in this profile · neutral wait";a.Telemetry.LogEvent("no_legal_actions",Executor.Self.Actor,"neutral_wait");yield return new WaitForSeconds(RetryDelay);continue;}
   string stateJson=StateSchema.Project(snapshot,profile.state_fields),questionsJson=ActionSchema.Build(profile.questions,allowed);
   yield return Client.Decide(snapshot,r=>result=r,allowed,stateJson,questionsJson);
   var record=new DecisionRecord{actor=actor,provider=provider,model=model,response_model=result.ResponseModel,inference_device=result.Device,inference_ms=result.InferenceMs,experiment_id=profile.experiment_id,condition_id=profile.condition_id,profile_hash=profile.Hash(),request_state_json=stateJson,questions_json=questionsJson,match_id=match,round_id=round,decision_id=match+"-"+actor+"-"+(++sequence),game_time=requestedGameTime,state_snapshot=snapshot,available_actions=allowed,request_timestamp=result.RequestedAt,response_timestamp=result.RespondedAt,latency_ms=result.LatencyMs,confidence=result.Confidence,selected_action=result.Valid?result.Action.ToString():null,raw_response=result.Raw};
   string staleReason=!Enabled||a.HUD.Paused||a.Match.Phase!=RoundPhase.Fight||a.Match.Practice||generation!=a.Match.Generation?"match_or_control_changed":Time.time<Executor.Self.StunnedUntil?"knockback_during_inference":!snapshot.self.near_edge&&Executor.Self.Edge<1.4f?"near_edge_during_inference":!snapshot.incoming_projectile&&AIPerception.Danger(Executor.Self)?"projectile_during_inference":null;
   if(staleReason!=null){record.execution_status="stale";record.interruption_reason=staleReason;StaleResponses++;if(generation==a.Match.Generation)Client.Status="Reply outdated · waiting for fresh state";a.Telemetry.Write(record);yield return new WaitForSeconds(DecisionGap);continue;}
   if(!result.Valid){record.execution_status="request_error";record.interruption_reason=result.Error;record.resulting_state=AIPerception.Capture(Executor,Executor.ObserveContext());a.Telemetry.Write(record);yield return new WaitForSeconds(RetryDelay);continue;}
   // The only production Begin() call uses the validated provider answer unchanged.
   record.execution_status="executed";a.Telemetry.Count(result,actor);active=record;active.action_start=Time.time;ExecutedDecisions++;Executor.Begin(result.Action,context);
   yield return new WaitForSeconds(DecisionGap);
  }}
 void Update(){if(!a||!Enabled||a.HUD.Paused||a.Match.Phase!=RoundPhase.Fight)return;bool nowEdge=Executor.Self.Edge<1.4f;bool nowDanger=AIPerception.Danger(Executor.Self);bool nowPush=Vector3.Distance(Executor.Self.transform.position,Executor.Opponent.transform.position)<1.8f;
  if(Executor.Running){if(nowEdge&&!nearEdge)Executor.Finish(false,"sudden_near_edge");else if(nowDanger&&!danger)Executor.Finish(false,"incoming_projectile");else if(nowPush&&!pushable)Executor.Finish(false,"opponent_in_push_range");}
  nearEdge=nowEdge;danger=nowDanger;pushable=nowPush;
 }
 void OnCompleted(AIActionExecutor executor){if(active==null)return;active.action_end=Time.time;active.execution_success=executor.Success;active.interruption_reason=executor.Outcome;active.resulting_state=AIPerception.Capture(executor,executor.ObserveContext());a.Telemetry.Write(active);active=null;}
 void OnDestroy(){if(a&&Executor)Executor.Completed-=OnCompleted;}
}
}
