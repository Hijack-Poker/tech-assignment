using UnityEditor;
using UnityEngine;

public static class DesktopBuilder
{
    [MenuItem("Build/Desktop (macOS)")]
    public static void Build()
    {
        string buildPath = System.Environment.GetEnvironmentVariable("DESKTOP_BUILD_PATH");
        if (string.IsNullOrEmpty(buildPath))
            buildPath = "Builds/Desktop/HijackPoker.app";

        var scenes = new[] { "Assets/Scenes/PokerTable.unity" };

        // Use Mono backend so IL2CPP install isn't required
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Desktop build succeeded: {report.summary.totalSize} bytes, output: {buildPath}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"Desktop build failed: {report.summary.result}");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Warning)
                        Debug.LogError($"  [{msg.type}] {msg.content}");
                }
            }
            EditorApplication.Exit(1);
        }
    }
}
