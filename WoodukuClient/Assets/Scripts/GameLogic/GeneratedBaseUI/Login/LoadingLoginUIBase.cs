using FishFramework;
using TMPro;

public class LoadingLoginUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/GameRes/Prefabs/Login/LoadingLoginUI.prefab";

    protected TextMeshProUGUI quateNameLb;
    protected TextMeshProUGUI quateDescLb;

    protected override void OnBindCompsAndEvents()
    {
        // Prefab 当前 OpElementList 为空；缺失绑定时保持 null
        if (viewBehaviour != null && viewBehaviour.opElementList != null && viewBehaviour.opElementList.Count > 0)
        {
            quateNameLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
        }

        if (viewBehaviour != null && viewBehaviour.opElementList != null && viewBehaviour.opElementList.Count > 1)
        {
            quateDescLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
        }
    }

    protected override void OnUnbindCompsAndEvents()
    {
        quateNameLb = null;
        quateDescLb = null;
    }
}
