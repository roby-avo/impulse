using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace Playground.Editor {
[InitializeOnLoad] public static class LiveMatchLauncher {
 static LiveMatchLauncher(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PG.LiveMatch",false))new GameObject("Test-only human driver").AddComponent<LiveMatchValidation>();};}
 public static void Run(){SessionState.SetBool("PG.LiveMatch",true);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
}
}
