using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishFramework
{
    public class TToggle : Toggle
    {
        public string AudioIdOne = "1058";
        public string AudioCancle = "2167";
        private bool musicPlayState = true;

        [SerializeField] private Color _NoSelectColor = Color.white;

        public Color NoSelectColor
        {
            get { return _NoSelectColor; }
            set
            {
                _NoSelectColor = value;
                SetLabelColor(isOn ? SelectColor : NoSelectColor);
            }
        }

        [SerializeField] private Color _SelectColor = Color.black;

        public Color SelectColor
        {
            get { return _SelectColor; }
            set
            {
                _SelectColor = value;
                SetLabelColor(isOn ? SelectColor : NoSelectColor);
            }
        }

        [SerializeField] private TextMeshProUGUI _ToggleLabel;

        public TextMeshProUGUI ToggleLabel
        {
            get { return _ToggleLabel; }
            set
            {
                _ToggleLabel = value;
                SetLabelColor(isOn ? SelectColor : NoSelectColor);
            }
        }

        [SerializeField] private GameObject _NoSelectNode = null;

        public GameObject NoSelectNode
        {
            get { return _NoSelectNode; }
            set
            {
                _NoSelectNode = value;
                SetSelectShow();
            }
        }

        [SerializeField] private GameObject _SelectNode = null;

        public GameObject SelectNode
        {
            get { return _SelectNode; }
            set
            {
                _SelectNode = value;
                SetSelectShow();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            onValueChanged.AddListener(isOn => OnToggleClick());
        }

        public void OnToggleClick()
        {
            if (musicPlayState)
            {
                if (isOn)
                {
                    if (!string.IsNullOrEmpty(AudioIdOne))
                    {
                    }
                }
                else
                {
                }
            }

            SetLabelColor(isOn ? SelectColor : NoSelectColor);
            SetSelectShow();
        }

        public void SetLabelColor(Color color)
        {
            if (ToggleLabel != null)
            {
                ToggleLabel.color = color;
            }
        }

        public void SetSelectShow()
        {
            if (SelectNode != null)
            {
                SelectNode.SetActive(isOn);
            }

            if (NoSelectNode != null)
            {
                NoSelectNode.SetActive(!isOn);
            }
        }

        public void SetAudioId(string audioId, string cancelAudioId)
        {
            if (!string.IsNullOrEmpty(audioId))
                AudioIdOne = audioId;

            if (!string.IsNullOrEmpty(cancelAudioId))
                AudioCancle = cancelAudioId;
        }

        public void CloseMusic()
        {
            musicPlayState = false;
        }

        public void OpenMusic()
        {
            musicPlayState = true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetLabelColor(isOn ? SelectColor : NoSelectColor);
            SetSelectShow();
        }
#endif
    }
}