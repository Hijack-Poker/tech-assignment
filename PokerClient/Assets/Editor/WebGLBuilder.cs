using UnityEditor;
using UnityEngine;

public class WebGLBuilder
{
    [MenuItem("Build/WebGL Build")]
    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/PokerTable.unity" };
        string buildPath = "../build/webgl";

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("WebGL build succeeded: " + buildPath);
        }
        else
        {
            Debug.LogError("WebGL build failed");
            EditorApplication.Exit(1);
        }
    }
}
