using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 对话 UI 构建器 —— 纯代码创建对话面板与选项面板
/// 参考 HUDBuilder 的 Builder/Manager 分离模式
/// 布局参考：
/// ┌──────────────────────────────────────────────────┐
/// │                                                  │
/// │          ┌──────────────────────────┐             │
/// │          │      选项面板 (4按钮)     │             │
/// │          └──────────────────────────┘             │
/// │  ┌──────────────────────────────────────────┐    │
/// │  │ [头像]  名字                              │    │
/// │  │         ──────────────────────────        │    │
/// │  │         对话内容...              继续提示  │    │
/// │  └──────────────────────────────────────────┘    │
/// └──────────────────────────────────────────────────┘
/// </summary>
public class DialogueUIBuilder : MonoBehaviour
{
    // ========== 构建产物引用（供 DialogueSystem 使用） ==========

    [HideInInspector] public Canvas dialogueCanvas;
    [HideInInspector] public GameObject dialoguePanel;
    [HideInInspector] public Image portraitImage;
    [HideInInspector] public GameObject portraitContainer;  // 头像容器（用于整体显隐）
    [HideInInspector] public TextMeshProUGUI nameText;
    [HideInInspector] public TextMeshProUGUI contentText;
    [HideInInspector] public TextMeshProUGUI hintText;
    [HideInInspector] public GameObject nameContainer;      // 名字+装饰线容器（用于旁白时隐藏）

    // 选项面板
    [HideInInspector] public GameObject choicePanel;
    [HideInInspector] public Button[] choiceButtons = new Button[4];
    [HideInInspector] public TextMeshProUGUI[] choiceTexts = new TextMeshProUGUI[4];
    [HideInInspector] public TextMeshProUGUI[] choiceHints = new TextMeshProUGUI[4];

    // ========== 颜色方案 ==========

    private static readonly Color PanelBgColor           = new Color(0.05f, 0.05f, 0.12f, 0.92f);
    private static readonly Color NameColor              = new Color(0.4f, 0.85f, 1f);
    private static readonly Color ContentColor           = Color.white;
    private static readonly Color HintColor              = new Color(0.7f, 0.7f, 0.7f, 0.8f);
    private static readonly Color ChoiceNormalColor       = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ChoiceHoverColor        = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color ChoicePressedColor      = new Color(0.15f, 0.25f, 0.50f, 1.0f);
    private static readonly Color ChoiceDisabledColor     = new Color(0.25f, 0.25f, 0.30f, 0.8f);
    private static readonly Color ChoiceDisabledTextColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color ChoicePanelBgColor      = new Color(0.05f, 0.05f, 0.12f, 0.85f);
    private static readonly Color DecoLineColor          = new Color(0.4f, 0.85f, 1f, 0.3f);
    private static readonly Color PortraitPlaceholderColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
    private static readonly Color TextWhite              = new Color(0.92f, 0.92f, 0.92f);

    // ========== 主入口 ==========

    /// <summary>
    /// 构建整个对话 UI（对话面板 + 选项面板），默认隐藏
    /// </summary>
    public void BuildDialogueUI()
    {
        CreateCanvas();
        CreateDialoguePanel();
        CreateChoicePanel();

        // 默认隐藏
        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
    }

    // ====================================================================
    //  Canvas
    // ====================================================================

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("DialogueCanvas");
        canvasObj.transform.SetParent(transform, false);

        dialogueCanvas = canvasObj.AddComponent<Canvas>();
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 200; // 对话在 HUD(100) 之上

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // ====================================================================
    //  对话面板
    // ====================================================================

    private void CreateDialoguePanel()
    {
        // ===== 对话面板（底部） =====
        dialoguePanel = CreatePanel("DialoguePanel", dialogueCanvas.transform, PanelBgColor);
        RectTransform panelRt = dialoguePanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.05f, 0.02f);
        panelRt.anchorMax = new Vector2(0.95f, 0.32f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        TMP_FontAsset chineseFont = GetChineseFont();

        // ===== 头像容器 =====
        portraitContainer = new GameObject("PortraitContainer");
        portraitContainer.transform.SetParent(dialoguePanel.transform, false);

        RectTransform pcRt = portraitContainer.AddComponent<RectTransform>();
        pcRt.anchorMin = new Vector2(0, 0.1f);
        pcRt.anchorMax = new Vector2(0, 0.9f);
        pcRt.offsetMin = new Vector2(15, 0);
        pcRt.offsetMax = new Vector2(15, 0);
        pcRt.sizeDelta = new Vector2(130, 0);
        pcRt.anchoredPosition = new Vector2(80, 0);

        // 头像 Image
        GameObject portraitObj = new GameObject("PortraitImage");
        portraitObj.transform.SetParent(portraitContainer.transform, false);

        RectTransform pRt = portraitObj.AddComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;

        portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.color = PortraitPlaceholderColor;
        portraitImage.preserveAspect = true;

        // ===== 名字容器（名字 + 装饰线，旁白时可整体隐藏） =====
        nameContainer = new GameObject("NameContainer");
        nameContainer.transform.SetParent(dialoguePanel.transform, false);

        RectTransform ncRt = nameContainer.AddComponent<RectTransform>();
        ncRt.anchorMin = new Vector2(0.12f, 0.73f);
        ncRt.anchorMax = new Vector2(0.95f, 0.95f);
        ncRt.offsetMin = Vector2.zero;
        ncRt.offsetMax = Vector2.zero;

        // NPC 名字
        nameText = CreateTMPText(nameContainer.transform, "NameText",
            new Vector2(0, 0.1f), new Vector2(0.6f, 1f), 30f);
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = NameColor;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        if (chineseFont != null) nameText.font = chineseFont;

        // 名字底部装饰线
        GameObject lineObj = CreatePanel("NameLine", nameContainer.transform, DecoLineColor);
        RectTransform lineRt = lineObj.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0, 0);
        lineRt.anchorMax = new Vector2(1, 0.05f);
        lineRt.offsetMin = Vector2.zero;
        lineRt.offsetMax = Vector2.zero;

        // ===== 对话内容 =====
        contentText = CreateTMPText(dialoguePanel.transform, "ContentText",
            new Vector2(0.12f, 0.08f), new Vector2(0.95f, 0.7f), 26f);
        contentText.color = ContentColor;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.lineSpacing = 10f;
        if (chineseFont != null) contentText.font = chineseFont;

        // ===== 底部提示文字 =====
        hintText = CreateTMPText(dialoguePanel.transform, "HintText",
            new Vector2(0.7f, 0.02f), new Vector2(0.98f, 0.15f), 18f);
        hintText.color = HintColor;
        hintText.alignment = TextAlignmentOptions.MidlineRight;
        hintText.text = "按 空格键 继续...";
        if (chineseFont != null) hintText.font = chineseFont;
    }

    // ====================================================================
    //  选项面板
    // ====================================================================

    private void CreateChoicePanel()
    {
        // 选项面板 —— 锚定在对话面板上方
        choicePanel = CreatePanel("ChoicePanel", dialogueCanvas.transform, ChoicePanelBgColor);
        RectTransform cpRt = choicePanel.GetComponent<RectTransform>();
        cpRt.anchorMin = new Vector2(0.15f, 0.34f);
        cpRt.anchorMax = new Vector2(0.85f, 0.75f);
        cpRt.offsetMin = Vector2.zero;
        cpRt.offsetMax = Vector2.zero;

        // 垂直布局
        VerticalLayoutGroup vlg = choicePanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 预创建 4 个选项按钮
        for (int i = 0; i < 4; i++)
        {
            CreateChoiceButton(i);
        }
    }

    /// <summary>
    /// 创建单个选项按钮（内含主文字 + 条件提示）
    /// </summary>
    private void CreateChoiceButton(int index)
    {
        TMP_FontAsset chineseFont = GetChineseFont();

        // ----- 按钮对象 -----
        GameObject btnObj = new GameObject($"ChoiceButton_{index}");
        btnObj.transform.SetParent(choicePanel.transform, false);

        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(0, 55f); // 高度 55px，宽度由布局 stretch

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = ChoiceNormalColor;

        Button btn = btnObj.AddComponent<Button>();

        // 按钮颜色过渡（同 HUDBuilder）
        ColorBlock cb = btn.colors;
        cb.normalColor = ChoiceNormalColor;
        cb.highlightedColor = ChoiceHoverColor;
        cb.pressedColor = ChoicePressedColor;
        cb.selectedColor = ChoiceNormalColor;
        cb.disabledColor = ChoiceDisabledColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // CanvasGroup（用于灰显控制）
        btnObj.AddComponent<CanvasGroup>();

        // ----- 按钮内部垂直布局 -----
        VerticalLayoutGroup innerVlg = btnObj.AddComponent<VerticalLayoutGroup>();
        innerVlg.spacing = 2f;
        innerVlg.padding = new RectOffset(10, 10, 4, 4);
        innerVlg.childAlignment = TextAnchor.MiddleCenter;
        innerVlg.childControlWidth = true;
        innerVlg.childControlHeight = false;
        innerVlg.childForceExpandWidth = true;
        innerVlg.childForceExpandHeight = false;

        // ----- 主文字 -----
        GameObject mainTextObj = new GameObject("ChoiceText");
        mainTextObj.transform.SetParent(btnObj.transform, false);

        RectTransform mtRt = mainTextObj.AddComponent<RectTransform>();
        mtRt.sizeDelta = new Vector2(0, 28f);

        TextMeshProUGUI mainTmp = mainTextObj.AddComponent<TextMeshProUGUI>();
        mainTmp.fontSize = 22f;
        mainTmp.color = ContentColor;
        mainTmp.alignment = TextAlignmentOptions.Center;
        mainTmp.enableWordWrapping = true;
        mainTmp.overflowMode = TextOverflowModes.Ellipsis;
        if (chineseFont != null) mainTmp.font = chineseFont;

        choiceTexts[index] = mainTmp;

        // ----- 条件提示（默认隐藏） -----
        GameObject hintObj = new GameObject("ChoiceHint");
        hintObj.transform.SetParent(btnObj.transform, false);

        RectTransform htRt = hintObj.AddComponent<RectTransform>();
        htRt.sizeDelta = new Vector2(0, 18f);

        TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
        hintTmp.fontSize = 14f;
        hintTmp.color = ChoiceDisabledTextColor;
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.enableWordWrapping = true;
        hintTmp.overflowMode = TextOverflowModes.Ellipsis;
        if (chineseFont != null) hintTmp.font = chineseFont;

        hintObj.SetActive(false); // 默认隐藏

        choiceHints[index] = hintTmp;

        // ----- 存储引用 -----
        choiceButtons[index] = btn;
    }

    // ====================================================================
    //  工具方法
    // ====================================================================

    /// <summary>创建带背景色的面板</summary>
    private GameObject CreatePanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();

        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;

        return panel;
    }

    /// <summary>
    /// 创建 TMP 文本组件（锚点模式，与 DialogueSystem 的 CreateTMPText 保持一致）
    /// </summary>
    private TextMeshProUGUI CreateTMPText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, float fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.enableWordWrapping = true;

        return tmp;
    }

    /// <summary>
    /// 获取中文字体（从 FontManager 自动获取，与 DialogueSystem 一致）
    /// </summary>
    private TMP_FontAsset GetChineseFont()
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            return FontManager.Instance.ChineseFont;
        }
        return null; // 会使用 TMP 默认字体 + fallback
    }
}
