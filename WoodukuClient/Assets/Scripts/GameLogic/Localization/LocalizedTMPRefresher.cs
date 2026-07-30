using System;
using I2.Loc;
using TMPro;
using UnityEngine;

namespace GameLogic.Localization
{
    /// <summary>
    /// 为运行时拼接的 TMP 文本保留刷新规则，使其能响应 I2 语言切换。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Game/Localization/Localized TMP Refresher")]
    public sealed class LocalizedTMPRefresher : MonoBehaviour
    {
        private TMP_Text _text;
        private Func<string> _getLocalizedText;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLocalizeEvent -= Refresh;
            LocalizationManager.OnLocalizeEvent += Refresh;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLocalizeEvent -= Refresh;
        }

        public static void SetText(TMP_Text target, Func<string> getLocalizedText)
        {
            if (target == null || getLocalizedText == null)
            {
                return;
            }

            target.text = getLocalizedText();

            LocalizedTMPRefresher refresher = target.GetComponent<LocalizedTMPRefresher>();
            if (refresher != null)
            {
                refresher._getLocalizedText = getLocalizedText;
            }
        }

        private void Refresh()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }

            if (_text == null)
            {
                return;
            }

            if (_getLocalizedText != null)
            {
                _text.text = _getLocalizedText();
                return;
            }

            Localize localize = GetComponent<Localize>();
            if (localize != null)
            {
                localize.OnLocalize();
            }
        }
    }
}
