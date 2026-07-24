using DG.Tweening;
using UnityEngine;

public class LoadingUI : LoadingLoginUIBase
{
    private Tween loadingSpinTween;

    private void ShowQuate()
    {
        if (quateDescLb != null)
        {
            quateDescLb.text = "Loading...";
        }

        if (quateNameLb != null)
        {
            quateNameLb.text = string.Empty;
        }
    }

    protected override void OnCreating()
    {
        base.OnCreating();
        ShowQuate();

        var loadingIcon = viewBehaviour != null ? viewBehaviour.transform.Find("loading") : null;
        loadingSpinTween = LoadingIconSpin.Start(loadingIcon);
    }

    protected override void OnDestroyed()
    {
        LoadingIconSpin.Stop(ref loadingSpinTween);
        base.OnDestroyed();
    }
}
