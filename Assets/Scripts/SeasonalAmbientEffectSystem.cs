using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全局季节飘落特效系统。
/// 默认根据 GameState.CurrentMonth 自动切换季节，也支持运行时手动指定。
/// 当前素材全部由代码生成，后续可直接替换为正式 Sprite。
/// </summary>
public class SeasonalAmbientEffectSystem : MonoBehaviour
{
    public enum SeasonalEffectMode
    {
        Auto = 0,
        None = 1,
        Spring = 2,
        Summer = 3,
        Autumn = 4,
        Winter = 5
    }

    [Serializable]
    private sealed class AmbientParticle
    {
        public RectTransform rect;
        public Image image;
        public float x;
        public float y;
        public float fallSpeed;
        public float driftSpeed;
        public float driftAmplitude;
        public float driftFrequency;
        public float driftPhase;
        public float rotationSpeed;
        public float angle;
        public float width;
        public float height;
        public float baseScale;
    }

    private struct EffectProfile
    {
        public Sprite Sprite;
        public Color[] Colors;
        public int ParticleCount;
        public Vector2 ScaleRange;
        public Vector2 FallSpeedRange;
        public Vector2 DriftSpeedRange;
        public Vector2 DriftAmplitudeRange;
        public Vector2 DriftFrequencyRange;
        public Vector2 RotationSpeedRange;
        public Vector2 SizeRange;
        public float SpawnTopPadding;
    }

    public static SeasonalAmbientEffectSystem Instance { get; private set; }

    [Header("Mode")]
    [SerializeField] private SeasonalEffectMode mode = SeasonalEffectMode.Auto;

    [Header("Overlay")]
    [SerializeField] private int sortingOrder = 120;
    [SerializeField] private bool hideInSceneView = false;

    [Header("Custom Sprites")]
    [Tooltip("可选：替换春季示例纹理")]
    [SerializeField] private Sprite springSpriteOverride;
    [Tooltip("可选：替换夏季示例纹理")]
    [SerializeField] private Sprite summerSpriteOverride;
    [Tooltip("可选：替换秋季示例纹理")]
    [SerializeField] private Sprite autumnSpriteOverride;
    [Tooltip("可选：替换冬季示例纹理")]
    [SerializeField] private Sprite winterSpriteOverride;

    private readonly List<AmbientParticle> particles = new List<AmbientParticle>();

    private Canvas overlayCanvas;
    private CanvasScaler canvasScaler;
    private RectTransform particleRoot;

    private Sprite generatedSpringSprite;
    private Sprite generatedSummerSprite;
    private Sprite generatedAutumnSprite;
    private Sprite generatedWinterSprite;

    private SeasonalEffectMode activeResolvedMode = SeasonalEffectMode.None;
    private GameState boundGameState;
    private int cachedMonth = -1;
    private Vector2 lastScreenSize = Vector2.zero;
    private float simulationAccumulator;

    private const float SimulationStep = 1f / 60f;
    private const float MaxFrameDelta = 1f / 15f;
    private const int MaxSimulationStepsPerFrame = 4;

    private Vector2 GetParticleViewportSize()
    {
        if (particleRoot != null)
        {
            Rect rect = particleRoot.rect;
            if (rect.width > 0.01f && rect.height > 0.01f)
            {
                return new Vector2(rect.width, rect.height);
            }
        }

        return new Vector2(Screen.width, Screen.height);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static SeasonalAmbientEffectSystem EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SeasonalAmbientEffectSystem existing = FindFirstObjectByType<SeasonalAmbientEffectSystem>();
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject("SeasonalAmbientEffectSystem");
        return root.AddComponent<SeasonalAmbientEffectSystem>();
    }

    public void SetMode(SeasonalEffectMode newMode, bool rebuildImmediately = true)
    {
        mode = newMode;
        if (rebuildImmediately)
        {
            RefreshEffect(true);
        }
    }

    public void RefreshFromGameState()
    {
        RefreshEffect(true);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateOverlayCanvas();
        EnsureSprites();

        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshEffect(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindGameState();
            Instance = null;
        }
    }

    private void Update()
    {
        TryBindGameState();

        Vector2 currentScreenSize = GetParticleViewportSize();
        if (currentScreenSize != lastScreenSize)
        {
            lastScreenSize = currentScreenSize;
            RefreshEffect(true);
        }

        SeasonalEffectMode resolvedMode = ResolveCurrentMode();
        if (resolvedMode != activeResolvedMode)
        {
            RefreshEffect(true);
        }

        if (activeResolvedMode == SeasonalEffectMode.None || particles.Count == 0)
        {
            return;
        }

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
        Vector2 viewportSize = GetParticleViewportSize();
        float width = viewportSize.x;
        float height = viewportSize.y;

        simulationAccumulator += deltaTime;
        int simulatedSteps = 0;
        while (simulationAccumulator >= SimulationStep && simulatedSteps < MaxSimulationStepsPerFrame)
        {
            SimulateParticles(SimulationStep, width, height);
            simulationAccumulator -= SimulationStep;
            simulatedSteps++;
        }

        if (simulatedSteps == 0)
        {
            SimulateParticles(Mathf.Max(0.0001f, deltaTime), width, height);
        }

        simulationAccumulator = Mathf.Min(simulationAccumulator, SimulationStep);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (overlayCanvas == null)
        {
            CreateOverlayCanvas();
        }

        overlayCanvas.sortingOrder = sortingOrder;
        RefreshEffect(true);
    }

    private void SimulateParticles(float deltaTime, float width, float height)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            AmbientParticle particle = particles[i];
            if (particle == null || particle.rect == null)
            {
                continue;
            }

            particle.y -= particle.fallSpeed * deltaTime;
            particle.x += particle.driftSpeed * deltaTime;

            float sway = Mathf.Sin((Time.unscaledTime + particle.driftPhase) * particle.driftFrequency) * particle.driftAmplitude;
            particle.angle += particle.rotationSpeed * deltaTime;

            if (particle.y < -particle.height - 32f)
            {
                RespawnParticle(particle, width, height, true);
            }

            if (particle.x < -width * 0.2f)
            {
                particle.x += width * 1.4f;
            }
            else if (particle.x > width * 1.2f)
            {
                particle.x -= width * 1.4f;
            }

            particle.rect.anchoredPosition = new Vector2(particle.x + sway, particle.y);
            particle.rect.localRotation = Quaternion.Euler(0f, 0f, particle.angle);
        }
    }

    private void OnGameStateChanged()
    {
        if (boundGameState == null)
        {
            return;
        }

        if (cachedMonth != boundGameState.CurrentMonth)
        {
            cachedMonth = boundGameState.CurrentMonth;
            if (mode == SeasonalEffectMode.Auto)
            {
                RefreshEffect(true);
            }
        }
    }

    private void TryBindGameState()
    {
        if (boundGameState == GameState.Instance)
        {
            return;
        }

        UnbindGameState();

        if (GameState.Instance == null)
        {
            return;
        }

        boundGameState = GameState.Instance;
        cachedMonth = boundGameState.CurrentMonth;
        boundGameState.OnStateChanged += OnGameStateChanged;
    }

    private void UnbindGameState()
    {
        if (boundGameState != null)
        {
            boundGameState.OnStateChanged -= OnGameStateChanged;
            boundGameState = null;
        }
    }

    private void CreateOverlayCanvas()
    {
        if (overlayCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("SeasonalAmbientCanvas");
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = sortingOrder;

        canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        GameObject rootObject = new GameObject("ParticleRoot");
        rootObject.transform.SetParent(canvasObject.transform, false);
        particleRoot = rootObject.AddComponent<RectTransform>();
        particleRoot.anchorMin = Vector2.zero;
        particleRoot.anchorMax = Vector2.one;
        particleRoot.offsetMin = Vector2.zero;
        particleRoot.offsetMax = Vector2.zero;

        CanvasGroup group = rootObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        lastScreenSize = GetParticleViewportSize();

        if (hideInSceneView)
        {
            canvasObject.hideFlags = HideFlags.HideInHierarchy;
            rootObject.hideFlags = HideFlags.HideInHierarchy;
        }
    }

    private void EnsureSprites()
    {
        if (generatedSpringSprite == null)
        {
            generatedSpringSprite = CreatePetalSprite("GeneratedSpringPetal", new Color(1f, 0.83f, 0.9f, 1f));
        }

        if (generatedSummerSprite == null)
        {
            generatedSummerSprite = CreateGlowDotSprite("GeneratedSummerMote", new Color(1f, 0.94f, 0.64f, 1f));
        }

        if (generatedAutumnSprite == null)
        {
            generatedAutumnSprite = CreateLeafSprite("GeneratedAutumnLeaf", new Color(0.92f, 0.47f, 0.17f, 1f));
        }

        if (generatedWinterSprite == null)
        {
            generatedWinterSprite = CreateSnowSprite("GeneratedWinterSnow", Color.white);
        }
    }

    private SeasonalEffectMode ResolveCurrentMode()
    {
        if (mode != SeasonalEffectMode.Auto)
        {
            return mode;
        }

        int month = boundGameState != null ? boundGameState.CurrentMonth : DateTime.Now.Month;

        if (month >= 3 && month <= 5)
        {
            return SeasonalEffectMode.Spring;
        }

        if (month >= 6 && month <= 8)
        {
            return SeasonalEffectMode.Summer;
        }

        if (month >= 9 && month <= 11)
        {
            return SeasonalEffectMode.Autumn;
        }

        return SeasonalEffectMode.Winter;
    }

    private void RefreshEffect(bool forceRebuild)
    {
        SeasonalEffectMode resolvedMode = ResolveCurrentMode();
        bool needsRebuild = forceRebuild || resolvedMode != activeResolvedMode;

        if (!needsRebuild)
        {
            return;
        }

        activeResolvedMode = resolvedMode;

        ClearParticles();

        if (particleRoot == null || resolvedMode == SeasonalEffectMode.None)
        {
            return;
        }

        EffectProfile profile = BuildProfile(resolvedMode);
        for (int i = 0; i < profile.ParticleCount; i++)
        {
            CreateParticle(profile);
        }
    }

    private EffectProfile BuildProfile(SeasonalEffectMode resolvedMode)
    {
        switch (resolvedMode)
        {
            case SeasonalEffectMode.Spring:
                return new EffectProfile
                {
                    Sprite = springSpriteOverride != null ? springSpriteOverride : generatedSpringSprite,
                    Colors = new[]
                    {
                        new Color(1f, 0.85f, 0.92f, 0.75f),
                        new Color(1f, 0.78f, 0.88f, 0.68f),
                        new Color(0.98f, 0.9f, 0.96f, 0.72f)
                    },
                    ParticleCount = 24,
                    ScaleRange = new Vector2(0.75f, 1.3f),
                    FallSpeedRange = new Vector2(28f, 54f),
                    DriftSpeedRange = new Vector2(-24f, 12f),
                    DriftAmplitudeRange = new Vector2(8f, 30f),
                    DriftFrequencyRange = new Vector2(0.8f, 1.5f),
                    RotationSpeedRange = new Vector2(-42f, 42f),
                    SizeRange = new Vector2(20f, 36f),
                    SpawnTopPadding = 48f
                };

            case SeasonalEffectMode.Summer:
                return new EffectProfile
                {
                    Sprite = summerSpriteOverride != null ? summerSpriteOverride : generatedSummerSprite,
                    Colors = new[]
                    {
                        new Color(1f, 0.96f, 0.72f, 0.4f),
                        new Color(0.82f, 1f, 0.76f, 0.35f),
                        new Color(1f, 0.9f, 0.6f, 0.28f)
                    },
                    ParticleCount = 18,
                    ScaleRange = new Vector2(0.8f, 1.45f),
                    FallSpeedRange = new Vector2(10f, 24f),
                    DriftSpeedRange = new Vector2(-10f, 10f),
                    DriftAmplitudeRange = new Vector2(6f, 18f),
                    DriftFrequencyRange = new Vector2(0.5f, 1f),
                    RotationSpeedRange = new Vector2(-18f, 18f),
                    SizeRange = new Vector2(14f, 24f),
                    SpawnTopPadding = 32f
                };

            case SeasonalEffectMode.Autumn:
                return new EffectProfile
                {
                    Sprite = autumnSpriteOverride != null ? autumnSpriteOverride : generatedAutumnSprite,
                    Colors = new[]
                    {
                        new Color(0.9f, 0.49f, 0.16f, 0.85f),
                        new Color(0.8f, 0.27f, 0.12f, 0.82f),
                        new Color(0.95f, 0.69f, 0.18f, 0.78f),
                        new Color(0.62f, 0.31f, 0.14f, 0.78f)
                    },
                    ParticleCount = 30,
                    ScaleRange = new Vector2(0.9f, 1.5f),
                    FallSpeedRange = new Vector2(36f, 82f),
                    DriftSpeedRange = new Vector2(-28f, 22f),
                    DriftAmplitudeRange = new Vector2(12f, 42f),
                    DriftFrequencyRange = new Vector2(0.9f, 1.8f),
                    RotationSpeedRange = new Vector2(-75f, 75f),
                    SizeRange = new Vector2(24f, 42f),
                    SpawnTopPadding = 56f
                };

            case SeasonalEffectMode.Winter:
            default:
                return new EffectProfile
                {
                    Sprite = winterSpriteOverride != null ? winterSpriteOverride : generatedWinterSprite,
                    Colors = new[]
                    {
                        new Color(1f, 1f, 1f, 0.92f),
                        new Color(0.9f, 0.97f, 1f, 0.78f),
                        new Color(0.95f, 0.98f, 1f, 0.84f)
                    },
                    ParticleCount = 42,
                    ScaleRange = new Vector2(0.8f, 1.45f),
                    FallSpeedRange = new Vector2(22f, 52f),
                    DriftSpeedRange = new Vector2(-12f, 12f),
                    DriftAmplitudeRange = new Vector2(6f, 24f),
                    DriftFrequencyRange = new Vector2(0.6f, 1.2f),
                    RotationSpeedRange = new Vector2(-20f, 20f),
                    SizeRange = new Vector2(12f, 26f),
                    SpawnTopPadding = 48f
                };
        }
    }

    private void CreateParticle(EffectProfile profile)
    {
        GameObject particleObject = new GameObject("AmbientParticle");
        particleObject.transform.SetParent(particleRoot, false);

        RectTransform rect = particleObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = particleObject.AddComponent<Image>();
        image.sprite = profile.Sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = profile.Colors[UnityEngine.Random.Range(0, profile.Colors.Length)];

        AmbientParticle particle = new AmbientParticle
        {
            rect = rect,
            image = image
        };

        particles.Add(particle);
        Vector2 viewportSize = GetParticleViewportSize();
        RespawnParticle(particle, viewportSize.x, viewportSize.y, true, profile);
    }

    private void RespawnParticle(AmbientParticle particle, float width, float height, bool fromTop)
    {
        RespawnParticle(particle, width, height, fromTop, BuildProfile(activeResolvedMode));
    }

    private void RespawnParticle(AmbientParticle particle, float width, float height, bool fromTop, EffectProfile profile)
    {
        if (particle == null || particle.rect == null || particle.image == null)
        {
            return;
        }

        float size = UnityEngine.Random.Range(profile.SizeRange.x, profile.SizeRange.y);
        float scale = UnityEngine.Random.Range(profile.ScaleRange.x, profile.ScaleRange.y);

        particle.width = size * scale;
        particle.height = size * scale;
        particle.baseScale = scale;
        particle.fallSpeed = UnityEngine.Random.Range(profile.FallSpeedRange.x, profile.FallSpeedRange.y);
        particle.driftSpeed = UnityEngine.Random.Range(profile.DriftSpeedRange.x, profile.DriftSpeedRange.y);
        particle.driftAmplitude = UnityEngine.Random.Range(profile.DriftAmplitudeRange.x, profile.DriftAmplitudeRange.y);
        particle.driftFrequency = UnityEngine.Random.Range(profile.DriftFrequencyRange.x, profile.DriftFrequencyRange.y);
        particle.rotationSpeed = UnityEngine.Random.Range(profile.RotationSpeedRange.x, profile.RotationSpeedRange.y);
        particle.driftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        particle.angle = UnityEngine.Random.Range(0f, 360f);

        particle.x = UnityEngine.Random.Range(-width * 0.1f, width * 1.1f);
        particle.y = fromTop
            ? height + UnityEngine.Random.Range(0f, profile.SpawnTopPadding)
            : UnityEngine.Random.Range(-height * 0.05f, height + profile.SpawnTopPadding);

        particle.rect.sizeDelta = new Vector2(particle.width, particle.height);
        particle.rect.anchoredPosition = new Vector2(particle.x, particle.y);
        particle.rect.localRotation = Quaternion.Euler(0f, 0f, particle.angle);
        particle.image.sprite = profile.Sprite;
        particle.image.color = profile.Colors[UnityEngine.Random.Range(0, profile.Colors.Length)];
    }

    private void ClearParticles()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            AmbientParticle particle = particles[i];
            if (particle != null && particle.rect != null)
            {
                Destroy(particle.rect.gameObject);
            }
        }

        particles.Clear();
    }

    private Sprite CreateSnowSprite(string spriteName, Color tint)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.34f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - Mathf.Pow(distance / radius, 2.2f));
                Color color = new Color(tint.r, tint.g, tint.b, alpha);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateGlowDotSprite(string spriteName, Color tint)
    {
        const int size = 28;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalized = Mathf.Clamp01(distance / radius);
                float alpha = Mathf.Clamp01(1f - normalized);
                alpha = alpha * alpha * 0.95f;
                texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateLeafSprite(string spriteName, Color tint)
    {
        const int width = 40;
        const int height = 56;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x / (width - 1f)) * 2f - 1f;
                float ny = (y / (height - 1f)) * 2f - 1f;

                float widthCurve = 0.92f * (1f - Mathf.Abs(ny));
                float veinTaper = 0.16f + 0.2f * (1f - Mathf.Abs(ny));
                float leafBody = Mathf.Abs(nx) <= Mathf.Max(0.02f, widthCurve - 0.15f * ny * ny) ? 1f : 0f;
                float sideCut = Mathf.Abs(nx) > widthCurve ? 0f : 1f;
                float topPoint = ny > 0.82f && Mathf.Abs(nx) > (1f - ny) * 2.2f ? 0f : 1f;
                float bottomStem = ny < -0.82f && Mathf.Abs(nx) > (ny + 1f) * 1.6f + 0.05f ? 0f : 1f;
                float alpha = leafBody * sideCut * topPoint * bottomStem;

                if (alpha > 0.5f)
                {
                    float midRib = Mathf.Abs(nx) < veinTaper ? 0.12f : 0f;
                    Color color = Color.Lerp(tint * 0.82f, tint, Mathf.InverseLerp(-1f, 1f, ny));
                    color += new Color(midRib, midRib * 0.45f, 0f, 0f);
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreatePetalSprite(string spriteName, Color tint)
    {
        const int width = 32;
        const int height = 42;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x / (width - 1f)) * 2f - 1f;
                float ny = (y / (height - 1f)) * 2f - 1f;

                float bulb = (nx * nx) / 0.62f + ((ny - 0.12f) * (ny - 0.12f)) / 0.95f;
                float taper = Mathf.Abs(nx) < Mathf.Lerp(0.04f, 0.55f, Mathf.InverseLerp(-1f, 0.15f, ny)) ? 1f : 0f;
                float alpha = bulb <= 1f && taper > 0f ? 1f : 0f;

                if (alpha > 0.5f)
                {
                    float highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.2f, 0.35f)) * 1.5f);
                    Color color = Color.Lerp(tint * 0.88f, tint, highlight);
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
