namespace GameLogic.Settings
{
    /// <summary>当前版本没有隐私页配置，保留组件以兼容原设置预制体。</summary>
    public sealed class SettingsPrivacyButton : SettingsButtonBase
    {
        public override void Init()
        {
            gameObject.SetActive(false);
        }

        public override void OnClick()
        {
        }
    }
}
