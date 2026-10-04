using System;
using UnityEngine;
using UnityEngine.AI;
namespace Playground {
// Append new actions at the end: recordings index action counts by these values.
public enum SemanticAction { APPROACH_OPPONENT,RETREAT_FROM_OPPONENT,PUSH_OPPONENT,GRAB_NEAREST_OBJECT,THROW_HELD_OBJECT_AT_OPPONENT,DODGE_LEFT,DODGE_RIGHT,MOVE_TOWARD_SAFETY,TAKE_COVER,JUMP,ATTACK_OPPONENT,CUT_OFF_OPPONENT }
public class ActionContext {public Prop Object;public Vector3 Cover;public bool HasCover;}
public class AIActionExecutor:MonoBehaviour {
 public Fighter Self,Opponent;public SemanticAction Selected;public bool Running;public string Outcome="idle";public bool Success;public float Started,Ended;public event Action<AIActionExecutor> Completed;
 ActionContext context;Vector3 target;float duration,strikeStarted;NavMeshPath path;bool committed,striking;
 // ATTACK_OPPONENT navigates until the opponent is in reach, then executes the same push as PUSH_OPPONENT.
 public bool Navigating=>Running&&Selected!=SemanticAction.PUSH_OPPONENT&&Selected!=SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT&&!striking;
 public bool Striking=>Running&&striking;
 public const float AttackReach=Fighter.PushRange-.2f,CutOffGap=1.9f;
 public bool CanPlanAhead=>Navigating&&Time.time-Started>.18f;
 public bool CanContinue(SemanticAction action,ActionContext ctx)=>Running&&(Navigating||striking&&!committed)&&Selected==action&&(action!=SemanticAction.GRAB_NEAREST_OBJECT||context.Object==ctx.Object)&&(action!=SemanticAction.TAKE_COVER||Vector3.Distance(target,ctx.Cover)<1);
 void Awake(){path=new NavMeshPath();}
 public void Begin(SemanticAction action,ActionContext ctx){if(Running)Finish(false,"replaced");Selected=action;context=ctx??new ActionContext();Started=Time.time;Running=true;Outcome="executing";committed=false;striking=false;Self.Move=Vector3.zero;
  var away=Vector3.ProjectOnPlane(Self.transform.position-Opponent.transform.position,Vector3.up).normalized;if(away.sqrMagnitude<.01f)away=Self.transform.forward;
  duration=2.5f;
  switch(action){
   case SemanticAction.APPROACH_OPPONENT:duration=1.2f;break;
   case SemanticAction.RETREAT_FROM_OPPONENT:target=RetreatTarget();duration=.8f;break;
   case SemanticAction.PUSH_OPPONENT:duration=1.5f;break;
   case SemanticAction.GRAB_NEAREST_OBJECT:if(!context.Object)Finish(false,"no_object");break;
   case SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT:duration=1.5f;break;
   case SemanticAction.DODGE_LEFT:Finish(Self.Dodge(Vector3.Cross(-away,Vector3.up)),"dodge");break;
   case SemanticAction.DODGE_RIGHT:Finish(Self.Dodge(Vector3.Cross(Vector3.up,-away)),"dodge");break;
   case SemanticAction.MOVE_TOWARD_SAFETY:target=Vector3.zero;break;
   case SemanticAction.TAKE_COVER:target=context.Cover;if(!context.HasCover)Finish(false,"no_cover");break;
   case SemanticAction.JUMP:Finish(Self.Jump(),"jump");break;
   case SemanticAction.ATTACK_OPPONENT:duration=2.5f;break;
   case SemanticAction.CUT_OFF_OPPONENT:target=CutOffPoint();duration=2f;break;
  }
 }
 void FixedUpdate(){if(!Running)return;if(!Self.Active){Finish(false,"round_ended");return;}if(Time.time<Self.StunnedUntil){Finish(false,"knockback");return;}
  if(!Navigating){
   bool shove=Selected==SemanticAction.PUSH_OPPONENT||striking;
   if(committed){if(shove&&Self.WindingUp){var gap=Vector3.ProjectOnPlane(Opponent.transform.position-Self.transform.position,Vector3.up);Self.Move=gap.magnitude>1.6f?gap.normalized:Vector3.zero;}if(!Self.WindingUp)Finish(shove?Self.LastPushHit:!Self.Held,shove?"push":"throw");return;}
   var aim=Selected==SemanticAction.THROW_HELD_OBJECT_AT_OPPONENT&&Self.Held?ThrowAim():Opponent.transform.position-Self.transform.position;Self.Face(aim);
   if(Time.time-(striking?strikeStarted:Started)>(striking?1.5f:duration)){Finish(false,"turn_timeout");return;}
   if(striking)Self.Move=Vector3.zero;
   if(Vector3.Angle(Self.transform.forward,Vector3.ProjectOnPlane(aim,Vector3.up))>12)return;
   if(shove){if(striking&&Time.time<Self.NextPush)return;committed=Self.Push();if(!committed)Finish(false,"push_unavailable");}
   else {if(!Self.Held){Finish(false,"no_object");return;}
    // Lead visible movement through wind-up and flight; facing still locks on commitment.
    committed=Self.Release(true,aim.normalized);if(!committed)Finish(false,"throw_unavailable");
   }return;
  }
  if(Selected==SemanticAction.APPROACH_OPPONENT||Selected==SemanticAction.ATTACK_OPPONENT)target=Opponent.transform.position;
  if(Selected==SemanticAction.CUT_OFF_OPPONENT)target=CutOffPoint();
  if(Selected==SemanticAction.ATTACK_OPPONENT){var gap=Vector3.ProjectOnPlane(Opponent.transform.position-Self.transform.position,Vector3.up);if(gap.magnitude<=AttackReach&&!Physics.Linecast(Self.transform.position+Vector3.up,Opponent.transform.position+Vector3.up,1<<8)){striking=true;strikeStarted=Time.time;Self.Move=Vector3.zero;return;}}
  if(Selected==SemanticAction.GRAB_NEAREST_OBJECT){if(!context.Object||context.Object.Holder||context.Object.transform.position.y<-.3f){Finish(false,"object_unavailable");return;}target=context.Object.transform.position;if(Vector3.Distance(Self.transform.position+Vector3.up*.7f,target)<2.2f){Self.Face(target-Self.transform.position);Finish(Self.Grab(context.Object),"grab");return;}}
  float stop=Selected==SemanticAction.APPROACH_OPPONENT?2.05f:Selected==SemanticAction.ATTACK_OPPONENT?.1f:.6f;var flat=Vector3.ProjectOnPlane(target-Self.transform.position,Vector3.up);
  if(flat.magnitude<stop){Finish(true,"arrived");return;}if(Time.time-Started>duration){Finish(Selected==SemanticAction.APPROACH_OPPONENT||Selected==SemanticAction.RETREAT_FROM_OPPONENT,Selected==SemanticAction.ATTACK_OPPONENT?"never_reached_opponent":"execution_window_ended");return;}
  // Navigation only executes the selected target. It never chooses an action or a new tactical target.
  if(!NavMesh.SamplePosition(target,out var dest,2,NavMesh.AllAreas)||!NavMesh.SamplePosition(Self.transform.position,out var source,1.2f,NavMesh.AllAreas)||!NavMesh.CalculatePath(source.position,dest.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){Finish(false,"unreachable");return;}
  Vector3 next=dest.position;foreach(var corner in path.corners){if(Vector3.ProjectOnPlane(corner-Self.transform.position,Vector3.up).magnitude>.45f){next=corner;break;}}
  var direction=Vector3.ProjectOnPlane(next-Self.transform.position,Vector3.up).normalized;Self.Move=direction;Self.Face(direction);
 }
 // Deterministic aiming executes a selected throw; it never chooses whether to attack.
 public Vector3 ThrowAim(){
  if(!Self.Held)return Opponent.transform.position-Self.transform.position;
  var origin=Self.ThrowOrigin;var delta=Opponent.transform.position+Vector3.up*.9f-origin;
  float flight=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude/Self.Held.ThrowSpeed;
  var velocity=Vector3.ClampMagnitude(Vector3.ProjectOnPlane(Opponent.Body.linearVelocity,Vector3.up),Fighter.MoveSpeed);
  for(int i=0;i<3;i++){
   var lead=velocity*Mathf.Min(.9f,Self.Held.ThrowWindup+flight);
   delta=Opponent.transform.position+Vector3.up*.9f+lead-origin-Self.Body.linearVelocity*.4f*flight;
   delta.y+=.5f*Mathf.Abs(Physics.gravity.y)*flight*flight;
   flight=delta.magnitude/Self.Held.ThrowSpeed;
  }
  return delta;
 }
 public void Finish(bool success,string reason){if(!Running)return;Self.Move=Vector3.zero;if(!success)Self.CancelAttack();Running=false;Success=success;Outcome=reason;Ended=Time.time;Completed?.Invoke(this);}
 // The spot on the roof-center side of the opponent, at shoving distance. Geometry only:
 // the model chooses whether to go there; a shove from there points toward the nearer edge.
 public Vector3 CutOffPoint(){var o=Vector3.ProjectOnPlane(Opponent.transform.position,Vector3.up);var inward=-o;if(inward.magnitude<.5f)inward=Vector3.ProjectOnPlane(Self.transform.position-Opponent.transform.position,Vector3.up);if(inward.sqrMagnitude<.01f)inward=Vector3.back;var point=o+inward.normalized*CutOffGap;float limit=Mathf.Max(.5f,Arena.Instance.Match.SafeHalfExtent-.8f);point.x=Mathf.Clamp(point.x,-limit,limit);point.z=Mathf.Clamp(point.z,-limit,limit);return point;}
 public Vector3 RetreatTarget(){
  var away=Vector3.ProjectOnPlane(Self.transform.position-Opponent.transform.position,Vector3.up).normalized;
  var destination=Self.transform.position+away*3;float limit=Mathf.Max(.5f,Arena.Instance.Match.SafeHalfExtent-.8f);
  destination.x=Mathf.Clamp(destination.x,-limit,limit);destination.z=Mathf.Clamp(destination.z,-limit,limit);return destination;
 }
 public ActionContext ObserveContext(){
  Prop nearest=null;float distance=float.PositiveInfinity;var navPath=new NavMeshPath();
  bool onMesh=NavMesh.SamplePosition(Self.transform.position,out var source,1.2f,NavMesh.AllAreas);
  foreach(var p in Arena.Props){
   if(!p||!p.CanGrab||p.Holder||p.transform.position.y<-.2f)continue;
   float d=Vector3.Distance(Self.transform.position,p.transform.position);
   if(d>=distance||Physics.Linecast(Self.transform.position+Vector3.up*1.6f,p.transform.position,1<<8))continue;
   if(!onMesh||!NavMesh.SamplePosition(p.transform.position,out var destination,1,NavMesh.AllAreas)||!NavMesh.CalculatePath(source.position,destination.position,NavMesh.AllAreas,navPath)||navPath.status!=NavMeshPathStatus.PathComplete)continue;
   nearest=p;distance=d;
  }
  var context=new ActionContext{Object=nearest};float best=float.PositiveInfinity;
  foreach(var cover in Arena.Instance.Cover){
   var center=cover.bounds.center;var behind=Vector3.ProjectOnPlane(center-Opponent.transform.position,Vector3.up).normalized;
   float radius=Mathf.Abs(behind.x)*cover.bounds.extents.x+Mathf.Abs(behind.z)*cover.bounds.extents.z+.9f;
   var point=Vector3.ProjectOnPlane(center+behind*radius,Vector3.up);
   if(!onMesh||!NavMesh.SamplePosition(point,out var destination,.6f,NavMesh.AllAreas))continue;
   point=destination.position;
   if(Arena.Instance.Match.SafeHalfExtent-Mathf.Max(Mathf.Abs(point.x),Mathf.Abs(point.z))<.8f)continue;
   var ray=new Ray(Opponent.transform.position+Vector3.up,point+Vector3.up-(Opponent.transform.position+Vector3.up));
   if(!cover.Raycast(ray,out _,Vector3.Distance(Opponent.transform.position,point))||!NavMesh.CalculatePath(source.position,point,NavMesh.AllAreas,navPath)||navPath.status!=NavMeshPathStatus.PathComplete)continue;
   float length=0;var previous=source.position;foreach(var corner in navPath.corners){length+=Vector3.Distance(previous,corner);previous=corner;}
   if(length<best){best=length;context.Cover=point;context.HasCover=true;}
  }
  return context;
 }
}
}
