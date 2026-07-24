using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace FishFramework
{
    public class UILoadHelper : MonoBehaviour
    {
        private static readonly Dictionary<string, AssetHandle> cacheAssets = new();

        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if (cacheAssets.TryGetValue(assetPath, out AssetHandle value))
            {
                return value.GetAssetObject<T>();
            }

            AssetHandle handle = ResourceModule.LoadAsset(assetPath, typeof(T));
            if (handle == null)
            {
                return null;
            }

            cacheAssets.Add(assetPath, handle);
            return handle.GetAssetObject<T>();
        }

        public static void LoadAssetAsync<T>(string assetPath, Action<T> onComplete) where T : UnityEngine.Object
        {
            if (cacheAssets.TryGetValue(assetPath, out AssetHandle cached))
            {
                onComplete?.Invoke(cached.GetAssetObject<T>());
                return;
            }

            ResourceModule.LoadAssetAsync(assetPath, typeof(T), handle =>
            {
                if (handle == null)
                {
                    onComplete?.Invoke(null);
                    return;
                }

                if (!cacheAssets.ContainsKey(assetPath))
                {
                    cacheAssets.Add(assetPath, handle);
                }

                onComplete?.Invoke(handle.GetAssetObject<T>());
            });
        }

        public static void RemoveAsset(string assetPath)
        {
            if (cacheAssets.TryGetValue(assetPath, out AssetHandle value))
            {
                value.Release();
                cacheAssets.Remove(assetPath);
            }
        }
    }
}
