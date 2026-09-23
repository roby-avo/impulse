using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Playground {
// The bundle icon alone can remain a cached placeholder in the running Dock.
// AppKit's applicationIconImage explicitly refreshes the current process tile.
public class MacDockIcon : MonoBehaviour {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
    [DllImport("PlaygroundDock")] static extern void PlaygroundInstallDockIcon(string verificationPath);
    [DllImport("PlaygroundDock")] static extern int PlaygroundDockIconStatus();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        var host = new GameObject("macOS Dock icon");
        DontDestroyOnLoad(host);
        host.AddComponent<MacDockIcon>();
    }
    IEnumerator Start() {
        PlaygroundInstallDockIcon(null);
        yield return new WaitForSecondsRealtime(1);
        // Unity/AppKit may finish their own startup icon assignment later.
        string verification = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--pg-verify-dock") >= 0
            ? System.IO.Path.Combine(Application.persistentDataPath, "dock-icon-runtime.png") : null;
        PlaygroundInstallDockIcon(verification);
        yield return new WaitForSecondsRealtime(1);
        int status = PlaygroundDockIconStatus();
        if (status == 1) Debug.Log("DOCK ICON APPLIED: AppKit updated the running app tile; runtime image is valid.");
        else Debug.LogError("Dock icon could not be applied. Native status: " + status);
    }
    void OnApplicationFocus(bool focused) {
        if (focused) PlaygroundInstallDockIcon(null);
    }
#endif
}
}
