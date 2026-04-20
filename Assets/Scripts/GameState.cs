using UnityEngine;
using System;

/// <summary>
/// 游戏状态数据类 —— 管理学年、学期、回合、月份、金钱、行动点等核心状态
/// </summary>
public class GameState : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========
    public static GameState Instance { get; private set; }

    // ========== 事件 ==========
    /// <summary>任何状态字段发生变化时触发</summary>
    public event Action OnStateChanged;

    /// <summary>金钱变化时触发，参数为新余额</summary>
    public event Action<int> OnMoneyChanged;

    /// <summary>当前地点变化时触发，参数为新地点</summary>
    public event Action<LocationId> OnLocationChanged;

    // ========== 常量 ==========
    /// <summary>每回合默认行动点（设计文档：基础上限20点）</summary>
    public const int DefaultActionPoints = 20;
    /// <summary>每学期回合数上限（设计文档：每学期5回合，共40回合）</summary>
    public const int MaxRoundsPerSemester = 5;

    // ========== 内部字段 ==========
    [Header("学业进度")]
    [SerializeField] private int currentYear = 1;       // 1-4
    [SerializeField] private int currentSemester = 1;    // 1=上, 2=下
    [SerializeField] private int currentRound = 1;       // 1-40
    [SerializeField] private int currentMonth = 9;       // 1-12

    [Header("资源")]
    [SerializeField] private int money = 8000;
    [SerializeField] private int actionPoints = DefaultActionPoints;

    [Header("地点")]
    [SerializeField] private LocationId currentLocation = LocationId.Dormitory;

    [Header("职务扣减")]
    [SerializeField] private int positionAPCost = 0;  // 所有职务的总行动点扣减

    [Header("玩家身份")]
    [SerializeField] private string playerName = "学生";
    [SerializeField] private int playerGender = 0;   // 0=男, 1=女
    [SerializeField] private string playerMajor = "生物科学";

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

    /// <summary>金钱余额（允许负数，支持债务机制）</summary>
    public int Money
    {
        get => money;
        set { money = value; NotifyChanged(); OnMoneyChanged?.Invoke(money); }
    }

    /// <summary>剩余行动点</summary>
    public int ActionPoints
    {
        get => actionPoints;
        set { actionPoints = Mathf.Clamp(value, 0, EffectiveMaxActionPoints); NotifyChanged(); }
    }

    /// <summary>所有职务的总行动点扣减值</summary>
    public int PositionAPCost
    {
        get => positionAPCost;
        set { positionAPCost = Mathf.Max(0, value); NotifyChanged(); }
    }

    /// <summary>当前所在地点</summary>
    public LocationId CurrentLocation
    {
        get => currentLocation;
        set { currentLocation = value; NotifyChanged(); OnLocationChanged?.Invoke(value); }
    }

    /// <summary>玩家姓名</summary>
    public string PlayerName
    {
        get => playerName;
        set { playerName = value; NotifyChanged(); }
    }

    /// <summary>玩家性别：0=男，1=女</summary>
    public int PlayerGender
    {
        get => playerGender;
        set { playerGender = value; NotifyChanged(); }
    }

    /// <summary>玩家专业</summary>
    public string PlayerMajor
    {
        get => playerMajor;
        set { playerMajor = value; NotifyChanged(); }
    }

    /// <summary>每回合实际可用最大行动点（基础 - 职务扣减）</summary>
    public int EffectiveMaxActionPoints => Mathf.Max(1, DefaultActionPoints - positionAPCost);

    // ========== 回合推进结果 ==========

    /// <summary>回合推进后的结果类型</summary>
    public enum RoundAdvanceResult
    {
        NextRound,      // 普通下一回合
        NextSemester,   // 进入下学期
        NextYear,       // 进入下学年
        Graduated       // 毕业（大四下学期结束）
    }

    // ========== 回合推进 ==========

    /// <summary>
    /// 推进到下一回合，自动处理学期/学年切换和月份更新。
    /// 返回推进结果类型。
    /// </summary>
    public RoundAdvanceResult AdvanceRound()
    {
        RoundAdvanceResult result;

        if (currentRound < MaxRoundsPerSemester)
        {
            // 普通推进：回合 +1
            currentRound++;
            result = RoundAdvanceResult.NextRound;
        }
        else if (currentSemester == 1)
        {
            // 上学期结束 → 进入下学期
            currentSemester = 2;
            currentRound = 1;
            result = RoundAdvanceResult.NextSemester;
        }
        else if (currentYear < 4)
        {
            // 下学期结束 → 进入下一学年
            currentYear++;
            currentSemester = 1;
            currentRound = 1;
            result = RoundAdvanceResult.NextYear;
        }
        else
        {
            // 大四下学期结束 → 毕业
            result = RoundAdvanceResult.Graduated;
        }

        // 更新月份
        currentMonth = CalculateMonth(currentSemester, currentRound);

        // 重置行动点（考虑职务扣减）
        actionPoints = EffectiveMaxActionPoints;

        // 通知变化
        NotifyChanged();

        return result;
    }

    /// <summary>
    /// 根据学期和回合计算对应月份。
    /// 每学期5回合，按设计文档映射：
    /// 上学期: 回合1→9月, 回合2→10月, 回合3→11月, 回合4→12月, 回合5→1月
    /// 下学期: 回合1→2月, 回合2→3月, 回合3→4月, 回合4→5月, 回合5→6月
    /// </summary>
    public static int CalculateMonth(int semester, int round)
    {
        int baseMonth = (semester == 1) ? 9 : 2; // 上学期从9月，下学期从2月

        int monthOffset = Mathf.Clamp(round - 1, 0, 4); // 0~4

        int month = baseMonth + monthOffset;
        if (month > 12) month -= 12; // 处理跨年（12月→1月）
        return month;
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

    /// <summary>重置本回合行动点（考虑职务扣减）</summary>
    public void ResetActionPoints()
    {
        ActionPoints = EffectiveMaxActionPoints;
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

    // ========== ISaveable 实现 ==========

    /// <summary>将游戏状态写入存档数据</summary>
    public void SaveToData(SaveData data)
    {
        data.currentYear = currentYear;
        data.currentSemester = currentSemester;
        data.currentRound = currentRound;
        data.currentMonth = currentMonth;
        data.money = money;
        data.actionPoints = actionPoints;
        data.currentLocation = currentLocation.ToString();
        data.playerName = playerName;
        data.playerGender = playerGender;
        data.playerMajor = playerMajor;
    }

    /// <summary>从存档数据恢复游戏状态</summary>
    public void LoadFromData(SaveData data)
    {
        currentYear = data.currentYear;
        currentSemester = data.currentSemester;
        currentRound = data.currentRound;
        currentMonth = data.currentMonth;
        money = data.money;
        actionPoints = data.actionPoints;

        if (!string.IsNullOrEmpty(data.playerName))
            playerName = data.playerName;
        playerGender = data.playerGender;
        if (!string.IsNullOrEmpty(data.playerMajor))
            playerMajor = data.playerMajor;

        // 恢复地点
        if (!string.IsNullOrEmpty(data.currentLocation) &&
            System.Enum.TryParse(data.currentLocation, true, out LocationId loc))
        {
            currentLocation = loc;
        }

        NotifyChanged();
    }

    /// <summary>批量设置状态（供调试工具使用）</summary>
    public void SetState(int year, int semester, int round, int month, int money, int ap)
    {
        currentYear = Mathf.Clamp(year, 1, 4);
        currentSemester = Mathf.Clamp(semester, 1, 2);
        currentRound = Mathf.Clamp(round, 1, MaxRoundsPerSemester);
        currentMonth = Mathf.Clamp(month, 1, 12);
        this.money = money;
        actionPoints = Mathf.Clamp(ap, 0, EffectiveMaxActionPoints);
        NotifyChanged();
    }
}
