#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Playground.Editor {
[InitializeOnLoad] public static class KeyboardGameplayValidationLauncher {
 static KeyboardGameplayValidationLauncher(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PG.Keyboard",false))new GameObject("Keyboard gameplay validation").AddComponent<KeyboardGameplayValidation>();};}
 public static void Run(){SessionState.SetBool("PG.Keyboard",true);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");EditorApplication.isPlaying=true;}
}
public class KeyboardGameplayValidation:MonoBehaviour {
 Keyboard keyboard;Mouse mouse;
 void Check(bool ok,string name){File.AppendAllText("validation/keyboard-gameplay.txt",(ok?"PASS ":"FAIL ")+name+"\n");if(!ok){Debug.LogError("KEYBOARD FAIL: "+name);EditorApplication.Exit(1);throw new Exception(name);}Debug.Log("KEYBOARD PASS: "+name);}
 IEnumerator Keys(float seconds,params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return new WaitForSecondsRealtime(seconds);}
 IEnumerator Start(){
  File.WriteAllText("validation/keyboard-gameplay.txt","");yield return new WaitForSeconds(.8f);var a=Arena.Instance;var h=a.Human;var enemy=a.AI;var input=h.GetComponent<HumanInput>();var originalBackground=InputSystem.settings.backgroundBehavior;var originalEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
  foreach(var key in keyboard.allKeys){bool prime=key.wasPressedThisFrame;}yield return Keys(.1f);a.Setup.Show();yield return Keys(.1f,Key.Enter);Check(!a.Setup.Open&&a.Setup.Started&&!a.HUD.Paused,"Enter starts the selected local match without a mouse");yield return Keys(.05f);
  a.Brain.Enabled=a.LeftBrain.Enabled=false;a.Client.CancelPending();a.LeftClient.CancelPending();a.StopExecutors("keyboard_validation");a.Match.Practice=true;a.Match.Phase=RoundPhase.Fight;h.Active=enemy.Active=true;input.Capture(true);
  a.ResetArena();h.ResetAt(new Vector3(0,.1f,-3));enemy.ResetAt(new Vector3(0,.1f,3));yield return new WaitForSeconds(.15f);
  var start=h.transform.position;yield return Keys(.25f,Key.W);Check(h.transform.position.z>start.z+.6f,"W moves toward the top of the fixed view");yield return Keys(.1f);
  start=h.transform.position;yield return Keys(.25f,Key.LeftArrow);Check(h.transform.position.x<start.x-.6f,"arrow keys move in the matching screen direction");yield return Keys(.1f);
  h.ResetAt(new Vector3(0,.1f,-3));enemy.ResetAt(new Vector3(-5,.1f,-3));yield return Keys(.25f,Key.D);Check(h.Move.x>.9f&&Vector3.Dot(h.transform.forward,Vector3.left)>.9f,"movement stays screen-relative while automatically facing opponent");yield return Keys(.1f);
  Check(HumanInput.Movement(Vector2.one).magnitude<=1.001f,"diagonal keys do not increase movement speed");
  a.ResetArena();h.ResetAt(new Vector3(0,.1f,-3));enemy.ResetAt(new Vector3(0,.1f,3));yield return new WaitForSeconds(.15f);
  var cameraPosition=a.CameraRig.transform.position;var cameraRotation=a.CameraRig.transform.rotation;int pushes=h.Pushes,throws=h.Throws;
  InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(600,-500),scroll=new Vector2(0,120)}.WithButton(MouseButton.Left).WithButton(MouseButton.Right));yield return new WaitForSeconds(.3f);
  Check(h.Pushes==pushes&&h.Throws==throws&&!h.WindingUp,"mouse buttons do not attack");Check(Vector3.Distance(cameraPosition,a.CameraRig.transform.position)<.001f&&Quaternion.Angle(cameraRotation,a.CameraRig.transform.rotation)<.01f,"mouse motion and wheel cannot rotate or zoom the arena view");InputSystem.QueueStateEvent(mouse,new MouseState());
  h.ResetAt(new Vector3(8,.1f,-8));enemy.ResetAt(new Vector3(-8,.1f,8));yield return new WaitForSeconds(.1f);Check(Vector3.Distance(cameraPosition,a.CameraRig.transform.position)<.001f,"camera does not chase either fighter");
  var cam=Camera.main;Check(cam.orthographic,"arena view uses stable orthographic projection");bool framed=true;foreach(float x in new[]{-10f,10f})foreach(float z in new[]{-10f,10f}){var point=cam.WorldToViewportPoint(new Vector3(x,1.8f,z));framed&=point.z>0&&point.x>.02f&&point.x<.98f&&point.y>.02f&&point.y<.98f;}Check(framed,"all roof corners and fighter head heights fit in view");
  a.ResetArena();h.ResetAt(new Vector3(0,.1f,0));enemy.ResetAt(new Vector3(0,.1f,1.65f));yield return new WaitForSeconds(.2f);yield return Keys(.08f,Key.F);Check(h.WindingUp,"F starts a push with empty hands");yield return Keys(.3f);Check(enemy.LastAttacker==h,"keyboard push physically hits opponent");
  a.ResetArena();h.ResetAt(new Vector3(0,.1f,-3));enemy.ResetAt(new Vector3(0,.1f,3));var prop=Arena.Props.Find(p=>p.Kind=="crate");prop.Body.position=new Vector3(0,1,-1.6f);prop.transform.position=prop.Body.position;Physics.SyncTransforms();yield return new WaitForSeconds(.2f);
  yield return Keys(.08f,Key.E);Check(h.Held==prop,"E grabs the nearby prop");yield return Keys(.08f);yield return Keys(.08f,Key.E);Check(!h.Held,"E drops the held prop");yield return Keys(.08f);yield return Keys(.08f,Key.E);Check(h.Held==prop,"E can pick the prop up again");yield return Keys(.12f);
  var aim=input.Aim;Check(aim.z>0&&Mathf.Abs(aim.x)<.2f,"throw aim targets the opponent automatically");yield return Keys(.08f,Key.F);Check(h.Attack=="throw","the same F key throws when holding a prop");yield return Keys(.8f);Check(h.Throws>throws&&enemy.LastAttacker==h,"keyboard-only throw physically hits the opponent");
  a.ResetArena();h.ResetAt(new Vector3(0,.1f,-3));enemy.ResetAt(new Vector3(0,.1f,3));yield return new WaitForSeconds(.15f);yield return Keys(.06f,Key.RightShift);Check(h.Body.linearVelocity.x>6,"Shift without a movement key dodges sideways");yield return Keys(.4f);h.ResetAt(new Vector3(8,.1f,0));enemy.ResetAt(new Vector3(8,.1f,4));h.NextDodge=0;yield return new WaitForSeconds(.15f);yield return Keys(.06f,Key.RightShift);Check(h.Body.linearVelocity.x<-6,"standing dodge sidesteps away from the nearer edge");yield return Keys(.4f);h.ResetAt(new Vector3(0,.1f,-3));yield return new WaitForSeconds(.15f);float height=h.transform.position.y;yield return Keys(.1f,Key.Space);Check(h.transform.position.y>height+.35f,"Space jumps");yield return Keys(.7f);
  yield return Keys(.08f,Key.Escape);Check(a.HUD.Paused&&!input.Captured&&Cursor.lockState==CursorLockMode.None,"Escape pauses and releases gameplay keys without mouse capture");yield return Keys(.08f);yield return Keys(.08f,Key.F,Key.W);Check(h.Move==Vector3.zero&&!h.WindingUp,"paused gameplay ignores move and attack keys");yield return Keys(.08f);yield return Keys(.08f,Key.Escape);yield return Keys(.15f);Check(!a.HUD.Paused&&input.Captured&&!h.WindingUp,"resume does not leak a paused attack into gameplay");float volume=AudioListener.volume;a.HUD.SetVolume(.3f);Check(Mathf.Abs(AudioListener.volume-.3f)<.001f&&Mathf.Abs(PlayerPrefs.GetFloat("master_volume")-.3f)<.001f,"volume setting applies and persists");a.HUD.SetVolume(volume);int generation=a.Match.Generation;yield return Keys(.15f,Key.R);yield return Keys(.1f);Check(a.Match.Generation==generation&&a.Match.Phase==RoundPhase.Fight,"tapping R does not restart the match");yield return Keys(1.1f,Key.R);Check(a.Match.Phase==RoundPhase.Countdown&&a.Match.Generation>generation,"holding R restarts the match");yield return Keys(.1f);
  InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=originalBackground;InputSystem.settings.editorInputBehaviorInPlayMode=originalEditorInput;SessionState.SetBool("PG.Keyboard",false);EditorApplication.Exit(0);
 }
}
}
#endif
