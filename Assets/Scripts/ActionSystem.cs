using UnityEngine;
using System;
using System.Collections.Generic;

// ========== 数据类 ==========

/// <summary>单条属性变化效果</summary>
[System.Serializable]
public class AttributeEffect
{
    public string attributeName; // "学力", "魅力", "体魄", "领导力", "压力", "心情"
    public int amount;

    public AttributeEffect(string name, int amount)
    {
        this.attributeName = name;
        this.amount = amount;
    }
}

/// <summary>行动定义数据</summary>
[System.Serializable]
public class ActionDefinition
{
    public string id;            // "study", "social", "goout", "sleep"
    public string displayName;   // "自习", "社交", "出校门", "睡觉"
    public int actionPointCost;  // 行动点消耗（通常为 1）
    public int moneyCost;        // 金钱消耗（出校门为 50）
    public bool endsRound;       // 是否立即结束回合（睡觉 = true）
    public bool isGlobal;        // 是否全局行动（不受地点限制，如"睡觉"在宿舍以外也显示）
    public LocationId? targetLocation; // 默认执行地点，会在结算前自动传送
    public AttributeEffect[] effects;
}

/// <summary>
/// 行动系统 —— 管理所有可执行行动的定义、校验与执行
/// </summary>
public class ActionSystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static ActionSystem Instance { get; private set; }

    // ========== 事件 ==========
    /// <summary>行动执行成功后触发</summary>
    public event Action<ActionDefinition> OnActionExecuted;

    // ========== 内部字段 ==========
    private List<ActionDefinition> actionDefinitions = new List<ActionDefinition>();

    // ========== 初始化 ==========

    /// <summary>初始化默认行动列表</summary>
    private void InitDefaultActions()
    {
        actionDefinitions.Clear();

        // ===== 图书馆/教学楼行动 =====

        // 自习（设计文档：2AP, 学力+5~10）：取中间值学力+7, 压力+5
        actionDefinitions.Add(new ActionDefinition
        {
            id = "study",
            displayName = "自习",
            actionPointCost = 2,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.TeachingBuilding,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("学力", 7),
                new AttributeEffect("压力", 5)
            }
        });

        // 上课（教学楼专属，设计文档未单独定义AP消耗，按行动点消耗规则保持2AP）
        actionDefinitions.Add(new ActionDefinition
        {
            id = "attend_class",
            displayName = "上课",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.TeachingBuilding,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("学力", 5),
                new AttributeEffect("压力", 3)
            }
        });

        // ===== 宿舍行动 =====

        // 社交（设计文档：1AP）：魅力+2, 领导力+1, 心情+8
        actionDefinitions.Add(new ActionDefinition
        {
            id = "social",
            displayName = "社交",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.Dormitory,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("魅力", 2),
                new AttributeEffect("领导力", 1),
                new AttributeEffect("心情", 8)
            }
        });

        // 打游戏（宿舍专属）：心情+12, 压力-8, 学力-2
        actionDefinitions.Add(new ActionDefinition
        {
            id = "play_game",
            displayName = "打游戏",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.Dormitory,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("心情", 12),
                new AttributeEffect("压力", -8),
                new AttributeEffect("学力", -2)
            }
        });

        // 睡觉（全局行动，设计文档：1AP, 行动点+3, 压力-5，每回合必须触发一次）
        // 注意：endsRound=true 会清空剩余AP并结束回合
        actionDefinitions.Add(new ActionDefinition
        {
            id = "sleep",
            displayName = "睡觉",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = true,
            targetLocation = LocationId.Dormitory,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("压力", -5)
            }
        });

        // ===== 校外/出门行动 =====

        // 出校门（设计文档：2AP, 进入子活动菜单）：全局行动，任何地点可触发
        actionDefinitions.Add(new ActionDefinition
        {
            id = "goout",
            displayName = "出校门",
            actionPointCost = 2,
            moneyCost = 50,
            endsRound = false,
            isGlobal = true,
            targetLocation = LocationId.TakeoutStation,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("心情", 10),
                new AttributeEffect("压力", -5)
            }
        });

        // ===== 食堂行动 =====

        // 吃饭：体魄+1, 心情+3
        actionDefinitions.Add(new ActionDefinition
        {
            id = "eat",
            displayName = "吃饭",
            actionPointCost = 1,
            moneyCost = 15,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.Canteen,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("体魄", 1),
                new AttributeEffect("心情", 3)
            }
        });

        // ===== 操场行动 =====

        // 校园跑（设计文档：1AP, 体魄+1, 压力-3, 心情+2）
        actionDefinitions.Add(new ActionDefinition
        {
            id = "exercise",
            displayName = "校园跑",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.Playground,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("体魄", 1),
                new AttributeEffect("压力", -3),
                new AttributeEffect("心情", 2)
            }
        });

        // 背单词（设计文档：1AP, 四六级通过率+5%）
        actionDefinitions.Add(new ActionDefinition
        {
            id = "memorize_words",
            displayName = "背单词",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = true,
            targetLocation = LocationId.TeachingBuilding,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("学力", 1),
                new AttributeEffect("压力", 2)
            }
        });

        // 体测：体魄+2, 压力+8
        actionDefinitions.Add(new ActionDefinition
        {
            id = "sports_test",
            displayName = "体测",
            actionPointCost = 2,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.Playground,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("体魄", 2),
                new AttributeEffect("压力", 8)
            }
        });

        // ===== 教超行动 =====

        // 购物：心情+5
        actionDefinitions.Add(new ActionDefinition
        {
            id = "shop",
            displayName = "购物",
            actionPointCost = 1,
            moneyCost = 30,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.Store,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("心情", 5)
            }
        });

        // ===== 快递站行动 =====

        // 取快递：心情+8
        actionDefinitions.Add(new ActionDefinition
        {
            id = "pickup_express",
            displayName = "取快递",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.ExpressStation,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("心情", 8)
            }
        });

        // ===== 外卖站行动 =====

        // 点外卖：心情+6, 体魄-1
        actionDefinitions.Add(new ActionDefinition
        {
            id = "order_takeout",
            displayName = "点外卖",
            actionPointCost = 1,
            moneyCost = 25,
            endsRound = false,
            isGlobal = false,
            targetLocation = LocationId.TakeoutStation,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("心情", 6),
                new AttributeEffect("体魄", -1)
            }
        });
    }

    // ========== 公共方法 ==========

    /// <summary>根据 id 获取行动定义，找不到返回 null</summary>
    public ActionDefinition GetAction(string id)
    {
        for (int i = 0; i < actionDefinitions.Count; i++)
        {
            if (actionDefinitions[i].id == id)
                return actionDefinitions[i];
        }
        return null;
    }

    /// <summary>返回所有行动定义数组</summary>
    public ActionDefinition[] GetAllActions()
    {
        return actionDefinitions.ToArray();
    }

    /// <summary>获取指定地点的可用行动（含全局行动）</summary>
    public ActionDefinition[] GetActionsForLocation(LocationId locationId)
    {
        if (LocationManager.Instance == null)
            return actionDefinitions.ToArray();

        return LocationManager.Instance.GetAvailableActions(locationId);
    }

    /// <summary>返回行动不可执行的原因；可执行则返回空字符串</summary>
    public string GetActionBlockReason(string id)
    {
        ActionDefinition action = GetAction(id);
        if (action == null)
        {
            return "行动数据缺失";
        }

        GameState gs = GameState.Instance;
        if (gs == null)
        {
            return "核心状态尚未初始化";
        }

        int extraAP = 0;
        if (PenaltySystem.Instance != null)
            extraAP = PenaltySystem.Instance.GetGuiltExtraAPCost();

        int totalAPCost = action.actionPointCost + extraAP;
        if (gs.ActionPoints < totalAPCost)
        {
            return extraAP > 0
                ? $"行动点不足，需要 {action.actionPointCost} AP + 额外 {extraAP} AP 负担"
                : $"行动点不足，需要 {action.actionPointCost} AP";
        }

        if (gs.Money < action.moneyCost)
        {
            return $"余额不足，需要 ¥{action.moneyCost}";
        }

        return string.Empty;
    }

    /// <summary>检查指定行动是否可执行（行动点、金钱、地点）</summary>
    public bool CanExecuteAction(string id)
    {
        string reason = GetActionBlockReason(id);
        if (!string.IsNullOrEmpty(reason))
        {
            ActionDefinition action = GetAction(id);
            if (action == null)
                Debug.LogWarning($"[ActionSystem] 未找到行动定义: {id}");
            else
                Debug.LogWarning($"[ActionSystem] 无法执行行动: {id}, 原因: {reason}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 执行指定行动：扣除行动点和金钱，应用属性效果，触发事件。
    /// 若行动标记为 endsRound 则清空剩余行动点。
    /// </summary>
    public bool ExecuteAction(string id)
    {
        // 1. 校验
        if (!CanExecuteAction(id))
        {
            Debug.LogWarning($"[ActionSystem] 无法执行行动: {id}");
            return false;
        }

        ActionDefinition action = GetAction(id);
        GameState gs = GameState.Instance;

        if (!TryMoveToActionLocation(action))
        {
            return false;
        }

        // 2. 扣除行动点（含负罪感额外消耗）
        int extraAPCost = 0;
        if (PenaltySystem.Instance != null)
            extraAPCost = PenaltySystem.Instance.GetGuiltExtraAPCost();
        gs.ConsumeActionPoint(action.actionPointCost + extraAPCost);

        // 3. 扣除金钱（通过 EconomyManager 记录交易）
        if (action.moneyCost > 0)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Spend(action.moneyCost,
                    TransactionRecord.TransactionType.OtherExpense,
                    $"行动消费: {action.displayName}");
            }
            else
            {
                // 降级回退：直接扣除
                gs.AddMoney(-action.moneyCost);
            }
        }

        // 4. 应用属性效果（考虑惩罚系统的效率乘数）
        if (action.effects != null && PlayerAttributes.Instance != null)
        {
            float guiltMult = 1f;
            float slackingStudyMult = 1f;
            if (PenaltySystem.Instance != null)
            {
                guiltMult = PenaltySystem.Instance.GetGuiltEfficiencyMultiplier();
                slackingStudyMult = PenaltySystem.Instance.GetSlackingStudyMultiplier();
            }

            // 天赋加成
            float studyLibraryBonus = 1f;
            if (TalentSystem.Instance != null && TalentSystem.Instance.HasTalentEffect("study_library_bonus"))
            {
                if (action.id == "study" && GameState.Instance != null && GameState.Instance.CurrentLocation == LocationId.Library)
                    studyLibraryBonus = 1f + TalentSystem.Instance.GetTalentEffectValue("study_library_bonus") / 100f;
            }

            for (int i = 0; i < action.effects.Length; i++)
            {
                AttributeEffect effect = action.effects[i];
                int finalAmount = effect.amount;

                // 正面效果受负罪感效率影响
                if (finalAmount > 0)
                {
                    finalAmount = Mathf.RoundToInt(finalAmount * guiltMult);
                }

                // 学力正收益受摆烂值影响
                if (effect.attributeName == "学力" && finalAmount > 0)
                {
                    finalAmount = Mathf.RoundToInt(finalAmount * slackingStudyMult * studyLibraryBonus);
                }

                // 天赋：压力增长减缓
                if (effect.attributeName == "压力" && finalAmount > 0)
                {
                    if (TalentSystem.Instance != null && TalentSystem.Instance.HasTalentEffect("stress_growth_reduction"))
                    {
                        float reduction = TalentSystem.Instance.GetTalentEffectValue("stress_growth_reduction") / 100f;
                        finalAmount = Mathf.RoundToInt(finalAmount * (1f - reduction));
                    }
                }

                PlayerAttributes.Instance.AddAttribute(effect.attributeName, finalAmount);
            }
        }

        // 5. 日志输出
        Debug.Log($"[ActionSystem] 执行行动: {action.displayName} (id={action.id}), " +
                  $"消耗行动点={action.actionPointCost}, 消耗金钱={action.moneyCost}");

        ShowActionNotification(action, extraAPCost);

        // 6. 触发事件
        OnActionExecuted?.Invoke(action);

        // 7. 返回成功
        return true;
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

        InitDefaultActions();
    }

    private bool TryMoveToActionLocation(ActionDefinition action)
    {
        if (action == null || !action.targetLocation.HasValue || GameState.Instance == null)
        {
            return true;
        }

        if (LocationManager.Instance == null)
        {
            Debug.LogWarning($"[ActionSystem] 地点管理器未初始化，无法为行动 {action.id} 执行场景跳转。");
            return false;
        }

        LocationId currentLocation = GameState.Instance.CurrentLocation;
        ActionDefinition[] currentActions = LocationManager.Instance.GetAvailableActions(currentLocation);
        for (int i = 0; i < currentActions.Length; i++)
        {
            if (currentActions[i] != null && currentActions[i].id == action.id)
            {
                return true;
            }
        }

        LocationId targetLocation = action.targetLocation.Value;
        if (currentLocation == targetLocation)
        {
            return true;
        }

        bool moved = LocationManager.Instance.MoveTo(targetLocation);
        if (!moved)
        {
            Debug.LogWarning($"[ActionSystem] 行动 {action.id} 需要传送到 {targetLocation}，但跳转失败。");
        }

        return moved;
    }

    private void ShowActionNotification(ActionDefinition action, int extraAPCost)
    {
        if (MissionUI.Instance == null || action == null)
        {
            return;
        }

        MissionUI.Instance.ShowSystemNotification(
            action.displayName,
            BuildActionSummary(action, extraAPCost),
            new Color(0.28f, 0.72f, 0.86f),
            3f);
    }

    private string BuildActionSummary(ActionDefinition action, int extraAPCost)
    {
        List<string> parts = new List<string>();

        int totalAPCost = action.actionPointCost + extraAPCost;
        if (totalAPCost > 0)
        {
            if (extraAPCost > 0)
                parts.Add($"AP-{totalAPCost}（含额外负担 {extraAPCost}）");
            else
                parts.Add($"AP-{totalAPCost}");
        }

        if (action.moneyCost > 0)
        {
            parts.Add($"花费¥{action.moneyCost}");
        }

        string effectSummary = BuildEffectSummary(action.effects);
        if (!string.IsNullOrEmpty(effectSummary))
        {
            parts.Add(effectSummary);
        }

        if (action.id == "sleep")
        {
            parts.Add("这一轮的节奏已经收尾，可以准备进入下一回合。");
        }

        return parts.Count > 0 ? string.Join("，", parts) : "已完成本次安排。";
    }

    private string BuildEffectSummary(AttributeEffect[] effects)
    {
        if (effects == null || effects.Length == 0)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        for (int i = 0; i < effects.Length; i++)
        {
            AttributeEffect effect = effects[i];
            if (effect == null || string.IsNullOrEmpty(effect.attributeName) || effect.amount == 0)
            {
                continue;
            }

            string sign = effect.amount > 0 ? "+" : string.Empty;
            parts.Add($"{effect.attributeName}{sign}{effect.amount}");
        }

        return parts.Count > 0 ? string.Join("，", parts) : string.Empty;
    }
}
