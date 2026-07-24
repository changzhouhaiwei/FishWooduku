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
        userDataVer = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
        quateDescLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
        quateNameLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(2, 0);
    }

    protected override void OnUnbindCompsAndEvents()
    {
        userDataVer = null;
        quateDescLb = null;
        quateNameLb = null;
    }
}
