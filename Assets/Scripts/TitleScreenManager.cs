using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using TMPro;

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

    // ===== 运行时引用 =====
    private Canvas canvas;
    private RectTransform canvasRect;
    private CanvasGroup fadeOverlay;
    private TMP_Text hintText;
    private RawImage videoImage;
    private readonly VideoPlayer[] videoPlayers = new VideoPlayer[2];
    private readonly RenderTexture[] videoTextures = new RenderTexture[2];
    private readonly bool[] playerPrepared = new bool[2];
    private Coroutine loopMonitorCoroutine;
    private int activePlayerIndex = -1;
    private int standbyPlayerIndex = -1;
    private bool isTransitioning = false;
    private bool hasEnteredMenu = false;
    private string resolvedVideoPath;

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

    // ===== 版本号 =====
    private TMP_Text versionText;

    // ===== 颜色缓存 =====
    private Color bgColor;
    private Color textColor;
    private Color primaryColor;
    private Color secondaryColor;
    private Color panelColor;
    private Color subPanelColor;

    // ===== 呼吸动画协程 =====
    private Coroutine breathCoroutine;

    #region 生命周期

    private void Awake()
    {
        resolvedVideoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName).Replace("\\", "/");
        CacheColors();
        BuildUI();
    }

    private void Start()
    {
        PrepareVideoBackground();

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
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            hintText.font = FontManager.Instance.ChineseFont;
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
        CreateTopRightIcons(overlayGO.transform as RectTransform);
        CreateLogoArea(overlayGO.transform as RectTransform);
        CreateVersionText(overlayGO.transform as RectTransform);
        CreateSettingsPanel(overlayGO.transform as RectTransform);

        overlayGO.SetActive(false);
    }

    /// <summary>
    /// 中央 Logo 区域 — 上半部分放游戏标题图
    /// </summary>
    private void CreateLogoArea(RectTransform parent)
    {
        GameObject logoGO = CreateUIElement("LogoArea", parent);
        RectTransform rt = logoGO.GetComponent<RectTransform>();
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

    /// <summary>
    /// 右上角四个图标按钮：游戏教程 / 成就 / 游戏CG / 制作人详情
    /// </summary>
    private void CreateTopRightIcons(RectTransform parent)
    {
        string[] labels = { "教程", "成就", "CG", "制作人" };
        string[] tooltips = { "游戏教程", "成就", "游戏CG", "制作人详情" };
        float iconSize = 56f;
        float spacing = 12f;
        float totalWidth = labels.Length * iconSize + (labels.Length - 1) * spacing;
        float startX = -(totalWidth / 2f) + iconSize / 2f;

        GameObject iconsRoot = CreateUIElement("TopRightIcons", parent);
        RectTransform rootRT = iconsRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(1f, 1f);
        rootRT.anchorMax = new Vector2(1f, 1f);
        rootRT.pivot = new Vector2(1f, 1f);
        rootRT.sizeDelta = new Vector2(totalWidth + 32f, iconSize + 24f);
        rootRT.anchoredPosition = new Vector2(-24f, -20f);

        for (int i = 0; i < labels.Length; i++)
        {
            int idx = i;
            float xOffset = startX + i * (iconSize + spacing);

            GameObject iconGO = CreateUIElement(labels[i] + "Btn", rootRT);
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
            txt.text = labels[i];
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
            btn.onClick.AddListener(() => OnTopIconClicked(captured, tooltips[captured]));

            // 保存引用
            if (idx == 0) tutorialButton = btn;
            else if (idx == 1) achievementButton = btn;
            else if (idx == 2) cgButton = btn;
            else creditsButton = btn;
        }
    }

    private void OnTopIconClicked(int index, string name)
    {
        switch (index)
        {
            case 1: // 成就回顾
                // 确保 AchievementUI 实例存在
                if (AchievementUI.Instance == null)
                {
                    GameObject obj = new GameObject("AchievementUI");
                    obj.AddComponent<AchievementUI>();
                }
                AchievementUI.Instance.ShowReviewPanel();
                return;
            default:
                Debug.Log($"[TitleScreen] 点击右上角：{name}（功能待接入）");
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
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 480f);
        panelRect.anchoredPosition = new Vector2(0f, -100f);

        // 按钮列表：文字、回调
        var menuItems = new System.Collections.Generic.List<(string label, UnityEngine.Events.UnityAction action)>
        {
            ("继续游戏",  ContinueGame),
            ("开始游戏",  StartGame),
            ("载入游戏",  OnLoadGame),
            ("设  置",    OpenSettings),
            ("退出游戏",  QuitGame),
        };

        float btnHeight = 62f;
        float gap = 10f;
        float totalH = menuItems.Count * btnHeight + (menuItems.Count - 1) * gap;
        float startY = totalH / 2f - btnHeight / 2f;

        for (int i = 0; i < menuItems.Count; i++)
        {
            var item = menuItems[i];
            float y = startY - i * (btnHeight + gap);
            bool isPrimary = (i == 0 || i == 1); // 继续/开始 高亮
            CreateTextMenuButton(panelRect, item.label, new Vector2(0f, y), item.action, isPrimary);
        }
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

        // TMP 描边（Outline）— 通过 materialForRendering 设置
        txt.outlineWidth = 0.2f;
        txt.outlineColor = new Color32(0, 0, 0, 180);

        // TMP 字体材质阴影（Underlay）— 实例化材质避免共享污染
        txt.fontMaterial = new Material(txt.fontMaterial);
        txt.fontMaterial.EnableKeyword("UNDERLAY_ON");
        txt.fontMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.6f));
        txt.fontMaterial.SetFloat("_UnderlayOffsetX", 0.8f);
        txt.fontMaterial.SetFloat("_UnderlayOffsetY", -0.8f);
        txt.fontMaterial.SetFloat("_UnderlayDilate", 0.2f);
        txt.fontMaterial.SetFloat("_UnderlaySoftness", 0.15f);

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

    private void OnLoadGame()
    {
        SaveLoadUI.Show(false);
    }

    private void CreateSettingsPanel(RectTransform parent)
    {
        settingsPanel = CreatePanel("SettingsPanel", parent, new Vector2(0.5f, 0.5f), new Vector2(700f, 620f), subPanelColor);
        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        panelRect.anchoredPosition = new Vector2(0f, -10f);

        CreatePanelTitle(settingsPanel.transform as RectTransform, "设置", 42, new Vector2(0f, 235f));

        musicVolumeSlider = CreateLabeledSlider(settingsPanel.transform as RectTransform, "音乐音量", new Vector2(0f, 110f));
        sfxVolumeSlider = CreateLabeledSlider(settingsPanel.transform as RectTransform, "音效音量", new Vector2(0f, 5f));
        fullscreenToggle = CreateLabeledToggle(settingsPanel.transform as RectTransform, "全屏显示", new Vector2(0f, -105f));

        backButton = CreateMenuButton(settingsPanel.transform as RectTransform, "BackButton", "返回", new Vector2(0f, -225f), primaryColor, BackToMainMenu);

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

        menuOverlay.gameObject.SetActive(true);
        menuOverlay.alpha = 0f;
        menuOverlay.interactable = false;
        menuOverlay.blocksRaycasts = false;

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

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
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
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

        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData(0))
        {
            SaveData data = SaveManager.Instance.LoadFromSlot(0);
            if (data != null)
            {
                SaveManager.PendingLoadData = data;
                Debug.Log("[TitleScreen] 已加载自动存档，准备进入游戏");
                StartGame();
            }
            else
            {
                Debug.LogWarning("[TitleScreen] 自动存档读取失败");
            }
        }
        else
        {
            Debug.Log("[TitleScreen] 无自动存档");
        }
    }

    public void StartGame()
    {
        StartCoroutine(TransitionToGameScene());
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

    private IEnumerator TransitionToGameScene()
    {
        menuOverlay.interactable = false;
        menuOverlay.blocksRaycasts = false;

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
        SceneLoader.LoadScene(gameSceneName);
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
        player.url = resolvedVideoPath;
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

    private void CreatePanelTitle(RectTransform parent, string title, int fontSize, Vector2 anchoredPosition)
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

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    #endregion
}
