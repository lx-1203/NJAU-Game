using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class UISceneGenerator : MonoBehaviour
{
    [Header("UI Config")]
    public UILayoutConfig layoutConfig;
    
    [Header("Prefabs")]
    public GameObject buttonPrefab;
    public GameObject panelPrefab;
    public GameObject textPrefab;
    
    [Header("References")]
    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private UIManager uiManager;
    private UIAnimator uiAnimator;
    
    private void Awake()
    {
        CreateCanvas();
        CreateUIElements();
        SetupEventHandlers();
    }
    
    private void CreateCanvas()
    {
        // 创建Canvas对象
        GameObject canvasObject = new GameObject("MainCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // 添加CanvasScaler
        canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = layoutConfig.scaleMode;
        canvasScaler.referenceResolution = layoutConfig.referenceResolution;
        canvasScaler.screenMatchMode = layoutConfig.screenMatchMode;
        canvasScaler.matchWidthOrHeight = layoutConfig.matchValue;
        
        // 添加GraphicRaycaster
        canvasObject.AddComponent<GraphicRaycaster>();
        
        // 添加EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
    
    private void CreateUIElements()
    {
        // 创建主菜单面板
        GameObject mainMenuPanel = CreatePanel("MainMenuPanel", Vector2.zero, new Vector2(800, 600));
        
        // 创建标题
        TextMeshProUGUI title = CreateText("GameTitle", layoutConfig.titlePosition, layoutConfig.titleFontSize, "游戏名称");
        title.fontStyle = FontStyles.Bold;
        title.color = layoutConfig.textColor;
        
        // 创建副标题
        TextMeshProUGUI subtitle = CreateText("GameSubtitle", layoutConfig.subtitlePosition, layoutConfig.subtitleFontSize, "一个精彩的2D游戏");
        subtitle.color = layoutConfig.textColor * 0.8f;
        
        // 创建按钮面板
        GameObject buttonPanel = CreatePanel("ButtonPanel", layoutConfig.buttonPanelPosition, new Vector2(300, 300));
        
        // 创建按钮
        Button startButton = CreateButton("StartButton", new Vector2(0, 80), "开始游戏");
        Button settingsButton = CreateButton("SettingsButton", new Vector2(0, 0), "设置");
        Button aboutButton = CreateButton("AboutButton", new Vector2(0, -80), "关于");
        Button quitButton = CreateButton("QuitButton", new Vector2(0, -160), "退出");
        
        // 创建版权信息
        TextMeshProUGUI credits = CreateText("Credits", layoutConfig.creditsPosition, 16, "© 2024 游戏工作室");
        credits.color = layoutConfig.textColor * 0.6f;
        
        // 创建设置面板
        GameObject settingsPanel = CreatePanel("SettingsPanel", Vector2.zero, new Vector2(600, 400));
        settingsPanel.SetActive(false);
        
        // 创建关于面板
        GameObject aboutPanel = CreatePanel("AboutPanel", Vector2.zero, new Vector2(600, 400));
        aboutPanel.SetActive(false);
        
        // 创建UIManager
        GameObject uiManagerObject = new GameObject("UIManager");
        uiManager = uiManagerObject.AddComponent<UIManager>();
        
        // 设置UIManager引用
        uiManager.mainMenuPanel = mainMenuPanel;
        uiManager.settingsPanel = settingsPanel;
        uiManager.aboutPanel = aboutPanel;
        uiManager.startGameButton = startButton;
        uiManager.settingsButton = settingsButton;
        uiManager.aboutButton = aboutButton;
        uiManager.quitButton = quitButton;
        
        // 创建UIAnimator
        GameObject animatorObject = new GameObject("UIAnimator");
        uiAnimator = animatorObject.AddComponent<UIAnimator>();
    }
    
    private GameObject CreatePanel(string name, Vector2 position, Vector2 size)
    {
        GameObject panel = Instantiate(panelPrefab, canvas.transform);
        panel.name = name;
        
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        
        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.color = layoutConfig.backgroundColor * 0.9f;
            image.type = Image.Type.Sliced;
        }
        
        return panel;
    }
    
    private Button CreateButton(string name, Vector2 position, string text)
    {
        GameObject buttonObject = Instantiate(buttonPrefab, canvas.transform);
        buttonObject.name = name;
        
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = layoutConfig.buttonSize;
        
        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            Image image = buttonObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = layoutConfig.buttonColor;
            }
            
            // 设置按钮文本
            TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = text;
                buttonText.fontSize = layoutConfig.bodyFontSize;
                buttonText.color = layoutConfig.textColor;

                // 自动应用中文字体
                TMP_FontAsset font = layoutConfig != null ? layoutConfig.GetBodyFont() : null;
                if (font != null)
                {
                    buttonText.font = font;
                }
                else if (FontManager.Instance != null)
                {
                    FontManager.Instance.ApplyChineseFont(buttonText);
                }
            }
        }
        
        return button;
    }
    
    private TextMeshProUGUI CreateText(string name, Vector2 position, int fontSize, string text)
    {
        GameObject textObject = Instantiate(textPrefab, canvas.transform);
        textObject.name = name;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;

            // 自动应用中文字体
            TMP_FontAsset font = layoutConfig != null ? layoutConfig.GetBodyFont() : null;
            if (font != null)
            {
                textComponent.font = font;
            }
            else if (FontManager.Instance != null)
            {
                FontManager.Instance.ApplyChineseFont(textComponent);
            }
        }

        return textComponent;
    }
    
    private void SetupEventHandlers()
    {
        // 设置按钮事件
        if (uiManager != null)
        {
            uiManager.startGameButton.onClick.AddListener(OnStartGame);
            uiManager.settingsButton.onClick.AddListener(OnSettings);
            uiManager.aboutButton.onClick.AddListener(OnAbout);
            uiManager.quitButton.onClick.AddListener(OnQuit);
        }
        
        // 设置动画效果
        if (uiAnimator != null)
        {
            // 在Start方法中播放入场动画
            StartCoroutine(PlayIntroAnimation());
        }
    }
    
    private void OnStartGame()
    {
        Debug.Log("开始游戏");
        // 这里应该加载游戏场景
    }
    
    private void OnSettings()
    {
        Debug.Log("打开设置");
        uiManager.OpenSettings();
    }
    
    private void OnAbout()
    {
        Debug.Log("打开关于");
        uiManager.OpenAbout();
    }
    
    private void OnQuit()
    {
        Debug.Log("退出游戏");
        uiManager.QuitGame();
    }
    
    private System.Collections.IEnumerator PlayIntroAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 播放UI元素的入场动画
        if (uiAnimator != null)
        {
            // 获取所有UI元素并按顺序动画
            RectTransform[] elements = canvas.GetComponentsInChildren<RectTransform>();
            foreach (RectTransform element in elements)
            {
                if (element != canvas.transform as RectTransform)
                {
                    // 从下方滑入
                    Vector2 startPos = element.anchoredPosition + new Vector2(0, -200);
                    element.anchoredPosition = startPos;
                    uiAnimator.MoveIn(element, startPos, element.anchoredPosition, 0.5f);
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }
}