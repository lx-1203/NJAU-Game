using System;
using System.Collections.Generic;

// ==========================================================================
// 结局数据模型 —— 结局定义、判定层级、条件、结果
// ==========================================================================

/// <summary>
/// 结局判定层级 (0 = 最高优先级)
/// </summary>
public enum EndingLayer
{
    ForcedEnding = 0,      // Layer 0: 强制结局 (自杀/破产/退学/开除)
    PeakEnding = 1,        // Layer 1: 巅峰结局 (7★校园传说 / 6★南农之光)
    PlannedPath = 2,       // Layer 2: 规划路径结局 (保研/考研/考公/参军/创业)
    UnplannedPath = 3,     // Layer 3: 非规划路径结局
    DarkEnding = 4,        // Layer 4: 黑暗结局 (PUA/网贷/赌博)
    SpecialEnding = 5,     // Layer 5: 文体/特殊结局
    NewCareer = 6,         // Layer 6: 新兴职业结局
    FallbackEnding = 7     // Layer 7: 兜底结局 (普通毕业/啃老/流浪)
}

/// <summary>
/// 结局条件类型 —— 用于参数化判定，避免字符串表达式引擎
/// </summary>
public enum EndingConditionType
{
    // ========== 属性条件 ==========
    GPA_GreaterOrEqual,         // GPA >= value
    GPA_Less,                   // GPA < value
    Study_GreaterOrEqual,       // 学力 >= value
    Charm_GreaterOrEqual,       // 魅力 >= value
    Physique_GreaterOrEqual,    // 体魄 >= value
    Leadership_GreaterOrEqual,  // 领导力 >= value
    Stress_GreaterOrEqual,      // 压力 >= value
    Mood_Equals,                // 心情 == value
    Mood_Less,                  // 心情 < value

    // ========== 状态条件 ==========
    Money_Less,                 // 金钱 < value
    Money_GreaterOrEqual,       // 金钱 >= value

    // ========== 社交/组织条件 ==========
    HasPartner,                 // 有恋人
    RomanceLevel_GreaterOrEqual,// 恋爱等级 >= value
    IsStudentCouncilPresident,  // 学生会主席
    IsPartyMember,              // 入党

    // ========== 成就/标记条件 ==========
    HasNationalScholarship,     // 获得国奖
    CheatingCount_GreaterOrEqual, // 作弊次数 >= value
    SlackingValue_GreaterOrEqual, // 摆烂值 >= value
    MentalHealth_Equals,        // 心理健康 == value

    // ========== 统计条件 ==========
    TotalStudyCount_GreaterOrEqual,  // 总学习次数 >= value
    TotalSocialCount_GreaterOrEqual, // 总社交次数 >= value
    GraduationScore_GreaterOrEqual,  // 毕业总评分 >= value

    // ========== 特殊 ==========
    AlwaysTrue                  // 无条件（兜底结局）
}

/// <summary>
/// 单个结局条件
/// </summary>
[Serializable]
public class EndingCondition
{
    public string type;         // 条件类型字符串 (对应 EndingConditionType)
    public float value;         // 比较值

    public EndingCondition() { }

    public EndingCondition(string type, float value)
    {
        this.type = type;
        this.value = value;
    }

    /// <summary>
    /// 解析条件类型枚举
    /// </summary>
    public EndingConditionType GetConditionType()
    {
        if (Enum.TryParse(type, out EndingConditionType result))
            return result;
        UnityEngine.Debug.LogWarning($"[EndingCondition] 未知条件类型: {type}");
        return EndingConditionType.AlwaysTrue;
    }
}

/// <summary>
/// 结局定义 —— 从 JSON 加载，定义单个结局的所有属性
/// </summary>
[Serializable]
public class EndingDefinition
{
    public string id;                   // "END_001"
    public string name;                 // 结局名称
    public int stars;                   // 星级 (0=坏结局, 1-7)
    public int layer;                   // 判定层级 (0-7)
    public List<EndingCondition> conditions;  // 所有条件 (AND 关系)
    public string description;          // 结局文本叙述
    public string cgId;                 // CG图ID (占位)

    public EndingDefinition()
    {
        conditions = new List<EndingCondition>();
    }

    /// <summary>
    /// 获取判定层级枚举
    /// </summary>
    public EndingLayer GetLayer()
    {
        return (EndingLayer)UnityEngine.Mathf.Clamp(layer, 0, 7);
    }
}

/// <summary>
/// JSON 根结构 —— 包裹结局定义数组
/// </summary>
[Serializable]
public class EndingDataRoot
{
    public List<EndingDefinition> endings;

    public EndingDataRoot()
    {
        endings = new List<EndingDefinition>();
    }
}

/// <summary>
/// 结局判定结果 —— 最终输出给 UI 层的完整结局信息
/// </summary>
[Serializable]
public class EndingResult
{
    public EndingDefinition ending;       // 判定的结局
    public int talentPoints;              // 天赋点奖励
    public float finalScore;              // 毕业总评分

    // ========== 大学四年统计数据 ==========
    public int totalStudyCount;           // 总学习次数
    public int totalSocialCount;          // 总社交次数
    public int totalGoOutCount;           // 总出校门次数
    public int totalSleepCount;           // 总睡觉次数
    public int totalMoneySpent;           // 总花费
    public float finalGPA;                // 最终GPA
    public int achievementCount;          // 成就总数
    public int totalRounds;              // 总回合数

    /// <summary>
    /// 根据星级计算天赋点
    /// 7★=10, 6★=8, 5★=6, 4★=5, 3★=4, 2★=2, 1★=1, 0★(坏结局)=0
    /// </summary>
    public static int CalculateTalentPoints(int stars)
    {
        switch (stars)
        {
            case 7: return 10;
            case 6: return 8;
            case 5: return 6;
            case 4: return 5;
            case 3: return 4;
            case 2: return 2;
            case 1: return 1;
            default: return 0;
        }
    }
}
