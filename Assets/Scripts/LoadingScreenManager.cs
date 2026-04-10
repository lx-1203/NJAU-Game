using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// 加载界面管理脚本
/// 负责加载流程控制、动画调度和场景跳转
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("加载界面 UI 组件")]
    public LoadingScreenUI loadingScreenUI;

    [Header("布局配置")]
    public UILayoutConfig layoutConfig;

    [Header("动画时长配置")]
    [Tooltip("淡入时长（秒）")]
    public float fadeInDuration = 0.5f;
    [Tooltip("淡出时长（秒）")]
    public float fadeOutDuration = 0.5f;

    [Header("提示切换配置")]
    [Tooltip("提示文案切换最小间隔（秒）")]
    public float tipSwitchMinInterval = 3f;
    [Tooltip("提示文案切换最大间隔（秒）")]
    public float tipSwitchMaxInterval = 5f;

    [Header("最短显示时间")]
    [Tooltip("加载界面最短显示时间（秒），即使加载很快也会等待这么久，确保能看到进度条变化")]
    public float minimumDisplayTime = 5f;

    [Header("自动跳转配置")]
    [Tooltip("加载完成后自动跳转延迟（秒）")]
    public float autoJumpDelay = 2f;

    // 协程引用
    private Coroutine loadingCoroutine;
    private Coroutine tipSwitchCoroutine;

    // 场景加载引用
    private AsyncOperation targetSceneLoadOperation;

    // 加载状态
    private bool isLoading = false;
    private bool canSkip = false;
    private bool hasSkipped = false;
    private string resolvedTargetSceneName;

    // 计时器
    private float loadingStartTime;
    private float targetProgress = 0f;
    private float currentDisplayProgress = 0f;

    // 平滑速度（降低使进度变化更明显可见）
    private const float PROGRESS_SMOOTH_SPEED = 3f;

    #region Unity 生命周期

    private void Awake()
    {
        // 如果没有指定 UI 组件，尝试获取子物体上的
        if (loadingScreenUI == null)
        {
            loadingScreenUI = GetComponentInChildren<LoadingScreenUI>();
        }

        // 如果仍然没有，创建新的 UI 组件
        if (loadingScreenUI == null)
        {
            GameObject uiObject = new GameObject("LoadingScreenUI");
            uiObject.transform.SetParent(transform);
            loadingScreenUI = uiObject.AddComponent<LoadingScreenUI>();
            loadingScreenUI.layoutConfig = layoutConfig;
        }

        // 设置画布配置
        SetupCanvas();
    }

    private void Start()
    {
        // 获取目标场景名称
        string targetScene = SceneLoader.TargetSceneName;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[LoadingScreenManager] 未指定目标场景，尝试加载 TitleScreen...");
            targetScene = "TitleScreen";
        }

        resolvedTargetSceneName = targetScene;

        // 启动加载流程
        StartLoading(targetScene);
    }

    private void Update()
    {
        // 平滑更新进度条显示
        if (isLoading)
        {
            currentDisplayProgress = Mathf.Lerp(currentDisplayProgress, targetProgress, Time.deltaTime * PROGRESS_SMOOTH_SPEED);

            // 接近目标进度时直接赋值，避免永远无法达到 100%
            if (Mathf.Abs(currentDisplayProgress - targetProgress) < 0.001f)
            {
                currentDisplayProgress = targetProgress;
            }

            if (loadingScreenUI != null)
            {
                loadingScreenUI.UpdateProgress(currentDisplayProgress);
            }
        }
    }

    #endregion

    #region 画布配置

    /// <summary>
    /// 设置画布配置
    /// </summary>
    private void SetupCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 确保在最上层

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        Vector2 refResolution = layoutConfig != null
            ? layoutConfig.referenceResolution
            : new Vector2(1920, 1080);

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = refResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    #endregion

    #region 加载流程

    /// <summary>
    /// 开始加载流程
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public void StartLoading(string sceneName)
    {
        if (isLoading) return;

        isLoading = true;
        canSkip = false;
        hasSkipped = false;

        // 记录开始时间
        loadingStartTime = Time.time;

        // 设置进度初始值
        targetProgress = 0f;
        currentDisplayProgress = 0f;

        // 不再做淡入动画，UI 直接可见（避免黑屏）

        // 启动提示文案切换协程
        tipSwitchCoroutine = StartCoroutine(TipSwitchLoop());

        // 启动跳过按钮监听
        SetupSkipButton();

        // 启动场景异步加载
        loadingCoroutine = StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// 异步加载场景
    /// 确保进度条有完整的 0→100% 变化过程
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[LoadingScreenManager] 开始异步加载场景: {sceneName}");

        // 开始异步加载
        targetSceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (targetSceneLoadOperation == null)
        {
            Debug.LogError($"[LoadingScreenManager] 无法创建场景加载任务: {sceneName}");
            yield break;
        }

        // 禁止自动切换场景（等我们手动处理）
        targetSceneLoadOperation.allowSceneActivation = false;

        // 阶段1: 实际加载进度（占总进度的 0~70%）
        // Unity 的 AsyncOperation.progress 最大到 0.9
        while (targetSceneLoadOperation.progress < 0.9f)
        {
            float realProgress = targetSceneLoadOperation.progress / 0.9f; // 归一化 0~1
            targetProgress = realProgress * 0.7f;          // 映射到 0~0.7
            yield return null;
        }

        // 阶段2: 加载已完成，用剩余的最短显示时间做 70%→100% 的平滑过渡
        // 确保玩家能看到进度条的变化过程
        float elapsed = Time.time - loadingStartTime;
        float remainingTime = Mathf.Max(minimumDisplayTime - elapsed, 1f);
        float startTime = Time.time;
        float startProgress = currentDisplayProgress;

        while (Time.time - startTime < remainingTime)
        {
            float t = (Time.time - startTime) / remainingTime;
            // 缓入缓出曲线，让进度条动起来更自然
            float smoothT = t * t * (3f - 2f * t);
            targetProgress = Mathf.Lerp(startProgress, 1f, smoothT);
            yield return null;
        }

        // 确保进度条填满
        targetProgress = 1f;
        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(1f);
        }

        // 短暂停留让玩家看到 100%
        yield return new WaitForSeconds(0.5f);

        // 激活场景并切换
        OnLoadingComplete();
    }

    /// <summary>
    /// 加载完成回调
    /// </summary>
    private void OnLoadingComplete()
    {
        if (hasSkipped) return;

        Debug.Log("[LoadingScreenManager] 加载完成！");

        // 停止提示切换协程
        if (tipSwitchCoroutine != null)
        {
            StopCoroutine(tipSwitchCoroutine);
            tipSwitchCoroutine = null;
        }

        // 激活跳过按钮
        canSkip = true;
        if (loadingScreenUI != null)
        {
            loadingScreenUI.SetSkipButtonInteractable(true);
        }

        // 更新最终进度
        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(1f);
        }

        // 等待自动跳转或玩家跳过
        StartCoroutine(AutoJumpOrSkip());
    }

    /// <summary>
    /// 自动跳转或等待玩家跳过
    /// </summary>
    private IEnumerator AutoJumpOrSkip()
    {
        // 等待自动跳转时间
        yield return new WaitForSeconds(autoJumpDelay);

        // 如果玩家已经跳过，则不再跳转
        if (hasSkipped) yield break;

        Debug.Log("[LoadingScreenManager] 自动跳转目标场景...");
        JumpToTargetScene();
    }

    /// <summary>
    /// 玩家点击跳过按钮
    /// </summary>
    public void OnSkipButtonClicked()
    {
        if (!canSkip || hasSkipped) return;

        hasSkipped = true;
        Debug.Log("[LoadingScreenManager] 玩家跳过，等待跳转...");

        JumpToTargetScene();
    }

    /// <summary>
    /// 跳转到目标场景（淡出过渡）
    /// </summary>
    private void JumpToTargetScene()
    {
        // 停止所有协程
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

        if (tipSwitchCoroutine != null)
        {
            StopCoroutine(tipSwitchCoroutine);
            tipSwitchCoroutine = null;
        }

        // 激活已经加载完成的目标场景
        if (targetSceneLoadOperation != null)
        {
            targetSceneLoadOperation.allowSceneActivation = true;
            targetSceneLoadOperation = null;
            return;
        }

        Debug.LogWarning("[LoadingScreenManager] 未找到可激活的已加载场景任务，回退为直接加载目标场景。");
        SceneManager.LoadScene(resolvedTargetSceneName);
    }

    #endregion

    #region 动画效果

    /// <summary>
    /// 淡入动画
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        CanvasGroup rootGroup = loadingScreenUI != null
            ? loadingScreenUI.GetRootCanvasGroup()
            : GetComponent<CanvasGroup>();

        if (rootGroup == null)
        {
            rootGroup = gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            float t = elapsed / fadeInDuration;
            // 使用平滑的缓入曲线
            float easeT = t * t;
            rootGroup.alpha = easeT;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rootGroup.alpha = 1f;
    }

    /// <summary>
    /// 淡出动画
    /// </summary>
    public IEnumerator FadeOutCoroutine(System.Action onComplete = null)
    {
        CanvasGroup rootGroup = loadingScreenUI != null
            ? loadingScreenUI.GetRootCanvasGroup()
            : GetComponent<CanvasGroup>();

        if (rootGroup == null)
        {
            rootGroup = GetComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            float t = elapsed / fadeOutDuration;
            // 使用平滑的缓出曲线
            float easeT = 1f - (1f - t) * (1f - t);
            rootGroup.alpha = 1f - easeT;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rootGroup.alpha = 0f;
        onComplete?.Invoke();
    }

    #endregion

    #region 提示文案切换

    /// <summary>
    /// 提示文案切换循环
    /// </summary>
    private IEnumerator TipSwitchLoop()
    {
        while (true)
        {
            // 随机等待一段时间
            float waitTime = Random.Range(tipSwitchMinInterval, tipSwitchMaxInterval);
            yield return new WaitForSeconds(waitTime);

            // 切换提示文案
            if (loadingScreenUI != null)
            {
                loadingScreenUI.SwitchTip();
            }
        }
    }

    #endregion

    #region 跳过按钮

    /// <summary>
    /// 设置跳过按钮监听
    /// </summary>
    private void SetupSkipButton()
    {
        if (loadingScreenUI == null) return;

        Button skipBtn = loadingScreenUI.GetSkipButton();
        if (skipBtn != null)
        {
            skipBtn.onClick.RemoveAllListeners();
            skipBtn.onClick.AddListener(OnSkipButtonClicked);
            Debug.Log("[LoadingScreenManager] 跳过按钮已注册点击事件");
        }
    }

    #endregion
}
