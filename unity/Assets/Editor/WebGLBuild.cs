using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    public static void Build()
    {
        const string output = "Build/WebGL";
        Directory.CreateDirectory(output);
        // GitHub Pages cannot add Content-Encoding headers to Unity's compressed
        // payloads, so publish plain assets that work on any static host.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"WebGL build failed: {report.summary.result}");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log($"WebGL build completed: {report.summary.outputPath}");
        EditorApplication.Exit(0);
    }
}
