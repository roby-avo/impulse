using UnityEngine;
namespace Playground {
// Depth cues for the fixed orthographic view: a team-colored ring stays on the roof under each
// robot (shrinking as it rises), and thrown props mark where they are above the ground.
public class GroundMarkers:MonoBehaviour {
 LineRenderer[] rings,props;Arena a;readonly RaycastHit[] hits=new RaycastHit[4];
 public LineRenderer RingFor(Fighter f)=>rings[f==a.Human?0:1];
 void Start(){a=Arena.Instance;rings=new[]{Ring("Ground ring / left",new Color(.25f,.9f,1)),Ring("Ground ring / right",new Color(1,.45f,.2f))};props=new LineRenderer[Arena.Props.Count];for(int i=0;i<props.Length;i++)props[i]=Ring("Prop ground mark",new Color(.85f,.92f,.95f));}
 LineRenderer Ring(string name,Color color){var g=new GameObject(name);g.transform.SetParent(transform);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=Resources.Load<Material>("Particles");l.startColor=l.endColor=color;l.widthMultiplier=.05f;l.loop=true;l.useWorldSpace=true;l.positionCount=28;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;l.enabled=false;return l;}
 bool Ground(Vector3 from,out Vector3 point){int n=Physics.RaycastNonAlloc(from+Vector3.up*.2f,Vector3.down,hits,30,1<<8,QueryTriggerInteraction.Ignore);float best=float.MaxValue;point=default;for(int i=0;i<n;i++)if(hits[i].distance<best){best=hits[i].distance;point=hits[i].point;}return n>0;}
 static void Place(LineRenderer l,Vector3 center,float radius){center.y+=.04f;for(int i=0;i<l.positionCount;i++){float t=i*Mathf.PI*2/l.positionCount;l.SetPosition(i,center+new Vector3(Mathf.Cos(t),0,Mathf.Sin(t))*radius);}}
 void LateUpdate(){bool show=!a.Setup.Open&&!a.Research.Open;
  for(int i=0;i<2;i++){var f=i==0?a.Human:a.AI;var l=rings[i];var floor=Vector3.zero;l.enabled=show&&Ground(f.transform.position,out floor)&&f.transform.position.y>floor.y-.5f;if(!l.enabled)continue;float height=Mathf.Max(0,f.transform.position.y-floor.y);Place(l,floor,Mathf.Lerp(.62f,.3f,Mathf.Clamp01(height/2.5f)));}
  for(int i=0;i<props.Length&&i<Arena.Props.Count;i++){var p=Arena.Props[i];var l=props[i];bool flying=show&&p&&!p.Holder&&p.Body.linearVelocity.sqrMagnitude>4;float height=0;var floor=Vector3.zero;l.enabled=flying&&Ground(p.transform.position,out floor)&&(height=p.transform.position.y-floor.y)>.6f;if(l.enabled)Place(l,floor,Mathf.Lerp(.45f,.2f,Mathf.Clamp01(height/4)));}
 }
}
}
