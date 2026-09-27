using UnityEngine;
using UnityEngine.InputSystem;
namespace Playground {
public class HumanInput:MonoBehaviour {
 public Fighter Fighter; public OrbitCamera Rig; public bool Captured=true;
 int captureFrame;float attackUntil=-1;
 public Vector3 Aim=>Arena.Instance.LeftExecutor.ThrowAim();
 public static Vector3 Movement(Vector2 axes)=>Vector3.ClampMagnitude(new Vector3(axes.x,0,axes.y),1);
 void Start(){Capture(!Arena.Instance.HUD.Paused&&!Arena.Instance.Setup.Open);}
 // Captured means gameplay keys are enabled; no pointer capture is necessary.
 public void Capture(bool value){value=value&&!Arena.Instance.Spectating;Captured=value;captureFrame=Time.frameCount;attackUntil=-1;Fighter.Move=Vector3.zero;Cursor.lockState=CursorLockMode.None;Cursor.visible=!value;}
 void Update(){var k=Keyboard.current;if(k==null)return;
  var a=Arena.Instance;
  if(a.Setup.Open){if(k.escapeKey.wasPressedThisFrame)a.Setup.Close();else if(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame)a.Setup.StartFromKeyboard();return;}
  if(a.Research.Open){Fighter.Move=Vector3.zero;if(k.escapeKey.wasPressedThisFrame)a.Research.Close();return;}
  if(k.escapeKey.wasPressedThisFrame)a.HUD.Pause(!a.HUD.Paused);
  if(!a.HUD.Paused&&k.rKey.wasPressedThisFrame){a.HUD.Pause(false);a.Match.NewMatch();}
  if(a.Spectating)return;
  if(!Captured||Time.frameCount<=captureFrame||!Fighter.Active){Fighter.Move=Vector3.zero;attackUntil=-1;return;}
  Vector2 axes=new(((k.dKey.isPressed||k.rightArrowKey.isPressed)?1:0)-((k.aKey.isPressed||k.leftArrowKey.isPressed)?1:0),((k.wKey.isPressed||k.upArrowKey.isPressed)?1:0)-((k.sKey.isPressed||k.downArrowKey.isPressed)?1:0));
  Fighter.Move=Movement(axes);var aim=Aim;Fighter.Face(aim);
  if(k.spaceKey.wasPressedThisFrame)Fighter.Jump();
  if(k.leftShiftKey.wasPressedThisFrame||k.rightShiftKey.wasPressedThisFrame)Fighter.Dodge(Fighter.Move.sqrMagnitude>.01f?Fighter.Move:Vector3.right);
  if(k.eKey.wasPressedThisFrame){attackUntil=-1;if(Fighter.Held)Fighter.Release(false,aim);else Fighter.Grab(Fighter.Nearest());}
  if(k.fKey.wasPressedThisFrame)attackUntil=Time.time+.4f;
  // A short input buffer lets an attack wait for bounded automatic facing/recovery.
  // The key chooses the attack; there is no automatic firing, snapping or homing.
  if(attackUntil>=Time.time&&Fighter.CanAct&&Vector3.Angle(Fighter.transform.forward,Vector3.ProjectOnPlane(aim,Vector3.up))<=12){
   bool started=Fighter.Held?Fighter.Release(true,aim):Fighter.Push();if(started)attackUntil=-1;
  }
 }
}
}
