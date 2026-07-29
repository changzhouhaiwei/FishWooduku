using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Wooduku
{
    [CreateAssetMenu(menuName = "Wooduku/Level Pack", fileName = "WoodukuLevelPack")]
    public sealed class WoodukuLevelPackAsset : ScriptableObject
    {
        public int firstLevelId = 1;
        public List<WoodukuLevelFile> levels = new List<WoodukuLevelFile>();

        public int LastLevelId => firstLevelId + Mathf.Max(0, levels.Count - 1);

        public bool TryGetLevel(int levelId, out WoodukuLevelFile level)
        {
            var index = levelId - firstLevelId;
            if (index < 0 || index >= levels.Count)
            {
                level = null;
                return false;
            }

            level = levels[index];
            return level != null;
        }
    }

    [Serializable]
    public sealed class WoodukuLevelPackEntry
    {
        public int firstLevelId;
        public int lastLevelId;
        public string assetPath;

        public bool Contains(int levelId)
        {
            return levelId >= firstLevelId && levelId <= lastLevelId;
        }
    }

    [CreateAssetMenu(menuName = "Wooduku/Level Catalog", fileName = "WoodukuLevelCatalog")]
    public sealed class WoodukuLevelCatalogAsset : ScriptableObject
    {
        public int totalLevelCount;
        public int levelsPerPack = 256;
        public List<WoodukuLevelPackEntry> packs = new List<WoodukuLevelPackEntry>();

        public WoodukuLevelPackEntry FindPack(int levelId)
        {
            for (var i = 0; i < packs.Count; i++)
            {
                var entry = packs[i];
                if (entry != null && entry.Contains(levelId))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
