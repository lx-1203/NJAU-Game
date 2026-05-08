using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the loading screen and transitions into the target scene.
/// Progress is driven by the real AsyncOperation progress reported by Unity.
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    private const float SceneReadyGateProgress = 0.97f;
    private const float PreActivationMaxProgress = 0.995f;
    private const float SimulationStep = 1f / 60f;
    private const float MaxFrameDelta = 1f / 15f;
    private const int MaxSimulationStepsPerFrame = 4;

    [Header("UI References")]
    [Tooltip("Loading screen UI component.")]
    public LoadingScreenUI loadingScreenUI;

    [Header("Layout")]
    public UILayoutConfig layoutConfig;

    [Header("Fade Durations")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Progress Timing")]
    [Tooltip("Loading screen stays visible for at least this duration, even when the target scene loads very quickly.")]
    public float minimumLoadingScreenDuration = 0.75f;
    [Tooltip("基础插值时间，数值越小越灵敏。实际显示会按前中后段与真实速率自适应调整。")]
    public float progressSmoothTime = 0.16f;
    [Tooltip("前段加载的平缓系数，值越大越柔和。")]
    public float earlyPhaseSmoothMultiplier = 1.2f;
    [Tooltip("中段加载的跟随系数，值越小越贴近真实速率波动。")]
    public float midPhaseSmoothMultiplier = 0.78f;
    [Tooltip("收尾阶段的缓冲系数，值越大越柔顺。")]
    public float latePhaseSmoothMultiplier = 1.45f;
    [Tooltip("How long the final 95% -> 100% stretch should take once the target scene is ready.")]
    public float finalProgressDuration = 0.28f;
    [Tooltip("Short pause after reaching 100% so the transition does not feel abrupt.")]
    public float completionHoldDuration = 0.08f;

    [Header("Tip Rotation")]
    public float tipSwitchMinInterval = 3f;
    public float tipSwitchMaxInterval = 5f;

    private Coroutine loadingCoroutine;
    private Coroutine tipSwitchCoroutine;
    private AsyncOperation targetSceneLoadOperation;

    private bool isLoading;
    private bool waitingForTargetSceneActivation;
    private string resolvedTargetSceneName;
    private float targetProgress;
    private float rawProgress;
    private float currentDisplayProgress;
    private float displayProgressVelocity;
    private float sourceProgressVelocity;
    private float loadingStartTime;
    private float simulationAccumulator;
    private bool hasExternalProgress;

    private void Awake()
    {
        UIFlowGuard.RestoreInteractiveState();
        UIFlowGuard.EnsureEventSystem();
        LoadingProgressBridge.Reset();

        if (loadingScreenUI == null)
        {
            loadingScreenUI = GetComponentInChildren<LoadingScreenUI>();
        }

        if (loadingScreenUI == null)
        {
            GameObject uiObject = new GameObject("LoadingScreenUI");
            uiObject.transform.SetParent(transform, false);
            loadingScreenUI = uiObject.AddComponent<LoadingScreenUI>();
            loadingScreenUI.layoutConfig = layoutConfig;
        }

        SetupCanvas();
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        string targetScene = SceneLoader.TargetSceneName;
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[LoadingScreenManager] No target scene specified. Falling back to TitleScreen.");
            targetScene = "TitleScreen";
            if (loadingScreenUI != null)
            {
                loadingScreenUI.SetStatusMessage("正在返回标题界面...", "这次没有收到有效目标场景，系统已自动回退到标题界面。");
            }
        }

        resolvedTargetSceneName = targetScene;
        StartLoading(targetScene);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LoadingProgressBridge.Reset();
    }

    private void Update()
    {
        if (!isLoading || loadingScreenUI == null)
        {
            return;
        }

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
        simulationAccumulator += deltaTime;

        int simulatedSteps = 0;
        while (simulationAccumulator >= SimulationStep && simulatedSteps < MaxSimulationStepsPerFrame)
        {
            SimulateDisplayProgress(SimulationStep);
            simulationAccumulator -= SimulationStep;
            simulatedSteps++;
        }

        if (simulatedSteps == 0)
        {
            SimulateDisplayProgress(Mathf.Max(0.0001f, deltaTime));
        }

        simulationAccumulator = Mathf.Min(simulationAccumulator, SimulationStep);

        loadingScreenUI.UpdateProgress(currentDisplayProgress);
    }

    private void SetupCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        Vector2 refResolution = layoutConfig != null
            ? layoutConfig.referenceResolution
            : new Vector2(1920f, 1080f);

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

    public void StartLoading(string sceneName)
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        hasExternalProgress = false;
        targetProgress = 0f;
        rawProgress = 0f;
        currentDisplayProgress = 0f;
        displayProgressVelocity = 0f;
        sourceProgressVelocity = 0f;
        loadingStartTime = Time.unscaledTime;
        simulationAccumulator = 0f;

        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(0f);
            loadingScreenUI.HideSkipButton();
        }

        tipSwitchCoroutine = StartCoroutine(TipSwitchLoop());
        loadingCoroutine = StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[LoadingScreenManager] Start async load: {sceneName}");

        targetSceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (targetSceneLoadOperation == null)
        {
            Debug.LogError($"[LoadingScreenManager] Failed to create async load operation for scene: {sceneName}");
            if (loadingScreenUI != null)
            {
                loadingScreenUI.SetStatusMessage("加载启动失败", "目标场景暂时无法预加载，系统将尝试直接进入。");
            }
            resolvedTargetSceneName = sceneName;
            JumpToTargetScene();
            isLoading = false;
            yield break;
        }

        targetSceneLoadOperation.allowSceneActivation = false;

        while (targetSceneLoadOperation.progress < 0.9f)
        {
            targetProgress = GetCombinedProgressBeforeActivation();
            yield return null;
        }

        targetProgress = Mathf.Max(targetProgress, GetCombinedProgressBeforeActivation());

        float minimumVisibleUntil = loadingStartTime + Mathf.Max(0f, minimumLoadingScreenDuration);
        while (Time.unscaledTime < minimumVisibleUntil || currentDisplayProgress < SceneReadyGateProgress - 0.02f)
        {
            targetProgress = Mathf.Max(targetProgress, GetCombinedProgressBeforeActivation());
            yield return null;
        }

        yield return FinalizeDisplayedProgress();

        OnLoadingComplete();
    }

    private IEnumerator FinalizeDisplayedProgress()
    {
        float startProgress = Mathf.Max(currentDisplayProgress, targetProgress);
        float duration = Mathf.Max(0.01f, finalProgressDuration);
        float elapsed = 0f;

        displayProgressVelocity = 0f;
        sourceProgressVelocity = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);
            targetProgress = Mathf.Lerp(startProgress, PreActivationMaxProgress, eased);
            yield return null;
        }

        targetProgress = PreActivationMaxProgress;

        while (currentDisplayProgress < PreActivationMaxProgress - 0.002f)
        {
            yield return null;
        }

        currentDisplayProgress = PreActivationMaxProgress;
        displayProgressVelocity = 0f;

        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(PreActivationMaxProgress);
        }
    }

    private void OnLoadingComplete()
    {
        Debug.Log("[LoadingScreenManager] Loading complete. Activating target scene.");

        if (tipSwitchCoroutine != null)
        {
            StopCoroutine(tipSwitchCoroutine);
            tipSwitchCoroutine = null;
        }

        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(1f);
        }

        JumpToTargetScene();
    }

    private void JumpToTargetScene()
    {
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

        isLoading = false;

        if (targetSceneLoadOperation != null)
        {
            waitingForTargetSceneActivation = true;
            targetSceneLoadOperation.allowSceneActivation = true;
            targetSceneLoadOperation = null;
            return;
        }

        Debug.LogWarning("[LoadingScreenManager] No prepared async operation found. Falling back to direct scene load.");
        if (loadingScreenUI != null)
        {
            loadingScreenUI.SetStatusMessage("正在直接进入场景...", "预加载状态已丢失，系统改为直接加载。");
        }
        waitingForTargetSceneActivation = true;
        SceneManager.LoadScene(resolvedTargetSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!waitingForTargetSceneActivation)
        {
            return;
        }

        if (!string.IsNullOrEmpty(resolvedTargetSceneName) && scene.name != resolvedTargetSceneName)
        {
            return;
        }

        waitingForTargetSceneActivation = false;
        StartCoroutine(CleanupAfterSceneActivation());
    }

    private IEnumerator CleanupAfterSceneActivation()
    {
        targetProgress = 1f;
        rawProgress = 1f;
        currentDisplayProgress = 1f;
        displayProgressVelocity = 0f;
        sourceProgressVelocity = 0f;

        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(1f);
        }

        if (completionHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(completionHoldDuration);
        }

        yield return null;

        CanvasGroup rootGroup = loadingScreenUI != null
            ? loadingScreenUI.GetRootCanvasGroup()
            : GetComponent<CanvasGroup>();

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        Destroy(gameObject);
    }

    public IEnumerator FadeInCoroutine()
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
            float t = elapsed / Mathf.Max(0.0001f, fadeInDuration);
            rootGroup.alpha = t * t;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        rootGroup.alpha = 1f;
    }

    public IEnumerator FadeOutCoroutine(Action onComplete = null)
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
            float t = elapsed / Mathf.Max(0.0001f, fadeOutDuration);
            float easeT = 1f - (1f - t) * (1f - t);
            rootGroup.alpha = 1f - easeT;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
        }

        onComplete?.Invoke();
    }

    private IEnumerator TipSwitchLoop()
    {
        while (true)
        {
            float waitTime = UnityEngine.Random.Range(tipSwitchMinInterval, tipSwitchMaxInterval);
            yield return new WaitForSeconds(waitTime);

            if (loadingScreenUI != null)
            {
                loadingScreenUI.SwitchTip();
            }
        }
    }

    private void SimulateDisplayProgress(float deltaTime)
    {
        float nextRawProgress = Mathf.Max(rawProgress, targetProgress);
        float rawVelocity = Mathf.Max(0f, nextRawProgress - rawProgress) / Mathf.Max(0.0001f, deltaTime);

        rawProgress = nextRawProgress;
        sourceProgressVelocity = Mathf.Lerp(sourceProgressVelocity, rawVelocity, 1f - Mathf.Exp(-deltaTime * 10f));

        float lag = Mathf.Max(0f, rawProgress - currentDisplayProgress);
        float adaptiveSmoothTime = EvaluateAdaptiveSmoothTime(rawProgress, lag, sourceProgressVelocity);
        float adaptiveMaxSpeed = EvaluateAdaptiveMaxSpeed(lag, sourceProgressVelocity);

        currentDisplayProgress = Mathf.SmoothDamp(
            currentDisplayProgress,
            rawProgress,
            ref displayProgressVelocity,
            adaptiveSmoothTime,
            adaptiveMaxSpeed,
            deltaTime);

        if (rawProgress >= 0.999f && currentDisplayProgress >= 0.9975f)
        {
            currentDisplayProgress = 1f;
            displayProgressVelocity = 0f;
            return;
        }

        if (rawProgress >= PreActivationMaxProgress && currentDisplayProgress >= PreActivationMaxProgress - 0.001f)
        {
            currentDisplayProgress = PreActivationMaxProgress;
            displayProgressVelocity = 0f;
        }
    }

    private float EvaluateAdaptiveSmoothTime(float normalizedProgress, float lag, float progressVelocity)
    {
        float phaseMultiplier = normalizedProgress < 0.3f
            ? earlyPhaseSmoothMultiplier
            : normalizedProgress < 0.82f
                ? midPhaseSmoothMultiplier
                : latePhaseSmoothMultiplier;

        float lagBoost = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(lag * 5.5f));
        float velocityBoost = Mathf.Lerp(1f, 0.68f, Mathf.Clamp01(progressVelocity * 2.2f));
        float smoothTime = progressSmoothTime * phaseMultiplier * lagBoost * velocityBoost;
        return Mathf.Clamp(smoothTime, 0.035f, 0.42f);
    }

    private float EvaluateAdaptiveMaxSpeed(float lag, float progressVelocity)
    {
        float lagFactor = Mathf.Lerp(0.65f, 2.8f, Mathf.Clamp01(lag * 7f));
        float velocityFactor = Mathf.Lerp(0.8f, 2.1f, Mathf.Clamp01(progressVelocity * 2.5f));
        return Mathf.Max(0.5f, lagFactor * velocityFactor);
    }

    private float GetCombinedProgressBeforeActivation()
    {
        float sceneProgress = targetSceneLoadOperation != null
            ? Mathf.Clamp01(targetSceneLoadOperation.progress / 0.9f) * SceneReadyGateProgress
            : 0f;

        float combinedProgress = sceneProgress;
        if (LoadingProgressBridge.TryGetSnapshot(out LoadingProgressBridge.Snapshot externalProgress))
        {
            hasExternalProgress = externalProgress.isActive;
            if (externalProgress.isActive)
            {
                float externalNormalized = Mathf.Clamp01(externalProgress.normalizedProgress);
                combinedProgress = Mathf.Min(sceneProgress, externalNormalized * SceneReadyGateProgress);

                string detailLabel = externalProgress.detailLabel;
                if (string.IsNullOrEmpty(detailLabel) && externalProgress.totalBytes > 0L)
                {
                    detailLabel = $"已加载 {FormatBytes(externalProgress.loadedBytes)} / {FormatBytes(externalProgress.totalBytes)}";
                }

                if (!string.IsNullOrEmpty(externalProgress.statusLabel) || !string.IsNullOrEmpty(detailLabel))
                {
                    loadingScreenUI.SetStatusMessage(externalProgress.statusLabel, detailLabel);
                }
            }
        }

        if (hasExternalProgress)
        {
            combinedProgress = Mathf.Max(0f, combinedProgress);
        }

        return Mathf.Clamp(combinedProgress, 0f, SceneReadyGateProgress);
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - Mathf.Clamp01(t);
        return 1f - inv * inv * inv;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0L)
        {
            return "0 B";
        }

        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024d && unitIndex < units.Length - 1)
        {
            size /= 1024d;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
