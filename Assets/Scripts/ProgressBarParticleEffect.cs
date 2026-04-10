using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 纯 UI 粒子效果 —— 在进度条前端产生上升、发光、飘散的粒子
/// 使用 UI Image 模拟粒子，100% 兼容 Screen Space Overlay Canvas
/// 无需额外摄像机或渲染管线
/// </summary>
public class ProgressBarParticleEffect : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("进度条填充 Image")]
    public Image progressBarFill;

    [Tooltip("进度条背景 RectTransform（用于确定边界）")]
    public RectTransform progressBarRect;

    [Header("粒子数量与生成")]
    [Tooltip("最大粒子数")]
    public int maxParticles = 50;
    [Tooltip("每秒生成粒子数")]
    public float spawnRate = 25f;
    [Tooltip("粒子最大生命周期（秒）")]
    public float particleLifetime = 1.5f;

    [Header("粒子大小")]
    [Tooltip("最小粒子大小")]
    public float minSize = 8f;
    [Tooltip("最大粒子大小")]
    public float maxSize = 22f;

    [Header("粒子运动")]
    [Tooltip("上升速度")]
    public float riseSpeed = 80f;
    [Tooltip("水平扩散范围")]
    public float spreadX = 50f;
    [Tooltip("上升加速度")]
    public float riseAccel = 20f;
    [Tooltip("水平阻力")]
    public float horizontalDrag = 0.96f;

    [Header("颜色")]
    [Tooltip("基础粒子颜色（主题绿色）")]
    public Color baseColor = new Color(0.05f, 0.65f, 0.50f, 0.9f);
    [Tooltip("高亮粒子颜色（明亮绿色）")]
    public Color brightColor = new Color(0.3f, 1f, 0.75f, 1f);
    [Tooltip("光晕颜色")]
    public Color glowColor = new Color(0.05f, 0.8f, 0.55f, 0.4f);
    [Tooltip("拖尾颜色（进度条后方）")]
    public Color trailColor = new Color(0.1f, 0.9f, 0.6f, 0.6f);

    [Header("前端光晕")]
    [Tooltip("是否启用前端光晕")]
    public bool enableGlow = true;
    [Tooltip("光晕大小")]
    public float glowSize = 80f;
    [Tooltip("光晕脉动速度")]
    public float glowPulseSpeed = 4f;

    [Header("拖尾效果")]
    [Tooltip("是否启用进度条后方的拖尾粒子")]
    public bool enableTrail = true;
    [Tooltip("拖尾粒子生成率")]
    public float trailSpawnRate = 12f;
    [Tooltip("拖尾粒子最小大小")]
    public float trailMinSize = 4f;
    [Tooltip("拖尾粒子最大大小")]
    public float trailMaxSize = 10f;

    [Header("总开关")]
    [Tooltip("是否启用粒子效果")]
    public bool enableParticles = true;

    // ============ 内部状态 ============
    private float currentProgress = 0f;
    private float spawnTimer = 0f;
    private float trailSpawnTimer = 0f;

    private Sprite circleSprite;
    private Sprite softCircleSprite;
    private RectTransform container;

    // 光晕
    private GameObject glowObj;
    private Image glowImage;
    private RectTransform glowRect;

    // 粒子池
    private List<UIParticle> activeParticles = new List<UIParticle>();
    private Queue<UIParticle> pool = new Queue<UIParticle>();

    /// <summary>
    /// UI 粒子数据
    /// </summary>
    private class UIParticle
    {
        public GameObject go;
        public RectTransform rt;
        public Image img;
        public float age;
        public float maxAge;
        public Vector2 vel;
        public float size;
        public Color color;
        public bool isTrail; // 是否是拖尾粒子
    }

    #region 生命周期

    private void Awake()
    {
        // 创建圆形纹理（硬边）
        circleSprite = CreateCircleSprite(32, false);
        // 创建软圆形纹理（用于光晕和拖尾）
        softCircleSprite = CreateCircleSprite(64, true);

        // 创建粒子容器
        var containerObj = new GameObject("_UIParticleContainer");
        containerObj.transform.SetParent(transform, false);
        container = containerObj.AddComponent<RectTransform>();
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;

        // 创建光晕
        if (enableGlow)
        {
            CreateGlow();
        }

        // 预创建粒子池
        for (int i = 0; i < maxParticles; i++)
        {
            pool.Enqueue(CreatePooledParticle());
        }
    }

    private void Update()
    {
        if (!enableParticles || progressBarRect == null) return;

        // 自动跟踪 fillAmount（Filled 模式）
        if (progressBarFill != null && progressBarFill.type == Image.Type.Filled)
        {
            currentProgress = progressBarFill.fillAmount;
        }

        bool shouldEmit = currentProgress > 0.005f && currentProgress < 0.995f;

        // 更新光晕
        if (glowObj != null)
        {
            glowObj.SetActive(shouldEmit);
            if (shouldEmit) UpdateGlow();
        }

        // 生成前端粒子
        if (shouldEmit)
        {
            spawnTimer += Time.deltaTime;
            float interval = 1f / spawnRate;
            while (spawnTimer >= interval && pool.Count > 0)
            {
                spawnTimer -= interval;
                EmitParticle(false);
            }

            // 生成拖尾粒子
            if (enableTrail)
            {
                trailSpawnTimer += Time.deltaTime;
                float trailInterval = 1f / trailSpawnRate;
                while (trailSpawnTimer >= trailInterval && pool.Count > 0)
                {
                    trailSpawnTimer -= trailInterval;
                    EmitParticle(true);
                }
            }
        }

        // 更新所有活跃粒子
        UpdateActiveParticles();
    }

    private void OnDestroy()
    {
        // 清理纹理
        if (circleSprite != null && circleSprite.texture != null)
        {
            Destroy(circleSprite.texture);
            Destroy(circleSprite);
        }
        if (softCircleSprite != null && softCircleSprite.texture != null)
        {
            Destroy(softCircleSprite.texture);
            Destroy(softCircleSprite);
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 外部设置当前进度（用于渐变材质模式）
    /// 由 LoadingScreenUI.UpdateProgress() 调用
    /// </summary>
    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// 触发一次完成时的爆发粒子
    /// </summary>
    public void PlayCompletionBurst()
    {
        if (progressBarRect == null) return;

        Rect r = progressBarRect.rect;
        int burstCount = Mathf.Min(30, pool.Count);

        for (int i = 0; i < burstCount; i++)
        {
            var pt = GetFromPool();
            if (pt == null) break;

            pt.age = 0f;
            pt.maxAge = Random.Range(0.8f, 2.0f);
            pt.size = Random.Range(maxSize, maxSize * 2.5f);
            pt.rt.sizeDelta = new Vector2(pt.size, pt.size);
            pt.color = Color.Lerp(baseColor, brightColor, Random.value);
            pt.img.color = pt.color;
            pt.img.sprite = circleSprite;
            pt.isTrail = false;

            // 沿进度条随机分布
            float randomX = r.xMin + r.width * Random.value;
            float cy = r.center.y;
            Vector3 wp = progressBarRect.TransformPoint(new Vector3(randomX, cy, 0));
            Vector3 lp = container.InverseTransformPoint(wp);
            pt.rt.anchoredPosition = new Vector2(lp.x, lp.y);

            // 向四面八方爆发
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(120f, 300f);
            pt.vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
        }
    }

    /// <summary>
    /// 停止所有粒子效果
    /// </summary>
    public void StopAllParticles()
    {
        enableParticles = false;
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            RecycleParticle(i);
        }
    }

    #endregion

    #region 粒子创建与回收

    private UIParticle CreatePooledParticle()
    {
        var pt = new UIParticle();
        pt.go = new GameObject("_p");
        pt.go.transform.SetParent(container, false);
        pt.rt = pt.go.AddComponent<RectTransform>();
        pt.img = pt.go.AddComponent<Image>();
        pt.img.sprite = circleSprite;
        pt.img.raycastTarget = false;
        pt.go.SetActive(false);
        return pt;
    }

    private UIParticle GetFromPool()
    {
        if (pool.Count == 0) return null;

        var pt = pool.Dequeue();
        pt.go.SetActive(true);
        activeParticles.Add(pt);
        return pt;
    }

    private void RecycleParticle(int index)
    {
        var pt = activeParticles[index];
        pt.go.SetActive(false);
        activeParticles.RemoveAt(index);
        pool.Enqueue(pt);
    }

    #endregion

    #region 粒子发射

    private void EmitParticle(bool isTrail)
    {
        var pt = GetFromPool();
        if (pt == null) return;

        pt.age = 0f;
        pt.isTrail = isTrail;

        Rect r = progressBarRect.rect;

        if (isTrail)
        {
            // 拖尾粒子：在已填充区域随机位置生成
            pt.maxAge = Random.Range(particleLifetime * 0.4f, particleLifetime * 0.8f);
            pt.size = Random.Range(trailMinSize, trailMaxSize);
            pt.color = trailColor;
            pt.color.a *= Random.Range(0.5f, 1.0f);
            pt.img.sprite = softCircleSprite;

            // 随机位置（在已填充的范围内）
            float randomX = r.xMin + r.width * currentProgress * Random.Range(0.3f, 1.0f);
            float randomY = r.center.y + Random.Range(-r.height * 0.4f, r.height * 0.4f);
            Vector3 wp = progressBarRect.TransformPoint(new Vector3(randomX, randomY, 0));
            Vector3 lp = container.InverseTransformPoint(wp);
            pt.rt.anchoredPosition = new Vector2(lp.x, lp.y);

            // 轻微上升
            pt.vel = new Vector2(Random.Range(-10f, 10f), Random.Range(15f, 40f));
        }
        else
        {
            // 前端粒子：在进度条前端生成
            pt.maxAge = Random.Range(particleLifetime * 0.5f, particleLifetime * 1.5f);
            pt.size = Random.Range(minSize, maxSize);
            pt.color = Color.Lerp(baseColor, brightColor, Random.value);
            pt.img.sprite = circleSprite;

            // 位置在进度前端
            float leadX = r.xMin + r.width * currentProgress;
            float centerY = r.center.y;
            Vector3 wp = progressBarRect.TransformPoint(new Vector3(leadX, centerY, 0));
            Vector3 lp = container.InverseTransformPoint(wp);

            float offsetX = Random.Range(-spreadX, spreadX);
            float offsetY = Random.Range(-r.height * 0.4f, r.height * 0.4f);
            pt.rt.anchoredPosition = new Vector2(lp.x + offsetX, lp.y + offsetY);

            // 主要向上运动，带随机水平速度
            float vx = Random.Range(-30f, 30f);
            float vy = Random.Range(riseSpeed * 0.4f, riseSpeed * 1.6f);
            pt.vel = new Vector2(vx, vy);
        }

        pt.rt.sizeDelta = new Vector2(pt.size, pt.size);
        pt.img.color = pt.color;
    }

    #endregion

    #region 粒子更新

    private void UpdateActiveParticles()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            var pt = activeParticles[i];
            pt.age += Time.deltaTime;

            // 超过生命周期则回收
            if (pt.age >= pt.maxAge)
            {
                RecycleParticle(i);
                continue;
            }

            float t = pt.age / pt.maxAge; // 归一化生命进度 0→1

            // 运动
            pt.rt.anchoredPosition += pt.vel * Time.deltaTime;
            pt.vel.y += riseAccel * Time.deltaTime;     // 上升加速
            pt.vel.x *= horizontalDrag;                  // 水平阻力

            // 渐变透明（二次衰减，后期消失更快）
            float alpha = pt.color.a * (1f - t * t);

            // 缩小（线性缩小到 10%）
            float scale = Mathf.Lerp(1f, 0.1f, t);
            float currentSize = pt.size * scale;

            // 应用
            pt.img.color = new Color(pt.color.r, pt.color.g, pt.color.b, alpha);
            pt.rt.sizeDelta = new Vector2(currentSize, currentSize);
        }
    }

    #endregion

    #region 光晕

    private void CreateGlow()
    {
        glowObj = new GameObject("_LeadGlow");
        glowObj.transform.SetParent(container, false);
        glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.sizeDelta = new Vector2(glowSize, glowSize);
        glowImage = glowObj.AddComponent<Image>();
        glowImage.sprite = softCircleSprite;
        glowImage.color = glowColor;
        glowImage.raycastTarget = false;
    }

    private void UpdateGlow()
    {
        if (progressBarRect == null || glowRect == null) return;

        Rect r = progressBarRect.rect;
        float leadX = r.xMin + r.width * currentProgress;
        float centerY = r.center.y;

        Vector3 wp = progressBarRect.TransformPoint(new Vector3(leadX, centerY, 0));
        Vector3 lp = container.InverseTransformPoint(wp);
        glowRect.anchoredPosition = new Vector2(lp.x, lp.y);

        // 脉动效果
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * glowPulseSpeed);
        glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * pulse);
        glowRect.sizeDelta = new Vector2(glowSize * pulse, glowSize * pulse);
    }

    #endregion

    #region 纹理生成

    /// <summary>
    /// 程序化生成圆形 Sprite
    /// </summary>
    /// <param name="size">纹理尺寸</param>
    /// <param name="soft">true = 柔和边缘（光晕用），false = 实心圆</param>
    private Sprite CreateCircleSprite(int size, bool soft)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = size / 2f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float alpha;

                if (soft)
                {
                    // 高斯式柔和衰减
                    alpha = Mathf.Clamp01(Mathf.Exp(-dist * dist * 3f));
                }
                else
                {
                    // 硬边圆，边缘带微量抗锯齿
                    alpha = Mathf.Clamp01((0.95f - dist) / 0.1f);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    #endregion
}
