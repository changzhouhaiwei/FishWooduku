using System.IO;
using UnityEngine;

/// <summary>
/// 仓库 / Unity 工程 / Android 导出路径约定。
/// Unity 工程：&lt;Repo&gt;/NewFishFramework ；导出：&lt;Repo&gt;/NewFishAndroid
/// </summary>
public static class BuildPaths
{
    public const string AndroidProjectFolderName = "NewFishAndroid";
    public const string UnityProjectFolderName = "NewFishFramework";
    public const string DefaultApplicationId = "com.defaultcompany.newfishframework";
    public const string DefaultGameName = "NewFishFramework";

    public static string GetUnityProjectPath()
    {
        return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
    }

    public static string GetRepoPath(string relativePath = "")
    {
        var unityProjectPath = GetUnityProjectPath();
        var repoPath = Directory.GetParent(unityProjectPath)?.FullName ?? unityProjectPath;
        return string.IsNullOrEmpty(relativePath) ? repoPath : Path.Combine(repoPath, relativePath);
    }

    public static string GetAndroidProjectExportPath()
    {
        return GetRepoPath(AndroidProjectFolderName);
    }

    public static string GetAndroidKeepFilesRoot()
    {
        return Path.Combine(Application.dataPath, "Editor/AndroidKeepFiles");
    }
}
