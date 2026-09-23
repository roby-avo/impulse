using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
namespace Playground {
public class OrbitCamera:MonoBehaviour {
 public Fighter Target; public float Yaw,Pitch=25,Impulse; public Vector3 Aim=>Quaternion.Euler(-8,Yaw,0)*Vector3.forward;
 CinemachineCamera cine; float distance=8.5f;
 void Awake(){var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>();cam.gameObject.AddComponent<CinemachineBrain>();cam.nearClipPlane=.15f;cam.farClipPlane=220;cam.backgroundColor=new Color(.12f,.18f,.27f);cam.clearFlags=CameraClearFlags.SolidColor;cine=gameObject.AddComponent<CinemachineCamera>();cine.Lens.FieldOfView=58;}
 void LateUpdate(){if(!Target)return;var input=Target.GetComponent<HumanInput>();if(Mouse.current!=null&&input&&input.Captured){var d=Mouse.current.delta.ReadValue();Yaw+=d.x*.12f;Pitch=Mathf.Clamp(Pitch-d.y*.09f,12,62);distance=Mathf.Clamp(distance-Mouse.current.scroll.ReadValue().y*.005f,5,12);}
  Vector3 focus=Target.transform.position+Vector3.up*1.2f+(Quaternion.Euler(0,Yaw,0)*Vector3.forward)*1.5f;focus.y=Mathf.Max(-.5f,focus.y);var rotation=Quaternion.Euler(Pitch,Yaw,0);var offset=rotation*Vector3.back*distance;
  if(Physics.SphereCast(focus,.25f,offset.normalized,out var hit,distance,1<<8))offset=offset.normalized*Mathf.Max(.5f,hit.distance-.1f);
  var wanted=focus+offset;Impulse=Mathf.MoveTowards(Impulse,0,Time.deltaTime*.6f);wanted+=new Vector3(Mathf.Sin(Time.time*91),Mathf.Cos(Time.time*77),0)*Impulse;transform.position=Vector3.Lerp(transform.position,wanted,1-Mathf.Exp(-12*Time.deltaTime));transform.rotation=Quaternion.LookRotation(focus-transform.position);
 }
}
}
