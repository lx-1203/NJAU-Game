#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 钟山台 —— 调试控制台 UI 层
/// 纯代码构建全部界面元素，包含标签页切换和预设按钮栏
/// </summary>
public class DebugConsoleUI : MonoBehaviour
{
    // ========== 静态引用 ==========
    private static DebugConsoleUI instance;

    // ========== 布局常量 ==========
    private const float TopBarHeight = 48f;
    private const float BottomBarHeight = 120f;  // 两行: 步长选择 + 增减按钮
    private const float SidebarWidth = 140f;
    private const float Padding = 6f;

    // ========== 颜色方案 ==========
    private static readonly Color BgColor         = new Color(0.05f, 0.05f, 0.08f, 0.85f);
    private static readonly Color PanelBgColor    = new Color(0.08f, 0.08f, 0.12f, 0.90f);
    private static readonly Color TopBarColor     = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color SidebarColor    = new Color(0.07f, 0.07f, 0.11f, 0.92f);
    private static readonly Color ContentBgColor  = new Color(0.06f, 0.06f, 0.10f, 0.88f);
    private static readonly Color BottomBarColor  = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color TextWhite       = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold        = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TabNormal       = new Color(0.15f, 0.15f, 0.22f, 0.80f);
    private static readonly Color TabSelected     = new Color(0.25f, 0.35f, 0.55f, 1.0f);
    private static readonly Color BtnNormal       = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color BtnHover        = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color BtnPressed      = new Color(0.15f, 0.25f, 0.50f, 1.0f);

    // ========== UI 引用 ==========
    private Canvas canvas;
    private CanvasGroup rootCanvasGroup;
    private RectTransform contentArea;

    // 标签页系统
    private readonly string[] tabNames = { "属性", "时间", "结局", "事件", "NPC", "经济", "公式", "快照", "日志" };
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<Image> tabBgImages = new List<Image>();
    private readonly List<GameObject> modulePanels = new List<GameObject>();
    private int activeTabIndex = -1;

    // 模块引用
    private AttributeModule attributeModule;
    private TimeModule timeModule;
    private EndingSimModule endingSimModule;
    private EventModule eventModule;
    private NPCModule npcModule;
    private EconomyModule economyModule;
    private FormulaModule formulaModule;
    private SnapshotModule snapshotModule;
    private LogModule logModule;

    // ========== 生命周期 ==========

    private void Awake()
    {
        instance = this;
        BuildUI();
        SwitchTab(0);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // ========== 静态方法 ==========

    public static void Show()
    {
        if (instance == null) return;
        instance.gameObject.SetActive(true);
        if (instance.rootCanvasGroup != null)
        {
            instance.rootCanvasGroup.alpha = 1f;
            instance.rootCanvasGroup.interactable = true;
            instance.rootCanvasGroup.blocksRaycasts = true;
        }
        // 刷新当前活跃模块 + 底栏数值
        instance.RefreshActiveModule();
        instance.RefreshAdjustValues();
    }

    public static void Hide()
    {
        if (instance == null) return;
        if (instance.rootCanvasGroup != null)
        {
            instance.rootCanvasGroup.alpha = 0f;
            instance.rootCanvasGroup.interactable = false;
            instance.rootCanvasGroup.blocksRaycasts = false;
        }
    }

    // ========== UI 构建 ==========

    private void BuildUI()
    {
        CreateCanvas();
        CreateBackground();
        CreateTopBar();
        CreateSidebar();
        CreateContentArea();
        CreateBottomBar();
        CreateModulePanels();
    }

    private void CreateCanvas()
    {
        // 确保 EventSystem 存在
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("DebugCanvas");
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        rootCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
    }

    private void CreateBackground()
    {
        GameObject bg = CreateUIElement("Background", canvas.transform);
        StretchFull(bg.GetComponent<RectTransform>());
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = BgColor;
        bgImg.raycastTarget = true; // 阻挡下层交互
    }

    private void CreateTopBar()
    {
        GameObject topBar = CreatePanel("TopBar", canvas.transform, TopBarColor);
        RectTransform topRT = topBar.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0, 1);
        topRT.anchorMax = new Vector2(1, 1);
        topRT.pivot = new Vector2(0.5f, 1);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0, TopBarHeight);

        // 标题
        TextMeshProUGUI titleText = CreateTMPText("Title", topBar.transform,
            "钟山台 · 开发者控制台", 20f, TextGold, TextAlignmentOptions.Left,
            new Vector2(400, TopBarHeight));
        RectTransform titleRT = titleText.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.5f);
        titleRT.anchorMax = new Vector2(0, 0.5f);
        titleRT.pivot = new Vector2(0, 0.5f);
        titleRT.anchoredPosition = new Vector2(16, 0);

        // 关闭按钮 (X)
        GameObject closeBtnObj = CreateUIElement("CloseBtn", topBar.transform);
        RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f);
        closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.sizeDelta = new Vector2(40, 40);
        closeRT.anchoredPosition = new Vector2(-10, 0);

        Image closeBg = closeBtnObj.AddComponent<Image>();
        closeBg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        ColorBlock closeCb = closeBtn.colors;
        closeCb.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        closeCb.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        closeCb.pressedColor = new Color(0.6f, 0.15f, 0.15f, 1f);
        closeBtn.colors = closeCb;
        closeBtn.onClick.AddListener(() => DebugConsoleManager.Instance?.Close());

        TextMeshProUGUI closeText = CreateTMPText("X", closeBtnObj.transform,
            "✕", 22f, TextWhite, TextAlignmentOptions.Center, new Vector2(40, 40));
        StretchFull(closeText.GetComponent<RectTransform>());
    }

    private void CreateSidebar()
    {
        GameObject sidebar = CreatePanel("Sidebar", canvas.transform, SidebarColor);
        RectTransform sideRT = sidebar.GetComponent<RectTransform>();
        sideRT.anchorMin = new Vector2(0, 0);
        sideRT.anchorMax = new Vector2(0, 1);
        sideRT.pivot = new Vector2(0, 0.5f);
        sideRT.anchoredPosition = new Vector2(0, (BottomBarHeight - TopBarHeight) / 2f);
        sideRT.sizeDelta = new Vector2(SidebarWidth, -(TopBarHeight + BottomBarHeight));

        // 垂直布局
        VerticalLayoutGroup vlg = sidebar.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(6, 6, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 创建标签按钮
        for (int i = 0; i < tabNames.Length; i++)
        {
            int tabIndex = i;
            CreateTabButton(sidebar.transform, tabNames[i], tabIndex);
        }
    }

    private void CreateTabButton(Transform parent, string label, int tabIndex)
    {
        GameObject btnObj = CreateUIElement("Tab_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(SidebarWidth - 12, 42);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = TabNormal;
        tabBgImages.Add(btnBg);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        btn.colors = cb;
        btn.onClick.AddListener(() => SwitchTab(tabIndex));
        tabButtons.Add(btn);

        TextMeshProUGUI txt = CreateTMPText("Label", btnObj.transform,
            label, 16f, TextWhite, TextAlignmentOptions.Center,
            new Vector2(SidebarWidth - 12, 42));
        StretchFull(txt.GetComponent<RectTransform>());
    }

    private void CreateContentArea()
    {
        GameObject content = CreatePanel("ContentArea", canvas.transform, ContentBgColor);
        contentArea = content.GetComponent<RectTransform>();
        contentArea.anchorMin = new Vector2(0, 0);
        contentArea.anchorMax = new Vector2(1, 1);
        contentArea.offsetMin = new Vector2(SidebarWidth, BottomBarHeight);
        contentArea.offsetMax = new Vector2(0, -TopBarHeight);
    }

    // ========== 增减按钮底栏 ==========

    // 属性增减 UI 引用
    private readonly string[] adjustAttrNames = { "学力", "魅力", "体魄", "领导力", "压力", "心情", "黑暗值", "负罪感", "幸运", "金钱" };
    private readonly List<TextMeshProUGUI> adjustValueTexts = new List<TextMeshProUGUI>();
    private readonly List<Image> stepBtnImages = new List<Image>();
    private static readonly Color StepNormal   = new Color(0.18f, 0.18f, 0.26f, 0.90f);
    private static readonly Color StepSelected = new Color(0.30f, 0.50f, 0.75f, 1.0f);
    private static readonly Color MinusBtnColor = new Color(0.65f, 0.25f, 0.25f, 1.0f);
    private static readonly Color PlusBtnColor  = new Color(0.25f, 0.55f, 0.30f, 1.0f);

    private void CreateBottomBar()
    {
        GameObject bottomBar = CreatePanel("BottomBar", canvas.transform, BottomBarColor);
        RectTransform botRT = bottomBar.GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0, 0);
        botRT.anchorMax = new Vector2(1, 0);
        botRT.pivot = new Vector2(0.5f, 0);
        botRT.anchoredPosition = Vector2.zero;
        botRT.sizeDelta = new Vector2(0, BottomBarHeight);

        // 纵向布局：第一行步长选择，第二行属性增减
        VerticalLayoutGroup vlg = bottomBar.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(10, 10, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ---- 第一行: 步长选择 ----
        CreateStepRow(bottomBar.transform);

        // ---- 第二行: 属性增减按钮 ----
        CreateAdjustRow(bottomBar.transform);
    }

    private void CreateStepRow(Transform parent)
    {
        GameObject row = CreateUIElement("StepRow", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 32f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 标签
        TextMeshProUGUI label = CreateTMPText("StepLabel", row.transform,
            "步长:", 14f, TextGold, TextAlignmentOptions.Right, new Vector2(50, 28));
        LayoutElement labelLE = label.gameObject.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 50f;

        // 4 个步长按钮
        int[] steps = DebugPresets.GetStepOptions();
        for (int i = 0; i < steps.Length; i++)
        {
            int idx = i;
            CreateStepButton(row.transform, steps[i].ToString(), idx);
        }

        // 初始高亮当前步长
        RefreshStepHighlight();
    }

    private void CreateStepButton(Transform parent, string label, int stepIndex)
    {
        GameObject btnObj = CreateUIElement("Step_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(56, 28);

        Image btnBg = btnObj.AddComponent<Image>();
        bool isActive = stepIndex == DebugPresets.CurrentStepIndex;
        btnBg.color = isActive ? StepSelected : StepNormal;
        stepBtnImages.Add(btnBg);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            DebugPresets.SetStepIndex(stepIndex);
            RefreshStepHighlight();
        });

        TextMeshProUGUI txt = CreateTMPText("Label", btnObj.transform,
            label, 14f, TextWhite, TextAlignmentOptions.Center, new Vector2(56, 28));
        StretchFull(txt.GetComponent<RectTransform>());
    }

    private void RefreshStepHighlight()
    {
        for (int i = 0; i < stepBtnImages.Count; i++)
        {
            stepBtnImages[i].color = (i == DebugPresets.CurrentStepIndex) ? StepSelected : StepNormal;
        }
    }

    private void CreateAdjustRow(Transform parent)
    {
        GameObject row = CreateUIElement("AdjustRow", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 70f);

        // 使用 GridLayoutGroup 实现 5×2 布局
        GridLayoutGroup grid = row.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160f, 30f);
        grid.spacing = new Vector2(6f, 4f);
        grid.padding = new RectOffset(4, 4, 2, 2);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        // 10 个属性: 每个 = [-] 名称 数值 [+]
        for (int i = 0; i < adjustAttrNames.Length; i++)
        {
            int idx = i;
            CreateAdjustCell(row.transform, adjustAttrNames[i], idx);
        }

        // 初始刷新数值
        RefreshAdjustValues();
    }

    private void CreateAdjustCell(Transform parent, string attrName, int index)
    {
        GameObject cell = CreateUIElement("Cell_" + attrName, parent);

        HorizontalLayoutGroup hlg = cell.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 2f;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // [-] 按钮
        CreateSmallButton(cell.transform, "-", MinusBtnColor, 24f, () =>
        {
            DebugPresets.AdjustAttribute(attrName, false);
            RefreshAdjustValues();
            RefreshActiveModule();
        });

        // 属性名
        string shortName = attrName.Length > 3 ? attrName.Substring(0, 3) : attrName;
        TextMeshProUGUI nameText = CreateTMPText("Name", cell.transform,
            shortName, 12f, TextWhite, TextAlignmentOptions.Center, new Vector2(42, 26));
        LayoutElement nameLE = nameText.gameObject.AddComponent<LayoutElement>();
        nameLE.preferredWidth = 42f;

        // 数值
        TextMeshProUGUI valText = CreateTMPText("Value", cell.transform,
            "0", 13f, TextGold, TextAlignmentOptions.Center, new Vector2(50, 26));
        LayoutElement valLE = valText.gameObject.AddComponent<LayoutElement>();
        valLE.preferredWidth = 50f;
        adjustValueTexts.Add(valText);

        // [+] 按钮
        CreateSmallButton(cell.transform, "+", PlusBtnColor, 24f, () =>
        {
            DebugPresets.AdjustAttribute(attrName, true);
            RefreshAdjustValues();
            RefreshActiveModule();
        });
    }

    private void CreateSmallButton(Transform parent, string label, Color bgColor, float size,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI txt = CreateTMPText("Label", btnObj.transform,
            label, 16f, TextWhite, TextAlignmentOptions.Center, new Vector2(size, size));
        txt.fontStyle = FontStyles.Bold;
        StretchFull(txt.GetComponent<RectTransform>());
    }

    /// <summary>刷新底栏所有属性数值显示</summary>
    private void RefreshAdjustValues()
    {
        for (int i = 0; i < adjustAttrNames.Length && i < adjustValueTexts.Count; i++)
        {
            int val = DebugPresets.GetAttributeValue(adjustAttrNames[i]);
            adjustValueTexts[i].text = adjustAttrNames[i] == "金钱" ? $"¥{val}" : val.ToString();
        }
    }

    // ========== 模块面板创建 ==========

    private void CreateModulePanels()
    {
        // 0: 属性
        attributeModule = CreateModulePanel<AttributeModule>("AttributePanel");
        // 1: 时间
        timeModule = CreateModulePanel<TimeModule>("TimePanel");
        // 2: 结局
        endingSimModule = CreateModulePanel<EndingSimModule>("EndingSimPanel");
        // 3: 事件
        eventModule = CreateModulePanel<EventModule>("EventPanel");
        // 4: NPC
        npcModule = CreateModulePanel<NPCModule>("NPCPanel");
        // 5: 经济
        economyModule = CreateModulePanel<EconomyModule>("EconomyPanel");
        // 6: 公式
        formulaModule = CreateModulePanel<FormulaModule>("FormulaPanel");
        // 7: 快照
        snapshotModule = CreateModulePanel<SnapshotModule>("SnapshotPanel");
        // 8: 日志
        logModule = CreateModulePanel<LogModule>("LogPanel");
    }

    private T CreateModulePanel<T>(string panelName) where T : MonoBehaviour, IDebugModule
    {
        GameObject panelObj = CreateUIElement(panelName, contentArea);
        StretchFull(panelObj.GetComponent<RectTransform>());

        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        T module = panelObj.AddComponent<T>();
        module.Init(panelObj.GetComponent<RectTransform>());

        modulePanels.Add(panelObj);
        panelObj.SetActive(false);
        return module;
    }

    // ========== 标签页切换 ==========

    private void SwitchTab(int index)
    {
        if (index == activeTabIndex) return;

        // 隐藏旧面板
        if (activeTabIndex >= 0 && activeTabIndex < modulePanels.Count)
        {
            SetPanelActive(modulePanels[activeTabIndex], false);
            tabBgImages[activeTabIndex].color = TabNormal;
        }

        activeTabIndex = index;

        // 显示新面板
        if (activeTabIndex >= 0 && activeTabIndex < modulePanels.Count)
        {
            SetPanelActive(modulePanels[activeTabIndex], true);
            tabBgImages[activeTabIndex].color = TabSelected;
            RefreshActiveModule();
        }
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        panel.SetActive(active);
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = active ? 1f : 0f;
            cg.interactable = active;
            cg.blocksRaycasts = active;
        }
    }

    private void RefreshActiveModule()
    {
        if (activeTabIndex < 0) return;

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

    // ========== 工具方法 ==========

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
            rt = go.AddComponent<RectTransform>();
        return go;
    }

    private GameObject CreatePanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = CreateUIElement(name, parent);
        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;
        return panel;
    }

    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string text,
        float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size)
    {
        GameObject obj = CreateUIElement(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            tmp.font = FontManager.Instance.ChineseFont;
        }

        return tmp;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

/// <summary>调试模块接口</summary>
public interface IDebugModule
{
    void Init(RectTransform parent);
    void Refresh();
}
#endif
