using FishFramework;
using UnityEngine;
using UnityEngine.UI;

public class UIMapTravelViewBase : UIWidget
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/GameRes/Prefabs/TUIStory/UIMapTravelView.prefab";

    protected RectTransform safeArea;
    protected TButton bookBtn;
    protected Image bg1;
    protected Image pinLocked;
    protected Image pinReached;
    protected Image pinCurrent;

    protected override void OnBindCompsAndEvents()
    {
        safeArea = viewBehaviour.GetComponentByIndexs<RectTransform>(0, 0);
        bookBtn = viewBehaviour.GetComponentByIndexs<TButton>(1, 0);
        bg1 = viewBehaviour.GetComponentByIndexs<Image>(2, 0);
        pinLocked = viewBehaviour.GetComponentByIndexs<Image>(3, 0);
        pinReached = viewBehaviour.GetComponentByIndexs<Image>(4, 0);
        pinCurrent = viewBehaviour.GetComponentByIndexs<Image>(5, 0);

        if (bookBtn != null)
        {
            BindEvent(bookBtn);
        }
    }

    protected override void OnUnbindCompsAndEvents()
    {
        if (bookBtn != null)
        {
            UnbindEvent(bookBtn);
        }

        safeArea = null;
        bookBtn = null;
        bg1 = null;
        pinLocked = null;
        pinReached = null;
        pinCurrent = null;
    }
}
