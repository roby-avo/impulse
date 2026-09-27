using UnityEngine;
using Unity.Cinemachine;
namespace Playground {
// The component name is retained for scene compatibility. Gameplay uses one fixed arena view.
public class OrbitCamera:MonoBehaviour {
 public Fighter Target; public float Impulse;
 public const float FixedPitch=55;
 public static Quaternion ViewRotation=>Quaternion.Euler(FixedPitch,0,0);
 CinemachineCamera cine;
 void Awake(){
  var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>();cam.gameObject.AddComponent<CinemachineBrain>();
  cam.orthographic=true;cam.orthographicSize=13f;cam.nearClipPlane=.15f;cam.farClipPlane=220;cam.backgroundColor=new Color(.12f,.18f,.27f);cam.clearFlags=CameraClearFlags.Skybox;
  cine=gameObject.AddComponent<CinemachineCamera>();cine.Lens.ModeOverride=LensSettings.OverrideModes.Orthographic;cine.Lens.OrthographicSize=13f;
  FrameArena();
 }
 void LateUpdate(){FrameArena();}
 void FrameArena(){
  // Leave room above robot heads for the scoreboard and below the roof for the HUD.
  // Fixed bearing, position and zoom; only aspect-ratio fitting changes on resize.
  float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
  cine.Lens.OrthographicSize=Mathf.Max(13f,13f/Mathf.Max(.3f,aspect));
  transform.SetPositionAndRotation(Vector3.up*.7f+ViewRotation*Vector3.back*32,ViewRotation);
  Impulse=0; // Impact feedback stays on the fighters; the arena view never shakes or rotates.
 }
}
}
