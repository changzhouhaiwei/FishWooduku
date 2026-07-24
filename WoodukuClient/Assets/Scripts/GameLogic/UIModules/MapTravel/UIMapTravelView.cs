using System;
using FishFramework;
using UnityEngine;

public sealed class UIMapTravelView : UIMapTravelViewBase
{
    private bool _created;

    public void Bind(UIWidgetBehaviour residentView)
    {
        if (residentView == null)
        {
            throw new ArgumentNullException(nameof(residentView));
        }

        if (_created)
        {
            RefreshBackground();
            return;
        }

        _created = true;
        Create(nameof(UIMapTravelView), residentView.transform.parent, residentView);
    }

    public void RefreshBackground()
    {
        // stub：完整地图背景后续接配置
    }

    public void CloseView()
    {
        if (!_created)
        {
            return;
        }

        Destroy();
        _created = false;
    }

    protected override void OnOpen()
    {
        Debug.Log("[UIMapTravelView] Bound (stub travel).");
        RefreshBackground();
    }

    protected override void OnClicked(UnityEngine.UI.Button button)
    {
        if (bookBtn != null && button == bookBtn)
        {
            Debug.Log("[UIMapTravelView] Book button clicked (stub).");
        }
    }
}
