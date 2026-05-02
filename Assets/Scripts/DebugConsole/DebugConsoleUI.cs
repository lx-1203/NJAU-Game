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
    private const float BottomBarHeight = 132f;
    private const float SidebarWidth = 156f;

    private static readonly Color BackgroundColor = new Color(0.04f, 0.04f, 0.07f, 0.9f);
    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color HeaderColor = new Color(0.1f, 0.1f, 0.16f, 0.98f);
    private static readonly Color SidebarColor = new Color(0.07f, 0.07f, 0.11f, 0.96f);
    private static readonly Color ContentColor = new Color(0.06f, 0.06f, 0.1f, 0.92f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.72f, 1f);
    private static readonly Color TabNormalColor = new Color(0.16f, 0.16f, 0.24f, 1f);
    private static readonly Color TabSelectedColor = new Color(0.28f, 0.46f, 0.76f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f, 1f);
    private static readonly Color PositiveColor = new Color(0.22f, 0.58f, 0.34f, 1f);
    private static readonly Color NegativeColor = new Color(0.68f, 0.28f, 0.28f, 1f);
    private static readonly Color ToggleOffColor = new Color(0.26f, 0.26f, 0.32f, 1f);
    private static readonly Color ToggleOnColor = new Color(0.24f, 0.54f, 0.76f, 1f);
    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);

    private static readonly string[] TabNames =
    {
        "属性",
        "时间",
        "结局",
        "事件",
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
    private readonly List<Image> tabImages = new List<Image>();
    private readonly List<GameObject> modulePanels = new List<GameObject>();
    private readonly List<QuickAdjustItem> quickAdjustItems = new List<QuickAdjustItem>();
    private readonly List<Image> stepButtonImages = new List<Image>();

    private Toggle skipSplashToggle;
    private Toggle skipCreateToggle;
    private Toggle skipIntroToggle;
    private Button prevDialogueButton;

    private AttributeModule attributeModule;
    private TimeModule timeModule;
    private EndingSimModule endingSimModule;
    private EventModule eventModule;
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

        GameObject background = CreatePanel("Background", canvasObject.transform, BackgroundColor);
        StretchFull(background.GetComponent<RectTransform>());

        CreateTopBar(canvasObject.transform);
        CreateSidebar(canvasObject.transform);
        CreateContentArea(canvasObject.transform);
        CreateBottomBar(canvasObject.transform);
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

        TextMeshProUGUI title = CreateText("Title", topBar.transform, "钟山台", 24f, AccentColor, TextAlignmentOptions.MidlineLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(320f, 34f);
        titleRect.anchoredPosition = new Vector2(18f, -14f);

        TextMeshProUGUI subtitle = CreateText("Subtitle", topBar.transform, "快速测试，清晰切换，把调试台收拾得明明白白。", 13f, TextColor, TextAlignmentOptions.MidlineLeft);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(-36f, 24f);
        subtitleRect.anchoredPosition = new Vector2(18f, -48f);

        Transform actionRow = CreateHorizontalRow("ActionRow", topBar.transform, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        RectTransform actionRect = actionRow.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0f, 0f);
        actionRect.anchorMax = new Vector2(1f, 0f);
        actionRect.pivot = new Vector2(0.5f, 0f);
        actionRect.offsetMin = new Vector2(18f, 10f);
        actionRect.offsetMax = new Vector2(-18f, 46f);

        prevDialogueButton = CreateButton(actionRow, "上一句", ButtonColor, () =>
        {
            DialogueSystem.Instance?.DebugStepBackOneLine();
            RefreshTopActionButtons();
        });
        SetLayoutSize(prevDialogueButton.gameObject, 110f, 34f);

        Button quickStartButton = CreateButton(actionRow, "快速开局", ButtonColor, () =>
        {
            StartupFlowSettings.ApplyQuickStartPreset();
            SyncTopControls();
            DebugConsoleManager.Log("Startup", "Applied quick-start preset");
        });
        SetLayoutSize(quickStartButton.gameObject, 110f, 34f);

        skipSplashToggle = CreateLabeledToggle(actionRow, "跳过开屏", StartupFlowSettings.SkipSplashLogo, value =>
        {
            StartupFlowSettings.SkipSplashLogo = value;
        });

        skipCreateToggle = CreateLabeledToggle(actionRow, "跳过建角", StartupFlowSettings.SkipCharacterCreation, value =>
        {
            StartupFlowSettings.SkipCharacterCreation = value;
        });

        skipIntroToggle = CreateLabeledToggle(actionRow, "跳过开场", StartupFlowSettings.SkipOpeningStory, value =>
        {
            StartupFlowSettings.SkipOpeningStory = value;
        });

        Button closeButton = CreateButton(actionRow, "关闭", NegativeColor, () => DebugConsoleManager.Instance?.Close());
        SetLayoutSize(closeButton.gameObject, 88f, 34f);
    }

    private void CreateSidebar(Transform parent)
    {
        GameObject sidebar = CreatePanel("Sidebar", parent, SidebarColor);
        RectTransform sidebarRect = sidebar.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(0f, 0f);
        sidebarRect.anchorMax = new Vector2(0f, 1f);
        sidebarRect.pivot = new Vector2(0f, 0.5f);
        sidebarRect.anchoredPosition = new Vector2(0f, (BottomBarHeight - TopBarHeight) * 0.5f);
        sidebarRect.sizeDelta = new Vector2(SidebarWidth, -(TopBarHeight + BottomBarHeight));

        VerticalLayoutGroup layout = sidebar.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 10, 10);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < TabNames.Length; i++)
        {
            int capturedIndex = i;
            GameObject tabObject = CreateRect($"Tab_{TabNames[i]}", sidebar.transform).gameObject;
            RectTransform tabRect = tabObject.GetComponent<RectTransform>();
            tabRect.sizeDelta = new Vector2(SidebarWidth - 16f, 40f);

            Image background = tabObject.AddComponent<Image>();
            background.color = TabNormalColor;
            tabImages.Add(background);

            Button button = tabObject.AddComponent<Button>();
            button.onClick.AddListener(() => SwitchTab(capturedIndex));

            TextMeshProUGUI label = CreateText("Label", tabObject.transform, TabNames[i], 15f, TextColor, TextAlignmentOptions.Center);
            StretchFull(label.rectTransform);
        }
    }

    private void CreateContentArea(Transform parent)
    {
        GameObject content = CreatePanel("ContentArea", parent, ContentColor);
        contentArea = content.GetComponent<RectTransform>();
        contentArea.anchorMin = new Vector2(0f, 0f);
        contentArea.anchorMax = new Vector2(1f, 1f);
        contentArea.offsetMin = new Vector2(SidebarWidth, BottomBarHeight);
        contentArea.offsetMax = new Vector2(0f, -TopBarHeight);
    }

    private void CreateBottomBar(Transform parent)
    {
        GameObject bottomBar = CreatePanel("BottomBar", parent, HeaderColor);
        RectTransform bottomRect = bottomBar.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = new Vector2(0f, BottomBarHeight);

        VerticalLayoutGroup layout = bottomBar.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Transform stepRow = CreateHorizontalRow("StepRow", bottomBar.transform, 8f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        stepRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
        TextMeshProUGUI stepLabel = CreateText("StepLabel", stepRow, "步进", 14f, AccentColor, TextAlignmentOptions.MidlineLeft);
        SetLayoutSize(stepLabel.gameObject, 44f, 28f);

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
            SetLayoutSize(stepButton.gameObject, 58f, 28f);
        }

        Transform gridRoot = CreateRect("AdjustGridRoot", bottomBar.transform);
        gridRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
        GridLayoutGroup grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(168f, 34f);
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
        Transform row = CreateHorizontalRow($"Adjust_{key}", parent, 4f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleCenter);

        Button minusButton = CreateButton(row, "-", NegativeColor, () =>
        {
            DebugPresets.AdjustAttribute(key, false);
            RefreshQuickAdjustValues();
            RefreshActiveModule();
        });
        SetLayoutSize(minusButton.gameObject, 28f, 28f);

        TextMeshProUGUI labelText = CreateText("Name", row, label, 12f, TextColor, TextAlignmentOptions.Center);
        SetLayoutSize(labelText.gameObject, 56f, 28f);

        TextMeshProUGUI valueText = CreateText("Value", row, "0", 13f, AccentColor, TextAlignmentOptions.Center);
        SetLayoutSize(valueText.gameObject, 52f, 28f);
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
        SetLayoutSize(plusButton.gameObject, 28f, 28f);
    }

    private void CreateModulePanels()
    {
        attributeModule = CreateModulePanel<AttributeModule>("AttributePanel");
        timeModule = CreateModulePanel<TimeModule>("TimePanel");
        endingSimModule = CreateModulePanel<EndingSimModule>("EndingPanel");
        eventModule = CreateModulePanel<EventModule>("EventPanel");
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
            case 2: endingSimModule?.Refresh(); break;
            case 3: eventModule?.Refresh(); break;
            case 4: npcModule?.Refresh(); break;
            case 5: economyModule?.Refresh(); break;
            case 6: formulaModule?.Refresh(); break;
            case 7: snapshotModule?.Refresh(); break;
            case 8: logModule?.Refresh(); break;
        }
    }

    private void RefreshQuickAdjustValues()
    {
        foreach (QuickAdjustItem item in quickAdjustItems)
        {
            int value = DebugPresets.GetAttributeValue(item.key);
            item.valueText.text = item.key == "Money" ? $"${value}" : value.ToString();
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
        SetLayoutSize(toggleObject, 150f, 34f);

        GameObject backgroundObject = CreateRect("Background", toggleObject.transform).gameObject;
        SetLayoutSize(backgroundObject, 56f, 24f);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = initialValue ? ToggleOnColor : ToggleOffColor;

        Toggle toggle = backgroundObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;

        GameObject checkmarkObject = CreateRect("Checkmark", backgroundObject.transform).gameObject;
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0f, 0f);
        checkmarkRect.anchorMax = new Vector2(0f, 1f);
        checkmarkRect.pivot = new Vector2(0f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(24f, 0f);
        Image checkmark = checkmarkObject.AddComponent<Image>();
        checkmark.color = Color.white;
        toggle.graphic = checkmark;
        toggle.isOn = initialValue;
        toggle.onValueChanged.AddListener(value =>
        {
            background.color = value ? ToggleOnColor : ToggleOffColor;
            onChanged?.Invoke(value);
        });

        TextMeshProUGUI labelText = CreateText("Label", toggleObject.transform, label, 13f, TextColor, TextAlignmentOptions.MidlineLeft);
        SetLayoutSize(labelText.gameObject, 88f, 28f);

        return toggle;
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        Image background = buttonObject.AddComponent<Image>();
        background.color = color;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.85f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 14f, Color.white, TextAlignmentOptions.Center);
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
