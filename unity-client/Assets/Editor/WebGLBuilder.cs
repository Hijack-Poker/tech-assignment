using UnityEditor;
using UnityEngine;

public static class WebGLBuilder
{
    [MenuItem("Build/WebGL")]
    public static void Build()
    {
        var scenes = new[] { "Assets/Scenes/PokerTable.unity" };
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError($"WebGL build failed: {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("WebGL build succeeded!");
        }
    }
}
