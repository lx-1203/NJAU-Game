using UnityEngine;

/// <summary>
/// 游戏场景初始化器
/// 确保所有核心系统按正确顺序创建和初始化
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    private void Start()
    {
        // 存档管理器（必须在其他系统之前初始化）
        SetupSaveManager();

        // 多周目传承管理器（必须在 GameState 之前，以便应用传承数据）
        SetupNewGamePlusManager();

        // 初始化核心系统（顺序重要：GameState → PlayerAttributes → LocationManager → ActionSystem → ClubSystem → EconomyManager → DebtSystem → ShopSystem → RomanceSystem → ConfessionSystem → AchievementSystem → AchievementUI → SemesterSummarySystem → EndingDeterminer → TurnManager → ExamSystem → CheatingSystem → EventHistory → EventScheduler → DialogueSystem → EventExecutor）
        SetupGameState();
        SetupPlayerAttributes();
        SetupLocationManager();
        SetupActionSystem();
        SetupClubSystem();
        SetupEconomyManager();
        SetupDebtSystem();
        SetupShopSystem();
        SetupRomanceSystem();
        SetupConfessionSystem();
        SetupCampusRunSystem();
        SetupPhysicalTestSystem();
        SetupAchievementSystem();
        SetupAchievementUI();
        SetupSemesterSummarySystem();
        SetupEndingDeterminer();
        SetupTurnManager();
        SetupExamSystem();
        SetupCheatingSystem();
        SetupEventHistory();
        SetupEventScheduler();
        SetupDialogueSystem();

        // 加载对话 JSON 数据（必须在 DialogueSystem 初始化之后）
        DialogueParser.LoadAllDialogues();

        // 初始化事件执行器（必须在 DialogueSystem 之后，因为它依赖 IDialogueTrigger）
        SetupEventExecutor();

        // 初始化 NPC 相关系统（顺序重要：NPCEventHub → NPCDatabase → AffinitySystem → NPCManager）
        SetupNPCEventHub();
        SetupNPCDatabase();
        SetupAffinitySystem();
        SetupRomanceBridge();
        SetupNPCManager();

        // 挂载区域检测器到玩家（走路自动切换地点）
        SetupLocationZoneDetector();

        // 校园新闻系统（依赖 PlayerAttributes, AffinitySystem, NPCDatabase, ClubSystem）
        SetupNewsSystem();

        // 天赋系统
        SetupTalentSystem();
        SetupTalentUI();

        // 工作/实习/副业系统
        SetupJobSystem();

        // 惩罚系统（依赖 TurnManager, ActionSystem, PlayerAttributes, TalentSystem）
        SetupPenaltySystem();

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
    }

    private void SetupLocationManager()
    {
        if (LocationManager.Instance == null)
        {
            GameObject obj = new GameObject("LocationManager");
            obj.AddComponent<LocationManager>();
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<LocationZoneDetector>() == null)
        {
            player.AddComponent<LocationZoneDetector>();
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

    // ========== 事件系统（原有） ==========

    private void SetupEventHistory()
    {
        if (EventHistory.Instance == null)
        {
            GameObject obj = new GameObject("EventHistory");
            obj.AddComponent<EventHistory>();
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
