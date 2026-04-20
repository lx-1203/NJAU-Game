using UnityEngine;
using System;

/// <summary>
/// 校园跑系统 —— 管理每学期跑步次数追踪、代跑、学期结算
/// 设计：体育社团成员每学期需32次，非社团成员需52次
/// 每次跑步消耗1AP，体魄+1
/// 代跑5-8元，无体魄增益，负罪感+1，黑暗值+1，5%被发现概率
/// </summary>
public class CampusRunSystem : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========
    public static CampusRunSystem Instance { get; private set; }

    // ========== 事件 ==========
    public event Action OnRunDataChanged;

    // ========== 常量 ==========
    private const int RequiredRunsSportsClub = 32;
    private const int RequiredRunsNormal = 52;
    private const int RunAPCost = 1;
    private const int PhysiquePerRun = 1;
    private const int StressReductionPerRun = 3;
    private const int MoodPerRun = 2;
    private const int ProxyRunMinCost = 5;
    private const int ProxyRunMaxCost = 8;
    private const float ProxyCaughtChance = 0.05f;
    private const int ProxyCaughtPenaltyGuilt = 5;
    private const int ProxyCaughtPenaltyStress = 10;

    // ========== 运行时状态 ==========
    private int completedRuns = 0;
    private int proxyRuns = 0;
    private int totalRuns => completedRuns + proxyRuns;

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
        {
            TurnManager.Instance.OnRoundAdvanced += HandleRoundAdvanced;
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= HandleRoundAdvanced;
        }
    }

    // ========== 查询 ==========

    /// <summary>本学期已完成的跑步次数（含代跑）</summary>
    public int TotalRuns => totalRuns;

    /// <summary>本学期亲自跑的次数</summary>
    public int CompletedRuns => completedRuns;

    /// <summary>本学期代跑次数</summary>
    public int ProxyRuns => proxyRuns;

    /// <summary>本学期需要完成的跑步次数</summary>
    public int RequiredRuns
    {
        get
        {
            // 体育社团成员减免：跑协或篮球社
            if (ClubSystem.Instance != null &&
                (ClubSystem.Instance.IsInClub("running_club") || ClubSystem.Instance.IsInClub("basketball_club")))
            {
                return RequiredRunsSportsClub;
            }
            return RequiredRunsNormal;
        }
    }

    /// <summary>本学期剩余需要完成的次数</summary>
    public int RemainingRuns => Mathf.Max(0, RequiredRuns - totalRuns);

    /// <summary>是否可以跑步（AP>=1）</summary>
    public bool CanRun
    {
        get
        {
            if (GameState.Instance == null) return false;
            return GameState.Instance.ActionPoints >= RunAPCost;
        }
    }

    /// <summary>是否可以代跑（金钱>=代跑费用）</summary>
    public bool CanProxyRun
    {
        get
        {
            if (GameState.Instance == null) return false;
            return GameState.Instance.Money >= ProxyRunMinCost;
        }
    }

    /// <summary>当前代跑费用（学期末更贵）</summary>
    public int CurrentProxyCost
    {
        get
        {
            if (GameState.Instance == null) return ProxyRunMinCost;
            // 越接近学期末越贵
            int round = GameState.Instance.CurrentRound;
            int maxRound = GameState.MaxRoundsPerSemester;
            float t = (float)round / maxRound;
            return Mathf.RoundToInt(Mathf.Lerp(ProxyRunMinCost, ProxyRunMaxCost, t));
        }
    }

    // ========== 执行 ==========

    /// <summary>
    /// 亲自跑步：消耗1AP，体魄+1，压力-3，心情+2
    /// </summary>
    public void DoRun()
    {
        if (!CanRun)
        {
            Debug.LogWarning("[CampusRunSystem] 无法跑步：行动点不足");
            return;
        }

        GameState.Instance.ConsumeActionPoint(RunAPCost);
        completedRuns++;

        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.Physique += PhysiquePerRun;
            PlayerAttributes.Instance.Stress -= StressReductionPerRun;
            PlayerAttributes.Instance.Mood += MoodPerRun;
        }

        Debug.Log($"[CampusRunSystem] 跑步完成！已跑{totalRuns}/{RequiredRuns}次");
        OnRunDataChanged?.Invoke();
    }

    /// <summary>
    /// 代跑：花钱代跑，无体魄增益，负罪感+1，黑暗值+1，5%被发现概率
    /// </summary>
    /// <returns>是否被发现</returns>
    public bool DoProxyRun()
    {
        if (!CanProxyRun)
        {
            Debug.LogWarning("[CampusRunSystem] 无法代跑：金钱不足");
            return false;
        }

        int cost = CurrentProxyCost;

        // 扣钱
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.Spend(cost,
                TransactionRecord.TransactionType.OtherExpense,
                "校园跑代跑");
        }
        else
        {
            GameState.Instance.AddMoney(-cost);
        }

        proxyRuns++;

        // 负罪感和黑暗值增加
        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.Guilt += 1;
            PlayerAttributes.Instance.Darkness += 1;
        }

        if (EventHistory.Instance != null)
        {
            EventHistory.Instance.AddDarkness(1);
        }

        // 5%被发现概率
        bool caught = UnityEngine.Random.value < ProxyCaughtChance;

        if (caught)
        {
            Debug.Log("[CampusRunSystem] 代跑被发现！");
            proxyRuns--; // 被发现的代跑不计入有效次数
            if (PlayerAttributes.Instance != null)
            {
                PlayerAttributes.Instance.Guilt += ProxyCaughtPenaltyGuilt;
                PlayerAttributes.Instance.Stress += ProxyCaughtPenaltyStress;
            }
        }
        else
        {
            Debug.Log($"[CampusRunSystem] 代跑成功！已跑{totalRuns}/{RequiredRuns}次，花费¥{cost}");
        }

        OnRunDataChanged?.Invoke();
        return caught;
    }

    // ========== 学期结算 ==========

    /// <summary>
    /// 学期结算：检查是否完成校园跑要求
    /// 返回完成率（0~1），供体测/学期总结使用
    /// </summary>
    public float GetCompletionRate()
    {
        if (RequiredRuns <= 0) return 1f;
        return Mathf.Clamp01((float)totalRuns / RequiredRuns);
    }

    /// <summary>
    /// 学期结算时调用，重置计数
    /// </summary>
    public void ResetForNewSemester()
    {
        Debug.Log($"[CampusRunSystem] 学期结算：完成{totalRuns}/{RequiredRuns}次 (亲跑{completedRuns}+代跑{proxyRuns})，完成率{GetCompletionRate():P0}");
        completedRuns = 0;
        proxyRuns = 0;
        OnRunDataChanged?.Invoke();
    }

    // ========== 回合事件 ==========

    private void HandleRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        if (result == GameState.RoundAdvanceResult.NextSemester ||
            result == GameState.RoundAdvanceResult.NextYear)
        {
            ResetForNewSemester();
        }
    }

    // ========== ISaveable ==========

    public void SaveToData(SaveData data)
    {
        data.campusRunCompleted = completedRuns;
        data.campusRunProxy = proxyRuns;
    }

    public void LoadFromData(SaveData data)
    {
        completedRuns = data.campusRunCompleted;
        proxyRuns = data.campusRunProxy;
        OnRunDataChanged?.Invoke();
    }
}
