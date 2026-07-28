using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class SpineInAndLoop : MonoBehaviour
{

    [SerializeField] SkeletonAnimation spine;
    [SerializeField] SkeletonGraphic spineGraphic;
    [SerializeField] string animIn;
    [SerializeField] string animLoop;
    [SerializeField] bool playIntroOncePerSession;
    [SerializeField] bool holdIntroLastFrame;

    private static readonly HashSet<string> PlayedIntroKeys = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlayedIntroKeys()
    {
        PlayedIntroKeys.Clear();
    }


    void PlayAnim()
    {
        Spine.AnimationState animationState = null;

        if (spine != null)
        {
            spine.Initialize(false);
            animationState = spine.AnimationState;
        }
        else if (spineGraphic != null)
        {
            spineGraphic.Initialize(false);
            animationState = spineGraphic.AnimationState;
        }

        if (animationState == null)
            return;

        string introKey = $"{animIn}|{animLoop}|{holdIntroLastFrame}";
        bool shouldPlayIntro = !playIntroOncePerSession || PlayedIntroKeys.Add(introKey);

        if (holdIntroLastFrame)
        {
            Spine.TrackEntry introEntry = animationState.SetAnimation(0, animIn, false);
            introEntry.TrackEnd = float.PositiveInfinity;

            // 后续复用该界面的实例直接显示末帧，不能再次播放入场动画。
            if (!shouldPlayIntro)
            {
                introEntry.TrackTime = introEntry.AnimationEnd;
                introEntry.TimeScale = 0f;
            }
        }
        else if (shouldPlayIntro)
        {
            animationState.SetAnimation(0, animIn, false);
            animationState.AddAnimation(0, animLoop, true, 0f);
        }
        else
        {
            animationState.SetAnimation(0, animLoop, true);
        }

        // 立即刷新到入场动画的第一帧，避免先显示一帧循环动画。
        if (spine != null)
            spine.Update(0f);
        else
            spineGraphic.Update(0f);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnEnable()
    {
        PlayAnim();
    }
}
