using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace FishFramework.FontTools
{
    /// <summary>
    /// 按「收集字符」方式维护 TMP 字体：从本地化 / Prefab 文案收集 Unique 字符，
    /// 写入 CharacterSet，再灌入 Dynamic TMP FontAsset（对齐参考工程 characterSequence 做法）。
    /// </summary>
    public static class TMPFontCharacterCollectTool
    {
        private const string DefaultFontPath = "Assets/GameRes/Fonts/Arial-Unicode-Bold-RSDF.asset";
        private const string CharacterSetDir = "Assets/GameRes/Fonts/CharacterSets";
        private const string CollectedCharsPath = CharacterSetDir + "/CollectedChars.txt";
        private const string I2LanguagesPath = "Assets/Resources/I2Languages.asset";

        private static readonly Regex PrefabTextRegex = new(
            @"m_text:\s*(.*)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex UnicodeEscapeRegex = new(
            @"\\u([0-9A-Fa-f]{4})",
            RegexOptions.Compiled);

        [MenuItem("FishFramework/Fonts/Collect Characters From Project", false, 100)]
        public static void CollectCharactersOnly()
        {
            var chars = CollectAllCharacters(out var report);
            WriteCharacterSet(chars);
            EditorUtility.DisplayDialog(
                "字体字符收集",
                $"已收集 {chars.Count} 个唯一字符\n写入: {CollectedCharsPath}\n\n{report}",
                "OK");
            Debug.Log($"[TMPFont] Collected {chars.Count} chars → {CollectedCharsPath}\n{report}");
        }

        [MenuItem("FishFramework/Fonts/Collect Characters And Apply To Font", false, 101)]
        public static void CollectAndApplyToFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);
            if (font == null)
            {
                EditorUtility.DisplayDialog("字体字符收集", $"找不到字体: {DefaultFontPath}", "OK");
                return;
            }

            var chars = CollectAllCharacters(out var report);
            WriteCharacterSet(chars);

            string sequence = new string(chars.OrderBy(c => c).ToArray());
            ApplyCharactersToFont(font, sequence, out var missing);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string missingInfo = string.IsNullOrEmpty(missing)
                ? "无缺失字形"
                : $"源字体缺字 {missing.Length} 个（已跳过）";

            EditorUtility.DisplayDialog(
                "字体字符收集并写入",
                $"字体: {DefaultFontPath}\n收集: {chars.Count} 字符\n{missingInfo}\n\n{report}",
                "OK");
            Debug.Log(
                $"[TMPFont] Applied {chars.Count} chars to {DefaultFontPath}. {missingInfo}\n{report}");
        }

        [MenuItem("FishFramework/Fonts/Open Collected Character Set", false, 102)]
        public static void OpenCollectedCharacterSet()
        {
            if (!File.Exists(CollectedCharsPath))
            {
                CollectCharactersOnly();
            }

            var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(CollectedCharsPath);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
            else
            {
                EditorUtility.RevealInFinder(CollectedCharsPath);
            }
        }

        private static void ApplyCharactersToFont(TMP_FontAsset font, string sequence, out string missing)
        {
            missing = string.Empty;

            // Dynamic：按需灌入图集；Multi Atlas 已在资产上打开
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                EditorUtility.SetDirty(font);
            }

            font.ReadFontAssetDefinition();

            bool ok = font.TryAddCharacters(sequence, out missing);
            if (!ok && !string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[TMPFont] TryAddCharacters partial. missing length={missing.Length}");
            }

            // 同步 CreationSettings 的 characterSequence，方便 Font Asset Creator 再次「从收集文本」生成
            var settings = font.creationSettings;
            settings.characterSetSelectionMode = 4; // Custom Characters
            settings.characterSequence = sequence.Length > 200000
                ? sequence.Substring(0, 200000)
                : sequence;
            settings.sourceFontFileGUID = AssetDatabase.AssetPathToGUID("Assets/Font/Arial-Unicode-Bold.ttf");
            font.creationSettings = settings;

            EditorUtility.SetDirty(font);
        }

        private static SortedSet<char> CollectAllCharacters(out string report)
        {
            var set = new SortedSet<char>();
            var sb = new StringBuilder();

            // 基础 ASCII 可打印 + 常用空白
            for (int i = 32; i <= 126; i++)
            {
                set.Add((char)i);
            }

            set.Add('\n');
            set.Add('\r');
            set.Add('\t');

            int before = set.Count;
            AddFromFileText(I2LanguagesPath, set);
            sb.AppendLine($"I2Languages: +{set.Count - before}");

            before = set.Count;
            AddFromPrefabs("Assets/GameRes", set);
            sb.AppendLine($"GameRes Prefabs m_text: +{set.Count - before}");

            before = set.Count;
            if (Directory.Exists(CharacterSetDir))
            {
                foreach (string path in Directory.GetFiles(CharacterSetDir, "*.txt", SearchOption.TopDirectoryOnly))
                {
                    if (path.Replace('\\', '/').EndsWith("CollectedChars.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AddFromFileText(path.Replace('\\', '/'), set);
                }
            }

            sb.AppendLine($"Extra CharacterSets/*.txt: +{set.Count - before}");
            report = sb.ToString().TrimEnd();
            return set;
        }

        private static void AddFromPrefabs(string rootFolder, ISet<char> set)
        {
            if (!Directory.Exists(rootFolder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { rootFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string text;
                try
                {
                    text = File.ReadAllText(path, Encoding.UTF8);
                }
                catch
                {
                    continue;
                }

                foreach (Match match in PrefabTextRegex.Matches(text))
                {
                    string raw = match.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(raw) || raw == "''" || raw == "\"\"")
                    {
                        continue;
                    }

                    // YAML 可能是 "xxx" 或直接字面量
                    if ((raw.StartsWith("\"") && raw.EndsWith("\"")) ||
                        (raw.StartsWith("'") && raw.EndsWith("'")))
                    {
                        raw = raw.Substring(1, raw.Length - 2);
                    }

                    AddDecodedText(raw, set);
                }
            }
        }

        private static void AddFromFileText(string assetPath, ISet<char> set)
        {
            string full = Path.GetFullPath(assetPath);
            if (!File.Exists(full))
            {
                return;
            }

            string text = File.ReadAllText(full, Encoding.UTF8);
            AddDecodedText(text, set);
        }

        private static void AddDecodedText(string text, ISet<char> set)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // 展开 \uXXXX
            string decoded = UnicodeEscapeRegex.Replace(text, m =>
            {
                int code = Convert.ToInt32(m.Groups[1].Value, 16);
                return char.ConvertFromUtf32(code);
            });

            foreach (char c in decoded)
            {
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                {
                    continue;
                }

                set.Add(c);
            }
        }

        private static void WriteCharacterSet(IEnumerable<char> chars)
        {
            if (!Directory.Exists(CharacterSetDir))
            {
                Directory.CreateDirectory(CharacterSetDir);
                AssetDatabase.Refresh();
            }

            string content = new string(chars.OrderBy(c => c).ToArray());
            File.WriteAllText(CollectedCharsPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.ImportAsset(CollectedCharsPath);
        }
    }
}
