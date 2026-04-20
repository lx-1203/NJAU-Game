using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 结局判定器 —— 根据玩家数据和结局定义文件，判定最终结局
/// </summary>
public class EndingDeterminer : MonoBehaviour
{
    // ========== 单例 ==========
    public static EndingDeterminer Instance { get; private set; }

    // ========== 结局定义 ==========

    /// <summary>从 JSON 加载的所有结局定义</summary>
    private List<EndingDefinition> endingDefinitions = new List<EndingDefinition>();

    /// <summary>是否成功加载了结局定义</summary>
    public bool IsLoaded => endingDefinitions.Count > 0;

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

        LoadEndingDefinitions();
    }

    // ========== 数据加载 ==========

    /// <summary>
    /// 从 Resources/Data/endings.json 加载结局定义
    /// </summary>
    private void LoadEndingDefinitions()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/endings");

        if (jsonAsset == null)
        {
            Debug.LogError("[EndingDeterminer] 无法加载 Resources/Data/endings.json，结局定义为空");
            return;
        }

        try
        {
            EndingDataRoot root = JsonUtility.FromJson<EndingDataRoot>(jsonAsset.text);
            if (root != null && root.endings != null)
            {
                endingDefinitions = root.endings;
                Debug.Log($"[EndingDeterminer] 成功加载 {endingDefinitions.Count} 个结局定义");
            }
            else
            {
                Debug.LogError("[EndingDeterminer] JSON 解析结果为空");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EndingDeterminer] JSON 解析失败: {e.Message}");
        }
    }

    // ========== 结局判定 ==========

    /// <summary>
    /// 判定最终结局 —— 按 layer 0→7 遍历，每 layer 内按 stars 降序，第一个满足的结局即为最终结局
    /// </summary>
    public EndingResult DetermineEnding()
    {
        if (endingDefinitions.Count == 0)
        {
            Debug.LogError("[EndingDeterminer] 结局定义为空，无法判定结局");
            return CreateFallbackResult();
        }

        // 按 layer 升序，同 layer 内按 stars 降序排序
        List<EndingDefinition> sorted = endingDefinitions
            .OrderBy(e => e.layer)
            .ThenByDescending(e => e.stars)
            .ToList();

        // 遍历寻找第一个满足所有条件的结局
        foreach (EndingDefinition ending in sorted)
        {
            if (CheckAllConditions(ending))
            {
                Debug.Log($"[EndingDeterminer] 结局判定: {ending.name} (id={ending.id}, " +
                          $"layer={ending.layer}, stars={ending.stars})");
                return BuildEndingResult(ending);
            }
        }

        // 理论上不会到这里 (Layer 7 AlwaysTrue 保底)，但以防万一
        Debug.LogWarning("[EndingDeterminer] 未找到匹配的结局，返回兜底结果");
        return CreateFallbackResult();
    }

    /// <summary>
    /// 检查一个结局定义的所有条件 (AND 关系)
    /// </summary>
    private bool CheckAllConditions(EndingDefinition ending)
    {
        if (ending.conditions == null || ending.conditions.Count == 0)
        {
            // 没有条件 → 视为 AlwaysTrue
            return true;
        }

        foreach (EndingCondition condition in ending.conditions)
        {
            if (!EvaluateCondition(condition))
            {
                return false;
            }
        }

        return true;
    }

    // ========== 条件求值 ==========

    /// <summary>
    /// 求值单个结局条件 —— 根据条件类型从各系统读取数据进行判定
    /// </summary>
    public bool EvaluateCondition(EndingCondition condition)
    {
        EndingConditionType condType = condition.GetConditionType();
        float val = condition.value;

        switch (condType)
        {
            // ========== 属性条件 ==========
            case EndingConditionType.GPA_GreaterOrEqual:
                return GetCurrentGPA() >= val;

            case EndingConditionType.GPA_Less:
                return GetCurrentGPA() < val;

            case EndingConditionType.Study_GreaterOrEqual:
                return GetPlayerAttribute("study") >= val;

            case EndingConditionType.Charm_GreaterOrEqual:
                return GetPlayerAttribute("charm") >= val;

            case EndingConditionType.Physique_GreaterOrEqual:
                return GetPlayerAttribute("physique") >= val;

            case EndingConditionType.Leadership_GreaterOrEqual:
                return GetPlayerAttribute("leadership") >= val;

            case EndingConditionType.Stress_GreaterOrEqual:
                return GetPlayerAttribute("stress") >= val;

            case EndingConditionType.Mood_Equals:
                return Mathf.Approximately(GetPlayerAttribute("mood"), val);

            case EndingConditionType.Mood_Less:
                return GetPlayerAttribute("mood") < val;

            // ========== 状态条件 ==========
            case EndingConditionType.Money_Less:
                return GetMoney() < val;

            case EndingConditionType.Money_GreaterOrEqual:
                return GetMoney() >= val;

            // ========== 社交/组织条件 ==========
            case EndingConditionType.HasPartner:
                return CheckHasPartner();

            case EndingConditionType.RomanceLevel_GreaterOrEqual:
                return GetMaxRomanceHealth() >= val;

            case EndingConditionType.IsStudentCouncilPresident:
                return CheckIsStudentCouncilPresident();

            case EndingConditionType.IsPartyMember:
                return CheckIsPartyMember();

            // ========== 成就/标记条件 ==========
            case EndingConditionType.HasNationalScholarship:
                return EventHistory.Instance != null && EventHistory.Instance.GetFlag("HasNationalScholarship");

            case EndingConditionType.CheatingCount_GreaterOrEqual:
                return CheatingSystem.Instance != null && CheatingSystem.Instance.CaughtCount >= val;

            case EndingConditionType.SlackingValue_GreaterOrEqual:
                return GetSlackingValue() >= val;

            case EndingConditionType.MentalHealth_Equals:
                return Mathf.Approximately(GetMentalHealth(), val);

            // ========== 统计条件 ==========
            case EndingConditionType.TotalStudyCount_GreaterOrEqual:
                return GetStudyCount() >= val;

            case EndingConditionType.TotalSocialCount_GreaterOrEqual:
                return GetSocialCount() >= val;

            case EndingConditionType.GraduationScore_GreaterOrEqual:
                return GetGraduationScore() >= val;

            // ========== 特殊 ==========
            case EndingConditionType.AlwaysTrue:
                return true;

            default:
                Debug.LogWarning($"[EndingDeterminer] 未处理的条件类型: {condType}");
                return false;
        }
    }

    // ========== 数据读取辅助 ==========

    /// <summary>
    /// 获取当前 GPA (从 SemesterSummarySystem 的 Provider 间接获取)
    /// </summary>
    private float GetCurrentGPA()
    {
        // 优先从考试系统获取真实累积GPA
        if (ExamSystem.Instance != null)
        {
            float realGPA = ExamSystem.Instance.GetCumulativeGPA();
            if (realGPA > 0f) return realGPA;
        }
        // 回退: 用学力估算
        if (PlayerAttributes.Instance == null) return 0f;
        float gpa = PlayerAttributes.Instance.Study / 25f;
        return Mathf.Clamp(gpa, 0f, 4.0f);
    }

    /// <summary>
    /// 获取玩家属性值
    /// </summary>
    private float GetPlayerAttribute(string attrName)
    {
        if (PlayerAttributes.Instance == null) return 0f;

        switch (attrName)
        {
            case "study":      return PlayerAttributes.Instance.Study;
            case "charm":      return PlayerAttributes.Instance.Charm;
            case "physique":   return PlayerAttributes.Instance.Physique;
            case "leadership": return PlayerAttributes.Instance.Leadership;
            case "stress":     return PlayerAttributes.Instance.Stress;
            case "mood":       return PlayerAttributes.Instance.Mood;
            default:           return 0f;
        }
    }

    /// <summary>
    /// 获取当前金钱
    /// </summary>
    private float GetMoney()
    {
        return GameState.Instance != null ? GameState.Instance.Money : 0f;
    }

    /// <summary>
    /// 获取总学习次数
    /// </summary>
    private float GetStudyCount()
    {
        return SemesterSummarySystem.Instance != null ? SemesterSummarySystem.Instance.StudyCount : 0f;
    }

    /// <summary>
    /// 获取总社交次数
    /// </summary>
    private float GetSocialCount()
    {
        return SemesterSummarySystem.Instance != null ? SemesterSummarySystem.Instance.SocialCount : 0f;
    }

    /// <summary>
    /// 获取毕业总评分
    /// </summary>
    private float GetGraduationScore()
    {
        return SemesterSummarySystem.Instance != null
            ? SemesterSummarySystem.Instance.CalculateGraduationScore()
            : 0f;
    }

    // ========== 条件判定辅助方法 ==========

    /// <summary>
    /// 检查玩家是否有恋人（任意NPC处于Dating状态）
    /// </summary>
    private bool CheckHasPartner()
    {
        if (RomanceSystem.Instance == null) return false;
        if (NPCDatabase.Instance == null) return false;

        var allNPCs = NPCDatabase.Instance.GetAllNPCs();
        if (allNPCs == null) return false;

        foreach (var npc in allNPCs)
        {
            if (RomanceSystem.Instance.GetRomanceState(npc.id) == RomanceState.Dating)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取所有NPC中最高的恋爱健康度（仅Dating状态的NPC）
    /// </summary>
    private float GetMaxRomanceHealth()
    {
        if (RomanceSystem.Instance == null) return 0f;
        if (NPCDatabase.Instance == null) return 0f;

        var allNPCs = NPCDatabase.Instance.GetAllNPCs();
        if (allNPCs == null) return 0f;

        float maxHealth = 0f;
        foreach (var npc in allNPCs)
        {
            if (RomanceSystem.Instance.GetRomanceState(npc.id) == RomanceState.Dating)
            {
                int health = RomanceSystem.Instance.GetRomanceHealth(npc.id);
                if (health > maxHealth) maxHealth = health;
            }
        }
        return maxHealth;
    }

    /// <summary>
    /// 检查是否是学生会主席（student_council社团最高职位）
    /// </summary>
    private bool CheckIsStudentCouncilPresident()
    {
        if (ClubSystem.Instance == null) return false;

        var membership = ClubSystem.Instance.GetMembership("student_union");
        if (membership == null) return false;

        var currentRank = ClubSystem.Instance.GetCurrentRank("student_union");
        var nextRank = ClubSystem.Instance.GetNextRank("student_union");

        // 如果没有下一级了，说明已经是最高职位（主席）
        return currentRank != null && nextRank == null;
    }

    /// <summary>
    /// 检查是否是正式党员（入党阶段为最终阶段）
    /// </summary>
    private bool CheckIsPartyMember()
    {
        if (ClubSystem.Instance == null) return false;

        int stageCount = ClubSystem.Instance.PartyStageCount;
        if (stageCount <= 0) return false;

        // 最终阶段 = PartyStageCount - 1（正式党员）
        return ClubSystem.Instance.CurrentPartyStage >= stageCount - 1;
    }

    /// <summary>
    /// 计算摆烂值 = max(0, 100 - 学力 - 累计学习次数*2)
    /// 学力低且不学习 → 高摆烂值
    /// </summary>
    private float GetSlackingValue()
    {
        int study = PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Study : 0;
        int studyCount = SemesterSummarySystem.Instance != null ? SemesterSummarySystem.Instance.StudyCount : 0;
        return Mathf.Max(0f, 100f - study - studyCount * 2f);
    }

    /// <summary>
    /// 计算心理健康值 = max(0, 心情 - 压力)
    /// 等于0代表心理健康危机
    /// </summary>
    private float GetMentalHealth()
    {
        if (PlayerAttributes.Instance == null) return 0f;
        return Mathf.Max(0f, PlayerAttributes.Instance.Mood - PlayerAttributes.Instance.Stress);
    }

    // ========== 结果构建 ==========

    /// <summary>
    /// 根据判定的结局构建完整的 EndingResult
    /// </summary>
    private EndingResult BuildEndingResult(EndingDefinition ending)
    {
        EndingResult result = new EndingResult();
        result.ending = ending;

        // 天赋点
        result.talentPoints = EndingResult.CalculateTalentPoints(ending.stars);

        // 毕业总评分
        result.finalScore = SemesterSummarySystem.Instance != null
            ? SemesterSummarySystem.Instance.CalculateGraduationScore()
            : 0f;

        // 统计数据 (从 SemesterSummarySystem 读取)
        if (SemesterSummarySystem.Instance != null)
        {
            result.totalStudyCount = SemesterSummarySystem.Instance.StudyCount;
            result.totalSocialCount = SemesterSummarySystem.Instance.SocialCount;
            result.totalGoOutCount = SemesterSummarySystem.Instance.GoOutCount;
            result.totalSleepCount = SemesterSummarySystem.Instance.SleepCount;
            result.totalMoneySpent = SemesterSummarySystem.Instance.TotalMoneySpent;
        }

        // GPA
        result.finalGPA = GetCurrentGPA();

        // 成就总数
        result.achievementCount = AchievementSystem.Instance != null
            ? AchievementSystem.Instance.GetUnlockedCount()
            : 0;

        // 总回合数
        result.totalRounds = CalculateTotalRounds();

        return result;
    }

    /// <summary>
    /// 创建兜底结局结果 (当无可用结局定义时)
    /// </summary>
    private EndingResult CreateFallbackResult()
    {
        EndingDefinition fallback = new EndingDefinition
        {
            id = "END_FALLBACK",
            name = "普通毕业",
            stars = 1,
            layer = 7,
            description = "你平平淡淡地度过了四年大学生活，拿到了毕业证。",
            cgId = ""
        };

        return BuildEndingResult(fallback);
    }

    /// <summary>
    /// 计算从游戏开始到现在经历的总回合数
    /// </summary>
    private int CalculateTotalRounds()
    {
        if (GameState.Instance == null) return 0;

        int year = GameState.Instance.CurrentYear;
        int semester = GameState.Instance.CurrentSemester;
        int round = GameState.Instance.CurrentRound;

        // 已完成的完整学期数
        int completedSemesters = (year - 1) * 2 + (semester - 1);
        return completedSemesters * GameState.MaxRoundsPerSemester + round;
    }
}
