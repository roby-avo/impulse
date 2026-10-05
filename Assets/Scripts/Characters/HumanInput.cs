using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Playground {
// Keyboard and gamepad play. Player 1 (cyan) always has an instance; Player 2 (orange) is
// active only in the local two-player mode and drives the right-hand fighter.
public class HumanInput:MonoBehaviour {
 public Fighter Fighter; public OrbitCamera Rig; public bool Captured=true; public bool SecondPlayer;
 public static readonly List<HumanInput> All=new();
 public const float RestartHoldSeconds=.8f;public float RestartHeld{get;private set;}
 int captureFrame;float attackUntil=-1;bool restartLatched;
 Arena A=>Arena.Instance;
 bool TwoPlayer=>A.Setup&&A.Setup.Mode==MatchMode.HumanVsHuman;
 public bool Playing=>SecondPlayer?TwoPlayer:!A.Spectating;
 Fighter Opponent=>Fighter==A.Human?A.AI:A.Human;
 public Vector3 Aim=>(SecondPlayer?A.Executor:A.LeftExecutor).ThrowAim();
 // Gamepad 1 controls Player 1, gamepad 2 controls Player 2.
 Gamepad Pad=>Gamepad.all.Count>(SecondPlayer?1:0)?Gamepad.all[SecondPlayer?1:0]:null;
 public static Vector3 Movement(Vector2 axes)=>Vector3.ClampMagnitude(new Vector3(axes.x,0,axes.y),1);
 void OnEnable(){All.Add(this);}void OnDisable(){All.Remove(this);}
 void Start(){Capture(!A.HUD.Paused&&!A.Setup.Open);}
 // Captured means gameplay keys are enabled; no pointer capture is necessary.
 public void Capture(bool value){value=value&&Playing;Captured=value;captureFrame=Time.frameCount;attackUntil=-1;if(Fighter&&(Playing||!SecondPlayer))Fighter.Move=Vector3.zero;if(SecondPlayer)return;Cursor.lockState=CursorLockMode.None;Cursor.visible=!value;}
 // A standing dodge sidesteps the opponent toward whichever side leaves more room before the edge.
 Vector3 Sidestep(){var toOpponent=Vector3.ProjectOnPlane(Opponent.transform.position-Fighter.transform.position,Vector3.up);if(toOpponent.sqrMagnitude<.01f)toOpponent=Fighter.transform.forward;var side=Vector3.Cross(Vector3.up,toOpponent).normalized;return EdgeAfter(side)>=EdgeAfter(-side)?side:-side;}
 float EdgeAfter(Vector3 direction){var p=Fighter.transform.position+direction*2.5f;return -Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.z));}
 static float Key(bool on)=>on?1:0;
 // Menus, pause and restart belong to Player 1's instance (keyboard or either gamepad's Start).
 bool Global(Keyboard k){var a=A;bool padStart=false,padSouth=false;foreach(var pad in Gamepad.all){padStart|=pad.startButton.wasPressedThisFrame;padSouth|=pad.buttonSouth.wasPressedThisFrame;}
  if(a.Setup.Open){if(k!=null&&k.escapeKey.wasPressedThisFrame)a.Setup.Close();else if(k!=null&&(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame)||padStart||padSouth)a.Setup.StartFromKeyboard();return true;}
  if(a.Research.Open){Fighter.Move=Vector3.zero;if(k!=null&&k.escapeKey.wasPressedThisFrame)a.Research.Close();return true;}
  if(k!=null&&k.escapeKey.wasPressedThisFrame||padStart)a.HUD.Pause(!a.HUD.Paused);
  // R sits between E and F, so a restart needs a deliberate hold instead of a single press.
  bool r=k!=null&&k.rKey.isPressed;if(!a.HUD.Paused&&r&&!restartLatched){RestartHeld+=Time.unscaledDeltaTime;if(RestartHeld>=RestartHoldSeconds){restartLatched=true;RestartHeld=0;a.HUD.Pause(false);a.Match.NewMatch();}}else{RestartHeld=0;if(!r)restartLatched=false;}
  return false;
 }
 void Update(){var k=Keyboard.current;var pad=Pad;
  if(!SecondPlayer&&Global(k))return;
  if(!Playing)return;
  if(!Captured||Time.frameCount<=captureFrame||!Fighter.Active){Fighter.Move=Vector3.zero;attackUntil=-1;return;}
  bool two=TwoPlayer;Vector2 axes=Vector2.zero;bool jump=false,dodge=false,grab=false,attack=false,charge=false;
  if(k!=null){
   if(SecondPlayer){axes=new(Key(k.rightArrowKey.isPressed)-Key(k.leftArrowKey.isPressed),Key(k.upArrowKey.isPressed)-Key(k.downArrowKey.isPressed));jump=k.slashKey.wasPressedThisFrame;dodge=k.rightShiftKey.wasPressedThisFrame;grab=k.commaKey.wasPressedThisFrame;attack=k.periodKey.wasPressedThisFrame;charge=k.periodKey.isPressed;}
   else{axes=new(Key(k.dKey.isPressed||!two&&k.rightArrowKey.isPressed)-Key(k.aKey.isPressed||!two&&k.leftArrowKey.isPressed),Key(k.wKey.isPressed||!two&&k.upArrowKey.isPressed)-Key(k.sKey.isPressed||!two&&k.downArrowKey.isPressed));jump=k.spaceKey.wasPressedThisFrame;dodge=k.leftShiftKey.wasPressedThisFrame||!two&&k.rightShiftKey.wasPressedThisFrame;grab=k.eKey.wasPressedThisFrame;attack=k.fKey.wasPressedThisFrame;charge=k.fKey.isPressed;}
  }
  if(pad!=null){var stick=pad.leftStick.ReadValue();if(stick.magnitude<.2f)stick=Vector2.zero;stick+=pad.dpad.ReadValue();if(stick.sqrMagnitude>axes.sqrMagnitude)axes=stick;jump|=pad.buttonSouth.wasPressedThisFrame;dodge|=pad.buttonEast.wasPressedThisFrame||pad.rightShoulder.wasPressedThisFrame;grab|=pad.buttonNorth.wasPressedThisFrame;attack|=pad.buttonWest.wasPressedThisFrame;charge|=pad.buttonWest.isPressed;}
  Fighter.Move=Movement(axes);var aim=Aim;Fighter.Face(aim);
  if(jump)Fighter.Jump();
  if(dodge)Fighter.Dodge(Fighter.Move.sqrMagnitude>.01f?Fighter.Move:Sidestep());
  if(grab){attackUntil=-1;if(Fighter.Held)Fighter.Release(false,aim);else Fighter.Grab(Fighter.Nearest());}
  if(attack)attackUntil=Time.time+.4f;
  // Holding the attack key past the wind-up charges a push; a tap is an ordinary push.
  Fighter.ChargeHeld=charge&&Fighter.Attack=="push";
  // A short input buffer lets an attack wait for bounded automatic facing/recovery.
  // The key chooses the attack; there is no automatic firing, snapping or homing.
  if(attackUntil>=Time.time&&Fighter.CanAct&&Vector3.Angle(Fighter.transform.forward,Vector3.ProjectOnPlane(aim,Vector3.up))<=12){
   bool started=Fighter.Held?Fighter.Release(true,aim):Fighter.Push();if(started)attackUntil=-1;
  }
 }
}
}
