using System;
using UnityEngine;

/// <summary>
/// 设置管理器 - 单例
/// 负责设置数据的保存、加载、应用
/// 提供事件通知机制供其他系统订阅
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // ========== 单例 ==========
    public static SettingsManager Instance { get; private set; }

    // ========== 当前设置数据 ==========
    public SettingsData CurrentSettings { get; private set; }

    // ========== 事件 ==========
    public event Action<SettingsData> OnSettingsChanged;
    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;
    public event Action<float> OnUIScaleChanged;

    // ========== PlayerPrefs 键名常量 ==========
    private const string KEY_PREFIX = "Settings_";
    private const string KEY_MASTER_VOLUME = KEY_PREFIX + "MasterVolume";
    private const string KEY_MUSIC_VOLUME = KEY_PREFIX + "MusicVolume";
    private const string KEY_SFX_VOLUME = KEY_PREFIX + "SFXVolume";
    private const string KEY_IS_MUTED = KEY_PREFIX + "IsMuted";
    private const string KEY_FULLSCREEN = KEY_PREFIX + "Fullscreen";
    private const string KEY_RESOLUTION_WIDTH = KEY_PREFIX + "ResolutionWidth";
    private const string KEY_RESOLUTION_HEIGHT = KEY_PREFIX + "ResolutionHeight";
    private const string KEY_UI_SCALE = KEY_PREFIX + "UIScale";
    private const string KEY_TEXT_SPEED = KEY_PREFIX + "TextSpeed";
    private const string KEY_LANGUAGE = KEY_PREFIX + "Language";

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadSettings();
        ApplyAllSettings();
    }

    private void Update()
    {
        // F1 快捷键打开设置面板
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // 避免在对话/事件期间打开
            if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;
            if (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting) return;

            SettingsUIBuilder.ShowSettings(false); // false = 游戏内模式
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== 公共 API ==========

    /// <summary>
    /// 保存设置到 PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        if (CurrentSettings == null) return;

        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, CurrentSettings.masterVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, CurrentSettings.musicVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, CurrentSettings.sfxVolume);
        PlayerPrefs.SetInt(KEY_IS_MUTED, CurrentSettings.isMuted ? 1 : 0);

        PlayerPrefs.SetInt(KEY_FULLSCREEN, CurrentSettings.fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RESOLUTION_WIDTH, CurrentSettings.resolutionWidth);
        PlayerPrefs.SetInt(KEY_RESOLUTION_HEIGHT, CurrentSettings.resolutionHeight);
        PlayerPrefs.SetFloat(KEY_UI_SCALE, CurrentSettings.uiScale);

        PlayerPrefs.SetInt(KEY_TEXT_SPEED, CurrentSettings.textSpeed);
        PlayerPrefs.SetInt(KEY_LANGUAGE, CurrentSettings.language);

        PlayerPrefs.Save();
        Debug.Log("[SettingsManager] 设置已保存");
    }

    /// <summary>
    /// 从 PlayerPrefs 加载设置
    /// </summary>
    public void LoadSettings()
    {
        CurrentSettings = new SettingsData();

        // 加载或使用默认值
        CurrentSettings.masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1.0f);
        CurrentSettings.musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 0.7f);
        CurrentSettings.sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 0.8f);
        CurrentSettings.isMuted = PlayerPrefs.GetInt(KEY_IS_MUTED, 0) == 1;

        CurrentSettings.fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
        CurrentSettings.resolutionWidth = PlayerPrefs.GetInt(KEY_RESOLUTION_WIDTH, Screen.currentResolution.width);
        CurrentSettings.resolutionHeight = PlayerPrefs.GetInt(KEY_RESOLUTION_HEIGHT, Screen.currentResolution.height);
        CurrentSettings.uiScale = PlayerPrefs.GetFloat(KEY_UI_SCALE, 1.0f);

        CurrentSettings.textSpeed = PlayerPrefs.GetInt(KEY_TEXT_SPEED, 1);
        CurrentSettings.language = PlayerPrefs.GetInt(KEY_LANGUAGE, 0);

        Debug.Log("[SettingsManager] 设置已加载");
    }

    /// <summary>
    /// 应用所有设置
    /// </summary>
    public void ApplyAllSettings()
    {
        if (CurrentSettings == null) return;

        ApplyAudioSettings();
        ApplyDisplaySettings();
        ApplyGameplaySettings();

        OnSettingsChanged?.Invoke(CurrentSettings);
    }

    /// <summary>
    /// 恢复默认设置
    /// </summary>
    public void ResetToDefaults()
    {
        if (CurrentSettings == null)
            CurrentSettings = new SettingsData();

        CurrentSettings.ResetToDefaults();
        SaveSettings();
        ApplyAllSettings();

        Debug.Log("[SettingsManager] 已恢复默认设置");
    }

    // ========== 分类应用方法 ==========

    /// <summary>
    /// 应用音频设置
    /// </summary>
    private void ApplyAudioSettings()
    {
        // 应用主音量到 AudioListener
        AudioListener.volume = CurrentSettings.GetEffectiveMusicVolume();

        // 触发事件（供其他系统订阅）
        OnMusicVolumeChanged?.Invoke(CurrentSettings.GetEffectiveMusicVolume());
        OnSFXVolumeChanged?.Invoke(CurrentSettings.GetEffectiveSFXVolume());
    }

    /// <summary>
    /// 应用显示设置
    /// </summary>
    private void ApplyDisplaySettings()
    {
        // 应用全屏模式
        Screen.fullScreen = CurrentSettings.fullscreen;

        // 应用分辨率
        Screen.SetResolution(
            CurrentSettings.resolutionWidth,
            CurrentSettings.resolutionHeight,
            CurrentSettings.fullscreen
        );

        // 触发 UI 缩放事件
        OnUIScaleChanged?.Invoke(CurrentSettings.uiScale);
    }

    /// <summary>
    /// 应用游戏性设置
    /// </summary>
    private void ApplyGameplaySettings()
    {
        // 应用文本速度到对话系统
        if (DialogueSystem.Instance != null)
        {
            float speed = CurrentSettings.textSpeed switch
            {
                0 => 0.05f,  // 慢速
                1 => 0.03f,  // 中速
                2 => 0.01f,  // 快速
                _ => 0.03f
            };
            DialogueSystem.Instance.SetTextSpeed(speed);
        }
    }
}
