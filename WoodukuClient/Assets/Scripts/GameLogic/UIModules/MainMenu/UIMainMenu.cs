using FishFramework;
using UnityEngine;

namespace GameLogic.MainMenu
{
    /// <summary>
    /// GameScene 常驻主页入口（保留源 Prefab 脚本 GUID）。
    /// 忽略 UIPage/UIController，改用 UIMainMenuHost(UIView) 挂载。
    /// </summary>
    public class UIMainMenu : MonoBehaviour
    {
        private UIMainMenuHost _host;

        private void Start()
        {
            // UIViewBehaviour 是 abstract，必须用具体子类
            var behaviour = GetComponent<UIViewBehaviour>();
            if (behaviour == null)
            {
                behaviour = gameObject.AddComponent<UIPanelBehaviour>();
            }

            _host = new UIMainMenuHost();
            _host.Bind(behaviour);
            Debug.Log("[UIMainMenu] Host bound to resident root.");
        }

        private void OnDestroy()
        {
            _host?.Unbind();
            _host = null;
        }
    }
}
