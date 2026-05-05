using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 统一 UI 过渡工具：淡入淡出、轻微位移与缩放。
/// 适合当前项目这种纯代码动态创建的面板。
/// </summary>
public static class UITransitionUtility
{
    private const float DefaultShowDuration = 0.22f;
    private const float DefaultHideDuration = 0.18f;

    private sealed class TransitionState : MonoBehaviour
    {
        public bool initialized;
        public Vector2 baseAnchoredPosition;
        public Vector3 baseScale = Vector3.one;
        public Coroutine runningCoroutine;
    }

    public static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
    }

    public static void Show(MonoBehaviour host, GameObject target, Vector2 offset, float startScale = 0.985f, float duration = DefaultShowDuration)
    {
        if (host == null || target == null)
        {
            return;
        }

        TransitionState state = EnsureState(target);
        CanvasGroup group = EnsureCanvasGroup(target);
        RectTransform rect = target.GetComponent<RectTransform>();

        StopRunning(state, host);

        target.SetActive(true);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        if (rect != null)
        {
            rect.anchoredPosition = state.baseAnchoredPosition + offset;
            rect.localScale = state.baseScale * startScale;
        }

        state.runningCoroutine = host.StartCoroutine(AnimateRoutine(
            group,
            rect,
            0f,
            1f,
            rect != null ? rect.anchoredPosition : Vector2.zero,
            state.baseAnchoredPosition,
            rect != null ? rect.localScale : Vector3.one,
            state.baseScale,
            duration,
            () =>
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
                group.interactable = true;
                if (rect != null)
                {
                    rect.anchoredPosition = state.baseAnchoredPosition;
                    rect.localScale = state.baseScale;
                }

                state.runningCoroutine = null;
            }));
    }

    public static void Hide(MonoBehaviour host, GameObject target, Vector2 offset, float endScale = 0.985f, float duration = DefaultHideDuration, Action onComplete = null)
    {
        if (host == null || target == null)
        {
            onComplete?.Invoke();
            return;
        }

        TransitionState state = EnsureState(target);
        CanvasGroup group = EnsureCanvasGroup(target);
        RectTransform rect = target.GetComponent<RectTransform>();

        StopRunning(state, host);

        group.blocksRaycasts = false;
        group.interactable = false;
        target.SetActive(true);

        Vector2 startPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
        Vector3 startScaleValue = rect != null ? rect.localScale : Vector3.one;

        state.runningCoroutine = host.StartCoroutine(AnimateRoutine(
            group,
            rect,
            group.alpha,
            0f,
            startPosition,
            state.baseAnchoredPosition + offset,
            startScaleValue,
            state.baseScale * endScale,
            duration,
            () =>
            {
                if (rect != null)
                {
                    rect.anchoredPosition = state.baseAnchoredPosition;
                    rect.localScale = state.baseScale;
                }

                group.alpha = 0f;
                target.SetActive(false);
                state.runningCoroutine = null;
                onComplete?.Invoke();
            }));
    }

    private static TransitionState EnsureState(GameObject target)
    {
        TransitionState state = target.GetComponent<TransitionState>();
        if (state == null)
        {
            state = target.AddComponent<TransitionState>();
        }

        if (!state.initialized)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            state.baseAnchoredPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
            state.baseScale = rect != null ? rect.localScale : Vector3.one;
            state.initialized = true;
        }

        return state;
    }

    private static void StopRunning(TransitionState state, MonoBehaviour host)
    {
        if (state == null || state.runningCoroutine == null || host == null)
        {
            return;
        }

        host.StopCoroutine(state.runningCoroutine);
        state.runningCoroutine = null;
    }

    private static IEnumerator AnimateRoutine(
        CanvasGroup group,
        RectTransform rect,
        float startAlpha,
        float endAlpha,
        Vector2 startPosition,
        Vector2 endPosition,
        Vector3 startScale,
        Vector3 endScale,
        float duration,
        Action onComplete)
    {
        if (duration <= 0f)
        {
            if (group != null)
            {
                group.alpha = endAlpha;
            }

            if (rect != null)
            {
                rect.anchoredPosition = endPosition;
                rect.localScale = endScale;
            }

            onComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = EaseOutCubic(t);

            if (group != null)
            {
                group.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
            }

            if (rect != null)
            {
                rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                rect.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (group != null)
        {
            group.alpha = endAlpha;
        }

        if (rect != null)
        {
            rect.anchoredPosition = endPosition;
            rect.localScale = endScale;
        }

        onComplete?.Invoke();
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
