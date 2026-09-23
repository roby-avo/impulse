using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Playground {
[Serializable] public class ExperimentProfile {
 public int schema_version=1;
 public string experiment_id="rooftop",condition_id="baseline",notes="";
 public float max_decisions_per_second=2;
 public string[] actions=Enum.GetNames(typeof(SemanticAction));
 public string[] state_fields=StateSchema.Paths;
 public QuestionFile questions;
 public ExperimentProfile Copy()=>JsonUtility.FromJson<ExperimentProfile>(JsonUtility.ToJson(this));
 public string Validate(){
  if(schema_version!=1)return "Unsupported profile schema version";
  foreach(var id in new[]{experiment_id,condition_id})if(string.IsNullOrEmpty(id)||id.Length>80||!System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-zA-Z0-9_.-]+$"))return "IDs: use 1–80 letters, digits, dots, underscores or hyphens";
  if(float.IsNaN(max_decisions_per_second)||max_decisions_per_second<.25f||max_decisions_per_second>10)return "Request cap must be 0.25–10 per game second";
  if(actions==null||actions.Length==0||actions.Distinct().Count()!=actions.Length||actions.Any(x=>!Enum.GetNames(typeof(SemanticAction)).Contains(x)))return "Select at least one supported action; no duplicates";
  if(state_fields==null||state_fields.Length==0||state_fields.Distinct().Count()!=state_fields.Length||state_fields.Any(x=>!StateSchema.Paths.Contains(x)))return "Select at least one known observation; no duplicates";
  if(questions?.action?.criteria==null||string.IsNullOrWhiteSpace(questions.action.instructions))return "Questions need instructions and criteria";
  foreach(var key in actions)if(string.IsNullOrWhiteSpace((string)typeof(ChoiceCriteria).GetField(key).GetValue(questions.action.criteria)))return "Missing criterion: "+key;
  return null;
 }
 public string Hash(){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)))).Replace("-","").ToLowerInvariant();}
}
public class ExperimentSettings:MonoBehaviour {
 ExperimentProfile current;
 public ExperimentProfile Current=>current.Copy();
 public string ProfilePath,Error;
 public bool Ready=>current!=null&&Error==null;
 public ExperimentProfile Defaults()=>new ExperimentProfile{questions=JsonUtility.FromJson<QuestionFile>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"laya-question.json")))};
 void Awake(){ProfilePath=Path.Combine(Application.persistentDataPath,"Experiments","profile.json");Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath));try{current=File.Exists(ProfilePath)?JsonUtility.FromJson<ExperimentProfile>(File.ReadAllText(ProfilePath)):Defaults();Error=current?.Validate()??(current==null?"Empty profile":null);}catch(Exception e){Error=e.Message;}if(current==null)current=Defaults();}
 public bool Apply(ExperimentProfile profile,bool save=true){var error=profile?.Validate()??(profile==null?"Empty profile":null);if(error!=null){Error=error;return false;}try{if(save)File.WriteAllText(ProfilePath,JsonUtility.ToJson(profile,true));current=profile.Copy();Error=null;return true;}catch(Exception e){Error=e.Message;return false;}}
}
public static class StateSchema {
 public static readonly string[] Paths=typeof(StateSnapshot).GetFields().SelectMany(f=>f.FieldType==typeof(string)||f.FieldType.IsPrimitive?new[]{f.Name}:f.FieldType.GetFields().Select(child=>f.Name+"."+child.Name)).ToArray();
 public static string Quote(string s){if(s==null)return "null";var b=new StringBuilder("\"");foreach(char c in s){switch(c){case '"':b.Append("\\\"");break;case '\\':b.Append("\\\\");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;default:if(c<32)b.Append("\\u"+((int)c).ToString("x4"));else b.Append(c);break;}}return b.Append('"').ToString();}
 public static string Project(StateSnapshot snapshot,string[] paths){return ProjectObject(snapshot,new HashSet<string>(paths),"");}
 static string ProjectObject(object value,HashSet<string> paths,string prefix){if(value==null)return "null";var parts=new List<string>();foreach(var field in value.GetType().GetFields()){string path=prefix+field.Name;object v=field.GetValue(value);if(field.FieldType!=typeof(string)&&!field.FieldType.IsPrimitive){if(paths.Any(x=>x.StartsWith(path+".",StringComparison.Ordinal)))parts.Add(Quote(field.Name)+":"+ProjectObject(v,paths,path+"."));}else if(paths.Contains(path)){string json=v==null?"null":v is string s?Quote(s):v is bool b?(b?"true":"false"):Convert.ToString(v,CultureInfo.InvariantCulture);parts.Add(Quote(field.Name)+":"+json);}}return "{"+string.Join(",",parts)+"}";}
}
}
