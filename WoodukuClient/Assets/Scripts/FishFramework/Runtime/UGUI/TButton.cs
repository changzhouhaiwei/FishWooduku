using System;
using DG.Tweening;
using FishFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TButton : Button
{
    public bool clickDoTween = true;
    public float downScale = 0.96f;
    public float upScale = 1.05f;
    
    public string audioId = "1001";
    
    private bool isLongPressing;
    private const float LONGPRESS_TIME = 0.5f;

    public Action onPointerDown; // 按下
    public Action onPointerUp; // 抬起
    public Action onLongClickHandler; // 长按

    private Material greyMaterial;
    private const bool disableToGray = true;
    private Vector3 localScale = Vector3.one;

    protected override void Start()
    {
        base.Start();
        transition = Transition.None;
        localScale = transform.localScale;
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        switch (state)
        {
            case SelectionState.Disabled:
                if (disableToGray)
                {
                    if (greyMaterial == null)
                    {
                        var shader = ResourceModule.LoadAsset<Shader>("Assets/GameRes/Shaders/Gray.shader");
                        if (shader == null)
                        {
                            shader = Shader.Find("UI/Grey");
                        }

                        if (shader != null)
                        {
                            greyMaterial = new Material(shader);
                        }
                    }
                    image.material = greyMaterial;
                }
                break;
            default:
                if (disableToGray)
                    image.material = null;
                break;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        transform.DOKill();
        CancelInvoke();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (interactable)
        {
            if (clickDoTween)
            {
                if (DOTween.IsTweening(transform))
                {
                    transform.DOKill();
                }

                transform.DOScale(downScale * localScale, 0.06f).SetUpdate(true);
            }

            Invoke(nameof(ExecuteLongClickHandler), LONGPRESS_TIME);
            onPointerDown?.Invoke();
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (interactable)
        {
            if (clickDoTween)
            {
                transform.DOKill();
                DOTween.Sequence()
                    .Append(transform.DOScale(upScale * localScale, 0.12f))
                    .Append(transform.DOScale(1.0f * localScale, 0.06f))
                    .SetUpdate(true).SetLink(gameObject,LinkBehaviour.KillOnDestroy);
            }

            isLongPressing = false;
            CancelInvoke(nameof(ExecuteLongClickHandler));
            onPointerUp?.Invoke();
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        if (interactable && !string.IsNullOrEmpty(audioId))
        {
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        if (isLongPressing)
        {
            isLongPressing = false;
            CancelInvoke(nameof(ExecuteLongClickHandler));
        }
    }

    private void ExecuteLongClickHandler()
    {
        isLongPressing = true;
        onLongClickHandler?.Invoke();
    }

    public void SetAudioId(string value)
    {
        audioId = value;
    }

    /// <summary>
    /// Sync resting scale used by click tween. Call when external code changes transform.localScale.
    /// </summary>
    public void SetRestingScale(Vector3 scale)
    {
        localScale = scale;
        transform.DOKill();
        transform.localScale = scale;
    }
}