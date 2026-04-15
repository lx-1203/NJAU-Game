/// <summary>
/// 恋爱系统数据定义 —— 枚举、记录类
/// </summary>

/// <summary>恋爱状态枚举</summary>
public enum RomanceState
{
    None,           // 无关系
    Crushing,       // 暗恋（好感≥60自动进入）
    Cooldown,       // 冷却中（告白失败/分手后）
    Dating,         // 恋爱中
    BrokenUp,       // 已分手
    Hostile         // 敌对（劈腿等严重事件）
}

/// <summary>恋爱结局等级</summary>
public enum RomanceEndingTier
{
    Single = 1,     // 1★ 单身
    BrokenUp = 2,   // 2★ 分手
    Confused = 3,   // 3★ 迷茫
    Sweet = 4,      // 4★ 甜蜜
    Engaged = 5     // 5★ 订婚
}

/// <summary>分手原因</summary>
public enum BreakupReason
{
    HealthZero,             // 恋爱健康度归零
    ConsecutiveNoInteract,  // 连续未互动
    CheatingDiscovered,     // 劈腿被发现
    PlayerInitiated,        // 玩家主动分手
    SpecialEvent,           // 特殊事件触发
    NPCInitiated            // NPC 主动分手
}

/// <summary>
/// 恋爱记录 —— 存储单个NPC的恋爱状态和相关数据
/// </summary>
[System.Serializable]
public class RomanceRecord
{
    /// <summary>NPC 唯一标识</summary>
    public string npcId;

    /// <summary>当前恋爱状态</summary>
    public RomanceState state = RomanceState.None;

    /// <summary>恋爱健康度 0~100，初始70</summary>
    public int healthScore = 70;

    /// <summary>开始恋爱的全局回合号（-1表示尚未恋爱）</summary>
    public int datingStartRound = -1;

    /// <summary>恋爱持续回合数</summary>
    public int durationRounds = 0;

    /// <summary>恋爱期间心情总和（用于计算均值）</summary>
    public float moodSumDuringDating = 0;

    /// <summary>心情采样次数</summary>
    public int moodSampleCount = 0;

    /// <summary>连续未互动回合数</summary>
    public int consecutiveNoInteract = 0;

    /// <summary>冷却剩余回合数</summary>
    public int cooldownRoundsLeft = 0;

    /// <summary>分手次数</summary>
    public int breakupCount = 0;

    /// <summary>是否已复合过</summary>
    public bool hasReunited = false;

    /// <summary>下次纪念日回合（每8回合一次，-1表示无）</summary>
    public int nextAnniversaryRound = -1;

    /// <summary>本回合是否与恋人互动过</summary>
    public bool interactedThisRound = false;

    // ===== 劈腿检测相关 =====

    /// <summary>是否处于劈腿状态（同时与多人恋爱）</summary>
    public bool isCheating = false;

    /// <summary>劈腿持续回合数（用于递增检测概率）</summary>
    public int cheatingRounds = 0;

    /// <summary>
    /// 计算恋爱期间心情均值
    /// </summary>
    public float AverageMoodDuringDating => moodSampleCount > 0 ? moodSumDuringDating / moodSampleCount : 0f;
}
