using System;
using System.Collections.Generic;
using UnityEngine;
namespace Playground {
[RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider))]
public class Fighter : MonoBehaviour {
 public const float MoveSpeed=6f, GrabRange=2.5f, PushRange=2.3f, PushWindup=.22f;
 public Rigidbody Body {get;private set;} public Prop Held {get;private set;}
 public Vector3 Move; public bool Active=true; public string Actor="Human";
 // Labelled rule changes (Rival boost). 1 = the shared baseline rules.
 public float PushPower=1,Stability=1;
 public float StunnedUntil, DodgeUntil, NextDodge, NextPush; public Fighter LastAttacker; public float LastHitTime=-100;
 public int Grabs,Throws,Hits,Pushes,Dodges,Jumps; public float EdgeTime,EdgeSum,Samples;
 public Transform Visual; public event Action<string> Event;
 public string Attack {get;private set;}="";
 public bool WindingUp=>Attack.Length>0;
 public float AttackStarted,AttackAt,RecoveryUntil;
 public bool LastPushHit {get;private set;}
 public Vector3 ContactPoint {get;private set;}
 public Vector3 HitDirection {get;private set;}
 public bool CanAct=>Active&&Time.time>=StunnedUntil&&Time.time>=DodgeUntil&&!WindingUp&&Time.time>=RecoveryUntil;
 public float CarryMultiplier=>Held?Held.CarryMultiplier:1;
 public bool Grounded => Physics.SphereCast(transform.position+Vector3.up*.5f,.36f,Vector3.down,out _,.25f,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
 public float Edge => (Arena.Instance&&Arena.Instance.Match?Arena.Instance.Match.SafeHalfExtent:10f)-Mathf.Max(Mathf.Abs(transform.position.x),Mathf.Abs(transform.position.z));
 public Vector3 ThrowOrigin=>Held?Held.Body.position:transform.position+Vector3.up*1.25f+transform.forward*1.05f;
 readonly HashSet<Rigidbody> pushed=new();
 readonly RaycastHit[] obstructionHits=new RaycastHit[16];
 Vector3 attackDirection;float lastGrounded=-10,jumpBufferedUntil=-10,nextJump;
 void Awake(){Body=GetComponent<Rigidbody>();Body.mass=70;Body.constraints=RigidbodyConstraints.FreezeRotation;Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;var c=GetComponent<CapsuleCollider>();c.center=Vector3.up*.9f;c.height=1.8f;c.radius=.4f;gameObject.layer=10;}
 public void Report(string action){Event?.Invoke(action);}
 void FixedUpdate(){
  bool grounded=Grounded;if(grounded&&Body.linearVelocity.y<.5f)lastGrounded=Time.time;
  if(!Active){CancelAttack();Body.linearVelocity=new Vector3(0,Body.linearVelocity.y,0);Move=Vector3.zero;}
  if(Active){EdgeSum+=Mathf.Max(0,Edge)*Time.fixedDeltaTime;Samples+=Time.fixedDeltaTime;if(Edge<2)EdgeTime+=Time.fixedDeltaTime;}
  if(jumpBufferedUntil>Time.time&&grounded&&CanAct)Jump();
  if(WindingUp&&Time.time>=AttackAt){string attack=Attack;Attack="";if(Active&&Time.time>=StunnedUntil){if(attack=="push")ResolvePush();else LaunchThrow();}}
  if(Active&&Time.time>=StunnedUntil&&Time.time>=DodgeUntil){
   var horizontal=Vector3.ProjectOnPlane(Body.linearVelocity,Vector3.up);
   float commitment=WindingUp?.35f:Time.time<RecoveryUntil?.65f:1;
   var desired=Vector3.ClampMagnitude(Move,1)*MoveSpeed*CarryMultiplier*commitment;
   float acceleration=grounded?(Move.sqrMagnitude<.01f?48f:40f):12f;
   Body.AddForce(Vector3.ClampMagnitude(desired-horizontal,acceleration*Time.fixedDeltaTime),ForceMode.VelocityChange);
  }
  if(Held){
   float height=Held.Kind=="barrel"?1.12f:1.28f;
   var desired=transform.position+Vector3.up*height+transform.forward*1.05f;
   if(Physics.SphereCast(transform.position+Vector3.up*height,.45f,transform.forward,out var wall,1.05f,1<<8))desired=wall.point+wall.normal*.5f;
   Held.Body.MovePosition(desired);Held.Body.MoveRotation(transform.rotation);
  }
 }
 // Both input sources use the same bounded turn rate. Committed attacks lock facing.
 public void Face(Vector3 direction){direction.y=0;if(direction.sqrMagnitude>.001f&&!WindingUp){var rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),540*Time.deltaTime);Body.rotation=rotation;transform.rotation=rotation;}}
 public void SnapFace(Vector3 direction){direction.y=0;if(direction.sqrMagnitude>.001f){var rotation=Quaternion.LookRotation(direction);Body.rotation=rotation;transform.rotation=rotation;}}
 public bool Jump(){if(!CanAct||Time.time<nextJump)return false;if(!Grounded&&Time.time-lastGrounded>.10f){jumpBufferedUntil=Time.time+.12f;return false;}jumpBufferedUntil=-10;lastGrounded=-10;nextJump=Time.time+.2f;Body.linearVelocity=new Vector3(Body.linearVelocity.x,7.1f,Body.linearVelocity.z);Jumps++;Report("jump");return true;}
 public bool Dodge(Vector3 direction){if(!CanAct||Time.time<NextDodge||!Grounded)return false;if(direction.sqrMagnitude<.1f)direction=transform.forward;direction.y=0;Body.linearVelocity=direction.normalized*10.5f*CarryMultiplier+Vector3.up*1.2f;DodgeUntil=Time.time+.24f;NextDodge=Time.time+1.1f;Dodges++;Report("dodge");return true;}
 public Prop Nearest(){Prop best=null;float d=GrabRange;foreach(var p in Arena.Props){if(!p||!p.CanGrab||p.Holder||p.transform.position.y<-.2f)continue;float n=Vector3.Distance(transform.position+Vector3.up*.7f,p.transform.position);if(n<d&&!Physics.Linecast(transform.position+Vector3.up,p.transform.position,1<<8)){best=p;d=n;}}return best;}
 public bool Grab(Prop p){if(!CanAct||Held||!p||p.Holder||!p.CanGrab||Vector3.Distance(transform.position+Vector3.up*.7f,p.transform.position)>GrabRange||Physics.Linecast(transform.position+Vector3.up,p.transform.position,1<<8))return false;
  Held=p;p.Holder=this;p.Owner=null;p.Body.linearVelocity=Vector3.zero;p.Body.angularVelocity=Vector3.zero;p.Body.isKinematic=true;foreach(var col in p.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(GetComponent<Collider>(),col,true);Grabs++;Report("grab");return true;
 }
 public Vector3 ThrowDirection(Vector3 aim){float elevation=Mathf.Clamp(Mathf.Asin(Mathf.Clamp(aim.normalized.y,-1,1))*Mathf.Rad2Deg,-15,55);return (transform.forward*Mathf.Cos(elevation*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(elevation*Mathf.Deg2Rad)).normalized;}
 Vector3 LaunchVelocity(Vector3 direction,float speed)=>direction*speed+Body.linearVelocity*.4f;
 public Vector3 ThrowVelocity(Vector3 aim)=>LaunchVelocity(Attack=="throw"?attackDirection:ThrowDirection(aim),Held?Held.ThrowSpeed:18);
 public bool Release(bool throwing,Vector3 aim){if(!Held||!CanAct)return false;if(!throwing){Drop();return true;}Attack="throw";attackDirection=ThrowDirection(aim);AttackStarted=Time.time;AttackAt=Time.time+Held.ThrowWindup;RecoveryUntil=AttackAt+.22f;Report("throw_windup");return true;}
 void LaunchThrow(){if(!Held)return;var p=Held;var velocity=LaunchVelocity(attackDirection,p.ThrowSpeed);Held=null;p.Holder=null;p.Body.isKinematic=false;p.Body.linearVelocity=velocity;p.Body.angularVelocity=transform.right*(p.Kind=="chair"?9:4);p.Owner=this;p.ThrownAt=Time.time;p.ResetHit();p.RestoreCollisionAfter(this,.3f);Throws++;Report("throw");}
 void Drop(){if(!Held)return;var p=Held;Held=null;p.Holder=null;p.Owner=null;p.Body.isKinematic=false;p.Body.linearVelocity=Body.linearVelocity;p.RestoreCollisionAfter(this,.3f);Report("release");}
 public bool Push(){if(!CanAct||Time.time<NextPush)return false;Attack="push";AttackStarted=Time.time;AttackAt=Time.time+PushWindup;NextPush=Time.time+.78f;RecoveryUntil=AttackAt+.3f;attackDirection=transform.forward;LastPushHit=false;Report("push_windup");return true;}
 void ResolvePush(){bool hit=false;pushed.Clear();var origin=transform.position+Vector3.up*.8f;
  foreach(var col in Physics.OverlapSphere(origin+attackDirection*.9f,1.05f,(1<<9)|(1<<10))){
   var rb=col.attachedRigidbody;if(!rb||rb==Body||!pushed.Add(rb))continue;
   var point=col.ClosestPoint(origin);var delta=rb.worldCenterOfMass-origin;
   if(Vector3.Dot(delta.normalized,attackDirection)<.45f||!ClearPushLine(origin,point,rb))continue;
   var other=rb.GetComponent<Fighter>();if(other){other.Knock(attackDirection*8*PushPower+Vector3.up*2,this,point);hit=true;}
   else if(!rb.isKinematic){rb.AddForce((attackDirection*10+Vector3.up*2)*Mathf.Min(rb.mass,18),ForceMode.Impulse);Arena.Instance.Feedback?.Contact(point,attackDirection,.5f);hit=true;}
  }
  LastPushHit=hit;RecoveryUntil=Time.time+(hit?.24f:.42f);if(hit)Pushes++;Report(hit?"push_hit":"push_miss");
 }
 bool ClearPushLine(Vector3 origin,Vector3 point,Rigidbody target){var delta=point-origin;int count=Physics.RaycastNonAlloc(origin,delta.normalized,obstructionHits,delta.magnitude,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);for(int i=0;i<count;i++){var hit=obstructionHits[i];if(hit.rigidbody==target||hit.rigidbody==Body||(Held&&hit.rigidbody==Held.Body))continue;return false;}return true;}
 public void Knock(Vector3 velocity,Fighter attacker){Knock(velocity,attacker,transform.position+Vector3.up);}
 public void Knock(Vector3 velocity,Fighter attacker,Vector3 point){if(!Active)return;CancelAttack();Body.AddForce(velocity/Mathf.Max(.1f,Stability),ForceMode.VelocityChange);StunnedUntil=Time.time+.28f;LastAttacker=attacker;LastHitTime=Time.time;ContactPoint=point;HitDirection=velocity.normalized;Report("impact");}
 public void CancelAttack(){if(WindingUp)Report("attack_cancelled");Attack="";}
 public void ResetAt(Vector3 p){CancelAttack();Drop();Body.isKinematic=true;Body.isKinematic=false;Body.position=p;transform.position=p;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;Move=Vector3.zero;StunnedUntil=DodgeUntil=NextDodge=NextPush=RecoveryUntil=0;nextJump=0;lastGrounded=jumpBufferedUntil=-10;LastAttacker=null;LastHitTime=-100;SnapFace(-p);}
 public void ClearStats(){Grabs=Throws=Hits=Pushes=Dodges=Jumps=0;EdgeTime=EdgeSum=Samples=0;}
}
}
