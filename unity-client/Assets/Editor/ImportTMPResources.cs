using UnityEditor;
using UnityEngine;
using System.IO;

public static class ImportTMPResources
{
    public static void Import()
    {
        // Find the TMP Essential Resources package
        string packagePath = Path.GetFullPath(
            "Library/PackageCache/com.unity.ugui@bb329a87fcdc/Package Resources/TMP Essential Resources.unitypackage");

        if (!File.Exists(packagePath))
        {
            // Try glob approach
            var dirs = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui*");
            foreach (var dir in dirs)
            {
                var candidate = Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(candidate))
                {
                    packagePath = Path.GetFullPath(candidate);
                    break;
                }
            }
        }

        if (!File.Exists(packagePath))
        {
            Debug.LogError($"TMP Essential Resources not found at: {packagePath}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"Importing TMP Essential Resources from: {packagePath}");
        AssetDatabase.ImportPackage(packagePath, false);
        AssetDatabase.Refresh();
        Debug.Log("TMP Essential Resources imported successfully");
        EditorApplication.Exit(0);
    }
}
