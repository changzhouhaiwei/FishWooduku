using System;
using System.Collections.Generic;
using System.IO;
using GameLogic.Wooduku;
using UnityEditor;
using UnityEngine;

namespace Wooduku.LevelEditor
{
    public static class QueensMasterLevelPackImporter
    {
        private const int LevelsPerPack = 256;
        private const string OutputRoot = "Assets/GameRes/WoodukuLevels/Imported";
        private const string CatalogPath = OutputRoot + "/WoodukuLevelCatalog.asset";
        private const string SourceFileName = "QueensMaster_2.3.0_square_single_queen.json";

        [Serializable]
        private sealed class SourcePayload
        {
            public string source;
            public string unityVersion;
            public SourceLevel[] levels;
        }

        [Serializable]
        private sealed class SourceLevel
        {
            public string sourceName;
            public int difficulty;
            public int difficultyScore;
            public int size;
            public int[] regions;
            public int[] solutionCols;
            public WoodukuCellRef[] fixedQueenCells;
        }

        [MenuItem("自定义窗口/Wooduku/导入 Queens Master 正方形关卡", priority = 81)]
        public static void Import()
        {
            var sourcePath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../WoodukuDoc", SourceFileName));
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[QueensMasterImporter] 找不到标准化关卡文件：{sourcePath}");
                return;
            }

            SourcePayload payload;
            try
            {
                payload = JsonUtility.FromJson<SourcePayload>(File.ReadAllText(sourcePath));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[QueensMasterImporter] 读取关卡文件失败：{exception}");
                return;
            }

            if (payload?.levels == null || payload.levels.Length == 0)
            {
                Debug.LogError("[QueensMasterImporter] 标准化关卡文件为空。");
                return;
            }

            EnsureAssetFolder(OutputRoot);
            var imported = new List<WoodukuLevelFile>(payload.levels.Length);
            var rejected = new List<string>();

            for (var i = 0; i < payload.levels.Length; i++)
            {
                var source = payload.levels[i];
                if (!TryConvert(source, imported.Count + 1, out var level, out var error))
                {
                    rejected.Add($"{source?.sourceName ?? "(null)"}: {error}");
                    continue;
                }

                imported.Add(level);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<WoodukuLevelCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WoodukuLevelCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.levelsPerPack = LevelsPerPack;
            catalog.totalLevelCount = imported.Count;
            catalog.packs.Clear();

            for (var offset = 0; offset < imported.Count; offset += LevelsPerPack)
            {
                var packNumber = offset / LevelsPerPack + 1;
                var count = Mathf.Min(LevelsPerPack, imported.Count - offset);
                var firstLevelId = offset + 1;
                var packPath = $"{OutputRoot}/WoodukuLevelPack_{packNumber:D2}.asset";
                var pack = AssetDatabase.LoadAssetAtPath<WoodukuLevelPackAsset>(packPath);
                if (pack == null)
                {
                    pack = ScriptableObject.CreateInstance<WoodukuLevelPackAsset>();
                    AssetDatabase.CreateAsset(pack, packPath);
                }

                pack.firstLevelId = firstLevelId;
                pack.levels.Clear();
                for (var i = 0; i < count; i++)
                {
                    pack.levels.Add(imported[offset + i]);
                }

                EditorUtility.SetDirty(pack);
                catalog.packs.Add(new WoodukuLevelPackEntry
                {
                    firstLevelId = firstLevelId,
                    lastLevelId = firstLevelId + count - 1,
                    assetPath = packPath
                });
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var reportPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../WoodukuDoc/QueensMaster_ImportReport.txt"));
            var reportLines = new List<string>
            {
                $"Source: {payload.source}",
                $"UnityVersion: {payload.unityVersion}",
                $"NormalizedLevels: {payload.levels.Length}",
                $"ImportedLevels: {imported.Count}",
                $"Packs: {catalog.packs.Count}",
                $"RejectedLevels: {rejected.Count}"
            };
            reportLines.AddRange(rejected);
            File.WriteAllLines(reportPath, reportLines);

            Debug.Log(
                $"[QueensMasterImporter] 导入完成：{imported.Count} 关，" +
                $"分包 {catalog.packs.Count} 个，拒绝 {rejected.Count} 关。报告：{reportPath}");
        }

        private static bool TryConvert(
            SourceLevel source,
            int levelId,
            out WoodukuLevelFile level,
            out string error)
        {
            level = null;
            error = null;
            if (source == null || source.size < 2 || source.size > 12)
            {
                error = "尺寸无效";
                return false;
            }

            var cellCount = source.size * source.size;
            if (source.regions == null || source.regions.Length != cellCount)
            {
                error = "区域数组长度错误";
                return false;
            }

            if (!TryNormalizeRegions(source.size, source.regions, out var normalizedRegions, out error))
            {
                return false;
            }

            var fixedCells = new List<WoodukuCellRef>(
                FilterFixedCells(source.fixedQueenCells, source.solutionCols));
            var result = WoodukuLevelSolver.Analyze(
                source.size, normalizedRegions, source.size, fixedCells);
            if (result.BoardValid && !result.HasUniqueSolution)
            {
                if (source.solutionCols == null || source.solutionCols.Length != source.size)
                {
                    error = "源解长度错误，无法补充预置皇后";
                    return false;
                }

                for (var row = 0; row < source.size && !result.HasUniqueSolution; row++)
                {
                    if (ContainsRow(fixedCells, row))
                    {
                        continue;
                    }

                    fixedCells.Add(new WoodukuCellRef { r = row, c = source.solutionCols[row] });
                    result = WoodukuLevelSolver.Analyze(
                        source.size, normalizedRegions, source.size, fixedCells);
                }
            }

            if (!result.HasUniqueSolution)
            {
                error = result.BoardValid
                    ? $"不是唯一解，解数量={result.SolutionCount}"
                    : result.BoardError;
                return false;
            }

            level = new WoodukuLevelFile
            {
                id = levelId,
                size = source.size,
                hintCount = 5,
                difficulty = (WoodukuLevelDifficulty)Mathf.Clamp(source.difficulty, 0, 2),
                difficultyScore = source.difficultyScore,
                sourceName = source.sourceName,
                hasUniqueSolution = true,
                solutionCount = 1,
                colors = Array.Empty<WoodukuColorEntry>(),
                regions = normalizedRegions,
                solutionCols = result.FirstSolutionCols,
                solutionCells = BuildSolutionCells(result.FirstSolutionCols),
                fixedQueenCells = fixedCells.ToArray()
            };
            return true;
        }

        private static bool ContainsRow(List<WoodukuCellRef> cells, int row)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null && cells[i].r == row)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryNormalizeRegions(
            int size,
            int[] source,
            out int[] normalized,
            out string error)
        {
            normalized = new int[source.Length];
            error = null;
            var map = new Dictionary<int, int>();
            for (var i = 0; i < source.Length; i++)
            {
                var sourceId = source[i];
                if (!map.TryGetValue(sourceId, out var normalizedId))
                {
                    normalizedId = map.Count;
                    map[sourceId] = normalizedId;
                }

                normalized[i] = normalizedId;
            }

            if (map.Count != size)
            {
                error = $"区域数={map.Count}，应为 {size}";
                return false;
            }

            return true;
        }

        private static WoodukuCellRef[] BuildSolutionCells(int[] solutionCols)
        {
            var cells = new WoodukuCellRef[solutionCols.Length];
            for (var row = 0; row < solutionCols.Length; row++)
            {
                cells[row] = new WoodukuCellRef { r = row, c = solutionCols[row] };
            }

            return cells;
        }

        private static WoodukuCellRef[] FilterFixedCells(
            WoodukuCellRef[] source,
            int[] solutionCols)
        {
            if (source == null ||
                source.Length == 0 ||
                solutionCols == null ||
                solutionCols.Length == 0)
            {
                return Array.Empty<WoodukuCellRef>();
            }

            var result = new List<WoodukuCellRef>();
            foreach (var cell in source)
            {
                if (cell != null &&
                    cell.r >= 0 &&
                    cell.r < solutionCols.Length &&
                    solutionCols[cell.r] == cell.c)
                {
                    result.Add(new WoodukuCellRef { r = cell.r, c = cell.c });
                }
            }

            return result.ToArray();
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var parts = assetPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
