using FishFramework;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Settings
{
    /// <summary>
    /// 原设置界面的音乐/音效开关。type=0 为音乐，type=1 为音效。
    /// </summary>
    public sealed class SettingsSoundToggleButton : SettingsButtonBase
    {
        [SerializeField] private bool universal;
        [SerializeField] private int type;
        [SerializeField] private Image imageRef;
        [SerializeField] private Image selectionImage;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite disableSprite;

        private const string MusicPrefsKey = "musicBtn";
        private const string SoundPrefsKey = "soundBtn";

        private bool _active;

        public override void Init()
        {
            RefreshState();
        }

        private void OnEnable()
        {
            RefreshState();
        }

        public override void OnClick()
        {
            _active = !_active;

            if (universal)
            {
                ApplyVolume(true, _active);
                ApplyVolume(false, _active);
            }
            else
            {
                ApplyVolume(type == 0, _active);
            }

            PlayerPrefs.Save();
            Redraw();
        }

        public override void Select()
        {
            base.Select();
            if (selectionImage != null)
            {
                selectionImage.gameObject.SetActive(true);
            }
        }

        public override void Deselect()
        {
            base.Deselect();
            if (selectionImage != null)
            {
                selectionImage.gameObject.SetActive(false);
            }
        }

        private void RefreshState()
        {
            _active = universal
                ? PlayerPrefs.GetInt(MusicPrefsKey, 1) != 0 && PlayerPrefs.GetInt(SoundPrefsKey, 1) != 0
                : PlayerPrefs.GetInt(type == 0 ? MusicPrefsKey : SoundPrefsKey, 1) != 0;
            Redraw();
        }

        private static void ApplyVolume(bool music, bool active)
        {
            float volume = active ? 1f : 0f;
            PlayerPrefs.SetInt(music ? MusicPrefsKey : SoundPrefsKey, active ? 1 : 0);

            AudioModule audio = GameModule.Audio;
            if (audio == null)
            {
                return;
            }

            if (music)
            {
                if (audio.BGM != null)
                {
                    audio.BGM.Volume = volume;
                }
            }
            else if (audio.SFX != null)
            {
                audio.SFX.Volume = volume;
            }
        }

        private void Redraw()
        {
            if (imageRef != null)
            {
                imageRef.sprite = _active ? activeSprite : disableSprite;
            }
        }
    }
}
