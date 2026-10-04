using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
namespace Playground {
// Development aid: `--pg-screenshot <folder>` saves the setup screen in each matchup, then quits.
// The game captures its own frames, so no screen-recording permission is needed.
public class VisualCheck:MonoBehaviour {
 string folder;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--pg-screenshot");if(i<0||i+1>=args.Length)return;var host=new GameObject("Visual check");DontDestroyOnLoad(host);host.AddComponent<VisualCheck>().folder=args[i+1];}
 IEnumerator Start(){Directory.CreateDirectory(folder);yield return new WaitForSecondsRealtime(4);var root=Arena.Instance.HUD.Root;var matchup=root.Q<DropdownField>("matchup");
  foreach(var (mode,file) in new[]{("Human vs Laya","setup-play-laya.png"),("Human vs TypeSafe","setup-play-typesafe.png"),("AI vs AI","setup-watch-ai.png")}){matchup.value=mode;yield return new WaitForSecondsRealtime(2.5f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,file));yield return new WaitForSecondsRealtime(.5f);}
  Application.Quit();
 }
}
}
