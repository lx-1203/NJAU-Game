using System;
using UnityEngine;

/// <summary>
/// 设置数据模型，序列化到 PlayerPrefs。
/// </summary>
[Serializable]
public class SettingsData
{
    // 音频
    public float masterVolume = 1.0f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    public bool isMuted = false;

    // 图像
    public bool fullscreen = true;
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public float uiScale = 1.0f;

    // 游戏
    public int textSpeed = 1;          // 0=慢, 1=正常, 2=快
    public int language = 0;           // 0=简体中文, 1=English(预留)
    public int autoPlayInterval = 1;   // 0=短, 1=正常, 2=长
    public int skipMode = 0;           // 0=快进所有对话, 1=仅快进已读
    public int fastForwardSpeed = 2;   // 0=x4, 1=x10, 2=x20, 3=x30

    public float GetEffectiveMasterVolume()
    {
        return isMuted ? 0f : masterVolume;
    }

    public float GetEffectiveMusicVolume()
    {
        return isMuted ? 0f : masterVolume * musicVolume;
    }

    public float GetEffectiveSFXVolume()
    {
        return isMuted ? 0f : masterVolume * sfxVolume;
    }

    public void ResetToDefaults()
    {
        masterVolume = 1.0f;
        musicVolume = 0.7f;
        sfxVolume = 0.8f;
        isMuted = false;

        fullscreen = true;
        Resolution currentRes = Screen.currentResolution;
        resolutionWidth = currentRes.width;
        resolutionHeight = currentRes.height;
        uiScale = 1.0f;

        textSpeed = 1;
        language = 0;
        autoPlayInterval = 1;
        skipMode = 0;
        fastForwardSpeed = 2;
    }

    public SettingsData Clone()
    {
        return (SettingsData)MemberwiseClone();
    }

    public string GetResolutionLabel()
    {
        return resolutionWidth + "x" + resolutionHeight;
    }

    public string GetTextSpeedLabel()
    {
        switch (textSpeed)
        {
            case 0: return "慢";
            case 2: return "快";
            default: return "正常";
        }
    }

    public string GetLanguageLabel()
    {
        return language == 1 ? "English (开发中)" : "简体中文";
    }

    public string GetAutoPlayIntervalLabel()
    {
        switch (autoPlayInterval)
        {
            case 0: return "较短";
            case 2: return "较长";
            default: return "正常";
        }
    }

    public string GetSkipModeLabel()
    {
        return skipMode == 1 ? "仅快进已读" : "快进所有对话";
    }

    public string GetFastForwardSpeedLabel()
    {
        switch (fastForwardSpeed)
        {
            case 0: return "x4";
            case 1: return "x10";
            case 3: return "x30";
            default: return "x20";
        }
    }
}
