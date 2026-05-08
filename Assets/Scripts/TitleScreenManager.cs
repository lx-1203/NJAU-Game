using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 标题过渡引导界面管理器
/// 游戏启动后显示全屏循环视频，点击任意位置后在当前画面上叠加菜单
/// 所有 UI 元素在 Awake 中通过代码动态创建，无需手动拖拽
/// </summary>
public class TitleScreenManager : MonoBehaviour
{
    [Header("视频配置")]
    [Tooltip("开始界面视频文件名（位于 StreamingAssets）")]
    public string videoFileName = "Start screen.mp4";

    [Tooltip("提前切换到下一播放器的时间（秒）")]
    public float loopSwitchLeadTime = 0.12f;

    [Header("场景跳转")]
    [Tooltip("开始游戏时进入的场景名称")]
    public string gameSceneName = "GameScene";

    [Tooltip("新游戏进入 GameScene 前是否播放开场校园叙事")]
    public bool playOpeningStoryOnNewGame = true;

    [Header("颜色配置（可选覆盖）")]
    [Tooltip("如果为空则使用内置默认色")]
    public UILayoutConfig layoutConfig;

    [Header("涟漪参数")]
    [Tooltip("涟漪扩散后到菜单显示的等待时间")]
    public float transitionDelay = 0.3f;

    [Header("菜单文案")]
    [Tooltip("点击提示文案")]
    public string hintMessage = "点击任意位置继续";

    [Header("继续游戏")]
    [Tooltip("是否允许继续游戏按钮直接进入游戏（首版占位逻辑）")]
    public bool continueGameStartsGame = true;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SFXVolume";
    private const string FullscreenKey = "Fullscreen";
    private const string GalleryUnlockedEndingsKey = "Gallery_UnlockedEndings";
    private const string GalleryUnlockedCgsKey = "Gallery_UnlockedCGs";

    // ===== 运行时引用 =====
    private Canvas canvas;
    private RectTransform canvasRect;
    private CanvasGroup fadeOverlay;
    private TMP_Text hintText;
    private RectTransform hintTextRect;
    private RawImage videoImage;
    private readonly VideoPlayer[] videoPlayers = new VideoPlayer[2];
    private readonly RenderTexture[] videoTextures = new RenderTexture[2];
    private readonly bool[] playerPrepared = new bool[2];
    private Coroutine loopMonitorCoroutine;
    private int activePlayerIndex = -1;
    private int standbyPlayerIndex = -1;
    private bool hasEnteredMenu = false;
    private bool transitionRequested = false;
    private string resolvedVideoPath;
    private string resolvedVideoUrl;
    private bool notifiedMissingMenuVideo;
    private bool notifiedVideoPlaybackError;
    private bool notifiedVideoRecoveryState;
    private GameObject titleNotificationRoot;
    private Image titleNotificationBg;
    private TextMeshProUGUI titleNotificationTitleText;
    private TextMeshProUGUI titleNotificationMessageText;
    private Coroutine titleNotificationCoroutine;

    // ===== 菜单 UI =====
    private CanvasGroup menuOverlay;
    private GameObject mainMenuPanel;
    private GameObject settingsPanel;
    private Button continueGameButton;
    private Button startGameButton;
    private Button settingsButton;
    private Button quitGameButton;
    private Button backButton;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private Toggle fullscreenToggle;

    // ===== 右上角图标按钮 =====
    private Button tutorialButton;
    private Button achievementButton;
    private Button cgButton;
    private Button creditsButton;
    private RectTransform topRightIconsRoot;
    private readonly List<TextMeshProUGUI> topRightIconTexts = new List<TextMeshProUGUI>();
    private readonly List<string> topRightIconTooltips = new List<string>();

    // ===== 制作人面板 =====
    private GameObject creditsPanel;
    private CanvasGroup creditsPanelGroup;
    private RectTransform creditsListContent;
    private RectTransform creditsTagsRoot;
    private TextMeshProUGUI creditsDetailTitle;
    private TextMeshProUGUI creditsDetailRole;
    private TextMeshProUGUI creditsDetailDescription;
    private TextMeshProUGUI creditsPanelTitleText;
    private TextMeshProUGUI creditsTabText;
    private TextMeshProUGUI creditsFooterText;
    private readonly List<Button> creditsEntryButtons = new List<Button>();
    private int currentCreditsEntryIndex = 0;
    private readonly List<ZhongshanDeckCreditsEntry> creditsEntries = new List<ZhongshanDeckCreditsEntry>();

    // ===== 教程面板 =====
    private GameObject tutorialPanel;
    private CanvasGroup tutorialPanelGroup;
    private RectTransform tutorialItemListContent;
    private RectTransform tutorialDetailContent;
    private RectTransform tutorialTabsRoot;
    private TextMeshProUGUI tutorialSectionTitle;
    private TextMeshProUGUI tutorialEntryTitle;
    private TextMeshProUGUI tutorialEntryDescription;
    private readonly List<Button> tutorialCategoryButtons = new List<Button>();
    private readonly List<Button> tutorialEntryButtons = new List<Button>();
    private int currentTutorialCategoryIndex = 0;
    private int currentTutorialEntryIndex = 0;

    // ===== 更新日志面板 =====
    private GameObject changelogPanel;
    private CanvasGroup changelogPanelGroup;
    private RectTransform changelogContent;
    private Button changelogButton;
    private RectTransform changelogButtonRect;
    private TextMeshProUGUI changelogButtonLabelText;
    private TextMeshProUGUI changelogPanelTitleText;
    private readonly List<ZhongshanDeckChangelogSection> changelogSections = new List<ZhongshanDeckChangelogSection>();
    private ZhongshanDeckTitleContent titleContentData;

    // ===== 游戏CG / 结局面板 =====
    private GameObject galleryPanel;
    private CanvasGroup galleryPanelGroup;
    private RectTransform galleryGridContent;
    private Button galleryPrevPageButton;
    private Button galleryNextPageButton;
    private TextMeshProUGUI galleryPageText;
    private TextMeshProUGUI galleryGridStatusText;
    private Button galleryGenderMaleButton;
    private Button galleryGenderFemaleButton;
    private TextMeshProUGUI gallerySectionTitle;
    private TextMeshProUGUI galleryPreviewTitle;
    private TextMeshProUGUI galleryPreviewSubtitle;
    private TextMeshProUGUI galleryPreviewDescription;
    private TextMeshProUGUI galleryCounterText;
    private Image galleryPreviewImage;
    private TextMeshProUGUI galleryPreviewImageLabel;
    private Button galleryOpenButton;
    private readonly List<Button> galleryTabButtons = new List<Button>();
    private readonly List<Button> galleryEntryButtons = new List<Button>();
    private readonly List<GalleryCategory> galleryCategories = new List<GalleryCategory>();
    private int currentGalleryCategoryIndex = 0;
    private int currentGalleryEntryIndex = 0;
    private int currentGalleryPageIndex = 0;
    private int currentGalleryPageStartIndex = 0;
    private int galleryRequirementPreviewGender = 0;

    private GameObject galleryViewerPanel;
    private CanvasGroup galleryViewerGroup;
    private Image galleryViewerImage;
    private TextMeshProUGUI galleryViewerImageLabel;
    private TextMeshProUGUI galleryViewerTitle;
    private TextMeshProUGUI galleryViewerSubtitle;
    private TextMeshProUGUI galleryViewerDescription;

    // ===== 版本号 =====
    private TMP_Text versionText;
    private RectTransform versionTextRect;
    private RectTransform logoAreaRect;
    private RectTransform mainMenuPanelRect;
    private TextMeshProUGUI settingsTitleText;
    private TextMeshProUGUI settingsBackButtonLabelText;
    private readonly Dictionary<string, Button> mainMenuButtonsByAction = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);

    // ===== 颜色缓存 =====
    private Color bgColor;
    private Color textColor;
    private Color primaryColor;
    private Color secondaryColor;
    private Color panelColor;
    private Color subPanelColor;

    private static readonly Color TutorialBackdropColor = new Color(0.17f, 0.14f, 0.12f, 0.9f);
    private static readonly Color TutorialFrameColor = new Color(0.73f, 0.67f, 0.61f, 0.98f);
    private static readonly Color TutorialPaperColor = new Color(0.975f, 0.955f, 0.915f, 1f);
    private static readonly Color TutorialPaperLineColor = new Color(0.89f, 0.84f, 0.75f, 0.33f);
    private static readonly Color TutorialAccentColor = new Color(0.61f, 0.39f, 0.22f, 1f);
    private static readonly Color TutorialHighlightColor = new Color(0.98f, 0.92f, 0.69f, 1f);
    private static readonly Color TutorialTextColor = new Color(0.38f, 0.23f, 0.14f, 1f);
    private static readonly Color TutorialMutedTextColor = new Color(0.52f, 0.4f, 0.3f, 0.95f);
    private static readonly Color TutorialListHoverColor = new Color(1f, 0.95f, 0.78f, 0.7f);
    private static readonly Color TutorialListNormalColor = new Color(1f, 1f, 1f, 0f);

    private sealed class TutorialEntry
    {
        public string Title;
        public string Lead;
        public string Description;
        public string[] Highlights;

        public TutorialEntry(string title, string lead, string description, params string[] highlights)
        {
            Title = title;
            Lead = lead;
            Description = description;
            Highlights = highlights ?? Array.Empty<string>();
        }
    }

    private sealed class TutorialCategory
    {
        public string Name;
        public List<TutorialEntry> Entries;

        public TutorialCategory(string name, params TutorialEntry[] entries)
        {
            Name = name;
            Entries = new List<TutorialEntry>(entries ?? Array.Empty<TutorialEntry>());
        }
    }

    private sealed class GalleryEntry
    {
        public string Id;
        public string Title;
        public string Subtitle;
        public string Description;
        public string ResourceKey;
        public string Badge;
        public bool IsUnlocked;
        public bool IsEnding;
        public List<EndingCondition> Conditions = new List<EndingCondition>();
    }

    private sealed class GalleryCategory
    {
        public string Name;
        public List<GalleryEntry> Entries = new List<GalleryEntry>();
    }

    private readonly List<TutorialCategory> tutorialCategories = new List<TutorialCategory>();

    // ===== 呼吸动画协程 =====
    private Coroutine breathCoroutine;

    #region 生命周期

    private void Awake()
    {
        UIFlowGuard.CleanupBlockingUI();
        EnsureSaveManager();
        ReloadAuthoredTitleContent();
        ResolveVideoSource();
        CacheColors();
        BuildUI();
    }

    private void Start()
    {
        PrepareVideoBackground();

        if (StartupFlowSettings.ShouldAutoSkipTitleScreenThisTime())
        {
            StartCoroutine(AutoSkipTitleScreen());
            return;
        }

        if (hintText != null)
        {
            breathCoroutine = StartCoroutine(BreathingAnimation(hintText));
        }
    }

    private void OnDestroy()
    {
        if (loopMonitorCoroutine != null)
        {
            StopCoroutine(loopMonitorCoroutine);
            loopMonitorCoroutine = null;
        }

        for (int i = 0; i < videoPlayers.Length; i++)
        {
            if (videoPlayers[i] != null)
            {
                videoPlayers[i].prepareCompleted -= OnVideoPrepared;
                videoPlayers[i].loopPointReached -= OnVideoLoopPointReached;
                videoPlayers[i].errorReceived -= OnVideoError;
                videoPlayers[i].Stop();
            }

            if (videoTextures[i] != null)
            {
                videoTextures[i].Release();
                videoTextures[i] = null;
            }
        }
    }

    private void Update()
    {
        if (hasEnteredMenu)
        {
            if (!transitionRequested && menuOverlay != null && menuOverlay.gameObject.activeInHierarchy)
            {
                RefreshContinueButtonState();
            }
            return;
        }

        bool clicked = Input.GetMouseButtonDown(0);
        bool touched = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;

        if (clicked || touched)
        {
            Vector2 screenPos = clicked ? (Vector2)Input.mousePosition : Input.GetTouch(0).position;
            OnScreenTapped(screenPos);
        }
    }

    #endregion

    #region 颜色初始化

    private void CacheColors()
    {
        if (layoutConfig != null)
        {
            bgColor = layoutConfig.backgroundColor;
            textColor = layoutConfig.textColor;
            primaryColor = layoutConfig.primaryColor;
            secondaryColor = layoutConfig.secondaryColor;
        }
        else
        {
            bgColor = new Color(0.1f, 0.1f, 0.15f);
            textColor = new Color(0.95f, 0.95f, 0.95f);
            primaryColor = new Color(0.24f, 0.46f, 0.88f);
            secondaryColor = new Color(0.85f, 0.46f, 0.22f);
        }

        panelColor = new Color(0.05f, 0.07f, 0.12f, 0.78f);
        subPanelColor = new Color(0.08f, 0.1f, 0.16f, 0.92f);
    }

    private void ReloadAuthoredTitleContent()
    {
        titleContentData = ZhongshanDeckToolStateBridge.GetTitleContent();
        titleContentData ??= new ZhongshanDeckTitleContent();
        titleContentData.EnsureInitialized();

        creditsEntries.Clear();
        if (titleContentData.credits?.entries != null)
        {
            for (int i = 0; i < titleContentData.credits.entries.Count; i++)
            {
                ZhongshanDeckCreditsEntry entry = titleContentData.credits.entries[i];
                if (entry != null)
                {
                    creditsEntries.Add(entry.Clone());
                }
            }
        }

        changelogSections.Clear();
        if (titleContentData.changelog?.sections != null)
        {
            for (int i = 0; i < titleContentData.changelog.sections.Count; i++)
            {
                ZhongshanDeckChangelogSection section = titleContentData.changelog.sections[i];
                if (section != null)
                {
                    changelogSections.Add(section.Clone());
                }
            }
        }

        tutorialCategories.Clear();
        if (titleContentData.tutorial?.categories != null)
        {
            for (int i = 0; i < titleContentData.tutorial.categories.Count; i++)
            {
                ZhongshanDeckTutorialCategoryData categoryData = titleContentData.tutorial.categories[i];
                if (categoryData == null)
                {
                    continue;
                }

                TutorialCategory category = new TutorialCategory(categoryData.name);
                if (categoryData.entries != null)
                {
                    for (int entryIndex = 0; entryIndex < categoryData.entries.Count; entryIndex++)
                    {
                        ZhongshanDeckTutorialEntryData entryData = categoryData.entries[entryIndex];
                        if (entryData != null)
                        {
                            category.Entries.Add(new TutorialEntry(
                                entryData.title,
                                entryData.lead,
                                entryData.description,
                                entryData.highlights != null ? entryData.highlights.ToArray() : Array.Empty<string>()));
                        }
                    }
                }

                tutorialCategories.Add(category);
            }
        }
    }

    public void DebugReloadAuthoredTitleContent()
    {
        ReloadAuthoredTitleContent();
        ApplyAuthoredTitleContentToPanels();
    }

    private void ApplyAuthoredTitleContentToPanels()
    {
        hintMessage = titleContentData?.homepage?.hintMessage ?? hintMessage;
        if (hintText != null)
        {
            hintText.text = hintMessage;
        }

        if (changelogButtonLabelText != null)
        {
            changelogButtonLabelText.text = titleContentData?.homepage?.changelogButtonLabel ?? "更新日志";
        }

        ApplyHomepageLayoutOverrides();

        ApplyHomepageMenuLabels();
        ApplyTopIconLabels();

        if (settingsTitleText != null)
        {
            settingsTitleText.text = titleContentData?.homepage?.settingsPanelTitle ?? "设置";
        }

        if (settingsBackButtonLabelText != null)
        {
            settingsBackButtonLabelText.text = titleContentData?.homepage?.settingsBackButtonLabel ?? "返回";
        }

        if (creditsPanelTitleText != null)
        {
            creditsPanelTitleText.text = titleContentData?.credits?.panelTitle ?? "制作人";
        }

        if (creditsTabText != null)
        {
            creditsTabText.text = titleContentData?.credits?.tabLabel ?? "STAFF";
        }

        if (creditsFooterText != null)
        {
            creditsFooterText.text = titleContentData?.credits?.footerText ?? string.Empty;
        }

        if (changelogPanelTitleText != null)
        {
            changelogPanelTitleText.text = titleContentData?.changelog?.panelTitle ?? "更新日志";
        }

        if (creditsListContent != null)
        {
            RebuildCreditsList();
            if (creditsEntries.Count > 0)
            {
                SelectCreditsEntry(Mathf.Clamp(currentCreditsEntryIndex, 0, creditsEntries.Count - 1));
            }
        }

        if (changelogContent != null)
        {
            RebuildChangelogContent();
        }

        if (tutorialTabsRoot != null)
        {
            RebuildTutorialCategoryTabs();
            if (tutorialCategories.Count > 0)
            {
                SelectTutorialCategory(Mathf.Clamp(currentTutorialCategoryIndex, 0, tutorialCategories.Count - 1));
            }
        }
    }

    private void ApplyHomepageLayoutOverrides()
    {
        ApplyHomepageLayoutToRect(hintTextRect, ZhongshanDeckTitleContentDefaults.LayoutHint);
        ApplyHomepageLayoutToRect(logoAreaRect, ZhongshanDeckTitleContentDefaults.LayoutLogo);
        ApplyHomepageLayoutToRect(mainMenuPanelRect, ZhongshanDeckTitleContentDefaults.LayoutMainMenu);
        ApplyHomepageLayoutToRect(topRightIconsRoot, ZhongshanDeckTitleContentDefaults.LayoutTopIcons);
        ApplyHomepageLayoutToRect(changelogButtonRect, ZhongshanDeckTitleContentDefaults.LayoutChangelog);
        ApplyHomepageLayoutToRect(versionTextRect, ZhongshanDeckTitleContentDefaults.LayoutVersion);
    }

    private void ApplyHomepageLayoutToRect(RectTransform target, string key)
    {
        if (target == null || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        ZhongshanDeckHomepageLayoutItem layoutItem = FindHomepageLayoutItem(key);
        if (layoutItem == null)
        {
            return;
        }

        layoutItem.EnsureInitialized();
        Vector2 anchor = GetLayoutAnchorVector(layoutItem.anchor);
        target.anchorMin = anchor;
        target.anchorMax = anchor;
        target.pivot = GetLayoutPivotVector(layoutItem.anchor);
        target.anchoredPosition = layoutItem.anchoredPosition;
        target.sizeDelta = layoutItem.size;
        target.localScale = GetHomepageLayoutScale(key, layoutItem.size);
        target.gameObject.SetActive(layoutItem.visible);
    }

    private ZhongshanDeckHomepageLayoutItem FindHomepageLayoutItem(string key)
    {
        List<ZhongshanDeckHomepageLayoutItem> layoutItems = titleContentData?.homepage?.layoutItems;
        if (layoutItems == null)
        {
            return null;
        }

        for (int i = 0; i < layoutItems.Count; i++)
        {
            ZhongshanDeckHomepageLayoutItem item = layoutItems[i];
            if (item != null && string.Equals(item.key, key, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private static Vector2 GetLayoutAnchorVector(ZhongshanDeckLayoutAnchor anchor)
    {
        switch (anchor)
        {
            case ZhongshanDeckLayoutAnchor.TopLeft: return new Vector2(0f, 1f);
            case ZhongshanDeckLayoutAnchor.TopCenter: return new Vector2(0.5f, 1f);
            case ZhongshanDeckLayoutAnchor.TopRight: return new Vector2(1f, 1f);
            case ZhongshanDeckLayoutAnchor.LeftCenter: return new Vector2(0f, 0.5f);
            case ZhongshanDeckLayoutAnchor.RightCenter: return new Vector2(1f, 0.5f);
            case ZhongshanDeckLayoutAnchor.BottomLeft: return new Vector2(0f, 0f);
            case ZhongshanDeckLayoutAnchor.BottomCenter: return new Vector2(0.5f, 0f);
            case ZhongshanDeckLayoutAnchor.BottomRight: return new Vector2(1f, 0f);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    private static Vector2 GetLayoutPivotVector(ZhongshanDeckLayoutAnchor anchor)
    {
        switch (anchor)
        {
            case ZhongshanDeckLayoutAnchor.TopLeft: return new Vector2(0f, 1f);
            case ZhongshanDeckLayoutAnchor.TopCenter: return new Vector2(0.5f, 1f);
            case ZhongshanDeckLayoutAnchor.TopRight: return new Vector2(1f, 1f);
            case ZhongshanDeckLayoutAnchor.LeftCenter: return new Vector2(0f, 0.5f);
            case ZhongshanDeckLayoutAnchor.RightCenter: return new Vector2(1f, 0.5f);
            case ZhongshanDeckLayoutAnchor.BottomLeft: return new Vector2(0f, 0f);
            case ZhongshanDeckLayoutAnchor.BottomCenter: return new Vector2(0.5f, 0f);
            case ZhongshanDeckLayoutAnchor.BottomRight: return new Vector2(1f, 0f);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    private static Vector3 GetHomepageLayoutScale(string key, Vector2 currentSize)
    {
        Vector2 defaultSize = GetDefaultHomepageLayoutSize(key);
        if (defaultSize.x <= 0.01f || defaultSize.y <= 0.01f)
        {
            return Vector3.one;
        }

        if (string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutMainMenu, StringComparison.Ordinal) ||
            string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutTopIcons, StringComparison.Ordinal))
        {
            return new Vector3(
                Mathf.Max(0.2f, currentSize.x / defaultSize.x),
                Mathf.Max(0.2f, currentSize.y / defaultSize.y),
                1f);
        }

        return Vector3.one;
    }

    private static Vector2 GetDefaultHomepageLayoutSize(string key)
    {
        if (string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutLogo, StringComparison.Ordinal))
        {
            return new Vector2(900f, 330f);
        }

        if (string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutMainMenu, StringComparison.Ordinal))
        {
            return new Vector2(420f, 480f);
        }

        if (string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutTopIcons, StringComparison.Ordinal))
        {
            return new Vector2(292f, 80f);
        }

        if (string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutChangelog, StringComparison.Ordinal))
        {
            return new Vector2(300f, 74f);
        }

        if (string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutHint, StringComparison.Ordinal))
        {
            return new Vector2(800f, 80f);
        }

        if (string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutVersion, StringComparison.Ordinal))
        {
            return new Vector2(200f, 36f);
        }

        return Vector2.zero;
    }

    private void ApplyHomepageMenuLabels()
    {
        if (titleContentData?.homepage?.mainMenuItems == null)
        {
            return;
        }

        for (int i = 0; i < titleContentData.homepage.mainMenuItems.Count; i++)
        {
            ZhongshanDeckMenuActionLabel item = titleContentData.homepage.mainMenuItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.actionId))
            {
                continue;
            }

            if (mainMenuButtonsByAction.TryGetValue(item.actionId, out Button button) && button != null)
            {
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = item.label;
                }
            }
        }
    }

    private void ApplyTopIconLabels()
    {
        topRightIconTooltips.Clear();
        if (titleContentData?.homepage?.topIcons == null)
        {
            return;
        }

        for (int i = 0; i < topRightIconTexts.Count && i < titleContentData.homepage.topIcons.Count; i++)
        {
            ZhongshanDeckIconEntry entry = titleContentData.homepage.topIcons[i];
            if (entry == null)
            {
                topRightIconTooltips.Add(string.Empty);
                continue;
            }

            if (topRightIconTexts[i] != null)
            {
                topRightIconTexts[i].text = entry.label;
            }

            topRightIconTooltips.Add(entry.tooltip ?? string.Empty);
        }
    }

    #endregion

    #region UI 构建

    private void BuildUI()
    {
        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvasRect = canvas.GetComponent<RectTransform>();

        CreateBackground();
        CreateVideoBackground();
        CreateHintText();
        CreateMenuOverlay();
        CreateFadeOverlay();    }

    private void CreateVideoBackground()
    {
        GameObject videoGO = CreateUIElement("VideoBackground", canvasRect);
        StretchFull(videoGO.GetComponent<RectTransform>());

        videoImage = videoGO.AddComponent<RawImage>();
        videoImage.color = new Color(1f, 1f, 1f, 0f);
        videoImage.raycastTarget = false;
    }

    private void CreateBackground()
    {
        GameObject bg = CreateUIElement("Background", canvasRect);
        StretchFull(bg.GetComponent<RectTransform>());

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = bgColor;
        bgImage.raycastTarget = false;
    }

    private void CreateHintText()
    {
        GameObject hintGO = CreateUIElement("HintText", canvasRect);
        RectTransform rt = hintGO.GetComponent<RectTransform>();
        hintTextRect = rt;
        rt.anchorMin = new Vector2(0.5f, 0.12f);
        rt.anchorMax = new Vector2(0.5f, 0.12f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800, 80);

        hintText = hintGO.AddComponent<TextMeshProUGUI>();
        hintText.text = hintMessage;
        hintText.fontSize = 36;
        hintText.color = new Color(textColor.r, textColor.g, textColor.b, 0.8f);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.raycastTarget = false;

        // 先应用中文字体（确保在材质操作之前完成字体设置）
        TMP_FontAsset chineseFont = GetAvailableChineseFont();
        if (chineseFont != null)
        {
            hintText.font = chineseFont;
        }

        // TMP 描边 + 阴影 — 使用实例化材质避免共享材质污染
        hintText.fontMaterial = new Material(hintText.fontMaterial);
        hintText.outlineWidth = 0.15f;
        hintText.outlineColor = new Color32(0, 0, 0, 160);
        hintText.fontMaterial.EnableKeyword("UNDERLAY_ON");
        hintText.fontMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.5f));
        hintText.fontMaterial.SetFloat("_UnderlayOffsetX", 0.6f);
        hintText.fontMaterial.SetFloat("_UnderlayOffsetY", -0.6f);
        hintText.fontMaterial.SetFloat("_UnderlayDilate", 0.1f);
        hintText.fontMaterial.SetFloat("_UnderlaySoftness", 0.08f);
    }

    private void CreateMenuOverlay()
    {
        GameObject overlayGO = CreateUIElement("MenuOverlay", canvasRect);
        StretchFull(overlayGO.GetComponent<RectTransform>());

        menuOverlay = overlayGO.AddComponent<CanvasGroup>();
        menuOverlay.alpha = 0f;
        menuOverlay.interactable = false;
        menuOverlay.blocksRaycasts = false;

        // 不加遮罩，保持视频背景完全可见
        CreateMainMenuPanel(overlayGO.transform as RectTransform);
        CreateChangelogEntryButton(overlayGO.transform as RectTransform);
        CreateTopRightIcons(overlayGO.transform as RectTransform);
        CreateLogoArea(overlayGO.transform as RectTransform);
        CreateVersionText(overlayGO.transform as RectTransform);
        CreateSettingsPanel(overlayGO.transform as RectTransform);
        CreateTutorialPanel(overlayGO.transform as RectTransform);
        CreateGalleryPanel(overlayGO.transform as RectTransform);
        CreateCreditsPanel(overlayGO.transform as RectTransform);
        CreateChangelogPanel(overlayGO.transform as RectTransform);
        ApplyAuthoredTitleContentToPanels();

        overlayGO.SetActive(false);
    }

    /// <summary>
    /// 中央 Logo 区域 — 上半部分放游戏标题图
    /// </summary>
    private void CreateLogoArea(RectTransform parent)
    {
        GameObject logoGO = CreateUIElement("LogoArea", parent);
        RectTransform rt = logoGO.GetComponent<RectTransform>();
        logoAreaRect = rt;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, 330f);
        rt.anchoredPosition = new Vector2(0f, 200f);

        // 尝试加载 Logo 图片，找不到就显示文字标题
        Sprite logoSprite = Resources.Load<Sprite>("GameLogo");
        if (logoSprite != null)
        {
            Image logoImg = logoGO.AddComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
            logoImg.color = Color.white;

            // Logo 图片阴影（适度）
            var logoShadow = logoGO.AddComponent<Shadow>();
            logoShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            logoShadow.effectDistance = new Vector2(2f, -2f);

            // 第二层阴影柔和光晕
            var logoShadow2 = logoGO.AddComponent<Shadow>();
            logoShadow2.effectColor = new Color(0f, 0f, 0f, 0.4f);
            logoShadow2.effectDistance = new Vector2(4f, -4f);
        }
        else
        {
            // 无图片时用文字标题代替
            TextMeshProUGUI titleTxt = logoGO.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "钟山下";
            titleTxt.fontSize = 96;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;

            // 添加阴影效果
            var shadow = logoGO.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }
    }

    private void CreateChangelogEntryButton(RectTransform parent)
    {
        GameObject buttonGO = CreateUIElement("ChangelogEntryButton", parent);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        changelogButtonRect = rect;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(300f, 74f);
        rect.anchoredPosition = new Vector2(44f, 46f);

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0.92f, 0.78f, 0.58f, 0.9f);

        Outline outline = buttonGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.36f, 0.18f, 0.08f, 0.65f);
        outline.effectDistance = new Vector2(2f, -2f);

        changelogButton = buttonGO.AddComponent<Button>();
        changelogButton.targetGraphic = bg;
        changelogButton.onClick.AddListener(ShowChangelogPanel);

        ColorBlock colors = changelogButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
        colors.pressedColor = new Color(0.9f, 0.78f, 0.58f, 1f);
        changelogButton.colors = colors;

        changelogButtonLabelText = CreateTMPBlock(rect, "Label", titleContentData?.homepage?.changelogButtonLabel ?? "更新日志", 30f, new Color(0.44f, 0.18f, 0.08f, 1f), TextAlignmentOptions.Center);
        StretchFull(changelogButtonLabelText.rectTransform);
        changelogButtonLabelText.fontStyle = FontStyles.Bold;
        changelogButtonLabelText.margin = new Vector4(36f, 0f, 0f, 0f);

        TextMeshProUGUI icon = CreateTMPBlock(rect, "Icon", "■", 34f, new Color(0.95f, 0.88f, 0.66f, 1f), TextAlignmentOptions.Center);
        icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        icon.rectTransform.pivot = new Vector2(0f, 0.5f);
        icon.rectTransform.sizeDelta = new Vector2(64f, 54f);
        icon.rectTransform.anchoredPosition = new Vector2(18f, 0f);
    }

    /// <summary>
    /// 右上角四个图标按钮：游戏教程 / 成就 / 游戏CG / 制作人详情
    /// </summary>
    private void CreateTopRightIcons(RectTransform parent)
    {
        List<ZhongshanDeckIconEntry> iconEntries = titleContentData?.homepage?.topIcons;
        if (iconEntries == null || iconEntries.Count == 0)
        {
            iconEntries = ZhongshanDeckTitleContentDefaults.CreateHomepageTopIcons();
        }

        float iconSize = 56f;
        float spacing = 12f;
        float totalWidth = iconEntries.Count * iconSize + (iconEntries.Count - 1) * spacing;
        float startX = -(totalWidth / 2f) + iconSize / 2f;

        GameObject iconsRoot = CreateUIElement("TopRightIcons", parent);
        RectTransform rootRT = iconsRoot.GetComponent<RectTransform>();
        topRightIconsRoot = rootRT;
        rootRT.anchorMin = new Vector2(1f, 1f);
        rootRT.anchorMax = new Vector2(1f, 1f);
        rootRT.pivot = new Vector2(1f, 1f);
        rootRT.sizeDelta = new Vector2(totalWidth + 32f, iconSize + 24f);
        rootRT.anchoredPosition = new Vector2(-24f, -20f);

        topRightIconTexts.Clear();
        topRightIconTooltips.Clear();

        for (int i = 0; i < iconEntries.Count; i++)
        {
            int idx = i;
            float xOffset = startX + i * (iconSize + spacing);
            ZhongshanDeckIconEntry iconEntry = iconEntries[i];
            string buttonName = string.IsNullOrWhiteSpace(iconEntry?.label) ? $"Icon{idx}" : iconEntry.label;

            GameObject iconGO = CreateUIElement(buttonName + "Btn", rootRT);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(iconSize, iconSize);
            iconRT.anchoredPosition = new Vector2(xOffset, 0f);

            // 圆形背景
            Image bg = iconGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.15f, 0.72f);

            Button btn = iconGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            // 图标文字（占位，后续可替换为 Sprite）
            GameObject labelGO = CreateUIElement("Label", iconRT);
            StretchFull(labelGO.GetComponent<RectTransform>());
            TextMeshProUGUI txt = labelGO.AddComponent<TextMeshProUGUI>();
            txt.text = iconEntry?.label ?? string.Empty;
            txt.fontSize = 16f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.9f, 0.9f, 0.95f, 0.9f);
            txt.raycastTarget = false;

            // TMP 图标文字描边+阴影 — 实例化材质避免共享污染
            txt.fontMaterial = new Material(txt.fontMaterial);
            txt.outlineWidth = 0.15f;
            txt.outlineColor = new Color32(0, 0, 0, 160);
            txt.fontMaterial.EnableKeyword("UNDERLAY_ON");
            txt.fontMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.5f));
            txt.fontMaterial.SetFloat("_UnderlayOffsetX", 0.6f);
            txt.fontMaterial.SetFloat("_UnderlayOffsetY", -0.6f);
            txt.fontMaterial.SetFloat("_UnderlayDilate", 0.1f);
            txt.fontMaterial.SetFloat("_UnderlaySoftness", 0.08f);

            int captured = idx;
            topRightIconTexts.Add(txt);
            topRightIconTooltips.Add(iconEntry?.tooltip ?? string.Empty);

            btn.onClick.AddListener(() => OnTopIconClicked(captured));

            // 保存引用
            if (idx == 0) tutorialButton = btn;
            else if (idx == 1) achievementButton = btn;
            else if (idx == 2) cgButton = btn;
            else creditsButton = btn;
        }
    }

    private void OnTopIconClicked(int index)
    {
        if (index == 0)
        {
            ShowTutorialPanel();
            return;
        }

        switch (index)
        {
            case 1: // 成就回顾
                if (AchievementSystem.Instance == null)
                {
                    GameObject systemObject = new GameObject("AchievementSystem");
                    systemObject.AddComponent<AchievementSystem>();
                }

                if (AchievementUI.Instance == null)
                {
                    GameObject obj = new GameObject("AchievementUI");
                    obj.AddComponent<AchievementUI>();
                }
                AchievementUI.Instance.ShowReviewPanel();
                return;
            case 2: // 游戏CG / 结局
                ShowGalleryPanel();
                return;
            case 3: // 制作人详情
                ShowCreditsPanel();
                return;
            default:
                ShowTutorialPanel();
                break;
        }
    }

    /// <summary>
    /// 右下角版本号
    /// </summary>
    private void CreateVersionText(RectTransform parent)
    {
        GameObject vGO = CreateUIElement("VersionText", parent);
        RectTransform rt = vGO.GetComponent<RectTransform>();
        versionTextRect = rt;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(200f, 36f);
        rt.anchoredPosition = new Vector2(-16f, 12f);

        versionText = vGO.AddComponent<TextMeshProUGUI>();
        versionText.text = "v" + Application.version;
        versionText.fontSize = 18f;
        versionText.alignment = TextAlignmentOptions.Right;
        versionText.color = new Color(1f, 1f, 1f, 0.45f);
        versionText.raycastTarget = false;

        var vShadow = vGO.AddComponent<UnityEngine.UI.Shadow>();
        vShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        vShadow.effectDistance = new Vector2(1f, -1f);
    }

    private void CreateMainMenuPanel(RectTransform parent)
    {
        // 无背景面板，直接在屏幕上放按钮
        mainMenuPanel = CreateUIElement("MainMenuPanel", parent);
        RectTransform panelRect = mainMenuPanel.GetComponent<RectTransform>();
        mainMenuPanelRect = panelRect;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 480f);
        panelRect.anchoredPosition = new Vector2(0f, -100f);

        // 按钮列表：文字、回调
        List<(string actionId, string label, UnityEngine.Events.UnityAction action)> menuItems = BuildHomepageMenuItems();

        float btnHeight = 62f;
        float gap = 10f;
        float totalH = menuItems.Count * btnHeight + (menuItems.Count - 1) * gap;
        float startY = totalH / 2f - btnHeight / 2f;

        for (int i = 0; i < menuItems.Count; i++)
        {
            var item = menuItems[i];
            float y = startY - i * (btnHeight + gap);
            bool isPrimary = (i == 0 || i == 1); // 继续/开始 高亮
            Button createdButton = CreateTextMenuButton(panelRect, item.label, new Vector2(0f, y), item.action, isPrimary);
            mainMenuButtonsByAction[item.actionId] = createdButton;

            switch (item.actionId)
            {
                case "continue":
                    continueGameButton = createdButton;
                    break;
                case "start":
                    startGameButton = createdButton;
                    break;
                case "settings":
                    settingsButton = createdButton;
                    break;
                case "quit":
                    quitGameButton = createdButton;
                    break;
            }
        }

        RefreshContinueButtonState();
    }

    private List<(string actionId, string label, UnityEngine.Events.UnityAction action)> BuildHomepageMenuItems()
    {
        List<(string actionId, string label, UnityEngine.Events.UnityAction action)> items = new List<(string actionId, string label, UnityEngine.Events.UnityAction action)>();
        List<ZhongshanDeckMenuActionLabel> authored = titleContentData?.homepage?.mainMenuItems;

        if (authored == null || authored.Count == 0)
        {
            authored = ZhongshanDeckTitleContentDefaults.CreateHomepageMenuItems();
        }

        for (int i = 0; i < authored.Count; i++)
        {
            ZhongshanDeckMenuActionLabel item = authored[i];
            if (item == null)
            {
                continue;
            }

            UnityEngine.Events.UnityAction action = item.actionId switch
            {
                "continue" => ContinueGame,
                "start" => StartGame,
                "load" => OnLoadGame,
                "settings" => OpenSettings,
                "quit" => QuitGame,
                _ => null
            };

            if (action != null)
            {
                items.Add((item.actionId, item.label, action));
            }
        }

        return items;
    }

    /// <summary>
    /// 纯文字风格菜单按钮 — 参考截图：无色块背景，文字+底部细线
    /// </summary>
    private Button CreateTextMenuButton(RectTransform parent, string label,
                                         Vector2 pos, UnityEngine.Events.UnityAction onClick,
                                         bool highlight = false)
    {
        GameObject btnGO = CreateUIElement(label + "Btn", parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(380f, 60f);
        rt.anchoredPosition = pos;

        // 透明可交互背景（接收射线）
        Image bg = btnGO.AddComponent<Image>();
        bg.color = Color.clear;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor    = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        cb.pressedColor   = new Color(1f, 1f, 1f, 0.06f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        // 文字
        GameObject textGO = CreateUIElement("Label", rt);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = highlight ? 38f : 32f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = highlight
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(1f, 1f, 1f, 0.9f);
        txt.fontStyle = highlight ? FontStyles.Bold : FontStyles.Normal;
        txt.raycastTarget = false;

        // 额外 UI 黑色阴影，增强浅色背景上的可读性
        Shadow textShadow = textGO.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0f, 0f, 0f, highlight ? 0.95f : 0.85f);
        textShadow.effectDistance = highlight ? new Vector2(3.5f, -3.5f) : new Vector2(3f, -3f);
        textShadow.useGraphicAlpha = true;

        // TMP 描边（Outline）— 通过 materialForRendering 设置
        txt.outlineWidth = highlight ? 0.25f : 0.22f;
        txt.outlineColor = new Color32(0, 0, 0, 210);

        // TMP 字体材质阴影（Underlay）— 实例化材质避免共享污染
        txt.fontMaterial = new Material(txt.fontMaterial);
        txt.fontMaterial.EnableKeyword("UNDERLAY_ON");
        txt.fontMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, highlight ? 0.75f : 0.65f));
        txt.fontMaterial.SetFloat("_UnderlayOffsetX", highlight ? 1.1f : 0.9f);
        txt.fontMaterial.SetFloat("_UnderlayOffsetY", highlight ? -1.1f : -0.9f);
        txt.fontMaterial.SetFloat("_UnderlayDilate", 0.22f);
        txt.fontMaterial.SetFloat("_UnderlaySoftness", 0.12f);

        // 底部细分割线
        GameObject lineGO = CreateUIElement("Line", rt);
        RectTransform lineRT = lineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.15f, 0f);
        lineRT.anchorMax = new Vector2(0.85f, 0f);
        lineRT.pivot = new Vector2(0.5f, 0f);
        lineRT.sizeDelta = new Vector2(0f, 1f);
        lineRT.anchoredPosition = Vector2.zero;
        Image lineImg = lineGO.AddComponent<Image>();
        lineImg.color = highlight
            ? new Color(1f, 1f, 1f, 0.35f)
            : new Color(1f, 1f, 1f, 0.15f);
        lineImg.raycastTarget = false;

        return btn;
    }

    private void RefreshContinueButtonState()
    {
        EnsureSaveManager();

        if (continueGameButton == null)
        {
            return;
        }

        bool hasAnySave = HasAnySaveData();
        continueGameButton.interactable = hasAnySave || continueGameStartsGame;

        TextMeshProUGUI label = continueGameButton.GetComponentInChildren<TextMeshProUGUI>(true);
        Image lineImage = null;
        if (continueGameButton.transform.childCount > 1)
        {
            Transform lineTransform = continueGameButton.transform.GetChild(1);
            if (lineTransform != null)
            {
                lineImage = lineTransform.GetComponent<Image>();
            }
        }

        if (label != null)
        {
            label.color = continueGameButton.interactable
                ? Color.white
                : new Color(1f, 1f, 1f, 0.38f);
        }

        if (lineImage != null)
        {
            lineImage.color = continueGameButton.interactable
                ? new Color(1f, 1f, 1f, 0.35f)
                : new Color(1f, 1f, 1f, 0.08f);
        }
    }

    private void OnLoadGame()
    {
        EnsureSaveManager();

        if (SaveManager.Instance == null)
        {
            ShowTitleNotification("读档入口不可用", "存档系统还没有准备好，现在暂时无法打开读档界面。");
            return;
        }

        SaveLoadUI.Show(false);
    }

    private void CreateSettingsPanel(RectTransform parent)
    {
        settingsPanel = CreatePanel("SettingsPanel", parent, new Vector2(0.5f, 0.5f), new Vector2(700f, 620f), subPanelColor);
        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = new Vector2(0f, -10f);

        settingsTitleText = CreatePanelTitle(settingsPanel.transform as RectTransform, titleContentData?.homepage?.settingsPanelTitle ?? "设置", 42, new Vector2(0f, 235f));

        musicVolumeSlider = CreateLabeledSlider(settingsPanel.transform as RectTransform, "音乐音量", new Vector2(0f, 110f));
        sfxVolumeSlider = CreateLabeledSlider(settingsPanel.transform as RectTransform, "音效音量", new Vector2(0f, 5f));
        fullscreenToggle = CreateLabeledToggle(settingsPanel.transform as RectTransform, "全屏显示", new Vector2(0f, -105f));

        backButton = CreateMenuButton(settingsPanel.transform as RectTransform, "BackButton", titleContentData?.homepage?.settingsBackButtonLabel ?? "返回", new Vector2(0f, -225f), primaryColor, BackToMainMenu);
        settingsBackButtonLabelText = backButton != null ? backButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        settingsPanel.SetActive(false);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(delegate { OnMusicVolumeChanged(); });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(delegate { OnSfxVolumeChanged(); });
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(delegate { OnFullscreenChanged(); });
        }

        LoadSettings();
    }

    private void CreateChangelogPanel(RectTransform parent)
    {
        changelogPanel = CreateUIElement("ChangelogPanel", parent);
        changelogPanel.transform.SetAsLastSibling();
        StretchFull(changelogPanel.GetComponent<RectTransform>());
        changelogPanel.SetActive(false);

        Image overlay = changelogPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.52f);

        changelogPanelGroup = changelogPanel.AddComponent<CanvasGroup>();
        changelogPanelGroup.alpha = 0f;
        changelogPanelGroup.interactable = false;
        changelogPanelGroup.blocksRaycasts = false;

        RectTransform frame = CreateUIElement("ChangelogFrame", changelogPanel.transform as RectTransform).GetComponent<RectTransform>();
        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(970f, 710f);
        frame.anchoredPosition = new Vector2(0f, -8f);

        Image frameBg = frame.gameObject.AddComponent<Image>();
        frameBg.color = new Color(0.98f, 0.95f, 0.88f, 1f);
        Outline frameOutline = frame.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.27f, 0.18f, 0.1f, 0.38f);
        frameOutline.effectDistance = new Vector2(6f, -6f);

        RectTransform header = CreateUIElement("Header", frame).GetComponent<RectTransform>();
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, 74f);
        header.anchoredPosition = Vector2.zero;
        header.gameObject.AddComponent<Image>().color = new Color(1f, 0.92f, 0.65f, 1f);
        CreateTutorialGridBackground(header);

        changelogPanelTitleText = CreateTMPBlock(header, "Title", titleContentData?.changelog?.panelTitle ?? "更新日志", 32f, TutorialAccentColor, TextAlignmentOptions.Center);
        StretchFull(changelogPanelTitleText.rectTransform);
        changelogPanelTitleText.fontStyle = FontStyles.Bold;

        RectTransform scrollRoot = CreateUIElement("ChangelogScroll", frame).GetComponent<RectTransform>();
        scrollRoot.anchorMin = new Vector2(0f, 0f);
        scrollRoot.anchorMax = new Vector2(1f, 1f);
        scrollRoot.offsetMin = new Vector2(54f, 92f);
        scrollRoot.offsetMax = new Vector2(-54f, -92f);

        ScrollRect scroll = CreateTutorialScrollView(scrollRoot, out changelogContent);
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        VerticalLayoutGroup layout = changelogContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 24, 10, 20);
        ContentSizeFitter fitter = changelogContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RebuildChangelogContent();

        Button closeButton = CreateMenuButton(frame, "ChangelogCloseButton", "关闭", new Vector2(0f, -320f), new Color(0.98f, 0.78f, 0.38f, 1f), HideChangelogPanel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.sizeDelta = new Vector2(170f, 52f);
        TextMeshProUGUI closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (closeLabel != null)
        {
            closeLabel.fontSize = 27f;
            closeLabel.color = TutorialAccentColor;
            closeLabel.fontStyle = FontStyles.Bold;
        }
    }

    private void AddChangelogLine(string text, float fontSize, Color color, FontStyles style, float preferredHeight)
    {
        TextMeshProUGUI line = CreateTMPLayoutItem(changelogContent, "ChangelogLine", text, fontSize, color, style, preferredHeight);
        line.enableWordWrapping = true;
        line.lineSpacing = 8f;
    }

    private void RebuildChangelogContent()
    {
        if (changelogContent == null)
        {
            return;
        }

        for (int i = changelogContent.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeOrEditor(changelogContent.GetChild(i).gameObject);
        }

        if (changelogSections.Count == 0)
        {
            AddChangelogLine("暂时还没有可显示的更新日志。", 25f, TutorialMutedTextColor, FontStyles.Italic, 54f);
            return;
        }

        for (int sectionIndex = 0; sectionIndex < changelogSections.Count; sectionIndex++)
        {
            ZhongshanDeckChangelogSection section = changelogSections[sectionIndex];
            if (section == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(section.heading))
            {
                AddChangelogLine(section.heading, 31f, new Color(0.1f, 0.45f, 0.9f, 1f), FontStyles.Bold, 46f);
            }

            if (section.bulletLines != null)
            {
                for (int lineIndex = 0; lineIndex < section.bulletLines.Count; lineIndex++)
                {
                    string line = section.bulletLines[lineIndex];
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        AddChangelogLine(line, 26f, TutorialTextColor, FontStyles.Bold, 42f);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(section.note))
            {
                AddChangelogLine("\n" + section.note, 23f, TutorialMutedTextColor, FontStyles.Normal, 78f);
            }
        }
    }

    private void CreateCreditsPanel(RectTransform parent)
    {
        creditsPanel = CreateUIElement("CreditsPanel", parent);
        StretchFull(creditsPanel.GetComponent<RectTransform>());
        creditsPanel.SetActive(false);

        Image overlay = creditsPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.42f);

        creditsPanelGroup = creditsPanel.AddComponent<CanvasGroup>();
        creditsPanelGroup.alpha = 0f;
        creditsPanelGroup.interactable = false;
        creditsPanelGroup.blocksRaycasts = false;

        RectTransform frameRect = CreateUIElement("CreditsFrame", creditsPanel.transform as RectTransform).GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.08f, 0.08f);
        frameRect.anchorMax = new Vector2(0.92f, 0.9f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        Image frameImage = frameRect.gameObject.AddComponent<Image>();
        frameImage.color = TutorialFrameColor;
        Outline frameOutline = frameRect.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.58f, 0.38f, 0.18f, 0.48f);
        frameOutline.effectDistance = new Vector2(6f, -6f);

        RectTransform paperRect = CreateUIElement("CreditsPaper", frameRect).GetComponent<RectTransform>();
        paperRect.anchorMin = new Vector2(0.02f, 0.03f);
        paperRect.anchorMax = new Vector2(0.98f, 0.9f);
        paperRect.offsetMin = Vector2.zero;
        paperRect.offsetMax = Vector2.zero;
        paperRect.gameObject.AddComponent<Image>().color = TutorialPaperColor;
        CreateTutorialGridBackground(paperRect);

        RectTransform tab = CreateUIElement("CreditsTab", frameRect).GetComponent<RectTransform>();
        tab.anchorMin = new Vector2(0.42f, 0.9f);
        tab.anchorMax = new Vector2(0.58f, 1f);
        tab.offsetMin = Vector2.zero;
        tab.offsetMax = Vector2.zero;
        tab.gameObject.AddComponent<Image>().color = TutorialHighlightColor;
        creditsTabText = CreateTMPBlock(tab, "Label", titleContentData?.credits?.tabLabel ?? "STAFF", 28f, TutorialAccentColor, TextAlignmentOptions.Center);
        StretchFull(creditsTabText.rectTransform);
        creditsTabText.fontStyle = FontStyles.Bold;

        CreateTutorialCloseButton(frameRect, HideCreditsPanel);

        creditsPanelTitleText = CreateTMPBlock(frameRect, "CreditsTitle", titleContentData?.credits?.panelTitle ?? "制作人", 48f, TutorialAccentColor, TextAlignmentOptions.Left);
        creditsPanelTitleText.rectTransform.anchorMin = new Vector2(0.04f, 0.91f);
        creditsPanelTitleText.rectTransform.anchorMax = new Vector2(0.24f, 1f);
        creditsPanelTitleText.rectTransform.offsetMin = Vector2.zero;
        creditsPanelTitleText.rectTransform.offsetMax = Vector2.zero;
        creditsPanelTitleText.fontStyle = FontStyles.Bold;

        RectTransform contentRoot = CreateUIElement("CreditsContent", paperRect).GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.035f, 0.055f);
        contentRoot.anchorMax = new Vector2(0.965f, 0.945f);
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;

        RectTransform listPane = CreateUIElement("CreditsListPane", contentRoot).GetComponent<RectTransform>();
        listPane.anchorMin = new Vector2(0f, 0f);
        listPane.anchorMax = new Vector2(0.31f, 1f);
        listPane.offsetMin = new Vector2(8f, 8f);
        listPane.offsetMax = new Vector2(-18f, -8f);
        listPane.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.93f, 0.82f, 0.9f);

        TextMeshProUGUI listTitle = CreateTMPBlock(listPane, "ListTitle", "目录", 30f, TutorialAccentColor, TextAlignmentOptions.Center);
        listTitle.rectTransform.anchorMin = new Vector2(0f, 0.9f);
        listTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        listTitle.rectTransform.offsetMin = Vector2.zero;
        listTitle.rectTransform.offsetMax = Vector2.zero;
        listTitle.fontStyle = FontStyles.Bold;

        RectTransform listScrollRoot = CreateUIElement("CreditsListScroll", listPane).GetComponent<RectTransform>();
        listScrollRoot.anchorMin = new Vector2(0.05f, 0.06f);
        listScrollRoot.anchorMax = new Vector2(0.95f, 0.88f);
        listScrollRoot.offsetMin = Vector2.zero;
        listScrollRoot.offsetMax = Vector2.zero;
        ScrollRect listScroll = CreateTutorialScrollView(listScrollRoot, out creditsListContent);
        listScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        VerticalLayoutGroup listLayout = creditsListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 12f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter listFitter = creditsListContent.gameObject.AddComponent<ContentSizeFitter>();
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform detailPane = CreateUIElement("CreditsDetailPane", contentRoot).GetComponent<RectTransform>();
        detailPane.anchorMin = new Vector2(0.33f, 0f);
        detailPane.anchorMax = new Vector2(1f, 1f);
        detailPane.offsetMin = new Vector2(18f, 8f);
        detailPane.offsetMax = new Vector2(-8f, -8f);
        detailPane.gameObject.AddComponent<Image>().color = new Color(0.995f, 0.97f, 0.9f, 0.96f);

        RectTransform stamp = CreateUIElement("CreditsStamp", detailPane).GetComponent<RectTransform>();
        stamp.anchorMin = new Vector2(1f, 1f);
        stamp.anchorMax = new Vector2(1f, 1f);
        stamp.pivot = new Vector2(0.5f, 0.5f);
        stamp.sizeDelta = new Vector2(140f, 140f);
        stamp.anchoredPosition = new Vector2(-96f, -90f);
        Image stampImage = stamp.gameObject.AddComponent<Image>();
        stampImage.color = new Color(0.88f, 0.32f, 0.2f, 0.16f);
        stampImage.raycastTarget = false;
        TextMeshProUGUI stampText = CreateTMPBlock(stamp, "StampText", "钟山下", 31f, new Color(0.7f, 0.2f, 0.12f, 0.58f), TextAlignmentOptions.Center);
        StretchFull(stampText.rectTransform);
        stampText.fontStyle = FontStyles.Bold;

        creditsDetailTitle = CreateTMPBlock(detailPane, "DetailTitle", string.Empty, 42f, TutorialTextColor, TextAlignmentOptions.Left);
        creditsDetailTitle.rectTransform.anchorMin = new Vector2(0.07f, 0.78f);
        creditsDetailTitle.rectTransform.anchorMax = new Vector2(0.72f, 0.9f);
        creditsDetailTitle.rectTransform.offsetMin = Vector2.zero;
        creditsDetailTitle.rectTransform.offsetMax = Vector2.zero;
        creditsDetailTitle.fontStyle = FontStyles.Bold;

        creditsDetailRole = CreateTMPBlock(detailPane, "DetailRole", string.Empty, 26f, TutorialAccentColor, TextAlignmentOptions.Left);
        creditsDetailRole.rectTransform.anchorMin = new Vector2(0.07f, 0.68f);
        creditsDetailRole.rectTransform.anchorMax = new Vector2(0.78f, 0.77f);
        creditsDetailRole.rectTransform.offsetMin = Vector2.zero;
        creditsDetailRole.rectTransform.offsetMax = Vector2.zero;

        RectTransform divider = CreateUIElement("CreditsDivider", detailPane).GetComponent<RectTransform>();
        divider.anchorMin = new Vector2(0.07f, 0.65f);
        divider.anchorMax = new Vector2(0.88f, 0.65f);
        divider.sizeDelta = new Vector2(0f, 2f);
        divider.anchoredPosition = Vector2.zero;
        divider.gameObject.AddComponent<Image>().color = new Color(0.72f, 0.52f, 0.3f, 0.36f);

        creditsDetailDescription = CreateTMPBlock(detailPane, "DetailDescription", string.Empty, 25f, TutorialMutedTextColor, TextAlignmentOptions.TopLeft);
        creditsDetailDescription.rectTransform.anchorMin = new Vector2(0.07f, 0.34f);
        creditsDetailDescription.rectTransform.anchorMax = new Vector2(0.9f, 0.62f);
        creditsDetailDescription.rectTransform.offsetMin = Vector2.zero;
        creditsDetailDescription.rectTransform.offsetMax = Vector2.zero;
        creditsDetailDescription.enableWordWrapping = true;
        creditsDetailDescription.lineSpacing = 8f;

        creditsTagsRoot = CreateUIElement("CreditsTags", detailPane).GetComponent<RectTransform>();
        creditsTagsRoot.anchorMin = new Vector2(0.07f, 0.2f);
        creditsTagsRoot.anchorMax = new Vector2(0.9f, 0.31f);
        creditsTagsRoot.offsetMin = Vector2.zero;
        creditsTagsRoot.offsetMax = Vector2.zero;
        HorizontalLayoutGroup tagLayout = creditsTagsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        tagLayout.spacing = 12f;
        tagLayout.childAlignment = TextAnchor.MiddleLeft;
        tagLayout.childControlWidth = false;
        tagLayout.childControlHeight = true;
        tagLayout.childForceExpandWidth = false;
        tagLayout.childForceExpandHeight = false;

        creditsFooterText = CreateTMPBlock(detailPane, "CreditsFooter", titleContentData?.credits?.footerText ?? string.Empty, 22f, TutorialMutedTextColor, TextAlignmentOptions.Center);
        creditsFooterText.rectTransform.anchorMin = new Vector2(0.06f, 0.06f);
        creditsFooterText.rectTransform.anchorMax = new Vector2(0.94f, 0.15f);
        creditsFooterText.rectTransform.offsetMin = Vector2.zero;
        creditsFooterText.rectTransform.offsetMax = Vector2.zero;
        creditsFooterText.fontStyle = FontStyles.Italic;

        RebuildCreditsList();
        if (creditsEntries.Count > 0)
        {
            SelectCreditsEntry(0);
        }
    }

    private void ShowCreditsPanel()
    {
        if (creditsPanel == null)
        {
            return;
        }

        DebugReloadAuthoredTitleContent();
        creditsPanel.SetActive(true);
        creditsPanel.transform.SetAsLastSibling();
        creditsPanelGroup.alpha = 1f;
        creditsPanelGroup.interactable = true;
        creditsPanelGroup.blocksRaycasts = true;
        if (creditsEntries.Count > 0)
        {
            SelectCreditsEntry(currentCreditsEntryIndex);
        }
    }

    private void HideCreditsPanel()
    {
        if (creditsPanel == null)
        {
            return;
        }

        creditsPanelGroup.alpha = 0f;
        creditsPanelGroup.interactable = false;
        creditsPanelGroup.blocksRaycasts = false;
        creditsPanel.SetActive(false);
    }

    private void RebuildCreditsList()
    {
        creditsEntryButtons.Clear();
        for (int i = creditsListContent.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeOrEditor(creditsListContent.GetChild(i).gameObject);
        }

        if (creditsEntries.Count == 0)
        {
            if (creditsDetailTitle != null) creditsDetailTitle.text = "暂无条目";
            if (creditsDetailRole != null) creditsDetailRole.text = string.Empty;
            if (creditsDetailDescription != null) creditsDetailDescription.text = "钟山台内容资产里还没有制作组条目。";
            RefreshCreditsTags(null);
            return;
        }

        for (int i = 0; i < creditsEntries.Count; i++)
        {
            int captured = i;
            creditsEntryButtons.Add(CreateTutorialEntryButton(creditsListContent, creditsEntries[i].title, () => SelectCreditsEntry(captured)));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(creditsListContent);
    }

    private void SelectCreditsEntry(int index)
    {
        if (creditsEntries.Count == 0)
        {
            return;
        }

        currentCreditsEntryIndex = Mathf.Clamp(index, 0, creditsEntries.Count - 1);
        ZhongshanDeckCreditsEntry entry = creditsEntries[currentCreditsEntryIndex];

        for (int i = 0; i < creditsEntryButtons.Count; i++)
        {
            UpdateTutorialEntryButtonState(creditsEntryButtons[i], i == currentCreditsEntryIndex);
        }

        creditsDetailTitle.text = entry.title;
        creditsDetailRole.text = entry.role;
        creditsDetailDescription.text = entry.description;
        RefreshCreditsTags(entry.tags);
    }

    private void RefreshCreditsTags(List<string> tags)
    {
        for (int i = creditsTagsRoot.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeOrEditor(creditsTagsRoot.GetChild(i).gameObject);
        }

        if (tags == null)
        {
            return;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            CreateTutorialTag(creditsTagsRoot, tags[i]);
        }
    }

    private void ShowChangelogPanel()
    {
        if (changelogPanel == null)
        {
            return;
        }

        DebugReloadAuthoredTitleContent();
        changelogPanel.SetActive(true);
        changelogPanel.transform.SetAsLastSibling();
        changelogPanelGroup.alpha = 1f;
        changelogPanelGroup.interactable = true;
        changelogPanelGroup.blocksRaycasts = true;
    }

    private void HideChangelogPanel()
    {
        if (changelogPanel == null)
        {
            return;
        }

        changelogPanelGroup.alpha = 0f;
        changelogPanelGroup.interactable = false;
        changelogPanelGroup.blocksRaycasts = false;
        changelogPanel.SetActive(false);
    }

    private void CreateGalleryPanel(RectTransform parent)
    {
        LoadGalleryCategories();

        galleryPanel = CreateUIElement("GalleryPanel", parent);
        StretchFull(galleryPanel.GetComponent<RectTransform>());
        galleryPanel.SetActive(false);

        Image overlay = galleryPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.42f);

        galleryPanelGroup = galleryPanel.AddComponent<CanvasGroup>();
        galleryPanelGroup.alpha = 0f;
        galleryPanelGroup.interactable = false;
        galleryPanelGroup.blocksRaycasts = false;

        RectTransform frameRect = CreateUIElement("GalleryFrame", galleryPanel.transform as RectTransform).GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.06f, 0.06f);
        frameRect.anchorMax = new Vector2(0.94f, 0.92f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        Image frameImage = frameRect.gameObject.AddComponent<Image>();
        frameImage.color = TutorialFrameColor;
        Outline frameOutline = frameRect.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.58f, 0.38f, 0.18f, 0.48f);
        frameOutline.effectDistance = new Vector2(6f, -6f);

        RectTransform paperRect = CreateUIElement("GalleryPaper", frameRect).GetComponent<RectTransform>();
        paperRect.anchorMin = new Vector2(0.018f, 0.025f);
        paperRect.anchorMax = new Vector2(0.982f, 0.91f);
        paperRect.offsetMin = Vector2.zero;
        paperRect.offsetMax = Vector2.zero;
        Image paperImage = paperRect.gameObject.AddComponent<Image>();
        paperImage.color = TutorialPaperColor;
        CreateTutorialGridBackground(paperRect);

        RectTransform tabsRoot = CreateUIElement("GalleryTabs", frameRect).GetComponent<RectTransform>();
        tabsRoot.anchorMin = new Vector2(0.24f, 0.91f);
        tabsRoot.anchorMax = new Vector2(0.76f, 1f);
        tabsRoot.offsetMin = Vector2.zero;
        tabsRoot.offsetMax = Vector2.zero;
        HorizontalLayoutGroup tabsLayout = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 20f;
        tabsLayout.childAlignment = TextAnchor.LowerCenter;
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childForceExpandHeight = true;

        galleryTabButtons.Clear();
        for (int i = 0; i < galleryCategories.Count; i++)
        {
            int captured = i;
            galleryTabButtons.Add(CreateTutorialCategoryButton(tabsRoot, galleryCategories[i].Name, () => SelectGalleryCategory(captured)));
        }

        CreateTutorialCloseButton(frameRect, HideGalleryPanel);

        TextMeshProUGUI title = CreateTMPBlock(frameRect, "GalleryTitle", "游戏CG/结局", 46f, TutorialAccentColor, TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0.04f, 0.91f);
        title.rectTransform.anchorMax = new Vector2(0.24f, 1f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;
        title.fontStyle = FontStyles.Bold;

        RectTransform contentRoot = CreateUIElement("GalleryContent", paperRect).GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.025f, 0.04f);
        contentRoot.anchorMax = new Vector2(0.975f, 0.96f);
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;

        RectTransform gridPane = CreateUIElement("GridPane", contentRoot).GetComponent<RectTransform>();
        gridPane.anchorMin = new Vector2(0f, 0.08f);
        gridPane.anchorMax = new Vector2(0.68f, 1f);
        gridPane.offsetMin = new Vector2(10f, 10f);
        gridPane.offsetMax = new Vector2(-22f, -12f);

        galleryGridContent = CreateUIElement("GalleryContent", gridPane).GetComponent<RectTransform>();
        galleryGridContent.anchorMin = new Vector2(0f, 1f);
        galleryGridContent.anchorMax = new Vector2(0f, 1f);
        galleryGridContent.pivot = new Vector2(0f, 1f);
        galleryGridContent.anchoredPosition = Vector2.zero;

        galleryPrevPageButton = CreateMenuButton(gridPane, "GalleryPrevPageButton", "<", Vector2.zero, new Color(0.9f, 0.84f, 0.7f, 0.95f), () => ChangeGalleryPage(-1));
        RectTransform prevRect = galleryPrevPageButton.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0f, 0f);
        prevRect.anchorMax = new Vector2(0f, 0f);
        prevRect.sizeDelta = new Vector2(52f, 42f);
        prevRect.anchoredPosition = new Vector2(34f, 26f);

        galleryNextPageButton = CreateMenuButton(gridPane, "GalleryNextPageButton", ">", Vector2.zero, new Color(0.9f, 0.84f, 0.7f, 0.95f), () => ChangeGalleryPage(1));
        RectTransform nextRect = galleryNextPageButton.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0f, 0f);
        nextRect.anchorMax = new Vector2(0f, 0f);
        nextRect.sizeDelta = new Vector2(52f, 42f);
        nextRect.anchoredPosition = new Vector2(172f, 26f);

        galleryPageText = CreateTMPBlock(gridPane, "GalleryPageText", string.Empty, 18f, TutorialMutedTextColor, TextAlignmentOptions.Center);
        galleryPageText.rectTransform.anchorMin = new Vector2(0f, 0f);
        galleryPageText.rectTransform.anchorMax = new Vector2(0f, 0f);
        galleryPageText.rectTransform.sizeDelta = new Vector2(96f, 32f);
        galleryPageText.rectTransform.anchoredPosition = new Vector2(104f, 26f);

        galleryGridStatusText = CreateTMPBlock(gridPane, "GalleryGridStatus", string.Empty, 18f, TutorialMutedTextColor, TextAlignmentOptions.Left);
        galleryGridStatusText.rectTransform.anchorMin = new Vector2(0f, 0f);
        galleryGridStatusText.rectTransform.anchorMax = new Vector2(1f, 0f);
        galleryGridStatusText.rectTransform.offsetMin = new Vector2(240f, 10f);
        galleryGridStatusText.rectTransform.offsetMax = new Vector2(-16f, 40f);

        RectTransform previewPane = CreateUIElement("PreviewPane", contentRoot).GetComponent<RectTransform>();
        previewPane.anchorMin = new Vector2(0.69f, 0.08f);
        previewPane.anchorMax = new Vector2(1f, 1f);
        previewPane.offsetMin = new Vector2(18f, 10f);
        previewPane.offsetMax = new Vector2(-12f, -12f);
        previewPane.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.93f, 0.82f, 0.92f);

        gallerySectionTitle = CreateTMPBlock(previewPane, "PreviewHeader", "游戏CG/结局", 31f, TutorialAccentColor, TextAlignmentOptions.Center);
        gallerySectionTitle.rectTransform.anchorMin = new Vector2(0f, 0.91f);
        gallerySectionTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        gallerySectionTitle.rectTransform.offsetMin = Vector2.zero;
        gallerySectionTitle.rectTransform.offsetMax = Vector2.zero;
        gallerySectionTitle.fontStyle = FontStyles.Bold;

        galleryGenderMaleButton = CreateMenuButton(previewPane, "GalleryGenderMaleButton", "男", Vector2.zero, new Color(0.85f, 0.9f, 0.98f, 1f), () => SetGalleryRequirementGender(0));
        RectTransform maleRect = galleryGenderMaleButton.GetComponent<RectTransform>();
        maleRect.anchorMin = new Vector2(0.16f, 0.84f);
        maleRect.anchorMax = new Vector2(0.34f, 0.9f);
        maleRect.offsetMin = Vector2.zero;
        maleRect.offsetMax = Vector2.zero;

        galleryGenderFemaleButton = CreateMenuButton(previewPane, "GalleryGenderFemaleButton", "女", Vector2.zero, new Color(0.98f, 0.84f, 0.9f, 1f), () => SetGalleryRequirementGender(1));
        RectTransform femaleRect = galleryGenderFemaleButton.GetComponent<RectTransform>();
        femaleRect.anchorMin = new Vector2(0.38f, 0.84f);
        femaleRect.anchorMax = new Vector2(0.56f, 0.9f);
        femaleRect.offsetMin = Vector2.zero;
        femaleRect.offsetMax = Vector2.zero;

        RectTransform imageFrame = CreateUIElement("PreviewImageFrame", previewPane).GetComponent<RectTransform>();
        imageFrame.anchorMin = new Vector2(0.06f, 0.48f);
        imageFrame.anchorMax = new Vector2(0.94f, 0.82f);
        imageFrame.offsetMin = Vector2.zero;
        imageFrame.offsetMax = Vector2.zero;
        Image previewBg = imageFrame.gameObject.AddComponent<Image>();
        previewBg.color = new Color(0.36f, 0.37f, 0.34f, 0.96f);
        galleryPreviewImage = previewBg;
        galleryPreviewImage.preserveAspect = true;
        galleryPreviewImageLabel = CreateTMPBlock(imageFrame, "ImageLabel", "未解锁", 24f, Color.white, TextAlignmentOptions.Center);
        StretchFull(galleryPreviewImageLabel.rectTransform);
        galleryPreviewImageLabel.fontStyle = FontStyles.Bold;

        galleryPreviewTitle = CreateTMPBlock(previewPane, "PreviewTitle", string.Empty, 30f, TutorialTextColor, TextAlignmentOptions.Center);
        galleryPreviewTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.36f);
        galleryPreviewTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.44f);
        galleryPreviewTitle.rectTransform.offsetMin = Vector2.zero;
        galleryPreviewTitle.rectTransform.offsetMax = Vector2.zero;
        galleryPreviewTitle.fontStyle = FontStyles.Bold;

        galleryPreviewSubtitle = CreateTMPBlock(previewPane, "PreviewSubtitle", string.Empty, 22f, TutorialAccentColor, TextAlignmentOptions.Center);
        galleryPreviewSubtitle.rectTransform.anchorMin = new Vector2(0.05f, 0.31f);
        galleryPreviewSubtitle.rectTransform.anchorMax = new Vector2(0.95f, 0.37f);
        galleryPreviewSubtitle.rectTransform.offsetMin = Vector2.zero;
        galleryPreviewSubtitle.rectTransform.offsetMax = Vector2.zero;

        galleryPreviewDescription = CreateTMPBlock(previewPane, "PreviewDescription", string.Empty, 21f, TutorialMutedTextColor, TextAlignmentOptions.TopLeft);
        galleryPreviewDescription.rectTransform.anchorMin = new Vector2(0.08f, 0.14f);
        galleryPreviewDescription.rectTransform.anchorMax = new Vector2(0.92f, 0.3f);
        galleryPreviewDescription.rectTransform.offsetMin = Vector2.zero;
        galleryPreviewDescription.rectTransform.offsetMax = Vector2.zero;
        galleryPreviewDescription.enableWordWrapping = true;
        galleryPreviewDescription.lineSpacing = 2f;
        galleryPreviewDescription.fontSize = 18f;

        galleryOpenButton = CreateMenuButton(previewPane, "GalleryOpenButton", "查看", new Vector2(0f, -250f), new Color(0.98f, 0.78f, 0.38f, 1f), OpenSelectedGalleryEntry);
        RectTransform openRect = galleryOpenButton.GetComponent<RectTransform>();
        openRect.anchorMin = new Vector2(0.5f, 0f);
        openRect.anchorMax = new Vector2(0.5f, 0f);
        openRect.sizeDelta = new Vector2(180f, 54f);
        openRect.anchoredPosition = new Vector2(0f, 38f);

        galleryCounterText = CreateTMPBlock(contentRoot, "GalleryCounter", string.Empty, 24f, TutorialMutedTextColor, TextAlignmentOptions.Right);
        galleryCounterText.rectTransform.anchorMin = new Vector2(0f, 0f);
        galleryCounterText.rectTransform.anchorMax = new Vector2(1f, 0.07f);
        galleryCounterText.rectTransform.offsetMin = new Vector2(20f, 0f);
        galleryCounterText.rectTransform.offsetMax = new Vector2(-26f, 0f);
        galleryCounterText.fontStyle = FontStyles.Bold;

        CreateGalleryViewer(galleryPanel.transform as RectTransform);
        SelectGalleryCategory(0);
    }

    private void LoadGalleryCategories()
    {
        galleryCategories.Clear();
        GalleryCategory cgCategory = new GalleryCategory { Name = "游戏CG" };
        GalleryCategory endingCategory = new GalleryCategory { Name = "结局" };
        HashSet<string> seenCgIds = new HashSet<string>();
        HashSet<string> unlockedEndings = LoadGallerySet(GalleryUnlockedEndingsKey);
        HashSet<string> unlockedCgs = LoadGallerySet(GalleryUnlockedCgsKey);

        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/endings");
        if (jsonAsset != null)
        {
            EndingDataRoot root = JsonUtility.FromJson<EndingDataRoot>(jsonAsset.text);
            if (root != null && root.endings != null)
            {
                for (int i = 0; i < root.endings.Count; i++)
                {
                    EndingDefinition ending = root.endings[i];
                    string indexLabel = (i + 1).ToString("000");
                    bool endingUnlocked = unlockedEndings.Contains(ending.id);
                    bool cgUnlocked = !string.IsNullOrEmpty(ending.cgId) && unlockedCgs.Contains(ending.cgId);

                    endingCategory.Entries.Add(new GalleryEntry
                    {
                        Id = ending.id,
                        Title = ending.name,
                        Subtitle = GetStarText(ending.stars),
                        Description = ending.description,
                        ResourceKey = ending.cgId,
                        Badge = indexLabel,
                        IsUnlocked = endingUnlocked,
                        IsEnding = true,
                        Conditions = CloneEndingConditions(ending.conditions)
                    });

                    if (!string.IsNullOrEmpty(ending.cgId) && seenCgIds.Add(ending.cgId))
                    {
                        cgCategory.Entries.Add(new GalleryEntry
                        {
                            Id = ending.cgId,
                            Title = ending.name,
                            Subtitle = GetStarText(ending.stars),
                            Description = ending.description,
                            ResourceKey = ending.cgId,
                            Badge = indexLabel,
                            IsUnlocked = cgUnlocked || endingUnlocked,
                            IsEnding = false,
                            Conditions = CloneEndingConditions(ending.conditions)
                        });
                    }
                }
            }
        }

        galleryCategories.Add(cgCategory);
        galleryCategories.Add(endingCategory);
    }

    private void ShowGalleryPanel()
    {
        if (galleryPanel == null)
        {
            return;
        }

        LoadGalleryCategories();
        galleryPanel.SetActive(true);
        galleryPanel.transform.SetAsLastSibling();
        galleryPanelGroup.alpha = 1f;
        galleryPanelGroup.interactable = true;
        galleryPanelGroup.blocksRaycasts = true;
        SelectGalleryCategory(currentGalleryCategoryIndex);
        StartCoroutine(RefreshGalleryLayoutNextFrame());
    }

    private void HideGalleryPanel()
    {
        if (galleryPanel == null)
        {
            return;
        }

        HideGalleryViewer();
        galleryPanelGroup.alpha = 0f;
        galleryPanelGroup.interactable = false;
        galleryPanelGroup.blocksRaycasts = false;
        galleryPanel.SetActive(false);
    }

    private void SelectGalleryCategory(int categoryIndex)
    {
        if (galleryCategories.Count == 0)
        {
            return;
        }

        currentGalleryCategoryIndex = Mathf.Clamp(categoryIndex, 0, galleryCategories.Count - 1);
        currentGalleryEntryIndex = 0;
        currentGalleryPageIndex = 0;

        for (int i = 0; i < galleryTabButtons.Count; i++)
        {
            UpdateTutorialCategoryButtonState(galleryTabButtons[i], i == currentGalleryCategoryIndex);
        }

        RebuildGalleryGrid();
        UpdateGalleryPreview();
    }

    private void RebuildGalleryGrid()
    {
        if (galleryGridContent == null)
        {
            return;
        }

        galleryEntryButtons.Clear();
        for (int i = galleryGridContent.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeOrEditor(galleryGridContent.GetChild(i).gameObject);
        }

        List<GalleryEntry> entries = galleryCategories[currentGalleryCategoryIndex].Entries;
        const int itemsPerPage = 12;
        const int columnCount = 3;
        const float cellWidth = 280f;
        const float cellHeight = 190f;
        const float horizontalSpacing = 18f;
        const float verticalSpacing = 18f;
        const int topPadding = 8;
        const int rightPadding = 24;
        const int bottomPadding = 24;
        const int leftPadding = 8;

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)itemsPerPage));
        currentGalleryPageIndex = Mathf.Clamp(currentGalleryPageIndex, 0, totalPages - 1);
        int startIndex = currentGalleryPageIndex * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, entries.Count);
        currentGalleryPageStartIndex = startIndex;

        for (int i = startIndex; i < endIndex; i++)
        {
            int captured = i;
            Button button = CreateGalleryEntryButton(galleryGridContent, entries[i], () =>
            {
                currentGalleryEntryIndex = captured;
                UpdateGallerySelection();
                UpdateGalleryPreview();
            });

            int localIndex = i - startIndex;
            int row = localIndex / columnCount;
            int column = localIndex % columnCount;
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(
                leftPadding + column * (cellWidth + horizontalSpacing),
                -(topPadding + row * (cellHeight + verticalSpacing)));

            galleryEntryButtons.Add(button);
        }

        int visibleCount = Mathf.Max(1, endIndex - startIndex);
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(visibleCount / (float)columnCount));
        float requiredWidth = leftPadding + rightPadding + columnCount * cellWidth + Mathf.Max(0, columnCount - 1) * horizontalSpacing;
        float requiredHeight = topPadding + bottomPadding + rowCount * cellHeight + Mathf.Max(0, rowCount - 1) * verticalSpacing;
        galleryGridContent.anchorMin = new Vector2(0f, 1f);
        galleryGridContent.anchorMax = new Vector2(0f, 1f);
        galleryGridContent.pivot = new Vector2(0f, 1f);
        galleryGridContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
        galleryGridContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
        galleryGridContent.anchoredPosition = Vector2.zero;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(galleryGridContent);
        UpdateGalleryPageControls(totalPages);
        if (galleryGridStatusText != null)
        {
            galleryGridStatusText.text = $"当前 {currentGalleryPageIndex + 1}/{totalPages} 页  显示 {Mathf.Max(0, endIndex - startIndex)}/{entries.Count}";
        }
        Debug.Log($"[Gallery] category={currentGalleryCategoryIndex} entries={entries.Count} page={currentGalleryPageIndex + 1}/{totalPages} visible={Mathf.Max(0, endIndex - startIndex)} contentChildren={galleryGridContent.childCount} size=({requiredWidth},{requiredHeight})");
        UpdateGallerySelection();
    }

    private Button CreateGalleryEntryButton(RectTransform parent, GalleryEntry entry, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = CreateUIElement(entry.Id + "Card", parent);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 170f);
        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredWidth = 280f;
        layout.preferredHeight = 190f;

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = entry.IsUnlocked ? new Color(0.96f, 0.93f, 0.85f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.98f);
        Outline outline = buttonGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.28f, 0.2f, 0.45f);
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        RectTransform thumbFrame = CreateUIElement("Thumbnail", rect).GetComponent<RectTransform>();
        thumbFrame.anchorMin = new Vector2(0.06f, 0.19f);
        thumbFrame.anchorMax = new Vector2(0.94f, 0.78f);
        thumbFrame.offsetMin = Vector2.zero;
        thumbFrame.offsetMax = Vector2.zero;
        Image thumbImage = thumbFrame.gameObject.AddComponent<Image>();
        thumbImage.preserveAspect = true;
        thumbImage.raycastTarget = false;

        TextMeshProUGUI thumbLabel = CreateTMPBlock(thumbFrame, "ThumbnailLabel", string.Empty, 24f, Color.white, TextAlignmentOptions.Center);
        StretchFull(thumbLabel.rectTransform);
        thumbLabel.fontStyle = FontStyles.Bold;

        SetGalleryImage(thumbImage, thumbLabel, entry);

        TextMeshProUGUI badge = CreateTMPBlock(rect, "Badge", entry.Badge, 20f, Color.white, TextAlignmentOptions.Left);
        badge.rectTransform.anchorMin = new Vector2(0f, 1f);
        badge.rectTransform.anchorMax = new Vector2(1f, 1f);
        badge.rectTransform.offsetMin = new Vector2(10f, -34f);
        badge.rectTransform.offsetMax = new Vector2(-10f, -2f);
        badge.fontStyle = FontStyles.Bold;

        string lockedTypeLabel = entry.IsEnding ? "结局未解锁" : "CG 未解锁";
        TextMeshProUGUI lockIcon = CreateTMPBlock(rect, "Lock", entry.IsUnlocked ? string.Empty : lockedTypeLabel, 24f, new Color(0.82f, 0.82f, 0.78f, 0.95f), TextAlignmentOptions.Center);
        lockIcon.rectTransform.anchorMin = new Vector2(0.12f, 0.41f);
        lockIcon.rectTransform.anchorMax = new Vector2(0.88f, 0.59f);
        lockIcon.rectTransform.offsetMin = Vector2.zero;
        lockIcon.rectTransform.offsetMax = Vector2.zero;
        lockIcon.fontStyle = FontStyles.Bold;

        TextMeshProUGUI title = CreateTMPBlock(rect, "Title", entry.IsUnlocked ? entry.Title : "？？？", 18f, entry.IsUnlocked ? TutorialTextColor : new Color(0.92f, 0.92f, 0.92f, 0.95f), TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
        title.rectTransform.anchorMax = new Vector2(0.92f, 0.18f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;
        title.fontStyle = FontStyles.Bold;
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;

        if (!entry.IsUnlocked)
        {
            TextMeshProUGUI hint = CreateTMPBlock(rect, "Hint", entry.IsEnding ? "达成后收录到结局册" : "解锁后收录到图鉴", 16f,
                new Color(0.82f, 0.8f, 0.74f, 0.9f), TextAlignmentOptions.Center);
            hint.rectTransform.anchorMin = new Vector2(0.08f, 0.0f);
            hint.rectTransform.anchorMax = new Vector2(0.92f, 0.08f);
            hint.rectTransform.offsetMin = Vector2.zero;
            hint.rectTransform.offsetMax = Vector2.zero;
            hint.enableWordWrapping = false;
            hint.overflowMode = TextOverflowModes.Ellipsis;
        }

        return button;
    }

    private void UpdateGallerySelection()
    {
        for (int i = 0; i < galleryEntryButtons.Count; i++)
        {
            Image bg = galleryEntryButtons[i].GetComponent<Image>();
            if (bg != null)
            {
                int absoluteIndex = currentGalleryPageStartIndex + i;
                if (absoluteIndex >= galleryCategories[currentGalleryCategoryIndex].Entries.Count)
                {
                    continue;
                }

                GalleryEntry entry = galleryCategories[currentGalleryCategoryIndex].Entries[absoluteIndex];
                bg.color = absoluteIndex == currentGalleryEntryIndex
                    ? (entry.IsUnlocked ? new Color(1f, 0.92f, 0.64f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f))
                    : (entry.IsUnlocked ? new Color(0.96f, 0.93f, 0.85f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.98f));
            }
        }
    }

    private void UpdateGalleryPreview()
    {
        GalleryEntry entry = GetSelectedGalleryEntry();
        if (entry == null)
        {
            galleryPreviewTitle.text = "图鉴待整理";
            galleryPreviewSubtitle.text = "当前分类还没有可展示内容";
            galleryPreviewDescription.text = "继续推进周目、结局与关键事件后，这里会逐步收录对应的结局卡与 CG。";
            galleryPreviewImage.sprite = null;
            galleryPreviewImageLabel.text = "暂无条目";
            gallerySectionTitle.text = galleryCategories.Count > currentGalleryCategoryIndex
                ? galleryCategories[currentGalleryCategoryIndex].Name
                : "图鉴";
            galleryOpenButton.interactable = false;
            SetGalleryGenderButtonsVisible(false);
            return;
        }

        gallerySectionTitle.text = galleryCategories[currentGalleryCategoryIndex].Name;
        galleryPreviewTitle.text = entry.IsUnlocked ? entry.Title : (entry.IsEnding ? "未解锁结局" : "未解锁 CG");
        galleryPreviewSubtitle.text = entry.IsUnlocked ? entry.Subtitle : (entry.IsEnding ? "尚未达成" : "尚未收集");
        galleryPreviewDescription.text = BuildGalleryPreviewDescription(entry);
        galleryOpenButton.interactable = entry.IsUnlocked;
        SetGalleryImage(galleryPreviewImage, galleryPreviewImageLabel, entry);
        SetGalleryGenderButtonsVisible(entry.IsEnding);
        UpdateGalleryGenderButtonState();

        int unlocked = 0;
        List<GalleryEntry> entries = galleryCategories[currentGalleryCategoryIndex].Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsUnlocked) unlocked++;
        }
        galleryCounterText.text = $"已收集 {unlocked}/{entries.Count}";
    }

    private GalleryEntry GetSelectedGalleryEntry()
    {
        if (galleryCategories.Count == 0) return null;
        List<GalleryEntry> entries = galleryCategories[currentGalleryCategoryIndex].Entries;
        if (entries.Count == 0) return null;
        currentGalleryEntryIndex = Mathf.Clamp(currentGalleryEntryIndex, 0, entries.Count - 1);
        return entries[currentGalleryEntryIndex];
    }

    private void OpenSelectedGalleryEntry()
    {
        GalleryEntry entry = GetSelectedGalleryEntry();
        if (entry == null)
        {
            return;
        }

        if (!entry.IsUnlocked)
        {
            return;
        }

        galleryViewerPanel.SetActive(true);
        galleryViewerPanel.transform.SetAsLastSibling();
        galleryViewerGroup.alpha = 1f;
        galleryViewerGroup.interactable = true;
        galleryViewerGroup.blocksRaycasts = true;
        galleryViewerTitle.text = entry.IsUnlocked ? entry.Title : "？？？";
        galleryViewerSubtitle.text = entry.Subtitle;
        galleryViewerDescription.text = BuildGalleryPreviewDescription(entry);
        SetGalleryImage(galleryViewerImage, galleryViewerImageLabel, entry);
    }

    private void CreateGalleryViewer(RectTransform parent)
    {
        galleryViewerPanel = CreateUIElement("GalleryViewer", parent);
        StretchFull(galleryViewerPanel.GetComponent<RectTransform>());
        galleryViewerPanel.SetActive(false);
        galleryViewerPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        galleryViewerGroup = galleryViewerPanel.AddComponent<CanvasGroup>();
        galleryViewerGroup.alpha = 0f;
        galleryViewerGroup.interactable = false;
        galleryViewerGroup.blocksRaycasts = false;

        RectTransform frame = CreateUIElement("ViewerFrame", galleryViewerPanel.transform as RectTransform).GetComponent<RectTransform>();
        frame.anchorMin = new Vector2(0.12f, 0.08f);
        frame.anchorMax = new Vector2(0.88f, 0.92f);
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;
        frame.gameObject.AddComponent<Image>().color = new Color(0.97f, 0.94f, 0.88f, 1f);

        RectTransform imageFrame = CreateUIElement("ViewerImageFrame", frame).GetComponent<RectTransform>();
        imageFrame.anchorMin = new Vector2(0.05f, 0.22f);
        imageFrame.anchorMax = new Vector2(0.95f, 0.86f);
        imageFrame.offsetMin = Vector2.zero;
        imageFrame.offsetMax = Vector2.zero;
        Image viewerBg = imageFrame.gameObject.AddComponent<Image>();
        viewerBg.color = new Color(0.28f, 0.29f, 0.27f, 1f);
        galleryViewerImage = viewerBg;
        galleryViewerImage.preserveAspect = true;
        galleryViewerImageLabel = CreateTMPBlock(imageFrame, "ViewerImageLabel", string.Empty, 30f, Color.white, TextAlignmentOptions.Center);
        StretchFull(galleryViewerImageLabel.rectTransform);

        galleryViewerTitle = CreateTMPBlock(frame, "ViewerTitle", string.Empty, 42f, TutorialTextColor, TextAlignmentOptions.Center);
        galleryViewerTitle.rectTransform.anchorMin = new Vector2(0.08f, 0.88f);
        galleryViewerTitle.rectTransform.anchorMax = new Vector2(0.92f, 0.98f);
        galleryViewerTitle.rectTransform.offsetMin = Vector2.zero;
        galleryViewerTitle.rectTransform.offsetMax = Vector2.zero;
        galleryViewerTitle.fontStyle = FontStyles.Bold;

        galleryViewerSubtitle = CreateTMPBlock(frame, "ViewerSubtitle", string.Empty, 24f, TutorialAccentColor, TextAlignmentOptions.Center);
        galleryViewerSubtitle.rectTransform.anchorMin = new Vector2(0.08f, 0.16f);
        galleryViewerSubtitle.rectTransform.anchorMax = new Vector2(0.92f, 0.21f);
        galleryViewerSubtitle.rectTransform.offsetMin = Vector2.zero;
        galleryViewerSubtitle.rectTransform.offsetMax = Vector2.zero;

        galleryViewerDescription = CreateTMPBlock(frame, "ViewerDescription", string.Empty, 23f, TutorialMutedTextColor, TextAlignmentOptions.TopLeft);
        galleryViewerDescription.rectTransform.anchorMin = new Vector2(0.08f, 0.06f);
        galleryViewerDescription.rectTransform.anchorMax = new Vector2(0.92f, 0.15f);
        galleryViewerDescription.rectTransform.offsetMin = Vector2.zero;
        galleryViewerDescription.rectTransform.offsetMax = Vector2.zero;
        galleryViewerDescription.enableWordWrapping = true;

        CreateTutorialCloseButton(frame, HideGalleryViewer);
    }

    private void HideGalleryViewer()
    {
        if (galleryViewerPanel == null)
        {
            return;
        }

        galleryViewerGroup.alpha = 0f;
        galleryViewerGroup.interactable = false;
        galleryViewerGroup.blocksRaycasts = false;
        galleryViewerPanel.SetActive(false);
    }

    private IEnumerator RefreshGalleryLayoutNextFrame()
    {
        yield return null;

        if (galleryPanel == null || !galleryPanel.activeInHierarchy)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        RebuildGalleryGrid();
        UpdateGalleryPreview();
    }

    private void ChangeGalleryPage(int delta)
    {
        List<GalleryEntry> entries = galleryCategories[currentGalleryCategoryIndex].Entries;
        const int itemsPerPage = 12;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)itemsPerPage));
        if (totalPages <= 1)
        {
            return;
        }

        int nextPage = Mathf.Clamp(currentGalleryPageIndex + delta, 0, totalPages - 1);
        if (nextPage == currentGalleryPageIndex)
        {
            return;
        }

        currentGalleryPageIndex = nextPage;
        currentGalleryEntryIndex = Mathf.Clamp(currentGalleryPageIndex * itemsPerPage, 0, Mathf.Max(0, entries.Count - 1));
        RebuildGalleryGrid();
        UpdateGalleryPreview();
    }

    private void UpdateGalleryPageControls(int totalPages)
    {
        if (galleryPageText != null)
        {
            galleryPageText.text = $"{currentGalleryPageIndex + 1}/{Mathf.Max(1, totalPages)}";
        }

        if (galleryPrevPageButton != null)
        {
            galleryPrevPageButton.interactable = currentGalleryPageIndex > 0;
        }

        if (galleryNextPageButton != null)
        {
            galleryNextPageButton.interactable = currentGalleryPageIndex < totalPages - 1;
        }
    }

    private void SetGalleryImage(Image image, TextMeshProUGUI label, GalleryEntry entry)
    {
        Sprite sprite = entry.IsUnlocked && !string.IsNullOrEmpty(entry.ResourceKey)
            ? Resources.Load<Sprite>($"CG/{entry.ResourceKey}")
            : null;
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : new Color(0.16f, 0.16f, 0.16f, 0.98f);
        if (sprite != null)
        {
            label.text = string.Empty;
            return;
        }

        if (!entry.IsUnlocked)
        {
            label.text = entry.IsEnding ? "结局未解锁" : "CG 未解锁";
            return;
        }

        label.text = entry.IsEnding ? "结局已解锁" : "CG 已解锁";
    }

    private string BuildLockedGalleryDescription(GalleryEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        return entry.IsEnding
            ? "这个结局暂时还没有被你收入结局册。继续推进不同路线、属性组合与关键抉择，达成后这里会显示对应标题、描述和预览。"
            : "这张游戏 CG 还没有被你收入图鉴。随着结局、事件或人物路线解锁，这里会补上对应画面。";
    }

    private string BuildGalleryPreviewDescription(GalleryEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        string baseDescription = entry.IsUnlocked ? entry.Description : BuildLockedGalleryDescription(entry);
        if (!entry.IsEnding)
        {
            return baseDescription;
        }

        string genderText = galleryRequirementPreviewGender == 1 ? "女主" : "男主";
        return $"{baseDescription}\n\n结局要求（{genderText}）\n{BuildEndingRequirementSummary(entry)}";
    }

    private string BuildEndingRequirementSummary(GalleryEntry entry)
    {
        if (entry == null || entry.Conditions == null || entry.Conditions.Count == 0)
        {
            return "无特殊条件";
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < entry.Conditions.Count; i++)
        {
            EndingCondition condition = entry.Conditions[i];
            if (condition == null)
            {
                continue;
            }

            lines.Add($"- {FormatEndingConditionForGallery(condition)}");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "无特殊条件";
    }

    private string FormatEndingConditionForGallery(EndingCondition condition)
    {
        EndingConditionType condType = condition.GetConditionType();
        float value = condition.value;
        switch (condType)
        {
            case EndingConditionType.GPA_GreaterOrEqual: return $"GPA >= {value:F1}";
            case EndingConditionType.GPA_Less: return $"GPA < {value:F1}";
            case EndingConditionType.Study_GreaterOrEqual: return $"学力 >= {value:F0}";
            case EndingConditionType.Charm_GreaterOrEqual: return $"魅力 >= {value:F0}";
            case EndingConditionType.Physique_GreaterOrEqual: return $"体魄 >= {value:F0}";
            case EndingConditionType.Leadership_GreaterOrEqual: return $"领导力 >= {value:F0}";
            case EndingConditionType.Stress_GreaterOrEqual: return $"压力 >= {value:F0}";
            case EndingConditionType.Study_Less: return $"学力 < {value:F0}";
            case EndingConditionType.Mood_Equals: return $"心情 = {value:F0}";
            case EndingConditionType.Mood_Less: return $"心情 < {value:F0}";
            case EndingConditionType.Money_Less: return $"金钱 < {value:F0}";
            case EndingConditionType.Money_GreaterOrEqual: return $"金钱 >= {value:F0}";
            case EndingConditionType.Guilt_LessOrEqual: return $"负罪感 <= {value:F0}";
            case EndingConditionType.Darkness_GreaterOrEqual: return $"黑暗值 >= {value:F0}";
            case EndingConditionType.HasPartner: return "拥有恋人";
            case EndingConditionType.NoPartner: return "没有恋人";
            case EndingConditionType.RomanceLevel_GreaterOrEqual: return $"恋爱结局等级 >= {value:F0}";
            case EndingConditionType.FriendCount_GreaterOrEqual: return $"朋友数 >= {value:F0}";
            case EndingConditionType.IsStudentCouncilPresident: return "学生会主席";
            case EndingConditionType.IsPartyMember: return "正式党员";
            case EndingConditionType.PlayerGender_Equals:
                int requiredGender = Mathf.RoundToInt(value) == 1 ? 1 : 0;
                bool matched = galleryRequirementPreviewGender == requiredGender;
                return $"主角性别 = {(requiredGender == 1 ? "女" : "男")}（当前：{(galleryRequirementPreviewGender == 1 ? "女" : "男")}，{(matched ? "满足" : "不满足")}）";
            case EndingConditionType.HasNationalScholarship: return "获得国奖";
            case EndingConditionType.EventFlag_True: return $"触发事件标记：{condition.targetId}";
            case EndingConditionType.CheatingCount_GreaterOrEqual: return $"作弊被抓次数 >= {value:F0}";
            case EndingConditionType.SlackingValue_GreaterOrEqual: return $"摆烂值 >= {value:F0}";
            case EndingConditionType.MentalHealth_Equals: return $"心理健康 = {value:F0}";
            case EndingConditionType.CET4Passed: return "通过英语四级";
            case EndingConditionType.CET6Passed: return "通过英语六级";
            case EndingConditionType.TotalStudyCount_GreaterOrEqual: return $"累计学习次数 >= {value:F0}";
            case EndingConditionType.TotalSocialCount_GreaterOrEqual: return $"累计社交次数 >= {value:F0}";
            case EndingConditionType.GraduationScore_GreaterOrEqual: return $"毕业总评 >= {value:F1}";
            case EndingConditionType.InternshipCount_GreaterOrEqual: return $"实习次数 >= {value:F0}";
            case EndingConditionType.AlwaysTrue: return "无条件";
            default: return condition.type;
        }
    }

    private void SetGalleryRequirementGender(int gender)
    {
        galleryRequirementPreviewGender = Mathf.Clamp(gender, 0, 1);
        UpdateGalleryGenderButtonState();
        UpdateGalleryPreview();
    }

    private void SetGalleryGenderButtonsVisible(bool visible)
    {
        if (galleryGenderMaleButton != null)
        {
            galleryGenderMaleButton.gameObject.SetActive(visible);
        }

        if (galleryGenderFemaleButton != null)
        {
            galleryGenderFemaleButton.gameObject.SetActive(visible);
        }
    }

    private void UpdateGalleryGenderButtonState()
    {
        UpdateGalleryGenderButtonVisual(galleryGenderMaleButton, galleryRequirementPreviewGender == 0, new Color(0.85f, 0.9f, 0.98f, 1f));
        UpdateGalleryGenderButtonVisual(galleryGenderFemaleButton, galleryRequirementPreviewGender == 1, new Color(0.98f, 0.84f, 0.9f, 1f));
    }

    private void UpdateGalleryGenderButtonVisual(Button button, bool selected, Color baseColor)
    {
        if (button == null)
        {
            return;
        }

        Image bg = button.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = selected ? new Color(baseColor.r * 0.95f, baseColor.g * 0.95f, baseColor.b * 0.95f, 1f) : new Color(0.95f, 0.93f, 0.9f, 0.95f);
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.color = selected ? TutorialAccentColor : TutorialMutedTextColor;
            text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private List<EndingCondition> CloneEndingConditions(List<EndingCondition> source)
    {
        List<EndingCondition> clones = new List<EndingCondition>();
        if (source == null)
        {
            return clones;
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
            {
                clones.Add(source[i].Clone());
            }
        }

        return clones;
    }

    private static HashSet<string> LoadGallerySet(string key)
    {
        HashSet<string> values = new HashSet<string>();
        string raw = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(raw)) return values;
        string[] parts = raw.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i])) values.Add(parts[i]);
        }
        return values;
    }

    private static void SaveGallerySet(string key, HashSet<string> values)
    {
        PlayerPrefs.SetString(key, string.Join("|", new List<string>(values).ToArray()));
    }

    public static void UnlockGalleryEnding(string endingId, string cgId)
    {
        HashSet<string> endings = LoadGallerySet(GalleryUnlockedEndingsKey);
        HashSet<string> cgs = LoadGallerySet(GalleryUnlockedCgsKey);
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(endingId) && endings.Add(endingId)) changed = true;
        if (!string.IsNullOrWhiteSpace(cgId) && cgs.Add(cgId)) changed = true;

        if (changed)
        {
            SaveGallerySet(GalleryUnlockedEndingsKey, endings);
            SaveGallerySet(GalleryUnlockedCgsKey, cgs);
            PlayerPrefs.Save();
        }
    }

    private string GetStarText(int stars)
    {
        if (stars <= 0) return "特殊结局";
        return new string('★', stars);
    }

    private void CreateTutorialPanel(RectTransform parent)
    {
        tutorialPanel = CreateUIElement("TutorialPanel", parent);
        StretchFull(tutorialPanel.GetComponent<RectTransform>());
        tutorialPanel.SetActive(false);

        Image overlay = tutorialPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.35f);

        tutorialPanelGroup = tutorialPanel.AddComponent<CanvasGroup>();
        tutorialPanelGroup.alpha = 0f;
        tutorialPanelGroup.interactable = false;
        tutorialPanelGroup.blocksRaycasts = false;

        RectTransform frameRect = CreateUIElement("TutorialFrame", tutorialPanel.transform as RectTransform).GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.08f, 0.08f);
        frameRect.anchorMax = new Vector2(0.92f, 0.9f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        Image frameImage = frameRect.gameObject.AddComponent<Image>();
        frameImage.color = TutorialBackdropColor;
        Outline frameOutline = frameRect.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.82f, 0.74f, 0.63f, 0.55f);
        frameOutline.effectDistance = new Vector2(3f, -3f);

        RectTransform bodyRect = CreateUIElement("TutorialBody", frameRect).GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.06f, 0.1f);
        bodyRect.anchorMax = new Vector2(0.94f, 0.88f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        Image bodyImage = bodyRect.gameObject.AddComponent<Image>();
        bodyImage.color = TutorialFrameColor;

        RectTransform paperRect = CreateUIElement("TutorialPaper", bodyRect).GetComponent<RectTransform>();
        paperRect.anchorMin = new Vector2(0.03f, 0.03f);
        paperRect.anchorMax = new Vector2(0.97f, 0.97f);
        paperRect.offsetMin = Vector2.zero;
        paperRect.offsetMax = Vector2.zero;
        Image paperImage = paperRect.gameObject.AddComponent<Image>();
        paperImage.color = TutorialPaperColor;
        CreateTutorialGridBackground(paperRect);

        tutorialTabsRoot = CreateUIElement("TutorialTabs", frameRect).GetComponent<RectTransform>();
        tutorialTabsRoot.anchorMin = new Vector2(0.06f, 0.88f);
        tutorialTabsRoot.anchorMax = new Vector2(0.94f, 0.98f);
        tutorialTabsRoot.offsetMin = Vector2.zero;
        tutorialTabsRoot.offsetMax = Vector2.zero;
        HorizontalLayoutGroup tabsLayout = tutorialTabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 14f;
        tabsLayout.childAlignment = TextAnchor.LowerLeft;
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childForceExpandHeight = true;
        tabsLayout.padding = new RectOffset(0, 110, 0, 0);
        RebuildTutorialCategoryTabs();

        CreateTutorialCloseButton(frameRect, HideTutorialPanel);

        RectTransform contentRoot = CreateUIElement("TutorialContent", paperRect).GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.02f, 0.03f);
        contentRoot.anchorMax = new Vector2(0.98f, 0.97f);
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;

        RectTransform leftPane = CreateUIElement("LeftPane", contentRoot).GetComponent<RectTransform>();
        leftPane.anchorMin = new Vector2(0f, 0f);
        leftPane.anchorMax = new Vector2(0.28f, 1f);
        leftPane.offsetMin = new Vector2(16f, 18f);
        leftPane.offsetMax = new Vector2(-18f, -18f);

        RectTransform divider = CreateUIElement("Divider", contentRoot).GetComponent<RectTransform>();
        divider.anchorMin = new Vector2(0.295f, 0.05f);
        divider.anchorMax = new Vector2(0.295f, 0.95f);
        divider.sizeDelta = new Vector2(3f, 0f);
        divider.anchoredPosition = Vector2.zero;
        divider.gameObject.AddComponent<Image>().color = new Color(0.84f, 0.75f, 0.65f, 0.8f);

        RectTransform rightPane = CreateUIElement("RightPane", contentRoot).GetComponent<RectTransform>();
        rightPane.anchorMin = new Vector2(0.315f, 0f);
        rightPane.anchorMax = new Vector2(1f, 1f);
        rightPane.offsetMin = new Vector2(18f, 18f);
        rightPane.offsetMax = new Vector2(-16f, -18f);

        ScrollRect leftScroll = CreateTutorialScrollView(leftPane, out tutorialItemListContent);
        leftScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        VerticalLayoutGroup leftLayout = tutorialItemListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.spacing = 10f;
        leftLayout.childAlignment = TextAnchor.UpperLeft;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = false;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;
        ContentSizeFitter leftFitter = tutorialItemListContent.gameObject.AddComponent<ContentSizeFitter>();
        leftFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        tutorialSectionTitle = CreateTMPBlock(rightPane, "SectionTitle", string.Empty, 42f, TutorialAccentColor, TextAlignmentOptions.Center);
        SetAnchoredBox(tutorialSectionTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(520f, 56f));
        tutorialSectionTitle.fontStyle = FontStyles.Bold;

        RectTransform titleRule = CreateUIElement("TitleRule", rightPane).GetComponent<RectTransform>();
        titleRule.anchorMin = new Vector2(0.08f, 1f);
        titleRule.anchorMax = new Vector2(0.92f, 1f);
        titleRule.pivot = new Vector2(0.5f, 1f);
        titleRule.sizeDelta = new Vector2(0f, 2f);
        titleRule.anchoredPosition = new Vector2(0f, -74f);
        titleRule.gameObject.AddComponent<Image>().color = new Color(0.85f, 0.76f, 0.67f, 0.55f);

        ScrollRect rightScroll = CreateTutorialScrollView(rightPane, out tutorialDetailContent);
        RectTransform rightScrollRect = rightScroll.GetComponent<RectTransform>();
        rightScrollRect.anchorMin = new Vector2(0f, 0f);
        rightScrollRect.anchorMax = new Vector2(1f, 1f);
        rightScrollRect.offsetMin = new Vector2(0f, 10f);
        rightScrollRect.offsetMax = new Vector2(0f, -92f);
        rightScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        VerticalLayoutGroup detailLayout = tutorialDetailContent.gameObject.AddComponent<VerticalLayoutGroup>();
        detailLayout.spacing = 24f;
        detailLayout.childAlignment = TextAnchor.UpperLeft;
        detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = false;
        detailLayout.childForceExpandWidth = true;
        detailLayout.childForceExpandHeight = false;
        detailLayout.padding = new RectOffset(20, 20, 8, 20);
        ContentSizeFitter detailFitter = tutorialDetailContent.gameObject.AddComponent<ContentSizeFitter>();
        detailFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        tutorialEntryTitle = CreateTMPLayoutItem(tutorialDetailContent, "EntryTitle", string.Empty, 34f, TutorialTextColor, FontStyles.Bold, 54f);
        tutorialEntryDescription = CreateTMPLayoutItem(tutorialDetailContent, "EntryDescription", string.Empty, 24f, TutorialMutedTextColor, FontStyles.Normal, 180f);
        tutorialEntryDescription.enableWordWrapping = true;
        tutorialEntryDescription.lineSpacing = 8f;

        CreateTutorialPreviewCard(tutorialDetailContent);

        LayoutRebuilder.ForceRebuildLayoutImmediate(frameRect);
        if (tutorialCategories.Count > 0)
        {
            SelectTutorialCategory(0);
        }
    }

    private void ShowTutorialPanel()
    {
        if (tutorialPanel == null)
        {
            return;
        }

        DebugReloadAuthoredTitleContent();
        tutorialPanel.SetActive(true);
        tutorialPanelGroup.alpha = 1f;
        tutorialPanelGroup.interactable = true;
        tutorialPanelGroup.blocksRaycasts = true;
        if (tutorialCategories.Count > 0)
        {
            SelectTutorialCategory(currentTutorialCategoryIndex);
        }
    }

    private void HideTutorialPanel()
    {
        if (tutorialPanel == null)
        {
            return;
        }

        tutorialPanelGroup.alpha = 0f;
        tutorialPanelGroup.interactable = false;
        tutorialPanelGroup.blocksRaycasts = false;
        tutorialPanel.SetActive(false);
    }

    private void SelectTutorialCategory(int categoryIndex)
    {
        if (tutorialCategories.Count == 0)
        {
            if (tutorialSectionTitle != null)
            {
                tutorialSectionTitle.text = titleContentData?.tutorial?.panelTitle ?? "新生手册";
            }
            return;
        }

        currentTutorialCategoryIndex = Mathf.Clamp(categoryIndex, 0, tutorialCategories.Count - 1);
        currentTutorialEntryIndex = 0;

        for (int i = 0; i < tutorialCategoryButtons.Count; i++)
        {
            UpdateTutorialCategoryButtonState(tutorialCategoryButtons[i], i == currentTutorialCategoryIndex);
        }

        RebuildTutorialEntryList();
        UpdateTutorialEntryDetail();
    }

    private void RebuildTutorialCategoryTabs()
    {
        tutorialCategoryButtons.Clear();
        if (tutorialTabsRoot == null)
        {
            return;
        }

        for (int i = tutorialTabsRoot.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeOrEditor(tutorialTabsRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < tutorialCategories.Count; i++)
        {
            int captured = i;
            tutorialCategoryButtons.Add(CreateTutorialCategoryButton(tutorialTabsRoot, tutorialCategories[i].Name, () => SelectTutorialCategory(captured)));
        }
    }

    private void RebuildTutorialEntryList()
    {
        tutorialEntryButtons.Clear();

        for (int i = tutorialItemListContent.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeOrEditor(tutorialItemListContent.GetChild(i).gameObject);
        }

        List<TutorialEntry> entries = tutorialCategories[currentTutorialCategoryIndex].Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            int captured = i;
            Button button = CreateTutorialEntryButton(tutorialItemListContent, entries[i].Title, () =>
            {
                currentTutorialEntryIndex = captured;
                UpdateTutorialEntrySelection();
                UpdateTutorialEntryDetail();
            });
            tutorialEntryButtons.Add(button);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(tutorialItemListContent);
        UpdateTutorialEntrySelection();
    }

    private void UpdateTutorialEntrySelection()
    {
        for (int i = 0; i < tutorialEntryButtons.Count; i++)
        {
            UpdateTutorialEntryButtonState(tutorialEntryButtons[i], i == currentTutorialEntryIndex);
        }
    }

    private void UpdateTutorialEntryDetail()
    {
        TutorialCategory category = tutorialCategories[currentTutorialCategoryIndex];
        if (category.Entries.Count == 0)
        {
            tutorialSectionTitle.text = category.Name;
            tutorialEntryTitle.text = "本栏内容整理中";
            tutorialEntryDescription.text = "这一页之后会补入更具体的玩法建议、路线提醒和系统说明。先从左侧切到其他主题继续查看。";
            RefreshTutorialHighlights(new[] { "切换其他主题", "先看现有条目", "后续持续补充" });
            return;
        }

        currentTutorialEntryIndex = Mathf.Clamp(currentTutorialEntryIndex, 0, category.Entries.Count - 1);
        TutorialEntry entry = category.Entries[currentTutorialEntryIndex];
        tutorialSectionTitle.text = category.Name;
        tutorialEntryTitle.text = entry.Title;
        tutorialEntryDescription.text = $"{entry.Lead}\n\n{entry.Description}";

        RefreshTutorialHighlights(entry.Highlights);
    }

    private void RefreshTutorialHighlights(string[] highlights)
    {
        Transform preview = tutorialDetailContent.Find("PreviewCard");
        if (preview == null)
        {
            return;
        }

        Transform tagsRoot = preview.Find("TagRow");
        if (tagsRoot == null)
        {
            return;
        }

        for (int i = tagsRoot.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeOrEditor(tagsRoot.GetChild(i).gameObject);
        }

        if (highlights == null)
        {
            return;
        }

        for (int i = 0; i < highlights.Length; i++)
        {
            CreateTutorialTag(tagsRoot as RectTransform, highlights[i]);
        }
    }

    private void CreateFadeOverlay()
    {
        GameObject overlayGO = CreateUIElement("FadeOverlay", canvasRect);
        StretchFull(overlayGO.GetComponent<RectTransform>());

        Image overlayImage = overlayGO.AddComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = false;

        fadeOverlay = overlayGO.AddComponent<CanvasGroup>();
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
        fadeOverlay.interactable = false;
        overlayGO.transform.SetAsLastSibling();
    }

    #endregion

    #region 交互逻辑

    private void OnScreenTapped(Vector2 screenPosition)
    {
        if (hasEnteredMenu)
        {
            return;
        }

        hasEnteredMenu = true;

        if (breathCoroutine != null)
        {
            StopCoroutine(breathCoroutine);
            breathCoroutine = null;
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }

        // 涟漪特效与菜单同步触发，互不等待
        RippleEffect.Create(canvasRect, screenPosition, null);
        ShowMenuOverlay();
    }

    private void OnRippleComplete()
    {
        // 保留空实现，避免旧引用报错
    }

    private System.Collections.IEnumerator ShowMenuAfterRipple()
    {
        // 保留空实现，避免旧引用报错
        yield break;
    }

    private void ShowMenuOverlay()
    {
        if (menuOverlay == null)
        {
            return;
        }

        UIFlowGuard.EnsureEventSystem();

        menuOverlay.gameObject.SetActive(true);
        menuOverlay.alpha = 0f;
        menuOverlay.interactable = false;
        menuOverlay.blocksRaycasts = false;

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        RefreshContinueButtonState();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        StartCoroutine(FadeInMenuOverlay());
    }

    private IEnumerator FadeInMenuOverlay()
    {
        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            menuOverlay.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        menuOverlay.alpha = 1f;
        menuOverlay.interactable = true;
        menuOverlay.blocksRaycasts = true;
    }

    public void OpenSettings()
    {
        SettingsUIBuilder.ShowSettings(true);
    }

    private IEnumerator AutoSkipTitleScreen()
    {
        hasEnteredMenu = true;

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }

        yield return null;

        if (!transitionRequested)
        {
            StartGame();
        }
    }

    public void BackToMainMenu()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    public void ContinueGame()
    {
        Debug.Log("[TitleScreen] 点击继续游戏");
        EnsureSaveManager();

        if (SaveManager.Instance == null)
        {
            ShowTitleNotification("继续游戏不可用", "存档系统还没有准备好，暂时无法读取自动存档。");
            return;
        }

        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData(0))
        {
            SaveData data = SaveManager.Instance.LoadFromSlot(0);
            if (data != null)
            {
                SaveManager.PendingLoadData = data;
                SaveManager.PendingLoadSlot = 0;
                Debug.Log("[TitleScreen] 已加载自动存档，准备进入游戏");
                BeginGameTransition(false);
                return;
            }

            Debug.LogWarning("[TitleScreen] 自动存档读取失败");
            ShowTitleNotification("自动存档读取失败", "自动存档暂时无法读取，需要的话可以改用“载入游戏”选择其他存档。");
            return;
        }

        Debug.Log("[TitleScreen] 无自动存档");
        if (continueGameStartsGame)
        {
            ShowTitleNotification("没有可继续的进度", "当前没有找到自动存档，本次会直接进入新游戏流程。", 2.6f, new Color(0.36f, 0.64f, 0.92f));
            StartGame();
            return;
        }

        ShowTitleNotification("没有可继续的进度", "当前没有找到自动存档，需要的话请使用“载入游戏”选择其他存档。");
    }

    private bool HasAnySaveData()
    {
        EnsureSaveManager();

        if (SaveManager.Instance == null)
        {
            return false;
        }

        for (int slot = 0; slot <= 3; slot++)
        {
            if (SaveManager.Instance.HasSaveData(slot))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasManualSaveData()
    {
        EnsureSaveManager();

        if (SaveManager.Instance == null)
        {
            return false;
        }

        for (int slot = 1; slot <= 3; slot++)
        {
            if (SaveManager.Instance.HasSaveData(slot))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            return;
        }

        GameObject obj = new GameObject("SaveManager");
        obj.AddComponent<SaveManager>();
    }

    public void StartGame()
    {
        UIFlowGuard.EnsureEventSystem();

        if (transitionRequested)
        {
            return;
        }

        // 显示角色创建UI
        if (StartupFlowSettings.SkipCharacterCreation)
        {
            CharacterCreationUI.ApplyDefaultPendingCharacter();
            HandleCharacterCreationComplete();
            return;
        }

        if (CharacterCreationUI.Instance == null)
        {
            GameObject obj = new GameObject("CharacterCreationUI");
            CharacterCreationUI ui = obj.AddComponent<CharacterCreationUI>();
            ui.OnCreationComplete -= HandleCharacterCreationComplete;
            ui.OnCreationComplete += HandleCharacterCreationComplete;
            ui.Show();
        }
        else
        {
            CharacterCreationUI.Instance.OnCreationComplete -= HandleCharacterCreationComplete;
            CharacterCreationUI.Instance.OnCreationComplete += HandleCharacterCreationComplete;
            CharacterCreationUI.Instance.Show();
        }
    }

    private void HandleCharacterCreationComplete()
    {
        if (CharacterCreationUI.Instance != null)
        {
            CharacterCreationUI.Instance.OnCreationComplete -= HandleCharacterCreationComplete;
        }

        GameplaySessionReset.ResetForFreshGame();
        BeginGameTransition(playOpeningStoryOnNewGame && !StartupFlowSettings.SkipOpeningStory);
    }

    private void BeginGameTransition(bool playOpeningStory)
    {
        if (transitionRequested)
        {
            return;
        }

        HideAllTitleOverlayPanels();
        transitionRequested = true;
        StartCoroutine(TransitionToGameScene(playOpeningStory));
    }

    public void QuitGame()
    {
        Debug.Log("[TitleScreen] 退出游戏");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private IEnumerator TransitionToGameScene(bool playOpeningStory)
    {
        UIFlowGuard.CleanupBlockingUI();
        HideAllTitleOverlayPanels();

        if (menuOverlay != null)
        {
            menuOverlay.interactable = false;
            menuOverlay.blocksRaycasts = false;
        }

        float fadeDuration = 0.45f;
        float elapsed = 0f;
        AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        while (elapsed < fadeDuration)
        {
            float t = curve.Evaluate(elapsed / fadeDuration);
            fadeOverlay.alpha = t;
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeOverlay.alpha = 1f;

        if (playOpeningStory)
        {
            OpeningStoryManager.Play(gameSceneName);
        }
        else
        {
            SceneLoader.LoadSceneAfterOpening(gameSceneName);
        }
    }

    private void HideAllTitleOverlayPanels()
    {
        HideGalleryViewer();
        HideGalleryPanel();
        HideCreditsPanel();
        HideTutorialPanel();
        HideChangelogPanel();

        if (menuOverlay != null)
        {
            menuOverlay.interactable = false;
            menuOverlay.blocksRaycasts = false;
        }
    }

    #endregion

    #region 设置逻辑

    private void LoadSettings()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f));
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, 1) == 1);
        }

        ApplySettings();
    }

    private void SaveSettings()
    {
        if (musicVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolumeSlider.value);
        }

        if (sfxVolumeSlider != null)
        {
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolumeSlider.value);
        }

        if (fullscreenToggle != null)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreenToggle.isOn ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        if (musicVolumeSlider != null)
        {
            AudioListener.volume = musicVolumeSlider.value;
        }

        if (fullscreenToggle != null)
        {
            Screen.fullScreen = fullscreenToggle.isOn;
        }
    }

    public void OnMusicVolumeChanged()
    {
        ApplySettings();
        SaveSettings();
    }

    public void OnSfxVolumeChanged()
    {
        SaveSettings();
    }

    public void OnFullscreenChanged()
    {
        ApplySettings();
        SaveSettings();
    }

    #endregion

    #region 视频播放

    private void PrepareVideoBackground()
    {
        if (videoImage == null)
        {
            return;
        }

        if (!System.IO.File.Exists(resolvedVideoPath))
        {
            Debug.LogWarning($"[TitleScreen] 未找到开始界面视频: {resolvedVideoPath}");
            if (!notifiedMissingMenuVideo)
            {
                notifiedMissingMenuVideo = true;
                ShowTitleNotification("标题背景视频缺失", "开始界面视频没有找到，标题页会继续使用静态背景运行。", 3f, new Color(0.86f, 0.62f, 0.24f));
            }
            return;
        }

        for (int i = 0; i < videoPlayers.Length; i++)
        {
            CreateVideoPlayer(i);
            videoPlayers[i].Prepare();
        }
    }

    private void CreateVideoPlayer(int index)
    {
        GameObject playerGO = new GameObject($"TitleVideoPlayer_{index}");
        playerGO.transform.SetParent(transform, false);

        videoTextures[index] = new RenderTexture(1920, 1080, 24);
        videoTextures[index].Create();

        VideoPlayer player = playerGO.AddComponent<VideoPlayer>();
        player.source = VideoSource.Url;
        player.url = resolvedVideoUrl;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = videoTextures[index];
        player.isLooping = false;
        player.skipOnDrop = true;
        player.waitForFirstFrame = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.playOnAwake = false;
        player.prepareCompleted += OnVideoPrepared;
        player.loopPointReached += OnVideoLoopPointReached;
        player.errorReceived += OnVideoError;

        videoPlayers[index] = player;
        playerPrepared[index] = false;
    }

    private void OnVideoPrepared(VideoPlayer preparedPlayer)
    {
        int preparedIndex = GetPlayerIndex(preparedPlayer);
        if (preparedIndex < 0)
        {
            return;
        }

        // 始终标记 prepared 状态，无论是否已有活跃播放器
        playerPrepared[preparedIndex] = true;
        Debug.Log($"[TitleScreen] VideoPlayer_{preparedIndex} 已准备就绪");

        if (activePlayerIndex >= 0)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // 已有活跃播放器 —— 检查活跃播放器是否已停止（卡住恢复）
            VideoPlayer activePlayer = videoPlayers[activePlayerIndex];
            if (activePlayer != null && !activePlayer.isPlaying && preparedIndex == standbyPlayerIndex)
            {
                Debug.Log($"[TitleScreen] 检测到活跃播放器已停止，自动切换到 VideoPlayer_{preparedIndex}");
                SwitchToPreparedPlayer(preparedIndex);
            }
            return;
        }

        // 首次启动：设定活跃/备用播放器
        activePlayerIndex = preparedIndex;
        standbyPlayerIndex = GetOtherPlayerIndex(preparedIndex);
        videoImage.texture = videoTextures[preparedIndex];
        videoImage.color = Color.white;
        preparedPlayer.Play();

        if (!Application.isPlaying)
        {
            return;
        }

        if (loopMonitorCoroutine == null)
        {
            loopMonitorCoroutine = StartCoroutine(MonitorVideoLoop());
        }
    }

    private void OnVideoLoopPointReached(VideoPlayer finishedPlayer)
    {
        int finishedIndex = GetPlayerIndex(finishedPlayer);
        if (finishedIndex != activePlayerIndex)
        {
            return;
        }

        Debug.Log($"[TitleScreen] VideoPlayer_{finishedIndex} 播放完毕");

        if (!Application.isPlaying)
        {
            finishedPlayer.time = 0;
            finishedPlayer.Play();
            return;
        }

        if (standbyPlayerIndex >= 0 && playerPrepared[standbyPlayerIndex])
        {
            SwitchToPreparedPlayer(standbyPlayerIndex);
            return;
        }

        // 备用播放器未就绪 —— 回退到单播放器循环
        Debug.Log("[TitleScreen] 备用播放器未就绪，使用单播放器重新播放");
        finishedPlayer.time = 0;
        finishedPlayer.Play();

        // 同时尝试重新准备备用播放器
        if (standbyPlayerIndex >= 0)
        {
            VideoPlayer standbyPlayer = videoPlayers[standbyPlayerIndex];
            if (standbyPlayer != null && !playerPrepared[standbyPlayerIndex])
            {
                standbyPlayer.Stop();
                standbyPlayer.Prepare();
            }
        }
    }

    private void OnVideoError(VideoPlayer erroredPlayer, string message)
    {
        int index = GetPlayerIndex(erroredPlayer);
        if (index >= 0)
        {
            playerPrepared[index] = false;
        }

        Debug.LogWarning("[TitleScreen] 开始界面视频播放失败: " + message);
        if (!Application.isPlaying)
        {
            return;
        }

        if (!notifiedVideoPlaybackError)
        {
            notifiedVideoPlaybackError = true;
            ShowTitleNotification("标题背景播放失败", "开始界面视频播放异常，标题页会继续保留可操作状态。", 3f, new Color(0.86f, 0.62f, 0.24f));
        }

        // 如果是备用播放器出错，延迟重试
        if (index == standbyPlayerIndex)
        {
            StartCoroutine(RetryPrepare(index, 1f));
        }
    }

    private IEnumerator RetryPrepare(int playerIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerIndex >= 0 && playerIndex < videoPlayers.Length && videoPlayers[playerIndex] != null)
        {
            Debug.Log($"[TitleScreen] 重试准备 VideoPlayer_{playerIndex}");
            videoPlayers[playerIndex].Prepare();
        }
    }

    private IEnumerator MonitorVideoLoop()
    {
        while (true)
        {
            if (activePlayerIndex >= 0 && standbyPlayerIndex >= 0)
            {
                VideoPlayer activePlayer = videoPlayers[activePlayerIndex];

                if (activePlayer != null && activePlayer.isPlaying && playerPrepared[standbyPlayerIndex])
                {
                    double remainingTime = activePlayer.length - activePlayer.time;
                    if (activePlayer.length > 0d && activePlayer.time > 0d && remainingTime <= loopSwitchLeadTime)
                    {
                        SwitchToPreparedPlayer(standbyPlayerIndex);
                    }
                }

                // 防卡死检测：活跃播放器既没在播放、也不在准备中
                if (activePlayer != null && !activePlayer.isPlaying && !activePlayer.isPrepared)
                {
                    Debug.LogWarning("[TitleScreen] 活跃播放器状态异常，尝试恢复");
                    if (!notifiedVideoRecoveryState)
                    {
                        notifiedVideoRecoveryState = true;
                        ShowTitleNotification("标题背景正在恢复", "背景视频状态异常，系统正在尝试自动恢复播放。", 2.8f, new Color(0.86f, 0.62f, 0.24f));
                    }
                    if (playerPrepared[standbyPlayerIndex])
                    {
                        SwitchToPreparedPlayer(standbyPlayerIndex);
                    }
                    else
                    {
                        // 两个都没准备好，重新准备当前的
                        activePlayer.Prepare();
                    }
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// 防止重入的切换锁
    /// </summary>
    private bool isSwitching = false;

    private void SwitchToPreparedPlayer(int nextPlayerIndex)
    {
        // 防止重入（MonitorVideoLoop 和 OnVideoLoopPointReached 可能同帧触发）
        if (isSwitching)
        {
            return;
        }

        if (nextPlayerIndex < 0 || nextPlayerIndex >= videoPlayers.Length)
        {
            return;
        }

        if (!playerPrepared[nextPlayerIndex])
        {
            return;
        }

        isSwitching = true;

        int previousPlayerIndex = activePlayerIndex;
        VideoPlayer nextPlayer = videoPlayers[nextPlayerIndex];
        if (nextPlayer == null)
        {
            isSwitching = false;
            return;
        }

        Debug.Log($"[TitleScreen] 切换: VideoPlayer_{previousPlayerIndex} -> VideoPlayer_{nextPlayerIndex}");

        videoImage.texture = videoTextures[nextPlayerIndex];
        nextPlayer.time = 0;
        nextPlayer.Play();

        activePlayerIndex = nextPlayerIndex;
        standbyPlayerIndex = previousPlayerIndex;
        playerPrepared[nextPlayerIndex] = false;

        if (previousPlayerIndex >= 0)
        {
            VideoPlayer previousPlayer = videoPlayers[previousPlayerIndex];
            if (previousPlayer != null)
            {
                previousPlayer.Stop();
                playerPrepared[previousPlayerIndex] = false;
                previousPlayer.Prepare();
            }
        }

        isSwitching = false;
    }

    private int GetPlayerIndex(VideoPlayer player)
    {
        for (int i = 0; i < videoPlayers.Length; i++)
        {
            if (videoPlayers[i] == player)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetOtherPlayerIndex(int index)
    {
        return index == 0 ? 1 : 0;
    }

    private void ResolveVideoSource()
    {
        resolvedVideoPath = string.Empty;
        resolvedVideoUrl = string.Empty;

        foreach (string candidate in GetVideoCandidates())
        {
            string absolutePath = Path.Combine(Application.streamingAssetsPath, candidate);
            if (!File.Exists(absolutePath))
            {
                continue;
            }

            resolvedVideoPath = absolutePath;
            resolvedVideoUrl = new Uri(absolutePath).AbsoluteUri;
            Debug.Log("[TitleScreen] Using background video: " + resolvedVideoPath);
            return;
        }

        Debug.LogWarning("[TitleScreen] No menu background video found in StreamingAssets.");
        if (!notifiedMissingMenuVideo)
        {
            notifiedMissingMenuVideo = true;
            ShowTitleNotification("未找到标题背景视频", "StreamingAssets 中没有可用的标题背景视频，系统会继续使用静态界面。", 3f, new Color(0.86f, 0.62f, 0.24f));
        }
    }

    private string[] GetVideoCandidates()
    {
        string fileName = string.IsNullOrWhiteSpace(videoFileName) ? "Start screen.mp4" : videoFileName.Trim();
        string extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            return new[]
            {
                fileName + ".mp4",
                fileName + ".webm"
            };
        }

        string alternateExtension = extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ? ".webm" : ".mp4";
        string alternateFileName = Path.GetFileNameWithoutExtension(fileName) + alternateExtension;
        return fileName.Equals(alternateFileName, StringComparison.OrdinalIgnoreCase)
            ? new[] { fileName }
            : new[] { fileName, alternateFileName };
    }

    #endregion

    #region 动画

    private IEnumerator BreathingAnimation(TMP_Text text)
    {
        float minAlpha = 0.2f;
        float maxAlpha = 0.8f;
        float cycleDuration = 2f;

        Color baseColor = text.color;

        while (true)
        {
            float elapsed = 0f;

            while (elapsed < cycleDuration / 2f)
            {
                float t = elapsed / (cycleDuration / 2f);
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < cycleDuration / 2f)
            {
                float t = elapsed / (cycleDuration / 2f);
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    #endregion

    #region UI 组件工厂

    private void CreateTutorialGridBackground(RectTransform parent)
    {
        for (int i = 1; i < 10; i++)
        {
            RectTransform vertical = CreateUIElement("GridV_" + i, parent).GetComponent<RectTransform>();
            vertical.anchorMin = new Vector2(i / 10f, 0f);
            vertical.anchorMax = new Vector2(i / 10f, 1f);
            vertical.sizeDelta = new Vector2(1f, 0f);
            vertical.anchoredPosition = Vector2.zero;
            vertical.gameObject.AddComponent<Image>().color = TutorialPaperLineColor;
        }

        for (int i = 1; i < 16; i++)
        {
            RectTransform horizontal = CreateUIElement("GridH_" + i, parent).GetComponent<RectTransform>();
            horizontal.anchorMin = new Vector2(0f, i / 16f);
            horizontal.anchorMax = new Vector2(1f, i / 16f);
            horizontal.sizeDelta = new Vector2(0f, 1f);
            horizontal.anchoredPosition = Vector2.zero;
            horizontal.gameObject.AddComponent<Image>().color = TutorialPaperLineColor;
        }
    }

    private Button CreateTutorialCategoryButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = CreateUIElement(label + "Tab", parent);
        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredWidth = 140f;
        layout.preferredHeight = 72f;

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = Color.white;

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateTMPBlock(buttonGO.transform as RectTransform, "Label", label, 28f, TutorialTextColor, TextAlignmentOptions.Center);
        StretchFull(text.rectTransform);
        text.fontStyle = FontStyles.Bold;

        return button;
    }

    private Button CreateTutorialEntryButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = CreateUIElement(label + "Item", parent);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 58f);
        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.preferredHeight = 58f;

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = TutorialListNormalColor;

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = bg;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = TutorialListHoverColor;
        colors.pressedColor = new Color(0.96f, 0.88f, 0.62f, 0.92f);
        colors.selectedColor = colors.pressedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateTMPBlock(rect, "Label", label, 24f, TutorialMutedTextColor, TextAlignmentOptions.Left);
        StretchFull(text.rectTransform);
        text.margin = new Vector4(20f, 0f, 0f, 0f);

        return button;
    }

    private void UpdateTutorialCategoryButtonState(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image bg = button.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = selected ? TutorialHighlightColor : new Color(0.96f, 0.95f, 0.93f, 0.92f);
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.color = selected ? TutorialAccentColor : new Color(0.47f, 0.39f, 0.34f, 0.95f);
        }
    }

    private void UpdateTutorialEntryButtonState(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image bg = button.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = selected ? new Color(0.99f, 0.95f, 0.78f, 0.95f) : TutorialListNormalColor;
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.color = selected ? TutorialAccentColor : TutorialMutedTextColor;
            text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private ScrollRect CreateTutorialScrollView(RectTransform parent, out RectTransform content)
    {
        GameObject scrollGO = CreateUIElement("ScrollView", parent);
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        StretchFull(scrollRect);

        Image bg = scrollGO.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.02f);

        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        RectTransform viewport = CreateUIElement("Viewport", scrollRect).GetComponent<RectTransform>();
        StretchFull(viewport);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        content = CreateUIElement("Content", viewport).GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        scroll.viewport = viewport;
        scroll.content = content;
        return scroll;
    }

    private ScrollRect CreateGalleryScrollView(RectTransform parent, out RectTransform content)
    {
        GameObject scrollGO = CreateUIElement("GalleryScrollView", parent);
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        StretchFull(scrollRect);

        Image bg = scrollGO.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.01f);

        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;

        RectTransform viewport = CreateUIElement("GalleryViewport", scrollRect).GetComponent<RectTransform>();
        StretchFull(viewport);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        content = CreateUIElement("GalleryContent", viewport).GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontalScrollbar = null;
        return scroll;
    }

    private void CreateTutorialPreviewCard(RectTransform parent)
    {
        RectTransform card = CreateUIElement("PreviewCard", parent).GetComponent<RectTransform>();
        card.sizeDelta = new Vector2(0f, 360f);
        LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 360f;

        Image bg = card.gameObject.AddComponent<Image>();
        bg.color = new Color(0.97f, 0.93f, 0.84f, 0.96f);

        RectTransform titleBar = CreateUIElement("CardTitleBar", card).GetComponent<RectTransform>();
        titleBar.anchorMin = new Vector2(0f, 1f);
        titleBar.anchorMax = new Vector2(1f, 1f);
        titleBar.pivot = new Vector2(0.5f, 1f);
        titleBar.sizeDelta = new Vector2(0f, 56f);
        titleBar.anchoredPosition = Vector2.zero;
        titleBar.gameObject.AddComponent<Image>().color = new Color(0.97f, 0.89f, 0.58f, 1f);

        TextMeshProUGUI cardTitle = CreateTMPBlock(titleBar, "CardTitle", "教程提示", 24f, TutorialAccentColor, TextAlignmentOptions.Left);
        StretchFull(cardTitle.rectTransform);
        cardTitle.margin = new Vector4(22f, 6f, 0f, 0f);
        cardTitle.fontStyle = FontStyles.Bold;

        RectTransform barsRoot = CreateUIElement("BarsRoot", card).GetComponent<RectTransform>();
        barsRoot.anchorMin = new Vector2(0.06f, 0.48f);
        barsRoot.anchorMax = new Vector2(0.56f, 0.83f);
        barsRoot.offsetMin = Vector2.zero;
        barsRoot.offsetMax = Vector2.zero;
        CreateTutorialStatRow(barsRoot, "学业成长", new Color(0.26f, 0.57f, 0.92f, 1f), 0.74f, 0);
        CreateTutorialStatRow(barsRoot, "社交推进", new Color(0.96f, 0.63f, 0.25f, 1f), 0.42f, 1);
        CreateTutorialStatRow(barsRoot, "体能稳定", new Color(0.44f, 0.82f, 0.3f, 1f), 0.58f, 2);

        RectTransform miniCard = CreateUIElement("MiniCard", card).GetComponent<RectTransform>();
        miniCard.anchorMin = new Vector2(0.6f, 0.42f);
        miniCard.anchorMax = new Vector2(0.94f, 0.8f);
        miniCard.offsetMin = Vector2.zero;
        miniCard.offsetMax = Vector2.zero;
        miniCard.gameObject.AddComponent<Image>().color = new Color(0.995f, 0.97f, 0.9f, 1f);

        RectTransform miniTitle = CreateUIElement("MiniTitle", miniCard).GetComponent<RectTransform>();
        miniTitle.anchorMin = new Vector2(0f, 1f);
        miniTitle.anchorMax = new Vector2(1f, 1f);
        miniTitle.pivot = new Vector2(0.5f, 1f);
        miniTitle.sizeDelta = new Vector2(0f, 48f);
        miniTitle.anchoredPosition = Vector2.zero;
        miniTitle.gameObject.AddComponent<Image>().color = new Color(0.99f, 0.92f, 0.68f, 1f);

        TextMeshProUGUI miniTitleText = CreateTMPBlock(miniTitle, "Label", "关键信息", 22f, TutorialAccentColor, TextAlignmentOptions.Left);
        StretchFull(miniTitleText.rectTransform);
        miniTitleText.margin = new Vector4(18f, 6f, 0f, 0f);
        miniTitleText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI line1 = CreateTMPBlock(miniCard, "Line1", "先做主线，再补短板", 21f, TutorialMutedTextColor, TextAlignmentOptions.Left);
        line1.rectTransform.anchorMin = new Vector2(0f, 1f);
        line1.rectTransform.anchorMax = new Vector2(1f, 1f);
        line1.rectTransform.pivot = new Vector2(0.5f, 1f);
        line1.rectTransform.offsetMin = new Vector2(16f, -114f);
        line1.rectTransform.offsetMax = new Vector2(-16f, -78f);

        TextMeshProUGUI line2 = CreateTMPBlock(miniCard, "Line2", "保持状态平衡，别让压力失控", 21f, TutorialMutedTextColor, TextAlignmentOptions.Left);
        line2.rectTransform.anchorMin = new Vector2(0f, 1f);
        line2.rectTransform.anchorMax = new Vector2(1f, 1f);
        line2.rectTransform.pivot = new Vector2(0.5f, 1f);
        line2.rectTransform.offsetMin = new Vector2(16f, -158f);
        line2.rectTransform.offsetMax = new Vector2(-16f, -122f);

        RectTransform tagRow = CreateUIElement("TagRow", card).GetComponent<RectTransform>();
        tagRow.anchorMin = new Vector2(0.05f, 0.06f);
        tagRow.anchorMax = new Vector2(0.95f, 0.24f);
        tagRow.offsetMin = Vector2.zero;
        tagRow.offsetMax = Vector2.zero;
        HorizontalLayoutGroup tagLayout = tagRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tagLayout.spacing = 12f;
        tagLayout.childAlignment = TextAnchor.MiddleLeft;
        tagLayout.childControlWidth = false;
        tagLayout.childControlHeight = true;
        tagLayout.childForceExpandWidth = false;
        tagLayout.childForceExpandHeight = false;
    }

    private void CreateTutorialStatRow(RectTransform parent, string label, Color fillColor, float fillAmount, int rowIndex)
    {
        RectTransform row = CreateUIElement(label + "Row", parent).GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = new Vector2(0f, 58f);
        row.anchoredPosition = new Vector2(0f, -rowIndex * 74f);

        TextMeshProUGUI name = CreateTMPBlock(row, "Label", label, 21f, TutorialAccentColor, TextAlignmentOptions.Left);
        name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        name.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        name.rectTransform.pivot = new Vector2(0f, 0.5f);
        name.rectTransform.sizeDelta = new Vector2(140f, 34f);
        name.rectTransform.anchoredPosition = new Vector2(0f, 0f);

        RectTransform barBg = CreateUIElement("BarBg", row).GetComponent<RectTransform>();
        barBg.anchorMin = new Vector2(0f, 0.5f);
        barBg.anchorMax = new Vector2(1f, 0.5f);
        barBg.offsetMin = new Vector2(150f, -12f);
        barBg.offsetMax = new Vector2(-8f, 12f);
        barBg.gameObject.AddComponent<Image>().color = new Color(0.82f, 0.76f, 0.67f, 0.45f);

        RectTransform fill = CreateUIElement("Fill", barBg).GetComponent<RectTransform>();
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(fillAmount, 1f);
        fill.offsetMin = new Vector2(4f, 4f);
        fill.offsetMax = new Vector2(-4f, -4f);
        fill.gameObject.AddComponent<Image>().color = fillColor;
    }

    private void CreateTutorialTag(RectTransform parent, string text)
    {
        GameObject tag = CreateUIElement(text + "Tag", parent);
        LayoutElement layout = tag.AddComponent<LayoutElement>();
        layout.preferredWidth = Mathf.Max(120f, text.Length * 26f);
        layout.preferredHeight = 40f;
        Image bg = tag.AddComponent<Image>();
        bg.color = new Color(0.99f, 0.91f, 0.62f, 0.98f);

        TextMeshProUGUI label = CreateTMPBlock(tag.transform as RectTransform, "Label", text, 18f, TutorialAccentColor, TextAlignmentOptions.Center);
        StretchFull(label.rectTransform);
        label.fontStyle = FontStyles.Bold;
    }

    private Button CreateTutorialCloseButton(RectTransform parent, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = CreateUIElement("TutorialCloseButton", parent);
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(72f, 72f);
        rect.anchoredPosition = new Vector2(16f, 14f);

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0.85f, 0.68f, 0.5f, 0.12f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI label = CreateTMPBlock(rect, "Label", "×", 52f, Color.white, TextAlignmentOptions.Center);
        StretchFull(label.rectTransform);

        return button;
    }

    private TextMeshProUGUI CreateTMPBlock(RectTransform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIElement(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        TMP_FontAsset chineseFont = GetAvailableChineseFont();
        if (chineseFont != null)
        {
            tmp.font = chineseFont;
        }

        return tmp;
    }

    private TextMeshProUGUI CreateTMPLayoutItem(RectTransform parent, string name, string text, float fontSize, Color color, FontStyles style, float preferredHeight)
    {
        TextMeshProUGUI tmp = CreateTMPBlock(parent, name, text, fontSize, color, TextAlignmentOptions.TopLeft);
        tmp.fontStyle = style;
        RectTransform rect = tmp.rectTransform;
        rect.sizeDelta = new Vector2(0f, preferredHeight);
        LayoutElement layout = tmp.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        return tmp;
    }

    private void SetAnchoredBox(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private GameObject CreatePanel(string name, RectTransform parent, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject panelGO = CreateUIElement(name, parent);
        RectTransform rect = panelGO.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = panelGO.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        return panelGO;
    }

    private TextMeshProUGUI CreatePanelTitle(RectTransform parent, string title, int fontSize, Vector2 anchoredPosition)
    {
        GameObject titleGO = CreateUIElement(title + "Title", parent);
        RectTransform rt = titleGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 80f);
        rt.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = titleGO.AddComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;
        text.raycastTarget = false;
        return text;
    }

    private void CreatePanelSubtitle(RectTransform parent, string content, Vector2 anchoredPosition)
    {
        GameObject subtitleGO = CreateUIElement("Subtitle", parent);
        RectTransform rt = subtitleGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 52f);
        rt.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = subtitleGO.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(textColor.r, textColor.g, textColor.b, 0.72f);
        text.raycastTarget = false;
    }

    private void CreateFooterText(RectTransform parent, string content, Vector2 anchoredPosition)
    {
        GameObject footerGO = CreateUIElement("FooterText", parent);
        RectTransform rt = footerGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500f, 60f);
        rt.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = footerGO.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(textColor.r, textColor.g, textColor.b, 0.55f);
        text.raycastTarget = false;
    }

    private Button CreateMenuButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = CreateUIElement(name, parent);
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360f, 64f);
        rt.anchoredPosition = anchoredPosition;

        Image image = buttonGO.AddComponent<Image>();
        image.color = color;

        Button button = buttonGO.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        GameObject textGO = CreateUIElement("Label", rt);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 28;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }

    private Slider CreateLabeledSlider(RectTransform parent, string label, Vector2 anchoredPosition)
    {
        GameObject root = CreateUIElement(label + "Row", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(540f, 84f);
        rootRect.anchoredPosition = anchoredPosition;

        GameObject labelGO = CreateUIElement("Label", rootRect);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(170f, 48f);
        labelRect.anchoredPosition = new Vector2(-250f, 0f);

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 26;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = textColor;
        labelText.raycastTarget = false;

        GameObject sliderGO = CreateUIElement("Slider", rootRect);
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(300f, 24f);
        sliderRect.anchoredPosition = new Vector2(95f, 0f);

        Image background = sliderGO.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.18f);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.7f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.targetGraphic = background;

        GameObject fillAreaGO = CreateUIElement("Fill Area", sliderRect);
        RectTransform fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(10f, 4f);
        fillAreaRect.offsetMax = new Vector2(-10f, -4f);

        GameObject fillGO = CreateUIElement("Fill", fillAreaRect);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        StretchFull(fillRect);
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = primaryColor;

        GameObject handleAreaGO = CreateUIElement("Handle Area", sliderRect);
        RectTransform handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        StretchFull(handleAreaRect);

        GameObject handleGO = CreateUIElement("Handle", handleAreaRect);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(26f, 26f);
        Image handleImage = handleGO.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;

        return slider;
    }

    private Toggle CreateLabeledToggle(RectTransform parent, string label, Vector2 anchoredPosition)
    {
        GameObject root = CreateUIElement(label + "Row", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(540f, 72f);
        rootRect.anchoredPosition = anchoredPosition;

        GameObject labelGO = CreateUIElement("Label", rootRect);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(200f, 48f);
        labelRect.anchoredPosition = new Vector2(-250f, 0f);

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 26;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = textColor;
        labelText.raycastTarget = false;

        GameObject toggleGO = CreateUIElement("Toggle", rootRect);
        RectTransform toggleRect = toggleGO.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.sizeDelta = new Vector2(44f, 44f);
        toggleRect.anchoredPosition = new Vector2(218f, 0f);

        Image background = toggleGO.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.16f);

        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = background;

        GameObject checkmarkGO = CreateUIElement("Checkmark", toggleRect);
        RectTransform checkRect = checkmarkGO.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(24f, 24f);
        checkRect.anchoredPosition = Vector2.zero;

        Image checkImage = checkmarkGO.AddComponent<Image>();
        checkImage.color = secondaryColor;
        toggle.graphic = checkImage;

        return toggle;
    }

    #endregion

    #region 工具方法

    private GameObject CreateUIElement(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = go.AddComponent<RectTransform>();
        }

        return go;
    }

    private TMP_FontAsset GetAvailableChineseFont()
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            return FontManager.Instance.ChineseFont;
        }

        FontManager[] fontManagers = Resources.FindObjectsOfTypeAll<FontManager>();
        FontManager sceneFontManager = fontManagers != null && fontManagers.Length > 0 ? fontManagers[0] : null;
        if (sceneFontManager != null)
        {
            if (sceneFontManager.chineseFontAsset != null)
            {
                return sceneFontManager.chineseFontAsset;
            }

            TMP_FontAsset[] fonts = Resources.LoadAll<TMP_FontAsset>(sceneFontManager.resourceFontPath);
            if (fonts != null && fonts.Length > 0)
            {
                return fonts[0];
            }
        }

        TMP_FontAsset[] resourceFonts = Resources.LoadAll<TMP_FontAsset>("Fonts");
        if (resourceFonts != null && resourceFonts.Length > 0)
        {
            return resourceFonts[0];
        }

        return null;
    }

    private static void DestroyRuntimeOrEditor(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obj);
            return;
        }

#if UNITY_EDITOR
        DestroyImmediate(obj);
#else
        Destroy(obj);
#endif
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void ShowTitleNotification(string title, string message, float duration = 2.8f, Color? color = null)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, color ?? new Color(0.82f, 0.38f, 0.30f), duration);
            return;
        }

        EnsureTitleNotificationUI();

        if (titleNotificationRoot == null || titleNotificationBg == null || titleNotificationTitleText == null || titleNotificationMessageText == null)
        {
            return;
        }

        Color resolvedColor = color ?? new Color(0.82f, 0.38f, 0.30f);
        titleNotificationBg.color = new Color(resolvedColor.r, resolvedColor.g, resolvedColor.b, 0.92f);
        titleNotificationTitleText.text = string.IsNullOrWhiteSpace(title) ? "系统提示" : title;
        titleNotificationMessageText.text = string.IsNullOrWhiteSpace(message) ? "当前操作没有成功完成。" : message;

        if (titleNotificationCoroutine != null)
        {
            StopCoroutine(titleNotificationCoroutine);
        }

        titleNotificationCoroutine = StartCoroutine(ShowTitleNotificationCoroutine(duration));
    }

    private void EnsureTitleNotificationUI()
    {
        if (canvasRect == null || titleNotificationRoot != null)
        {
            return;
        }

        titleNotificationRoot = CreateUIElement("TitleNotification", canvasRect);
        titleNotificationRoot.transform.SetAsLastSibling();

        RectTransform rootRT = titleNotificationRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 1f);
        rootRT.anchorMax = new Vector2(0.5f, 1f);
        rootRT.pivot = new Vector2(0.5f, 1f);
        rootRT.sizeDelta = new Vector2(560f, 110f);
        rootRT.anchoredPosition = new Vector2(0f, -26f);

        titleNotificationBg = titleNotificationRoot.AddComponent<Image>();
        titleNotificationBg.color = new Color(0.82f, 0.38f, 0.30f, 0f);
        titleNotificationBg.raycastTarget = false;

        CanvasGroup group = titleNotificationRoot.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject titleGO = CreateUIElement("NotificationTitle", rootRT);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.offsetMin = new Vector2(22f, -44f);
        titleRT.offsetMax = new Vector2(-22f, -10f);

        titleNotificationTitleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleNotificationTitleText.fontSize = 24f;
        titleNotificationTitleText.fontStyle = FontStyles.Bold;
        titleNotificationTitleText.alignment = TextAlignmentOptions.Left;
        titleNotificationTitleText.color = Color.white;
        titleNotificationTitleText.raycastTarget = false;
        TMP_FontAsset chineseFont = GetAvailableChineseFont();
        if (chineseFont != null)
        {
            titleNotificationTitleText.font = chineseFont;
        }

        GameObject messageGO = CreateUIElement("NotificationMessage", rootRT);
        RectTransform messageRT = messageGO.GetComponent<RectTransform>();
        messageRT.anchorMin = new Vector2(0f, 0f);
        messageRT.anchorMax = new Vector2(1f, 1f);
        messageRT.pivot = new Vector2(0.5f, 0.5f);
        messageRT.offsetMin = new Vector2(22f, 14f);
        messageRT.offsetMax = new Vector2(-22f, -46f);

        titleNotificationMessageText = messageGO.AddComponent<TextMeshProUGUI>();
        titleNotificationMessageText.fontSize = 19f;
        titleNotificationMessageText.alignment = TextAlignmentOptions.TopLeft;
        titleNotificationMessageText.enableWordWrapping = true;
        titleNotificationMessageText.color = new Color(1f, 1f, 1f, 0.95f);
        titleNotificationMessageText.raycastTarget = false;
        TMP_FontAsset chineseFontMessage = GetAvailableChineseFont();
        if (chineseFontMessage != null)
        {
            titleNotificationMessageText.font = chineseFontMessage;
        }
    }

    private IEnumerator ShowTitleNotificationCoroutine(float duration)
    {
        if (titleNotificationRoot == null)
        {
            yield break;
        }

        CanvasGroup group = titleNotificationRoot.GetComponent<CanvasGroup>();
        if (group == null)
        {
            yield break;
        }

        titleNotificationRoot.SetActive(true);

        float fadeIn = 0.18f;
        float fadeOut = 0.22f;
        float elapsed = 0f;

        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Clamp01(elapsed / fadeIn);
            yield return null;
        }

        group.alpha = 1f;
        yield return new WaitForSeconds(Mathf.Max(0.6f, duration));

        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            group.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
            yield return null;
        }

        group.alpha = 0f;
        titleNotificationCoroutine = null;
    }

#if UNITY_EDITOR
    public bool EditorPreviewIsBuilt()
    {
        return canvas != null && menuOverlay != null && logoAreaRect != null && mainMenuPanelRect != null;
    }

    public void EditorEnsureLivePreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (!EditorPreviewIsBuilt())
        {
            EditorBuildLivePreview();
            return;
        }

        EditorSyncLivePreview();
    }

    public void EditorBuildLivePreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EditorClearLivePreview();
        ReloadAuthoredTitleContent();
        ResolveVideoSource();
        CacheColors();
        BuildUI();
        ApplyAuthoredTitleContentToPanels();
        EditorShowMenuImmediately();
        PrepareVideoBackground();
        EditorUtility.SetDirty(this);
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void EditorSyncLivePreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        ReloadAuthoredTitleContent();
        ApplyAuthoredTitleContentToPanels();
        EditorShowMenuImmediately();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void EditorApplyHomepageLayoutPreview()
    {
        if (Application.isPlaying || !EditorPreviewIsBuilt())
        {
            return;
        }

        ReloadAuthoredTitleContent();
        ApplyHomepageLayoutOverrides();
        ApplyHomepageMenuLabels();
        ApplyTopIconLabels();
        EditorShowMenuImmediately();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public RectTransform EditorGetHomepageLayoutRect(string key)
    {
        switch (key)
        {
            case ZhongshanDeckTitleContentDefaults.LayoutLogo:
                return logoAreaRect;
            case ZhongshanDeckTitleContentDefaults.LayoutMainMenu:
                return mainMenuPanelRect;
            case ZhongshanDeckTitleContentDefaults.LayoutTopIcons:
                return topRightIconsRoot;
            case ZhongshanDeckTitleContentDefaults.LayoutChangelog:
                return changelogButtonRect;
            case ZhongshanDeckTitleContentDefaults.LayoutHint:
                return hintTextRect;
            case ZhongshanDeckTitleContentDefaults.LayoutVersion:
                return versionTextRect;
            default:
                return null;
        }
    }

    public static Vector2 EditorGetDefaultHomepageLayoutSize(string key)
    {
        return GetDefaultHomepageLayoutSize(key);
    }

    public static bool EditorUsesScaledLayout(string key)
    {
        return string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutMainMenu, StringComparison.Ordinal) ||
               string.Equals(key, ZhongshanDeckTitleContentDefaults.LayoutTopIcons, StringComparison.Ordinal);
    }

    public void EditorSetPreviewVisible(bool visible)
    {
        if (Application.isPlaying || canvas == null)
        {
            return;
        }

        canvas.gameObject.SetActive(visible);
        if (menuOverlay != null)
        {
            menuOverlay.gameObject.SetActive(visible);
        }
    }

    private void EditorShowMenuImmediately()
    {
        hasEnteredMenu = true;
        transitionRequested = false;

        if (menuOverlay != null)
        {
            menuOverlay.gameObject.SetActive(true);
            menuOverlay.alpha = 1f;
            menuOverlay.interactable = false;
            menuOverlay.blocksRaycasts = false;
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (galleryPanel != null) galleryPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (changelogPanel != null) changelogPanel.SetActive(false);
        if (galleryViewerPanel != null) galleryViewerPanel.SetActive(false);
        if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
    }

    public void EditorClearLivePreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        for (int i = 0; i < videoPlayers.Length; i++)
        {
            if (videoPlayers[i] != null)
            {
                videoPlayers[i].prepareCompleted -= OnVideoPrepared;
                videoPlayers[i].loopPointReached -= OnVideoLoopPointReached;
                videoPlayers[i].errorReceived -= OnVideoError;
                if (videoPlayers[i].gameObject != null)
                {
                    DestroyImmediate(videoPlayers[i].gameObject);
                }
                videoPlayers[i] = null;
            }

            if (videoTextures[i] != null)
            {
                videoTextures[i].Release();
                DestroyImmediate(videoTextures[i]);
                videoTextures[i] = null;
            }

            playerPrepared[i] = false;
        }

        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            DestroyImmediate(transform.GetChild(childIndex).gameObject);
        }

        canvas = null;
        canvasRect = null;
        fadeOverlay = null;
        hintText = null;
        hintTextRect = null;
        videoImage = null;
        loopMonitorCoroutine = null;
        activePlayerIndex = -1;
        standbyPlayerIndex = -1;
        titleNotificationRoot = null;
        titleNotificationBg = null;
        titleNotificationTitleText = null;
        titleNotificationMessageText = null;
        titleNotificationCoroutine = null;
        menuOverlay = null;
        mainMenuPanel = null;
        settingsPanel = null;
        continueGameButton = null;
        startGameButton = null;
        settingsButton = null;
        quitGameButton = null;
        backButton = null;
        musicVolumeSlider = null;
        sfxVolumeSlider = null;
        fullscreenToggle = null;
        tutorialButton = null;
        achievementButton = null;
        cgButton = null;
        creditsButton = null;
        topRightIconsRoot = null;
        topRightIconTexts.Clear();
        topRightIconTooltips.Clear();
        creditsPanel = null;
        creditsPanelGroup = null;
        creditsListContent = null;
        creditsTagsRoot = null;
        creditsDetailTitle = null;
        creditsDetailRole = null;
        creditsDetailDescription = null;
        creditsPanelTitleText = null;
        creditsTabText = null;
        creditsFooterText = null;
        creditsEntryButtons.Clear();
        tutorialPanel = null;
        tutorialPanelGroup = null;
        tutorialItemListContent = null;
        tutorialDetailContent = null;
        tutorialTabsRoot = null;
        tutorialSectionTitle = null;
        tutorialEntryTitle = null;
        tutorialEntryDescription = null;
        tutorialCategoryButtons.Clear();
        tutorialEntryButtons.Clear();
        changelogPanel = null;
        changelogPanelGroup = null;
        changelogContent = null;
        changelogButton = null;
        changelogButtonRect = null;
        changelogButtonLabelText = null;
        changelogPanelTitleText = null;
        galleryPanel = null;
        galleryPanelGroup = null;
        galleryGridContent = null;
        galleryPrevPageButton = null;
        galleryNextPageButton = null;
        galleryPageText = null;
        galleryGridStatusText = null;
        galleryGenderMaleButton = null;
        galleryGenderFemaleButton = null;
        gallerySectionTitle = null;
        galleryPreviewTitle = null;
        galleryPreviewSubtitle = null;
        galleryPreviewDescription = null;
        galleryCounterText = null;
        galleryPreviewImage = null;
        galleryPreviewImageLabel = null;
        galleryOpenButton = null;
        galleryTabButtons.Clear();
        galleryEntryButtons.Clear();
        galleryViewerPanel = null;
        galleryViewerGroup = null;
        galleryViewerImage = null;
        galleryViewerImageLabel = null;
        galleryViewerTitle = null;
        galleryViewerSubtitle = null;
        galleryViewerDescription = null;
        versionText = null;
        versionTextRect = null;
        logoAreaRect = null;
        mainMenuPanelRect = null;
        settingsTitleText = null;
        settingsBackButtonLabelText = null;
        mainMenuButtonsByAction.Clear();
        breathCoroutine = null;
        hasEnteredMenu = false;
        transitionRequested = false;
    }
#endif

    #endregion
}
