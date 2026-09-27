#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Playground.Editor {
public class PlayValidation:MonoBehaviour {
 void Check(bool ok,string label){if(!ok){Debug.LogError("VALIDATION FAILED: "+label);File.AppendAllText("validation/results.txt","FAIL "+label+"\n");EditorApplication.Exit(1);throw new System.Exception(label);}Debug.Log("PASS "+label);File.AppendAllText("validation/results.txt","PASS "+label+"\n");}
 IEnumerator Start(){yield return new WaitForSeconds(.6f);var a=Arena.Instance;var f=a.Human;a.Brain.Enabled=false;a.Match.Practice=true;a.Human.Active=a.AI.Active=true;f.GetComponent<HumanInput>().enabled=false;
 Check(f.Grounded,"M1 human grounded");var start=f.transform.position;f.Move=Vector3.forward;yield return new WaitForSeconds(.4f);Check(f.transform.position.z>start.z+1,"M1 movement");f.Move=Vector3.zero;
 Check(f.Jump(),"M1 jump accepted");yield return new WaitForSeconds(.15f);Check(f.transform.position.y>start.y+.5f,"M1 jump rises");yield return new WaitForSeconds(1.5f);
 Check(f.Dodge(Vector3.right),"M1 dodge");yield return new WaitForSeconds(.5f);
 var barrel=Arena.Props.Find(p=>p.Kind=="barrel");f.ResetAt(barrel.transform.position+Vector3.back*1.5f-Vector3.up*.5f);yield return new WaitForSeconds(.1f);Check(f.Grab(barrel),"M1 grab barrel");yield return new WaitForFixedUpdate();Check(barrel.Body.isKinematic,"M1 held prop");Check(f.Release(true,Vector3.forward),"M1 throw");yield return new WaitForSeconds(.42f);Check(barrel.Body.linearVelocity.magnitude>10,"M1 physical throw velocity");
 a.ResetArena();yield return new WaitForSeconds(.3f);var crate=Arena.Props.Find(p=>p.Kind=="crate");f.ResetAt(crate.transform.position+Vector3.back*1.3f-Vector3.up*.5f);f.SnapFace(Vector3.forward);yield return new WaitForFixedUpdate();Check(f.Push(),"M1 push prop");yield return new WaitForSeconds(.26f);Check(crate.Body.linearVelocity.magnitude>1,"M1 push impulse");f.ResetAt(new Vector3(12,1,0));yield return new WaitForSeconds(1.8f);Check(a.Resets>=2&&f.transform.position.y>-.2f,"M1 fall and reset");
 a.ResetArena();yield return new WaitForSeconds(.1f);Check(Arena.Props.TrueForAll(p=>!p.Holder&&!p.Body.isKinematic&&p.transform.position.y>0),"M1 props reset");
 File.AppendAllText("validation/results.txt","M1 COMPLETE\n");
 if(SessionState.GetInt("PG.Milestone",1)>=2){
  Check(a.AI.Body.mass==f.Body.mass,"M2 symmetric physical avatar");
  foreach(SemanticAction action in System.Enum.GetValues(typeof(SemanticAction))){a.ResetArena();yield return new WaitForSeconds(.3f);a.AI.ResetAt(new Vector3(0,.1f,2));f.ResetAt(new Vector3(0,.1f,0));
   if(action==SemanticAction.GRAB_NEAREST_OBJECT){var p=Arena.Props[0];p.Body.position=a.AI.transform.position+Vector3.right*1.4f;yield return new WaitForFixedUpdate();}
   if(action==SemanticAction.MOVE_TOWARD_SAFETY){a.AI.ResetAt(new Vector3(0,.1f,7));f.ResetAt(new Vector3(0,.1f,-5));}
   if(action==SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT){var p=Arena.Props[0];p.Body.position=a.AI.transform.position+Vector3.forward;yield return new WaitForFixedUpdate();Check(a.AI.Grab(p),"M2 throw setup");}
   a.Executor.Begin(action,a.Executor.ObserveContext());float deadline=Time.time+4;while(a.Executor.Running&&Time.time<deadline)yield return null;Check(!a.Executor.Running&&a.Executor.Outcome!="executing","M2 terminal outcome "+action+" / "+a.Executor.Outcome);if(action==SemanticAction.GRAB_NEAREST_OBJECT||action==SemanticAction.MOVE_TOWARD_SAFETY)Check(a.Executor.Success,"M2 successful execution "+action);
  }
  File.AppendAllText("validation/results.txt","M2 COMPLETE\n");
 }
 if(SessionState.GetInt("PG.Milestone",1)>=3){a.ResetArena();yield return new WaitForSeconds(.3f);DecisionResult result=null;yield return a.Client.Decide(AIPerception.Capture(a.Executor,a.Executor.ObserveContext()),r=>result=r);Check(result!=null&&result.Valid,"M3 real Laya action in Play Mode");Check(result.LatencyMs>0,"M3 measured asynchronous latency");File.WriteAllText("validation/unity-laya-response.json",result.Raw);File.AppendAllText("validation/results.txt","M3 COMPLETE\n");}
 if(SessionState.GetInt("PG.Milestone",1)>=4){
  a.Match.Practice=false;a.Match.NewMatch();a.Brain.Enabled=true;float deadline=Time.time+20;while(a.Brain.ExecutedDecisions<3&&Time.time<deadline)yield return null;Check(a.Brain.ExecutedDecisions>=3,"M4 autonomous real Laya loop");Check(System.Array.IndexOf(ActionSchema.Available(a.Executor,a.Executor.ObserveContext()),a.AI.Held?"GRAB_NEAREST_OBJECT":"THROW_HELD_OBJECT_AT_OPPONENT")<0,"M4 impossible interaction actions excluded");
  a.Brain.Enabled=false;while(a.Client.Busy)yield return null;a.Executor.Finish(false,"validation");
  a.Match.NewMatch();yield return new WaitForSeconds(2.6f);a.Match.RingOut(a.Human);a.Match.RingOut(a.AI);yield return null;yield return null;Check(a.Match.HumanWins==0&&a.Match.AIWins==0,"M4 simultaneous ring-out draw");
  a.Match.NewMatch();
  for(int round=0;round<5;round++){while(a.Match.Phase!=RoundPhase.Fight)yield return null;a.AI.ResetAt(new Vector3(12,.5f,0));float until=Time.time+5;while(a.Match.Phase==RoundPhase.Fight&&Time.time<until)yield return null;Check(a.Match.HumanWins==round+1,"M4 ring-out score round "+(round+1));yield return new WaitForSeconds(2.4f);}
  Check(a.Match.Phase==RoundPhase.MatchOver&&a.Match.HumanWins==5,"M4 first-to-five match completed");Check(File.Exists(a.Telemetry.FilePath)&&new FileInfo(a.Telemetry.FilePath).Length>0,"M4 JSONL telemetry persisted");
  a.Match.NewMatch();Check(a.Match.HumanWins==0&&a.Match.AIWins==0&&a.Human.Throws==0,"M4 clean rematch");
  a.Client.Endpoint="http://127.0.0.1:1";a.Client.TimeoutSeconds=1;int before=a.Brain.ExecutedDecisions;a.Brain.Enabled=true;yield return new WaitForSeconds(5);Check(a.Client.Failures>0&&a.Brain.ExecutedDecisions==before&&!a.Executor.Running,"M4 offline means wait, never fallback");
  File.AppendAllText("validation/results.txt","M4 COMPLETE\n");
 }
 if(SessionState.GetInt("PG.Milestone",1)>=5){Check(a.HUD&&a.Feedback&&a.Human.GetComponent<FighterPresentation>(),"M5 HUD, audio/VFX and animated fighters");a.HUD.Pause(true);Check(Time.timeScale==0,"M5 pause freezes simulation");a.HUD.Pause(false);Check(Time.timeScale==1,"M5 resume");Check(Resources.Load<Material>("Surface")&&Resources.Load<Material>("Particles"),"M5 shaders retained for build");File.AppendAllText("validation/results.txt","M5 AUTOMATED CHECKS COMPLETE\n");}
 if(SessionState.GetInt("PG.Milestone",1)>=6){
  Check(VisualAssets.SpawnedModels>=30,"M6 authored Blender models instantiated");
  Check(a.Human.GetComponentsInChildren<Collider>().Length==1&&a.AI.GetComponentsInChildren<Collider>().Length==1,"M6 visual meshes add no fighter colliders");
  Check(Arena.Props.TrueForAll(p=>p.GetComponentsInChildren<Collider>().Length==1),"M6 prop collision proxies preserved");
  Check(a.Human.GetComponentsInChildren<MeshFilter>().Length>20,"M6 detailed robot replaces primitives");
  var bounds=new Bounds(a.Human.transform.position,Vector3.zero);foreach(var r in a.Human.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
  Check(bounds.size.y>1.5f&&bounds.size.y<2.3f&&bounds.size.x<1.8f,"M6 meter scale and imported mesh bounds");
  File.AppendAllText("validation/results.txt","M6 COMPLETE\n");
 }
 if(SessionState.GetInt("PG.Milestone",1)>=7){
  a.Brain.Enabled=false;while(a.Client.Busy)yield return null;a.Executor.Finish(false,"validation");a.Client.Endpoint="http://127.0.0.1:8000";a.Client.TimeoutSeconds=8;
  var original=a.Experiments.Current;var profile=a.Experiments.Defaults();profile.experiment_id="validation-m7";profile.condition_id="restricted";profile.actions=new[]{"JUMP","MOVE_TOWARD_SAFETY"};profile.state_fields=new[]{"self.grounded","opponent.distance_m"};profile.max_decisions_per_second=.5f;profile.notes="</script><script>alert(1)</script>";
  Check(profile.Validate()==null,"M7 valid restricted profile");var bad=profile.Copy();bad.actions=new string[0];Check(bad.Validate()!=null,"M7 rejects empty action schema");bad=profile.Copy();bad.state_fields=new[]{"secret"};Check(bad.Validate()!=null,"M7 rejects unknown observation");bad=profile.Copy();bad.max_decisions_per_second=float.NaN;Check(bad.Validate()!=null,"M7 rejects nonfinite frequency");
  var projected=StateSchema.Project(AIPerception.Capture(a.Executor,a.Executor.ObserveContext()),profile.state_fields);Check(projected.Contains("grounded")&&projected.Contains("distance_m")&&!projected.Contains("near_edge")&&!projected.Contains("nearest_object"),"M7 observation projection omits unselected fields");
  a.Experiments.Apply(profile,false);a.Match.NewMatch();a.Brain.Enabled=true;int before=a.Brain.ExecutedDecisions;float deadline=Time.time+25;while(a.Brain.ExecutedDecisions<before+3&&Time.time<deadline)yield return null;a.Brain.Enabled=false;while(a.Client.Busy)yield return null;a.Executor.Finish(false,"validation");
  Check(a.Brain.ExecutedDecisions>=before+3,"M7 real Laya executes restricted schema");
  a.Telemetry.Flush();var records=new System.Collections.Generic.List<DecisionRecord>();foreach(var line in File.ReadAllLines(a.Telemetry.FilePath))if(JsonUtility.FromJson<EventRecord>(line).type=="decision")records.Add(JsonUtility.FromJson<DecisionRecord>(line));records.Sort((x,y)=>x.game_time.CompareTo(y.game_time));
  Check(records.Count>=3&&records.TrueForAll(d=>d.experiment_id=="validation-m7"&&d.profile_hash==profile.Hash()&&d.available_actions.Length<=2&&!d.request_state_json.Contains("near_edge")),"M7 exact request and experiment provenance");
  bool rate=true;for(int n=1;n<records.Count;n++)if(records[n].game_time-records[n-1].game_time<1.98f)rate=false;Check(rate,"M7 request-start frequency cap respected");
  a.Telemetry.Flush();string report=MatchExport.Export(a.Telemetry.FilePath);string html=File.ReadAllText(report);Check(File.Exists(report)&&File.Exists(Path.Combine(Path.GetDirectoryName(report),"decisions.csv"))&&html.Contains("SPATIAL REPLAY")&&!html.Contains("</script><script>alert(1)</script>"),"M7 offline report and CSV export with safe embedded data");
  Check(File.ReadAllText(a.Telemetry.FilePath).Contains("\"type\":\"frame\""),"M7 sampled spatial replay persisted");File.Copy(a.Telemetry.FilePath,"validation/m7-experiment.jsonl",true);File.Copy(report,"validation/m7-report.html",true);
  profile.actions=new[]{"THROW_HELD_OBJECT_AT_OPPONENT"};a.Experiments.Apply(profile,false);a.Match.NewMatch();before=a.Brain.ExecutedDecisions;int valid=a.Client.ValidResponses;a.Brain.Enabled=true;yield return new WaitForSeconds(5);Check(a.Brain.ExecutedDecisions==before&&a.Client.ValidResponses==valid&&!a.Executor.Running,"M7 mechanically empty schema waits without fallback or request");
  a.Brain.Enabled=false;profile.actions=new[]{"JUMP","MOVE_TOWARD_SAFETY"};profile.max_decisions_per_second=2;a.Experiments.Apply(profile,false);a.Match.NewMatch();a.Brain.Enabled=true;deadline=Time.time+15;while(!a.Client.Busy&&Time.time<deadline)yield return null;Check(a.Client.Busy,"M7 request in flight for rematch isolation");string oldLog=a.Telemetry.FilePath;string oldId=a.Telemetry.MatchId;var changed=profile.Copy();changed.condition_id="changed";a.Experiments.Apply(changed,false);a.Match.NewMatch();while(a.Client.Busy)yield return null;yield return null;a.Telemetry.Flush();bool isolated=false;foreach(var line in File.ReadAllLines(oldLog)){if(JsonUtility.FromJson<EventRecord>(line).type!="decision")continue;var decision=JsonUtility.FromJson<DecisionRecord>(line);if(decision.execution_status=="stale"&&decision.match_id==oldId&&decision.condition_id=="restricted")isolated=true;}Check(isolated,"M7 late response retains original match and experiment");
  a.Brain.Enabled=false;a.Experiments.Apply(original,false);a.Match.NewMatch();File.AppendAllText("validation/results.txt","M7 COMPLETE\n");
 }
 if(SessionState.GetInt("PG.Milestone",1)>=7){
  a.Brain.Enabled=false;Check(a.HUD.Root!=null&&a.GetComponents<UIDocument>().Length==1,"UI retained document initialized once");
  for(int n=0;n<3;n++){a.Research.Show();a.Research.SelectTab(n);int builds=a.Research.BuildCount;yield return new WaitForSecondsRealtime(.2f);Check(a.Research.BuildCount==builds,"UI lab does not rebuild during idle frames "+n);a.Research.Close();}
  a.Research.Show();a.Research.BeginExport(false);float deadline=Time.realtimeSinceStartup+10;while(a.Research.Exporting&&Time.realtimeSinceStartup<deadline)yield return null;Check(!a.Research.Exporting&&a.Research.ExportError==null&&File.Exists(a.Research.LastExportPath),"UI background export completes while paused");a.Research.Close();Check(Time.timeScale==1&&a.Human.GetComponent<HumanInput>().Captured,"UI resume restores time and input");
  Check(Resources.Load<Texture2D>("UI/AppIcon")!=null,"UI app icon asset loaded");File.AppendAllText("validation/results.txt","UI STABILITY COMPLETE\n");
 }
 SessionState.SetBool("PG.Validate",false);EditorApplication.Exit(0);
 }
}
}

#endif
