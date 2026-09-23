#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
namespace Playground.Editor {
// Test-only scripted HUMAN inputs. The Laya opponent remains entirely model-controlled.
public class LiveMatchValidation:MonoBehaviour {
 IEnumerator Start(){yield return new WaitForSeconds(.6f);var a=Arena.Instance;var h=a.Human;h.GetComponent<HumanInput>().enabled=false;float deadline=Time.time+180;int round=0;var path=new NavMeshPath();
  while(a.Match.Phase!=RoundPhase.MatchOver&&Time.time<deadline){
   if(a.Match.Round!=round){round=a.Match.Round;Debug.Log($"LIVE MATCH round {round} score {a.Match.HumanWins}:{a.Match.AIWins}");}
   if(a.Match.Phase==RoundPhase.Fight){var delta=a.AI.transform.position-h.transform.position;var target=h.Edge<1.3f?Vector3.zero:a.AI.transform.position;
    Vector3 direction=Vector3.ProjectOnPlane(target-h.transform.position,Vector3.up).normalized;
    if(NavMesh.SamplePosition(h.transform.position,out var start,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(target,out var end,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)){foreach(var c in path.corners){var d=Vector3.ProjectOnPlane(c-h.transform.position,Vector3.up);if(d.magnitude>.5f){direction=d.normalized;break;}}}
    h.Move=direction;h.Face(delta);if(delta.magnitude<2.2f)h.Push();
   }else h.Move=Vector3.zero;
   yield return null;
  }
  bool ok=a.Match.Phase==RoundPhase.MatchOver&&a.Brain.ExecutedDecisions>=a.Match.WinsRequired;
  var report=$"{(ok?"PASS":"FAIL")} LIVE full physical match: Human {a.Match.HumanWins}, Laya {a.Match.AIWins}, {a.Brain.ExecutedDecisions} real model decisions, {a.Telemetry.TotalLatency/Mathf.Max(1,a.Telemetry.DecisionCount):0} ms average, {Time.time:0}s elapsed. Log: {a.Telemetry.FilePath}";
  Debug.Log(report);File.WriteAllText("validation/live-match.txt",report+"\n");SessionState.SetBool("PG.LiveMatch",false);EditorApplication.Exit(ok?0:1);
 }
}
}

#endif
