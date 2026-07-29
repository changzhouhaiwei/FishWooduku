using GameLogic.Wooduku;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Wooduku.LevelEditor
{
    /// <summary>
    /// 一次性生成局内 UI Prefab：自定义窗口 / Wooduku 生成局内 Prefab
    /// </summary>
    public static class WoodukuGamePrefabBuilder
    {
        private const string Folder = "Assets/GameRes/Prefabs/Wooduku";
        private const string Path = Folder + "/UIWoodukuGame.prefab";

        [MenuItem("自定义窗口/Wooduku 生成局内 Prefab")]
        public static void Build()
        {
            EnsureFolder();

            var root = new GameObject("UIWoodukuGame", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(WoodukuGameplayView));
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);
            var rootImg = root.GetComponent<Image>();
            rootImg.color = new Color(0.96f, 0.93f, 0.88f, 1f);
            rootImg.raycastTarget = true;

            var top = Make("TopBar", root.transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -120), Vector2.zero);
            var topImg = top.gameObject.AddComponent<Image>();
            topImg.color = new Color(1f, 1f, 1f, 0.35f);
            topImg.raycastTarget = false;

            var backGo = Make("BackButton", top, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(24, -40), new Vector2(104, 40));
            var backImg = backGo.gameObject.AddComponent<Image>();
            backImg.color = new Color(0.85f, 0.75f, 0.65f, 1f);
            var backBtn = backGo.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            MakeTmp("BackLabel", backGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "返回", 28);

            MakeTmp("LevelLabel", top, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-100, -30), new Vector2(100, 30), "关卡 1", 32);
            MakeTmp("ProgressLabel", top, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-160, -30), new Vector2(-40, 30), "0/6", 32);

            var board = Make("BoardRoot", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-320, -280), new Vector2(320, 360));
            var boardImg = board.gameObject.AddComponent<Image>();
            boardImg.color = new Color(1f, 1f, 1f, 0.05f);
            boardImg.raycastTarget = true;
            var grid = board.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.spacing = new Vector2(4, 4);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var win = Make("WinOverlay", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var winImg = win.gameObject.AddComponent<Image>();
            winImg.color = new Color(0f, 0f, 0f, 0.55f);
            winImg.raycastTarget = true;
            win.gameObject.SetActive(false);

            var winPanel = Make("WinPanel", win, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-220, -140), new Vector2(220, 140));
            var winPanelImg = winPanel.gameObject.AddComponent<Image>();
            winPanelImg.color = new Color(1f, 0.97f, 0.92f, 1f);
            MakeTmp("WinTitle", winPanel, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f),
                new Vector2(-180, -40), new Vector2(180, 40), "通关！", 42);

            var winBack = Make("WinBackButton", winPanel, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f),
                new Vector2(-200, -36), new Vector2(-20, 36));
            var winBackImg = winBack.gameObject.AddComponent<Image>();
            winBackImg.color = new Color(0.85f, 0.7f, 0.45f, 1f);
            var winBackBtn = winBack.gameObject.AddComponent<Button>();
            winBackBtn.targetGraphic = winBackImg;
            MakeTmp("WinBackLabel", winBack, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "返回主页", 30);

            var winNext = Make("WinNextButton", winPanel, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f),
                new Vector2(20, -36), new Vector2(200, 36));
            var winNextImg = winNext.gameObject.AddComponent<Image>();
            winNextImg.color = new Color(0.65f, 0.8f, 0.45f, 1f);
            var winNextBtn = winNext.gameObject.AddComponent<Button>();
            winNextBtn.targetGraphic = winNextImg;
            MakeTmp("WinNextLabel", winNext, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "下一关", 30);

            PrefabUtility.SaveAsPrefabAsset(root, Path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Wooduku] Prefab saved: " + Path);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GameRes/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/GameRes", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/GameRes/Prefabs", "Wooduku");
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static RectTransform Make(string name, Transform parent, Vector2 amin, Vector2 amax,
            Vector2 omin, Vector2 omax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = amin;
            rt.anchorMax = amax;
            rt.offsetMin = omin;
            rt.offsetMax = omax;
            return rt;
        }

        private static TextMeshProUGUI MakeTmp(string name, Transform parent, Vector2 amin, Vector2 amax,
            Vector2 omin, Vector2 omax, string text, float size)
        {
            var rt = Make(name, parent, amin, amax, omin, omax);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 0.15f, 0.1f, 1f);
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
