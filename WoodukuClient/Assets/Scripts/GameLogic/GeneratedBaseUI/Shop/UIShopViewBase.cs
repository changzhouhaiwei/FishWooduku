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
        // Prefab 可能被裁剪过 OpElement；按数量安全绑定，避免打开商店即崩
        var ops = viewBehaviour != null ? viewBehaviour.opElementList : null;
        int count = ops != null ? ops.Count : 0;

        if (count > 0)
        {
            backButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
        }

        if (count > 2)
        {
            shopCellNoAds = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(2, 0);
        }

        if (count > 3)
        {
            shopCellGold = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(3, 0);
        }

        if (count > 4)
        {
            shopCellBig = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(4, 0);
        }

        if (count > 5)
        {
            assetNode_UIWidgetBehaviour = viewBehaviour.GetComponentByIndexs<UIWidgetBehaviour>(5, 0);
            assetNode_RectTransform = viewBehaviour.GetComponentByIndexs<RectTransform>(5, 1);
        }

        if (count > 6)
        {
            safeArea = viewBehaviour.GetComponentByIndexs<RectTransform>(6, 0);
        }
        else if (count > 2)
        {
            // 裁剪版：SafeArea 可能落在末尾
            safeArea = viewBehaviour.GetComponentByIndexs<RectTransform>(count - 1, 0);
        }

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
