using System;
using System.IO;
using UnityEngine;
namespace Playground {
[Serializable] public class EventRecord {public string type,match_id,actor,action,timestamp;public int round_id;public float game_time;}
[Serializable] public class DecisionRecord {
 public string type="decision",match_id,decision_id,request_timestamp,response_timestamp,selected_action,interruption_reason,raw_response;
 public int round_id;public float game_time,latency_ms,confidence,action_start,action_end;public bool execution_success;
 public string[] available_actions;public StateSnapshot state_snapshot,resulting_state;
}
public class Telemetry:MonoBehaviour {
 public string DirectoryPath,FilePath,MatchId;public int DecisionCount;public float TotalLatency;public readonly int[] Distribution=new int[10];StreamWriter writer;
 void Awake(){DirectoryPath=Path.Combine(Application.persistentDataPath,"Telemetry");Directory.CreateDirectory(DirectoryPath);}
 public void NewMatch(){writer?.Dispose();MatchId=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);FilePath=Path.Combine(DirectoryPath,MatchId+".jsonl");writer=new StreamWriter(FilePath){AutoFlush=true};DecisionCount=0;TotalLatency=0;Array.Clear(Distribution,0,Distribution.Length);LogEvent("match_start","game","first_to_"+Arena.Instance.Match.WinsRequired);}
 public void LogEvent(string type,string actor,string action){Write(new EventRecord{type=type,match_id=MatchId,round_id=Arena.Instance.Match.Round,actor=actor,action=action,game_time=Time.time,timestamp=DateTime.UtcNow.ToString("O")});}
 public void Write(object record){try{if(record is DecisionRecord decision&&decision.match_id!=MatchId)File.AppendAllText(Path.Combine(DirectoryPath,decision.match_id+".jsonl"),JsonUtility.ToJson(record)+"\n");else writer?.WriteLine(JsonUtility.ToJson(record));}catch(IOException e){Debug.LogWarning("Telemetry write failed: "+e.Message);}}
 public void Count(DecisionResult result){if(!result.Valid)return;DecisionCount++;TotalLatency+=result.LatencyMs;Distribution[(int)result.Action]++;}
 public void Summary(MatchController match){var path=Path.Combine(DirectoryPath,"matches.csv");bool header=!File.Exists(path);using(var csv=new StreamWriter(path,true)){if(header)csv.WriteLine("match_id,human_wins,laya_wins,human_self_ringouts,laya_self_ringouts,human_throws,laya_throws,human_hits,laya_hits,ai_decisions,mean_ai_latency_ms");csv.WriteLine(string.Join(",",MatchId,match.HumanWins,match.AIWins,match.HumanSelfOuts,match.AISelfOuts,Arena.Instance.Human.Throws,Arena.Instance.AI.Throws,Arena.Instance.Human.Hits,Arena.Instance.AI.Hits,DecisionCount,(TotalLatency/Mathf.Max(1,DecisionCount)).ToString("F1",System.Globalization.CultureInfo.InvariantCulture)));}LogEvent("match_end","game",match.HumanWins>match.AIWins?"Human":"Laya");}
 void OnDestroy(){writer?.Dispose();}
}
}
