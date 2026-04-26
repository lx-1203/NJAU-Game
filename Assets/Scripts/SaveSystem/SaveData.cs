using System;
using System.Collections.Generic;

// ========================================================================
//  存档数据结构 —— 序列化为 JSON 持久化到磁盘
//  使用 JsonUtility 兼容序列化（无 Dictionary，全部用 List）
// ========================================================================

/// <summary>存档元信息</summary>
[Serializable]
public class SaveMetaInfo
{
    /// <summary>存档时间 (ISO 8601)</summary>
    public string saveTime;
    /// <summary>进度描述 (如 "大二上 · 回合15 · 10月")</summary>
    public string progressDesc;
    /// <summary>游戏累计时长（秒）</summary>
    public float playTimeSeconds;
    /// <summary>槽位索引 0=auto, 1-3=manual</summary>
    public int slotIndex;
}

/// <summary>NPC 关系存档数据</summary>
[Serializable]
public class NPCRelationshipSaveData
{
    public string npcId;
    public int affinity;
    public string affinityLevel;        // AffinityLevel 枚举名
    public int consecutiveNoInteractionTurns;
    public string lastInteractionActionId;
    public int repeatedActionCount;
    public List<string> memories;
    public string romanceState;         // RomanceState 枚举名

    public NPCRelationshipSaveData() { memories = new List<string>(); }

    /// <summary>从运行时 NPCRelationshipData 转换</summary>
    public static NPCRelationshipSaveData FromRuntime(NPCRelationshipData rd)
    {
        return new NPCRelationshipSaveData
        {
            npcId = rd.npcId,
            affinity = rd.affinity,
            affinityLevel = rd.level.ToString(),
            consecutiveNoInteractionTurns = rd.consecutiveNoInteractionTurns,
            lastInteractionActionId = rd.lastInteractionActionId,
            repeatedActionCount = rd.repeatedActionCount,
            memories = rd.memories != null ? new List<string>(rd.memories) : new List<string>(),
            romanceState = rd.romanceState.ToString()
        };
    }

    /// <summary>恢复到运行时 NPCRelationshipData</summary>
    public NPCRelationshipData ToRuntime()
    {
        var rd = new NPCRelationshipData(npcId);
        rd.affinity = affinity;
        if (Enum.TryParse(affinityLevel, true, out AffinityLevel lvl)) rd.level = lvl;
        rd.consecutiveNoInteractionTurns = consecutiveNoInteractionTurns;
        rd.lastInteractionActionId = lastInteractionActionId;
        rd.repeatedActionCount = repeatedActionCount;
        rd.memories = memories != null ? new List<string>(memories) : new List<string>();
        if (Enum.TryParse(romanceState, true, out RomanceState rs)) rd.romanceState = rs;
        return rd;
    }
}

/// <summary>课程成绩记录（由 ExamSystem 写入期末/补考成绩）</summary>
[Serializable]
public class CourseRecord
{
    public string courseName;
    public int score;
    public int year;
    public int semester;
}

// TransactionRecord 定义在 EconomyManager.cs 中（含 TransactionType 枚举）
// SaveData.transactionRecords 直接使用该类型

/// <summary>社团成员完整存档记录</summary>
[Serializable]
public class ClubMemberRecord
{
    public string clubId;
    public string clubName;
    public string role;
    public int currentRank;
    public int joinedAtRound;
    public int roundsInClub;
    public int eventChainProgress;
    public int inactiveRounds;           // 连续未活动回合数
}

/// <summary>键值对（替代 Dictionary&lt;string,int&gt;，兼容 JsonUtility）</summary>
[Serializable]
public class StringIntPair
{
    public string key;
    public int value;

    public StringIntPair() { }
    public StringIntPair(string key, int value) { this.key = key; this.value = value; }
}

/// <summary>键值对（替代 Dictionary&lt;string,bool&gt;，兼容 JsonUtility）</summary>
[Serializable]
public class StringBoolPair
{
    public string key;
    public bool value;

    public StringBoolPair() { }
    public StringBoolPair(string key, bool value) { this.key = key; this.value = value; }
}

/// <summary>事件历史记录（用于存档序列化）</summary>
[Serializable]
public class EventHistoryRecord
{
    public string eventId;
    public int triggerYear;
    public int triggerSemester;
    public int triggerRound;
    public int choiceIndex;
}

// ========================================================================

/// <summary>
/// 核心存档数据结构
/// </summary>
[Serializable]
public class SaveData
{
    // ========== 版本 ==========
    public int version = 1;

    // ========== 元信息 ==========
    public SaveMetaInfo meta = new SaveMetaInfo();

    // ========== 玩家身份 ==========
    public string playerName = "";
    public int playerGender = 0;  // 0=男, 1=女
    public string playerMajor = "";

    // ========== 游戏状态 (GameState) ==========
    public int currentYear = 1;
    public int currentSemester = 1;
    public int currentRound = 1;
    public int currentMonth = 9;
    public int money = 1488;
    public int actionPoints = 20;

    // ========== 玩家属性 (PlayerAttributes) ==========
    public int study = 10;
    public int charm = 5;
    public int physique = 8;
    public int leadership = 3;
    public int stress = 20;
    public int mood = 70;
    public int darkness = 0;
    public int guilt = 0;
    public int luck = 50;

    // ========== NPC 关系数据 ==========
    public List<NPCRelationshipSaveData> npcRelationships = new List<NPCRelationshipSaveData>();

    // ========== 事件历史 ==========
    public List<string> triggeredEventIds = new List<string>();
    /// <summary>事件状态 (eventId → state)，用 List 替代 Dictionary</summary>
    public List<StringIntPair> eventStates = new List<StringIntPair>();
    /// <summary>完整事件记录（含触发时间、选项索引）</summary>
    public List<EventHistoryRecord> eventRecords = new List<EventHistoryRecord>();
    /// <summary>全局标记字典</summary>
    public List<StringBoolPair> eventFlags = new List<StringBoolPair>();
    /// <summary>黑暗值</summary>
    public int darknessValue = 0;

    // ========== 成就 ==========
    public List<string> unlockedAchievements = new List<string>();

    // ========== 地点 ==========
    public string currentLocation = "";

    // ========== 课程 & 成绩（ExamSystem 存档） ==========
    public List<CourseRecord> courseRecords = new List<CourseRecord>();
    public List<SemesterGPA> semesterGPAHistory = new List<SemesterGPA>();
    public List<ExamResult> failedCourses = new List<ExamResult>();
    public int studyCountThisSemester = 0;
    public bool cet4Passed = false;
    public bool cet6Passed = false;
    public bool computerLevelPassed = false;
    public List<ExamResult> lastMidtermResults = new List<ExamResult>();

    // ========== 作弊系统（CheatingSystem 存档） ==========
    public int cheatCaughtCount = 0;
    public int cheatTotalAttempts = 0;

    // ========== 行动统计（SemesterSummarySystem 存档） ==========
    public int studyCount = 0;
    public int socialCount = 0;
    public int goOutCount = 0;
    public int sleepCount = 0;
    public int totalMoneySpent = 0;

    // ========== 交易记录（EconomyManager 存档） ==========
    public List<TransactionRecord> transactionRecords = new List<TransactionRecord>();

    // ========== 社团 ==========
    public List<ClubMemberRecord> clubRecords = new List<ClubMemberRecord>();
    public int currentPartyStage = 0;
    public int partyApplicationRound = 0;
    /// <summary>退出冷却记录 (clubId → 剩余回合数)</summary>
    public List<StringIntPair> clubExitCooldowns = new List<StringIntPair>();

    // ========== 校园跑（CampusRunSystem 存档） ==========
    public int campusRunCompleted = 0;
    public int campusRunProxy = 0;

    // ========== 任务系统（MissionSystem 存档） ==========
    public MissionSaveData missionData = new MissionSaveData();

    // ========== 游戏时长 ==========
    public float totalPlayTimeSeconds = 0f;

    public void EnsureInitialized()
    {
        meta ??= new SaveMetaInfo();
        npcRelationships ??= new List<NPCRelationshipSaveData>();
        triggeredEventIds ??= new List<string>();
        eventStates ??= new List<StringIntPair>();
        eventRecords ??= new List<EventHistoryRecord>();
        eventFlags ??= new List<StringBoolPair>();
        unlockedAchievements ??= new List<string>();
        courseRecords ??= new List<CourseRecord>();
        semesterGPAHistory ??= new List<SemesterGPA>();
        failedCourses ??= new List<ExamResult>();
        lastMidtermResults ??= new List<ExamResult>();
        transactionRecords ??= new List<TransactionRecord>();
        clubRecords ??= new List<ClubMemberRecord>();
        clubExitCooldowns ??= new List<StringIntPair>();
        missionData ??= new MissionSaveData();
        missionData.EnsureInitialized();
    }
}
