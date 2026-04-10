using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 单个属性条组件 —— 显示一个属性的名称、彩色进度条和数值
/// 支持平滑动画过渡（参考 UIAnimator 协程风格）
/// </summary>
public class AttributeBar : MonoBehaviour
{
    // ========== UI 引用 ==========
    private TextMeshProUGUI nameLabel;   // 属性名
    private Image fillImage;             // 进度条填充
    private TextMeshProUGUI valueLabel;  // 数值文本

    // ========== 动画参数 ==========
    private float animDuration = 0.35f;
    private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private Coroutine fillCoroutine;

    // ========== 缓存 ==========
    private float currentFill = 0f;

    // ========== 尺寸常量 ==========
    private const float BarHeight = 18f;
    private const float BarWidth = 150f;
    private const float NameWidth = 55f;
    private const float ValueWidth = 50f;
    private const float TotalHeight = 24f;

    /// <summary>
    /// 在指定父对象下创建一个属性条，并返回组件实例
    /// </summary>
    public static AttributeBar Create(Transform parent)
    {
        // 根容器
        GameObject root = new GameObject("AttributeBar");
        root.transform.SetParent(parent, false);
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(NameWidth + BarWidth + ValueWidth + 10f, TotalHeight);

        // 添加 HorizontalLayoutGroup 方便水平排列
        HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        AttributeBar bar = root.AddComponent<AttributeBar>();

        // --- 属性名 ---
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.sizeDelta = new Vector2(NameWidth, TotalHeight);

        bar.nameLabel = nameObj.AddComponent<TextMeshProUGUI>();
        bar.nameLabel.fontSize = 14f;
        bar.nameLabel.alignment = TextAlignmentOptions.Left;
        bar.nameLabel.color = new Color(0.85f, 0.85f, 0.85f);
        bar.nameLabel.text = "";

        // --- 进度条背景 ---
        GameObject barBg = new GameObject("BarBackground");
        barBg.transform.SetParent(root.transform, false);
        RectTransform bgRT = barBg.AddComponent<RectTransform>();
        bgRT.sizeDelta = new Vector2(BarWidth, BarHeight);

        Image bgImage = barBg.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.20f, 0.8f);

        // --- 进度条填充 ---
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barBg.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        // 锚点设在左侧，通过宽度控制填充
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.pivot = new Vector2(0, 0.5f);
        fillRT.anchoredPosition = Vector2.zero;
        fillRT.sizeDelta = new Vector2(0, 0); // 初始宽度为 0

        bar.fillImage = fillObj.AddComponent<Image>();
        bar.fillImage.color = Color.white;

        // --- 数值标签 ---
        GameObject valObj = new GameObject("Value");
        valObj.transform.SetParent(root.transform, false);
        RectTransform valRT = valObj.AddComponent<RectTransform>();
        valRT.sizeDelta = new Vector2(ValueWidth, TotalHeight);

        bar.valueLabel = valObj.AddComponent<TextMeshProUGUI>();
        bar.valueLabel.fontSize = 13f;
        bar.valueLabel.alignment = TextAlignmentOptions.Right;
        bar.valueLabel.color = new Color(0.8f, 0.8f, 0.8f);
        bar.valueLabel.text = "0";

        return bar;
    }

    /// <summary>
    /// 设置属性条数据（带平滑动画）
    /// </summary>
    public void SetAttribute(PlayerAttributes.AttributeInfo info)
    {
        // 更新名称与颜色
        if (nameLabel != null) nameLabel.text = info.name;
        if (fillImage != null) fillImage.color = info.barColor;

        // 更新数值文本
        if (valueLabel != null)
        {
            valueLabel.text = info.isPercentage
                ? $"{info.value}%"
                : info.value.ToString();
        }

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
        if (nameLabel != null) nameLabel.text = info.name;
        if (fillImage != null) fillImage.color = info.barColor;

        if (valueLabel != null)
        {
            valueLabel.text = info.isPercentage
                ? $"{info.value}%"
                : info.value.ToString();
        }

        float targetFill = info.NormalizedValue;
        currentFill = targetFill;
        UpdateFillWidth(targetFill);
    }

    // ========== 协程动画（参考 UIAnimator 风格） ==========

    /// <summary>
    /// 平滑过渡进度条填充量
    /// </summary>
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

    /// <summary>
    /// 根据归一化值更新填充条宽度
    /// </summary>
    private void UpdateFillWidth(float normalizedValue)
    {
        if (fillImage != null)
        {
            RectTransform fillRT = fillImage.rectTransform;
            fillRT.sizeDelta = new Vector2(BarWidth * normalizedValue, fillRT.sizeDelta.y);
        }
    }
}
