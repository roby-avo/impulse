using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Playground {
// Opt-in standalone benchmark. No input driver or substitute AI policy.
public class PerformanceProbe:MonoBehaviour {
 [Serializable] public class Stage {public string name;public bool allocation_counter_available;public int frames,gc_collections;public float mean_ms,p50_ms,p95_ms,p99_ms,max_ms,allocated_bytes_per_frame;}
 [Serializable] public class Report {public string unity,device,resolution;public Stage[] stages;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--pg-benchmark")>=0)new GameObject("Frame timing benchmark").AddComponent<PerformanceProbe>();}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(5);var a=Arena.Instance;var stages=new List<Stage>();foreach(string stage in new[]{"gameplay","pause","experiments"}){if(stage=="pause")a.HUD.Pause(true);if(stage=="experiments")a.Research.Show();yield return new WaitForSecondsRealtime(2);var frames=new List<float>(2000);long allocated=GC.GetAllocatedBytesForCurrentThread();int gc=GC.CollectionCount(0);float end=Time.realtimeSinceStartup+8;while(Time.realtimeSinceStartup<end){frames.Add(Time.unscaledDeltaTime*1000);yield return null;}long used=GC.GetAllocatedBytesForCurrentThread()-allocated;frames.Sort();stages.Add(new Stage{name=stage,frames=frames.Count,gc_collections=GC.CollectionCount(0)-gc,mean_ms=frames.Average(),p50_ms=frames[frames.Count/2],p95_ms=frames[(int)(frames.Count*.95f)],p99_ms=frames[(int)(frames.Count*.99f)],max_ms=frames[frames.Count-1],allocation_counter_available=used>0,allocated_bytes_per_frame=used>0?(float)used/frames.Count:-1});}
 string path=Path.Combine(Application.persistentDataPath,"performance.json");File.WriteAllText(path,JsonUtility.ToJson(new Report{unity=Application.unityVersion,device=SystemInfo.graphicsDeviceName,resolution=Screen.width+"x"+Screen.height,stages=stages.ToArray()},true));Debug.Log("BENCHMARK COMPLETE: "+path);Application.Quit();}
}
}
