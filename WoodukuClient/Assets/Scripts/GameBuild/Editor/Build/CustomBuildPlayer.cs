using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CustomBuildPlayer
{
    public static void BuildAndroid()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (BuildTarget.Android != EditorUserBuildSettings.activeBuildTarget)
        {
            EditorUtility.DisplayDialog("打包平台错误", "请先在 File - Build Settings 切换到 Android 平台", "确定");
            return;
        }

        BuildPlayer("Release", BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.CompressWithLz4);
    }

    public static bool ExportAndroidProject(string versionName = null, int buildNumber = 0)
    {
        if (Application.isPlaying)
        {
            return false;
        }

        if (BuildTarget.Android != EditorUserBuildSettings.activeBuildTarget)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("切换 Android 平台失败");
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(versionName))
        {
            PlayerSettings.bundleVersion = versionName.Trim();
        }

        if (buildNumber > 0)
        {
            PlayerSettings.Android.bundleVersionCode = buildNumber;
        }

        if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)))
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BuildPaths.DefaultApplicationId);
        }

        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        AssetDatabase.SaveAssets();

        var levels = BuildUtility.GetLevelsFromBuildSettings();
        if (levels.Length == 0)
        {
            Debug.LogError("Nothing to export. 请先在 Build Settings 勾选场景。");
            return false;
        }

        var exportPath = BuildPaths.GetAndroidProjectExportPath();
        Directory.CreateDirectory(exportPath);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = exportPath,
            targetGroup = BuildTargetGroup.Android,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4 | BuildOptions.AcceptExternalModificationsToPlayer
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var success = report.summary.result == BuildResult.Succeeded;
        if (success)
        {
            KeepAndroidGradleWrapper();
            Debug.Log("Android 工程导出完成: " + exportPath);
        }
        else
        {
            Debug.LogError($"Android 工程导出失败: {report.summary.result}");
        }

        return success;
    }

    public static void ExportAndroidProjectCommandLine()
    {
        var versionName = GetCommandLineValue("-VersionName");
        var buildNumberText = GetCommandLineValue("-BuildNumber");
        int.TryParse(buildNumberText, out var buildNumber);

        var success = ExportAndroidProject(versionName, buildNumber);
        EditorApplication.Exit(success ? 0 : 1);
    }

    public static void BuildAndroidDevelopment()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (BuildTarget.Android != EditorUserBuildSettings.activeBuildTarget)
        {
            EditorUtility.DisplayDialog("打包平台错误", "请先在 File - Build Settings 切换到 Android 平台", "确定");
            return;
        }

        BuildPlayer("Debug", BuildTargetGroup.Android, BuildTarget.Android,
            BuildOptions.CompressWithLz4 | BuildOptions.Development | BuildOptions.AllowDebugging |
            BuildOptions.ConnectWithProfiler);
    }

    private static void BuildPlayer(string extraName, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget,
        BuildOptions buildOptions)
    {
        var path = BuildPaths.GetRepoPath("BuildOutputs/Players/");
        Directory.CreateDirectory(path);
        var levels = BuildUtility.GetLevelsFromBuildSettings();
        if (levels.Length == 0)
        {
            Debug.Log("Nothing to build.");
            return;
        }

        var buildTargetName = GetBuildTargetName(buildTarget, extraName);
        if (buildTargetName == null)
        {
            return;
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = path + buildTargetName,
            targetGroup = buildTargetGroup,
            target = buildTarget,
            options = buildOptions
        };
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        EditorUtility.OpenWithDefaultApp(path);
    }

    private static void KeepAndroidGradleWrapper()
    {
        var wrapperPath = Path.Combine(BuildPaths.GetAndroidProjectExportPath(), "gradle", "wrapper",
            "gradle-wrapper.properties");
        var keepWrapperPath = Path.Combine(BuildPaths.GetAndroidKeepFilesRoot(), "gradle/wrapper/gradle-wrapper.properties");
        Directory.CreateDirectory(Path.GetDirectoryName(wrapperPath) ?? string.Empty);

        if (File.Exists(keepWrapperPath))
        {
            File.Copy(keepWrapperPath, wrapperPath, true);
            return;
        }

        File.WriteAllText(wrapperPath,
            "distributionUrl=https\\://services.gradle.org/distributions/gradle-8.11.1-bin.zip\r\n",
            new UTF8Encoding(true));
    }

    private static string GetCommandLineValue(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return string.Empty;
    }

    private static string GetTimeForNow()
    {
        return DateTime.Now.ToString("yyyyMMdd-HHmmss");
    }

    private static string GetBuildTargetName(BuildTarget target, string extraName)
    {
        var targetName =
            $"/{PlayerSettings.productName}-{extraName}-v{PlayerSettings.bundleVersion}-{GetTimeForNow()}";
        switch (target)
        {
            case BuildTarget.Android:
                return targetName + ".apk";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return targetName + ".exe";
            default:
                Debug.Log("Target not implemented.");
                return null;
        }
    }
}
