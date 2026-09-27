using UnityEngine;
namespace Playground {
public class FighterPresentation:MonoBehaviour {
 Fighter f;Transform[] boots,gloves;Vector3[] bootHome,gloveHome;float gestureUntil,stepAt,landUntil;string gesture;bool grounded;Transform helmet;Transform[] arms,legs;float gait,moveBlend;
 void Start(){f=GetComponent<Fighter>();var bs=new System.Collections.Generic.List<Transform>();var gs=new System.Collections.Generic.List<Transform>();foreach(Transform t in f.Visual){if(t.name=="Boot")bs.Add(t);if(t.name=="Glove")gs.Add(t);}boots=bs.ToArray();gloves=gs.ToArray();bootHome=System.Array.ConvertAll(boots,t=>t.localPosition);gloveHome=System.Array.ConvertAll(gloves,t=>t.localPosition);helmet=f.Visual.Find("Helmet");var limbMaterial=Arena.Material(new Color(.11f,.18f,.21f));arms=new Transform[gloves.Length];legs=new Transform[boots.Length];for(int i=0;i<arms.Length;i++)arms[i]=Arena.Shape("Arm linkage",PrimitiveType.Cylinder,Vector3.zero,Vector3.one,limbMaterial,false,f.Visual).transform;for(int i=0;i<legs.Length;i++)legs[i]=Arena.Shape("Leg linkage",PrimitiveType.Cylinder,Vector3.zero,Vector3.one,limbMaterial,false,f.Visual).transform;f.Event+=OnEvent;grounded=f.Grounded;}
 static void Link(Transform limb,Vector3 from,Vector3 to,float radius){var delta=to-from;limb.localPosition=(from+to)*.5f;limb.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);limb.localScale=new Vector3(radius,delta.magnitude*.5f,radius);}
 void OnEvent(string evt){gesture=evt;gestureUntil=Time.time+.25f;}
 void OnDestroy(){if(f)f.Event-=OnEvent;}
 void Update(){if(!f)return;float speed=Vector3.ProjectOnPlane(f.Body.linearVelocity,Vector3.up).magnitude;moveBlend=Mathf.Lerp(moveBlend,Mathf.Clamp01(speed/4),1-Mathf.Exp(-12*Time.deltaTime));gait+=speed*Time.deltaTime*2.8f;float stride=Mathf.Sin(gait)*moveBlend;bool onGround=f.Grounded;bool walk=onGround&&f.Active;
  if(onGround&&!grounded&&f.Active){landUntil=Time.time+.16f;f.Report("land");}grounded=onGround;
  bool stun=Time.time<f.StunnedUntil,dodge=Time.time<f.DodgeUntil,landing=Time.time<landUntil;
  float anticipation=f.WindingUp?Mathf.Clamp01((Time.time-f.AttackStarted)/Mathf.Max(.01f,f.AttackAt-f.AttackStarted)):0;
  for(int i=0;i<boots.Length;i++){float side=i==0?1:-1;var offset=walk?new Vector3(0,Mathf.Max(0,stride*side)*.14f,stride*side*.2f):new Vector3(0,.14f,-.12f);boots[i].localPosition=Vector3.Lerp(boots[i].localPosition,bootHome[i]+offset,1-Mathf.Exp(-24*Time.deltaTime));boots[i].localRotation=Quaternion.Slerp(boots[i].localRotation,Quaternion.Euler(walk?stride*side*12:20,0,0),1-Mathf.Exp(-20*Time.deltaTime));}
  for(int i=0;i<gloves.Length;i++){
   Vector3 offset=f.Held?new Vector3(0,f.Held.Kind=="barrel"?.16f:.36f,.5f):new Vector3(0,0,stride*(i==0?-1:1)*.14f);
   if(f.WindingUp)offset+=new Vector3(0,.12f,-.38f*anticipation);
   else if(gestureUntil>Time.time&&(gesture=="push_hit"||gesture=="push_miss"||gesture=="throw"))offset=new Vector3(0,.1f,.7f);
   else if(stun)offset=new Vector3((i==0?-1:1)*.14f,.1f,-.18f);
   gloves[i].localPosition=Vector3.Lerp(gloves[i].localPosition,gloveHome[i]+offset,1-Mathf.Exp(-28*Time.deltaTime));
   gloves[i].localRotation=Quaternion.Euler(f.WindingUp?-25:0,0,stun?(i==0?-25:25):0);
  }
  var localVelocity=f.transform.InverseTransformDirection(f.Body.linearVelocity);
  float lean=stun?-18:dodge?25:f.WindingUp?-10*anticipation:Mathf.Clamp(localVelocity.z*1.4f,-9,9);
  float roll=stun?Vector3.Dot(f.HitDirection,f.transform.right)*14:-Mathf.Clamp(localVelocity.x*1.2f,-10,10);
  f.Visual.localRotation=Quaternion.Slerp(f.Visual.localRotation,Quaternion.Euler(lean,0,roll+(walk?stride*1.2f:0)),1-Mathf.Exp(-18*Time.deltaTime));
  float squash=landing?-.09f:walk?Mathf.Abs(stride)*.035f:0;float idle=onGround?(1-moveBlend)*Mathf.Sin(Time.time*2.4f)*.012f:0;f.Visual.localPosition=Vector3.Lerp(f.Visual.localPosition,new Vector3(0,squash+idle,0),1-Mathf.Exp(-22*Time.deltaTime));
  if(helmet){var arena=Arena.Instance;var opponent=f==arena.Human?arena.AI:arena.Human;var direction=f.transform.InverseTransformDirection(opponent.transform.position-f.transform.position);float turn=Mathf.Clamp(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-24,24);helmet.localRotation=Quaternion.Slerp(helmet.localRotation,Quaternion.Euler(stun?-12:f.WindingUp?-6:0,stun?0:turn,stun?8:0),1-Mathf.Exp(-8*Time.deltaTime));}
  for(int i=0;i<arms.Length;i++)Link(arms[i],new Vector3(gloveHome[i].x*.82f,1.16f,0),gloves[i].localPosition+Vector3.up*.10f,.10f);
  for(int i=0;i<legs.Length;i++)Link(legs[i],new Vector3(bootHome[i].x,.57f,0),boots[i].localPosition+Vector3.up*.10f,.12f);
  if(walk&&speed>2&&Time.time>stepAt){stepAt=Time.time+.28f;Arena.Instance.Feedback.Footstep(f.transform.position);}
 }
}
}
