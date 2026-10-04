using UnityEngine;
namespace Playground {
// Explicit, unscored training only. This instructor never controls a competitive AI.
public class TutorialDirector:MonoBehaviour {
 public bool Running {get;private set;}public int Stage {get;private set;}
 public string Instruction=>Stage switch {0=>"01 / PICK UP   •   Move to the glowing prop and press E",1=>"02 / THROW   •   Press F to throw. Your robot aims automatically",2=>"03 / EVADE   •   Watch its hands. SHIFT + A or D during the wind-up",3=>"04 / RING OUT   •   Move between the center and the robot, then press F",_=>"TRAINING COMPLETE   •   Ready for the rooftop"};
 Arena a;float nextPush,finishAt;bool savedLeft,savedRight;int dodgesAtWindup;bool checkingDodge;
 void Awake(){a=Arena.Instance;}
 public void Begin(){if(Running)return;savedLeft=a.LeftBrain.Enabled;savedRight=a.Brain.Enabled;a.Client.CancelPending();a.LeftClient.CancelPending();a.StopExecutors("tutorial");Stage=0;checkingDodge=false;a.Match.NewMatch();
  Running=true;a.Match.Practice=true;a.Match.ApplyRules(true);a.Match.Phase=RoundPhase.Fight;a.Brain.Enabled=a.LeftBrain.Enabled=false;a.Human.Active=a.AI.Active=true;a.ResetArena();a.AI.ResetAt(new Vector3(0,.15f,1));a.Human.Event+=OnHuman;a.Telemetry.LogEvent("tutorial_start","game","unscored_training");a.HUD.Pause(false);
 }
 public void Stop(bool newMatch=true){if(!Running)return;Running=false;a.Human.Event-=OnHuman;a.Match.Practice=false;a.Brain.Enabled=savedRight;a.LeftBrain.Enabled=savedLeft;a.StopExecutors("tutorial_end");a.Telemetry.LogEvent("tutorial_end","game",Stage>=4?"completed":"exited");if(newMatch)a.Match.NewMatch();}
 void OnHuman(string action){if(Stage==0&&action=="grab")Stage=1;else if(Stage==1&&action=="throw_hit"){Stage=2;a.Human.ResetAt(new Vector3(0,.15f,-1.5f));a.AI.ResetAt(new Vector3(0,.15f,0));nextPush=Time.time+1.8f;}}
 void Update(){if(!Running||a.HUD.Paused)return;a.AI.Move=Vector3.zero;
  if(Stage==1&&a.AI.Edge<3)a.AI.ResetAt(new Vector3(0,.15f,1));
  if(Stage==1&&!a.Human.Held&&!Arena.Props.Exists(p=>p.CanGrab&&p.transform.position.y>0)){a.ResetArena();a.AI.ResetAt(new Vector3(0,.15f,1));}
  if(Stage==2){
   a.AI.Face(a.Human.transform.position-a.AI.transform.position);
   if(checkingDodge&&!a.AI.WindingUp){checkingDodge=false;if(a.Human.Dodges>dodgesAtWindup&&!a.AI.LastPushHit){Stage=3;a.AI.ResetAt(new Vector3(0,.15f,8));a.Human.ResetAt(new Vector3(0,.15f,5));}}
   if(Stage==2&&Time.time>nextPush&&!checkingDodge){if(Vector3.Distance(a.Human.transform.position,a.AI.transform.position)>2.5f){a.AI.ResetAt(a.Human.transform.position+a.Human.transform.forward*1.8f);a.AI.SnapFace(a.Human.transform.position-a.AI.transform.position);}dodgesAtWindup=a.Human.Dodges;checkingDodge=a.AI.Push();nextPush=Time.time+2;}
  }
  if(Stage==4&&Time.time>finishAt){Stop();a.Setup.Show();}
 }
 public void RingOut(Fighter f){if(!Running)return;if(f==a.AI&&Stage==3){Stage=4;finishAt=Time.time+2.5f;a.Human.Active=a.AI.Active=false;return;}a.ResetArena();a.AI.ResetAt(new Vector3(0,.15f,Stage==3?8:1));if(Stage==3)a.Human.ResetAt(new Vector3(0,.15f,5));}
}
}
