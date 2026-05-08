#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DebugConsoleUI : MonoBehaviour
{
    private sealed class QuickAdjustItem
    {
        public string key;
        public TextMeshProUGUI valueText;
    }

    private static DebugConsoleUI instance;

    private const float TopBarHeight = 118f;
    private const float BottomBarHeight = 118f;
    private const float SidebarWidth = 172f;
    private const float FramePadding = 18f;
    private const float SectionGap = 14f;

    private static readonly Color BackdropColor = new Color(0.03f, 0.02f, 0.02f, 0.56f);
    private static readonly Color BackgroundColor = new Color32(0x17, 0x13, 0x1D, 0xF3);
    private static readonly Color HeaderColor = new Color32(0x21, 0x1B, 0x29, 0xFC);
    private static readonly Color SidebarColor = new Color32(0x1C, 0x17, 0x24, 0xFA);
    private static readonly Color ContentColor = new Color32(0x1B, 0x16, 0x23, 0xF2);
    private static readonly Color CardColor = new Color32(0x27, 0x20, 0x30, 0xFA);
    private static readonly Color BorderColor = new Color32(0x73, 0x5C, 0x46, 0xD6);
    private static readonly Color ButtonColor = new Color32(0x7A, 0x56, 0x35, 0xFF);
    private static readonly Color ButtonHighlightColor = new Color32(0x9D, 0x75, 0x49, 0xFF);
    private static readonly Color TabNormalColor = new Color32(0x2A, 0x24, 0x33, 0xFF);
    private static readonly Color TabSelectedColor = new Color32(0x4F, 0x39, 0x2A, 0xFF);
    private static readonly Color AccentColor = new Color32(0xF2, 0xC5, 0x68, 0xFF);
    private static readonly Color AccentSoftColor = new Color32(0xD6, 0xB2, 0x7C, 0xFF);
    private static readonly Color PositiveColor = new Color32(0x3D, 0x7E, 0x57, 0xFF);
    private static readonly Color NegativeColor = new Color32(0x9E, 0x4E, 0x4B, 0xFF);
    private static readonly Color ToggleOffColor = new Color32(0x46, 0x40, 0x4E, 0xFF);
    private static readonly Color ToggleOnColor = new Color32(0x9B, 0x75, 0x48, 0xFF);
    private static readonly Color TextColor = new Color32(0xF1, 0xEA, 0xDB, 0xFF);
    private static readonly Color TextMutedColor = new Color32(0xB8, 0xA7, 0x92, 0xFF);

    private static readonly string[] TabNames =
    {
        "属性",
        "时间",
        "内容",
        "新闻",
        "结局",
        "事件",
        "剧情",
        "NPC",
        "经济",
        "公式",
        "快照",
        "日志"
    };

    private static readonly (string Label, string Key)[] QuickAdjustKeys =
    {
        ("学力", "Study"),
        ("魅力", "Charm"),
        ("体魄", "Physique"),
        ("领导力", "Leadership"),
        ("压力", "Stress"),
        ("心情", "Mood"),
        ("黑暗值", "Darkness"),
        ("负罪感", "Guilt"),
        ("幸运", "Luck"),
        ("金钱", "Money")
    };

    private CanvasGroup rootCanvasGroup;
    private RectTransform contentArea;
    private RectTransform frameRect;
    private readonly List<Image> tabImages = new List<Image>();
    private readonly List<GameObject> modulePanels = new List<GameObject>();
    private readonly List<QuickAdjustItem> quickAdjustItems = new List<QuickAdjustItem>();
    private readonly List<Image> stepButtonImages = new List<Image>();

    private Toggle skipSplashToggle;
    private Toggle skipCreateToggle;
    private Toggle skipIntroToggle;
    private Toggle skipTitleToggle;
    private Button prevDialogueButton;

    private AttributeModule attributeModule;
    private TimeModule timeModule;
    private ContentModule contentModule;
    private NewsModule newsModule;
    private EndingSimModule endingSimModule;
    private EventModule eventModule;
    private StoryDebugModule storyModule;
    private NPCModule npcModule;
    private EconomyModule economyModule;
    private FormulaModule formulaModule;
    private SnapshotModule snapshotModule;
    private LogModule logModule;

    private int activeTabIndex = -1;

    private void Awake()
    {
        instance = this;
        BuildUI();
        SwitchTab(0);
        Hide();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void Show()
    {
        if (instance == null)
        {
            return;
        }

        instance.gameObject.SetActive(true);
        instance.rootCanvasGroup.alpha = 1f;
        instance.rootCanvasGroup.interactable = true;
        instance.rootCanvasGroup.blocksRaycasts = true;
        instance.SyncTopControls();
        instance.RefreshQuickAdjustValues();
        instance.RefreshActiveModule();
    }

    public static void Hide()
    {
        if (instance == null)
        {
            return;
        }

        instance.rootCanvasGroup.alpha = 0f;
        instance.rootCanvasGroup.interactable = false;
        instance.rootCanvasGroup.blocksRaycasts = false;
    }

    private void BuildUI()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("DebugCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        rootCanvasGroup = canvasObject.AddComponent<CanvasGroup>();

        GameObject backdrop = CreateRect("Backdrop", canvasObject.transform).gameObject;
        StretchFull(backdrop.GetComponent<RectTransform>());
        Image backdropImage = backdrop.AddComponent<Image>();
        backdropImage.color = BackdropColor;

        GameObject background = CreatePanel("Background", canvasObject.transform, BackgroundColor);
        StretchFull(background.GetComponent<RectTransform>());

        GameObject frame = CreateRect("Frame", canvasObject.transform).gameObject;
        frameRect = frame.GetComponent<RectTransform>();
        StretchFull(frameRect);
        frameRect.offsetMin = new Vector2(FramePadding, FramePadding);
        frameRect.offsetMax = new Vector2(-FramePadding, -FramePadding);

        CreateTopBar(frameRect);
        CreateSidebar(frameRect);
        CreateContentArea(frameRect);
        CreateBottomBar(frameRect);
        CreateModulePanels();
    }

    private void CreateTopBar(Transform parent)
    {
        GameObject topBar = CreatePanel("TopBar", parent, HeaderColor);
        RectTransform topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0f, TopBarHeight);

        TextMeshProUGUI title = CreateText("Title", topBar.transform, "钟山台", 22f, AccentColor, TextAlignmentOptions.MidlineLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        title.fontSize = 28f;
        titleRect.sizeDelta = new Vector2(320f, 36f);
        titleRect.anchoredPosition = new Vector2(20f, -14f);

        TextMeshProUGUI subtitle = CreateText("Subtitle", topBar.transform, "局内调试、速编与状态修正统一收口在这里。", 12f, TextMutedColor, TextAlignmentOptions.MidlineLeft);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(0f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(460f, 24f);
        subtitleRect.anchoredPosition = new Vector2(20f, -46f);

        GameObject badgeObject = CreatePanel("RuntimeBadge", topBar.transform, CardColor);
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.sizeDelta = new Vector2(168f, 30f);
        badgeRect.anchoredPosition = new Vector2(-18f, -14f);
        AddOutline(badgeObject, BorderColor, new Vector2(1f, -1f));

        TextMeshProUGUI badgeText = CreateText("BadgeText", badgeObject.transform, "运行时调试台", 12f, AccentSoftColor, TextAlignmentOptions.Center);
        StretchFull(badgeText.rectTransform);

        Transform primaryRow = CreateHorizontalRow("PrimaryRow", topBar.transform, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        RectTransform primaryRect = primaryRow.GetComponent<RectTransform>();
        primaryRect.anchorMin = new Vector2(0f, 0f);
        primaryRect.anchorMax = new Vector2(1f, 0f);
        primaryRect.offsetMin = new Vector2(610f, 44f);
        primaryRect.offsetMax = new Vector2(-18f, 78f);

        prevDialogueButton = CreateButton(primaryRow, "上一句", ButtonColor, () =>
        {
            DialogueSystem.Instance?.DebugStepBackOneLine();
            RefreshTopActionButtons();
        });
        SetLayoutSize(prevDialogueButton.gameObject, 104f, 34f);

        Button quickStartButton = CreateButton(primaryRow, "快速开局", ButtonColor, () =>
        {
            StartupFlowSettings.ApplyQuickStartPreset();
            SyncTopControls();
            DebugConsoleManager.Log("Startup", "Applied quick-start preset");
        });
        SetLayoutSize(quickStartButton.gameObject, 112f, 34f);

        Button closeButton = CreateButton(primaryRow, "关闭", NegativeColor, () => DebugConsoleManager.Instance?.Close());
        SetLayoutSize(closeButton.gameObject, 84f, 34f);

        Transform toggleRow = CreateHorizontalRow("ToggleRow", topBar.transform, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        RectTransform toggleRect = toggleRow.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 0f);
        toggleRect.anchorMax = new Vector2(1f, 0f);
        toggleRect.offsetMin = new Vector2(18f, 10f);
        toggleRect.offsetMax = new Vector2(-18f, 44f);

        skipSplashToggle = CreateLabeledToggle(toggleRow, "跳过开屏", StartupFlowSettings.SkipSplashLogo, value =>
        {
            StartupFlowSettings.SkipSplashLogo = value;
        });

        skipCreateToggle = CreateLabeledToggle(toggleRow, "跳过建角", StartupFlowSettings.SkipCharacterCreation, value =>
        {
            StartupFlowSettings.SkipCharacterCreation = value;
        });

        skipIntroToggle = CreateLabeledToggle(toggleRow, "跳过开场", StartupFlowSettings.SkipOpeningStory, value =>
        {
            StartupFlowSettings.SkipOpeningStory = value;
        });

        skipTitleToggle = CreateLabeledToggle(toggleRow, "跳过首页", StartupFlowSettings.SkipTitleScreen, value =>
        {
            StartupFlowSettings.SkipTitleScreen = value;
        });
    }

    private void CreateSidebar(Transform parent)
    {
        GameObject sidebar = CreatePanel("Sidebar", parent, SidebarColor);
        RectTransform sidebarRect = sidebar.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(0f, 0f);
        sidebarRect.anchorMax = new Vector2(0f, 1f);
        sidebarRect.pivot = new Vector2(0f, 0.5f);
        sidebarRect.offsetMin = new Vector2(0f, BottomBarHeight + SectionGap);
        sidebarRect.offsetMax = new Vector2(SidebarWidth, -TopBarHeight - SectionGap);

        VerticalLayoutGroup layout = sidebar.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject labelObject = CreateRect("SidebarLabel", sidebar.transform).gameObject;
        LayoutElement labelLayout = labelObject.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 24f;
        TextMeshProUGUI sidebarLabel = CreateText("Label", labelObject.transform, "模块导航", 12f, TextMutedColor, TextAlignmentOptions.MidlineLeft);
        StretchFull(sidebarLabel.rectTransform);
        sidebarLabel.margin = new Vector4(10f, 2f, 2f, 2f);

        for (int i = 0; i < TabNames.Length; i++)
        {
            int capturedIndex = i;
            GameObject tabObject = CreateRect($"Tab_{TabNames[i]}", sidebar.transform).gameObject;
            RectTransform tabRect = tabObject.GetComponent<RectTransform>();
            tabRect.sizeDelta = new Vector2(SidebarWidth - 20f, 42f);

            Image background = tabObject.AddComponent<Image>();
            background.color = TabNormalColor;
            tabImages.Add(background);
            AddOutline(tabObject, BorderColor, new Vector2(1f, -1f));

            Button button = tabObject.AddComponent<Button>();
            button.onClick.AddListener(() => SwitchTab(capturedIndex));

            TextMeshProUGUI label = CreateText("Label", tabObject.transform, TabNames[i], 14f, TextColor, TextAlignmentOptions.MidlineLeft);
            StretchFull(label.rectTransform);
            label.margin = new Vector4(16f, 4f, 8f, 4f);
        }
    }

    private void CreateContentArea(Transform parent)
    {
        GameObject content = CreatePanel("ContentArea", parent, ContentColor);
        contentArea = content.GetComponent<RectTransform>();
        contentArea.anchorMin = new Vector2(0f, 0f);
        contentArea.anchorMax = new Vector2(1f, 1f);
        contentArea.offsetMin = new Vector2(SidebarWidth + SectionGap, BottomBarHeight + SectionGap);
        contentArea.offsetMax = new Vector2(0f, -TopBarHeight - SectionGap);
    }

    private void CreateBottomBar(Transform parent)
    {
        GameObject bottomBar = CreatePanel("BottomBar", parent, HeaderColor);
        RectTransform bottomRect = bottomBar.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.offsetMin = new Vector2(SidebarWidth + SectionGap, 0f);
        bottomRect.sizeDelta = new Vector2(0f, BottomBarHeight);

        VerticalLayoutGroup layout = bottomBar.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Transform stepRow = CreateHorizontalRow("StepRow", bottomBar.transform, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        stepRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
        TextMeshProUGUI stepLabel = CreateText("StepLabel", stepRow, "微调步进", 14f, AccentColor, TextAlignmentOptions.MidlineLeft);
        SetLayoutSize(stepLabel.gameObject, 70f, 28f);

        int[] steps = DebugPresets.GetStepOptions();
        for (int i = 0; i < steps.Length; i++)
        {
            int capturedIndex = i;
            Button stepButton = CreateButton(stepRow, steps[i].ToString(), DebugPresets.CurrentStepIndex == i ? TabSelectedColor : TabNormalColor, () =>
            {
                DebugPresets.SetStepIndex(capturedIndex);
                RefreshStepButtons();
            });
            stepButtonImages.Add(stepButton.GetComponent<Image>());
            SetLayoutSize(stepButton.gameObject, 64f, 30f);
        }

        Transform gridRoot = CreateRect("AdjustGridRoot", bottomBar.transform);
        gridRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;
        GridLayoutGroup grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(142f, 30f);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        foreach ((string label, string key) in QuickAdjustKeys)
        {
            CreateQuickAdjustCell(gridRoot, label, key);
        }
    }

    private void CreateQuickAdjustCell(Transform parent, string label, string key)
    {
        GameObject cell = CreatePanel($"Adjust_{key}", parent, CardColor);
        LayoutElement cellLayout = cell.AddComponent<LayoutElement>();
        cellLayout.preferredWidth = 142f;
        cellLayout.preferredHeight = 30f;
        AddOutline(cell, BorderColor, new Vector2(1f, -1f));

        Transform row = CreateHorizontalRow($"AdjustRow_{key}", cell.transform, 4f, new RectOffset(8, 8, 6, 6), TextAnchor.MiddleCenter);
        StretchFull(row.GetComponent<RectTransform>());

        Button minusButton = CreateButton(row, "-", NegativeColor, () =>
        {
            DebugPresets.AdjustAttribute(key, false);
            RefreshQuickAdjustValues();
            RefreshActiveModule();
        });
        SetLayoutSize(minusButton.gameObject, 22f, 20f);

        TextMeshProUGUI labelText = CreateText("Name", row, label, 12f, TextMutedColor, TextAlignmentOptions.MidlineLeft);
        SetLayoutSize(labelText.gameObject, 42f, 22f);

        TextMeshProUGUI valueText = CreateText("Value", row, "0", 13f, AccentColor, TextAlignmentOptions.Center);
        SetLayoutSize(valueText.gameObject, 48f, 22f);
        quickAdjustItems.Add(new QuickAdjustItem
        {
            key = key,
            valueText = valueText
        });

        Button plusButton = CreateButton(row, "+", PositiveColor, () =>
        {
            DebugPresets.AdjustAttribute(key, true);
            RefreshQuickAdjustValues();
            RefreshActiveModule();
        });
        SetLayoutSize(plusButton.gameObject, 22f, 20f);
    }

    private void CreateModulePanels()
    {
        attributeModule = CreateModulePanel<AttributeModule>("AttributePanel");
        timeModule = CreateModulePanel<TimeModule>("TimePanel");
        contentModule = CreateModulePanel<ContentModule>("ContentPanel");
        newsModule = CreateModulePanel<NewsModule>("NewsPanel");
        endingSimModule = CreateModulePanel<EndingSimModule>("EndingPanel");
        eventModule = CreateModulePanel<EventModule>("EventPanel");
        storyModule = CreateModulePanel<StoryDebugModule>("StoryPanel");
        npcModule = CreateModulePanel<NPCModule>("NPCPanel");
        economyModule = CreateModulePanel<EconomyModule>("EconomyPanel");
        formulaModule = CreateModulePanel<FormulaModule>("FormulaPanel");
        snapshotModule = CreateModulePanel<SnapshotModule>("SnapshotPanel");
        logModule = CreateModulePanel<LogModule>("LogPanel");
    }

    private T CreateModulePanel<T>(string name) where T : MonoBehaviour, IDebugModule
    {
        GameObject panel = CreateRect(name, contentArea).gameObject;
        StretchFull(panel.GetComponent<RectTransform>());

        CanvasGroup group = panel.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        T module = panel.AddComponent<T>();
        module.Init(panel.GetComponent<RectTransform>());
        modulePanels.Add(panel);
        panel.SetActive(false);
        return module;
    }

    private void SwitchTab(int index)
    {
        if (index == activeTabIndex)
        {
            return;
        }

        if (activeTabIndex >= 0 && activeTabIndex < modulePanels.Count)
        {
            SetPanelVisible(modulePanels[activeTabIndex], false);
            tabImages[activeTabIndex].color = TabNormalColor;
        }

        activeTabIndex = index;
        if (activeTabIndex >= 0 && activeTabIndex < modulePanels.Count)
        {
            SetPanelVisible(modulePanels[activeTabIndex], true);
            tabImages[activeTabIndex].color = TabSelectedColor;
            RefreshActiveModule();
        }
    }

    private void SetPanelVisible(GameObject panel, bool visible)
    {
        panel.SetActive(visible);
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }

    private void RefreshActiveModule()
    {
        RefreshTopActionButtons();

        switch (activeTabIndex)
        {
            case 0: attributeModule?.Refresh(); break;
            case 1: timeModule?.Refresh(); break;
            case 2: contentModule?.Refresh(); break;
            case 3: newsModule?.Refresh(); break;
            case 4: endingSimModule?.Refresh(); break;
            case 5: eventModule?.Refresh(); break;
            case 6: storyModule?.Refresh(); break;
            case 7: npcModule?.Refresh(); break;
            case 8: economyModule?.Refresh(); break;
            case 9: formulaModule?.Refresh(); break;
            case 10: snapshotModule?.Refresh(); break;
            case 11: logModule?.Refresh(); break;
        }
    }

    private void RefreshQuickAdjustValues()
    {
        foreach (QuickAdjustItem item in quickAdjustItems)
        {
            int value = DebugPresets.GetAttributeValue(item.key);
            item.valueText.text = item.key == "Money" ? $"¥{value}" : value.ToString();
        }
    }

    private void RefreshStepButtons()
    {
        for (int i = 0; i < stepButtonImages.Count; i++)
        {
            stepButtonImages[i].color = i == DebugPresets.CurrentStepIndex ? TabSelectedColor : TabNormalColor;
        }
    }

    private void SyncTopControls()
    {
        SetToggleValue(skipSplashToggle, StartupFlowSettings.SkipSplashLogo);
        SetToggleValue(skipCreateToggle, StartupFlowSettings.SkipCharacterCreation);
        SetToggleValue(skipIntroToggle, StartupFlowSettings.SkipOpeningStory);
        SetToggleValue(skipTitleToggle, StartupFlowSettings.SkipTitleScreen);
        RefreshTopActionButtons();
        RefreshStepButtons();
    }

    private void RefreshTopActionButtons()
    {
        if (prevDialogueButton != null)
        {
            bool canStepBack = DialogueSystem.Instance != null && DialogueSystem.Instance.CanStepBack;
            prevDialogueButton.interactable = canStepBack;
            prevDialogueButton.GetComponent<Image>().color = canStepBack ? ButtonColor : ToggleOffColor;
        }
    }

    private void SetToggleValue(Toggle toggle, bool value)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.SetIsOnWithoutNotify(value);
        Image bg = toggle.targetGraphic as Image;
        if (bg != null)
        {
            bg.color = value ? ToggleOnColor : ToggleOffColor;
        }

        if (toggle.graphic != null)
        {
            RectTransform knobRect = toggle.graphic.rectTransform;
            knobRect.anchoredPosition = new Vector2(value ? 24f : 4f, 0f);
        }
    }

    private Toggle CreateLabeledToggle(Transform parent, string label, bool initialValue, UnityEngine.Events.UnityAction<bool> onChanged)
    {
        GameObject toggleObject = CreateRect($"Toggle_{label}", parent).gameObject;
        HorizontalLayoutGroup layout = toggleObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        SetLayoutSize(toggleObject, 138f, 34f);

        GameObject backgroundObject = CreateRect("Background", toggleObject.transform).gameObject;
        SetLayoutSize(backgroundObject, 44f, 24f);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = initialValue ? ToggleOnColor : ToggleOffColor;
        AddOutline(backgroundObject, BorderColor, new Vector2(1f, -1f));

        Toggle toggle = backgroundObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;

        GameObject checkmarkObject = CreateRect("Knob", backgroundObject.transform).gameObject;
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
        checkmarkRect.pivot = new Vector2(0f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(16f, 16f);
        checkmarkRect.anchoredPosition = new Vector2(initialValue ? 24f : 4f, 0f);
        Image checkmark = checkmarkObject.AddComponent<Image>();
        checkmark.color = new Color(0.98f, 0.96f, 0.92f, 1f);
        toggle.graphic = checkmark;
        toggle.isOn = initialValue;
        toggle.onValueChanged.AddListener(value =>
        {
            background.color = value ? ToggleOnColor : ToggleOffColor;
            checkmarkRect.anchoredPosition = new Vector2(value ? 24f : 4f, 0f);
            onChanged?.Invoke(value);
        });

        TextMeshProUGUI labelText = CreateText("Label", toggleObject.transform, label, 12f, TextMutedColor, TextAlignmentOptions.MidlineLeft);
        SetLayoutSize(labelText.gameObject, 84f, 28f);

        return toggle;
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        Image background = buttonObject.AddComponent<Image>();
        background.color = color;
        AddOutline(buttonObject, BorderColor, new Vector2(1f, -1f));

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, ButtonHighlightColor, 0.55f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 13f, TextColor, TextAlignmentOptions.Center);
        StretchFull(text.rectTransform);
        return button;
    }

    private Transform CreateHorizontalRow(string name, Transform parent, float spacing, RectOffset padding, TextAnchor alignment)
    {
        Transform row = CreateRect(name, parent);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding;
        layout.childAlignment = alignment;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateRect(name, parent).gameObject;
        Image background = panel.AddComponent<Image>();
        background.color = color;
        AddOutline(panel, BorderColor, new Vector2(1f, -1f));
        return panel;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRect(name, parent).gameObject;
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = new Vector4(2f, 4f, 2f, 4f);
        text.extraPadding = true;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }

        return text;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject objectRef = new GameObject(name, typeof(RectTransform));
        objectRef.transform.SetParent(parent, false);
        return objectRef.GetComponent<RectTransform>();
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetLayoutSize(GameObject objectRef, float width, float height)
    {
        LayoutElement layout = objectRef.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }

    private void AddOutline(GameObject target, Color color, Vector2 effectDistance)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = effectDistance;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}

public interface IDebugModule
{
    void Init(RectTransform parent);
    void Refresh();
}
#endif
