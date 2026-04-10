using UnityEngine;
using TMPro;

/// <summary>
/// 全局字体管理器 - 单例模式
///
/// 【一劳永逸解决中文显示问题】
///
/// 用法1（推荐）：把中文 SDF 字体拖到 Inspector 的 chineseFontAsset 字段
/// 用法2（自动）：把中文 SDF 字体放到 Resources/Fonts/ 目录下，会自动加载第一个找到的
///
/// 之后任何地方创建 TMP 文本，只需要调用：
///   FontManager.Instance.ApplyChineseFont(textComponent);
/// 或者直接获取字体：
///   FontManager.Instance.ChineseFont;
///
/// 同时本管理器会在启动时自动配置 TMP 全局 fallback，
/// 所以即使你忘了手动调用，大部分情况中文也能正常显示。
/// </summary>
public class FontManager : MonoBehaviour
{
    public static FontManager Instance { get; private set; }

    [Header("中文字体设置")]
    [Tooltip("拖入你的中文 TMP SDF 字体资源（如华文新魏 SDF）")]
    public TMP_FontAsset chineseFontAsset;

    [Header("自动加载设置")]
    [Tooltip("如果上面没有手动指定字体，会从 Resources 的这个路径自动加载")]
    public string resourceFontPath = "Fonts";

    [Header("自动应用设置")]
    [Tooltip("启动时自动把中文字体设为 TMP 全局 fallback")]
    public bool autoSetFallback = true;

    [Tooltip("启动时自动替换场景中所有 TMP 文本的字体")]
    public bool autoApplyToScene = true;

    /// <summary>
    /// 获取中文字体资源
    /// </summary>
    public TMP_FontAsset ChineseFont => chineseFontAsset;

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 自动加载字体
        if (chineseFontAsset == null)
        {
            LoadFontFromResources();
        }

        // 自动配置 TMP 全局 fallback
        if (autoSetFallback && chineseFontAsset != null)
        {
            SetGlobalFallbackFont();
        }
    }

    private void Start()
    {
        // 自动应用到场景中已有的 TMP 文本
        if (autoApplyToScene && chineseFontAsset != null)
        {
            ApplyToAllSceneTexts();
        }
    }

    /// <summary>
    /// 从 Resources 目录自动加载中文字体
    /// </summary>
    private void LoadFontFromResources()
    {
        // 先尝试加载指定路径下的所有 TMP 字体
        TMP_FontAsset[] fonts = Resources.LoadAll<TMP_FontAsset>(resourceFontPath);
        if (fonts != null && fonts.Length > 0)
        {
            chineseFontAsset = fonts[0];
            Debug.Log($"[FontManager] 自动加载字体: {chineseFontAsset.name}");
            return;
        }

        // 再尝试从根 Resources 加载
        fonts = Resources.LoadAll<TMP_FontAsset>("");
        if (fonts != null && fonts.Length > 0)
        {
            foreach (var font in fonts)
            {
                // 优先选择非 LiberationSans 的字体（跳过 TMP 默认英文字体）
                if (!font.name.Contains("Liberation"))
                {
                    chineseFontAsset = font;
                    Debug.Log($"[FontManager] 自动加载字体: {chineseFontAsset.name}");
                    return;
                }
            }
        }

        Debug.LogWarning("[FontManager] 未找到中文字体！请将 SDF 字体资源拖到 FontManager 的 chineseFontAsset 字段，或放到 Resources/Fonts/ 目录下。");
    }

    /// <summary>
    /// 把中文字体设为 TMP 全局 fallback font
    /// 这是"一劳永逸"的关键：设置后，所有 TMP 文本遇到中文字符都会自动回退到这个字体
    /// </summary>
    private void SetGlobalFallbackFont()
    {
        if (chineseFontAsset == null) return;

        // 获取 TMP Settings
        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
        {
            Debug.LogWarning("[FontManager] 未找到 TMP Settings，请先导入 TextMesh Pro 的 Essential Resources。");
            return;
        }

        // 获取默认字体，把中文字体加入它的 fallback 列表
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
        {
            if (defaultFont.fallbackFontAssetTable == null)
            {
                defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }

            // 避免重复添加
            if (!defaultFont.fallbackFontAssetTable.Contains(chineseFontAsset))
            {
                defaultFont.fallbackFontAssetTable.Add(chineseFontAsset);
                Debug.Log($"[FontManager] 已将 {chineseFontAsset.name} 设为 TMP 默认字体的 fallback");
            }
        }

        // 同时也设置 TMP Settings 级别的全局 fallback
        // 通过反射设置（因为 TMP_Settings 的 fallbackFontAssets 是内部字段）
        var fallbackField = typeof(TMP_Settings).GetField("m_fallbackFontAssets",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fallbackField != null && settings != null)
        {
            var fallbackList = fallbackField.GetValue(settings) as System.Collections.Generic.List<TMP_FontAsset>;
            if (fallbackList == null)
            {
                fallbackList = new System.Collections.Generic.List<TMP_FontAsset>();
            }
            if (!fallbackList.Contains(chineseFontAsset))
            {
                fallbackList.Add(chineseFontAsset);
                fallbackField.SetValue(settings, fallbackList);
                Debug.Log($"[FontManager] 已将 {chineseFontAsset.name} 设为 TMP Settings 全局 fallback");
            }
        }
    }

    /// <summary>
    /// 给单个 TMP 文本组件应用中文字体
    /// </summary>
    public void ApplyChineseFont(TMP_Text textComponent)
    {
        if (textComponent == null || chineseFontAsset == null) return;
        textComponent.font = chineseFontAsset;
    }

    /// <summary>
    /// 给单个 TMP 文本组件设置中文字体为 fallback（不替换主字体）
    /// 适合想保留英文字体样式、但需要中文兜底的场景
    /// </summary>
    public void AddChineseFallback(TMP_Text textComponent)
    {
        if (textComponent == null || chineseFontAsset == null) return;

        if (textComponent.font != null)
        {
            if (textComponent.font.fallbackFontAssetTable == null)
            {
                textComponent.font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }
            if (!textComponent.font.fallbackFontAssetTable.Contains(chineseFontAsset))
            {
                textComponent.font.fallbackFontAssetTable.Add(chineseFontAsset);
            }
        }
    }

    /// <summary>
    /// 遍历场景中所有 TMP 文本，自动应用中文字体
    /// </summary>
    public void ApplyToAllSceneTexts()
    {
        if (chineseFontAsset == null) return;

        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text text in allTexts)
        {
            // 如果当前文本使用的是 TMP 默认字体（LiberationSans），替换为中文字体
            if (text.font == null || text.font.name.Contains("Liberation"))
            {
                text.font = chineseFontAsset;
            }
        }
        Debug.Log($"[FontManager] 已对场景中 {allTexts.Length} 个 TMP 文本应用中文字体");
    }

    /// <summary>
    /// 创建一个已经配好中文字体的 TMP 文本组件
    /// 以后新建界面时直接调用这个方法就行
    /// </summary>
    public TextMeshProUGUI CreateChineseText(Transform parent, string text, float fontSize = 24f)
    {
        GameObject textObj = new GameObject("Text_" + text, typeof(RectTransform), typeof(CanvasRenderer));
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;

        if (chineseFontAsset != null)
        {
            tmp.font = chineseFontAsset;
        }

        return tmp;
    }
}
