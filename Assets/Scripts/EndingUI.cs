using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// 结局展示面板 — 纯代码动态创建，展示结局名称、星级、CG、统计数据与天赋点。
/// </summary>
public class EndingUI : MonoBehaviour
{
    // ========== 单例 ==========

    /// <summary>单例实例 (场景内，不跨场景保留)</summary>
    public static EndingUI Instance { get; private set; }

    // ========== 公开属性 ==========

    /// <summary>面板是否正在显示</summary>
    public bool isShowing { get; private set; }

    // ========== 私有引用 ==========

    private GameObject _canvasRoot;
    private Canvas _canvas;
    private CanvasGroup _backgroundGroup;
    private Image _backgroundImage;

    // 内容元素
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _starsText;
    private CanvasGroup _cgGroup;
    private TextMeshProUGUI _cgLabel;
    private CanvasGroup _descGroup;
    private TextMeshProUGUI _descText;
    private CanvasGroup _statsGroup;
    private RectTransform _statsContainer;
    private CanvasGroup _talentGroup;
    private TextMeshProUGUI _talentText;
    private CanvasGroup _buttonGroup;
    private Button _returnButton;

    // 动画数据
    private EndingResult _currentResult;
    private Coroutine _showCoroutine;

    // ========== 颜色常量 ==========

    private static readonly Color COLOR_GOLD = new Color32(0xFF, 0xD7, 0x00, 0xFF);
    private static readonly Color COLOR_STAR_EMPTY = new Color32(0x40, 0x40, 0x40, 0xFF);
    private static readonly Color COLOR_WHITE = Color.white;
    private static readonly Color COLOR_LIGHT_GRAY = new Color(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color COLOR_DARK_GRAY = new Color(0.15f, 0.15f, 0.15f, 1f);

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
            Instance = null;
    }

    // ========== 公开方法 ==========

    /// <summary>
    /// 显示结局面板
    /// </summary>
    /// <param name="result">结局结算数据</param>
    public void Show(EndingResult result)
    {
        if (isShowing) return;

        _currentResult = result;
        isShowing = true;

        // 构建 UI
        CreateCanvas();

        var contentRoot = CreateContentRoot();
        _titleText = CreateEndingTitle(contentRoot, result.ending.name);
        _starsText = CreateStarDisplay(contentRoot, result.ending.stars);
        _cgGroup = CreateCGPlaceholder(contentRoot, result.ending.cgId);
        _descGroup = CreateDescriptionText(contentRoot, result.ending.description);
        CreateSeparator(contentRoot);
        _statsGroup = CreateStatsSummary(contentRoot, result);
        _talentGroup = CreateTalentPointDisplay(contentRoot, result.talentPoints);
        _buttonGroup = CreateReturnButton(contentRoot);

        // 初始隐藏所有元素
        SetAlpha(_backgroundGroup, 0f);
        _titleText.color = SetAlpha(_titleText.color, 0f);
        _starsText.color = SetAlpha(_starsText.color, 0f);
        SetAlpha(_cgGroup, 0f);
        SetAlpha(_descGroup, 0f);
        SetAlpha(_statsGroup, 0f);
        SetAlpha(_talentGroup, 0f);
        _talentGroup.transform.localScale = Vector3.zero;
        SetAlpha(_buttonGroup, 0f);

        // 启动动画序列
        _showCoroutine = StartCoroutine(PlayShowSequence());
    }

    /// <summary>
    /// 关闭结局面板
    /// </summary>
    public void Hide()
    {
        if (!isShowing) return;

        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        if (_canvasRoot != null)
        {
            Destroy(_canvasRoot);
            _canvasRoot = null;
        }

        isShowing = false;
    }

    // ========== UI 构建 ==========

    /// <summary>
    /// 创建独立 Canvas (sortingOrder=200, ScreenSpaceOverlay, 1920x1080)
    /// </summary>
    private void CreateCanvas()
    {
        // Canvas 根对象
        _canvasRoot = new GameObject("EndingUI_Canvas");
        _canvasRoot.transform.SetParent(transform);

        _canvas = _canvasRoot.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

        var scaler = _canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _canvasRoot.AddComponent<GraphicRaycaster>();

        // 全屏纯黑背景
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_canvasRoot.transform, false);

        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        _backgroundImage = bgObj.AddComponent<Image>();
        _backgroundImage.color = Color.black;

        _backgroundGroup = bgObj.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// 创建内容根节点 (居中垂直布局 + 滚动)
    /// </summary>
    private RectTransform CreateContentRoot()
    {
        // ScrollView
        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(_canvasRoot.transform, false);

        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 30f;

        // Viewport (带 Mask)
        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);

        var viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 0);
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        scroll.viewport = viewportRect;

        // Content (垂直布局)
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(1920f, 0f);

        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 30f;
        vlg.padding = new RectOffset(200, 200, 80, 80);

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        return contentRect;
    }

    /// <summary>
    /// 创建结局名称 — 大字金色 (fontSize=60)
    /// </summary>
    private TextMeshProUGUI CreateEndingTitle(RectTransform parent, string endingName)
    {
        var obj = new GameObject("EndingTitle");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 80f);

        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 80f;
        le.flexibleWidth = 1f;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = ""; // 由 TypeWriter 协程逐字填充
        tmp.fontSize = 60;
        tmp.color = COLOR_GOLD;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        _titleText = tmp;
        return tmp;
    }

    /// <summary>
    /// 创建星级显示 — "★" 字符，满星金色，空星灰色
    /// </summary>
    private TextMeshProUGUI CreateStarDisplay(RectTransform parent, int stars)
    {
        var obj = new GameObject("StarDisplay");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 60f);

        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 60f;
        le.flexibleWidth = 1f;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.richText = true;

        // 构建富文本: 满星金色 + 空星灰色
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 7; i++)
        {
            if (i < stars)
                sb.Append($"<color=#FFD700>★</color>");
            else
                sb.Append($"<color=#404040>★</color>");
        }
        tmp.text = sb.ToString();

        // 初始全部灰色，由协程逐个亮起
        var initSb = new System.Text.StringBuilder();
        for (int i = 0; i < 7; i++)
        {
            initSb.Append($"<color=#404040>★</color>");
        }
        tmp.text = initSb.ToString();

        _starsText = tmp;
        return tmp;
    }

    /// <summary>
    /// 创建 CG 占位区 — 灰色矩形框，中间写 "CG: {cgId}"
    /// </summary>
    private CanvasGroup CreateCGPlaceholder(RectTransform parent, string cgId)
    {
        var obj = new GameObject("CGPlaceholder");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 300f);

        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 300f;
        le.flexibleWidth = 1f; // 宽度由父 LayoutGroup 的 padding 控制约 80%

        var bg = obj.AddComponent<Image>();
        bg.color = COLOR_DARK_GRAY;

        // 边框效果 — 用 Outline
        var outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        outline.effectDistance = new Vector2(2, 2);

        // CG 标签文字
        var labelObj = new GameObject("CGLabel");
        labelObj.transform.SetParent(obj.transform, false);

        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        _cgLabel = labelObj.AddComponent<TextMeshProUGUI>();
        _cgLabel.text = $"CG: {cgId}";
        _cgLabel.fontSize = 36;
        _cgLabel.color = COLOR_LIGHT_GRAY;
        _cgLabel.alignment = TextAlignmentOptions.Center;

        var group = obj.AddComponent<CanvasGroup>();
        _cgGroup = group;
        return group;
    }

    /// <summary>
    /// 创建结局描述文本 — 多行, fontSize=24, 居中, 行间距1.2
    /// </summary>
    private CanvasGroup CreateDescriptionText(RectTransform parent, string description)
    {
        var obj = new GameObject("Description");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();

        var le = obj.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = description ?? "";
        tmp.fontSize = 24;
        tmp.color = COLOR_WHITE;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.lineSpacing = 20f; // TMP lineSpacing 是百分比偏移, 20 ≈ 1.2x 行间距

        _descText = tmp;

        var group = obj.AddComponent<CanvasGroup>();
        _descGroup = group;
        return group;
    }

    /// <summary>
    /// 创建分隔线 — 细白色横线
    /// </summary>
    private void CreateSeparator(RectTransform parent)
    {
        var obj = new GameObject("Separator");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 2f);

        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.flexibleWidth = 1f;

        var img = obj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.3f);
    }

    /// <summary>
    /// 创建 "你的大学四年" 数据总结 — 标题 + 2x4 网格
    /// </summary>
    private CanvasGroup CreateStatsSummary(RectTransform parent, EndingResult result)
    {
        var containerObj = new GameObject("StatsSummary");
        containerObj.transform.SetParent(parent, false);

        var containerRect = containerObj.AddComponent<RectTransform>();

        var containerLE = containerObj.AddComponent<LayoutElement>();
        containerLE.flexibleWidth = 1f;

        var containerVLG = containerObj.AddComponent<VerticalLayoutGroup>();
        containerVLG.childAlignment = TextAnchor.UpperCenter;
        containerVLG.childControlWidth = true;
        containerVLG.childControlHeight = true;
        containerVLG.childForceExpandWidth = false;
        containerVLG.childForceExpandHeight = false;
        containerVLG.spacing = 20f;

        var containerCSF = containerObj.AddComponent<ContentSizeFitter>();
        containerCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 标题: "你的大学四年"
        var titleObj = new GameObject("StatsTitle");
        titleObj.transform.SetParent(containerObj.transform, false);

        var titleLE = titleObj.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 50f;
        titleLE.flexibleWidth = 1f;

        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "你的大学四年";
        titleTmp.fontSize = 36;
        titleTmp.color = COLOR_WHITE;
        titleTmp.alignment = TextAlignmentOptions.Center;

        // 第一行: 总学习次 | 总社交次 | 总出校门 | 总睡觉次
        CreateStatsRow(containerObj.transform,
            ("总学习次", result.totalStudyCount.ToString()),
            ("总社交次", result.totalSocialCount.ToString()),
            ("总出校门", result.totalGoOutCount.ToString()),
            ("总睡觉次", result.totalSleepCount.ToString())
        );

        // 第二行: 总花费 | 最终GPA | 成就数 | 总回合数
        CreateStatsRow(containerObj.transform,
            ("总花费", $"¥{result.totalMoneySpent}"),
            ("最终GPA", result.finalGPA.ToString("F2")),
            ("成就数", result.achievementCount.ToString()),
            ("总回合数", result.totalRounds.ToString())
        );

        var group = containerObj.AddComponent<CanvasGroup>();
        _statsGroup = group;
        _statsContainer = containerRect;
        return group;
    }

    /// <summary>
    /// 创建统计行 — 4 列
    /// </summary>
    private void CreateStatsRow(Transform parent, params (string label, string value)[] items)
    {
        var rowObj = new GameObject("StatsRow");
        rowObj.transform.SetParent(parent, false);

        var rowLE = rowObj.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 80f;
        rowLE.flexibleWidth = 1f;

        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 10f;

        foreach (var (label, value) in items)
        {
            CreateStatCell(rowObj.transform, label, value);
        }
    }

    /// <summary>
    /// 创建单个统计格子 — 标签 + 数值上下排列
    /// </summary>
    private void CreateStatCell(Transform parent, string label, string value)
    {
        var cellObj = new GameObject($"Stat_{label}");
        cellObj.transform.SetParent(parent, false);

        var cellVLG = cellObj.AddComponent<VerticalLayoutGroup>();
        cellVLG.childAlignment = TextAnchor.MiddleCenter;
        cellVLG.childControlWidth = true;
        cellVLG.childControlHeight = true;
        cellVLG.childForceExpandWidth = true;
        cellVLG.childForceExpandHeight = false;
        cellVLG.spacing = 4f;

        // 数值 (大字)
        var valueObj = new GameObject("Value");
        valueObj.transform.SetParent(cellObj.transform, false);

        var valueLE = valueObj.AddComponent<LayoutElement>();
        valueLE.preferredHeight = 40f;

        var valueTmp = valueObj.AddComponent<TextMeshProUGUI>();
        valueTmp.text = value;
        valueTmp.fontSize = 32;
        valueTmp.color = COLOR_GOLD;
        valueTmp.alignment = TextAlignmentOptions.Center;

        // 标签 (小字)
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(cellObj.transform, false);

        var labelLE = labelObj.AddComponent<LayoutElement>();
        labelLE.preferredHeight = 30f;

        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 20;
        labelTmp.color = COLOR_LIGHT_GRAY;
        labelTmp.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// 创建天赋点显示 — "获得天赋点: X" 金色大字
    /// </summary>
    private CanvasGroup CreateTalentPointDisplay(RectTransform parent, int points)
    {
        var obj = new GameObject("TalentPoints");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 70f);

        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 70f;
        le.flexibleWidth = 1f;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"获得天赋点: {points}";
        tmp.fontSize = 48;
        tmp.color = COLOR_GOLD;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        _talentText = tmp;

        var group = obj.AddComponent<CanvasGroup>();
        _talentGroup = group;
        return group;
    }

    /// <summary>
    /// 创建 "返回标题" 按钮
    /// </summary>
    private CanvasGroup CreateReturnButton(RectTransform parent)
    {
        // 按钮容器 (用于居中 + LayoutElement)
        var wrapperObj = new GameObject("ReturnButtonWrapper");
        wrapperObj.transform.SetParent(parent, false);

        var wrapperLE = wrapperObj.AddComponent<LayoutElement>();
        wrapperLE.preferredHeight = 70f;
        wrapperLE.preferredWidth = 300f;
        wrapperLE.flexibleWidth = 0f;

        // 按钮背景
        var btnImage = wrapperObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Outline 边框
        var btnOutline = wrapperObj.AddComponent<Outline>();
        btnOutline.effectColor = COLOR_GOLD;
        btnOutline.effectDistance = new Vector2(2, 2);

        // Button 组件
        _returnButton = wrapperObj.AddComponent<Button>();
        var colors = _returnButton.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        _returnButton.colors = colors;
        _returnButton.onClick.AddListener(OnReturnButtonClicked);

        // 按钮文字
        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(wrapperObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.text = "返回标题";
        textTmp.fontSize = 32;
        textTmp.color = COLOR_GOLD;
        textTmp.alignment = TextAlignmentOptions.Center;

        var group = wrapperObj.AddComponent<CanvasGroup>();
        _buttonGroup = group;
        return group;
    }

    // ========== 按钮回调 ==========

    /// <summary>
    /// 返回标题按钮点击回调
    /// </summary>
    private void OnReturnButtonClicked()
    {
        Hide();
        SceneManager.LoadScene("TitleScreen");
    }

    // ========== 动画协程 ==========

    /// <summary>
    /// 主展示动画序列 — 依次播放各元素的入场动画
    /// </summary>
    private IEnumerator PlayShowSequence()
    {
        // 1. 黑屏 FadeIn (0.5s)
        yield return StartCoroutine(FadeCanvasGroup(_backgroundGroup, 0f, 1f, 0.5f));

        // 2. 结局名称 TypeWriter 逐字出现 (每字 0.08s)
        yield return StartCoroutine(TypeWriterCoroutine(_titleText, _currentResult.ending.name, 0.08f));

        // 3. 星星逐个亮起 (每颗 0.3s 间隔)
        yield return StartCoroutine(StarLightUpCoroutine(_starsText, _currentResult.ending.stars, 0.3f));

        // 4. CG 框 FadeIn (0.8s)
        yield return StartCoroutine(FadeCanvasGroup(_cgGroup, 0f, 1f, 0.8f));

        // 5. 结局文本 FadeIn (0.5s)
        yield return StartCoroutine(FadeCanvasGroup(_descGroup, 0f, 1f, 0.5f));

        // 6. 统计区域 FadeIn (0.5s)
        yield return StartCoroutine(FadeCanvasGroup(_statsGroup, 0f, 1f, 0.5f));

        // 7. 天赋点 ScaleIn 弹跳 (0.5s)
        yield return StartCoroutine(ScaleBounceIn(_talentGroup, 0.5f));

        // 8. "返回标题" 按钮 FadeIn (延迟 0.5s 后)
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeCanvasGroup(_buttonGroup, 0f, 1f, 0.5f));

        _showCoroutine = null;
    }

    /// <summary>
    /// TypeWriter 逐字出现协程
    /// </summary>
    /// <param name="tmp">目标文本组件</param>
    /// <param name="fullText">完整文本</param>
    /// <param name="charInterval">每字间隔 (秒)</param>
    private IEnumerator TypeWriterCoroutine(TextMeshProUGUI tmp, string fullText, float charInterval)
    {
        tmp.color = SetAlpha(tmp.color, 1f);
        tmp.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            tmp.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(charInterval);
        }
    }

    /// <summary>
    /// 星星逐个亮起协程
    /// </summary>
    /// <param name="tmp">星星文本组件</param>
    /// <param name="stars">满星数量 (0-7)</param>
    /// <param name="interval">每颗星间隔 (秒)</param>
    private IEnumerator StarLightUpCoroutine(TextMeshProUGUI tmp, int stars, float interval)
    {
        tmp.color = SetAlpha(tmp.color, 1f);

        for (int lit = 0; lit <= stars; lit++)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 7; i++)
            {
                if (i < lit)
                    sb.Append("<color=#FFD700>★</color>");
                else
                    sb.Append("<color=#404040>★</color>");
            }
            tmp.text = sb.ToString();

            if (lit < stars)
                yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// CanvasGroup 透明度渐变协程
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        AnimationCurve curve = AnimationCurve.EaseInOut(0f, from, 1f, to);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = curve.Evaluate(t);
            yield return null;
        }

        group.alpha = to;
    }

    /// <summary>
    /// Scale 弹跳入场协程 (0 → 过冲 → 1)
    /// </summary>
    private IEnumerator ScaleBounceIn(CanvasGroup group, float duration)
    {
        if (group == null) yield break;

        Transform t = group.transform;
        group.alpha = 1f;
        t.localScale = Vector3.zero;

        float elapsed = 0f;

        // 关键帧: 0→0, 0.6→1.15 (过冲), 0.8→0.95, 1.0→1.0
        Keyframe[] keys = new Keyframe[]
        {
            new Keyframe(0f, 0f),
            new Keyframe(0.6f, 1.15f),
            new Keyframe(0.8f, 0.95f),
            new Keyframe(1f, 1f)
        };
        AnimationCurve curve = new AnimationCurve(keys);

        // 平滑切线
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float scale = curve.Evaluate(p);
            t.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    // ========== 工具方法 ==========

    /// <summary>
    /// 设置 CanvasGroup 透明度
    /// </summary>
    private void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    /// <summary>
    /// 返回修改了 alpha 的颜色副本
    /// </summary>
    private Color SetAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
