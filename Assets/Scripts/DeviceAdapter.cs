using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class DeviceAdapter : MonoBehaviour
{
    [Header("Device Profiles")]
    [Tooltip("移动端配置")]
    public DeviceProfile mobileProfile = new DeviceProfile
    {
        scaleFactor = 0.8f,
        fontSizeScale = 0.9f,
        buttonSizeScale = 0.9f,
        spacingScale = 0.8f
    };
    
    [Tooltip("平板配置")]
    public DeviceProfile tabletProfile = new DeviceProfile
    {
        scaleFactor = 0.9f,
        fontSizeScale = 0.95f,
        buttonSizeScale = 0.95f,
        spacingScale = 0.9f
    };
    
    [Tooltip("桌面配置")]
    public DeviceProfile desktopProfile = new DeviceProfile
    {
        scaleFactor = 1.0f,
        fontSizeScale = 1.0f,
        buttonSizeScale = 1.0f,
        spacingScale = 1.0f
    };
    
    [Header("Thresholds")]
    [Tooltip("移动端最大宽度阈值")]
    public int mobileWidthThreshold = 768;
    
    [Tooltip("平板最大宽度阈值")]
    public int tabletWidthThreshold = 1024;
    
    [Header("References")]
    private CanvasScaler canvasScaler;
    private DeviceType currentDeviceType;
    private DeviceProfile currentProfile;
    
    private void Awake()
    {
        canvasScaler = FindObjectOfType<CanvasScaler>();
        Initialize();
    }
    
    private void Initialize()
    {
        DetectDeviceType();
        ApplyDeviceProfile();
    }
    
    #region Device Detection
    
    private void DetectDeviceType()
    {
        int screenWidth = Screen.width;
        
        if (screenWidth<= mobileWidthThreshold)
        {
            currentDeviceType = DeviceType.Mobile;
            currentProfile = mobileProfile;
        }
        else if (screenWidth <= tabletWidthThreshold)
        {
            currentDeviceType = DeviceType.Tablet;
            currentProfile = tabletProfile;
        }
        else
        {
            currentDeviceType = DeviceType.Desktop;
            currentProfile = desktopProfile;
        }
        
        Debug.Log($"Detected device type: {currentDeviceType} (Width: {screenWidth})");
    }
    
    #endregion
    
    #region Profile Application
    
    private void ApplyDeviceProfile()
    {
        if (canvasScaler != null)
        {
            // 应用缩放因子
            canvasScaler.scaleFactor = currentProfile.scaleFactor;
        }
        
        // 调整UI元素
        AdjustUIElements();
        
        // 调整字体大小
        AdjustFontSizes();
        
        // 调整按钮大小
        AdjustButtonSizes();
        
        // 调整间距
        AdjustSpacing();
    }
    
    private void AdjustUIElements()
    {
        RectTransform[] allElements = FindObjectsOfType<RectTransform>();
        
        foreach (RectTransform element in allElements)
        {
            // 排除Canvas和EventSystem
            if (element.GetComponent<Canvas>() != null || element.GetComponent<EventSystem>() != null)
            {
                continue;
            }
            
            // 调整位置
            Vector2 newPosition = element.anchoredPosition * currentProfile.spacingScale;
            element.anchoredPosition = newPosition;
            
            // 调整大小
            Vector2 newSize = element.sizeDelta * currentProfile.buttonSizeScale;
            element.sizeDelta = newSize;
        }
    }
    
    private void AdjustFontSizes()
    {
        Text[] texts = FindObjectsOfType<Text>();
        foreach (Text text in texts)
        {
            text.fontSize = Mathf.RoundToInt(text.fontSize * currentProfile.fontSizeScale);
        }
        
        TMPro.TMP_Text[] tmpTexts = FindObjectsOfType<TMPro.TMP_Text>();
        foreach (TMPro.TMP_Text text in tmpTexts)
        {
            text.fontSize *= currentProfile.fontSizeScale;
        }
    }
    
    private void AdjustButtonSizes()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta *= currentProfile.buttonSizeScale;
            }
        }
    }
    
    private void AdjustSpacing()
    {
        // 这里可以添加额外的间距调整逻辑
        // 比如调整面板内元素的间距等
    }
    
    #endregion
    
    #region Public API
    
    public void RefreshDeviceProfile()
    {
        DetectDeviceType();
        ApplyDeviceProfile();
    }
    
    public DeviceType GetCurrentDeviceType()
    {
        return currentDeviceType;
    }
    
    public DeviceProfile GetCurrentProfile()
    {
        return currentProfile;
    }
    
    public void ForceDeviceType(DeviceType deviceType)
    {
        currentDeviceType = deviceType;
        
        switch (deviceType)
        {
            case DeviceType.Mobile:
                currentProfile = mobileProfile;
                break;
            case DeviceType.Tablet:
                currentProfile = tabletProfile;
                break;
            case DeviceType.Desktop:
                currentProfile = desktopProfile;
                break;
        }
        
        ApplyDeviceProfile();
    }
    
    #endregion
    
    #region Device Enums and Classes
    
    public enum DeviceType
    {
        Mobile,
        Tablet,
        Desktop
    }
    
    [Serializable]
    public class DeviceProfile
    {
        [Tooltip("整体缩放因子")]
        public float scaleFactor = 1.0f;
        
        [Tooltip("字体大小缩放因子")]
        public float fontSizeScale = 1.0f;
        
        [Tooltip("按钮大小缩放因子")]
        public float buttonSizeScale = 1.0f;
        
        [Tooltip("间距缩放因子")]
        public float spacingScale = 1.0f;
    }
    
    #endregion
}