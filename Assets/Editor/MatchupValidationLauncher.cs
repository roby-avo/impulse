using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Playground.Editor {
[InitializeOnLoad] public static class MatchupValidationLauncher {
 static MatchupValidationLauncher(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PG.Matchups",false))new GameObject("Matchup validation").AddComponent<MatchupValidation>();};}
 public static void Run(){ProjectSetup.EnsureResources();SessionState.SetBool("PG.Matchups",true);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");EditorApplication.isPlaying=true;}
}
}
