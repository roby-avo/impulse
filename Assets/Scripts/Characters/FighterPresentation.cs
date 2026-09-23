using UnityEngine;
namespace Playground {
public class FighterPresentation:MonoBehaviour {
 Fighter f;Transform[] boots,gloves;Vector3[] bootHome,gloveHome;float gestureUntil,stepAt;string gesture;
 void Start(){f=GetComponent<Fighter>();var bs=new System.Collections.Generic.List<Transform>();var gs=new System.Collections.Generic.List<Transform>();foreach(Transform t in f.Visual){if(t.name=="Boot")bs.Add(t);if(t.name=="Glove")gs.Add(t);}boots=bs.ToArray();gloves=gs.ToArray();bootHome=System.Array.ConvertAll(boots,t=>t.localPosition);gloveHome=System.Array.ConvertAll(gloves,t=>t.localPosition);f.Event+=evt=>{gesture=evt;gestureUntil=Time.time+.22f;};}
 void Update(){if(!f)return;float speed=Vector3.ProjectOnPlane(f.Body.linearVelocity,Vector3.up).magnitude;float stride=Mathf.Sin(Time.time*15)*Mathf.Clamp01(speed/4);bool walk=f.Grounded&&f.Active;
  for(int i=0;i<boots.Length;i++)boots[i].localPosition=bootHome[i]+(walk?new Vector3(0,Mathf.Max(0,stride*(i==0?1:-1))*.1f,stride*(i==0?1:-1)*.12f):Vector3.zero);
  for(int i=0;i<gloves.Length;i++){var offset=f.Held?new Vector3(0,.35f,.5f):gestureUntil>Time.time&&(gesture.StartsWith("push")||gesture=="throw")?new Vector3(0,.15f,.7f):new Vector3(0,0,stride*(i==0?-1:1)*.09f);gloves[i].localPosition=Vector3.Lerp(gloves[i].localPosition,gloveHome[i]+offset,1-Mathf.Exp(-22*Time.deltaTime));}
  float lean=Time.time<f.StunnedUntil?18:Time.time<f.DodgeUntil?12:0;f.Visual.localRotation=Quaternion.Slerp(f.Visual.localRotation,Quaternion.Euler(-lean,0,walk?stride*2:0),1-Mathf.Exp(-15*Time.deltaTime));f.Visual.localPosition=new Vector3(0,walk?Mathf.Abs(stride)*.025f:0,0);
  if(walk&&speed>2&&Time.time>stepAt){stepAt=Time.time+.28f;Arena.Instance.Feedback.Footstep();}
 }
}
}
