using UnityEngine;
using UnityEngine.UI;

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
        if (mainMenuPanel == null || settingsPanel == null || aboutPanel == null)
        {
            Debug.LogError($"[UIManager] Missing panel references. mainMenuPanel={mainMenuPanel}, settingsPanel={settingsPanel}, aboutPanel={aboutPanel}");
            ShowUINotification("标题界面未配置完整", "主菜单面板缺少必要引用，当前标题页功能无法完整展开。");
            return;
        }

        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        aboutPanel.SetActive(false);

        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (aboutButton != null) aboutButton.onClick.AddListener(OpenAbout);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        if (versionText != null)
        {
            versionText.text = "Version 1.0.0";
        }
    }

    private void LoadSettings()
    {
        if (musicVolumeSlider == null || sfxVolumeSlider == null || fullscreenToggle == null)
        {
            ShowUINotification("设置面板未配置完整", "设置控件缺少必要引用，暂时无法正确读取和应用参数。");
            return;
        }

        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        ApplySettings();
    }

    private void SaveSettings()
    {
        if (musicVolumeSlider == null || sfxVolumeSlider == null || fullscreenToggle == null)
        {
            ShowUINotification("设置保存失败", "设置控件没有准备好，这次修改暂时没有写入。");
            return;
        }

        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySettings()
    {
        if (musicVolumeSlider == null || fullscreenToggle == null)
        {
            return;
        }

        AudioListener.volume = musicVolumeSlider.value;
        Screen.fullScreen = fullscreenToggle.isOn;
    }

    public void StartGame()
    {
        Debug.Log("开始游戏...");
        SceneLoader.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        if (mainMenuPanel == null || aboutPanel == null || settingsPanel == null)
        {
            ShowUINotification("无法打开设置", "设置面板没有成功构建，当前无法切换到设置页。");
            return;
        }

        mainMenuPanel.SetActive(false);
        aboutPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OpenAbout()
    {
        if (mainMenuPanel == null || settingsPanel == null || aboutPanel == null)
        {
            ShowUINotification("无法打开说明", "说明面板没有成功构建，当前无法切换到说明页。");
            return;
        }

        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        aboutPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        if (mainMenuPanel == null || settingsPanel == null || aboutPanel == null)
        {
            ShowUINotification("无法返回主菜单", "标题页面板缺少引用，当前切换操作没有完成。");
            return;
        }

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

    private void ShowUINotification(string title, string message, float duration = 2.8f)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, new Color(0.82f, 0.38f, 0.30f), duration);
        }
    }
}
