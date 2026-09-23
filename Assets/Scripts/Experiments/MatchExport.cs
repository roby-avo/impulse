using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;
namespace Playground {
public static class MatchExport {
 public static string Csv(string value)=>"\""+(value??"").Replace("\"","\"\"")+"\"";
 static readonly object ExportLock=new();
 public static string Export(string source,string templatePath=null,string[] snapshot=null){lock(ExportLock)return ExportCore(source,templatePath,snapshot);}
 static string ExportCore(string source,string templatePath,string[] snapshot){
  var lines=(snapshot??File.ReadAllLines(source)).Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
  var folder=Path.Combine(Path.GetDirectoryName(source),"Exports",Path.GetFileNameWithoutExtension(source));Directory.CreateDirectory(folder);
  File.WriteAllLines(Path.Combine(folder,"match.jsonl"),lines);
  File.WriteAllText(Path.Combine(folder,"match.json"),"["+string.Join(",\n",lines)+"]");
  var csv=new StringBuilder("match_id,experiment_id,condition_id,profile_hash,round,decision_id,game_time,action,confidence,latency_ms,status,success,outcome,request_state_json,available_actions,raw_response\n");
  foreach(var line in lines){var tag=JsonUtility.FromJson<EventRecord>(line);if(tag.type!="decision")continue;var d=JsonUtility.FromJson<DecisionRecord>(line);csv.AppendLine(string.Join(",",new[]{d.match_id,d.experiment_id,d.condition_id,d.profile_hash,d.round_id.ToString(),d.decision_id,d.game_time.ToString(CultureInfo.InvariantCulture),d.selected_action,d.confidence.ToString(CultureInfo.InvariantCulture),d.latency_ms.ToString(CultureInfo.InvariantCulture),d.execution_status,d.execution_success.ToString(),d.interruption_reason,d.request_state_json,string.Join(";",d.available_actions??Array.Empty<string>()),d.raw_response}.Select(Csv)));}
  File.WriteAllText(Path.Combine(folder,"decisions.csv"),csv.ToString());
  string data=("["+string.Join(",",lines)+"]").Replace("<","\\u003c").Replace("\u2028","\\u2028").Replace("\u2029","\\u2029");
  string html=File.ReadAllText(templatePath??Path.Combine(Application.streamingAssetsPath,"inspector-template.html")).Replace("/*MATCH_DATA*/[]",data);
  string report=Path.Combine(folder,"report.html");File.WriteAllText(report,html);return report;
 }
}
}
