using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Object = UnityEngine.Object;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class ResourceModule : MonoBehaviour
    {
        public const string DefaultPackageName = "DefaultPackage";

        [Tooltip("是否开启日志")] public bool loggable = true;

        [Tooltip("非编辑器默认运行模式（编辑器固定使用 EditorSimulateMode）")]
        [SerializeField]
        private EPlayMode playMode = EPlayMode.OfflinePlayMode;

        public static ResourcePackage Package { get; private set; }

        public static bool IsInitialized => Package != null && Package.InitializeStatus == EOperationStatus.Succeeded;

        public IEnumerator InitializeAsync()
        {
            if (!YooAssets.IsInitialized)
            {
                YooAssets.Initialize();
            }

            if (!YooAssets.TryGetPackage(DefaultPackageName, out var package))
            {
                package = YooAssets.CreatePackage(DefaultPackageName);
            }

            Package = package;

            InitializePackageOperation initOp = null;

#if UNITY_EDITOR
            {
                var buildResult = EditorSimulateBuildInvoker.Build(DefaultPackageName, (int)EBundleType.VirtualAssetBundle);
                var packageRoot = buildResult.PackageRootDirectory;
                var options = new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
                };
                initOp = package.InitializePackageAsync(options);
            }
#else
            {
                if (playMode == EPlayMode.OfflinePlayMode)
                {
                    var options = new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                    };
                    initOp = package.InitializePackageAsync(options);
                }
                else if (playMode == EPlayMode.HostPlayMode)
                {
                    Debug.LogError("[ResourceModule] HostPlayMode requires remote service; falling back to OfflinePlayMode.");
                    var options = new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                    };
                    initOp = package.InitializePackageAsync(options);
                }
                else
                {
                    var options = new OfflinePlayModeOptions
                    {
                        BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                    };
                    initOp = package.InitializePackageAsync(options);
                }
            }
#endif

            yield return initOp;

            if (initOp.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceModule] InitializePackage failed: {initOp.Error}");
                yield break;
            }

            var versionOp = package.RequestPackageVersionAsync();
            yield return versionOp;
            if (versionOp.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceModule] RequestPackageVersion failed: {versionOp.Error}");
                yield break;
            }

            var manifestOp = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(versionOp.PackageVersion, 60));
            yield return manifestOp;
            if (manifestOp.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceModule] LoadPackageManifest failed: {manifestOp.Error}");
                yield break;
            }

            if (loggable)
            {
                Debug.Log($"[ResourceModule] Ready. Package={DefaultPackageName}, Version={versionOp.PackageVersion}");
            }
        }

        public static AssetHandle LoadAsset(string assetPath, Type type)
        {
            if (Package == null)
            {
                Debug.LogError("[ResourceModule] Package not initialized.");
                return null;
            }

            var handle = Package.LoadAssetSync(assetPath, type);
            if (handle == null || handle.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceModule] LoadAsset failed: {assetPath}, {handle?.Error}");
                handle?.Release();
                return null;
            }

            return handle;
        }

        public static void LoadAssetAsync(string assetPath, Type type, Action<AssetHandle> callback)
        {
            if (Package == null)
            {
                Debug.LogError("[ResourceModule] Package not initialized.");
                callback?.Invoke(null);
                return;
            }

            var handle = Package.LoadAssetAsync(assetPath, type);
            handle.Completed += h =>
            {
                if (h.Status != EOperationStatus.Succeeded)
                {
                    Debug.LogError($"[ResourceModule] LoadAssetAsync failed: {assetPath}, {h.Error}");
                    h.Release();
                    callback?.Invoke(null);
                    return;
                }

                callback?.Invoke(h);
            };
        }

        public static void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : Object
        {
            LoadAssetAsync(assetPath, typeof(T), handle =>
            {
                if (handle == null)
                {
                    callback?.Invoke(null);
                    return;
                }

                callback?.Invoke(handle.GetAssetObject<T>());
            });
        }

        public static T LoadAsset<T>(string assetPath) where T : Object
        {
            var handle = LoadAsset(assetPath, typeof(T));
            if (handle == null)
            {
                return null;
            }

            return handle.GetAssetObject<T>();
        }

        /// <summary>
        /// 异步加载场景（YooAsset）。
        /// </summary>
        public static SceneHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single,
            Action<SceneHandle> onCompleted = null)
        {
            if (Package == null)
            {
                Debug.LogError("[ResourceModule] Package not initialized.");
                return null;
            }

            var handle = Package.LoadSceneAsync(location, sceneMode);
            if (onCompleted != null)
            {
                handle.Completed += onCompleted;
            }

            return handle;
        }

        /// <summary>
        /// 释放当前可释放的内存（资源缓存、未使用资源、GC）。
        /// </summary>
        public static AsyncOperation ReleaseAllReleasableMemory()
        {
            if (Package != null)
            {
                Package.UnloadUnusedAssetsAsync();
            }

            GC.Collect();
            var unloadOp = Resources.UnloadUnusedAssets();
            GC.Collect();
            return unloadOp;
        }
    }
}
