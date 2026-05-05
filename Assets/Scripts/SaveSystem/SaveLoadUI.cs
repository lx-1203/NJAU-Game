using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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

    // 当前显示的槽位页（预留翻页功能）
    private int currentPage = 0;

    private static Sprite cachedSpringBoardSprite;
    private static readonly Dictionary<string, Sprite> PreviewSpriteCache = new Dictionary<string, Sprite>();

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
            Close();
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < runtimeThumbnailObjects.Count; i++)
        {
            if (runtimeThumbnailObjects[i] != null)
            {
                Destroy(runtimeThumbnailObjects[i]);
            }
        }

        runtimeThumbnailObjects.Clear();
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

    // ========== UI 构建总入口 ==========

    private void BuildUI()
    {
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

        springBoardSprite = LoadCachedSprite("UI/SaveLoadSpringBg", ref cachedSpringBoardSprite);

        // 2. 主面板（春季素材底图）
        GameObject mainPanel = CreateMainPanel();
        RectTransform mainRT = mainPanel.GetComponent<RectTransform>();

        // 3. 四格存档覆盖层
        CreateCardGrid(mainRT);

        // 4. 返回按钮热区
        CreateReturnButton(mainRT);

        if (pendingPreviewRequests.Count > 0)
        {
            StartCoroutine(LoadQueuedPreviews());
        }
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
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

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

    // ========== 4. 卡片网格 (2x2) ==========

    private void CreateCardGrid(RectTransform parent)
    {
        SaveData[] slotData = new SaveData[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            slotData[i] = SaveManager.Instance?.GetSlotSaveData(i);
        }

        CreateSlotOverlay(parent, 0, slotData[0], GetSlotCenter(0), AutoPink, true);
        CreateSlotOverlay(parent, 1, slotData[1], GetSlotCenter(1), Slot01Pink, false);
        CreateSlotOverlay(parent, 2, slotData[2], GetSlotCenter(2), Slot02Green, false);
        CreateSlotOverlay(parent, 3, slotData[3], GetSlotCenter(3), Slot03Yellow, false);
    }

    private void CreateSlotOverlay(RectTransform parent, int slot, SaveData data,
        Vector2 position, Color themeColor, bool isAutoSlot)
    {
        bool isEmpty = (data == null);

        GameObject slotRoot = CreateUIElement($"SlotOverlay_{slot}", parent);
        RectTransform slotRT = slotRoot.GetComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0.5f, 0.5f);
        slotRT.anchorMax = new Vector2(0.5f, 0.5f);
        slotRT.pivot = new Vector2(0.5f, 0.5f);
        slotRT.sizeDelta = new Vector2(SlotWidth, SlotHeight);
        slotRT.anchoredPosition = position;

        Image hitArea = slotRoot.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.01f);
        hitArea.raycastTarget = true;

        Button slotButton = slotRoot.AddComponent<Button>();
        slotButton.targetGraphic = hitArea;
        ColorBlock cb = slotButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.14f);
        slotButton.colors = cb;
        slotButton.transition = Selectable.Transition.ColorTint;
        slotButton.onClick.AddListener(() => OnSlotClicked(slot, isEmpty));

        CreateSlotPhoto(slotRT, data, isEmpty);
        CreateInfoTextArea(slotRT, data, isEmpty, themeColor);
        CreateCardButtons(slotRT, slot, isEmpty, isAutoSlot, themeColor);

        if (!isSaveMode && isEmpty)
        {
            slotButton.interactable = false;
        }
    }

    // ---- 信息文字区 ----

    private void CreateSlotPhoto(RectTransform slotRT, SaveData data, bool isEmpty)
    {
        GameObject photoFrame = CreateUIElement("PhotoFrame", slotRT);
        RectTransform frameRT = photoFrame.GetComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0f, 0.5f);
        frameRT.anchorMax = new Vector2(0f, 0.5f);
        frameRT.pivot = new Vector2(0f, 0.5f);
        frameRT.sizeDelta = new Vector2(PhotoFrameWidth, PhotoFrameHeight);
        frameRT.anchoredPosition = new Vector2(34f, 10f);

        Image frameImg = photoFrame.AddComponent<Image>();
        frameImg.color = new Color(0.98f, 0.96f, 0.93f, 0.82f);
        frameImg.raycastTarget = false;

        GameObject photoInner = CreateUIElement("PhotoInner", frameRT);
        RectTransform innerRT = photoInner.GetComponent<RectTransform>();
        innerRT.anchorMin = new Vector2(0.5f, 0.5f);
        innerRT.anchorMax = new Vector2(0.5f, 0.5f);
        innerRT.pivot = new Vector2(0.5f, 0.5f);
        innerRT.sizeDelta = new Vector2(PhotoWidth, PhotoHeight);
        innerRT.anchoredPosition = Vector2.zero;

        Image photoImg = photoInner.AddComponent<Image>();
        photoImg.raycastTarget = false;

        photoImg.color = PhotoInnerGray;

        GameObject fallbackGO = CreateUIElement("FallbackLabel", innerRT);
        StretchFull(fallbackGO.GetComponent<RectTransform>());
        TextMeshProUGUI fallbackText = fallbackGO.AddComponent<TextMeshProUGUI>();
        fallbackText.text = isEmpty ? "空位" : GetLocationName(data.currentLocation);
        fallbackText.fontSize = 22f;
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

    private void CreateInfoTextArea(RectTransform slotRT, SaveData data, bool isEmpty, Color themeColor)
    {
        float infoStartX = 246f;
        float infoWidth = 188f;

        if (isEmpty)
        {
            GameObject emptyGO = CreateUIElement("EmptyHint", slotRT);
            RectTransform emptyRT = emptyGO.GetComponent<RectTransform>();
            emptyRT.anchorMin = new Vector2(0f, 1f);
            emptyRT.anchorMax = new Vector2(0f, 1f);
            emptyRT.pivot = new Vector2(0f, 1f);
            emptyRT.sizeDelta = new Vector2(infoWidth, 112f);
            emptyRT.anchoredPosition = new Vector2(infoStartX, -24f);

            TextMeshProUGUI emptyText = emptyGO.AddComponent<TextMeshProUGUI>();
            emptyText.text = isSaveMode
                ? "空存档位\n<size=18><color=#8B7355>点击此处保存当前进度</color></size>"
                : "空存档位\n<size=18><color=#8B7355>暂无存档记录</color></size>";
            emptyText.fontSize = 20;
            emptyText.alignment = TextAlignmentOptions.Left;
            emptyText.color = TextGray;
            emptyText.raycastTarget = false;
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

        CreateAccentDot(slotRT, new Vector2(infoStartX - 12f, -22f), themeColor);

        float lineH = 20f;
        float startY = -22f;
        int lineIdx = 0;

        CreateInfoLine(slotRT, $"{playerName}，{age}岁",
            infoStartX, startY - lineIdx * lineH, infoWidth, 18f, TextBrown, FontStyles.Bold);
        lineIdx++;

        CreateInfoLine(slotRT, $"学年：{yearSeason}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 14f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(slotRT, $"季节：{season}    专业：{major}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 11.5f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(slotRT, $"地点：{locationName}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 13f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(slotRT, $"金钱：{moneyStr}   GPA：{gpa:F2}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 13f, TextBrownLight, FontStyles.Bold);
        lineIdx++;

        CreateInfoLine(slotRT, $"核心：学{data.study} 魅{data.charm} 体{data.physique} 领{data.leadership}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 10.5f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(slotRT, $"状态：心情{data.mood}  压力{data.stress}  幸运{data.luck}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 10.5f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(slotRT, $"回合：{data.currentRound}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 13f, TextBrownLight);
        lineIdx++;

        CreateInfoLine(slotRT, $"存档时间：{saveTime}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 10.5f, TextGray);
        lineIdx++;

        CreateInfoLine(slotRT, $"游玩时长：{playTime}",
            infoStartX, startY - lineIdx * lineH, infoWidth, 10.5f, TextGray);
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
        rt.sizeDelta = new Vector2(width, fontSize + 4f);
        rt.anchoredPosition = new Vector2(x, y);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
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

    private void CreateCardButtons(RectTransform cardRT, int slot, bool isEmpty,
        bool isAutoSlot, Color themeColor)
    {
        float btnY = 18f;
        float btnX = 76f;

        if (isAutoSlot)
        {
            bool canClick = !isSaveMode && !isEmpty;
            CreateCardButton(cardRT, "自动存档", new Vector2(btnX, btnY),
                new Vector2(138f, 34f), themeColor,
                canClick ? () => OnSlotClicked(slot, false) : null);
        }
        else
        {
            string slotLabel = $"存档 {slot:D2}";
            CreateCardButton(cardRT, slotLabel, new Vector2(btnX, btnY),
                new Vector2(102f, 34f), themeColor,
                isSaveMode ? () => OnSlotClicked(slot, isEmpty) : null);

            CreateCardButton(cardRT, "负载", new Vector2(btnX + 114f, btnY),
                new Vector2(82f, 34f), themeColor,
                (!isSaveMode && !isEmpty) ? () => OnSlotClicked(slot, false) : null);
        }
    }

    private Button CreateCardButton(RectTransform parent, string label,
        Vector2 position, Vector2 size, Color bgColor, Action onClick)
    {
        GameObject btnGO = CreateUIElement($"Btn_{label}", parent);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0f, 0f);
        btnRT.anchorMax = new Vector2(0f, 0f);
        btnRT.pivot = new Vector2(0f, 0f);
        btnRT.sizeDelta = size;
        btnRT.anchoredPosition = position;

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.94f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f);
        cb.disabledColor = new Color(0.70f, 0.70f, 0.70f);
        btn.colors = cb;

        if (onClick != null)
        {
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
        else
        {
            btn.interactable = false;
            btnBg.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.42f);
        }

        // 按钮文字
        GameObject textGO = CreateUIElement("Label", btnRT);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextBrown;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        ApplyChineseFont(tmp);

        return btn;
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
        GameObject btnGO = CreateUIElement("ReturnButton", parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(170f, 86f);
        rt.anchoredPosition = new Vector2(-126f, -126f);

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(1f, 1f, 1f, 0.01f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        btn.colors = cb;
        btn.onClick.AddListener(Close);
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
        Close();
        SceneLoader.LoadScene("GameScene");
    }

    private void Close()
    {
        Destroy(gameObject);
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
        Close();
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
        if (springBoardSprite == null)
        {
            return new Vector2(PanelMaxWidth, PanelMaxHeight);
        }

        Rect spriteRect = springBoardSprite.rect;
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
        switch (slot)
        {
            case 0: return new Vector2(-268f, 96f);
            case 1: return new Vector2(284f, 96f);
            case 2: return new Vector2(-268f, -206f);
            case 3: return new Vector2(284f, -206f);
            default: return Vector2.zero;
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

    private void CreateAccentDot(RectTransform parent, Vector2 anchoredPosition, Color color)
    {
        GameObject dotGO = CreateUIElement("AccentDot", parent);
        RectTransform rt = dotGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(9f, 9f);
        rt.anchoredPosition = anchoredPosition;

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

    private static Sprite LoadCachedSprite(string resourcePath, ref Sprite cacheField)
    {
        if (cacheField != null)
        {
            return cacheField;
        }

        cacheField = LoadCachedSprite(resourcePath);
        return cacheField;
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
                request.targetImage.preserveAspect = true;

                if (request.fallbackText != null)
                {
                    request.fallbackText.gameObject.SetActive(false);
                }
            }

            yield return null;
        }

        pendingPreviewRequests.Clear();
    }
}
