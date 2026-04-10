using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HUD 管理器 —— 负责初始化 HUD、绑定数据、刷新显示、处理按钮事件
/// 协程动画风格参考 UIAnimator.cs
/// </summary>
public class HUDManager : MonoBehaviour
{
    // ========== 引用 ==========
    private HUDBuilder builder;
    private GameState gameState;
    private PlayerAttributes playerAttributes;

    // ========== 动画参数 ==========
    private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ========== 属性条缓存 ==========
    private List<AttributeBar> attrBars = new List<AttributeBar>();

    // ========== 初始化 ==========

    private void Start()
    {
        // 获取或创建数据单例
        EnsureDataInstances();

        // 构建 HUD UI
        builder = gameObject.AddComponent<HUDBuilder>();
        builder.BuildHUD();

        // 创建属性条
        CreateAttributeBars();

        // 绑定按钮事件
        BindButtonEvents();

        // 订阅数据变化事件
        if (gameState != null) gameState.OnStateChanged += RefreshTopBar;
        if (playerAttributes != null) playerAttributes.OnAttributesChanged += RefreshAttributes;

        // 首次刷新
        RefreshAll();

        // 播放入场动画
        StartCoroutine(PlayEntryAnimation());
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (gameState != null) gameState.OnStateChanged -= RefreshTopBar;
        if (playerAttributes != null) playerAttributes.OnAttributesChanged -= RefreshAttributes;
    }

    /// <summary>确保数据单例存在</summary>
    private void EnsureDataInstances()
    {
        // GameState
        gameState = GameState.Instance;
        if (gameState == null)
        {
            GameObject gsObj = new GameObject("GameState");
            gameState = gsObj.AddComponent<GameState>();
        }

        // PlayerAttributes
        playerAttributes = PlayerAttributes.Instance;
        if (playerAttributes == null)
        {
            GameObject paObj = new GameObject("PlayerAttributes");
            playerAttributes = paObj.AddComponent<PlayerAttributes>();
        }
    }

    /// <summary>根据属性数量创建属性条</summary>
    private void CreateAttributeBars()
    {
        PlayerAttributes.AttributeInfo[] attrs = playerAttributes.GetAllAttributes();
        attrBars.Clear();

        foreach (var attr in attrs)
        {
            AttributeBar bar = builder.AddAttributeBar();
            if (bar != null)
            {
                bar.SetAttributeImmediate(attr);
                attrBars.Add(bar);
            }
        }
    }

    // ========== 数据刷新 ==========

    /// <summary>刷新所有 HUD 元素</summary>
    public void RefreshAll()
    {
        RefreshTopBar();
        RefreshAttributes();
    }

    /// <summary>刷新顶栏信息</summary>
    public void RefreshTopBar()
    {
        if (builder == null || gameState == null) return;

        // 时间描述
        if (builder.timeText != null)
        {
            builder.timeText.text = $"当前时间：{gameState.GetTimeDescription()}";
        }

        // 金钱
        if (builder.moneyText != null)
        {
            builder.moneyText.text = $"金钱：￥{gameState.Money}";
        }

        // 行动点（实心/空心圆）
        if (builder.actionPointsText != null)
        {
            builder.actionPointsText.text = $"行动点：{BuildActionPointsDisplay()}";
        }
    }

    /// <summary>刷新属性条</summary>
    public void RefreshAttributes()
    {
        if (playerAttributes == null) return;

        PlayerAttributes.AttributeInfo[] attrs = playerAttributes.GetAllAttributes();
        for (int i = 0; i < attrBars.Count && i < attrs.Length; i++)
        {
            attrBars[i].SetAttribute(attrs[i]);
        }
    }

    // ========== 行动点显示 ==========

    /// <summary>用实心●和空心○构建行动点显示字符串</summary>
    private string BuildActionPointsDisplay()
    {
        int current = gameState.ActionPoints;
        int max = GameState.DefaultActionPoints;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < max; i++)
        {
            sb.Append(i < current ? "●" : "○");
        }
        return sb.ToString();
    }

    // ========== 按钮事件 ==========

    private void BindButtonEvents()
    {
        if (builder.btnStudy != null)
            builder.btnStudy.onClick.AddListener(() => OnActionButtonClicked("自习", builder.btnStudy));
        if (builder.btnSocial != null)
            builder.btnSocial.onClick.AddListener(() => OnActionButtonClicked("社交", builder.btnSocial));
        if (builder.btnGoOut != null)
            builder.btnGoOut.onClick.AddListener(() => OnActionButtonClicked("出校门", builder.btnGoOut));
        if (builder.btnSleep != null)
            builder.btnSleep.onClick.AddListener(() => OnActionButtonClicked("睡觉", builder.btnSleep));
    }

    /// <summary>行动按钮被点击</summary>
    private void OnActionButtonClicked(string actionName, Button button)
    {
        // 检查行动点
        if (!gameState.ConsumeActionPoint())
        {
            Debug.Log($"[HUD] 行动点不足，无法执行: {actionName}");
            StartCoroutine(ShakeButtonCoroutine(button));
            return;
        }

        Debug.Log($"[HUD] 执行行动: {actionName}，剩余行动点: {gameState.ActionPoints}");

        // 播放按钮按压动画
        if (builder.hudAnimator != null)
        {
            builder.hudAnimator.ButtonPressEffect(button);
        }

        // 根据行动类型产生效果（示例逻辑）
        switch (actionName)
        {
            case "自习":
                playerAttributes.AddAttribute("学力", 3);
                playerAttributes.AddAttribute("压力", 5);
                break;
            case "社交":
                playerAttributes.AddAttribute("魅力", 2);
                playerAttributes.AddAttribute("领导力", 1);
                playerAttributes.AddAttribute("心情", 8);
                break;
            case "出校门":
                playerAttributes.AddAttribute("心情", 10);
                playerAttributes.AddAttribute("压力", -5);
                gameState.AddMoney(-50);
                break;
            case "睡觉":
                playerAttributes.AddAttribute("体魄", 2);
                playerAttributes.AddAttribute("压力", -15);
                playerAttributes.AddAttribute("心情", 5);
                break;
        }

        // 数据变化事件已自动触发 RefreshTopBar / RefreshAttributes，无需手动刷新
    }

    // ========== 协程动画（参考 UIAnimator 风格） ==========

    /// <summary>入场动画：各区域依次淡入滑入</summary>
    private IEnumerator PlayEntryAnimation()
    {
        if (builder == null || builder.hudCanvas == null) yield break;

        // 收集需要动画的顶层面板
        Transform canvasRoot = builder.hudCanvas.transform;
        List<CanvasGroup> panels = new List<CanvasGroup>();

        foreach (Transform child in canvasRoot)
        {
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            panels.Add(cg);
        }

        // 等待一帧让布局稳定
        yield return null;

        // 依次淡入每个面板
        float delay = 0.08f;
        float duration = 0.35f;

        foreach (CanvasGroup cg in panels)
        {
            StartCoroutine(FadeInCoroutine(cg, duration));
            RectTransform rt = cg.GetComponent<RectTransform>();
            if (rt != null)
            {
                StartCoroutine(SlideInCoroutine(rt, duration));
            }
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>淡入动画</summary>
    private IEnumerator FadeInCoroutine(CanvasGroup cg, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = easingCurve.Evaluate(elapsedTime / duration);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1f;
    }

    /// <summary>从下方滑入动画</summary>
    private IEnumerator SlideInCoroutine(RectTransform rt, float duration)
    {
        Vector2 originalPos = rt.anchoredPosition;
        Vector2 startPos = originalPos + new Vector2(0, -30f);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = easingCurve.Evaluate(elapsedTime / duration);
            rt.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = originalPos;
    }

    /// <summary>按钮抖动动画（行动点不足时的反馈）</summary>
    private IEnumerator ShakeButtonCoroutine(Button button)
    {
        if (button == null) yield break;

        RectTransform rt = button.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 originalPos = rt.anchoredPosition;
        float shakeDuration = 0.3f;
        float shakeIntensity = 8f;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float progress = elapsedTime / shakeDuration;
            // 衰减抖动
            float offset = Mathf.Sin(progress * Mathf.PI * 6f) * shakeIntensity * (1f - progress);
            rt.anchoredPosition = originalPos + new Vector2(offset, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = originalPos;
    }
}
