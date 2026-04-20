using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 惩罚系统 —— 每回合结算负罪感/摆烂值/心理健康的梯度效果
/// 设计文档: 06_角色属性设计.md
/// 负罪感4阶: 30-59轻度 / 60-89中度 / 90-149重度 / >=150临界
/// 摆烂值4阶: 30-59轻度 / 60-89中度 / 90-119重度 / >=120退学
/// 心理健康: 100→0，多维度增减，触发抑郁系统
/// </summary>
public class PenaltySystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static PenaltySystem Instance { get; private set; }

    // ========== 事件 ==========
    public event Action<string> OnPenaltyWarning;

    // ========== 运行时状态 ==========
    private int slackingValue = 0;
    private int mentalHealth = 100;
    private int consecutiveNoStudyRounds = 0;
    private int consecutiveNoSocialRounds = 0;
    private int consecutiveHighGuiltRounds = 0;

    // ========== 属性 ==========
    public int SlackingValue => slackingValue;
    public int MentalHealth => mentalHealth;

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
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;

        if (ActionSystem.Instance != null)
            ActionSystem.Instance.OnActionExecuted += OnActionExecuted;
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
        if (ActionSystem.Instance != null)
            ActionSystem.Instance.OnActionExecuted -= OnActionExecuted;
    }

    // ========== 摆烂值修改 ==========

    /// <summary>增加摆烂值</summary>
    public void AddSlacking(int amount, string reason = "")
    {
        if (amount <= 0) return;
        slackingValue += amount;
        Debug.Log($"[PenaltySystem] 摆烂值+{amount} ({reason})，当前: {slackingValue}");
    }

    /// <summary>减少摆烂值</summary>
    public void ReduceSlacking(int amount, string reason = "")
    {
        if (amount <= 0) return;
        slackingValue = Mathf.Max(0, slackingValue - amount);
        Debug.Log($"[PenaltySystem] 摆烂值-{amount} ({reason})，当前: {slackingValue}");
    }

    /// <summary>修改心理健康值</summary>
    public void ModifyMentalHealth(int delta, string reason = "")
    {
        mentalHealth = Mathf.Clamp(mentalHealth + delta, 0, 100);
        Debug.Log($"[PenaltySystem] 心理健康{(delta >= 0 ? "+" : "")}{delta} ({reason})，当前: {mentalHealth}");
    }

    // ========== 行动回调 ==========

    private void OnActionExecuted(ActionDefinition action)
    {
        // 自习/上课 → 减少摆烂值，重置连续未自习计数
        if (action.id == "study" || action.id == "attend_class")
        {
            ReduceSlacking(action.id == "study" ? 2 : 1, "自习/上课");
            consecutiveNoStudyRounds = 0;
        }

        // 社交 → 重置连续无社交计数
        if (action.id == "social" || action.id == "eat_together" || action.id == "hang_out")
        {
            consecutiveNoSocialRounds = 0;
        }

        // 睡觉次数检查（在ActionSystem中已消耗AP）
    }

    // ========== 回合结算 ==========

    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        if (result == GameState.RoundAdvanceResult.Graduated) return;
        if (PlayerAttributes.Instance == null) return;

        // 追踪连续行为
        consecutiveNoStudyRounds++;
        consecutiveNoSocialRounds++;

        // 连续2回合不自习 → 摆烂+5
        if (consecutiveNoStudyRounds >= 2)
        {
            AddSlacking(5, "连续2回合不自习");
        }

        // 连续3回合不社交 → 摆烂+4，心理健康-5
        if (consecutiveNoSocialRounds >= 3)
        {
            AddSlacking(4, "连续3回合不参加社交");
            ModifyMentalHealth(-5, "社交孤立");
        }

        // ===== 处理负罪感梯度效果 =====
        ProcessGuiltEffects();

        // ===== 处理摆烂值梯度效果 =====
        ProcessSlackingEffects();

        // ===== 处理心理健康 =====
        ProcessMentalHealth();
    }

    // ========== 负罪感梯度 ==========

    private void ProcessGuiltEffects()
    {
        int guilt = PlayerAttributes.Instance.Guilt;

        if (guilt >= 150)
        {
            // 临界破防：极速崩溃
            PlayerAttributes.Instance.Stress += 15;
            PlayerAttributes.Instance.Mood -= 10;
            ModifyMentalHealth(-15, "负罪感临界破防");
            AddSlacking(8, "负罪感极度内耗");
            PlayerAttributes.Instance.Darkness += 3;
            Debug.Log("[PenaltySystem] 负罪感临界破防! 压力+15，心情-10，心理健康-15，摆烂+8");
            OnPenaltyWarning?.Invoke("负罪感已到临界！你感到一切都在崩塌...");
        }
        else if (guilt >= 90)
        {
            // 重度负罪感
            PlayerAttributes.Instance.Stress += 10;
            PlayerAttributes.Instance.Mood -= 8;
            ModifyMentalHealth(-4, "重度负罪感");

            // 核心属性随机衰减-5
            int rnd = UnityEngine.Random.Range(0, 3);
            switch (rnd)
            {
                case 0: PlayerAttributes.Instance.Study -= 5; break;
                case 1: PlayerAttributes.Instance.Charm -= 5; break;
                case 2: PlayerAttributes.Instance.Physique -= 5; break;
            }

            // 冲动消费 50-200元
            int impulseSpend = UnityEngine.Random.Range(50, 201);
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Spend(impulseSpend, TransactionRecord.TransactionType.OtherExpense, "冲动消费");
            else if (GameState.Instance != null)
                GameState.Instance.AddMoney(-impulseSpend);

            consecutiveHighGuiltRounds++;
            Debug.Log($"[PenaltySystem] 重度负罪感! 压力+10，心情-8，属性衰减，冲动消费¥{impulseSpend}");
            OnPenaltyWarning?.Invoke("负罪感压得你喘不过气来，你开始冲动消费来麻痹自己...");
        }
        else if (guilt >= 60)
        {
            // 中度负罪感
            PlayerAttributes.Instance.Stress += 6;
            PlayerAttributes.Instance.Mood -= 5;
            ModifyMentalHealth(-2, "中度负罪感焦虑");
            consecutiveHighGuiltRounds = 0;
            Debug.Log("[PenaltySystem] 中度负罪感: 压力+6，心情-5，心理健康-2");
        }
        else if (guilt >= 30)
        {
            // 轻度负罪感
            PlayerAttributes.Instance.Stress += 3;
            PlayerAttributes.Instance.Mood -= 2;
            consecutiveHighGuiltRounds = 0;
            Debug.Log("[PenaltySystem] 轻度负罪感: 压力+3，心情-2");
        }
        else
        {
            consecutiveHighGuiltRounds = 0;
        }
    }

    // ========== 摆烂值梯度 ==========

    private void ProcessSlackingEffects()
    {
        if (slackingValue >= 120)
        {
            // 彻底摆烂 → 强制退学
            Debug.Log("[PenaltySystem] 摆烂值>=120，触发退学结局！");
            OnPenaltyWarning?.Invoke("你已经完全放弃了自己。辅导员通知你：学校决定劝退...");

            // 设置标记，由EndingDeterminer检查
            if (EventHistory.Instance != null)
            {
                EventHistory.Instance.SetFlag("forced_expulsion", true);
            }
        }
        else if (slackingValue >= 90)
        {
            // 重度摆烂
            PlayerAttributes.Instance.Study -= 5;
            PlayerAttributes.Instance.Leadership -= 3;
            Debug.Log("[PenaltySystem] 重度摆烂: 学力-5，领导力-3");
            OnPenaltyWarning?.Invoke("辅导员找你谈话：再这样下去，恐怕要发学业警告了...");
        }
        else if (slackingValue >= 60)
        {
            // 中度摆烂
            PlayerAttributes.Instance.Study -= 3;
            Debug.Log("[PenaltySystem] 中度摆烂: 学力-3");
        }
        else if (slackingValue >= 30)
        {
            // 轻度摆烂：心情+2（摆着爽），但学力收益打折在ActionSystem中处理
            PlayerAttributes.Instance.Mood += 2;
            Debug.Log("[PenaltySystem] 轻度摆烂: 心情+2（摆着爽）");
        }
    }

    // ========== 心理健康 ==========

    private void ProcessMentalHealth()
    {
        // 压力>60 → 心理健康-3/回合
        if (PlayerAttributes.Instance.Stress > 60)
        {
            ModifyMentalHealth(-3, "持续高压");
        }

        // 心情<40 → 心理健康-3/回合
        if (PlayerAttributes.Instance.Mood < 40)
        {
            ModifyMentalHealth(-3, "持续低落");
        }

        // 金钱<0 → 心理健康-5/回合
        if (GameState.Instance != null && GameState.Instance.Money < 0)
        {
            ModifyMentalHealth(-5, "负债焦虑");
        }

        // 心理健康恢复因子
        if (PlayerAttributes.Instance.Mood > 70 && PlayerAttributes.Instance.Stress < 30)
        {
            ModifyMentalHealth(2, "心态良好");
        }

        // 天赋：心理疏导 - 心理健康<50时自动恢复+2
        if (TalentSystem.Instance != null && TalentSystem.Instance.HasTalentEffect("auto_mental_recovery"))
        {
            if (mentalHealth < 50)
            {
                int recovery = Mathf.FloorToInt(TalentSystem.Instance.GetTalentEffectValue("auto_mental_recovery"));
                ModifyMentalHealth(recovery, "天赋:心理疏导");
            }
        }

        // 天赋：恢复力强 - 心情<30时自动恢复+3
        if (TalentSystem.Instance != null && TalentSystem.Instance.HasTalentEffect("auto_mood_recovery"))
        {
            if (PlayerAttributes.Instance.Mood < 30)
            {
                int recovery = Mathf.FloorToInt(TalentSystem.Instance.GetTalentEffectValue("auto_mood_recovery"));
                PlayerAttributes.Instance.Mood += recovery;
            }
        }

        // 心理健康阶段警告
        if (mentalHealth <= 20 && mentalHealth > 0)
        {
            OnPenaltyWarning?.Invoke("你感到前所未有的疲惫和空虚，也许该找人聊聊...");
        }
        else if (mentalHealth <= 0)
        {
            Debug.Log("[PenaltySystem] 心理健康归零！触发抑郁结局前置");
            OnPenaltyWarning?.Invoke("一切都变得灰暗，你已经很久没有笑过了...");
            if (EventHistory.Instance != null)
            {
                EventHistory.Instance.SetFlag("depression_critical", true);
            }
        }
    }

    // ========== 效果修正查询（供其他系统调用） ==========

    /// <summary>获取负罪感导致的收益倍率（1.0=正常, 0.8=轻度, 0.5=中度, 0.2=重度, 0=临界）</summary>
    public float GetGuiltEfficiencyMultiplier()
    {
        if (PlayerAttributes.Instance == null) return 1f;
        int guilt = PlayerAttributes.Instance.Guilt;

        if (guilt >= 150) return 0f;
        if (guilt >= 90) return 0.2f;
        if (guilt >= 60) return 0.5f;
        if (guilt >= 30) return 0.8f;
        return 1f;
    }

    /// <summary>获取摆烂值导致的学力收益倍率</summary>
    public float GetSlackingStudyMultiplier()
    {
        if (slackingValue >= 90) return 0.5f; // 重度
        if (slackingValue >= 60) return 0.5f; // 中度
        if (slackingValue >= 30) return 0.8f; // 轻度
        return 1f;
    }

    /// <summary>获取负罪感导致的额外AP消耗</summary>
    public int GetGuiltExtraAPCost()
    {
        if (PlayerAttributes.Instance == null) return 0;
        int guilt = PlayerAttributes.Instance.Guilt;

        // 天赋：情绪隔离 - 中度负罪感不额外消耗AP
        if (guilt >= 60 && guilt < 90)
        {
            if (TalentSystem.Instance != null && TalentSystem.Instance.HasTalentEffect("guilt_ap_immunity"))
                return 0;
        }

        if (guilt >= 90) return 2;
        if (guilt >= 60) return 1;
        return 0;
    }

    // ========== ISaveable 兼容 ==========

    public void SaveToData(SaveData data)
    {
        // 使用eventFlags存储
        data.eventFlags.RemoveAll(f => f.key.StartsWith("penalty_"));
        data.eventFlags.Add(new StringBoolPair($"penalty_slacking_{slackingValue}", true));
        data.eventFlags.Add(new StringBoolPair($"penalty_mental_{mentalHealth}", true));
        data.eventFlags.Add(new StringBoolPair($"penalty_nostudy_{consecutiveNoStudyRounds}", true));
        data.eventFlags.Add(new StringBoolPair($"penalty_nosocial_{consecutiveNoSocialRounds}", true));
    }

    public void LoadFromData(SaveData data)
    {
        slackingValue = 0;
        mentalHealth = 100;
        consecutiveNoStudyRounds = 0;
        consecutiveNoSocialRounds = 0;

        foreach (var pair in data.eventFlags)
        {
            if (pair.key.StartsWith("penalty_slacking_"))
            {
                if (int.TryParse(pair.key.Substring("penalty_slacking_".Length), out int v)) slackingValue = v;
            }
            else if (pair.key.StartsWith("penalty_mental_"))
            {
                if (int.TryParse(pair.key.Substring("penalty_mental_".Length), out int v)) mentalHealth = v;
            }
            else if (pair.key.StartsWith("penalty_nostudy_"))
            {
                if (int.TryParse(pair.key.Substring("penalty_nostudy_".Length), out int v)) consecutiveNoStudyRounds = v;
            }
            else if (pair.key.StartsWith("penalty_nosocial_"))
            {
                if (int.TryParse(pair.key.Substring("penalty_nosocial_".Length), out int v)) consecutiveNoSocialRounds = v;
            }
        }
    }
}
