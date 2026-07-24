using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace FishFramework
{
    public abstract partial class UIView
    {
        #region 资源管理

        protected static T LoadAsset<T>(string assetPath) where T : Object
        {
            if (cacheAssets.TryGetValue(assetPath, out AssetHandle value))
            {
                return value.GetAssetObject<T>();
            }

            AssetHandle request = ResourceModule.LoadAsset(assetPath, typeof(T));
            if (request == null)
            {
                return null;
            }

            cacheAssets.Add(assetPath, request);
            return request.GetAssetObject<T>();
        }

        protected static Sprite LoadSprite(string abDirectory, string assetName)
        {
            string assetNamePath = $"Assets/GameRes/Sprites/{abDirectory}/{assetName}.png";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static Sprite LoadSpriteBg(string abDirectory, string assetName)
        {
            string assetNamePath = $"Assets/GameRes/Sprites/{abDirectory}/{assetName}.jpg";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static Sprite LoadSmallSprite(string assetName)
        {
            string assetNamePath = $"Assets/GameRes/Sprites/ItemSmall/{assetName}.png";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static Sprite LoadAtlasSprite(string abDirectory, string assetName)
        {
            string assetNamePath = $"Assets/GameRes/SpriteAtlas/{abDirectory}/{assetName}.png";
            return LoadAsset<Sprite>(assetNamePath);
        }

        protected static AudioClip LoadMusic(string assetName)
        {
            string assetNamePath = $"Assets/GameRes/Audio/Music/{assetName}.wav";
            return LoadAsset<AudioClip>(assetNamePath);
        }

        protected static AudioClip LoadSFX(string assetName)
        {
            string assetNamePath = $"Assets/GameRes/Audio/SFX/{assetName}.wav";
            return LoadAsset<AudioClip>(assetNamePath);
        }

        private static void Clear()
        {
            foreach (AssetHandle request in cacheAssets.Values)
            {
                request.Release();
            }

            cacheAssets.Clear();
        }

        #endregion 资源管理
    }
}
