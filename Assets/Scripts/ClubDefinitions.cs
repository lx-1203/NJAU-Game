using UnityEngine;
using System;
using System.Collections.Generic;

// ====================================================================
//  社团系统 —— 数据模型定义
//  包含社团定义、晋升体系、入党阶段、运行时状态、小游戏接口
// ====================================================================

// ========== 加入条件 ==========

/// <summary>社团加入条件定义（支持 AND/OR 逻辑）</summary>
[System.Serializable]
public class JoinRequirement
{
    public string attributeName;         // 属性名（中文）
    public int minValue;                 // 最低要求值
    public string logic;                 // "AND"(默认) 或 "OR"

    public JoinRequirement() { logic = "AND"; }
}

// ========== 社团定义 ==========

/// <summary>社团定义数据（JSON 配置驱动）</summary>
[System.Serializable]
public class ClubDefinition
{
    public string id;                    // 英文唯一标识，如 "running_club"
    public string name;                  // 中文显示名，如 "跑协"
    public string category;              // 分类: 体育/文艺/学术/志愿/官方组织/兴趣
    public string primaryAttribute;      // 主要提升属性名
    public string npcId;                 // 专属 NPC ID
    public string[] eventChainIds;       // 5 阶段事件链 ID 列表
    public string minigameType;          // 小游戏类型标识
    public bool occupiesSlot;            // 是否占社团名额
    public bool isOfficial;              // 是否为官方组织（不可主动退出）
    public int activityAPCost;           // 参加社团活动消耗的行动点
    public int activityMoneyCost;        // 参加社团活动消耗的金钱
    public AttributeEffect[] activityEffects; // 社团活动的属性变化
    public string promotionPath;         // 晋升路径名: "standard" / "student_union" / "youth_league"
    public JoinRequirement[] joinRequirements; // 加入条件（可为null/空=无条件）
}

// ========== 晋升阶梯 ==========

/// <summary>晋升职务等级定义</summary>
[System.Serializable]
public class PromotionRank
{
    public int rank;                     // 等级序号 (0=干事, 1=部长, ...)
    public string title;                 // 中文职务名
    public int apCost;                   // 该职务每回合自动扣减行动点
    public int requiredRounds;           // 晋升需要的社团内回合数
    public AttributeRequirement[] requiredAttributes; // 晋升需要的属性条件
}

/// <summary>属性要求条件</summary>
[System.Serializable]
public class AttributeRequirement
{
    public string attributeName;         // 属性名（中文）
    public int minValue;                 // 最低要求值

    public AttributeRequirement() { }

    public AttributeRequirement(string attributeName, int minValue)
    {
        this.attributeName = attributeName;
        this.minValue = minValue;
    }
}

// ========== 晋升路径 ==========

/// <summary>一条完整的晋升路径（包含多个等级）</summary>
[System.Serializable]
public class PromotionPath
{
    public string pathName;              // 路径名: "standard" / "student_union" / "youth_league"
    public PromotionRank[] ranks;        // 该路径下的所有等级
}

// ========== 入党阶段 ==========

/// <summary>入党阶段定义</summary>
[System.Serializable]
public class PartyMembershipStage
{
    public int stage;                    // 阶段序号 (0=未申请, 1=已提交, ...)
    public string name;                  // 阶段中文名
    public int requiredRound;            // 最早可达该阶段的总回合数（从提交申请起算）
}

// ========== 入党条件 ==========

/// <summary>入党的全局前置条件</summary>
[System.Serializable]
public class PartyRequirements
{
    public int minLeadership;            // 最低领导力
    public int minStudy;                 // 最低学力（替代 GPA）
    public bool noDisciplinaryRecord;    // 无违纪记录
}

// ========== JSON 根容器 ==========

/// <summary>ClubData.json 的根对象，用于 JsonUtility 反序列化</summary>
[System.Serializable]
public class ClubDataWrapper
{
    public ClubDefinition[] clubs;
    public PromotionPath[] promotionPaths;
    public PartyMembershipStage[] partyMembershipStages;
    public PartyRequirements partyRequirements;
}

// ========== 运行时社团成员状态 ==========

/// <summary>玩家在某个社团中的运行时状态</summary>
[System.Serializable]
public class ClubMembership
{
    public string clubId;                // 社团 ID
    public int currentRank;              // 当前职务等级 (从 0 开始)
    public int joinedAtRound;            // 加入时的全局回合数
    public int roundsInClub;             // 在社团内经历的回合数
    public int eventChainProgress;       // 事件链进度 (0-4, -1=未开始)

    public ClubMembership(string clubId, int joinedAtRound)
    {
        this.clubId = clubId;
        this.currentRank = 0;
        this.joinedAtRound = joinedAtRound;
        this.roundsInClub = 0;
        this.eventChainProgress = -1;
    }
}

// ========== 小游戏接口 ==========

/// <summary>
/// 社团小游戏接口 —— 本阶段仅定义，具体实现后续迭代
/// </summary>
public interface IClubMinigame
{
    /// <summary>小游戏类型标识</summary>
    string MinigameType { get; }

    /// <summary>
    /// 启动小游戏，完成后通过回调返回结果
    /// </summary>
    /// <param name="onComplete">true=成功, false=失败</param>
    void StartMinigame(System.Action<bool> onComplete);
}
