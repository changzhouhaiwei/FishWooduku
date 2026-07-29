using FishFramework;
using TMPro;
using UnityEngine.UI;

public class GPLevelChooseDlgBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/GameRes/Prefabs/GamePlayUI/GPLevelChooseDlg.prefab";

    protected TButton closeButton;
    protected TMP_InputField levelInputFieldTMP;
    protected TButton jumpLevelBtn;
    protected TButton addItemsBtn;
    protected TextMeshProUGUI uidInfo;
    protected TextMeshProUGUI abInfo;
    protected TButton buttonA;
    protected TButton buttonB;
    protected TButton deleteBtn;
    protected TButton addCoinBtn;
    protected TButton jumpInfinitLevelBtn;
    protected TMP_InputField infinitLevelInputFieldTMP;
    protected TButton buttonDefaultAB;
    protected TextMeshProUGUI userQualityInfo;
    protected TextMeshProUGUI failLevelCountInfo;

    protected override void OnBindCompsAndEvents()
    {
        closeButton = viewBehaviour.GetComponentByIndexs<TButton>(0, 0);
        levelInputFieldTMP = viewBehaviour.GetComponentByIndexs<TMP_InputField>(1, 0);
        jumpLevelBtn = viewBehaviour.GetComponentByIndexs<TButton>(2, 0);
        addItemsBtn = viewBehaviour.GetComponentByIndexs<TButton>(3, 0);
        uidInfo = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(4, 0);
        abInfo = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(5, 0);
        buttonA = viewBehaviour.GetComponentByIndexs<TButton>(6, 0);
        buttonB = viewBehaviour.GetComponentByIndexs<TButton>(7, 0);
        deleteBtn = viewBehaviour.GetComponentByIndexs<TButton>(8, 0);
        addCoinBtn = viewBehaviour.GetComponentByIndexs<TButton>(9, 0);
        jumpInfinitLevelBtn = viewBehaviour.GetComponentByIndexs<TButton>(10, 0);
        infinitLevelInputFieldTMP = viewBehaviour.GetComponentByIndexs<TMP_InputField>(11, 0);
        buttonDefaultAB = viewBehaviour.GetComponentByIndexs<TButton>(12, 0);
        userQualityInfo = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(13, 0);
        failLevelCountInfo = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(14, 0);

        BindEvent(closeButton);
        BindEvent(levelInputFieldTMP);
        BindEvent(jumpLevelBtn);
        BindEvent(addItemsBtn);
        BindEvent(buttonA);
        BindEvent(buttonB);
        BindEvent(deleteBtn);
        BindEvent(addCoinBtn);
        BindEvent(jumpInfinitLevelBtn);
        BindEvent(infinitLevelInputFieldTMP);
        BindEvent(buttonDefaultAB);
    }

    protected override void OnUnbindCompsAndEvents()
    {
        UnbindEvent(closeButton);
        UnbindEvent(levelInputFieldTMP);
        UnbindEvent(jumpLevelBtn);
        UnbindEvent(addItemsBtn);
        UnbindEvent(buttonA);
        UnbindEvent(buttonB);
        UnbindEvent(deleteBtn);
        UnbindEvent(addCoinBtn);
        UnbindEvent(jumpInfinitLevelBtn);
        UnbindEvent(infinitLevelInputFieldTMP);
        UnbindEvent(buttonDefaultAB);

        closeButton = null;
        levelInputFieldTMP = null;
        jumpLevelBtn = null;
        addItemsBtn = null;
        uidInfo = null;
        abInfo = null;
        buttonA = null;
        buttonB = null;
        deleteBtn = null;
        addCoinBtn = null;
        jumpInfinitLevelBtn = null;
        infinitLevelInputFieldTMP = null;
        buttonDefaultAB = null;
        userQualityInfo = null;
        failLevelCountInfo = null;
    }
}
