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
    private const string UseStartupTimeOverrideKey = Prefix + "UseStartupTimeOverride";
    private const string SemesterRoundCountKey = Prefix + "SemesterRoundCount";
    private const string StartupYearKey = Prefix + "StartupYear";
    private const string StartupSemesterKey = Prefix + "StartupSemester";
    private const string StartupRoundKey = Prefix + "StartupRound";
    private const string StartupStudyKey = Prefix + "StartupStudy";
    private const string StartupCharmKey = Prefix + "StartupCharm";
    private const string StartupPhysiqueKey = Prefix + "StartupPhysique";
    private const string StartupLeadershipKey = Prefix + "StartupLeadership";
    private const string StartupStressKey = Prefix + "StartupStress";
    private const string StartupMoodKey = Prefix + "StartupMood";
    private const string StartupDarknessKey = Prefix + "StartupDarkness";
    private const string StartupGuiltKey = Prefix + "StartupGuilt";
    private const string StartupLuckKey = Prefix + "StartupLuck";
    private const string StartupMoneyKey = Prefix + "StartupMoney";
    private const string StartupActionPointsKey = Prefix + "StartupActionPoints";

    private const string FallbackName = "Teat";
    private const int FallbackGender = 1;
    private const string FallbackMajor = "\u751f\u7269\u79d1\u5b66\u4e13\u4e1a";
    private const int FallbackStudy = 10;
    private const int FallbackCharm = 5;
    private const int FallbackPhysique = 8;
    private const int FallbackLeadership = 3;
    private const int FallbackStress = 20;
    private const int FallbackMood = 70;
    private const int FallbackDarkness = 0;
    private const int FallbackGuilt = 0;
    private const int FallbackLuck = 50;
    private const int FallbackMoney = 8000;
    private const int FallbackActionPoints = 20;

    private static bool suppressNextTitleAutoSkip;
    private static int? editorPreviewPlayerGenderOverride;

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

    public static bool UseStartupTimeOverride
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            bool value = state != null ? state.useStartupTimeOverride : PlayerPrefs.GetInt(UseStartupTimeOverrideKey, 0) == 1;
            SyncBoolPref(UseStartupTimeOverrideKey, value);
            return value;
        }
        set
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.useStartupTimeOverride = value;
                ZhongshanDeckToolStateBridge.SaveState();
            }

            PlayerPrefs.SetInt(UseStartupTimeOverrideKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int SemesterRoundCount
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            int value = state != null ? state.semesterRoundCount : PlayerPrefs.GetInt(SemesterRoundCountKey, 5);
            value = Mathf.Clamp(value, 3, 12);
            PlayerPrefs.SetInt(SemesterRoundCountKey, value);
            PlayerPrefs.Save();
            return value;
        }
        set
        {
            int normalized = Mathf.Clamp(value, 3, 12);
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.semesterRoundCount = normalized;
                if (state.startupRound > normalized)
                {
                    state.startupRound = normalized;
                }
                ZhongshanDeckToolStateBridge.SaveState();
            }

            PlayerPrefs.SetInt(SemesterRoundCountKey, normalized);
            if (StartupRound > normalized)
            {
                PlayerPrefs.SetInt(StartupRoundKey, normalized);
            }
            PlayerPrefs.Save();
        }
    }

    public static int StartupYear
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            int value = state != null ? state.startupYear : PlayerPrefs.GetInt(StartupYearKey, 1);
            value = Mathf.Clamp(value, 1, 4);
            PlayerPrefs.SetInt(StartupYearKey, value);
            PlayerPrefs.Save();
            return value;
        }
        set
        {
            int normalized = Mathf.Clamp(value, 1, 4);
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.startupYear = normalized;
                ZhongshanDeckToolStateBridge.SaveState();
            }

            PlayerPrefs.SetInt(StartupYearKey, normalized);
            PlayerPrefs.Save();
        }
    }

    public static int StartupSemester
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            int value = state != null ? state.startupSemester : PlayerPrefs.GetInt(StartupSemesterKey, 1);
            value = Mathf.Clamp(value, 1, 2);
            PlayerPrefs.SetInt(StartupSemesterKey, value);
            PlayerPrefs.Save();
            return value;
        }
        set
        {
            int normalized = Mathf.Clamp(value, 1, 2);
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.startupSemester = normalized;
                ZhongshanDeckToolStateBridge.SaveState();
            }

            PlayerPrefs.SetInt(StartupSemesterKey, normalized);
            PlayerPrefs.Save();
        }
    }

    public static int StartupRound
    {
        get
        {
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            int value = state != null ? state.startupRound : PlayerPrefs.GetInt(StartupRoundKey, 1);
            value = Mathf.Clamp(value, 1, SemesterRoundCount);
            PlayerPrefs.SetInt(StartupRoundKey, value);
            PlayerPrefs.Save();
            return value;
        }
        set
        {
            int normalized = Mathf.Clamp(value, 1, SemesterRoundCount);
            ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
            if (state != null)
            {
                state.startupRound = normalized;
                ZhongshanDeckToolStateBridge.SaveState();
            }

            PlayerPrefs.SetInt(StartupRoundKey, normalized);
            PlayerPrefs.Save();
        }
    }

    public static int InitialStudy
    {
        get => GetStartupInt(StartupStudyKey, state => state.startupStudy, FallbackStudy, 0, 999);
        set => SetStartupInt(StartupStudyKey, value, 0, 999, (state, normalized) => state.startupStudy = normalized);
    }

    public static int InitialCharm
    {
        get => GetStartupInt(StartupCharmKey, state => state.startupCharm, FallbackCharm, 0, 999);
        set => SetStartupInt(StartupCharmKey, value, 0, 999, (state, normalized) => state.startupCharm = normalized);
    }

    public static int InitialPhysique
    {
        get => GetStartupInt(StartupPhysiqueKey, state => state.startupPhysique, FallbackPhysique, 0, 999);
        set => SetStartupInt(StartupPhysiqueKey, value, 0, 999, (state, normalized) => state.startupPhysique = normalized);
    }

    public static int InitialLeadership
    {
        get => GetStartupInt(StartupLeadershipKey, state => state.startupLeadership, FallbackLeadership, 0, 999);
        set => SetStartupInt(StartupLeadershipKey, value, 0, 999, (state, normalized) => state.startupLeadership = normalized);
    }

    public static int InitialStress
    {
        get => GetStartupInt(StartupStressKey, state => state.startupStress, FallbackStress, 0, PlayerAttributes.MaxStatusValue);
        set => SetStartupInt(StartupStressKey, value, 0, PlayerAttributes.MaxStatusValue, (state, normalized) => state.startupStress = normalized);
    }

    public static int InitialMood
    {
        get => GetStartupInt(StartupMoodKey, state => state.startupMood, FallbackMood, 0, PlayerAttributes.MaxStatusValue);
        set => SetStartupInt(StartupMoodKey, value, 0, PlayerAttributes.MaxStatusValue, (state, normalized) => state.startupMood = normalized);
    }

    public static int InitialDarkness
    {
        get => GetStartupInt(StartupDarknessKey, state => state.startupDarkness, FallbackDarkness, 0, 999);
        set => SetStartupInt(StartupDarknessKey, value, 0, 999, (state, normalized) => state.startupDarkness = normalized);
    }

    public static int InitialGuilt
    {
        get => GetStartupInt(StartupGuiltKey, state => state.startupGuilt, FallbackGuilt, 0, PlayerAttributes.MaxStatusValue);
        set => SetStartupInt(StartupGuiltKey, value, 0, PlayerAttributes.MaxStatusValue, (state, normalized) => state.startupGuilt = normalized);
    }

    public static int InitialLuck
    {
        get => GetStartupInt(StartupLuckKey, state => state.startupLuck, FallbackLuck, 0, PlayerAttributes.MaxStatusValue);
        set => SetStartupInt(StartupLuckKey, value, 0, PlayerAttributes.MaxStatusValue, (state, normalized) => state.startupLuck = normalized);
    }

    public static int InitialMoney
    {
        get => GetStartupInt(StartupMoneyKey, state => state.startupMoney, FallbackMoney, -999999, 999999);
        set => SetStartupInt(StartupMoneyKey, value, -999999, 999999, (state, normalized) => state.startupMoney = normalized);
    }

    public static int InitialActionPoints
    {
        get => GetStartupInt(StartupActionPointsKey, state => state.startupActionPoints, FallbackActionPoints, 1, 999);
        set => SetStartupInt(StartupActionPointsKey, value, 1, 999, (state, normalized) => state.startupActionPoints = normalized);
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

    public static void ApplyStartupPlayerAttributes(PlayerAttributes playerAttributes)
    {
        if (playerAttributes == null)
        {
            return;
        }

        playerAttributes.SetAll(
            InitialStudy,
            InitialCharm,
            InitialPhysique,
            InitialLeadership,
            InitialStress,
            InitialMood,
            InitialDarkness,
            InitialGuilt,
            InitialLuck);
    }

    public static void ApplyStartupCoreState(GameState gameState)
    {
        if (gameState == null)
        {
            return;
        }

        int year = gameState.CurrentYear;
        int semester = gameState.CurrentSemester;
        int round = gameState.CurrentRound;
        int month = gameState.CurrentMonth;

        if (UseStartupTimeOverride)
        {
            year = StartupYear;
            semester = StartupSemester;
            round = StartupRound;
            month = GameState.CalculateMonth(semester, round);
        }

        gameState.SetState(year, semester, round, month, InitialMoney, gameState.EffectiveMaxActionPoints);
    }

    public static bool ApplyStartupTimeToGameState(GameState gameState)
    {
        if (gameState == null || !UseStartupTimeOverride)
        {
            return false;
        }

        ApplyStartupCoreState(gameState);
        return true;
    }

    public static void SetEditorPreviewPlayerGenderOverride(int gender)
    {
        editorPreviewPlayerGenderOverride = Mathf.Clamp(gender, 0, 1);
    }

    public static bool TryGetEditorPreviewPlayerGenderOverride(out int gender)
    {
        if (editorPreviewPlayerGenderOverride.HasValue)
        {
            gender = Mathf.Clamp(editorPreviewPlayerGenderOverride.Value, 0, 1);
            return true;
        }

        gender = FallbackGender;
        return false;
    }

    public static void ClearEditorPreviewPlayerGenderOverride()
    {
        editorPreviewPlayerGenderOverride = null;
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

    private static int GetStartupInt(string key, System.Func<ZhongshanDeckToolState, int> stateGetter, int fallback, int min, int max)
    {
        ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
        int value = state != null ? stateGetter(state) : PlayerPrefs.GetInt(key, fallback);
        value = Mathf.Clamp(value, min, max);
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        return value;
    }

    private static void SetStartupInt(string key, int value, int min, int max, System.Action<ZhongshanDeckToolState, int> stateSetter)
    {
        int normalized = Mathf.Clamp(value, min, max);
        ZhongshanDeckToolState state = ZhongshanDeckToolStateBridge.GetState();
        if (state != null)
        {
            stateSetter(state, normalized);
            ZhongshanDeckToolStateBridge.SaveState();
        }

        PlayerPrefs.SetInt(key, normalized);
        PlayerPrefs.Save();
    }
}
