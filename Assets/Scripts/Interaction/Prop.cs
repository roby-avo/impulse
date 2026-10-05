using System.Collections;
using UnityEngine;
namespace Playground {
public class Prop:MonoBehaviour {
 public Rigidbody Body; public string Kind; public bool CanGrab=true; public Fighter Holder,Owner; public float ThrownAt,FellAt=-1;
 public Vector3 Spawn=>spawn;
 Vector3 spawn; Quaternion rotation; bool counted;
 public float ThrowSpeed=>Kind=="chair"?21:Kind=="barrel"?14:18;
 public float ThrowWindup=>Kind=="chair"?.12f:Kind=="barrel"?.38f:.22f;
 public float CarryMultiplier=>Kind=="barrel"?.72f:Kind=="crate"?.9f:1;
 public string Handling=>Kind=="chair"?"LIGHT / FAST THROW":Kind=="barrel"?"HEAVY / STRONG IMPACT":"BALANCED / MEDIUM THROW";
 public void ResetHit(){counted=false;}
 public void SetSpawn(Vector3 position){spawn=position;}

 public void Init(string kind,float mass){Kind=kind;Body=gameObject.AddComponent<Rigidbody>();Body.mass=mass;Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;Body.linearDamping=.15f;Body.angularDamping=.15f;CanGrab=mass<=18;spawn=transform.position;rotation=transform.rotation;gameObject.layer=9;Arena.Props.Add(this);}
 public void RestoreCollisionAfter(Fighter who,float delay){StartCoroutine(Restore(who,delay));}
 IEnumerator Restore(Fighter who,float delay){yield return new WaitForSeconds(delay);if(who)foreach(var c in GetComponentsInChildren<Collider>())Physics.IgnoreCollision(who.GetComponent<Collider>(),c,false);}
 void OnCollisionEnter(Collision c){if(Owner&&!counted&&Time.time-ThrownAt<4&&c.relativeVelocity.magnitude>2){var f=c.rigidbody?c.rigidbody.GetComponent<Fighter>():null;if(f&&f!=Owner){var dir=(f.transform.position-transform.position).normalized;dir.y=.2f;f.Knock(dir*Mathf.Clamp(c.relativeVelocity.magnitude*Body.mass/18f,3,11),Owner,c.GetContact(0).point);if(!counted){Owner.Hits++;Owner.Report("throw_hit");counted=true;}}}}
 // A lost prop returns by dropping from above its spawn point; its ground mark shows where it will land.
 public void DropIn(float height){ResetProp();var p=spawn+Vector3.up*height;var r=rotation*Quaternion.Euler(0,Random.Range(0,360f),0);Body.position=p;Body.rotation=r;transform.SetPositionAndRotation(p,r);Body.linearVelocity=Vector3.down*2;}
 public void ResetProp(){StopAllCoroutines();Holder=null;Owner=null;counted=false;FellAt=-1;Body.isKinematic=false;Body.position=spawn;Body.rotation=rotation;transform.SetPositionAndRotation(spawn,rotation);Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;foreach(var f in Object.FindObjectsByType<Fighter>(FindObjectsSortMode.None))foreach(var c in GetComponentsInChildren<Collider>())Physics.IgnoreCollision(f.GetComponent<Collider>(),c,false);}
}
}
