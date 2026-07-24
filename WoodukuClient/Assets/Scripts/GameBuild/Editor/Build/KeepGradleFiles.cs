using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// Unity 导出 Gradle 工程后，用 AndroidKeepFiles 模板覆盖/合并关键文件。
/// </summary>
public class KeepGradleFiles : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    public static bool SkipChangeGradle;

    private static readonly HashSet<string> PreserveGradlePropertyKeys = new()
    {
        "unityStreamingAssets",
        "unityTemplateVersion"
    };

    private readonly List<(string unityPath, string asPath)> _pathArr = new()
    {
        ("Editor/AndroidKeepFiles/launcher/build.gradle", "launcher/build.gradle"),
        ("Editor/AndroidKeepFiles/launcher/strings.xml", "launcher/src/main/res/values/strings.xml"),
        ("Editor/AndroidKeepFiles/settings.gradle", "settings.gradle"),
        ("Editor/AndroidKeepFiles/build.gradle", "build.gradle"),
        ("Editor/AndroidKeepFiles/gradle/wrapper/gradle-wrapper.properties",
            "gradle/wrapper/gradle-wrapper.properties"),
    };

    private static string ResolveGradleProjectRoot(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return path;
        }

        if (File.Exists(Path.Combine(path, "settings.gradle")))
        {
            return Path.GetFullPath(path);
        }

        var parent = Path.GetFullPath(Path.Combine(path, ".."));
        if (File.Exists(Path.Combine(parent, "settings.gradle")))
        {
            return parent;
        }

        return Path.GetFullPath(path);
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (SkipChangeGradle)
        {
            return;
        }

        string gradleRoot = ResolveGradleProjectRoot(path);
        Debug.Log($"[KeepGradleFiles] Gradle project root: {gradleRoot} (Unity path: {path})");

        foreach (var item in _pathArr)
        {
            string customGradlePath = Path.Combine(Application.dataPath, item.unityPath);
            string destPath = Path.Combine(gradleRoot, item.asPath);

            if (!File.Exists(customGradlePath))
            {
                Debug.LogWarning("[KeepGradleFiles] Template not found: " + customGradlePath);
                continue;
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(customGradlePath, destPath, true);
            Debug.Log("[KeepGradleFiles] Copied to: " + destPath);
        }

        MergeGradleProperties(gradleRoot);
        WriteLocalPropertiesIfNeeded(gradleRoot);
    }

    private static void MergeGradleProperties(string gradleRoot)
    {
        var templatePath = Path.Combine(Application.dataPath, "Editor/AndroidKeepFiles/gradle.properties");
        var targetPath = Path.Combine(gradleRoot, "gradle.properties");
        if (!File.Exists(templatePath))
        {
            Debug.LogWarning("[KeepGradleFiles] gradle.properties template not found: " + templatePath);
            return;
        }

        if (!File.Exists(targetPath))
        {
            File.Copy(templatePath, targetPath, true);
            return;
        }

        var targetLines = File.ReadAllLines(targetPath).ToList();
        var templateLines = File.ReadAllLines(templatePath);

        foreach (var templateLine in templateLines)
        {
            if (string.IsNullOrWhiteSpace(templateLine) || templateLine.TrimStart().StartsWith("#"))
            {
                continue;
            }

            var separatorIndex = templateLine.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = templateLine.Substring(0, separatorIndex).Trim();
            if (PreserveGradlePropertyKeys.Contains(key))
            {
                continue;
            }

            var replaced = false;
            for (var i = 0; i < targetLines.Count; i++)
            {
                if (!targetLines[i].StartsWith(key + "=", System.StringComparison.Ordinal))
                {
                    continue;
                }

                targetLines[i] = templateLine;
                replaced = true;
                break;
            }

            if (!replaced)
            {
                targetLines.Add(templateLine);
            }
        }

        File.WriteAllLines(targetPath, targetLines);
        Debug.Log("[KeepGradleFiles] Merged gradle.properties.");
    }

    private static void WriteLocalPropertiesIfNeeded(string gradleRoot)
    {
        var targetPath = Path.Combine(gradleRoot, "local.properties");
        if (File.Exists(targetPath))
        {
            return;
        }

        string sdkDir = System.Environment.GetEnvironmentVariable("ANDROID_HOME")
                        ?? System.Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (string.IsNullOrEmpty(sdkDir))
        {
            // 尝试 Unity 自带 SDK
            var unityRoot = Path.GetDirectoryName(EditorApplication.applicationPath);
            var candidate = Path.Combine(unityRoot ?? string.Empty,
                "Data/PlaybackEngines/AndroidPlayer/SDK");
            if (Directory.Exists(candidate))
            {
                sdkDir = candidate;
            }
        }

        if (string.IsNullOrEmpty(sdkDir))
        {
            Debug.LogWarning("[KeepGradleFiles] ANDROID_HOME not set; local.properties not written.");
            return;
        }

        var escaped = sdkDir.Replace("\\", "\\\\").Replace(":", "\\:");
        File.WriteAllText(targetPath, $"sdk.dir={escaped}\n");
        Debug.Log("[KeepGradleFiles] Wrote local.properties sdk.dir=" + sdkDir);
    }
}
