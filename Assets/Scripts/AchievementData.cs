using System;
using System.Collections.Generic;

// ==========================================================================
// 成就数据模型 —— 成就定义、条件、运行时状态
// ==========================================================================

/// <summary>
/// 成就条件类型
/// </summary>
public enum AchievementConditionType
{
    // ========== 属性达标 ==========
    Study_GreaterOrEqual,          // 学力 >= value
    Charm_GreaterOrEqual,          // 魅力 >= value
    Physique_GreaterOrEqual,       // 体魄 >= value
    Leadership_GreaterOrEqual,     // 领导力 >= value
    Stress_GreaterOrEqual,         // 压力 >= value
    Mood_GreaterOrEqual,           // 心情 >= value

    // ========== 行动累计 ==========
    StudyCount_GreaterOrEqual,     // 学习次数 >= value
    SocialCount_GreaterOrEqual,    // 社交次数 >= value
    GoOutCount_GreaterOrEqual,     // 出校门次数 >= value
    SleepCount_GreaterOrEqual,     // 睡觉次数 >= value

    // ========== 资源 ==========
    Money_GreaterOrEqual,          // 金钱 >= value
    Money_Less,                    // 金钱 < value
    TotalSpent_GreaterOrEqual,     // 总花费 >= value

    // ========== GPA ==========
    GPA_GreaterOrEqual,            // GPA >= value

    // ========== 时间 ==========
    Year_GreaterOrEqual,           // 学年 >= value
    Semester_Equals,               // 学期 == value
    TotalRounds_GreaterOrEqual,    // 总游玩回合 >= value

    // ========== 社交 ==========
    FriendCount_GreaterOrEqual,    // 好友数 >= value

    // ========== 特殊 ==========
    SemesterGrade_Equals,          // 学期评级 == value (0=D,1=C,2=B,3=A,4=S)

    // ========== 复合（预留） ==========
    AllAttributes_GreaterOrEqual   // 所有核心属性 >= value
}

/// <summary>
/// 单个成就条件
/// </summary>
[Serializable]
public class AchievementCondition
{
    public string type;     // 条件类型字符串 (对应 AchievementConditionType)
    public float value;     // 比较值

    public AchievementCondition() { }

    public AchievementCondition(string type, float value)
    {
        this.type = type;
        this.value = value;
    }

    /// <summary>
    /// 解析条件类型枚举
    /// </summary>
    public AchievementConditionType GetConditionType()
    {
        if (Enum.TryParse(type, out AchievementConditionType result))
            return result;
        UnityEngine.Debug.LogWarning($"[AchievementCondition] 未知条件类型: {type}");
        return AchievementConditionType.TotalRounds_GreaterOrEqual;
    }
}

/// <summary>
/// 成就定义 —— 从 JSON 加载
/// </summary>
[Serializable]
public class AchievementDefinition
{
    public string id;                           // "ACH_001"
    public string name;                         // 成就名称
    public string description;                  // 成就描述
    public List<AchievementCondition> conditions; // 解锁条件 (AND 关系)
    public string iconId;                       // 图标ID (占位)
    public int points;                          // 成就分值

    public AchievementDefinition()
    {
        conditions = new List<AchievementCondition>();
    }
}

/// <summary>
/// JSON 根结构 —— 包裹成就定义数组
/// </summary>
[Serializable]
public class AchievementDataRoot
{
    public List<AchievementDefinition> achievements;

    public AchievementDataRoot()
    {
        achievements = new List<AchievementDefinition>();
    }
}

/// <summary>
/// 成就运行时状态 —— 记录解锁信息
/// </summary>
[Serializable]
public class AchievementRuntimeState
{
    public string id;               // 成就ID
    public bool unlocked;           // 是否已解锁
    public string unlockTime;       // 解锁时间 (ISO 8601)

    public AchievementRuntimeState() { }

    public AchievementRuntimeState(string id)
    {
        this.id = id;
        this.unlocked = false;
        this.unlockTime = "";
    }
}

/// <summary>
/// 成就持久化数据 —— 整体序列化到 PlayerPrefs
/// </summary>
[Serializable]
public class AchievementSaveData
{
    public List<AchievementRuntimeState> states;

    public AchievementSaveData()
    {
        states = new List<AchievementRuntimeState>();
    }
}
