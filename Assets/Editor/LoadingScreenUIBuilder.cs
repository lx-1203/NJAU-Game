using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 编辑器工具：一键搭建 LoadingScreen 场景的 UI 结构
/// 菜单路径：Tools → 搭建加载界面 UI
/// </summary>
public class LoadingScreenUIBuilder
{
    [MenuItem("Tools/搭建加载界面 UI")]
    public static void BuildLoadingScreenUI()
    {
        // ========== 1. 打开 LoadingScreen 场景 ==========
        string scenePath = "Assets/Scenes/LoadingScreen.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 清理场景中已有的物体
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(go);
        }

        // ========== 2. 创建 Camera ==========
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.tag = "MainCamera";

        // ========== 3. 创建 EventSystem ==========
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        // ========== 4. 创建 Canvas (LoadingScreenManager) ==========
        GameObject canvasObj = new GameObject("LoadingScreenManager");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // 添加 LoadingScreenManager 脚本
        LoadingScreenManager manager = canvasObj.AddComponent<LoadingScreenManager>();

        // ========== 5. 创建 LoadingScreenUI 子物体 ==========
        GameObject uiRoot = new GameObject("LoadingScreenUI");
        uiRoot.transform.SetParent(canvasObj.transform, false);

        // 设置为全屏 RectTransform
        RectTransform uiRootRect = uiRoot.AddComponent<RectTransform>();
        SetStretchAll(uiRootRect);

        // 添加 CanvasGroup（用于淡入淡出）
        CanvasGroup rootCanvasGroup = uiRoot.AddComponent<CanvasGroup>();

        // 添加 LoadingScreenUI 脚本
        LoadingScreenUI uiScript = uiRoot.AddComponent<LoadingScreenUI>();

        // ========== 6. 创建 Background ==========
        GameObject bgObj = CreateUIElement("Background", uiRoot.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.95f, 0.96f, 0.94f); // 浅米白色，接近白色但柔和
        SetStretchAll(bgObj.GetComponent<RectTransform>());

        // ========== 7. 创建 Logo（左上角）==========
        GameObject logoObj = CreateUIElement("Logo", uiRoot.transform);
        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0, 1);
        logoRect.anchorMax = new Vector2(0, 1);
        logoRect.pivot = new Vector2(0, 1);
        logoRect.anchoredPosition = new Vector2(60, -40);
        logoRect.sizeDelta = new Vector2(400, 80);

        TextMeshProUGUI logoText = logoObj.AddComponent<TextMeshProUGUI>();
        logoText.text = "钟山下";
        logoText.fontSize = 48;
        logoText.fontStyle = FontStyles.Bold;
        logoText.color = new Color(0.05f, 0.35f, 0.30f, 1f); // 深绿色，匹配背景图主题
        logoText.alignment = TextAlignmentOptions.Left;

        // ========== 8. 创建 VersionText（右上角）==========
        GameObject versionObj = CreateUIElement("VersionText", uiRoot.transform);
        RectTransform versionRect = versionObj.GetComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(1, 1);
        versionRect.anchorMax = new Vector2(1, 1);
        versionRect.pivot = new Vector2(1, 1);
        versionRect.anchoredPosition = new Vector2(-60, -40);
        versionRect.sizeDelta = new Vector2(200, 60);

        TextMeshProUGUI versionText = versionObj.AddComponent<TextMeshProUGUI>();
        versionText.text = "v1.0";
        versionText.fontSize = 28;
        versionText.color = new Color(0.05f, 0.35f, 0.30f, 0.5f); // 深绿色半透明
        versionText.alignment = TextAlignmentOptions.Right;

        // ========== 9. 创建 LoadingTip（进度条正上方，居中偏左）==========
        GameObject tipObj = CreateUIElement("LoadingTip", uiRoot.transform);
        RectTransform tipRect = tipObj.GetComponent<RectTransform>();
        // 锚定在底部中央，紧贴进度条上方
        tipRect.anchorMin = new Vector2(0.5f, 0);
        tipRect.anchorMax = new Vector2(0.5f, 0);
        tipRect.pivot = new Vector2(1f, 0);  // 右对齐 pivot，让文字靠近中线左侧
        tipRect.anchoredPosition = new Vector2(-5, 198);  // 进度条(192px)上方留6px间隙
        tipRect.sizeDelta = new Vector2(400, 40);

        TextMeshProUGUI tipText = tipObj.AddComponent<TextMeshProUGUI>();
        tipText.text = "加载中...";
        tipText.fontSize = 24;
        tipText.color = new Color(0.1f, 0.4f, 0.35f, 0.85f); // 深绿色
        tipText.alignment = TextAlignmentOptions.Right;  // 文字靠右，贴近中线

        // ========== 10. 创建 ProgressBar（全宽底部条，高度匹配图片原始比例）==========
        GameObject progressBarObj = CreateUIElement("ProgressBar", uiRoot.transform);
        RectTransform progressBarRect = progressBarObj.GetComponent<RectTransform>();
        // 左右完全铺满屏幕，贴底放置
        progressBarRect.anchorMin = new Vector2(0, 0);
        progressBarRect.anchorMax = new Vector2(1, 0);
        progressBarRect.pivot = new Vector2(0.5f, 0);
        progressBarRect.anchoredPosition = new Vector2(0, 0);  // 贴底
        progressBarRect.sizeDelta = new Vector2(0, 192);  // 匹配图片原始高度 1920x192

        Image progressBarBg = progressBarObj.AddComponent<Image>();
        progressBarBg.color = new Color(0.05f, 0.35f, 0.30f, 0.3f); // 深绿色半透明底

        // ========== 11. 创建 ProgressBarFill（渐变覆盖方式，完全对齐背景）==========
        GameObject fillObj = CreateUIElement("ProgressBarFill", progressBarObj.transform);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        SetStretchAll(fillRect);  // 完全铺满父级，无内边距

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.white;
        // 使用 Simple 类型，渐变由 Shader 控制
        fillImage.type = Image.Type.Simple;
        fillImage.preserveAspect = false;
        // 注意：不在这里设置 sprite，由 Shader 的 _MainTex 直接采样纹理
        // 这样避免 Image 组件的 PerRendererData 覆盖 Shader 的纹理设置

        // ========== 12. 创建 PercentText（进度条正上方，居中偏右，与 LoadingTip 同行）==========
        GameObject percentObj = CreateUIElement("PercentText", uiRoot.transform);
        RectTransform percentRect = percentObj.GetComponent<RectTransform>();
        // 锚定在底部中央，和 LoadingTip 对称
        percentRect.anchorMin = new Vector2(0.5f, 0);
        percentRect.anchorMax = new Vector2(0.5f, 0);
        percentRect.pivot = new Vector2(0f, 0);  // 左对齐 pivot，让文字靠近中线右侧
        percentRect.anchoredPosition = new Vector2(5, 198);  // 与 LoadingTip 同高
        percentRect.sizeDelta = new Vector2(200, 40);

        TextMeshProUGUI percentText = percentObj.AddComponent<TextMeshProUGUI>();
        percentText.text = "50%";
        percentText.fontSize = 24;
        percentText.fontStyle = FontStyles.Bold;
        percentText.color = new Color(0.05f, 0.35f, 0.30f, 0.9f);
        percentText.alignment = TextAlignmentOptions.Left;  // 文字靠左，贴近中线

        // ========== 13. 创建 GameTip（进度条上方第二行，居中）==========
        GameObject gameTipObj = CreateUIElement("GameTip", uiRoot.transform);
        RectTransform gameTipRect = gameTipObj.GetComponent<RectTransform>();
        gameTipRect.anchorMin = new Vector2(0.5f, 0);
        gameTipRect.anchorMax = new Vector2(0.5f, 0);
        gameTipRect.pivot = new Vector2(0.5f, 0);
        gameTipRect.anchoredPosition = new Vector2(0, 238);  // 在"加载中... 50%"上方
        gameTipRect.sizeDelta = new Vector2(800, 40);

        TextMeshProUGUI gameTipText = gameTipObj.AddComponent<TextMeshProUGUI>();
        gameTipText.text = "小贴士：合理安排时间，学习娱乐两不误！";
        gameTipText.fontSize = 22;
        gameTipText.color = new Color(0.1f, 0.4f, 0.35f, 0.7f); // 深绿色
        gameTipText.alignment = TextAlignmentOptions.Center;  // 居中对齐

        // ========== 14. 创建 SkipButton（右下角，进度条内部）==========
        GameObject skipBtnObj = CreateUIElement("SkipButton", uiRoot.transform);
        RectTransform skipBtnRect = skipBtnObj.GetComponent<RectTransform>();
        skipBtnRect.anchorMin = new Vector2(1, 0);
        skipBtnRect.anchorMax = new Vector2(1, 0);
        skipBtnRect.pivot = new Vector2(1, 0);
        skipBtnRect.anchoredPosition = new Vector2(-20, 10);  // 贴底右侧
        skipBtnRect.sizeDelta = new Vector2(180, 45);

        CanvasGroup skipBtnCanvasGroup = skipBtnObj.AddComponent<CanvasGroup>();
        skipBtnCanvasGroup.alpha = 0.4f;

        Image skipBtnBg = skipBtnObj.AddComponent<Image>();
        skipBtnBg.color = new Color(0.05f, 0.49f, 0.42f, 0.8f); // 主题绿色按钮背景

        Button skipButton = skipBtnObj.AddComponent<Button>();
        ColorBlock colors = skipButton.colors;
        colors.highlightedColor = new Color(0.08f, 0.55f, 0.48f, 1f);
        colors.pressedColor = new Color(0.03f, 0.30f, 0.25f, 1f);
        colors.disabledColor = new Color(0.05f, 0.35f, 0.30f, 0.3f);
        skipButton.colors = colors;

        // 跳过按钮文字
        GameObject skipTextObj = CreateUIElement("Text", skipBtnObj.transform);
        RectTransform skipTextRect = skipTextObj.GetComponent<RectTransform>();
        SetStretchAll(skipTextRect);

        TextMeshProUGUI skipBtnText = skipTextObj.AddComponent<TextMeshProUGUI>();
        skipBtnText.text = "点击跳过 >>>";
        skipBtnText.fontSize = 24;
        skipBtnText.color = Color.white;
        skipBtnText.alignment = TextAlignmentOptions.Center;

        // ========== 15. 自动查找并绑定资源 ==========
        // 查找字体
        string[] fontGuids = AssetDatabase.FindAssets("STXINWEI SDF t:TMP_FontAsset");
        TMP_FontAsset fontAsset = null;
        if (fontGuids.Length > 0)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        }

        // 查找图片
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/GameBackground.png");
        Sprite progressBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/LoadingProgressBar.png");
        Sprite progressFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Progress bar filling.png");

        // 查找 UILayoutConfig
        string[] configGuids = AssetDatabase.FindAssets("t:UILayoutConfig");
        UILayoutConfig layoutConfig = null;
        if (configGuids.Length > 0)
        {
            string configPath = AssetDatabase.GUIDToAssetPath(configGuids[0]);
            layoutConfig = AssetDatabase.LoadAssetAtPath<UILayoutConfig>(configPath);
        }

        // 查找或创建渐变进度条材质
        string gradientMatPath = "Assets/Materials/ProgressBarGradient.mat";
        Material gradientMat = AssetDatabase.LoadAssetAtPath<Material>(gradientMatPath);
        if (gradientMat == null)
        {
            // 自动创建材质
            Shader gradientShader = Shader.Find("UI/ProgressBarGradient");
            if (gradientShader != null)
            {
                // 确保 Materials 文件夹存在
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                gradientMat = new Material(gradientShader);
                gradientMat.SetFloat("_Progress", 0.5f);
                gradientMat.SetFloat("_EdgeSoftness", 0.05f);
                // 将填充图片纹理设置到材质上
                if (progressFillSprite != null)
                {
                    gradientMat.SetTexture("_MainTex", progressFillSprite.texture);
                }
                AssetDatabase.CreateAsset(gradientMat, gradientMatPath);
                Debug.Log("[LoadingScreen] 已自动创建渐变进度条材质: " + gradientMatPath);
            }
            else
            {
                Debug.LogWarning("[LoadingScreen] 未找到 UI/ProgressBarGradient Shader！请确认 Shader 文件存在。");
            }
        }
        else
        {
            // 材质已存在，更新填充纹理
            if (progressFillSprite != null)
            {
                gradientMat.SetTexture("_MainTex", progressFillSprite.texture);
                EditorUtility.SetDirty(gradientMat);
            }
        }

        // ========== 16. 绑定所有引用到 LoadingScreenUI ==========
        uiScript.canvas = canvas;
        uiScript.canvasScaler = scaler;
        uiScript.graphicRaycaster = raycaster;
        uiScript.layoutConfig = layoutConfig;
        uiScript.chineseFontAsset = fontAsset;
        uiScript.backgroundSprite = bgSprite;
        uiScript.progressBarBackgroundSprite = progressBgSprite;
        uiScript.progressBarFillSprite = progressFillSprite;
        uiScript.progressBarGradientMaterial = gradientMat;

        // UI 元素引用
        uiScript.rootCanvasGroup = rootCanvasGroup;
        uiScript.logoText = logoText;
        uiScript.versionText = versionText;
        uiScript.tipText = tipText;
        uiScript.gameTipText = gameTipText;
        uiScript.percentText = percentText;
        uiScript.skipButton = skipButton;
        uiScript.skipButtonCanvasGroup = skipBtnCanvasGroup;
        uiScript.skipButtonText = skipBtnText;
        uiScript.backgroundImage = bgImage;
        uiScript.progressBarBackground = progressBarBg;
        uiScript.progressBarFill = fillImage;
        uiScript.tipRect = tipRect;

        // 绑定 LoadingScreenManager
        manager.loadingScreenUI = uiScript;
        manager.layoutConfig = layoutConfig;

        // 如果找到字体，给所有文字设置字体
        if (fontAsset != null)
        {
            logoText.font = fontAsset;
            tipText.font = fontAsset;
            gameTipText.font = fontAsset;
            percentText.font = fontAsset;
            skipBtnText.font = fontAsset;
        }

        // 如果找到背景图，设置到 Image 上
        if (bgSprite != null)
        {
            bgImage.sprite = bgSprite;
            bgImage.color = Color.white;
        }

        if (progressBgSprite != null)
        {
            progressBarBg.sprite = progressBgSprite;
            progressBarBg.color = Color.white;
        }

        if (progressFillSprite != null)
        {
            // 不设置 fillImage.sprite，纹理通过材质的 _MainTex 传递给 Shader
            // fillImage 保持无 sprite，仅作为渲染载体
            fillImage.color = Color.white;
            Debug.Log("[LoadingScreen] 进度条填充纹理已设置到渐变材质上");
        }

        // 将渐变材质直接应用到 Fill Image 上（编辑器预览用）
        if (gradientMat != null)
        {
            fillImage.material = gradientMat;
        }

        // ========== 16.5 创建纯 UI 粒子效果 ==========
        // 粒子效果直接作为 ProgressBar 的子物体，使用 UI Image 模拟粒子
        // 100% 兼容 Screen Space Overlay Canvas
        GameObject particleObj = CreateUIElement("ProgressBarParticles", progressBarObj.transform);
        RectTransform particleRect = particleObj.GetComponent<RectTransform>();
        SetStretchAll(particleRect);  // 铺满进度条区域

        ProgressBarParticleEffect particleEffect = particleObj.AddComponent<ProgressBarParticleEffect>();
        particleEffect.progressBarRect = progressBarRect;
        particleEffect.progressBarFill = fillImage;
        particleEffect.enableParticles = true;
        particleEffect.enableGlow = true;
        particleEffect.enableTrail = true;

        // 绑定粒子效果到 LoadingScreenUI
        uiScript.particleEffect = particleEffect;

        // ========== 17. 标记脏数据并保存 ==========
        EditorUtility.SetDirty(uiScript);
        EditorUtility.SetDirty(manager);
        bool saved = EditorSceneManager.SaveScene(scene, scenePath);

        if (saved)
        {
            Debug.Log("========================================");
            Debug.Log("[LoadingScreen] UI 搭建完成并已保存！");
            Debug.Log($"  字体: {(fontAsset != null ? fontAsset.name : "未找到")}");
            Debug.Log($"  背景图: {(bgSprite != null ? "已设置" : "未找到")}");
            Debug.Log($"  进度条背景: {(progressBgSprite != null ? "已设置" : "未找到")}");
            Debug.Log($"  进度条填充: {(progressFillSprite != null ? "已设置" : "未找到")}");
            Debug.Log($"  布局配置: {(layoutConfig != null ? "已设置" : "未找到")}");
            Debug.Log("========================================");
            Debug.Log("现在你可以在 Scene 视图中自由拖拽调整每个 UI 元素的位置和大小了！");

            Selection.activeGameObject = uiRoot;
        }
        else
        {
            Debug.LogError("[LoadingScreen] 场景保存失败！");
        }
    }

    /// <summary>
    /// 创建带 RectTransform 的 UI 元素
    /// </summary>
    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    /// <summary>
    /// 设置 RectTransform 为四角拉伸（铺满父物体）
    /// </summary>
    private static void SetStretchAll(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
