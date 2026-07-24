using UnityEditor;
using UnityEngine;

namespace Luban.Editor
{
    public static class LubanMenu
    {
        private const string AssetPath = "Assets/Luban.asset";

        [MenuItem("Luban/Open Export Config", priority = 0)]
        public static void OpenExportConfig()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LubanExportConfig>(AssetPath);
            if (asset == null)
            {
                Debug.LogError($"[Luban] 未找到 {AssetPath}，可通过 Create > Luban > ExportConfig 创建。");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
