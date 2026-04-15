using UnityEngine;
using System;
using System.Collections.Generic;

// ========================================================================
//  NPC 数据模型
//  定义 NPC 静态数据、关系数据、社交行动定义与扩展接口
// ========================================================================

// ========== 枚举 ==========

/// <summary>好感度等级（6级）</summary>
public enum AffinityLevel
{
    Stranger,       // 0-19
    Acquaintance,   // 20-39
    Friend,         // 40-59
    CloseFriend,    // 60-79
    BestFriend,     // 80-100
    Lover           // 80-100（需特殊解锁）
}

/// <summary>NPC 类型</summary>
public enum NPCType
{
    Roommate,       // 室友
    Senior,         // 学长/学姐
    Classmate,      // 同学
    Teacher,        // 老师
    Other
}

/// <summary>NPC 性格</summary>
public enum NPCPersonality
{
    Introvert,      // 内向
    Extrovert,      // 外向
    Easygoing,      // 随性/佛系
    Mysterious,     // 神秘
    Cheerful,       // 开朗
    Serious         // 严肃
}

// RomanceState 枚举已移至 RomanceData.cs（含完整恋爱状态机）

// ========== 时间段 ==========

/// <summary>一天内的时间段（用于日程系统）</summary>
public enum TimeSlot
{
    Morning,        // 早上（行动点 5-4）
    Afternoon,      // 下午（行动点 3-2）
    Evening         // 晚上（行动点 1）
}

// ========== NPC 日程条目 ==========

[Serializable]
public class NPCScheduleEntry
{
    public string timeSlot;     // "Morning" / "Afternoon" / "Evening"
    public string location;     // 出现地点

    [NonSerialized]
    private TimeSlot? _parsedSlot;

    public TimeSlot GetTimeSlot()
    {
        if (_parsedSlot == null)
        {
            if (Enum.TryParse(timeSlot, true, out TimeSlot slot))
                _parsedSlot = slot;
            else
                _parsedSlot = TimeSlot.Morning;
        }
        return _parsedSlot.Value;
    }
}

// ========== NPC 静态数据 ==========

[Serializable]
public class NPCData
{
    public string id;               // "NPC_RoommateA"
    public string displayName;      // "林知秋"
    public string type;             // "Roommate"
    public string personality;      // "Introvert"
    public string description;      // 简介
    public string portraitId;       // 立绘资源ID

    public string[] likedActionIds;     // 喜好行动（+好感修正）
    public string[] dislikedActionIds;  // 厌恶行动（-好感修正）

    public NPCScheduleEntry[] schedule; // 日程
    public string[] greetingLines;      // 默认打招呼台词
    public string dialogueId;           // JSON 对话 ID（优先于 greetingLines 启动数据驱动对话）

    // 缓存
    [NonSerialized] private NPCType? _parsedType;
    [NonSerialized] private NPCPersonality? _parsedPersonality;
    [NonSerialized] private HashSet<string> _likedSet;
    [NonSerialized] private HashSet<string> _dislikedSet;

    public NPCType GetNPCType()
    {
        if (_parsedType == null)
            _parsedType = Enum.TryParse(type, true, out NPCType t) ? t : NPCType.Other;
        return _parsedType.Value;
    }

    public NPCPersonality GetPersonality()
    {
        if (_parsedPersonality == null)
            _parsedPersonality = Enum.TryParse(personality, true, out NPCPersonality p) ? p : NPCPersonality.Easygoing;
        return _parsedPersonality.Value;
    }

    public bool LikesAction(string actionId)
    {
        if (_likedSet == null)
            _likedSet = likedActionIds != null ? new HashSet<string>(likedActionIds) : new HashSet<string>();
        return _likedSet.Contains(actionId);
    }

    public bool DislikesAction(string actionId)
    {
        if (_dislikedSet == null)
            _dislikedSet = dislikedActionIds != null ? new HashSet<string>(dislikedActionIds) : new HashSet<string>();
        return _dislikedSet.Contains(actionId);
    }
}

// ========== NPC 数据库 JSON 根 ==========

[Serializable]
public class NPCDatabaseRoot
{
    public NPCData[] npcs;
    public SocialActionDefinition[] socialActions;
}

// ========== 社交行动定义 ==========

[Serializable]
public class SocialActionDefinition
{
    public string id;                // "greet", "chat", "eat_together", "give_gift" …
    public string displayName;       // "打招呼", "聊天" …
    public int actionPointCost;      // 行动点消耗
    public int moneyCost;            // 金钱消耗
    public string minAffinityLevel;  // 最低好感等级 "Stranger" / "Friend" …
    public int baseAffinityMin;      // 基础好感增量下限
    public int baseAffinityMax;      // 基础好感增量上限
    public AttributeEffect[] attributeEffects; // 附带属性效果

    [NonSerialized] private AffinityLevel? _parsedMinLevel;

    public AffinityLevel GetMinAffinityLevel()
    {
        if (_parsedMinLevel == null)
            _parsedMinLevel = Enum.TryParse(minAffinityLevel, true, out AffinityLevel l) ? l : AffinityLevel.Stranger;
        return _parsedMinLevel.Value;
    }
}

// ========== NPC 关系数据（运行时） ==========

[Serializable]
public class NPCRelationshipData
{
    public string npcId;
    public int affinity;                         // 0-100
    public AffinityLevel level;
    public int consecutiveNoInteractionTurns;    // 连续无互动回合数
    public string lastInteractionActionId;       // 上次互动行动ID
    public int repeatedActionCount;              // 连续相同行动次数
    public List<string> memories;                // 互动记忆列表
    public RomanceState romanceState;            // 预留恋爱状态

    public NPCRelationshipData(string npcId)
    {
        this.npcId = npcId;
        this.affinity = 0;
        this.level = AffinityLevel.Stranger;
        this.consecutiveNoInteractionTurns = 0;
        this.lastInteractionActionId = "";
        this.repeatedActionCount = 0;
        this.memories = new List<string>();
        this.romanceState = RomanceState.None;
    }
}

// ========== 扩展接口（为独立恋爱模块预留） ==========

/// <summary>
/// 关系扩展接口：由恋爱系统模块实现以挂载告白/约会/分手逻辑
/// </summary>
public interface IRelationshipExtension
{
    /// <summary>当好感度等级变化时调用</summary>
    void OnAffinityLevelChanged(string npcId, AffinityLevel oldLevel, AffinityLevel newLevel);

    /// <summary>当社交互动执行后调用</summary>
    void OnInteractionCompleted(string npcId, string socialActionId, int affinityDelta);

    /// <summary>检查是否允许进入 Lover 等级</summary>
    bool CanEnterLoverLevel(string npcId);
}
