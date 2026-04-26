using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 游戏内暂停菜单 —— Esc 键唤起/关闭
/// 功能: 存档 / 读档 / 返回标题 / 继续游戏
/// 全屏半透明覆盖, 纯代码 UI, Canvas sortingOrder=250
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    // ========== 单例 ==========
    public static PauseMenuUI Instance { get; private set; }

    // ========== 常量 ==========
    private const int CanvasSortOrder = 250;

    // 颜色
    private static readonly Color OverlayColor = new Color(0.02f, 0.02f, 0.06f, 0.75f);
    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.14f, 0.95f);
    private static readonly Color PanelBorder = new Color(0.25f, 0.35f, 0.55f, 0.40f);
    private static readonly Color BtnNormal = new Color(0.14f, 0.14f, 0.22f, 0.90f);
    private static readonly Color BtnHover = new Color(0.20f, 0.24f, 0.36f, 0.95f);
    private static readonly Color BtnSave = new Color(0.18f, 0.38f, 0.65f, 0.90f);
    private static readonly Color BtnLoad = new Color(0.20f, 0.50f, 0.40f, 0.90f);
    private static readonly Color BtnQuit = new Color(0.55f, 0.18f, 0.18f, 0.90f);
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGray = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color TextSubtle = new Color(0.70f, 0.72f, 0.78f);

    // ========== 运行时状态 ==========
    private Canvas canvas;
    private GameObject rootObj;
    private bool isOpen;

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UIFlowGuard.EnsureEventSystem();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
                Close();
            else
                Open();
        }
    }

    private void OnDestroy()
    {
        if (isOpen || Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        if (rootObj != null)
        {
            Destroy(rootObj);
            rootObj = null;
        }

        if (Instance == this) Instance = null;
    }

    // ========== 公共方法 ==========

    public void Open()
    {
        // 避免在其他高层UI打开时弹出
        if (isOpen) return;

        // 不在对话/事件/考试期间打开暂停菜单
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;
        if (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting) return;
        if (NewsSystem.Instance != null && NewsSystem.Instance.IsShowing) return;
        if (ConfirmDialogUI.Instance != null && ConfirmDialogUI.Instance.IsOpen) return;
        if (PhysicalTestUI.Instance != null && PhysicalTestUI.Instance.IsOpen) return;
        if (AchievementUI.Instance != null && AchievementUI.Instance.isReviewShowing) return;

        isOpen = true;
        Time.timeScale = 0f;
        BuildUI();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        Time.timeScale = 1f;

        if (rootObj != null)
        {
            Destroy(rootObj);
            rootObj = null;
        }
    }

    public bool IsOpen => isOpen;

    // ========== UI 构建 ==========

    private void BuildUI()
    {
        if (rootObj != null) Destroy(rootObj);

        rootObj = new GameObject("PauseMenuCanvas");
        rootObj.transform.SetParent(transform, false);

        // Canvas
        canvas = rootObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;

        CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        rootObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = rootObj.GetComponent<RectTransform>();

        // 半透明背景遮罩
        GameObject overlay = CreateUI("Overlay", canvasRT);
        Stretch(overlay);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = OverlayColor;
        overlayImg.raycastTarget = true;

        // 中央面板
        GameObject panel = CreateUI("Panel", canvasRT);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(420f, 590f); // 增加高度以容纳新按钮
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = PanelColor;

        // 面板外边框效果（用 Outline 组件模拟）
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = PanelBorder;
        panelOutline.effectDistance = new Vector2(2, 2);

        // 标题: "暂停"
        CreateLabel(panelRT, "Title", "暂  停", 38, TextWhite,
            new Vector2(0.5f, 1f), new Vector2(300f, 60f), new Vector2(0f, -30f));

        // 分隔线
        GameObject divider = CreateUI("Divider", panelRT);
        RectTransform divRT = divider.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.5f, 1f);
        divRT.anchorMax = new Vector2(0.5f, 1f);
        divRT.pivot = new Vector2(0.5f, 0.5f);
        divRT.sizeDelta = new Vector2(340f, 2f);
        divRT.anchoredPosition = new Vector2(0f, -90f);
        Image divImg = divider.AddComponent<Image>();
        divImg.color = new Color(0.30f, 0.35f, 0.50f, 0.40f);

        // 当前进度提示
        string progressText = "---";
        if (GameState.Instance != null)
            progressText = GameState.Instance.GetTimeDescription();
        CreateLabel(panelRT, "Progress", progressText, 18, TextSubtle,
            new Vector2(0.5f, 1f), new Vector2(340f, 30f), new Vector2(0f, -108f));

        // 按钮组
        float btnWidth = 300f;
        float btnHeight = 56f;
        float startY = -155f;
        float spacing = 14f;

        // 继续游戏
        CreateMenuButton(panelRT, "继续游戏", BtnNormal, BtnHover,
            new Vector2(btnWidth, btnHeight), new Vector2(0f, startY), Close);

        // 设置
        CreateMenuButton(panelRT, "设  置", BtnNormal, BtnHover,
            new Vector2(btnWidth, btnHeight), new Vector2(0f, startY - (btnHeight + spacing)),
            () => {
                Close();
                if (SettingsManager.Instance != null)
                {
                    SettingsUIBuilder.ShowSettings(false);
                }
            });

        // 存档
        CreateMenuButton(panelRT, "存  档", BtnSave, BtnHover,
            new Vector2(btnWidth, btnHeight), new Vector2(0f, startY - 2 * (btnHeight + spacing)),
            () => { Close(); SaveLoadUI.Show(true); });

        // 读档
        CreateMenuButton(panelRT, "读  档", BtnLoad, BtnHover,
            new Vector2(btnWidth, btnHeight), new Vector2(0f, startY - 3 * (btnHeight + spacing)),
            () => { Close(); SaveLoadUI.Show(false); });

        // 返回标题
        CreateMenuButton(panelRT, "返回标题", BtnQuit, BtnHover,
            new Vector2(btnWidth, btnHeight), new Vector2(0f, startY - 4 * (btnHeight + spacing)),
            OnReturnToTitle);

        // 底部提示
        CreateLabel(panelRT, "Hint", "按 Esc 继续游戏", 16, TextGray,
            new Vector2(0.5f, 0f), new Vector2(300f, 30f), new Vector2(0f, 20f));
    }

    // ========== 交互逻辑 ==========

    private void OnReturnToTitle()
    {
        // 先自动存档
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();
            Debug.Log("[PauseMenu] 返回标题前自动存档");
        }

        Close();
        SceneLoader.LoadScene("TitleScreen");
    }

    // ========== UI 工具方法 ==========

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

    private Button CreateMenuButton(RectTransform parent, string label, Color normalColor,
        Color hoverColor, Vector2 size, Vector2 pos, Action onClick)
    {
        GameObject btnObj = CreateUI("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(
            hoverColor.r / Mathf.Max(normalColor.r, 0.01f),
            hoverColor.g / Mathf.Max(normalColor.g, 0.01f),
            hoverColor.b / Mathf.Max(normalColor.b, 0.01f),
            1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());

        // 按钮文字
        GameObject textObj = CreateUI("Label", rt);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = TextWhite;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);

        return btn;
    }

    private void ApplyFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;
    }
}
