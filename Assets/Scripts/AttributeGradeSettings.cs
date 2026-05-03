using System;
using UnityEngine;

/// <summary>
/// 核心属性等级阈值配置。
/// 默认档位：D/C/B/A/S，支持运行时调整与持久化。
/// </summary>
public static class AttributeGradeSettings
{
    public const int TierCount = 5;

    private static readonly int[] DefaultThresholds = { 25, 45, 65, 80, 90 };
    private static readonly string[] TierLabels = { "D", "C", "B", "A", "S" };
    private const string PlayerPrefsKeyPrefix = "AttributeGradeThreshold_";
    private const int MinThreshold = 1;
    private const int MaxThreshold = 999;

    private static readonly int[] thresholds = new int[TierCount];
    private static bool loaded;

    public static event Action OnThresholdsChanged;

    public static int[] GetThresholds()
    {
        EnsureLoaded();
        int[] copy = new int[TierCount];
        Array.Copy(thresholds, copy, TierCount);
        return copy;
    }

    public static int GetThreshold(int index)
    {
        EnsureLoaded();
        if (index < 0 || index >= TierCount)
        {
            return 0;
        }

        return thresholds[index];
    }

    public static string GetTierLabel(int index)
    {
        return index >= 0 && index < TierCount ? TierLabels[index] : string.Empty;
    }

    public static void SetThreshold(int index, int value)
    {
        EnsureLoaded();
        if (index < 0 || index >= TierCount)
        {
            return;
        }

        thresholds[index] = Mathf.Clamp(value, MinThreshold, MaxThreshold);
        NormalizeThresholdsFrom(index);
        Save();
        OnThresholdsChanged?.Invoke();
    }

    public static void ResetDefaults()
    {
        EnsureLoaded();
        Array.Copy(DefaultThresholds, thresholds, TierCount);
        Save();
        OnThresholdsChanged?.Invoke();
    }

    public static string GetGradeLetter(int value)
    {
        EnsureLoaded();

        if (value >= thresholds[4]) return "S";
        if (value >= thresholds[3]) return "A";
        if (value >= thresholds[2]) return "B";
        if (value >= thresholds[1]) return "C";
        if (value >= thresholds[0]) return "D";
        return "F";
    }

    public static int GetNextThreshold(int value)
    {
        EnsureLoaded();

        for (int i = 0; i < thresholds.Length; i++)
        {
            if (value < thresholds[i])
            {
                return thresholds[i];
            }
        }

        return value;
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        for (int i = 0; i < TierCount; i++)
        {
            thresholds[i] = PlayerPrefs.GetInt(PlayerPrefsKeyPrefix + i, DefaultThresholds[i]);
        }

        NormalizeAllThresholds();
        loaded = true;
    }

    private static void NormalizeAllThresholds()
    {
        thresholds[0] = Mathf.Clamp(thresholds[0], MinThreshold, MaxThreshold);

        for (int i = 1; i < TierCount; i++)
        {
            thresholds[i] = Mathf.Clamp(thresholds[i], thresholds[i - 1] + 1, MaxThreshold);
        }
    }

    private static void NormalizeThresholdsFrom(int index)
    {
        if (index <= 0)
        {
            thresholds[0] = Mathf.Clamp(thresholds[0], MinThreshold, MaxThreshold);
            index = 1;
        }

        for (int i = index; i < TierCount; i++)
        {
            thresholds[i] = Mathf.Clamp(thresholds[i], thresholds[i - 1] + 1, MaxThreshold);
        }
    }

    private static void Save()
    {
        for (int i = 0; i < TierCount; i++)
        {
            PlayerPrefs.SetInt(PlayerPrefsKeyPrefix + i, thresholds[i]);
        }

        PlayerPrefs.Save();
    }
}
