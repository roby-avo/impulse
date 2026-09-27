using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Playground {
public class ArenaDressing:MonoBehaviour {
 VolumeProfile grading;LineRenderer boundary;Transform[] markers=new Transform[2];
 void Start(){
  var sky=Resources.Load<Shader>("SkyGradient");if(sky)RenderSettings.skybox=new Material(sky);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.38f,.49f,.64f);RenderSettings.ambientEquatorColor=new Color(.34f,.36f,.39f);RenderSettings.ambientGroundColor=new Color(.12f,.15f,.20f);
  RenderSettings.fogColor=new Color(.38f,.40f,.45f);RenderSettings.fogDensity=.006f;
  var distant=Arena.Material(new Color(.23f,.29f,.35f));
  for(int i=0;i<36;i++){float angle=i*Mathf.PI*2/36;float radius=64+(i%3)*13;float height=9+(i*13%14);Arena.Shape("Distant skyline",PrimitiveType.Cube,new Vector3(Mathf.Cos(angle)*radius,-24+height*.5f,Mathf.Sin(angle)*radius),new Vector3(5+i%4,height,5+i%3),distant,false,transform);}
  // A restrained filmic pass keeps the bright shells readable against the roof.
  var volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;grading=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=grading;
  grading.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
  var bloom=grading.Add<Bloom>();bloom.threshold.Override(1.2f);bloom.intensity.Override(.16f);bloom.scatter.Override(.5f);
  var vignette=grading.Add<Vignette>();vignette.intensity.Override(.14f);vignette.smoothness.Override(.4f);
  var camera=Camera.main;if(camera)camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
  var facade=Arena.Material(new Color(.1f,.16f,.21f));Arena.Shape("Building beneath roof",PrimitiveType.Cube,new Vector3(0,-18,0),new Vector3(19.8f,34,19.8f),facade,false,transform);
  // Small surface details are flat and never introduce invisible collision.
  var ribs=Arena.Material(new Color(.18f,.26f,.30f));var trim=Arena.Material(new Color(.16f,.65f,.67f),true);
  for(int side=0;side<4;side++){var parent=new GameObject("Facade detail").transform;parent.SetParent(transform,false);parent.localRotation=Quaternion.Euler(0,side*90,0);for(int n=-8;n<=8;n+=4){Arena.Shape("Vertical facade rib",PrimitiveType.Cube,new Vector3(n,-6,10.02f),new Vector3(.15f,10,.12f),ribs,false,parent);Arena.Shape("Facade light",PrimitiveType.Cube,new Vector3(n,-2.4f,10.1f),new Vector3(1.2f,.045f,.025f),trim,false,parent);}}
  var scuff=Arena.Material(new Color(.22f,.28f,.30f));
  for(int i=0;i<22;i++){float x=-8+(i*17%160)*.1f,z=-8+(i*31%160)*.1f;var g=Arena.Shape("Deck wear",PrimitiveType.Cube,new Vector3(x,.008f,z),new Vector3(.15f+i%3*.11f,.006f,.018f),scuff,false,transform);g.transform.rotation=Quaternion.Euler(0,i*29,0);}
  boundary=MakeLine("Sudden death boundary",new Color(1,.3f,.1f),.08f);boundary.loop=true;boundary.positionCount=4;
  for(int i=0;i<2;i++){var line=MakeLine("Fighter ground marker",i==0?Color.cyan:new Color(1,.4f,.15f),.025f);line.loop=true;line.positionCount=40;line.useWorldSpace=false;for(int n=0;n<40;n++){float angle=n*Mathf.PI*2/40;line.SetPosition(n,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.5f);}markers[i]=line.transform;}
 }
 LineRenderer MakeLine(string name,Color color,float width){var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(transform);line.sharedMaterial=Resources.Load<Material>("Particles");line.startColor=line.endColor=color;line.widthMultiplier=width;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;}
 void OnDestroy(){if(grading)Destroy(grading);}
 void LateUpdate(){var a=Arena.Instance;float half=a.Match.SafeHalfExtent;boundary.enabled=a.Match.SuddenDeath&&!a.Tutorial.Running;if(boundary.enabled){boundary.SetPosition(0,new Vector3(-half,.07f,-half));boundary.SetPosition(1,new Vector3(half,.07f,-half));boundary.SetPosition(2,new Vector3(half,.07f,half));boundary.SetPosition(3,new Vector3(-half,.07f,half));}
  for(int i=0;i<2;i++){var f=i==0?a.Human:a.AI;var p=f.transform.position;markers[i].gameObject.SetActive(p.y>-.5f);markers[i].position=new Vector3(p.x,.04f,p.z);}
 }
}
}
