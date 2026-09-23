using UnityEngine;
using UnityEngine.InputSystem;
namespace Playground {
public class HumanInput:MonoBehaviour {
 public Fighter Fighter; public OrbitCamera Rig; public bool Captured=true;
 void Start(){Capture(true);}
 public void Capture(bool value){Captured=value;Cursor.lockState=value?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!value;}
 void Update(){var k=Keyboard.current;var m=Mouse.current;if(k==null)return;
  if(k.escapeKey.wasPressedThisFrame)Arena.Instance.HUD.Pause(!Arena.Instance.HUD.Paused);
  if(k.rKey.wasPressedThisFrame){Arena.Instance.HUD.Pause(false);Arena.Instance.Match.NewMatch();}
  if(!Captured||!Fighter.Active){Fighter.Move=Vector3.zero;return;}
  Vector2 axes=new((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
  var forward=Quaternion.Euler(0,Rig.Yaw,0)*Vector3.forward;var right=Quaternion.Euler(0,Rig.Yaw,0)*Vector3.right;
  Fighter.Move=Vector3.ClampMagnitude(forward*axes.y+right*axes.x,1); Fighter.Face(forward);
  if(k.spaceKey.wasPressedThisFrame)Fighter.Jump();if(k.leftShiftKey.wasPressedThisFrame)Fighter.Dodge(Fighter.Move);
  if(k.eKey.wasPressedThisFrame){if(Fighter.Held)Fighter.Release(false,forward);else Fighter.Grab(Fighter.Nearest());}
  if(m!=null&&m.leftButton.wasPressedThisFrame){if(Fighter.Held)Fighter.Release(true,Rig.Aim);else Fighter.Push();}
  if(k.fKey.wasPressedThisFrame||(m!=null&&m.rightButton.wasPressedThisFrame))Fighter.Push();
 }
}
}
