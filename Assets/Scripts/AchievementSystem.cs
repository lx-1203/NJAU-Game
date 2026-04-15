using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 成就系统 - 管理成就定义加载、条件检测、解锁与持久化
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    // ========== 单例 ==========

    public static AchievementSystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>
    /// 成就解锁时触发，供 UI 弹窗监听
    /// </summary>
    public event Action<AchievementDefinition> OnAchievementUnlocked;

    // ========== 数据 ==========

    /// <summary>
    /// 所有成就定义（从 JSON 加载）
    /// </summary>
    private List<AchievementDefinition> allDefinitions = new List<AchievementDefinition>();

    /// <summary>
    /// 已解锁成就的运行时状态，key = 成就 id
    /// </summary>
    private Dictionary<string, AchievementRuntimeState> unlockedStates = new Dictionary<string, AchievementRuntimeState>();

    /// <summary>
    /// 当前学期内解锁的成就 id 列表
    /// </summary>
    private List<string> currentSemesterAchievements = new List<string>();

    // ========== 内部统计（备用） ==========

    /// <summary>
    /// 简单估算的总行动次数（每次 OnActionExecuted 时 +1）
    /// </summary>
    private int totalRoundsPlayed = 0;

    // ========== 常量 ==========

    private const string SAVE_KEY = "AchievementData";

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 从 JSON 加载成就定义
        LoadDefinitions();

        // 从 PlayerPrefs 加载已解锁状态
        LoadAchievements();
    }

    private void Start()
    {
        // 订阅行动系统事件（安全检查）
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted += OnActionExecuted;
        }
        else
        {
            Debug.LogWarning("[AchievementSystem] ActionSystem.Instance 为 null，将在后续重试订阅");
        }
    }

    private void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted -= OnActionExecuted;
        }
    }

    // ========== 数据加载 ==========

    /// <summary>
    /// 从 Resources/Data/achievements.json 加载成就定义
    /// </summary>
    private void LoadDefinitions()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/achievements");
        if (jsonAsset == null)
        {
            Debug.LogError("[AchievementSystem] 无法加载成就定义文件: Resources/Data/achievements.json");
            return;
        }

        try
        {
            AchievementDataRoot root = JsonUtility.FromJson<AchievementDataRoot>(jsonAsset.text);
            if (root != null && root.achievements != null)
            {
                allDefinitions = root.achievements;
                Debug.Log($"[AchievementSystem] 成功加载 {allDefinitions.Count} 个成就定义");
            }
            else
            {
                Debug.LogWarning("[AchievementSystem] 成就定义为空");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AchievementSystem] 解析成就 JSON 失败: {e.Message}");
        }
    }

    // ========== 事件回调 ==========

    /// <summary>
    /// 行动执行后的回调，触发成就检测
    /// </summary>
    private void OnActionExecuted(ActionDefinition action)
    {
        totalRoundsPlayed++;
        CheckAchievements();
    }

    // ========== 成就检测 ==========

    /// <summary>
    /// 遍历所有成就定义，检查未解锁的是否满足条件
    /// </summary>
    public void CheckAchievements()
    {
        foreach (var def in allDefinitions)
        {
            // 跳过已解锁的
            if (IsUnlocked(def.id))
                continue;

            // 检查所有条件是否满足
            bool allConditionsMet = true;
            if (def.conditions != null)
            {
                foreach (var condition in def.conditions)
                {
                    if (!EvaluateCondition(condition))
                    {
                        allConditionsMet = false;
                        break;
                    }
                }
            }

            if (allConditionsMet)
            {
                UnlockAchievement(def);
            }
        }
    }

    /// <summary>
    /// 条件求值：根据条件类型读取对应系统数据并比较
    /// </summary>
    private bool EvaluateCondition(AchievementCondition condition)
    {
        AchievementConditionType condType = condition.GetConditionType();

        switch (condType)
        {
            // ========== 玩家属性 ==========
            case AchievementConditionType.Study_GreaterOrEqual:
                return PlayerAttributes.Instance != null &&
                       PlayerAttributes.Instance.Study >= condition.value;

            case AchievementConditionType.Charm_GreaterOrEqual:
                return PlayerAttributes.Instance != null &&
                       PlayerAttributes.Instance.Charm >= condition.value;

            case AchievementConditionType.Physique_GreaterOrEqual:
                return PlayerAttributes.Instance != null &&
                       PlayerAttributes.Instance.Physique >= condition.value;

            case AchievementConditionType.Leadership_GreaterOrEqual:
                return PlayerAttributes.Instance != null &&
                       PlayerAttributes.Instance.Leadership >= condition.value;

            case AchievementConditionType.Stress_GreaterOrEqual:
                return PlayerAttributes.Instance != null &&
                       PlayerAttributes.Instance.Stress >= condition.value;

            case AchievementConditionType.Mood_GreaterOrEqual:
                return PlayerAttributes.Instance != null &&
                       PlayerAttributes.Instance.Mood >= condition.value;

            // ========== 学期行动统计（优先读 SemesterSummarySystem） ==========
            case AchievementConditionType.StudyCount_GreaterOrEqual:
                if (SemesterSummarySystem.Instance != null)
                    return SemesterSummarySystem.Instance.StudyCount >= condition.value;
                return false;

            case AchievementConditionType.SocialCount_GreaterOrEqual:
                if (SemesterSummarySystem.Instance != null)
                    return SemesterSummarySystem.Instance.SocialCount >= condition.value;
                return false;

            case AchievementConditionType.GoOutCount_GreaterOrEqual:
                if (SemesterSummarySystem.Instance != null)
                    return SemesterSummarySystem.Instance.GoOutCount >= condition.value;
                return false;

            case AchievementConditionType.SleepCount_GreaterOrEqual:
                if (SemesterSummarySystem.Instance != null)
                    return SemesterSummarySystem.Instance.SleepCount >= condition.value;
                return false;

            // ========== 金钱 ==========
            case AchievementConditionType.Money_GreaterOrEqual:
                return GameState.Instance != null &&
                       GameState.Instance.Money >= condition.value;

            case AchievementConditionType.Money_Less:
                return GameState.Instance != null &&
                       GameState.Instance.Money < condition.value;

            case AchievementConditionType.TotalSpent_GreaterOrEqual:
                if (SemesterSummarySystem.Instance != null)
                    return SemesterSummarySystem.Instance.TotalMoneySpent >= condition.value;
                return false;

            // ========== GPA ==========
            case AchievementConditionType.GPA_GreaterOrEqual:
                float gpa = EstimateGPA();
                return gpa >= condition.value;

            // ========== 时间 ==========
            case AchievementConditionType.Year_GreaterOrEqual:
                return GameState.Instance != null &&
                       GameState.Instance.CurrentYear >= condition.value;

            case AchievementConditionType.Semester_Equals:
                return GameState.Instance != null &&
                       Mathf.Approximately(GameState.Instance.CurrentSemester, condition.value);

            case AchievementConditionType.TotalRounds_GreaterOrEqual:
                return EstimateTotalRounds() >= condition.value;

            // ========== 社交 ==========
            case AchievementConditionType.FriendCount_GreaterOrEqual:
                return AffinitySystem.Instance != null &&
                       AffinitySystem.Instance.GetFriendOrAboveCount() >= condition.value;

            // ========== 学期成绩 ==========
            case AchievementConditionType.SemesterGrade_Equals:
                if (SemesterSummarySystem.Instance == null) return false;
                {
                    // 获取最近完成学期的评级
                    var summaries = SemesterSummarySystem.Instance.GetAllSummaries();
                    if (summaries.Count == 0) return false;
                    // 找到最近一个学期的评级
                    SemesterGrade latestGrade = SemesterGrade.D;
                    foreach (var kvp in summaries)
                    {
                        latestGrade = kvp.Value.grade;
                    }
                    return (int)latestGrade == (int)condition.value;
                }

            // ========== 综合属性 ==========
            case AchievementConditionType.AllAttributes_GreaterOrEqual:
                return PlayerAttributes.Instance != null &&
                       PlayerAttributes.Instance.Study >= condition.value &&
                       PlayerAttributes.Instance.Charm >= condition.value &&
                       PlayerAttributes.Instance.Physique >= condition.value &&
                       PlayerAttributes.Instance.Leadership >= condition.value;

            default:
                Debug.LogWarning($"[AchievementSystem] 未处理的条件类型: {condType}");
                return false;
        }
    }

    /// <summary>
    /// 估算 GPA：优先从 SemesterSummarySystem 读取，否则用 Study/25 估算
    /// </summary>
    private float EstimateGPA()
    {
        // 优先从考试系统获取真实累积GPA
        if (ExamSystem.Instance != null)
        {
            float realGPA = ExamSystem.Instance.GetCumulativeGPA();
            if (realGPA > 0f) return realGPA;
        }

        // 备用估算：Study / 25（满分 100 对应 GPA 4.0）
        if (PlayerAttributes.Instance != null)
            return PlayerAttributes.Instance.Study / 25f;

        return 0f;
    }

    /// <summary>
    /// 估算总回合数：(year-1)*80 + (semester-1)*40 + round
    /// </summary>
    private float EstimateTotalRounds()
    {
        if (GameState.Instance == null) return totalRoundsPlayed;

        int year = GameState.Instance.CurrentYear;
        int semester = GameState.Instance.CurrentSemester;
        int round = GameState.Instance.CurrentRound;

        return (year - 1) * 80 + (semester - 1) * 40 + round;
    }

    // ========== 解锁 ==========

    /// <summary>
    /// 解锁成就：标记状态、记录时间、触发事件、持久化
    /// </summary>
    private void UnlockAchievement(AchievementDefinition def)
    {
        if (unlockedStates.ContainsKey(def.id))
            return;

        // 创建运行时状态
        AchievementRuntimeState state = new AchievementRuntimeState
        {
            id = def.id,
            unlocked = true,
            unlockTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        unlockedStates[def.id] = state;

        // 记录到当前学期成就列表
        currentSemesterAchievements.Add(def.id);

        Debug.Log($"[AchievementSystem] 成就解锁: {def.name} ({def.id})");

        // 持久化
        SaveAchievements();

        // 触发事件（供 UI 弹窗等监听）
        OnAchievementUnlocked?.Invoke(def);
    }

    // ========== 持久化 ==========

    /// <summary>
    /// 将已解锁状态序列化到 PlayerPrefs
    /// </summary>
    public void SaveAchievements()
    {
        AchievementSaveData saveData = new AchievementSaveData
        {
            states = new List<AchievementRuntimeState>(unlockedStates.Values)
        };

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从 PlayerPrefs 反序列化已解锁状态
    /// </summary>
    public void LoadAchievements()
    {
        unlockedStates.Clear();

        if (!PlayerPrefs.HasKey(SAVE_KEY))
            return;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            AchievementSaveData saveData = JsonUtility.FromJson<AchievementSaveData>(json);
            if (saveData != null && saveData.states != null)
            {
                foreach (var state in saveData.states)
                {
                    if (state.unlocked)
                    {
                        unlockedStates[state.id] = state;
                    }
                }
                Debug.Log($"[AchievementSystem] 从存档加载 {unlockedStates.Count} 个已解锁成就");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AchievementSystem] 加载成就存档失败: {e.Message}");
        }
    }

    // ========== 查询接口 ==========

    /// <summary>
    /// 获取所有成就定义
    /// </summary>
    public List<AchievementDefinition> GetAllAchievements()
    {
        return allDefinitions;
    }

    /// <summary>
    /// 获取所有已解锁的成就定义
    /// </summary>
    public List<AchievementDefinition> GetUnlockedAchievements()
    {
        return allDefinitions.Where(def => IsUnlocked(def.id)).ToList();
    }

    /// <summary>获取已解锁成就总数</summary>
    public int GetUnlockedCount()
    {
        return unlockedStates.Count;
    }

    /// <summary>
    /// 查询指定成就是否已解锁
    /// </summary>
    public bool IsUnlocked(string id)
    {
        return unlockedStates.ContainsKey(id);
    }

    /// <summary>
    /// 获取已解锁成就的总积分
    /// </summary>
    public int GetTotalAchievementScore()
    {
        int total = 0;
        foreach (var def in allDefinitions)
        {
            if (IsUnlocked(def.id))
            {
                total += def.points;
            }
        }
        return total;
    }

    // ========== 学期成就追踪 ==========

    /// <summary>
    /// 获取当前学期内解锁的成就 id 列表
    /// </summary>
    public List<string> GetSemesterAchievements()
    {
        return new List<string>(currentSemesterAchievements);
    }

    /// <summary>
    /// 重置学期成就列表，在学期切换时由 SemesterSummarySystem 调用
    /// </summary>
    public void ResetSemesterAchievements()
    {
        currentSemesterAchievements.Clear();
    }
}
