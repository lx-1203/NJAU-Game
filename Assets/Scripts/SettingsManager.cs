using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置管理器，负责设置数据的加载、保存和应用。
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public SettingsData CurrentSettings { get; private set; }

    public event Action<SettingsData> OnSettingsChanged;
    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;
    public event Action<float> OnUIScaleChanged;

    private const string KeyPrefix = "Settings_";
    private const string KeyMasterVolume = KeyPrefix + "MasterVolume";
    private const string KeyMusicVolume = KeyPrefix + "MusicVolume";
    private const string KeySfxVolume = KeyPrefix + "SFXVolume";
    private const string KeyIsMuted = KeyPrefix + "IsMuted";
    private const string KeyFullscreen = KeyPrefix + "Fullscreen";
    private const string KeyResolutionWidth = KeyPrefix + "ResolutionWidth";
    private const string KeyResolutionHeight = KeyPrefix + "ResolutionHeight";
    private const string KeyUiScale = KeyPrefix + "UIScale";
    private const string KeyTextSpeed = KeyPrefix + "TextSpeed";
    private const string KeyLanguage = KeyPrefix + "Language";
    private const string KeyAutoPlayInterval = KeyPrefix + "AutoPlayInterval";
    private const string KeySkipMode = KeyPrefix + "SkipMode";
    private const string KeyFastForwardSpeed = KeyPrefix + "FastForwardSpeed";
    private readonly Dictionary<int, Vector2> canvasReferenceCache = new Dictionary<int, Vector2>();
    private int lastUIScaleRefreshFrame = -999;

    public static SettingsManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject obj = new GameObject("SettingsManager");
        return obj.AddComponent<SettingsManager>();
    }

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
        if (!Input.GetKeyDown(KeyCode.F1))
        {
            return;
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;
        if (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting) return;

        SettingsUIBuilder.ShowSettings(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SaveSettings()
    {
        SaveSettings(CurrentSettings);
    }

    public void EnsureLoaded()
    {
        if (CurrentSettings != null)
        {
            return;
        }

        LoadSettings();
        ApplyAllSettings();
    }

    public void SaveSettings(SettingsData settings)
    {
        if (settings == null)
        {
            return;
        }

        PlayerPrefs.SetFloat(KeyMasterVolume, settings.masterVolume);
        PlayerPrefs.SetFloat(KeyMusicVolume, settings.musicVolume);
        PlayerPrefs.SetFloat(KeySfxVolume, settings.sfxVolume);
        PlayerPrefs.SetInt(KeyIsMuted, settings.isMuted ? 1 : 0);

        PlayerPrefs.SetInt(KeyFullscreen, settings.fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KeyResolutionWidth, settings.resolutionWidth);
        PlayerPrefs.SetInt(KeyResolutionHeight, settings.resolutionHeight);
        PlayerPrefs.SetFloat(KeyUiScale, settings.uiScale);

        PlayerPrefs.SetInt(KeyTextSpeed, settings.textSpeed);
        PlayerPrefs.SetInt(KeyLanguage, settings.language);
        PlayerPrefs.SetInt(KeyAutoPlayInterval, settings.autoPlayInterval);
        PlayerPrefs.SetInt(KeySkipMode, settings.skipMode);
        PlayerPrefs.SetInt(KeyFastForwardSpeed, settings.fastForwardSpeed);

        PlayerPrefs.Save();
        Debug.Log("[SettingsManager] 设置已保存");
    }

    public void LoadSettings()
    {
        SettingsData data = new SettingsData();

        data.masterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 1.0f);
        data.musicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, 0.7f);
        data.sfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 0.8f);
        data.isMuted = PlayerPrefs.GetInt(KeyIsMuted, 0) == 1;

        data.fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
        data.resolutionWidth = PlayerPrefs.GetInt(KeyResolutionWidth, Screen.currentResolution.width);
        data.resolutionHeight = PlayerPrefs.GetInt(KeyResolutionHeight, Screen.currentResolution.height);
        data.uiScale = PlayerPrefs.GetFloat(KeyUiScale, 1.0f);

        data.textSpeed = PlayerPrefs.GetInt(KeyTextSpeed, 1);
        data.language = PlayerPrefs.GetInt(KeyLanguage, 0);
        data.autoPlayInterval = PlayerPrefs.GetInt(KeyAutoPlayInterval, 1);
        data.skipMode = PlayerPrefs.GetInt(KeySkipMode, 0);
        data.fastForwardSpeed = PlayerPrefs.GetInt(KeyFastForwardSpeed, 2);

        CurrentSettings = Sanitize(data);
        Debug.Log("[SettingsManager] 设置已加载");
    }

    public void ApplyAllSettings()
    {
        ApplySettings(CurrentSettings);
    }

    public void ApplySettings(SettingsData settings)
    {
        if (settings == null)
        {
            return;
        }

        CurrentSettings = Sanitize(settings.Clone());

        ApplyAudioSettings(CurrentSettings);
        ApplyDisplaySettings(CurrentSettings);
        ApplyGameplaySettings(CurrentSettings);

        OnSettingsChanged?.Invoke(CurrentSettings);
    }

    public void SaveAndApply(SettingsData settings)
    {
        if (settings == null)
        {
            return;
        }

        CurrentSettings = Sanitize(settings.Clone());
        SaveSettings(CurrentSettings);
        ApplyAllSettings();
    }

    public void ResetToDefaults()
    {
        if (CurrentSettings == null)
        {
            CurrentSettings = new SettingsData();
        }

        CurrentSettings.ResetToDefaults();
        CurrentSettings = Sanitize(CurrentSettings);
        SaveSettings();
        ApplyAllSettings();
        Debug.Log("[SettingsManager] 已恢复默认设置");
    }

    private SettingsData Sanitize(SettingsData settings)
    {
        if (settings == null)
        {
            settings = new SettingsData();
        }

        settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
        settings.musicVolume = Mathf.Clamp01(settings.musicVolume);
        settings.sfxVolume = Mathf.Clamp01(settings.sfxVolume);
        settings.uiScale = Mathf.Clamp(settings.uiScale, 0.8f, 1.3f);
        settings.textSpeed = Mathf.Clamp(settings.textSpeed, 0, 2);
        settings.language = Mathf.Clamp(settings.language, 0, 1);
        settings.autoPlayInterval = Mathf.Clamp(settings.autoPlayInterval, 0, 2);
        settings.skipMode = Mathf.Clamp(settings.skipMode, 0, 1);
        settings.fastForwardSpeed = Mathf.Clamp(settings.fastForwardSpeed, 0, 3);

        if (settings.resolutionWidth <= 0 || settings.resolutionHeight <= 0)
        {
            settings.resolutionWidth = Screen.currentResolution.width;
            settings.resolutionHeight = Screen.currentResolution.height;
        }

        return settings;
    }

    private void ApplyAudioSettings(SettingsData settings)
    {
        AudioListener.volume = settings.GetEffectiveMasterVolume();
        OnMusicVolumeChanged?.Invoke(settings.GetEffectiveMusicVolume());
        OnSFXVolumeChanged?.Invoke(settings.GetEffectiveSFXVolume());
    }

    private void ApplyDisplaySettings(SettingsData settings)
    {
        Screen.fullScreen = settings.fullscreen;
        Screen.SetResolution(settings.resolutionWidth, settings.resolutionHeight, settings.fullscreen);
        ApplyUIScaleToActiveCanvases(settings.uiScale);
        OnUIScaleChanged?.Invoke(settings.uiScale);
    }

    private void ApplyGameplaySettings(SettingsData settings)
    {
        if (DialogueSystem.Instance == null)
        {
            return;
        }

        float speed;
        switch (settings.textSpeed)
        {
            case 0:
                speed = 0.05f;
                break;
            case 2:
                speed = 0.01f;
                break;
            default:
                speed = 0.03f;
                break;
        }

        DialogueSystem.Instance.SetTextSpeed(speed);
    }

    private void LateUpdate()
    {
        if (CurrentSettings == null)
        {
            return;
        }

        if (Time.frameCount - lastUIScaleRefreshFrame < 30)
        {
            return;
        }

        ApplyUIScaleToActiveCanvases(CurrentSettings.uiScale);
    }

    private void ApplyUIScaleToActiveCanvases(float uiScale)
    {
        uiScale = Mathf.Clamp(uiScale, 0.8f, 1.3f);
        CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < scalers.Length; i++)
        {
            CanvasScaler scaler = scalers[i];
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                continue;
            }

            int id = scaler.GetInstanceID();
            if (!canvasReferenceCache.TryGetValue(id, out Vector2 baseResolution))
            {
                baseResolution = scaler.referenceResolution;
                canvasReferenceCache[id] = baseResolution;
            }

            scaler.referenceResolution = baseResolution / uiScale;
        }

        lastUIScaleRefreshFrame = Time.frameCount;
    }
}
