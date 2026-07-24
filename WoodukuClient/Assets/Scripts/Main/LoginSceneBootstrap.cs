using FishFramework;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// LoginScene 入口：打开 LoginUI Panel。
/// </summary>
public class LoginSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        EnsureEventSystem();

        if (GameModule.UI == null)
        {
            Debug.LogError("[LoginScene] GameModule.UI is null. Was StartScene bootstrap skipped?");
            return;
        }

        // 从 GameScene 返回时恢复专用 UICamera
        GameModule.UI.RestoreUICamera();

        Debug.Log("[LoginScene] OpenPanel<LoginUI>");
        GameModule.UI.OpenPanel<LoginUI>();
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }
}
