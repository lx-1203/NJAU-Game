using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// HUD 构建器 —— 参照参考界面复刻的游戏主界面
/// 布局参考（2560x1600 → 1920x1200 设计分辨率）：
/// ┌───────────────────────────────────────────────────────────────┐
/// │ [左上] 季节·年份·年龄 + 图标   [中央] AP黄色进度条   [右上] 属性图标行  │
/// │                                                               │
/// │ [左侧] 任务/知识面板         [中央] 游戏场景         [右侧] 地图按钮  │
/// │ [左中] 地点通知                                                │
/// │                                                               │
/// │ [左下] 角色卡片(头像+属性条)  [底部中央] 行动图标按钮  [右下] Tab快捷键  │
/// └───────────────────────────────────────────────────────────────┘
/// </summary>
public class HUDBuilder : MonoBehaviour
{
    // ========== 构建产物引用（供 HUDManager 使用） ==========

    [HideInInspector] public Canvas hudCanvas;

    // --- 左上角：时间信息 ---
    [HideInInspector] public TextMeshProUGUI seasonText;       // 季节文字（春/夏/秋/冬）
    [HideInInspector] public TextMeshProUGUI yearAgeText;      // 年份+年龄 "2003\n18岁"
    [HideInInspector] public Image seasonIcon;                 // 季节图标背景
    [HideInInspector] public GameObject topLeftPanel;           // 左上角容器

    // --- 顶部中央：AP 进度条 ---
    [HideInInspector] public Image apBarFill;                  // AP 黄色填充条
    [HideInInspector] public TextMeshProUGUI apText;           // AP 数值文本
    [HideInInspector] public Image apIcon;                     // 闪电图标

    // --- 右上角：属性快捷显示 ---
    [HideInInspector] public TextMeshProUGUI moneyStatText;    // 金钱数值
    [HideInInspector] public TextMeshProUGUI popularityStatText; // 人气/魅力数值
    [HideInInspector] public TextMeshProUGUI moodStatText;     // 心情数值
    [HideInInspector] public TextMeshProUGUI energyStatText;   // 体力/压力数值

    // --- 左侧：任务/知识追踪面板 ---
    [HideInInspector] public GameObject questPanel;
    [HideInInspector] public TextMeshProUGUI questTitleText;
    [HideInInspector] public TextMeshProUGUI questProgressText;

    // --- 左中：地点通知 ---
    [HideInInspector] public GameObject locationNotice;
    [HideInInspector] public TextMeshProUGUI locationNoticeText;

    // --- 右侧：地图按钮 ---
    [HideInInspector] public Button btnMapIcon;

    // --- 左下角：角色卡片 ---
    [HideInInspector] public GameObject characterCard;
    [HideInInspector] public Image characterAvatar;            // 小头像
    [HideInInspector] public TextMeshProUGUI playerNameText;   // 姓名
    [HideInInspector] public TextMeshProUGUI playerGradeText;  // 年级
    [HideInInspector] public TextMeshProUGUI cardHintText;     // "点击查看更多"
    [HideInInspector] public List<AttributeBar> attributeBars = new List<AttributeBar>();

    // --- 底部中央：行动按钮 ---
    [HideInInspector] public GameObject actionButtonRow;

    // --- 底部右下：快捷键提示 + 功能按钮 ---
    [HideInInspector] public GameObject hotkeyPanel;
    [HideInInspector] public Button btnFeature;                // 右下角功能按钮（关注新同学→社交）

    // --- 旧接口兼容（HUDManager 引用） ---
    [HideInInspector] public TextMeshProUGUI timeText;
    [HideInInspector] public TextMeshProUGUI moneyText;
    [HideInInspector] public TextMeshProUGUI moneyWarningText;
    [HideInInspector] public TextMeshProUGUI actionPointsText;
    [HideInInspector] public TextMeshProUGUI gpaText;
    [HideInInspector] public Image portraitPlaceholder;
    [HideInInspector] public GameObject centerPanel;

    // 底栏按钮（保留引用，部分隐藏）
    [HideInInspector] public Button btnStudy;
    [HideInInspector] public Button btnSocial;
    [HideInInspector] public Button btnGoOut;
    [HideInInspector] public Button btnSleep;
    [HideInInspector] public Button btnShop;
    [HideInInspector] public Button btnClub;
    [HideInInspector] public Button btnSave;
    [HideInInspector] public Button btnTalent;
    [HideInInspector] public Button btnMap;

    // 动画器
    [HideInInspector] public UIAnimator hudAnimator;

    // 子系统 UI
    [HideInInspector] public ShopUIBuilder shopUIBuilder;
    [HideInInspector] public NPCInteractionMenu npcInteractionMenu;
    [HideInInspector] public ClubPanelBuilder clubPanelBuilder;
    [HideInInspector] public ClubPanelManager clubPanelManager;

    // ========== 颜色方案（参考截图暖色调） ==========
    // 顶部栏：木纹/棕色半透明
    private static readonly Color TopFrameColor = new Color(0.55f, 0.35f, 0.18f, 0.92f);
    // 面板底色：米黄半透明
    private static readonly Color PanelBgCream = new Color(0.98f, 0.95f, 0.88f, 0.93f);
    // 角色卡片底色：浅蓝
    private static readonly Color CardBgLight = new Color(0.85f, 0.93f, 0.98f, 0.95f);
    // AP 进度条黄色
    private static readonly Color APBarYellow = new Color(1.0f, 0.82f, 0.0f, 1.0f);
    // AP 进度条背景
    private static readonly Color APBarBg = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    // 按钮底色
    private static readonly Color BtnNormal = new Color(0.95f, 0.90f, 0.80f, 0.95f);
    private static readonly Color BtnHover = new Color(1.0f, 0.95f, 0.85f, 1.0f);
    private static readonly Color BtnPressed = new Color(0.85f, 0.80f, 0.70f, 1.0f);
    // 文字颜色
    private static readonly Color TextDark = new Color(0.20f, 0.15f, 0.10f);
    private static readonly Color TextBrown = new Color(0.45f, 0.30f, 0.15f);
    private static readonly Color TextWhite = new Color(0.98f, 0.98f, 0.96f);
    private static readonly Color TextGold = new Color(0.85f, 0.65f, 0.10f);
    // 季节颜色
    private static readonly Color SeasonSpring = new Color(0.45f, 0.78f, 0.45f);
    private static readonly Color SeasonSummer = new Color(0.95f, 0.55f, 0.20f);
    private static readonly Color SeasonAutumn = new Color(0.85f, 0.60f, 0.25f);
    private static readonly Color SeasonWinter = new Color(0.50f, 0.70f, 0.90f);
    // 通知颜色
    private static readonly Color NoticeBg = new Color(0.90f, 0.20f, 0.20f, 0.90f);
    // 属性图标圈颜色
    private static readonly Color IconCircleGold = new Color(0.95f, 0.80f, 0.25f, 1.0f);
    private static readonly Color IconCirclePink = new Color(0.95f, 0.50f, 0.55f, 1.0f);
    private static readonly Color IconCircleBlue = new Color(0.50f, 0.75f, 0.95f, 1.0f);
    private static readonly Color IconCircleRed = new Color(0.95f, 0.40f, 0.30f, 1.0f);
    // 快捷键栏底色
    private static readonly Color HotkeyBg = new Color(0.15f, 0.15f, 0.20f, 0.75f);
    // 功能按钮天蓝
    private static readonly Color FeatureBtnBg = new Color(0.70f, 0.90f, 0.95f, 0.95f);

    // ========== 对外接口 ==========

    public void BuildHUD()
    {
        CreateCanvas();
        CreateTopLeft();
        CreateTopCenterAPBar();
        CreateTopRightStats();
        CreateQuestPanel();
        CreateLocationNotice();
        CreateMapButton();
        CreateCharacterCard();
        CreateActionButtonRow();
        CreateHotkeyPanel();
        CreateCenterPanel();
        CreateAnimator();
        CreateNPCInteractionMenu();
        CreateShopUI();
        CreateClubPanel();

        // 创建隐藏的兼容按钮引用
        CreateHiddenCompatButtons();
    }

    // ====================================================================
    //  Canvas
    // ====================================================================

    private void CreateCanvas()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("HUDCanvas");
        canvasObj.transform.SetParent(transform, false);

        hudCanvas = canvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // ====================================================================
    //  左上角：季节 + 年份/年龄 + 图标
    // ====================================================================

    private void CreateTopLeft()
    {
        topLeftPanel = new GameObject("TopLeftPanel");
        topLeftPanel.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = topLeftPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(15, -10);
        rt.sizeDelta = new Vector2(380, 80);

        HorizontalLayoutGroup hlg = topLeftPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(5, 5, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 季节图标（圆角矩形）
        GameObject seasonObj = new GameObject("SeasonIcon");
        seasonObj.transform.SetParent(topLeftPanel.transform, false);
        RectTransform srt = seasonObj.AddComponent<RectTransform>();
        srt.sizeDelta = new Vector2(60, 65);
        seasonIcon = seasonObj.AddComponent<Image>();
        seasonIcon.color = SeasonSummer;
        // 季节文字叠加
        seasonText = CreateTMPText("SeasonText", seasonObj.transform, "夏",
            28f, TextWhite, TextAlignmentOptions.Center, new Vector2(60, 65));
        StretchToParent(seasonText.rectTransform);

        // 年份 + 年龄
        GameObject yearAgeObj = new GameObject("YearAge");
        yearAgeObj.transform.SetParent(topLeftPanel.transform, false);
        RectTransform yaRT = yearAgeObj.AddComponent<RectTransform>();
        yaRT.sizeDelta = new Vector2(70, 65);
        yearAgeText = CreateTMPText("YearAgeLabel", yearAgeObj.transform, "2024\n<size=18>大一</size>",
            22f, TextDark, TextAlignmentOptions.Left, new Vector2(70, 65));
        StretchToParent(yearAgeText.rectTransform);
        yearAgeText.lineSpacing = -10;

        // 图标组：奖杯 + 叶子（用圆形图标模拟）
        CreateTopLeftIcon(topLeftPanel.transform, "TrophyIcon", "\u2605", IconCircleGold, 50);
        CreateTopLeftIcon(topLeftPanel.transform, "LeafIcon", "\u2663", SeasonSpring, 50);

        // 兼容旧 timeText（指向 yearAgeText）
        timeText = yearAgeText;
    }

    private void CreateTopLeftIcon(Transform parent, string name, string symbol, Color bgColor, float size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;

        CreateTMPText(name + "Symbol", obj.transform, symbol,
            22f, TextWhite, TextAlignmentOptions.Center, new Vector2(size, size));
    }

    // ====================================================================
    //  顶部中央：AP 黄色进度条
    // ====================================================================

    private void CreateTopCenterAPBar()
    {
        // 外框容器
        GameObject apContainer = new GameObject("APBarContainer");
        apContainer.transform.SetParent(hudCanvas.transform, false);
        RectTransform crt = apContainer.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 1);
        crt.anchorMax = new Vector2(0.5f, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = new Vector2(0, -15);
        crt.sizeDelta = new Vector2(300, 40);

        // 背景
        Image apBg = apContainer.AddComponent<Image>();
        apBg.color = APBarBg;

        // 填充条
        GameObject fillObj = new GameObject("APFill");
        fillObj.transform.SetParent(apContainer.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = new Vector2(3, 3);
        fillRT.offsetMax = new Vector2(-3, -3);

        apBarFill = fillObj.AddComponent<Image>();
        apBarFill.color = APBarYellow;
        apBarFill.type = Image.Type.Filled;
        apBarFill.fillMethod = Image.FillMethod.Horizontal;
        apBarFill.fillAmount = 1.0f;

        // 闪电图标（左侧）
        GameObject iconObj = new GameObject("APIcon");
        iconObj.transform.SetParent(apContainer.transform, false);
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(1, 0.5f);
        iconRT.anchoredPosition = new Vector2(-8, 0);
        iconRT.sizeDelta = new Vector2(30, 30);
        apIcon = iconObj.AddComponent<Image>();
        apIcon.color = APBarYellow;
        // 闪电符号
        CreateTMPText("LightningSymbol", iconObj.transform, "AP",
            20f, TextDark, TextAlignmentOptions.Center, new Vector2(30, 30));

        // AP 数值文本（中央叠加）
        apText = CreateTMPText("APText", apContainer.transform, "20",
            22f, TextDark, TextAlignmentOptions.Center, new Vector2(300, 40));
        StretchToParent(apText.rectTransform);
        apText.fontStyle = FontStyles.Bold;

        // 兼容旧字段
        actionPointsText = apText;
    }

    // ====================================================================
    //  右上角：属性图标快捷显示
    // ====================================================================

    private void CreateTopRightStats()
    {
        GameObject statsPanel = new GameObject("TopRightStats");
        statsPanel.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = statsPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-15, -10);
        rt.sizeDelta = new Vector2(520, 60);

        HorizontalLayoutGroup hlg = statsPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(5, 5, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 金钱: ¥ 图标 + 数值 + 变化量
        moneyStatText = CreateStatIcon(statsPanel.transform, "MoneyStat", "\uFFE5", IconCircleGold, "8000");
        // 魅力/人气
        popularityStatText = CreateStatIcon(statsPanel.transform, "PopStat", "\u2605", IconCirclePink, "5");
        // 心情
        moodStatText = CreateStatIcon(statsPanel.transform, "MoodStat", "\u263A", IconCircleBlue, "70");
        // 体力/压力
        energyStatText = CreateStatIcon(statsPanel.transform, "EnergyStat", "HP", IconCircleRed, "20");

        // 兼容旧引用
        moneyText = moneyStatText;
        // 创建隐藏的警告文本
        GameObject warnObj = new GameObject("MoneyWarning");
        warnObj.transform.SetParent(statsPanel.transform, false);
        warnObj.AddComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
        moneyWarningText = warnObj.AddComponent<TextMeshProUGUI>();
        moneyWarningText.text = "";
        warnObj.SetActive(false);

        // GPA 文本（隐藏在 stats 面板右侧）
        gpaText = CreateTMPText("GPAText", statsPanel.transform, "",
            16f, TextDark, TextAlignmentOptions.Right, new Vector2(90, 50));
        gpaText.gameObject.SetActive(false); // 初始隐藏，有GPA时再显示
    }

    private TextMeshProUGUI CreateStatIcon(Transform parent, string name, string symbol, Color circleColor, string defaultValue)
    {
        // 每个 stat = 圆形图标 + 数值文本
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        RectTransform grt = group.AddComponent<RectTransform>();
        grt.sizeDelta = new Vector2(115, 50);

        HorizontalLayoutGroup hlg = group.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 圆形图标
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(group.transform, false);
        RectTransform irt = iconObj.AddComponent<RectTransform>();
        irt.sizeDelta = new Vector2(40, 40);
        Image iconBg = iconObj.AddComponent<Image>();
        iconBg.color = circleColor;

        CreateTMPText("Sym", iconObj.transform, symbol,
            18f, TextWhite, TextAlignmentOptions.Center, new Vector2(40, 40));

        // 数值文本
        TextMeshProUGUI valueText = CreateTMPText("Value", group.transform, defaultValue,
            20f, TextDark, TextAlignmentOptions.Left, new Vector2(65, 50));
        valueText.fontStyle = FontStyles.Bold;

        return valueText;
    }

    // ====================================================================
    //  左侧：任务/知识追踪面板
    // ====================================================================

    private void CreateQuestPanel()
    {
        questPanel = new GameObject("QuestPanel");
        questPanel.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = questPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(15, -100);
        rt.sizeDelta = new Vector2(280, 80);

        Image bg = questPanel.AddComponent<Image>();
        bg.color = PanelBgCream;

        VerticalLayoutGroup vlg = questPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 小头像 + 标题行
        GameObject titleRow = new GameObject("TitleRow");
        titleRow.transform.SetParent(questPanel.transform, false);
        titleRow.AddComponent<RectTransform>().sizeDelta = new Vector2(260, 28);
        HorizontalLayoutGroup thlg = titleRow.AddComponent<HorizontalLayoutGroup>();
        thlg.spacing = 8f;
        thlg.childAlignment = TextAnchor.MiddleLeft;
        thlg.childControlWidth = false;
        thlg.childControlHeight = false;

        // 小头像
        GameObject avatarObj = new GameObject("QuestAvatar");
        avatarObj.transform.SetParent(titleRow.transform, false);
        avatarObj.AddComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
        Image avatarImg = avatarObj.AddComponent<Image>();
        avatarImg.color = new Color(0.7f, 0.7f, 0.7f);

        questTitleText = CreateTMPText("QuestTitle", titleRow.transform, "学习知识点",
            16f, TextBrown, TextAlignmentOptions.Left, new Vector2(200, 28));
        questTitleText.fontStyle = FontStyles.Bold;

        // 进度文本
        questProgressText = CreateTMPText("QuestProgress", questPanel.transform,
            "  \u25CF (0/1) 学习一次知识点",
            14f, TextDark, TextAlignmentOptions.Left, new Vector2(260, 24));
    }

    // ====================================================================
    //  左中：地点通知
    // ====================================================================

    private void CreateLocationNotice()
    {
        locationNotice = new GameObject("LocationNotice");
        locationNotice.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = locationNotice.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(15, 40);
        rt.sizeDelta = new Vector2(180, 40);

        Image bg = locationNotice.AddComponent<Image>();
        bg.color = NoticeBg;

        HorizontalLayoutGroup hlg = locationNotice.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 警告图标
        CreateTMPText("NoticeIcon", locationNotice.transform, "\u2757",
            18f, TextWhite, TextAlignmentOptions.Center, new Vector2(24, 30));

        locationNoticeText = CreateTMPText("NoticeText", locationNotice.transform, "操场已开放",
            15f, TextWhite, TextAlignmentOptions.Left, new Vector2(130, 30));
        locationNoticeText.fontStyle = FontStyles.Bold;

        // 默认隐藏
        locationNotice.SetActive(false);
    }

    // ====================================================================
    //  右侧：地图按钮
    // ====================================================================

    private void CreateMapButton()
    {
        GameObject mapObj = new GameObject("MapButton");
        mapObj.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = mapObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-20, 50);
        rt.sizeDelta = new Vector2(65, 65);

        Image bg = mapObj.AddComponent<Image>();
        bg.color = PanelBgCream;

        btnMapIcon = mapObj.AddComponent<Button>();
        ColorBlock cb = btnMapIcon.colors;
        cb.normalColor = PanelBgCream;
        cb.highlightedColor = BtnHover;
        cb.pressedColor = BtnPressed;
        cb.fadeDuration = 0.1f;
        btnMapIcon.colors = cb;

        // 地图图标符号
        CreateTMPText("MapSymbol", mapObj.transform, "MAP",
            28f, TextBrown, TextAlignmentOptions.Center, new Vector2(65, 65));

        // 兼容旧 btnMap
        btnMap = btnMapIcon;
    }

    // ====================================================================
    //  左下角：角色卡片
    // ====================================================================

    private void CreateCharacterCard()
    {
        characterCard = new GameObject("CharacterCard");
        characterCard.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = characterCard.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(15, 55);
        rt.sizeDelta = new Vector2(280, 190);

        Image bg = characterCard.AddComponent<Image>();
        bg.color = CardBgLight;

        // 卡片内布局
        HorizontalLayoutGroup hlg = characterCard.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 左侧：小头像
        GameObject avatarPanel = new GameObject("AvatarPanel");
        avatarPanel.transform.SetParent(characterCard.transform, false);
        RectTransform avRT = avatarPanel.AddComponent<RectTransform>();
        avRT.sizeDelta = new Vector2(60, 170);

        VerticalLayoutGroup avlg = avatarPanel.AddComponent<VerticalLayoutGroup>();
        avlg.spacing = 4f;
        avlg.childAlignment = TextAnchor.UpperCenter;
        avlg.childControlWidth = false;
        avlg.childControlHeight = false;

        GameObject avatarObj = new GameObject("Avatar");
        avatarObj.transform.SetParent(avatarPanel.transform, false);
        avatarObj.AddComponent<RectTransform>().sizeDelta = new Vector2(55, 55);
        characterAvatar = avatarObj.AddComponent<Image>();
        characterAvatar.color = new Color(0.75f, 0.75f, 0.80f);

        // 姓名 + 年级
        playerNameText = CreateTMPText("PlayerName", avatarPanel.transform, "姓名",
            12f, TextDark, TextAlignmentOptions.Center, new Vector2(60, 18));
        playerGradeText = CreateTMPText("PlayerGrade", avatarPanel.transform, "年级",
            11f, TextBrown, TextAlignmentOptions.Center, new Vector2(60, 16));

        // 右侧：属性条区域
        GameObject attrPanel = new GameObject("AttrPanel");
        attrPanel.transform.SetParent(characterCard.transform, false);
        RectTransform apRT = attrPanel.AddComponent<RectTransform>();
        apRT.sizeDelta = new Vector2(190, 170);

        VerticalLayoutGroup avlg2 = attrPanel.AddComponent<VerticalLayoutGroup>();
        avlg2.spacing = 6f;
        avlg2.padding = new RectOffset(0, 0, 5, 5);
        avlg2.childAlignment = TextAnchor.UpperLeft;
        avlg2.childControlWidth = true;
        avlg2.childControlHeight = false;
        avlg2.childForceExpandWidth = true;
        avlg2.childForceExpandHeight = false;

        // "点击查看更多" 提示
        cardHintText = CreateTMPText("CardHint", attrPanel.transform, "点击查看更多",
            11f, new Color(0.5f, 0.5f, 0.6f), TextAlignmentOptions.Right, new Vector2(190, 16));

        // 兼容旧 portraitPlaceholder
        portraitPlaceholder = characterAvatar;
    }

    /// <summary>
    /// 在角色卡片的属性区域追加一个属性条
    /// </summary>
    public AttributeBar AddAttributeBar()
    {
        Transform attrPanel = hudCanvas.transform.Find("CharacterCard/AttrPanel");
        if (attrPanel == null)
        {
            Debug.LogError("[HUDBuilder] 找不到 AttrPanel！");
            return null;
        }

        // 在 "点击查看更多" 之前插入属性条
        AttributeBar bar = AttributeBar.Create(attrPanel);
        if (bar != null && cardHintText != null)
        {
            // 确保提示文本在最后
            cardHintText.transform.SetAsLastSibling();
        }
        attributeBars.Add(bar);
        return bar;
    }

    // ====================================================================
    //  底部中央：行动图标按钮行
    // ====================================================================

    private void CreateActionButtonRow()
    {
        actionButtonRow = new GameObject("ActionButtonRow");
        actionButtonRow.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = actionButtonRow.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 15);
        rt.sizeDelta = new Vector2(600, 80);

        HorizontalLayoutGroup hlg = actionButtonRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
    }

    // ====================================================================
    //  底部右下：快捷键提示栏 + 功能按钮
    // ====================================================================

    private void CreateHotkeyPanel()
    {
        // 快捷键提示栏
        hotkeyPanel = new GameObject("HotkeyPanel");
        hotkeyPanel.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = hotkeyPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-15, 5);
        rt.sizeDelta = new Vector2(360, 32);

        Image bg = hotkeyPanel.AddComponent<Image>();
        bg.color = HotkeyBg;

        HorizontalLayoutGroup hlg = hotkeyPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15f;
        hlg.padding = new RectOffset(12, 12, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateHotkeyLabel(hotkeyPanel.transform, "Tab", "信息");
        CreateHotkeyLabel(hotkeyPanel.transform, "1", "社交");
        CreateHotkeyLabel(hotkeyPanel.transform, "2", "成长");
        CreateHotkeyLabel(hotkeyPanel.transform, "Esc", "菜单");

        // 右下功能按钮
        GameObject featureObj = new GameObject("FeatureButton");
        featureObj.transform.SetParent(hudCanvas.transform, false);
        RectTransform frt = featureObj.AddComponent<RectTransform>();
        frt.anchorMin = new Vector2(1, 0);
        frt.anchorMax = new Vector2(1, 0);
        frt.pivot = new Vector2(1, 0);
        frt.anchoredPosition = new Vector2(-15, 42);
        frt.sizeDelta = new Vector2(200, 45);

        Image fBg = featureObj.AddComponent<Image>();
        fBg.color = FeatureBtnBg;

        btnFeature = featureObj.AddComponent<Button>();
        ColorBlock fcb = btnFeature.colors;
        fcb.normalColor = FeatureBtnBg;
        fcb.highlightedColor = new Color(0.80f, 0.95f, 1.0f, 1.0f);
        fcb.pressedColor = new Color(0.60f, 0.85f, 0.90f, 1.0f);
        fcb.fadeDuration = 0.1f;
        btnFeature.colors = fcb;

        // 信封图标 + 文字
        HorizontalLayoutGroup fhlg = featureObj.AddComponent<HorizontalLayoutGroup>();
        fhlg.spacing = 8f;
        fhlg.padding = new RectOffset(12, 12, 5, 5);
        fhlg.childAlignment = TextAnchor.MiddleCenter;
        fhlg.childControlWidth = false;
        fhlg.childControlHeight = true;

        CreateTMPText("FeatureIcon", featureObj.transform, "MSG",
            20f, TextDark, TextAlignmentOptions.Center, new Vector2(28, 35));
        CreateTMPText("FeatureLabel", featureObj.transform, "社交互动",
            16f, TextDark, TextAlignmentOptions.Left, new Vector2(120, 35));

        // 兼容旧 btnSocial
        btnSocial = btnFeature;
    }

    private void CreateHotkeyLabel(Transform parent, string key, string label)
    {
        GameObject obj = new GameObject("Hotkey_" + key);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>().sizeDelta = new Vector2(70, 24);

        HorizontalLayoutGroup hlg = obj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        // Key 背景
        GameObject keyObj = new GameObject("Key");
        keyObj.transform.SetParent(obj.transform, false);
        keyObj.AddComponent<RectTransform>().sizeDelta = new Vector2(30, 20);
        Image keyBg = keyObj.AddComponent<Image>();
        keyBg.color = new Color(0.35f, 0.35f, 0.40f, 0.9f);
        CreateTMPText("KeyText", keyObj.transform, key,
            11f, TextWhite, TextAlignmentOptions.Center, new Vector2(30, 20));

        // Label
        CreateTMPText("LabelText", obj.transform, label,
            13f, new Color(0.75f, 0.75f, 0.80f), TextAlignmentOptions.Left, new Vector2(36, 24));
    }

    // ====================================================================
    //  中央区域（透明，点击穿透）
    // ====================================================================

    private void CreateCenterPanel()
    {
        centerPanel = new GameObject("CenterPanel");
        centerPanel.transform.SetParent(hudCanvas.transform, false);
        RectTransform rt = centerPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = centerPanel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;

        // 移到所有UI之后（最底层）
        centerPanel.transform.SetAsFirstSibling();
    }

    // ====================================================================
    //  隐藏的兼容按钮（保持旧代码引用不报错）
    // ====================================================================

    private void CreateHiddenCompatButtons()
    {
        GameObject hidden = new GameObject("HiddenButtons");
        hidden.transform.SetParent(hudCanvas.transform, false);
        hidden.SetActive(false);

        btnStudy = CreateHiddenButton("BtnStudy", hidden.transform);
        btnGoOut = CreateHiddenButton("BtnGoOut", hidden.transform);
        btnSleep = CreateHiddenButton("BtnSleep", hidden.transform);
        btnShop = CreateHiddenButton("BtnShop", hidden.transform);
        btnClub = CreateHiddenButton("BtnClub", hidden.transform);
        btnSave = CreateHiddenButton("BtnSave", hidden.transform);
        btnTalent = CreateHiddenButton("BtnTalent", hidden.transform);
    }

    private Button CreateHiddenButton(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        obj.AddComponent<Image>().color = Color.clear;
        return obj.AddComponent<Button>();
    }

    // ====================================================================
    //  UIAnimator
    // ====================================================================

    private void CreateAnimator()
    {
        GameObject animObj = new GameObject("HUDAnimator");
        animObj.transform.SetParent(transform, false);
        hudAnimator = animObj.AddComponent<UIAnimator>();
    }

    // ====================================================================
    //  NPC 社交互动菜单
    // ====================================================================

    private void CreateNPCInteractionMenu()
    {
        GameObject menuObj = new GameObject("NPCInteractionMenu");
        menuObj.transform.SetParent(transform, false);
        npcInteractionMenu = menuObj.AddComponent<NPCInteractionMenu>();
        npcInteractionMenu.Initialize(hudCanvas);
    }

    // ====================================================================
    //  商店 UI
    // ====================================================================

    private void CreateShopUI()
    {
        GameObject shopObj = new GameObject("ShopUIBuilder");
        shopObj.transform.SetParent(transform, false);
        shopUIBuilder = shopObj.AddComponent<ShopUIBuilder>();
        shopUIBuilder.BuildShopUI();
    }

    // ====================================================================
    //  社团面板
    // ====================================================================

    private void CreateClubPanel()
    {
        GameObject clubObj = new GameObject("ClubPanelBuilder");
        clubObj.transform.SetParent(transform, false);
        clubPanelBuilder = clubObj.AddComponent<ClubPanelBuilder>();
        clubPanelBuilder.BuildPanel();

        clubPanelManager = clubObj.AddComponent<ClubPanelManager>();
        clubPanelManager.Initialize(clubPanelBuilder);
    }

    // ====================================================================
    //  工具方法
    // ====================================================================

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

    private void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>创建行动图标按钮（圆形，用于底部行动栏）</summary>
    public Button CreateActionIconButton(Transform parent, string name, string icon, string label, Color iconColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(70, 75);

        VerticalLayoutGroup vlg = btnObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        // 图标圆形
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform irt = iconObj.AddComponent<RectTransform>();
        irt.sizeDelta = new Vector2(55, 55);
        Image iconBg = iconObj.AddComponent<Image>();
        iconBg.color = iconColor;

        CreateTMPText("IconSym", iconObj.transform, icon,
            24f, TextWhite, TextAlignmentOptions.Center, new Vector2(55, 55));

        // 按钮组件挂在最外层
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = iconBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = iconColor;
        cb.highlightedColor = new Color(
            Mathf.Min(1, iconColor.r + 0.15f),
            Mathf.Min(1, iconColor.g + 0.15f),
            Mathf.Min(1, iconColor.b + 0.15f), 1);
        cb.pressedColor = new Color(iconColor.r * 0.8f, iconColor.g * 0.8f, iconColor.b * 0.8f, 1);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        btnObj.AddComponent<CanvasGroup>();

        return btn;
    }
}
