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
        // Prefab 中允许保留空绑定项；组件缺失时保持 null，避免索引越界。
        userDataVer = GetTextComponent(0);
        quateDescLb = GetTextComponent(1);
        quateNameLb = GetTextComponent(2);
    }

    private TextMeshProUGUI GetTextComponent(int elementIndex)
    {
        if (viewBehaviour == null ||
            viewBehaviour.opElementList == null ||
            elementIndex < 0 ||
            elementIndex >= viewBehaviour.opElementList.Count)
        {
            return null;
        }

        var element = viewBehaviour.opElementList[elementIndex];
        if (element == null || element.componentList == null || element.componentList.Count == 0)
        {
            return null;
        }

        return element.GetComponentByIndex<TextMeshProUGUI>(0);
    }

    protected override void OnUnbindCompsAndEvents()
    {
        userDataVer = null;
        quateDescLb = null;
        quateNameLb = null;
    }
}
