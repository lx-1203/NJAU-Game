using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置UI构建器 - 纯代码动态创建设置面板
/// 支持从标题界面和游戏内两种模式打开
/// Canvas sortingOrder: 标题界面=300, 游戏内=250
/// </summary>
public class SettingsUIBuilder : MonoBehaviour
{
    // ========== 单例 ==========
    public static SettingsUIBuilder Instance { get; private set; }

    // ========== 常量 ==========
    private const int CanvasSortOrder_Title = 300;  // 标题界面
    private const int CanvasSortOrder_Game = 250;   // 游戏内

    // 颜色方案（复用项目统一色板）
    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.60f);
    private static readonly Color PanelBgColor = new Color(0.08f, 0.08f, 0.12f, 0.92f);
    private static readonly Color TopBarColor = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color SectionBgColor = new Color(0.06f, 0.06f, 0.10f, 0.50f);
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color BtnNormal = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color BtnHover = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color BtnReset = new Color(0.55f, 0.35f, 0.18f, 0.90f);
    private static readonly Color SliderBg = new Color(1f, 1f, 1f, 0.15f);
    private static readonly Color SliderFill = new Color(0.30f, 0.55f, 0.85f, 1.0f);
    private static readonly Color ToggleBg = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color ToggleCheck = new Color(0.30f, 0.70f, 0.50f, 1.0f);

    // ========== 运行时状态 ==========
    private GameObject rootCanvasObj;
    private Canvas canvas;
    private bool isOpen;
    private bool isTitleScreen;

    // UI 引用
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private Button muteButton;
    private TextMeshProUGUI muteButtonText;
    private Toggle fullscreenToggle;
    private TMP_Dropdown resolutionDropdown;
    private Slider uiScaleSlider;
    private Slider textSpeedSlider;
    private TMP_Dropdown languageDropdown;

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Esc 键关闭面板
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            HideSettings();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== 公共 API ==========

    /// <summary>
    /// 显示设置面板
    /// </summary>
    /// <param name="isTitleScreenMode">是否为标题界面模式（影响sortingOrder）</param>
    public static void ShowSettings(bool isTitleScreenMode)
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("SettingsUIBuilder");
            obj.AddComponent<SettingsUIBuilder>();
        }

        UIFlowGuard.EnsureEventSystem();
        Instance.isTitleScreen = isTitleScreenMode;
        Instance.BuildUI();
        Instance.isOpen = true;
    }

    /// <summary>
    /// 隐藏设置面板
    /// </summary>
    public static void HideSettings()
    {
        if (Instance == null) return;

        Instance.isOpen = false;
        if (Instance.rootCanvasObj != null)
        {
            Destroy(Instance.rootCanvasObj);
            Instance.rootCanvasObj = null;
        }
    }

    public bool IsOpen => isOpen;

    // ========== UI 构建 ==========

    private void BuildUI()
    {
        if (rootCanvasObj != null) Destroy(rootCanvasObj);

        // 创建 Canvas
        rootCanvasObj = new GameObject("SettingsCanvas");
        rootCanvasObj.transform.SetParent(transform, false);

        canvas = rootCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = isTitleScreen ? CanvasSortOrder_Title : CanvasSortOrder_Game;

        CanvasScaler scaler = rootCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        rootCanvasObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = rootCanvasObj.GetComponent<RectTransform>();

        // 半透明遮罩
        GameObject overlay = CreateUI("Overlay", canvasRT);
        Stretch(overlay);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = OverlayColor;
        overlayImg.raycastTarget = true;

        // 点击遮罩关闭
        Button overlayBtn = overlay.AddComponent<Button>();
        overlayBtn.onClick.AddListener(HideSettings);
        ColorBlock cb = overlayBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white;
        cb.pressedColor = Color.white;
        cb.fadeDuration = 0f;
        overlayBtn.colors = cb;

        // 主面板
        GameObject panel = CreateUI("MainPanel", canvasRT);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(600f, 720f);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = PanelBgColor;
        panelBg.raycastTarget = true; // 阻止点击穿透

        // 标题栏
        CreateTitleBar(panelRT);

        // 内容区（ScrollView）
        CreateContentArea(panelRT);

        // 底部按钮栏
        CreateBottomBar(panelRT);

        // 加载当前设置值到UI
        RefreshUIFromSettings();
    }

    private void CreateTitleBar(RectTransform parent)
    {
        GameObject titleBar = CreateUI("TitleBar", parent);
        RectTransform rt = titleBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 60f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = titleBar.AddComponent<Image>();
        bg.color = TopBarColor;

        // 标题文字
        CreateLabel(rt, "TitleText", "设  置", 32, TextGold,
            new Vector2(0.5f, 0.5f), new Vector2(200f, 50f), Vector2.zero);
    }

    private void CreateContentArea(RectTransform parent)
    {
        GameObject scrollView = CreateUI("ScrollView", parent);
        RectTransform scrollRT = scrollView.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = new Vector2(0f, 60f); // 底部留60px给按钮栏
        scrollRT.offsetMax = new Vector2(0f, -60f); // 顶部留60px给标题栏

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewport = CreateUI("Viewport", scrollRT);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        Stretch(viewport);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        GameObject content = CreateUI("Content", viewportRT);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 800f); // 初始高度，会自动调整

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false;
        vlg.childControlWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.spacing = 20f;
        vlg.padding = new RectOffset(30, 30, 20, 20);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRT;
        scroll.content = contentRT;

        // 创建各个设置分组
        CreateAudioSection(contentRT);
        CreateDisplaySection(contentRT);
        CreateGameplaySection(contentRT);
    }

    private void CreateAudioSection(RectTransform parent)
    {
        // 分组标题
        CreateSectionTitle(parent, "【音频设置】");

        // 主音量
        GameObject masterRow = CreateSettingRow(parent, 70f);
        CreateLabel(masterRow.GetComponent<RectTransform>(), "Label", "主音量", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        masterVolumeSlider = CreateSlider(masterRow.GetComponent<RectTransform>(),
            new Vector2(200f, 0f), 0f, 1f, 1.0f);
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        // 静音按钮
        muteButton = CreateIconButton(masterRow.GetComponent<RectTransform>(), "M",
            new Vector2(50f, 50f), new Vector2(430f, 0f));
        muteButton.onClick.AddListener(OnMuteToggled);
        muteButtonText = muteButton.GetComponentInChildren<TextMeshProUGUI>();

        // 音乐音量
        GameObject musicRow = CreateSettingRow(parent, 70f);
        CreateLabel(musicRow.GetComponent<RectTransform>(), "Label", "音乐音量", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        musicVolumeSlider = CreateSlider(musicRow.GetComponent<RectTransform>(),
            new Vector2(200f, 0f), 0f, 1f, 0.7f);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        // 音效音量
        GameObject sfxRow = CreateSettingRow(parent, 70f);
        CreateLabel(sfxRow.GetComponent<RectTransform>(), "Label", "音效音量", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        sfxVolumeSlider = CreateSlider(sfxRow.GetComponent<RectTransform>(),
            new Vector2(200f, 0f), 0f, 1f, 0.8f);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void CreateDisplaySection(RectTransform parent)
    {
        // 分组标题
        CreateSectionTitle(parent, "【显示设置】");

        // 全屏模式
        GameObject fullscreenRow = CreateSettingRow(parent, 70f);
        CreateLabel(fullscreenRow.GetComponent<RectTransform>(), "Label", "全屏模式", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        fullscreenToggle = CreateToggle(fullscreenRow.GetComponent<RectTransform>(),
            new Vector2(380f, 0f), true);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        // 分辨率
        GameObject resRow = CreateSettingRow(parent, 70f);
        CreateLabel(resRow.GetComponent<RectTransform>(), "Label", "分辨率", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        resolutionDropdown = CreateDropdown(resRow.GetComponent<RectTransform>(),
            new Vector2(200f, 0f), new string[] { "1280x720", "1920x1080", "2560x1440", "3840x2160" }, 1);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        // UI缩放
        GameObject scaleRow = CreateSettingRow(parent, 70f);
        CreateLabel(scaleRow.GetComponent<RectTransform>(), "Label", "UI缩放", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        uiScaleSlider = CreateSlider(scaleRow.GetComponent<RectTransform>(),
            new Vector2(200f, 0f), 0.8f, 1.2f, 1.0f);
        uiScaleSlider.onValueChanged.AddListener(OnUIScaleChanged);
    }

    private void CreateGameplaySection(RectTransform parent)
    {
        // 分组标题
        CreateSectionTitle(parent, "【游戏性设置】");

        // 文本速度
        GameObject textSpeedRow = CreateSettingRow(parent, 70f);
        CreateLabel(textSpeedRow.GetComponent<RectTransform>(), "Label", "文本速度", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        textSpeedSlider = CreateSlider(textSpeedRow.GetComponent<RectTransform>(),
            new Vector2(200f, 0f), 0f, 2f, 1f);
        textSpeedSlider.wholeNumbers = true;
        textSpeedSlider.onValueChanged.AddListener(OnTextSpeedChanged);

        // 语言（预留）
        GameObject langRow = CreateSettingRow(parent, 70f);
        CreateLabel(langRow.GetComponent<RectTransform>(), "Label", "语言", 22, TextWhite,
            new Vector2(0f, 0.5f), new Vector2(120f, 40f), new Vector2(60f, 0f));
        languageDropdown = CreateDropdown(langRow.GetComponent<RectTransform>(),
            new Vector2(200f, 0f), new string[] { "中文", "English (开发中)" }, 0);
        languageDropdown.interactable = false; // 暂时禁用
    }

    private void CreateBottomBar(RectTransform parent)
    {
        GameObject bottomBar = CreateUI("BottomBar", parent);
        RectTransform rt = bottomBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, 60f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = bottomBar.AddComponent<Image>();
        bg.color = TopBarColor;

        // 恢复默认按钮
        Button resetBtn = CreateMenuButton(rt, "恢复默认", BtnReset, BtnHover,
            new Vector2(160f, 45f), new Vector2(-220f, 30f));
        resetBtn.onClick.AddListener(OnResetToDefaults);

        // 关闭按钮
        Button closeBtn = CreateMenuButton(rt, "关  闭", BtnNormal, BtnHover,
            new Vector2(160f, 45f), new Vector2(220f, 30f));
        closeBtn.onClick.AddListener(HideSettings);
    }

    // ========== UI 辅助方法 ==========

    private GameObject CreateUI(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        return go;
    }

    private void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text,
        float fontSize, Color color, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject go = CreateUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);

        return tmp;
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;
    }

    private void CreateSectionTitle(RectTransform parent, string title)
    {
        GameObject titleObj = CreateUI("SectionTitle_" + title, parent);
        RectTransform rt = titleObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(540f, 40f);

        TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 24;
        tmp.color = TextGold;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
    }

    private GameObject CreateSettingRow(RectTransform parent, float height)
    {
        GameObject row = CreateUI("SettingRow", parent);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(540f, height);
        return row;
    }

    private Slider CreateSlider(RectTransform parent, Vector2 pos, float min, float max, float value)
    {
        GameObject sliderObj = CreateUI("Slider", parent);
        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.anchorMin = sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRT.pivot = new Vector2(0.5f, 0.5f);
        sliderRT.sizeDelta = new Vector2(280f, 24f);
        sliderRT.anchoredPosition = pos;

        Image bg = sliderObj.AddComponent<Image>();
        bg.color = SliderBg;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.direction = Slider.Direction.LeftToRight;

        // Fill Area
        GameObject fillAreaObj = CreateUI("Fill Area", sliderRT);
        RectTransform fillAreaRT = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(10f, 4f);
        fillAreaRT.offsetMax = new Vector2(-10f, -4f);

        GameObject fillObj = CreateUI("Fill", fillAreaRT);
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        Stretch(fillObj);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = SliderFill;

        // Handle
        GameObject handleAreaObj = CreateUI("Handle Area", sliderRT);
        RectTransform handleAreaRT = handleAreaObj.GetComponent<RectTransform>();
        Stretch(handleAreaObj);

        GameObject handleObj = CreateUI("Handle", handleAreaRT);
        RectTransform handleRT = handleObj.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20f, 20f);
        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.fillRect = fillRT;
        slider.handleRect = handleRT;

        return slider;
    }

    private Toggle CreateToggle(RectTransform parent, Vector2 pos, bool isOn)
    {
        GameObject toggleObj = CreateUI("Toggle", parent);
        RectTransform toggleRT = toggleObj.GetComponent<RectTransform>();
        toggleRT.anchorMin = toggleRT.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRT.pivot = new Vector2(0.5f, 0.5f);
        toggleRT.sizeDelta = new Vector2(50f, 50f);
        toggleRT.anchoredPosition = pos;

        Image bg = toggleObj.AddComponent<Image>();
        bg.color = ToggleBg;

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.isOn = isOn;

        // Checkmark
        GameObject checkObj = CreateUI("Checkmark", toggleRT);
        RectTransform checkRT = checkObj.GetComponent<RectTransform>();
        checkRT.anchorMin = checkRT.anchorMax = new Vector2(0.5f, 0.5f);
        checkRT.pivot = new Vector2(0.5f, 0.5f);
        checkRT.sizeDelta = new Vector2(30f, 30f);
        checkRT.anchoredPosition = Vector2.zero;

        Image checkImage = checkObj.AddComponent<Image>();
        checkImage.color = ToggleCheck;
        toggle.graphic = checkImage;

        return toggle;
    }

    private TMP_Dropdown CreateDropdown(RectTransform parent, Vector2 pos, string[] options, int defaultIndex)
    {
        GameObject dropdownObj = CreateUI("Dropdown", parent);
        RectTransform dropdownRT = dropdownObj.GetComponent<RectTransform>();
        dropdownRT.anchorMin = dropdownRT.anchorMax = new Vector2(0.5f, 0.5f);
        dropdownRT.pivot = new Vector2(0.5f, 0.5f);
        dropdownRT.sizeDelta = new Vector2(280f, 40f);
        dropdownRT.anchoredPosition = pos;

        Image bg = dropdownObj.AddComponent<Image>();
        bg.color = SliderBg;

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(options));
        dropdown.value = defaultIndex;

        // Label
        GameObject labelObj = CreateUI("Label", dropdownRT);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10f, 0f);
        labelRT.offsetMax = new Vector2(-30f, 0f);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = 18;
        labelText.color = TextWhite;
        labelText.alignment = TextAlignmentOptions.Left;
        ApplyFont(labelText);
        dropdown.captionText = labelText;

        // Arrow
        GameObject arrowObj = CreateUI("Arrow", dropdownRT);
        RectTransform arrowRT = arrowObj.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1f, 0.5f);
        arrowRT.anchorMax = new Vector2(1f, 0.5f);
        arrowRT.pivot = new Vector2(0.5f, 0.5f);
        arrowRT.sizeDelta = new Vector2(20f, 20f);
        arrowRT.anchoredPosition = new Vector2(-15f, 0f);

        TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.fontSize = 16;
        arrowText.color = TextWhite;
        arrowText.alignment = TextAlignmentOptions.Center;

        // Template (简化版，使用默认)
        GameObject templateObj = CreateUI("Template", dropdownRT);
        RectTransform templateRT = templateObj.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0f, 0f);
        templateRT.anchorMax = new Vector2(1f, 0f);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.sizeDelta = new Vector2(0f, 150f);
        templateRT.anchoredPosition = new Vector2(0f, 2f);
        templateObj.SetActive(false);

        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = PanelBgColor;

        ScrollRect templateScroll = templateObj.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;

        GameObject viewportObj = CreateUI("Viewport", templateRT);
        Stretch(viewportObj);
        viewportObj.AddComponent<Mask>();
        viewportObj.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        GameObject contentObj = CreateUI("Content", viewportObj.GetComponent<RectTransform>());
        RectTransform contentRT = contentObj.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 0f);

        GameObject itemObj = CreateUI("Item", contentRT);
        RectTransform itemRT = itemObj.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0f, 30f);

        Toggle itemToggle = itemObj.AddComponent<Toggle>();
        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        itemToggle.targetGraphic = itemBg;

        GameObject itemLabelObj = CreateUI("Item Label", itemRT);
        Stretch(itemLabelObj);
        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.fontSize = 18;
        itemLabel.color = TextWhite;
        itemLabel.alignment = TextAlignmentOptions.Left;
        itemLabel.margin = new Vector4(10f, 0f, 10f, 0f);
        ApplyFont(itemLabel);

        dropdown.template = templateRT;
        dropdown.itemText = itemLabel;
        templateScroll.content = contentRT;
        templateScroll.viewport = viewportObj.GetComponent<RectTransform>();

        return dropdown;
    }

    private Button CreateIconButton(RectTransform parent, string icon, Vector2 size, Vector2 pos)
    {
        GameObject btnObj = CreateUI("IconButton", parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.20f, 0.8f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        TextMeshProUGUI text = CreateLabel(rt, "Icon", icon, 28, TextWhite,
            new Vector2(0.5f, 0.5f), size, Vector2.zero);

        return btn;
    }

    private Button CreateMenuButton(RectTransform parent, string label, Color normalColor,
        Color hoverColor, Vector2 size, Vector2 pos)
    {
        GameObject btnObj = CreateUI("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(
            hoverColor.r / Mathf.Max(normalColor.r, 0.01f),
            hoverColor.g / Mathf.Max(normalColor.g, 0.01f),
            hoverColor.b / Mathf.Max(normalColor.b, 0.01f),
            1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        TextMeshProUGUI text = CreateLabel(rt, "Label", label, 24, TextWhite,
            new Vector2(0.5f, 0.5f), size, Vector2.zero);

        return btn;
    }

    // ========== 事件处理 ==========

    private void OnMasterVolumeChanged(float value)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.CurrentSettings.masterVolume = value;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplyAllSettings();
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.CurrentSettings.musicVolume = value;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplyAllSettings();
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.CurrentSettings.sfxVolume = value;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplyAllSettings();
    }

    private void OnMuteToggled()
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.CurrentSettings.isMuted = !SettingsManager.Instance.CurrentSettings.isMuted;

        // 更新按钮图标
        if (muteButtonText != null)
        {
            muteButtonText.text = SettingsManager.Instance.CurrentSettings.isMuted ? "X" : "M";
        }

        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplyAllSettings();
    }

    private void OnFullscreenChanged(bool value)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.CurrentSettings.fullscreen = value;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplyAllSettings();
    }

    private void OnResolutionChanged(int index)
    {
        if (SettingsManager.Instance == null) return;

        // 解析分辨率字符串
        string[] resolutions = { "1280x720", "1920x1080", "2560x1440", "3840x2160" };
        if (index < 0 || index >= resolutions.Length) return;

        string[] parts = resolutions[index].Split('x');
        if (parts.Length == 2)
        {
            SettingsManager.Instance.CurrentSettings.resolutionWidth = int.Parse(parts[0]);
            SettingsManager.Instance.CurrentSettings.resolutionHeight = int.Parse(parts[1]);
            SettingsManager.Instance.SaveSettings();
            SettingsManager.Instance.ApplyAllSettings();
        }
    }

    private void OnUIScaleChanged(float value)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.CurrentSettings.uiScale = value;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplyAllSettings();
    }

    private void OnTextSpeedChanged(float value)
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.CurrentSettings.textSpeed = (int)value;
        SettingsManager.Instance.SaveSettings();
        SettingsManager.Instance.ApplyAllSettings();
    }

    private void OnResetToDefaults()
    {
        // 显示确认对话框
        CreateConfirmDialog("确定要恢复所有设置到默认值吗？", () =>
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ResetToDefaults();
                RefreshUIFromSettings();
            }
        });
    }

    // ========== UI 刷新 ==========

    private void RefreshUIFromSettings()
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.CurrentSettings == null) return;

        SettingsData settings = SettingsManager.Instance.CurrentSettings;

        // 音频
        if (masterVolumeSlider != null) masterVolumeSlider.value = settings.masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = settings.musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = settings.sfxVolume;
        if (muteButtonText != null) muteButtonText.text = settings.isMuted ? "X" : "M";

        // 显示
        if (fullscreenToggle != null) fullscreenToggle.isOn = settings.fullscreen;
        if (uiScaleSlider != null) uiScaleSlider.value = settings.uiScale;

        // 分辨率下拉菜单
        if (resolutionDropdown != null)
        {
            string currentRes = $"{settings.resolutionWidth}x{settings.resolutionHeight}";
            string[] resolutions = { "1280x720", "1920x1080", "2560x1440", "3840x2160" };
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i] == currentRes)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
        }

        // 游戏性
        if (textSpeedSlider != null) textSpeedSlider.value = settings.textSpeed;
        if (languageDropdown != null) languageDropdown.value = settings.language;
    }

    // ========== 确认对话框 ==========

    private void CreateConfirmDialog(string message, Action onConfirm)
    {
        // 创建对话框Canvas（比设置面板高1层）
        GameObject dialogCanvasObj = new GameObject("ConfirmDialogCanvas");
        dialogCanvasObj.transform.SetParent(transform, false);

        Canvas dialogCanvas = dialogCanvasObj.AddComponent<Canvas>();
        dialogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogCanvas.sortingOrder = canvas.sortingOrder + 1;

        CanvasScaler scaler = dialogCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        dialogCanvasObj.AddComponent<GraphicRaycaster>();
        RectTransform dialogCanvasRT = dialogCanvasObj.GetComponent<RectTransform>();

        // 半透明遮罩
        GameObject overlay = CreateUI("Overlay", dialogCanvasRT);
        Stretch(overlay);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.70f);

        // 对话框面板
        GameObject panel = CreateUI("DialogPanel", dialogCanvasRT);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(400f, 200f);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = PanelBgColor;

        // 提示文字
        CreateLabel(panelRT, "Message", message, 22, TextWhite,
            new Vector2(0.5f, 0.5f), new Vector2(360f, 80f), new Vector2(0f, 20f));

        // 确认按钮
        Button confirmBtn = CreateMenuButton(panelRT, "确  认", BtnNormal, BtnHover,
            new Vector2(140f, 45f), new Vector2(-80f, -50f));
        confirmBtn.onClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            Destroy(dialogCanvasObj);
        });

        // 取消按钮
        Button cancelBtn = CreateMenuButton(panelRT, "取  消", new Color(0.3f, 0.3f, 0.3f, 0.9f), BtnHover,
            new Vector2(140f, 45f), new Vector2(80f, -50f));
        cancelBtn.onClick.AddListener(() =>
        {
            Destroy(dialogCanvasObj);
        });
    }
}
