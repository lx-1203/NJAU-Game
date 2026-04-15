using UnityEngine;
using System;

// ========== 债务阈值常量 ==========

/// <summary>
/// 债务系统的金额阈值定义
/// </summary>
public static class DebtThresholds
{
    /// <summary>食物受限阈值：余额低于此值时只能吃泡面</summary>
    public const int FoodRestriction = 200;
    /// <summary>透支阈值：余额低于此值时进入透支状态</summary>
    public const int Overdraft = 0;
    /// <summary>网贷事件阈值：余额低于此值时触发网贷剧情</summary>
    public const int LoanEvent = -2000;
    /// <summary>破产阈值：余额低于此值时触发破产结局</summary>
    public const int Bankruptcy = -5000;
}

// ========== 债务事件触发器接口 ==========

/// <summary>
/// 债务事件触发器接口 —— 各阈值被突破时的回调
/// </summary>
public interface IDebtEventTrigger
{
    /// <summary>余额低于200：食物选择受限提示</summary>
    void OnFoodRestricted();
    /// <summary>余额低于0：透支状态开始</summary>
    void OnOverdraftStarted();
    /// <summary>余额低于-2000：网贷事件触发</summary>
    void OnLoanEventTriggered();
    /// <summary>余额低于-5000：破产结局触发</summary>
    void OnBankruptcyTriggered();
}

// ========== 默认债务事件触发器 ==========

/// <summary>
/// 债务事件触发器的默认实现 —— 通过 Debug.Log 和 DialogueSystem 进行提示
/// </summary>
public class DefaultDebtEventTrigger : IDebtEventTrigger
{
    /// <summary>食物受限：提示只能吃泡面</summary>
    public void OnFoodRestricted()
    {
        Debug.Log("[DebtSystem] 快揭不开锅了！只能吃泡面了。");
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue("系统", new string[]
            {
                "你的余额已不足200元，只能吃最便宜的泡面了..."
            });
        }
    }

    /// <summary>透支开始：提示每回合压力+10</summary>
    public void OnOverdraftStarted()
    {
        Debug.Log("[DebtSystem] 进入透支状态！每回合压力+10");
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue("系统", new string[]
            {
                "你的账户已经透支了！欠债的压力让你喘不过气...",
                "每回合压力将增加10点，尽快想办法赚钱还债吧。"
            });
        }
    }

    /// <summary>网贷事件：触发网贷剧情线</summary>
    public void OnLoanEventTriggered()
    {
        Debug.Log("[DebtSystem] 触发网贷事件！DE_003");
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue("系统", new string[]
            {
                "你收到了一条短信：\"急用钱？秒批秒到账！\"",
                "你的债务已超过2000元，有人开始向你推销网贷了..."
            });
        }
    }

    /// <summary>破产结局：设置破产标记，弹出对话，触发结局UI</summary>
    public void OnBankruptcyTriggered()
    {
        Debug.Log("[DebtSystem] 触发网贷破产结局！");

        // 1. 设置破产标记，供 EndingDeterminer Layer0 条件匹配
        if (EventHistory.Instance != null)
        {
            EventHistory.Instance.SetFlag("bankruptcy_triggered", true);
        }

        // 2. 弹出对话作为视觉反馈
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue("系统", new string[]
            {
                "你的债务已经超过5000元，再也无力偿还...",
                "催债电话响个不停，你的大学生活走到了尽头。"
            });
        }

        // 3. 触发破产结局
        if (EndingDeterminer.Instance != null)
        {
            EndingResult result = EndingDeterminer.Instance.DetermineEnding();
            if (EndingUI.Instance == null)
            {
                GameObject uiObj = new GameObject("EndingUI");
                uiObj.AddComponent<EndingUI>();
            }
            EndingUI.Instance.Show(result);
        }
    }
}

// ========== 债务系统 ==========

/// <summary>
/// 债务系统 —— 监控玩家金钱余额，根据阈值触发不同等级的债务事件和惩罚
/// </summary>
public class DebtSystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static DebtSystem Instance { get; private set; }

    // ========== 债务等级枚举 ==========

    /// <summary>
    /// 债务等级 —— 数值越大情况越严重
    /// </summary>
    public enum DebtLevel
    {
        /// <summary>正常：余额 >= 200</summary>
        Normal,
        /// <summary>食物受限：余额 &lt; 200</summary>
        FoodRestricted,
        /// <summary>透支：余额 &lt; 0</summary>
        Overdrafted,
        /// <summary>网贷触发：余额 &lt; -2000</summary>
        LoanTrigger,
        /// <summary>破产：余额 &lt; -5000</summary>
        Bankruptcy
    }

    // ========== 事件 ==========

    /// <summary>债务等级发生变化时触发，参数为新的债务等级</summary>
    public event Action<DebtLevel> OnDebtLevelChanged;

    // ========== 属性 ==========

    /// <summary>当前债务等级</summary>
    public DebtLevel CurrentDebtLevel { get; private set; } = DebtLevel.Normal;

    /// <summary>食物是否受限（余额低于200）</summary>
    public bool IsFoodRestricted => CurrentDebtLevel >= DebtLevel.FoodRestricted;

    /// <summary>是否处于透支状态（余额低于0）</summary>
    public bool IsOverdrafted => CurrentDebtLevel >= DebtLevel.Overdrafted;

    // ========== 内部字段 ==========

    /// <summary>事件触发器实例</summary>
    private IDebtEventTrigger eventTrigger;

    /// <summary>上一次记录的债务等级，用于检测变化</summary>
    private DebtLevel lastLevel = DebtLevel.Normal;

    // ========== 生命周期 ==========

    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化默认事件触发器
        eventTrigger = new DefaultDebtEventTrigger();
    }

    private void Start()
    {
        // 订阅 GameState 金钱变化事件，自动检查债务状态
        if (GameState.Instance != null)
        {
            GameState.Instance.OnMoneyChanged += CheckDebtStatus;
        }
        else
        {
            Debug.LogWarning("[DebtSystem] GameState.Instance 尚未就绪，无法订阅 OnMoneyChanged");
        }
    }

    private void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        if (GameState.Instance != null)
        {
            GameState.Instance.OnMoneyChanged -= CheckDebtStatus;
        }
    }

    // ========== 公共方法 ==========

    /// <summary>
    /// 根据当前余额检查债务状态，当等级恶化时触发对应事件
    /// </summary>
    /// <param name="balance">当前金钱余额</param>
    public void CheckDebtStatus(int balance)
    {
        DebtLevel newLevel = CalculateDebtLevel(balance);

        // 情况恶化：触发对应的事件回调
        if (newLevel > lastLevel)
        {
            TriggerDebtEvents(lastLevel, newLevel);
        }

        // 等级发生任何变化（恶化或改善）都触发事件通知
        if (newLevel != lastLevel)
        {
            CurrentDebtLevel = newLevel;
            lastLevel = newLevel;
            OnDebtLevelChanged?.Invoke(newLevel);
        }
    }

    /// <summary>
    /// 每回合债务惩罚 —— 透支状态下每回合增加10点压力
    /// </summary>
    public void ProcessRoundDebtPenalty()
    {
        if (IsOverdrafted)
        {
            if (PlayerAttributes.Instance != null)
            {
                PlayerAttributes.Instance.Stress += 10;
                Debug.Log("[DebtSystem] 透支惩罚：压力 +10");
            }
        }
    }

    /// <summary>
    /// 替换事件触发器实现
    /// </summary>
    /// <param name="trigger">新的事件触发器，传 null 则恢复为默认触发器</param>
    public void SetEventTrigger(IDebtEventTrigger trigger)
    {
        eventTrigger = trigger ?? new DefaultDebtEventTrigger();
    }

    // ========== 内部方法 ==========

    /// <summary>
    /// 纯函数：根据余额计算对应的债务等级
    /// </summary>
    /// <param name="balance">当前金钱余额</param>
    /// <returns>对应的债务等级</returns>
    private DebtLevel CalculateDebtLevel(int balance)
    {
        if (balance < DebtThresholds.Bankruptcy)
            return DebtLevel.Bankruptcy;
        if (balance < DebtThresholds.LoanEvent)
            return DebtLevel.LoanTrigger;
        if (balance < DebtThresholds.Overdraft)
            return DebtLevel.Overdrafted;
        if (balance < DebtThresholds.FoodRestriction)
            return DebtLevel.FoodRestricted;
        return DebtLevel.Normal;
    }

    /// <summary>
    /// 按从低到高的顺序触发从 oldLevel 到 newLevel 之间所有未触发的事件
    /// </summary>
    /// <param name="oldLevel">旧债务等级</param>
    /// <param name="newLevel">新债务等级（更严重）</param>
    private void TriggerDebtEvents(DebtLevel oldLevel, DebtLevel newLevel)
    {
        // 按等级从低到高依次触发，确保不遗漏中间等级的事件
        if (oldLevel < DebtLevel.FoodRestricted && newLevel >= DebtLevel.FoodRestricted)
        {
            eventTrigger.OnFoodRestricted();
        }

        if (oldLevel < DebtLevel.Overdrafted && newLevel >= DebtLevel.Overdrafted)
        {
            eventTrigger.OnOverdraftStarted();
        }

        if (oldLevel < DebtLevel.LoanTrigger && newLevel >= DebtLevel.LoanTrigger)
        {
            eventTrigger.OnLoanEventTriggered();
        }

        if (oldLevel < DebtLevel.Bankruptcy && newLevel >= DebtLevel.Bankruptcy)
        {
            // 破产立即触发，不等回合结算
            eventTrigger.OnBankruptcyTriggered();
        }
    }
}
