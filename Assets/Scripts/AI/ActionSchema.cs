using System;
using System.Collections.Generic;
using UnityEngine;
namespace Playground {
[Serializable] public class ChoiceCriteria {public string APPROACH_OPPONENT,RETREAT_FROM_OPPONENT,PUSH_OPPONENT,GRAB_NEAREST_OBJECT,THROW_HELD_OBJECT_AT_OPPONENT,DODGE_LEFT,DODGE_RIGHT,MOVE_TOWARD_SAFETY,TAKE_COVER,JUMP,ATTACK_OPPONENT,CUT_OFF_OPPONENT;}
[Serializable] public class ChoiceQuestion {public string type,instructions;public ChoiceCriteria criteria;}
[Serializable] public class QuestionFile {public ChoiceQuestion action;}
public static class ActionSchema {
 public static string[] Available(AIActionExecutor e,ActionContext ctx){var list=new List<string>();var s=e.Self;if(!s.CanAct)return list.ToArray();
  foreach(SemanticAction action in Enum.GetValues(typeof(SemanticAction))){bool executable=true;
   switch(action){
    case SemanticAction.APPROACH_OPPONENT:executable=Vector3.ProjectOnPlane(s.transform.position-e.Opponent.transform.position,Vector3.up).magnitude>2.05f;break;
    case SemanticAction.RETREAT_FROM_OPPONENT:executable=Vector3.ProjectOnPlane(s.transform.position-e.RetreatTarget(),Vector3.up).magnitude>.6f;break;
    case SemanticAction.MOVE_TOWARD_SAFETY:executable=Vector3.ProjectOnPlane(s.transform.position,Vector3.up).magnitude>.6f;break;
    case SemanticAction.PUSH_OPPONENT:executable=Time.time>=s.NextPush&&Vector3.Distance(s.transform.position,e.Opponent.transform.position)<=Fighter.PushRange&&!Physics.Linecast(s.transform.position+Vector3.up,e.Opponent.transform.position+Vector3.up,1<<8);break;
    case SemanticAction.GRAB_NEAREST_OBJECT:executable=!s.Held&&ctx.Object;break;
    case SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT:executable=s.Held;break;
    case SemanticAction.DODGE_LEFT:case SemanticAction.DODGE_RIGHT:executable=s.Grounded&&Time.time>=s.NextDodge;break;
    // The shared ledge save is a jump too, offered during its short window after leaving the roof.
    case SemanticAction.JUMP:executable=s.Grounded||s.CanLedgeSave;break;
    case SemanticAction.TAKE_COVER:executable=ctx.HasCover&&Vector3.ProjectOnPlane(s.transform.position-ctx.Cover,Vector3.up).magnitude>.6f;break;
    // A rush is offered while the shove is ready or nearly ready; holding a prop, the attack is a throw.
    case SemanticAction.ATTACK_OPPONENT:executable=!s.Held&&s.NextPush-Time.time<.5f;break;
    case SemanticAction.CUT_OFF_OPPONENT:executable=Vector3.ProjectOnPlane(s.transform.position-e.CutOffPoint(),Vector3.up).magnitude>.8f;break;
   } if(executable)list.Add(action.ToString());
  }return list.ToArray();
 }
 static string Quote(string v)=>StateSchema.Quote(v);
 public static string Build(QuestionFile file,string[] allowed){var options=new List<string>();foreach(var key in allowed){var field=typeof(ChoiceCriteria).GetField(key);if(field==null)throw new ArgumentException("Unknown action");options.Add(Quote(key)+":"+Quote((string)field.GetValue(file.action.criteria)));}return "{\"action\":{\"type\":\"choice\",\"instructions\":"+Quote(file.action.instructions)+",\"criteria\":{"+string.Join(",",options)+"}}}";}
}
}
