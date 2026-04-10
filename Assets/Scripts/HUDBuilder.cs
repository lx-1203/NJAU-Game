using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// HUD 构建器 —— 用纯代码构建游戏主界面的所有 UI 元素
/// 布局参考：
/// ┌──────────────────────────────────────────────────┐
/// │  [顶栏] 时间 · 金钱 · 行动点                       │
/// ├─────────────┬────────────────────────────────────┤
/// │  角色立绘    │      中央区域（地图占位）             │
/// │  + 属性条    │                                    │
/// │ （左侧栏）   │                                    │
/// ├─────────────┴────────────────────────────────────┤
/// │  [底栏] 行动按钮：自习 ｜ 社交 ｜ 出校门 ｜ 睡觉    │
/// └──────────────────────────────────────────────────┘
/// </summary>
public class HUDBuilder : MonoBehaviour
{
    // ========== 构建产物引用（供 HUDManager 使用） ==========

    [HideInInspector] public Canvas hudCanvas;

    // 顶栏
    [HideInInspector] public TextMeshProUGUI timeText;
    [HideInInspector] public TextMeshProUGUI moneyText;
    [HideInInspector] public TextMeshProUGUI actionPointsText;

    // 左侧栏
    [HideInInspector] public Image portraitPlaceholder;
    [HideInInspector] public List<AttributeBar> attributeBars = new List<AttributeBar>();

    // 中央区域
    [HideInInspector] public GameObject centerPanel;

    // 底栏按钮
    [HideInInspector] public Button btnStudy;
    [HideInInspector] public Button btnSocial;
    [HideInInspector] public Button btnGoOut;
    [HideInInspector] public Button btnSleep;

    // 动画器
    [HideInInspector] public UIAnimator hudAnimator;

    // ========== 布局常量 ==========
    private const float TopBarHeight = 60f;
    private const float BottomBarHeight = 70f;
    private const float LeftPanelWidth = 260f;
    private const float Padding = 10f;

    // ========== 颜色方案 ==========
    private static readonly Color PanelBgColor       = new Color(0.08f, 0.08f, 0.12f, 0.90f);
    private static readonly Color TopBarColor        = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color BottomBarColor     = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color LeftPanelColor     = new Color(0.10f, 0.10f, 0.15f, 0.92f);
    private static readonly Color CenterPanelColor   = new Color(0.05f, 0.05f, 0.08f, 0.50f);
    private static readonly Color ButtonNormalColor   = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ButtonHoverColor    = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color ButtonPressedColor  = new Color(0.15f, 0.25f, 0.50f, 1.0f);
    private static readonly Color TextWhite           = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold            = new Color(1.0f, 0.85f, 0.30f);

    // ========== 对外接口 ==========

    /// <summary>
    /// 构建整个 HUD，返回 Canvas 根对象
    /// </summary>
    public void BuildHUD()
    {
        CreateCanvas();
        CreateTopBar();
        CreateLeftPanel();
        CreateCenterPanel();
        CreateBottomBar();
        CreateAnimator();
    }

    // ====================================================================
    //  Canvas
    // ====================================================================

    private void CreateCanvas()
    {
        // 检查是否已有 EventSystem
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
        hudCanvas.sortingOrder = 100; // HUD 在最上层

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // ====================================================================
    //  顶栏
    // ====================================================================

    private void CreateTopBar()
    {
        // 顶栏容器 —— 锚定在屏幕顶部、横向拉满
        GameObject topBar = CreatePanel("TopBar", hudCanvas.transform, TopBarColor);
        RectTransform topRT = topBar.GetComponent<RectTransform>();
        SetAnchorsStretchTop(topRT, TopBarHeight);

        // 添加水平布局
        HorizontalLayoutGroup hlg = topBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30f;
        hlg.padding = new RectOffset(20, 20, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 时间文本
        timeText = CreateTMPText("TimeText", topBar.transform, "大一上 · 回合1 · 9月",
            20f, TextWhite, TextAlignmentOptions.Left, new Vector2(350, TopBarHeight));

        // 金钱文本
        moneyText = CreateTMPText("MoneyText", topBar.transform, "金钱：￥0",
            20f, TextGold, TextAlignmentOptions.Center, new Vector2(200, TopBarHeight));

        // 行动点文本
        actionPointsText = CreateTMPText("ActionPointsText", topBar.transform, "行动点：●●●●●",
            20f, TextWhite, TextAlignmentOptions.Right, new Vector2(280, TopBarHeight));
    }

    // ====================================================================
    //  左侧栏
    // ====================================================================

    private void CreateLeftPanel()
    {
        // 左侧栏容器 —— 锚定在左侧，顶栏下方到底栏上方
        GameObject leftPanel = CreatePanel("LeftPanel", hudCanvas.transform, LeftPanelColor);
        RectTransform leftRT = leftPanel.GetComponent<RectTransform>();
        // 手动设置锚点：左侧，上下留出顶栏和底栏的空间
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0, 1);
        leftRT.pivot = new Vector2(0, 0.5f);
        leftRT.anchoredPosition = new Vector2(0, (BottomBarHeight - TopBarHeight) / 2f);
        leftRT.sizeDelta = new Vector2(LeftPanelWidth, -(TopBarHeight + BottomBarHeight));

        // 垂直布局
        VerticalLayoutGroup vlg = leftPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // --- 角色立绘占位 ---
        GameObject portraitObj = new GameObject("PortraitPlaceholder");
        portraitObj.transform.SetParent(leftPanel.transform, false);
        RectTransform portraitRT = portraitObj.AddComponent<RectTransform>();
        portraitRT.sizeDelta = new Vector2(220, 260);

        portraitPlaceholder = portraitObj.AddComponent<Image>();
        portraitPlaceholder.color = new Color(0.18f, 0.18f, 0.25f, 0.9f);

        // 占位文字
        TextMeshProUGUI placeholderText = CreateTMPText("PlaceholderLabel", portraitObj.transform,
            "角色立绘", 18f, new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.Center,
            new Vector2(220, 260));
        RectTransform phRT = placeholderText.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;

        // --- "属性" 标题 ---
        CreateTMPText("AttributeTitle", leftPanel.transform, "— 属性 —",
            15f, new Color(0.65f, 0.65f, 0.70f), TextAlignmentOptions.Center,
            new Vector2(LeftPanelWidth - 20, 22f));

        // --- 属性条 ---
        // 属性条在 HUDManager 初始化数据后由 HUDManager 调用创建
    }

    /// <summary>
    /// 在左侧栏追加一个属性条（由 HUDManager 调用）
    /// </summary>
    public AttributeBar AddAttributeBar()
    {
        Transform leftPanel = hudCanvas.transform.Find("LeftPanel");
        if (leftPanel == null)
        {
            Debug.LogError("[HUDBuilder] 找不到 LeftPanel！");
            return null;
        }
        AttributeBar bar = AttributeBar.Create(leftPanel);
        attributeBars.Add(bar);
        return bar;
    }

    // ====================================================================
    //  中央区域
    // ====================================================================

    private void CreateCenterPanel()
    {
        centerPanel = CreatePanel("CenterPanel", hudCanvas.transform, CenterPanelColor);
        RectTransform centerRT = centerPanel.GetComponent<RectTransform>();
        // 锚定：左侧偏移 LeftPanelWidth，上方偏移 TopBarHeight，下方偏移 BottomBarHeight
        centerRT.anchorMin = new Vector2(0, 0);
        centerRT.anchorMax = new Vector2(1, 1);
        centerRT.offsetMin = new Vector2(LeftPanelWidth, BottomBarHeight);
        centerRT.offsetMax = new Vector2(0, -TopBarHeight);

        // 占位文字
        TextMeshProUGUI mapPlaceholder = CreateTMPText("MapPlaceholder", centerPanel.transform,
            "地图 / 当前场景\n（点击地点触发对应事件）",
            24f, new Color(0.4f, 0.4f, 0.45f), TextAlignmentOptions.Center,
            new Vector2(400, 100));
        RectTransform mpRT = mapPlaceholder.GetComponent<RectTransform>();
        mpRT.anchorMin = new Vector2(0.5f, 0.5f);
        mpRT.anchorMax = new Vector2(0.5f, 0.5f);
        mpRT.anchoredPosition = Vector2.zero;
    }

    // ====================================================================
    //  底栏
    // ====================================================================

    private void CreateBottomBar()
    {
        // 底栏容器 —— 锚定在屏幕底部、横向拉满
        GameObject bottomBar = CreatePanel("BottomBar", hudCanvas.transform, BottomBarColor);
        RectTransform botRT = bottomBar.GetComponent<RectTransform>();
        SetAnchorsStretchBottom(botRT, BottomBarHeight);

        // 水平居中布局
        HorizontalLayoutGroup hlg = bottomBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 40f;
        hlg.padding = new RectOffset(40, 40, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 创建 4 个行动按钮
        btnStudy  = CreateActionButton("BtnStudy",  bottomBar.transform, "自习");
        btnSocial = CreateActionButton("BtnSocial", bottomBar.transform, "社交");
        btnGoOut  = CreateActionButton("BtnGoOut",  bottomBar.transform, "出校门");
        btnSleep  = CreateActionButton("BtnSleep",  bottomBar.transform, "睡觉");
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
    //  工具方法
    // ====================================================================

    /// <summary>创建带背景色的面板</summary>
    private GameObject CreatePanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();

        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;

        return panel;
    }

    /// <summary>创建 TextMeshPro 文本</summary>
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

    /// <summary>创建行动按钮</summary>
    private Button CreateActionButton(string name, Transform parent, string label)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 50);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = ButtonNormalColor;

        Button btn = btnObj.AddComponent<Button>();

        // 设置按钮颜色过渡
        ColorBlock cb = btn.colors;
        cb.normalColor = ButtonNormalColor;
        cb.highlightedColor = ButtonHoverColor;
        cb.pressedColor = ButtonPressedColor;
        cb.selectedColor = ButtonNormalColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // 按钮文字
        TextMeshProUGUI btnText = CreateTMPText(name + "Label", btnObj.transform, label,
            20f, TextWhite, TextAlignmentOptions.Center, new Vector2(160, 50));
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // 添加 CanvasGroup 用于动画
        btnObj.AddComponent<CanvasGroup>();

        return btn;
    }

    // ========== 锚点辅助 ==========

    /// <summary>横向拉满并固定在顶部</summary>
    private void SetAnchorsStretchTop(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, height);
    }

    /// <summary>横向拉满并固定在底部</summary>
    private void SetAnchorsStretchBottom(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, height);
    }
}
