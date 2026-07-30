using System.Collections.Generic;

using I2.Loc;

using TMPro;

using UnityEngine;

using FishFramework;



public class UILanguageDlg : MonoBehaviour

{

    // I2Languages.asset 中 UI_Language：各语言列对应该语言的显示名（English / Español / 简体中文 …）

    private const string LanguageTerm = "UI_Language";



    [SerializeField] CanvasGroup canvasGroupDlg;

    [SerializeField] private Transform languageContentTransform;

    [SerializeField] private TButton closeButton;
    
    [SerializeField] private TButton confirmButton;

    [SerializeField] private TToggle languageItemPrefab;



    private bool built;

    private readonly Dictionary<string, TToggle> languageCodeToToggle = new();

    private bool isApplyingSelection;

    private bool languagePanelWasVisible;

    private string pendingLanguage;



    private void Awake()

    {

        if (closeButton != null)

        {

            closeButton.onClick.AddListener(OnCloseButtonClicked);

        }



        if (confirmButton != null)

        {

            confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        }

    }



    private void OnEnable()

    {

        BuildLanguageList();

        SyncLanguageSelection();

    }



    private void LateUpdate()

    {

        bool visible = IsLanguagePanelVisible();

        if (visible && !languagePanelWasVisible)

            SyncLanguageSelection();

        languagePanelWasVisible = visible;

    }



    private void BuildLanguageList()

    {

        if (built) return;



        if (languageContentTransform == null)

        {

            Debug.LogError("[UILanguageDlg] languageContentTransform is not assigned.");

            return;

        }



        // languageItemPrefab 是 languageContentTransform 的第一个 child，作为模板使用

        if (languageItemPrefab == null && languageContentTransform.childCount > 0)

        {

            languageItemPrefab = languageContentTransform.GetChild(0).GetComponent<TToggle>();

        }



        if (languageItemPrefab == null)

        {

            Debug.LogError("[UILanguageDlg] languageItemPrefab is missing.");

            return;

        }



        // 模板隐藏，仅作为克隆来源

        languageItemPrefab.gameObject.SetActive(false);

        languageCodeToToggle.Clear();



        if (LocalizationManager.Sources.Count == 0)

        {

            LocalizationManager.UpdateSources();

        }



        List<string> languages = LocalizationManager.GetAllLanguages();

        if (languages == null || languages.Count == 0)

        {

            Debug.LogWarning("[UILanguageDlg] No languages found from I2Localization sources.");

            return;

        }



        // I2 的 GetAllLanguages 跨 source 时是按 Name 去重，可能出现：

        //   1) 资产里残留的空白名（例如末尾被 CSV 导入带进来的 "\r"）

        //   2) 同一种语言在不同 source 中 Name 不同、Code 相同（如 "Simplified Chinese" vs "Chinese"）

        // 这里按 Code 进行二次去重，并跳过空白条目，确保每种实际语言只生成一个 toggle。

        var seenCodes = new HashSet<string>();

        foreach (var language in languages)

        {

            if (string.IsNullOrWhiteSpace(language)) continue;



            var code = LocalizationManager.GetLanguageCode(language);

            if (string.IsNullOrEmpty(code)) continue;

            if (!seenCodes.Add(code)) continue;



            var item = Instantiate(languageItemPrefab, languageContentTransform);

            item.gameObject.SetActive(true);

            item.name = "LanguageItem_" + language;



            SetItemLabel(item, language);

            languageCodeToToggle[code] = item;



            var captured = language;

            item.onValueChanged.AddListener(isOn =>

            {

                if (isApplyingSelection || !isOn)

                    return;



                SetPendingLanguage(captured);

            });

        }



        built = true;

        SyncLanguageSelection();

    }



    void SyncLanguageSelection()

    {

        if (!built || languageCodeToToggle.Count == 0)

            return;



        var currentCode = LocalizationManager.GetLanguageCode(LocalizationManager.CurrentLanguage);

        if (string.IsNullOrEmpty(currentCode))

            return;



        isApplyingSelection = true;

        try

        {

            foreach (var kv in languageCodeToToggle)

            {

                var isCurrent = string.Equals(kv.Key, currentCode);

                kv.Value.SetIsOnWithoutNotify(isCurrent);

                // 每个 toggle 都要刷新视觉；仅对选中项调用时，未确认点击过的项 label 字色不会恢复

                kv.Value.OnToggleClick();

            }

        }

        finally

        {

            isApplyingSelection = false;

        }



        pendingLanguage = LocalizationManager.CurrentLanguage;

    }



    bool IsLanguagePanelVisible()

    {

        return canvasGroupDlg != null && canvasGroupDlg.alpha > 0.01f && canvasGroupDlg.interactable;

    }



    private void SetPendingLanguage(string i2LanguageName)

    {

        pendingLanguage = i2LanguageName;

    }



    static void ApplyLanguageSelection(string i2LanguageName)

    {

        LocalizationManager.CurrentLanguage = i2LanguageName;

        var code = LocalizationManager.GetLanguageCode(i2LanguageName);

        if (!string.IsNullOrEmpty(code) && GameModule.Setting != null)

            GameModule.Setting.SetLanguageByCode(code);

    }



    private static void SetItemLabel(TToggle item, string language)

    {

        var labelTransform = item.transform.Find("Label");

        if (labelTransform == null) return;



        // 禁用 Localize，避免切换当前语言时 item 文本被 I2 重新刷新

        var localize = labelTransform.GetComponent<Localize>();

        if (localize != null)

        {

            localize.enabled = false;

        }



        var tmp = labelTransform.GetComponent<TextMeshProUGUI>();

        if (tmp == null) return;



        // 读取 UI_Language 在该语言列下的文本，例如 English="English"、简体中文="简体中文"

        string text = LocalizationManager.GetTermTranslation(LanguageTerm, FixForRTL: false, overrideLanguage: language);

        if (string.IsNullOrEmpty(text))

        {

            text = language;

        }



        tmp.text = text.Trim('\u200B', '\uFEFF').Trim();

    }



    private void OnConfirmButtonClicked()

    {

        if (!string.IsNullOrEmpty(pendingLanguage))

            ApplyLanguageSelection(pendingLanguage);



        OnCloseButtonClicked();

    }



    private void OnCloseButtonClicked()

    {

        SyncLanguageSelection();



        if (canvasGroupDlg == null) return;

        canvasGroupDlg.alpha = 0;

        canvasGroupDlg.blocksRaycasts = false;

        canvasGroupDlg.interactable = false;

        languagePanelWasVisible = false;

    }

}


