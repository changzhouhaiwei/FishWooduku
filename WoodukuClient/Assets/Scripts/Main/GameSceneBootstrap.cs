using FishFramework;
using GameLogic.MainMenu;
using GameLogic.Wooduku;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// GameScene 入口：主相机绑定 UI，隐藏 UICamera，再挂载常驻主页。
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    private const string MainMenuPrefabPath = "Assets/GameRes/Prefabs/TUI/Canvas/UI Main Menu.prefab";
    private const int MainMenuSortingOrder = 50;

    private GameObject _mainMenuInstance;

    private void Start()
    {
        Debug.Log("[GameScene] Entered GameScene.");
        EnsureEventSystem();
        BindMainCameraToUI();
        SpawnMainMenu();
        WoodukuGameplayView.EnsureSpawned();
    }

    private void OnDestroy()
    {
        if (_mainMenuInstance != null)
        {
            Destroy(_mainMenuInstance);
            _mainMenuInstance = null;
        }
    }

    private static void BindMainCameraToUI()
    {
        if (GameModule.UI == null)
        {
            Debug.LogError("[GameScene] GameModule.UI is null.");
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            // 排除 UICamera，取场景中第一台可用相机
            var cameras = FindObjectsOfType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam != null && cam != GameModule.UI.UICamera && cam.enabled)
                {
                    mainCam = cam;
                    break;
                }
            }
        }

        if (mainCam == null)
        {
            Debug.LogError("[GameScene] No main camera found to bind UI.");
            return;
        }

        // Screen Space - Camera 下主相机需能看到 UI；Depth Only 会清掉 UI
        if (mainCam.clearFlags == CameraClearFlags.Depth)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
        }

        GameModule.UI.BindRenderCamera(mainCam, hideUICamera: true);
    }

    private void SpawnMainMenu()
    {
        if (FindObjectOfType<UIMainMenu>() != null)
        {
            Debug.Log("[GameScene] UIMainMenu already present in scene.");
            return;
        }

        if (GameModule.UI == null)
        {
            Debug.LogError("[GameScene] GameModule.UI is null, cannot mount main menu.");
            return;
        }

        var prefab = ResourceModule.LoadAsset<GameObject>(MainMenuPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GameScene] Failed to load main menu prefab: {MainMenuPrefabPath}");
            return;
        }

        RectTransform layer = GameModule.UI.GetLayerRect(PanelLayer.Panel);
        _mainMenuInstance = Instantiate(prefab, layer, false);
        _mainMenuInstance.name = "UI Main Menu";
        ConfigureMainMenuTransform(_mainMenuInstance);
        ConfigureMainMenuCanvas(_mainMenuInstance);
        Debug.Log("[GameScene] Main menu mounted under UI Panel layer.");
    }

    private static void ConfigureMainMenuTransform(GameObject go)
    {
        var rt = go.transform as RectTransform;
        if (rt == null)
        {
            return;
        }

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();
    }

    private static void ConfigureMainMenuCanvas(GameObject go)
    {
        var canvas = go.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = go.AddComponent<Canvas>();
        }

        Camera renderCam = GameModule.UI.GetRenderCamera();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = renderCam;
        canvas.planeDistance = 100f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = MainMenuSortingOrder;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                                         | AdditionalCanvasShaderChannels.Normal
                                         | AdditionalCanvasShaderChannels.Tangent;

        if (go.GetComponent<GraphicRaycaster>() == null)
        {
            go.AddComponent<GraphicRaycaster>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }
}
