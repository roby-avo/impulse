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
 public float max_decisions_per_second=4;
 // CUT_OFF_OPPONENT is opt-in (F2 lab): the pretrained Laya checkpoint never selected it in probes.
 public static readonly string[] OptIn={"CUT_OFF_OPPONENT"};
 public string[] actions=Enum.GetNames(typeof(SemanticAction)).Where(a=>!OptIn.Contains(a)).ToArray();
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
 void Awake(){ProfilePath=Path.Combine(Application.persistentDataPath,"Experiments","profile.json");Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath));try{current=File.Exists(ProfilePath)?JsonUtility.FromJson<ExperimentProfile>(File.ReadAllText(ProfilePath)):Defaults();if(IsLegacyDefault(current)||IsPreviousFactoryDefault(current,Defaults()))current=Defaults();Error=current?.Validate()??(current==null?"Empty profile":null);}catch(Exception e){Error=e.Message;}if(current==null)current=Defaults();}
 // The untouched factory profile from before ATTACK_OPPONENT, CUT_OFF_OPPONENT and opponent.edge_behind_m
 // existed. Edited profiles are left exactly as saved; only the pristine default moves forward.
 public static readonly string[] Added={"ATTACK_OPPONENT","CUT_OFF_OPPONENT"};public const string AddedField="opponent.edge_behind_m";
 public static bool IsPreviousFactoryDefault(ExperimentProfile p,ExperimentProfile now){
  if(p==null||p.schema_version!=1||p.experiment_id!="rooftop"||p.condition_id!="baseline"||!string.IsNullOrEmpty(p.notes)||p.max_decisions_per_second!=now.max_decisions_per_second||p.questions?.action?.criteria==null||p.state_fields==null)return false;
  var oldActions=now.actions.Where(a=>!Added.Contains(a)).ToArray();if(p.actions==null||!p.actions.SequenceEqual(oldActions))return false;
  if(!new HashSet<string>(p.state_fields).SetEquals(now.state_fields.Where(f=>f!=AddedField)))return false;
  if(p.questions.action.type!=now.questions.action.type||p.questions.action.instructions!=now.questions.action.instructions)return false;
  return oldActions.All(key=>(string)typeof(ChoiceCriteria).GetField(key).GetValue(p.questions.action.criteria)==(string)typeof(ChoiceCriteria).GetField(key).GetValue(now.questions.action.criteria));
 }
 public static bool IsLegacyDefault(ExperimentProfile p){
  if(p==null||p.schema_version!=1||p.experiment_id!="rooftop"||p.condition_id!="baseline"||!string.IsNullOrEmpty(p.notes)||p.max_decisions_per_second!=2||p.actions==null||!p.actions.SequenceEqual(Enum.GetNames(typeof(SemanticAction)).Where(a=>!Added.Contains(a)))||p.state_fields==null||p.questions?.action?.criteria==null||p.questions.action.type!="choice"||p.state_fields.Distinct().Count()!=p.state_fields.Length)return false;
  string[] oldFields={"self.distance_to_edge_m","self.speed","self.balance","self.near_edge","self.holding_object","self.grounded","self.held_object_type","opponent.distance_m","opponent.distance_to_edge_m","opponent.speed","opponent.near_edge","opponent.holding_object","opponent.facing_self","opponent.held_object_type","nearest_object.type","nearest_object.id","nearest_object.mass_class","nearest_object.distance_m","incoming_projectile","cover_available","previous_action","previous_outcome"};
  var expanded=oldFields.Concat(new[]{"self.push_ready_in","self.dodge_ready_in","opponent.winding_up","opponent.recovering","sudden_death"});
  if(!(new HashSet<string>(p.state_fields).SetEquals(oldFields)||new HashSet<string>(p.state_fields).SetEquals(expanded)))return false;
  if(p.questions.action.instructions!="Choose the best next action to win this physics rooftop duel by knocking the opponent off. Stay on the roof. Use the visible state and previous result.")return false;
  string[] criteria={"Close distance to engage opponent.","Create distance from opponent.","Shove opponent in close range.","Go to and pick up the observed nearest object.","Throw the object currently held at opponent.","Quick dodge left.","Quick dodge right.","Move to roof center away from open edges.","Move behind the observed ventilation unit.","Jump over a low threat."};
  return p.actions.Select((key,i)=>(string)typeof(ChoiceCriteria).GetField(key).GetValue(p.questions.action.criteria)==criteria[i]).All(x=>x);
 }
 public bool Apply(ExperimentProfile profile,bool save=true){var error=profile?.Validate()??(profile==null?"Empty profile":null);if(error!=null){Error=error;return false;}try{if(save)File.WriteAllText(ProfilePath,JsonUtility.ToJson(profile,true));current=profile.Copy();Error=null;return true;}catch(Exception e){Error=e.Message;return false;}}
}
public static class StateSchema {
 public static readonly string[] Paths=typeof(StateSnapshot).GetFields().SelectMany(f=>f.FieldType==typeof(string)||f.FieldType.IsPrimitive?new[]{f.Name}:f.FieldType.GetFields().Select(child=>f.Name+"."+child.Name)).ToArray();
 public static string Quote(string s){if(s==null)return "null";var b=new StringBuilder("\"");foreach(char c in s){switch(c){case '"':b.Append("\\\"");break;case '\\':b.Append("\\\\");break;case '\n':b.Append("\\n");break;case '\r':b.Append("\\r");break;case '\t':b.Append("\\t");break;default:if(c<32)b.Append("\\u"+((int)c).ToString("x4"));else b.Append(c);break;}}return b.Append('"').ToString();}
 public static string Project(StateSnapshot snapshot,string[] paths){return ProjectObject(snapshot,new HashSet<string>(paths),"");}
 static string ProjectObject(object value,HashSet<string> paths,string prefix){if(value==null)return "null";var parts=new List<string>();foreach(var field in value.GetType().GetFields()){string path=prefix+field.Name;object v=field.GetValue(value);if(field.FieldType!=typeof(string)&&!field.FieldType.IsPrimitive){if(paths.Any(x=>x.StartsWith(path+".",StringComparison.Ordinal)))parts.Add(Quote(field.Name)+":"+ProjectObject(v,paths,path+"."));}else if(paths.Contains(path)){string json=v==null?"null":v is string s?Quote(s):v is bool b?(b?"true":"false"):Convert.ToString(v,CultureInfo.InvariantCulture);parts.Add(Quote(field.Name)+":"+json);}}return "{"+string.Join(",",parts)+"}";}
}
}
