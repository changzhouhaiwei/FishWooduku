using System.Collections;
using FishFramework;
using UnityEngine;

/// <summary>
/// StartScene 入口：初始化 GameModule / YooAsset，再进入 LoginScene。
/// </summary>
public class MainBootstrap : MonoBehaviour
{
    [SerializeField] private float splashDelay = 0.3f;

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        if (splashDelay > 0f)
        {
            yield return new WaitForSeconds(splashDelay);
        }

        var gameModule = FindObjectOfType<GameModule>();
        if (gameModule == null)
        {
            Debug.LogError("[MainBootstrap] GameModule not found in StartScene.");
            yield break;
        }

        yield return gameModule.StartCoroutine(gameModule.Initialize());

        Debug.Log("[MainBootstrap] Framework ready, loading LoginScene...");
        SceneLoaderHelper.LoadSceneAsync(ScenePaths.LoginScene);
    }
}
