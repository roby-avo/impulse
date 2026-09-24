#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
namespace Playground.Editor {
public class MatchupValidation:MonoBehaviour {
 const string Fixture="http://127.0.0.1:8766",Key="fixture-test-key";Arena a;int checks;
 void Check(bool ok,string label){if(!ok){Debug.LogError("MATCHUP FAIL: "+label);File.AppendAllText("validation/matchups.txt","FAIL "+label+"\n");EditorApplication.Exit(1);throw new Exception(label);}checks++;Debug.Log("MATCHUP PASS: "+label);File.AppendAllText("validation/matchups.txt","PASS "+label+"\n");}
 void Remote(string suffix=""){a.Client.Configure(AIProvider.TypeSafe,"jev-fixture",Key);a.Client.Endpoint=Fixture+suffix;}
 IEnumerator Start(){File.WriteAllText("validation/matchups.txt","");yield return new WaitForSeconds(.6f);a=Arena.Instance;a.Brain.Enabled=false;a.LeftBrain.Enabled=false;
  string answer="{\"model\":\"jev-test\",\"answers\":{\"action\":{\"type\":\"choice\",\"choice\":\"JUMP\",\"confidence\":0.9}}}";
  Check(LayaClient.Parse(answer,new[]{"JUMP"},true).Valid,"typed choice accepted");
  Check(!LayaClient.Parse(answer,new[]{"PUSH_OPPONENT"},true).Valid,"out-of-schema choice rejected");
  Check(!LayaClient.Parse(answer.Replace(",\"confidence\":0.9",""),new[]{"JUMP"},true).Valid,"missing confidence rejected");
  Check(!LayaClient.Parse(answer.Replace("0.9","2"),new[]{"JUMP"},true).Valid,"out-of-range confidence rejected");
  Check(!LayaClient.Parse(answer.Replace("\"type\":\"choice\",",""),new[]{"JUMP"},true).Valid,"wrong answer type rejected");
  string[] models=null;string error=null;yield return LayaClient.DiscoverModels(Key,(m,e)=>{models=m;error=e;},Fixture);Check(error==null&&models.SequenceEqual(new[]{"jev-fixture"}),"authenticated model discovery uses official contract");
  yield return LayaClient.DiscoverModels("bad-key",(m,e)=>{models=m;error=e;},Fixture);Check(models==null&&error.Contains("API key rejected")&&!error.Contains(Key),"bad key is actionable and redacted");
  Check(!a.Setup.Configure(MatchMode.HumanVsTypeSafe,"jev-fixture",""),"cloud matchup rejects missing credentials");
  Remote();DecisionResult result=null;yield return a.Client.Decide(AIPerception.Capture(a.Executor,a.Executor.ObserveContext()),r=>result=r);Check(result.Valid&&result.ResponseModel=="jev-fixture-resolved"&&!result.Raw.Contains(Key),"cloud request authenticated, model selected, response credential redacted");
  Remote("/unauthorized");yield return a.Client.Decide(AIPerception.Capture(a.Executor,a.Executor.ObserveContext()),r=>result=r);Check(!result.Valid&&a.Client.Blocked&&!a.Client.ReadyToRequest&&result.Raw==null,"401 blocks retries and drops error body");
  Remote("/rate");yield return a.Client.Decide(AIPerception.Capture(a.Executor,a.Executor.ObserveContext()),r=>result=r);Check(!result.Valid&&!a.Client.Blocked&&!a.Client.ReadyToRequest,"429 enters timed backoff");yield return new WaitForSecondsRealtime(3.1f);Check(a.Client.ReadyToRequest,"429 retries become eligible after delay");
  Check(a.Setup.Configure(MatchMode.HumanVsLaya)&&!a.Spectating&&!a.LeftBrain.Enabled&&a.Client.Provider==AIProvider.Laya,"Human vs Laya mapping");
  Check(a.Setup.Configure(MatchMode.HumanVsTypeSafe,"jev-fixture",Key)&&!a.Spectating&&!a.LeftBrain.Enabled&&a.RightName=="TypeSafe","Human vs TypeSafe mapping");
  Check(a.Setup.Configure(MatchMode.LayaVsTypeSafe,"jev-fixture",Key)&&a.Spectating&&a.LeftBrain.Enabled&&a.LeftName=="Laya"&&a.RightName=="TypeSafe","Laya vs TypeSafe mapping");a.Client.Endpoint=Fixture;
  a.Human.GetComponent<HumanInput>().Capture(true);Check(!a.Human.GetComponent<HumanInput>().Captured,"spectator disables player control and unlocks cursor");
  var original=a.Experiments.Current;var p=a.Experiments.Defaults();p.experiment_id="matchup-validation";p.actions=new[]{"APPROACH_OPPONENT","PUSH_OPPONENT","MOVE_TOWARD_SAFETY"};p.max_decisions_per_second=2;a.Experiments.Apply(p,false);a.Match.NewMatch();int lb=a.LeftBrain.ExecutedDecisions,rb=a.Brain.ExecutedDecisions;float deadline=Time.time+30;while((a.LeftBrain.ExecutedDecisions<lb+2||a.Brain.ExecutedDecisions<rb+2)&&Time.time<deadline)yield return null;
  Check(a.LeftBrain.ExecutedDecisions>=lb+2&&a.Brain.ExecutedDecisions>=rb+2,"independent real-Laya and fixture-TypeSafe loops execute");
  a.Brain.Enabled=a.LeftBrain.Enabled=false;while(a.Client.Busy||a.LeftClient.Busy)yield return null;a.StopExecutors("validation");a.Telemetry.Flush();var records=File.ReadAllLines(a.Telemetry.FilePath);var decisions=records.Where(x=>x.Contains("\"type\":\"decision\"")).Select(x=>JsonUtility.FromJson<DecisionRecord>(x)).ToArray();
  Check(decisions.Any(d=>d.actor=="Laya"&&d.provider=="Laya"&&d.model=="english")&&decisions.Any(d=>d.actor=="TypeSafe"&&d.provider=="TypeSafe"&&d.response_model=="jev-fixture-resolved"),"per-player telemetry identifies provider and served model");Check(decisions.Any(d=>d.actor=="Laya"&&!string.IsNullOrEmpty(d.inference_device)&&d.inference_ms>0),"real Laya records device and server inference timing");Check(decisions.Select(d=>d.decision_id).Distinct().Count()==decisions.Length,"dual controllers have unique decision IDs");Check(!string.Join("",records).Contains(Key),"credentials absent from complete match log");Check(a.Telemetry.StatsFor("Laya").executed_decisions>=2&&a.Telemetry.StatsFor("TypeSafe").executed_decisions>=2,"separate per-player distributions and latency");
  a.Match.WinsRequired=1;a.Match.RingOut(a.AI);yield return null;yield return null;yield return new WaitForSeconds(2.4f);Check(a.Match.Phase==RoundPhase.MatchOver&&a.Match.Announcement=="LAYA WINS THE MATCH","AI-vs-AI match ends with correct winner");a.Match.NewMatch();yield return new WaitForSeconds(2.6f);a.Match.RingOut(a.Human);yield return null;yield return null;yield return new WaitForSeconds(2.4f);Check(a.Match.Phase==RoundPhase.MatchOver&&a.Match.Announcement=="TYPESAFE WINS THE MATCH","opposite AI winner and rematch score reset");
  a.Match.WinsRequired=5;a.Match.NewMatch();Remote("/slow");a.Brain.Enabled=true;deadline=Time.time+10;while(!a.Client.Busy&&Time.time<deadline)yield return null;Check(a.Client.Busy,"request in flight before matchup switch");yield return new WaitForSecondsRealtime(.15f);Check(a.Client.RequestAge>=.1f&&a.Client.WaitStatus.Contains("Deciding"),"HUD reports elapsed in-flight time");string oldPath=a.Telemetry.FilePath;int executed=a.Brain.ExecutedDecisions;a.Setup.Configure(MatchMode.HumanVsLaya);Check(!a.Client.Status.Contains("deciding"),"cancelled request does not leave deciding status behind");a.Brain.Enabled=false;a.Match.NewMatch();while(a.Client.Busy)yield return null;yield return null;a.Telemetry.Flush();Check(a.Brain.ExecutedDecisions==executed&&!a.Executor.Running&&!a.LeftExecutor.Running,"switch aborts request without executing stale action");var old=File.ReadAllLines(oldPath).Where(x=>x.Contains("\"type\":\"decision\"")).Select(x=>JsonUtility.FromJson<DecisionRecord>(x));Check(old.Any(d=>d.actor=="TypeSafe"&&d.provider=="TypeSafe"&&d.execution_status=="stale"),"cancelled response keeps old match and actor provenance");
  a.Setup.Show();Check(a.Setup.Open&&a.HUD.Paused&&Time.timeScale==0,"setup pauses game");a.HUD.Pause(false);Check(a.HUD.Paused,"cannot bypass initial setup by unpausing");var field=a.HUD.Root.Q<TextField>("typesafe-key");Check(field.isPasswordField,"API key UI is masked");a.HUD.Root.Q<DropdownField>("matchup").value="Human vs TypeSafe";Check(!a.HUD.Root.Q<Button>("start-match").enabledSelf,"unverified cloud key cannot start paid requests");
  a.Telemetry.Flush();string report=MatchExport.Export(oldPath);string csv=File.ReadAllText(Path.Combine(Path.GetDirectoryName(report),"decisions.csv"));Check(csv.Contains("actor,provider,model,response_model")&&!csv.Contains(Key),"exports include actor provenance and exclude credentials");File.Copy(oldPath,"validation/matchup-sample.jsonl",true);
  a.Experiments.Apply(original,false);SessionState.SetBool("PG.Matchups",false);Debug.Log("MATCHUP COMPLETE: "+checks+" checks; TypeSafe used HTTP fixture, Laya used real model.");EditorApplication.Exit(0);
 }
}
}
#endif
