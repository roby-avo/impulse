using System;
using System.Collections.Generic;
using UnityEngine;
namespace Playground {
[Serializable] public class ChoiceCriteria {public string APPROACH_OPPONENT,RETREAT_FROM_OPPONENT,PUSH_OPPONENT,GRAB_NEAREST_OBJECT,THROW_HELD_OBJECT_AT_OPPONENT,DODGE_LEFT,DODGE_RIGHT,MOVE_TOWARD_SAFETY,TAKE_COVER,JUMP;}
[Serializable] public class ChoiceQuestion {public string type,instructions;public ChoiceCriteria criteria;}
[Serializable] public class QuestionFile {public ChoiceQuestion action;}
public static class ActionSchema {
 public static string[] Available(AIActionExecutor e,ActionContext ctx){var list=new List<string>();var s=e.Self;
  foreach(SemanticAction action in Enum.GetValues(typeof(SemanticAction))){bool executable=true;
   switch(action){
    case SemanticAction.PUSH_OPPONENT:executable=Time.time>=s.NextPush&&Vector3.Distance(s.transform.position,e.Opponent.transform.position)<=Fighter.PushRange;break;
    case SemanticAction.GRAB_NEAREST_OBJECT:executable=!s.Held&&ctx.Object;break;
    case SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT:executable=s.Held;break;
    case SemanticAction.DODGE_LEFT:case SemanticAction.DODGE_RIGHT:executable=s.Grounded&&Time.time>=s.NextDodge;break;
    case SemanticAction.JUMP:executable=s.Grounded;break;
    case SemanticAction.TAKE_COVER:executable=ctx.HasCover;break;
   } if(executable)list.Add(action.ToString());
  }return list.ToArray();
 }
 static string Quote(string v)=>"\""+v.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","\\r")+"\"";
 public static string Build(QuestionFile file,string[] allowed){var options=new List<string>();foreach(var key in allowed){var field=typeof(ChoiceCriteria).GetField(key);if(field==null)throw new ArgumentException("Unknown action");options.Add(Quote(key)+":"+Quote((string)field.GetValue(file.action.criteria)));}return "{\"action\":{\"type\":\"choice\",\"instructions\":"+Quote(file.action.instructions)+",\"criteria\":{"+string.Join(",",options)+"}}}";}
}
}
