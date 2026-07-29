using FishFramework;
using GameLogic.Wooduku;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GM 选关面板（Prefab: GPLevelChooseDlg）。F1 打开。
/// Wooduku 侧：跳关走 WoodukuGameplayView.EnterLevel（1-based）。
/// </summary>
public class GMLevelChooseUI : GPLevelChooseDlgBase
{
    protected override void OnOpen()
    {
        base.OnOpen();
        GameModule.Setting?.EnableGmLevelPassGlobalSwitch();
        RefreshInfo();
    }

    protected override void OnClicked(Button button)
    {
        if (button == jumpLevelBtn)
        {
            if (!TryParseLevel(levelInputFieldTMP != null ? levelInputFieldTMP.text : null, out int levelId))
            {
                Debug.LogWarning("[GM] 关卡号无效，请输入 >= 1 的整数。");
                return;
            }

            DestroySelf();
            var gameplay = WoodukuGameplayView.EnsureSpawned();
            if (gameplay == null)
            {
                Debug.LogError("[GM] WoodukuGameplayView 不可用，无法跳关。");
                return;
            }

            gameplay.EnterLevel(levelId);
            return;
        }

        if (button == closeButton)
        {
            DestroySelf();
            return;
        }

        // 其余按钮暂 stub（无尽关 / AB / 道具等尚未移植）
        if (button == jumpInfinitLevelBtn ||
            button == addItemsBtn ||
            button == deleteBtn ||
            button == addCoinBtn ||
            button == buttonA ||
            button == buttonB ||
            button == buttonDefaultAB)
        {
            Debug.Log("[GM] 该功能尚未在 Wooduku 接入。");
            return;
        }

        DestroySelf();
    }

    private void RefreshInfo()
    {
        if (uidInfo != null)
        {
            uidInfo.text = "UID: Wooduku(本地)";
        }

        if (abInfo != null)
        {
            abInfo.text = "AB: 未接入";
        }

        if (userQualityInfo != null)
        {
            userQualityInfo.text = "user_quality: -";
        }

        if (failLevelCountInfo != null)
        {
            failLevelCountInfo.text = "失败关数: -";
        }

        if (levelInputFieldTMP != null && string.IsNullOrEmpty(levelInputFieldTMP.text))
        {
            levelInputFieldTMP.text = "1";
        }
    }

    private static bool TryParseLevel(string raw, out int levelId)
    {
        levelId = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (!int.TryParse(raw.Trim(), out levelId))
        {
            return false;
        }

        return levelId >= 1;
    }
}
