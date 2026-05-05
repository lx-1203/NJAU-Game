using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 成就 UI：负责游戏内解锁提示，以及首页/暂停页可打开的成就回顾界面。
/// </summary>
public class AchievementUI : MonoBehaviour
{
    public static AchievementUI Instance { get; private set; }

    public bool isReviewShowing { get; private set; }

    private enum ReviewCategory
    {
        Basic,
        Study,
        Social,
        Sport,
        Money,
        Growth
    }

    private enum ReviewFilter
    {
        All,
        Locked,
        Unlocked
    }

    private const float NotificationSlideDuration = 0.28f;
    private const float NotificationStayDuration = 2.3f;
    private const float NotificationFadeDuration = 0.35f;
    private const float ReviewFadeDuration = 0.22f;
    private const int NotificationCanvasOrder = 150;
    private const int ReviewCanvasOrder = 300;

    private static readonly Color Gold = new Color32(0xF2, 0xC7, 0x6C, 0xFF);
    private static readonly Color WarmPaper = new Color32(0xF7, 0xF0, 0xDF, 0xFF);
    private static readonly Color PanelBrown = new Color32(0x6E, 0x5A, 0x49, 0xF4);
    private static readonly Color PanelInner = new Color32(0xFB, 0xF7, 0xEE, 0xFF);
    private static readonly Color AccentBrown = new Color32(0x78, 0x5A, 0x40, 0xFF);
    private static readonly Color AccentMuted = new Color32(0x98, 0x88, 0x76, 0xFF);
    private static readonly Color Green = new Color32(0x1F, 0xB3, 0x47, 0xFF);
    private static readonly Color Blue = new Color32(0x2B, 0x7B, 0xF5, 0xFF);
    private static readonly Color Overlay = new Color(0f, 0f, 0f, 0.58f);
    private static readonly Color TabIdle = new Color32(0xEF, 0xEA, 0xE2, 0xF2);
    private static readonly Color TabActive = new Color32(0xFF, 0xF1, 0xB8, 0xFF);
    private static readonly Color FilterIdle = new Color32(0xF5, 0xF2, 0xEC, 0xF7);
    private static readonly Color FilterActive = new Color32(0xF8, 0xC9, 0xC8, 0xFF);
    private static readonly Color ItemUnlocked = new Color32(0xFF, 0xF4, 0xC7, 0xFF);
    private static readonly Color ItemLocked = new Color32(0xE8, 0xDE, 0xCC, 0xE8);
    private static readonly Color ItemBorder = new Color32(0xE2, 0xD6, 0xBC, 0xFF);
    private static readonly Color LockedIcon = new Color32(0xA9, 0xA4, 0x9B, 0xFF);

    private readonly Queue<AchievementDefinition> notificationQueue = new Queue<AchievementDefinition>();
    private readonly Dictionary<ReviewCategory, Button> categoryButtons = new Dictionary<ReviewCategory, Button>();
    private readonly Dictionary<ReviewCategory, TextMeshProUGUI> categoryTexts = new Dictionary<ReviewCategory, TextMeshProUGUI>();
    private readonly Dictionary<ReviewFilter, Button> filterButtons = new Dictionary<ReviewFilter, Button>();
    private readonly Dictionary<ReviewFilter, TextMeshProUGUI> filterTexts = new Dictionary<ReviewFilter, TextMeshProUGUI>();

    private Canvas notificationCanvas;
    private bool isShowingNotification;

    private Canvas reviewCanvas;
    private CanvasGroup reviewCanvasGroup;
    private RectTransform reviewPanelBody;
    private RectTransform reviewListContent;
    private TextMeshProUGUI summaryText;
    private TextMeshProUGUI titleProgressText;
    private ScrollRect reviewScrollRect;
    private Coroutine reviewAnimationCoroutine;

    private ReviewCategory currentCategory = ReviewCategory.Basic;
    private ReviewFilter currentFilter = ReviewFilter.All;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateNotificationCanvas();
    }

    private void Start()
    {
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
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (AchievementSystem.Instance == null)
        {
            yield return null;
        }

        AchievementSystem.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
    }

    private void OnAchievementUnlocked(AchievementDefinition def)
    {
        notificationQueue.Enqueue(def);
        if (!isShowingNotification)
        {
            ShowNextNotification();
        }
    }

    private void CreateNotificationCanvas()
    {
        GameObject canvasObject = new GameObject("AchievementNotificationCanvas");
        canvasObject.transform.SetParent(transform, false);

        notificationCanvas = canvasObject.AddComponent<Canvas>();
        notificationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        notificationCanvas.sortingOrder = NotificationCanvasOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ShowNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isShowingNotification = false;
            return;
        }

        isShowingNotification = true;
        AchievementDefinition definition = notificationQueue.Dequeue();
        GameObject popup = CreateNotificationPopup(definition);
        StartCoroutine(NotificationCoroutine(popup));
    }

    private GameObject CreateNotificationPopup(AchievementDefinition def)
    {
        GameObject popupObject = new GameObject("AchievementNotification");
        popupObject.transform.SetParent(notificationCanvas.transform, false);

        RectTransform popupRect = popupObject.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(1f, 1f);
        popupRect.anchorMax = new Vector2(1f, 1f);
        popupRect.pivot = new Vector2(1f, 1f);
        popupRect.anchoredPosition = new Vector2(460f, -36f);
        popupRect.sizeDelta = new Vector2(430f, 126f);

        Image background = popupObject.AddComponent<Image>();
        background.color = new Color(0.13f, 0.11f, 0.09f, 0.95f);

        Outline outline = popupObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.9f, 0.62f, 0.4f);
        outline.effectDistance = new Vector2(2f, -2f);

        CanvasGroup canvasGroup = popupObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        HorizontalLayoutGroup layout = popupObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject iconRoot = CreateUIObject("IconRoot", popupRect);
        LayoutElement iconLayout = iconRoot.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 72f;
        iconLayout.preferredHeight = 72f;
        Image iconBg = iconRoot.AddComponent<Image>();
        iconBg.color = new Color(Gold.r, Gold.g, Gold.b, 0.18f);

        TextMeshProUGUI iconText = CreateTMPText(iconRoot.transform, "Icon", "★", 42f, Gold, TextAlignmentOptions.Center);
        Stretch(iconText.rectTransform);

        GameObject contentRoot = CreateUIObject("ContentRoot", popupRect);
        LayoutElement contentLayout = contentRoot.AddComponent<LayoutElement>();
        contentLayout.preferredWidth = 300f;
        contentLayout.preferredHeight = 82f;

        VerticalLayoutGroup contentGroup = contentRoot.AddComponent<VerticalLayoutGroup>();
        contentGroup.spacing = 2f;
        contentGroup.childAlignment = TextAnchor.UpperLeft;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = false;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;

        TextMeshProUGUI unlockText = CreateTMPText(contentRoot.transform, "UnlockText", "成就解锁", 19f, Gold, TextAlignmentOptions.Left);
        unlockText.fontStyle = FontStyles.Bold;
        unlockText.rectTransform.sizeDelta = new Vector2(300f, 26f);

        TextMeshProUGUI nameText = CreateTMPText(contentRoot.transform, "NameText", def.name, 28f, Color.white, TextAlignmentOptions.Left);
        nameText.fontStyle = FontStyles.Bold;
        nameText.rectTransform.sizeDelta = new Vector2(300f, 34f);

        TextMeshProUGUI descText = CreateTMPText(contentRoot.transform, "DescText", def.description, 17f, new Color(0.86f, 0.82f, 0.74f, 1f), TextAlignmentOptions.Left);
        descText.rectTransform.sizeDelta = new Vector2(300f, 24f);

        return popupObject;
    }

    private IEnumerator NotificationCoroutine(GameObject popup)
    {
        if (popup == null)
        {
            yield break;
        }

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        float targetX = -36f;
        float startX = 460f;
        float y = popupRect.anchoredPosition.y;

        float elapsed = 0f;
        while (elapsed < NotificationSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / NotificationSlideDuration));
            popupRect.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), y);
            yield return null;
        }

        popupRect.anchoredPosition = new Vector2(targetX, y);
        yield return new WaitForSecondsRealtime(NotificationStayDuration);

        elapsed = 0f;
        while (elapsed < NotificationFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / NotificationFadeDuration);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }

        Destroy(popup);
        ShowNextNotification();
    }

    public void ShowReviewPanel()
    {
        UIFlowGuard.EnsureEventSystem();

        if (!UIFlowGuard.PrepareForExclusiveWindow(UIFlowGuard.WindowAchievementReview))
        {
            ShowSystemNotification("成就回顾未打开", "当前还有其他关键界面占用操作，先处理完再来看成就记录吧。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return;
        }

        if (isReviewShowing)
        {
            RefreshReviewList();
            return;
        }

        isReviewShowing = true;

        EnsureReviewDependencies();

        if (reviewCanvas == null)
        {
            CreateReviewCanvas();
            CreateReviewPanel();
        }

        reviewCanvas.gameObject.SetActive(true);
        RefreshReviewList();
        if (reviewAnimationCoroutine != null)
        {
            StopCoroutine(reviewAnimationCoroutine);
        }
        reviewAnimationCoroutine = StartCoroutine(ReviewPanelOpenCoroutine());
    }

    public void HideReviewPanel()
    {
        if (!isReviewShowing || reviewCanvas == null)
        {
            return;
        }

        if (reviewAnimationCoroutine != null)
        {
            StopCoroutine(reviewAnimationCoroutine);
        }
        reviewAnimationCoroutine = StartCoroutine(ReviewPanelCloseCoroutine());
    }

    public void HideReviewPanelImmediate()
    {
        if (reviewAnimationCoroutine != null)
        {
            StopCoroutine(reviewAnimationCoroutine);
            reviewAnimationCoroutine = null;
        }

        if (reviewCanvas != null)
        {
            reviewCanvas.gameObject.SetActive(false);
        }

        if (reviewCanvasGroup != null)
        {
            reviewCanvasGroup.alpha = 0f;
        }

        isReviewShowing = false;
    }

    private void EnsureReviewDependencies()
    {
        if (AchievementSystem.Instance == null)
        {
            GameObject systemObject = new GameObject("AchievementSystem");
            systemObject.AddComponent<AchievementSystem>();

            if (AchievementSystem.Instance == null)
            {
                ShowSystemNotification("成就系统未就绪", "成就数据这次没有成功初始化，稍后再打开回顾界面试试。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
        }
    }

    private void CreateReviewCanvas()
    {
        GameObject canvasObject = new GameObject("AchievementReviewCanvas");
        canvasObject.transform.SetParent(transform, false);

        reviewCanvas = canvasObject.AddComponent<Canvas>();
        reviewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        reviewCanvas.sortingOrder = ReviewCanvasOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        reviewCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        reviewCanvasGroup.alpha = 0f;
    }

    private void CreateReviewPanel()
    {
        GameObject root = CreateUIObject("ReviewRoot", reviewCanvas.transform as RectTransform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        Image overlay = root.AddComponent<Image>();
        overlay.color = Overlay;

        Button overlayButton = root.AddComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        overlayButton.onClick.AddListener(HideReviewPanel);

        GameObject shell = CreateUIObject("PanelShell", rootRect);
        reviewPanelBody = shell.GetComponent<RectTransform>();
        reviewPanelBody.anchorMin = new Vector2(0.08f, 0.07f);
        reviewPanelBody.anchorMax = new Vector2(0.92f, 0.92f);
        reviewPanelBody.pivot = new Vector2(0.5f, 0.5f);
        reviewPanelBody.offsetMin = Vector2.zero;
        reviewPanelBody.offsetMax = Vector2.zero;

        Image shellImage = shell.AddComponent<Image>();
        shellImage.color = PanelBrown;
        Outline shellOutline = shell.AddComponent<Outline>();
        shellOutline.effectColor = new Color(0.88f, 0.78f, 0.6f, 0.55f);
        shellOutline.effectDistance = new Vector2(3f, -3f);

        GameObject chrome = CreateUIObject("Chrome", reviewPanelBody);
        RectTransform chromeRect = chrome.GetComponent<RectTransform>();
        chromeRect.anchorMin = Vector2.zero;
        chromeRect.anchorMax = Vector2.one;
        chromeRect.offsetMin = new Vector2(16f, 16f);
        chromeRect.offsetMax = new Vector2(-16f, -16f);
        Image chromeImage = chrome.AddComponent<Image>();
        chromeImage.color = new Color(0.38f, 0.31f, 0.26f, 0.62f);

        Button chromeButton = chrome.AddComponent<Button>();
        chromeButton.transition = Selectable.Transition.None;

        CreateTopTabs(reviewPanelBody);
        CreateCloseButton(reviewPanelBody);
        CreateInnerPanel(reviewPanelBody);
    }

    private void CreateTopTabs(RectTransform parent)
    {
        GameObject tabBar = CreateUIObject("TabBar", parent);
        RectTransform tabBarRect = tabBar.GetComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0.16f, 0.915f);
        tabBarRect.anchorMax = new Vector2(0.88f, 1.02f);
        tabBarRect.offsetMin = Vector2.zero;
        tabBarRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 18f;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = false;
        tabLayout.childControlHeight = false;
        tabLayout.childForceExpandWidth = false;
        tabLayout.childForceExpandHeight = false;

        CreateCategoryTab(tabBar.transform, ReviewCategory.Basic, "属性");
        CreateCategoryTab(tabBar.transform, ReviewCategory.Study, "学业");
        CreateCategoryTab(tabBar.transform, ReviewCategory.Social, "人际");
        CreateCategoryTab(tabBar.transform, ReviewCategory.Sport, "体魄");
        CreateCategoryTab(tabBar.transform, ReviewCategory.Money, "财富");
        CreateCategoryTab(tabBar.transform, ReviewCategory.Growth, "其他");
    }

    private void CreateCloseButton(RectTransform parent)
    {
        GameObject closeButtonObject = CreateButton(parent, "CloseButton", "×", 56f, new Vector2(74f, 74f), HideReviewPanel);
        RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.985f, 0.995f);
        closeRect.anchorMax = new Vector2(0.985f, 0.995f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = Vector2.zero;

        Image image = closeButtonObject.GetComponent<Image>();
        image.color = new Color(0.72f, 0.58f, 0.44f, 0.95f);

        TextMeshProUGUI text = closeButtonObject.GetComponentInChildren<TextMeshProUGUI>();
        text.color = new Color(1f, 0.97f, 0.93f, 1f);
    }

    private void CreateInnerPanel(RectTransform parent)
    {
        GameObject board = CreateUIObject("Board", parent);
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.anchorMin = new Vector2(0.12f, 0.07f);
        boardRect.anchorMax = new Vector2(0.95f, 0.88f);
        boardRect.offsetMin = Vector2.zero;
        boardRect.offsetMax = Vector2.zero;

        Image boardImage = board.AddComponent<Image>();
        boardImage.color = Gold;

        GameObject paper = CreateUIObject("Paper", boardRect);
        RectTransform paperRect = paper.GetComponent<RectTransform>();
        paperRect.anchorMin = Vector2.zero;
        paperRect.anchorMax = Vector2.one;
        paperRect.offsetMin = new Vector2(22f, 20f);
        paperRect.offsetMax = new Vector2(-22f, -20f);
        Image paperImage = paper.AddComponent<Image>();
        paperImage.color = PanelInner;

        Outline paperOutline = paper.AddComponent<Outline>();
        paperOutline.effectColor = new Color(0.79f, 0.72f, 0.59f, 0.55f);
        paperOutline.effectDistance = new Vector2(2f, -2f);

        CreateFilterRibbonStack(parent);
        CreateDecorativePocket(parent);
        CreateNotebookPageShadow(parent);
        CreateBoardHeader(paperRect);
        CreateAchievementScrollArea(paperRect);
        CreateBoardSummary(paperRect);
    }

    private void CreateFilterRibbonStack(RectTransform parent)
    {
        GameObject stack = CreateUIObject("FilterStack", parent);
        RectTransform stackRect = stack.GetComponent<RectTransform>();
        stackRect.anchorMin = new Vector2(0.015f, 0.52f);
        stackRect.anchorMax = new Vector2(0.145f, 0.83f);
        stackRect.offsetMin = Vector2.zero;
        stackRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = stack.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateFilterButton(stack.transform, ReviewFilter.All, "所有");
        CreateFilterButton(stack.transform, ReviewFilter.Locked, "未完成");
        CreateFilterButton(stack.transform, ReviewFilter.Unlocked, "已完成");
    }

    private void CreateDecorativePocket(RectTransform parent)
    {
        GameObject pocket = CreateUIObject("SidePocket", parent);
        RectTransform pocketRect = pocket.GetComponent<RectTransform>();
        pocketRect.anchorMin = new Vector2(0.965f, 0.34f);
        pocketRect.anchorMax = new Vector2(1.04f, 0.58f);
        pocketRect.offsetMin = Vector2.zero;
        pocketRect.offsetMax = Vector2.zero;
        Image pocketImage = pocket.AddComponent<Image>();
        pocketImage.color = new Color(0.42f, 0.33f, 0.28f, 0.86f);
        pocketImage.raycastTarget = false;
        Outline outline = pocket.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.72f, 0.62f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI dot = CreateTMPText(pocket.transform, "Dot", "●", 34f, new Color(0.75f, 0.84f, 0.86f, 0.95f), TextAlignmentOptions.Center);
        dot.raycastTarget = false;
        Stretch(dot.rectTransform);
    }

    private void CreateNotebookPageShadow(RectTransform parent)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject page = CreateUIObject("PageShadow" + i, parent);
            RectTransform rect = page.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.14f + i * 0.008f, 0.055f + i * 0.012f);
            rect.anchorMax = new Vector2(0.955f + i * 0.008f, 0.855f + i * 0.012f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            Image image = page.AddComponent<Image>();
            image.color = new Color(0.82f, 0.78f, 0.63f, 0.35f - i * 0.06f);
            image.raycastTarget = false;
        }
    }

    private void CreateBoardHeader(RectTransform parent)
    {
        GameObject header = CreateUIObject("Header", parent);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.03f, 0.87f);
        headerRect.anchorMax = new Vector2(0.97f, 0.965f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        GameObject titleRoot = CreateUIObject("HeaderTitle", headerRect);
        RectTransform titleRect = titleRoot.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0.52f, 1f);
        titleRect.offsetMin = new Vector2(20f, 0f);
        titleRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleText = CreateTMPText(titleRoot.transform, "Title", "成就回顾", 40f, AccentBrown, TextAlignmentOptions.Left);
        Stretch(titleText.rectTransform);
        titleText.fontStyle = FontStyles.Bold;

        GameObject progressRoot = CreateUIObject("HeaderProgress", headerRect);
        RectTransform progressRect = progressRoot.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.52f, 0f);
        progressRect.anchorMax = new Vector2(1f, 1f);
        progressRect.offsetMin = new Vector2(0f, 0f);
        progressRect.offsetMax = new Vector2(-18f, 0f);

        titleProgressText = CreateTMPText(progressRoot.transform, "Progress", "0/0", 28f, AccentMuted, TextAlignmentOptions.Right);
        Stretch(titleProgressText.rectTransform);
        titleProgressText.fontStyle = FontStyles.Bold;
    }

    private void CreateAchievementScrollArea(RectTransform parent)
    {
        GameObject scrollObject = CreateUIObject("ScrollView", parent);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.03f, 0.08f);
        scrollRect.anchorMax = new Vector2(0.97f, 0.85f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        Image scrollBg = scrollObject.AddComponent<Image>();
        scrollBg.color = new Color(0.96f, 0.93f, 0.84f, 0.82f);
        CreateGridBackground(scrollRect);

        reviewScrollRect = scrollObject.AddComponent<ScrollRect>();
        reviewScrollRect.horizontal = false;
        reviewScrollRect.vertical = true;
        reviewScrollRect.scrollSensitivity = 28f;
        reviewScrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUIObject("Viewport", scrollRect);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        viewport.AddComponent<Image>().color = Color.clear;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        viewport.transform.SetAsLastSibling();

        GameObject content = CreateUIObject("Content", viewportRect);
        reviewListContent = content.GetComponent<RectTransform>();
        reviewListContent.anchorMin = new Vector2(0f, 1f);
        reviewListContent.anchorMax = new Vector2(1f, 1f);
        reviewListContent.pivot = new Vector2(0.5f, 1f);
        reviewListContent.offsetMin = new Vector2(18f, 0f);
        reviewListContent.offsetMax = new Vector2(-18f, 0f);
        reviewListContent.anchoredPosition = Vector2.zero;
        reviewListContent.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(592f, 118f);
        grid.spacing = new Vector2(18f, 16f);
        grid.padding = new RectOffset(8, 8, 8, 28);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        reviewScrollRect.viewport = viewportRect;
        reviewScrollRect.content = reviewListContent;
    }

    private void CreateBoardSummary(RectTransform parent)
    {
        GameObject footer = CreateUIObject("Summary", parent);
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0.03f, 0.015f);
        footerRect.anchorMax = new Vector2(0.97f, 0.07f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = Vector2.zero;

        summaryText = CreateTMPText(footer.transform, "SummaryText", string.Empty, 22f, AccentMuted, TextAlignmentOptions.Center);
        Stretch(summaryText.rectTransform);
    }

    private void CreateGridBackground(RectTransform parent)
    {
        Color lineColor = new Color(0.78f, 0.72f, 0.62f, 0.24f);
        for (int i = 1; i < 12; i++)
        {
            RectTransform line = CreateUIObject("GridV" + i, parent).GetComponent<RectTransform>();
            line.anchorMin = new Vector2(i / 12f, 0f);
            line.anchorMax = new Vector2(i / 12f, 1f);
            line.sizeDelta = new Vector2(1f, 0f);
            line.anchoredPosition = Vector2.zero;
            Image image = line.gameObject.AddComponent<Image>();
            image.color = lineColor;
            image.raycastTarget = false;
        }

        for (int i = 1; i < 9; i++)
        {
            RectTransform line = CreateUIObject("GridH" + i, parent).GetComponent<RectTransform>();
            line.anchorMin = new Vector2(0f, i / 9f);
            line.anchorMax = new Vector2(1f, i / 9f);
            line.sizeDelta = new Vector2(0f, 1f);
            line.anchoredPosition = Vector2.zero;
            Image image = line.gameObject.AddComponent<Image>();
            image.color = lineColor;
            image.raycastTarget = false;
        }
    }

    private void CreateCategoryTab(Transform parent, ReviewCategory category, string label)
    {
        GameObject tabObject = CreateButton(parent, label + "Tab", label, 30f, new Vector2(156f, 66f), () => SetCategory(category));
        Image image = tabObject.GetComponent<Image>();
        image.color = TabIdle;

        TextMeshProUGUI text = tabObject.GetComponentInChildren<TextMeshProUGUI>();
        text.color = AccentMuted;
        text.fontStyle = FontStyles.Bold;

        categoryButtons[category] = tabObject.GetComponent<Button>();
        categoryTexts[category] = text;
    }

    private void CreateFilterButton(Transform parent, ReviewFilter filter, string label)
    {
        GameObject buttonObject = CreateButton(parent, label + "Filter", label, 30f, new Vector2(190f, 82f), () => SetFilter(filter));
        Image image = buttonObject.GetComponent<Image>();
        image.color = FilterIdle;

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 82f;

        TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        text.color = AccentBrown;
        text.fontStyle = FontStyles.Bold;

        filterButtons[filter] = buttonObject.GetComponent<Button>();
        filterTexts[filter] = text;
    }

    private void SetCategory(ReviewCategory category)
    {
        if (currentCategory == category && reviewCanvas != null && reviewCanvas.gameObject.activeSelf)
        {
            return;
        }

        currentCategory = category;
        RefreshReviewList();
    }

    private void SetFilter(ReviewFilter filter)
    {
        if (currentFilter == filter && reviewCanvas != null && reviewCanvas.gameObject.activeSelf)
        {
            return;
        }

        currentFilter = filter;
        RefreshReviewList();
    }

    private void RefreshReviewList()
    {
        if (reviewListContent == null || AchievementSystem.Instance == null)
        {
            ShowSystemNotification("成就回顾未刷新", "成就数据或界面内容区暂时不可用，这次没法更新列表。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return;
        }

        for (int i = reviewListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(reviewListContent.GetChild(i).gameObject);
        }

        List<AchievementDefinition> allAchievements = AchievementSystem.Instance.GetAllAchievements() ?? new List<AchievementDefinition>();
        List<AchievementDefinition> categoryAchievements = allAchievements
            .Where(def => GetCategoryForAchievement(def) == currentCategory)
            .ToList();

        List<AchievementDefinition> filteredAchievements = categoryAchievements
            .Where(PassesCurrentFilter)
            .OrderByDescending(def => AchievementSystem.Instance.IsUnlocked(def.id))
            .ThenBy(def => def.id)
            .ToList();

        if (filteredAchievements.Count == 0)
        {
            GameObject emptyState = CreateEmptyReviewState(categoryAchievements.Count > 0);
            emptyState.transform.SetParent(reviewListContent, false);
        }
        else
        {
            foreach (AchievementDefinition definition in filteredAchievements)
            {
                bool unlocked = AchievementSystem.Instance.IsUnlocked(definition.id);
                GameObject item = CreateAchievementItem(definition, unlocked);
                item.transform.SetParent(reviewListContent, false);
            }
        }

        if (reviewScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(reviewListContent);
            if (reviewScrollRect.viewport != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(reviewScrollRect.viewport);
            }
            reviewScrollRect.verticalNormalizedPosition = 1f;
        }

        int totalUnlocked = AchievementSystem.Instance.GetUnlockedCount();
        int totalCount = allAchievements.Count;
        int categoryUnlocked = categoryAchievements.Count(def => AchievementSystem.Instance.IsUnlocked(def.id));

        if (titleProgressText != null)
        {
            titleProgressText.text = string.Format("{0}/{1}", totalUnlocked, totalCount);
        }

        if (summaryText != null)
        {
            summaryText.text = string.Format("{0}  {1}/{2}", GetSummaryLabel(), categoryUnlocked, categoryAchievements.Count);
        }

        UpdateCategoryTabStyles();
        UpdateFilterStyles(filteredAchievements.Count);
    }

    private void ShowSystemNotification(string title, string message, Color color, float duration)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, color, duration);
        }
    }

    private void UpdateCategoryTabStyles()
    {
        List<AchievementDefinition> allAchievements = AchievementSystem.Instance != null
            ? AchievementSystem.Instance.GetAllAchievements() ?? new List<AchievementDefinition>()
            : new List<AchievementDefinition>();

        foreach (KeyValuePair<ReviewCategory, Button> pair in categoryButtons)
        {
            bool active = pair.Key == currentCategory;
            Image image = pair.Value.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? TabActive : TabIdle;
            }

            if (categoryTexts.TryGetValue(pair.Key, out TextMeshProUGUI text))
            {
                int unlocked = allAchievements.Count(def => GetCategoryForAchievement(def) == pair.Key && AchievementSystem.Instance != null && AchievementSystem.Instance.IsUnlocked(def.id));
                int total = allAchievements.Count(def => GetCategoryForAchievement(def) == pair.Key);
                text.text = string.Format("{0}{1}", GetCategoryLabel(pair.Key), total > 0 ? $" {unlocked}/{total}" : string.Empty);
                text.fontSize = active ? 29f : 27f;
                text.color = active ? AccentBrown : AccentMuted;
            }
        }
    }

    private void UpdateFilterStyles(int visibleCount)
    {
        foreach (KeyValuePair<ReviewFilter, Button> pair in filterButtons)
        {
            bool active = pair.Key == currentFilter;
            Image image = pair.Value.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? FilterActive : FilterIdle;
            }

            if (filterTexts.TryGetValue(pair.Key, out TextMeshProUGUI text))
            {
                text.color = active ? new Color32(0x5C, 0x35, 0x2D, 0xFF) : AccentBrown;
                if (active)
                {
                    text.text = string.Format("{0}\n{1}", GetFilterLabel(pair.Key), visibleCount);
                    text.fontSize = 21f;
                }
                else
                {
                    text.text = GetFilterLabel(pair.Key);
                    text.fontSize = 28f;
                }
            }
        }
    }

    private GameObject CreateAchievementItem(AchievementDefinition definition, bool unlocked)
    {
        GameObject itemObject = CreateUIObject("Achievement_" + definition.id, null);
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(592f, 118f);

        Image background = itemObject.AddComponent<Image>();
        background.color = unlocked ? ItemUnlocked : ItemLocked;

        Outline outline = itemObject.AddComponent<Outline>();
        outline.effectColor = ItemBorder;
        outline.effectDistance = new Vector2(2f, -2f);

        HorizontalLayoutGroup layout = itemObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject iconRoot = CreateUIObject("IconRoot", itemRect);
        LayoutElement iconLayout = iconRoot.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 72f;
        iconLayout.preferredHeight = 72f;

        Image iconBg = iconRoot.AddComponent<Image>();
        iconBg.color = unlocked ? WarmPaper : new Color(1f, 1f, 1f, 0.28f);

        iconRoot.transform.SetAsLastSibling();
        TextMeshProUGUI iconText = CreateTMPText(iconRoot.transform, "Icon", unlocked ? GetUnlockedIcon(definition) : "锁", 31f, unlocked ? AccentBrown : LockedIcon, TextAlignmentOptions.Center);
        Stretch(iconText.rectTransform);

        GameObject textRoot = CreateUIObject("TextRoot", itemRect);
        LayoutElement textLayout = textRoot.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        textLayout.preferredHeight = 78f;

        VerticalLayoutGroup textGroup = textRoot.AddComponent<VerticalLayoutGroup>();
        textGroup.spacing = 6f;
        textGroup.childAlignment = TextAnchor.MiddleLeft;
        textGroup.childControlWidth = true;
        textGroup.childControlHeight = false;
        textGroup.childForceExpandWidth = true;
        textGroup.childForceExpandHeight = false;

        TextMeshProUGUI nameText = CreateTMPText(
            textRoot.transform,
            "Name",
            definition.name,
            26f,
            unlocked ? AccentBrown : AccentMuted,
            TextAlignmentOptions.Left);
        nameText.fontStyle = FontStyles.Bold;
        nameText.rectTransform.sizeDelta = new Vector2(0f, 34f);

        TextMeshProUGUI descText = CreateTMPText(
            textRoot.transform,
            "Desc",
            definition.description,
            18f,
            unlocked ? AccentMuted : new Color(0.56f, 0.53f, 0.5f, 1f),
            TextAlignmentOptions.Left);
        descText.rectTransform.sizeDelta = new Vector2(0f, 28f);

        GameObject rewardRoot = CreateUIObject("RewardRoot", itemRect);
        LayoutElement rewardLayout = rewardRoot.AddComponent<LayoutElement>();
        rewardLayout.preferredWidth = 128f;
        rewardLayout.preferredHeight = 66f;

        VerticalLayoutGroup rewardGroup = rewardRoot.AddComponent<VerticalLayoutGroup>();
        rewardGroup.spacing = 6f;
        rewardGroup.childAlignment = TextAnchor.MiddleRight;
        rewardGroup.childControlWidth = true;
        rewardGroup.childControlHeight = false;
        rewardGroup.childForceExpandWidth = true;
        rewardGroup.childForceExpandHeight = false;

        TextMeshProUGUI rewardText = CreateTMPText(rewardRoot.transform, "Reward", string.Format("荣誉 +{0}", definition.points), 19f, Green, TextAlignmentOptions.Right);
        rewardText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        rewardText.rectTransform.sizeDelta = new Vector2(120f, 24f);

        TextMeshProUGUI statusText = CreateTMPText(rewardRoot.transform, "Status", unlocked ? "已完成" : "未完成", 17f, unlocked ? Blue : AccentMuted, TextAlignmentOptions.Right);
        statusText.rectTransform.sizeDelta = new Vector2(120f, 22f);

        if (unlocked)
        {
            GameObject seal = CreateUIObject("Seal", itemRect);
            RectTransform sealRect = seal.GetComponent<RectTransform>();
            sealRect.anchorMin = new Vector2(1f, 1f);
            sealRect.anchorMax = new Vector2(1f, 1f);
            sealRect.pivot = new Vector2(0.5f, 0.5f);
            sealRect.sizeDelta = new Vector2(58f, 58f);
            sealRect.anchoredPosition = new Vector2(-34f, -26f);
            Image sealImage = seal.AddComponent<Image>();
            sealImage.color = new Color(1f, 0.86f, 0.24f, 0.92f);
            sealImage.raycastTarget = false;
            TextMeshProUGUI sealText = CreateTMPText(seal.transform, "Check", "✓", 38f, Color.white, TextAlignmentOptions.Center);
            Stretch(sealText.rectTransform);
        }

        return itemObject;
    }

    private GameObject CreateEmptyReviewState(bool hasCategoryContent)
    {
        GameObject itemObject = CreateUIObject("AchievementEmptyState", null);
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(592f, 132f);

        Image background = itemObject.AddComponent<Image>();
        background.color = new Color(1f, 0.99f, 0.95f, 0.92f);

        Outline outline = itemObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.86f, 0.78f, 0.67f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup layout = itemObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 18, 18);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateTMPText(itemObject.transform, "Title",
            hasCategoryContent ? "这一筛选下还没有条目" : "这一分类还没有成就条目", 24f, AccentBrown, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.rectTransform.sizeDelta = new Vector2(540f, 34f);

        TextMeshProUGUI desc = CreateTMPText(itemObject.transform, "Desc",
            hasCategoryContent ? "换个筛选看看，或者继续推进路线，把未达成的目标慢慢点亮。" : "后续随着玩法和内容继续铺开，这里会逐步补进对应收集。", 18f,
            AccentMuted, TextAlignmentOptions.Center);
        desc.enableWordWrapping = true;
        desc.overflowMode = TextOverflowModes.Overflow;
        desc.rectTransform.sizeDelta = new Vector2(540f, 50f);

        return itemObject;
    }

    private bool PassesCurrentFilter(AchievementDefinition definition)
    {
        bool unlocked = AchievementSystem.Instance != null && AchievementSystem.Instance.IsUnlocked(definition.id);

        switch (currentFilter)
        {
            case ReviewFilter.Locked:
                return !unlocked;
            case ReviewFilter.Unlocked:
                return unlocked;
            default:
                return true;
        }
    }

    private ReviewCategory GetCategoryForAchievement(AchievementDefinition definition)
    {
        if (definition == null || definition.conditions == null || definition.conditions.Count == 0)
        {
            return ReviewCategory.Basic;
        }

        AchievementConditionType type = definition.conditions[0].GetConditionType();
        switch (type)
        {
            case AchievementConditionType.Study_GreaterOrEqual:
            case AchievementConditionType.StudyCount_GreaterOrEqual:
            case AchievementConditionType.GPA_GreaterOrEqual:
            case AchievementConditionType.SemesterGrade_Equals:
                return ReviewCategory.Study;

            case AchievementConditionType.Charm_GreaterOrEqual:
            case AchievementConditionType.SocialCount_GreaterOrEqual:
            case AchievementConditionType.FriendCount_GreaterOrEqual:
                return ReviewCategory.Social;

            case AchievementConditionType.Physique_GreaterOrEqual:
            case AchievementConditionType.SleepCount_GreaterOrEqual:
            case AchievementConditionType.GoOutCount_GreaterOrEqual:
                return ReviewCategory.Sport;

            case AchievementConditionType.Money_GreaterOrEqual:
            case AchievementConditionType.Money_Less:
            case AchievementConditionType.TotalSpent_GreaterOrEqual:
                return ReviewCategory.Money;

            case AchievementConditionType.TotalRounds_GreaterOrEqual:
            case AchievementConditionType.Semester_Equals:
                return ReviewCategory.Basic;

            case AchievementConditionType.Leadership_GreaterOrEqual:
            case AchievementConditionType.Stress_GreaterOrEqual:
            case AchievementConditionType.Mood_GreaterOrEqual:
            case AchievementConditionType.AllAttributes_GreaterOrEqual:
                return ReviewCategory.Basic;

            default:
                return ReviewCategory.Growth;
        }
    }

    private string GetCategoryLabel(ReviewCategory category)
    {
        switch (category)
        {
            case ReviewCategory.Study:
                return "学业";
            case ReviewCategory.Social:
                return "人际";
            case ReviewCategory.Sport:
                return "体魄";
            case ReviewCategory.Money:
                return "财富";
            case ReviewCategory.Growth:
                return "其他";
            default:
                return "属性";
        }
    }

    private string GetFilterLabel(ReviewFilter filter)
    {
        switch (filter)
        {
            case ReviewFilter.Locked:
                return "未完成";
            case ReviewFilter.Unlocked:
                return "已完成";
            default:
                return "所有";
        }
    }

    private string GetSummaryLabel()
    {
        return string.Format("{0}分类 · {1}", GetCategoryLabel(currentCategory), GetFilterLabel(currentFilter));
    }

    private string GetUnlockedIcon(AchievementDefinition definition)
    {
        switch (GetCategoryForAchievement(definition))
        {
            case ReviewCategory.Study:
                return "学";
            case ReviewCategory.Social:
                return "友";
            case ReviewCategory.Sport:
                return "体";
            case ReviewCategory.Money:
                return "财";
            case ReviewCategory.Growth:
                return "星";
            default:
                return "新";
        }
    }

    private IEnumerator ReviewPanelOpenCoroutine()
    {
        if (reviewCanvasGroup == null || reviewPanelBody == null)
        {
            yield break;
        }

        reviewPanelBody.localScale = new Vector3(0.97f, 0.97f, 1f);
        Vector2 startPosition = new Vector2(0f, -36f);
        Vector2 endPosition = Vector2.zero;
        float elapsed = 0f;

        while (elapsed < ReviewFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ReviewFadeDuration));
            reviewCanvasGroup.alpha = t;
            reviewPanelBody.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            float scale = Mathf.Lerp(0.97f, 1f, t);
            reviewPanelBody.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        reviewCanvasGroup.alpha = 1f;
        reviewPanelBody.anchoredPosition = endPosition;
        reviewPanelBody.localScale = Vector3.one;
        reviewAnimationCoroutine = null;
    }

    private IEnumerator ReviewPanelCloseCoroutine()
    {
        if (reviewCanvasGroup == null)
        {
            isReviewShowing = false;
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = reviewCanvasGroup.alpha;

        while (elapsed < ReviewFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / ReviewFadeDuration);
            reviewCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            if (reviewPanelBody != null)
            {
                float scale = Mathf.Lerp(1f, 0.98f, t);
                reviewPanelBody.localScale = new Vector3(scale, scale, 1f);
            }
            yield return null;
        }

        reviewCanvasGroup.alpha = 0f;
        reviewCanvas.gameObject.SetActive(false);
        if (reviewPanelBody != null)
        {
            reviewPanelBody.localScale = Vector3.one;
        }
        isReviewShowing = false;
        reviewAnimationCoroutine = null;
    }

    private TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent as RectTransform);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            tmp.font = FontManager.Instance.ChineseFont;
        }

        return tmp;
    }

    private GameObject CreateButton(Transform parent, string name, string label, float fontSize, Vector2 size, UnityAction onClick)
    {
        GameObject buttonObject = CreateUIObject(name, parent as RectTransform);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = Color.white;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.targetGraphic = image;

        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        TextMeshProUGUI buttonText = CreateTMPText(buttonObject.transform, "Text", label, fontSize, AccentBrown, TextAlignmentOptions.Center);
        Stretch(buttonText.rectTransform);
        buttonText.fontStyle = FontStyles.Bold;

        return buttonObject;
    }

    private GameObject CreateUIObject(string name, RectTransform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
