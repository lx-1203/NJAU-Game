using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// 水涟漪特效 — 带背景扭曲
///
/// 原理：
///   GrabPass 在 ScreenSpaceOverlay Canvas 上抓不到背景，
///   改为直接把视频 RenderTexture 传给 Shader 做 UV 扭曲采样。
///   其余区域完全透明，实现"只在波前扭曲背景"的效果。
/// </summary>
public class RippleEffect : MonoBehaviour
{
    [Header("涟漪参数")]
    [Tooltip("持续时间（秒）")]
    public float duration = 2.0f;

    [Tooltip("圈数")]
    public int ringCount = 3;

    [Tooltip("每圈延迟（秒）")]
    public float ringDelay = 0.28f;

    [Tooltip("最大扩散半径（归一化，0.65 ≈ 半屏）")]
    public float maxRadius = 0.7f;

    [Tooltip("扭曲强度（0.02~0.06）")]
    public float distortionStrength = 0.22f;

    [Tooltip("圆环宽度（归一化，0.1~0.3）")]
    public float ringWidth = 0.26f;

    [Tooltip("高亮圆环颜色")]
    public Color ringColor = new Color(0.95f, 0.99f, 1f, 0.03f);

    // ── 静态缓存 ──
    private static Sprite s_ringSprite;

    // ── 公共工厂方法（签名与旧版兼容）──
    public static RippleEffect Create(RectTransform parent, Vector2 screenPosition,
                                      Camera camera, System.Action onComplete = null)
    {
        var go = new GameObject("RippleEffect");
        go.transform.SetParent(parent, false);
        var fx = go.AddComponent<RippleEffect>();
        fx.StartCoroutine(fx.Run(parent, screenPosition, camera, onComplete));
        return fx;
    }

    // ── 主协程 ──
    private IEnumerator Run(RectTransform parent, Vector2 screenPos,
                            Camera camera, System.Action onComplete)
    {
        // 屏幕坐标 → 归一化 [0,1]
        Vector2 normCenter = new Vector2(screenPos.x / Screen.width,
                                         screenPos.y / Screen.height);

        // 找视频的 RenderTexture（用来做扭曲采样的背景）
        Texture bgTex = FindVideoTexture();

        for (int i = 0; i < ringCount; i++)
        {
            float myMaxR    = maxRadius * (1f + i * 0.06f);
            float peakAlpha = Mathf.Max(0.08f, 1f - i * 0.25f);
            StartCoroutine(RunSingleRing(parent, normCenter, i * ringDelay,
                                         myMaxR, peakAlpha, bgTex));
        }

        yield return new WaitForSeconds(duration + ringDelay * (ringCount - 1) + 0.1f);
        onComplete?.Invoke();
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    // ── 找视频 RenderTexture ──
    private Texture FindVideoTexture()
    {
        // 优先找 VideoPlayer 的 targetTexture
        var vp = FindObjectOfType<VideoPlayer>();
        if (vp != null && vp.targetTexture != null)
            return vp.targetTexture;

        // 其次找场景里的 RawImage（通常是视频背景层）
        var rawImages = FindObjectsOfType<RawImage>();
        foreach (var ri in rawImages)
        {
            if (ri.texture != null && ri.gameObject.name.ToLower().Contains("video"))
                return ri.texture;
        }
        // 兜底：任意 RawImage 的纹理
        foreach (var ri in rawImages)
        {
            if (ri.texture != null) return ri.texture;
        }
        return null;
    }

    // ── 单圈协程 ──
    private IEnumerator RunSingleRing(RectTransform parent, Vector2 normCenter,
                                       float startDelay, float myMaxRadius,
                                       float peakAlpha, Texture bgTex)
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        var shader = Shader.Find("UI/WaterRipple");
        if (shader == null)
        {
            Debug.LogError("[RippleEffect] Shader UI/WaterRipple not found!");
            yield break;
        }

        // —— 扭曲层（全屏 RawImage + WaterRipple Shader）——
        var distortGO = new GameObject("RippleDistort");
        distortGO.transform.SetParent(parent, false);
        distortGO.transform.SetAsLastSibling();

        var distortRT = distortGO.AddComponent<RectTransform>();
        distortRT.anchorMin = Vector2.zero;
        distortRT.anchorMax = Vector2.one;
        distortRT.offsetMin = Vector2.zero;
        distortRT.offsetMax = Vector2.zero;

        var rawImg = distortGO.AddComponent<RawImage>();
        rawImg.raycastTarget = false;
        rawImg.color = Color.white;

        // 创建独立 Material 实例
        var mat = new Material(shader);
        if (bgTex != null)
            mat.SetTexture("_MainTex", bgTex);
        mat.SetVector("_RippleCenter", new Vector4(normCenter.x, normCenter.y, 0, 0));
        mat.SetFloat("_MaxRadius",   myMaxRadius);
        mat.SetFloat("_Strength",    distortionStrength);
        mat.SetFloat("_RingWidth",   ringWidth);
        mat.SetColor("_RingColor",   ringColor);
        mat.SetFloat("_Alpha",       0f);
        rawImg.material = mat;

        // 设置 rawImg.texture 为背景纹理（Shader 用 _MainTex 采样）
        if (bgTex != null)
            rawImg.texture = bgTex;

        // —— 高亮圆环层（Image + 空心 Sprite）——
        var ringGO = new GameObject("RippleRing");
        ringGO.transform.SetParent(parent, false);
        ringGO.transform.SetAsLastSibling();

        var ringRT = ringGO.AddComponent<RectTransform>();
        ringRT.anchorMin = new Vector2(0.5f, 0.5f);
        ringRT.anchorMax = new Vector2(0.5f, 0.5f);
        ringRT.pivot     = new Vector2(0.5f, 0.5f);

        Vector2 localPt;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            new Vector2(normCenter.x * Screen.width, normCenter.y * Screen.height),
            null, out localPt);
        ringRT.anchoredPosition = localPt;
        ringRT.sizeDelta        = new Vector2(80f, 80f);
        ringRT.localScale       = Vector3.zero;

        var ringImg = ringGO.AddComponent<Image>();
        ringImg.sprite        = GetRingSprite();
        ringImg.color         = ringColor;
        ringImg.raycastTarget = false;

        var ringCG = ringGO.AddComponent<CanvasGroup>();
        ringCG.alpha          = 0f;
        ringCG.blocksRaycasts = false;
        ringCG.interactable   = false;

        // —— 动画主循环 ——
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // cubic ease-out
            float tEased = 1f - Mathf.Pow(1f - t, 3f);

            // 更新 Shader 进度
            mat.SetFloat("_Progress", tEased);

            // alpha：快速淡入，平缓淡出
            float alpha = t < 0.05f
                ? t / 0.05f
                : Mathf.Pow(1f - (t - 0.05f) / 0.95f, 1.4f);
            float finalAlpha = alpha * peakAlpha;
            mat.SetFloat("_Alpha", finalAlpha);

            // 高亮圆环跟随波前
            float ringScale = tEased * myMaxRadius / 0.65f * 24f;
            ringRT.localScale = new Vector3(ringScale, ringScale, 1f);
            ringCG.alpha      = finalAlpha * 0.75f;

            elapsed += Time.deltaTime;
            yield return null;
        }

        mat.SetFloat("_Alpha", 0f);
        ringCG.alpha = 0f;

        Destroy(distortGO);
        Destroy(ringGO);
        Destroy(mat);
    }

    // ── 空心圆环 Sprite ──
    private static Sprite GetRingSprite()
    {
        if (s_ringSprite != null) return s_ringSprite;

        const int   res      = 256;
        const float outer    = res / 2f;
        const float ringFrac = 0.14f;
        float inner   = outer * (1f - ringFrac);
        float soft    = outer * ringFrac * 0.8f;

        var tex    = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[res * res];
        var ctr    = new Vector2(outer, outer);

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d    = Vector2.Distance(new Vector2(x, y), ctr);
            float aOut = Mathf.Clamp01((outer - d) / soft);
            float aIn  = Mathf.Clamp01((d - inner) / soft);
            pixels[y * res + x] = new Color(1f, 1f, 1f, Mathf.Min(aOut, aIn));
        }

        tex.SetPixels(pixels);
        tex.Apply();
        s_ringSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
        return s_ringSprite;
    }
}
