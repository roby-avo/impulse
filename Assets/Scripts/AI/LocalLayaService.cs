using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Playground {
// Starts only the installed local runtime, on demand. No model downloads or cloud calls.
public class LocalLayaService : MonoBehaviour {
    static LocalLayaService instance;
    static float nextAttempt;
    public static string Message { get; private set; }
    [Serializable] class StartupResult { public string state, message; }

    public static bool CanManage(string endpoint) =>
        endpoint == "http://127.0.0.1:8000" &&
        (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor ||
         Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor);

    public static string FindProjectRoot(string dataPath) {
        for (var directory = new DirectoryInfo(dataPath); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ai", "ensure_service.py"))) return directory.FullName;
        return null;
    }

    public static string RequestStart() {
        if (instance || Time.realtimeSinceStartup < nextAttempt) return Message;
        nextAttempt = Time.realtimeSinceStartup + 10;
        var root = FindProjectRoot(Application.dataPath);
        if (root == null) return Message = "Local Laya not found · keep the app in the project's Builds folder";
        var python = Path.Combine(root, "ai", ".venv", "bin", "python");
        if (!File.Exists(python)) return Message = "Laya setup missing · run ai/setup.sh in the project folder";
        Message = "Starting local Laya · reconnecting automatically";
        var host = new GameObject("Local Laya startup");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<LocalLayaService>();
        instance.StartCoroutine(instance.StartService(root, python));
        return Message;
    }

    static string Launch(string root, string python) {
        try {
            // WorkingDirectory avoids shell quoting and works with spaces in the project path.
            using (var process = Process.Start(new ProcessStartInfo(python, "ai/ensure_service.py") {
                WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true
            })) {
                if (!process.WaitForExit(8000)) {
                    process.Kill();
                    return "";
                }
                return process.StandardOutput.ReadToEnd();
            }
        } catch (Exception) { return ""; }
    }

    IEnumerator StartService(string root, string python) {
        var task = Task.Run(() => Launch(root, python));
        while (!task.IsCompleted) yield return null;
        StartupResult result = null;
        try { result = JsonUtility.FromJson<StartupResult>(task.Result); } catch (Exception) { }
        Message = string.IsNullOrEmpty(result?.message)
            ? "Cannot start Laya · run ai/start.sh to see diagnostics" : result.message;
        UnityEngine.Debug.Log("Local Laya: " + Message);
        instance = null;
        Destroy(gameObject);
    }
}
}
