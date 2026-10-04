#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Playground.Editor {
[InitializeOnLoad] public static class AIDecisionValidationLauncher {
 static AIDecisionValidationLauncher(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PG.AIDecisions",false))new GameObject("AI decision validation").AddComponent<AIDecisionValidation>();};}
 public static void Run(){SessionState.SetBool("PG.AIDecisions",true);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");EditorApplication.isPlaying=true;}
}
public class AIDecisionValidation:MonoBehaviour {
 void Check(bool ok,string name){File.AppendAllText("validation/laya-improvement/checks.txt",(ok?"PASS ":"FAIL ")+name+"\n");if(!ok){Debug.LogError("AI FAIL: "+name);EditorApplication.Exit(1);throw new Exception(name);}Debug.Log("AI PASS: "+name);}
 IEnumerator Start(){
  File.WriteAllText("validation/laya-improvement/checks.txt","");yield return new WaitForSeconds(.8f);var a=Arena.Instance;var self=a.AI;var enemy=a.Human;
  a.Brain.Enabled=a.LeftBrain.Enabled=false;a.Client.CancelPending();a.LeftClient.CancelPending();a.StopExecutors("validation");a.Match.Practice=true;self.Active=enemy.Active=true;enemy.GetComponent<HumanInput>().enabled=false;
  a.ResetArena();self.ResetAt(new Vector3(0,.1f,0));enemy.ResetAt(new Vector3(0,.1f,5));yield return new WaitForSeconds(.1f);
  var context=a.Executor.ObserveContext();Check(context.HasCover,"cover observation has a reachable hiding position");Check(Physics.Linecast(enemy.transform.position+Vector3.up,context.Cover+Vector3.up,1<<8),"observed cover actually blocks enemy sight");
  var snapshot=AIPerception.Capture(a.Executor,context);Check(snapshot.situation.Contains("empty handed")&&snapshot.situation.Contains("meters away"),"readable context describes observed combat facts");
  Check(!StateSchema.Project(snapshot,new[]{"self.grounded"}).Contains("situation"),"ablations can omit the derived situation entirely");
  self.ResetAt(new Vector3(9.7f,.1f,0));enemy.ResetAt(new Vector3(7,.1f,0));Check(a.Executor.RetreatTarget().x<=9.2f,"retreat destination stays inside navigable roof margin");Check(AIPerception.Describe(a.Executor,context).Contains("dangerously close"),"edge danger is described explicitly");
  a.Match.Practice=false;a.Match.TimedRounds=true;a.Match.Phase=RoundPhase.Fight;a.Match.FightStarted=Time.time-90;Check(AIPerception.Describe(a.Executor,context).Contains("outside the safe boundary"),"shrinking boundary appears in model context");a.Match.Practice=true;a.Match.TimedRounds=false;
  a.ResetArena();self.ResetAt(new Vector3(0,.1f,0));enemy.ResetAt(new Vector3(0,.1f,5));yield return new WaitForSeconds(.1f);
  var projectile=Arena.Props.First(p=>p.Kind=="crate");projectile.Body.position=new Vector3(0,1,4);projectile.Body.linearVelocity=Vector3.back*15;projectile.Owner=enemy;Physics.SyncTransforms();Check(AIPerception.Danger(self),"collision-course projectile is detected");
  projectile.Body.position=new Vector3(3,1,4);Physics.SyncTransforms();Check(!AIPerception.Danger(self),"projectile passing to the side is not an imminent threat");projectile.Body.position=new Vector3(0,1,4);projectile.Owner=self;Physics.SyncTransforms();Check(!AIPerception.Danger(self),"own outgoing projectile does not trigger defensive decisions");
  a.ResetArena();self.ResetAt(new Vector3(0,.1f,0));enemy.ResetAt(new Vector3(0,.1f,5));projectile.Body.position=new Vector3(0,1,1.4f);projectile.transform.position=projectile.Body.position;Physics.SyncTransforms();yield return new WaitForSeconds(.1f);Check(self.Grab(projectile),"aim test can acquire weapon");
  enemy.Body.linearVelocity=Vector3.zero;var straight=a.Executor.ThrowAim();enemy.Body.linearVelocity=Vector3.right*3;var lead=a.Executor.ThrowAim();Check(lead.x>straight.x+.7f,"throw leads visible lateral movement through windup and flight");Check(lead.y>-.5f,"throw compensates gravity");
  self.SnapFace(Vector3.forward);enemy.Move=Vector3.right*.5f;a.Executor.Begin(SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT,a.Executor.ObserveContext());float hitDeadline=Time.time+1.4f;while(enemy.LastAttacker!=self&&Time.time<hitDeadline)yield return null;Check(enemy.LastAttacker==self,"selected throw physically hits a moving opponent");enemy.Move=Vector3.zero;a.Executor.Finish(false,"validation");
  a.ResetArena();self.ResetAt(new Vector3(0,.1f,0));enemy.ResetAt(new Vector3(0,.1f,5));yield return new WaitForSeconds(.1f);
  context=a.Executor.ObserveContext();a.Executor.Begin(SemanticAction.RETREAT_FROM_OPPONENT,context);float started=a.Executor.Started;yield return new WaitForSeconds(.2f);Check(a.Executor.CanContinue(SemanticAction.RETREAT_FROM_OPPONENT,a.Executor.ObserveContext())&&a.Executor.Started==started,"same model movement can continue without resetting its execution window");Check(!a.Executor.CanContinue(SemanticAction.APPROACH_OPPONENT,context),"a different model action cannot continue the old movement");a.Executor.Finish(false,"validation");
  a.ResetArena();self.ResetAt(new Vector3(0,.1f,0));enemy.ResetAt(new Vector3(0,.1f,4));a.Match.Phase=RoundPhase.Fight;a.Brain.Enabled=true;yield return null;a.Executor.Begin(SemanticAction.RETREAT_FROM_OPPONENT,a.Executor.ObserveContext());enemy.ResetAt(new Vector3(0,.1f,1.5f));yield return null;yield return null;Check(a.Executor.Running,"enemy entering melee range does not cancel an escape movement");a.Brain.Enabled=false;a.Executor.Finish(false,"validation");
  var profile=a.Experiments.Defaults();Check(profile.max_decisions_per_second==4,"fresh default has four decisions per second");
  var tenActions=profile.actions.Where(x=>!ExperimentSettings.Added.Contains(x)).ToArray();var legacy=profile.Copy();legacy.actions=tenActions;legacy.max_decisions_per_second=2;legacy.state_fields=StateSchema.Paths.Where(x=>x!="situation"&&x!=ExperimentSettings.AddedField).ToArray();legacy.questions=JsonUtility.FromJson<QuestionFile>(File.ReadAllText("validation/laya-improvement/before-question.json"));Check(ExperimentSettings.IsLegacyDefault(legacy),"untouched factory profile upgrades");
  legacy.questions.action.criteria.JUMP="Custom jumping criterion";Check(!ExperimentSettings.IsLegacyDefault(legacy),"custom criteria survive factory migration");legacy.questions=profile.questions;legacy.condition_id="custom";Check(!ExperimentSettings.IsLegacyDefault(legacy),"named experiments are never upgraded implicitly");
  var previous=profile.Copy();previous.actions=tenActions;previous.state_fields=profile.state_fields.Where(x=>x!=ExperimentSettings.AddedField).ToArray();var withCutOff=profile.Copy();withCutOff.actions=withCutOff.actions.Append("CUT_OFF_OPPONENT").ToArray();Check(profile.actions.Contains("ATTACK_OPPONENT")&&!profile.actions.Contains("CUT_OFF_OPPONENT")&&withCutOff.Validate()==null&&ExperimentSettings.IsPreviousFactoryDefault(previous,profile)&&!ExperimentSettings.IsPreviousFactoryDefault(profile,profile),"the untouched previous default gains the attack action and observation; cut-off is opt-in");
  previous.questions.action.criteria.PUSH_OPPONENT="My own push wording";Check(!ExperimentSettings.IsPreviousFactoryDefault(previous,profile),"an edited previous profile is left exactly as saved");
  a.Match.Phase=RoundPhase.Fight;self.Active=enemy.Active=true;a.ResetArena();self.ResetAt(new Vector3(0,.1f,0));enemy.ResetAt(new Vector3(0,.1f,5));yield return new WaitForSeconds(.1f);Check(ActionSchema.Available(a.Executor,a.Executor.ObserveContext()).Contains("ATTACK_OPPONENT"),"attack is offered with the shove ready");
  a.Executor.Begin(SemanticAction.ATTACK_OPPONENT,a.Executor.ObserveContext());bool closed=false;float limit=Time.time+3.5f;while(a.Executor.Running&&Time.time<limit){closed|=a.Executor.Striking;yield return null;}
  Check(closed&&a.Executor.Outcome=="push"&&a.Executor.Success&&enemy.LastAttacker==self,"one attack decision closes the gap and lands the shove");
  a.ResetArena();var crate=Arena.Props.Find(x=>x.Kind=="crate");self.ResetAt(crate.transform.position+Vector3.back*1.4f-Vector3.up*.5f);yield return new WaitForSeconds(.1f);self.Grab(crate);Check(self.Held&&!ActionSchema.Available(a.Executor,a.Executor.ObserveContext()).Contains("ATTACK_OPPONENT"),"holding a prop, the attack is a throw instead");
  a.ResetArena();self.ResetAt(new Vector3(4,.1f,6));enemy.ResetAt(new Vector3(0,.1f,7.5f));yield return new WaitForSeconds(.1f);var cut=a.Executor.CutOffPoint();Check(Mathf.Abs(cut.x)<.1f&&Mathf.Abs(cut.z-5.6f)<.15f,"cut-off point sits on the enemy's roof-center side");
  a.Executor.Begin(SemanticAction.CUT_OFF_OPPONENT,a.Executor.ObserveContext());limit=Time.time+3;while(a.Executor.Running&&Time.time<limit)yield return null;var after=AIPerception.Capture(a.Executor,a.Executor.ObserveContext());
  Check(a.Executor.Outcome=="arrived"&&after.opponent.edge_behind_m<3.5f,"cutting off leaves little roof behind the enemy");
  self.ResetAt(new Vector3(0,.1f,6.6f));enemy.ResetAt(new Vector3(0,.1f,8.5f));yield return new WaitForSeconds(.1f);var reading=AIPerception.Capture(a.Executor,a.Executor.ObserveContext());Check(Mathf.Abs(reading.opponent.edge_behind_m-1.5f)<.2f&&reading.situation.Contains("toward the edge"),"observation reports roof left behind the enemy");
  SessionState.SetBool("PG.AIDecisions",false);EditorApplication.Exit(0);
 }
}
}
#endif
