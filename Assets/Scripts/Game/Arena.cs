using System.Collections.Generic;
using UnityEngine;
namespace Playground {
public class Arena:MonoBehaviour {
 public static Arena Instance; public static readonly List<Prop> Props=new();
 public readonly List<Collider> Cover=new();
 public Fighter Human,AI; public AIActionExecutor Executor;public LayaClient Client;public MatchController Match;public Telemetry Telemetry;public LayaDecisionController Brain;public ExperimentSettings Experiments;public ResearchPanel Research;public GameHUD HUD;public GameFeedback Feedback; public OrbitCamera CameraRig; public string Banner="ROOFTOP // SANDBOX"; public int Resets;
 public AIActionExecutor LeftExecutor;public LayaClient LeftClient;public LayaDecisionController LeftBrain;public MatchSetup Setup;public CombatGuide Guide;public TutorialDirector Tutorial;
 public bool Spectating=>Setup&&Setup.Mode==MatchMode.LayaVsTypeSafe;
 public string LeftName=>Spectating?"Laya":"Human";public string RightName=>Setup&&Setup.Mode!=MatchMode.HumanVsLaya?"TypeSafe":"Laya";
 public void StopExecutors(string reason){Executor.Finish(false,reason);LeftExecutor.Finish(false,reason);}
 public static Material Material(Color c,bool glow=false){var template=Resources.Load<Material>("Surface");var m=template?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=c;m.SetFloat("_Smoothness",.25f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*2);}return m;}
 public static GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,bool solid=true,Transform parent=null){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;g.layer=8;if(!solid)Destroy(g.GetComponent<Collider>());return g;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(!FindFirstObjectByType<Arena>())new GameObject("Physics Playground").AddComponent<Arena>();}
 void Awake(){Instance=this;Props.Clear();Build();Experiments=gameObject.AddComponent<ExperimentSettings>();Research=gameObject.AddComponent<ResearchPanel>();Match=gameObject.AddComponent<MatchController>();Telemetry=gameObject.AddComponent<Telemetry>();Brain=gameObject.AddComponent<LayaDecisionController>();Brain.Executor=Executor;Brain.Client=Client;LeftBrain=gameObject.AddComponent<LayaDecisionController>();LeftBrain.Executor=LeftExecutor;LeftBrain.Client=LeftClient;LeftBrain.Enabled=false;Setup=gameObject.AddComponent<MatchSetup>();HUD=gameObject.AddComponent<GameHUD>();Feedback=gameObject.AddComponent<GameFeedback>();Guide=gameObject.AddComponent<CombatGuide>();Tutorial=gameObject.AddComponent<TutorialDirector>();gameObject.AddComponent<ArenaDressing>();Application.targetFrameRate=120;QualitySettings.vSyncCount=1;Human.Event+=s=>Telemetry.LogEvent(Spectating?"avatar_event":"human_event",Human.Actor,s);AI.Event+=s=>Telemetry.LogEvent("avatar_event",AI.Actor,s);}
 System.Collections.IEnumerator Start(){yield return null;var nav=gameObject.AddComponent<Unity.AI.Navigation.NavMeshSurface>();nav.layerMask=1<<8;nav.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;nav.BuildNavMesh();}
 void Build(){
  var floor=Material(new Color(.14f,.21f,.26f));var dark=Material(new Color(.07f,.11f,.15f));var steel=Material(new Color(.3f,.4f,.46f));var yellow=Material(new Color(1,.68f,.16f));var cyan=Material(new Color(.1f,.85f,1),true);
  var roof=Shape("Rooftop foundation",PrimitiveType.Cube,new Vector3(0,-.55f,0),new Vector3(20,1.1f,20),floor);roof.GetComponent<Renderer>().enabled=false;
  VisualAssets.Spawn("rooftop",transform,Vector3.zero,new Color(.1f,.65f,.85f));
  var west=Shape("Vent west",PrimitiveType.Cube,new Vector3(-4.3f,.8f,1.8f),new Vector3(2.4f,1.6f,1.8f),steel);VisualAssets.SkinProxy(west,"vent",Color.cyan);
  var east=Shape("Vent east",PrimitiveType.Cube,new Vector3(4.3f,.8f,-1.8f),new Vector3(2.4f,1.6f,1.8f),steel);VisualAssets.SkinProxy(east,"vent",Color.cyan);Cover.Add(west.GetComponent<Collider>());Cover.Add(east.GetComponent<Collider>());
  for(int i=0;i<16;i++){float a=i*Mathf.PI*2/16;var tower=VisualAssets.Spawn("skyline",transform,new Vector3(Mathf.Cos(a)*43,-12,Mathf.Sin(a)*43),Color.cyan);tower.transform.localScale=new Vector3(1, .6f+(i*7%10)*.13f,1);tower.transform.localRotation=Quaternion.Euler(0,i*37,0);}
  Human=CreateFighter("Human",new Vector3(0,.15f,-5),new Color(.12f,.85f,1));
  AI=CreateFighter("Laya",new Vector3(0,.15f,5),new Color(1,.38f,.18f));Executor=AI.gameObject.AddComponent<AIActionExecutor>();Executor.Self=AI;Executor.Opponent=Human;Client=gameObject.AddComponent<LayaClient>();gameObject.AddComponent<ExecutorDebugPanel>().Executor=Executor;
  LeftExecutor=Human.gameObject.AddComponent<AIActionExecutor>();LeftExecutor.Self=Human;LeftExecutor.Opponent=AI;LeftClient=gameObject.AddComponent<LayaClient>();
  CameraRig=new GameObject("Fixed arena camera").AddComponent<OrbitCamera>();CameraRig.Target=Human;var input=Human.gameObject.AddComponent<HumanInput>();input.Fighter=Human;input.Rig=CameraRig;
  var sun=new GameObject("Warm sunset").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.65f;sun.color=new Color(1,.83f,.65f);sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.33f,.42f,.55f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.12f,.18f,.27f);RenderSettings.fogDensity=.012f;
  var kill=new GameObject("Knockout volume");kill.transform.position=new Vector3(0,-5,0);var trigger=kill.AddComponent<BoxCollider>();trigger.size=new Vector3(150,4,150);trigger.isTrigger=true;kill.AddComponent<KnockoutVolume>();
  AddProp("crate",new Vector3(-2,.6f,-3),8,new Color(.77f,.49f,.22f));AddProp("crate",new Vector3(2,.6f,3),8,new Color(.77f,.49f,.22f));
  AddProp("barrel",new Vector3(2,.7f,-4),16,new Color(.95f,.37f,.15f));AddProp("barrel",new Vector3(-2,.7f,4),16,new Color(.95f,.37f,.15f));
  AddProp("chair",new Vector3(-5,.65f,-4),4,new Color(.36f,.7f,.65f));AddProp("chair",new Vector3(5,.65f,4),4,new Color(.36f,.7f,.65f));
  AddProp("table",new Vector3(-6,.55f,5),40,new Color(.45f,.5f,.55f));AddProp("table",new Vector3(6,.55f,-5),40,new Color(.45f,.5f,.55f));
 }
 public Fighter CreateFighter(string name,Vector3 pos,Color color){
  var g=new GameObject(name);g.transform.position=pos;g.AddComponent<CapsuleCollider>();var f=g.AddComponent<Fighter>();f.Actor=name;
  var visual=new GameObject("Armored robot").transform;visual.SetParent(g.transform,false);f.Visual=visual;
  VisualAssets.Spawn("robot_body",visual,Vector3.zero,color);var head=VisualAssets.Spawn("robot_head",visual,Vector3.up*1.44f,color);head.name="Helmet";
  for(int i=-1;i<=1;i+=2){var glove=VisualAssets.Spawn("robot_glove",visual,new Vector3(i*.52f,.88f,.12f),color);glove.name="Glove";var boot=VisualAssets.Spawn("robot_boot",visual,new Vector3(i*.21f,.16f,.08f),color);boot.name="Boot";}
  f.SnapFace(-pos);g.AddComponent<FighterPresentation>();return f;
 }
 void AddProp(string kind,Vector3 pos,float mass,Color color){var mat=Material(color);var scale=kind=="barrel"?new Vector3(.85f,.65f,.85f):kind=="chair"?new Vector3(.8f,.45f,.8f):kind=="table"?new Vector3(2.1f,1,1.3f):Vector3.one;
  var g=Shape(kind,kind=="barrel"?PrimitiveType.Cylinder:PrimitiveType.Cube,pos,scale,mat);var prop=g.AddComponent<Prop>();prop.Init(kind,mass);g.name=kind+"-"+Props.Count;
  VisualAssets.SkinProxy(g,kind=="table"?"cargo":kind,kind=="chair"?new Color(.12f,.54f,.56f):color);
 }
 public void ResetArena(){if(Match)Match.Generation++;StopExecutors("reset");AI.ResetAt(new Vector3(0,.15f,5));Human.ResetAt(new Vector3(0,.15f,-5));ApplyLayout();foreach(var p in Props)p.ResetProp();Resets++;Banner="BACK ON THE ROOF";}
 public int CurrentLayout=>Tutorial&&Tutorial.Running?0:Match?(Match.Layout==3?(Match.Round-1)%3:Match.Layout):0;
 public string LayoutName=>new[]{"Foundry","Crossfire","Flank"}[CurrentLayout];
 void ApplyLayout(){
  Vector3[] points=CurrentLayout==1?new[]{new Vector3(-2,.6f,0),new Vector3(2,.6f,0),new Vector3(0,.7f,-2),new Vector3(0,.7f,2),new Vector3(-5,.65f,-4),new Vector3(5,.65f,4),new Vector3(-6,.55f,5),new Vector3(6,.55f,-5)}:
   CurrentLayout==2?new[]{new Vector3(-6,.6f,-2),new Vector3(6,.6f,2),new Vector3(3,.7f,-1),new Vector3(-3,.7f,1),new Vector3(-2,.65f,-4),new Vector3(2,.65f,4),new Vector3(-1,.55f,3),new Vector3(1,.55f,-3)}:
   new[]{new Vector3(-2,.6f,-3),new Vector3(2,.6f,3),new Vector3(2,.7f,-4),new Vector3(-2,.7f,4),new Vector3(-5,.65f,-4),new Vector3(5,.65f,4),new Vector3(-6,.55f,5),new Vector3(6,.55f,-5)};
  for(int i=0;i<Props.Count&&i<points.Length;i++)Props[i].SetSpawn(points[i]);
 }
 public void RingOut(Fighter f){if(Tutorial&&Tutorial.Running){Tutorial.RingOut(f);return;}if(Match)Match.RingOut(f);else ResetArena();}
 void Update(){if(Human.transform.position.y<-7)RingOut(Human);if(AI.transform.position.y<-7)RingOut(AI);}

}
public class KnockoutVolume:MonoBehaviour {void OnTriggerEnter(Collider c){var f=c.GetComponent<Fighter>();if(f&&Arena.Instance)Arena.Instance.RingOut(f);}}
}
