using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 游戏场景初始化器
/// 确保所有核心系统按正确顺序创建和初始化
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    private void Start()
    {
        UIFlowGuard.CleanupBlockingUI();
        EnsureEventSystem();

        // 提前初始化提示通道，确保后续系统在 Awake/Start 阶段发出的前台反馈能被接住。
        SetupMissionUI();

        // 存档管理器（必须在其他系统之前初始化）
        SetupSaveManager();

        // 多周目传承管理器（必须在 GameState 之前，以便应用传承数据）
        SetupNewGamePlusManager();

        // 初始化核心系统（顺序重要：GameState → PlayerAttributes → LocationManager → ActionSystem → ClubSystem → EconomyManager → DebtSystem → ShopSystem → RomanceSystem → ConfessionSystem → AchievementSystem → AchievementUI → SemesterSummarySystem → EndingDeterminer → TurnManager → ExamSystem → CheatingSystem → EventHistory → EventScheduler → DialogueSystem → EventExecutor）
        SetupGameState();
        SetupPlayerAttributes();
        SetupLocationManager();
        SetupLocationSceneController();
        SetupLocationSceneBuilder();
        SetupCameraFollow();
        SetupActionSystem();
        SetupClubSystem();
        SetupEconomyManager();
        SetupDebtSystem();
        SetupShopSystem();
        SetupInventorySystem();
        SetupRomanceSystem();
        SetupConfessionSystem();
        SetupCampusRunSystem();
        SetupPhysicalTestSystem();
        SetupAchievementSystem();
        SetupAchievementUI();
        SetupSemesterSummarySystem();
        SetupEndingDeterminer();
        SetupGameEndingManager();
        SetupTurnManager();
        SetupExamSystem();
        SetupCheatingSystem();
        SetupEventHistory();
        SetupMissionSystem();
        SetupEventScheduler();
        SetupDialogueSystem();
        SetupLocationRandomEventSystem();

        // 加载对话 JSON 数据（必须在 DialogueSystem 初始化之后）
        DialogueParser.LoadAllDialogues();

        // 初始化事件执行器（必须在 DialogueSystem 之后，因为它依赖 IDialogueTrigger）
        SetupEventExecutor();

        // 初始化 NPC 相关系统（顺序重要：NPCEventHub → NPCDatabase → AffinitySystem → NPCManager）
        SetupNPCEventHub();
        SetupNPCDatabase();
        SetupAffinitySystem();
        ApplyNewGamePlusInheritanceIfNeeded();
        SetupRomanceBridge();
        SetupNPCManager();

        // 校园新闻系统（依赖 PlayerAttributes, AffinitySystem, NPCDatabase, ClubSystem）
        SetupNewsSystem();

        // 天赋系统
        SetupTalentSystem();
        SetupTalentUI();

        // 工作/实习/副业系统
        SetupJobSystem();

        // 惩罚系统（依赖 TurnManager, ActionSystem, PlayerAttributes, TalentSystem）
        SetupPenaltySystem();

        // 游戏内功能 UI / 管理器
        SetupSettingsManager();
        SetupAudioManager();
        SetupPauseMenuUI();
        SetupUIEscapeRouter();
        SetupInfoPanelManager();
        SetupInventoryUI();
        SetupMissionPanelBuilder();
        SetupJobSelectionUI();
        SetupPhysicalTestUI();
        SetupConfirmDialogUI();
        SetupCourseScheduleUI();

        // 注入真实 Provider（所有子系统已初始化完毕）
        if (SemesterSummarySystem.Instance != null)
        {
            SemesterSummarySystem.Instance.InjectRealProviders();
        }

        // HUD 管理器（必须在所有子系统初始化之后，确保事件订阅成功）
        SetupHUDManager();

        // 初始化调试控制台（仅 Development/Editor 构建）
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        SetupDebugConsole();
#endif

        // 检查是否有待加载的存档数据
        if (SaveManager.Instance != null && SaveManager.PendingLoadData != null)
        {
            SaveManager.Instance.ApplyLoadedData(SaveManager.PendingLoadData);
            SaveManager.PendingLoadData = null;
            SaveManager.PendingLoadSlot = -1;
            if (MissionSystem.Instance != null)
            {
                MissionSystem.Instance.RefreshMissionState();
            }
            Debug.Log("[GameSceneInit] 已从存档恢复游戏状态");
        }
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    private void SetupDebugConsole()
    {
        if (DebugConsoleManager.Instance == null)
        {
            GameObject obj = new GameObject("DebugConsoleManager");
            obj.AddComponent<DebugConsoleManager>();
        }
    }
#endif

    private void SetupSaveManager()
    {
        if (SaveManager.Instance == null)
        {
            GameObject obj = new GameObject("SaveManager");
            obj.AddComponent<SaveManager>();
        }
    }

    private void SetupNewGamePlusManager()
    {
        if (NewGamePlusManager.Instance == null)
        {
            GameObject obj = new GameObject("NewGamePlusManager");
            obj.AddComponent<NewGamePlusManager>();
        }
    }

    private void SetupGameState()
    {
        if (GameState.Instance == null)
        {
            GameObject obj = new GameObject("GameState");
            obj.AddComponent<GameState>();
        }
    }

    private void SetupPlayerAttributes()
    {
        if (PlayerAttributes.Instance == null)
        {
            GameObject obj = new GameObject("PlayerAttributes");
            obj.AddComponent<PlayerAttributes>();
        }

        if (SaveManager.PendingLoadData == null && PlayerAttributes.Instance != null)
        {
            StartupFlowSettings.ApplyStartupPlayerAttributes(PlayerAttributes.Instance);
        }
    }

    private void SetupLocationManager()
    {
        if (LocationManager.Instance == null)
        {
            GameObject obj = new GameObject("LocationManager");
            obj.AddComponent<LocationManager>();
        }
    }

    private void SetupLocationSceneBuilder()
    {
        if (LocationSceneBuilder.Instance == null)
        {
            GameObject obj = new GameObject("LocationSceneBuilder");
            obj.AddComponent<LocationSceneBuilder>();
        }
    }

    private void SetupCameraFollow()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5.5f;
            cameraObject.transform.position = new Vector3(0f, -2.5f, -10f);
        }

        if (mainCamera.GetComponent<CameraFollow>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow>();
        }
    }

    private void SetupActionSystem()
    {
        if (ActionSystem.Instance == null)
        {
            GameObject obj = new GameObject("ActionSystem");
            obj.AddComponent<ActionSystem>();
        }
    }

    private void SetupLocationSceneController()
    {
        if (FindFirstObjectByType<LocationSceneController>() == null)
        {
            GameObject obj = new GameObject("LocationSceneController");
            obj.AddComponent<LocationSceneController>();
        }
    }

    private void SetupRomanceSystem()
    {
        if (RomanceSystem.Instance == null)
        {
            GameObject obj = new GameObject("RomanceSystem");
            obj.AddComponent<RomanceSystem>();
        }
    }

    private void SetupConfessionSystem()
    {
        if (ConfessionSystem.Instance == null)
        {
            GameObject obj = new GameObject("ConfessionSystem");
            obj.AddComponent<ConfessionSystem>();
        }
    }

    private void SetupEconomyManager()
    {
        if (EconomyManager.Instance == null)
        {
            GameObject obj = new GameObject("EconomyManager");
            obj.AddComponent<EconomyManager>();
        }
    }

    private void SetupDebtSystem()
    {
        if (DebtSystem.Instance == null)
        {
            GameObject obj = new GameObject("DebtSystem");
            obj.AddComponent<DebtSystem>();
        }
    }

    private void SetupShopSystem()
    {
        if (ShopSystem.Instance == null)
        {
            GameObject obj = new GameObject("ShopSystem");
            obj.AddComponent<ShopSystem>();
        }
    }

    private void SetupInventorySystem()
    {
        if (InventorySystem.Instance == null)
        {
            GameObject obj = new GameObject("InventorySystem");
            obj.AddComponent<InventorySystem>();
        }
    }

    private void SetupTurnManager()
    {
        if (TurnManager.Instance == null)
        {
            GameObject obj = new GameObject("TurnManager");
            obj.AddComponent<TurnManager>();
        }
    }

    private void SetupDialogueSystem()
    {
        if (DialogueSystem.Instance == null)
        {
            GameObject dialogueObj = new GameObject("DialogueSystem");
            dialogueObj.AddComponent<DialogueSystem>();
        }
    }

    private void SetupLocationRandomEventSystem()
    {
        if (LocationRandomEventSystem.Instance == null)
        {
            GameObject obj = new GameObject("LocationRandomEventSystem");
            obj.AddComponent<LocationRandomEventSystem>();
        }
    }

    private void SetupNPCEventHub()
    {
        if (NPCEventHub.Instance == null)
        {
            GameObject obj = new GameObject("NPCEventHub");
            obj.AddComponent<NPCEventHub>();
        }
    }

    private void SetupNPCDatabase()
    {
        if (NPCDatabase.Instance == null)
        {
            GameObject obj = new GameObject("NPCDatabase");
            obj.AddComponent<NPCDatabase>();
        }
    }

    private void SetupAffinitySystem()
    {
        if (AffinitySystem.Instance == null)
        {
            GameObject obj = new GameObject("AffinitySystem");
            obj.AddComponent<AffinitySystem>();
        }
    }

    private void ApplyNewGamePlusInheritanceIfNeeded()
    {
        if (SaveManager.PendingLoadData != null)
        {
            return;
        }

        if (NewGamePlusManager.Instance == null || !NewGamePlusManager.Instance.HasNewGamePlusData)
        {
            return;
        }

        NewGamePlusManager.Instance.ApplyInheritance();
    }

    private void SetupRomanceBridge()
    {
        if (RomanceBridge.Instance == null)
        {
            GameObject obj = new GameObject("RomanceBridge");
            obj.AddComponent<RomanceBridge>();
        }
    }

    private void SetupNPCManager()
    {
        if (NPCManager.Instance == null)
        {
            GameObject obj = new GameObject("NPCManager");
            obj.AddComponent<NPCManager>();
        }
    }

    private void SetupLocationZoneDetector()
    {
        // Locations are intentionally changed only through the campus map.
        // Keeping this detector attached would let horizontal movement rewrite
        // GameState.CurrentLocation and make map navigation fail intermittently.
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            UIFlowGuard.EnsureEventSystem();
        }
    }

    // ========== HUD 管理器 ==========

    private void SetupHUDManager()
    {
        if (FindObjectOfType<HUDManager>() == null)
        {
            GameObject obj = new GameObject("HUDManager");
            obj.AddComponent<HUDManager>();
        }
    }

    // ========== 社团系统 ==========

    private void SetupClubSystem()
    {
        if (ClubSystem.Instance == null)
        {
            GameObject obj = new GameObject("ClubSystem");
            obj.AddComponent<ClubSystem>();
        }
    }

    // ========== 学期总结 / 成就 / 结局系统 ==========

    private void SetupAchievementSystem()
    {
        if (AchievementSystem.Instance == null)
        {
            GameObject obj = new GameObject("AchievementSystem");
            obj.AddComponent<AchievementSystem>();
        }
    }

    private void SetupAchievementUI()
    {
        if (AchievementUI.Instance == null)
        {
            GameObject obj = new GameObject("AchievementUI");
            obj.AddComponent<AchievementUI>();
        }
    }

    private void SetupSemesterSummarySystem()
    {
        if (SemesterSummarySystem.Instance == null)
        {
            GameObject obj = new GameObject("SemesterSummarySystem");
            obj.AddComponent<SemesterSummarySystem>();
        }
    }

    private void SetupEndingDeterminer()
    {
        if (EndingDeterminer.Instance == null)
        {
            GameObject obj = new GameObject("EndingDeterminer");
            obj.AddComponent<EndingDeterminer>();
        }
    }

    private void SetupGameEndingManager()
    {
        if (GameEndingManager.Instance == null)
        {
            GameObject obj = new GameObject("GameEndingManager");
            obj.AddComponent<GameEndingManager>();
        }
    }

    // ========== 考试系统 ==========

    private void SetupExamSystem()
    {
        if (ExamSystem.Instance == null)
        {
            GameObject obj = new GameObject("ExamSystem");
            obj.AddComponent<ExamSystem>();
        }
    }

    private void SetupCheatingSystem()
    {
        if (CheatingSystem.Instance == null)
        {
            GameObject obj = new GameObject("CheatingSystem");
            obj.AddComponent<CheatingSystem>();
        }
    }

    // ========== 体测系统 ==========

    private void SetupPhysicalTestSystem()
    {
        if (PhysicalTestSystem.Instance == null)
        {
            GameObject obj = new GameObject("PhysicalTestSystem");
            obj.AddComponent<PhysicalTestSystem>();
        }
    }

    // ========== 校园跑系统 ==========

    private void SetupCampusRunSystem()
    {
        if (CampusRunSystem.Instance == null)
        {
            GameObject obj = new GameObject("CampusRunSystem");
            obj.AddComponent<CampusRunSystem>();
        }
    }

    // ========== 校园新闻系统 ==========

    private void SetupNewsSystem()
    {
        if (NewsSystem.Instance == null)
        {
            GameObject obj = new GameObject("NewsSystem");
            obj.AddComponent<NewsSystem>();
        }
    }

    // ========== 天赋系统 ==========

    private void SetupTalentSystem()
    {
        if (TalentSystem.Instance == null)
        {
            GameObject obj = new GameObject("TalentSystem");
            obj.AddComponent<TalentSystem>();
        }
    }

    private void SetupTalentUI()
    {
        if (TalentUI.Instance == null)
        {
            GameObject obj = new GameObject("TalentUI");
            obj.AddComponent<TalentUI>();
        }
    }

    // ========== 事件系统（原有） ==========

    private void SetupJobSystem()
    {
        if (JobSystem.Instance == null)
        {
            GameObject obj = new GameObject("JobSystem");
            obj.AddComponent<JobSystem>();
        }
    }

    // ========== 惩罚系统 ==========

    private void SetupPenaltySystem()
    {
        if (PenaltySystem.Instance == null)
        {
            GameObject obj = new GameObject("PenaltySystem");
            obj.AddComponent<PenaltySystem>();
        }
    }

    private void SetupSettingsManager()
    {
        if (SettingsManager.Instance == null)
        {
            GameObject obj = new GameObject("SettingsManager");
            obj.AddComponent<SettingsManager>();
        }
    }

    private void SetupAudioManager()
    {
        if (AudioManager.Instance == null)
        {
            GameObject obj = new GameObject("AudioManager");
            obj.AddComponent<AudioManager>();
        }
    }

    private void SetupPauseMenuUI()
    {
        if (PauseMenuUI.Instance == null)
        {
            GameObject obj = new GameObject("PauseMenuUI");
            obj.AddComponent<PauseMenuUI>();
        }
    }

    private void SetupUIEscapeRouter()
    {
        if (FindFirstObjectByType<UIEscapeRouter>() == null)
        {
            GameObject obj = new GameObject("UIEscapeRouter");
            obj.AddComponent<UIEscapeRouter>();
        }
    }

    private void SetupInfoPanelManager()
    {
        if (InfoPanelManager.Instance == null)
        {
            GameObject obj = new GameObject("InfoPanelManager");
            obj.AddComponent<InfoPanelManager>();
        }
    }

    private void SetupInventoryUI()
    {
        if (InventoryUIManager.Instance == null)
        {
            GameObject obj = new GameObject("InventoryUIManager");
            obj.AddComponent<InventoryUIManager>();
        }
    }

    private void SetupMissionUI()
    {
        if (MissionUI.Instance == null)
        {
            GameObject obj = new GameObject("MissionUI");
            obj.AddComponent<MissionUI>();
        }
    }

    private void SetupMissionPanelBuilder()
    {
        if (MissionPanelBuilder.Instance == null)
        {
            GameObject obj = new GameObject("MissionPanelBuilder");
            obj.AddComponent<MissionPanelBuilder>();
        }
    }

    private void SetupJobSelectionUI()
    {
        if (JobSelectionUI.Instance == null)
        {
            GameObject obj = new GameObject("JobSelectionUI");
            obj.AddComponent<JobSelectionUI>();
        }
    }

    private void SetupPhysicalTestUI()
    {
        if (PhysicalTestUI.Instance == null)
        {
            GameObject obj = new GameObject("PhysicalTestUI");
            obj.AddComponent<PhysicalTestUI>();
        }
    }

    private void SetupConfirmDialogUI()
    {
        if (ConfirmDialogUI.Instance == null)
        {
            GameObject obj = new GameObject("ConfirmDialogUI");
            obj.AddComponent<ConfirmDialogUI>();
        }
    }

    private void SetupCourseScheduleUI()
    {
        if (CourseScheduleUI.Instance == null)
        {
            GameObject obj = new GameObject("CourseScheduleUI");
            obj.AddComponent<CourseScheduleUI>();
        }
    }

    // ========== 事件系统（原有） ==========

    private void SetupEventHistory()
    {
        if (EventHistory.Instance == null)
        {
            GameObject obj = new GameObject("EventHistory");
            obj.AddComponent<EventHistory>();
        }
    }

    private void SetupMissionSystem()
    {
        if (MissionSystem.Instance == null)
        {
            GameObject obj = new GameObject("MissionSystem");
            obj.AddComponent<MissionSystem>();
        }
    }

    private void SetupEventScheduler()
    {
        if (EventScheduler.Instance == null)
        {
            GameObject obj = new GameObject("EventScheduler");
            obj.AddComponent<EventScheduler>();
        }

        // 加载事件 JSON 数据
        EventScheduler.Instance.LoadEvents();
    }

    private void SetupEventExecutor()
    {
        if (EventExecutor.Instance == null)
        {
            GameObject obj = new GameObject("EventExecutor");
            obj.AddComponent<EventExecutor>();
        }
    }
}
