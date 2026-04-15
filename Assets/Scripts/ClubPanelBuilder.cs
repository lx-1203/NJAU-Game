using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 社团面板构建器 —— 用纯代码构建社团系统的所有 UI 元素
/// 布局参考：
/// ┌───────────────────────────────────────────────────────┐
/// │  ✕                     社团系统                        │  ← 标题栏 (50px)
/// ├──────────────┬────────────────────────────────────────┤
/// │  【已加入】    │                                        │
/// │  ▶ 跑协       │  ┌──────────────────────────────────┐  │
/// │    篮球社     │  │  跑协                             │  │  ← 社团详情
/// │              │  │  分类：体育  主要属性：体魄          │  │
/// │  【可加入】    │  │  当前职务：干事                    │  │
/// │    吉他社     │  │  下次晋升：部长                    │  │
/// │    辩论社     │  │    条件：10回合 + 领导力≥30        │  │
/// │    青协       │  │                                    │  │
/// │    学生会     │  │  [参加活动]  [退出社团]             │  │
/// │    动漫社     │  └──────────────────────────────────┘  │
/// │              │                                        │
/// │  【特殊组织】  │  ┌──────────────────────────────────┐  │
/// │    校团委     │  │  入党进度                          │  │
/// │    党建班     │  │  ████████░░░░░ 发展对象 (3/5)      │  │
/// │              │  │  [申请入党]                         │  │
/// │              │  └──────────────────────────────────┘  │
/// └──────────────┴────────────────────────────────────────┘
/// </summary>
public class ClubPanelBuilder : MonoBehaviour
{
    // ========== 构建产物引用（供 ClubPanelManager 使用） ==========

    [HideInInspector] public Canvas clubCanvas;
    [HideInInspector] public GameObject panelRoot;       // 整个面板根
    [HideInInspector] public Button btnClose;             // 关闭按钮
    [HideInInspector] public Transform listContent;       // 左侧列表的 Content 容器
    [HideInInspector] public Transform detailContainer;   // 右侧详情区容器

    // 详情区子元素
    [HideInInspector] public TextMeshProUGUI detailName;
    [HideInInspector] public TextMeshProUGUI detailInfo;       // 分类+主属性
    [HideInInspector] public TextMeshProUGUI detailPosition;   // 当前职务
    [HideInInspector] public TextMeshProUGUI detailNextRank;   // 晋升条件
    [HideInInspector] public Button btnActivity;               // 参加活动按钮
    [HideInInspector] public Button btnJoinLeave;              // 加入/退出按钮
    [HideInInspector] public TextMeshProUGUI btnJoinLeaveText; // 按钮文字

    // 入党区
    [HideInInspector] public GameObject partySection;          // 入党区域根
    [HideInInspector] public Image partyProgressFill;          // 进度条填充
    [HideInInspector] public TextMeshProUGUI partyStageText;   // 阶段文字
    [HideInInspector] public Button btnPartyApply;             // 申请入党按钮
    [HideInInspector] public TextMeshProUGUI btnPartyApplyText;

    // ========== 布局常量 ==========

    private const float HeaderHeight = 50f;
    private const float LeftListWidth = 280f;
    private const float ListItemHeight = 40f;
    private const float SectionHeaderHeight = 30f;
    private const float Padding = 10f;

    // ========== 颜色方案（深蓝色系，沿用项目色板） ==========

    private static readonly Color PanelBgColor       = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color HeaderColor        = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color ListBgColor        = new Color(0.06f, 0.06f, 0.10f, 0.90f);
    private static readonly Color DetailBgColor      = new Color(0.10f, 0.10f, 0.15f, 0.92f);
    private static readonly Color ItemNormalColor    = new Color(0.12f, 0.12f, 0.18f, 0.85f);
    private static readonly Color ItemSelectedColor  = new Color(0.20f, 0.25f, 0.40f, 0.95f);
    private static readonly Color BtnJoinColor       = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnLeaveColor      = new Color(0.55f, 0.20f, 0.20f, 1.0f);
    private static readonly Color BtnActivityColor   = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color BtnCloseColor      = new Color(0.50f, 0.15f, 0.15f, 1.0f);
    private static readonly Color PartyProgressColor = new Color(0.85f, 0.20f, 0.20f, 1.0f);
    private static readonly Color TextWhite          = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold           = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray           = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color SectionHeaderColor = new Color(0.70f, 0.70f, 0.75f);
    private static readonly Color OverlayColor       = new Color(0f, 0f, 0f, 0.50f);
    private static readonly Color ProgressBgColor    = new Color(0.15f, 0.15f, 0.20f, 1.0f);

    // ========== 对外接口 ==========

    /// <summary>
    /// 构建整个社团面板
    /// </summary>
    public void BuildPanel()
    {
        CreateCanvas();
        CreateOverlay();
        CreatePanelRoot();
        CreateHeader();
        CreateLeftList();
        CreateDetailPanel();
        CreatePartySection();

        // 面板初始隐藏
        panelRoot.SetActive(false);
    }

    /// <summary>创建一个社团列表项（由 Manager 调用）</summary>
    public GameObject CreateClubListItem(string clubName, bool isJoined, Transform parent)
    {
        string prefix = isJoined ? "● " : "○ ";
        string displayName = prefix + clubName;

        GameObject itemObj = new GameObject("ClubItem_" + clubName);
        itemObj.transform.SetParent(parent, false);

        RectTransform rt = itemObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, ListItemHeight);

        // 使用 LayoutElement 让 VerticalLayoutGroup 控制宽度
        LayoutElement le = itemObj.AddComponent<LayoutElement>();
        le.minHeight = ListItemHeight;
        le.preferredHeight = ListItemHeight;

        Image bg = itemObj.AddComponent<Image>();
        bg.color = ItemNormalColor;

        Button btn = itemObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = ItemNormalColor;
        cb.highlightedColor = new Color(
            ItemNormalColor.r + 0.05f,
            ItemNormalColor.g + 0.05f,
            ItemNormalColor.b + 0.08f,
            ItemNormalColor.a
        );
        cb.pressedColor = new Color(
            ItemNormalColor.r - 0.03f,
            ItemNormalColor.g - 0.03f,
            ItemNormalColor.b - 0.03f,
            ItemNormalColor.a
        );
        cb.selectedColor = ItemSelectedColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // 列表项文字
        TextMeshProUGUI label = CreateTMPText("Label", itemObj.transform, displayName,
            16f, TextWhite, TextAlignmentOptions.Left, new Vector2(0, ListItemHeight));
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(12f, 0);
        labelRT.offsetMax = new Vector2(-4f, 0);

        return itemObj;
    }

    /// <summary>创建分组标题文本（由 Manager 调用）</summary>
    public TextMeshProUGUI CreateSectionHeader(string text, Transform parent)
    {
        TextMeshProUGUI header = CreateTMPText("SectionHeader_" + text, parent, text,
            14f, SectionHeaderColor, TextAlignmentOptions.Left, new Vector2(0, SectionHeaderHeight));

        RectTransform rt = header.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, SectionHeaderHeight);

        LayoutElement le = header.gameObject.AddComponent<LayoutElement>();
        le.minHeight = SectionHeaderHeight;
        le.preferredHeight = SectionHeaderHeight;

        // 左侧留出一些缩进
        header.margin = new Vector4(10f, 0, 0, 0);

        return header;
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

        GameObject canvasObj = new GameObject("ClubPanelCanvas");
        canvasObj.transform.SetParent(transform, false);

        clubCanvas = canvasObj.AddComponent<Canvas>();
        clubCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        clubCanvas.sortingOrder = 200; // 在 HUD(100) 上面

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // ====================================================================
    //  半透明背景遮罩
    // ====================================================================

    private GameObject overlayObj;

    private void CreateOverlay()
    {
        overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(clubCanvas.transform, false);

        RectTransform rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image overlayBg = overlayObj.AddComponent<Image>();
        overlayBg.color = OverlayColor;

        // 点击遮罩关闭面板（通过 Button 组件实现）
        Button overlayBtn = overlayObj.AddComponent<Button>();
        ColorBlock cb = overlayBtn.colors;
        cb.normalColor = OverlayColor;
        cb.highlightedColor = OverlayColor;
        cb.pressedColor = OverlayColor;
        cb.selectedColor = OverlayColor;
        overlayBtn.colors = cb;
        // 点击事件由 Manager 绑定 btnClose
    }

    // ====================================================================
    //  面板主体
    // ====================================================================

    private void CreatePanelRoot()
    {
        panelRoot = CreatePanel("ClubPanelRoot", clubCanvas.transform, PanelBgColor);
        RectTransform rt = panelRoot.GetComponent<RectTransform>();

        // 居中，85% x 80% 屏幕
        rt.anchorMin = new Vector2(0.075f, 0.10f);
        rt.anchorMax = new Vector2(0.925f, 0.90f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ====================================================================
    //  标题栏
    // ====================================================================

    private void CreateHeader()
    {
        GameObject header = CreatePanel("Header", panelRoot.transform, HeaderColor);
        RectTransform headerRT = header.GetComponent<RectTransform>();

        // 锚定在面板顶部，横向拉满
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, HeaderHeight);

        // 标题文字 —— 居中
        CreateTMPText("TitleText", header.transform, "社团系统",
            24f, TextWhite, TextAlignmentOptions.Center, new Vector2(200, HeaderHeight));
        RectTransform titleRT = header.transform.Find("TitleText").GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // 关闭按钮 ✕ —— 右上角
        btnClose = CreateButton("BtnClose", header.transform, "✕", BtnCloseColor, new Vector2(40, 36));
        RectTransform closeRT = btnClose.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f);
        closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-8f, 0);
    }

    // ====================================================================
    //  左侧列表区
    // ====================================================================

    private void CreateLeftList()
    {
        // 左侧列表背景容器
        GameObject listPanel = CreatePanel("ListPanel", panelRoot.transform, ListBgColor);
        RectTransform listPanelRT = listPanel.GetComponent<RectTransform>();

        // 锚定在左侧，标题栏下方到底部
        listPanelRT.anchorMin = new Vector2(0, 0);
        listPanelRT.anchorMax = new Vector2(0, 1);
        listPanelRT.pivot = new Vector2(0, 0.5f);
        listPanelRT.anchoredPosition = Vector2.zero;
        listPanelRT.sizeDelta = new Vector2(LeftListWidth, -HeaderHeight);
        listPanelRT.offsetMax = new Vector2(LeftListWidth, -HeaderHeight);
        listPanelRT.offsetMin = new Vector2(0, 0);

        // ScrollRect
        ScrollRect scrollRect = listPanel.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(listPanel.transform, false);
        RectTransform vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        Image vpMask = viewport.AddComponent<Image>();
        vpMask.color = Color.white;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect.viewport = vpRT;

        // Content 容器
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();

        // 从顶部向下排列
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        // VerticalLayoutGroup
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ContentSizeFitter 使内容自适应高度
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        listContent = content.transform;
    }

    // ====================================================================
    //  右侧详情区
    // ====================================================================

    private void CreateDetailPanel()
    {
        // 详情区背景
        GameObject detailPanel = CreatePanel("DetailPanel", panelRoot.transform, DetailBgColor);
        RectTransform detailRT = detailPanel.GetComponent<RectTransform>();

        // 锚定：左侧偏移 LeftListWidth，上方偏移 HeaderHeight
        detailRT.anchorMin = new Vector2(0, 0);
        detailRT.anchorMax = new Vector2(1, 1);
        detailRT.offsetMin = new Vector2(LeftListWidth, 0);
        detailRT.offsetMax = new Vector2(0, -HeaderHeight);

        detailContainer = detailPanel.transform;

        // 使用 VerticalLayoutGroup 排列详情内容
        VerticalLayoutGroup vlg = detailPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // --- 社团名称 ---
        detailName = CreateTMPText("DetailName", detailPanel.transform, "",
            28f, TextGold, TextAlignmentOptions.Left, new Vector2(0, 40));
        detailName.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        // --- 分类 + 主属性 ---
        detailInfo = CreateTMPText("DetailInfo", detailPanel.transform, "",
            18f, TextWhite, TextAlignmentOptions.Left, new Vector2(0, 28));
        detailInfo.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        // --- 当前职务 ---
        detailPosition = CreateTMPText("DetailPosition", detailPanel.transform, "",
            18f, TextWhite, TextAlignmentOptions.Left, new Vector2(0, 28));
        detailPosition.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        // --- 晋升条件 ---
        detailNextRank = CreateTMPText("DetailNextRank", detailPanel.transform, "",
            16f, TextGray, TextAlignmentOptions.Left, new Vector2(0, 50));
        LayoutElement nrLE = detailNextRank.gameObject.AddComponent<LayoutElement>();
        nrLE.preferredHeight = 50f;
        nrLE.flexibleHeight = 0;

        // --- 按钮区域 ---
        GameObject btnRow = new GameObject("ButtonRow");
        btnRow.transform.SetParent(detailPanel.transform, false);
        RectTransform btnRowRT = btnRow.AddComponent<RectTransform>();
        btnRowRT.sizeDelta = new Vector2(0, 45);
        LayoutElement btnRowLE = btnRow.AddComponent<LayoutElement>();
        btnRowLE.preferredHeight = 45f;

        HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 参加活动按钮
        btnActivity = CreateButton("BtnActivity", btnRow.transform, "参加活动",
            BtnActivityColor, new Vector2(140, 40));

        // 加入/退出按钮
        btnJoinLeave = CreateButton("BtnJoinLeave", btnRow.transform, "加入社团",
            BtnJoinColor, new Vector2(140, 40));

        // 缓存加入/退出按钮的文字引用
        btnJoinLeaveText = btnJoinLeave.GetComponentInChildren<TextMeshProUGUI>();
    }

    // ====================================================================
    //  入党进度区
    // ====================================================================

    private void CreatePartySection()
    {
        // 入党区域根 —— 放在详情区底部
        partySection = CreatePanel("PartySection", detailContainer, new Color(0.08f, 0.10f, 0.14f, 0.90f));
        LayoutElement psLE = partySection.AddComponent<LayoutElement>();
        psLE.preferredHeight = 120f;
        psLE.flexibleHeight = 0;

        VerticalLayoutGroup vlg = partySection.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // --- 标题 ---
        TextMeshProUGUI partyTitle = CreateTMPText("PartyTitle", partySection.transform,
            "入党进度", 18f, TextWhite, TextAlignmentOptions.Left, new Vector2(0, 26));
        partyTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        // --- 进度条容器 ---
        GameObject progressBar = new GameObject("ProgressBar");
        progressBar.transform.SetParent(partySection.transform, false);
        RectTransform pbRT = progressBar.AddComponent<RectTransform>();
        pbRT.sizeDelta = new Vector2(0, 20);
        LayoutElement pbLE = progressBar.AddComponent<LayoutElement>();
        pbLE.preferredHeight = 20f;

        // 进度条背景
        Image pbBg = progressBar.AddComponent<Image>();
        pbBg.color = ProgressBgColor;

        // 进度条填充
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(progressBar.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1); // 初始宽度为 0
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        partyProgressFill = fillObj.AddComponent<Image>();
        partyProgressFill.color = PartyProgressColor;
        partyProgressFill.type = Image.Type.Filled;
        partyProgressFill.fillMethod = Image.FillMethod.Horizontal;
        partyProgressFill.fillAmount = 0f;

        // 让填充覆盖整个进度条区域（用锚点 0-1 + fillAmount 控制）
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // --- 阶段文字 ---
        partyStageText = CreateTMPText("PartyStageText", partySection.transform,
            "未申请", 15f, TextGray, TextAlignmentOptions.Left, new Vector2(0, 22));
        partyStageText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        // --- 申请入党按钮 ---
        btnPartyApply = CreateButton("BtnPartyApply", partySection.transform, "申请入党",
            BtnJoinColor, new Vector2(140, 36));
        btnPartyApply.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        btnPartyApplyText = btnPartyApply.GetComponentInChildren<TextMeshProUGUI>();
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

    /// <summary>创建按钮</summary>
    private Button CreateButton(string name, Transform parent, string label, Color normalColor, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = normalColor;

        Button btn = btnObj.AddComponent<Button>();

        // 设置按钮颜色过渡
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = new Color(
            Mathf.Min(normalColor.r + 0.10f, 1f),
            Mathf.Min(normalColor.g + 0.10f, 1f),
            Mathf.Min(normalColor.b + 0.10f, 1f),
            normalColor.a
        );
        cb.pressedColor = new Color(
            Mathf.Max(normalColor.r - 0.05f, 0f),
            Mathf.Max(normalColor.g - 0.05f, 0f),
            Mathf.Max(normalColor.b - 0.05f, 0f),
            normalColor.a
        );
        cb.selectedColor = normalColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // 按钮文字
        TextMeshProUGUI btnText = CreateTMPText(name + "Label", btnObj.transform, label,
            18f, TextWhite, TextAlignmentOptions.Center, size);
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btn;
    }
}
