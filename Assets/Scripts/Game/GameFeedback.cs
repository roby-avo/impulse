using UnityEngine;
namespace Playground {
public class GameFeedback:MonoBehaviour {
 public const float HitStopSeconds=.065f,HitStopScale=.04f,FallSlowScale=.3f,FallSlowSeconds=.65f;
 Arena a;AudioSource audioSource,music,wind;AudioClip impact,jump,grab,throwClip,step,win,whoosh,stinger;RoundPhase phase;int countdown;Material sparkMaterial;AudioSource[] voices;int nextVoice;ParticleSystem[] bursts;int nextBurst;
 float hitStopUntil,slowUntil,slowStarted;readonly bool[] falling=new bool[2];
 public bool TimeEffectActive=>Time.unscaledTime<hitStopUntil||Time.unscaledTime<slowUntil;public int BurstPoolSize=>bursts?.Length??0;public bool MusicPlaying=>music&&music.isPlaying;
 void Start(){a=Arena.Instance;audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.volume=.4f;impact=Tone("Impact",95,.19f,true);jump=Tone("Jump",320,.15f,false);grab=Tone("Grab",650,.07f,false);throwClip=Tone("Throw",160,.17f,true);step=Tone("Step",80,.055f,true);win=Tone("Victory",540,.5f,false);whoosh=Whoosh();stinger=Stinger();sparkMaterial=Resources.Load<Material>("Particles");voices=new AudioSource[12];for(int i=0;i<voices.Length;i++){var g=new GameObject("Game audio voice");g.transform.SetParent(transform);var voice=g.AddComponent<AudioSource>();voice.spatialBlend=.65f;voice.minDistance=15;voice.maxDistance=70;voices[i]=voice;}a.Human.Event+=s=>Event(a.Human,s);a.AI.Event+=s=>Event(a.AI,s);
  bursts=new ParticleSystem[16];for(int i=0;i<bursts.Length;i++)bursts[i]=CreateBurst();
  music=Loop("Music",Music(),0);wind=Loop("Rooftop wind",Wind(),0);SetMusicVolume(PlayerPrefs.GetFloat("music_volume",.6f));}
 AudioSource Loop(string name,AudioClip clip,float volume){var g=new GameObject(name);g.transform.SetParent(transform);var s=g.AddComponent<AudioSource>();s.clip=clip;s.loop=true;s.spatialBlend=0;s.volume=volume;s.ignoreListenerPause=true;s.Play();return s;}
 public void SetMusicVolume(float value){value=Mathf.Clamp01(value);PlayerPrefs.SetFloat("music_volume",value);if(music)music.volume=.22f*value;if(wind)wind.volume=.05f+.1f*value;}
 AudioClip Tone(string name,float frequency,float length,bool noise){int n=(int)(22050*length);var data=new float[n];for(int i=0;i<n;i++){float t=i/22050f;float envelope=Mathf.Pow(1-i/(float)n,2);float random=Mathf.Sin(i*78.233f)*43758.5453f;random=(random-Mathf.Floor(random))-.5f;float metallic=Mathf.Sin(2*Mathf.PI*frequency*t)*.4f+Mathf.Sin(2*Mathf.PI*frequency*2.73f*t)*.16f+Mathf.Sin(2*Mathf.PI*frequency*5.17f*t)*.08f;
 data[i]=(noise?metallic+random*.55f*Mathf.Exp(-t*40):Mathf.Sin(2*Mathf.PI*(frequency*t+100*t*t))*.45f)*envelope;}var clip=AudioClip.Create(name,n,1,22050,false);clip.SetData(data,0);return clip;}
 static float Noise(int i){float r=Mathf.Sin(i*12.9898f+78.233f)*43758.5453f;return (r-Mathf.Floor(r))*2-1;}
 // Falling air: noise through a low-pass filter whose cutoff sweeps down as the robot drops.
 static AudioClip Whoosh(){int n=(int)(22050*.9f);var data=new float[n];float low=0;for(int i=0;i<n;i++){float t=i/(float)n;float cutoff=Mathf.Lerp(.35f,.03f,t);low+=(Noise(i)-low)*cutoff;float envelope=Mathf.Sin(Mathf.PI*Mathf.Pow(t,.6f));data[i]=low*envelope*1.6f;}var clip=AudioClip.Create("Fall whoosh",n,1,22050,false);clip.SetData(data,0);return clip;}
 // Match win: a rising major arpeggio that resolves into a held chord.
 static AudioClip Stinger(){int n=(int)(22050*1.6f);var data=new float[n];float[] notes={523.25f,659.25f,783.99f,1046.5f};for(int i=0;i<n;i++){float t=i/22050f;float sum=0;for(int k=0;k<notes.Length;k++){float start=k*.12f;if(t<start)continue;float local=t-start;float env=Mathf.Min(1,local*40)*Mathf.Exp(-local*(k==notes.Length-1?1.6f:2.4f));sum+=(Mathf.Sin(2*Mathf.PI*notes[k]*local)+.3f*Mathf.Sin(4*Mathf.PI*notes[k]*local))*env*.22f;}data[i]=sum;}var clip=AudioClip.Create("Match stinger",n,1,22050,false);clip.SetData(data,0);return clip;}
 // A soft, seamless 8-bar loop (100 BPM, Am–F–C–G): sub bass pulse, slow pad and a quiet pluck.
 static AudioClip Music(){const int rate=22050;const float beat=.6f;int bars=8,n=(int)(rate*beat*4*bars);var data=new float[n];
  float[][] chords={new[]{110f,220f,261.63f,329.63f},new[]{87.31f,174.61f,220f,261.63f},new[]{130.81f,196f,261.63f,329.63f},new[]{98f,196f,246.94f,293.66f}};
  for(int bar=0;bar<bars;bar++){var chord=chords[bar%4];int barStart=(int)(bar*4*beat*rate),barLength=(int)(4*beat*rate);
   for(int i=0;i<barLength;i++){float t=i/(float)rate;float pad=0;for(int k=1;k<4;k++)pad+=Mathf.Sin(2*Mathf.PI*chord[k]*t)+.25f*Mathf.Sin(2*Mathf.PI*chord[k]*2.003f*t);float padEnv=Mathf.Clamp01(t/.8f)*Mathf.Clamp01((barLength/(float)rate-t)/.5f);
    float eighth=t%(beat/2);float bass=Mathf.Sin(2*Mathf.PI*chord[0]*t)*Mathf.Exp(-eighth*7)*(.55f+.45f*Mathf.Sin(2*Mathf.PI*chord[0]*2*t));
    int step=(int)(t/(beat/4));float sixteenth=t%(beat/4);float pluckNote=chord[1+step%3]*2;float pluck=step%2==0?Mathf.Sin(2*Mathf.PI*pluckNote*sixteenth)*Mathf.Exp(-sixteenth*18):0;
    data[barStart+i]=pad*padEnv*.05f+bass*.32f+pluck*.07f;}}
  var clip=AudioClip.Create("Rooftop loop",n,1,rate,false);clip.SetData(data,0);return clip;}
 // Rooftop wind: filtered noise with slow gusts; the tail is crossfaded into the head so it loops cleanly.
 static AudioClip Wind(){const int rate=22050;int n=rate*8,fade=rate/2;var raw=new float[n+fade];float low=0,lower=0;for(int i=0;i<raw.Length;i++){low+=(Noise(i)-low)*.08f;lower+=(low-lower)*.05f;float t=i/(float)rate;raw[i]=lower*3f*(.6f+.4f*Mathf.Sin(2*Mathf.PI*t/4f)*Mathf.Sin(2*Mathf.PI*t/8f+1));}
  var data=new float[n];for(int i=0;i<n;i++)data[i]=raw[i];for(int i=0;i<fade;i++){float w=i/(float)fade;data[i]=raw[i]*w+raw[n+i]*(1-w);}var clip=AudioClip.Create("Rooftop wind",n,1,rate,false);clip.SetData(data,0);return clip;}
 public void Footstep(Vector3 position){PlayAt(step,position,.13f);}
 void PlayAt(AudioClip clip,Vector3 position,float volume){if(!clip||voices==null)return;var source=voices[nextVoice++%voices.Length];source.transform.position=position;source.clip=clip;source.volume=volume;source.pitch=Random.Range(.94f,1.06f);source.Play();}
 public void Contact(Vector3 point,Vector3 direction,float strength){Burst(point,Color.Lerp(new Color(.3f,.85f,1),new Color(1,.65f,.25f),strength),12);PlayAt(impact,point,.4f+strength*.2f);if(strength>=1)a.CameraRig.Kick(direction,strength);}
 // Brief freezes sell contact; they never run while paused and always hand back normal time.
 public void HitStop(float seconds=HitStopSeconds){hitStopUntil=Mathf.Max(hitStopUntil,Time.unscaledTime+seconds);}
 public void SlowMotion(float seconds=FallSlowSeconds){slowStarted=Time.unscaledTime;slowUntil=slowStarted+seconds;}
 void ApplyTime(){if(a.HUD.Paused)return;float now=Time.unscaledTime,scale=1;if(now<hitStopUntil)scale=HitStopScale;else if(now<slowUntil){float t=(now-slowStarted)/Mathf.Max(.01f,slowUntil-slowStarted);scale=t<.6f?FallSlowScale:Mathf.Lerp(FallSlowScale,1,(t-.6f)/.4f);}if(!Mathf.Approximately(Time.timeScale,scale))Time.timeScale=scale;}
 void Event(Fighter fighter,string evt){if(!audioSource)return;AudioClip clip=evt=="jump"||evt=="dodge"?jump:evt=="land"?step:evt=="grab"||evt=="release"?grab:evt=="throw"?throwClip:evt=="push_windup"?throwClip:null;if(clip)PlayAt(clip,fighter.transform.position,evt=="push_windup"?.2f:.35f);
  if(evt=="impact"){Contact(fighter.ContactPoint,fighter.HitDirection,1);HitStop();if(fighter==a.Human)a.HUD.HitUntil=Time.unscaledTime+.16f;}
  if(evt=="dodge")Burst(fighter.transform.position+Vector3.up*.1f,new Color(.3f,.7f,.85f),8);
  if(evt=="vent_launch"){PlayAt(whoosh,fighter.transform.position,.45f);Burst(fighter.transform.position,new Color(.45f,1,1),16);}
  if(evt=="ledge_save"){PlayAt(jump,fighter.transform.position,.5f);Burst(fighter.transform.position,new Color(1,.9f,.4f),18);}
  if(evt=="push_charged")PlayAt(impact,fighter.transform.position,.3f);
 }
 // The moment a robot drops past the roof edge: whoosh and a short slow-motion beat.
 void WatchFalls(){bool live=a.Match.Phase==RoundPhase.Fight||a.Tutorial.Running;for(int i=0;i<2;i++){var f=i==0?a.Human:a.AI;var p=f.transform.position;bool over=live&&p.y<-.4f&&Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.z))>9.4f;if(over&&!falling[i]){falling[i]=true;PlayAt(whoosh,p,.7f);if(!a.Tutorial.Running)SlowMotion();}else if(!live||p.y>0)falling[i]=false;}}
 void Update(){if(!a)return;WatchFalls();ApplyTime();if(a.Match.Phase!=phase){phase=a.Match.Phase;if(phase==RoundPhase.Winner){var winner=a.Match.LastRoundWinner;if(winner){audioSource.PlayOneShot(win,.7f);Burst(winner.transform.position+Vector3.up*2,winner==a.Human?Color.cyan:new Color(1,.45f,.2f),50);}else audioSource.PlayOneShot(impact,.5f);}else if(phase==RoundPhase.MatchOver)audioSource.PlayOneShot(stinger,.8f);else if(phase==RoundPhase.Fight)audioSource.PlayOneShot(grab,.8f);}
  int tick=Mathf.CeilToInt(a.Match.PhaseEnds-Time.time);if(a.Match.Phase==RoundPhase.Countdown&&tick!=countdown){countdown=tick;audioSource.PlayOneShot(grab,.35f);}}
 ParticleSystem CreateBurst(){var g=new GameObject("Impact sparks");g.transform.SetParent(transform);var p=g.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=p.main;main.duration=.5f;main.loop=false;main.startLifetime=.35f;main.startSpeed=3.5f;main.startSize=.09f;main.gravityModifier=1.3f;main.maxParticles=60;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=false;var emission=p.emission;emission.enabled=false;var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.15f;if(sparkMaterial)p.GetComponent<ParticleSystemRenderer>().sharedMaterial=sparkMaterial;return p;}
 // Pooled bursts: world-space particles keep flying when their emitter is reused elsewhere.
 void Burst(Vector3 position,Color color,int count){var p=bursts[nextBurst++%bursts.Length];p.transform.position=position;var main=p.main;main.startColor=color;if(!p.isPlaying)p.Play();p.Emit(count);}
}
}
