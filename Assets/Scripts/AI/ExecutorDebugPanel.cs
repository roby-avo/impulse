using UnityEngine;
using UnityEngine.InputSystem;
namespace Playground {
public class ExecutorDebugPanel:MonoBehaviour {
 public AIActionExecutor Executor;public bool Open;
 void Update(){if(Arena.Instance.Setup.Open||Arena.Instance.Spectating||Arena.Instance.Research.Open)return;if(Keyboard.current!=null&&Keyboard.current.f1Key.wasPressedThisFrame){Open=!Open;Arena.Instance.Human.GetComponent<HumanInput>().Capture(!Open);Arena.Instance.Match.Practice=Open;Arena.Instance.Telemetry.LogEvent("debug", "developer", Open?"practice_start":"practice_end");if(Open){Executor.Finish(false,"debug_opened");Arena.Instance.Human.Active=Arena.Instance.AI.Active=true;}else Arena.Instance.Match.NewMatch();}}
 void OnGUI(){if(!Open||Arena.Instance.Research.Open)return;GUI.Box(new Rect(20,75,315,370),"EXECUTOR LAB · manual commands");int y=110;foreach(SemanticAction action in System.Enum.GetValues(typeof(SemanticAction))){if(GUI.Button(new Rect(30,y,295,25),action.ToString()))Executor.Begin(action,Executor.ObserveContext());y+=28;}GUI.Label(new Rect(30,y,295,40),Executor.Outcome);}
}
}
