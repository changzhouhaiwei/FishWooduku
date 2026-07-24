using System;
using FishFramework;
using UnityEngine;

public sealed class UIShopView : UIShopViewBase
{
    private bool _created;
    private bool _showBackButton;
    private Action _backAction;

    public void Bind(UIWidgetBehaviour residentView, bool showBackButton, Action backAction)
    {
        if (residentView == null)
        {
            throw new ArgumentNullException(nameof(residentView));
        }

        _showBackButton = showBackButton;
        _backAction = backAction;

        if (_created)
        {
            if (backButton != null)
            {
                backButton.gameObject.SetActive(_showBackButton);
            }

            SetListening(true);
            return;
        }

        _created = true;
        Create(nameof(UIShopView), residentView.transform.parent, residentView);

        if (backButton != null)
        {
            backButton.gameObject.SetActive(_showBackButton);
        }
    }

    public void SetListening(bool listening)
    {
        // stub：完整商店监听后续接 IAP / ShopCfg
        if (gameObject != null)
        {
            gameObject.SetActive(listening || gameObject.activeSelf);
        }
    }

    protected override void OnClicked(UnityEngine.UI.Button button)
    {
        if (backButton != null && button == backButton)
        {
            _backAction?.Invoke();
        }
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
        Debug.Log("[UIShopView] Bound (stub shop list).");
    }
}
