using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
namespace Playground {
// Development aid: `--pg-screenshot <folder>` saves the setup screen in each matchup and a few
// frames of a Human vs Laya match, then quits.
// The game captures its own frames, so no screen-recording permission is needed.
public class VisualCheck:MonoBehaviour {
 string folder;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--pg-screenshot");if(i<0||i+1>=args.Length)return;var host=new GameObject("Visual check");DontDestroyOnLoad(host);host.AddComponent<VisualCheck>().folder=args[i+1];}
 IEnumerator Start(){Directory.CreateDirectory(folder);yield return new WaitForSecondsRealtime(4);var root=Arena.Instance.HUD.Root;var matchup=root.Q<DropdownField>("matchup");
  foreach(var (mode,file) in new[]{("Human vs Laya","setup-play-laya.png"),("Human vs TypeSafe","setup-play-typesafe.png"),("AI vs AI","setup-watch-ai.png"),("2 Players","setup-two-players.png")}){matchup.value=mode;yield return new WaitForSecondsRealtime(2.5f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,file));yield return new WaitForSecondsRealtime(.5f);}
  matchup.value="Human vs Laya";yield return new WaitForSecondsRealtime(1);Arena.Instance.Setup.StartFromKeyboard();
  for(int i=1;i<=3;i++){yield return new WaitForSecondsRealtime(i==1?5:1.5f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,$"gameplay-{i}.png"));}
  yield return new WaitForSecondsRealtime(.5f);Application.Quit();
 }
}
}
