using DG.Tweening;
using FishFramework;
using UnityEngine;

public class LoginUI : LoginUIBase
{
    private Tween loadingSpinTween;
    private Sequence enterGameSequence;
    private bool hasEnterGame;

    protected override void OnCreating()
    {
        var loadingIcon = viewBehaviour != null ? viewBehaviour.transform.Find("loading") : null;
        loadingSpinTween = LoadingIconSpin.Start(loadingIcon);
    }

    protected override void OnOpen()
    {
        InitUI();
    }

    private void InitUI()
    {
        GameModule.Setting?.SetFrameRate();
        GameModule.Setting?.SetDynamicResolution();
        ShowQuate();

        // Editor / 无 SDK：短延迟后进主场景
        Utility.DGDelayedCall(0.2f, StartEnterGame);
    }

    private void ShowQuate()
    {
        if (quateDescLb != null)
        {
            quateDescLb.text = "Welcome";
        }

        if (quateNameLb != null)
        {
            quateNameLb.text = "— NewFish";
        }

        if (userDataVer != null)
        {
            userDataVer.text = Application.version;
        }
    }

    private void StartEnterGame()
    {
#if UNITY_EDITOR
        float wait = 0.5f;
#else
        float wait = 1.5f;
#endif
        enterGameSequence?.Kill();
        enterGameSequence = DOTween.Sequence()
            .AppendInterval(wait)
            .AppendCallback(EnterGameScene)
            .SetUpdate(true);
    }

    private void EnterGameScene()
    {
        if (hasEnterGame)
        {
            return;
        }

        hasEnterGame = true;
        SceneLoaderHelper.LoadSceneAsync(ScenePaths.GameScene, needLoading: true, onCompleted: () =>
        {
            GameModule.UI?.DestroyAllPanel();
        });
    }

    protected override void OnDestroyed()
    {
        LoadingIconSpin.Stop(ref loadingSpinTween);
        enterGameSequence?.Kill();
        enterGameSequence = null;
    }
}
