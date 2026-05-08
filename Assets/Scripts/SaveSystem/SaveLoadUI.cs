using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 存档/读档 UI —— 春季主题手账风格，2x2 四格存档位布局
/// 所有 UI 通过代码动态创建，设计参考春季主题素材图。
/// </summary>
public class SaveLoadUI : MonoBehaviour
{
    // ========== 常量 ==========

    private const int SlotCount = 4; // 0=auto, 1-3=manual
    private const int CanvasSortOrder = 350;

    // ========== 春季主题色彩体系 ==========

    // 木框 / 底板
    private static readonly Color WoodFrameBrown  = new Color(0.55f, 0.38f, 0.18f, 1f);
    private static readonly Color WoodInnerBeige  = new Color(0.96f, 0.94f, 0.88f, 1f);

    // 卡片底色
    private static readonly Color CardBeige       = new Color(1.00f, 0.97f, 0.90f, 0.95f);
    private static readonly Color CardLineColor   = new Color(0.85f, 0.82f, 0.75f, 0.50f);

    // 各槽位主题色
    private static readonly Color AutoPink        = new Color(0.96f, 0.63f, 0.69f); // 自动存档粉色
    private static readonly Color Slot01Pink      = new Color(0.96f, 0.63f, 0.69f); // 存档01粉色
    private static readonly Color Slot02Green     = new Color(0.63f, 0.78f, 0.63f); // 存档02嫩绿色
    private static readonly Color Slot03Yellow    = new Color(0.94f, 0.85f, 0.56f); // 空存档暖黄色

    // 和纸胶带
    private static readonly Color TapePink        = new Color(0.97f, 0.78f, 0.83f, 0.85f);
    private static readonly Color TapeGreen       = new Color(0.72f, 0.85f, 0.72f, 0.85f);
    private static readonly Color TapeYellow      = new Color(0.97f, 0.91f, 0.75f, 0.85f);

    // 按钮
    private static readonly Color ButtonPink      = new Color(0.94f, 0.65f, 0.71f);
    private static readonly Color ButtonGreen     = new Color(0.60f, 0.76f, 0.60f);
    private static readonly Color ButtonYellow    = new Color(0.92f, 0.82f, 0.55f);
    private static readonly Color ButtonReturn    = new Color(0.85f, 0.70f, 0.60f);
    private static readonly Color ButtonReturnBg  = new Color(1.00f, 0.95f, 0.85f);

    // 文字
    private static readonly Color TextBrown       = new Color(0.36f, 0.24f, 0.13f);
    private static readonly Color TextBrownLight  = new Color(0.55f, 0.40f, 0.25f);
    private static readonly Color TextWhite       = new Color(0.98f, 0.98f, 0.98f);
    private static readonly Color TextGray        = new Color(0.60f, 0.55f, 0.48f);

    // 照片占位
    private static readonly Color PhotoFrameWhite = new Color(0.96f, 0.96f, 0.94f);
    private static readonly Color PhotoInnerGray  = new Color(0.88f, 0.86f, 0.82f);

    // 遮罩
    private static readonly Color OverlayDim      = new Color(0f, 0f, 0f, 0.55f);

    // 装饰色
    private static readonly Color CherryPink      = new Color(0.98f, 0.75f, 0.80f);
    private static readonly Color LeafGreen       = new Color(0.55f, 0.75f, 0.50f);
    private static readonly Color SwallowBlack    = new Color(0.15f, 0.15f, 0.18f);

    // ========== 布局常量 (1920x1080 参考) ==========

    private const float PanelMaxWidth = 1520f;
    private const float PanelMaxHeight = 900f;
    private const float SpringDesignWidth = 2755f;
    private const float SpringDesignHeight = 1537f;
    private const string SpringResourceFolder = "UI/SaveLoadSpring";
    private const float SlotWidth = 470f;
    private const float SlotHeight = 210f;
    private const float PhotoWidth = 188f;
    private const float PhotoHeight = 120f;
    private const float PhotoFrameWidth = 202f;
    private const float PhotoFrameHeight = 136f;
    private const float ActionButtonWidth = 120f;
    private const float ActionButtonHeight = 42f;

    // ========== 运行时状态 ==========

    private bool isSaveMode;
    private bool isSaving;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Sprite springBoardSprite;
    private GameObject confirmDialog;
    private Button confirmDialogConfirmButton;
    private Action confirmDialogConfirmAction;
    private Action confirmDialogCancelAction;
    private readonly List<PendingPreviewRequest> pendingPreviewRequests = new List<PendingPreviewRequest>();
    private readonly List<UnityEngine.Object> runtimeThumbnailObjects = new List<UnityEngine.Object>();
    private readonly Dictionary<string, RectTransform> editorLayoutRects = new Dictionary<string, RectTransform>(StringComparer.Ordinal);
    private readonly Dictionary<int, SaveData> editorPreviewSlotData = new Dictionary<int, SaveData>();
    private ZhongshanDeckSaveLoadContent layoutContent;
    private RectTransform mainPanelRect;
    private RectTransform topOverlayRect;
    private RectTransform prevPageButtonRect;
    private RectTransform nextPageButtonRect;
    private RectTransform returnButtonRect;
    private bool useEditorPreviewData;
    private bool editorPreviewSaveMode;
    private bool editorPreviewBuilt;

    private static readonly Dictionary<string, Sprite> PreviewSpriteCache = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite> SpringMaskSpriteCache = new Dictionary<string, Sprite>();

    private sealed class SlotVisualConfig
    {
        public int slot;
        public bool isAutoSlot;
        public string cardFileName;
        public string frameFileName;
        public string maskFileName;
        public string buttonFileName;
        public Rect cardRect;
        public Rect previewRect;
        public Rect buttonRect;
        public float previewRotation;
        public Color themeColor;
    }

    public bool IsOpen => this != null && gameObject != null && gameObject.activeInHierarchy;

    private sealed class PendingPreviewRequest
    {
        public Image targetImage;
        public TextMeshProUGUI fallbackText;
        public SaveData data;
    }

    private void Update()
    {
        if (PauseMenuUI.IsBlockingUnderlyingInput && !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (confirmDialog != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                confirmDialogCancelAction?.Invoke();
                return;
            }

            if (UIInputHelper.IsConfirmPressed())
            {
                UIInputHelper.TryClick(confirmDialogConfirmButton);
            }

            return;
        }

        if (PauseMenuUI.ShouldBlockUnderlyingEscape())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < runtimeThumbnailObjects.Count; i++)
        {
            if (runtimeThumbnailObjects[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(runtimeThumbnailObjects[i]);
                }
#if UNITY_EDITOR
                else
                {
                    DestroyImmediate(runtimeThumbnailObjects[i]);
                }
#endif
            }
        }

        runtimeThumbnailObjects.Clear();
        editorLayoutRects.Clear();
        editorPreviewSlotData.Clear();
        mainPanelRect = null;
        returnButtonRect = null;
        canvas = null;
        canvasRect = null;
        layoutContent = null;
        editorPreviewBuilt = false;
    }

    // ========== 静态入口 ==========

    public static void Show(bool isSaveMode)
    {
        if (!UIFlowGuard.PrepareForExclusiveWindow(UIFlowGuard.WindowSaveLoad))
        {
            MissionUI.Instance?.ShowSystemNotification("存档界面未打开",
                "当前还有其他关键界面占用操作，先处理完再管理存档。",
                new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return;
        }

        if (SaveManager.Instance == null)
        {
            MissionUI.Instance?.ShowSystemNotification("存档界面不可用",
                "存档系统还没有准备好，当前无法打开存档/读档面板。",
                new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return;
        }

        SaveLoadUI existing = FindObjectOfType<SaveLoadUI>();
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject uiRoot = new GameObject("SaveLoadUI");
        SaveLoadUI ui = uiRoot.AddComponent<SaveLoadUI>();
        ui.isSaveMode = isSaveMode;
        ui.BuildUI();
    }

    private ZhongshanDeckSaveLoadContent LoadLayoutContent()
    {
        ZhongshanDeckSaveLoadContent content = ZhongshanDeckToolStateBridge.GetSaveLoadContent();
        content ??= new ZhongshanDeckSaveLoadContent();
        content.EnsureInitialized();
        return content;
    }

    private SaveData GetSlotDataForBuild(int slot)
    {
        if (useEditorPreviewData)
        {
            return editorPreviewSlotData.TryGetValue(slot, out SaveData preview) ? preview : null;
        }

        return SaveManager.Instance?.GetSlotSaveData(slot);
    }

    private ZhongshanDeckSaveLoadLayoutItem GetLayoutItem(string key)
    {
        if (layoutContent?.layoutItems == null || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        for (int i = 0; i < layoutContent.layoutItems.Count; i++)
        {
            ZhongshanDeckSaveLoadLayoutItem item = layoutContent.layoutItems[i];
            if (item != null && string.Equals(item.key, key, StringComparison.Ordinal))
            {
                item.EnsureInitialized();
                return item;
            }
        }

        return null;
    }

    private void RegisterLayoutRect(string key, RectTransform rect)
    {
        if (string.IsNullOrWhiteSpace(key) || rect == null)
        {
            return;
        }

        editorLayoutRects[key] = rect;
    }

    // ========== UI 构建总入口 ==========

    private void BuildUI()
    {
        layoutContent = LoadLayoutContent();
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasRect = canvas.GetComponent<RectTransform>();

        // 1. 半透明遮罩
        CreateOverlayBackground();

        // 2. 主面板（春季素材底图）
        GameObject mainPanel = CreateMainPanel();
        RectTransform mainRT = mainPanel.GetComponent<RectTransform>();
        RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.LayoutBoard, mainRT);

        // 3. 四格存档覆盖层
        CreateCardGrid(mainRT);

        // 4. 翻页按钮
        CreatePageButtons(mainRT);

        // 5. 顶层装饰与模式标题，必须压在引导和卡片层上方
        CreateTopOverlay(mainRT);

        // 6. 返回按钮热区
        CreateReturnButton(mainRT);

        if (pendingPreviewRequests.Count > 0)
        {
            if (Application.isPlaying)
            {
                StartCoroutine(LoadQueuedPreviews());
            }
            else
            {
                ApplyQueuedPreviewsImmediately();
            }
        }

        editorPreviewBuilt = true;
    }

    // ========== 1. 遮罩背景 ==========

    private void CreateOverlayBackground()
    {
        GameObject bg = CreateUIElement("OverlayBg", canvasRect);
        StretchFull(bg.GetComponent<RectTransform>());

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = OverlayDim;
        bgImage.raycastTarget = true;
    }

    // ========== 2. 主面板 ==========

    private GameObject CreateMainPanel()
    {
        GameObject panel = CreateUIElement("SpringBoard", canvasRect);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        mainPanelRect = panelRT;
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        springBoardSprite = LoadSpringSprite("底图");

        Vector2 panelSize = CalculatePanelSize();
        panelRT.sizeDelta = panelSize;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = Color.white;
        panelImage.raycastTarget = true;

        if (springBoardSprite != null)
        {
            panelImage.sprite = springBoardSprite;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = true;
        }
        else
        {
            panelImage.color = WoodInnerBeige;
        }

        return panel;
    }

    private void CreateTopOverlay(RectTransform parent)
    {
        Sprite topOverlaySprite = LoadSpringSprite("顶图");
        if (topOverlaySprite != null)
        {
            GameObject overlayGO = CreateUIElement("TopOverlay", parent);
            RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
            StretchFull(overlayRT);
            topOverlayRect = overlayRT;
            RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.LayoutTopOverlay, overlayRT);

            Image overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.sprite = topOverlaySprite;
            overlayImage.preserveAspect = true;
            overlayImage.color = Color.white;
            overlayImage.raycastTarget = false;
        }

    }

    private void CreateHeaderLabel(RectTransform parent, string text, Vector2 imageCenter, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject labelGO = CreateUIElement("HeaderLabel", parent);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 0.5f);
        labelRT.anchorMax = new Vector2(0.5f, 0.5f);
        labelRT.pivot = new Vector2(0.5f, 0.5f);
        labelRT.anchoredPosition = ImagePointToLocal(imageCenter.x, imageCenter.y);
        labelRT.sizeDelta = ScalePanelSize(size);

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize * GetPanelScale();
        label.alignment = alignment;
        label.color = color;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
        ApplyChineseFont(label);
    }

    // ========== 4. 卡片网格 (2x2) ==========

    private void CreateCardGrid(RectTransform parent)
    {
        SaveData[] slotData = new SaveData[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slotData[i] = GetSlotDataForBuild(i);
        }

        for (int slot = 0; slot < SlotCount; slot++)
        {
            CreateSlotComposite(parent, GetSlotVisualConfig(slot), slotData[slot]);
        }
    }

    private void CreateSlotComposite(RectTransform parent, SlotVisualConfig config, SaveData data)
    {
        bool isEmpty = (data == null);
        CreateFullPanelArtLayer(parent, $"CardArt_{config.slot}", config.cardFileName);
        CreateFullPanelArtLayer(parent, $"FrameArt_{config.slot}", config.frameFileName);

        RectTransform previewRoot = CreatePreviewMaskRoot(parent, config, $"SlotPreview_{config.slot}");
        CreateSlotPhoto(previewRoot, config, data, isEmpty);

        CreateFullPanelArtLayer(parent, $"ButtonArt_{config.slot}", config.buttonFileName);

        RectTransform overlayRoot = CreateSlotRoot(parent, config, $"SlotOverlay_{config.slot}", registerLayout: true);
        CreateInteractiveOverlay(overlayRoot, config, isEmpty);
        CreateInfoTextArea(overlayRoot, config.slot, data, isEmpty, config.themeColor);
        CreateCardButtons(overlayRoot, config, isEmpty);
    }

    // ---- 信息文字区 ----

    private void CreateSlotPhoto(RectTransform slotRT, SlotVisualConfig config, SaveData data, bool isEmpty)
    {
        Rect previewRect = ToPanelLocalRect(config.previewRect);

        GameObject contentGO = CreateUIElement("PhotoContent", slotRT);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        ApplyPanelRect(contentRT, previewRect);
        RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotPhotoKey(config.slot), contentRT);
        contentRT.localRotation = Quaternion.Euler(0f, 0f, config.previewRotation);
        contentRT.localScale = Vector3.one * 1.08f;

        GameObject photoInner = CreateUIElement("PhotoInner", contentRT);
        RectTransform innerRT = photoInner.GetComponent<RectTransform>();
        StretchFull(innerRT);

        Image photoImg = photoInner.AddComponent<Image>();
        photoImg.raycastTarget = false;
        photoImg.color = PhotoInnerGray;
        photoImg.preserveAspect = false;

        GameObject fallbackGO = CreateUIElement("FallbackLabel", contentRT);
        StretchFull(fallbackGO.GetComponent<RectTransform>());
        TextMeshProUGUI fallbackText = fallbackGO.AddComponent<TextMeshProUGUI>();
        fallbackText.text = isEmpty ? "空位" : GetLocationName(data.currentLocation);
        fallbackText.fontSize = 26f * GetPanelScale();
        fallbackText.alignment = TextAlignmentOptions.Center;
        fallbackText.color = TextGray;
        fallbackText.raycastTarget = false;
        ApplyChineseFont(fallbackText);

        if (!isEmpty)
        {
            pendingPreviewRequests.Add(new PendingPreviewRequest
            {
                targetImage = photoImg,
                fallbackText = fallbackText,
                data = data
            });
        }
    }

    private void CreateInfoTextArea(RectTransform slotRT, int slot, SaveData data, bool isEmpty, Color themeColor)
    {
        float infoStartX = 435f;
        float infoWidth = 300f;

        GameObject infoRootGO = CreateUIElement("InfoRoot", slotRT);
        RectTransform infoRootRT = infoRootGO.GetComponent<RectTransform>();
        infoRootRT.anchorMin = new Vector2(0f, 1f);
        infoRootRT.anchorMax = new Vector2(0f, 1f);
        infoRootRT.pivot = new Vector2(0f, 1f);
        infoRootRT.sizeDelta = ScalePanelSize(new Vector2(infoWidth, 156f));
        infoRootRT.anchoredPosition = ScalePanelSize(new Vector2(infoStartX, -34f));
        RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotInfoKey(slot), infoRootRT);

        if (isEmpty)
        {
            GameObject emptyGO = CreateUIElement("EmptyHint", infoRootRT);
            RectTransform emptyRT = emptyGO.GetComponent<RectTransform>();
            StretchFull(emptyRT);

            TextMeshProUGUI emptyText = emptyGO.AddComponent<TextMeshProUGUI>();
            emptyText.text = isSaveMode
                ? "空存档位\n<size=24><color=#8B7355>点击卡片或下方按钮保存当前进度</color></size>"
                : "空存档位\n<size=24><color=#8B7355>这里还没有留下任何记录</color></size>";
            emptyText.fontSize = 24f * GetPanelScale();
            emptyText.alignment = TextAlignmentOptions.TopLeft;
            emptyText.color = TextGray;
            emptyText.raycastTarget = false;
            emptyText.enableWordWrapping = true;
            ApplyChineseFont(emptyText);
            return;
        }

        // 从 SaveData 提取信息
        string playerName = data.playerName ?? "学生";
        int age = 18 + (data.currentYear - 1);
        string yearName = GetYearName(data.currentYear);
        string yearSeason = $"{yearName}{GetSeason(data.currentMonth)}";
        string season = GetSeason(data.currentMonth);
        string locationName = GetLocationName(data.currentLocation);
        float gpa = CalcDisplayGPA(data);
        string major = string.IsNullOrWhiteSpace(data.playerMajor) ? "未定专业" : data.playerMajor;
        string saveTime = FormatSaveTime(data.meta?.saveTime);
        string playTime = FormatPlayTime(data.totalPlayTimeSeconds);
        string moneyStr = data.money >= 0 ? $"¥{data.money}" : $"-¥{-data.money}";

        CreateAccentDot(infoRootRT, new Vector2(-12f, 8f), themeColor);

        float lineH = 24f;
        float startY = 10f;
        int lineIdx = 0;

        CreateInfoLine(infoRootRT, $"{playerName}，{age}岁",
            0f, startY - lineIdx * lineH, infoWidth, 22f, TextBrown, FontStyles.Bold);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"学年：{yearSeason}    回合：{data.currentRound}",
            0f, startY - lineIdx * lineH, infoWidth, 15f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"季节：{season}    专业：{major}",
            0f, startY - lineIdx * lineH, infoWidth, 14f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"地点：{locationName}",
            0f, startY - lineIdx * lineH, infoWidth, 14f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"金钱：{moneyStr}   GPA：{gpa:F2}",
            0f, startY - lineIdx * lineH, infoWidth, 14f, TextBrownLight, FontStyles.Bold);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"核心：学{data.study} 魅{data.charm} 体{data.physique} 领{data.leadership}",
            0f, startY - lineIdx * lineH, infoWidth, 13f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"状态：心情{data.mood}  压力{data.stress}  幸运{data.luck}",
            0f, startY - lineIdx * lineH, infoWidth, 13f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"存档时间：{saveTime}",
            0f, startY - lineIdx * lineH, infoWidth, 12f, TextGray);
        lineIdx++;

        CreateInfoLine(infoRootRT, $"游玩时长：{playTime}",
            0f, startY - lineIdx * lineH, infoWidth, 12f, TextGray);
    }

    private void CreateInfoLine(RectTransform parent, string text,
        float x, float y, float width, float fontSize, Color color,
        FontStyles style = FontStyles.Normal)
    {
        GameObject go = CreateUIElement("InfoLine", parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = ScalePanelSize(new Vector2(width, fontSize + 12f));
        rt.anchoredPosition = ScalePanelSize(new Vector2(x, y));

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize * GetPanelScale();
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        ApplyChineseFont(tmp);
    }

    // ---- 和纸胶带 ----

    private void CreateWashiTape(RectTransform cardRT, Vector2 cornerPos, float rotation, Color color)
    {
        GameObject tape = CreateUIElement("WashiTape", cardRT);
        RectTransform tapeRT = tape.GetComponent<RectTransform>();
        tapeRT.anchorMin = new Vector2(0.5f, 0.5f);
        tapeRT.anchorMax = new Vector2(0.5f, 0.5f);
        tapeRT.pivot = new Vector2(0.5f, 0.5f);
        tapeRT.sizeDelta = new Vector2(56f, 18f);
        tapeRT.anchoredPosition = cornerPos;
        tapeRT.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image tapeImg = tape.AddComponent<Image>();
        tapeImg.color = color;
        tapeImg.raycastTarget = false;

        // 胶带上的花纹小点
        GameObject dot = CreateUIElement("TapeDot", tapeRT);
        RectTransform dotRT = dot.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0.5f, 0.5f);
        dotRT.anchorMax = new Vector2(0.5f, 0.5f);
        dotRT.pivot = new Vector2(0.5f, 0.5f);
        dotRT.sizeDelta = new Vector2(6f, 6f);
        dotRT.anchoredPosition = new Vector2(10f, 0f);

        Image dotImg = dot.AddComponent<Image>();
        dotImg.color = new Color(1f, 1f, 1f, 0.30f);
        dotImg.raycastTarget = false;
    }

    // ---- 卡片按钮 ----

    private void CreateCardButtons(RectTransform cardRT, SlotVisualConfig config, bool isEmpty)
    {
        Rect pairRect = ToSlotLocalRect(config.cardRect, config.buttonRect);
        GameObject buttonsRootGO = CreateUIElement("ButtonsRoot", cardRT);
        RectTransform buttonsRootRT = buttonsRootGO.GetComponent<RectTransform>();
        ApplySlotRect(buttonsRootRT, pairRect);
        RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotButtonsKey(config.slot), buttonsRootRT);

        if (config.isAutoSlot)
        {
            CreateCardButton(buttonsRootRT,
                config,
                "主按钮",
                isSaveMode ? "快速保存" : "读取",
                new Rect(0f, 0f, pairRect.width, pairRect.height),
                isSaveMode ? (Action)(() => OnSlotClicked(config.slot, isEmpty)) : (!isEmpty ? () => OnSlotClicked(config.slot, false) : null),
                ZhongshanDeckSaveLoadContentDefaults.GetSlotPrimaryButtonKey(config.slot));

            if (!isSaveMode && !isEmpty)
            {
                CreateButtonCaption(buttonsRootRT, "AutoHint", "读取自动存档", new Rect(0f, 0f, pairRect.width, pairRect.height), 17f);
            }
        }
        else
        {
            Rect leftRect = new Rect(0f, 0f, pairRect.width * 0.48f, pairRect.height);
            Rect rightRect = new Rect(pairRect.width * 0.52f, 0f, pairRect.width * 0.48f, pairRect.height);

            string leftLabel = isSaveMode ? (isEmpty ? "保存" : "覆盖") : "读取";
            string rightLabel = isEmpty ? "空位" : "删除";

            CreateCardButton(buttonsRootRT,
                config,
                "LeftButton",
                leftLabel,
                leftRect,
                isSaveMode ? (Action)(() => OnSlotClicked(config.slot, isEmpty)) : (!isEmpty ? () => OnSlotClicked(config.slot, false) : null),
                ZhongshanDeckSaveLoadContentDefaults.GetSlotLeftButtonKey(config.slot));

            CreateCardButton(buttonsRootRT,
                config,
                "RightButton",
                rightLabel,
                rightRect,
                isEmpty ? null : (Action)(() => OnDeleteSlotClicked(config.slot)),
                ZhongshanDeckSaveLoadContentDefaults.GetSlotRightButtonKey(config.slot));
        }
    }

    private Button CreateCardButton(RectTransform parent, SlotVisualConfig config, string name, string label, Rect rect, Action onClick, string layoutKey = null)
    {
        GameObject btnGO = CreateUIElement($"Btn_{name}", parent);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        ApplySlotRect(btnRT, rect);
        if (!string.IsNullOrWhiteSpace(layoutKey))
        {
            RegisterLayoutRect(layoutKey, btnRT);
        }

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(1f, 1f, 1f, 0.01f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.16f);
        cb.disabledColor = new Color(0.70f, 0.70f, 0.70f);
        btn.colors = cb;

        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
        else
        {
            btn.interactable = false;
            btnBg.color = new Color(1f, 1f, 1f, 0.004f);
        }

        GameObject textGO = CreateUIElement("Label", btnRT);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18f * GetPanelScale();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextBrown;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        ApplyChineseFont(tmp);

        return btn;
    }

    private void CreateButtonCaption(RectTransform parent, string name, string text, Rect rect, float fontSize)
    {
        GameObject textGO = CreateUIElement(name, parent);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        ApplySlotRect(textRT, new Rect(rect.xMin, rect.yMax + 8f * GetPanelScale(), rect.width, 24f * GetPanelScale()));

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize * GetPanelScale();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextBrownLight;
        tmp.raycastTarget = false;
        ApplyChineseFont(tmp);
    }

    // ========== 装饰元素工厂 ==========

    private void CreateCherryBlossom(RectTransform parent, Vector2 position, float scale)
    {
        GameObject cherry = CreateUIElement("CherryBlossom", parent);
        RectTransform crt = cherry.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        float s = 16f * scale;
        crt.sizeDelta = new Vector2(s, s);
        crt.anchoredPosition = position;

        // 五瓣花用多个小圆模拟
        for (int i = 0; i < 5; i++)
        {
            GameObject petal = CreateUIElement("Petal", crt);
            RectTransform prt = petal.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(s * 0.45f, s * 0.45f);
            float angle = i * 72f;
            float rad = angle * Mathf.Deg2Rad;
            float dist = s * 0.22f;
            prt.anchoredPosition = new Vector2(Mathf.Cos(rad) * dist, Mathf.Sin(rad) * dist);

            Image pImg = petal.AddComponent<Image>();
            pImg.color = CherryPink;
            pImg.raycastTarget = false;
        }

        // 花心
        GameObject center = CreateUIElement("Center", crt);
        RectTransform cr = center.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.5f);
        cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(s * 0.25f, s * 0.25f);
        cr.anchoredPosition = Vector2.zero;

        Image cImg = center.AddComponent<Image>();
        cImg.color = new Color(1f, 0.85f, 0.70f);
        cImg.raycastTarget = false;
    }

    private void CreateLeafDecoration(RectTransform parent, Vector2 position)
    {
        GameObject leaf = CreateUIElement("Leaf", parent);
        RectTransform lrt = leaf.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(14f, 8f);
        lrt.anchoredPosition = position;
        lrt.localRotation = Quaternion.Euler(0f, 0f, 30f);

        Image lImg = leaf.AddComponent<Image>();
        lImg.color = LeafGreen;
        lImg.raycastTarget = false;
    }

    private void CreateMusicNoteSticker(RectTransform parent, Vector2 position, Color color)
    {
        GameObject sticker = CreateUIElement("MusicSticker", parent);
        RectTransform srt = sticker.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(28f, 22f);
        srt.anchoredPosition = position;
        srt.localRotation = Quaternion.Euler(0f, 0f, 5f);

        Image sImg = sticker.AddComponent<Image>();
        sImg.color = color;
        sImg.raycastTarget = false;

        // 音符符号
        GameObject note = CreateUIElement("Note", srt);
        RectTransform nrt = note.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.5f, 0.5f);
        nrt.anchorMax = new Vector2(0.5f, 0.5f);
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.sizeDelta = new Vector2(20f, 16f);
        nrt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI noteText = note.AddComponent<TextMeshProUGUI>();
        noteText.text = "\u266a"; // 八分音符
        noteText.fontSize = 14;
        noteText.alignment = TextAlignmentOptions.Center;
        noteText.color = TextBrown;
        noteText.raycastTarget = false;
    }

    private void CreateSwallow(RectTransform parent, Vector2 position)
    {
        // 燕子剪影（用 V 形+身体模拟）
        GameObject swallow = CreateUIElement("Swallow", parent);
        RectTransform swrt = swallow.GetComponent<RectTransform>();
        swrt.anchorMin = new Vector2(0.5f, 0.5f);
        swrt.anchorMax = new Vector2(0.5f, 0.5f);
        swrt.pivot = new Vector2(0.5f, 0.5f);
        swrt.sizeDelta = new Vector2(24f, 12f);
        swrt.anchoredPosition = position;
        swrt.localRotation = Quaternion.Euler(0f, 0f, -15f);

        // 左翼
        GameObject leftWing = CreateUIElement("LeftWing", swrt);
        RectTransform lwRT = leftWing.GetComponent<RectTransform>();
        lwRT.anchorMin = new Vector2(0.5f, 0.5f);
        lwRT.anchorMax = new Vector2(0.5f, 0.5f);
        lwRT.pivot = new Vector2(1f, 0.5f);
        lwRT.sizeDelta = new Vector2(14f, 3f);
        lwRT.anchoredPosition = new Vector2(0f, 2f);
        lwRT.localRotation = Quaternion.Euler(0f, 0f, -25f);

        Image lwImg = leftWing.AddComponent<Image>();
        lwImg.color = SwallowBlack;
        lwImg.raycastTarget = false;

        // 右翼
        GameObject rightWing = CreateUIElement("RightWing", swrt);
        RectTransform rwRT = rightWing.GetComponent<RectTransform>();
        rwRT.anchorMin = new Vector2(0.5f, 0.5f);
        rwRT.anchorMax = new Vector2(0.5f, 0.5f);
        rwRT.pivot = new Vector2(0f, 0.5f);
        rwRT.sizeDelta = new Vector2(14f, 3f);
        rwRT.anchoredPosition = new Vector2(0f, 2f);
        rwRT.localRotation = Quaternion.Euler(0f, 0f, 25f);

        Image rwImg = rightWing.AddComponent<Image>();
        rwImg.color = SwallowBlack;
        rwImg.raycastTarget = false;

        // 身体（小圆）
        GameObject body = CreateUIElement("Body", swrt);
        RectTransform brt = body.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(4f, 6f);
        brt.anchoredPosition = new Vector2(1f, -1f);

        Image bImg = body.AddComponent<Image>();
        bImg.color = SwallowBlack;
        bImg.raycastTarget = false;

        // 尾羽
        GameObject tail = CreateUIElement("Tail", swrt);
        RectTransform trt = tail.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(1f, 0.5f);
        trt.sizeDelta = new Vector2(8f, 2f);
        trt.anchoredPosition = new Vector2(-4f, 1f);
        trt.localRotation = Quaternion.Euler(0f, 0f, -10f);

        Image tImg = tail.AddComponent<Image>();
        tImg.color = SwallowBlack;
        tImg.raycastTarget = false;
    }

    private void CreateFlagSticker(RectTransform parent, Vector2 position,
        Color stripe1, Color stripe2)
    {
        GameObject flag = CreateUIElement("Flag", parent);
        RectTransform frt = flag.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0.5f, 0.5f);
        frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = new Vector2(20f, 28f);
        frt.anchoredPosition = position;
        frt.localRotation = Quaternion.Euler(0f, 0f, 10f);

        // 旗杆
        GameObject pole = CreateUIElement("Pole", frt);
        RectTransform prt = pole.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0.5f);
        prt.anchorMax = new Vector2(0f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(2f, 28f);
        prt.anchoredPosition = new Vector2(-8f, 0f);

        Image pImg = pole.AddComponent<Image>();
        pImg.color = new Color(0.55f, 0.38f, 0.18f);
        pImg.raycastTarget = false;

        // 条纹旗帜
        GameObject cloth = CreateUIElement("Cloth", frt);
        RectTransform crt = cloth.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 0.5f);
        crt.anchorMax = new Vector2(0f, 0.5f);
        crt.pivot = new Vector2(0f, 0.5f);
        crt.sizeDelta = new Vector2(14f, 20f);
        crt.anchoredPosition = new Vector2(-6f, 0f);

        // 条纹效果：上半红、下半白
        GameObject stripeTop = CreateUIElement("StripeTop", crt);
        RectTransform stRT = stripeTop.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0.5f);
        stRT.anchorMax = new Vector2(1f, 1f);
        stRT.offsetMin = Vector2.zero;
        stRT.offsetMax = Vector2.zero;

        Image stImg = stripeTop.AddComponent<Image>();
        stImg.color = stripe1;
        stImg.raycastTarget = false;

        GameObject stripeBot = CreateUIElement("StripeBot", crt);
        RectTransform sbRT = stripeBot.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0f, 0f);
        sbRT.anchorMax = new Vector2(1f, 0.5f);
        sbRT.offsetMin = Vector2.zero;
        sbRT.offsetMax = Vector2.zero;

        Image sbImg = stripeBot.AddComponent<Image>();
        sbImg.color = stripe2;
        sbImg.raycastTarget = false;
    }

    private void CreatePaperclipNote(RectTransform parent, Vector2 position)
    {
        // 回形针（银色小矩形框）
        GameObject clip = CreateUIElement("Paperclip", parent);
        RectTransform crt = clip.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(8f, 18f);
        crt.anchoredPosition = position;
        crt.localRotation = Quaternion.Euler(0f, 0f, 15f);

        Image cImg = clip.AddComponent<Image>();
        cImg.color = new Color(0.75f, 0.78f, 0.82f);
        cImg.raycastTarget = false;

        // 小标签纸
        GameObject note = CreateUIElement("ClipNote", parent);
        RectTransform nrt = note.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.5f, 0.5f);
        nrt.anchorMax = new Vector2(0.5f, 0.5f);
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.sizeDelta = new Vector2(24f, 18f);
        nrt.anchoredPosition = position + new Vector2(12f, -6f);
        nrt.localRotation = Quaternion.Euler(0f, 0f, -8f);

        Image nImg = note.AddComponent<Image>();
        nImg.color = new Color(1f, 1f, 0.92f, 0.90f);
        nImg.raycastTarget = false;
    }

    // ========== 4. 返回按钮 ==========

    private void CreateReturnButton(RectTransform parent)
    {
        const float returnMinX = 2081f;
        const float returnMinY = 188f;
        const float returnMaxX = 2283f;
        const float returnMaxY = 291f;

        GameObject btnGO = CreateUIElement("ReturnButton", parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = ImagePointToLocal((returnMinX + returnMaxX) * 0.5f, (returnMinY + returnMaxY) * 0.5f);
        rt.sizeDelta = ImageSizeToLocal(returnMaxX - returnMinX + 1f, returnMaxY - returnMinY + 1f);
        returnButtonRect = rt;
        RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.LayoutReturnButton, rt);

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(1f, 1f, 1f, 0.01f);
        btnBg.raycastTarget = true;

        Sprite btnSprite = LoadSpringSprite("返回界面显示按钮");
        Image hoverImage = null;
        if (btnSprite != null)
        {
            GameObject hoverGO = CreateUIElement("ReturnHoverVisual", parent);
            RectTransform hoverRT = hoverGO.GetComponent<RectTransform>();
            StretchFull(hoverRT);
            hoverImage = hoverGO.AddComponent<Image>();
            hoverImage.sprite = btnSprite;
            hoverImage.preserveAspect = true;
            hoverImage.color = new Color(1f, 1f, 1f, 0f);
            hoverImage.raycastTarget = false;
            hoverGO.transform.SetAsLastSibling();
        }

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(ClosePanel);

        EventTrigger trigger = btnGO.AddComponent<EventTrigger>();
        trigger.triggers = new List<EventTrigger.Entry>();
        bool isHovering = false;

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            isHovering = true;
            if (hoverImage != null)
            {
                hoverImage.color = Color.white;
            }
        });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            isHovering = false;
            if (hoverImage != null)
            {
                hoverImage.color = new Color(1f, 1f, 1f, 0f);
            }
        });
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener(_ =>
        {
            if (hoverImage != null)
            {
                hoverImage.color = Color.white;
            }
        });
        trigger.triggers.Add(downEntry);

        EventTrigger.Entry upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener(_ =>
        {
            if (hoverImage != null)
            {
                hoverImage.color = isHovering ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        });
        trigger.triggers.Add(upEntry);
    }

    private void CreatePageButtons(RectTransform parent)
    {
        CreatePageButton(parent, "PrevPageButton", "左-不可翻页", false,
            new Vector2(134f, 768f), new Vector2(86f, 86f), null);
        CreatePageButton(parent, "NextPageButton", "右-不可翻页", false,
            new Vector2(2624f, 768f), new Vector2(86f, 86f), null);
    }

    private void CreatePageButton(RectTransform parent, string objectName, string spriteName, bool interactable, Vector2 imageCenter, Vector2 imageSize, Action onClick)
    {
        Sprite sprite = LoadSpringSprite(spriteName);
        if (sprite == null)
        {
            return;
        }

        GameObject rootGO = CreateUIElement(objectName, parent);
        RectTransform rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = ImagePointToLocal(imageCenter.x, imageCenter.y);
        rootRT.sizeDelta = ImageSizeToLocal(imageSize.x, imageSize.y);

        if (string.Equals(objectName, "PrevPageButton", StringComparison.Ordinal))
        {
            prevPageButtonRect = rootRT;
            RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.LayoutPrevPageButton, rootRT);
        }
        else if (string.Equals(objectName, "NextPageButton", StringComparison.Ordinal))
        {
            nextPageButtonRect = rootRT;
            RegisterLayoutRect(ZhongshanDeckSaveLoadContentDefaults.LayoutNextPageButton, rootRT);
        }

        Image img = rootGO.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = interactable;
        img.color = Color.white;

        if (!interactable)
        {
            return;
        }

        Button btn = rootGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    // ========== 交互逻辑 ==========

    private void OnSlotClicked(int slot, bool isEmpty)
    {
        if (isSaveMode)
        {
            if (isEmpty)
            {
                DoSave(slot);
            }
            else
            {
                ShowConfirmDialog("确认覆盖此存档？\n当前存档记录将被替换", () => DoSave(slot));
            }
        }
        else
        {
            if (isEmpty)
            {
                ShowSystemNotification("无法读档", "这个槽位还没有存档记录，先在游戏里保存一次进度。");
                return;
            }
            ShowConfirmDialog("确认加载此存档？\n当前未保存的进度将丢失", () => DoLoad(slot));
        }
    }

    private void DoSave(int slot)
    {
        if (SaveManager.Instance == null)
        {
            ShowSystemNotification("存档失败", "存档系统暂时不可用，这次进度没有写入。");
            return;
        }

        if (isSaving)
        {
            return;
        }

        isSaving = true;
        StartCoroutine(DoSaveRoutine(slot));
    }

    private void DoLoad(int slot)
    {
        if (SaveManager.Instance == null)
        {
            ShowSystemNotification("读档失败", "存档系统暂时不可用，现在无法读取这份进度。");
            return;
        }

        SaveData data = SaveManager.Instance.LoadFromSlot(slot);
        if (data == null)
        {
            Debug.LogWarning("[SaveLoadUI] 读档失败");
            ShowSystemNotification("读档失败", "这个存档暂时无法读取，请换一个槽位试试。");
            return;
        }

        SaveManager.PendingLoadData = data;
        SaveManager.PendingLoadSlot = slot;

        Debug.Log($"[SaveLoadUI] 已设置 PendingLoadData，准备跳转 GameScene");
        ClosePanel();
        SceneLoader.LoadScene("GameScene");
    }

    public void ClosePanel()
    {
        Close();
    }

    private void Close()
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
#if UNITY_EDITOR
        else
        {
            DestroyImmediate(gameObject);
        }
#endif
    }

    private void OnDeleteSlotClicked(int slot)
    {
        if (SaveManager.Instance == null)
        {
            ShowSystemNotification("删除失败", "存档系统暂时不可用，现在无法移除这个槽位。");
            return;
        }

        ShowConfirmDialog("确认删除这份存档？\n删除后将无法恢复", () =>
        {
            SaveManager.Instance.DeleteSlot(slot);
            ShowSystemNotification("已删除", slot == 0 ? "自动存档已移除。" : $"存档 {slot:D2} 已移除。");
            Show(isSaveMode);
        });
    }

    private IEnumerator DoSaveRoutine(int slot)
    {
        if (canvas != null)
        {
            canvas.enabled = false;
        }

        yield return null;

        bool saveCompleted = false;
        SaveManager.Instance.CaptureAndSaveToSlot(slot, _ => saveCompleted = true);

        while (!saveCompleted)
        {
            yield return null;
        }

        Debug.Log($"[SaveLoadUI] 已保存到槽位{slot}");
        string slotName = slot == 0 ? "自动存档" : $"存档 {slot:D2}";
        ShowSystemNotification("存档完成", $"{slotName} 已保存。");
        ClosePanel();
    }

    // ========== 确认对话框（春季风格） ==========

    private void ShowConfirmDialog(string message, Action onConfirm)
    {
        if (confirmDialog != null)
        {
            Destroy(confirmDialog);
        }

        confirmDialogConfirmAction = () =>
        {
            if (confirmDialog != null)
            {
                Destroy(confirmDialog);
                confirmDialog = null;
            }
            confirmDialogConfirmButton = null;
            Action callback = onConfirm;
            confirmDialogConfirmAction = null;
            confirmDialogCancelAction = null;
            callback?.Invoke();
        };

        confirmDialogCancelAction = () =>
        {
            if (confirmDialog != null)
            {
                Destroy(confirmDialog);
                confirmDialog = null;
            }
            confirmDialogConfirmButton = null;
            confirmDialogConfirmAction = null;
            confirmDialogCancelAction = null;
        };

        confirmDialog = CreateUIElement("ConfirmDialog", canvasRect);
        StretchFull(confirmDialog.GetComponent<RectTransform>());

        Image dimBg = confirmDialog.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.45f);
        dimBg.raycastTarget = true;

        // 对话框面板（春季暖色调）
        GameObject dialogPanel = CreateUIElement("DialogPanel", confirmDialog.GetComponent<RectTransform>());
        RectTransform panelRT = dialogPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(420f, 220f);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = dialogPanel.AddComponent<Image>();
        panelBg.color = new Color(0.98f, 0.96f, 0.90f, 0.98f);

        // 消息文字
        GameObject msgGO = CreateUIElement("Message", panelRT);
        RectTransform msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.5f, 1f);
        msgRT.anchorMax = new Vector2(0.5f, 1f);
        msgRT.pivot = new Vector2(0.5f, 1f);
        msgRT.sizeDelta = new Vector2(360f, 110f);
        msgRT.anchoredPosition = new Vector2(0f, -20f);

        TextMeshProUGUI msgText = msgGO.AddComponent<TextMeshProUGUI>();
        msgText.text = message;
        msgText.fontSize = 22;
        msgText.alignment = TextAlignmentOptions.Center;
        msgText.color = TextBrown;
        msgText.raycastTarget = false;
        ApplyChineseFont(msgText);

        // 按钮
        float btnY = -160f;
        float btnWidth = 130f;
        float btnHeight = 44f;
        float btnSpacing = 26f;

        confirmDialogConfirmButton = CreateDialogButton(panelRT, "ConfirmBtn", "确认",
            new Color(0.94f, 0.65f, 0.71f),
            new Vector2(-btnWidth / 2f - btnSpacing / 2f, btnY),
            new Vector2(btnWidth, btnHeight),
            () => confirmDialogConfirmAction?.Invoke());

        CreateDialogButton(panelRT, "CancelBtn", "取消",
            new Color(0.72f, 0.68f, 0.62f),
            new Vector2(btnWidth / 2f + btnSpacing / 2f, btnY),
            new Vector2(btnWidth, btnHeight),
            () => confirmDialogCancelAction?.Invoke());

        confirmDialog.transform.SetAsLastSibling();
        UIInputHelper.FocusSelectable(confirmDialogConfirmButton);
    }

    private Button CreateDialogButton(RectTransform parent, string name, string label,
        Color bgColor, Vector2 position, Vector2 size, Action onClick)
    {
        GameObject btnGO = CreateUIElement(name, parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        GameObject textGO = CreateUIElement("Label", rt);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextWhite;
        tmp.raycastTarget = false;
        ApplyChineseFont(tmp);
        return btn;
    }

    // ========== 辅助方法 ==========

    private string GetYearName(int year)
    {
        switch (year)
        {
            case 1: return "大一";
            case 2: return "大二";
            case 3: return "大三";
            case 4: return "大四";
            default: return $"大{year}";
        }
    }

    private Vector2 CalculatePanelSize()
    {
        Rect spriteRect = springBoardSprite != null
            ? springBoardSprite.rect
            : new Rect(0f, 0f, SpringDesignWidth, SpringDesignHeight);
        float spriteAspect = spriteRect.width / spriteRect.height;
        float width = PanelMaxWidth;
        float height = width / spriteAspect;

        if (height > PanelMaxHeight)
        {
            height = PanelMaxHeight;
            width = height * spriteAspect;
        }

        return new Vector2(width, height);
    }

    private Vector2 GetSlotCenter(int slot)
    {
        SlotVisualConfig config = GetSlotVisualConfig(slot);
        Rect rect = config.cardRect;
        return ImagePointToLocal((rect.xMin + rect.xMax) * 0.5f, (rect.yMin + rect.yMax) * 0.5f);
    }

    private string GetSlotLayoutKey(int slot)
    {
        switch (slot)
        {
            case 0: return ZhongshanDeckSaveLoadContentDefaults.LayoutAutoSlot;
            case 1: return ZhongshanDeckSaveLoadContentDefaults.LayoutSlot01;
            case 2: return ZhongshanDeckSaveLoadContentDefaults.LayoutSlot02;
            case 3: return ZhongshanDeckSaveLoadContentDefaults.LayoutSlot03;
            default: return string.Empty;
        }
    }

    private void ApplyLayoutToRect(RectTransform target, ZhongshanDeckSaveLoadLayoutItem item, Vector2 fallbackAnchor, Vector2 fallbackPosition, Vector2 fallbackSize)
    {
        if (target == null)
        {
            return;
        }

        if (item == null)
        {
            target.anchorMin = fallbackAnchor;
            target.anchorMax = fallbackAnchor;
            target.pivot = fallbackAnchor;
            target.anchoredPosition = fallbackPosition;
            target.sizeDelta = fallbackSize;
            return;
        }

        Vector2 anchor = GetLayoutAnchorVector(item.anchor);
        Vector2 pivot = GetLayoutPivotVector(item.anchor);
        target.anchorMin = anchor;
        target.anchorMax = anchor;
        target.pivot = pivot;
        target.anchoredPosition = item.anchoredPosition;
        target.sizeDelta = item.size;
        target.gameObject.SetActive(item.visible);
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

    private Sprite LoadSlotPreviewSprite(SaveData data)
    {
        if (data == null)
        {
            return null;
        }

        Sprite savedThumbnail = LoadSavedThumbnailSprite(data.thumbnailFileName);
        if (savedThumbnail != null)
        {
            return savedThumbnail;
        }

        switch (data.currentLocation)
        {
            case "Dormitory":
                return LoadCachedSprite(data.playerGender == 1
                    ? "Backgrounds/DormitoryFemaleBackground"
                    : "Backgrounds/DormitoryTemporaryBackground");

            case "TeachingBuilding":
                return LoadCachedSprite("LocationScenes/TeachingBuildings/TeachingBuilding");

            case "Library":
                return LoadCachedSprite("LocationScenes/Librarys/Library");

            case "Canteen":
                return LoadCachedSprite("LocationScenes/Canteens/Canteen");

            case "Playground":
                return LoadCachedSprite("LocationScenes/Playgrounds/Playground");
        }

        return LoadCachedSprite("GameLogo");
    }

    private RectTransform CreateSlotRoot(RectTransform parent, SlotVisualConfig config, string objectName, bool registerLayout)
    {
        GameObject rootGO = CreateUIElement(objectName, parent);
        RectTransform rootRT = rootGO.GetComponent<RectTransform>();
        Vector2 center = ImagePointToLocal(
            (config.cardRect.xMin + config.cardRect.xMax) * 0.5f,
            (config.cardRect.yMin + config.cardRect.yMax) * 0.5f);
        Vector2 size = ImageSizeToLocal(config.cardRect.width, config.cardRect.height);

        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = center;
        rootRT.sizeDelta = size;

        if (registerLayout)
        {
            RegisterLayoutRect(GetSlotLayoutKey(config.slot), rootRT);
        }

        return rootRT;
    }

    private RectTransform CreatePreviewMaskRoot(RectTransform parent, SlotVisualConfig config, string objectName)
    {
        GameObject rootGO = CreateUIElement(objectName, parent);
        RectTransform rootRT = rootGO.GetComponent<RectTransform>();
        StretchFull(rootRT);

        Sprite maskSprite = LoadSpringMaskSprite(System.IO.Path.GetFileNameWithoutExtension(config.maskFileName));
        Image maskImage = rootGO.AddComponent<Image>();
        maskImage.sprite = maskSprite;
        maskImage.preserveAspect = true;
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;

        Mask mask = rootGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        return rootRT;
    }

    private void CreateInteractiveOverlay(RectTransform slotRT, SlotVisualConfig config, bool isEmpty)
    {
        Image hitArea = slotRT.gameObject.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.01f);
        hitArea.raycastTarget = true;

        Button slotButton = slotRT.gameObject.AddComponent<Button>();
        slotButton.targetGraphic = hitArea;
        ColorBlock cb = slotButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        slotButton.colors = cb;
        slotButton.transition = Selectable.Transition.ColorTint;
        slotButton.onClick.AddListener(() => OnSlotClicked(config.slot, isEmpty));

        if (!isSaveMode && isEmpty)
        {
            slotButton.interactable = false;
        }
    }

    private void CreateFullPanelArtLayer(RectTransform parent, string objectName, string fileName)
    {
        Sprite sprite = LoadSpringSprite(System.IO.Path.GetFileNameWithoutExtension(fileName));
        if (sprite == null)
        {
            return;
        }

        GameObject layerGO = CreateUIElement(objectName, parent);
        RectTransform layerRT = layerGO.GetComponent<RectTransform>();
        StretchFull(layerRT);

        Image image = layerGO.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private SlotVisualConfig GetSlotVisualConfig(int slot)
    {
        switch (slot)
        {
            case 0:
                return new SlotVisualConfig
                {
                    slot = 0,
                    isAutoSlot = true,
                    cardFileName = "游戏卡片左上.png",
                    frameFileName = "游戏截图左上.png",
                    maskFileName = "游戏截图左上遮罩.png",
                    buttonFileName = "红色短按钮.png",
                    cardRect = Rect.MinMaxRect(547f, 366f, 1368f, 821f),
                    previewRect = Rect.MinMaxRect(582f, 407f, 965f, 705f),
                    buttonRect = Rect.MinMaxRect(653f, 704f, 894f, 782f),
                    previewRotation = 3.0f,
                    themeColor = AutoPink
                };
            case 1:
                return new SlotVisualConfig
                {
                    slot = 1,
                    isAutoSlot = false,
                    cardFileName = "游戏卡片右上.png",
                    frameFileName = "游戏截图右上.png",
                    maskFileName = "游戏截图右上遮罩.png",
                    buttonFileName = "蓝色短按钮.png",
                    cardRect = Rect.MinMaxRect(1425f, 366f, 2246f, 821f),
                    previewRect = Rect.MinMaxRect(1458f, 407f, 1841f, 705f),
                    buttonRect = Rect.MinMaxRect(1498f, 704f, 1825f, 782f),
                    previewRotation = 3.0f,
                    themeColor = Slot01Pink
                };
            case 2:
                return new SlotVisualConfig
                {
                    slot = 2,
                    isAutoSlot = false,
                    cardFileName = "游戏卡片左下.png",
                    frameFileName = "游戏截图左下.png",
                    maskFileName = "游戏截图左下遮罩.png",
                    buttonFileName = "绿色短按钮.png",
                    cardRect = Rect.MinMaxRect(547f, 867f, 1368f, 1322f),
                    previewRect = Rect.MinMaxRect(582f, 903f, 965f, 1201f),
                    buttonRect = Rect.MinMaxRect(610f, 1205f, 937f, 1283f),
                    previewRotation = 3.0f,
                    themeColor = Slot02Green
                };
            case 3:
                return new SlotVisualConfig
                {
                    slot = 3,
                    isAutoSlot = false,
                    cardFileName = "游戏卡片右下.png",
                    frameFileName = "游戏截图右下.png",
                    maskFileName = "游戏截图右下遮罩.png",
                    buttonFileName = "黄色端按钮.png",
                    cardRect = Rect.MinMaxRect(1425f, 867f, 2246f, 1322f),
                    previewRect = Rect.MinMaxRect(1458f, 903f, 1841f, 1201f),
                    buttonRect = Rect.MinMaxRect(1489f, 1205f, 1816f, 1283f),
                    previewRotation = 3.0f,
                    themeColor = Slot03Yellow
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
        }
    }

    private Rect ToSlotLocalRect(Rect cardRect, Rect childRect)
    {
        float scale = GetPanelScale();
        float left = (childRect.xMin - cardRect.xMin) * scale;
        float top = (childRect.yMin - cardRect.yMin) * scale;
        float width = childRect.width * scale;
        float height = childRect.height * scale;

        return new Rect(
            -ImageSizeToLocal(cardRect.width, 0f).x * 0.5f + left,
            ImageSizeToLocal(0f, cardRect.height).y * 0.5f - top - height,
            width,
            height);
    }

    private Rect ToPanelLocalRect(Rect imageRect)
    {
        Vector2 min = ImagePointToLocal(imageRect.xMin, imageRect.yMax);
        Vector2 size = ImageSizeToLocal(imageRect.width, imageRect.height);
        return new Rect(min.x, min.y, size.x, size.y);
    }

    private void ApplySlotRect(RectTransform target, Rect rect)
    {
        target.anchorMin = new Vector2(0f, 0f);
        target.anchorMax = new Vector2(0f, 0f);
        target.pivot = new Vector2(0f, 0f);
        target.anchoredPosition = new Vector2(rect.xMin, rect.yMin);
        target.sizeDelta = new Vector2(rect.width, rect.height);
    }

    private void ApplyPanelRect(RectTransform target, Rect rect)
    {
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = new Vector2(rect.xMin + rect.width * 0.5f, rect.yMin + rect.height * 0.5f);
        target.sizeDelta = new Vector2(rect.width, rect.height);
    }

    private float GetPanelScale()
    {
        return mainPanelRect != null ? mainPanelRect.rect.width / SpringDesignWidth : 1f;
    }

    private Vector2 ImagePointToLocal(float imageX, float imageY)
    {
        float scale = GetPanelScale();
        return new Vector2(
            (imageX - SpringDesignWidth * 0.5f) * scale,
            (SpringDesignHeight * 0.5f - imageY) * scale);
    }

    private Vector2 ImageSizeToLocal(float imageWidth, float imageHeight)
    {
        float scale = GetPanelScale();
        return new Vector2(imageWidth * scale, imageHeight * scale);
    }

    private Vector2 ScalePanelLocalPoint(Vector2 localCenter)
    {
        return localCenter * GetPanelScale();
    }

    private Vector2 ScalePanelSize(Vector2 size)
    {
        return size * GetPanelScale();
    }

    private Sprite LoadSpringSprite(string resourceName)
    {
        return LoadCachedSprite($"{SpringResourceFolder}/{resourceName}");
    }

    private Sprite LoadSpringMaskSprite(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return null;
        }

        if (SpringMaskSpriteCache.TryGetValue(resourceName, out Sprite cachedMask) && cachedMask != null)
        {
            return cachedMask;
        }

        Sprite sourceSprite = LoadSpringSprite(resourceName);
        if (sourceSprite == null || sourceSprite.texture == null)
        {
            return sourceSprite;
        }

        Texture2D readableTexture = ExtractReadableTexture(sourceSprite);
        if (readableTexture == null)
        {
            return sourceSprite;
        }

        Color[] pixels = readableTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color color = pixels[i];
            float alpha = Mathf.Clamp01((color.r + color.g + color.b) / 3f);
            pixels[i] = new Color(1f, 1f, 1f, alpha);
        }

        Texture2D maskTexture = new Texture2D(readableTexture.width, readableTexture.height, TextureFormat.RGBA32, false);
        maskTexture.name = resourceName + "_AlphaMask";
        maskTexture.SetPixels(pixels);
        maskTexture.Apply(false, false);

        Sprite maskSprite = Sprite.Create(
            maskTexture,
            new Rect(0f, 0f, maskTexture.width, maskTexture.height),
            new Vector2(0.5f, 0.5f),
            sourceSprite.pixelsPerUnit);

        SpringMaskSpriteCache[resourceName] = maskSprite;
        runtimeThumbnailObjects.Add(readableTexture);
        runtimeThumbnailObjects.Add(maskTexture);
        runtimeThumbnailObjects.Add(maskSprite);
        return maskSprite;
    }

    private Texture2D ExtractReadableTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return null;
        }

        Rect rect = sprite.rect;
        RenderTexture renderTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(sprite.texture, renderTexture);
            RenderTexture.active = renderTexture;

            Texture2D texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
            texture.Apply(false, false);
            return texture;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    private Rect GetSlotPhotoFallbackRect(int slot)
    {
        SlotVisualConfig config = GetSlotVisualConfig(slot);
        return ToPanelLocalRect(config.previewRect);
    }

    private Rect GetSlotInfoFallbackRect(int slot)
    {
        return new Rect(435f, -34f, 300f, 156f);
    }

    private Rect GetSlotButtonsFallbackRect(int slot)
    {
        SlotVisualConfig config = GetSlotVisualConfig(slot);
        return ToSlotLocalRect(config.cardRect, config.buttonRect);
    }

    private Rect GetSlotPrimaryButtonFallbackRect(int slot)
    {
        Rect buttonsRect = GetSlotButtonsFallbackRect(slot);
        return new Rect(0f, 0f, buttonsRect.width, buttonsRect.height);
    }

    private Rect GetSlotLeftButtonFallbackRect(int slot)
    {
        Rect buttonsRect = GetSlotButtonsFallbackRect(slot);
        return new Rect(0f, 0f, buttonsRect.width * 0.48f, buttonsRect.height);
    }

    private Rect GetSlotRightButtonFallbackRect(int slot)
    {
        Rect buttonsRect = GetSlotButtonsFallbackRect(slot);
        return new Rect(buttonsRect.width * 0.52f, 0f, buttonsRect.width * 0.48f, buttonsRect.height);
    }

    private void CreateAccentDot(RectTransform parent, Vector2 anchoredPosition, Color color)
    {
        GameObject dotGO = CreateUIElement("AccentDot", parent);
        RectTransform rt = dotGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = ScalePanelSize(new Vector2(9f, 9f));
        rt.anchoredPosition = ScalePanelSize(anchoredPosition);

        Image dotImage = dotGO.AddComponent<Image>();
        dotImage.color = color;
        dotImage.raycastTarget = false;
    }

    private string GetSeason(int month)
    {
        if (month >= 3 && month <= 5) return "春";
        if (month >= 6 && month <= 8) return "夏";
        if (month >= 9 && month <= 11) return "秋";
        return "冬";
    }

    private string GetLocationName(string locationStr)
    {
        if (string.IsNullOrEmpty(locationStr)) return "未知";

        switch (locationStr)
        {
            case "Dormitory": return "宿舍";
            case "TeachingBuilding": return "教学楼";
            case "Library": return "图书馆";
            case "Canteen": return "食堂";
            case "Playground": return "操场";
            case "Supermarket": return "教超";
            case "ExpressStation": return "快递站";
            case "TakeoutStation": return "外卖站";
            default: return locationStr;
        }
    }

    private float CalcDisplayGPA(SaveData data)
    {
        if (data.semesterGPAHistory != null && data.semesterGPAHistory.Count > 0)
        {
            return GPACalculator.CalcCumulativeGPA(data.semesterGPAHistory);
        }
        return 0f;
    }

    private string FormatSaveTime(string isoTime)
    {
        if (string.IsNullOrEmpty(isoTime)) return "未记录";

        try
        {
            DateTime dt = DateTime.Parse(isoTime);
            return dt.ToString("yyyy.MM.dd HH:mm");
        }
        catch
        {
            return isoTime;
        }
    }

    private string FormatPlayTime(float seconds)
    {
        int totalMinutes = Mathf.FloorToInt(seconds / 60f);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours > 0)
            return $"{hours}:{minutes:D2}:00";
        else
            return $"0:{minutes:D2}:00";
    }

    private void ShowSystemNotification(string title, string message)
    {
        MissionUI.Instance?.ShowSystemNotification(title, message,
            new Color(0.94f, 0.65f, 0.71f), 2.6f);
    }

    // ========== 基础 UI 工具 ==========

    private GameObject CreateUIElement(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
        {
            go.AddComponent<RectTransform>();
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

    private void ApplyChineseFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }
    }

    private static Sprite LoadCachedSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        if (PreviewSpriteCache.TryGetValue(resourcePath, out Sprite cached))
        {
            return cached;
        }

        Sprite loaded = Resources.Load<Sprite>(resourcePath);
        if (loaded != null)
        {
            PreviewSpriteCache[resourcePath] = loaded;
        }

        return loaded;
    }

    private Sprite LoadSavedThumbnailSprite(string thumbnailFileName)
    {
        if (SaveManager.Instance == null || string.IsNullOrWhiteSpace(thumbnailFileName))
        {
            return null;
        }

        string fullPath = SaveManager.Instance.GetThumbnailPath(thumbnailFileName);
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(fullPath);
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!texture.LoadImage(imageBytes))
        {
            Destroy(texture);
            return null;
        }

        texture.name = $"SaveThumb_{thumbnailFileName}";
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        runtimeThumbnailObjects.Add(sprite);
        runtimeThumbnailObjects.Add(texture);
        return sprite;
    }

    private void ApplyQueuedPreviewsImmediately()
    {
        for (int i = 0; i < pendingPreviewRequests.Count; i++)
        {
            PendingPreviewRequest request = pendingPreviewRequests[i];
            if (request == null || request.targetImage == null)
            {
                continue;
            }

            Sprite previewSprite = LoadSlotPreviewSprite(request.data);
            if (previewSprite == null)
            {
                continue;
            }

            request.targetImage.sprite = previewSprite;
            request.targetImage.color = Color.white;
            request.targetImage.preserveAspect = false;

            if (request.fallbackText != null)
            {
                request.fallbackText.gameObject.SetActive(false);
            }
        }

        pendingPreviewRequests.Clear();
    }

    private IEnumerator LoadQueuedPreviews()
    {
        yield return null;

        for (int i = 0; i < pendingPreviewRequests.Count; i++)
        {
            PendingPreviewRequest request = pendingPreviewRequests[i];
            if (request == null || request.targetImage == null)
            {
                continue;
            }

            Sprite previewSprite = LoadSlotPreviewSprite(request.data);
            if (previewSprite != null)
            {
                request.targetImage.sprite = previewSprite;
                request.targetImage.color = Color.white;
                request.targetImage.preserveAspect = false;

                if (request.fallbackText != null)
                {
                    request.fallbackText.gameObject.SetActive(false);
                }
            }

            yield return null;
        }

        pendingPreviewRequests.Clear();
    }

#if UNITY_EDITOR
    public bool EditorPreviewIsBuilt()
    {
        return !Application.isPlaying && editorPreviewBuilt && canvas != null && mainPanelRect != null;
    }

    public void EditorBuildLivePreview(bool previewAsSaveMode)
    {
        if (Application.isPlaying)
        {
            return;
        }

        PreviewSpriteCache.Clear();
        SpringMaskSpriteCache.Clear();
        isSaveMode = previewAsSaveMode;
        editorPreviewSaveMode = previewAsSaveMode;
        useEditorPreviewData = true;
        BuildEditorPreviewData();
        EditorClearLivePreview();
        BuildUI();
        EditorUtility.SetDirty(this);
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void EditorSyncLivePreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (!EditorPreviewIsBuilt())
        {
            EditorBuildLivePreview(editorPreviewSaveMode);
            return;
        }

        PreviewSpriteCache.Clear();
        SpringMaskSpriteCache.Clear();
        layoutContent = LoadLayoutContent();
        ApplyEditorLayoutPreview();
        ApplyQueuedPreviewsImmediately();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public void EditorApplyLayoutPreview()
    {
        if (Application.isPlaying || !EditorPreviewIsBuilt())
        {
            return;
        }

        layoutContent = LoadLayoutContent();
        ApplyEditorLayoutPreview();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    public RectTransform EditorGetLayoutRect(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return editorLayoutRects.TryGetValue(key, out RectTransform rect) ? rect : null;
    }

    private void EditorClearLivePreview()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Component[] components = GetComponents<Component>();
        for (int i = components.Length - 1; i >= 0; i--)
        {
            Component component = components[i];
            if (component is Transform || component is SaveLoadUI)
            {
                continue;
            }

            DestroyImmediate(component);
        }

        for (int i = 0; i < runtimeThumbnailObjects.Count; i++)
        {
            if (runtimeThumbnailObjects[i] != null)
            {
                DestroyImmediate(runtimeThumbnailObjects[i]);
            }
        }

        runtimeThumbnailObjects.Clear();
        pendingPreviewRequests.Clear();
        editorLayoutRects.Clear();
        mainPanelRect = null;
        topOverlayRect = null;
        prevPageButtonRect = null;
        nextPageButtonRect = null;
        returnButtonRect = null;
        canvas = null;
        canvasRect = null;
        confirmDialog = null;
        confirmDialogConfirmButton = null;
        confirmDialogConfirmAction = null;
        confirmDialogCancelAction = null;
        editorPreviewBuilt = false;
    }

    private void ApplyEditorLayoutPreview()
    {
        if (mainPanelRect != null)
        {
            ZhongshanDeckSaveLoadLayoutItem boardItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.LayoutBoard);
            ApplyLayoutToRect(mainPanelRect, boardItem, new Vector2(0.5f, 0.5f), Vector2.zero, CalculatePanelSize());
        }

        if (topOverlayRect != null)
        {
            ZhongshanDeckSaveLoadLayoutItem overlayItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.LayoutTopOverlay);
            ApplyLayoutToRect(topOverlayRect, overlayItem, new Vector2(0.5f, 0.5f), Vector2.zero, mainPanelRect != null ? mainPanelRect.sizeDelta : CalculatePanelSize());
        }

        if (prevPageButtonRect != null)
        {
            ZhongshanDeckSaveLoadLayoutItem prevItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.LayoutPrevPageButton);
            ApplyLayoutToRect(prevPageButtonRect, prevItem, new Vector2(0.5f, 0.5f), ImagePointToLocal(134f, 768f), ImageSizeToLocal(86f, 86f));
        }

        if (nextPageButtonRect != null)
        {
            ZhongshanDeckSaveLoadLayoutItem nextItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.LayoutNextPageButton);
            ApplyLayoutToRect(nextPageButtonRect, nextItem, new Vector2(0.5f, 0.5f), ImagePointToLocal(2624f, 768f), ImageSizeToLocal(86f, 86f));
        }

        for (int slot = 0; slot < SlotCount; slot++)
        {
            string key = GetSlotLayoutKey(slot);
            RectTransform rect = EditorGetLayoutRect(key);
            if (rect == null)
            {
                continue;
            }

            ZhongshanDeckSaveLoadLayoutItem item = GetLayoutItem(key);
            ApplyLayoutToRect(rect, item, new Vector2(0.5f, 0.5f), GetSlotCenter(slot), new Vector2(SlotWidth, SlotHeight));

            RectTransform photoRect = EditorGetLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotPhotoKey(slot));
            if (photoRect != null)
            {
                Rect fallback = GetSlotPhotoFallbackRect(slot);
                ZhongshanDeckSaveLoadLayoutItem photoItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.GetSlotPhotoKey(slot));
                ApplyLayoutToRect(photoRect, photoItem, new Vector2(0.5f, 0.5f), new Vector2(fallback.center.x, fallback.center.y), new Vector2(fallback.width, fallback.height));
            }

            RectTransform infoRect = EditorGetLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotInfoKey(slot));
            if (infoRect != null)
            {
                Rect fallback = GetSlotInfoFallbackRect(slot);
                ZhongshanDeckSaveLoadLayoutItem infoItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.GetSlotInfoKey(slot));
                ApplyLayoutToRect(infoRect, infoItem, new Vector2(0f, 1f), new Vector2(fallback.xMin, fallback.yMin), new Vector2(fallback.width, fallback.height));
            }

            RectTransform buttonsRect = EditorGetLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotButtonsKey(slot));
            if (buttonsRect != null)
            {
                Rect fallback = GetSlotButtonsFallbackRect(slot);
                ZhongshanDeckSaveLoadLayoutItem buttonsItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.GetSlotButtonsKey(slot));
                ApplyLayoutToRect(buttonsRect, buttonsItem, new Vector2(0f, 1f), new Vector2(fallback.xMin, fallback.yMin), new Vector2(fallback.width, fallback.height));
            }

            RectTransform primaryButtonRect = EditorGetLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotPrimaryButtonKey(slot));
            if (primaryButtonRect != null)
            {
                Rect fallback = GetSlotPrimaryButtonFallbackRect(slot);
                ZhongshanDeckSaveLoadLayoutItem primaryItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.GetSlotPrimaryButtonKey(slot));
                ApplyLayoutToRect(primaryButtonRect, primaryItem, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(fallback.width, fallback.height));
            }

            RectTransform leftButtonRect = EditorGetLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotLeftButtonKey(slot));
            if (leftButtonRect != null)
            {
                Rect fallback = GetSlotLeftButtonFallbackRect(slot);
                ZhongshanDeckSaveLoadLayoutItem leftItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.GetSlotLeftButtonKey(slot));
                ApplyLayoutToRect(leftButtonRect, leftItem, new Vector2(0.5f, 0.5f), new Vector2(fallback.center.x, fallback.center.y), new Vector2(fallback.width, fallback.height));
            }

            RectTransform rightButtonRect = EditorGetLayoutRect(ZhongshanDeckSaveLoadContentDefaults.GetSlotRightButtonKey(slot));
            if (rightButtonRect != null)
            {
                Rect fallback = GetSlotRightButtonFallbackRect(slot);
                ZhongshanDeckSaveLoadLayoutItem rightItem = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.GetSlotRightButtonKey(slot));
                ApplyLayoutToRect(rightButtonRect, rightItem, new Vector2(0.5f, 0.5f), new Vector2(fallback.center.x, fallback.center.y), new Vector2(fallback.width, fallback.height));
            }
        }

        if (returnButtonRect != null)
        {
            ZhongshanDeckSaveLoadLayoutItem item = GetLayoutItem(ZhongshanDeckSaveLoadContentDefaults.LayoutReturnButton);
            ApplyLayoutToRect(returnButtonRect, item, new Vector2(1f, 1f), new Vector2(-126f, -126f), new Vector2(170f, 86f));
        }
    }

    private void BuildEditorPreviewData()
    {
        editorPreviewSlotData.Clear();
        editorPreviewSlotData[0] = CreateEditorPreviewSaveData("林见书", 1, 2, 4, 10, 0, "Library", "中文系", 12340, 24510f, "2026-05-06T21:10:00");
        editorPreviewSlotData[1] = CreateEditorPreviewSaveData("陈望秋", 2, 1, 2, 9, 1, "Dormitory", "新闻传播", 8750, 18240f, "2026-05-05T18:32:00");
        editorPreviewSlotData[2] = CreateEditorPreviewSaveData("苏青禾", 3, 2, 5, 7, 1, "TeachingBuilding", "计算机", -320, 33480f, "2026-05-04T23:08:00");
        editorPreviewSlotData[3] = null;
    }

    private SaveData CreateEditorPreviewSaveData(string playerName, int year, int semester, int round, int month, int gender, string location, string major, int money, float playSeconds, string saveTime)
    {
        SaveData data = new SaveData
        {
            playerName = playerName,
            playerGender = gender % 2,
            playerMajor = major,
            currentYear = year,
            currentSemester = semester,
            currentRound = round,
            currentMonth = month,
            currentLocation = location,
            money = money,
            actionPoints = 12,
            study = 72 + year * 4,
            charm = 55 + semester * 3,
            physique = 48 + round,
            leadership = 36 + year * 2,
            stress = 28 + semester * 4,
            mood = 68 - round,
            luck = 61,
            totalPlayTimeSeconds = playSeconds,
            meta = new SaveMetaInfo
            {
                saveTime = saveTime
            }
        };

        data.semesterGPAHistory = new List<SemesterGPA>
        {
            new SemesterGPA { year = Mathf.Max(1, year - 1), semester = 1, gpa = 3.42f },
            new SemesterGPA { year = year, semester = semester, gpa = 3.76f }
        };
        data.EnsureInitialized();
        return data;
    }
#endif
}
