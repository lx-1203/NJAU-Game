using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// New-game opening story sequence.
/// Shows a black curtain, then advances through four story images by mouse click.
/// Place sprites at Resources/Intro/intro_1 ... intro_4 to replace the fallback panels.
/// </summary>
public class OpeningStoryManager : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    public class OpeningStorySlide
    {
        public string resourcePath;
        [TextArea(2, 4)] public string caption;
    }

    private const int StoryCanvasOrder = 420;
    private const string GameSceneName = "GameScene";
    private static bool hasPendingNewGameCharacter;
    private static bool hasPlayedPendingOpening;
    private static bool isOpeningPlaying;
    private static int pendingPlayerGender;

    private static readonly OpeningStorySlide[] MaleSlides =
    {
        new OpeningStorySlide
        {
            resourcePath = "Intro/Male/intro_1",
            caption = "拖着行李走进钟山脚下的那个夏天，你第一次意识到，大学生活已经真的开始了。"
        },
        new OpeningStorySlide
        {
            resourcePath = "Intro/Male/intro_2",
            caption = "宿舍门被推开，陌生的室友、球场的喧闹和晚归的灯光，一点点变成新的日常。"
        },
        new OpeningStorySlide
        {
            resourcePath = "Intro/Male/intro_3",
            caption = "课堂、社团、操场和图书馆，把四年的选择摊开放在你面前。"
        },
        new OpeningStorySlide
        {
            resourcePath = "Intro/Male/intro_4",
            caption = "从今天开始，你要在钟山下写下属于自己的大学生活。"
        }
    };

    private static readonly OpeningStorySlide[] FemaleSlides =
    {
        new OpeningStorySlide
        {
            resourcePath = "Intro/Female/intro_1",
            caption = "录取通知书寄到的那个夏天，你把对大学的想象悄悄装进了行李箱。"
        },
        new OpeningStorySlide
        {
            resourcePath = "Intro/Female/intro_2",
            caption = "宿舍楼的灯亮到很晚，陌生的名字慢慢变成每天都会听见的声音。"
        },
        new OpeningStorySlide
        {
            resourcePath = "Intro/Female/intro_3",
            caption = "课堂、社团、图书馆和校园路口，都在等待你做出不同的选择。"
        },
        new OpeningStorySlide
        {
            resourcePath = "Intro/Female/intro_4",
            caption = "从今天开始，你要在钟山下写下属于自己的大学生活。"
        }
    };

    [Header("Story")]
    public OpeningStorySlide[] slides = MaleSlides;
    public string targetSceneName = "GameScene";

    [Header("Timing")]
    public float curtainFadeDuration = 0.8f;
    public float imageFadeDuration = 0.7f;
    public float textFadeDuration = 0.35f;
    public float finishFadeDuration = 0.65f;
    public float minClickInterval = 0.25f;

    private CanvasGroup rootGroup;
    private Image storyImage;
    private Image fallbackImage;
    private TextMeshProUGUI captionText;
    private TextMeshProUGUI clickHintText;
    private bool isAdvancing;
    private bool hasFinished;
    private int currentIndex = -1;
    private float nextClickTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureGenderTracker()
    {
        if (FindFirstObjectByType<OpeningStoryGenderTracker>() != null)
        {
            return;
        }

        GameObject tracker = new GameObject("OpeningStoryGenderTracker");
        DontDestroyOnLoad(tracker);
        tracker.hideFlags = HideFlags.HideAndDontSave;
        tracker.AddComponent<OpeningStoryGenderTracker>();
    }

    public static bool TryInterceptSceneLoad(string sceneName)
    {
        if (!string.Equals(sceneName, GameSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (StartupFlowSettings.SkipOpeningStory)
        {
            hasPendingNewGameCharacter = false;
            return false;
        }

        if (!hasPendingNewGameCharacter || hasPlayedPendingOpening || isOpeningPlaying)
        {
            return false;
        }

        Play(sceneName);
        return true;
    }

    public static void Play(string targetSceneName)
    {
        UIFlowGuard.EnsureEventSystem();

        isOpeningPlaying = true;
        hasPlayedPendingOpening = true;

        GameObject root = new GameObject("OpeningStoryManager");
        OpeningStoryManager manager = root.AddComponent<OpeningStoryManager>();
        manager.targetSceneName = string.IsNullOrEmpty(targetSceneName) ? "GameScene" : targetSceneName;
        manager.slides = ResolveSlidesForCurrentPlayer();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private void Update()
    {
        if (hasFinished || isAdvancing || Time.unscaledTime < nextClickTime)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(Advance());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasFinished || isAdvancing || Time.unscaledTime < nextClickTime)
        {
            return;
        }

        StartCoroutine(Advance());
    }

    private IEnumerator PlaySequence()
    {
        Time.timeScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
        rootGroup.alpha = 0f;
        yield return FadeCanvas(rootGroup, 0f, 1f, curtainFadeDuration);
        yield return Advance();
    }

    private IEnumerator Advance()
    {
        isAdvancing = true;
        nextClickTime = Time.unscaledTime + minClickInterval;

        int nextIndex = currentIndex + 1;
        if (slides == null || slides.Length == 0 || nextIndex >= slides.Length)
        {
            yield return FinishAndLoad();
            yield break;
        }

        currentIndex = nextIndex;
        OpeningStorySlide slide = slides[currentIndex];

        yield return FadeText(0f, textFadeDuration);
        yield return FadeImageOut();
        ApplySlide(slide, currentIndex);
        yield return FadeImageIn();
        yield return FadeText(1f, textFadeDuration);

        isAdvancing = false;
    }

    private void ApplySlide(OpeningStorySlide slide, int index)
    {
        Sprite sprite = null;
        if (slide != null && !string.IsNullOrWhiteSpace(slide.resourcePath))
        {
            sprite = LoadSlideSprite(slide.resourcePath.Trim(), index);
        }

        storyImage.sprite = sprite;
        storyImage.enabled = sprite != null;
        fallbackImage.enabled = sprite == null;
        fallbackImage.color = GetFallbackColor(index);

        captionText.text = slide == null ? string.Empty : slide.caption;
        clickHintText.text = index >= slides.Length - 1 ? "点击进入校园" : "点击继续";
    }

    private IEnumerator FinishAndLoad()
    {
        hasFinished = true;
        isAdvancing = true;

        yield return FadeText(0f, textFadeDuration);
        yield return FadeCanvas(rootGroup, rootGroup.alpha, 1f, finishFadeDuration);

        UIFlowGuard.CleanupBlockingUI();
        isOpeningPlaying = false;
        hasPendingNewGameCharacter = false;
        SceneLoader.LoadSceneAfterOpening(targetSceneName);
        Destroy(gameObject);
    }

    private IEnumerator FadeImageOut()
    {
        Color storyStart = storyImage.color;
        Color fallbackStart = fallbackImage.color;

        float elapsed = 0f;
        while (elapsed < imageFadeDuration)
        {
            float t = Ease(elapsed / imageFadeDuration);
            storyImage.color = WithAlpha(storyStart, Mathf.Lerp(storyStart.a, 0f, t));
            fallbackImage.color = WithAlpha(fallbackStart, Mathf.Lerp(fallbackStart.a, 0f, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        storyImage.color = WithAlpha(storyStart, 0f);
        fallbackImage.color = WithAlpha(fallbackStart, 0f);
    }

    private IEnumerator FadeImageIn()
    {
        Color storyStart = WithAlpha(storyImage.color, 0f);
        Color fallbackStart = WithAlpha(fallbackImage.color, 0f);

        float elapsed = 0f;
        while (elapsed < imageFadeDuration)
        {
            float t = Ease(elapsed / imageFadeDuration);
            storyImage.color = WithAlpha(storyStart, Mathf.Lerp(0f, 1f, t));
            fallbackImage.color = WithAlpha(fallbackStart, Mathf.Lerp(0f, 1f, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        storyImage.color = WithAlpha(storyStart, 1f);
        fallbackImage.color = WithAlpha(fallbackStart, 1f);
    }

    private IEnumerator FadeText(float targetAlpha, float duration)
    {
        Color captionStart = captionText.color;
        Color hintStart = clickHintText.color;
        float captionAlpha = captionStart.a;
        float hintAlpha = hintStart.a;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Ease(elapsed / duration);
            captionText.color = WithAlpha(captionStart, Mathf.Lerp(captionAlpha, targetAlpha, t));
            clickHintText.color = WithAlpha(hintStart, Mathf.Lerp(hintAlpha, targetAlpha * 0.72f, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        captionText.color = WithAlpha(captionStart, targetAlpha);
        clickHintText.color = WithAlpha(hintStart, targetAlpha * 0.72f);
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            group.alpha = Mathf.Lerp(from, to, Ease(elapsed / duration));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        group.alpha = to;
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = StoryCanvasOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        rootGroup = gameObject.AddComponent<CanvasGroup>();
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;

        RectTransform root = gameObject.GetComponent<RectTransform>();

        Image curtain = CreateImage("BlackCurtain", root, Color.black);
        StretchFull(curtain.rectTransform);

        fallbackImage = CreateImage("FallbackStoryImage", root, GetFallbackColor(0));
        StretchFull(fallbackImage.rectTransform);
        fallbackImage.raycastTarget = false;
        fallbackImage.color = WithAlpha(fallbackImage.color, 0f);

        storyImage = CreateImage("StoryImage", root, Color.white);
        StretchFull(storyImage.rectTransform);
        storyImage.preserveAspect = true;
        storyImage.raycastTarget = false;
        storyImage.color = WithAlpha(storyImage.color, 0f);

        Image vignette = CreateImage("BottomVignette", root, new Color(0f, 0f, 0f, 0.58f));
        RectTransform vignetteRect = vignette.rectTransform;
        vignetteRect.anchorMin = new Vector2(0f, 0f);
        vignetteRect.anchorMax = new Vector2(1f, 0f);
        vignetteRect.pivot = new Vector2(0.5f, 0f);
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = new Vector2(0f, 300f);
        vignette.raycastTarget = false;

        captionText = CreateText("Caption", root, 36f, Color.white, TextAlignmentOptions.BottomLeft);
        RectTransform captionRect = captionText.rectTransform;
        captionRect.anchorMin = new Vector2(0f, 0f);
        captionRect.anchorMax = new Vector2(1f, 0f);
        captionRect.pivot = new Vector2(0.5f, 0f);
        captionRect.offsetMin = new Vector2(120f, 92f);
        captionRect.offsetMax = new Vector2(-120f, 230f);
        captionText.enableWordWrapping = true;
        captionText.color = WithAlpha(captionText.color, 0f);

        clickHintText = CreateText("ClickHint", root, 22f, new Color(1f, 1f, 1f, 0.72f), TextAlignmentOptions.BottomRight);
        RectTransform hintRect = clickHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.offsetMin = new Vector2(120f, 42f);
        hintRect.offsetMax = new Vector2(-120f, 88f);
        clickHintText.color = WithAlpha(clickHintText.color, 0f);
    }

    private Image CreateImage(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }

        return text;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Color GetFallbackColor(int index)
    {
        switch (index % 4)
        {
            case 0: return new Color(0.12f, 0.18f, 0.25f, 1f);
            case 1: return new Color(0.25f, 0.16f, 0.18f, 1f);
            case 2: return new Color(0.16f, 0.24f, 0.18f, 1f);
            default: return new Color(0.22f, 0.18f, 0.28f, 1f);
        }
    }

    private static OpeningStorySlide[] ResolveSlidesForCurrentPlayer()
    {
        int gender = hasPendingNewGameCharacter ? pendingPlayerGender
            : GameState.Instance != null ? GameState.Instance.PlayerGender : 0;

        Debug.Log($"[OpeningStory] Using {(gender == 1 ? "female" : "male")} opening slides.");
        return gender == 1 ? FemaleSlides : MaleSlides;
    }

    private static void RecordPendingGender(int gender)
    {
        pendingPlayerGender = gender == 1 ? 1 : 0;
        hasPendingNewGameCharacter = true;
    }

    private sealed class OpeningStoryGenderTracker : MonoBehaviour
    {
        private FieldInfo femaleToggleField;

        private void Update()
        {
            CharacterCreationUI ui = CharacterCreationUI.Instance;
            if (ui == null)
            {
                return;
            }

            if (femaleToggleField == null)
            {
                femaleToggleField = typeof(CharacterCreationUI).GetField("femaleToggle", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            Toggle femaleToggle = femaleToggleField == null ? null : femaleToggleField.GetValue(ui) as Toggle;
            if (femaleToggle != null)
            {
                RecordPendingGender(femaleToggle.isOn ? 1 : 0);
            }
        }
    }

    private Sprite LoadSlideSprite(string resourcePath, int index)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        string commonPath = $"Intro/intro_{index + 1}";
        return Resources.Load<Sprite>(commonPath);
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
