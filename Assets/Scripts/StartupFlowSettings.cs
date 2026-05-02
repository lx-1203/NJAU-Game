using UnityEngine;

public static class StartupFlowSettings
{
    private const string Prefix = "StartupFlow_";
    private const string SkipSplashKey = Prefix + "SkipSplash";
    private const string SkipCharacterCreationKey = Prefix + "SkipCharacterCreation";
    private const string SkipOpeningStoryKey = Prefix + "SkipOpeningStory";
    private const string SkipTitleScreenKey = Prefix + "SkipTitleScreen";
    private const string DefaultNameKey = Prefix + "DefaultName";
    private const string DefaultGenderKey = Prefix + "DefaultGender";
    private const string DefaultMajorKey = Prefix + "DefaultMajor";

    private const string FallbackName = "Teat";
    private const int FallbackGender = 1;
    private const string FallbackMajor = "\u751f\u7269\u79d1\u5b66\u4e13\u4e1a";

    private static bool suppressNextTitleAutoSkip;

    public static bool SkipSplashLogo
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            bool value = state != null ? state.skipSplashLogo : PlayerPrefs.GetInt(SkipSplashKey, 0) == 1;
            SyncBoolPref(SkipSplashKey, value);
            return value;
        }
        set
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.skipSplashLogo = value;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            PlayerPrefs.SetInt(SkipSplashKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SkipCharacterCreation
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            bool value = state != null ? state.skipCharacterCreation : PlayerPrefs.GetInt(SkipCharacterCreationKey, 0) == 1;
            SyncBoolPref(SkipCharacterCreationKey, value);
            return value;
        }
        set
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.skipCharacterCreation = value;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            PlayerPrefs.SetInt(SkipCharacterCreationKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SkipOpeningStory
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            bool value = state != null ? state.skipOpeningStory : PlayerPrefs.GetInt(SkipOpeningStoryKey, 0) == 1;
            SyncBoolPref(SkipOpeningStoryKey, value);
            return value;
        }
        set
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.skipOpeningStory = value;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            PlayerPrefs.SetInt(SkipOpeningStoryKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SkipTitleScreen
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            bool value = state != null ? state.skipTitleScreen : PlayerPrefs.GetInt(SkipTitleScreenKey, 0) == 1;
            SyncBoolPref(SkipTitleScreenKey, value);
            return value;
        }
        set
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.skipTitleScreen = value;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            PlayerPrefs.SetInt(SkipTitleScreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static string DefaultPlayerName
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            string value = state != null ? state.defaultPlayerName : PlayerPrefs.GetString(DefaultNameKey, FallbackName);
            PlayerPrefs.SetString(DefaultNameKey, string.IsNullOrWhiteSpace(value) ? FallbackName : value.Trim());
            PlayerPrefs.Save();
            return string.IsNullOrWhiteSpace(value) ? FallbackName : value.Trim();
        }
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? FallbackName : value.Trim();
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.defaultPlayerName = normalized;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            PlayerPrefs.SetString(DefaultNameKey, normalized);
            PlayerPrefs.Save();
        }
    }

    public static int DefaultPlayerGender
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            int value = state != null ? state.defaultPlayerGender : PlayerPrefs.GetInt(DefaultGenderKey, FallbackGender);
            value = Mathf.Clamp(value, 0, 1);
            PlayerPrefs.SetInt(DefaultGenderKey, value);
            PlayerPrefs.Save();
            return value;
        }
        set
        {
            int normalized = Mathf.Clamp(value, 0, 1);
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.defaultPlayerGender = normalized;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            PlayerPrefs.SetInt(DefaultGenderKey, normalized);
            PlayerPrefs.Save();
        }
    }

    public static string DefaultPlayerMajor
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            string value = state != null ? state.defaultPlayerMajor : PlayerPrefs.GetString(DefaultMajorKey, FallbackMajor);
            PlayerPrefs.SetString(DefaultMajorKey, string.IsNullOrWhiteSpace(value) ? FallbackMajor : value.Trim());
            PlayerPrefs.Save();
            return string.IsNullOrWhiteSpace(value) ? FallbackMajor : value.Trim();
        }
        set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? FallbackMajor : value.Trim();
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.defaultPlayerMajor = normalized;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            PlayerPrefs.SetString(DefaultMajorKey, normalized);
            PlayerPrefs.Save();
        }
    }

    public static void ApplyQuickStartPreset()
    {
        SkipSplashLogo = true;
        SkipCharacterCreation = true;
        SkipOpeningStory = true;
        SkipTitleScreen = true;
        DefaultPlayerName = FallbackName;
        DefaultPlayerGender = FallbackGender;
        DefaultPlayerMajor = FallbackMajor;
    }

    public static void SuspendTitleAutoSkipOnce()
    {
        suppressNextTitleAutoSkip = true;
    }

    public static bool ShouldAutoSkipTitleScreenThisTime()
    {
        if (suppressNextTitleAutoSkip)
        {
            suppressNextTitleAutoSkip = false;
            return false;
        }

        return SkipTitleScreen;
    }

    private static void SyncBoolPref(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
