using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 交易记录数据类 —— 记录每一笔收入/支出的详细信息
/// </summary>
[System.Serializable]
public class TransactionRecord
{
    /// <summary>交易类型枚举</summary>
    public enum TransactionType
    {
        // 收入
        LivingExpense,    // 生活费
        PositionSalary,   // 职务工资
        PartTimeJob,      // 兼职收入
        Scholarship,      // 奖学金
        CompetitionPrize, // 竞赛奖金
        OtherIncome,      // 其他收入
        // 支出
        Tuition,          // 学费
        Food,             // 伙食
        DailyNecessities, // 日用品
        Clothing,         // 服装
        Entertainment,    // 娱乐
        StudyMaterial,    // 学习资料
        SocialExpense,    // 社交费用
        DatingExpense,    // 恋爱开支
        Electronics,      // 电子产品
        Transportation,   // 交通
        Medical,          // 医疗
        Counseling,       // 心理咨询
        OtherExpense      // 其他支出
    }

    /// <summary>交易类型</summary>
    public TransactionType type;
    /// <summary>交易金额（正数=收入, 负数=支出）</summary>
    public int amount;
    /// <summary>发生时的回合</summary>
    public int round;
    /// <summary>发生时的学期</summary>
    public int semester;
    /// <summary>发生时的学年</summary>
    public int year;
    /// <summary>交易描述</summary>
    public string description;

    /// <summary>JSON反序列化需要的无参构造函数</summary>
    public TransactionRecord() { }

    /// <summary>
    /// 构造交易记录
    /// </summary>
    /// <param name="type">交易类型</param>
    /// <param name="amount">金额（正数=收入, 负数=支出）</param>
    /// <param name="round">回合</param>
    /// <param name="semester">学期</param>
    /// <param name="year">学年</param>
    /// <param name="description">描述</param>
    public TransactionRecord(TransactionType type, int amount, int round, int semester, int year, string description)
    {
        this.type = type;
        this.amount = amount;
        this.round = round;
        this.semester = semester;
        this.year = year;
        this.description = description;
    }
}

/// <summary>
/// 经济管理器 —— 管理游戏内所有金钱收支、交易流水记录
/// </summary>
public class EconomyManager : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========

    public static EconomyManager Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>每笔交易记录后触发</summary>
    public event Action<TransactionRecord> OnTransactionLogged;

    /// <summary>余额变化时触发，参数为 (旧余额, 新余额)</summary>
    public event Action<int, int> OnBalanceChanged;

    // ========== 常量 ==========

    /// <summary>每回合生活费</summary>
    private const int LivingExpensePerRound = 1500;

    /// <summary>班委每回合工资（干事/部长）</summary>
    private const int PositionSalaryPerRound = 200;

    /// <summary>高级职务每回合工资（副主席/主席）</summary>
    private const int HighPositionSalaryPerRound = 500;

    /// <summary>每学期学费</summary>
    private const int TuitionPerSemester = 5000;

    // ========== 内部字段 ==========

    /// <summary>交易流水记录</summary>
    private List<TransactionRecord> transactionLog = new List<TransactionRecord>();

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
    }

    // ========== 公共方法 ==========

    /// <summary>
    /// 增加金钱（收入），记录交易流水
    /// </summary>
    /// <param name="amount">收入金额（正数）</param>
    /// <param name="type">交易类型</param>
    /// <param name="description">交易描述</param>
    public void Earn(int amount, TransactionRecord.TransactionType type, string description)
    {
        int oldBalance = GameState.Instance.Money;

        GameState.Instance.AddMoney(amount);

        int newBalance = GameState.Instance.Money;

        LogTransaction(type, amount, description);

        OnBalanceChanged?.Invoke(oldBalance, newBalance);
    }

    /// <summary>
    /// 扣除金钱（支出），即使余额不足也执行（进入透支），记录交易流水。
    /// 始终返回 true。
    /// </summary>
    /// <param name="amount">支出金额（正数，内部取反记录）</param>
    /// <param name="type">交易类型</param>
    /// <param name="description">交易描述</param>
    /// <returns>始终返回 true</returns>
    public bool Spend(int amount, TransactionRecord.TransactionType type, string description)
    {
        int oldBalance = GameState.Instance.Money;

        GameState.Instance.AddMoney(-amount);

        int newBalance = GameState.Instance.Money;

        LogTransaction(type, -amount, description);

        OnBalanceChanged?.Invoke(oldBalance, newBalance);

        return true;
    }

    /// <summary>
    /// 检查当前余额是否足够支付指定金额
    /// </summary>
    /// <param name="amount">需要的金额</param>
    /// <returns>余额 >= amount 时返回 true</returns>
    public bool CanAfford(int amount)
    {
        return GameState.Instance.Money >= amount;
    }

    /// <summary>
    /// 获取当前余额（来自 GameState）
    /// </summary>
    /// <returns>当前金钱余额</returns>
    public int GetBalance()
    {
        return GameState.Instance.Money;
    }

    /// <summary>
    /// 获取全部交易流水记录
    /// </summary>
    /// <returns>完整交易记录列表</returns>
    public List<TransactionRecord> GetTransactionLog()
    {
        return new List<TransactionRecord>(transactionLog);
    }

    /// <summary>
    /// 获取最近的交易记录
    /// </summary>
    /// <param name="count">需要获取的条数</param>
    /// <returns>最近 count 条交易记录</returns>
    public List<TransactionRecord> GetRecentTransactions(int count)
    {
        int startIndex = Mathf.Max(0, transactionLog.Count - count);
        int actualCount = Mathf.Min(count, transactionLog.Count);
        return transactionLog.GetRange(startIndex, actualCount);
    }

    // ========== 结算方法 ==========

    /// <summary>
    /// 每回合收入结算：发放生活费，以及班委工资（若满足条件）
    /// </summary>
    public void ProcessRoundIncome()
    {
        // 生活费
        Earn(LivingExpensePerRound, TransactionRecord.TransactionType.LivingExpense, "每回合生活费");

        // 学生会职务工资：根据职务等级发放不同金额
        if (ClubSystem.Instance != null)
        {
            var membership = ClubSystem.Instance.GetMembership("student_union");
            if (membership != null)
            {
                var rank = ClubSystem.Instance.GetCurrentRank("student_union");
                if (rank != null)
                {
                    string title = rank.title;
                    if (rank.rank >= 2)
                    {
                        // 副主席/主席：500/回合
                        Earn(HighPositionSalaryPerRound, TransactionRecord.TransactionType.PositionSalary,
                            $"学生会{title}工资");
                    }
                    else
                    {
                        // 干事/部长：200/回合
                        Earn(PositionSalaryPerRound, TransactionRecord.TransactionType.PositionSalary,
                            $"学生会{title}工资");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 学期开始结算：扣除学费
    /// </summary>
    public void ProcessSemesterStart()
    {
        Spend(TuitionPerSemester, TransactionRecord.TransactionType.Tuition, "学期学费");
    }

    // ========== 内部方法 ==========

    /// <summary>
    /// 记录一笔交易到流水日志，并触发交易事件
    /// </summary>
    /// <param name="type">交易类型</param>
    /// <param name="amount">金额（正数=收入, 负数=支出）</param>
    /// <param name="description">描述</param>
    private void LogTransaction(TransactionRecord.TransactionType type, int amount, string description)
    {
        TransactionRecord record = new TransactionRecord(
            type,
            amount,
            GameState.Instance.CurrentRound,
            GameState.Instance.CurrentSemester,
            GameState.Instance.CurrentYear,
            description
        );

        transactionLog.Add(record);

        OnTransactionLogged?.Invoke(record);
    }

    // ========== ISaveable 实现 ==========

    /// <summary>将交易日志写入存档数据</summary>
    public void SaveToData(SaveData data)
    {
        data.transactionRecords = new List<TransactionRecord>(transactionLog);
    }

    /// <summary>从存档数据恢复交易日志</summary>
    public void LoadFromData(SaveData data)
    {
        if (data.transactionRecords != null)
            transactionLog = new List<TransactionRecord>(data.transactionRecords);
        else
            transactionLog = new List<TransactionRecord>();
    }
}
