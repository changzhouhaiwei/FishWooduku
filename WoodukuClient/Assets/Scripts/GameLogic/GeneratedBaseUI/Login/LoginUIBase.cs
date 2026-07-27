using FishFramework;
using TMPro;

public class LoginUIBase : UIPanel
{
    public override PanelLayer Layer { set; get; }
    protected override string PrefabPath => "Assets/GameRes/Prefabs/Login/LoginUI.prefab";

    protected TextMeshProUGUI userDataVer;
    protected TextMeshProUGUI quateDescLb;
    protected TextMeshProUGUI quateNameLb;

    protected override void OnBindCompsAndEvents()
    {
        // Prefab 当前只绑定了 userDataVer；quote 文本节点缺失时保持 null
        if (viewBehaviour != null && viewBehaviour.opElementList != null && viewBehaviour.opElementList.Count > 0)
        {
            userDataVer = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
        }

        if (viewBehaviour != null && viewBehaviour.opElementList != null && viewBehaviour.opElementList.Count > 1)
        {
            quateDescLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
        }

        if (viewBehaviour != null && viewBehaviour.opElementList != null && viewBehaviour.opElementList.Count > 2)
        {
            quateNameLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
        }
    }

    protected override void OnUnbindCompsAndEvents()
    {
        userDataVer = null;
        quateDescLb = null;
        quateNameLb = null;
    }
}
