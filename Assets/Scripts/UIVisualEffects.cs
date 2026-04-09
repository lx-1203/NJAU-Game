using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIVisualEffects : MonoBehaviour
{
    [Header("Particle Effects")]
    [Tooltip("按钮点击粒子效果预制件")]
    public GameObject buttonClickParticles;
    
    [Tooltip("背景粒子效果预制件")]
    public GameObject backgroundParticles;
    
    [Header("Glow Effects")]
    [Tooltip("发光材质")]
    public Material glowMaterial;
    
    [Tooltip("发光强度")]
    public float glowIntensity = 1.5f;
    
    [Header("Floating Effects")]
    [Tooltip("浮动动画速度")]
    public float floatingSpeed = 1f;
    
    [Tooltip("浮动动画幅度")]
    public float floatingAmplitude = 10f;
    
    [Header("Shake Effects")]
    [Tooltip("震动持续时间")]
    public float shakeDuration = 0.3f;
    
    [Tooltip("震动强度")]
    public float shakeIntensity = 5f;
    
    [Header("References")]
    private Dictionary<GameObject, Coroutine> activeEffects = new Dictionary<GameObject, Coroutine>();
    
    #region Particle Effects
    
    public void SpawnButtonClickParticles(Button button)
    {
        if (buttonClickParticles != null)
        {
            Vector3 worldPosition = button.transform.position;
            GameObject particles = Instantiate(buttonClickParticles, worldPosition, Quaternion.identity);
            
            // 设置粒子系统的生命周期
            ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float lifetime = particleSystem.main.duration + particleSystem.main.startLifetime.constant;
                Destroy(particles, lifetime);
            }
            else
            {
                Destroy(particles, 2f); // 默认2秒后销毁
            }
        }
    }
    
    public void SpawnBackgroundParticles()
    {
        if (backgroundParticles != null)
        {
            Instantiate(backgroundParticles, Vector3.zero, Quaternion.identity);
        }
    }
    
    #endregion
    
    #region Glow Effects
    
    public void AddGlowEffect(Graphic graphic)
    {
        if (glowMaterial != null && graphic != null)
        {
            graphic.material = glowMaterial;
            
            // 设置发光强度
            if (glowMaterial.HasProperty("_EmissionColor"))
            {
                Color emissionColor = glowMaterial.GetColor("_EmissionColor");
                glowMaterial.SetColor("_EmissionColor", emissionColor * glowIntensity);
            }
        }
    }
    
    public void RemoveGlowEffect(Graphic graphic)
    {
        if (graphic != null)
        {
            graphic.material = null; // 恢复默认材质
        }
    }
    
    public void ToggleGlowEffect(Graphic graphic, bool enable)
    {
        if (enable)
        {
            AddGlowEffect(graphic);
        }
        else
        {
            RemoveGlowEffect(graphic);
        }
    }
    
    #endregion
    
    #region Floating Effects
    
    public void StartFloatingEffect(Transform target)
    {
        if (activeEffects.ContainsKey(target.gameObject))
        {
            StopCoroutine(activeEffects[target.gameObject]);
        }
        
        activeEffects[target.gameObject] = StartCoroutine(FloatingCoroutine(target));
    }
    
    public void StopFloatingEffect(Transform target)
    {
        if (activeEffects.ContainsKey(target.gameObject))
        {
            StopCoroutine(activeEffects[target.gameObject]);
            activeEffects.Remove(target.gameObject);
            
            // 恢复原始位置
            target.localPosition = new Vector3(target.localPosition.x, 0, target.localPosition.z);
        }
    }
    
    private IEnumerator FloatingCoroutine(Transform target)
    {
        Vector3 originalPosition = target.localPosition;
        
        while (true)
        {
            float yOffset = Mathf.Sin(Time.time * floatingSpeed) * floatingAmplitude;
            target.localPosition = new Vector3(originalPosition.x, originalPosition.y + yOffset, originalPosition.z);
            yield return null;
        }
    }
    
    #endregion
    
    #region Shake Effects
    
    public void ShakeElement(RectTransform element)
    {
        if (activeEffects.ContainsKey(element.gameObject))
        {
            StopCoroutine(activeEffects[element.gameObject]);
        }
        
        activeEffects[element.gameObject] = StartCoroutine(ShakeCoroutine(element));
    }
    
    private IEnumerator ShakeCoroutine(RectTransform element)
    {
        Vector2 originalPosition = element.anchoredPosition;
        float elapsedTime = 0f;
        
        while (elapsedTime< shakeDuration)
        {
            float xOffset = Random.Range(-shakeIntensity, shakeIntensity);
            float yOffset = Random.Range(-shakeIntensity, shakeIntensity);
            
            element.anchoredPosition = originalPosition + new Vector2(xOffset, yOffset);
            elapsedTime += Time.deltaTime;
            
            yield return null;
        }
        
        element.anchoredPosition = originalPosition;
        activeEffects.Remove(element.gameObject);
    }
    
    #endregion
    
    #region Screen Effects
    
    public void FlashScreen(Color flashColor, float duration = 0.2f)
    {
        // 创建全屏闪烁效果
        GameObject flashObject = new GameObject("ScreenFlash");
        flashObject.transform.SetParent(transform);
        
        RectTransform rectTransform = flashObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        
        Image image = flashObject.AddComponent<Image>();
        image.color = flashColor;
        image.raycastTarget = false;
        
        StartCoroutine(FadeOutCoroutine(image, duration, () => Destroy(flashObject)));
    }
    
    private IEnumerator FadeOutCoroutine(Image image, float duration, Action onComplete)
    {
        float elapsedTime = 0f;
        Color startColor = image.color;
        
        while (elapsedTime< duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            image.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        image.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        onComplete?.Invoke();
    }
    
    #endregion
    
    #region Utility Methods
    
    public void StopAllEffects()
    {
        foreach (Coroutine coroutine in activeEffects.Values)
        {
            StopCoroutine(coroutine);
        }
        activeEffects.Clear();
    }
    
    public void StopEffect(GameObject target)
    {
        if (activeEffects.ContainsKey(target))
        {
            StopCoroutine(activeEffects[target]);
            activeEffects.Remove(target);
        }
    }
    
    #endregion
    
    // 内部辅助类
    private delegate void Action();
}