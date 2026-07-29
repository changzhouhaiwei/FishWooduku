using FishFramework;
using GameLogic.Wooduku;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.MainMenu
{
    /// <summary>
    /// 主页 Host：绑定 GameScene 常驻根节点。
    /// </summary>
    public sealed class UIMainMenuHost : UIView
    {
        private Button _playButton;
        private bool _bound;

        public void Bind(UIViewBehaviour rootBehaviour)
        {
            if (rootBehaviour == null)
            {
                throw new System.ArgumentNullException(nameof(rootBehaviour));
            }

            if (_bound)
            {
                return;
            }

            // parent 可为 null（场景根实例）；与当前 parent 一致则不会被 SetParent
            Create(nameof(UIMainMenuHost), rootBehaviour.transform.parent, rootBehaviour);
            _bound = true;
        }

        public void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            // 只解绑逻辑，不 Destroy 常驻根（由场景 / UIMainMenu 负责销毁 GameObject）
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
                _playButton = null;
            }

            _bound = false;
            viewBehaviour = null;
            gameObject = null;
            rectTransform = null;
        }

        protected override void OnCreating()
        {
            _playButton = FindChildRect("Play Button")?.GetComponent<Button>();
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
                _playButton.onClick.AddListener(OnPlayClicked);
            }
            else
            {
                Debug.LogWarning("[UIMainMenuHost] Play Button not found.");
            }
        }

        private void OnPlayClicked()
        {
            var gameplay = WoodukuGameplayView.EnsureSpawned();
            if (gameplay == null)
            {
                Debug.LogError("[UIMainMenuHost] WoodukuGameplayView spawn failed.");
                return;
            }

            gameplay.EnterLevel(1);
        }

        private RectTransform FindChildRect(string objectName)
        {
            foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }
    }
}
