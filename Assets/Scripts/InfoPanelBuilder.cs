using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 信息面板UI构建器 - 构建玩家信息、人际关系、任务三个子面板
/// 采用纯代码构建UI，独立Canvas (sortingOrder=180)
/// </summary>
public class InfoPanelBuilder : MonoBehaviour
{
    // ========== Canvas 引用 ==========
    [HideInInspector] public Canvas infoCanvas;
    [HideInInspector] public GameObject panelRoot;
    [HideInInspector] public GameObject overlayObj;

    // ========== 顶部组件 ==========
    [HideInInspector] public Button btnClose;
    [HideInInspector] public Button[] tabButtons; // 3个标签按钮
    [HideInInspector] public TextMeshProUGUI txtTitle;

    // ========== 三个子面板容器 ==========
    [HideInInspector] public GameObject playerInfoPanel;
    [HideInInspector] public GameObject relationshipPanel;
    [HideInInspector] public GameObject questPanel;

    // ========== 个人信息面板组件 ==========
    [HideInInspector] public TextMeshProUGUI txtPlayerName;
    [HideInInspector] public TextMeshProUGUI txtPlayerInfo; // 性别、专业
    [HideInInspector] public TextMeshProUGUI txtTimeInfo;
    [HideInInspector] public Transform attributeContainer;
    [HideInInspector] public TextMeshProUGUI txtStress;
    [HideInInspector] public TextMeshProUGUI txtMood;
    [HideInInspector] public Image imgStressBar;
    [HideInInspector] public Image imgMoodBar;
    [HideInInspector] public TextMeshProUGUI txtHiddenAttrs; // 黑暗值、负罪感、幸运
    [HideInInspector] public TextMeshProUGUI txtGPA;
    [HideInInspector] public TextMeshProUGUI txtCredits;
    [HideInInspector] public TextMeshProUGUI txtMoney;
    [HideInInspector] public TextMeshProUGUI txtDebt;
    [HideInInspector] public TextMeshProUGUI txtClubs;
    [HideInInspector] public TextMeshProUGUI txtParty;

    // ========== 人际关系面板组件 ==========
    [HideInInspector] public Transform npcListContent;
    [HideInInspector] public Transform npcDetailContainer;
    [HideInInspector] public TextMeshProUGUI txtNPCName;
    [HideInInspector] public TextMeshProUGUI txtNPCAffinity;
    [HideInInspector] public Image imgAffinityBar;
    [HideInInspector] public TextMeshProUGUI txtAffinityLevel;
    [HideInInspector] public TextMeshProUGUI txtRomanceState;
    [HideInInspector] public TextMeshProUGUI txtPreferences;
    [HideInInspector] public Transform interactionRecordContainer;
    [HideInInspector] public Button btnSocialInteract;

    // ========== 任务面板组件 ==========
    [HideInInspector] public Transform questListContent;

    // ========== 颜色常量 ==========
    private static readonly Color PanelBgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color HeaderColor = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color CardBgColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    private static readonly Color SectionBgColor = new Color(0.10f, 0.10f, 0.15f, 0.90f);

    private static readonly Color ButtonNormal = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color TabActiveColor = new Color(0.30f, 0.50f, 0.80f, 1.0f);
    private static readonly Color TabInactiveColor = new Color(0.15f, 0.15f, 0.20f, 1.0f);

    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.55f, 0.55f, 0.60f);

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.50f);
    private static readonly Color SeparatorColor = new Color(0.25f, 0.25f, 0.30f, 1.0f);

    // ========== 构建方法 ==========

    public void BuildUI()
    {
        // 1. 创建独立Canvas
        CreateCanvas();

        // 2. 创建遮罩层
        CreateOverlay();

        // 3. 创建主面板
        CreateMainPanel();

        // 4. 创建Header
        CreateHeader();

        // 5. 创建TabBar
        CreateTabBar();

        // 6. 创建三个子面板
        CreatePlayerInfoPanel();
        CreateRelationshipPanel();
        CreateQuestPanel();

        // 默认隐藏面板
        panelRoot.SetActive(false);
    }

    // ========== Canvas 创建 ==========

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("InfoCanvas");
        canvasObj.transform.SetParent(transform, false);

        infoCanvas = canvasObj.AddComponent<Canvas>();
        infoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        infoCanvas.sortingOrder = 180;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // ========== 遮罩层 ==========

    private void CreateOverlay()
    {
        overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(infoCanvas.transform, false);

        RectTransform rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = overlayObj.AddComponent<Image>();
        img.color = OverlayColor;

        Button btn = overlayObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        // 点击事件由Manager绑定
    }

    // ========== 主面板 ==========

    private void CreateMainPanel()
    {
        panelRoot = new GameObject("MainPanel");
        panelRoot.transform.SetParent(infoCanvas.transform, false);

        RectTransform rt = panelRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1200, 800);
        rt.anchoredPosition = Vector2.zero;

        Image bg = panelRoot.AddComponent<Image>();
        bg.color = PanelBgColor;
    }

    // ========== Header（标题栏 + 关闭按钮）==========

    private void CreateHeader()
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(panelRoot.transform, false);

        RectTransform rt = header.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 60);
        rt.anchoredPosition = Vector2.zero;

        Image bg = header.AddComponent<Image>();
        bg.color = HeaderColor;

        // 标题文字
        txtTitle = CreateTMPText("Title", header.transform, "个人信息", 24f, TextGold,
            TextAlignmentOptions.Center, new Vector2(800, 60));
        RectTransform titleRT = txtTitle.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = Vector2.zero;

        // 关闭按钮
        btnClose = CreateButton("BtnClose", header.transform, "✕", ButtonNormal, new Vector2(50, 50));
        RectTransform closeRT = btnClose.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f);
        closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-5, 0);
    }

    // ========== TabBar（三个标签按钮）==========

    private void CreateTabBar()
    {
        GameObject tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(panelRoot.transform, false);

        RectTransform rt = tabBar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 50);
        rt.anchoredPosition = new Vector2(0, -60);

        HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        tabButtons = new Button[3];
        string[] tabNames = { "个人信息", "人际关系", "任务" };

        for (int i = 0; i < 3; i++)
        {
            tabButtons[i] = CreateButton($"Tab{i}", tabBar.transform, tabNames[i], TabInactiveColor, new Vector2(400, 50));
        }
    }

    // ========== 个人信息面板 ==========

    private void CreatePlayerInfoPanel()
    {
        playerInfoPanel = new GameObject("PlayerInfoPanel");
        playerInfoPanel.transform.SetParent(panelRoot.transform, false);

        RectTransform rt = playerInfoPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, -110); // 减去Header和TabBar高度

        // 创建ScrollView
        ScrollRect scrollRect = CreateScrollView("ScrollView", playerInfoPanel.transform, new Vector2(1200, 690));
        Transform content = scrollRect.content;

        // 基础信息区块
        CreatePlayerBasicInfoSection(content);

        // 核心属性区块
        CreatePlayerAttributesSection(content);

        // 状态值区块
        CreatePlayerStatusSection(content);

        // 隐性属性区块
        CreatePlayerHiddenAttrsSection(content);

        // 学业信息区块
        CreatePlayerAcademicSection(content);

        // 经济信息区块
        CreatePlayerEconomySection(content);

        // 社团信息区块
        CreatePlayerClubSection(content);
    }

    private void CreatePlayerBasicInfoSection(Transform parent)
    {
        GameObject section = CreateSection("BasicInfoSection", parent, "基础信息");

        txtPlayerName = CreateTMPText("PlayerName", section.transform, "姓名：XXX", 18f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(1150, 30));

        txtPlayerInfo = CreateTMPText("PlayerInfo", section.transform, "性别：男    专业：生物科学", 16f, TextGray,
            TextAlignmentOptions.Left, new Vector2(1150, 25));

        txtTimeInfo = CreateTMPText("TimeInfo", section.transform, "大一上 · 回合2 · 10月    年龄：18岁", 16f, TextGray,
            TextAlignmentOptions.Left, new Vector2(1150, 25));

        CreateSeparator(parent);
    }

    private void CreatePlayerAttributesSection(Transform parent)
    {
        GameObject section = CreateSection("AttributesSection", parent, "核心属性");

        attributeContainer = section.transform;

        // AttributeBar组件将由Manager动态创建

        CreateSeparator(parent);
    }

    private void CreatePlayerStatusSection(Transform parent)
    {
        GameObject section = CreateSection("StatusSection", parent, "状态值");

        // 压力条
        GameObject stressRow = new GameObject("StressRow");
        stressRow.transform.SetParent(section.transform, false);
        RectTransform stressRT = stressRow.AddComponent<RectTransform>();
        stressRT.sizeDelta = new Vector2(1150, 30);

        HorizontalLayoutGroup stressHLG = stressRow.AddComponent<HorizontalLayoutGroup>();
        stressHLG.spacing = 10;
        stressHLG.childAlignment = TextAnchor.MiddleLeft;
        stressHLG.childControlWidth = false;
        stressHLG.childControlHeight = false;

        CreateTMPText("StressLabel", stressRow.transform, "压力", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(80, 30));

        GameObject stressBarBg = CreatePanel("StressBarBg", stressRow.transform, new Color(0.2f, 0.2f, 0.2f));
        RectTransform stressBarBgRT = stressBarBg.GetComponent<RectTransform>();
        stressBarBgRT.sizeDelta = new Vector2(800, 20);

        GameObject stressBarFill = new GameObject("Fill");
        stressBarFill.transform.SetParent(stressBarBg.transform, false);
        RectTransform fillRT = stressBarFill.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.pivot = new Vector2(0, 0.5f);
        fillRT.sizeDelta = new Vector2(800, 20);
        imgStressBar = stressBarFill.AddComponent<Image>();
        imgStressBar.color = new Color(0.9f, 0.3f, 0.3f);

        txtStress = CreateTMPText("StressValue", stressRow.transform, "40%", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(100, 30));

        // 心情条
        GameObject moodRow = new GameObject("MoodRow");
        moodRow.transform.SetParent(section.transform, false);
        RectTransform moodRT = moodRow.AddComponent<RectTransform>();
        moodRT.sizeDelta = new Vector2(1150, 30);

        HorizontalLayoutGroup moodHLG = moodRow.AddComponent<HorizontalLayoutGroup>();
        moodHLG.spacing = 10;
        moodHLG.childAlignment = TextAnchor.MiddleLeft;
        moodHLG.childControlWidth = false;
        moodHLG.childControlHeight = false;

        CreateTMPText("MoodLabel", moodRow.transform, "心情", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(80, 30));

        GameObject moodBarBg = CreatePanel("MoodBarBg", moodRow.transform, new Color(0.2f, 0.2f, 0.2f));
        RectTransform moodBarBgRT = moodBarBg.GetComponent<RectTransform>();
        moodBarBgRT.sizeDelta = new Vector2(800, 20);

        GameObject moodBarFill = new GameObject("Fill");
        moodBarFill.transform.SetParent(moodBarBg.transform, false);
        RectTransform moodFillRT = moodBarFill.AddComponent<RectTransform>();
        moodFillRT.anchorMin = new Vector2(0, 0);
        moodFillRT.anchorMax = new Vector2(0, 1);
        moodFillRT.pivot = new Vector2(0, 0.5f);
        moodFillRT.sizeDelta = new Vector2(800, 20);
        imgMoodBar = moodBarFill.AddComponent<Image>();
        imgMoodBar.color = new Color(0.3f, 0.8f, 0.5f);

        txtMood = CreateTMPText("MoodValue", moodRow.transform, "70%", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(100, 30));

        CreateSeparator(parent);
    }

    private void CreatePlayerHiddenAttrsSection(Transform parent)
    {
        GameObject section = CreateSection("HiddenAttrsSection", parent, "隐性属性");

        txtHiddenAttrs = CreateTMPText("HiddenAttrs", section.transform, "黑暗值：5    负罪感：10    幸运：50", 16f, TextGray,
            TextAlignmentOptions.Left, new Vector2(1150, 50));

        CreateSeparator(parent);
    }

    private void CreatePlayerAcademicSection(Transform parent)
    {
        GameObject section = CreateSection("AcademicSection", parent, "学业信息");

        txtGPA = CreateTMPText("GPA", section.transform, "累计GPA：3.5    本学期GPA：3.8", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(1150, 25));

        txtCredits = CreateTMPText("Credits", section.transform, "已修学分：30 / 121", 16f, TextGray,
            TextAlignmentOptions.Left, new Vector2(1150, 25));

        CreateSeparator(parent);
    }

    private void CreatePlayerEconomySection(Transform parent)
    {
        GameObject section = CreateSection("EconomySection", parent, "经济信息");

        txtMoney = CreateTMPText("Money", section.transform, "当前金钱：¥8000", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(1150, 25));

        txtDebt = CreateTMPText("Debt", section.transform, "债务等级：正常", 16f, TextGray,
            TextAlignmentOptions.Left, new Vector2(1150, 25));

        CreateSeparator(parent);
    }

    private void CreatePlayerClubSection(Transform parent)
    {
        GameObject section = CreateSection("ClubSection", parent, "社团信息");

        txtClubs = CreateTMPText("Clubs", section.transform, "已加入：跑协（干事）、学生会（部长）", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(1150, 25));

        txtParty = CreateTMPText("Party", section.transform, "入党进度：发展对象 (3/5)", 16f, TextGray,
            TextAlignmentOptions.Left, new Vector2(1150, 25));
    }

    // ========== 人际关系面板 ==========

    private void CreateRelationshipPanel()
    {
        relationshipPanel = new GameObject("RelationshipPanel");
        relationshipPanel.transform.SetParent(panelRoot.transform, false);

        RectTransform rt = relationshipPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, -110);

        // 左右分栏
        GameObject leftPanel = CreatePanel("LeftPanel", relationshipPanel.transform, CardBgColor);
        RectTransform leftRT = leftPanel.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0, 1);
        leftRT.pivot = new Vector2(0, 0.5f);
        leftRT.sizeDelta = new Vector2(300, 0);
        leftRT.anchoredPosition = new Vector2(0, 0);

        GameObject rightPanel = CreatePanel("RightPanel", relationshipPanel.transform, SectionBgColor);
        RectTransform rightRT = rightPanel.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0, 0);
        rightRT.anchorMax = new Vector2(1, 1);
        rightRT.pivot = new Vector2(0, 0.5f);
        rightRT.offsetMin = new Vector2(310, 0);
        rightRT.offsetMax = new Vector2(0, 0);

        // 左侧NPC列表
        ScrollRect leftScroll = CreateScrollView("NPCListScroll", leftPanel.transform, new Vector2(300, 690));
        npcListContent = leftScroll.content;

        // 右侧详情区
        ScrollRect rightScroll = CreateScrollView("NPCDetailScroll", rightPanel.transform, new Vector2(880, 690));
        npcDetailContainer = rightScroll.content;

        CreateNPCDetailUI(npcDetailContainer);

        relationshipPanel.SetActive(false);
    }

    private void CreateNPCDetailUI(Transform parent)
    {
        // NPC名称
        txtNPCName = CreateTMPText("NPCName", parent, "林知秋", 22f, TextGold,
            TextAlignmentOptions.Center, new Vector2(850, 35));

        // 好感度条
        GameObject affinityRow = new GameObject("AffinityRow");
        affinityRow.transform.SetParent(parent, false);
        RectTransform affinityRT = affinityRow.AddComponent<RectTransform>();
        affinityRT.sizeDelta = new Vector2(850, 30);

        HorizontalLayoutGroup affinityHLG = affinityRow.AddComponent<HorizontalLayoutGroup>();
        affinityHLG.spacing = 10;
        affinityHLG.childAlignment = TextAnchor.MiddleCenter;
        affinityHLG.childControlWidth = false;
        affinityHLG.childControlHeight = false;

        CreateTMPText("AffinityLabel", affinityRow.transform, "好感度", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(80, 30));

        GameObject affinityBarBg = CreatePanel("AffinityBarBg", affinityRow.transform, new Color(0.2f, 0.2f, 0.2f));
        RectTransform affinityBarBgRT = affinityBarBg.GetComponent<RectTransform>();
        affinityBarBgRT.sizeDelta = new Vector2(600, 20);

        GameObject affinityBarFill = new GameObject("Fill");
        affinityBarFill.transform.SetParent(affinityBarBg.transform, false);
        RectTransform fillRT = affinityBarFill.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.pivot = new Vector2(0, 0.5f);
        fillRT.sizeDelta = new Vector2(600, 20);
        imgAffinityBar = affinityBarFill.AddComponent<Image>();
        imgAffinityBar.color = new Color(1.0f, 0.6f, 0.8f);

        txtNPCAffinity = CreateTMPText("AffinityValue", affinityRow.transform, "75 / 100", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(100, 30));

        // 关系等级和恋爱状态
        txtAffinityLevel = CreateTMPText("AffinityLevel", parent, "关系等级：亲密朋友", 16f, TextWhite,
            TextAlignmentOptions.Left, new Vector2(850, 25));

        txtRomanceState = CreateTMPText("RomanceState", parent, "恋爱状态：暗恋中", 16f, new Color(1.0f, 0.7f, 0.8f),
            TextAlignmentOptions.Left, new Vector2(850, 25));

        CreateSeparator(parent);

        // 性格偏好区块
        GameObject prefSection = CreateSection("PreferencesSection", parent, "性格偏好");
        txtPreferences = CreateTMPText("Preferences", prefSection.transform, "喜欢：深入交谈、一起学习\n不喜欢：参加派对", 16f, TextGray,
            TextAlignmentOptions.Left, new Vector2(820, 50));

        CreateSeparator(parent);

        // 互动记录区块
        GameObject recordSection = CreateSection("InteractionRecordSection", parent, "互动记录");
        interactionRecordContainer = recordSection.transform;

        CreateSeparator(parent);

        // 底部按钮
        GameObject buttonRow = new GameObject("ButtonRow");
        buttonRow.transform.SetParent(parent, false);
        RectTransform btnRowRT = buttonRow.AddComponent<RectTransform>();
        btnRowRT.sizeDelta = new Vector2(850, 50);

        HorizontalLayoutGroup btnHLG = buttonRow.AddComponent<HorizontalLayoutGroup>();
        btnHLG.spacing = 20;
        btnHLG.childAlignment = TextAnchor.MiddleCenter;
        btnHLG.childControlWidth = false;
        btnHLG.childControlHeight = false;

        btnSocialInteract = CreateButton("BtnSocialInteract", buttonRow.transform, "社交互动", ButtonNormal, new Vector2(200, 45));
    }

    // ========== 任务面板 ==========

    private void CreateQuestPanel()
    {
        questPanel = new GameObject("QuestPanel");
        questPanel.transform.SetParent(panelRoot.transform, false);

        RectTransform rt = questPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, -110);

        ScrollRect scrollRect = CreateScrollView("QuestScrollView", questPanel.transform, new Vector2(1200, 690));
        questListContent = scrollRect.content;

        // 占位文本
        CreateTMPText("PlaceholderText", questListContent, "任务系统开发中...", 18f, TextGray,
            TextAlignmentOptions.Center, new Vector2(1150, 50));

        questPanel.SetActive(false);
    }

    // ========== 工具方法 ==========

    private GameObject CreatePanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();
        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;
        return panel;
    }

    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string text,
        float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, Color normalColor, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = new Color(
            Mathf.Min(normalColor.r + 0.10f, 1f),
            Mathf.Min(normalColor.g + 0.10f, 1f),
            Mathf.Min(normalColor.b + 0.10f, 1f),
            normalColor.a
        );
        cb.pressedColor = new Color(
            Mathf.Max(normalColor.r - 0.05f, 0f),
            Mathf.Max(normalColor.g - 0.05f, 0f),
            Mathf.Max(normalColor.b - 0.05f, 0f),
            normalColor.a
        );
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        TextMeshProUGUI btnText = CreateTMPText(name + "Label", btnObj.transform, label,
            18f, TextWhite, TextAlignmentOptions.Center, size);
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btn;
    }

    private ScrollRect CreateScrollView(string name, Transform parent, Vector2 size)
    {
        GameObject scrollObj = new GameObject(name);
        scrollObj.transform.SetParent(parent, false);

        RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        Image vpMask = viewport.AddComponent<Image>();
        vpMask.color = Color.white;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect.viewport = vpRT;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        return scrollRect;
    }

    private GameObject CreateSection(string name, Transform parent, string title)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);

        RectTransform sectionRT = section.AddComponent<RectTransform>();
        sectionRT.sizeDelta = new Vector2(1150, 0);

        VerticalLayoutGroup vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.padding = new RectOffset(0, 0, 5, 5);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = section.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 区块标题
        CreateTMPText("SectionTitle", section.transform, $"【{title}】", 18f, TextGold,
            TextAlignmentOptions.Left, new Vector2(1150, 30));

        return section;
    }

    private void CreateSeparator(Transform parent)
    {
        GameObject separator = new GameObject("Separator");
        separator.transform.SetParent(parent, false);

        RectTransform rt = separator.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1150, 1);

        Image img = separator.AddComponent<Image>();
        img.color = SeparatorColor;
    }
}

