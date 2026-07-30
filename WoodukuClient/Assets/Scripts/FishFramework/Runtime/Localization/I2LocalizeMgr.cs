using I2.Loc;
using UnityEngine;

namespace FishFramework
{
    /// <summary>
    /// I2 本地化入口。语言资源随包放在 Resources/I2Languages.asset 中。
    /// </summary>
    public sealed class I2LocalizeMgr : MonoSingleton<I2LocalizeMgr>
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void UpdateSources()
        {
            LocalizationManager.UpdateSources();
            LocalizationManager.LocalizeAll(true);
        }

        public static void SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return;
            }

            LocalizationManager.CurrentLanguage = language;
        }

        public static string GetLanguage()
        {
            return LocalizationManager.CurrentLanguage;
        }

        public static string GetString(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return string.Empty;
            }

            string text = LocalizationManager.GetTranslation(term);
            return string.IsNullOrEmpty(text) ? term : text;
        }
    }
}
