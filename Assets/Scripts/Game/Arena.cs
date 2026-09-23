using System.Collections.Generic;
using UnityEngine;
namespace Playground {
public class Arena:MonoBehaviour {
 public static Arena Instance; public static readonly List<Prop> Props=new();
 public Fighter Human,AI; public AIActionExecutor Executor;public LayaClient Client;public MatchController Match;public Telemetry Telemetry;public LayaDecisionController Brain;public GameHUD HUD;public GameFeedback Feedback; public OrbitCamera CameraRig; public string Banner="ROOFTOP // SANDBOX"; public int Resets;
 public static Material Material(Color c,bool glow=false){var template=Resources.Load<Material>("Surface");var m=template?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=c;m.SetFloat("_Smoothness",.25f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*2);}return m;}
 public static GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,bool solid=true,Transform parent=null){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;g.layer=8;if(!solid)Destroy(g.GetComponent<Collider>());return g;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(!FindFirstObjectByType<Arena>())new GameObject("Physics Playground").AddComponent<Arena>();}
 void Awake(){Instance=this;Props.Clear();Build();Match=gameObject.AddComponent<MatchController>();Telemetry=gameObject.AddComponent<Telemetry>();Brain=gameObject.AddComponent<LayaDecisionController>();HUD=gameObject.AddComponent<GameHUD>();Feedback=gameObject.AddComponent<GameFeedback>();Application.targetFrameRate=120;QualitySettings.vSyncCount=1;Human.Event+=s=>Telemetry.LogEvent("human_event","Human",s);AI.Event+=s=>Telemetry.LogEvent("avatar_event","Laya",s);}
 System.Collections.IEnumerator Start(){yield return null;var nav=gameObject.AddComponent<Unity.AI.Navigation.NavMeshSurface>();nav.layerMask=1<<8;nav.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;nav.BuildNavMesh();}
 void Build(){
  var floor=Material(new Color(.14f,.21f,.26f));var dark=Material(new Color(.07f,.11f,.15f));var steel=Material(new Color(.3f,.4f,.46f));var yellow=Material(new Color(1,.68f,.16f));var cyan=Material(new Color(.1f,.85f,1),true);
  Shape("Rooftop foundation",PrimitiveType.Cube,new Vector3(0,-.55f,0),new Vector3(20,1.1f,20),floor);
  for(int i=-9;i<=9;i+=3){Shape("Deck seam",PrimitiveType.Cube,new Vector3(i,.005f,0),new Vector3(.025f,.01f,19),steel,false);Shape("Deck seam",PrimitiveType.Cube,new Vector3(0,.005f,i),new Vector3(19,.01f,.025f),steel,false);}
  for(int side=0;side<4;side++){float angle=side*90;var pivot=new GameObject("Edge markings").transform;pivot.rotation=Quaternion.Euler(0,angle,0);for(int i=-9;i<=9;i++){var mark=Shape("Caution stripe",PrimitiveType.Cube,new Vector3(i,.018f,9.7f),new Vector3(.6f,.03f,.4f),yellow,false,pivot);mark.transform.localRotation=Quaternion.Euler(0,-30,0);}Shape("Perimeter light",PrimitiveType.Cube,new Vector3(0,-.25f,10.02f),new Vector3(19,.06f,.06f),cyan,false,pivot);}
  Shape("Vent west",PrimitiveType.Cube,new Vector3(-4.3f,.8f,1.8f),new Vector3(2.4f,1.6f,1.8f),steel);
  Shape("Vent east",PrimitiveType.Cube,new Vector3(4.3f,.8f,-1.8f),new Vector3(2.4f,1.6f,1.8f),steel);
  for(int i=0;i<7;i++){Shape("Vent slat",PrimitiveType.Cube,new Vector3(-5.2f+i*.3f,1.62f,1.8f),new Vector3(.1f,.04f,1.5f),dark,false);Shape("Vent slat",PrimitiveType.Cube,new Vector3(3.4f+i*.3f,1.62f,-1.8f),new Vector3(.1f,.04f,1.5f),dark,false);}
  // Distant skyline is decoration only; it cannot affect physics or perception.
  for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20;float h=8+(i*7%13);Shape("City silhouette",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*46,-16+h/2,Mathf.Sin(a)*46),new Vector3(5,h,6),dark,false);}
  Human=CreateFighter("Human",new Vector3(0,.15f,-5),new Color(.12f,.85f,1));
  AI=CreateFighter("Laya",new Vector3(0,.15f,5),new Color(1,.38f,.18f));Executor=AI.gameObject.AddComponent<AIActionExecutor>();Executor.Self=AI;Executor.Opponent=Human;Client=gameObject.AddComponent<LayaClient>();gameObject.AddComponent<ExecutorDebugPanel>().Executor=Executor;
  CameraRig=new GameObject("Third person Cinemachine rig").AddComponent<OrbitCamera>();CameraRig.Target=Human;CameraRig.transform.position=new Vector3(0,6,-12);var input=Human.gameObject.AddComponent<HumanInput>();input.Fighter=Human;input.Rig=CameraRig;
  var sun=new GameObject("Warm sunset").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.2f;sun.color=new Color(1,.83f,.65f);sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.53f,.68f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.12f,.18f,.27f);RenderSettings.fogDensity=.012f;
  var kill=new GameObject("Knockout volume");kill.transform.position=new Vector3(0,-5,0);var trigger=kill.AddComponent<BoxCollider>();trigger.size=new Vector3(150,4,150);trigger.isTrigger=true;kill.AddComponent<KnockoutVolume>();
  AddProp("crate",new Vector3(-2,.6f,-3),8,new Color(.77f,.49f,.22f));AddProp("crate",new Vector3(2,.6f,3),8,new Color(.77f,.49f,.22f));
  AddProp("barrel",new Vector3(2,.7f,-4),16,new Color(.95f,.37f,.15f));AddProp("barrel",new Vector3(-2,.7f,4),16,new Color(.95f,.37f,.15f));
  AddProp("chair",new Vector3(-5,.65f,-4),4,new Color(.36f,.7f,.65f));AddProp("chair",new Vector3(5,.65f,4),4,new Color(.36f,.7f,.65f));
  AddProp("table",new Vector3(-6,.55f,5),40,new Color(.45f,.5f,.55f));AddProp("table",new Vector3(6,.55f,-5),40,new Color(.45f,.5f,.55f));
 }
 public Fighter CreateFighter(string name,Vector3 pos,Color color){var g=new GameObject(name);g.transform.position=pos;g.AddComponent<CapsuleCollider>();var f=g.AddComponent<Fighter>();f.Actor=name;var mat=Material(color);var black=Material(new Color(.03f,.06f,.1f));var visual=new GameObject("Robot shell").transform;visual.SetParent(g.transform,false);f.Visual=visual;Shape("Body",PrimitiveType.Capsule,new Vector3(0,.85f,0),new Vector3(.78f,.6f,.7f),mat,false,visual);Shape("Helmet",PrimitiveType.Sphere,new Vector3(0,1.6f,0),new Vector3(.73f,.65f,.72f),mat,false,visual);Shape("Visor",PrimitiveType.Cube,new Vector3(0,1.63f,.33f),new Vector3(.53f,.18f,.1f),black,false,visual);for(int i=-1;i<=1;i+=2){Shape("Glove",PrimitiveType.Sphere,new Vector3(i*.52f,.88f,.12f),Vector3.one*.3f,mat,false,visual);Shape("Boot",PrimitiveType.Cube,new Vector3(i*.21f,.16f,.08f),new Vector3(.28f,.3f,.45f),black,false,visual);}f.Face(-pos);g.AddComponent<FighterPresentation>();return f;}
 void AddProp(string kind,Vector3 pos,float mass,Color color){var mat=Material(color);var scale=kind=="barrel"?new Vector3(.85f,.65f,.85f):kind=="chair"?new Vector3(.8f,.45f,.8f):kind=="table"?new Vector3(2.1f,1,1.3f):Vector3.one;
  var g=Shape(kind,kind=="barrel"?PrimitiveType.Cylinder:PrimitiveType.Cube,pos,scale,mat);var prop=g.AddComponent<Prop>();prop.Init(kind,mass);g.name=kind+"-"+Props.Count;
  if(kind=="chair")Shape("Chair back",PrimitiveType.Cube,new Vector3(0,.8f,-.4f),new Vector3(1,1.6f,.16f),mat,false,g.transform);
  if(kind=="crate")for(int i=-1;i<=1;i+=2)Shape("Crate band",PrimitiveType.Cube,new Vector3(i*.32f,0,0),new Vector3(.07f,1.02f,1.02f),Material(new Color(.23f,.17f,.12f)),false,g.transform);
 }
 public void ResetArena(){if(Match)Match.Generation++;Executor.Finish(false,"reset");AI.ResetAt(new Vector3(0,.15f,5));Human.ResetAt(new Vector3(0,.15f,-5));foreach(var p in Props)p.ResetProp();Resets++;Banner="BACK ON THE ROOF";}
 public void RingOut(Fighter f){if(Match)Match.RingOut(f);else ResetArena();}
 void Update(){if(Human.transform.position.y<-7)RingOut(Human);if(AI.transform.position.y<-7)RingOut(AI);}

}
public class KnockoutVolume:MonoBehaviour {void OnTriggerEnter(Collider c){var f=c.GetComponent<Fighter>();if(f&&Arena.Instance)Arena.Instance.RingOut(f);}}
}
