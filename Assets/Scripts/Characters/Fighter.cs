using System;
using System.Collections.Generic;
using UnityEngine;
namespace Playground {
[RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider))]
public class Fighter : MonoBehaviour {
 public const float MoveSpeed=6f, GrabRange=2.5f, PushRange=2.3f;
 public Rigidbody Body {get;private set;} public Prop Held {get;private set;}
 public Vector3 Move; public bool Active=true; public string Actor="Human";
 public float StunnedUntil, DodgeUntil, NextDodge, NextPush; public Fighter LastAttacker; public float LastHitTime=-100;
 public int Grabs,Throws,Hits,Pushes,Dodges,Jumps; public float EdgeTime,EdgeSum,Samples;
 public Transform Visual; public event Action<string> Event;
 readonly HashSet<Fighter> pushed=new();
 public bool Grounded => Physics.SphereCast(transform.position+Vector3.up*.5f,.36f,Vector3.down,out _,.25f,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore);
 public float Edge => 10f-Mathf.Max(Mathf.Abs(transform.position.x),Mathf.Abs(transform.position.z));
 void Awake(){Body=GetComponent<Rigidbody>(); Body.mass=70; Body.constraints=RigidbodyConstraints.FreezeRotation; Body.interpolation=RigidbodyInterpolation.Interpolate; Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; var c=GetComponent<CapsuleCollider>(); c.center=Vector3.up*.9f; c.height=1.8f; c.radius=.4f; gameObject.layer=10;}
 public void Report(string action){Event?.Invoke(action);}
 void FixedUpdate(){
  if(!Active){Body.linearVelocity=new Vector3(0,Body.linearVelocity.y,0);Move=Vector3.zero;}
  if(Active){EdgeSum+=Mathf.Max(0,Edge)*Time.fixedDeltaTime; Samples+=Time.fixedDeltaTime;if(Edge<2)EdgeTime+=Time.fixedDeltaTime;}
  if(Active && Time.time>=StunnedUntil && Time.time>=DodgeUntil){
   var horizontal=Vector3.ProjectOnPlane(Body.linearVelocity,Vector3.up); var desired=Vector3.ClampMagnitude(Move,1)*MoveSpeed;
   Body.AddForce(Vector3.ClampMagnitude(desired-horizontal,(Grounded?36f:10f)*Time.fixedDeltaTime),ForceMode.VelocityChange);
  }
  if(Held){
   var desired=transform.position+Vector3.up*1.25f+transform.forward*1.05f;
   if(Physics.SphereCast(transform.position+Vector3.up*1.25f,.45f,transform.forward,out var wall,1.05f,1<<8))desired=wall.point-wall.normal*0f+wall.normal*.5f;
   Held.Body.MovePosition(desired); Held.Body.MoveRotation(transform.rotation);
  }
 }
 public void Face(Vector3 direction){direction.y=0;if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(direction);}
 public bool Jump(){if(!Active||!Grounded||Time.time<StunnedUntil)return false; Body.linearVelocity=new Vector3(Body.linearVelocity.x,7.1f,Body.linearVelocity.z); Jumps++;Report("jump");return true;}
 public bool Dodge(Vector3 direction){if(!Active||Time.time<NextDodge||!Grounded||Time.time<StunnedUntil)return false; if(direction.sqrMagnitude<.1f)direction=transform.forward; direction.y=0;Body.linearVelocity=direction.normalized*10.5f+Vector3.up*1.2f;DodgeUntil=Time.time+.24f;NextDodge=Time.time+1.1f;Dodges++;Report("dodge");return true;}
 public Prop Nearest(){Prop best=null;float d=GrabRange;foreach(var p in Arena.Props){if(!p||!p.CanGrab||p.Holder||p.transform.position.y<-.2f)continue;float n=Vector3.Distance(transform.position+Vector3.up*.7f,p.transform.position);if(n<d && !Physics.Linecast(transform.position+Vector3.up,p.transform.position,1<<8)){best=p;d=n;}}return best;}
 public bool Grab(Prop p){if(!Active||Held||!p||p.Holder||!p.CanGrab||Time.time<StunnedUntil||Vector3.Distance(transform.position+Vector3.up*.7f,p.transform.position)>GrabRange||Physics.Linecast(transform.position+Vector3.up,p.transform.position,1<<8))return false;
  Held=p;p.Holder=this;p.Owner=null;p.Body.linearVelocity=Vector3.zero;p.Body.angularVelocity=Vector3.zero;p.Body.isKinematic=true;foreach(var col in p.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(GetComponent<Collider>(),col,true);Grabs++;Report("grab");return true;}
 public bool Release(bool throwing,Vector3 aim){if(!Held)return false;var p=Held;Held=null;p.Holder=null;p.Body.isKinematic=false;
  p.Body.linearVelocity=Body.linearVelocity;p.Owner=throwing?this:null;p.ThrownAt=Time.time;
  if(throwing){aim.y=Mathf.Max(.15f,aim.y);p.Body.linearVelocity=aim.normalized*(p.Body.mass>10?14:18)+Body.linearVelocity*.4f;p.Body.angularVelocity=transform.right*5;Throws++;Report("throw");}else Report("release");
  p.RestoreCollisionAfter(this,.3f);return true;}
 public bool Push(){if(!Active||Time.time<NextPush||Time.time<StunnedUntil)return false;NextPush=Time.time+.7f;bool hit=false;pushed.Clear();
  foreach(var col in Physics.OverlapSphere(transform.position+Vector3.up*.8f+transform.forward*.9f,1.2f,(1<<9)|(1<<10))){
   var rb=col.attachedRigidbody;if(!rb||rb==Body)continue;var delta=rb.worldCenterOfMass-(transform.position+Vector3.up*.8f);if(Vector3.Dot(delta.normalized,transform.forward)<.1f)continue;
   var other=rb.GetComponent<Fighter>();if(other){if(!pushed.Add(other))continue;other.Knock(transform.forward*8+Vector3.up*2,this);hit=true;}
   else if(!rb.isKinematic){rb.AddForce((transform.forward*10+Vector3.up*2)*Mathf.Min(rb.mass,18),ForceMode.Impulse);hit=true;}
  } if(hit)Pushes++;Report(hit?"push_hit":"push_miss");return hit;}
 public void Knock(Vector3 velocity,Fighter attacker){if(!Active)return;Body.AddForce(velocity,ForceMode.VelocityChange);StunnedUntil=Time.time+.28f;LastAttacker=attacker;LastHitTime=Time.time;Report("impact");}
 public void ResetAt(Vector3 p){Release(false,transform.forward);Body.isKinematic=true;Body.isKinematic=false;Body.position=p;transform.position=p;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;Move=Vector3.zero;StunnedUntil=DodgeUntil=NextDodge=NextPush=0;LastAttacker=null;LastHitTime=-100;Face(-p);}
 public void ClearStats(){Grabs=Throws=Hits=Pushes=Dodges=Jumps=0;EdgeTime=EdgeSum=Samples=0;}
}
}
