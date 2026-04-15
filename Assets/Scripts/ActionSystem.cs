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

        // 自习：学力+3, 压力+5
        actionDefinitions.Add(new ActionDefinition
        {
            id = "study",
            displayName = "自习",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("学力", 3),
                new AttributeEffect("压力", 5)
            }
        });

        // 上课（教学楼专属）：学力+5, 压力+3
        actionDefinitions.Add(new ActionDefinition
        {
            id = "attend_class",
            displayName = "上课",
            actionPointCost = 2,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("学力", 5),
                new AttributeEffect("压力", 3)
            }
        });

        // ===== 宿舍行动 =====

        // 社交：魅力+2, 领导力+1, 心情+8
        actionDefinitions.Add(new ActionDefinition
        {
            id = "social",
            displayName = "社交",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
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
            effects = new AttributeEffect[]
            {
                new AttributeEffect("心情", 12),
                new AttributeEffect("压力", -8),
                new AttributeEffect("学力", -2)
            }
        });

        // 睡觉（全局行动）：体魄+2, 压力-15, 心情+5
        actionDefinitions.Add(new ActionDefinition
        {
            id = "sleep",
            displayName = "睡觉",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = true,
            isGlobal = true,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("体魄", 2),
                new AttributeEffect("压力", -15),
                new AttributeEffect("心情", 5)
            }
        });

        // ===== 校外/出门行动 =====

        // 出校门：心情+10, 压力-5
        actionDefinitions.Add(new ActionDefinition
        {
            id = "goout",
            displayName = "出校门",
            actionPointCost = 1,
            moneyCost = 50,
            endsRound = false,
            isGlobal = false,
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
            effects = new AttributeEffect[]
            {
                new AttributeEffect("体魄", 1),
                new AttributeEffect("心情", 3)
            }
        });

        // ===== 操场行动 =====

        // 校园跑：体魄+4, 压力-5, 心情+2
        actionDefinitions.Add(new ActionDefinition
        {
            id = "exercise",
            displayName = "校园跑",
            actionPointCost = 1,
            moneyCost = 0,
            endsRound = false,
            isGlobal = false,
            effects = new AttributeEffect[]
            {
                new AttributeEffect("体魄", 4),
                new AttributeEffect("压力", -5),
                new AttributeEffect("心情", 2)
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

    /// <summary>检查指定行动是否可执行（行动点、金钱、地点）</summary>
    public bool CanExecuteAction(string id)
    {
        ActionDefinition action = GetAction(id);
        if (action == null)
        {
            Debug.LogWarning($"[ActionSystem] 未找到行动定义: {id}");
            return false;
        }

        GameState gs = GameState.Instance;
        if (gs == null)
        {
            Debug.LogError("[ActionSystem] GameState 实例不存在");
            return false;
        }

        if (gs.ActionPoints < action.actionPointCost)
            return false;

        if (gs.Money < action.moneyCost)
            return false;

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

        // 2. 扣除行动点
        gs.ConsumeActionPoint(action.actionPointCost);

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

        // 4. 应用属性效果
        if (action.effects != null && PlayerAttributes.Instance != null)
        {
            for (int i = 0; i < action.effects.Length; i++)
            {
                AttributeEffect effect = action.effects[i];
                PlayerAttributes.Instance.AddAttribute(effect.attributeName, effect.amount);
            }
        }

        // 5. 日志输出
        Debug.Log($"[ActionSystem] 执行行动: {action.displayName} (id={action.id}), " +
                  $"消耗行动点={action.actionPointCost}, 消耗金钱={action.moneyCost}");

        // 6. 触发事件
        OnActionExecuted?.Invoke(action);

        // 7. 如果 endsRound 且还有剩余行动点，清零
        if (action.endsRound && gs.ActionPoints > 0)
        {
            gs.ActionPoints = 0;
        }

        // 8. 返回成功
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
}
