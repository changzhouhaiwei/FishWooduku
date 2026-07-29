using System.Collections.Generic;
using FishFramework;
using UnityEngine;

namespace GameLogic.Wooduku
{
    public static class WoodukuLevelRepository
    {
        public const string CatalogPath =
            "Assets/GameRes/WoodukuLevels/Imported/WoodukuLevelCatalog.asset";

        public const int LegacyLevelCount = 10;

        private static WoodukuLevelCatalogAsset _catalog;
        private static readonly Dictionary<string, WoodukuLevelPackAsset> LoadedPacks =
            new Dictionary<string, WoodukuLevelPackAsset>();

        public static int TotalLevelCount
        {
            get
            {
                EnsureCatalog();
                return _catalog != null && _catalog.totalLevelCount > 0
                    ? _catalog.totalLevelCount
                    : LegacyLevelCount;
            }
        }

        public static bool TryLoadLevel(int levelId, out WoodukuLevelFile level)
        {
            level = null;
            if (levelId < 1)
            {
                return false;
            }

            EnsureCatalog();
            var entry = _catalog?.FindPack(levelId);
            if (entry != null && !string.IsNullOrEmpty(entry.assetPath))
            {
                if (!LoadedPacks.TryGetValue(entry.assetPath, out var pack) || pack == null)
                {
                    pack = ResourceModule.LoadAsset<WoodukuLevelPackAsset>(entry.assetPath);
                    if (pack != null)
                    {
                        LoadedPacks[entry.assetPath] = pack;
                    }
                }

                if (pack != null && pack.TryGetLevel(levelId, out level))
                {
                    return true;
                }
            }

            return TryLoadLegacyLevel(levelId, out level);
        }

        public static void ClearCache()
        {
            _catalog = null;
            LoadedPacks.Clear();
        }

        private static void EnsureCatalog()
        {
            if (_catalog == null && ResourceModule.IsInitialized)
            {
                _catalog = ResourceModule.LoadAsset<WoodukuLevelCatalogAsset>(CatalogPath);
            }
        }

        private static bool TryLoadLegacyLevel(int levelId, out WoodukuLevelFile level)
        {
            level = null;
            var path = $"Assets/GameRes/WoodukuLevels/level_{levelId:D3}.json";
            var textAsset = ResourceModule.LoadAsset<TextAsset>(path);
            if (textAsset == null)
            {
                return false;
            }

            level = WoodukuLevelJson.FromJson(textAsset.text);
            return level != null;
        }
    }
}
