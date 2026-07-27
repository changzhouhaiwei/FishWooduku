using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Wooduku.LevelEditor
{
    /// <summary>
    /// Wooduku 关卡编辑器：设定 N、色板存档、格子涂色、唯一解检测、导出 JSON。
    /// 菜单：自定义窗口 / Wooduku 关卡编辑器
    /// </summary>
    public sealed class WoodukuLevelEditorWindow : EditorWindow
    {
        private const string DefaultLevelDir = "Assets/GameRes/WoodukuLevels";
        private const string DefaultPalettePath = "Assets/GameRes/WoodukuLevels/WoodukuColorPalette.asset";

        private int _levelId = 1;
        private int _size = 4;
        private int _hintCount = 5;
        private int[] _regions;
        private int _paintColorIndex;
        private WoodukuColorPaletteAsset _palette;
        private Vector2 _scroll;
        private Vector2 _boardScroll;
        private string _status = "就绪。标定色区后点击「检测固有解」。";
        private MessageType _statusType = MessageType.Info;
        private WoodukuLevelSolver.Result _lastResult;
        private bool _showSolutionOnBoard = true;
        private float _cellPx = 36f;
        private bool _isDraggingPaint;
        private string _exportPath;

        [MenuItem("自定义窗口/Wooduku 关卡编辑器", priority = 80)]
        private static void Open()
        {
            var win = GetWindow<WoodukuLevelEditorWindow>();
            win.titleContent = new GUIContent("Wooduku 关卡");
            win.minSize = new Vector2(720, 560);
            win.Show();
        }

        private void OnEnable()
        {
            EnsurePalette();
            EnsureBoard(_size);
            _exportPath = Path.Combine(DefaultLevelDir, $"level_{_levelId:D3}.json").Replace('\\', '/');
        }

        private void OnGUI()
        {
            EnsurePalette();
            EnsureBoard(_size);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            EditorGUILayout.Space(8);
            DrawPaletteSection();
            EditorGUILayout.Space(8);
            DrawBoardSection();
            EditorGUILayout.Space(8);
            DrawValidateSection();
            EditorGUILayout.Space(8);
            DrawExportSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("关卡基本信息", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _levelId = EditorGUILayout.IntField("关卡 ID", _levelId);
                _hintCount = EditorGUILayout.IntField("提示次数", Mathf.Max(0, _hintCount));
            }

            EditorGUI.BeginChangeCheck();
            var newSize = EditorGUILayout.IntSlider("边长 N（N×N）", _size, 2, 12);
            if (EditorGUI.EndChangeCheck() && newSize != _size)
            {
                ResizeBoard(newSize);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("清空棋盘", GUILayout.Width(100)))
                {
                    for (var i = 0; i < _regions.Length; i++)
                    {
                        _regions[i] = -1;
                    }

                    _lastResult = null;
                    SetStatus("已清空棋盘。", MessageType.Info);
                }

                if (GUILayout.Button("按行填充 0..N-1（调试）", GUILayout.Width(160)))
                {
                    FillDebugRows();
                }
            }
        }

        private void DrawPaletteSection()
        {
            EditorGUILayout.LabelField("颜色区（存档 / 开启）", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _palette = (WoodukuColorPaletteAsset)EditorGUILayout.ObjectField(
                "色板资源", _palette, typeof(WoodukuColorPaletteAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                EnsurePalette();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新建色板资源", GUILayout.Width(120)))
                {
                    CreatePaletteAsset();
                }

                if (GUILayout.Button("保存色板", GUILayout.Width(100)))
                {
                    EditorUtility.SetDirty(_palette);
                    AssetDatabase.SaveAssets();
                    SetStatus($"色板已保存：{AssetDatabase.GetAssetPath(_palette)}", MessageType.Info);
                }

                if (GUILayout.Button("补齐默认色到 N", GUILayout.Width(130)))
                {
                    _palette.EnsureDefaults(_size);
                    EditorUtility.SetDirty(_palette);
                }

                if (GUILayout.Button("添加颜色", GUILayout.Width(80)))
                {
                    var i = _palette.slots.Count;
                    _palette.slots.Add(new WoodukuColorPaletteAsset.Slot
                    {
                        name = $"色{i}",
                        color = WoodukuColorPaletteAsset.DefaultColors[i % WoodukuColorPaletteAsset.DefaultColors.Length],
                        enabled = true
                    });
                    EditorUtility.SetDirty(_palette);
                }
            }

            var enabled = _palette.GetEnabledIndices();
            EditorGUILayout.HelpBox(
                $"已开启 {enabled.Count} 种颜色；检测固有解时须恰好等于 N={_size}。\n点击下方色块设为画笔，再在棋盘上单击/拖拽标定颜色区。",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < _palette.slots.Count; i++)
                {
                    var slot = _palette.slots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(72)))
                    {
                        var wasEnabled = slot.enabled;
                        slot.enabled = EditorGUILayout.ToggleLeft("开", slot.enabled, GUILayout.Width(70));
                        if (wasEnabled != slot.enabled)
                        {
                            EditorUtility.SetDirty(_palette);
                        }

                        var style = new GUIStyle(GUI.skin.button);
                        if (i == _paintColorIndex)
                        {
                            style.fontStyle = FontStyle.Bold;
                        }

                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = slot.enabled ? slot.color : Color.Lerp(slot.color, Color.gray, 0.6f);
                        if (GUILayout.Button($"{i}", style, GUILayout.Height(28), GUILayout.Width(70)))
                        {
                            _paintColorIndex = i;
                        }

                        GUI.backgroundColor = prev;

                        slot.name = EditorGUILayout.TextField(slot.name, GUILayout.Width(70));
                        slot.color = EditorGUILayout.ColorField(GUIContent.none, slot.color, false, false, false,
                            GUILayout.Width(70), GUILayout.Height(18));
                    }
                }
            }

            // 画笔映射：palette 槽位 index → 导出时的逻辑色 id（仅 enabled 按顺序重映射 0..N-1）
            EditorGUILayout.LabelField($"当前画笔：槽位 {_paintColorIndex}" +
                                       (_paintColorIndex >= 0 && _paintColorIndex < _palette.slots.Count
                                           ? $"（{_palette.slots[_paintColorIndex].name}）"
                                           : ""));
        }

        private void DrawBoardSection()
        {
            EditorGUILayout.LabelField("棋盘标定", EditorStyles.boldLabel);
            _cellPx = EditorGUILayout.Slider("格子大小", _cellPx, 22f, 56f);
            _showSolutionOnBoard = EditorGUILayout.Toggle("显示检测到的解（猫）", _showSolutionOnBoard);

            var boardW = _size * (_cellPx + 2f) + 8f;
            var boardH = _size * (_cellPx + 2f) + 8f;
            _boardScroll = EditorGUILayout.BeginScrollView(_boardScroll, GUILayout.Height(Mathf.Min(boardH + 20f, 420f)));
            var rect = GUILayoutUtility.GetRect(boardW, boardH);

            HandleBoardInput(rect);
            DrawBoard(rect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField("左键涂色 / 拖拽连涂；右键清除格子为未定义。", EditorStyles.miniLabel);
        }

        private void DrawBoard(Rect origin)
        {
            EditorGUI.DrawRect(origin, new Color(0.15f, 0.15f, 0.15f, 0.35f));

            int[] solCols = null;
            if (_showSolutionOnBoard && _lastResult != null && _lastResult.SolutionCount >= 1)
            {
                solCols = _lastResult.FirstSolutionCols;
            }

            for (var r = 0; r < _size; r++)
            {
                for (var c = 0; c < _size; c++)
                {
                    var cell = CellRect(origin, r, c);
                    var region = _regions[r * _size + c];
                    var fill = new Color(0.25f, 0.25f, 0.25f, 0.9f);
                    var label = "";
                    if (region >= 0 && region < _palette.slots.Count && _palette.slots[region] != null)
                    {
                        fill = _palette.slots[region].color;
                        label = region.ToString();
                    }
                    else if (region >= 0)
                    {
                        fill = Color.magenta;
                        label = "?";
                    }

                    EditorGUI.DrawRect(cell, fill);
                    EditorGUI.DrawRect(new Rect(cell.x, cell.y, cell.width, 1f), Color.black);
                    EditorGUI.DrawRect(new Rect(cell.x, cell.yMax - 1f, cell.width, 1f), Color.black);
                    EditorGUI.DrawRect(new Rect(cell.x, cell.y, 1f, cell.height), Color.black);
                    EditorGUI.DrawRect(new Rect(cell.xMax - 1f, cell.y, 1f, cell.height), Color.black);

                    var isSol = solCols != null && solCols[r] == c;
                    var text = isSol ? "猫" : label;
                    if (!string.IsNullOrEmpty(text))
                    {
                        var style = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = isSol ? Color.white : Color.black }
                        };
                        GUI.Label(cell, text, style);
                    }
                }
            }
        }

        private void HandleBoardInput(Rect origin)
        {
            var e = Event.current;
            if (e == null || !origin.Contains(e.mousePosition))
            {
                if (e != null && e.type == EventType.MouseUp)
                {
                    _isDraggingPaint = false;
                }

                return;
            }

            if (!TryPickCell(origin, e.mousePosition, out var r, out var c))
            {
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                PaintCell(r, c, _paintColorIndex);
                _isDraggingPaint = true;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && _isDraggingPaint)
            {
                PaintCell(r, c, _paintColorIndex);
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                PaintCell(r, c, -1);
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp)
            {
                _isDraggingPaint = false;
            }
        }

        private Rect CellRect(Rect origin, int r, int c)
        {
            return new Rect(
                origin.x + 4f + c * (_cellPx + 2f),
                origin.y + 4f + r * (_cellPx + 2f),
                _cellPx,
                _cellPx);
        }

        private bool TryPickCell(Rect origin, Vector2 mouse, out int r, out int c)
        {
            r = -1;
            c = -1;
            var local = mouse - new Vector2(origin.x + 4f, origin.y + 4f);
            if (local.x < 0 || local.y < 0)
            {
                return false;
            }

            c = Mathf.FloorToInt(local.x / (_cellPx + 2f));
            r = Mathf.FloorToInt(local.y / (_cellPx + 2f));
            return r >= 0 && r < _size && c >= 0 && c < _size;
        }

        private void PaintCell(int r, int c, int paletteSlot)
        {
            if (paletteSlot >= 0)
            {
                if (paletteSlot >= _palette.slots.Count || !_palette.slots[paletteSlot].enabled)
                {
                    SetStatus("当前画笔颜色未开启，请先勾选「开」。", MessageType.Warning);
                    return;
                }
            }

            _regions[r * _size + c] = paletteSlot;
            _lastResult = null;
        }

        private void DrawValidateSection()
        {
            EditorGUILayout.LabelField("检测固有解", EditorStyles.boldLabel);
            if (GUILayout.Button("检测是否固有解（唯一解）", GUILayout.Height(28)))
            {
                RunValidate();
            }

            EditorGUILayout.HelpBox(_status, _statusType);

            if (_lastResult != null && _lastResult.BoardValid)
            {
                EditorGUILayout.LabelField($"解数量：{_lastResult.SolutionCount}");
                if (_lastResult.SolutionCount >= 1)
                {
                    EditorGUILayout.LabelField(
                        "解（行,列）：" + WoodukuLevelSolver.FormatSolution(_size, _lastResult.FirstSolutionCols));
                }
            }
        }

        private void DrawExportSection()
        {
            EditorGUILayout.LabelField("导出 / 导入 JSON", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _exportPath = EditorGUILayout.TextField("路径", _exportPath);
                if (GUILayout.Button("…", GUILayout.Width(28)))
                {
                    EnsureDir(DefaultLevelDir);
                    var abs = EditorUtility.SaveFilePanel(
                        "导出关卡 JSON",
                        Path.GetFullPath(DefaultLevelDir),
                        $"level_{_levelId:D3}.json",
                        "json");
                    if (!string.IsNullOrEmpty(abs))
                    {
                        _exportPath = AbsoluteToAssetPath(abs);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _lastResult != null && _lastResult.HasUniqueSolution;
                if (GUILayout.Button("导出 JSON（须唯一解）", GUILayout.Height(28)))
                {
                    ExportJson(requireUnique: true);
                }

                GUI.enabled = true;
                if (GUILayout.Button("导出 JSON（允许非唯一）", GUILayout.Height(28)))
                {
                    if (_lastResult == null)
                    {
                        RunValidate();
                    }

                    ExportJson(requireUnique: false);
                }

                if (GUILayout.Button("导入 JSON", GUILayout.Height(28)))
                {
                    ImportJson();
                }
            }
        }

        private void RunValidate()
        {
            var map = BuildExportColorMap(out var enabledSlots, out var err);
            if (map == null)
            {
                _lastResult = null;
                SetStatus(err, MessageType.Error);
                return;
            }

            var logical = new int[_regions.Length];
            for (var i = 0; i < _regions.Length; i++)
            {
                var slot = _regions[i];
                if (slot < 0)
                {
                    logical[i] = -1;
                    continue;
                }

                if (!map.TryGetValue(slot, out var lid))
                {
                    SetStatus($"格子使用了未开启颜色槽位 {slot}。", MessageType.Error);
                    _lastResult = null;
                    return;
                }

                logical[i] = lid;
            }

            _lastResult = WoodukuLevelSolver.Analyze(_size, logical, enabledSlots.Count);
            if (!_lastResult.BoardValid)
            {
                SetStatus("棋盘无效：" + _lastResult.BoardError, MessageType.Error);
                return;
            }

            if (_lastResult.HasUniqueSolution)
            {
                SetStatus(
                    $"固有解：唯一解。{WoodukuLevelSolver.FormatSolution(_size, _lastResult.FirstSolutionCols)}",
                    MessageType.Info);
            }
            else if (_lastResult.SolutionCount == 0)
            {
                SetStatus("无解：不存在满足规则的解方块集合。", MessageType.Warning);
            }
            else
            {
                SetStatus($"非固有解：找到至少 {_lastResult.SolutionCount} 组解（已停止继续搜索）。", MessageType.Warning);
            }

            Repaint();
        }

        /// <summary>
        /// 开启的色板槽位按槽位序号排序，映射为逻辑色 id 0..K-1。
        /// 棋盘上只允许使用这些槽位；K 须等于 N。
        /// </summary>
        private Dictionary<int, int> BuildExportColorMap(out List<int> enabledSlots, out string error)
        {
            error = null;
            enabledSlots = _palette.GetEnabledIndices();
            enabledSlots.Sort();
            if (enabledSlots.Count != _size)
            {
                error = $"已开启颜色数={enabledSlots.Count}，须等于 N={_size}。请在色板中开启/关闭颜色。";
                return null;
            }

            var map = new Dictionary<int, int>();
            for (var i = 0; i < enabledSlots.Count; i++)
            {
                map[enabledSlots[i]] = i;
            }

            return map;
        }

        private void ExportJson(bool requireUnique)
        {
            if (_lastResult == null || !_lastResult.BoardValid)
            {
                RunValidate();
            }

            if (_lastResult == null || !_lastResult.BoardValid)
            {
                SetStatus("无法导出：棋盘未通过校验。", MessageType.Error);
                return;
            }

            if (requireUnique && !_lastResult.HasUniqueSolution)
            {
                SetStatus("无法导出：不是固有解（唯一解）。", MessageType.Error);
                return;
            }

            var map = BuildExportColorMap(out var enabledSlots, out var err);
            if (map == null)
            {
                SetStatus(err, MessageType.Error);
                return;
            }

            var logical = new int[_regions.Length];
            for (var i = 0; i < _regions.Length; i++)
            {
                logical[i] = map[_regions[i]];
            }

            var colors = new WoodukuColorEntry[enabledSlots.Count];
            for (var i = 0; i < enabledSlots.Count; i++)
            {
                var slot = _palette.slots[enabledSlots[i]];
                colors[i] = new WoodukuColorEntry
                {
                    id = i,
                    name = slot.name,
                    hex = WoodukuLevelJson.ColorToHex(slot.color),
                    enabled = true
                };
            }

            WoodukuCellRef[] cells = null;
            if (_lastResult.FirstSolutionCols != null)
            {
                cells = new WoodukuCellRef[_size];
                for (var r = 0; r < _size; r++)
                {
                    cells[r] = new WoodukuCellRef { r = r, c = _lastResult.FirstSolutionCols[r] };
                }
            }

            var file = new WoodukuLevelFile
            {
                id = _levelId,
                size = _size,
                hintCount = _hintCount,
                hasUniqueSolution = _lastResult.HasUniqueSolution,
                solutionCount = _lastResult.SolutionCount,
                colors = colors,
                regions = logical,
                solutionCols = _lastResult.FirstSolutionCols,
                solutionCells = cells
            };

            var path = string.IsNullOrEmpty(_exportPath)
                ? Path.Combine(DefaultLevelDir, $"level_{_levelId:D3}.json").Replace('\\', '/')
                : _exportPath.Replace('\\', '/');

            if (!path.StartsWith("Assets/"))
            {
                // 绝对路径
                var abs = path;
                var dir = Path.GetDirectoryName(abs);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(abs, WoodukuLevelJson.ToJson(file));
                SetStatus($"已导出：{abs}", MessageType.Info);
                return;
            }

            EnsureDir(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var full = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? DefaultLevelDir);
            File.WriteAllText(full, WoodukuLevelJson.ToJson(file));
            AssetDatabase.Refresh();
            _exportPath = path;
            SetStatus($"已导出：{path}", MessageType.Info);
        }

        private void ImportJson()
        {
            EnsureDir(DefaultLevelDir);
            var abs = EditorUtility.OpenFilePanel("导入关卡 JSON", Path.GetFullPath(DefaultLevelDir), "json");
            if (string.IsNullOrEmpty(abs))
            {
                return;
            }

            var json = File.ReadAllText(abs);
            var file = WoodukuLevelJson.FromJson(json);
            if (file.size < 2 || file.regions == null || file.regions.Length != file.size * file.size)
            {
                SetStatus("导入失败：JSON 格式无效或 regions 长度不匹配。", MessageType.Error);
                return;
            }

            _levelId = file.id;
            _hintCount = file.hintCount;
            ResizeBoard(file.size);

            // 将逻辑色写回色板前 N 个开启槽
            _palette.EnsureDefaults(_size);
            for (var i = 0; i < _palette.slots.Count; i++)
            {
                _palette.slots[i].enabled = i < _size;
            }

            if (file.colors != null)
            {
                for (var i = 0; i < file.colors.Length && i < _palette.slots.Count; i++)
                {
                    var c = file.colors[i];
                    _palette.slots[i].name = c.name;
                    _palette.slots[i].color = WoodukuLevelJson.HexToColor(c.hex, _palette.slots[i].color);
                    _palette.slots[i].enabled = true;
                }
            }

            EditorUtility.SetDirty(_palette);

            // regions 已是逻辑 0..N-1，直接对应槽位 0..N-1
            for (var i = 0; i < file.regions.Length; i++)
            {
                _regions[i] = file.regions[i];
            }

            _exportPath = AbsoluteToAssetPath(abs);
            _lastResult = null;
            RunValidate();
            SetStatus($"已导入：{abs}", _statusType);
        }

        private void EnsurePalette()
        {
            if (_palette != null)
            {
                _palette.EnsureDefaults(Mathf.Max(_size, 4));
                return;
            }

            _palette = AssetDatabase.LoadAssetAtPath<WoodukuColorPaletteAsset>(DefaultPalettePath);
            if (_palette == null)
            {
                CreatePaletteAsset();
            }
            else
            {
                _palette.EnsureDefaults(Mathf.Max(_size, 4));
            }
        }

        private void CreatePaletteAsset()
        {
            EnsureDir(DefaultLevelDir);
            var asset = CreateInstance<WoodukuColorPaletteAsset>();
            asset.EnsureDefaults(8);
            AssetDatabase.CreateAsset(asset, DefaultPalettePath);
            AssetDatabase.SaveAssets();
            _palette = asset;
            SetStatus($"已创建色板：{DefaultPalettePath}", MessageType.Info);
        }

        private void EnsureBoard(int size)
        {
            if (_regions != null && _regions.Length == size * size)
            {
                return;
            }

            ResizeBoard(size);
        }

        private void ResizeBoard(int newSize)
        {
            var old = _regions;
            var oldSize = old == null ? 0 : (int)Mathf.Sqrt(old.Length);
            _size = newSize;
            _regions = new int[newSize * newSize];
            for (var i = 0; i < _regions.Length; i++)
            {
                _regions[i] = -1;
            }

            if (old != null && oldSize > 0)
            {
                var copy = Mathf.Min(oldSize, newSize);
                for (var r = 0; r < copy; r++)
                {
                    for (var c = 0; c < copy; c++)
                    {
                        _regions[r * newSize + c] = old[r * oldSize + c];
                    }
                }
            }

            _lastResult = null;
            _exportPath = Path.Combine(DefaultLevelDir, $"level_{_levelId:D3}.json").Replace('\\', '/');
        }

        private void FillDebugRows()
        {
            var enabled = _palette.GetEnabledIndices();
            enabled.Sort();
            if (enabled.Count < _size)
            {
                _palette.EnsureDefaults(_size);
                for (var i = 0; i < _size; i++)
                {
                    _palette.slots[i].enabled = true;
                }

                enabled = _palette.GetEnabledIndices();
                enabled.Sort();
            }

            for (var r = 0; r < _size; r++)
            {
                for (var c = 0; c < _size; c++)
                {
                    _regions[r * _size + c] = enabled[r % _size];
                }
            }

            _lastResult = null;
            SetStatus("已按行填充调试色区（通常无固有解，仅便于看色）。", MessageType.Info);
        }

        private void SetStatus(string msg, MessageType type)
        {
            _status = msg;
            _statusType = type;
        }

        private static void EnsureDir(string assetDir)
        {
            if (string.IsNullOrEmpty(assetDir))
            {
                return;
            }

            assetDir = assetDir.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(assetDir))
            {
                return;
            }

            var parts = assetDir.Split('/');
            var cur = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }

                cur = next;
            }
        }

        private static string AbsoluteToAssetPath(string absolute)
        {
            var data = Application.dataPath.Replace('\\', '/');
            var abs = absolute.Replace('\\', '/');
            if (abs.StartsWith(data))
            {
                return "Assets" + abs.Substring(data.Length);
            }

            return abs;
        }
    }
}
