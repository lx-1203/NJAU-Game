using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 开场制作组动画管理器
/// 序列：logo.webm视频 -> logo1.png淡入 -> logo_2.png淡入 -> 跳转LoadingScreen
/// </summary>
public class SplashScreenManager : MonoBehaviour
{
    [Header("资源路径")]
    [Tooltip("视频文件名（位于 StreamingAssets）")]
    public string videoFileName = "logo.mp4";

    [Header("图片")]
    public Sprite logo1Sprite;  // 拖入 logo1.png
    public Sprite logo2Sprite; // 拖入 logo_2.png

    [Header("时间配置")]
    [Tooltip("视频播放等待时间（秒），视频播完前等待的最长时间")]
    public float videoWaitTime = 5f;
    [Tooltip("logo1 显示时长（秒）")]
    public float logo1DisplayTime = 2f;
    [Tooltip("logo2 显示时长（秒）")]
    public float logo2DisplayTime = 2f;
    [Tooltip("淡入淡出时长（秒）")]
    public float fadeDuration = 1f;

    [Header("跳转目标")]
    [Tooltip("动画结束后跳转的场景")]
    public string nextScene = "LoadingScreen";

    // ===== 运行时组件 =====
    private Canvas canvas;
    private CanvasScaler scaler;
    private GraphicRaycaster raycaster;
    private VideoPlayer videoPlayer;
    private RawImage videoImage;
    private Image logo1Image;
    private Image logo2Image;
    private CanvasGroup globalFade;
    private string resolvedVideoPath;

    // ===== 状态 =====
    private bool videoPrepared = false;
    private bool videoFinished = false;
    private bool videoFailed = false;
    private bool canSkip = false;
    private bool hasSkipped = false;

    // ===== 序列阶段 =====

    #region Unity 生命周期

    private void Awake()
    {
        resolvedVideoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName).Replace("\\", "/");
        SetupCanvas();
        CreateVideoPlayer();
        CreateImages();
        CreateFadeOverlay();
    }

    private void Start()
    {
        StartCoroutine(AnimationSequence());
    }

    private void Update()
    {
        // 检测跳过：任意按键或点击
        if (canSkip && !hasSkipped)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                SkipToEnd();
            }
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
            if (videoPlayer.targetTexture != null)
            {
                videoPlayer.targetTexture.Release();
            }
        }
    }

    #endregion

    #region Canvas 设置

    private void SetupCanvas()
    {
        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();
    }

    #endregion

    #region 视频播放器

    private void CreateVideoPlayer()
    {
        // GameObject for VideoPlayer
        GameObject videoGO = new GameObject("VideoPlayer");
        videoGO.transform.SetParent(transform, false);

        RectTransform rt = videoGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // RenderTexture for video output
        RenderTexture rtTexture = new RenderTexture(1920, 1080, 24);
        rtTexture.Create();

        videoPlayer = videoGO.AddComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = resolvedVideoPath;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rtTexture;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.SetDirectAudioMute(0, true);

        // RawImage 显示视频
        videoImage = videoGO.AddComponent<RawImage>();
        videoImage.texture = rtTexture;
        videoImage.raycastTarget = false;

        // 监听准备完成事件
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;

        // 开始准备视频
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        videoPrepared = true;
        if (globalFade != null)
        {
            globalFade.alpha = 0f;
        }
        Debug.Log("[SplashScreen] 视频准备完成，开始播放");
        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoFinished = true;
        Debug.Log("[SplashScreen] 视频播放结束");
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        videoFailed = true;
        Debug.LogWarning("[SplashScreen] 视频播放失败: " + message);
    }

    #endregion

    #region 图片

    private void CreateImages()
    {
        // logo1 - 初始不可见
        GameObject logo1GO = new GameObject("Logo1");
        logo1GO.transform.SetParent(transform, false);
        RectTransform l1rt = logo1GO.AddComponent<RectTransform>();
        l1rt.anchorMin = Vector2.zero;
        l1rt.anchorMax = Vector2.one;
        l1rt.offsetMin = Vector2.zero;
        l1rt.offsetMax = Vector2.zero;

        logo1Image = logo1GO.AddComponent<Image>();
        logo1Image.sprite = logo1Sprite;
        logo1Image.preserveAspect = false;
        logo1Image.color = new Color(1, 1, 1, 0);
        logo1Image.raycastTarget = false;

        // logo2 - 初始不可见
        GameObject logo2GO = new GameObject("Logo2");
        logo2GO.transform.SetParent(transform, false);
        RectTransform l2rt = logo2GO.AddComponent<RectTransform>();
        l2rt.anchorMin = Vector2.zero;
        l2rt.anchorMax = Vector2.one;
        l2rt.offsetMin = Vector2.zero;
        l2rt.offsetMax = Vector2.zero;

        logo2Image = logo2GO.AddComponent<Image>();
        logo2Image.sprite = logo2Sprite;
        logo2Image.preserveAspect = false;
        logo2Image.color = new Color(1, 1, 1, 0);
        logo2Image.raycastTarget = false;
    }

    #endregion

    #region 淡出遮罩

    private void CreateFadeOverlay()
    {
        GameObject fadeGO = new GameObject("GlobalFade");
        fadeGO.transform.SetParent(transform, false);
        RectTransform frt = fadeGO.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        Image fadeImage = fadeGO.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        globalFade = fadeGO.AddComponent<CanvasGroup>();
        globalFade.alpha = 1f;
        globalFade.blocksRaycasts = false;
    }

    #endregion

    #region 动画序列

    private System.Collections.IEnumerator AnimationSequence()
    {
        Debug.Log("[SplashScreen] 开场动画开始");

        canSkip = true;

        float prepareDeadline = Time.time + videoWaitTime;
        while (!videoPrepared && !videoFailed && Time.time < prepareDeadline)
        {
            yield return null;
        }

        if (videoPrepared && videoPlayer != null)
        {
            float playbackDeadline = Time.time + Mathf.Max((float)videoPlayer.length + 1f, videoWaitTime);
            while (!videoFinished && !videoFailed && Time.time < playbackDeadline)
            {
                yield return null;
            }

            if (!videoFinished && !videoFailed)
            {
                Debug.LogWarning("[SplashScreen] 视频播放超时，继续后续流程");
            }

            videoPlayer.Stop();
        }
        else
        {
            Debug.LogWarning("[SplashScreen] 视频未成功播放，直接进入 Logo 流程");
        }

        if (videoImage != null)
        {
            videoImage.gameObject.SetActive(false);
        }

        if (logo1Image != null)
        {
            logo1Image.color = Color.white;
        }

        yield return new WaitForSeconds(logo1DisplayTime);

        yield return StartCoroutine(CrossFadeImages(logo1Image, logo2Image));
        yield return new WaitForSeconds(logo2DisplayTime);

        yield return StartCoroutine(TransitionToNextScene());
    }

    private System.Collections.IEnumerator FadeOutVideo()
    {
        if (videoImage == null) yield break;

        float elapsed = 0f;
        Color startColor = videoImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            float easeT = EaseInOut(t);
            videoImage.color = Color.Lerp(startColor, endColor, easeT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        videoImage.color = endColor;
    }

    private System.Collections.IEnumerator FadeInImage(Image img)
    {
        if (img == null) yield break;

        float elapsed = 0f;
        Color startColor = img.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            float easeT = EaseInOut(t);
            img.color = Color.Lerp(startColor, endColor, easeT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        img.color = endColor;
    }

    private System.Collections.IEnumerator CrossFadeImages(Image fromImg, Image toImg)
    {
        if (fromImg == null || toImg == null) yield break;

        float elapsed = 0f;
        float fromAlphaStart = fromImg.color.a;
        float toAlphaStart = toImg.color.a;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            float easeT = EaseInOut(t);

            // from: 1 -> 0
            float fromAlpha = Mathf.Lerp(fromAlphaStart, 0f, easeT);
            fromImg.color = new Color(1, 1, 1, fromAlpha);

            // to: 0 -> 1
            float toAlpha = Mathf.Lerp(toAlphaStart, 1f, easeT);
            toImg.color = new Color(1, 1, 1, toAlpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        fromImg.color = new Color(1, 1, 1, 0f);
        toImg.color = new Color(1, 1, 1, 1f);
    }

    private System.Collections.IEnumerator TransitionToNextScene()
    {
        Debug.Log("[SplashScreen] 过渡到: " + nextScene);

        canSkip = false;

        // 清理 Splash 自己的显示，避免遮住 LoadingScreen
        if (logo1Image != null)
        {
            logo1Image.color = new Color(1f, 1f, 1f, 0f);
        }
        if (logo2Image != null)
        {
            logo2Image.color = new Color(1f, 1f, 1f, 0f);
        }
        if (globalFade != null)
        {
            globalFade.alpha = 0f;
        }

        yield return null;

        // 加载下一场景
        if (!string.IsNullOrEmpty(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.LogWarning("[SplashScreen] 未设置下一场景！");
        }
    }

    #endregion

    #region 跳过

    private void SkipToEnd()
    {
        if (hasSkipped) return;
        hasSkipped = true;
        canSkip = false;
        Debug.Log("[SplashScreen] 用户跳过");

        StopAllCoroutines();
        StartCoroutine(TransitionToNextScene());
    }

    #endregion

    #region 工具

    private float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    #endregion
}
