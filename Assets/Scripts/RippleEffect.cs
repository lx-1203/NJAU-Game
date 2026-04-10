using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 涟漪扩散特效组件
/// 在点击位置生成一个圆形涟漪，向外扩散后自动销毁
/// </summary>
public class RippleEffect : MonoBehaviour
{
    [Header("涟漪参数")]
    [Tooltip("扩散最终大小")]
    public float maxScale = 30f;

    [Tooltip("扩散持续时间")]
    public float duration = 0.8f;

    [Tooltip("涟漪颜色")]
    public Color rippleColor = new Color(1f, 1f, 1f, 0.4f);

    [Tooltip("涟漪边缘柔和度 (0=硬边, 1=全模糊)")]
    public float edgeSoftness = 0.3f;

    private static Sprite cachedCircleSprite;

    private RectTransform rectTransform;
    private Image image;
    private CanvasGroup canvasGroup;

    /// <summary>
    /// 在指定 Canvas 下、指定屏幕位置创建涟漪
    /// </summary>
    /// <param name="parent">父级 RectTransform（通常是全屏 Canvas）</param>
    /// <param name="screenPosition">屏幕空间点击坐标</param>
    /// <param name="camera">渲染该 Canvas 的摄像机（Overlay 模式传 null）</param>
    /// <param name="onComplete">扩散完成后的回调</param>
    public static RippleEffect Create(RectTransform parent, Vector2 screenPosition, Camera camera, System.Action onComplete = null)
    {
        // 创建涟漪 GameObject
        GameObject rippleGO = new GameObject("Ripple");
        rippleGO.transform.SetParent(parent, false);

        // 添加组件
        RippleEffect ripple = rippleGO.AddComponent<RippleEffect>();
        ripple.Setup(parent, screenPosition, camera, onComplete);

        return ripple;
    }

    private void Setup(RectTransform parent, Vector2 screenPosition, Camera camera, System.Action onComplete)
    {
        // 添加 RectTransform
        rectTransform = gameObject.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        // 设置锚点为中心
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 将屏幕坐标转换为 Canvas 内的局部坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, camera, out localPoint);
        rectTransform.anchoredPosition = localPoint;

        // 初始大小为一个小圆
        float baseSize = 50f;
        rectTransform.sizeDelta = new Vector2(baseSize, baseSize);
        rectTransform.localScale = Vector3.zero;

        // 添加 Image 组件（圆形）
        image = gameObject.AddComponent<Image>();
        image.color = rippleColor;
        image.raycastTarget = false;

        // 使用缓存圆形 Sprite，避免点击时重复生成贴图
        image.sprite = GetOrCreateCircleSprite();
        image.type = Image.Type.Simple;

        // 添加 CanvasGroup 用于透明度控制
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 确保涟漪显示在最上层
        transform.SetAsLastSibling();

        // 开始扩散动画
        StartCoroutine(ExpandCoroutine(onComplete));
    }

    /// <summary>
    /// 扩散动画协程
    /// </summary>
    private IEnumerator ExpandCoroutine(System.Action onComplete)
    {
        float elapsed = 0f;
        AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        // 透明度：前 60% 保持，后 40% 淡出
        AnimationCurve alphaCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.6f, 0.8f),
            new Keyframe(1f, 0f)
        );

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scaleValue = scaleCurve.Evaluate(t) * maxScale;
            rectTransform.localScale = new Vector3(scaleValue, scaleValue, 1f);
            canvasGroup.alpha = alphaCurve.Evaluate(t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终状态
        rectTransform.localScale = new Vector3(maxScale, maxScale, 1f);
        canvasGroup.alpha = 0f;

        // 回调
        onComplete?.Invoke();

        // 销毁自身
        Destroy(gameObject);
    }

    /// <summary>
    /// 获取或创建缓存的圆形 Sprite
    /// </summary>
    private Sprite GetOrCreateCircleSprite()
    {
        if (cachedCircleSprite == null)
        {
            cachedCircleSprite = CreateCircleSprite();
        }

        return cachedCircleSprite;
    }

    /// <summary>
    /// 运行时生成一个圆形 Sprite（白色填充圆）
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int resolution = 128;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        float center = resolution / 2f;
        float radius = resolution / 2f;
        // 边缘柔和范围（像素）
        float softPixels = radius * edgeSoftness;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < radius - softPixels)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else if (dist < radius)
                {
                    // 边缘渐变
                    float alpha = 1f - (dist - (radius - softPixels)) / softPixels;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }
}
