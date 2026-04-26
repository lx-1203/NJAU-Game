using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using static PhysicalTestSystem;

/// <summary>
/// 体能测试 UI 构建器 —— 纯代码动态创建体测界面和成绩界面
/// </summary>
public class PhysicalTestUI : MonoBehaviour
{
    // ========== 单例 ==========
    public static PhysicalTestUI Instance { get; private set; }

    // ========== 事件 ==========
    public event Action OnUIClosed;

    // ========== 颜色方案 ==========

    public static readonly Color PanelBgColor       = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    public static readonly Color HeaderBgColor      = new Color(0.12f, 0.12f, 0.18f, 1.0f);

    // 策略按钮颜色
    public static readonly Color StrategyConservativeNormal  = new Color(0.20f, 0.60f, 0.30f, 1.0f);
    public static readonly Color StrategyConservativeHover   = new Color(0.30f, 0.70f, 0.40f, 1.0f);

    public static readonly Color StrategyPassiveNormal     = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    public static readonly Color StrategyPassiveHover      = new Color(0.30f, 0.45f, 0.70f, 1.0f);

    public static readonly Color StrategyAggressiveNormal    = new Color(0.80f, 0.20f, 0.20f, 1.0f);
    public static readonly Color StrategyAggressiveHover     = new Color(0.90f, 0.30f, 0.30f, 1.0f);

    public static readonly Color TextWhite          = Color.white;
    public static readonly Color TextGold           = new Color(1.0f, 0.85f, 0.3f, 1.0f);
    public static readonly Color TextFail           = new Color(1.0f, 0.3f, 0.3f, 1.0f);
    public static readonly Color TextDesc           = new Color(0.7f, 0.7f, 0.7f, 1.0f);

    public static readonly Color ConfirmBtnColor    = new Color(0.20f, 0.50f, 0.85f, 1.0f);
    public static readonly Color UnselectedColor    = new Color(0.15f, 0.15f, 0.20f, 1.0f);

    // ========== 引用 ==========

    private GameObject ptCanvasObj;
    private GameObject testPanel;
    private GameObject resultPanel;

    private TextMeshProUGUI currentPhysiqueText;

    // 结果面板引用
    private TextMeshProUGUI resultRunText;
    private TextMeshProUGUI resultStrengthText;
    private TextMeshProUGUI resultJumpText;
    private TextMeshProUGUI resultTotalText;
    private TextMeshProUGUI resultGradeText;

    // 当前选择的策略 3项目 x 3阶段 = 9
    private TestStrategy[] currentStrategies = new TestStrategy[9];

    // 策略按钮字典，用于高亮控制
    // Key: index (0-8), Value: Dictionary<TestStrategy, Image>
    private Dictionary<int, Dictionary<TestStrategy, Image>> strategyButtons = new Dictionary<int, Dictionary<TestStrategy, Image>>();

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
        UIFlowGuard.EnsureEventSystem();

        // 默认初始化全部为Passive（平衡）
        for (int i = 0; i < 9; i++)
        {
            currentStrategies[i] = TestStrategy.Passive;
        }

        BuildUI();
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    // ========== UI 构建 ==========

    public void BuildUI()
    {
        CreateCanvas();
        BuildTestPanel();
        BuildResultPanel();

        // 默认隐藏
        if (ptCanvasObj != null) ptCanvasObj.SetActive(false);
        testPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    public void Show()
    {
        // 刷新体魄显示
        if (ptCanvasObj != null) ptCanvasObj.SetActive(true);

        if (PlayerAttributes.Instance != null && currentPhysiqueText != null)
        {
            currentPhysiqueText.text = $"当前体魄: {PlayerAttributes.Instance.Physique}";
        }

        // 重置策略为Passive
        for (int i = 0; i < 9; i++)
        {
            SelectStrategy(i, TestStrategy.Passive);
        }

        testPanel.SetActive(true);
        resultPanel.SetActive(false);
    }

    public void Hide()
    {
        if (ptCanvasObj != null) ptCanvasObj.SetActive(false);
        if (testPanel != null) testPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        OnUIClosed?.Invoke();
    }

    public bool IsOpen =>
        (testPanel != null && testPanel.activeSelf) ||
        (resultPanel != null && resultPanel.activeSelf);

    // ========== 面板构建 ==========

    private void CreateCanvas()
    {
        ptCanvasObj = new GameObject("PhysicalTestCanvas");
        ptCanvasObj.transform.SetParent(transform, false);

        Canvas canvas = ptCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // 与考试一样的高优先级，强制覆盖其他UI

        CanvasScaler scaler = ptCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        ptCanvasObj.AddComponent<GraphicRaycaster>();

        // 添加全局遮罩背景
        GameObject bgObj = CreatePanel("Background", ptCanvasObj.transform, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.7f));
        // 不绑定点击隐藏事件，强制交互
    }

    private void BuildTestPanel()
    {
        testPanel = CreatePanel("TestPanel", ptCanvasObj.transform,
            new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f), PanelBgColor);

        // 标题区域
        GameObject headerArea = CreatePanel("HeaderArea", testPanel.transform,
            new Vector2(0f, 0.88f), new Vector2(1f, 1f), HeaderBgColor);

        CreateTMPText("Title", headerArea.transform, "体能测试", 32, TextGold, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 1f));

        // 描述文本
        CreateTMPText("Desc", testPanel.transform, "选择每个项目的策略组合", 22, TextWhite, TextAlignmentOptions.Center,
            new Vector2(0f, 0.82f), new Vector2(1f, 0.88f));

        // 当前体魄显示
        currentPhysiqueText = CreateTMPText("PhysiqueText", testPanel.transform, "当前体魄: 50", 22, TextGold, TextAlignmentOptions.MidlineRight,
            new Vector2(0.6f, 0.82f), new Vector2(0.95f, 0.88f));

        // 测项列表区域
        GameObject itemsArea = CreatePanel("ItemsArea", testPanel.transform,
            new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.80f), new Color(0, 0, 0, 0));

        VerticalLayoutGroup vlg = itemsArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childForceExpandHeight = true;
        vlg.childForceExpandWidth = true;

        // 三个测试项目
        BuildTestItemCard(itemsArea.transform, "中长跑", 0, new[] {"起跑阶段", "途中跑", "最后冲刺"});
        BuildTestItemCard(itemsArea.transform, "力量测试", 3, new[] {"开始发力", "中段坚持", "最后时刻"});
        BuildTestItemCard(itemsArea.transform, "立定跳远", 6, new[] {"第一次尝试", "第二次尝试", "第三次尝试"});

        // 底部按钮区域
        GameObject footerArea = CreatePanel("FooterArea", testPanel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.12f), new Color(0, 0, 0, 0));

        // 开始测试按钮
        Button startBtn = CreateButton("StartBtn", footerArea.transform, "开始测试",
            new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.8f), ConfirmBtnColor);
        startBtn.onClick.AddListener(OnStartTestClicked);
    }

    private void BuildTestItemCard(Transform parent, string title, int startIndex, string[] phaseNames)
    {
        GameObject card = CreatePanel($"Card_{title}", parent, Vector2.zero, Vector2.one, HeaderBgColor);

        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.padding = new RectOffset(15, 15, 10, 10);
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        // 卡片标题
        GameObject titleObj = new GameObject("CardTitle");
        titleObj.transform.SetParent(card.transform, false);
        LayoutElement le = titleObj.AddComponent<LayoutElement>();
        le.minHeight = 35f;
        le.preferredHeight = 35f;

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 24;
        titleText.color = TextGold;
        titleText.font = GetChineseFont();

        // 阶段行
        for (int i = 0; i < 3; i++)
        {
            BuildPhaseRow(card.transform, phaseNames[i], startIndex + i);
        }
    }

    private void BuildPhaseRow(Transform parent, string phaseName, int index)
    {
        GameObject row = new GameObject($"Phase_{index}");
        row.transform.SetParent(parent, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 45f;
        le.preferredHeight = 45f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15f;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;

        // 阶段名
        GameObject nameObj = new GameObject("PhaseName");
        nameObj.transform.SetParent(row.transform, false);
        LayoutElement leName = nameObj.AddComponent<LayoutElement>();
        leName.minWidth = 150f;
        leName.preferredWidth = 150f;

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = phaseName;
        nameText.fontSize = 20;
        nameText.color = TextWhite;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.font = GetChineseFont();

        // 策略按钮字典初始化
        strategyButtons[index] = new Dictionary<TestStrategy, Image>();

        // 创建三个策略按钮
        CreateStrategyButton(row.transform, index, TestStrategy.Conservative, "保守", "成功率高 | 得分较低 | 压力小", StrategyConservativeNormal, StrategyConservativeHover);
        CreateStrategyButton(row.transform, index, TestStrategy.Passive, "平衡", "成功率中 | 得分正常 | 压力中", StrategyPassiveNormal, StrategyPassiveHover);
        CreateStrategyButton(row.transform, index, TestStrategy.Aggressive, "激进", "成功率低 | 得分较高 | 压力大", StrategyAggressiveNormal, StrategyAggressiveHover);
    }

    private void CreateStrategyButton(Transform parent, int index, TestStrategy strategy, string label, string tooltip, Color normalCol, Color hoverCol)
    {
        GameObject btnObj = new GameObject($"Btn_{strategy}");
        btnObj.transform.SetParent(parent, false);
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = 200f;
        le.flexibleWidth = 1f;

        Image img = btnObj.AddComponent<Image>();
        img.color = UnselectedColor; // 默认未选中颜色

        Button btn = btnObj.AddComponent<Button>();

        // 主文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0.4f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 20;
        txt.color = TextWhite;
        txt.alignment = TextAlignmentOptions.Bottom;
        txt.font = GetChineseFont();

        // 描述文本
        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(btnObj.transform, false);
        RectTransform descRt = descObj.AddComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 0.4f);
        descRt.offsetMin = new Vector2(0, 5f);
        descRt.offsetMax = Vector2.zero;

        TextMeshProUGUI desc = descObj.AddComponent<TextMeshProUGUI>();
        desc.text = tooltip;
        desc.fontSize = 12;
        desc.color = TextDesc;
        desc.alignment = TextAlignmentOptions.Top;
        desc.font = GetChineseFont();

        // 保存引用
        strategyButtons[index][strategy] = img;

        btn.onClick.AddListener(() => {
            SelectStrategy(index, strategy);
        });
    }

    private void BuildResultPanel()
    {
        resultPanel = CreatePanel("ResultPanel", ptCanvasObj.transform,
            new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f), PanelBgColor);

        // 标题区域
        GameObject headerArea = CreatePanel("HeaderArea", resultPanel.transform,
            new Vector2(0f, 0.85f), new Vector2(1f, 1f), HeaderBgColor);

        CreateTMPText("Title", headerArea.transform, "体能测试成绩单", 32, TextGold, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 1f));

        // 成绩内容区域
        GameObject contentArea = CreatePanel("ContentArea", resultPanel.transform,
            new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.85f), new Color(0,0,0,0));

        VerticalLayoutGroup vlg = contentArea.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15f;
        vlg.padding = new RectOffset(20, 20, 30, 10);
        vlg.childForceExpandHeight = false;

        resultRunText = CreateScoreRow(contentArea.transform, "中长跑");
        resultStrengthText = CreateScoreRow(contentArea.transform, "力量测试");
        resultJumpText = CreateScoreRow(contentArea.transform, "立定跳远");

        // 分割线
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(contentArea.transform, false);
        LayoutElement le = divider.AddComponent<LayoutElement>();
        le.minHeight = 2f;
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);

        resultTotalText = CreateScoreRow(contentArea.transform, "总分");
        resultGradeText = CreateScoreRow(contentArea.transform, "最终评级");

        // 底部确认按钮
        Button confirmBtn = CreateButton("ConfirmBtn", resultPanel.transform, "确认",
            new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.15f), ConfirmBtnColor);
        confirmBtn.onClick.AddListener(Hide);
    }

    private TextMeshProUGUI CreateScoreRow(Transform parent, string label)
    {
        GameObject row = new GameObject($"Row_{label}");
        row.transform.SetParent(parent, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 35f;

        // 标签
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(0.5f, 1f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI labelTxt = labelObj.AddComponent<TextMeshProUGUI>();
        labelTxt.text = label;
        labelTxt.fontSize = 24;
        labelTxt.color = TextWhite;
        labelTxt.alignment = TextAlignmentOptions.MidlineLeft;
        labelTxt.font = GetChineseFont();

        // 值
        GameObject valObj = new GameObject("Value");
        valObj.transform.SetParent(row.transform, false);
        RectTransform valRt = valObj.AddComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.5f, 0f);
        valRt.anchorMax = new Vector2(1f, 1f);
        valRt.offsetMin = Vector2.zero;
        valRt.offsetMax = Vector2.zero;

        TextMeshProUGUI valTxt = valObj.AddComponent<TextMeshProUGUI>();
        valTxt.text = "-";
        valTxt.fontSize = 24;
        valTxt.color = TextGold;
        valTxt.alignment = TextAlignmentOptions.MidlineRight;
        valTxt.font = GetChineseFont();

        return valTxt;
    }

    // ========== 交互逻辑 ==========

    private void SelectStrategy(int index, TestStrategy strategy)
    {
        currentStrategies[index] = strategy;

        // 更新高亮
        foreach (var kvp in strategyButtons[index])
        {
            TestStrategy st = kvp.Key;
            Image img = kvp.Value;

            if (st == strategy)
            {
                // 设置对应的选中颜色
                switch (st)
                {
                    case TestStrategy.Conservative: img.color = StrategyConservativeNormal; break;
                    case TestStrategy.Passive: img.color = StrategyPassiveNormal; break;
                    case TestStrategy.Aggressive: img.color = StrategyAggressiveNormal; break;
                }
            }
            else
            {
                img.color = UnselectedColor;
            }
        }
    }

    private void OnStartTestClicked()
    {
        if (PhysicalTestSystem.Instance == null)
        {
            Debug.LogError("[PhysicalTestUI] 找不到 PhysicalTestSystem.Instance");
            return;
        }

        // 隐藏测试面板
        testPanel.SetActive(false);

        // 调用系统执行测试
        PhysicalTestResult result = PhysicalTestSystem.Instance.ExecuteTest(currentStrategies);

        // 显示成绩单
        ShowResult(result);
    }

    private void ShowResult(PhysicalTestResult result)
    {
        if (ptCanvasObj != null) ptCanvasObj.SetActive(true);

        resultRunText.text = result.runScore.ToString();
        resultRunText.color = result.runScore >= 60 ? TextGold : TextFail;

        resultStrengthText.text = result.strengthScore.ToString();
        resultStrengthText.color = result.strengthScore >= 60 ? TextGold : TextFail;

        resultJumpText.text = result.jumpScore.ToString();
        resultJumpText.color = result.jumpScore >= 60 ? TextGold : TextFail;

        resultTotalText.text = result.totalScore.ToString();
        resultTotalText.color = result.totalScore >= 60 ? TextGold : TextFail;

        // 评级本地化映射
        string gradeStr = result.grade;
        switch (result.grade)
        {
            case "Excellent": gradeStr = "优秀"; resultGradeText.color = new Color(0.2f, 0.8f, 0.3f); break;
            case "Good": gradeStr = "良好"; resultGradeText.color = new Color(0.4f, 0.8f, 0.9f); break;
            case "Pass": gradeStr = "及格"; resultGradeText.color = TextGold; break;
            case "Fail": gradeStr = "不及格"; resultGradeText.color = TextFail; break;
        }
        resultGradeText.text = gradeStr;

        resultPanel.SetActive(true);
    }

    // ========== 工具方法 ==========

    private TMP_FontAsset GetChineseFont()
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            return FontManager.Instance.ChineseFont;
        }
        return null;
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = bgColor;

        return panel;
    }

    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        tmp.font = GetChineseFont();

        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color normalColor)
    {
        GameObject btnObj = CreatePanel(name, parent, anchorMin, anchorMax, normalColor);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color(normalColor.r + 0.1f, normalColor.g + 0.1f, normalColor.b + 0.1f, 1f);
        colors.pressedColor = new Color(normalColor.r - 0.1f, normalColor.g - 0.1f, normalColor.b - 0.1f, 1f);
        btn.colors = colors;

        CreateTMPText(name + "Text", btnObj.transform, label, 24, TextWhite, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        return btn;
    }
}
