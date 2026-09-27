using UnityEngine;
namespace Playground {
// World-space affordances use the exact same launch velocity and origin as Fighter.
public class CombatGuide:MonoBehaviour {
 public Vector3 AimPoint {get;private set;}public bool HasAim {get;private set;}
 LineRenderer arc,selection;readonly RaycastHit[] hits=new RaycastHit[24];
 void Start(){arc=Line("Throw trajectory",new Color(.4f,.95f,1),.035f);selection=Line("Grab target",new Color(.4f,.95f,1),.045f);selection.loop=true;}
 LineRenderer Line(string name,Color color,float width){var g=new GameObject(name);g.transform.SetParent(transform);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=Resources.Load<Material>("Particles");l.startColor=l.endColor=color;l.widthMultiplier=width;l.useWorldSpace=true;l.numCornerVertices=3;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;l.positionCount=0;return l;}
 void LateUpdate(){var a=Arena.Instance;bool playing=!a.Spectating&&!a.HUD.Paused&&!a.Setup.Open&&(a.Match.Phase==RoundPhase.Fight||a.Tutorial.Running);var f=a.Human;
  HasAim=playing&&f.Held;arc.enabled=HasAim;selection.enabled=playing&&!f.Held;
  if(HasAim){var origin=f.ThrowOrigin;var velocity=f.ThrowVelocity(f.GetComponent<HumanInput>().Aim);var previous=origin;arc.positionCount=1;arc.SetPosition(0,origin);
   for(int i=1;i<=40;i++){float t=i*.045f;var point=origin+velocity*t+Physics.gravity*(.5f*t*t);var step=point-previous;int count=Physics.SphereCastNonAlloc(previous,.35f,step.normalized,hits,step.magnitude,(1<<8)|(1<<9)|(1<<10),QueryTriggerInteraction.Ignore);float nearest=float.MaxValue;bool stop=false;
    for(int h=0;h<count;h++){var body=hits[h].rigidbody;if(body==f.Body||body==f.Held.Body)continue;if(hits[h].distance<nearest){nearest=hits[h].distance;point=hits[h].point;stop=true;}}
    arc.positionCount=i+1;arc.SetPosition(i,point);AimPoint=point;previous=point;if(stop||point.y<-2)break;
   }
  }
  if(selection.enabled){var p=f.Nearest();selection.enabled=p;if(p){selection.positionCount=33;float radius=p.Kind=="chair"?.66f:.78f;var center=p.transform.position;center.y=.055f;for(int i=0;i<33;i++){float angle=i*Mathf.PI*2/33;selection.SetPosition(i,center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius);}}}
 }
}
}
