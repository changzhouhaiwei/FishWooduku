using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic.Wooduku
{
    /// <summary>
    /// 棋盘指针手势：单击 / 双击 / 滑动打或擦 X。
    /// 滑动模式由起点决定：空白→打 X；已是 X→沿路径取消。
    /// 单击延迟到双击窗口结束，避免正解双击时先闪 X。
    /// </summary>
    public sealed class WoodukuBoardInput : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public const float DoubleClickSeconds = 0.2f;
        private const float DragThresholdPx = 12f;

        private WoodukuGameSession _session;
        private Func<Vector2, bool, (int r, int c, bool ok)> _screenToCell;
        private Action<int, int> _onWrongConfirm;
        private Action<int, int> _onExcludeClicked;
        private Action _onVisualRefresh;

        private bool _pointerDown;
        private bool _dragging;
        private int _downR = -1;
        private int _downC = -1;
        private Vector2 _downPos;
        private int _lastClickR = -1;
        private int _lastClickC = -1;
        private float _lastClickTime = -999f;
        private int _lastSwipeR = -1;
        private int _lastSwipeC = -1;
        private bool _swipeClearExclude;
        private Coroutine _pendingClickCo;

        public void Bind(
            WoodukuGameSession session,
            Func<Vector2, bool, (int r, int c, bool ok)> screenToCell,
            Action<int, int> onWrongConfirm,
            Action<int, int> onExcludeClicked,
            Action onVisualRefresh)
        {
            _session = session;
            _screenToCell = screenToCell;
            _onWrongConfirm = onWrongConfirm;
            _onExcludeClicked = onExcludeClicked;
            _onVisualRefresh = onVisualRefresh;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_session == null || _session.IsCleared || _screenToCell == null)
            {
                return;
            }

            var hit = _screenToCell(eventData.position, true);
            if (!hit.ok)
            {
                return;
            }

            _pointerDown = true;
            _dragging = false;
            _downR = hit.r;
            _downC = hit.c;
            _downPos = eventData.position;
            _lastSwipeR = hit.r;
            _lastSwipeC = hit.c;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_pointerDown || _session == null || _session.IsCleared || _screenToCell == null)
            {
                return;
            }

            if (!_dragging)
            {
                if ((eventData.position - _downPos).sqrMagnitude < DragThresholdPx * DragThresholdPx)
                {
                    return;
                }

                CancelPendingClick();
                _dragging = true;
                _session.ResolveSwipeMode(_downR, _downC, out _swipeClearExclude);
                if (_session.SwipePaint(_downR, _downC, _swipeClearExclude))
                {
                    _onVisualRefresh?.Invoke();
                }
            }

            var hit = _screenToCell(eventData.position, false);
            if (!hit.ok)
            {
                return;
            }

            if (hit.r == _lastSwipeR && hit.c == _lastSwipeC)
            {
                return;
            }

            _lastSwipeR = hit.r;
            _lastSwipeC = hit.c;
            if (_session.SwipePaint(hit.r, hit.c, _swipeClearExclude))
            {
                _onVisualRefresh?.Invoke();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pointerDown || _session == null)
            {
                return;
            }

            _pointerDown = false;
            if (_dragging || _session.IsCleared)
            {
                _dragging = false;
                return;
            }

            var now = Time.unscaledTime;
            var isDouble = _downR == _lastClickR
                           && _downC == _lastClickC
                           && now - _lastClickTime <= DoubleClickSeconds;

            if (isDouble)
            {
                CancelPendingClick();
                _lastClickR = -1;
                _lastClickC = -1;
                _lastClickTime = -999f;
                if (_session.TryConfirm(_downR, _downC, out var isCorrect))
                {
                    _onVisualRefresh?.Invoke();
                    if (!isCorrect)
                    {
                        _onWrongConfirm?.Invoke(_downR, _downC);
                    }
                }

                return;
            }

            _lastClickR = _downR;
            _lastClickC = _downC;
            _lastClickTime = now;
            CancelPendingClick();
            var r = _downR;
            var c = _downC;
            _pendingClickCo = StartCoroutine(DeferredToggle(r, c));
        }

        private IEnumerator DeferredToggle(int r, int c)
        {
            yield return new WaitForSecondsRealtime(DoubleClickSeconds);
            _pendingClickCo = null;
            if (_session == null || _session.IsCleared)
            {
                yield break;
            }

            // 窗口内若已双击，lastClick 会被清空
            if (_lastClickR != r || _lastClickC != c)
            {
                yield break;
            }

            _lastClickR = -1;
            _lastClickC = -1;
            if (_session.TryToggleExclude(r, c))
            {
                _onVisualRefresh?.Invoke();
                if (_session.GetMark(r, c) == WoodukuCellMark.Exclude)
                {
                    _onExcludeClicked?.Invoke(r, c);
                }
            }
        }

        private void CancelPendingClick()
        {
            if (_pendingClickCo == null)
            {
                return;
            }

            StopCoroutine(_pendingClickCo);
            _pendingClickCo = null;
        }

        private void OnDisable()
        {
            CancelPendingClick();
            _pointerDown = false;
            _dragging = false;
        }
    }
}
