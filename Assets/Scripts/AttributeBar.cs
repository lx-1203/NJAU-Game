using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 单个属性条组件 —— 参照参考界面的卡片式属性条
/// 布局：[属性图标] [属性名] [进度条 + 还差X] [等级字母]
/// 支持平滑动画过渡
/// </summary>
public class AttributeBar : MonoBehaviour
{
    // ========== UI 引用 ==========
    private Image iconImage;             // 属性图标
    private TextMeshProUGUI nameLabel;   // 属性名
    private Image fillImage;             // 进度条填充
    private Image barBackground;         // 进度条背景
    private TextMeshProUGUI valueLabel;  // 数值文本（还差X）
    private TextMeshProUGUI gradeLabel;  // 等级字母（F/D/C/B/A/S）

    // ========== 动画参数 ==========
    private float animDuration = 0.35f;
    private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Coroutine fillCoroutine;

    // ========== 缓存 ==========
    private float currentFill = 0f;

    // ========== 尺寸常量 ==========
    private const float BarHeight = 14f;
    private const float BarWidth = 100f;
    private const float IconSize = 22f;
    private const float NameWidth = 20f;
    private const float ValueWidth = 55f;
    private const float GradeWidth = 22f;
    private const float TotalHeight = 28f;

    // ========== 颜色 ==========
    private static readonly Color BarBgColor = new Color(0.75f, 0.82f, 0.88f, 0.6f);
    private static readonly Color TextDark = new Color(0.20f, 0.15f, 0.10f);
    private static readonly Color TextGray = new Color(0.45f, 0.45f, 0.50f);

    /// <summary>
    /// 在指定父对象下创建一个属性条
    /// </summary>
    public static AttributeBar Create(Transform parent)
    {
        GameObject root = new GameObject("AttributeBar");
        root.transform.SetParent(parent, false);
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(190, TotalHeight);

        HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        AttributeBar bar = root.AddComponent<AttributeBar>();

        // --- 属性图标（彩色小圆点） ---
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(root.transform, false);
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(IconSize, IconSize);
        bar.iconImage = iconObj.AddComponent<Image>();
        bar.iconImage.color = Color.white;

        // --- 进度条区域 ---
        GameObject barArea = new GameObject("BarArea");
        barArea.transform.SetParent(root.transform, false);
        RectTransform baRT = barArea.AddComponent<RectTransform>();
        baRT.sizeDelta = new Vector2(BarWidth, TotalHeight);

        // 进度条背景
        GameObject barBg = new GameObject("BarBackground");
        barBg.transform.SetParent(barArea.transform, false);
        RectTransform bgRT = barBg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.3f);
        bgRT.anchorMax = new Vector2(1, 0.7f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bar.barBackground = barBg.AddComponent<Image>();
        bar.barBackground.color = BarBgColor;

        // 进度条填充
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barBg.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.pivot = new Vector2(0, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fillRT.sizeDelta = new Vector2(0, 0);
        bar.fillImage = fillObj.AddComponent<Image>();
        bar.fillImage.color = Color.white;

        // 数值文本（还差X 或 当前值）
        bar.valueLabel = CreateText("Value", barArea.transform,
            "", 11f, TextGray, TextAlignmentOptions.Right, new Vector2(BarWidth, TotalHeight));
        RectTransform valRT = bar.valueLabel.rectTransform;
        valRT.anchorMin = Vector2.zero;
        valRT.anchorMax = Vector2.one;
        valRT.offsetMin = Vector2.zero;
        valRT.offsetMax = Vector2.zero;

        // --- 等级字母 ---
        GameObject gradeObj = new GameObject("Grade");
        gradeObj.transform.SetParent(root.transform, false);
        RectTransform gradeRT = gradeObj.AddComponent<RectTransform>();
        gradeRT.sizeDelta = new Vector2(GradeWidth, TotalHeight);
        bar.gradeLabel = gradeObj.AddComponent<TextMeshProUGUI>();
        bar.gradeLabel.fontSize = 18f;
        bar.gradeLabel.alignment = TextAlignmentOptions.Center;
        bar.gradeLabel.color = TextDark;
        bar.gradeLabel.fontStyle = FontStyles.Bold;
        bar.gradeLabel.text = "F";

        return bar;
    }

    /// <summary>
    /// 设置属性条数据（带平滑动画）
    /// </summary>
    public void SetAttribute(PlayerAttributes.AttributeInfo info)
    {
        if (iconImage != null) iconImage.color = info.barColor;
        if (fillImage != null) fillImage.color = info.barColor;

        // 更新数值文本
        UpdateValueText(info);

        // 更新等级字母
        UpdateGrade(info);

        // 启动平滑填充动画
        float targetFill = info.NormalizedValue;
        if (fillCoroutine != null) StopCoroutine(fillCoroutine);
        fillCoroutine = StartCoroutine(AnimateFillCoroutine(targetFill));
    }

    /// <summary>
    /// 立即设置属性条（无动画），用于初始化
    /// </summary>
    public void SetAttributeImmediate(PlayerAttributes.AttributeInfo info)
    {
        if (iconImage != null) iconImage.color = info.barColor;
        if (fillImage != null) fillImage.color = info.barColor;

        UpdateValueText(info);
        UpdateGrade(info);

        float targetFill = info.NormalizedValue;
        currentFill = targetFill;
        UpdateFillWidth(targetFill);
    }

    private void UpdateValueText(PlayerAttributes.AttributeInfo info)
    {
        if (valueLabel == null) return;

        // 计算距离下一等级还差多少
        int nextThreshold = GetNextThreshold(info.value);
        int remaining = nextThreshold - info.value;

        if (remaining > 0)
        {
            valueLabel.text = $"{info.value}  还差{remaining}";
        }
        else
        {
            valueLabel.text = info.isPercentage ? $"{info.value}%" : $"{info.value}";
        }
    }

    private void UpdateGrade(PlayerAttributes.AttributeInfo info)
    {
        if (gradeLabel == null) return;

        string grade;
        Color gradeColor;

        if (info.value >= 90)
        {
            grade = "S"; gradeColor = new Color(0.85f, 0.60f, 0.10f); // 金色
        }
        else if (info.value >= 75)
        {
            grade = "A"; gradeColor = new Color(0.20f, 0.70f, 0.30f); // 绿色
        }
        else if (info.value >= 60)
        {
            grade = "B"; gradeColor = new Color(0.30f, 0.55f, 0.85f); // 蓝色
        }
        else if (info.value >= 40)
        {
            grade = "C"; gradeColor = new Color(0.80f, 0.60f, 0.20f); // 橙色
        }
        else if (info.value >= 25)
        {
            grade = "D"; gradeColor = new Color(0.85f, 0.40f, 0.30f); // 红橙
        }
        else
        {
            grade = "F"; gradeColor = new Color(0.70f, 0.25f, 0.25f); // 红色
        }

        gradeLabel.text = grade;
        gradeLabel.color = gradeColor;
    }

    private int GetNextThreshold(int currentValue)
    {
        if (currentValue < 25) return 25;
        if (currentValue < 40) return 40;
        if (currentValue < 60) return 60;
        if (currentValue < 75) return 75;
        if (currentValue < 90) return 90;
        return 100;
    }

    // ========== 协程动画 ==========

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
        if (fillImage != null)
        {
            RectTransform fillRT = fillImage.rectTransform;
            fillRT.sizeDelta = new Vector2(BarWidth * normalizedValue, fillRT.sizeDelta.y);
        }
    }

    // ========== 工具 ==========

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text,
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
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }
}
