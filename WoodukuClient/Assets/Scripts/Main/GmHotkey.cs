using FishFramework;
using UnityEngine;

/// <summary>
/// 全局 GM 热键：F1 打开 GMLevelChooseUI（对齐参考工程）。
/// Editor 始终可用；真机需 GameSettings.gmMode = true。
/// </summary>
public sealed class GmHotkey : MonoBehaviour
{
    private static GmHotkey _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
        {
            return;
        }

        var go = new GameObject("[GmHotkey]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<GmHotkey>();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F1))
        {
            return;
        }

        if (!CanOpenGm())
        {
            return;
        }

        if (GameModule.UI == null || GameModule.UI.UIRoot == null)
        {
            Debug.LogWarning("[GM] UI 尚未 InitRoot，稍后再按 F1。");
            return;
        }

        if (GameModule.UI.ExistPanel<GMLevelChooseUI>())
        {
            GameModule.UI.DestroyPanel<GMLevelChooseUI>();
            return;
        }

        // 与参考工程一致：走默认 Panel 层 OpenPanel<T>()（内部会 Open）
        Debug.Log("[GM] OpenPanel<GMLevelChooseUI> (F1)");
        GameModule.UI.OpenPanel<GMLevelChooseUI>();
    }

    private static bool CanOpenGm()
    {
#if UNITY_EDITOR
        return true;
#else
        return GameModule.Setting != null && GameModule.Setting.GmMode;
#endif
    }
}
