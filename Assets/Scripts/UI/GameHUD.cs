using UnityEngine;
using UnityEngine.InputSystem;
namespace Playground {
public class GameHUD:MonoBehaviour {
 public bool Paused;public float HitUntil;Arena a;GUIStyle small,body,title,big,center,button;Texture2D pixel;bool wasOver;
 readonly Color cyan=new(.2f,.88f,1),orange=new(1,.43f,.22f),muted=new(.65f,.73f,.79f),panel=new(.035f,.06f,.085f,.94f);
 void Start(){a=Arena.Instance;}
 void Update(){if(!a)return;var k=Keyboard.current;if(k!=null&&k.tabKey.isPressed){}bool over=a.Match.Phase==RoundPhase.MatchOver;if(over&&!wasOver)a.Human.GetComponent<HumanInput>().Capture(false);wasOver=over;}
 public void Pause(bool value){Paused=value;Time.timeScale=value?0:1;a.Human.GetComponent<HumanInput>().Capture(!value);}
 Texture2D ButtonTexture(Color color){var texture=new Texture2D(1,1);texture.SetPixel(0,0,color);texture.Apply();return texture;}
 void Styles(){if(body!=null)return;pixel=Texture2D.whiteTexture;body=new GUIStyle(GUI.skin.label){fontSize=17,normal={textColor=Color.white}};small=new GUIStyle(body){fontSize=13,normal={textColor=muted}};title=new GUIStyle(body){fontSize=24,fontStyle=FontStyle.Bold};big=new GUIStyle(title){fontSize=44,alignment=TextAnchor.MiddleCenter};center=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};button=new GUIStyle(GUI.skin.button){fontSize=18,padding=new RectOffset(10,10,12,12)};button.normal.background=ButtonTexture(new Color(.1f,.22f,.29f));button.hover.background=ButtonTexture(new Color(.14f,.34f,.42f));button.active.background=ButtonTexture(new Color(.12f,.48f,.56f));button.normal.textColor=button.hover.textColor=button.active.textColor=Color.white;}
 void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),pixel);GUI.color=Color.white;}
 void Text(float x,float y,float w,float h,string text,GUIStyle style,Color? color=null){var old=GUI.color;GUI.color=color??Color.white;GUI.Label(new Rect(x,y,w,h),text,style);GUI.color=old;}
 void OnGUI(){if(!a)return;Styles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1440f,Screen.height/900f,1));
  Box(24,22,315,78,panel);Box(24,22,4,78,cyan);Text(42,31,280,30,"PHYSICS / PLAYGROUND",body);Text(42,65,280,24,"ROOFTOP 01   /   RING-OUT DUEL",small);
  Box(539,22,362,88,panel);Text(554,34,85,25,"YOU",center,cyan);Text(801,34,85,25,"LAYA",center,orange);Text(629,23,182,60,$"{a.Match.HumanWins}  :  {a.Match.AIWins}",big);Text(569,80,302,20,$"FIRST TO {a.Match.WinsRequired}     ·     ROUND {a.Match.Round}",small);
  Box(1060,22,356,98,panel);Box(1077,41,7,7,a.Client.Failures>0&&!a.Client.Busy&&a.Client.Status.Contains("unavailable")?orange:cyan);Text(1093,32,305,23,"LOCAL LAYA  /  SYSTEM 1",small);Text(1077,58,320,25,a.Client.Busy?"Thinking…":(a.Brain.ExecutedDecisions>0?a.Executor.Selected.ToString().Replace('_',' '):"Connecting to local model"),small);Text(1077,86,320,22,a.Client.Status.Contains("unavailable")?"OFFLINE · Waiting and retrying":$"Decision latency  {a.Client.LastLatency:0} ms",small);
  if(a.Match.Practice)Text(430,130,580,30,"EXECUTOR LAB · PRACTICE · SCORING DISABLED",center,orange);
  if(a.Match.Phase==RoundPhase.Countdown){Text(420,235,600,65,Mathf.CeilToInt(a.Match.PhaseEnds-Time.time).ToString(),big);Text(420,301,600,30,"STAY ON THE ROOF. SEND LAYA OFF.",center);}
  if(a.Match.Phase==RoundPhase.Winner){Box(340,232,760,110,panel);Text(350,250,740,48,a.Match.Announcement,title);Text(350,300,740,25,"Next round in a moment",center);}
  if(a.Match.Phase==RoundPhase.Fight&&!Paused){if(a.Human.Held&&Camera.main){float speed=a.Human.Held.Body.mass>10?14:18;float flight=6/speed;Vector3 point=a.Human.transform.position+Vector3.up*1.25f+a.Human.transform.forward*1.05f+a.CameraRig.Aim*speed*flight+Physics.gravity*(.5f*flight*flight);Vector3 screen=Camera.main.WorldToScreenPoint(point);float x=screen.x*1440/Screen.width,y=(Screen.height-screen.y)*900/Screen.height;if(screen.z>0){Box(x-6,y,12,2,Color.white);Box(x-1,y-5,2,12,Color.white);}}
   string interaction=a.Human.Held?"LEFT CLICK  ·  THROW "+a.Human.Held.Kind.ToUpper():a.Human.Nearest()?"E  ·  GRAB "+a.Human.Nearest().Kind.ToUpper():"F  ·  PUSH";
   Box(540,751,360,37,panel);Text(550,757,340,27,interaction,center);
   if(a.Human.Edge<1.8f){Text(535,698,370,34,"OPEN EDGE · WATCH YOUR STEP",center,orange);Box(0,0,6,900,orange);Box(1434,0,6,900,orange);}
  }
  Box(24,789,286,69,panel);Text(40,798,260,22,a.Human.Held?"HOLDING  /  "+a.Human.Held.Kind.ToUpper():"HANDS FREE",body,cyan);Text(40,826,260,22,Time.time<a.Human.StunnedUntil?"OFF BALANCE":Time.time<a.Human.NextDodge?$"Dodge ready in {a.Human.NextDodge-Time.time:0.0}s":"DODGE READY  /  LEFT SHIFT",small);
  Box(334,810,1082,48,panel);Text(354,822,1040,30,"WASD move   ·   Mouse look   ·   Space jump   ·   E grab / drop   ·   Click throw   ·   F push   ·   Tab stats   ·   Esc pause",small);
  if(Time.unscaledTime<HitUntil){Box(0,0,1440,7,new Color(1,.5f,.2f,.7f));Box(0,893,1440,7,new Color(1,.5f,.2f,.7f));}
  bool stats=(Keyboard.current!=null&&Keyboard.current.tabKey.isPressed)||a.Match.Phase==RoundPhase.MatchOver;
  if(stats)Results();if(Paused)PausePanel();
 }
 void Results(){Box(24,147,247,390,panel);Text(42,168,210,26,"LAYA DECISIONS",body);string[] names={"Approach","Retreat","Push","Grab","Throw","Dodge left","Dodge right","Move to safety","Take cover","Jump"};for(int j=0;j<10;j++){Text(42,207+j*30,170,26,names[j],small);Text(210,207+j*30,50,26,a.Telemetry.Distribution[j].ToString(),body,orange);}
  Box(295,147,850,603,panel);Box(295,147,850,3,cyan);Text(325,174,790,45,a.Match.Phase==RoundPhase.MatchOver?a.Match.Announcement:"MATCH SNAPSHOT",title);Text(325,217,790,25,"HUMAN vs LAYA  /  SAME PHYSICS. DIFFERENT DECISIONS.",small);
  Text(786,260,130,25,"YOU",body,cyan);Text(946,260,130,25,"LAYA",body,orange);var h=a.Human;var l=a.AI;
  string[] labels={"Round wins","Self ring-outs","Objects grabbed / thrown","Throw hit rate","Successful pushes","Dodges","Time near edge","Average edge distance","Opponent-caused ring-outs"};
  string[] left={a.Match.HumanWins.ToString(),a.Match.HumanSelfOuts.ToString(),$"{h.Grabs} / {h.Throws}",h.Throws>0?$"{100f*h.Hits/h.Throws:0}%":"—",h.Pushes.ToString(),h.Dodges.ToString(),$"{h.EdgeTime:0.0}s",$"{h.EdgeSum/Mathf.Max(.01f,h.Samples):0.0} m",a.Match.HumanEnvironmentalKOs.ToString()};
  string[] right={a.Match.AIWins.ToString(),a.Match.AISelfOuts.ToString(),$"{l.Grabs} / {l.Throws}",l.Throws>0?$"{100f*l.Hits/l.Throws:0}%":"—",l.Pushes.ToString(),l.Dodges.ToString(),$"{l.EdgeTime:0.0}s",$"{l.EdgeSum/Mathf.Max(.01f,l.Samples):0.0} m",a.Match.AIEnvironmentalKOs.ToString()};
  for(int i=0;i<labels.Length;i++){int y=295+i*32;Box(325,y+28,790,1,new Color(1,1,1,.07f));Text(325,y,430,28,labels[i],body);Text(786,y,130,28,left[i],body,cyan);Text(946,y,130,28,right[i],body,orange);}
  Text(325,596,790,25,$"Laya: {a.Telemetry.DecisionCount} decisions  ·  Mean latency {a.Telemetry.TotalLatency/Mathf.Max(1,a.Telemetry.DecisionCount):0} ms",body);
  int most=0;for(int i=1;i<10;i++)if(a.Telemetry.Distribution[i]>a.Telemetry.Distribution[most])most=i;Text(325,626,790,23,"Most selected: "+((SemanticAction)most).ToString().Replace('_',' ')+"  ·  Full distribution saved in decision log",small);
  if(a.Match.Phase==RoundPhase.MatchOver){if(GUI.Button(new Rect(525,674,390,48),"PLAY AGAIN   /   R",button)){Pause(false);a.Match.NewMatch();}}
  else Text(325,674,790,30,"Release TAB to return to the roof",center);
 }
 void PausePanel(){Box(0,0,1440,900,new Color(.015f,.03f,.05f,.8f));Box(480,255,480,360,panel);Text(520,282,400,60,"PAUSED",big);Text(515,351,410,30,"Your next move can wait.",center);if(GUI.Button(new Rect(530,408,380,45),"RESUME",button))Pause(false);if(GUI.Button(new Rect(530,465,380,45),"NEW MATCH",button)){Pause(false);a.Match.NewMatch();}if(GUI.Button(new Rect(530,522,380,45),"QUIT GAME",button))Application.Quit();}
 void OnDestroy(){Time.timeScale=1;}
}
}
