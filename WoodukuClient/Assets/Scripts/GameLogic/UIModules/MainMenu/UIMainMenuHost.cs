using FishFramework;
using GameLogic.Wooduku;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.MainMenu
{
    /// <summary>
    /// 主页 Host：绑定 GameScene 常驻根节点，懒挂载 Shop / Travel Widget。
    /// </summary>
    public sealed class UIMainMenuHost : UIView
    {
        private UIMainMenuTabPager _tabPager;
        private RectTransform _shopNode;
        private UIWidgetBehaviour _shopResidentView;
        private UIWidgetBehaviour _travelResidentView;
        private UIShopView _shopView;
        private UIMapTravelView _mapTravelView;
        private Button _playButton;
        private bool _bound;

        public void Bind(UIViewBehaviour rootBehaviour)
        {
            if (rootBehaviour == null)
            {
                throw new System.ArgumentNullException(nameof(rootBehaviour));
            }

            if (_bound)
            {
                return;
            }

            // parent 可为 null（场景根实例）；与当前 parent 一致则不会被 SetParent
            Create(nameof(UIMainMenuHost), rootBehaviour.transform.parent, rootBehaviour);
            _bound = true;
        }

        public void Unbind()
        {
            if (!_bound)
            {
                return;
            }

            // 只解绑逻辑，不 Destroy 常驻根（由场景 / UIMainMenu 负责销毁 GameObject）
            if (_tabPager != null)
            {
                _tabPager.PageChanged -= OnTabPageChanged;
            }

            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
                _playButton = null;
            }

            // 驻留 Widget 与主页同树，勿 CloseView（会 Object.Destroy 子节点）
            _shopView = null;
            _mapTravelView = null;
            _shopResidentView = null;
            _travelResidentView = null;
            _tabPager = null;
            _bound = false;
            viewBehaviour = null;
            gameObject = null;
            rectTransform = null;
        }

        protected override void OnCreating()
        {
            _tabPager = gameObject.GetComponent<UIMainMenuTabPager>();
            if (_tabPager == null)
            {
                _tabPager = gameObject.AddComponent<UIMainMenuTabPager>();
            }

            _tabPager.Initialize();

            _shopNode = FindChildRect("shopNode");
            Transform residentShop = _shopNode != null ? _shopNode.Find("UIShopView") : null;
            _shopResidentView = residentShop != null
                ? residentShop.GetComponent<UIWidgetBehaviour>()
                : null;

            RectTransform tripNode = FindChildRect("tripNode");
            Transform residentTravel = tripNode != null ? tripNode.Find("UIMapTravelView") : null;
            _travelResidentView = residentTravel != null
                ? residentTravel.GetComponent<UIWidgetBehaviour>()
                : null;

            _tabPager.PageChanged -= OnTabPageChanged;
            _tabPager.PageChanged += OnTabPageChanged;

            _playButton = FindChildRect("Play Button")?.GetComponent<Button>();
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
                _playButton.onClick.AddListener(OnPlayClicked);
            }
            else
            {
                Debug.LogWarning("[UIMainMenuHost] Play Button not found.");
            }
        }

        private void OnPlayClicked()
        {
            var gameplay = WoodukuGameplayView.EnsureSpawned();
            if (gameplay == null)
            {
                Debug.LogError("[UIMainMenuHost] WoodukuGameplayView spawn failed.");
                return;
            }

            gameplay.EnterLevel(1);
        }

        protected override void OnDestroyed()
        {
            if (_tabPager != null)
            {
                _tabPager.PageChanged -= OnTabPageChanged;
            }

            _shopView = null;
            _mapTravelView = null;
            _shopResidentView = null;
            _travelResidentView = null;
            _tabPager = null;
        }

        private void OnTabPageChanged(int pageIndex)
        {
            const int shopPageIndex = 0;
            const int travelPageIndex = 2;

            if (pageIndex != shopPageIndex)
            {
                _shopView?.SetListening(false);
            }

            if (pageIndex == travelPageIndex)
            {
                EnsureTravelBound();
            }

            if (pageIndex == shopPageIndex)
            {
                EnsureShopBound();
            }
        }

        private void EnsureShopBound()
        {
            if (_shopView != null)
            {
                _shopView.SetListening(true);
                return;
            }

            if (_shopResidentView == null)
            {
                Debug.LogError("[UIMainMenuHost] Resident UIShopView is missing.");
                return;
            }

            _shopView = new UIShopView();
            _shopView.Bind(_shopResidentView, false, null);
        }

        private void EnsureTravelBound()
        {
            if (_mapTravelView != null)
            {
                _mapTravelView.RefreshBackground();
                return;
            }

            if (_travelResidentView == null)
            {
                Debug.LogError("[UIMainMenuHost] Resident UIMapTravelView is missing.");
                return;
            }

            _mapTravelView = new UIMapTravelView();
            _mapTravelView.Bind(_travelResidentView);
        }

        private RectTransform FindChildRect(string objectName)
        {
            foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }
    }
}
