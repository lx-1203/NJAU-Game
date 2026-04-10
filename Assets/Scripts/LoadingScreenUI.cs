using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 加载界面 UI 构建与更新
/// 改为 Prefab 可视化编辑方式：所有 UI 元素在编辑器中拖拽配置
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI 画布配置")]
    public Canvas canvas;
    public CanvasScaler canvasScaler;
    public GraphicRaycaster graphicRaycaster;

    [Header("布局配置")]
    public UILayoutConfig layoutConfig;

    [Header("中文字体资源 (TMP Font Asset)")]
    public TMP_FontAsset chineseFontAsset;

    [Header("背景图片")]
    public Sprite backgroundSprite;

    [Header("进度条图片 (底部)")]
    public Sprite progressBarBackgroundSprite;
    public Sprite progressBarFillSprite;

    [Header("进度条渐变材质")]
    [Tooltip("使用 UI/ProgressBarGradient Shader 的材质，实现从左到右渐变覆盖效果")]
    public Material progressBarGradientMaterial;

    [Header("进度条配置")]
    public float progressBarWidth = 0f;  // 0 表示使用锚点拉伸（左右铺满）
    public float progressBarHeight = 192f;  // 匹配图片原始高度
    public float progressBarBottomOffset = 0f;  // 贴底
    [Tooltip("渐变边缘柔和度，值越大过渡越柔和")]
    [Range(0f, 0.15f)]
    public float edgeSoftness = 0.05f;

    [Header("加载提示文案池")]
    [Tooltip("加载提示文案列表")]
    public List<string> loadingTips = new List<string>
    {
        "正在翻找课本...",
        "食堂阿姨正在打饭...",
        "图书馆占座中...",
        "正在铺床单...",
        "辅导员正在点名...",
        "快递小哥正在派件..."
    };

    [Header("游戏小贴士文案池")]
    [Tooltip("游戏小贴士列表")]
    public List<string> gameTips = new List<string>
    {
        "合理安排时间，学习娱乐两不误！",
        "多和室友交流，大学生活更精彩！",
        "别忘了按时吃饭，身体是革命的本钱！",
        "适度游戏益脑，沉迷游戏伤身！"
    };

    [Header("--- UI 元素引用（从场景中拖入） ---")]
    [Tooltip("根节点的 CanvasGroup，用于整体淡入淡出")]
    public CanvasGroup rootCanvasGroup;

    [Tooltip("左上角 Logo 文字")]
    public TextMeshProUGUI logoText;

    [Tooltip("右上角版本号文字")]
    public TextMeshProUGUI versionText;

    [Tooltip("加载提示文字")]
    public TextMeshProUGUI tipText;

    [Tooltip("游戏小贴士文字")]
    public TextMeshProUGUI gameTipText;

    [Tooltip("百分比文字")]
    public TextMeshProUGUI percentText;

    [Tooltip("跳过按钮")]
    public Button skipButton;

    [Tooltip("跳过按钮的 CanvasGroup")]
    public CanvasGroup skipButtonCanvasGroup;

    [Tooltip("跳过按钮文字")]
    public TextMeshProUGUI skipButtonText;

    [Tooltip("全屏背景 Image")]
    public Image backgroundImage;

    [Tooltip("进度条背景 Image")]
    public Image progressBarBackground;

    [Tooltip("进度条填充 Image")]
    public Image progressBarFill;

    [Tooltip("加载提示的 RectTransform（用于动画）")]
    public RectTransform tipRect;

    [Header("粒子效果")]
    [Tooltip("进度条粒子特效控制器")]
    public ProgressBarParticleEffect particleEffect;

    // 当前显示的提示索引
    private int currentTipIndex = 0;
    private int currentGameTipIndex = 0;

    // 渐变材质实例（运行时创建，避免修改共享材质）
    private Material gradientMaterialInstance;

    #region Unity 生命周期

    private void Awake()
    {
        // 确保存在 EventSystem
        EnsureEventSystem();

        // 初始化 UI 状态
        InitializeUI();
    }

    private void OnDestroy()
    {
        // 清理材质实例，防止内存泄漏
        if (gradientMaterialInstance != null)
        {
            Destroy(gradientMaterialInstance);
            gradientMaterialInstance = null;
        }
    }

    #endregion

    #region 公开接口

    /// <summary>
    /// 更新进度条显示（通过材质渐变覆盖）
    /// </summary>
    /// <param name="progress">进度值 0~1</param>
    public void UpdateProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);

        // 通过材质 _Progress 属性控制渐变覆盖
        if (progressBarFill != null && gradientMaterialInstance != null)
        {
            gradientMaterialInstance.SetFloat("_Progress", clampedProgress);
        }

        if (percentText != null)
        {
            int percent = Mathf.RoundToInt(clampedProgress * 100);
            percentText.text = $"{percent}%";
        }

        // 同步驱动粒子效果
        if (particleEffect != null)
        {
            particleEffect.SetProgress(clampedProgress);
        }
    }

    /// <summary>
    /// 获取根 CanvasGroup
    /// </summary>
    public CanvasGroup GetRootCanvasGroup()
    {
        return rootCanvasGroup;
    }

    /// <summary>
    /// 随机切换趣味提示文案（带淡入淡出动画）
    /// </summary>
    public void SwitchTip()
    {
        if (tipRect == null) return;
        StartCoroutine(SwitchTipCoroutine());
    }

    /// <summary>
    /// 获取跳过按钮的 CanvasGroup
    /// </summary>
    public CanvasGroup GetSkipButtonCanvasGroup()
    {
        return skipButtonCanvasGroup;
    }

    /// <summary>
    /// 设置跳过按钮是否可交互
    /// </summary>
    public void SetSkipButtonInteractable(bool interactable)
    {
        if (skipButton != null)
        {
            skipButton.interactable = interactable;
        }
        if (skipButtonCanvasGroup != null)
        {
            skipButtonCanvasGroup.alpha = interactable ? 1f : 0.4f;
        }
    }

    /// <summary>
    /// 获取跳过按钮组件
    /// </summary>
    public Button GetSkipButton()
    {
        return skipButton;
    }

    #endregion

    #region UI 初始化

    /// <summary>
    /// 初始化 UI 状态（不再动态创建，只设置初始值）
    /// </summary>
    private void InitializeUI()
    {
        // 设置背景图片
        if (backgroundImage != null && backgroundSprite != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }

        // 设置进度条背景图片
        if (progressBarBackground != null && progressBarBackgroundSprite != null)
        {
            progressBarBackground.sprite = progressBarBackgroundSprite;
            progressBarBackground.type = Image.Type.Simple;
        }

        // 设置进度条填充图片（使用渐变材质覆盖方式）
        if (progressBarFill != null)
        {
            // 不设置 Image.sprite，纹理完全通过材质的 _MainTex 传递给 Shader
            // 这样避免 Image 组件的 PerRendererData 覆盖 Shader 的纹理设置
            progressBarFill.type = Image.Type.Simple;
            progressBarFill.preserveAspect = false;

            // 创建渐变材质实例
            if (progressBarGradientMaterial != null)
            {
                gradientMaterialInstance = new Material(progressBarGradientMaterial);
                gradientMaterialInstance.SetFloat("_Progress", 0f);
                gradientMaterialInstance.SetFloat("_EdgeSoftness", edgeSoftness);

                // 显式设置填充纹理到材质上
                if (progressBarFillSprite != null)
                {
                    gradientMaterialInstance.SetTexture("_MainTex", progressBarFillSprite.texture);
                }

                progressBarFill.material = gradientMaterialInstance;
            }
        }

        // 设置字体
        if (chineseFontAsset != null)
        {
            if (logoText != null) logoText.font = chineseFontAsset;
            if (tipText != null) tipText.font = chineseFontAsset;
            if (gameTipText != null) gameTipText.font = chineseFontAsset;
            if (percentText != null) percentText.font = chineseFontAsset;
            if (skipButtonText != null) skipButtonText.font = chineseFontAsset;
        }

        // 初始设置进度条
        UpdateProgress(0f);

        // 初始设置跳过按钮为不可交互
        SetSkipButtonInteractable(false);

        // 立即显示 UI（不再初始透明，避免黑屏）
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
        }

        // 显示初始提示文案
        ShowInitialTips();
    }

    /// <summary>
    /// 确保存在 EventSystem
    /// </summary>
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    #endregion

    #region 提示文案切换

    /// <summary>
    /// 显示初始提示文案
    /// </summary>
    private void ShowInitialTips()
    {
        if (loadingTips.Count > 0)
        {
            currentTipIndex = Random.Range(0, loadingTips.Count);
            if (tipText != null)
            {
                tipText.text = loadingTips[currentTipIndex];
            }
        }

        if (gameTips.Count > 0)
        {
            currentGameTipIndex = Random.Range(0, gameTips.Count);
            if (gameTipText != null)
            {
                gameTipText.text = $"小贴士：{gameTips[currentGameTipIndex]}";
            }
        }
    }

    /// <summary>
    /// 切换趣味提示文案（协程：淡出 → 切换 → 淡入）
    /// </summary>
    private System.Collections.IEnumerator SwitchTipCoroutine()
    {
        if (tipText == null) yield break;

        // 淡出
        float elapsed = 0f;
        float fadeOutDuration = 0.3f;
        Color originalColor = tipText.color;

        while (elapsed < fadeOutDuration)
        {
            float t = elapsed / fadeOutDuration;
            tipText.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(1f, 0f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 切换文案
        currentTipIndex = (currentTipIndex + 1) % loadingTips.Count;
        tipText.text = loadingTips[currentTipIndex];

        // 淡入
        elapsed = 0f;
        float fadeInDuration = 0.3f;

        while (elapsed < fadeInDuration)
        {
            float t = elapsed / fadeInDuration;
            tipText.color = new Color(originalColor.r, originalColor.g, originalColor.b, Mathf.Lerp(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        tipText.color = originalColor;
    }

    #endregion
}
