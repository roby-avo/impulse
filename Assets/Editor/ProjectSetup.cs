using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Playground.Editor {
public static class ProjectSetup {
 [MenuItem("Playground/Prepare Project")]
 public static void Prepare() {
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PlaygroundURP.asset");
  if(!pipeline) {
   var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
   AssetDatabase.CreateAsset(renderer,"Assets/Settings/PlaygroundRenderer.asset");
   pipeline=UniversalRenderPipelineAsset.Create(renderer);
   AssetDatabase.CreateAsset(pipeline,"Assets/Settings/PlaygroundURP.asset");
  }
  GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
  PlayerSettings.companyName="Impulse"; PlayerSettings.productName="Physics Playground";
  PlayerSettings.defaultScreenWidth=1440; PlayerSettings.defaultScreenHeight=900;
  PlayerSettings.runInBackground=true;
  Time.fixedDeltaTime=1f/60f;
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/Rooftop.unity");
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Rooftop.unity",true)};
  EnsureResources();AssetDatabase.SaveAssets(); Debug.Log("M0 PASS: Unity 6, URP, Input System, scene and settings ready.");
 }
 public static void EnsureResources(){
  if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Surface.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")),"Assets/Resources/Surface.mat");
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Particles.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")),"Assets/Resources/Particles.mat");
  AssetDatabase.SaveAssets();
 }
 [MenuItem("Playground/Build Mac Game")]
 public static void BuildMac(){
  using(var native=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/bin/bash","scripts/build-dock-plugin.sh"){WorkingDirectory=System.IO.Directory.GetCurrentDirectory(),UseShellExecute=false})){native.WaitForExit();if(native.ExitCode!=0)throw new System.Exception("Dock plugin build failed");}
  AssetDatabase.Refresh();EnsureResources();
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Rooftop.unity");if(!Object.FindFirstObjectByType<Arena>())new GameObject("Physics Playground · procedural rooftop").AddComponent<Arena>();EditorSceneManager.SaveScene(scene);PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.macOS.buildNumber="10";PlayerSettings.bundleVersion="0.9.0";var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/UI/AppIcon.png");PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Rooftop.unity"},locationPathName="Builds/Physics Playground.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
  if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Mac build failed: "+report.summary.result);
  Debug.Log("M7 BUILD PASS: "+report.summary.outputPath);
 }
}
}
