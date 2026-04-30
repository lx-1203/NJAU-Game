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
    [Header("UI References")]
    [Tooltip("Loading screen UI component.")]
    public LoadingScreenUI loadingScreenUI;

    [Header("Layout")]
    public UILayoutConfig layoutConfig;

    [Header("Fade Durations")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

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
    private float currentDisplayProgress;

    private void Awake()
    {
        UIFlowGuard.RestoreInteractiveState();
        UIFlowGuard.EnsureEventSystem();

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
        }

        resolvedTargetSceneName = targetScene;
        StartLoading(targetScene);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!isLoading || loadingScreenUI == null)
        {
            return;
        }

        currentDisplayProgress = targetProgress;
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
        targetProgress = 0f;
        currentDisplayProgress = 0f;

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
            isLoading = false;
            yield break;
        }

        targetSceneLoadOperation.allowSceneActivation = false;

        while (targetSceneLoadOperation.progress < 0.9f)
        {
            targetProgress = Mathf.Clamp01(targetSceneLoadOperation.progress / 0.9f);
            yield return null;
        }

        targetProgress = 1f;
        if (loadingScreenUI != null)
        {
            loadingScreenUI.UpdateProgress(1f);
        }

        OnLoadingComplete();
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
            elapsed += Time.deltaTime;
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
            elapsed += Time.deltaTime;
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
}
