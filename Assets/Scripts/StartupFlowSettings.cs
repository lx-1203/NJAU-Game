using UnityEngine;

public static class StartupFlowSettings
{
    private const string Prefix = "StartupFlow_";
    private const string SkipSplashKey = Prefix + "SkipSplash";
    private const string SkipCharacterCreationKey = Prefix + "SkipCharacterCreation";
    private const string SkipOpeningStoryKey = Prefix + "SkipOpeningStory";
    private const string DefaultNameKey = Prefix + "DefaultName";
    private const string DefaultGenderKey = Prefix + "DefaultGender";
    private const string DefaultMajorKey = Prefix + "DefaultMajor";

    private const string FallbackName = "Teat";
    private const int FallbackGender = 1;
    private const string FallbackMajor = "\u751f\u7269\u79d1\u5b66\u4e13\u4e1a";

    public static bool SkipSplashLogo
    {
        get => PlayerPrefs.GetInt(SkipSplashKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SkipSplashKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SkipCharacterCreation
    {
        get => PlayerPrefs.GetInt(SkipCharacterCreationKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SkipCharacterCreationKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SkipOpeningStory
    {
        get => PlayerPrefs.GetInt(SkipOpeningStoryKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SkipOpeningStoryKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static string DefaultPlayerName
    {
        get
        {
            string value = PlayerPrefs.GetString(DefaultNameKey, FallbackName);
            return string.IsNullOrWhiteSpace(value) ? FallbackName : value.Trim();
        }
        set
        {
            PlayerPrefs.SetString(DefaultNameKey, string.IsNullOrWhiteSpace(value) ? FallbackName : value.Trim());
            PlayerPrefs.Save();
        }
    }

    public static int DefaultPlayerGender
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(DefaultGenderKey, FallbackGender), 0, 1);
        set
        {
            PlayerPrefs.SetInt(DefaultGenderKey, Mathf.Clamp(value, 0, 1));
            PlayerPrefs.Save();
        }
    }

    public static string DefaultPlayerMajor
    {
        get
        {
            string value = PlayerPrefs.GetString(DefaultMajorKey, FallbackMajor);
            return string.IsNullOrWhiteSpace(value) ? FallbackMajor : value.Trim();
        }
        set
        {
            PlayerPrefs.SetString(DefaultMajorKey, string.IsNullOrWhiteSpace(value) ? FallbackMajor : value.Trim());
            PlayerPrefs.Save();
        }
    }

    public static void ApplyQuickStartPreset()
    {
        SkipSplashLogo = true;
        SkipCharacterCreation = true;
        SkipOpeningStory = true;
        DefaultPlayerName = FallbackName;
        DefaultPlayerGender = FallbackGender;
        DefaultPlayerMajor = FallbackMajor;
    }
}
