using System;
using System.Collections.Generic;

// ========================================================================
//  多周目传承数据 —— 独立于普通存档，存储在单独文件
// ========================================================================

[Serializable]
public class NewGamePlusData
{
    // ========== 周目信息 ==========

    /// <summary>当前周目数（1=首周目，2=二周目...）</summary>
    public int cycleCount = 1;

    /// <summary>总天赋点（预留）</summary>
    public int totalTalentPoints = 0;

    /// <summary>已解锁天赋ID列表（预留）</summary>
    public List<string> unlockedTalents = new List<string>();

    // ========== 上周目终值 ==========

    public int lastStudy;
    public int lastCharm;
    public int lastPhysique;
    public int lastLeadership;
    public int lastStress;
    public int lastMood;
    public int lastMoney;
    public string lastEnding = "";
    public List<NPCRelationshipSaveData> lastRelationships = new List<NPCRelationshipSaveData>();

    // ========== 传承公式 ==========

    /// <summary>
    /// 获取属性传承比例
    /// 首→二10%, 二→三15%, 三→四20%, 四→五+25%
    /// </summary>
    public static float GetInheritRate(int nextCycle)
    {
        switch (nextCycle)
        {
            case 2: return 0.10f;
            case 3: return 0.15f;
            case 4: return 0.20f;
            default: return 0.25f; // 五周目及以后
        }
    }

    /// <summary>
    /// 获取周目奖励金钱
    /// 二+500, 三+1000, 四+1500, 五及以后+1500
    /// </summary>
    public static int GetBonusMoney(int nextCycle)
    {
        switch (nextCycle)
        {
            case 2: return 500;
            case 3: return 1000;
            case 4: return 1500;
            default: return 1500;
        }
    }

    /// <summary>
    /// 计算传承后的属性初始值
    /// 初始 = 基础值 + min(上周目终值 × 传承比例, 上限值)
    /// </summary>
    public static int CalcInheritedAttribute(int baseValue, int lastValue, float inheritRate, int cap = 30)
    {
        int bonus = (int)(lastValue * inheritRate);
        bonus = Math.Min(bonus, cap);
        return baseValue + bonus;
    }

    /// <summary>
    /// 计算传承后的初始金钱
    /// 8000 + max(上周目剩余×5%, 0) + 周目奖励, 上限15000
    /// </summary>
    public static int CalcInheritedMoney(int lastMoney, int nextCycle)
    {
        int baseMoney = 8000;
        int fromLast = Math.Max((int)(lastMoney * 0.05f), 0);
        int bonus = GetBonusMoney(nextCycle);
        int total = baseMoney + fromLast + bonus;
        return Math.Min(total, 15000);
    }

    /// <summary>
    /// 计算传承后的NPC好感度
    /// 已认识NPC好感×20%，已攻略恋人×30%(上限60)
    /// </summary>
    public static int CalcInheritedAffinity(int lastAffinity, bool wasLover)
    {
        float rate = wasLover ? 0.30f : 0.20f;
        int result = (int)(lastAffinity * rate);
        return Math.Min(result, 60);
    }

    // ========== 周目解锁查询 ==========

    /// <summary>检查指定功能是否在当前周目解锁</summary>
    public bool IsFeatureUnlocked(string featureKey)
    {
        switch (featureKey)
        {
            case "same_sex_route":   return cycleCount >= 2;
            case "hidden_npc":       return cycleCount >= 2;
            case "npc_full_memory":  return cycleCount >= 3;
            case "true_ending":      return cycleCount >= 4;
            default: return false;
        }
    }
}
