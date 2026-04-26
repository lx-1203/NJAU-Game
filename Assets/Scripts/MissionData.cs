using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务类型
/// </summary>
public enum MissionType
{
    MainStory,      // 主线任务（强制推进剧情）
    SideQuest       // 支线任务（可选）
}

/// <summary>
/// 任务状态
/// </summary>
public enum MissionStatus
{
    Locked,         // 未解锁
    Available,      // 可接取
    Active,         // 进行中
    Completed,      // 已完成
    Failed          // 已失败
}

/// <summary>
/// 任务目标类型
/// </summary>
public enum MissionObjectiveType
{
    ReachRound,             // 到达指定回合
    ReachSemester,          // 到达指定学期
    AttributeThreshold,     // 属性达到阈值
    MoneyThreshold,         // 金钱达到阈值
    NPCAffinityThreshold,   // NPC好感度达到阈值
    JoinClub,               // 加入社团
    ActionCount,            // 执行指定行动次数
    PassExam,               // 通过考试
    CompleteEvent,          // 完成特定事件
    Custom                  // 自定义条件（通过代码检查）
}

/// <summary>
/// 任务奖励类型
/// </summary>
public enum MissionRewardType
{
    Money,          // 金钱
    Attribute,      // 属性
    Unlock,         // 解锁内容（事件/NPC/地点等）
    Item            // 物品（预留）
}

/// <summary>
/// 任务触发条件
/// </summary>
[Serializable]
public class MissionTriggerCondition
{
    public string conditionType;    // Round/Semester/Attribute/Money/Event/NPCAffinity
    public string targetId;         // 目标ID（NPC ID/事件ID等）
    public int minValue;
    public int maxValue;
    public string comparisonOperator; // >=, <=, ==, !=
}

/// <summary>
/// 任务目标
/// </summary>
[Serializable]
public class MissionObjective
{
    public string objectiveId;
    public MissionObjectiveType type;
    public string description;
    public string targetId;         // 目标ID（NPC ID/行动ID/社团ID等）
    public int targetValue;         // 目标值
    public int currentValue;        // 当前进度
    public bool isCompleted;
}

/// <summary>
/// 任务奖励
/// </summary>
[Serializable]
public class MissionReward
{
    public MissionRewardType type;
    public string targetId;         // 属性名/解锁ID
    public int value;
    public string description;
}

/// <summary>
/// 任务定义
/// </summary>
[Serializable]
public class MissionDefinition
{
    public string missionId;
    public string missionName;
    public string description;
    public MissionType type;
    public int priority;            // 优先级（数值越小越优先）

    public List<MissionTriggerCondition> triggerConditions;
    public List<string> prerequisiteMissions;   // 前置任务ID
    public List<MissionObjective> objectives;
    public List<MissionReward> rewards;

    public int timeLimit;           // 时间限制（回合数，0表示无限制）
    public bool autoAccept;         // 是否自动接取
    public bool canAbandon;         // 是否可放弃
}

/// <summary>
/// 任务运行时数据
/// </summary>
[Serializable]
public class MissionRuntimeData
{
    public string missionId;
    public MissionStatus status;
    public int acceptedRound;       // 接取时的回合数
    public List<MissionObjective> objectives;

    public MissionRuntimeData(MissionDefinition definition)
    {
        missionId = definition.missionId;
        status = MissionStatus.Available;
        acceptedRound = 0;
        objectives = new List<MissionObjective>();

        // 深拷贝目标
        foreach (var obj in definition.objectives)
        {
            objectives.Add(new MissionObjective
            {
                objectiveId = obj.objectiveId,
                type = obj.type,
                description = obj.description,
                targetId = obj.targetId,
                targetValue = obj.targetValue,
                currentValue = 0,
                isCompleted = false
            });
        }
    }
}

/// <summary>
/// 任务存档数据
/// </summary>
[Serializable]
public class MissionSaveData
{
    public List<MissionRuntimeData> activeMissions = new List<MissionRuntimeData>();
    public List<string> completedMissionIds = new List<string>();
    public List<string> failedMissionIds = new List<string>();

    public void EnsureInitialized()
    {
        activeMissions ??= new List<MissionRuntimeData>();
        completedMissionIds ??= new List<string>();
        failedMissionIds ??= new List<string>();
    }
}
