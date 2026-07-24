using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace FishFramework
{
    /// <summary>
    /// 场景加载封装：走 YooAsset，失败时回退到 SceneManager（需在 Build Settings 中）。
    /// </summary>
    public static class SceneLoaderHelper
    {
        /// <summary>
        /// 可选：加载前回调（如打开 LoadingUI），完成后回调（如关闭 LoadingUI）。
        /// </summary>
        public static Action OnLoadingBegin { get; set; }

        public static Action OnLoadingEnd { get; set; }

        public static SceneHandle LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Single,
            Action onCompleted = null)
        {
            return LoadSceneAsync(location, mode, needLoading: false, onCompleted);
        }

        public static SceneHandle LoadSceneAsync(string location, bool needLoading, Action onCompleted = null)
        {
            return LoadSceneAsync(location, LoadSceneMode.Single, needLoading, onCompleted);
        }

        public static SceneHandle LoadSceneAsync(string location, LoadSceneMode mode, bool needLoading,
            Action onCompleted = null)
        {
            if (needLoading)
            {
                OnLoadingBegin?.Invoke();
            }

            void Finish()
            {
                if (needLoading)
                {
                    OnLoadingEnd?.Invoke();
                }

                onCompleted?.Invoke();
            }

            if (ResourceModule.IsInitialized)
            {
                return ResourceModule.LoadSceneAsync(location, mode, handle =>
                {
                    if (handle.Status != EOperationStatus.Succeeded)
                    {
                        Debug.LogError(
                            $"[SceneLoader] YooAsset load failed: {location}, {handle.Error}. Fallback SceneManager.");
                        LoadBySceneManager(location, mode, Finish);
                        return;
                    }

                    Finish();
                });
            }

            Debug.LogWarning($"[SceneLoader] ResourceModule not ready, fallback SceneManager: {location}");
            LoadBySceneManager(location, mode, Finish);
            return null;
        }

        private static void LoadBySceneManager(string location, LoadSceneMode mode, Action onCompleted)
        {
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(location);
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] SceneManager cannot load '{sceneName}'. Is it in Build Settings?");
                return;
            }

            op.completed += _ => onCompleted?.Invoke();
        }
    }
}
