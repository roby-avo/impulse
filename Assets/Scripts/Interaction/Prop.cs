using System.Collections;
using UnityEngine;
namespace Playground {
public class Prop:MonoBehaviour {
 public Rigidbody Body; public string Kind; public bool CanGrab=true; public Fighter Holder,Owner; public float ThrownAt;
 Vector3 spawn; Quaternion rotation; bool counted;
 public void Init(string kind,float mass){Kind=kind;Body=gameObject.AddComponent<Rigidbody>();Body.mass=mass;Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;Body.linearDamping=.15f;Body.angularDamping=.15f;CanGrab=mass<=18;spawn=transform.position;rotation=transform.rotation;gameObject.layer=9;Arena.Props.Add(this);}
 public void RestoreCollisionAfter(Fighter who,float delay){StartCoroutine(Restore(who,delay));}
 IEnumerator Restore(Fighter who,float delay){yield return new WaitForSeconds(delay);if(who)foreach(var c in GetComponentsInChildren<Collider>())Physics.IgnoreCollision(who.GetComponent<Collider>(),c,false);counted=false;}
 void OnCollisionEnter(Collision c){if(Owner&&Time.time-ThrownAt<4&&c.relativeVelocity.magnitude>2){var f=c.rigidbody?c.rigidbody.GetComponent<Fighter>():null;if(f&&f!=Owner){var dir=(f.transform.position-transform.position).normalized;dir.y=.2f;f.Knock(dir*Mathf.Clamp(c.relativeVelocity.magnitude*Body.mass/18f,3,11),Owner);if(!counted){Owner.Hits++;Owner.Report("throw_hit");counted=true;}}}}
 public void ResetProp(){StopAllCoroutines();Holder=null;Owner=null;counted=false;Body.isKinematic=false;Body.position=spawn;Body.rotation=rotation;transform.SetPositionAndRotation(spawn,rotation);Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;foreach(var f in Object.FindObjectsByType<Fighter>(FindObjectsSortMode.None))foreach(var c in GetComponentsInChildren<Collider>())Physics.IgnoreCollision(f.GetComponent<Collider>(),c,false);}
}
}
