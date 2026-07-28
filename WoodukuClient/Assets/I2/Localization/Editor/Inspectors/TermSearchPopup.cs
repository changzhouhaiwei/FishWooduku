using System;
using UnityEditor;
using UnityEngine;

namespace I2.Loc
{
    internal sealed class TermSearchPopup : PopupWindowContent
    {
        private readonly string[] _options;
        private readonly Action<int> _onSelected;
        private readonly int _selectedIndex;

        private string _search = string.Empty;
        private Vector2 _scroll;
        private bool _focusSearch;
        private bool _isDraggingList;
        private bool _dragPending;
        private float _lastDragY;
        private float _dragDistance;
        private bool _isResizing;
        private ResizeMode _resizeMode = ResizeMode.None;
        private Vector2 _resizeStartMouse;
        private Vector2 _resizeStartSize;
        private Vector2 _windowSize;

        private static readonly Vector2 DefaultSize = new Vector2(560f, 1080f);
        private static readonly Vector2 MinSize = new Vector2(260f, 220f);
        private static readonly Vector2 MaxSize = new Vector2(1400f, 2400f);
        private const float ResizeBorder = 6f;

        private enum ResizeMode
        {
            None,
            Right,
            Bottom,
            BottomRight
        }

        public static void Show(Rect activatorRect, string[] options, int selectedIndex, Action<int> onSelected)
        {
            if (options == null || options.Length == 0)
            {
                return;
            }

            PopupWindow.Show(activatorRect, new TermSearchPopup(options, selectedIndex, onSelected));
        }

        private TermSearchPopup(string[] options, int selectedIndex, Action<int> onSelected)
        {
            _options = options;
            _selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Length - 1);
            _onSelected = onSelected;
            _focusSearch = true;
            _windowSize = GetDefaultWindowSize();
        }

        public override Vector2 GetWindowSize()
        {
            return _windowSize;
        }

        private static Vector2 GetDefaultWindowSize()
        {
            float screenHeight = Mathf.Max(DefaultSize.y, Screen.currentResolution.height);
            return new Vector2(DefaultSize.x, screenHeight);
        }

        public override void OnGUI(Rect rect)
        {
            HandleResize(rect);
            DrawSearchBar();
            DrawList();
        }

        private void DrawSearchBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("I2TermSearchField");
            _search = GUILayout.TextField(_search ?? string.Empty, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField);
            if (GUILayout.Button(string.Empty, string.IsNullOrEmpty(_search) ? GUI.skin.FindStyle("ToolbarSeachCancelButtonEmpty") ?? EditorStyles.toolbarButton : GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? EditorStyles.toolbarButton))
            {
                _search = string.Empty;
                GUI.FocusControl("I2TermSearchField");
            }
            GUILayout.EndHorizontal();

            if (_focusSearch && Event.current.type == EventType.Repaint)
            {
                _focusSearch = false;
                EditorGUI.FocusTextInControl("I2TermSearchField");
            }
        }

        private void DrawList()
        {
            string keyword = (_search ?? string.Empty).Trim();
            bool hasKeyword = keyword.Length > 0;
            var filtered = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _options.Length; i++)
            {
                string option = _options[i] ?? string.Empty;
                if (!hasKeyword || option.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    filtered.Add(i);
                }
            }

            Rect viewRect = GUILayoutUtility.GetRect(10f, 10000f, 10f, 10000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            HandleListDrag(viewRect, filtered.Count);

            float rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            Rect contentRect = new Rect(0f, 0f, Mathf.Max(10f, viewRect.width - 16f), Mathf.Max(viewRect.height, filtered.Count * rowHeight + 4f));
            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

            for (int row = 0; row < filtered.Count; row++)
            {
                int optionIndex = filtered[row];
                string option = _options[optionIndex] ?? string.Empty;
                Rect rowRect = new Rect(4f, row * rowHeight + 2f, contentRect.width - 8f, rowHeight - 1f);

                GUIStyle style = optionIndex == _selectedIndex ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUI.Button(rowRect, option, style))
                {
                    if (_dragDistance > 4f)
                    {
                        continue;
                    }
                    _onSelected?.Invoke(optionIndex);
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }
            }

            GUI.EndScrollView();
        }

        private void HandleListDrag(Rect viewRect, int itemCount)
        {
            Event evt = Event.current;
            float rowHeight = EditorGUIUtility.singleLineHeight + 4f;
            float contentHeight = Mathf.Max(viewRect.height, itemCount * rowHeight + 4f);
            float maxScroll = Mathf.Max(0f, contentHeight - viewRect.height);

            if (evt.type == EventType.MouseDown && evt.button == 0 && viewRect.Contains(evt.mousePosition))
            {
                _dragPending = true;
                _isDraggingList = false;
                _lastDragY = evt.mousePosition.y;
                _dragDistance = 0f;
                return;
            }

            if (evt.type == EventType.MouseDrag && (_dragPending || _isDraggingList))
            {
                float delta = evt.mousePosition.y - _lastDragY;
                _dragDistance += Mathf.Abs(delta);

                if (!_isDraggingList && _dragDistance > 4f)
                {
                    _isDraggingList = true;
                }

                if (_isDraggingList)
                {
                    _scroll.y = Mathf.Clamp(_scroll.y - delta, 0f, maxScroll);
                    evt.Use();
                }

                _lastDragY = evt.mousePosition.y;
                return;
            }

            if (evt.type == EventType.MouseUp || evt.type == EventType.Ignore)
            {
                bool consumed = _isDraggingList;
                _isDraggingList = false;
                _dragPending = false;
                EditorApplication.delayCall += () =>
                {
                    _dragDistance = 0f;
                };
                if (consumed)
                {
                    evt.Use();
                }
            }
        }

        private void HandleResize(Rect windowRect)
        {
            Rect rightEdge = new Rect(windowRect.width - ResizeBorder, 0f, ResizeBorder, windowRect.height);
            Rect bottomEdge = new Rect(0f, windowRect.height - ResizeBorder, windowRect.width, ResizeBorder);
            Rect corner = new Rect(windowRect.width - ResizeBorder * 2f, windowRect.height - ResizeBorder * 2f, ResizeBorder * 2f, ResizeBorder * 2f);

            EditorGUIUtility.AddCursorRect(rightEdge, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(bottomEdge, MouseCursor.ResizeVertical);
            EditorGUIUtility.AddCursorRect(corner, MouseCursor.ResizeUpLeft);

            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (corner.Contains(evt.mousePosition))
                {
                    BeginResize(ResizeMode.BottomRight, evt.mousePosition);
                    evt.Use();
                    return;
                }
                if (rightEdge.Contains(evt.mousePosition))
                {
                    BeginResize(ResizeMode.Right, evt.mousePosition);
                    evt.Use();
                    return;
                }
                if (bottomEdge.Contains(evt.mousePosition))
                {
                    BeginResize(ResizeMode.Bottom, evt.mousePosition);
                    evt.Use();
                    return;
                }
            }

            if (evt.type == EventType.MouseDrag && _isResizing && editorWindow != null)
            {
                Vector2 delta = evt.mousePosition - _resizeStartMouse;
                float width = _resizeStartSize.x;
                float height = _resizeStartSize.y;

                if (_resizeMode == ResizeMode.Right || _resizeMode == ResizeMode.BottomRight)
                {
                    width += delta.x;
                }
                if (_resizeMode == ResizeMode.Bottom || _resizeMode == ResizeMode.BottomRight)
                {
                    height += delta.y;
                }

                width = Mathf.Clamp(width, MinSize.x, MaxSize.x);
                height = Mathf.Clamp(height, MinSize.y, MaxSize.y);

                Rect pos = editorWindow.position;
                pos.size = new Vector2(width, height);
                editorWindow.position = pos;
                _windowSize = pos.size;
                evt.Use();
                return;
            }

            if ((evt.type == EventType.MouseUp || evt.type == EventType.Ignore) && _isResizing)
            {
                _isResizing = false;
                _resizeMode = ResizeMode.None;
                evt.Use();
            }
        }

        private void BeginResize(ResizeMode mode, Vector2 mousePos)
        {
            if (editorWindow == null)
            {
                return;
            }
            _isResizing = true;
            _resizeMode = mode;
            _resizeStartMouse = mousePos;
            _resizeStartSize = editorWindow.position.size;
        }
    }
}
