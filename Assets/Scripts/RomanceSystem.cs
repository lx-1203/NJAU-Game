using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 恋爱系统 —— 核心单例，管理所有NPC的恋爱状态、健康度、回合结算、分手/告白/结局判定
/// </summary>
public class RomanceSystem : MonoBehaviour, IRomanceProvider
{
    // ========== 单例 ==========
    public static RomanceSystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>恋爱状态变化时触发（npcId, 旧状态, 新状态）</summary>
    public event Action<string, RomanceState, RomanceState> OnRomanceStateChanged;

    /// <summary>恋爱健康度变化时触发（npcId）</summary>
    public event Action<string> OnRomanceHealthChanged;

    // ========== 内部数据 ==========

    /// <summary>所有NPC的恋爱记录</summary>
    private Dictionary<string, RomanceRecord> records = new Dictionary<string, RomanceRecord>();

    /// <summary>好感度缓存（由外部 AffinitySystem 或测试代码同步）</summary>
    private Dictionary<string, int> affinityCache = new Dictionary<string, int>();

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

    /// <summary>
    /// 订阅 TurnManager 事件，确保 TurnManager 已完成初始化
    /// </summary>
    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;
            TurnManager.Instance.OnGameCompleted += OnGameCompleted;
            Debug.Log("[RomanceSystem] 初始化完成，已订阅回合推进和游戏结束事件");
        }
        else
        {
            Debug.LogWarning("[RomanceSystem] TurnManager 实例不存在，无法订阅事件");
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
            TurnManager.Instance.OnGameCompleted -= OnGameCompleted;
        }
    }

    // ========== 公共方法：记录访问 ==========

    /// <summary>
    /// 获取或创建指定NPC的恋爱记录
    /// </summary>
    public RomanceRecord GetOrCreateRecord(string npcId)
    {
        if (!records.ContainsKey(npcId))
        {
            var record = new RomanceRecord { npcId = npcId };
            records[npcId] = record;
        }
        return records[npcId];
    }

    /// <summary>
    /// 获取指定NPC的恋爱状态
    /// </summary>
    public RomanceState GetRomanceState(string npcId)
    {
        return records.ContainsKey(npcId) ? records[npcId].state : RomanceState.None;
    }

    /// <summary>
    /// 获取指定NPC的恋爱健康度
    /// </summary>
    public int GetRomanceHealth(string npcId)
    {
        return records.ContainsKey(npcId) ? records[npcId].healthScore : 70;
    }

    // ========== 公共方法：好感度缓存 ==========

    /// <summary>
    /// 更新好感度缓存（由外部 AffinitySystem 调用以同步数据）
    /// </summary>
    public void UpdateAffinityCache(string npcId, int affinity)
    {
        affinityCache[npcId] = affinity;
    }

    /// <summary>
    /// 获取缓存的好感度值
    /// </summary>
    private int GetAffinity(string npcId)
    {
        return affinityCache.ContainsKey(npcId) ? affinityCache[npcId] : 0;
    }

    // ========== 公共方法：条件判断 ==========

    /// <summary>
    /// 检查是否可以向指定NPC告白
    /// 条件：Crushing态 + 好感≥80 + 非冷却中 + 行动点≥2
    /// </summary>
    public bool CanConfess(string npcId)
    {
        var record = GetOrCreateRecord(npcId);
        int affinity = GetAffinity(npcId);
        int ap = GameState.Instance != null ? GameState.Instance.ActionPoints : 0;

        return record.state == RomanceState.Crushing
            && affinity >= 80
            && record.cooldownRoundsLeft <= 0
            && ap >= 2;
    }

    /// <summary>
    /// 检查是否可以与指定NPC约会
    /// 条件：Dating态
    /// </summary>
    public bool CanDate(string npcId)
    {
        var record = GetOrCreateRecord(npcId);
        return record.state == RomanceState.Dating;
    }

    /// <summary>
    /// 检查是否可以与指定NPC复合
    /// 条件：BrokenUp态 + 冷却结束 + 好感≥70 + 分手次数＜2 + 未复合过
    /// </summary>
    public bool CanReunite(string npcId)
    {
        var record = GetOrCreateRecord(npcId);
        int affinity = GetAffinity(npcId);

        return record.state == RomanceState.BrokenUp
            && record.cooldownRoundsLeft <= 0
            && affinity >= 70
            && record.breakupCount < 2
            && !record.hasReunited;
    }

    // ========== 公共方法：状态转换 ==========

    /// <summary>
    /// 检查并更新暗恋状态
    /// 好感≥60 且当前 None → Crushing；好感＜60 且当前 Crushing → None
    /// </summary>
    public void CheckAndUpdateCrushingState(string npcId, int newAffinity)
    {
        // 同步好感度缓存
        UpdateAffinityCache(npcId, newAffinity);

        var record = GetOrCreateRecord(npcId);

        if (newAffinity >= 60 && record.state == RomanceState.None)
        {
            TransitionState(npcId, RomanceState.Crushing);
        }
        else if (newAffinity < 60 && record.state == RomanceState.Crushing)
        {
            TransitionState(npcId, RomanceState.None);
        }
    }

    /// <summary>
    /// 通用状态转移，触发事件和副作用
    /// </summary>
    public void TransitionState(string npcId, RomanceState newState)
    {
        var record = GetOrCreateRecord(npcId);
        RomanceState oldState = record.state;

        if (oldState == newState) return;

        record.state = newState;
        Debug.Log($"[RomanceSystem] {npcId} 状态转移: {oldState} → {newState}");
        OnRomanceStateChanged?.Invoke(npcId, oldState, newState);
    }

    // ========== 公共方法：告白 ==========

    /// <summary>
    /// 告白成功处理：设为 Dating，初始化恋爱数据
    /// </summary>
    public void OnConfessionSuccess(string npcId)
    {
        var record = GetOrCreateRecord(npcId);

        TransitionState(npcId, RomanceState.Dating);

        record.healthScore = 70;
        record.datingStartRound = GetCurrentGlobalRound();
        record.durationRounds = 0;
        record.moodSumDuringDating = 0;
        record.moodSampleCount = 0;
        record.consecutiveNoInteract = 0;
        record.nextAnniversaryRound = GetCurrentGlobalRound() + 8;

        // 如果是复合，标记
        if (record.breakupCount > 0)
        {
            record.hasReunited = true;
        }

        Debug.Log($"[RomanceSystem] {npcId} 告白成功！开始恋爱，纪念日: 回合{record.nextAnniversaryRound}");
    }

    /// <summary>
    /// 告白失败处理：设为 Cooldown，冷却4回合
    /// </summary>
    public void OnConfessionFail(string npcId)
    {
        var record = GetOrCreateRecord(npcId);

        TransitionState(npcId, RomanceState.Cooldown);
        record.cooldownRoundsLeft = 4;

        Debug.Log($"[RomanceSystem] {npcId} 告白失败，进入冷却期（4回合）");
    }

    // ========== 公共方法：互动与健康度 ==========

    /// <summary>
    /// 标记本回合已与指定NPC互动
    /// </summary>
    public void MarkInteractedThisRound(string npcId)
    {
        var record = GetOrCreateRecord(npcId);
        record.interactedThisRound = true;
    }

    /// <summary>
    /// 修改恋爱健康度，clamp 0~100，健康度归零时触发分手
    /// </summary>
    public void ModifyHealth(string npcId, int delta)
    {
        var record = GetOrCreateRecord(npcId);
        int oldHealth = record.healthScore;

        record.healthScore = Mathf.Clamp(record.healthScore + delta, 0, 100);

        if (record.healthScore != oldHealth)
        {
            OnRomanceHealthChanged?.Invoke(npcId);
        }

        // 健康度归零 → 触发分手
        if (record.healthScore <= 0 && record.state == RomanceState.Dating)
        {
            Debug.Log($"[RomanceSystem] {npcId} 恋爱健康度归零，触发分手");
            InitiateBreakup(npcId, BreakupReason.HealthZero);
        }
    }

    // ========== 公共方法：分手 ==========

    /// <summary>
    /// 分手处理：状态转移、计数、冷却、属性惩罚
    /// </summary>
    public void InitiateBreakup(string npcId, BreakupReason reason)
    {
        var record = GetOrCreateRecord(npcId);

        // 劈腿 → Hostile，其他 → BrokenUp
        RomanceState targetState = (reason == BreakupReason.CheatingDiscovered)
            ? RomanceState.Hostile
            : RomanceState.BrokenUp;

        TransitionState(npcId, targetState);

        record.breakupCount++;
        record.cooldownRoundsLeft = 4;

        // 分手属性惩罚
        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.Mood -= 10;
            PlayerAttributes.Instance.Stress += 5;
        }

        Debug.Log($"[RomanceSystem] {npcId} 分手！原因: {reason}，分手次数: {record.breakupCount}");
    }

    // ========== 回合结算 ==========

    /// <summary>
    /// 回合推进回调
    /// </summary>
    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        ProcessRoundEnd();
    }

    /// <summary>
    /// 回合结算逻辑 —— 遍历所有恋爱记录进行结算
    /// </summary>
    public void ProcessRoundEnd()
    {
        // 使用临时列表避免遍历过程中字典被修改
        var npcIds = new List<string>(records.Keys);

        foreach (string npcId in npcIds)
        {
            var record = records[npcId];

            // === Cooldown 态结算 ===
            if (record.state == RomanceState.Cooldown)
            {
                record.cooldownRoundsLeft--;
                if (record.cooldownRoundsLeft <= 0)
                {
                    record.cooldownRoundsLeft = 0;

                    // Bug2 修复: 冷却结束后检查好感度，若仍>=60则恢复暗恋状态
                    int affinity = GetAffinityDirect(npcId);
                    if (affinity >= 60)
                    {
                        TransitionState(npcId, RomanceState.Crushing);
                        Debug.Log($"[RomanceSystem] {npcId} 冷却结束，好感度{affinity}≥60，恢复 Crushing 状态");
                    }
                    else
                    {
                        TransitionState(npcId, RomanceState.None);
                        Debug.Log($"[RomanceSystem] {npcId} 冷却结束，恢复 None 状态");
                    }
                }
            }

            // === BrokenUp 态结算 (Bug1 修复: 冷却递减) ===
            if (record.state == RomanceState.BrokenUp)
            {
                if (record.cooldownRoundsLeft > 0)
                {
                    record.cooldownRoundsLeft--;
                    if (record.cooldownRoundsLeft <= 0)
                    {
                        record.cooldownRoundsLeft = 0;
                        Debug.Log($"[RomanceSystem] {npcId} BrokenUp 冷却结束，可以尝试复合");
                    }
                }
            }

            // === Dating 态结算 ===
            if (record.state == RomanceState.Dating)
            {
                // 持续回合 +1
                record.durationRounds++;

                // 采样心情
                if (PlayerAttributes.Instance != null)
                {
                    record.moodSumDuringDating += PlayerAttributes.Instance.Mood;
                    record.moodSampleCount++;
                }

                // 未互动检查
                if (!record.interactedThisRound)
                {
                    record.consecutiveNoInteract++;
                    ModifyHealth(npcId, -8);

                    // 连续未互动≥4回合 → 分手
                    if (record.consecutiveNoInteract >= 4 && record.state == RomanceState.Dating)
                    {
                        Debug.Log($"[RomanceSystem] {npcId} 连续{record.consecutiveNoInteract}回合未互动，触发分手");
                        InitiateBreakup(npcId, BreakupReason.ConsecutiveNoInteract);
                        // 分手后跳过后续 Dating 结算
                        record.interactedThisRound = false;
                        continue;
                    }
                }
                else
                {
                    // 互动了 → 重置连续未互动计数
                    record.consecutiveNoInteract = 0;
                }

                // 纪念日检查
                int currentGlobalRound = GetCurrentGlobalRound();
                if (record.nextAnniversaryRound > 0 && currentGlobalRound == record.nextAnniversaryRound)
                {
                    if (!record.interactedThisRound)
                    {
                        // 纪念日未互动 → 健康度大幅下降
                        ModifyHealth(npcId, -20);
                        Debug.Log($"[RomanceSystem] {npcId} 纪念日未互动！健康度 -20");
                    }
                    else
                    {
                        // 纪念日互动了 → 健康度提升
                        ModifyHealth(npcId, +15);
                        Debug.Log($"[RomanceSystem] {npcId} 纪念日互动！健康度 +15");
                    }

                    // 设置下次纪念日（每8回合）
                    record.nextAnniversaryRound = currentGlobalRound + 8;
                }

                // 恋爱中附加效果（每回合）
                if (PlayerAttributes.Instance != null && record.state == RomanceState.Dating)
                {
                    PlayerAttributes.Instance.Mood += 3;
                    PlayerAttributes.Instance.Stress -= 2;
                }

                // 恋爱金钱消耗
                if (GameState.Instance != null && record.state == RomanceState.Dating)
                {
                    GameState.Instance.AddMoney(-20);
                }

                // === Bug3-NPC主动分手: 好感度<50时NPC提出分手 ===
                if (record.state == RomanceState.Dating)
                {
                    int currentAffinity = GetAffinityDirect(npcId);
                    if (currentAffinity < 50)
                    {
                        Debug.Log($"[RomanceSystem] {npcId} 好感度{currentAffinity}<50，NPC主动提出分手");
                        InitiateBreakup(npcId, BreakupReason.NPCInitiated);
                        record.interactedThisRound = false;
                        continue;
                    }
                }

                // === Bug4-劈腿检测: 劈腿状态下每回合递增概率被发现 ===
                if (record.state == RomanceState.Dating && record.isCheating)
                {
                    record.cheatingRounds++;
                    // 概率: 20% + 每回合+10%
                    float discoveryChance = 0.20f + (record.cheatingRounds - 1) * 0.10f;
                    discoveryChance = UnityEngine.Mathf.Clamp01(discoveryChance);

                    float roll = UnityEngine.Random.value;
                    if (roll <= discoveryChance)
                    {
                        Debug.Log($"[RomanceSystem] 劈腿被发现！概率{discoveryChance:P0}，掷骰{roll:F3}");
                        // 所有 Dating 关系变 Hostile
                        TriggerCheatingDiscovered();
                        // 清零所有记录的互动标记，因为即将 break 退出主循环
                        foreach (var kvp in records)
                            kvp.Value.interactedThisRound = false;
                        break; // 退出循环，因为所有 Dating 记录已被修改
                    }
                }
            }

            // === 所有记录：重置本回合互动标记 ===
            record.interactedThisRound = false;
        }
    }

    // ========== 分手入口方法 (Bug3 修复) ==========

    /// <summary>
    /// 供事件系统调用的特殊事件分手入口
    /// </summary>
    public void TriggerSpecialBreakup(string npcId)
    {
        if (GetRomanceState(npcId) == RomanceState.Dating)
        {
            Debug.Log($"[RomanceSystem] {npcId} 特殊事件触发分手");
            InitiateBreakup(npcId, BreakupReason.SpecialEvent);
        }
    }

    /// <summary>
    /// 供 NPCInteractionMenu 调用的玩家主动分手入口
    /// </summary>
    public void TriggerPlayerBreakup(string npcId)
    {
        if (GetRomanceState(npcId) == RomanceState.Dating)
        {
            Debug.Log($"[RomanceSystem] {npcId} 玩家主动分手");
            InitiateBreakup(npcId, BreakupReason.PlayerInitiated);
        }
    }

    // ========== 劈腿检测 (Bug4) ==========

    /// <summary>
    /// 标记劈腿状态 —— 当玩家同时与多个NPC恋爱时调用
    /// </summary>
    public void MarkCheating(string npcId)
    {
        var record = GetOrCreateRecord(npcId);
        if (!record.isCheating)
        {
            record.isCheating = true;
            record.cheatingRounds = 0;
            Debug.Log($"[RomanceSystem] {npcId} 被标记为劈腿状态");
        }
    }

    /// <summary>
    /// 劈腿被发现 —— 所有 Dating 关系变 Hostile
    /// </summary>
    private void TriggerCheatingDiscovered()
    {
        var npcIds = new List<string>(records.Keys);
        foreach (string npcId in npcIds)
        {
            var record = records[npcId];
            if (record.state == RomanceState.Dating)
            {
                Debug.Log($"[RomanceSystem] {npcId} 因劈腿被发现，关系变为 Hostile");
                InitiateBreakup(npcId, BreakupReason.CheatingDiscovered);
            }
        }
    }

    // ========== IRomanceProvider 实现 (Bug5+6 修复) ==========

    /// <summary>
    /// 获取当前所有 Dating 状态的 NPC ID 列表
    /// </summary>
    public List<string> GetAllDatingNPCs()
    {
        List<string> datingNPCs = new List<string>();
        foreach (var kvp in records)
        {
            if (kvp.Value.state == RomanceState.Dating)
            {
                datingNPCs.Add(kvp.Key);
            }
        }
        return datingNPCs;
    }

    /// <summary>
    /// 是否有恋人 (任一 NPC 处于 Dating 状态)
    /// </summary>
    public bool HasPartner()
    {
        foreach (var kvp in records)
        {
            if (kvp.Value.state == RomanceState.Dating)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取恋人名称 (第一个 Dating NPC 的显示名)
    /// </summary>
    public string GetPartnerName()
    {
        foreach (var kvp in records)
        {
            if (kvp.Value.state == RomanceState.Dating)
            {
                if (NPCDatabase.Instance != null)
                {
                    NPCData npcData = NPCDatabase.Instance.GetNPC(kvp.Key);
                    if (npcData != null) return npcData.displayName;
                }
                return kvp.Key;
            }
        }
        return "";
    }

    /// <summary>
    /// 获取恋爱等级 (最高 Dating NPC 的 RomanceEndingTier 数值)
    /// IRomanceProvider 接口实现
    /// </summary>
    public int GetRomanceLevel()
    {
        int maxLevel = 0;
        foreach (var kvp in records)
        {
            if (kvp.Value.state == RomanceState.Dating)
            {
                RomanceEndingTier tier = CalculateEnding(kvp.Key);
                int level = (int)tier;
                if (level > maxLevel) maxLevel = level;
            }
        }
        return maxLevel;
    }

    // ========== 好感度直接查询 (避免缓存时序问题) ==========

    /// <summary>
    /// 直接从 AffinitySystem 查询好感度，避免 affinityCache 的时序问题
    /// </summary>
    private int GetAffinityDirect(string npcId)
    {
        if (AffinitySystem.Instance != null)
        {
            NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(npcId);
            return rel.affinity;
        }
        // 回退到缓存
        return GetAffinity(npcId);
    }

    // ========== 结局判定 ==========

    /// <summary>
    /// 计算指定NPC的恋爱结局等级（参考 GDD §12.11）
    /// </summary>
    public RomanceEndingTier CalculateEnding(string npcId)
    {
        var record = GetOrCreateRecord(npcId);
        int affinity = GetAffinity(npcId);

        // 5★ Engaged: 恋爱中 + 心情均值≥70 + 好感=100
        if (record.state == RomanceState.Dating
            && record.AverageMoodDuringDating >= 70f
            && affinity == 100)
        {
            return RomanceEndingTier.Engaged;
        }

        // 4★ Sweet: 恋爱中 + 心情均值≥60 + 好感≥90
        if (record.state == RomanceState.Dating
            && record.AverageMoodDuringDating >= 60f
            && affinity >= 90)
        {
            return RomanceEndingTier.Sweet;
        }

        // 3★ Confused: 恋爱中 + 心情均值<60
        if (record.state == RomanceState.Dating
            && record.AverageMoodDuringDating < 60f)
        {
            return RomanceEndingTier.Confused;
        }

        // 2★ BrokenUp: 非恋爱中 + 曾恋爱过
        if (record.state != RomanceState.Dating
            && record.datingStartRound >= 0)
        {
            return RomanceEndingTier.BrokenUp;
        }

        // 1★ Single: 默认
        return RomanceEndingTier.Single;
    }

    // ========== 游戏结束回调 ==========

    /// <summary>
    /// 游戏完成（毕业）时的回调，可在此触发结局演出
    /// </summary>
    private void OnGameCompleted()
    {
        Debug.Log("[RomanceSystem] 游戏结束，计算恋爱结局...");

        foreach (var kvp in records)
        {
            RomanceEndingTier ending = CalculateEnding(kvp.Key);
            Debug.Log($"[RomanceSystem] {kvp.Key} 结局: {ending} ({(int)ending}★)");
        }
    }

    // ========== 工具方法 ==========

    /// <summary>
    /// 获取当前全局回合号（学年*80 + 学期*40 + 回合）
    /// </summary>
    private int GetCurrentGlobalRound()
    {
        if (GameState.Instance == null) return 0;

        int year = GameState.Instance.CurrentYear;
        int semester = GameState.Instance.CurrentSemester;
        int round = GameState.Instance.CurrentRound;

        return (year - 1) * 80 + (semester - 1) * 40 + round;
    }
}
