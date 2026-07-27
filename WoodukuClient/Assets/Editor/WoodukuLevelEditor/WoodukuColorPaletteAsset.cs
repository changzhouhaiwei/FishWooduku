using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wooduku.LevelEditor
{
    /// <summary>
    /// 颜色存档：可增删、改色、开启/关闭。关闭的颜色不参与涂色与导出。
    /// </summary>
    [CreateAssetMenu(menuName = "Wooduku/Color Palette", fileName = "WoodukuColorPalette")]
    public class WoodukuColorPaletteAsset : ScriptableObject
    {
        [Serializable]
        public class Slot
        {
            public string name = "色块";
            public Color color = Color.gray;
            public bool enabled = true;
        }

        public List<Slot> slots = new List<Slot>();

        public static readonly Color[] DefaultColors =
        {
            new Color(0.55f, 0.35f, 0.22f),
            new Color(0.85f, 0.70f, 0.25f),
            new Color(0.90f, 0.55f, 0.65f),
            new Color(0.45f, 0.70f, 0.45f),
            new Color(0.65f, 0.55f, 0.85f),
            new Color(0.75f, 0.40f, 0.45f),
            new Color(0.40f, 0.60f, 0.80f),
            new Color(0.70f, 0.70f, 0.45f),
            new Color(0.50f, 0.50f, 0.50f),
            new Color(0.35f, 0.55f, 0.55f),
            new Color(0.80f, 0.50f, 0.30f),
            new Color(0.55f, 0.35f, 0.55f),
        };

        public void EnsureDefaults(int minCount)
        {
            while (slots.Count < minCount)
            {
                var i = slots.Count;
                slots.Add(new Slot
                {
                    name = $"色{i}",
                    color = DefaultColors[i % DefaultColors.Length],
                    enabled = true
                });
            }
        }

        public List<int> GetEnabledIndices()
        {
            var list = new List<int>();
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].enabled)
                {
                    list.Add(i);
                }
            }

            return list;
        }
    }
}
