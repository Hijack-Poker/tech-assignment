using UnityEditor;
using UnityEngine;

public static class iOSBuilder
{
    [MenuItem("Build/iOS")]
    public static void Build()
    {
        string buildPath = System.Environment.GetEnvironmentVariable("IOS_BUILD_PATH");
        if (string.IsNullOrEmpty(buildPath))
            buildPath = "Builds/iOS";

        var scenes = new[] { "Assets/Scenes/PokerTable.unity" };

        // Allow HTTP connections to local dev server
        PlayerSettings.iOS.allowHTTPDownload = true;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"iOS build succeeded: {report.summary.totalSize} bytes, output: {buildPath}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"iOS build failed: {report.summary.result}");
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
