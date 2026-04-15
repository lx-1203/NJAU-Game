using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 存档/读档 UI —— 全屏覆盖式界面，支持存档模式和读档模式
/// 所有 UI 通过代码动态创建，遵循项目 HUDBuilder 风格
/// </summary>
public class SaveLoadUI : MonoBehaviour
{
    // ========== 常量 ==========

    private const int SlotCount = 4; // 0=auto, 1-3=manual
    private const int CanvasSortOrder = 150;

    // 颜色方案（匹配 HUDBuilder 风格）
    private static readonly Color BgColor       = new Color(0.08f, 0.08f, 0.12f, 0.92f);
    private static readonly Color CardColor     = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    private static readonly Color CardHoverColor = new Color(0.18f, 0.18f, 0.26f, 0.95f);
    private static readonly Color CardEmptyColor = new Color(0.10f, 0.10f, 0.14f, 0.80f);
    private static readonly Color CardDisabledColor = new Color(0.08f, 0.08f, 0.10f, 0.70f);
    private static readonly Color TextWhite     = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGray      = new Color(0.50f, 0.50f, 0.55f);
    private static readonly Color TextRed       = new Color(0.90f, 0.30f, 0.30f);
    private static readonly Color ButtonPrimary = new Color(0.24f, 0.46f, 0.88f);
    private static readonly Color ButtonDanger  = new Color(0.80f, 0.20f, 0.20f);
    private static readonly Color OverlayDim    = new Color(0f, 0f, 0f, 0.60f);

    // ========== 运行时状态 ==========

    private bool isSaveMode;
    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject confirmDialog;

    // ========== 静态入口 ==========

    /// <summary>
    /// 显示存档/读档界面
    /// </summary>
    /// <param name="isSaveMode">true=存档模式, false=读档模式</param>
    public static void Show(bool isSaveMode)
    {
        // 避免重复创建
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

    // ========== UI 构建 ==========

    private void BuildUI()
    {
        // 创建 Canvas
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

        // 半透明背景遮罩
        CreateOverlayBackground();

        // 主面板
        GameObject mainPanel = CreateMainPanel();
        RectTransform mainPanelRT = mainPanel.GetComponent<RectTransform>();

        // 标题
        CreateTitle(mainPanelRT);

        // 存档槽位卡片
        CreateSlotCards(mainPanelRT);

        // 返回按钮
        CreateBackButton(mainPanelRT);
    }

    private void CreateOverlayBackground()
    {
        GameObject bg = CreateUIElement("OverlayBg", canvasRect);
        StretchFull(bg.GetComponent<RectTransform>());

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = OverlayDim;
        bgImage.raycastTarget = true; // 阻挡下层点击
    }

    private GameObject CreateMainPanel()
    {
        GameObject panel = CreateUIElement("MainPanel", canvasRect);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700f, 780f);
        rt.anchoredPosition = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = BgColor;

        return panel;
    }

    private void CreateTitle(RectTransform parent)
    {
        GameObject titleGO = CreateUIElement("Title", parent);
        RectTransform rt = titleGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(600f, 80f);
        rt.anchoredPosition = new Vector2(0f, -20f);

        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = isSaveMode ? "存档" : "读档";
        titleText.fontSize = 42;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = TextWhite;
        titleText.raycastTarget = false;
        ApplyChineseFont(titleText);
    }

    private void CreateSlotCards(RectTransform parent)
    {
        SaveMetaInfo[] metas = null;
        if (SaveManager.Instance != null)
        {
            metas = SaveManager.Instance.GetAllSlotMetas();
        }

        float cardHeight = 120f;
        float cardSpacing = 16f;
        float startY = -120f; // 标题下方

        for (int i = 0; i < SlotCount; i++)
        {
            int slot = i;
            SaveMetaInfo meta = (metas != null && i < metas.Length) ? metas[i] : null;
            float yPos = startY - i * (cardHeight + cardSpacing);

            CreateSlotCard(parent, slot, meta, new Vector2(0f, yPos), cardHeight);
        }
    }

    private void CreateSlotCard(RectTransform parent, int slot, SaveMetaInfo meta, Vector2 position, float height)
    {
        bool isEmpty = (meta == null);
        bool isAutoSlot = (slot == 0);
        bool isDisabled = (isSaveMode && isAutoSlot); // 存档模式下自动存档槽不可操作

        // 卡片容器
        GameObject card = CreateUIElement($"SlotCard_{slot}", parent);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 1f);
        cardRT.anchorMax = new Vector2(0.5f, 1f);
        cardRT.pivot = new Vector2(0.5f, 1f);
        cardRT.sizeDelta = new Vector2(620f, height);
        cardRT.anchoredPosition = position;

        Image cardBg = card.AddComponent<Image>();
        if (isDisabled)
        {
            cardBg.color = CardDisabledColor;
        }
        else if (isEmpty)
        {
            cardBg.color = CardEmptyColor;
        }
        else
        {
            cardBg.color = CardColor;
        }

        // 可点击按钮（非禁用状态）
        if (!isDisabled)
        {
            Button cardBtn = card.AddComponent<Button>();
            cardBtn.targetGraphic = cardBg;
            ColorBlock cb = cardBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cardBtn.colors = cb;

            int capturedSlot = slot;
            bool capturedEmpty = isEmpty;
            cardBtn.onClick.AddListener(() => OnSlotClicked(capturedSlot, capturedEmpty));
        }

        // ---- 卡片内容 ----

        // 槽位标签
        string slotLabel = isAutoSlot ? "自动存档" : $"存档 {slot}";
        GameObject labelGO = CreateUIElement("SlotLabel", cardRT);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 1f);
        labelRT.anchorMax = new Vector2(0f, 1f);
        labelRT.pivot = new Vector2(0f, 1f);
        labelRT.sizeDelta = new Vector2(200f, 36f);
        labelRT.anchoredPosition = new Vector2(20f, -12f);

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = slotLabel;
        labelText.fontSize = 24;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = isDisabled ? TextGray : TextWhite;
        labelText.raycastTarget = false;
        ApplyChineseFont(labelText);

        if (isEmpty)
        {
            // 空槽位提示
            GameObject emptyGO = CreateUIElement("EmptyHint", cardRT);
            RectTransform emptyRT = emptyGO.GetComponent<RectTransform>();
            emptyRT.anchorMin = new Vector2(0.5f, 0.5f);
            emptyRT.anchorMax = new Vector2(0.5f, 0.5f);
            emptyRT.pivot = new Vector2(0.5f, 0.5f);
            emptyRT.sizeDelta = new Vector2(400f, 40f);
            emptyRT.anchoredPosition = new Vector2(0f, -8f);

            TextMeshProUGUI emptyText = emptyGO.AddComponent<TextMeshProUGUI>();
            emptyText.text = "— 空 —";
            emptyText.fontSize = 26;
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.color = TextGray;
            emptyText.raycastTarget = false;
            ApplyChineseFont(emptyText);
        }
        else
        {
            // 进度描述
            GameObject progressGO = CreateUIElement("Progress", cardRT);
            RectTransform progressRT = progressGO.GetComponent<RectTransform>();
            progressRT.anchorMin = new Vector2(0f, 0f);
            progressRT.anchorMax = new Vector2(0f, 0f);
            progressRT.pivot = new Vector2(0f, 0f);
            progressRT.sizeDelta = new Vector2(350f, 30f);
            progressRT.anchoredPosition = new Vector2(20f, 16f);

            TextMeshProUGUI progressText = progressGO.AddComponent<TextMeshProUGUI>();
            progressText.text = meta.progressDesc ?? "未知进度";
            progressText.fontSize = 20;
            progressText.alignment = TextAlignmentOptions.Left;
            progressText.color = new Color(0.75f, 0.80f, 0.90f);
            progressText.raycastTarget = false;
            ApplyChineseFont(progressText);

            // 存档时间
            GameObject timeGO = CreateUIElement("SaveTime", cardRT);
            RectTransform timeRT = timeGO.GetComponent<RectTransform>();
            timeRT.anchorMin = new Vector2(1f, 1f);
            timeRT.anchorMax = new Vector2(1f, 1f);
            timeRT.pivot = new Vector2(1f, 1f);
            timeRT.sizeDelta = new Vector2(280f, 30f);
            timeRT.anchoredPosition = new Vector2(-60f, -14f);

            TextMeshProUGUI timeText = timeGO.AddComponent<TextMeshProUGUI>();
            timeText.text = FormatSaveTime(meta.saveTime);
            timeText.fontSize = 18;
            timeText.alignment = TextAlignmentOptions.Right;
            timeText.color = TextGray;
            timeText.raycastTarget = false;
            ApplyChineseFont(timeText);

            // 游戏时长
            GameObject playTimeGO = CreateUIElement("PlayTime", cardRT);
            RectTransform playTimeRT = playTimeGO.GetComponent<RectTransform>();
            playTimeRT.anchorMin = new Vector2(1f, 0f);
            playTimeRT.anchorMax = new Vector2(1f, 0f);
            playTimeRT.pivot = new Vector2(1f, 0f);
            playTimeRT.sizeDelta = new Vector2(200f, 26f);
            playTimeRT.anchoredPosition = new Vector2(-60f, 16f);

            TextMeshProUGUI playTimeText = playTimeGO.AddComponent<TextMeshProUGUI>();
            playTimeText.text = FormatPlayTime(meta.playTimeSeconds);
            playTimeText.fontSize = 16;
            playTimeText.alignment = TextAlignmentOptions.Right;
            playTimeText.color = TextGray;
            playTimeText.raycastTarget = false;
            ApplyChineseFont(playTimeText);

            // 删除按钮（红色 X）
            if (!isDisabled)
            {
                CreateDeleteButton(cardRT, slot);
            }
        }

        // 存档模式下自动存档槽位标记
        if (isDisabled)
        {
            GameObject disabledGO = CreateUIElement("DisabledMark", cardRT);
            RectTransform disabledRT = disabledGO.GetComponent<RectTransform>();
            disabledRT.anchorMin = new Vector2(1f, 0.5f);
            disabledRT.anchorMax = new Vector2(1f, 0.5f);
            disabledRT.pivot = new Vector2(1f, 0.5f);
            disabledRT.sizeDelta = new Vector2(120f, 30f);
            disabledRT.anchoredPosition = new Vector2(-16f, 0f);

            TextMeshProUGUI disabledText = disabledGO.AddComponent<TextMeshProUGUI>();
            disabledText.text = "(仅自动)";
            disabledText.fontSize = 16;
            disabledText.alignment = TextAlignmentOptions.Right;
            disabledText.color = TextGray;
            disabledText.raycastTarget = false;
            ApplyChineseFont(disabledText);
        }
    }

    private void CreateDeleteButton(RectTransform cardRT, int slot)
    {
        GameObject delGO = CreateUIElement("DeleteBtn", cardRT);
        RectTransform delRT = delGO.GetComponent<RectTransform>();
        delRT.anchorMin = new Vector2(1f, 0.5f);
        delRT.anchorMax = new Vector2(1f, 0.5f);
        delRT.pivot = new Vector2(1f, 0.5f);
        delRT.sizeDelta = new Vector2(40f, 40f);
        delRT.anchoredPosition = new Vector2(-10f, 0f);

        Image delBg = delGO.AddComponent<Image>();
        delBg.color = new Color(0.60f, 0.15f, 0.15f, 0.80f);

        Button delBtn = delGO.AddComponent<Button>();
        delBtn.targetGraphic = delBg;
        ColorBlock dcb = delBtn.colors;
        dcb.normalColor = Color.white;
        dcb.highlightedColor = new Color(1.2f, 1.0f, 1.0f, 1f);
        dcb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        delBtn.colors = dcb;

        // X 文字
        GameObject xGO = CreateUIElement("X", delRT);
        StretchFull(xGO.GetComponent<RectTransform>());
        TextMeshProUGUI xText = xGO.AddComponent<TextMeshProUGUI>();
        xText.text = "✕";
        xText.fontSize = 22;
        xText.alignment = TextAlignmentOptions.Center;
        xText.color = TextWhite;
        xText.raycastTarget = false;

        int capturedSlot = slot;
        delBtn.onClick.AddListener(() => OnDeleteClicked(capturedSlot));
    }

    private void CreateBackButton(RectTransform parent)
    {
        GameObject btnGO = CreateUIElement("BackButton", parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(200f, 56f);
        rt.anchoredPosition = new Vector2(0f, 24f);

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = ButtonPrimary;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(Close);

        // 按钮文字
        GameObject textGO = CreateUIElement("Label", rt);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "返回";
        text.fontSize = 26;
        text.alignment = TextAlignmentOptions.Center;
        text.color = TextWhite;
        text.raycastTarget = false;
        ApplyChineseFont(text);
    }

    // ========== 交互逻辑 ==========

    private void OnSlotClicked(int slot, bool isEmpty)
    {
        if (isSaveMode)
        {
            // 存档模式
            if (isEmpty)
            {
                // 空槽位直接存档
                DoSave(slot);
            }
            else
            {
                // 已有存档，确认覆盖
                ShowConfirmDialog("确认覆盖此存档？", () => DoSave(slot));
            }
        }
        else
        {
            // 读档模式
            if (isEmpty)
            {
                // 空槽位无法读档
                return;
            }
            ShowConfirmDialog("确认加载此存档？\n当前未保存的进度将丢失", () => DoLoad(slot));
        }
    }

    private void OnDeleteClicked(int slot)
    {
        ShowConfirmDialog("确认删除此存档？\n此操作不可撤销", () =>
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSlot(slot);
                Debug.Log($"[SaveLoadUI] 已删除存档槽位{slot}");
            }
            // 刷新界面
            RefreshUI();
        });
    }

    private void DoSave(int slot)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveToSlot(slot);
            Debug.Log($"[SaveLoadUI] 已保存到槽位{slot}");
        }
        RefreshUI();
    }

    private void DoLoad(int slot)
    {
        if (SaveManager.Instance == null) return;

        SaveData data = SaveManager.Instance.LoadFromSlot(slot);
        if (data == null)
        {
            Debug.LogWarning("[SaveLoadUI] 读档失败");
            return;
        }

        // 设置跨场景加载数据
        SaveManager.PendingLoadData = data;

        Debug.Log($"[SaveLoadUI] 已设置 PendingLoadData，准备跳转 GameScene");
        Close();

        // 跳转到游戏场景
        SceneLoader.LoadScene("GameScene");
    }

    private void Close()
    {
        Destroy(gameObject);
    }

    private void RefreshUI()
    {
        // 销毁旧 UI 并重新创建
        bool mode = isSaveMode;

        // 清除确认对话框
        if (confirmDialog != null)
        {
            Destroy(confirmDialog);
            confirmDialog = null;
        }

        // 删除所有子对象（除了Canvas组件本身的）
        // 最简单的方式：销毁并重建
        Destroy(gameObject);
        Show(mode);
    }

    // ========== 确认对话框 ==========

    private void ShowConfirmDialog(string message, Action onConfirm)
    {
        // 清除旧对话框
        if (confirmDialog != null)
        {
            Destroy(confirmDialog);
        }

        // 对话框遮罩
        confirmDialog = CreateUIElement("ConfirmDialog", canvasRect);
        StretchFull(confirmDialog.GetComponent<RectTransform>());

        Image dimBg = confirmDialog.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.50f);
        dimBg.raycastTarget = true;

        // 对话框面板
        GameObject dialogPanel = CreateUIElement("DialogPanel", confirmDialog.GetComponent<RectTransform>());
        RectTransform panelRT = dialogPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(440f, 240f);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = dialogPanel.AddComponent<Image>();
        panelBg.color = new Color(0.10f, 0.10f, 0.16f, 0.98f);

        // 消息文字
        GameObject msgGO = CreateUIElement("Message", panelRT);
        RectTransform msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.5f, 1f);
        msgRT.anchorMax = new Vector2(0.5f, 1f);
        msgRT.pivot = new Vector2(0.5f, 1f);
        msgRT.sizeDelta = new Vector2(380f, 120f);
        msgRT.anchoredPosition = new Vector2(0f, -24f);

        TextMeshProUGUI msgText = msgGO.AddComponent<TextMeshProUGUI>();
        msgText.text = message;
        msgText.fontSize = 24;
        msgText.alignment = TextAlignmentOptions.Center;
        msgText.color = TextWhite;
        msgText.raycastTarget = false;
        ApplyChineseFont(msgText);

        // 按钮容器
        float btnY = -170f;
        float btnWidth = 140f;
        float btnHeight = 48f;
        float btnSpacing = 30f;

        // 确认按钮
        CreateDialogButton(panelRT, "ConfirmBtn", "确认", ButtonPrimary,
            new Vector2(-btnSpacing - btnWidth / 2f + btnWidth / 2f, btnY),
            new Vector2(btnWidth, btnHeight), () =>
            {
                Destroy(confirmDialog);
                confirmDialog = null;
                onConfirm?.Invoke();
            });

        // 取消按钮
        CreateDialogButton(panelRT, "CancelBtn", "取消", new Color(0.40f, 0.40f, 0.45f),
            new Vector2(btnSpacing + btnWidth / 2f - btnWidth / 2f, btnY),
            new Vector2(btnWidth, btnHeight), () =>
            {
                Destroy(confirmDialog);
                confirmDialog = null;
            });

        // 确保对话框在最上层
        confirmDialog.transform.SetAsLastSibling();
    }

    private void CreateDialogButton(RectTransform parent, string name, string label,
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
        cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // 按钮文字
        GameObject textGO = CreateUIElement("Label", rt);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.color = TextWhite;
        text.raycastTarget = false;
        ApplyChineseFont(text);
    }

    // ========== 工具方法 ==========

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

    private void ApplyChineseFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }
    }

    /// <summary>
    /// 格式化存档时间为可读字符串
    /// </summary>
    private string FormatSaveTime(string isoTime)
    {
        if (string.IsNullOrEmpty(isoTime)) return "未知时间";

        try
        {
            DateTime dt = DateTime.Parse(isoTime);
            return dt.ToString("yyyy/MM/dd HH:mm");
        }
        catch
        {
            return isoTime;
        }
    }

    /// <summary>
    /// 格式化游戏时长为 "XXh XXm" 格式
    /// </summary>
    private string FormatPlayTime(float seconds)
    {
        int totalMinutes = Mathf.FloorToInt(seconds / 60f);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours > 0)
        {
            return $"游戏时长 {hours}h {minutes:D2}m";
        }
        else
        {
            return $"游戏时长 {minutes}m";
        }
    }
}
