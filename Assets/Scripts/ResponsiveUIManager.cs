using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ResponsiveUIManager : MonoBehaviour
{
    [Header("Reference Resolution")]
    [Tooltip("The reference resolution for UI scaling")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    
    [Header("Scaling Mode")]
    [Tooltip("The scaling mode for the canvas")]
    public CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    
    [Tooltip("The screen match mode for scaling")]
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
    
    [Tooltip("The match value (0 = width, 1 = height)")]
    [Range(0f, 1f)]
    public float matchValue = 0.5f;
    
    [Header("UI Elements")]
    [Tooltip("List of UI elements that need special positioning")]
    public List<RectTransform> responsiveElements = new List<RectTransform>();
    
    [Tooltip("Original positions for responsive elements")]
    private Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();
    
    [Tooltip("Original sizes for responsive elements")]
    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();
    
    private CanvasScaler canvasScaler;
    private Vector2 currentResolution;
    
    private void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler != null)
        {
            ConfigureCanvasScaler();
        }
        
        StoreOriginalPositions();
        currentResolution = new Vector2(Screen.width, Screen.height);
    }
    
    private void Start()
    {
        ApplyResponsiveLayout();
    }
    
    private void Update()
    {
        // 检测分辨率变化
        Vector2 newResolution = new Vector2(Screen.width, Screen.height);
        if (newResolution != currentResolution)
        {
            currentResolution = newResolution;
            ApplyResponsiveLayout();
        }
    }
    
    private void ConfigureCanvasScaler()
    {
        canvasScaler.uiScaleMode = scaleMode;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = screenMatchMode;
        canvasScaler.matchWidthOrHeight = matchValue;
    }
    
    private void StoreOriginalPositions()
    {
        foreach (RectTransform element in responsiveElements)
        {
            if (element != null)
            {
                originalPositions[element] = element.anchoredPosition;
                originalSizes[element] = element.sizeDelta;
            }
        }
    }
    
    private void ApplyResponsiveLayout()
    {
        // 计算缩放比例
        float widthRatio = (float)Screen.width / referenceResolution.x;
        float heightRatio = (float)Screen.height / referenceResolution.y;
        
        // 根据匹配模式应用缩放
        float scaleFactor;
        if (screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
        {
            scaleFactor = Mathf.Lerp(widthRatio, heightRatio, matchValue);
        }
        else if (screenMatchMode == CanvasScaler.ScreenMatchMode.Expand)
        {
            scaleFactor = Mathf.Min(widthRatio, heightRatio);
        }
        else // Shrink
        {
            scaleFactor = Mathf.Max(widthRatio, heightRatio);
        }
        
        // 应用特殊定位和大小调整
        ApplySpecialLayout(widthRatio, heightRatio, scaleFactor);
    }
    
    private void ApplySpecialLayout(float widthRatio, float heightRatio, float scaleFactor)
    {
        foreach (RectTransform element in responsiveElements)
        {
            if (element != null && originalPositions.ContainsKey(element))
            {
                // 获取原始位置和大小
                Vector2 originalPos = originalPositions[element];
                Vector2 originalSize = originalSizes[element];
                
                // 根据屏幕比例调整位置
                Vector2 newPosition = new Vector2(
                    originalPos.x * widthRatio,
                    originalPos.y * heightRatio
                );
                
                // 根据缩放比例调整大小
                Vector2 newSize = originalSize * scaleFactor;
                
                // 应用新的位置和大小
                element.anchoredPosition = newPosition;
                element.sizeDelta = newSize;
            }
        }
    }
    
    [ContextMenu("Update Responsive Elements")]
    public void UpdateResponsiveElements()
    {
        StoreOriginalPositions();
        ApplyResponsiveLayout();
    }
    
    [ContextMenu("Reset to Original")]
    public void ResetToOriginal()
    {
        foreach (RectTransform element in responsiveElements)
        {
            if (element != null && originalPositions.ContainsKey(element))
            {
                element.anchoredPosition = originalPositions[element];
                element.sizeDelta = originalSizes[element];
            }
        }
    }
}