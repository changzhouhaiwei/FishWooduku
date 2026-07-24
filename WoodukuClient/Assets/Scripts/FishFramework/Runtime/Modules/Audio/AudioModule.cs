using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class AudioModule : MonoBehaviour
    {
        [SerializeField] private List<AudioDatabase> databases = new List<AudioDatabase>(0);

        private AudioListener audioListener;

        //AudioListener跟随的Transform
        private Transform listenerTrans;

        public BGMController BGM { get; private set; }
        public SFXController SFX { get; private set; }

        private bool m_bVibrate;
        private void Awake()
        {
            EnsureSingleListener();
            SceneManager.sceneLoaded += OnSceneLoaded;

            BGM = GetComponentInChildren<BGMController>();
            SFX = GetComponentInChildren<SFXController>();

            InitVibrateAndMusicSound();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 新场景相机常自带 AudioListener，统一清掉，只保留框架 Listener
            EnsureSingleListener();
        }

        private void EnsureSingleListener()
        {
            var listeners = FindObjectsOfType<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null)
                {
                    continue;
                }

                if (audioListener != null && listener == audioListener)
                {
                    continue;
                }

                Destroy(listener);
            }

            if (audioListener == null)
            {
                var listenerGo = new GameObject("Listener");
                listenerGo.transform.SetParent(transform);
                audioListener = listenerGo.AddComponent<AudioListener>();
            }
            else if (!audioListener.enabled)
            {
                audioListener.enabled = true;
            }
        }

        private void Update()
        {
            if (listenerTrans != null && audioListener != null)
                audioListener.transform.position = listenerTrans.position;
        }

        public void SetListener(Transform listenerTrans)
        {
            this.listenerTrans = listenerTrans;
        }

        public AudioClip FromDatabase(string databaseName, string clipName)
        {
            AudioDatabase database = databases.Find(m => m.name == databaseName);
            return database != null ? database[clipName] : null;
        }

        public AudioClip FromDatabase(string databaseName, string clipName, out AudioMixerGroup outputAudioMixerGroup)
        {
            outputAudioMixerGroup = null;
            AudioDatabase database = databases.Find(m => m.name == databaseName);
            if (database != null)
            {
                outputAudioMixerGroup = database.outputAudioMixerGroup;
                return database[clipName];
            }

            return null;
        }

        public void InitVibrateAndMusicSound()
        {
            var val1 = PlayerPrefs.GetInt("vibrateBtn", 1);
            m_bVibrate = (val1 == 1 ? true : false);

            var val2 = PlayerPrefs.GetInt("musicBtn", 1);
            if (BGM != null) BGM.Volume = val2;

            var val3 = PlayerPrefs.GetInt("soundBtn", 1);
            if (SFX != null) SFX.Volume = val3;
        }

        public void PlayVibrate(float v1, float v2, float v3)
        {
            if (m_bVibrate)
            {
                // HapticPatterns.PlayConstant(v1, v2, v3);
            }
        }

        public void SetBVibrate(bool b)
        {
            m_bVibrate = b;
        }
    }
}
