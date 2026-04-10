using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject aboutPanel;
    
    [Header("Buttons")]
    public Button startGameButton;
    public Button settingsButton;
    public Button aboutButton;
    public Button quitButton;
    
    [Header("Settings Elements")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    
    [Header("About Elements")]
    public Text versionText;
    
    private void Awake()
    {
        InitializeUI();
        LoadSettings();
    }
    
    private void InitializeUI()
    {
        // 设置初始面板状态
        if (mainMenuPanel == null || settingsPanel == null || aboutPanel == null)
        {
            Debug.LogError($"[UIManager] 面板引用未赋值! mainMenuPanel={mainMenuPanel}, settingsPanel={settingsPanel}, aboutPanel={aboutPanel}，请在Inspector中设置。");
            return;
        }

        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        aboutPanel.SetActive(false);

        // 注册按钮事件
        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (aboutButton != null) aboutButton.onClick.AddListener(OpenAbout);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        // 设置版本信息
        if (versionText != null)
        {
            versionText.text = "Version 1.0.0";
        }
    }
    
    private void LoadSettings()
    {
        // 从PlayerPrefs加载设置
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        
        // 应用设置
        ApplySettings();
    }
    
    private void SaveSettings()
    {
        // 保存设置到PlayerPrefs
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    private void ApplySettings()
    {
        // 应用音量设置
        AudioListener.volume = musicVolumeSlider.value;
        
        // 应用全屏设置
        Screen.fullScreen = fullscreenToggle.isOn;
    }
    
    public void StartGame()
    {
        Debug.Log("开始游戏...");
        // 通过加载界面过渡到游戏场景（异步加载，不会卡顿）
        SceneLoader.LoadScene("GameScene");
    }
    
    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        aboutPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    public void OpenAbout()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        aboutPanel.SetActive(true);
    }
    
    public void BackToMainMenu()
    {
        settingsPanel.SetActive(false);
        aboutPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
    public void QuitGame()
    {
        Debug.Log("退出游戏...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // 设置变更事件处理
    public void OnMusicVolumeChanged()
    {
        ApplySettings();
        SaveSettings();
    }
    
    public void OnSFXVolumeChanged()
    {
        SaveSettings();
    }
    
    public void OnFullscreenChanged()
    {
        ApplySettings();
        SaveSettings();
    }
}