using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using FishFramework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键 Android 打包窗口（参考 Tile2 BuildWindow，无 Odin）。
/// </summary>
public class BuildWindow : EditorWindow
{
    public enum AndroidGradleExportType
    {
        Apk = 1,
        Aab = 2,
        ApkAndAab = 3
    }

    private string _androidVersionName;
    private int _androidBuildNumber = 1;
    private AndroidGradleExportType _androidExportType = AndroidGradleExportType.ApkAndAab;
    private bool _saveToPlayerSettings = true;
    private bool _buildAssetBundlesBeforeAndroid;
    private bool _exportSymbolsZip;
    private Vector2 _scroll;
    private GameSettings _gameSettings;

    [MenuItem("自定义窗口/打包编辑器", priority = 100)]
    private static void OpenWindow()
    {
        var window = GetWindow<BuildWindow>();
        window.titleContent = new GUIContent("打包编辑器");
        window.minSize = new Vector2(480, 520);
        window.Show();
    }

    private void OnEnable()
    {
        _androidVersionName = PlayerSettings.bundleVersion;
        _androidBuildNumber = PlayerSettings.Android.bundleVersionCode > 0
            ? PlayerSettings.Android.bundleVersionCode
            : 1;
        TryReadAndroidVersion(ref _androidVersionName, ref _androidBuildNumber);
        _gameSettings = BuildUtility.LoadGameSettings();
    }

    private void OnDisable()
    {
        AssetDatabase.SaveAssets();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawAndroidGradleSection();
        EditorGUILayout.Space(12);
        DrawBundlesSection();
        EditorGUILayout.Space(12);
        DrawSettingsSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawAndroidGradleSection()
    {
        EditorGUILayout.LabelField("Android Gradle 一键打包", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            _androidVersionName = EditorGUILayout.DelayedTextField("版本号 Version Name", _androidVersionName);
            _androidBuildNumber = Mathf.Max(1, EditorGUILayout.DelayedIntField("Build 号 Version Code", _androidBuildNumber));
            _androidExportType = (AndroidGradleExportType)EditorGUILayout.EnumPopup("导出选项", _androidExportType);
            _saveToPlayerSettings = EditorGUILayout.Toggle("同步写入 Unity PlayerSettings", _saveToPlayerSettings);
            _buildAssetBundlesBeforeAndroid = EditorGUILayout.Toggle("是否打 AssetBundle (YooAsset)", _buildAssetBundlesBeforeAndroid);
            _exportSymbolsZip = EditorGUILayout.Toggle("是否导出 symbols.zip", _exportSymbolsZip);

            EditorGUILayout.Space(6);

            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("一键导出 Android 包", GUILayout.Height(36)))
            {
                LaunchGradleBuild(exportUnityProject: true);
            }

            GUI.backgroundColor = new Color(0.75f, 0.85f, 1f);
            if (GUILayout.Button("仅 Gradle 打包（跳过 Unity 导出）", GUILayout.Height(28)))
            {
                LaunchGradleBuild(exportUnityProject: false);
            }

            GUI.backgroundColor = oldColor;
            if (GUILayout.Button("打开 Android 包输出目录", GUILayout.Height(24)))
            {
                var outputPath = BuildPaths.GetRepoPath("BuildOutputs/Android");
                Directory.CreateDirectory(outputPath);
                EditorUtility.OpenWithDefaultApp(outputPath);
            }
        }
    }

    private void DrawBundlesSection()
    {
        EditorGUILayout.LabelField("生成 Bundles", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("Build All Bundles (YooAsset)", GUILayout.Height(32)))
            {
                BuildUtility.BuildAllBundles();
                _gameSettings = BuildUtility.LoadGameSettings();
            }

            GUI.backgroundColor = oldColor;
        }
    }

    private void DrawSettingsSection()
    {
        EditorGUILayout.LabelField("配置 (GameSettings)", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            _gameSettings = (GameSettings)EditorGUILayout.ObjectField("GameSettings", _gameSettings, typeof(GameSettings), false);
            if (_gameSettings == null)
            {
                EditorGUILayout.HelpBox("未找到 Resources/GameSettings。可在 Project 窗口 Create → GameSettings 后放到 Resources。", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _gameSettings.logMode = EditorGUILayout.Toggle("日志模式", _gameSettings.logMode);
            _gameSettings.gmMode = EditorGUILayout.Toggle("GM", _gameSettings.gmMode);
            _gameSettings.appVersion = EditorGUILayout.IntField("游戏大版本", _gameSettings.appVersion);
            EditorGUILayout.LabelField("资源版本", _gameSettings.GetVersion());
            _gameSettings.major = EditorGUILayout.IntField("Major", _gameSettings.major);
            _gameSettings.minor = EditorGUILayout.IntField("Minor", _gameSettings.minor);
            _gameSettings.build = EditorGUILayout.IntField("Build", _gameSettings.build);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_gameSettings);
            }
        }
    }

    private void LaunchGradleBuild(bool exportUnityProject)
    {
        if (!ValidateAndroidVersionInputs())
        {
            return;
        }

        SaveAndroidVersionDefaultsToDisk(_androidVersionName.Trim(), _androidBuildNumber, _saveToPlayerSettings, true);

        if (_buildAssetBundlesBeforeAndroid)
        {
            BuildUtility.BuildAllBundles();
        }

        if (exportUnityProject)
        {
            if (!CustomBuildPlayer.ExportAndroidProject(
                    _saveToPlayerSettings ? _androidVersionName.Trim() : null,
                    _saveToPlayerSettings ? _androidBuildNumber : 0))
            {
                EditorUtility.DisplayDialog("导出失败", "Android 工程导出失败，请查看 Console 日志。", "确定");
                return;
            }
        }

        var scriptPath = BuildPaths.GetRepoPath("BuildAndroid.ps1");
        if (!File.Exists(scriptPath))
        {
            EditorUtility.DisplayDialog("找不到打包脚本", $"脚本不存在：{scriptPath}", "确定");
            return;
        }

        var args = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" " +
                   $"-VersionName \"{_androidVersionName.Trim()}\" " +
                   $"-BuildNumber {_androidBuildNumber} " +
                   $"-BuildTarget {(int)_androidExportType} " +
                   $"-SkipUnityExport true " +
                   $"-ExportSymbolsZip {_exportSymbolsZip}";

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = args,
            WorkingDirectory = BuildPaths.GetRepoPath(),
            UseShellExecute = true
        });

        UnityEngine.Debug.Log(
            $"已启动 Android Gradle 打包：VersionName={_androidVersionName}, BuildNumber={_androidBuildNumber}, ExportType={_androidExportType}, UnityExport={(exportUnityProject ? "done" : "skipped")}");
    }

    private bool ValidateAndroidVersionInputs()
    {
        if (string.IsNullOrWhiteSpace(_androidVersionName))
        {
            EditorUtility.DisplayDialog("参数错误", "版本号不能为空", "确定");
            return false;
        }

        if (_androidBuildNumber <= 0)
        {
            EditorUtility.DisplayDialog("参数错误", "Build 号必须大于 0", "确定");
            return false;
        }

        return true;
    }

    private static void SaveAndroidVersionDefaultsToDisk(string versionName, int buildNumber, bool saveToPlayerSettings,
        bool log)
    {
        if (saveToPlayerSettings)
        {
            PlayerSettings.bundleVersion = versionName;
            PlayerSettings.Android.bundleVersionCode = buildNumber;
            AssetDatabase.SaveAssets();
        }

        UpdateLauncherGradleVersionDefaults(
            BuildPaths.GetRepoPath($"{BuildPaths.AndroidProjectFolderName}/launcher/build.gradle"), versionName,
            buildNumber);
        UpdateLauncherGradleVersionDefaults(
            Path.Combine(BuildPaths.GetAndroidKeepFilesRoot(), "launcher/build.gradle"), versionName, buildNumber);
        AssetDatabase.Refresh();

        if (log)
        {
            UnityEngine.Debug.Log($"已保存 Android 版本号：VersionName={versionName}, VersionCode={buildNumber}");
        }
    }

    private static void UpdateLauncherGradleVersionDefaults(string launcherGradlePath, string versionName, int buildNumber)
    {
        if (!File.Exists(launcherGradlePath))
        {
            return;
        }

        var content = File.ReadAllText(launcherGradlePath);
        content = Regex.Replace(content, @"VERSION_CODE'\)\s*\?:\s*'\d+'",
            _ => $"VERSION_CODE') ?: '{buildNumber}'");
        content = Regex.Replace(content, @"VERSION_NAME'\)\s*\?:\s*'[^']+'",
            _ => $"VERSION_NAME') ?: '{versionName}'");
        content = Regex.Replace(content, @"versionCode\s+(\d+)", _ => $"versionCode {buildNumber}");
        content = Regex.Replace(content, @"versionName\s+'[^']+'", _ => $"versionName '{versionName}'");
        File.WriteAllText(launcherGradlePath, content);
    }

    private static bool TryReadAndroidVersion(ref string versionName, ref int buildNumber)
    {
        var launcherGradle = BuildPaths.GetRepoPath($"{BuildPaths.AndroidProjectFolderName}/launcher/build.gradle");
        if (!File.Exists(launcherGradle))
        {
            launcherGradle = Path.Combine(BuildPaths.GetAndroidKeepFilesRoot(), "launcher/build.gradle");
        }

        if (!File.Exists(launcherGradle))
        {
            return false;
        }

        var content = File.ReadAllText(launcherGradle);
        var versionNameMatch = Regex.Match(content, @"VERSION_NAME'\)\s*\?:\s*'([^']+)'");
        if (!versionNameMatch.Success)
        {
            versionNameMatch = Regex.Match(content, @"versionName\s+'([^']+)'");
        }

        if (versionNameMatch.Success)
        {
            versionName = versionNameMatch.Groups[1].Value;
        }

        var buildNumberMatch = Regex.Match(content, @"VERSION_CODE'\)\s*\?:\s*'(\d+)'");
        if (!buildNumberMatch.Success)
        {
            buildNumberMatch = Regex.Match(content, @"versionCode\s+(\d+)");
        }

        if (buildNumberMatch.Success && int.TryParse(buildNumberMatch.Groups[1].Value, out var parsedBuildNumber))
        {
            buildNumber = parsedBuildNumber;
        }

        return versionNameMatch.Success || buildNumberMatch.Success;
    }
}
