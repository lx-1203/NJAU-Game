using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 好感度系统 —— 管理所有 NPC 关系数据、好感度公式、等级换算与自然衰减
/// 单例模式，在 GameSceneInitializer 中初始化
/// </summary>
public class AffinitySystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static AffinitySystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>好感度变化时触发（npcId, 旧好感值, 新好感值, 变化量）</summary>
    public event Action<string, int, int, int> OnAffinityChanged;

    /// <summary>好感度等级变化时触发（npcId, 旧等级, 新等级）</summary>
    public event Action<string, AffinityLevel, AffinityLevel> OnAffinityLevelChanged;

    /// <summary>社交互动完成时触发（npcId, socialActionId, affinityDelta）</summary>
    public event Action<string, string, int> OnInteractionCompleted;

    // ========== 扩展接口 ==========
    private IRelationshipExtension relationshipExtension;

    public void RegisterExtension(IRelationshipExtension ext)
    {
        relationshipExtension = ext;
    }

    // ========== 等级常量 ==========

    /// <summary>各等级的好感度阈值（下限）</summary>
    private static readonly int[] LevelThresholds = { 0, 20, 40, 60, 80, 80 };

    /// <summary>各等级自然衰减值（每回合）</summary>
    private static readonly int[] LevelDecay = { 0, 0, -1, -2, -3, -4 };

    // ========== 内部字段 ==========
    private Dictionary<string, NPCRelationshipData> relationships = new Dictionary<string, NPCRelationshipData>();

    // ========== 公共方法 ==========

    /// <summary>获取指定 NPC 的关系数据，若不存在则自动创建</summary>
    public NPCRelationshipData GetRelationship(string npcId)
    {
        if (!relationships.TryGetValue(npcId, out NPCRelationshipData data))
        {
            data = new NPCRelationshipData(npcId);
            relationships[npcId] = data;
        }
        return data;
    }

    /// <summary>获取所有已有关系数据</summary>
    public Dictionary<string, NPCRelationshipData> GetAllRelationships()
    {
        return relationships;
    }

    /// <summary>统计好感度等级 >= Friend 的NPC数量</summary>
    public int GetFriendOrAboveCount()
    {
        int count = 0;
        foreach (var kvp in relationships)
        {
            if ((int)kvp.Value.level >= (int)AffinityLevel.Friend)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 获取指定 NPC 当前可执行的社交行动列表
    /// </summary>
    public List<SocialActionDefinition> GetAvailableSocialActions(string npcId)
    {
        List<SocialActionDefinition> result = new List<SocialActionDefinition>();
        if (NPCDatabase.Instance == null) return result;

        NPCRelationshipData rel = GetRelationship(npcId);
        SocialActionDefinition[] allActions = NPCDatabase.Instance.GetAllSocialActions();
        GameState gs = GameState.Instance;

        for (int i = 0; i < allActions.Length; i++)
        {
            SocialActionDefinition action = allActions[i];

            // 检查好感等级解锁
            if (GetLevelOrder(rel.level) < GetLevelOrder(action.GetMinAffinityLevel()))
                continue;

            // 检查行动点
            if (gs != null && gs.ActionPoints < action.actionPointCost)
                continue;

            // 检查金钱
            if (gs != null && gs.Money < action.moneyCost)
                continue;

            result.Add(action);
        }

        return result;
    }

    /// <summary>
    /// 执行社交互动：计算好感度变化并应用效果
    /// 返回实际好感度增量
    /// </summary>
    public int ExecuteInteraction(string npcId, string socialActionId)
    {
        if (NPCDatabase.Instance == null) return 0;

        SocialActionDefinition action = NPCDatabase.Instance.GetSocialAction(socialActionId);
        if (action == null)
        {
            Debug.LogWarning($"[AffinitySystem] 未找到社交行动: {socialActionId}");
            return 0;
        }

        NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
        if (npcData == null)
        {
            Debug.LogWarning($"[AffinitySystem] 未找到NPC: {npcId}");
            return 0;
        }

        NPCRelationshipData rel = GetRelationship(npcId);
        GameState gs = GameState.Instance;

        // 1. 扣除行动点
        if (gs != null)
        {
            if (!gs.ConsumeActionPoint(action.actionPointCost))
            {
                Debug.Log($"[AffinitySystem] 行动点不足");
                return 0;
            }

            // 2. 扣除金钱
            if (action.moneyCost > 0)
            {
                if (gs.Money < action.moneyCost)
                {
                    Debug.Log($"[AffinitySystem] 金钱不足");
                    // 回退行动点（简易处理）
                    gs.ActionPoints += action.actionPointCost;
                    return 0;
                }
                gs.AddMoney(-action.moneyCost);
            }
        }

        // 3. 应用属性效果
        if (action.attributeEffects != null && PlayerAttributes.Instance != null)
        {
            for (int i = 0; i < action.attributeEffects.Length; i++)
            {
                AttributeEffect eff = action.attributeEffects[i];
                PlayerAttributes.Instance.AddAttribute(eff.attributeName, eff.amount);
            }
        }

        // 4. 计算好感度变化
        int baseDelta = UnityEngine.Random.Range(action.baseAffinityMin, action.baseAffinityMax + 1);
        float actualDelta = CalculateAffinityDelta(baseDelta, npcData, rel, socialActionId);
        int intDelta = Mathf.RoundToInt(actualDelta);

        // 5. 应用好感度变化
        int oldAffinity = rel.affinity;
        AffinityLevel oldLevel = rel.level;

        rel.affinity = Mathf.Clamp(rel.affinity + intDelta, 0, 100);

        // 6. 更新重复行动计数
        if (rel.lastInteractionActionId == socialActionId)
        {
            rel.repeatedActionCount++;
        }
        else
        {
            rel.repeatedActionCount = 1;
            rel.lastInteractionActionId = socialActionId;
        }

        // 7. 重置无互动计数
        rel.consecutiveNoInteractionTurns = 0;

        // 8. 添加记忆
        string memory = $"回合{(gs != null ? gs.CurrentRound.ToString() : "?")}:{action.displayName}(+{intDelta})";
        rel.memories.Add(memory);
        if (rel.memories.Count > 20) rel.memories.RemoveAt(0); // 保留最近20条

        // 9. 等级换算
        AffinityLevel newLevel = CalculateLevel(rel.affinity, rel);
        rel.level = newLevel;

        // 10. 触发事件
        OnAffinityChanged?.Invoke(npcId, oldAffinity, rel.affinity, intDelta);

        if (newLevel != oldLevel)
        {
            OnAffinityLevelChanged?.Invoke(npcId, oldLevel, newLevel);
            relationshipExtension?.OnAffinityLevelChanged(npcId, oldLevel, newLevel);
        }

        OnInteractionCompleted?.Invoke(npcId, socialActionId, intDelta);
        relationshipExtension?.OnInteractionCompleted(npcId, socialActionId, intDelta);

        Debug.Log($"[AffinitySystem] {npcData.displayName}: {action.displayName} → 好感度 {oldAffinity}→{rel.affinity} ({(intDelta >= 0 ? "+" : "")}{intDelta}), 等级: {rel.level}");

        return intDelta;
    }

    /// <summary>
    /// 回合推进时调用：所有 NPC 关系自然衰减
    /// </summary>
    public void ProcessTurnDecay()
    {
        foreach (var kvp in relationships)
        {
            NPCRelationshipData rel = kvp.Value;
            int levelIndex = (int)rel.level;
            int decay = LevelDecay[Mathf.Clamp(levelIndex, 0, LevelDecay.Length - 1)];

            // 连续无互动加重衰减
            rel.consecutiveNoInteractionTurns++;
            if (rel.consecutiveNoInteractionTurns > 3 && decay < 0)
            {
                decay -= 1; // 额外衰减
            }

            if (decay != 0)
            {
                int oldAffinity = rel.affinity;
                rel.affinity = Mathf.Clamp(rel.affinity + decay, 0, 100);

                AffinityLevel oldLevel = rel.level;
                rel.level = CalculateLevel(rel.affinity, rel);

                if (rel.affinity != oldAffinity)
                {
                    OnAffinityChanged?.Invoke(rel.npcId, oldAffinity, rel.affinity, decay);
                }

                if (rel.level != oldLevel)
                {
                    OnAffinityLevelChanged?.Invoke(rel.npcId, oldLevel, rel.level);
                    relationshipExtension?.OnAffinityLevelChanged(rel.npcId, oldLevel, rel.level);
                }
            }
        }
    }

    // ========== 好感度公式 ==========

    /// <summary>
    /// 实际增量 = base × charm_coeff × personality_match × repeat_decay
    /// </summary>
    private float CalculateAffinityDelta(int baseDelta, NPCData npcData, NPCRelationshipData rel, string actionId)
    {
        // charm_coeff = 1 + 魅力/100
        float charmCoeff = 1f;
        if (PlayerAttributes.Instance != null)
        {
            charmCoeff = 1f + PlayerAttributes.Instance.Charm / 100f;
        }

        // personality_match
        float personalityMatch = 1.0f;
        if (npcData.LikesAction(actionId))
        {
            personalityMatch = 1.5f;
        }
        else if (npcData.DislikesAction(actionId))
        {
            personalityMatch = 0.5f;
        }

        // repeat_decay：连续同一行动衰减
        float repeatDecay = 1.0f;
        if (rel.lastInteractionActionId == actionId && rel.repeatedActionCount > 0)
        {
            // 每连续重复一次衰减 15%，最低 30%
            repeatDecay = Mathf.Max(0.3f, 1f - 0.15f * rel.repeatedActionCount);
        }

        float result = baseDelta * charmCoeff * personalityMatch * repeatDecay;
        return result;
    }

    // ========== 等级换算 ==========

    /// <summary>根据好感度值计算关系等级</summary>
    private AffinityLevel CalculateLevel(int affinity, NPCRelationshipData rel)
    {
        // Lover 需要特殊解锁
        if (rel.romanceState == RomanceState.Dating && affinity >= 80)
        {
            if (relationshipExtension == null || relationshipExtension.CanEnterLoverLevel(rel.npcId))
                return AffinityLevel.Lover;
        }

        if (affinity >= 80) return AffinityLevel.BestFriend;
        if (affinity >= 60) return AffinityLevel.CloseFriend;
        if (affinity >= 40) return AffinityLevel.Friend;
        if (affinity >= 20) return AffinityLevel.Acquaintance;
        return AffinityLevel.Stranger;
    }

    /// <summary>等级排序值（用于比较解锁条件）</summary>
    private int GetLevelOrder(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Stranger:     return 0;
            case AffinityLevel.Acquaintance: return 1;
            case AffinityLevel.Friend:       return 2;
            case AffinityLevel.CloseFriend:  return 3;
            case AffinityLevel.BestFriend:   return 4;
            case AffinityLevel.Lover:        return 5;
            default: return 0;
        }
    }

    // ========== 当前时间段 ==========

    /// <summary>根据当前行动点推算日内时间段</summary>
    public static TimeSlot GetCurrentTimeSlot()
    {
        if (GameState.Instance == null) return TimeSlot.Morning;

        int ap = GameState.Instance.ActionPoints;
        if (ap >= 4) return TimeSlot.Morning;
        if (ap >= 2) return TimeSlot.Afternoon;
        return TimeSlot.Evening;
    }

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
    }

    private void Start()
    {
        // 订阅回合推进事件
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;
        }

        // 为所有 NPC 初始化关系数据
        if (NPCDatabase.Instance != null)
        {
            NPCData[] allNPCs = NPCDatabase.Instance.GetAllNPCs();
            for (int i = 0; i < allNPCs.Length; i++)
            {
                GetRelationship(allNPCs[i].id); // 确保每个NPC都有关系数据
            }
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
        }
    }

    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        ProcessTurnDecay();
    }
}
