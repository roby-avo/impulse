using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections;
using System.IO;
namespace Playground.Editor {
[InitializeOnLoad] public static class Validation {
 static Validation(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PG.Validate",false)){new GameObject("Validation runner").AddComponent<PlayValidation>();}};}
 public static void M7(){ProjectSetup.EnsureResources();SessionState.SetBool("PG.Validate",true);SessionState.SetInt("PG.Milestone",7);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
 public static void M6(){ProjectSetup.EnsureResources();SessionState.SetBool("PG.Validate",true);SessionState.SetInt("PG.Milestone",6);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
 public static void M5(){ProjectSetup.EnsureResources();SessionState.SetBool("PG.Validate",true);SessionState.SetInt("PG.Milestone",5);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
 public static void M4(){SessionState.SetBool("PG.Validate",true);SessionState.SetInt("PG.Milestone",4);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
 public static void M3(){SessionState.SetBool("PG.Validate",true);SessionState.SetInt("PG.Milestone",3);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
 public static void M2(){SessionState.SetBool("PG.Validate",true);SessionState.SetInt("PG.Milestone",2);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
 public static void M1(){SessionState.SetBool("PG.Validate",true);SessionState.SetInt("PG.Milestone",1);EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");foreach(var db in UnityEditor.Search.SearchService.EnumerateDatabases()){}EditorApplication.isPlaying=true;}
}
}
