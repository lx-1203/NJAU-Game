using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pure-code settings panel builder.
/// </summary>
public class SettingsUIBuilder : MonoBehaviour
{
    private enum SettingsTab
    {
        Gameplay,
        Display,
        Audio
    }

    public static SettingsUIBuilder Instance { get; private set; }

    private const int CanvasSortOrderTitle = 300;
    private const int CanvasSortOrderGame = 250;

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.52f);
    private static readonly Color BookOuterColor = new Color32(0xF4, 0xE3, 0xBE, 0xFF);
    private static readonly Color BookInnerColor = new Color32(0xF7, 0xF1, 0xE3, 0xFF);
    private static readonly Color PaperColor = new Color32(0xF4, 0xEE, 0xDD, 0xFF);
    private static readonly Color SectionColor = new Color32(0xD9, 0xA5, 0x6D, 0xFF);
    private static readonly Color SectionFadeColor = new Color32(0xF0, 0xE8, 0xD2, 0x00);
    private static readonly Color RowColor = new Color32(0xF3, 0xE9, 0xD3, 0xFF);
    private static readonly Color ValueBoxColor = new Color32(0xFF, 0xFC, 0xF8, 0xFF);
    private static readonly Color ValueBorderColor = new Color32(0xD9, 0xC9, 0xB3, 0xFF);
    private static readonly Color AccentColor = new Color32(0xD8, 0x99, 0x55, 0xFF);
    private static readonly Color AccentStrongColor = new Color32(0xF3, 0xBF, 0x57, 0xFF);
    private static readonly Color TextPrimary = new Color32(0x6D, 0x39, 0x22, 0xFF);
    private static readonly Color TextSecondary = new Color32(0x9D, 0x7A, 0x60, 0xFF);
    private static readonly Color TabActiveColor = new Color32(0xFF, 0xF0, 0xB5, 0xFF);
    private static readonly Color TabInactiveColor = new Color32(0xF2, 0xF0, 0xEB, 0xFF);
    private static readonly Color SliderTrackColor = new Color32(0xE5, 0xD5, 0xBC, 0xFF);
    private static readonly Color SliderFillColor = new Color32(0xF0, 0xB6, 0x63, 0xFF);

    private GameObject rootCanvasObj;
    private Canvas canvas;
    private bool isOpen;
    private bool isTitleScreen;
    private SettingsTab currentTab = SettingsTab.Gameplay;

    private SettingsData draftSettings;
    private List<string> resolutionOptions = new List<string>();

    private TextMeshProUGUI statusText;
    private TextMeshProUGUI languageValueText;
    private TextMeshProUGUI textSpeedValueText;
    private TextMeshProUGUI autoPlayValueText;
    private TextMeshProUGUI skipModeValueText;
    private TextMeshProUGUI fastForwardValueText;
    private TextMeshProUGUI dialogueSkipHelpText;
    private TextMeshProUGUI dialogueHistoryValueText;
    private TextMeshProUGUI fullscreenValueText;
    private TextMeshProUGUI resolutionValueText;
    private TextMeshProUGUI uiScaleValueText;
    private TextMeshProUGUI masterVolumeValueText;
    private TextMeshProUGUI musicVolumeValueText;
    private TextMeshProUGUI sfxVolumeValueText;
    private TextMeshProUGUI muteValueText;

    private Slider uiScaleSlider;
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;

    private Button gameplayTabButton;
    private Button displayTabButton;
    private Button audioTabButton;
    private Image gameplayTabImage;
    private Image displayTabImage;
    private Image audioTabImage;
    private TextMeshProUGUI gameplayTabText;
    private TextMeshProUGUI displayTabText;
    private TextMeshProUGUI audioTabText;

    private GameObject gameplayPage;
    private GameObject displayPage;
    private GameObject audioPage;

    private GameObject confirmDialogCanvasObj;
    private Action confirmDialogConfirmAction;
    private Action confirmDialogCancelAction;
    private Button confirmDialogConfirmButton;

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
        if (!isOpen)
        {
            return;
        }

        if (confirmDialogCanvasObj != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                confirmDialogCancelAction?.Invoke();
                return;
            }

            if (UIInputHelper.IsConfirmPressed())
            {
                UIInputHelper.TryClick(confirmDialogConfirmButton);
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TryClose();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

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

    public static void HideSettings()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.isOpen = false;
        Instance.ClearConfirmDialog();

        if (Instance.rootCanvasObj != null)
        {
            Destroy(Instance.rootCanvasObj);
            Instance.rootCanvasObj = null;
        }
    }

    public bool IsOpen
    {
        get { return isOpen; }
    }

    private void BuildUI()
    {
        if (rootCanvasObj != null)
        {
            Destroy(rootCanvasObj);
        }

        if (SettingsManager.Instance == null)
        {
            return;
        }

        draftSettings = SettingsManager.Instance.CurrentSettings != null
            ? SettingsManager.Instance.CurrentSettings.Clone()
            : new SettingsData();

        resolutionOptions = BuildResolutionOptions(draftSettings);

        rootCanvasObj = new GameObject("SettingsCanvas");
        rootCanvasObj.transform.SetParent(transform, false);

        canvas = rootCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = isTitleScreen ? CanvasSortOrderTitle : CanvasSortOrderGame;

        CanvasScaler scaler = rootCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        rootCanvasObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = rootCanvasObj.GetComponent<RectTransform>();

        GameObject overlay = CreateUI("Overlay", canvasRT);
        Stretch(overlay);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = OverlayColor;
        overlayImage.raycastTarget = true;

        Button overlayButton = overlay.AddComponent<Button>();
        overlayButton.onClick.AddListener(TryClose);
        ColorBlock overlayColors = overlayButton.colors;
        overlayColors.normalColor = Color.white;
        overlayColors.highlightedColor = Color.white;
        overlayColors.pressedColor = Color.white;
        overlayColors.fadeDuration = 0f;
        overlayButton.colors = overlayColors;

        RectTransform panelRT = CreateBookPanel(canvasRT);
        CreateHeader(panelRT);
        CreateTabs(panelRT);
        CreateContentArea(panelRT);
        CreateBottomBar(panelRT);

        RefreshAllUI();
    }

    private RectTransform CreateBookPanel(RectTransform parent)
    {
        GameObject panel = CreateUI("BookPanel", parent);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1750f, 920f);
        panelRT.anchoredPosition = new Vector2(0f, -10f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = BookOuterColor;

        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.55f, 0.38f, 0.20f, 0.28f);
        panelOutline.effectDistance = new Vector2(4f, -4f);

        GameObject inner = CreateUI("InnerPaper", panelRT);
        RectTransform innerRT = inner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(18f, 18f);
        innerRT.offsetMax = new Vector2(-18f, -18f);
        inner.AddComponent<Image>().color = BookInnerColor;

        GameObject contentPaper = CreateUI("ContentPaper", innerRT);
        RectTransform contentRT = contentPaper.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.offsetMin = new Vector2(46f, 72f);
        contentRT.offsetMax = new Vector2(-28f, -24f);
        contentPaper.AddComponent<Image>().color = PaperColor;

        panelRT = contentRT;

        CreateBinderRing(innerRT, new Vector2(-36f, 0.86f));
        CreateBinderRing(innerRT, new Vector2(-36f, 0.48f));
        CreateBinderRing(innerRT, new Vector2(-36f, 0.08f));

        return panelRT;
    }

    private void CreateBinderRing(RectTransform parent, Vector2 anchored)
    {
        GameObject ring = CreateUI("Ring", parent);
        RectTransform rt = ring.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchored;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(52f, 52f);
        Image image = ring.AddComponent<Image>();
        image.color = new Color(0.74f, 0.66f, 0.56f, 1f);

        GameObject hole = CreateUI("Hole", rt);
        RectTransform holeRT = hole.GetComponent<RectTransform>();
        holeRT.anchorMin = holeRT.anchorMax = new Vector2(0.5f, 0.5f);
        holeRT.sizeDelta = new Vector2(26f, 26f);
        hole.AddComponent<Image>().color = PaperColor;
    }

    private void CreateHeader(RectTransform parent)
    {
        CreateLabel(parent, "TitleIcon", "\u8bbe\u7f6e", 52f, new Color32(0x88, 0x7B, 0xE3, 0xFF),
            new Vector2(0f, 1f), new Vector2(240f, 72f), new Vector2(-54f, 24f), TextAlignmentOptions.Left);

        CreateLabel(parent, "TitleWrench", "\u2699", 30f, AccentColor,
            new Vector2(0f, 1f), new Vector2(52f, 52f), new Vector2(-115f, -6f), TextAlignmentOptions.Center);
    }

    private void CreateTabs(RectTransform parent)
    {
        RectTransform tabsRoot = CreateUI("TabsRoot", parent).GetComponent<RectTransform>();
        tabsRoot.anchorMin = new Vector2(1f, 1f);
        tabsRoot.anchorMax = new Vector2(1f, 1f);
        tabsRoot.pivot = new Vector2(1f, 1f);
        tabsRoot.sizeDelta = new Vector2(600f, 74f);
        tabsRoot.anchoredPosition = new Vector2(-62f, 34f);

        HorizontalLayoutGroup layout = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateTabButton(tabsRoot, "\u6e38\u620f", SettingsTab.Gameplay, out gameplayTabButton, out gameplayTabImage, out gameplayTabText);
        CreateTabButton(tabsRoot, "\u56fe\u50cf", SettingsTab.Display, out displayTabButton, out displayTabImage, out displayTabText);
        CreateTabButton(tabsRoot, "\u97f3\u9891", SettingsTab.Audio, out audioTabButton, out audioTabImage, out audioTabText);
    }

    private void CreateTabButton(RectTransform parent, string label, SettingsTab tab,
        out Button button, out Image image, out TextMeshProUGUI text)
    {
        GameObject buttonObj = CreateUI("Tab_" + label, parent);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(170f, 64f);

        image = buttonObj.AddComponent<Image>();
        image.color = TabInactiveColor;

        button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(delegate { SwitchTab(tab); });

        text = CreateLabel(rt, "Label", label, 28f, TextPrimary,
            new Vector2(0.5f, 0.5f), new Vector2(160f, 54f), Vector2.zero, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
    }

    private void CreateContentArea(RectTransform parent)
    {
        RectTransform contentRoot = CreateUI("ContentRoot", parent).GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 0f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.offsetMin = new Vector2(22f, 102f);
        contentRoot.offsetMax = new Vector2(-12f, -102f);

        GameObject scrollView = CreateUI("ScrollView", contentRoot);
        RectTransform scrollRT = scrollView.GetComponent<RectTransform>();
        Stretch(scrollView);

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUI("Viewport", scrollRT);
        Stretch(viewport);
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUI("Content", viewport.GetComponent<RectTransform>());
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.padding = new RectOffset(0, 0, 0, 14);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRT;

        gameplayPage = CreatePage(contentRT);
        displayPage = CreatePage(contentRT);
        audioPage = CreatePage(contentRT);

        BuildGameplayPage(gameplayPage.GetComponent<RectTransform>());
        BuildDisplayPage(displayPage.GetComponent<RectTransform>());
        BuildAudioPage(audioPage.GetComponent<RectTransform>());
    }

    private GameObject CreatePage(RectTransform parent)
    {
        GameObject page = CreateUI("Page", parent);
        VerticalLayoutGroup layout = page.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = page.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return page;
    }

    private void BuildGameplayPage(RectTransform parent)
    {
        CreateSectionHeader(parent, "\u8bed\u8a00\u8bbe\u7f6e");
        CreateSelectorRow(parent, "\u8bed\u8a00", out languageValueText,
            delegate { ChangeLanguage(-1); }, delegate { ChangeLanguage(1); });

        CreateSectionHeader(parent, "\u5267\u60c5\u64ad\u653e\u8bbe\u7f6e");
        CreateSelectorRow(parent, "\u6587\u672c\u64ad\u653e\u901f\u5ea6", out textSpeedValueText,
            delegate { ChangeTextSpeed(-1); }, delegate { ChangeTextSpeed(1); });
        CreateSelectorRow(parent, "\u81ea\u52a8\u64ad\u653e\u95f4\u9694", out autoPlayValueText,
            delegate { ChangeAutoPlayInterval(-1); }, delegate { ChangeAutoPlayInterval(1); });
        CreateSelectorRow(parent, "\u5feb\u8fdb\u89c4\u5219", out skipModeValueText,
            delegate { ChangeSkipMode(-1); }, delegate { ChangeSkipMode(1); });
        CreateSelectorRow(parent, "\u5feb\u8fdb\u901f\u5ea6", out fastForwardValueText,
            delegate { ChangeFastForwardSpeed(-1); }, delegate { ChangeFastForwardSpeed(1); });
        CreateInfoRow(parent, out dialogueSkipHelpText);

        CreateSectionHeader(parent, "\u5df2\u8bfb\u8bb0\u5f55");
        CreateInfoActionRow(parent, "\u5df2\u8bfb\u5bf9\u8bdd", out dialogueHistoryValueText, "\u6e05\u7a7a\u8bb0\u5f55", OnClearDialogueHistory);
    }

    private void BuildDisplayPage(RectTransform parent)
    {
        CreateSectionHeader(parent, "\u663e\u793a\u8bbe\u7f6e");
        CreateSelectorRow(parent, "\u5168\u5c4f\u6a21\u5f0f", out fullscreenValueText,
            delegate { ToggleFullscreen(); }, delegate { ToggleFullscreen(); });
        CreateSelectorRow(parent, "\u5206\u8fa8\u7387", out resolutionValueText,
            delegate { ChangeResolution(-1); }, delegate { ChangeResolution(1); });
        CreateSliderRow(parent, "\u754c\u9762\u7f29\u653e", 0.8f, 1.3f, out uiScaleSlider, out uiScaleValueText, OnUIScaleSliderChanged);
    }

    private void BuildAudioPage(RectTransform parent)
    {
        CreateSectionHeader(parent, "\u97f3\u91cf\u8bbe\u7f6e");
        CreateSliderRow(parent, "\u4e3b\u97f3\u91cf", 0f, 1f, out masterVolumeSlider, out masterVolumeValueText, OnMasterVolumeSliderChanged);
        CreateSliderRow(parent, "\u97f3\u4e50\u97f3\u91cf", 0f, 1f, out musicVolumeSlider, out musicVolumeValueText, OnMusicVolumeSliderChanged);
        CreateSliderRow(parent, "\u97f3\u6548\u97f3\u91cf", 0f, 1f, out sfxVolumeSlider, out sfxVolumeValueText, OnSFXVolumeSliderChanged);
        CreateSelectorRow(parent, "\u9759\u97f3", out muteValueText,
            delegate { ToggleMute(); }, delegate { ToggleMute(); });
    }

    private void CreateBottomBar(RectTransform parent)
    {
        GameObject bottom = CreateUI("BottomBar", parent);
        RectTransform rt = bottom.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, 84f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        bottom.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        statusText = CreateLabel(rt, "Status", "\u672a\u4fdd\u5b58\u4fee\u6539", 20f, TextSecondary,
            new Vector2(0f, 0.5f), new Vector2(280f, 40f), new Vector2(34f, 0f), TextAlignmentOptions.Left);

        Button resetButton = CreateActionButton(rt, "\u6062\u590d\u9ed8\u8ba4", new Vector2(220f, 64f), new Vector2(760f, 0f),
            new Color32(0xF5, 0xE6, 0xC6, 0xFF), AccentColor, OnResetToDefaults);
        Button cancelButton = CreateActionButton(rt, "\u53d6\u6d88", new Vector2(220f, 64f), new Vector2(1010f, 0f),
            new Color32(0xFB, 0xF2, 0xDE, 0xFF), AccentColor, TryClose);
        Button saveButton = CreateActionButton(rt, "\u4fdd\u5b58\u4fee\u6539", new Vector2(220f, 64f), new Vector2(1260f, 0f),
            new Color32(0xFF, 0xE7, 0x84, 0xFF), new Color32(0xD1, 0x87, 0x37, 0xFF), SaveAndClose);

        UIInputHelper.FocusSelectable(saveButton);
        resetButton.navigation = Navigation.defaultNavigation;
        cancelButton.navigation = Navigation.defaultNavigation;
    }

    private void CreateSectionHeader(RectTransform parent, string title)
    {
        GameObject header = CreateUI("SectionHeader_" + title, parent);
        RectTransform rt = header.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 46f);
        LayoutElement layout = header.AddComponent<LayoutElement>();
        layout.preferredHeight = 46f;

        Image image = header.AddComponent<Image>();
        image.color = SectionColor;

        CreateSectionFade(rt);
        TextMeshProUGUI text = CreateLabel(rt, "Label", title, 24f, Color.white,
            new Vector2(0f, 0.5f), new Vector2(320f, 42f), new Vector2(18f, 0f), TextAlignmentOptions.Left);
        text.fontStyle = FontStyles.Bold;
    }

    private void CreateSectionFade(RectTransform parent)
    {
        GameObject fade = CreateUI("Fade", parent);
        RectTransform rt = fade.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 0f);
        rt.anchoredPosition = new Vector2(0f, 0f);

        Image image = fade.AddComponent<Image>();
        Texture2D texture = new Texture2D(2, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixels(new[] { SectionColor, SectionFadeColor });
        texture.Apply();
        image.sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 1f), new Vector2(0.5f, 0.5f));
        image.type = Image.Type.Simple;
    }

    private void CreateSelectorRow(RectTransform parent, string label, out TextMeshProUGUI valueText, Action onPrevious, Action onNext)
    {
        GameObject row = CreateRow(parent, 86f);
        RectTransform rowRT = row.GetComponent<RectTransform>();

        CreateLabel(rowRT, "Label", label, 28f, TextPrimary,
            new Vector2(0f, 0.5f), new Vector2(360f, 50f), new Vector2(46f, 0f), TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;

        RectTransform boxRT = CreateValueBox(rowRT, new Vector2(0.76f, 0.5f), new Vector2(820f, 58f), Vector2.zero);
        CreateArrowButton(boxRT, "Prev", "<", new Vector2(34f, 34f), new Vector2(-362f, 0f), onPrevious);
        valueText = CreateLabel(boxRT, "Value", "---", 26f, TextSecondary,
            new Vector2(0.5f, 0.5f), new Vector2(560f, 42f), Vector2.zero, TextAlignmentOptions.Center);
        valueText.fontStyle = FontStyles.Bold;
        CreateArrowButton(boxRT, "Next", ">", new Vector2(34f, 34f), new Vector2(362f, 0f), onNext);
    }

    private void CreateSliderRow(RectTransform parent, string label, float min, float max,
        out Slider slider, out TextMeshProUGUI valueText, Action<float> onValueChanged)
    {
        GameObject row = CreateRow(parent, 104f);
        RectTransform rowRT = row.GetComponent<RectTransform>();

        CreateLabel(rowRT, "Label", label, 28f, TextPrimary,
            new Vector2(0f, 0.5f), new Vector2(360f, 50f), new Vector2(46f, 0f), TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;

        RectTransform boxRT = CreateValueBox(rowRT, new Vector2(0.76f, 0.5f), new Vector2(820f, 76f), Vector2.zero);

        slider = CreateSlider(boxRT, new Vector2(0.5f, 0.5f), new Vector2(520f, 24f), min, max, onValueChanged);
        valueText = CreateLabel(boxRT, "Value", "100%", 24f, TextSecondary,
            new Vector2(1f, 0.5f), new Vector2(160f, 40f), new Vector2(-44f, 0f), TextAlignmentOptions.Right);
        valueText.fontStyle = FontStyles.Bold;
    }

    private void CreateInfoRow(RectTransform parent, out TextMeshProUGUI infoText)
    {
        GameObject row = CreateRow(parent, 104f);
        RectTransform rowRT = row.GetComponent<RectTransform>();

        RectTransform boxRT = CreateValueBox(rowRT, new Vector2(0.5f, 0.5f), new Vector2(1600f, 74f), Vector2.zero);
        infoText = CreateLabel(boxRT, "Info", string.Empty, 22f, TextSecondary,
            new Vector2(0.5f, 0.5f), new Vector2(1480f, 56f), Vector2.zero, TextAlignmentOptions.MidlineLeft);
        infoText.enableWordWrapping = true;
        infoText.overflowMode = TextOverflowModes.Overflow;
    }

    private void CreateInfoActionRow(RectTransform parent, string label, out TextMeshProUGUI valueText, string buttonLabel, Action onClick)
    {
        GameObject row = CreateRow(parent, 92f);
        RectTransform rowRT = row.GetComponent<RectTransform>();

        CreateLabel(rowRT, "Label", label, 28f, TextPrimary,
            new Vector2(0f, 0.5f), new Vector2(360f, 50f), new Vector2(46f, 0f), TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;

        RectTransform boxRT = CreateValueBox(rowRT, new Vector2(0.62f, 0.5f), new Vector2(700f, 58f), Vector2.zero);
        valueText = CreateLabel(boxRT, "Value", "---", 24f, TextSecondary,
            new Vector2(0.5f, 0.5f), new Vector2(640f, 42f), Vector2.zero, TextAlignmentOptions.Center);
        valueText.fontStyle = FontStyles.Bold;

        CreateActionButton(rowRT, buttonLabel, new Vector2(220f, 56f), new Vector2(1320f, 0f),
            new Color32(0xFB, 0xF2, 0xDE, 0xFF), AccentColor, onClick);
    }

    private GameObject CreateRow(RectTransform parent, float height)
    {
        GameObject row = CreateUI("Row", parent);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        Image image = row.AddComponent<Image>();
        image.color = RowColor;
        return row;
    }

    private RectTransform CreateValueBox(RectTransform parent, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject box = CreateUI("ValueBox", parent);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image image = box.AddComponent<Image>();
        image.color = ValueBoxColor;

        Outline outline = box.AddComponent<Outline>();
        outline.effectColor = ValueBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        return rt;
    }

    private Button CreateArrowButton(RectTransform parent, string name, string icon, Vector2 size, Vector2 pos, Action onClick)
    {
        GameObject buttonObj = CreateUI(name, parent);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(delegate { onClick?.Invoke(); });

        TextMeshProUGUI text = CreateLabel(rt, "Icon", icon, 28f, AccentColor,
            new Vector2(0.5f, 0.5f), size, Vector2.zero, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private Slider CreateSlider(RectTransform parent, Vector2 anchor, Vector2 size, float min, float max, Action<float> onValueChanged)
    {
        GameObject sliderObj = CreateUI("Slider", parent);
        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(-86f, 0f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject bg = CreateUI("Background", rt);
        Stretch(bg);
        bg.AddComponent<Image>().color = SliderTrackColor;

        GameObject fillArea = CreateUI("FillArea", rt);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(0f, 0f);
        fillAreaRT.offsetMax = new Vector2(0f, 0f);

        GameObject fill = CreateUI("Fill", fillAreaRT);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        Stretch(fill);
        fill.AddComponent<Image>().color = SliderFillColor;

        GameObject handleArea = CreateUI("HandleArea", rt);
        Stretch(handleArea);

        GameObject handle = CreateUI("Handle", handleArea.GetComponent<RectTransform>());
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(26f, 26f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = AccentStrongColor;

        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(delegate(float value) { onValueChanged?.Invoke(value); });
        return slider;
    }

    private Button CreateActionButton(RectTransform parent, string label, Vector2 size, Vector2 pos, Color bgColor, Color textColor, Action onClick)
    {
        GameObject buttonObj = CreateUI("Button_" + label, parent);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image image = buttonObj.AddComponent<Image>();
        image.color = bgColor;

        Outline outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.81f, 0.69f, 0.45f, 0.6f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(delegate { onClick?.Invoke(); });

        TextMeshProUGUI text = CreateLabel(rt, "Label", label, 26f, textColor,
            new Vector2(0.5f, 0.5f), size, Vector2.zero, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private void SwitchTab(SettingsTab tab)
    {
        currentTab = tab;
        UpdateTabVisuals();
    }

    private void UpdateTabVisuals()
    {
        bool gameplayActive = currentTab == SettingsTab.Gameplay;
        bool displayActive = currentTab == SettingsTab.Display;
        bool audioActive = currentTab == SettingsTab.Audio;

        if (gameplayPage != null) gameplayPage.SetActive(gameplayActive);
        if (displayPage != null) displayPage.SetActive(displayActive);
        if (audioPage != null) audioPage.SetActive(audioActive);

        ApplyTabState(gameplayTabImage, gameplayTabText, gameplayActive);
        ApplyTabState(displayTabImage, displayTabText, displayActive);
        ApplyTabState(audioTabImage, audioTabText, audioActive);
    }

    private void ApplyTabState(Image image, TextMeshProUGUI text, bool active)
    {
        if (image != null)
        {
            image.color = active ? TabActiveColor : TabInactiveColor;
        }

        if (text != null)
        {
            text.color = active ? TextPrimary : TextSecondary;
        }
    }

    private void RefreshAllUI()
    {
        if (draftSettings == null)
        {
            return;
        }

        if (languageValueText != null) languageValueText.text = draftSettings.GetLanguageLabel();
        if (textSpeedValueText != null) textSpeedValueText.text = draftSettings.GetTextSpeedLabel();
        if (autoPlayValueText != null) autoPlayValueText.text = draftSettings.GetAutoPlayIntervalLabel();
        if (skipModeValueText != null) skipModeValueText.text = draftSettings.GetSkipModeLabel();
        if (fastForwardValueText != null) fastForwardValueText.text = draftSettings.GetFastForwardSpeedLabel();
        if (dialogueSkipHelpText != null) dialogueSkipHelpText.text = GetDialogueSkipHelpText();
        if (dialogueHistoryValueText != null) dialogueHistoryValueText.text = GetDialogueHistoryLabel();
        if (fullscreenValueText != null) fullscreenValueText.text = draftSettings.fullscreen ? "\u5f00\u542f" : "\u5173\u95ed";
        if (resolutionValueText != null) resolutionValueText.text = draftSettings.GetResolutionLabel();
        if (uiScaleValueText != null) uiScaleValueText.text = Mathf.RoundToInt(draftSettings.uiScale * 100f) + "%";
        if (masterVolumeValueText != null) masterVolumeValueText.text = Mathf.RoundToInt(draftSettings.masterVolume * 100f) + "%";
        if (musicVolumeValueText != null) musicVolumeValueText.text = Mathf.RoundToInt(draftSettings.musicVolume * 100f) + "%";
        if (sfxVolumeValueText != null) sfxVolumeValueText.text = Mathf.RoundToInt(draftSettings.sfxVolume * 100f) + "%";
        if (muteValueText != null) muteValueText.text = draftSettings.isMuted ? "\u5f00\u542f" : "\u5173\u95ed";

        if (uiScaleSlider != null) uiScaleSlider.SetValueWithoutNotify(draftSettings.uiScale);
        if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(draftSettings.masterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(draftSettings.musicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(draftSettings.sfxVolume);

        if (statusText != null)
        {
            statusText.text = HasUnsavedChanges() ? "\u672a\u4fdd\u5b58\u4fee\u6539" : "\u5f53\u524d\u8bbe\u7f6e\u5df2\u540c\u6b65";
            statusText.color = HasUnsavedChanges() ? TextSecondary : AccentColor;
        }

        UpdateTabVisuals();
    }

    private bool HasUnsavedChanges()
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.CurrentSettings == null || draftSettings == null)
        {
            return false;
        }

        SettingsData current = SettingsManager.Instance.CurrentSettings;
        return Math.Abs(current.masterVolume - draftSettings.masterVolume) > 0.001f
            || Math.Abs(current.musicVolume - draftSettings.musicVolume) > 0.001f
            || Math.Abs(current.sfxVolume - draftSettings.sfxVolume) > 0.001f
            || current.isMuted != draftSettings.isMuted
            || current.fullscreen != draftSettings.fullscreen
            || current.resolutionWidth != draftSettings.resolutionWidth
            || current.resolutionHeight != draftSettings.resolutionHeight
            || Math.Abs(current.uiScale - draftSettings.uiScale) > 0.001f
            || current.textSpeed != draftSettings.textSpeed
            || current.language != draftSettings.language
            || current.autoPlayInterval != draftSettings.autoPlayInterval
            || current.skipMode != draftSettings.skipMode
            || current.fastForwardSpeed != draftSettings.fastForwardSpeed;
    }

    private void TryClose()
    {
        if (!HasUnsavedChanges())
        {
            HideSettings();
            return;
        }

        CreateConfirmDialog("\u6709\u672a\u4fdd\u5b58\u7684\u4fee\u6539\uff0c\u786e\u5b9a\u76f4\u63a5\u5173\u95ed\u5417\uff1f", HideSettings);
    }

    private void SaveAndClose()
    {
        if (SettingsManager.Instance == null || draftSettings == null)
        {
            HideSettings();
            return;
        }

        SettingsManager.Instance.SaveAndApply(draftSettings.Clone());
        HideSettings();
    }

    private void OnResetToDefaults()
    {
        CreateConfirmDialog("\u786e\u5b9a\u6062\u590d\u4e3a\u9ed8\u8ba4\u8bbe\u7f6e\u5417\uff1f", delegate
        {
            if (draftSettings == null)
            {
                draftSettings = new SettingsData();
            }

            draftSettings.ResetToDefaults();
            resolutionOptions = BuildResolutionOptions(draftSettings);
            RefreshAllUI();
        });
    }

    private void ChangeLanguage(int direction)
    {
        draftSettings.language = WrapIndex(draftSettings.language + direction, 2);
        RefreshAllUI();
    }

    private void ChangeTextSpeed(int direction)
    {
        draftSettings.textSpeed = WrapIndex(draftSettings.textSpeed + direction, 3);
        RefreshAllUI();
    }

    private void ChangeAutoPlayInterval(int direction)
    {
        draftSettings.autoPlayInterval = WrapIndex(draftSettings.autoPlayInterval + direction, 3);
        RefreshAllUI();
    }

    private void ChangeSkipMode(int direction)
    {
        draftSettings.skipMode = WrapIndex(draftSettings.skipMode + direction, 2);
        RefreshAllUI();
    }

    private void ChangeFastForwardSpeed(int direction)
    {
        draftSettings.fastForwardSpeed = WrapIndex(draftSettings.fastForwardSpeed + direction, 4);
        RefreshAllUI();
    }

    private void ToggleFullscreen()
    {
        draftSettings.fullscreen = !draftSettings.fullscreen;
        RefreshAllUI();
    }

    private void ChangeResolution(int direction)
    {
        if (resolutionOptions.Count == 0)
        {
            return;
        }

        int currentIndex = resolutionOptions.IndexOf(draftSettings.GetResolutionLabel());
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        currentIndex = WrapIndex(currentIndex + direction, resolutionOptions.Count);
        ApplyResolutionLabel(resolutionOptions[currentIndex]);
        RefreshAllUI();
    }

    private void ApplyResolutionLabel(string value)
    {
        string[] parts = value.Split('x');
        if (parts.Length != 2)
        {
            return;
        }

        int width;
        int height;
        if (int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height))
        {
            draftSettings.resolutionWidth = width;
            draftSettings.resolutionHeight = height;
        }
    }

    private void OnUIScaleSliderChanged(float value)
    {
        draftSettings.uiScale = value;
        RefreshAllUI();
    }

    private void OnMasterVolumeSliderChanged(float value)
    {
        draftSettings.masterVolume = value;
        RefreshAllUI();
    }

    private void OnMusicVolumeSliderChanged(float value)
    {
        draftSettings.musicVolume = value;
        RefreshAllUI();
    }

    private void OnSFXVolumeSliderChanged(float value)
    {
        draftSettings.sfxVolume = value;
        RefreshAllUI();
    }

    private void ToggleMute()
    {
        draftSettings.isMuted = !draftSettings.isMuted;
        RefreshAllUI();
    }

    private string GetDialogueSkipHelpText()
    {
        string ruleText = draftSettings != null && draftSettings.skipMode == 1
            ? "\u6309\u4f4f Ctrl \u65f6\uff0c\u65b0\u5bf9\u8bdd\u53ea\u4f1a\u5148\u8865\u5168\u6587\u5b57\uff1b\u5df2\u8bfb\u5bf9\u8bdd\u624d\u4f1a\u8fde\u7eed\u8df3\u8fc7\u3002"
            : "\u6309\u4f4f Ctrl \u65f6\uff0c\u4f1a\u8fde\u7eed\u5feb\u8fdb\u5f53\u524d\u5bf9\u8bdd\u3002";
        string speedText = draftSettings != null ? draftSettings.GetFastForwardSpeedLabel() : "x20";
        return "\u5feb\u6377\u952e\uff1a\u6309\u4f4f Ctrl \u5feb\u8fdb\u3002\u9047\u5230\u9009\u9879\u4f1a\u81ea\u52a8\u505c\u4e0b\uff0c\u4e0d\u4f1a\u66ff\u4f60\u9009\u62e9\u3002\u5f53\u524d\u901f\u5ea6\uff1a" + speedText + "\u3002" + ruleText;
    }

    private string GetDialogueHistoryLabel()
    {
        int count = DialogueSystem.Instance != null ? DialogueSystem.Instance.GetSeenDialogueEntryCount() : 0;
        return "\u5df2\u8bb0\u5f55 " + count + " \u6761";
    }

    private void OnClearDialogueHistory()
    {
        CreateConfirmDialog("\u786e\u5b9a\u6e05\u7a7a\u5f53\u524d\u5b58\u6863\u7684\u5df2\u8bfb\u5bf9\u8bdd\u8bb0\u5f55\u5417\uff1f", delegate
        {
            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.ClearSeenDialogueEntries();
            }

            RefreshAllUI();
        });
    }

    private int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        while (index < 0)
        {
            index += count;
        }

        while (index >= count)
        {
            index -= count;
        }

        return index;
    }

    private List<string> BuildResolutionOptions(SettingsData data)
    {
        HashSet<string> unique = new HashSet<string>();
        List<string> options = new List<string>();

        AddResolution(options, unique, "1280x720");
        AddResolution(options, unique, "1600x900");
        AddResolution(options, unique, "1920x1080");
        AddResolution(options, unique, "2560x1440");

        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            AddResolution(options, unique, resolutions[i].width + "x" + resolutions[i].height);
        }

        if (data != null)
        {
            AddResolution(options, unique, data.GetResolutionLabel());
        }

        return options;
    }

    private void AddResolution(List<string> options, HashSet<string> unique, string value)
    {
        if (!unique.Add(value))
        {
            return;
        }

        options.Add(value);
    }

    private void CreateConfirmDialog(string message, Action onConfirm)
    {
        ClearConfirmDialog();

        confirmDialogCanvasObj = new GameObject("ConfirmDialogCanvas");
        confirmDialogCanvasObj.transform.SetParent(transform, false);

        Canvas dialogCanvas = confirmDialogCanvasObj.AddComponent<Canvas>();
        dialogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogCanvas.sortingOrder = canvas.sortingOrder + 1;

        CanvasScaler scaler = confirmDialogCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        confirmDialogCanvasObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = confirmDialogCanvasObj.GetComponent<RectTransform>();

        GameObject overlay = CreateUI("Overlay", canvasRT);
        Stretch(overlay);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.68f);

        GameObject panel = CreateUI("Panel", canvasRT);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(520f, 250f);
        panelRT.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = BookInnerColor;

        CreateLabel(panelRT, "Message", message, 24f, TextPrimary,
            new Vector2(0.5f, 0.5f), new Vector2(420f, 90f), new Vector2(0f, 26f), TextAlignmentOptions.Center);

        confirmDialogConfirmAction = delegate
        {
            Action callback = onConfirm;
            ClearConfirmDialog();
            callback?.Invoke();
        };

        confirmDialogCancelAction = ClearConfirmDialog;

        confirmDialogConfirmButton = CreateActionButton(panelRT, "\u786e\u5b9a", new Vector2(170f, 56f), new Vector2(86f, -56f),
            new Color32(0xFF, 0xE7, 0x84, 0xFF), new Color32(0xB8, 0x6F, 0x2C, 0xFF),
            delegate { confirmDialogConfirmAction?.Invoke(); });

        CreateActionButton(panelRT, "\u53d6\u6d88", new Vector2(170f, 56f), new Vector2(280f, -56f),
            new Color32(0xF5, 0xE7, 0xC9, 0xFF), AccentColor,
            delegate { confirmDialogCancelAction?.Invoke(); });

        UIInputHelper.FocusSelectable(confirmDialogConfirmButton);
    }

    private void ClearConfirmDialog()
    {
        if (confirmDialogCanvasObj != null)
        {
            Destroy(confirmDialogCanvasObj);
            confirmDialogCanvasObj = null;
        }

        confirmDialogConfirmAction = null;
        confirmDialogCancelAction = null;
        confirmDialogConfirmButton = null;
    }

    private GameObject CreateUI(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
        {
            go.AddComponent<RectTransform>();
        }

        return go;
    }

    private void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text, float fontSize, Color color,
        Vector2 anchor, Vector2 size, Vector2 pos, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x <= 0.01f ? 0f : anchor.x >= 0.99f ? 1f : 0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        return tmp;
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }
    }
}

