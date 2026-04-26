using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// 通用确认弹窗组件 - 纯代码动态构建UI
/// 单例模式, DontDestroyOnLoad
/// Canvas sortingOrder = 600 (最高层级, 覆盖所有UI)
/// </summary>
public class ConfirmDialogUI : MonoBehaviour
{
    // ========== 单例 ==========
    public static ConfirmDialogUI Instance { get; private set; }

    // ========== 常量 ==========
    private const int CanvasSortOrder = 600;
    private const float PanelWidth = 500f;
    private const float PanelHeight = 300f;
    private const float ButtonWidth = 180f;
    private const float ButtonHeight = 45f;
    private const float AnimDuration = 0.15f;

    // 颜色方案
    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.60f);
    private static readonly Color PanelBgColor = new Color(0.10f, 0.10f, 0.16f, 0.98f);
    private static readonly Color TitleColor = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color ContentColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color BtnConfirmColor = new Color(0.20f, 0.35f, 0.60f);
    private static readonly Color BtnConfirmHover = new Color(0.28f, 0.43f, 0.70f);
    private static readonly Color BtnCancelColor = new Color(0.25f, 0.25f, 0.30f);
    private static readonly Color BtnCancelHover = new Color(0.33f, 0.33f, 0.40f);
    private static readonly Color BtnTextColor = new Color(0.95f, 0.95f, 0.95f);

    // ========== 运行时状态 ==========
    private GameObject rootCanvasObj;
    private GameObject panelObj;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI contentText;
    private Button confirmButton;
    private Button cancelButton;
    private GameObject cancelButtonObj;
    private bool isOpen;

    private Action onConfirmCallback;
    private Action onCancelCallback;

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
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancelClicked();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== 公共 API ==========

    /// <summary>
    /// 显示确认弹窗（确认+取消两个按钮）
    /// </summary>
    /// <param name="title">标题文本</param>
    /// <param name="message">内容文本</param>
    /// <param name="onConfirm">确认回调</param>
    /// <param name="onCancel">取消回调（可选）</param>
    public static void Show(string title, string message, Action onConfirm, Action onCancel = null)
    {
        EnsureInstance();
        Instance.ShowInternal(title, message, onConfirm, onCancel, showCancel: true);
    }

    /// <summary>
    /// 显示仅有确认按钮的信息弹窗
    /// </summary>
    /// <param name="title">标题文本</param>
    /// <param name="message">内容文本</param>
    /// <param name="onClose">关闭回调（可选）</param>
    public static void ShowInfo(string title, string message, Action onClose = null)
    {
        EnsureInstance();
        Instance.ShowInternal(title, message, onClose, null, showCancel: false);
    }

    /// <summary>
    /// 关闭弹窗
    /// </summary>
    public void Hide()
    {
        isOpen = false;
        onConfirmCallback = null;
        onCancelCallback = null;

        if (rootCanvasObj != null)
        {
            Destroy(rootCanvasObj);
            rootCanvasObj = null;
        }
    }

    public bool IsOpen => isOpen;

    // ========== 内部实现 ==========

    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("ConfirmDialogUI");
            obj.AddComponent<ConfirmDialogUI>();
        }
    }

    private void ShowInternal(string title, string message, Action onConfirm, Action onCancel, bool showCancel)
    {
        UIFlowGuard.EnsureEventSystem();

        // 如果已经打开, 先销毁旧UI
        if (rootCanvasObj != null) Destroy(rootCanvasObj);

        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        BuildUI(title, message, showCancel);
        isOpen = true;

        // 缩放动画
        StartCoroutine(PlayOpenAnimation());
    }

    // ========== UI 构建 ==========

    private void BuildUI(string title, string message, bool showCancel)
    {
        // --- Canvas ---
        rootCanvasObj = new GameObject("ConfirmDialogCanvas");
        rootCanvasObj.transform.SetParent(transform, false);

        Canvas canvas = rootCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;

        CanvasScaler scaler = rootCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        rootCanvasObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = rootCanvasObj.GetComponent<RectTransform>();

        // --- 半透明遮罩 ---
        GameObject overlay = CreateUI("Overlay", canvasRT);
        Stretch(overlay);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = OverlayColor;
        overlayImg.raycastTarget = true;

        // 点击遮罩 = 取消
        if (showCancel)
        {
            Button overlayBtn = overlay.AddComponent<Button>();
            overlayBtn.onClick.AddListener(OnCancelClicked);
            ColorBlock cb = overlayBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.fadeDuration = 0f;
            overlayBtn.colors = cb;
        }

        // --- 主面板 ---
        panelObj = CreateUI("Panel", canvasRT);
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = PanelBgColor;
        panelBg.raycastTarget = true; // 阻止点击穿透

        // --- 标题 ---
        titleText = CreateLabel(panelRT, "Title", title, 28, TitleColor,
            new Vector2(0.5f, 1f), new Vector2(PanelWidth - 40f, 50f),
            new Vector2(0f, -30f));
        titleText.fontStyle = FontStyles.Bold;

        // --- 内容 ---
        contentText = CreateLabel(panelRT, "Content", message, 22, ContentColor,
            new Vector2(0.5f, 0.5f), new Vector2(PanelWidth - 60f, 130f),
            new Vector2(0f, 15f));
        contentText.alignment = TextAlignmentOptions.Center;
        contentText.enableWordWrapping = true;
        contentText.overflowMode = TextOverflowModes.Ellipsis;

        // --- 底部按钮区 ---
        float buttonY = -PanelHeight / 2f + ButtonHeight / 2f + 30f;

        if (showCancel)
        {
            // 两个按钮: 取消在左, 确认在右
            float spacing = 20f;
            float leftX = -(ButtonWidth / 2f + spacing / 2f);
            float rightX = ButtonWidth / 2f + spacing / 2f;

            cancelButtonObj = CreateButton(panelRT, "BtnCancel", "取消",
                BtnCancelColor, BtnCancelHover, new Vector2(leftX, buttonY));
            cancelButton = cancelButtonObj.GetComponent<Button>();
            cancelButton.onClick.AddListener(OnCancelClicked);

            GameObject confirmObj = CreateButton(panelRT, "BtnConfirm", "确定",
                BtnConfirmColor, BtnConfirmHover, new Vector2(rightX, buttonY));
            confirmButton = confirmObj.GetComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        else
        {
            // 仅确认按钮居中
            GameObject confirmObj = CreateButton(panelRT, "BtnConfirm", "确定",
                BtnConfirmColor, BtnConfirmHover, new Vector2(0f, buttonY));
            confirmButton = confirmObj.GetComponent<Button>();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    // ========== 按钮回调 ==========

    private void OnConfirmClicked()
    {
        Action callback = onConfirmCallback;
        Hide();
        callback?.Invoke();
    }

    private void OnCancelClicked()
    {
        Action callback = onCancelCallback;
        Hide();
        callback?.Invoke();
    }

    // ========== 动画 ==========

    private IEnumerator PlayOpenAnimation()
    {
        if (panelObj == null) yield break;

        CanvasGroup cg = panelObj.AddComponent<CanvasGroup>();
        RectTransform panelRT = panelObj.GetComponent<RectTransform>();

        float elapsed = 0f;
        while (elapsed < AnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / AnimDuration);
            // 缓动: ease-out
            float eased = 1f - (1f - t) * (1f - t);

            float scale = Mathf.Lerp(0.8f, 1.0f, eased);
            panelRT.localScale = new Vector3(scale, scale, 1f);
            cg.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        // 确保最终状态
        panelRT.localScale = Vector3.one;
        cg.alpha = 1f;
    }

    // ========== UI 辅助方法 ==========

    private GameObject CreateUI(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        return go;
    }

    private void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateLabel(RectTransform parent, string name, string text,
        float fontSize, Color color, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject go = CreateUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);

        return tmp;
    }

    private GameObject CreateButton(RectTransform parent, string name, string label,
        Color normalColor, Color hoverColor, Vector2 pos)
    {
        GameObject btnObj = CreateUI(name, parent);
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        btnRT.anchoredPosition = pos;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = Color.white;
        btnBg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = hoverColor;
        Color pressed = normalColor * 0.8f;
        pressed.a = 1f;
        cb.pressedColor = pressed;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.targetGraphic = btnBg;

        // 按钮文字
        TextMeshProUGUI btnText = CreateLabel(btnRT, "Text", label, 22, BtnTextColor,
            new Vector2(0.5f, 0.5f), new Vector2(ButtonWidth, ButtonHeight), Vector2.zero);

        return btnObj;
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;
    }
}
