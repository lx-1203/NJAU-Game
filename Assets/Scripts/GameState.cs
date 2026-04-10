using UnityEngine;
using System;

/// <summary>
/// 游戏状态数据类 —— 管理学年、学期、回合、月份、金钱、行动点等核心状态
/// </summary>
public class GameState : MonoBehaviour
{
    // ========== 单例 ==========
    public static GameState Instance { get; private set; }

    // ========== 事件 ==========
    /// <summary>任何状态字段发生变化时触发</summary>
    public event Action OnStateChanged;

    // ========== 常量 ==========
    /// <summary>每回合默认行动点</summary>
    public const int DefaultActionPoints = 5;
    /// <summary>每学期回合数上限</summary>
    public const int MaxRoundsPerSemester = 40;

    // ========== 内部字段 ==========
    [Header("学业进度")]
    [SerializeField] private int currentYear = 1;       // 1-4
    [SerializeField] private int currentSemester = 1;    // 1=上, 2=下
    [SerializeField] private int currentRound = 1;       // 1-40
    [SerializeField] private int currentMonth = 9;       // 1-12

    [Header("资源")]
    [SerializeField] private int money = 1488;
    [SerializeField] private int actionPoints = DefaultActionPoints;

    // ========== 属性访问器（写入时自动通知） ==========

    /// <summary>当前学年 1-4</summary>
    public int CurrentYear
    {
        get => currentYear;
        set { currentYear = Mathf.Clamp(value, 1, 4); NotifyChanged(); }
    }

    /// <summary>当前学期 1=上学期 2=下学期</summary>
    public int CurrentSemester
    {
        get => currentSemester;
        set { currentSemester = Mathf.Clamp(value, 1, 2); NotifyChanged(); }
    }

    /// <summary>当前回合 1-40</summary>
    public int CurrentRound
    {
        get => currentRound;
        set { currentRound = Mathf.Clamp(value, 1, MaxRoundsPerSemester); NotifyChanged(); }
    }

    /// <summary>当前月份 1-12</summary>
    public int CurrentMonth
    {
        get => currentMonth;
        set { currentMonth = Mathf.Clamp(value, 1, 12); NotifyChanged(); }
    }

    /// <summary>金钱余额</summary>
    public int Money
    {
        get => money;
        set { money = Mathf.Max(0, value); NotifyChanged(); }
    }

    /// <summary>剩余行动点</summary>
    public int ActionPoints
    {
        get => actionPoints;
        set { actionPoints = Mathf.Clamp(value, 0, DefaultActionPoints); NotifyChanged(); }
    }

    // ========== 便捷方法 ==========

    /// <summary>获取学年的中文描述，如"大一"</summary>
    public string GetYearName()
    {
        switch (currentYear)
        {
            case 1: return "大一";
            case 2: return "大二";
            case 3: return "大三";
            case 4: return "大四";
            default: return "大" + currentYear;
        }
    }

    /// <summary>获取学期中文描述，如"上"或"下"</summary>
    public string GetSemesterName()
    {
        return currentSemester == 1 ? "上" : "下";
    }

    /// <summary>获取完整时间描述，如"大一上 · 回合2 · 10月"</summary>
    public string GetTimeDescription()
    {
        return $"{GetYearName()}{GetSemesterName()} · 回合{currentRound} · {currentMonth}月";
    }

    /// <summary>消耗行动点，返回是否成功</summary>
    public bool ConsumeActionPoint(int amount = 1)
    {
        if (actionPoints >= amount)
        {
            ActionPoints -= amount;
            return true;
        }
        return false;
    }

    /// <summary>重置本回合行动点</summary>
    public void ResetActionPoints()
    {
        ActionPoints = DefaultActionPoints;
    }

    /// <summary>增减金钱</summary>
    public void AddMoney(int amount)
    {
        Money += amount;
    }

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

    private void NotifyChanged()
    {
        OnStateChanged?.Invoke();
    }
}
