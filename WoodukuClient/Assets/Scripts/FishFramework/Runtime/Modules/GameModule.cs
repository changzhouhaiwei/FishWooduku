using System.Collections;
using UnityEngine;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class GameModule : MonoBehaviour
    {
        public static AudioModule Audio { get; private set; }

        public static ObjectPoolModule ObjectPool { get; private set; }

        public static ResourceModule Resource { get; private set; }

        public static UIModule UI { get; private set; }

        public static SettingModule Setting { get; private set; }

        private void Awake()
        {
            gameObject.name = $"[{nameof(GameModule)}]";
            DontDestroyOnLoad(gameObject);

            Audio = GetComponentInChildren<AudioModule>();
            ObjectPool = GetComponentInChildren<ObjectPoolModule>();
            Resource = GetComponentInChildren<ResourceModule>();
            UI = GetComponentInChildren<UIModule>();
            Setting = GetComponentInChildren<SettingModule>();
        }

        public IEnumerator Initialize()
        {
            yield return StartCoroutine(Resource.InitializeAsync());
            if (!ResourceModule.IsInitialized)
            {
                Debug.LogError("[GameModule] Resource initialization failed.");
                yield break;
            }

            UI.InitRoot();
            yield return StartCoroutine(Setting.Initialize());
            Application.lowMemory += OnLowMemory;
        }

        private void OnApplicationQuit()
        {
            Application.lowMemory -= OnLowMemory;
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            Audio = null;
            ObjectPool = null;
            Resource = null;
            UI = null;
            Setting = null;
        }

        private void OnLowMemory()
        {
            Debug.Log("Low memory reported...");
        }
    }
}
