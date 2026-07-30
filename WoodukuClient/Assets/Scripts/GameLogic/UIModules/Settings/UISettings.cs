using FishFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Settings
{
    /// <summary>
    /// 主菜单设置面板。沿用原工程界面，只接入当前工程已有功能。
    /// </summary>
    public sealed class UISettings : UIPanel
    {
        private Button _closeButton;
        private Button _languageButton;
        private CanvasGroup _languageDialog;
        private TextMeshProUGUI _deviceLabel;
        private TextMeshProUGUI _resolutionLabel;

        protected override string PrefabPath => "Assets/GameRes/Prefabs/TUI/Canvas/UI Settings.prefab";

        protected override void OnBindCompsAndEvents()
        {
            Transform root = viewBehaviour.transform;
            _closeButton = Find<Button>(root, "Canvas/upper/Close Button");
            _languageButton = Find<Button>(root, "Canvas/mid/language");
            _languageDialog = Find<CanvasGroup>(root, "Canvas/UILanguage");
            _deviceLabel = Find<TextMeshProUGUI>(root, "Canvas/text");
            _resolutionLabel = Find<TextMeshProUGUI>(root, "Canvas/text (1)");

            SetUnsupportedElementsVisible(root, false);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(CloseSettings);
            }

            if (_languageButton != null)
            {
                _languageButton.onClick.AddListener(OpenLanguageDialog);
            }
        }

        protected override void OnOpen()
        {
            if (_deviceLabel != null)
            {
                _deviceLabel.text = SystemInfo.deviceModel;
            }

            if (_resolutionLabel != null)
            {
                _resolutionLabel.text = $"{Screen.width}x{Screen.height}";
            }
        }

        protected override void OnUnbindCompsAndEvents()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(CloseSettings);
            }

            if (_languageButton != null)
            {
                _languageButton.onClick.RemoveListener(OpenLanguageDialog);
            }

            _closeButton = null;
            _languageButton = null;
            _languageDialog = null;
            _deviceLabel = null;
            _resolutionLabel = null;
        }

        protected override bool OnEscButtonPressed()
        {
            CloseSettings();
            return true;
        }

        private void CloseSettings()
        {
            CloseSelf();
        }

        private void OpenLanguageDialog()
        {
            if (_languageDialog == null)
            {
                return;
            }

            _languageDialog.alpha = 1f;
            _languageDialog.interactable = true;
            _languageDialog.blocksRaycasts = true;
        }

        private static void SetUnsupportedElementsVisible(Transform root, bool visible)
        {
            string[] paths =
            {
                "Canvas/GMBtn",
                "Canvas/mid/cmpBtn",
                "Canvas/mid/restoreBtn",
                "Canvas/mid/RateUs",
                "Canvas/mid/customerBtn",
                "Canvas/bottom/privacy",
                "Canvas/bottom/service",
                "Canvas/upper/Auto Complete"
            };

            foreach (string path in paths)
            {
                Transform node = root.Find(path);
                if (node != null)
                {
                    node.gameObject.SetActive(visible);
                }
            }
        }

        private static T Find<T>(Transform root, string path) where T : Component
        {
            Transform node = root.Find(path);
            return node != null ? node.GetComponent<T>() : null;
        }
    }
}
