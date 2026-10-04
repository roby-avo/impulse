using System.Collections.Generic;
using UnityEngine;
namespace Playground {
// The rooftop fan vents blow upward: anything that lands on a vent top is launched into the air.
// Shared by every fighter and loose prop; launches are reported as "vent_launch" events.
public class VentLaunchers:MonoBehaviour {
 public const float LaunchSpeed=9f,PropLaunchSpeed=7.5f,Cooldown=1f;
 Arena a;readonly Dictionary<Rigidbody,float> ready=new();LineRenderer[] pads;
 void Start(){a=Arena.Instance;}
 // Collider bounds are empty until physics has registered the vents, so outlines are built on first use.
 void BuildPads(){Physics.SyncTransforms();pads=new LineRenderer[a.Vents.Count];for(int i=0;i<pads.Length;i++){var b=a.Vents[i].bounds;var g=new GameObject("Vent updraft outline");g.transform.SetParent(transform);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=Resources.Load<Material>("Particles");l.widthMultiplier=.05f;l.loop=true;l.useWorldSpace=true;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;float y=b.max.y+.03f;l.positionCount=4;l.SetPositions(new[]{new Vector3(b.min.x+.08f,y,b.min.z+.08f),new Vector3(b.max.x-.08f,y,b.min.z+.08f),new Vector3(b.max.x-.08f,y,b.max.z-.08f),new Vector3(b.min.x+.08f,y,b.max.z-.08f)});pads[i]=l;}}
 static bool Over(Bounds b,Vector3 p,float inset)=>p.x>b.min.x+inset&&p.x<b.max.x-inset&&p.z>b.min.z+inset&&p.z<b.max.z-inset;
 bool Ready(Rigidbody body)=>!ready.TryGetValue(body,out var t)||Time.time>=t;
 void FixedUpdate(){if(!a.Match)return;
  foreach(var vent in a.Vents){var b=vent.bounds;float top=b.max.y;
   foreach(var f in new[]{a.Human,a.AI}){var p=f.transform.position;if(!f.Active||!Over(b,p,.15f)||p.y<top-.2f||p.y>top+.4f||f.Body.linearVelocity.y>1||!Ready(f.Body))continue;var v=f.Body.linearVelocity;f.Body.linearVelocity=new Vector3(v.x*.6f,LaunchSpeed,v.z*.6f);ready[f.Body]=Time.time+Cooldown;f.Report("vent_launch");}
   foreach(var prop in Arena.Props){if(!prop||prop.Holder||prop.Body.isKinematic)continue;var p=prop.transform.position;if(!Over(b,p,0)||p.y<top-.1f||p.y>top+.8f||prop.Body.linearVelocity.y>1||!Ready(prop.Body))continue;var v=prop.Body.linearVelocity;prop.Body.linearVelocity=new Vector3(v.x*.6f,PropLaunchSpeed,v.z*.6f);ready[prop.Body]=Time.time+Cooldown;}
  }
 }
 void Update(){if(pads==null){if(a.Vents.Count==0||a.Vents[0].bounds.size.sqrMagnitude<.01f)return;BuildPads();}float pulse=.55f+.45f*Mathf.Sin(Time.unscaledTime*3);var color=Color.Lerp(new Color(.1f,.45f,.5f),new Color(.45f,1,1),pulse);foreach(var l in pads)l.startColor=l.endColor=color;}
}
}
