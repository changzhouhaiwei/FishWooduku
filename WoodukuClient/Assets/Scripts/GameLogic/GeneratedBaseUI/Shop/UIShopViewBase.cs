using FishFramework;
using UnityEngine;
using UnityEngine.UI;

public class UIShopViewBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/GameRes/Prefabs/Shop/UIShopView.prefab";

    protected TButton backButton;
    protected UIWidgetBehaviour shopCellNoAds;
    protected UIWidgetBehaviour shopCellGold;
    protected UIWidgetBehaviour shopCellBig;
    protected UIWidgetBehaviour assetNode_UIWidgetBehaviour;
    protected RectTransform assetNode_RectTransform;
    protected RectTransform safeArea;

    protected override void OnBindCompsAndEvents()
    {
        // 索引与源 GeneratedBase 对齐；无 SuperScrollView 时跳过 LoopListView2
        backButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
        shopCellNoAds = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
        shopCellGold = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
        shopCellBig = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
        assetNode_UIWidgetBehaviour = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
        assetNode_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 1);
        safeArea = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);

        if (backButton != null)
        {
            BindEvent(backButton);
        }
    }

    protected override void OnUnbindCompsAndEvents()
    {
        if (backButton != null)
        {
            UnbindEvent(backButton);
        }

        backButton = null;
        shopCellNoAds = null;
        shopCellGold = null;
        shopCellBig = null;
        assetNode_UIWidgetBehaviour = null;
        assetNode_RectTransform = null;
        safeArea = null;
    }
}
