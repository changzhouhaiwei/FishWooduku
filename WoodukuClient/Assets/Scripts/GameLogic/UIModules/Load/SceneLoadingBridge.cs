using FishFramework;
using UnityEngine;

/// <summary>
/// 将 LoadingUI 接到 SceneLoaderHelper 的 Begin/End 回调。
/// </summary>
public static class SceneLoadingBridge
{
    private static bool _registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        SceneLoaderHelper.OnLoadingBegin = ShowLoading;
        SceneLoaderHelper.OnLoadingEnd = HideLoading;
    }

        private static void ShowLoading()
        {
            if (GameModule.UI == null)
            {
                return;
            }

            var panel = GameModule.UI.OpenPanel<LoadingUI>(PanelLayer.Tips, PanelOpenType.Single);
            panel?.Open();
        }

    private static void HideLoading()
    {
        if (GameModule.UI == null)
        {
            return;
        }

        GameModule.UI.DestroyPanel<LoadingUI>();
    }
}
