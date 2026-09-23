using UnityEngine;
namespace Playground {
public class GameFeedback:MonoBehaviour {
 Arena a;AudioSource audioSource;AudioClip impact,jump,grab,throwClip,step,win;RoundPhase phase;int countdown;Material sparkMaterial;
 void Start(){a=Arena.Instance;audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.volume=.4f;impact=Tone("Impact",95,.19f,true);jump=Tone("Jump",320,.15f,false);grab=Tone("Grab",650,.07f,false);throwClip=Tone("Throw",160,.17f,true);step=Tone("Step",80,.055f,true);win=Tone("Victory",540,.5f,false);sparkMaterial=Resources.Load<Material>("Particles");a.Human.Event+=s=>Event(a.Human,s);a.AI.Event+=s=>Event(a.AI,s);}
 AudioClip Tone(string name,float frequency,float length,bool noise){int n=(int)(22050*length);var data=new float[n];for(int i=0;i<n;i++){float t=i/22050f;float envelope=Mathf.Pow(1-i/(float)n,2);float random=Mathf.Sin(i*78.233f)*43758.5453f;random=(random-Mathf.Floor(random))-.5f;data[i]=(Mathf.Sin(2*Mathf.PI*(frequency*t+100*t*t))*.45f+(noise?random*.6f:0))*envelope;}var clip=AudioClip.Create(name,n,1,22050,false);clip.SetData(data,0);return clip;}
 public void Footstep(){if(audioSource)audioSource.PlayOneShot(step,.17f);}
 void Event(Fighter fighter,string evt){if(!audioSource)return;AudioClip clip=evt=="jump"||evt=="dodge"?jump:evt=="grab"||evt=="release"?grab:evt=="throw"?throwClip:evt=="impact"||evt=="push_hit"||evt=="throw_hit"?impact:null;if(clip)audioSource.PlayOneShot(clip,fighter==a.Human?.65f:.4f);
  if(evt=="impact"||evt=="throw_hit"||evt=="push_hit"){Burst(fighter.transform.position+Vector3.up,evt=="impact"?new Color(1,.6f,.2f):Color.white,15);a.CameraRig.Impulse=Mathf.Max(a.CameraRig.Impulse,.07f);if(fighter==a.Human)a.HUD.HitUntil=Time.unscaledTime+.13f;}
  if(evt=="dodge")Burst(fighter.transform.position+Vector3.up*.1f,new Color(.3f,.7f,.85f),8);
 }
 void Update(){if(!a)return;if(a.Match.Phase!=phase){phase=a.Match.Phase;if(phase==RoundPhase.Winner){audioSource.PlayOneShot(win,.7f);var winner=a.Match.Announcement.StartsWith(a.LeftName.ToUpperInvariant())?a.Human:a.AI;Burst(winner.transform.position+Vector3.up*2, winner==a.Human?Color.cyan:new Color(1,.45f,.2f),50);}else if(phase==RoundPhase.Fight)audioSource.PlayOneShot(grab,.8f);}
  int tick=Mathf.CeilToInt(a.Match.PhaseEnds-Time.time);if(a.Match.Phase==RoundPhase.Countdown&&tick!=countdown){countdown=tick;audioSource.PlayOneShot(grab,.35f);}}
 void Burst(Vector3 position,Color color,int count){var g=new GameObject("Impact sparks");g.transform.position=position;var p=g.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=p.main;main.duration=.5f;main.loop=false;main.startLifetime=.35f;main.startSpeed=3.5f;main.startSize=.09f;main.startColor=color;main.gravityModifier=1.3f;main.maxParticles=60;var emission=p.emission;emission.enabled=false;var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.15f;if(sparkMaterial)p.GetComponent<ParticleSystemRenderer>().sharedMaterial=sparkMaterial;p.Play();p.Emit(count);Destroy(g,1);}
}
}
