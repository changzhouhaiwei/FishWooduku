using FishFramework;
using GameLogic.Settings;
using GameLogic.Wooduku;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.MainMenu
{
    /// <summary>
    /// 主页 Host：绑定 GameScene 常驻根节点。
    /// </summary>
    public sealed class UIMainMenuHost : UIView
    {
        private TButton _playButton;
        private TextMeshProUGUI _playLevelLabel;
        private readonly List<Button> _settingsButtons = new();
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

            foreach (Button button in _settingsButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(OnSettingsClicked);
                }
            }
            _settingsButtons.Clear();

            _playLevelLabel = null;
            _bound = false;
            viewBehaviour = null;
            gameObject = null;
            rectTransform = null;
        }

        protected override void OnCreating()
        {
            _playButton = FindChildRect("Play Button")?.GetComponent<TButton>();
            // Prefab 中 DailyWinButton 下也有同名的 Play Text，必须限定在主 Play Button 内查找。
            _playLevelLabel = _playButton != null
                ? FindChildRect(_playButton.transform, "Play Text")?.GetComponent<TextMeshProUGUI>()
                : null;
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
                _playButton.onClick.AddListener(OnPlayClicked);
            }
            else
            {
                Debug.LogWarning("[UIMainMenuHost] Play Button not found.");
            }

            BindSettingsButtons();
            RefreshLevelProgress();
        }

        public void RefreshLevelProgress()
        {
            if (_playLevelLabel != null)
            {
                _playLevelLabel.text = $"Level {WoodukuLevelProgress.CurrentLevelId}";
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

            gameplay.EnterLevel(WoodukuLevelProgress.CurrentLevelId);
        }

        private void BindSettingsButtons()
        {
            foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "SettingsButton")
                {
                    continue;
                }

                Button button = child.GetComponent<Button>();
                if (button == null || _settingsButtons.Contains(button))
                {
                    continue;
                }

                button.onClick.RemoveListener(OnSettingsClicked);
                button.onClick.AddListener(OnSettingsClicked);
                _settingsButtons.Add(button);
            }

            if (_settingsButtons.Count == 0)
            {
                Debug.LogWarning("[UIMainMenuHost] SettingsButton not found.");
            }
        }

        private static void OnSettingsClicked()
        {
            if (GameModule.UI == null)
            {
                Debug.LogError("[UIMainMenuHost] UI module is not initialized.");
                return;
            }

            GameModule.UI.OpenPanel<UISettings>();
        }

        private RectTransform FindChildRect(string objectName)
        {
            return FindChildRect(gameObject.transform, objectName);
        }

        private static RectTransform FindChildRect(Transform root, string objectName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
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
