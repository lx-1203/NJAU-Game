using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 成就UI管理器
/// 包含游戏内成就解锁弹窗通知和标题界面成就回顾面板
/// </summary>
public class AchievementUI : MonoBehaviour
{
    // ========== 单例 ==========

    /// <summary>
    /// 全局单例实例
    /// </summary>
    public static AchievementUI Instance { get; private set; }

    // ========== 公开属性 ==========

    /// <summary>
    /// 回顾面板是否正在显示
    /// </summary>
    public bool isReviewShowing { get; private set; }

    // ========== 弹窗相关字段 ==========

    /// <summary>弹窗独立Canvas</summary>
    private Canvas notificationCanvas;
    /// <summary>弹窗通知队列</summary>
    private Queue<AchievementDefinition> notificationQueue = new Queue<AchievementDefinition>();
    /// <summary>当前是否正在显示弹窗</summary>
    private bool isShowingNotification = false;

    // ========== 回顾面板相关字段 ==========

    /// <summary>回顾面板独立Canvas</summary>
    private Canvas reviewCanvas;
    /// <summary>回顾面板根物体</summary>
    private GameObject reviewPanelRoot;
    /// <summary>成就列表容器 (ScrollRect 的 content)</summary>
    private RectTransform reviewListContent;
    /// <summary>进度文字</summary>
    private TextMeshProUGUI progressText;
    /// <summary>面板主体 (用于动画)</summary>
    private RectTransform reviewPanelBody;
    /// <summary>面板 CanvasGroup (用于淡入淡出)</summary>
    private CanvasGroup reviewCanvasGroup;

    // ========== 常量 ==========

    private const float NOTIFICATION_SLIDE_DURATION = 0.3f;
    private const float NOTIFICATION_STAY_DURATION = 2.5f;
    private const float NOTIFICATION_FADE_DURATION = 0.5f;
    private const float REVIEW_FADE_DURATION = 0.3f;
    private const float REVIEW_ITEM_STAGGER = 0.05f;
    private const int NOTIFICATION_CANVAS_ORDER = 150;
    private const int REVIEW_CANVAS_ORDER = 300;

    // ========== 颜色常量 ==========

    private static readonly Color COLOR_GOLD = new Color(1f, 0.84f, 0f, 1f);
    private static readonly Color COLOR_DARK_BG = new Color(0.12f, 0.12f, 0.16f, 0.92f);
    private static readonly Color COLOR_GOLD_BORDER = new Color(1f, 0.84f, 0f, 0.8f);
    private static readonly Color COLOR_GRAY_BORDER = new Color(0.4f, 0.4f, 0.4f, 0.6f);
    private static readonly Color COLOR_GREEN_CHECK = new Color(0.2f, 0.9f, 0.3f, 1f);
    private static readonly Color COLOR_GRAY_LOCK = new Color(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color COLOR_OVERLAY = new Color(0f, 0f, 0f, 0.7f);
    private static readonly Color COLOR_ITEM_BG = new Color(0.18f, 0.18f, 0.22f, 0.95f);
    private static readonly Color COLOR_ITEM_LOCKED_BG = new Color(0.14f, 0.14f, 0.16f, 0.9f);

    // ========== 生命周期 ==========

    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建弹窗Canvas (只创建一次)
        CreateNotificationCanvas();
    }

    private void Start()
    {
        // 安全订阅成就解锁事件
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        }
        else
        {
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 等待 AchievementSystem 初始化后再订阅
    /// </summary>
    private IEnumerator WaitAndSubscribe()
    {
        while (AchievementSystem.Instance == null)
        {
            yield return null;
        }
        AchievementSystem.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
    }

    // ========== 事件回调 ==========

    /// <summary>
    /// 成就解锁回调，将成就加入弹窗队列
    /// </summary>
    private void OnAchievementUnlocked(AchievementDefinition def)
    {
        notificationQueue.Enqueue(def);
        if (!isShowingNotification)
        {
            ShowNextNotification();
        }
    }

    // ========== A. 弹窗通知系统 ==========

    /// <summary>
    /// 创建弹窗专用Canvas (sortingOrder=150)
    /// </summary>
    private void CreateNotificationCanvas()
    {
        GameObject canvasObj = new GameObject("AchievementNotificationCanvas");
        canvasObj.transform.SetParent(transform);

        notificationCanvas = canvasObj.AddComponent<Canvas>();
        notificationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        notificationCanvas.sortingOrder = NOTIFICATION_CANVAS_ORDER;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 弹窗Canvas不需要GraphicRaycaster，确保不阻断游戏交互
    }

    /// <summary>
    /// 从队列取出下一个成就并显示弹窗
    /// </summary>
    private void ShowNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isShowingNotification = false;
            return;
        }

        isShowingNotification = true;
        AchievementDefinition def = notificationQueue.Dequeue();
        GameObject popup = CreateNotificationPopup(def);
        StartCoroutine(NotificationCoroutine(popup));
    }

    /// <summary>
    /// 创建单个弹窗GameObject
    /// </summary>
    private GameObject CreateNotificationPopup(AchievementDefinition def)
    {
        // 弹窗根物体
        GameObject popupObj = new GameObject("NotificationPopup");
        popupObj.transform.SetParent(notificationCanvas.transform, false);

        RectTransform popupRect = popupObj.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(1f, 1f);
        popupRect.anchorMax = new Vector2(1f, 1f);
        popupRect.pivot = new Vector2(1f, 1f);
        popupRect.anchoredPosition = new Vector2(-30f, -30f);
        popupRect.sizeDelta = new Vector2(420f, 120f);

        // CanvasGroup 用于淡出动画 & 不阻断交互
        CanvasGroup cg = popupObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // 背景 (深色半透明)
        Image bgImage = popupObj.AddComponent<Image>();
        bgImage.color = COLOR_DARK_BG;
        bgImage.type = Image.Type.Sliced;

        // 横向布局
        HorizontalLayoutGroup hLayout = popupObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(16, 16, 12, 12);
        hLayout.spacing = 14f;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // 左侧: 图标占位 (金色奖杯文字)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(popupObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(64f, 64f);

        // 图标背景
        Image iconBg = iconObj.AddComponent<Image>();
        iconBg.color = new Color(1f, 0.84f, 0f, 0.15f);

        TextMeshProUGUI iconText = CreateTMPText(iconObj.transform, "IconText",
            "AC", 36, COLOR_GOLD, TextAlignmentOptions.Center);
        RectTransform iconTextRect = iconText.GetComponent<RectTransform>();
        iconTextRect.anchorMin = Vector2.zero;
        iconTextRect.anchorMax = Vector2.one;
        iconTextRect.sizeDelta = Vector2.zero;
        iconTextRect.anchoredPosition = Vector2.zero;

        // 右侧: 纵向容器
        GameObject rightObj = new GameObject("RightContent");
        rightObj.transform.SetParent(popupObj.transform, false);
        RectTransform rightRect = rightObj.AddComponent<RectTransform>();
        rightRect.sizeDelta = new Vector2(300f, 96f);

        VerticalLayoutGroup vLayout = rightObj.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(0, 0, 2, 2);
        vLayout.spacing = 2f;
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        // "成就解锁!" 小字
        TextMeshProUGUI unlockLabel = CreateTMPText(rightObj.transform, "UnlockLabel",
            "\u6210\u5c31\u89e3\u9501!", 18, COLOR_GOLD, TextAlignmentOptions.TopLeft);
        unlockLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 24f);
        unlockLabel.fontStyle = FontStyles.Bold;

        // 成就名称 大字
        TextMeshProUGUI nameText = CreateTMPText(rightObj.transform, "NameText",
            def.name, 26, Color.white, TextAlignmentOptions.TopLeft);
        nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 34f);
        nameText.fontStyle = FontStyles.Bold;

        // 描述 小字
        TextMeshProUGUI descText = CreateTMPText(rightObj.transform, "DescText",
            def.description, 16, new Color(0.75f, 0.75f, 0.75f, 1f), TextAlignmentOptions.TopLeft);
        descText.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 24f);

        return popupObj;
    }

    /// <summary>
    /// 弹窗生命周期协程: SlideIn → 停留 → FadeOut → 销毁 → 下一个
    /// </summary>
    private IEnumerator NotificationCoroutine(GameObject popup)
    {
        if (popup == null) yield break;

        RectTransform rect = popup.GetComponent<RectTransform>();
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // 初始位置 (屏幕右侧外部)
        float targetX = -30f;
        float startX = 450f; // 在屏幕右侧外
        float targetY = rect.anchoredPosition.y;

        // SlideIn 动画 (0.3s)
        float elapsed = 0f;
        while (elapsed < NOTIFICATION_SLIDE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / NOTIFICATION_SLIDE_DURATION));
            float x = Mathf.Lerp(startX, targetX, t);
            rect.anchoredPosition = new Vector2(x, targetY);
            yield return null;
        }
        rect.anchoredPosition = new Vector2(targetX, targetY);

        // 停留 2.5s
        yield return new WaitForSecondsRealtime(NOTIFICATION_STAY_DURATION);

        // FadeOut 动画 (0.5s)
        elapsed = 0f;
        while (elapsed < NOTIFICATION_FADE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / NOTIFICATION_FADE_DURATION));
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        cg.alpha = 0f;

        // 销毁弹窗
        Destroy(popup);

        // 显示队列中下一个
        ShowNextNotification();
    }

    // ========== B. 成就回顾面板 ==========

    /// <summary>
    /// 显示成就回顾面板 (从标题界面调用)
    /// </summary>
    public void ShowReviewPanel()
    {
        if (isReviewShowing) return;
        isReviewShowing = true;

        // 懒加载: 首次调用时创建Canvas和面板
        if (reviewCanvas == null)
        {
            CreateReviewCanvas();
            CreateReviewPanel();
        }

        reviewCanvas.gameObject.SetActive(true);

        // 刷新列表数据
        RefreshReviewList();

        // 播放打开动画
        StartCoroutine(ReviewPanelOpenCoroutine());
    }

    /// <summary>
    /// 隐藏成就回顾面板
    /// </summary>
    public void HideReviewPanel()
    {
        if (!isReviewShowing) return;
        StartCoroutine(ReviewPanelCloseCoroutine());
    }

    public void HideReviewPanelImmediate()
    {
        StopAllCoroutines();

        if (reviewCanvasGroup != null)
        {
            reviewCanvasGroup.alpha = 0f;
        }

        if (reviewCanvas != null)
        {
            reviewCanvas.gameObject.SetActive(false);
        }

        isReviewShowing = false;
    }

    /// <summary>
    /// 创建回顾面板专用Canvas (sortingOrder=300)
    /// </summary>
    private void CreateReviewCanvas()
    {
        GameObject canvasObj = new GameObject("AchievementReviewCanvas");
        canvasObj.transform.SetParent(transform);

        reviewCanvas = canvasObj.AddComponent<Canvas>();
        reviewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        reviewCanvas.sortingOrder = REVIEW_CANVAS_ORDER;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 整体 CanvasGroup 用于淡入淡出
        reviewCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        reviewCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 创建回顾面板内容 (全屏覆盖)
    /// </summary>
    private void CreateReviewPanel()
    {
        // 面板根物体 (全屏)
        reviewPanelRoot = new GameObject("ReviewPanelRoot");
        reviewPanelRoot.transform.SetParent(reviewCanvas.transform, false);

        RectTransform rootRect = reviewPanelRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;

        // 半透明黑色背景遮罩
        Image overlay = reviewPanelRoot.AddComponent<Image>();
        overlay.color = COLOR_OVERLAY;

        // 面板主体 (居中，带边距)
        GameObject panelBody = new GameObject("PanelBody");
        panelBody.transform.SetParent(reviewPanelRoot.transform, false);

        reviewPanelBody = panelBody.AddComponent<RectTransform>();
        reviewPanelBody.anchorMin = new Vector2(0.1f, 0.05f);
        reviewPanelBody.anchorMax = new Vector2(0.9f, 0.95f);
        reviewPanelBody.sizeDelta = Vector2.zero;
        reviewPanelBody.anchoredPosition = Vector2.zero;

        Image panelBg = panelBody.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

        VerticalLayoutGroup panelLayout = panelBody.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(30, 30, 20, 20);
        panelLayout.spacing = 10f;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        // ===== 顶部栏: 返回按钮 + 标题 =====
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(panelBody.transform, false);
        RectTransform topBarRect = topBar.AddComponent<RectTransform>();
        topBarRect.sizeDelta = new Vector2(0f, 60f);

        HorizontalLayoutGroup topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(10, 10, 5, 5);
        topLayout.spacing = 20f;
        topLayout.childAlignment = TextAnchor.MiddleLeft;
        topLayout.childControlWidth = false;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = true;

        // 返回按钮
        GameObject backBtnObj = CreateButton(topBar.transform, "BackButton",
            "\u2190\u8fd4\u56de", 22, new Vector2(120f, 50f), HideReviewPanel);
        Image backBtnImg = backBtnObj.GetComponent<Image>();
        backBtnImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

        // 标题文字
        TextMeshProUGUI titleText = CreateTMPText(topBar.transform, "TitleText",
            "\u6210\u5c31\u56de\u987e", 36, Color.white, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(400f, 50f);

        // ===== 中部: ScrollRect 列表 =====
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panelBody.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.sizeDelta = new Vector2(0f, 0f);

        // ScrollView 需要占据剩余空间
        LayoutElement scrollLE = scrollObj.AddComponent<LayoutElement>();
        scrollLE.flexibleHeight = 1f;
        scrollLE.minHeight = 200f;

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.08f, 0.08f, 0.1f, 0.5f);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;

        viewportObj.AddComponent<Image>().color = Color.clear;
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        scroll.viewport = viewportRect;

        // Content (列表容器)
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        reviewListContent = contentObj.AddComponent<RectTransform>();
        reviewListContent.anchorMin = new Vector2(0f, 1f);
        reviewListContent.anchorMax = new Vector2(1f, 1f);
        reviewListContent.pivot = new Vector2(0.5f, 1f);
        reviewListContent.anchoredPosition = Vector2.zero;
        reviewListContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(15, 15, 10, 10);
        contentLayout.spacing = 8f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = reviewListContent;

        // ===== 底部: 进度文字 =====
        GameObject bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(panelBody.transform, false);
        RectTransform bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.sizeDelta = new Vector2(0f, 50f);

        progressText = CreateTMPText(bottomBar.transform, "ProgressText",
            "\u5df2\u89e3\u9501: 0/0", 24, new Color(0.8f, 0.8f, 0.8f, 1f),
            TextAlignmentOptions.Center);
        RectTransform progressRect = progressText.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = Vector2.one;
        progressRect.sizeDelta = Vector2.zero;
        progressRect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 创建单个成就条目
    /// </summary>
    private GameObject CreateAchievementItem(AchievementDefinition def, bool unlocked)
    {
        // 条目根物体
        GameObject itemObj = new GameObject("AchievementItem_" + def.id);
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0f, 90f);

        // 背景 + 边框效果 (通过 Outline 模拟)
        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = unlocked ? COLOR_ITEM_BG : COLOR_ITEM_LOCKED_BG;

        Outline itemOutline = itemObj.AddComponent<Outline>();
        itemOutline.effectColor = unlocked ? COLOR_GOLD_BORDER : COLOR_GRAY_BORDER;
        itemOutline.effectDistance = new Vector2(2f, 2f);

        // 用于条目淡入动画
        CanvasGroup itemCG = itemObj.AddComponent<CanvasGroup>();
        itemCG.alpha = 0f;

        // 横向布局
        HorizontalLayoutGroup hLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(14, 14, 10, 10);
        hLayout.spacing = 14f;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // 左侧: 图标占位
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(itemObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(60f, 60f);

        Image iconBg = iconObj.AddComponent<Image>();
        iconBg.color = unlocked
            ? new Color(1f, 0.84f, 0f, 0.15f)
            : new Color(0.3f, 0.3f, 0.3f, 0.2f);

        string iconSymbol = unlocked ? "OK" : "?";
        Color iconColor = unlocked ? COLOR_GOLD : COLOR_GRAY_LOCK;
        TextMeshProUGUI iconText = CreateTMPText(iconObj.transform, "IconText",
            iconSymbol, 32, iconColor, TextAlignmentOptions.Center);
        RectTransform iconTextRect = iconText.GetComponent<RectTransform>();
        iconTextRect.anchorMin = Vector2.zero;
        iconTextRect.anchorMax = Vector2.one;
        iconTextRect.sizeDelta = Vector2.zero;
        iconTextRect.anchoredPosition = Vector2.zero;

        // 中间: 名称 + 描述 (纵向)
        GameObject midObj = new GameObject("MidContent");
        midObj.transform.SetParent(itemObj.transform, false);
        RectTransform midRect = midObj.AddComponent<RectTransform>();
        midRect.sizeDelta = new Vector2(0f, 70f);

        LayoutElement midLE = midObj.AddComponent<LayoutElement>();
        midLE.flexibleWidth = 1f;
        midLE.preferredHeight = 70f;

        VerticalLayoutGroup vLayout = midObj.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 4f;
        vLayout.childAlignment = TextAnchor.MiddleLeft;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        // 名称
        string displayName = unlocked ? def.name : "??????";
        Color nameColor = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        TextMeshProUGUI nameText = CreateTMPText(midObj.transform, "NameText",
            displayName, 24, nameColor, TextAlignmentOptions.MidlineLeft);
        nameText.fontStyle = FontStyles.Bold;
        nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

        // 描述
        string displayDesc = unlocked ? def.description : "??????";
        Color descColor = unlocked
            ? new Color(0.7f, 0.7f, 0.7f, 1f)
            : new Color(0.4f, 0.4f, 0.4f, 1f);
        TextMeshProUGUI descText = CreateTMPText(midObj.transform, "DescText",
            displayDesc, 18, descColor, TextAlignmentOptions.MidlineLeft);
        descText.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 26f);

        // 右侧: 状态标记
        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(itemObj.transform, false);
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.sizeDelta = new Vector2(50f, 50f);

        string statusSymbol = unlocked ? "V" : "L";
        Color statusColor = unlocked ? COLOR_GREEN_CHECK : COLOR_GRAY_LOCK;
        int statusSize = unlocked ? 32 : 28;
        TextMeshProUGUI statusText = CreateTMPText(statusObj.transform, "StatusText",
            statusSymbol, statusSize, statusColor, TextAlignmentOptions.Center);
        RectTransform statusTextRect = statusText.GetComponent<RectTransform>();
        statusTextRect.anchorMin = Vector2.zero;
        statusTextRect.anchorMax = Vector2.one;
        statusTextRect.sizeDelta = Vector2.zero;
        statusTextRect.anchoredPosition = Vector2.zero;

        return itemObj;
    }

    /// <summary>
    /// 刷新回顾列表内容
    /// </summary>
    private void RefreshReviewList()
    {
        if (reviewListContent == null) return;
        if (AchievementSystem.Instance == null) return;

        // 清除旧条目
        for (int i = reviewListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(reviewListContent.GetChild(i).gameObject);
        }

        // 获取所有成就
        var allAchievements = AchievementSystem.Instance.GetAllAchievements();
        var unlockedAchievements = AchievementSystem.Instance.GetUnlockedAchievements();
        int unlockedCount = unlockedAchievements != null ? unlockedAchievements.Count : 0;
        int totalCount = allAchievements != null ? allAchievements.Count : 0;

        // 创建条目 (已解锁的排前面)
        if (allAchievements != null)
        {
            foreach (var def in allAchievements)
            {
                bool unlocked = AchievementSystem.Instance.IsUnlocked(def.id);
                GameObject item = CreateAchievementItem(def, unlocked);
                item.transform.SetParent(reviewListContent, false);
            }
        }

        // 更新进度文字
        if (progressText != null)
        {
            progressText.text = string.Format("\u5df2\u89e3\u9501: {0}/{1}", unlockedCount, totalCount);
        }
    }

    // ========== 回顾面板动画 ==========

    /// <summary>
    /// 回顾面板打开动画: FadeIn + SlideIn + 列表条目依次淡入
    /// </summary>
    private IEnumerator ReviewPanelOpenCoroutine()
    {
        AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // 面板整体 FadeIn + SlideIn
        float elapsed = 0f;
        Vector2 bodyStart = new Vector2(0f, -50f);
        Vector2 bodyEnd = Vector2.zero;

        while (elapsed < REVIEW_FADE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / REVIEW_FADE_DURATION));
            reviewCanvasGroup.alpha = t;
            if (reviewPanelBody != null)
            {
                reviewPanelBody.anchoredPosition = Vector2.Lerp(bodyStart, bodyEnd, t);
            }
            yield return null;
        }
        reviewCanvasGroup.alpha = 1f;
        if (reviewPanelBody != null)
        {
            reviewPanelBody.anchoredPosition = bodyEnd;
        }

        // 列表条目依次 FadeIn
        if (reviewListContent != null)
        {
            for (int i = 0; i < reviewListContent.childCount; i++)
            {
                CanvasGroup itemCG = reviewListContent.GetChild(i).GetComponent<CanvasGroup>();
                if (itemCG != null)
                {
                    StartCoroutine(FadeInItem(itemCG, 0.2f));
                }
                yield return new WaitForSecondsRealtime(REVIEW_ITEM_STAGGER);
            }
        }
    }

    /// <summary>
    /// 回顾面板关闭动画: FadeOut
    /// </summary>
    private IEnumerator ReviewPanelCloseCoroutine()
    {
        AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        float elapsed = 0f;

        while (elapsed < REVIEW_FADE_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / REVIEW_FADE_DURATION));
            reviewCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        reviewCanvasGroup.alpha = 0f;

        reviewCanvas.gameObject.SetActive(false);
        isReviewShowing = false;
    }

    /// <summary>
    /// 单个条目淡入动画
    /// </summary>
    private IEnumerator FadeInItem(CanvasGroup cg, float duration)
    {
        AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            cg.alpha = t;
            yield return null;
        }
        cg.alpha = 1f;
    }

    // ========== UI 工具方法 ==========

    /// <summary>
    /// 创建 TextMeshProUGUI 文本组件
    /// </summary>
    private TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;

        return tmp;
    }

    /// <summary>
    /// 创建按钮
    /// </summary>
    private GameObject CreateButton(Transform parent, string name, string label,
        float fontSize, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = size;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.45f, 1f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        btn.colors = colors;

        if (onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }

        TextMeshProUGUI btnText = CreateTMPText(btnObj.transform, "Text",
            label, fontSize, Color.white, TextAlignmentOptions.Center);
        RectTransform textRect = btnText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        btnText.raycastTarget = false;

        return btnObj;
    }
}
