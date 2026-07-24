using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class UIMainMenuTabPager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const int ShopIndex = 0;
        private const int MainIndex = 1;
        private const int TripIndex = 2;

        [SerializeField, Range(0.1f, 0.5f)] private float snapDistanceRatio = 0.25f;
        [SerializeField] private float snapVelocity = 900f;
        [SerializeField] private float snapDuration = 0.3f;

        private RectTransform _root;
        private RectTransform _scrollContent;
        private RectTransform _shopNode;
        private RectTransform _mainNode;
        private RectTransform _tripNode;
        private RectTransform _tabBar;
        private RectTransform _middleHighlight;
        private Button _buttonL;
        private Button _buttonM;
        private Button _buttonR;
        private GameObject _labelL;
        private GameObject _labelM;
        private GameObject _labelR;
        private RectTransform _labelRectL;
        private RectTransform _labelRectM;
        private RectTransform _labelRectR;

        private readonly Vector2[] _highlightPositions = new Vector2[3];
        private readonly Vector2[] _highlightSizes = new Vector2[3];
        private readonly Vector2[] _tabIconRestPositions = new Vector2[3];

        private Coroutine _snapCoroutine;
        private Coroutine _highlightCoroutine;
        private Coroutine _tabIconCoroutine;
        private float _pageWidth;
        private float _selectedIconY;
        private float _mainIconRestY;
        private float _selectedLabelY;
        private float _dragStartContentX;
        private float _dragStartTime;
        private Vector2 _dragStartLocalPoint;
        private bool _initialized;
        private bool _layouting;
        private bool _dragging;

        public int CurrentIndex { get; private set; } = MainIndex;
        public event Action<int> PageChanged;

        public bool Initialize()
        {
            if (_initialized)
            {
                return true;
            }

            _root = transform as RectTransform;
            _scrollContent = FindRectTransform("scrollContent");
            _shopNode = FindRectTransform("shopNode");
            _mainNode = FindRectTransform("mainNode");
            _tripNode = FindRectTransform("tripNode");
            _tabBar = FindRectTransform("tabBar");
            _middleHighlight = FindRectTransform("Middle");
            _buttonL = FindComponent<Button>("ButtonL");
            _buttonM = FindComponent<Button>("ButtonM");
            _buttonR = FindComponent<Button>("ButtonR");

            if (_root == null || _scrollContent == null || _shopNode == null || _mainNode == null
                || _tripNode == null || _tabBar == null || _buttonL == null || _buttonM == null
                || _buttonR == null)
            {
                Debug.LogError("[UIMainMenuTabPager] Missing scrollContent, page node, tabBar, or tab button.");
                enabled = false;
                return false;
            }

            CacheHighlightTargets();
            CacheTabLabels();
            CacheTabIconTargets();

            Canvas.ForceUpdateCanvases();
            ConfigurePageLayout();
            MoveMainMenuContentIntoMainNode();

            _buttonL.onClick.RemoveListener(OpenShopPage);
            _buttonM.onClick.RemoveListener(OpenMainPage);
            _buttonR.onClick.RemoveListener(OpenTripPage);
            _buttonL.onClick.AddListener(OpenShopPage);
            _buttonM.onClick.AddListener(OpenMainPage);
            _buttonR.onClick.AddListener(OpenTripPage);

            _initialized = true;
            CurrentIndex = MainIndex;
            SetContentPositionImmediate(CurrentIndex);
            UpdateTabHighlight(MainIndex, animated: false);
            StartCoroutine(RefreshLayoutUntilReady());
            Debug.Log("[UIMainMenuTabPager] Initialized.");
            return true;
        }

        public void ResetToMain(bool animated = false)
        {
            if (!_initialized && !Initialize())
            {
                return;
            }

            SwitchTo(MainIndex, animated);
        }

        public void SwitchTo(int index, bool animated = true)
        {
            if (!_initialized && !Initialize())
            {
                return;
            }

            if (_pageWidth <= 0f)
            {
                ConfigurePageLayout();
            }

            int targetIndex = Mathf.Clamp(index, ShopIndex, TripIndex);
            SetCurrentIndex(targetIndex);
            UpdateTabHighlight(targetIndex, animated && isActiveAndEnabled && _pageWidth > 0f);

            if (!animated || !isActiveAndEnabled || _pageWidth <= 0f)
            {
                SetContentPositionImmediate(targetIndex);
                return;
            }

            StartSnap(TargetContentX(targetIndex));
        }

        public void SwitchToShop(bool animated = true)
        {
            SwitchTo(ShopIndex, animated);
        }

        public void CancelDrag()
        {
            _dragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_initialized || _pageWidth <= 0f)
            {
                return;
            }

            StopSnap();
            _dragging = true;
            _dragStartContentX = _scrollContent.anchoredPosition.x;
            _dragStartTime = Time.unscaledTime;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root, eventData.position, eventData.pressEventCamera, out _dragStartLocalPoint);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root, eventData.position, eventData.pressEventCamera, out Vector2 currentPoint);

            float targetX = _dragStartContentX + currentPoint.x - _dragStartLocalPoint.x;
            float currentPageX = TargetContentX(CurrentIndex);
            targetX = Mathf.Clamp(targetX, currentPageX - _pageWidth, currentPageX + _pageWidth);
            targetX = Mathf.Clamp(targetX, TargetContentX(TripIndex), TargetContentX(ShopIndex));
            SetContentX(targetX);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            float currentPageX = TargetContentX(CurrentIndex);
            float displacement = _scrollContent.anchoredPosition.x - currentPageX;
            float elapsed = Mathf.Max(0.01f, Time.unscaledTime - _dragStartTime);
            float velocity = displacement / elapsed;
            float distanceThreshold = _pageWidth * snapDistanceRatio;

            if ((displacement > distanceThreshold || velocity > snapVelocity) && CurrentIndex > ShopIndex)
            {
                SetCurrentIndex(CurrentIndex - 1);
            }
            else if ((displacement < -distanceThreshold || velocity < -snapVelocity) && CurrentIndex < TripIndex)
            {
                SetCurrentIndex(CurrentIndex + 1);
            }

            UpdateTabHighlight(CurrentIndex, animated: true);
            StartSnap(TargetContentX(CurrentIndex));
        }

        private void OpenShopPage()
        {
            Debug.Log("[UIMainMenuTabPager] Click Shop");
            SwitchTo(ShopIndex);
        }

        private void OpenMainPage()
        {
            Debug.Log("[UIMainMenuTabPager] Click Main");
            SwitchTo(MainIndex);
        }

        private void OpenTripPage()
        {
            Debug.Log("[UIMainMenuTabPager] Click Trip");
            SwitchTo(TripIndex);
        }

        private void ConfigurePageLayout()
        {
            if (_layouting || _root == null || _scrollContent == null)
            {
                return;
            }

            _layouting = true;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);

            _pageWidth = ResolvePageWidth();
            if (_pageWidth <= 0f)
            {
                _layouting = false;
                return;
            }

            _scrollContent.anchorMin = Vector2.zero;
            _scrollContent.anchorMax = Vector2.one;
            _scrollContent.pivot = new Vector2(0.5f, 0.5f);
            _scrollContent.offsetMin = Vector2.zero;
            _scrollContent.offsetMax = Vector2.zero;

            float tabBarBottomInset = GetTabBarBottomInset();
            ConfigurePage(_shopNode, -_pageWidth, tabBarBottomInset, clipOverflow: true);
            ConfigurePage(_mainNode, 0f, 0f, clipOverflow: false);
            ConfigurePage(_tripNode, _pageWidth, tabBarBottomInset, clipOverflow: true);

            SetContentPositionImmediate(CurrentIndex);
            _layouting = false;
        }

        private void ConfigurePage(RectTransform page, float positionX, float bottomInset, bool clipOverflow)
        {
            page.anchorMin = new Vector2(0.5f, 0f);
            page.anchorMax = new Vector2(0.5f, 1f);
            page.pivot = new Vector2(0.5f, 0.5f);
            page.sizeDelta = new Vector2(_pageWidth, -bottomInset);
            page.anchoredPosition = new Vector2(positionX, bottomInset * 0.5f);

            // shop / trip 侧页裁切溢出背景，避免横向滚动时压住 mainNode
            if (clipOverflow && page.GetComponent<RectMask2D>() == null)
            {
                page.gameObject.AddComponent<RectMask2D>();
            }
        }

        private float GetTabBarBottomInset()
        {
            if (_tabBar == null)
            {
                return 0f;
            }

            var corners = new Vector3[4];
            _tabBar.GetWorldCorners(corners);
            float tabBarTop = _root.InverseTransformPoint(corners[1]).y;
            return Mathf.Clamp(tabBarTop - _root.rect.yMin, 0f, _root.rect.height);
        }

        private void MoveMainMenuContentIntoMainNode()
        {
            var childrenToMove = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != _scrollContent && child != _tabBar)
                {
                    childrenToMove.Add(child);
                }
            }

            foreach (Transform child in childrenToMove)
            {
                child.SetParent(_mainNode, true);
                child.SetAsLastSibling();
            }
        }

        private IEnumerator RefreshLayoutUntilReady()
        {
            // Overlay/相机 Canvas 首帧 rect 可能为 0，多等几帧直到可滑动
            for (int i = 0; i < 8; i++)
            {
                yield return null;
                ConfigurePageLayout();
                if (_pageWidth > 0f)
                {
                    SetContentPositionImmediate(CurrentIndex);
                    yield break;
                }
            }

            Debug.LogWarning($"[UIMainMenuTabPager] pageWidth still 0 after retries. root={_root?.rect}");
        }

        private float ResolvePageWidth()
        {
            float width = _root.rect.width;
            if (width > 1f)
            {
                return width;
            }

            Canvas canvas = _root.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                float scalerWidth = canvas.pixelRect.width;
                if (canvas.scaleFactor > 0f)
                {
                    scalerWidth /= canvas.scaleFactor;
                }

                if (scalerWidth > 1f)
                {
                    return scalerWidth;
                }
            }

            return Screen.width > 1 ? Screen.width : 0f;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_initialized && isActiveAndEnabled)
            {
                ConfigurePageLayout();
            }
        }

        private void StartSnap(float targetX)
        {
            StopSnap();
            _snapCoroutine = StartCoroutine(SnapTo(targetX));
        }

        private IEnumerator SnapTo(float targetX)
        {
            float startX = _scrollContent.anchoredPosition.x;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, snapDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                SetContentX(Mathf.LerpUnclamped(startX, targetX, eased));
                yield return null;
            }

            SetContentX(targetX);
            _snapCoroutine = null;
        }

        private void StopSnap()
        {
            if (_snapCoroutine == null)
            {
                return;
            }

            StopCoroutine(_snapCoroutine);
            _snapCoroutine = null;
        }

        private void SetContentPositionImmediate(int index)
        {
            StopSnap();
            SetContentX(TargetContentX(index));
        }

        private void SetContentX(float x)
        {
            Vector2 position = _scrollContent.anchoredPosition;
            position.x = x;
            _scrollContent.anchoredPosition = position;
        }

        private float TargetContentX(int index)
        {
            return (MainIndex - index) * _pageWidth;
        }

        private void SetCurrentIndex(int index)
        {
            if (CurrentIndex == index)
            {
                return;
            }

            CurrentIndex = index;
            PageChanged?.Invoke(CurrentIndex);
        }

        private void CacheHighlightTargets()
        {
            RectTransform leftHighlight = FindRectTransform("left");
            RectTransform rightHighlight = FindRectTransform("right");

            if (_middleHighlight != null)
            {
                var middleImage = _middleHighlight.GetComponent<Image>();
                if (middleImage != null)
                {
                    middleImage.raycastTarget = false;
                }

                _highlightPositions[MainIndex] = _middleHighlight.anchoredPosition;
                _highlightSizes[MainIndex] = _middleHighlight.sizeDelta;
            }
            else
            {
                _highlightPositions[MainIndex] = Vector2.zero;
                _highlightSizes[MainIndex] = new Vector2(275f, 209.393f);
            }

            if (leftHighlight != null)
            {
                _highlightPositions[ShopIndex] = leftHighlight.anchoredPosition;
                _highlightSizes[ShopIndex] = leftHighlight.sizeDelta;
                leftHighlight.gameObject.SetActive(false);
            }
            else
            {
                _highlightPositions[ShopIndex] = new Vector2(-247.91f, _highlightPositions[MainIndex].y);
                _highlightSizes[ShopIndex] = new Vector2(224.1804f, _highlightSizes[MainIndex].y);
            }

            if (rightHighlight != null)
            {
                _highlightPositions[TripIndex] = rightHighlight.anchoredPosition;
                _highlightSizes[TripIndex] = rightHighlight.sizeDelta;
                rightHighlight.gameObject.SetActive(false);
            }
            else
            {
                _highlightPositions[TripIndex] = new Vector2(248f, _highlightPositions[MainIndex].y);
                _highlightSizes[TripIndex] = new Vector2(224.1804f, _highlightSizes[MainIndex].y);
            }

            if (_middleHighlight != null)
            {
                _middleHighlight.gameObject.SetActive(true);
            }
        }

        private void CacheTabLabels()
        {
            _labelL = FindTabLabel(_buttonL);
            _labelM = FindTabLabel(_buttonM);
            _labelR = FindTabLabel(_buttonR);
            _labelRectL = _labelL != null ? _labelL.transform as RectTransform : null;
            _labelRectM = _labelM != null ? _labelM.transform as RectTransform : null;
            _labelRectR = _labelR != null ? _labelR.transform as RectTransform : null;
        }

        private void CacheTabIconTargets()
        {
            _tabIconRestPositions[ShopIndex] = ((RectTransform)_buttonL.transform).anchoredPosition;
            _tabIconRestPositions[MainIndex] = ((RectTransform)_buttonM.transform).anchoredPosition;
            _tabIconRestPositions[TripIndex] = ((RectTransform)_buttonR.transform).anchoredPosition;

            // 当前主页图标的高度就是三个 Tab 的选中高度。
            _selectedIconY = _tabIconRestPositions[MainIndex].y;
            // 主页未选中时回到两侧图标的基线之间，保持底部栏视觉平衡。
            _mainIconRestY = (_tabIconRestPositions[ShopIndex].y + _tabIconRestPositions[TripIndex].y) * 0.5f;
            // 当前主页文字已经在正确的“图标下方”位置，复用它作为三个 Tab 的选中文字位置。
            _selectedLabelY = _labelRectM != null ? _labelRectM.anchoredPosition.y : -80f;
        }

        private static GameObject FindTabLabel(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Transform text = button.transform.Find("Text");
            return text != null ? text.gameObject : null;
        }

        private void UpdateTabHighlight(int index, bool animated)
        {
            int targetIndex = Mathf.Clamp(index, ShopIndex, TripIndex);
            UpdateTabIconState(targetIndex, animated);

            if (_middleHighlight == null)
            {
                return;
            }

            Vector2 targetPos = _highlightPositions[targetIndex];
            Vector2 targetSize = _highlightSizes[targetIndex];

            if (!animated || !isActiveAndEnabled)
            {
                StopHighlightTween();
                _middleHighlight.anchoredPosition = targetPos;
                _middleHighlight.sizeDelta = targetSize;
                return;
            }

            StartHighlightTween(targetPos, targetSize);
        }

        private void UpdateTabIconState(int selectedIndex, bool animated)
        {
            SetTabLabelVisible(_labelL, _labelRectL, selectedIndex == ShopIndex);
            SetTabLabelVisible(_labelM, _labelRectM, selectedIndex == MainIndex);
            SetTabLabelVisible(_labelR, _labelRectR, selectedIndex == TripIndex);

            Vector2 leftTarget = GetTabIconTargetPosition(ShopIndex, selectedIndex == ShopIndex);
            Vector2 middleTarget = GetTabIconTargetPosition(MainIndex, selectedIndex == MainIndex);
            Vector2 rightTarget = GetTabIconTargetPosition(TripIndex, selectedIndex == TripIndex);

            if (!animated || !isActiveAndEnabled)
            {
                StopTabIconTween();
                ((RectTransform)_buttonL.transform).anchoredPosition = leftTarget;
                ((RectTransform)_buttonM.transform).anchoredPosition = middleTarget;
                ((RectTransform)_buttonR.transform).anchoredPosition = rightTarget;
                return;
            }

            StopTabIconTween();
            _tabIconCoroutine = StartCoroutine(AnimateTabIcons(leftTarget, middleTarget, rightTarget));
        }

        private Vector2 GetTabIconTargetPosition(int index, bool selected)
        {
            Vector2 target = _tabIconRestPositions[index];
            target.y = selected ? _selectedIconY : (index == MainIndex ? _mainIconRestY : target.y);
            return target;
        }

        private void SetTabLabelVisible(GameObject label, RectTransform labelRect, bool visible)
        {
            if (label == null)
            {
                return;
            }

            if (visible && labelRect != null)
            {
                Vector2 pos = labelRect.anchoredPosition;
                pos.y = _selectedLabelY;
                labelRect.anchoredPosition = pos;
            }

            label.SetActive(visible);
        }

        private IEnumerator AnimateTabIcons(Vector2 leftTarget, Vector2 middleTarget, Vector2 rightTarget)
        {
            RectTransform left = (RectTransform)_buttonL.transform;
            RectTransform middle = (RectTransform)_buttonM.transform;
            RectTransform right = (RectTransform)_buttonR.transform;
            Vector2 leftStart = left.anchoredPosition;
            Vector2 middleStart = middle.anchoredPosition;
            Vector2 rightStart = right.anchoredPosition;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, snapDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                left.anchoredPosition = Vector2.LerpUnclamped(leftStart, leftTarget, eased);
                middle.anchoredPosition = Vector2.LerpUnclamped(middleStart, middleTarget, eased);
                right.anchoredPosition = Vector2.LerpUnclamped(rightStart, rightTarget, eased);
                yield return null;
            }

            left.anchoredPosition = leftTarget;
            middle.anchoredPosition = middleTarget;
            right.anchoredPosition = rightTarget;
            _tabIconCoroutine = null;
        }

        private void StartHighlightTween(Vector2 targetPos, Vector2 targetSize)
        {
            StopHighlightTween();
            _highlightCoroutine = StartCoroutine(AnimateHighlight(targetPos, targetSize));
        }

        private IEnumerator AnimateHighlight(Vector2 targetPos, Vector2 targetSize)
        {
            Vector2 startPos = _middleHighlight.anchoredPosition;
            Vector2 startSize = _middleHighlight.sizeDelta;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, snapDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _middleHighlight.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
                _middleHighlight.sizeDelta = Vector2.LerpUnclamped(startSize, targetSize, eased);
                yield return null;
            }

            _middleHighlight.anchoredPosition = targetPos;
            _middleHighlight.sizeDelta = targetSize;
            _highlightCoroutine = null;
        }

        private void StopHighlightTween()
        {
            if (_highlightCoroutine == null)
            {
                return;
            }

            StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = null;
        }

        private void StopTabIconTween()
        {
            if (_tabIconCoroutine == null)
            {
                return;
            }

            StopCoroutine(_tabIconCoroutine);
            _tabIconCoroutine = null;
        }

        private RectTransform FindRectTransform(string objectName)
        {
            Transform target = FindTransform(objectName);
            return target as RectTransform;
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            Transform target = FindTransform(objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private Transform FindTransform(string objectName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        private void OnDisable()
        {
            _dragging = false;
            StopSnap();
            StopHighlightTween();
            StopTabIconTween();
        }

        private void OnDestroy()
        {
            if (_buttonL != null)
            {
                _buttonL.onClick.RemoveListener(OpenShopPage);
            }

            if (_buttonM != null)
            {
                _buttonM.onClick.RemoveListener(OpenMainPage);
            }

            if (_buttonR != null)
            {
                _buttonR.onClick.RemoveListener(OpenTripPage);
            }

            PageChanged = null;
        }
    }
}
