using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 主流程 UI 兜底工具，负责恢复输入环境并关闭跨场景残留的阻塞界面。
/// </summary>
public static class UIFlowGuard
{
    public const string WindowPauseMenu = "PauseMenu";
    public const string WindowConfirmDialog = "ConfirmDialog";
    public const string WindowNews = "News";
    public const string WindowAchievementReview = "AchievementReview";
    public const string WindowPhysicalTest = "PhysicalTest";
    public const string WindowCourseSchedule = "CourseSchedule";
    public const string WindowInventory = "Inventory";
    public const string WindowMissionPanel = "MissionPanel";
    public const string WindowJobSelection = "JobSelection";
    public const string WindowTalent = "Talent";
    public const string WindowInfoPanel = "InfoPanel";
    public const string WindowNpcInteraction = "NpcInteraction";
    public const string WindowClubPanel = "ClubPanel";
    public const string WindowShop = "Shop";
    public const string WindowSaveLoad = "SaveLoad";
    public const string WindowSettings = "Settings";
    public const string WindowCharacterCreation = "CharacterCreation";
    public const string WindowSemesterSummary = "SemesterSummary";
    public const string WindowExam = "Exam";

    public static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            return;
        }

        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    public static void RestoreInteractiveState()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public static void CleanupBlockingUI()
    {
        CleanupBlockingUIExcept(null);
    }

    public static bool PrepareForExclusiveWindow(string windowKey)
    {
        if (HasExclusiveLock(windowKey))
        {
            return false;
        }

        CleanupBlockingUIExcept(windowKey);
        return true;
    }

    public static void CleanupBlockingUIExcept(string exceptWindowKey)
    {
        RestoreInteractiveState();
        EnsureEventSystem();

        if (exceptWindowKey != WindowPauseMenu &&
            PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen)
        {
            PauseMenuUI.Instance.Close();
        }

        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (DebugConsoleManager.Instance != null && DebugConsoleManager.Instance.IsOpen)
        {
            DebugConsoleManager.Instance.Close();
        }
        #endif

        if (exceptWindowKey != WindowConfirmDialog &&
            ConfirmDialogUI.Instance != null && ConfirmDialogUI.Instance.IsOpen)
        {
            ConfirmDialogUI.Instance.Hide();
        }

        if (exceptWindowKey != WindowNews &&
            NewsSystem.Instance != null && NewsSystem.Instance.IsShowing)
        {
            NewsSystem.Instance.DismissNews();
        }

        if (exceptWindowKey != WindowAchievementReview &&
            AchievementUI.Instance != null && AchievementUI.Instance.isReviewShowing)
        {
            AchievementUI.Instance.HideReviewPanelImmediate();
        }

        if (exceptWindowKey != WindowPhysicalTest &&
            PhysicalTestUI.Instance != null && PhysicalTestUI.Instance.IsOpen)
        {
            PhysicalTestUI.Instance.Hide();
        }

        if (exceptWindowKey != WindowCourseSchedule &&
            CourseScheduleUI.Instance != null && CourseScheduleUI.Instance.IsOpen)
        {
            CourseScheduleUI.Instance.HideForSceneTransition();
        }

        if (exceptWindowKey != WindowInventory &&
            InventoryUIManager.Instance != null && InventoryUIManager.Instance.IsOpen)
        {
            InventoryUIManager.Instance.ClosePanel();
        }

        if (exceptWindowKey != WindowMissionPanel &&
            MissionPanelBuilder.Instance != null && MissionPanelBuilder.Instance.IsOpen)
        {
            MissionPanelBuilder.Instance.ClosePanel();
        }

        if (exceptWindowKey != WindowJobSelection &&
            JobSelectionUI.Instance != null && JobSelectionUI.Instance.IsOpen)
        {
            JobSelectionUI.Instance.Hide();
        }

        if (exceptWindowKey != WindowTalent &&
            TalentUI.Instance != null && TalentUI.Instance.IsOpen)
        {
            TalentUI.Instance.ClosePanel();
        }

        if (exceptWindowKey != WindowInfoPanel &&
            InfoPanelManager.Instance != null && InfoPanelManager.Instance.IsOpen)
        {
            InfoPanelManager.Instance.ClosePanel();
        }

        if (exceptWindowKey != WindowNpcInteraction &&
            NPCInteractionMenu.Instance != null && NPCInteractionMenu.Instance.IsMenuOpen)
        {
            NPCInteractionMenu.Instance.CloseMenu();
        }

        ClubPanelManager clubPanelManager = Object.FindObjectOfType<ClubPanelManager>();
        if (exceptWindowKey != WindowClubPanel &&
            clubPanelManager != null && clubPanelManager.IsOpen)
        {
            clubPanelManager.ClosePanel();
        }

        ShopUIBuilder shopUIBuilder = Object.FindObjectOfType<ShopUIBuilder>();
        if (exceptWindowKey != WindowShop &&
            shopUIBuilder != null && shopUIBuilder.IsShopOpen)
        {
            shopUIBuilder.HideShop();
        }

        SaveLoadUI saveLoadUI = Object.FindObjectOfType<SaveLoadUI>();
        if (exceptWindowKey != WindowSaveLoad && saveLoadUI != null)
        {
            Object.Destroy(saveLoadUI.gameObject);
        }

        if (exceptWindowKey != WindowSettings &&
            SettingsUIBuilder.Instance != null && SettingsUIBuilder.Instance.IsOpen)
        {
            SettingsUIBuilder.HideSettings();
        }

        if (exceptWindowKey != WindowSemesterSummary &&
            SemesterSummaryUI.Instance != null && SemesterSummaryUI.Instance.isShowing)
        {
            SemesterSummaryUI.Instance.Hide();
        }

        if (exceptWindowKey != WindowExam &&
            ExamUIManager.Instance != null && ExamUIManager.Instance.IsExamActive)
        {
            ExamUIManager.Instance.ForceTerminateSequence();
        }

        if (exceptWindowKey != WindowCharacterCreation &&
            CharacterCreationUI.Instance != null &&
            CharacterCreationUI.Instance.IsOpen)
        {
            CharacterCreationUI.Instance.ForceClose();
        }

        if (exceptWindowKey != WindowCharacterCreation) DestroyNamedRoot("CharacterCreationCanvas");
        if (exceptWindowKey != WindowPauseMenu) DestroyNamedRoot("PauseMenuCanvas");
        if (exceptWindowKey != WindowSaveLoad) DestroyNamedRoot("SaveLoadUI");
        if (exceptWindowKey != WindowConfirmDialog) DestroyNamedRoot("ConfirmDialogCanvas");
        if (exceptWindowKey != WindowSettings) DestroyNamedRoot("SettingsCanvas");
    }

    private static void DestroyNamedRoot(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            Object.Destroy(target);
        }
    }

    private static bool HasExclusiveLock(string requestingWindowKey)
    {
        if (requestingWindowKey != WindowConfirmDialog &&
            ConfirmDialogUI.Instance != null && ConfirmDialogUI.Instance.IsOpen)
        {
            return true;
        }

        if (requestingWindowKey != WindowCourseSchedule &&
            CourseScheduleUI.Instance != null && CourseScheduleUI.Instance.IsOpen)
        {
            return true;
        }

        if (requestingWindowKey != WindowExam &&
            ExamUIManager.Instance != null && ExamUIManager.Instance.IsExamActive)
        {
            return true;
        }

        if (requestingWindowKey != WindowSemesterSummary &&
            SemesterSummaryUI.Instance != null && SemesterSummaryUI.Instance.isShowing)
        {
            return true;
        }

        if (requestingWindowKey != WindowPhysicalTest &&
            PhysicalTestUI.Instance != null && PhysicalTestUI.Instance.IsOpen)
        {
            return true;
        }

        if (requestingWindowKey != WindowCharacterCreation &&
            CharacterCreationUI.Instance != null &&
            CharacterCreationUI.Instance.IsOpen)
        {
            return true;
        }

        return false;
    }
}
