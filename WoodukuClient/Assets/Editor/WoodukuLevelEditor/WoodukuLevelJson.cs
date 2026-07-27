using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Wooduku.LevelEditor
{
    [Serializable]
    public class WoodukuLevelFile
    {
        public int id = 1;
        public int size = 4;
        public int hintCount = 5;
        public bool hasUniqueSolution;
        public int solutionCount;
        /// <summary>启用颜色条目（按 id 0..N-1）。</summary>
        public WoodukuColorEntry[] colors;
        /// <summary>row-major，长度 size*size。</summary>
        public int[] regions;
        /// <summary>解：每行放置的列；无解时为空。</summary>
        public int[] solutionCols;
        public WoodukuCellRef[] solutionCells;
    }

    [Serializable]
    public class WoodukuColorEntry
    {
        public int id;
        public string name;
        public string hex;
        public bool enabled = true;
    }

    [Serializable]
    public class WoodukuCellRef
    {
        public int r;
        public int c;
    }

    /// <summary>
    /// 手写 JSON，避免 JsonUtility 对数组/可读性的限制。
    /// </summary>
    public static class WoodukuLevelJson
    {
        public static string ToJson(WoodukuLevelFile file, bool pretty = true)
        {
            var sb = new StringBuilder(512);
            var indent = pretty ? 0 : -1;
            AppendObject(sb, indent, () =>
            {
                AppendField(sb, indent, "id", file.id, true);
                AppendField(sb, indent, "size", file.size, true);
                AppendField(sb, indent, "hintCount", file.hintCount, true);
                AppendField(sb, indent, "hasUniqueSolution", file.hasUniqueSolution, true);
                AppendField(sb, indent, "solutionCount", file.solutionCount, true);

                AppendKey(sb, indent, "colors");
                AppendArray(sb, indent, file.colors?.Length ?? 0, i =>
                {
                    var c = file.colors[i];
                    AppendObject(sb, indent + 1, () =>
                    {
                        AppendField(sb, indent + 1, "id", c.id, true);
                        AppendField(sb, indent + 1, "name", c.name ?? "", true);
                        AppendField(sb, indent + 1, "hex", c.hex ?? "#FFFFFF", true);
                        AppendField(sb, indent + 1, "enabled", c.enabled, false);
                    });
                });
                sb.Append(',');
                if (pretty)
                {
                    sb.Append('\n');
                }

                AppendKey(sb, indent, "regions");
                AppendIntArray(sb, indent, file.regions);
                sb.Append(',');
                if (pretty)
                {
                    sb.Append('\n');
                }

                AppendKey(sb, indent, "solutionCols");
                AppendIntArray(sb, indent, file.solutionCols);
                sb.Append(',');
                if (pretty)
                {
                    sb.Append('\n');
                }

                AppendKey(sb, indent, "solutionCells");
                AppendArray(sb, indent, file.solutionCells?.Length ?? 0, i =>
                {
                    var cell = file.solutionCells[i];
                    AppendObject(sb, indent + 1, () =>
                    {
                        AppendField(sb, indent + 1, "r", cell.r, true);
                        AppendField(sb, indent + 1, "c", cell.c, false);
                    });
                }, trailingComma: false);
            });
            return sb.ToString();
        }

        public static WoodukuLevelFile FromJson(string json)
        {
            return ParseManual(json);
        }

        public static string ColorToHex(Color color)
        {
            var r = Mathf.RoundToInt(color.r * 255f);
            var g = Mathf.RoundToInt(color.g * 255f);
            var b = Mathf.RoundToInt(color.b * 255f);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public static Color HexToColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return fallback;
            }

            hex = hex.Trim();
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length != 6)
            {
                return fallback;
            }

            if (!byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out var r) ||
                !byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out var g) ||
                !byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out var b))
            {
                return fallback;
            }

            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private static WoodukuLevelFile ParseManual(string json)
        {
            var file = new WoodukuLevelFile
            {
                id = ReadInt(json, "id", 1),
                size = ReadInt(json, "size", 4),
                hintCount = ReadInt(json, "hintCount", 5),
                hasUniqueSolution = ReadBool(json, "hasUniqueSolution", false),
                solutionCount = ReadInt(json, "solutionCount", 0),
                regions = ReadIntArray(json, "regions"),
                solutionCols = ReadIntArray(json, "solutionCols")
            };

            // colors / solutionCells：导入时用 regions + 编辑器当前色板即可；颜色块尽量恢复 hex
            file.colors = ParseColors(json);
            if (file.solutionCols != null && file.solutionCols.Length == file.size)
            {
                file.solutionCells = new WoodukuCellRef[file.size];
                for (var r = 0; r < file.size; r++)
                {
                    file.solutionCells[r] = new WoodukuCellRef { r = r, c = file.solutionCols[r] };
                }
            }

            return file;
        }

        private static WoodukuColorEntry[] ParseColors(string json)
        {
            var key = "\"colors\"";
            var idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0)
            {
                return Array.Empty<WoodukuColorEntry>();
            }

            var arrStart = json.IndexOf('[', idx);
            if (arrStart < 0)
            {
                return Array.Empty<WoodukuColorEntry>();
            }

            var depth = 0;
            var arrEnd = -1;
            for (var i = arrStart; i < json.Length; i++)
            {
                if (json[i] == '[')
                {
                    depth++;
                }
                else if (json[i] == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        arrEnd = i;
                        break;
                    }
                }
            }

            if (arrEnd < 0)
            {
                return Array.Empty<WoodukuColorEntry>();
            }

            var slice = json.Substring(arrStart, arrEnd - arrStart + 1);
            var list = new System.Collections.Generic.List<WoodukuColorEntry>();
            var pos = 0;
            while (true)
            {
                var objStart = slice.IndexOf('{', pos);
                if (objStart < 0)
                {
                    break;
                }

                var objEnd = slice.IndexOf('}', objStart);
                if (objEnd < 0)
                {
                    break;
                }

                var obj = slice.Substring(objStart, objEnd - objStart + 1);
                list.Add(new WoodukuColorEntry
                {
                    id = ReadInt(obj, "id", list.Count),
                    name = ReadString(obj, "name", $"色{list.Count}"),
                    hex = ReadString(obj, "hex", "#CCCCCC"),
                    enabled = ReadBool(obj, "enabled", true)
                });
                pos = objEnd + 1;
            }

            return list.ToArray();
        }

        private static int ReadInt(string json, string key, int fallback)
        {
            var pattern = $"\"{key}\"";
            var idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0)
            {
                return fallback;
            }

            var colon = json.IndexOf(':', idx + pattern.Length);
            if (colon < 0)
            {
                return fallback;
            }

            var i = colon + 1;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t'))
            {
                i++;
            }

            var start = i;
            if (i < json.Length && json[i] == '-')
            {
                i++;
            }

            while (i < json.Length && char.IsDigit(json[i]))
            {
                i++;
            }

            if (start == i || (i == start + 1 && json[start] == '-'))
            {
                return fallback;
            }

            return int.TryParse(json.Substring(start, i - start), out var v) ? v : fallback;
        }

        private static bool ReadBool(string json, string key, bool fallback)
        {
            var pattern = $"\"{key}\"";
            var idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0)
            {
                return fallback;
            }

            var colon = json.IndexOf(':', idx + pattern.Length);
            if (colon < 0)
            {
                return fallback;
            }

            var tail = json.Substring(colon + 1).TrimStart();
            if (tail.StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (tail.StartsWith("false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return fallback;
        }

        private static string ReadString(string json, string key, string fallback)
        {
            var pattern = $"\"{key}\"";
            var idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0)
            {
                return fallback;
            }

            var colon = json.IndexOf(':', idx + pattern.Length);
            if (colon < 0)
            {
                return fallback;
            }

            var q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0)
            {
                return fallback;
            }

            var q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0)
            {
                return fallback;
            }

            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        private static int[] ReadIntArray(string json, string key)
        {
            var pattern = $"\"{key}\"";
            var idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0)
            {
                return Array.Empty<int>();
            }

            var arrStart = json.IndexOf('[', idx);
            if (arrStart < 0)
            {
                return Array.Empty<int>();
            }

            var arrEnd = json.IndexOf(']', arrStart);
            if (arrEnd < 0)
            {
                return Array.Empty<int>();
            }

            var body = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
            if (string.IsNullOrWhiteSpace(body))
            {
                return Array.Empty<int>();
            }

            var parts = body.Split(',');
            var list = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                int.TryParse(parts[i].Trim(), out list[i]);
            }

            return list;
        }

        private static void AppendObject(StringBuilder sb, int indent, Action body)
        {
            var pretty = indent >= 0;
            sb.Append('{');
            if (pretty)
            {
                sb.Append('\n');
            }

            body();
            if (pretty)
            {
                AppendIndent(sb, indent);
            }

            sb.Append('}');
        }

        private static void AppendArray(StringBuilder sb, int indent, int count, Action<int> writeItem, bool trailingComma = true)
        {
            var pretty = indent >= 0;
            sb.Append('[');
            if (pretty && count > 0)
            {
                sb.Append('\n');
            }

            for (var i = 0; i < count; i++)
            {
                if (pretty)
                {
                    AppendIndent(sb, indent + 1);
                }

                writeItem(i);
                if (i < count - 1)
                {
                    sb.Append(',');
                }

                if (pretty)
                {
                    sb.Append('\n');
                }
            }

            if (pretty && count > 0)
            {
                AppendIndent(sb, indent);
            }

            sb.Append(']');
            if (trailingComma)
            {
                // caller handles commas between fields
            }
        }

        private static void AppendIntArray(StringBuilder sb, int indent, int[] values)
        {
            var pretty = indent >= 0;
            sb.Append('[');
            if (values != null)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                        if (pretty)
                        {
                            sb.Append(' ');
                        }
                    }

                    sb.Append(values[i]);
                }
            }

            sb.Append(']');
        }

        private static void AppendKey(StringBuilder sb, int indent, string key)
        {
            if (indent >= 0)
            {
                AppendIndent(sb, indent + 1);
            }

            sb.Append('"').Append(key).Append('"').Append(':');
            if (indent >= 0)
            {
                sb.Append(' ');
            }
        }

        private static void AppendField(StringBuilder sb, int indent, string key, int value, bool comma)
        {
            AppendKey(sb, indent, key);
            sb.Append(value);
            EndField(sb, indent, comma);
        }

        private static void AppendField(StringBuilder sb, int indent, string key, bool value, bool comma)
        {
            AppendKey(sb, indent, key);
            sb.Append(value ? "true" : "false");
            EndField(sb, indent, comma);
        }

        private static void AppendField(StringBuilder sb, int indent, string key, string value, bool comma)
        {
            AppendKey(sb, indent, key);
            sb.Append('"').Append(Escape(value)).Append('"');
            EndField(sb, indent, comma);
        }

        private static void EndField(StringBuilder sb, int indent, bool comma)
        {
            if (comma)
            {
                sb.Append(',');
            }

            if (indent >= 0)
            {
                sb.Append('\n');
            }
        }

        private static void AppendIndent(StringBuilder sb, int indent)
        {
            for (var i = 0; i < indent; i++)
            {
                sb.Append("  ");
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
