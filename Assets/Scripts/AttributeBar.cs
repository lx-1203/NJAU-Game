using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 单个属性条组件。
/// 参考目标是“标签 + 大数字 + 进度条 + 等级徽记”的高辨识度表现，
/// 同时兼容 HUD 紧凑版和信息面板详细版。
/// </summary>
public class AttributeBar : MonoBehaviour
{
    private enum DisplayMode
    {
        Compact,
        Detailed
    }

    // ========== UI 引用 ==========
    private Image cardBackground;
    private Image accentStrip;
    private Image iconImage;
    private TextMeshProUGUI nameLabel;
    private TextMeshProUGUI valueLabel;
    private TextMeshProUGUI deltaLabel;
    private Image fillImage;
    private Image barBackground;
    private TextMeshProUGUI gradeLabel;
    private Image gradeBadge;

    // ========== 动画参数 ==========
    private float animDuration = 0.35f;
    private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Coroutine fillCoroutine;

    // ========== 缓存 ==========
    private float currentFill = 0f;
    private float fillTrackWidth = 150f;
    private DisplayMode mode = DisplayMode.Compact;

    // ========== 颜色 ==========
    private static readonly Color CardBgColor = new Color(0.97f, 0.90f, 0.80f, 0.98f);
    private static readonly Color CardBgDetailed = new Color(0.99f, 0.94f, 0.86f, 0.99f);
    private static readonly Color BorderColor = new Color(0.55f, 0.34f, 0.18f, 0.95f);
    private static readonly Color TextDark = new Color(0.22f, 0.15f, 0.10f, 1f);
    private static readonly Color TextMuted = new Color(0.54f, 0.38f, 0.24f, 1f);
    private static readonly Color BarBgColor = new Color(0.74f, 0.80f, 0.84f, 0.82f);
    private static readonly Color BadgeBg = new Color(0.30f, 0.20f, 0.12f, 0.95f);

    /// <summary>
    /// 创建属性条。
    /// HUD 使用紧凑版，信息面板可使用详细版。
    /// </summary>
    public static AttributeBar Create(Transform parent, bool detailed = false)
    {
        GameObject root = new GameObject("AttributeBar");
        root.transform.SetParent(parent, false);

        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = detailed ? new Vector2(1020f, 88f) : new Vector2(230f, 56f);

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = rootRT.sizeDelta.x;
        layout.preferredHeight = rootRT.sizeDelta.y;
        layout.minHeight = rootRT.sizeDelta.y;

        AttributeBar bar = root.AddComponent<AttributeBar>();
        bar.mode = detailed ? DisplayMode.Detailed : DisplayMode.Compact;
        bar.BuildUI(rootRT);
        return bar;
    }

    public void SetAttribute(PlayerAttributes.AttributeInfo info)
    {
        ApplyStaticVisuals(info);
        UpdateTexts(info);
        UpdateGrade(info);

        float targetFill = Mathf.Clamp01(info.NormalizedValue);
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }
        fillCoroutine = StartCoroutine(AnimateFillCoroutine(targetFill));
    }

    public void SetAttributeImmediate(PlayerAttributes.AttributeInfo info)
    {
        ApplyStaticVisuals(info);
        UpdateTexts(info);
        UpdateGrade(info);

        float targetFill = Mathf.Clamp01(info.NormalizedValue);
        currentFill = targetFill;
        UpdateFillWidth(targetFill);
    }

    private void BuildUI(RectTransform rootRT)
    {
        Image rootBg = gameObject.AddComponent<Image>();
        rootBg.color = mode == DisplayMode.Detailed ? CardBgDetailed : CardBgColor;
        cardBackground = rootBg;

        GameObject frame = CreateRectObject("Frame", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = BorderColor;
        frame.transform.SetAsFirstSibling();

        GameObject inner = CreateRectObject("Inner", transform, Vector2.zero, Vector2.one,
            mode == DisplayMode.Detailed ? new Vector2(3f, 3f) : new Vector2(2f, 2f),
            mode == DisplayMode.Detailed ? new Vector2(-3f, -3f) : new Vector2(-2f, -2f));
        inner.AddComponent<Image>().color = mode == DisplayMode.Detailed ? CardBgDetailed : CardBgColor;

        GameObject accent = CreateRectObject("Accent", inner.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            Vector2.zero, Vector2.zero);
        RectTransform accentRT = accent.GetComponent<RectTransform>();
        accentRT.sizeDelta = new Vector2(mode == DisplayMode.Detailed ? 16f : 10f, 0f);
        accentStrip = accent.AddComponent<Image>();

        if (mode == DisplayMode.Compact)
        {
            BuildCompact(inner.transform, rootRT.sizeDelta);
        }
        else
        {
            BuildDetailed(inner.transform, rootRT.sizeDelta);
        }
    }

    private void BuildCompact(Transform parent, Vector2 size)
    {
        GameObject iconBg = CreateRectObject("IconBg", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, -18f), Vector2.zero);
        RectTransform iconBgRT = iconBg.GetComponent<RectTransform>();
        iconBgRT.pivot = new Vector2(0f, 0.5f);
        iconBgRT.sizeDelta = new Vector2(38f, 38f);
        Image iconBgImage = iconBg.AddComponent<Image>();
        iconBgImage.color = new Color(1f, 1f, 1f, 0.52f);

        GameObject icon = CreateRectObject("Icon", iconBg.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(22f, 22f);
        iconImage = icon.AddComponent<Image>();

        nameLabel = CreateText("Name", parent, "", 15f, TextDark, FontStyles.Bold);
        RectTransform nameRT = nameLabel.rectTransform;
        nameRT.anchorMin = new Vector2(0f, 0.5f);
        nameRT.anchorMax = new Vector2(0f, 0.5f);
        nameRT.pivot = new Vector2(0f, 0.5f);
        nameRT.anchoredPosition = new Vector2(64f, 11f);
        nameRT.sizeDelta = new Vector2(50f, 22f);

        GameObject barBg = CreateRectObject("BarBg", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(110f, -16f), Vector2.zero);
        RectTransform barBgRT = barBg.GetComponent<RectTransform>();
        barBgRT.pivot = new Vector2(0f, 0.5f);
        barBgRT.sizeDelta = new Vector2(86f, 12f);
        barBackground = barBg.AddComponent<Image>();
        barBackground.color = BarBgColor;

        GameObject fill = CreateRectObject("Fill", barBg.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            Vector2.zero, Vector2.zero);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.sizeDelta = new Vector2(0f, 0f);
        fillImage = fill.AddComponent<Image>();
        fillTrackWidth = 86f;

        GameObject badge = CreateRectObject("GradeBadge", parent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-12f, 0f), Vector2.zero);
        RectTransform badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.pivot = new Vector2(1f, 0.5f);
        badgeRT.sizeDelta = new Vector2(28f, 28f);
        gradeBadge = badge.AddComponent<Image>();
        gradeBadge.color = BadgeBg;

        gradeLabel = CreateText("Grade", badge.transform, "F", 18f, Color.white, FontStyles.Bold);
        Stretch(gradeLabel.rectTransform, Vector2.zero, Vector2.zero);
        gradeLabel.alignment = TextAlignmentOptions.Center;

        GameObject overlay = CreateRectObject("Overlay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.transform.SetAsLastSibling();

        valueLabel = CreateText("Value", overlay.transform, "0", 22f, new Color(0.98f, 0.82f, 0.35f, 1f), FontStyles.Bold);
        RectTransform valueRT = valueLabel.rectTransform;
        valueRT.anchorMin = new Vector2(0f, 0.5f);
        valueRT.anchorMax = new Vector2(0f, 0.5f);
        valueRT.pivot = new Vector2(0f, 0.5f);
        valueRT.anchoredPosition = new Vector2(118f, 8f);
        valueRT.sizeDelta = new Vector2(52f, 28f);

        deltaLabel = CreateText("Delta", overlay.transform, "", 10f, TextMuted, FontStyles.Normal);
        RectTransform deltaRT = deltaLabel.rectTransform;
        deltaRT.anchorMin = new Vector2(0f, 0.5f);
        deltaRT.anchorMax = new Vector2(0f, 0.5f);
        deltaRT.pivot = new Vector2(0f, 0.5f);
        deltaRT.anchoredPosition = new Vector2(108f, -2f);
        deltaRT.sizeDelta = new Vector2(96f, 14f);
    }

    private void BuildDetailed(Transform parent, Vector2 size)
    {
        GameObject titleGroup = CreateRectObject("TitleGroup", parent, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(24f, 0f), Vector2.zero);
        RectTransform titleRT = titleGroup.GetComponent<RectTransform>();
        titleRT.pivot = new Vector2(0f, 0.5f);
        titleRT.sizeDelta = new Vector2(196f, 0f);

        GameObject iconBg = CreateRectObject("IconBg", titleGroup.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0f), Vector2.zero);
        RectTransform iconBgRT = iconBg.GetComponent<RectTransform>();
        iconBgRT.pivot = new Vector2(0f, 0.5f);
        iconBgRT.sizeDelta = new Vector2(38f, 38f);
        iconBg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.56f);

        GameObject icon = CreateRectObject("Icon", iconBg.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(22f, 22f);
        iconImage = icon.AddComponent<Image>();

        nameLabel = CreateText("Name", titleGroup.transform, "", 24f, TextDark, FontStyles.Bold);
        RectTransform nameLabelRT = nameLabel.rectTransform;
        nameLabelRT.anchorMin = new Vector2(0f, 0.5f);
        nameLabelRT.anchorMax = new Vector2(0f, 0.5f);
        nameLabelRT.pivot = new Vector2(0f, 0.5f);
        nameLabelRT.anchoredPosition = new Vector2(56f, 4f);
        nameLabelRT.sizeDelta = new Vector2(136f, 40f);

        GameObject barBg = CreateRectObject("BarBg", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(286f, -20f), Vector2.zero);
        RectTransform barBgRT = barBg.GetComponent<RectTransform>();
        barBgRT.pivot = new Vector2(0f, 0.5f);
        barBgRT.sizeDelta = new Vector2(484f, 18f);
        barBackground = barBg.AddComponent<Image>();
        barBackground.color = BarBgColor;

        GameObject fill = CreateRectObject("Fill", barBg.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            Vector2.zero, Vector2.zero);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.sizeDelta = new Vector2(0f, 0f);
        fillImage = fill.AddComponent<Image>();
        fillTrackWidth = 484f;

        GameObject badge = CreateRectObject("GradeBadge", parent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-18f, 0f), Vector2.zero);
        RectTransform badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.pivot = new Vector2(1f, 0.5f);
        badgeRT.sizeDelta = new Vector2(56f, 56f);
        gradeBadge = badge.AddComponent<Image>();
        gradeBadge.color = BadgeBg;

        gradeLabel = CreateText("Grade", badge.transform, "F", 28f, Color.white, FontStyles.Bold);
        Stretch(gradeLabel.rectTransform, Vector2.zero, Vector2.zero);
        gradeLabel.alignment = TextAlignmentOptions.Center;

        GameObject overlay = CreateRectObject("Overlay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.transform.SetAsLastSibling();

        valueLabel = CreateText("Value", overlay.transform, "0", 34f, new Color(0.16f, 0.12f, 0.08f, 1f), FontStyles.Bold);
        RectTransform valueRT = valueLabel.rectTransform;
        valueRT.anchorMin = new Vector2(0f, 0.5f);
        valueRT.anchorMax = new Vector2(0f, 0.5f);
        valueRT.pivot = new Vector2(0f, 0.5f);
        valueRT.anchoredPosition = new Vector2(244f, 12f);
        valueRT.sizeDelta = new Vector2(100f, 40f);

        deltaLabel = CreateText("Delta", overlay.transform, "", 14f, TextMuted, FontStyles.Bold);
        RectTransform deltaRT = deltaLabel.rectTransform;
        deltaRT.anchorMin = new Vector2(0f, 0.5f);
        deltaRT.anchorMax = new Vector2(0f, 0.5f);
        deltaRT.pivot = new Vector2(0f, 0.5f);
        deltaRT.anchoredPosition = new Vector2(372f, 12f);
        deltaRT.sizeDelta = new Vector2(150f, 24f);
    }

    private void ApplyStaticVisuals(PlayerAttributes.AttributeInfo info)
    {
        if (iconImage != null)
        {
            iconImage.color = info.barColor;
        }

        if (fillImage != null)
        {
            fillImage.color = info.barColor;
        }

        if (accentStrip != null)
        {
            accentStrip.color = Color.Lerp(info.barColor, Color.white, 0.18f);
        }

        if (nameLabel != null)
        {
            nameLabel.text = info.name;
        }

        if (cardBackground != null)
        {
            cardBackground.color = mode == DisplayMode.Detailed ? CardBgDetailed : CardBgColor;
        }
    }

    private void UpdateTexts(PlayerAttributes.AttributeInfo info)
    {
        if (valueLabel != null)
        {
            valueLabel.text = info.isPercentage ? $"{info.value}%" : info.value.ToString();
        }

        if (deltaLabel == null)
        {
            return;
        }

        int nextThreshold = AttributeGradeSettings.GetNextThreshold(info.value);
        int remaining = nextThreshold - info.value;
        if (remaining > 0)
        {
            deltaLabel.text = $"还差 {remaining}";
            deltaLabel.gameObject.SetActive(true);
        }
        else
        {
            deltaLabel.text = info.isPercentage ? "已满" : "顶级";
            deltaLabel.gameObject.SetActive(mode == DisplayMode.Detailed);
        }
    }

    private void UpdateGrade(PlayerAttributes.AttributeInfo info)
    {
        if (gradeLabel == null)
        {
            return;
        }

        string grade;
        Color gradeColor;

        grade = AttributeGradeSettings.GetGradeLetter(info.value);

        if (grade == "S")
        {
            gradeColor = new Color(1.00f, 0.88f, 0.38f);
        }
        else if (grade == "A")
        {
            gradeColor = new Color(1.00f, 0.67f, 0.22f);
        }
        else if (grade == "B")
        {
            gradeColor = new Color(0.46f, 0.78f, 0.35f);
        }
        else if (grade == "C")
        {
            gradeColor = new Color(0.38f, 0.67f, 0.95f);
        }
        else if (grade == "D")
        {
            gradeColor = new Color(0.88f, 0.53f, 0.26f);
        }
        else
        {
            gradeColor = new Color(0.86f, 0.36f, 0.32f);
        }

        gradeLabel.text = grade;
        gradeLabel.color = gradeColor;

        if (gradeBadge != null)
        {
            gradeBadge.color = new Color(0.24f, 0.16f, 0.10f, 0.96f);
        }
    }

    private IEnumerator AnimateFillCoroutine(float targetFill)
    {
        float startFill = currentFill;
        float elapsedTime = 0f;

        while (elapsedTime < animDuration)
        {
            float t = easingCurve.Evaluate(elapsedTime / animDuration);
            currentFill = Mathf.Lerp(startFill, targetFill, t);
            UpdateFillWidth(currentFill);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentFill = targetFill;
        UpdateFillWidth(targetFill);
        fillCoroutine = null;
    }

    private void UpdateFillWidth(float normalizedValue)
    {
        if (fillImage == null)
        {
            return;
        }

        RectTransform fillRT = fillImage.rectTransform;
        fillRT.sizeDelta = new Vector2(fillTrackWidth * Mathf.Clamp01(normalizedValue), fillRT.sizeDelta.y);
    }

    private static GameObject CreateRectObject(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return obj;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, FontStyles style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 30f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.margin = new Vector4(2f, 4f, 2f, 4f);
        tmp.extraPadding = true;

        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(tmp);
        }

        return tmp;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }
}
