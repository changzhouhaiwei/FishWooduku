using System;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 场景快速入口（对齐 ATile2 GoToScene 菜单）。
/// StartScene 快捷键：Ctrl+Shift+G
/// </summary>
public static class SceneQuickEntry
{
    private const string StartScenePath = "Assets/GameRes/ScenesInBuild/StartScene.unity";
    private const string LoginScenePath = "Assets/GameRes/Scenes/LoginScene.unity";
    private const string GameScenePath = "Assets/GameRes/Scenes/GameScene.unity";

    [MenuItem("游戏工具/工程目录")]
    private static void OpenProject()
    {
        System.Diagnostics.Process.Start(Environment.CurrentDirectory);
    }

    [MenuItem("GoToScene/StartScene %#g", false, 0)]
    private static void GoToStartScene()
    {
        OpenScene(StartScenePath);
    }

    [MenuItem("GoToScene/LoginScene", false, 1)]
    private static void GoToLoginScene()
    {
        OpenScene(LoginScenePath);
    }

    [MenuItem("GoToScene/GameScene", false, 2)]
    private static void GoToGameScene()
    {
        OpenScene(GameScenePath);
    }

    private static void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }
}
