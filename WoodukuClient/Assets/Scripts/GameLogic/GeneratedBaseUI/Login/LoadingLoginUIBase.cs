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
        quateNameLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(0, 0);
        quateDescLb = viewBehaviour.GetComponentByIndexs<TextMeshProUGUI>(1, 0);
    }

    protected override void OnUnbindCompsAndEvents()
    {
        quateNameLb = null;
        quateDescLb = null;
    }
}
