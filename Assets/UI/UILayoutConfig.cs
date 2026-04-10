using UnityEngine;
using UnityEngine.UI;
using TMPro;

[CreateAssetMenu(fileName = "UILayoutConfig", menuName = "UI/Layout Config")]
public class UILayoutConfig : ScriptableObject
{
    [Header("Canvas Settings")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    public CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    public float matchValue = 0.5f;

    [Header("Color Palette")]
    public Color primaryColor = new Color(0.2f, 0.4f, 0.8f);
    public Color secondaryColor = new Color(0.8f, 0.4f, 0.2f);
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f);
    public Color textColor = new Color(0.9f, 0.9f, 0.9f);
    public Color buttonColor = new Color(0.25f, 0.25f, 0.35f);
    public Color buttonHoverColor = new Color(0.35f, 0.35f, 0.45f);

    [Header("Font Settings (TMP)")]
    [Tooltip("标题字体（TMP SDF 字体资源），留空则自动从 FontManager 获取")]
    public TMP_FontAsset titleFontTMP;
    [Tooltip("正文字体（TMP SDF 字体资源），留空则自动从 FontManager 获取")]
    public TMP_FontAsset bodyFontTMP;
    public int titleFontSize = 72;
    public int subtitleFontSize = 36;
    public int bodyFontSize = 24;

    /// <summary>
    /// 获取标题字体，优先用配置的，否则从 FontManager 自动获取
    /// </summary>
    public TMP_FontAsset GetTitleFont()
    {
        if (titleFontTMP != null) return titleFontTMP;
        if (FontManager.Instance != null) return FontManager.Instance.ChineseFont;
        return null;
    }

    /// <summary>
    /// 获取正文字体，优先用配置的，否则从 FontManager 自动获取
    /// </summary>
    public TMP_FontAsset GetBodyFont()
    {
        if (bodyFontTMP != null) return bodyFontTMP;
        if (FontManager.Instance != null) return FontManager.Instance.ChineseFont;
        return null;
    }
    
    [Header("Spacing and Padding")]
    public float sectionSpacing = 40f;
    public float elementSpacing = 20f;
    public float panelPadding = 30f;
    
    [Header("Button Settings")]
    public Vector2 buttonSize = new Vector2(250, 60);
    public float buttonCornerRadius = 8f;
    
    [Header("Panel Settings")]
    public float panelCornerRadius = 12f;
    public float panelShadowDistance = 5f;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public float animationDelay = 0.1f;
    
    [Header("Responsive Settings")]
    public float mobileScaleFactor = 0.8f;
    public float tabletScaleFactor = 0.9f;
    
    [Header("Layout Positions")]
    public Vector2 titlePosition = new Vector2(0, 200);
    public Vector2 subtitlePosition = new Vector2(0, 120);
    public Vector2 buttonPanelPosition = new Vector2(0, -50);
    public Vector2 creditsPosition = new Vector2(0, -300);
}