using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 副业/兼职系统数据与管理 (对应策划文档 4.5.7 倒卖副业 和 4.6 实习系统)
/// 实习、副业均视为在特定条件解锁的行动，通过 ActionSystem 管理或独立界面调用
/// </summary>
public class JobSystem : MonoBehaviour, ISaveable
{
    public static JobSystem Instance { get; private set; }

    [Header("实习状态")]
    public int totalInternshipCount = 0;
    public string currentInternshipId = "";
    public int consecutiveInternRounds = 0;

    [Header("副业状态")]
    public string currentSideHustleId = "";
    public int consecutiveHustleRounds = 0;

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
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
        }
    }

    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        if (result == GameState.RoundAdvanceResult.Graduated) return;

        // 处理副业规模化效果（连续3回合以上，黑暗值每回合额外+2）
        if (!string.IsNullOrEmpty(currentSideHustleId))
        {
            consecutiveHustleRounds++;
            if (consecutiveHustleRounds >= 3)
            {
                if (PlayerAttributes.Instance != null)
                {
                    PlayerAttributes.Instance.Darkness += 2;
                }
            }
        }
        else
        {
            consecutiveHustleRounds = 0;
        }

        // 处理实习回合计数
        if (!string.IsNullOrEmpty(currentInternshipId))
        {
            consecutiveInternRounds++;
            totalInternshipCount++;
        }
        else
        {
            consecutiveInternRounds = 0;
        }

        // 实习状态和副业状态都是按行动计次的，回合结束自动清空，要求下回合重新主动执行
        currentSideHustleId = "";
        currentInternshipId = "";
    }

    /// <summary>检查实习系统是否解锁</summary>
    public bool IsInternshipUnlocked()
    {
        if (GameState.Instance == null) return false;

        // 大二下学期（第4学期，即 Year 2, Semester 2）或更晚
        bool isTimeReached = GameState.Instance.CurrentYear > 2 ||
                            (GameState.Instance.CurrentYear == 2 && GameState.Instance.CurrentSemester == 2);

        if (!isTimeReached) return false;

        // 条件：通过四级 或 GPA >= 2.5
        bool passedCET4 = false;
        float gpa = 0;

        if (ExamSystem.Instance != null)
        {
            passedCET4 = ExamSystem.Instance.IsCET4Passed;
            gpa = ExamSystem.Instance.GetCumulativeGPA();
        }

        return passedCET4 || gpa >= 2.5f;
    }

    /// <summary>检查倒卖副业是否解锁</summary>
    public bool IsSideHustleUnlocked()
    {
        if (GameState.Instance == null || PlayerAttributes.Instance == null) return false;

        // 大二起
        if (GameState.Instance.CurrentYear < 2) return false;

        int darkness = PlayerAttributes.Instance.Darkness;
        int friendCount = 0;

        if (AffinitySystem.Instance != null)
        {
            friendCount = AffinitySystem.Instance.GetFriendCount();
        }

        return darkness >= 15 || friendCount >= 3;
    }

    // ==========================================
    // 执行逻辑由 EventSystem 或特定的 Action 菜单调用
    // ==========================================

    public void RecordInternshipExecution(string internId)
    {
        currentInternshipId = internId;
    }

    public void RecordSideHustleExecution(string hustleId)
    {
        currentSideHustleId = hustleId;
    }

    public void SaveToData(SaveData data)
    {
        data.eventFlags.RemoveAll(f => f.key.StartsWith("job_"));
        data.eventFlags.Add(new StringBoolPair($"job_intern_count_{totalInternshipCount}", true));
    }

    public void LoadFromData(SaveData data)
    {
        totalInternshipCount = 0;
        currentInternshipId = "";
        currentSideHustleId = "";
        consecutiveHustleRounds = 0;
        consecutiveInternRounds = 0;

        foreach (var flag in data.eventFlags)
        {
            if (flag.key.StartsWith("job_intern_count_"))
            {
                if (int.TryParse(flag.key.Substring("job_intern_count_".Length), out int v))
                {
                    totalInternshipCount = v;
                }
            }
        }
    }
}
