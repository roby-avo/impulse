using UnityEngine;
namespace Playground {
[System.Serializable] public class GameConfig {public int wins_required=5;}
public enum RoundPhase{Countdown,Fight,Winner,MatchOver}
public class MatchController:MonoBehaviour {
 public RoundPhase Phase;public int WinsRequired=5,HumanWins,AIWins,Round=1,Generation,HumanSelfOuts,AISelfOuts,HumanEnvironmentalKOs,AIEnvironmentalKOs;
 public float PhaseEnds;public string Announcement;public bool Practice;int pending;
 Arena a;
 void Start(){a=Arena.Instance;var path=System.IO.Path.Combine(Application.streamingAssetsPath,"game-config.json");if(System.IO.File.Exists(path)){var config=JsonUtility.FromJson<GameConfig>(System.IO.File.ReadAllText(path));WinsRequired=Mathf.Clamp(config.wins_required,1,20);}NewMatch();}
 public void NewMatch(){if(!a)a=Arena.Instance;Generation++;HumanWins=AIWins=HumanSelfOuts=AISelfOuts=HumanEnvironmentalKOs=AIEnvironmentalKOs=0;Round=1;a.Human.ClearStats();a.AI.ClearStats();a.ResetArena();a.Telemetry.NewMatch();Countdown();}
 void Countdown(){Generation++;pending=0;Phase=RoundPhase.Countdown;PhaseEnds=Time.time+2.4f;Announcement="READY";a.Human.Active=a.AI.Active=false;a.Human.Move=a.AI.Move=Vector3.zero;}
 void Update(){if(Practice)return;if(Phase==RoundPhase.Countdown&&Time.time>=PhaseEnds){Phase=RoundPhase.Fight;Announcement="FIGHT";a.Human.Active=a.AI.Active=true;a.Telemetry.LogEvent("round_start","game","fight");}
  if(Phase==RoundPhase.Winner&&Time.time>=PhaseEnds){if(HumanWins>=WinsRequired||AIWins>=WinsRequired){Phase=RoundPhase.MatchOver;Announcement=(HumanWins>AIWins?a.LeftName:a.RightName).ToUpperInvariant()+" WINS THE MATCH";a.Telemetry.Summary(this);}else{Round++;a.ResetArena();Countdown();}}
 }
 public void RingOut(Fighter fighter){if(Practice){a.ResetArena();return;}if(Phase!=RoundPhase.Fight)return;pending|=fighter==a.Human?1:2;}
 void LateUpdate(){if(pending==0||Phase!=RoundPhase.Fight)return;int outs=pending;pending=0;Generation++;a.StopExecutors("ring_out");a.Human.Active=a.AI.Active=false;a.Human.Move=a.AI.Move=Vector3.zero;
  if(outs==3)Announcement="DOUBLE RING-OUT · DRAW";else {bool humanLost=outs==1;var loser=humanLost?a.Human:a.AI;var winner=humanLost?a.AI:a.Human;if(humanLost)AIWins++;else HumanWins++;bool caused=loser.LastAttacker==winner&&Time.time-loser.LastHitTime<5;if(!caused){if(humanLost)HumanSelfOuts++;else AISelfOuts++;}else{if(humanLost)AIEnvironmentalKOs++;else HumanEnvironmentalKOs++;}Announcement=(humanLost?a.RightName:a.LeftName).ToUpperInvariant()+" TAKES THE ROUND";a.Telemetry.LogEvent("ring_out",loser.Actor,caused?"opponent_caused":"self_ring_out");}
  Phase=RoundPhase.Winner;PhaseEnds=Time.time+2.2f;a.Telemetry.LogEvent("round_end","game",Announcement);
 }
}
}
