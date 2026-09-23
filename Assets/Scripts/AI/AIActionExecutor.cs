using System;
using UnityEngine;
using UnityEngine.AI;
namespace Playground {
public enum SemanticAction { APPROACH_OPPONENT,RETREAT_FROM_OPPONENT,PUSH_OPPONENT,GRAB_NEAREST_OBJECT,THROW_HELD_OBJECT_AT_OPPONENT,DODGE_LEFT,DODGE_RIGHT,MOVE_TOWARD_SAFETY,TAKE_COVER,JUMP }
public class ActionContext {public Prop Object;public Vector3 Cover;public bool HasCover;}
public class AIActionExecutor:MonoBehaviour {
 public Fighter Self,Opponent;public SemanticAction Selected;public bool Running;public string Outcome="idle";public bool Success;public float Started,Ended;public event Action<AIActionExecutor> Completed;
 ActionContext context;Vector3 target;float duration;NavMeshPath path;
 void Awake(){path=new NavMeshPath();}
 public void Begin(SemanticAction action,ActionContext ctx){if(Running)Finish(false,"replaced");Selected=action;context=ctx??new ActionContext();Started=Time.time;Running=true;Outcome="executing";Self.Move=Vector3.zero;
  var away=Vector3.ProjectOnPlane(Self.transform.position-Opponent.transform.position,Vector3.up).normalized;if(away.sqrMagnitude<.01f)away=Self.transform.forward;
  duration=2.5f;
  switch(action){
   case SemanticAction.APPROACH_OPPONENT:duration=1.2f;break;
   case SemanticAction.RETREAT_FROM_OPPONENT:target=Self.transform.position+away*3;duration=.8f;break;
   case SemanticAction.PUSH_OPPONENT:Self.Face(-away);Finish(Self.Push(),"push");break;
   case SemanticAction.GRAB_NEAREST_OBJECT:if(!context.Object)Finish(false,"no_object");break;
   case SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT:Self.Face(-away);var delta=(Opponent.transform.position+Vector3.up*.9f)-(Self.transform.position+Vector3.up*1.25f);delta.y+=delta.magnitude*.12f;Finish(Self.Release(true,delta.normalized),"throw");break;
   case SemanticAction.DODGE_LEFT:Finish(Self.Dodge(Vector3.Cross(-away,Vector3.up)),"dodge");break;
   case SemanticAction.DODGE_RIGHT:Finish(Self.Dodge(Vector3.Cross(Vector3.up,-away)),"dodge");break;
   case SemanticAction.MOVE_TOWARD_SAFETY:target=Vector3.zero;break;
   case SemanticAction.TAKE_COVER:target=context.Cover;if(!context.HasCover)Finish(false,"no_cover");break;
   case SemanticAction.JUMP:Finish(Self.Jump(),"jump");break;
  }
 }
 void FixedUpdate(){if(!Running)return;if(!Self.Active){Finish(false,"round_ended");return;}if(Time.time<Self.StunnedUntil){Finish(false,"knockback");return;}
  if(Selected==SemanticAction.APPROACH_OPPONENT)target=Opponent.transform.position;
  if(Selected==SemanticAction.GRAB_NEAREST_OBJECT){if(!context.Object||context.Object.Holder||context.Object.transform.position.y<-.3f){Finish(false,"object_unavailable");return;}target=context.Object.transform.position;if(Vector3.Distance(Self.transform.position+Vector3.up*.7f,target)<2.2f){Self.Face(target-Self.transform.position);Finish(Self.Grab(context.Object),"grab");return;}}
  float stop=Selected==SemanticAction.APPROACH_OPPONENT?1.6f:.6f;var flat=Vector3.ProjectOnPlane(target-Self.transform.position,Vector3.up);
  if(flat.magnitude<stop){Finish(true,"arrived");return;}if(Time.time-Started>duration){Finish(Selected==SemanticAction.APPROACH_OPPONENT||Selected==SemanticAction.RETREAT_FROM_OPPONENT,"execution_window_ended");return;}
  // Navigation only executes the selected target. It never chooses an action or a new tactical target.
  if(!NavMesh.SamplePosition(target,out var dest,2,NavMesh.AllAreas)||!NavMesh.SamplePosition(Self.transform.position,out var source,1.2f,NavMesh.AllAreas)||!NavMesh.CalculatePath(source.position,dest.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){Finish(false,"unreachable");return;}
  Vector3 next=dest.position;foreach(var corner in path.corners){if(Vector3.ProjectOnPlane(corner-Self.transform.position,Vector3.up).magnitude>.45f){next=corner;break;}}
  var direction=Vector3.ProjectOnPlane(next-Self.transform.position,Vector3.up).normalized;Self.Move=direction;Self.Face(direction);
 }
 public void Finish(bool success,string reason){if(!Running)return;Self.Move=Vector3.zero;Running=false;Success=success;Outcome=reason;Ended=Time.time;Completed?.Invoke(this);}
 public ActionContext ObserveContext(){Prop nearest=null;float distance=100;foreach(var p in Arena.Props){if(!p||!p.CanGrab||p.Holder||p.transform.position.y<-.2f)continue;var d=Vector3.Distance(Self.transform.position,p.transform.position);if(d<distance&&!Physics.Linecast(Self.transform.position+Vector3.up*1.6f,p.transform.position,1<<8)){nearest=p;distance=d;}}
  Vector3 vent=Self.transform.position.x<0?new Vector3(-4.3f,0,1.8f):new Vector3(4.3f,0,-1.8f);var behind=Vector3.ProjectOnPlane(vent-Opponent.transform.position,Vector3.up).normalized;return new ActionContext{Object=nearest,Cover=vent+behind*2.3f,HasCover=true};}
}
}
