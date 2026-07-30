using UnityEditor;

namespace FishFramework.EditorTools
{
    /// <summary>
    /// 为未配置 Android 平台覆盖的新导入图片设置默认压缩格式。
    /// 已手动配置过平台格式的图片不会被改写。
    /// </summary>
    internal sealed class TextureImportSettings : AssetPostprocessor
    {
        private const string AndroidPlatform = "Android";

        private void OnPreprocessTexture()
        {
            var textureImporter = (TextureImporter)assetImporter;
            var androidSettings = textureImporter.GetPlatformTextureSettings(AndroidPlatform);

            if (androidSettings.overridden)
            {
                return;
            }

            androidSettings.overridden = true;
            androidSettings.format = TextureImporterFormat.ASTC_4x4;
            textureImporter.SetPlatformTextureSettings(androidSettings);
        }
    }
}
