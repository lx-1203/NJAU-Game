using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 告白系统 —— 单例 MonoBehaviour
/// 计算告白成功率、执行告白流程、处理复合逻辑
/// 公式: 50% + Charm*0.2% - Stress*0.1% + (affinity-80)*1% + locationBonus + npcBonus
/// 成功率 clamp [20%, 95%]
/// 复合成功率 = 正常成功率 * 0.7
/// </summary>
public class ConfessionSystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static ConfessionSystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>告白结果事件（npcId, 是否成功, 成功率）</summary>
    public event Action<string, bool, float> OnConfessionResult;

    // ========== 地点加成字典 ==========

    private static readonly Dictionary<string, float> LocationBonusMap = new Dictionary<string, float>
    {
        { "Library",       0.00f },
        { "Dormitory",     0.02f },
        { "Cafeteria",     0.03f },
        { "Playground",    0.05f },
        { "Garden",        0.08f },
        { "Rooftop",       0.10f },
        { "Lake",          0.10f },
        { "CampusGate",    0.03f }
    };

    // ========== NPC 个性加成 ==========

    private static readonly Dictionary<string, float> PersonalityBonusMap = new Dictionary<string, float>
    {
        { "Extrovert",  0.05f },
        { "Cheerful",   0.05f },
        { "Easygoing",  0.03f },
        { "Introvert", -0.03f },
        { "Serious",   -0.05f },
        { "Mysterious", -0.02f }
    };

    // ========== 常量 ==========
    private const float BASE_RATE = 0.50f;
    private const float CHARM_COEFF = 0.002f;       // 每点魅力 +0.2%
    private const float STRESS_COEFF = 0.001f;       // 每点压力 -0.1%
    private const float AFFINITY_COEFF = 0.01f;      // (affinity-80) 每点 +1%
    private const float MIN_RATE = 0.20f;
    private const float MAX_RATE = 0.95f;
    private const float REUNION_MULTIPLIER = 0.70f;  // 复合成功率乘数
    private const int CONFESSION_AP_COST = 2;        // 告白消耗行动点

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

    // ========== 公共方法：成功率计算 ==========

    /// <summary>
    /// 计算告白成功率
    /// </summary>
    /// <param name="npcId">目标 NPC ID</param>
    /// <param name="locationId">当前地点（可选，为空时无地点加成）</param>
    /// <returns>成功概率 [0.20, 0.95]</returns>
    public float CalculateSuccessRate(string npcId, string locationId = null)
    {
        // 基础值
        float rate = BASE_RATE;

        // 魅力加成
        if (PlayerAttributes.Instance != null)
        {
            rate += PlayerAttributes.Instance.Charm * CHARM_COEFF;
        }

        // 压力减成
        if (PlayerAttributes.Instance != null)
        {
            rate -= PlayerAttributes.Instance.Stress * STRESS_COEFF;
        }

        // 好感度加成 (affinity - 80) * 1%
        int affinity = GetAffinity(npcId);
        rate += (affinity - 80) * AFFINITY_COEFF;

        // 地点加成
        if (!string.IsNullOrEmpty(locationId) && LocationBonusMap.TryGetValue(locationId, out float locBonus))
        {
            rate += locBonus;
        }

        // NPC 个性加成
        if (NPCDatabase.Instance != null)
        {
            NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
            if (npcData != null && !string.IsNullOrEmpty(npcData.personality))
            {
                if (PersonalityBonusMap.TryGetValue(npcData.personality, out float persBonus))
                {
                    rate += persBonus;
                }
            }
        }

        // Clamp
        rate = Mathf.Clamp(rate, MIN_RATE, MAX_RATE);

        return rate;
    }

    /// <summary>
    /// 计算复合告白成功率（正常成功率 * 0.7）
    /// </summary>
    public float CalculateReunionRate(string npcId, string locationId = null)
    {
        float normalRate = CalculateSuccessRate(npcId, locationId);
        float reunionRate = normalRate * REUNION_MULTIPLIER;
        return Mathf.Clamp(reunionRate, MIN_RATE, MAX_RATE);
    }

    // ========== 公共方法：执行告白 ==========

    /// <summary>
    /// 执行告白流程
    /// 1. 检查前置条件（CanConfess / CanReunite）
    /// 2. 扣除行动点
    /// 3. 掷骰判定
    /// 4. 调用 RomanceSystem 处理结果
    /// 5. 触发对话
    /// </summary>
    /// <param name="npcId">目标 NPC ID</param>
    /// <param name="isReunion">是否为复合告白</param>
    /// <param name="onComplete">回调（成功/失败）</param>
    public void ExecuteConfession(string npcId, bool isReunion = false, Action<bool> onComplete = null)
    {
        if (RomanceSystem.Instance == null)
        {
            Debug.LogError("[ConfessionSystem] RomanceSystem 未初始化");
            onComplete?.Invoke(false);
            return;
        }

        // 前置条件检查
        bool canProceed = isReunion
            ? RomanceSystem.Instance.CanReunite(npcId)
            : RomanceSystem.Instance.CanConfess(npcId);

        if (!canProceed)
        {
            Debug.LogWarning($"[ConfessionSystem] {npcId} 不满足{(isReunion ? "复合" : "告白")}条件");
            onComplete?.Invoke(false);
            return;
        }

        // 扣除行动点
        if (GameState.Instance != null)
        {
            if (!GameState.Instance.ConsumeActionPoint(CONFESSION_AP_COST))
            {
                Debug.LogWarning("[ConfessionSystem] 行动点不足");
                onComplete?.Invoke(false);
                return;
            }
        }

        // 获取当前地点
        string locationId = null;
        if (GameState.Instance != null)
        {
            locationId = GameState.Instance.CurrentLocation.ToString();
        }

        // 计算成功率
        float successRate = isReunion
            ? CalculateReunionRate(npcId, locationId)
            : CalculateSuccessRate(npcId, locationId);

        // 掷骰
        float roll = UnityEngine.Random.value;
        bool success = roll <= successRate;

        Debug.Log($"[ConfessionSystem] {npcId} {(isReunion ? "复合" : "告白")} — 成功率:{successRate:P1}, 掷骰:{roll:F3}, 结果:{(success ? "成功" : "失败")}");

        // 处理结果
        if (success)
        {
            // Bug4-劈腿标记: 告白成功时检测是否已有其他 Dating 对象
            List<string> existingPartners = RomanceSystem.Instance.GetAllDatingNPCs();

            RomanceSystem.Instance.OnConfessionSuccess(npcId);

            // 如果之前已有恋人，标记所有 Dating 关系为劈腿状态
            if (existingPartners.Count > 0)
            {
                Debug.Log($"[ConfessionSystem] 劈腿检测: 已有{existingPartners.Count}个恋人，标记劈腿状态");
                // 标记新恋人
                RomanceSystem.Instance.MarkCheating(npcId);
                // 标记旧恋人
                foreach (string partnerId in existingPartners)
                {
                    RomanceSystem.Instance.MarkCheating(partnerId);
                }
            }

            // 同步 NPCRelationshipData 的 romanceState
            SyncRelationshipRomanceState(npcId, RomanceState.Dating);
        }
        else
        {
            RomanceSystem.Instance.OnConfessionFail(npcId);

            // 同步 NPCRelationshipData 的 romanceState
            SyncRelationshipRomanceState(npcId, RomanceState.Cooldown);
        }

        // 触发事件
        OnConfessionResult?.Invoke(npcId, success, successRate);

        // 触发对话（如果 DialogueSystem 可用）
        TriggerConfessionDialogue(npcId, success, isReunion);

        // 回调
        onComplete?.Invoke(success);
    }

    // ========== 内部方法 ==========

    /// <summary>
    /// 获取 NPC 好感度
    /// </summary>
    private int GetAffinity(string npcId)
    {
        if (AffinitySystem.Instance != null)
        {
            NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(npcId);
            return rel.affinity;
        }
        return 0;
    }

    /// <summary>
    /// 同步 NPCRelationshipData 中的 romanceState 字段
    /// </summary>
    private void SyncRelationshipRomanceState(string npcId, RomanceState state)
    {
        if (AffinitySystem.Instance != null)
        {
            NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(npcId);
            rel.romanceState = state;
        }
    }

    /// <summary>
    /// 触发告白结果对话
    /// </summary>
    private void TriggerConfessionDialogue(string npcId, bool success, bool isReunion)
    {
        if (DialogueSystem.Instance == null) return;

        // 尝试加载对应的 JSON 对话
        string dialogueId = isReunion
            ? (success ? $"{npcId}_reunion_success" : $"{npcId}_reunion_fail")
            : (success ? $"{npcId}_confession_success" : $"{npcId}_confession_fail");

        // 先检查是否存在 JSON 对话数据
        DialogueData data = DialogueParser.GetDialogue(dialogueId);
        if (data != null)
        {
            DialogueSystem.Instance.StartDialogue(dialogueId);
        }
        else
        {
            // 回退到简单文本对话
            string speakerName = GetNPCDisplayName(npcId);
            string[] lines;

            if (success)
            {
                lines = isReunion
                    ? new string[] { "......我愿意再给我们一次机会。" }
                    : new string[] { "......我也喜欢你。" };
            }
            else
            {
                lines = isReunion
                    ? new string[] { "......对不起，我觉得我们还是做朋友比较好。" }
                    : new string[] { "......谢谢你的心意，但是......" };
            }

            DialogueSystem.Instance.StartDialogue(speakerName, lines);
        }
    }

    /// <summary>
    /// 获取 NPC 显示名
    /// </summary>
    private string GetNPCDisplayName(string npcId)
    {
        if (NPCDatabase.Instance != null)
        {
            NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
            if (npcData != null) return npcData.displayName;
        }
        return npcId;
    }
}
