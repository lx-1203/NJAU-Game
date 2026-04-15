using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 学期总结 UI 面板 —— 全屏覆盖式结算界面
/// 显示成绩单、属性变化、NPC好感、成就、评分明细和总评等级
/// 纯代码动态创建，独立 Canvas (sortingOrder=100)
/// </summary>
public class SemesterSummaryUI : MonoBehaviour
{
    // ========== 单例 ==========

    public static SemesterSummaryUI Instance { get; private set; }

    // ========== 公开属性 ==========

    /// <summary>面板是否正在显示（供外部查询模态状态）</summary>
    public bool isShowing { get; private set; }

    // ========== UI 引用 ==========

    private Canvas summaryCanvas;
    private GameObject rootObj;
    private CanvasGroup overlayCanvasGroup;
    private RectTransform contentPanelRT;
    private CanvasGroup contentCanvasGroup;
    private TextMeshProUGUI gradeText;
    private RectTransform gradeTextRT;
    private CanvasGroup continueButtonCG;

    // ========== 动画参数 ==========

    private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ========== 布局常量 ==========

    private const float CanvasSortingOrder = 100;
    private const float PanelWidthRatio = 0.80f;
    private const float PanelHeightRatio = 0.90f;
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    // ========== 颜色方案 ==========

    private static readonly Color OverlayColor       = new Color(0.00f, 0.00f, 0.00f, 0.70f);
    private static readonly Color PanelBgColor       = new Color(0.10f, 0.12f, 0.18f, 0.96f);
    private static readonly Color SectionBgColor     = new Color(0.08f, 0.10f, 0.15f, 0.80f);
    private static readonly Color HeaderColor        = new Color(0.85f, 0.90f, 1.00f);
    private static readonly Color SubHeaderColor     = new Color(0.70f, 0.78f, 0.90f);
    private static readonly Color TextWhite          = new Color(0.90f, 0.90f, 0.90f);
    private static readonly Color TextDim            = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color PositiveColor      = new Color(0.30f, 0.85f, 0.35f);
    private static readonly Color NegativeColor      = new Color(0.90f, 0.30f, 0.30f);
    private static readonly Color NeutralColor       = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color ButtonNormalColor  = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ButtonHoverColor   = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color ButtonPressedColor = new Color(0.15f, 0.25f, 0.50f, 1.0f);
    private static readonly Color DividerColor       = new Color(0.30f, 0.35f, 0.45f, 0.50f);

    // ========== 缓存数据 ==========

    private SemesterSummaryData currentData;

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ====================================================================
    //  对外接口
    // ====================================================================

    /// <summary>
    /// 显示学期总结面板
    /// </summary>
    /// <param name="data">学期总结数据</param>
    public void Show(SemesterSummaryData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SemesterSummaryUI] 数据为空，无法显示");
            return;
        }

        if (isShowing)
        {
            Debug.LogWarning("[SemesterSummaryUI] 面板已在显示中");
            return;
        }

        currentData = data;
        isShowing = true;

        // 创建完整 UI
        CreateCanvas();
        CreateOverlay();
        CreatePanel(data);

        // 播放入场动画
        StartCoroutine(PlayShowAnimation());

        Debug.Log($"[SemesterSummaryUI] 显示学期总结: {data.yearName}{data.semesterName}");
    }

    /// <summary>
    /// 关闭学期总结面板
    /// </summary>
    public void Hide()
    {
        if (!isShowing) return;

        StartCoroutine(PlayHideAnimation());
    }

    // ====================================================================
    //  Canvas 创建
    // ====================================================================

    /// <summary>创建独立的覆盖画布</summary>
    private void CreateCanvas()
    {
        // 确保 EventSystem 存在
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        rootObj = new GameObject("SemesterSummaryCanvas");
        rootObj.transform.SetParent(transform, false);

        summaryCanvas = rootObj.AddComponent<Canvas>();
        summaryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        summaryCanvas.sortingOrder = (int)CanvasSortingOrder;

        CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        rootObj.AddComponent<GraphicRaycaster>();
    }

    // ====================================================================
    //  半透明遮罩
    // ====================================================================

    /// <summary>创建半透明黑色背景遮罩</summary>
    private void CreateOverlay()
    {
        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(rootObj.transform, false);

        RectTransform rt = overlayObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image overlayImage = overlayObj.AddComponent<Image>();
        overlayImage.color = OverlayColor;

        overlayCanvasGroup = overlayObj.AddComponent<CanvasGroup>();
        overlayCanvasGroup.alpha = 0f;
    }

    // ====================================================================
    //  内容面板
    // ====================================================================

    /// <summary>创建内容面板及所有子元素</summary>
    private void CreatePanel(SemesterSummaryData data)
    {
        // --- 内容面板容器 ---
        GameObject panelObj = new GameObject("ContentPanel");
        panelObj.transform.SetParent(rootObj.transform, false);

        contentPanelRT = panelObj.AddComponent<RectTransform>();
        contentPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
        contentPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        contentPanelRT.pivot = new Vector2(0.5f, 0.5f);
        contentPanelRT.sizeDelta = new Vector2(
            ReferenceWidth * PanelWidthRatio,
            ReferenceHeight * PanelHeightRatio
        );

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = PanelBgColor;

        contentCanvasGroup = panelObj.AddComponent<CanvasGroup>();
        contentCanvasGroup.alpha = 0f;

        // --- 整体垂直布局（使用 ScrollView 承载以防内容溢出） ---
        GameObject scrollViewObj = CreateScrollView(panelObj.transform);
        Transform contentContainer = scrollViewObj.transform.Find("Viewport/Content");

        // --- 构建各区域 ---
        CreateTitle(contentContainer, data);
        CreateDivider(contentContainer);
        CreateMiddleSection(contentContainer, data);
        CreateDivider(contentContainer);
        CreateScoreBreakdown(contentContainer, data);
        CreateDivider(contentContainer);
        CreateGradeDisplay(contentContainer, data);
        CreateContinueButton(contentContainer);
    }

    /// <summary>创建可滚动视图容器</summary>
    private GameObject CreateScrollView(Transform parent)
    {
        // ScrollView 根
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(parent, false);

        RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(20, 20);
        scrollRT.offsetMax = new Vector2(-20, -20);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);

        RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        Image viewportMask = viewportObj.AddComponent<Image>();
        viewportMask.color = Color.white;

        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content 容器
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRT = contentObj.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(20, 20, 15, 25);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 关联 ScrollRect
        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;

        return scrollObj;
    }

    // ====================================================================
    //  标题
    // ====================================================================

    /// <summary>创建标题行 "大X - X学期 总结"</summary>
    private void CreateTitle(Transform parent, SemesterSummaryData data)
    {
        string titleStr = $"{data.yearName} - {data.semesterName} 总结";

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);

        RectTransform rt = titleObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = titleStr;
        titleText.fontSize = 38;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = HeaderColor;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.enableWordWrapping = false;
    }

    // ====================================================================
    //  中部区域（左右两列）
    // ====================================================================

    /// <summary>创建中部左右两列区域</summary>
    private void CreateMiddleSection(Transform parent, SemesterSummaryData data)
    {
        GameObject middleObj = new GameObject("MiddleSection");
        middleObj.transform.SetParent(parent, false);

        RectTransform middleRT = middleObj.AddComponent<RectTransform>();
        middleRT.sizeDelta = new Vector2(0, 420);

        HorizontalLayoutGroup hlg = middleObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.UpperCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // --- 左列 (50%): 成绩单 + NPC好感 ---
        GameObject leftColumn = CreateColumnContainer("LeftColumn", middleObj.transform);
        CreateCourseList(leftColumn.transform, data);
        CreateNPCList(leftColumn.transform, data);

        // --- 右列 (50%): 属性变化 + 成就 ---
        GameObject rightColumn = CreateColumnContainer("RightColumn", middleObj.transform);
        CreateAttributeChanges(rightColumn.transform, data);
        CreateAchievementList(rightColumn.transform, data);
    }

    /// <summary>创建纵向列容器</summary>
    private GameObject CreateColumnContainer(string name, Transform parent)
    {
        GameObject colObj = new GameObject(name);
        colObj.transform.SetParent(parent, false);

        colObj.AddComponent<RectTransform>();

        Image bg = colObj.AddComponent<Image>();
        bg.color = SectionBgColor;

        // 内部使用 ScrollView 以防内容溢出
        GameObject scrollViewObj = new GameObject("ColumnScroll");
        scrollViewObj.transform.SetParent(colObj.transform, false);

        RectTransform scrollRT = scrollViewObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);

        RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        Image viewportMask = viewportObj.AddComponent<Image>();
        viewportMask.color = Color.white;

        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRT = contentObj.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;

        // 返回 Content 的父对象 (colObj)，但实际添加子元素应到 Content 下
        // 我们通过 tag 标记 Content，或直接让调用方找到 Content
        // 这里把 contentObj 设为返回对象的 "Content" 引用
        // 为简化，返回 colObj 并在调用方用 GetColumnContent
        return colObj;
    }

    /// <summary>获取列容器内的内容区域</summary>
    private Transform GetColumnContent(GameObject colObj)
    {
        // 路径: colObj -> ColumnScroll -> Viewport -> Content
        Transform scroll = colObj.transform.Find("ColumnScroll");
        if (scroll == null) return colObj.transform;
        Transform viewport = scroll.Find("Viewport");
        if (viewport == null) return colObj.transform;
        Transform content = viewport.Find("Content");
        return content != null ? content : colObj.transform;
    }

    // ====================================================================
    //  成绩列表
    // ====================================================================

    /// <summary>创建成绩列表（课程名 + 分数 + 绩点）</summary>
    private void CreateCourseList(Transform parent, SemesterSummaryData data)
    {
        Transform content = GetColumnContent(parent.gameObject);

        // 标题
        CreateSectionHeader(content, "成绩单");

        // 课程表头
        CreateCourseRow(content, "课程", "分数", "绩点", true);

        // 各科成绩
        if (data.courses != null)
        {
            foreach (CourseGrade course in data.courses)
            {
                CreateCourseRow(content,
                    course.courseName,
                    course.score.ToString("F1"),
                    course.gradePoint.ToString("F2"),
                    false
                );
            }
        }

        // GPA 显示
        CreateGPARow(content, data.gpa);
    }

    /// <summary>创建课程成绩行</summary>
    private void CreateCourseRow(Transform parent, string name, string score, string gpa, bool isHeader)
    {
        GameObject rowObj = new GameObject("CourseRow");
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRT = rowObj.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, isHeader ? 28 : 24);

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(4, 4, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        float fontSize = isHeader ? 16f : 15f;
        Color color = isHeader ? SubHeaderColor : TextWhite;
        FontStyles style = isHeader ? FontStyles.Bold : FontStyles.Normal;

        // 课程名 (占比较大)
        CreateFlexText("Name", rowObj.transform, name, fontSize, color, style,
            TextAlignmentOptions.Left, 3f);

        // 分数
        CreateFlexText("Score", rowObj.transform, score, fontSize, color, style,
            TextAlignmentOptions.Center, 1.2f);

        // 绩点
        CreateFlexText("GPA", rowObj.transform, gpa, fontSize, color, style,
            TextAlignmentOptions.Center, 1f);
    }

    /// <summary>创建 GPA 汇总行</summary>
    private void CreateGPARow(Transform parent, float gpa)
    {
        GameObject rowObj = new GameObject("GPARow");
        rowObj.transform.SetParent(parent, false);

        RectTransform rt = rowObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 36);

        TextMeshProUGUI text = rowObj.AddComponent<TextMeshProUGUI>();
        text.text = $"GPA：{gpa:F2}";
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1.0f, 0.85f, 0.30f); // 金色
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
    }

    // ====================================================================
    //  NPC 好感度列表
    // ====================================================================

    /// <summary>创建 NPC 好感度列表</summary>
    private void CreateNPCList(Transform parent, SemesterSummaryData data)
    {
        Transform content = GetColumnContent(parent.gameObject);

        // 分隔
        CreateThinDivider(content);

        // 标题
        CreateSectionHeader(content, "NPC 好感度");

        if (data.npcRelations == null || data.npcRelations.Count == 0)
        {
            CreateSimpleText(content, "暂无NPC关系数据", TextDim, 14f);
            return;
        }

        foreach (NPCRelationInfo npc in data.npcRelations)
        {
            CreateNPCRow(content, npc);
        }
    }

    /// <summary>创建 NPC 好感行 (名字 + 好感值 + 变化量)</summary>
    private void CreateNPCRow(Transform parent, NPCRelationInfo npc)
    {
        GameObject rowObj = new GameObject("NPCRow");
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRT = rowObj.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 24);

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(4, 4, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // NPC 名字
        CreateFlexText("Name", rowObj.transform, npc.npcName, 15f, TextWhite,
            FontStyles.Normal, TextAlignmentOptions.Left, 2f);

        // 好感值
        CreateFlexText("Friendship", rowObj.transform, npc.friendship.ToString(), 15f,
            TextWhite, FontStyles.Normal, TextAlignmentOptions.Center, 1f);

        // 变化量
        string changeStr = FormatChange(npc.friendshipChange);
        Color changeColor = GetChangeColor(npc.friendshipChange);
        CreateFlexText("Change", rowObj.transform, changeStr, 15f, changeColor,
            FontStyles.Bold, TextAlignmentOptions.Right, 1f);
    }

    // ====================================================================
    //  属性变化列表
    // ====================================================================

    /// <summary>创建属性变化列表</summary>
    private void CreateAttributeChanges(Transform parent, SemesterSummaryData data)
    {
        Transform content = GetColumnContent(parent.gameObject);

        // 标题
        CreateSectionHeader(content, "属性变化");

        if (data.attributeChanges == null || data.attributeChanges.Count == 0)
        {
            CreateSimpleText(content, "暂无属性变化数据", TextDim, 14f);
            return;
        }

        foreach (var kvp in data.attributeChanges)
        {
            CreateAttributeRow(content, kvp.Key, kvp.Value, data);
        }
    }

    /// <summary>创建属性变化行 (属性名 + 变化条 + 变化值)</summary>
    private void CreateAttributeRow(Transform parent, string attrName, int change,
        SemesterSummaryData data)
    {
        GameObject rowObj = new GameObject("AttrRow_" + attrName);
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRT = rowObj.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 30);

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 属性名
        CreateFlexText("Name", rowObj.transform, attrName, 15f, TextWhite,
            FontStyles.Normal, TextAlignmentOptions.Left, 1.2f);

        // 变化条
        CreateChangeBar(rowObj.transform, change);

        // 变化值文本
        string changeStr = FormatChange(change);
        Color changeColor = GetChangeColor(change);
        CreateFlexText("Value", rowObj.transform, changeStr, 15f, changeColor,
            FontStyles.Bold, TextAlignmentOptions.Right, 0.8f);
    }

    /// <summary>创建变化指示条</summary>
    private void CreateChangeBar(Transform parent, int change)
    {
        GameObject barContainer = new GameObject("BarContainer");
        barContainer.transform.SetParent(parent, false);

        RectTransform containerRT = barContainer.AddComponent<RectTransform>();

        LayoutElement containerLE = barContainer.AddComponent<LayoutElement>();
        containerLE.flexibleWidth = 2f;
        containerLE.preferredHeight = 12f;

        // 背景条
        Image barBg = barContainer.AddComponent<Image>();
        barBg.color = new Color(0.15f, 0.15f, 0.20f, 0.80f);

        // 填充条
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barContainer.transform, false);

        RectTransform fillRT = fillObj.AddComponent<RectTransform>();

        // 变化条占比 (以 50 为最大变化量参考)
        float ratio = Mathf.Clamp01(Mathf.Abs(change) / 50f);
        Color fillColor = change >= 0 ? PositiveColor : NegativeColor;
        if (change == 0) fillColor = NeutralColor;

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fillColor;

        if (change >= 0)
        {
            // 从左侧开始填充
            fillRT.anchorMin = new Vector2(0, 0.15f);
            fillRT.anchorMax = new Vector2(ratio, 0.85f);
        }
        else
        {
            // 从右侧向左填充
            fillRT.anchorMin = new Vector2(1f - ratio, 0.15f);
            fillRT.anchorMax = new Vector2(1f, 0.85f);
        }
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
    }

    // ====================================================================
    //  成就列表
    // ====================================================================

    /// <summary>创建成就列表</summary>
    private void CreateAchievementList(Transform parent, SemesterSummaryData data)
    {
        Transform content = GetColumnContent(parent.gameObject);

        // 分隔
        CreateThinDivider(content);

        // 标题
        CreateSectionHeader(content, "本学期成就");

        if (data.unlockedAchievements == null || data.unlockedAchievements.Count == 0)
        {
            CreateSimpleText(content, "本学期暂无解锁成就", TextDim, 14f);
            return;
        }

        foreach (string achievement in data.unlockedAchievements)
        {
            CreateAchievementRow(content, achievement);
        }
    }

    /// <summary>创建成就行</summary>
    private void CreateAchievementRow(Transform parent, string achievementName)
    {
        GameObject rowObj = new GameObject("AchievementRow");
        rowObj.transform.SetParent(parent, false);

        RectTransform rt = rowObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 26);

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.padding = new RectOffset(4, 4, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 星标图标
        CreateSizedText("Star", rowObj.transform, "\u2605", 16f,
            new Color(1.0f, 0.85f, 0.30f), FontStyles.Normal,
            TextAlignmentOptions.Center, new Vector2(24, 26));

        // 成就名
        CreateSizedText("Name", rowObj.transform, achievementName, 15f,
            TextWhite, FontStyles.Normal,
            TextAlignmentOptions.Left, new Vector2(300, 26));
    }

    // ====================================================================
    //  评分明细
    // ====================================================================

    /// <summary>创建评分明细区域</summary>
    private void CreateScoreBreakdown(Transform parent, SemesterSummaryData data)
    {
        GameObject sectionObj = new GameObject("ScoreBreakdown");
        sectionObj.transform.SetParent(parent, false);

        RectTransform sectionRT = sectionObj.AddComponent<RectTransform>();
        sectionRT.sizeDelta = new Vector2(0, 180);

        Image bg = sectionObj.AddComponent<Image>();
        bg.color = SectionBgColor;

        VerticalLayoutGroup vlg = sectionObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(30, 30, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 标题
        CreateSectionHeader(sectionObj.transform, "评分明细");

        // 评分项目
        CreateScoreRow(sectionObj.transform, "学业分", data.academicScore, TextWhite);
        CreateScoreRow(sectionObj.transform, "人际分", data.socialScore, TextWhite);
        CreateScoreRow(sectionObj.transform, "体育分", data.sportsScore, TextWhite);
        CreateScoreRow(sectionObj.transform, "成就分", data.achievementScore, TextWhite);
        CreateScoreRow(sectionObj.transform, "扣分项", -data.penaltyScore, NegativeColor);

        // 分隔线
        CreateThinDivider(sectionObj.transform);

        // 总分
        CreateScoreRow(sectionObj.transform, "总    分", data.totalScore,
            new Color(1.0f, 0.85f, 0.30f), 22f, true);
    }

    /// <summary>创建评分行</summary>
    private void CreateScoreRow(Transform parent, string label, int score, Color scoreColor,
        float fontSize = 17f, bool isBold = false)
    {
        GameObject rowObj = new GameObject("ScoreRow_" + label);
        rowObj.transform.SetParent(parent, false);

        RectTransform rt = rowObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, isBold ? 30 : 24);

        HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(20, 20, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        FontStyles style = isBold ? FontStyles.Bold : FontStyles.Normal;

        // 标签
        CreateFlexText("Label", rowObj.transform, label, fontSize, TextWhite,
            style, TextAlignmentOptions.Left, 1f);

        // 分数
        string scoreStr = score >= 0 ? score.ToString() : score.ToString();
        CreateFlexText("Score", rowObj.transform, scoreStr, fontSize, scoreColor,
            style, TextAlignmentOptions.Right, 1f);
    }

    // ====================================================================
    //  等级大字
    // ====================================================================

    /// <summary>创建总评等级大字显示</summary>
    private void CreateGradeDisplay(Transform parent, SemesterSummaryData data)
    {
        GameObject gradeContainer = new GameObject("GradeDisplay");
        gradeContainer.transform.SetParent(parent, false);

        RectTransform containerRT = gradeContainer.AddComponent<RectTransform>();
        containerRT.sizeDelta = new Vector2(0, 120);

        // "总评等级" 小标题
        GameObject labelObj = new GameObject("GradeLabel");
        labelObj.transform.SetParent(gradeContainer.transform, false);

        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.7f);
        labelRT.anchorMax = new Vector2(1, 1f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "总评等级";
        labelText.fontSize = 18;
        labelText.color = SubHeaderColor;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.enableWordWrapping = false;

        // 等级字母（超大）
        GameObject gradeObj = new GameObject("GradeLetter");
        gradeObj.transform.SetParent(gradeContainer.transform, false);

        gradeTextRT = gradeObj.AddComponent<RectTransform>();
        gradeTextRT.anchorMin = new Vector2(0, 0);
        gradeTextRT.anchorMax = new Vector2(1, 0.75f);
        gradeTextRT.offsetMin = Vector2.zero;
        gradeTextRT.offsetMax = Vector2.zero;

        gradeText = gradeObj.AddComponent<TextMeshProUGUI>();
        gradeText.text = data.GetGradeDisplay();
        gradeText.fontSize = 72;
        gradeText.fontStyle = FontStyles.Bold;
        gradeText.color = data.GetGradeColor();
        gradeText.alignment = TextAlignmentOptions.Center;
        gradeText.enableWordWrapping = false;

        // 初始缩放为0 (等待动画)
        gradeTextRT.localScale = Vector3.zero;
    }

    // ====================================================================
    //  "继续"按钮
    // ====================================================================

    /// <summary>创建"继续"按钮</summary>
    private void CreateContinueButton(Transform parent)
    {
        GameObject btnObj = new GameObject("ContinueButton");
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 50);

        // 用 LayoutElement 让按钮在 VerticalLayoutGroup 中不被拉满宽度
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 220;
        le.preferredHeight = 50;
        le.flexibleWidth = 0;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = ButtonNormalColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = ButtonNormalColor;
        cb.highlightedColor = ButtonHoverColor;
        cb.pressedColor = ButtonPressedColor;
        cb.selectedColor = ButtonNormalColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        btn.onClick.AddListener(() => Hide());

        // 按钮文字
        GameObject textObj = new GameObject("BtnText");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "继 续";
        btnText.fontSize = 22;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = TextWhite;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.enableWordWrapping = false;

        // CanvasGroup 用于 FadeIn 动画
        continueButtonCG = btnObj.AddComponent<CanvasGroup>();
        continueButtonCG.alpha = 0f;
        continueButtonCG.interactable = false;
    }

    // ====================================================================
    //  动画
    // ====================================================================

    /// <summary>显示动画序列</summary>
    private IEnumerator PlayShowAnimation()
    {
        // 1. 遮罩 FadeIn 0.3s
        StartCoroutine(FadeCanvasGroup(overlayCanvasGroup, 0f, 1f, 0.3f));

        yield return new WaitForSeconds(0.1f);

        // 2. 内容面板从底部 SlideIn + FadeIn 0.5s
        Vector2 panelTargetPos = contentPanelRT.anchoredPosition;
        contentPanelRT.anchoredPosition = panelTargetPos + new Vector2(0, -120f);
        StartCoroutine(FadeCanvasGroup(contentCanvasGroup, 0f, 1f, 0.5f));
        StartCoroutine(SlideIn(contentPanelRT, panelTargetPos, 0.5f));

        yield return new WaitForSeconds(0.5f);

        // 3. 等级字母 ScaleIn 弹跳 0.8s
        if (gradeTextRT != null)
        {
            StartCoroutine(AnimateGrade(gradeTextRT, 0.8f));
        }

        yield return new WaitForSeconds(0.8f);

        // 4. "继续"按钮 FadeIn (延迟后出现)
        if (continueButtonCG != null)
        {
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(FadeCanvasGroup(continueButtonCG, 0f, 1f, 0.4f));
            continueButtonCG.interactable = true;
        }
    }

    /// <summary>隐藏动画序列</summary>
    private IEnumerator PlayHideAnimation()
    {
        // 内容面板 FadeOut
        if (contentCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(contentCanvasGroup, 1f, 0f, 0.3f));
        }

        // 遮罩 FadeOut
        if (overlayCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(overlayCanvasGroup, 1f, 0f, 0.3f));
        }

        yield return new WaitForSeconds(0.35f);

        // 销毁 UI
        if (rootObj != null)
        {
            Destroy(rootObj);
            rootObj = null;
        }

        isShowing = false;
        currentData = null;

        Debug.Log("[SemesterSummaryUI] 面板已关闭");
    }

    /// <summary>
    /// 等级字母弹跳动画 —— 从 0 缩放到 1.2 再回弹到 1.0
    /// </summary>
    private IEnumerator AnimateGrade(RectTransform rt, float duration)
    {
        if (rt == null) yield break;

        float elapsed = 0f;

        // 阶段1: 0 -> 1.2 (占 60% 时间)
        float phase1Duration = duration * 0.6f;
        while (elapsed < phase1Duration)
        {
            float t = easingCurve.Evaluate(elapsed / phase1Duration);
            float scale = Mathf.Lerp(0f, 1.2f, t);
            rt.localScale = new Vector3(scale, scale, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.localScale = new Vector3(1.2f, 1.2f, 1f);

        // 阶段2: 1.2 -> 1.0 (占 40% 时间，回弹效果)
        elapsed = 0f;
        float phase2Duration = duration * 0.4f;
        while (elapsed < phase2Duration)
        {
            float t = easingCurve.Evaluate(elapsed / phase2Duration);
            float scale = Mathf.Lerp(1.2f, 1.0f, t);
            rt.localScale = new Vector3(scale, scale, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    /// <summary>CanvasGroup 透明度渐变协程</summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = easingCurve.Evaluate(elapsed / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cg.alpha = to;
    }

    /// <summary>从下方滑入动画协程</summary>
    private IEnumerator SlideIn(RectTransform rt, Vector2 targetPos, float duration)
    {
        if (rt == null) yield break;

        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = easingCurve.Evaluate(elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = targetPos;
    }

    // ====================================================================
    //  UI 工具方法
    // ====================================================================

    /// <summary>创建区域标题文本</summary>
    private void CreateSectionHeader(Transform parent, string text)
    {
        GameObject obj = new GameObject("Header_" + text);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 28);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = SubHeaderColor;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
    }

    /// <summary>创建简单文本行</summary>
    private void CreateSimpleText(Transform parent, string text, Color color, float fontSize)
    {
        GameObject obj = new GameObject("SimpleText");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 22);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    /// <summary>创建弹性宽度文本（用于 HorizontalLayoutGroup 中）</summary>
    private TextMeshProUGUI CreateFlexText(string name, Transform parent, string text,
        float fontSize, Color color, FontStyles style,
        TextAlignmentOptions alignment, float flexWidth)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        obj.AddComponent<RectTransform>();

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.flexibleWidth = flexWidth;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    /// <summary>创建固定尺寸文本</summary>
    private TextMeshProUGUI CreateSizedText(string name, Transform parent, string text,
        float fontSize, Color color, FontStyles style,
        TextAlignmentOptions alignment, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    /// <summary>创建分隔线</summary>
    private void CreateDivider(Transform parent)
    {
        GameObject divObj = new GameObject("Divider");
        divObj.transform.SetParent(parent, false);

        RectTransform rt = divObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 2);

        Image img = divObj.AddComponent<Image>();
        img.color = DividerColor;
    }

    /// <summary>创建细分隔线</summary>
    private void CreateThinDivider(Transform parent)
    {
        GameObject divObj = new GameObject("ThinDivider");
        divObj.transform.SetParent(parent, false);

        RectTransform rt = divObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 1);

        Image img = divObj.AddComponent<Image>();
        img.color = new Color(DividerColor.r, DividerColor.g, DividerColor.b, 0.30f);
    }

    // ====================================================================
    //  文本辅助
    // ====================================================================

    /// <summary>
    /// 格式化变化量: 正数 "+12", 负数 "-5", 零 "0"
    /// </summary>
    private string FormatChange(int change)
    {
        if (change > 0) return $"+{change}";
        if (change < 0) return change.ToString();
        return "0";
    }

    /// <summary>
    /// 获取变化量对应颜色: 正绿, 负红, 零灰
    /// </summary>
    private Color GetChangeColor(int change)
    {
        if (change > 0) return PositiveColor;
        if (change < 0) return NegativeColor;
        return NeutralColor;
    }
}
