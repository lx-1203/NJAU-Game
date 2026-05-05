using System;
using System.Collections.Generic;

// ========================================================================
//  游戏事件数据模型
//  定义事件系统所有 [Serializable] 数据类，供 JsonUtility 反序列化
// ========================================================================

// ========== 枚举 ==========

/// <summary>事件类型</summary>
public enum EventType
{
    Fixed,          // FE - 固定事件（每回合触发）
    MainStory,      // ME - 主线事件（特定回合触发）
    Conditional,    // CE - 条件事件（满足条件触发）
    Dark            // DE - 黑暗事件（黑暗值/行为触发）
}

/// <summary>事件优先级（数值越小越高）</summary>
public enum EventPriority
{
    Forced = 0,         // 强制（黑暗事件中的强制类型）
    MainStory = 1,      // 主线
    Conditional = 2,    // 条件
    Fixed = 3           // 固定
}

/// <summary>事件触发阶段</summary>
public enum TriggerPhase
{
    RoundStart,         // 回合开始（强制阶段）
    ActionComplete,     // 行动完成后
    RoundEnd            // 回合结算
}

// ========== 触发条件 ==========

/// <summary>属性条件（某属性与阈值的比较）</summary>
[Serializable]
public class AttributeCondition
{
    /// <summary>属性名："学力", "魅力", "体魄", "领导力", "压力", "心情"</summary>
    public string attributeName;
    /// <summary>比较运算符：">=", "<=", "==", ">", "<"</summary>
    public string comparison;
    /// <summary>比较阈值</summary>
    public int value;
}

/// <summary>好感度条件</summary>
[Serializable]
public class AffinityCondition
{
    /// <summary>NPC ID</summary>
    public string npcId;
    /// <summary>最低好感等级名（如 "Friend", "CloseFriend"）</summary>
    public string minLevel;
    /// <summary>最低好感数值（直接比较）</summary>
    public int minValue;
}

[Serializable]
public class RomanceCondition
{
    /// <summary>NPC ID，留空时可用于“任意恋人”类条件。</summary>
    public string npcId;
    /// <summary>要求的恋爱状态，如 Dating / Crushing / BrokenUp。</summary>
    public string requiredState;
    /// <summary>最低恋爱健康度。</summary>
    public int minHealth;
    /// <summary>是否要求玩家当前拥有任意恋人。</summary>
    public bool requireAnyPartner;
}

[Serializable]
public class ClubCondition
{
    /// <summary>社团 ID。留空且要求入党阶段时可只校验党团进度。</summary>
    public string clubId;
    /// <summary>是否要求已加入社团。</summary>
    public bool requireJoined;
    /// <summary>最低社团职级序号。</summary>
    public int minRank;
    /// <summary>在社团中经历的最少回合数。</summary>
    public int minRoundsInClub;
    /// <summary>最低入党阶段序号。</summary>
    public int minPartyStage;
}

/// <summary>事件触发条件集合</summary>
[Serializable]
public class EventTriggerCondition
{
    // ----- 时间条件 -----
    /// <summary>学年 (0=不限, 1-4)</summary>
    public int year;
    /// <summary>学期 (0=不限, 1=上, 2=下)</summary>
    public int semester;
    /// <summary>回合下限 (0=不限)</summary>
    public int roundMin;
    /// <summary>回合上限 (0=不限)</summary>
    public int roundMax;
    /// <summary>指定回合列表（为空表示不限）</summary>
    public int[] specificRounds;

    // ----- 属性条件 -----
    /// <summary>属性比较条件列表（全部满足才通过）</summary>
    public AttributeCondition[] attributeConditions;

    // ----- 金钱条件 -----
    /// <summary>最低金钱 (0=不限)</summary>
    public int minMoney;
    /// <summary>最高金钱 (0=不限)</summary>
    public int maxMoney;

    // ----- 好感度条件 -----
    /// <summary>好感度条件列表</summary>
    public AffinityCondition[] affinityConditions;
    /// <summary>恋爱条件列表</summary>
    public RomanceCondition[] romanceConditions;
    /// <summary>社团 / 入党条件列表</summary>
    public ClubCondition[] clubConditions;

    // ----- 前置事件条件 -----
    /// <summary>必须已触发的事件ID列表</summary>
    public string[] requiredEventIds;
    /// <summary>必须未触发的事件ID列表</summary>
    public string[] excludedEventIds;
    /// <summary>必须为真的全局标记列表</summary>
    public string[] requiredFlags;
    /// <summary>必须为假的全局标记列表</summary>
    public string[] excludedFlags;

    // ----- 黑暗值条件（DE 专用） -----
    /// <summary>最低黑暗值 (0=不限)</summary>
    public int minDarkness;

    // ----- 行为触发（DE 专用） -----
    /// <summary>触发行为标识，如 "cheat", "loan", "pua"</summary>
    public string triggerBehavior;

    /// <summary>触发概率，0~1，默认 1</summary>
    public float triggerChance = 1f;

    /// <summary>要求玩家处于指定地点；留空表示不限。</summary>
    public string requiredLocationId;

    // ----- 触发阶段 -----
    /// <summary>在哪个阶段检查："RoundStart", "ActionComplete", "RoundEnd"</summary>
    public string phase;
}

// ========== 事件内容 ==========

/// <summary>事件对话段落</summary>
[Serializable]
public class EventDialogue
{
    /// <summary>说话人名字</summary>
    public string speaker;
    /// <summary>对话行列表</summary>
    public string[] lines;
    /// <summary>头像资源ID（可选）</summary>
    public string portraitId;
}

/// <summary>事件效果</summary>
[Serializable]
public class EventEffect
{
    /// <summary>效果类型："attribute", "money", "unlock", "flag", "darkness"</summary>
    public string type;
    /// <summary>作用目标：属性名 / 标记名</summary>
    public string target;
    /// <summary>变化值</summary>
    public int value;
    /// <summary>效果描述（UI 显示用）</summary>
    public string description;
}

/// <summary>事件选项</summary>
[Serializable]
public class EventChoice
{
    /// <summary>选项显示文字</summary>
    public string text;
    /// <summary>选择该选项时额外消耗的行动点</summary>
    public int actionPointCost;
    /// <summary>选择该选项时额外消耗的金钱</summary>
    public int moneyCost;
    /// <summary>选择后应用的效果列表</summary>
    public EventEffect[] effects;
    /// <summary>选择后触发的事件ID（事件链）</summary>
    public string triggerEventId;
    /// <summary>显示此选项所需的属性条件（可选，全部满足才显示）</summary>
    public AttributeCondition[] showConditions;
}

[Serializable]
public class EventPresentationDefinition
{
    /// <summary>演出场景键，可用于钟山台检索和筛选。</summary>
    public string sceneKey;
    /// <summary>演出标题，可显示在事件场景层顶部。</summary>
    public string sceneDisplayName;
    /// <summary>关联地点 ID（可选，主要用于配置定位和后续美术整理）。</summary>
    public string locationId;
    /// <summary>背景图 Resources 路径。</summary>
    public string backgroundResourcePath;
    /// <summary>主角立绘 Resources 路径。</summary>
    public string protagonistPortraitResourcePath;
    /// <summary>NPC 立绘 Resources 路径。</summary>
    public string npcPortraitResourcePath;
    /// <summary>背景资源占位名，资源缺失时显示。</summary>
    public string backgroundSlotName;
    /// <summary>主角立绘占位名，资源缺失时显示。</summary>
    public string protagonistSlotName;
    /// <summary>NPC 立绘占位名，资源缺失时显示。</summary>
    public string npcSlotName;
}

// ========== 事件定义（核心） ==========

/// <summary>
/// 事件定义 —— JSON 配置中的单个事件完整数据
/// ID 前缀规则: ME_001, FE_001, CE_001, DE_001
/// </summary>
[Serializable]
public class EventDefinition
{
    /// <summary>事件ID，如 "ME_001"</summary>
    public string id;
    /// <summary>事件类型字符串："Fixed", "MainStory", "Conditional", "Dark"</summary>
    public string eventType;
    /// <summary>事件标题</summary>
    public string title;
    /// <summary>事件描述</summary>
    public string description;
    /// <summary>优先级数值（越小越高）</summary>
    public int priority;
    /// <summary>是否强制触发（不可跳过）</summary>
    public bool isForced;
    /// <summary>是否可重复触发</summary>
    public bool isRepeatable;
    /// <summary>事件基础行动点消耗（固定事件可作为行动面板事件成本）</summary>
    public int actionPointCost;
    /// <summary>事件基础金钱消耗</summary>
    public int moneyCost;
    /// <summary>每回合最多可触发次数，0 表示不限制</summary>
    public int maxTriggersPerRound;

    /// <summary>触发条件</summary>
    public EventTriggerCondition trigger;

    /// <summary>对话序列</summary>
    public EventDialogue[] dialogues;
    /// <summary>玩家选项（无选项时为空数组）</summary>
    public EventChoice[] choices;

    /// <summary>默认效果（无选项时自动应用）</summary>
    public EventEffect[] defaultEffects;
    /// <summary>后续自动触发的事件ID列表</summary>
    public string[] chainEventIds;
    /// <summary>事件专属场景演出配置。</summary>
    public EventPresentationDefinition presentation;

    // ========== 运行时缓存 ==========

    [NonSerialized] private EventType? _parsedType;
    [NonSerialized] private TriggerPhase? _parsedPhase;

    /// <summary>解析事件类型枚举</summary>
    public EventType GetEventType()
    {
        if (_parsedType == null)
        {
            _parsedType = Enum.TryParse(eventType, true, out EventType t) ? t : EventType.Fixed;
        }
        return _parsedType.Value;
    }

    /// <summary>解析触发阶段枚举</summary>
    public TriggerPhase GetTriggerPhase()
    {
        if (_parsedPhase == null)
        {
            if (trigger != null && !string.IsNullOrEmpty(trigger.phase))
            {
                _parsedPhase = Enum.TryParse(trigger.phase, true, out TriggerPhase p)
                    ? p : TriggerPhase.RoundStart;
            }
            else
            {
                // 默认阶段按事件类型决定
                switch (GetEventType())
                {
                    case EventType.Fixed:
                    case EventType.MainStory:
                        _parsedPhase = TriggerPhase.RoundStart;
                        break;
                    case EventType.Conditional:
                    case EventType.Dark:
                        _parsedPhase = TriggerPhase.RoundEnd;
                        break;
                    default:
                        _parsedPhase = TriggerPhase.RoundStart;
                        break;
                }
            }
        }
        return _parsedPhase.Value;
    }
}

// ========== JSON 根对象 ==========

/// <summary>事件 JSON 文件的根结构</summary>
[Serializable]
public class EventDatabaseRoot
{
    public EventDefinition[] events;
}
