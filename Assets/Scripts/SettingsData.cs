using System;
using UnityEngine;

/// <summary>
/// 设置数据模型 - 可序列化到 PlayerPrefs
/// 包含音频、显示、游戏性等所有配置项
/// </summary>
[Serializable]
public class SettingsData
{
    // ========== 音频设置 ==========
    public float masterVolume = 1.0f;    // 主音量 (0-1)
    public float musicVolume = 0.7f;     // 音乐音量 (0-1)
    public float sfxVolume = 0.8f;       // 音效音量 (0-1)
    public bool isMuted = false;         // 静音状态

    // ========== 显示设置 ==========
    public bool fullscreen = true;       // 全屏模式
    public int resolutionWidth = 1920;   // 分辨率宽度
    public int resolutionHeight = 1080;  // 分辨率高度
    public float uiScale = 1.0f;         // UI缩放 (0.8-1.2)

    // ========== 游戏性设置 ==========
    public int textSpeed = 1;            // 文本速度 (0=慢/1=中/2=快)
    public int language = 0;             // 语言 (0=中文/1=英文，预留)

    // ========== 工具方法 ==========

    /// <summary>
    /// 获取实际音乐音量（考虑主音量和静音）
    /// </summary>
    public float GetEffectiveMusicVolume()
    {
        return isMuted ? 0f : masterVolume * musicVolume;
    }

    /// <summary>
    /// 获取实际音效音量（考虑主音量和静音）
    /// </summary>
    public float GetEffectiveSFXVolume()
    {
        return isMuted ? 0f : masterVolume * sfxVolume;
    }

    /// <summary>
    /// 恢复默认值
    /// </summary>
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
    }

    /// <summary>
    /// 克隆当前设置
    /// </summary>
    public SettingsData Clone()
    {
        return (SettingsData)MemberwiseClone();
    }
}
