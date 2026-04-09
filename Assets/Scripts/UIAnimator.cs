using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Default animation duration")]
    public float defaultDuration = 0.3f;
    
    [Tooltip("Default delay between animations")]
    public float defaultDelay = 0.1f;
    
    [Tooltip("Easing curve for animations")]
    public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Canvas Groups")]
    [Tooltip("Canvas group for fade animations")]
    public CanvasGroup mainCanvasGroup;
    
    private Dictionary<GameObject, Coroutine> runningAnimations = new Dictionary<GameObject, Coroutine>();
    
    private void Awake()
    {
        if (mainCanvasGroup == null)
        {
            mainCanvasGroup = GetComponent<CanvasGroup>();
        }
    }
    
    #region Fade Animations
    
    public void FadeIn(float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(FadeCoroutine(mainCanvasGroup, 0f, 1f, duration));
    }
    
    public void FadeOut(float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(FadeCoroutine(mainCanvasGroup, 1f, 0f, duration));
    }
    
    public void FadeElement(CanvasGroup canvasGroup, float targetAlpha, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(FadeCoroutine(canvasGroup, canvasGroup.alpha, targetAlpha, duration));
    }
    
    private IEnumerator FadeCoroutine(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime< duration)
        {
            float t = easingCurve.Evaluate(elapsedTime / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
    }
    
    #endregion
    
    #region Position Animations
    
    public void MoveIn(RectTransform element, Vector2 startPosition, Vector2 endPosition, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(MoveCoroutine(element, startPosition, endPosition, duration));
    }
    
    public void MoveOut(RectTransform element, Vector2 endPosition, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(MoveCoroutine(element, element.anchoredPosition, endPosition, duration));
    }
    
    private IEnumerator MoveCoroutine(RectTransform element, Vector2 startPos, Vector2 endPos, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime< duration)
        {
            float t = easingCurve.Evaluate(elapsedTime / duration);
            element.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        element.anchoredPosition = endPos;
    }
    
    #endregion
    
    #region Scale Animations
    
    public void ScaleIn(RectTransform element, Vector3 startScale, Vector3 endScale, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(ScaleCoroutine(element, startScale, endScale, duration));
    }
    
    public void ScaleOut(RectTransform element, Vector3 endScale, float duration = -1f)
    {
        if (duration < 0) duration = defaultDuration;
        StartCoroutine(ScaleCoroutine(element, element.localScale, endScale, duration));
    }
    
    private IEnumerator ScaleCoroutine(RectTransform element, Vector3 startScale, Vector3 endScale, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime< duration)
        {
            float t = easingCurve.Evaluate(elapsedTime / duration);
            element.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        element.localScale = endScale;
    }
    
    #endregion
    
    #region Sequence Animations
    
    public void AnimateMenuElements(List<RectTransform> elements, float delayBetween = -1f)
    {
        if (delayBetween < 0) delayBetween = defaultDelay;
        StartCoroutine(AnimateSequenceCoroutine(elements, delayBetween));
    }
    
    private IEnumerator AnimateSequenceCoroutine(List<RectTransform> elements, float delayBetween)
    {
        foreach (RectTransform element in elements)
        {
            if (element != null)
            {
                // 从下方滑入
                Vector2 startPos = element.anchoredPosition + new Vector2(0, -200);
                element.anchoredPosition = startPos;
                StartCoroutine(MoveCoroutine(element, startPos, element.anchoredPosition, defaultDuration));
            }
            yield return new WaitForSeconds(delayBetween);
        }
    }
    
    #endregion
    
    #region Button Animations
    
    public void ButtonPressEffect(Button button, float scaleAmount = 0.95f, float duration = 0.1f)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // 取消之前的动画
        if (runningAnimations.ContainsKey(button.gameObject))
        {
            StopCoroutine(runningAnimations[button.gameObject]);
        }
        
        runningAnimations[button.gameObject] = StartCoroutine(ButtonPressCoroutine(rectTransform, scaleAmount, duration));
    }
    
    private IEnumerator ButtonPressCoroutine(RectTransform rectTransform, float scaleAmount, float duration)
    {
        Vector3 originalScale = rectTransform.localScale;
        Vector3 pressedScale = originalScale * scaleAmount;
        
        // 按下动画
        yield return ScaleCoroutine(rectTransform, originalScale, pressedScale, duration);
        
        // 释放动画
        yield return ScaleCoroutine(rectTransform, pressedScale, originalScale, duration);
        
        // 移除运行中的动画记录
        runningAnimations.Remove(rectTransform.gameObject);
    }
    
    #endregion
    
    #region Utility Methods
    
    public void StopAllAnimations()
    {
        foreach (Coroutine coroutine in runningAnimations.Values)
        {
            StopCoroutine(coroutine);
        }
        runningAnimations.Clear();
    }
    
    public void StopAnimation(GameObject target)
    {
        if (runningAnimations.ContainsKey(target))
        {
            StopCoroutine(runningAnimations[target]);
            runningAnimations.Remove(target);
        }
    }
    
    #endregion
}