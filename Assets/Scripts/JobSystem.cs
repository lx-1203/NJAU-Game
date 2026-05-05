using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JobRequirementData
{
    public string type;
    public string target;
    public int min;
}

[Serializable]
public class JobEffectData
{
    public string type;
    public string target;
    public int value;
}

[Serializable]
public class JobDefinitionData
{
    public string id;
    public string name;
    public string description;
    public List<JobRequirementData> requirements = new List<JobRequirementData>();
    public int baseIncome;
    public int apCost;
    public List<JobEffectData> effects = new List<JobEffectData>();
    public int unlockSemester;
    public bool isInternship;
}

[Serializable]
public class JobDatabaseData
{
    public List<JobDefinitionData> internships = new List<JobDefinitionData>();
    public List<JobDefinitionData> sideHustles = new List<JobDefinitionData>();
}

/// <summary>
/// 副业/实习系统：负责数据加载、解锁判断、执行逻辑与存档状态。
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

    private readonly List<JobDefinitionData> internships = new List<JobDefinitionData>();
    private readonly List<JobDefinitionData> sideHustles = new List<JobDefinitionData>();
    private readonly Dictionary<string, JobDefinitionData> jobsById = new Dictionary<string, JobDefinitionData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadJobDatabase();
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

    private void LoadJobDatabase()
    {
        internships.Clear();
        sideHustles.Clear();
        jobsById.Clear();

        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/jobs");
        if (jsonAsset == null)
        {
            Debug.LogWarning("[JobSystem] 未找到 Resources/Data/jobs.json，工作系统将使用空数据。");
            ShowJobNotification("工作数据缺失", "兼职与实习数据没有加载成功，本局的工作系统会暂时保持为空。");
            return;
        }

        try
        {
            JobDatabaseData database = JsonUtility.FromJson<JobDatabaseData>(jsonAsset.text);
            if (database == null)
            {
                Debug.LogWarning("[JobSystem] jobs.json 解析为空。");
                ShowJobNotification("工作数据为空", "jobs.json 已读取，但没有解析出任何工作条目。");
                return;
            }

            RegisterJobs(database.internships, true, internships);
            RegisterJobs(database.sideHustles, false, sideHustles);
            Debug.Log($"[JobSystem] 已加载 {internships.Count} 个实习和 {sideHustles.Count} 个副业条目。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[JobSystem] jobs.json 解析失败: {e.Message}");
            ShowJobNotification("工作数据损坏", "兼职与实习数据解析失败，这一局无法正常开启工作系统。");
        }
    }

    private void RegisterJobs(List<JobDefinitionData> source, bool isInternship, List<JobDefinitionData> target)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            JobDefinitionData job = source[i];
            if (job == null || string.IsNullOrWhiteSpace(job.id))
            {
                continue;
            }

            job.requirements ??= new List<JobRequirementData>();
            job.effects ??= new List<JobEffectData>();
            job.isInternship = isInternship;
            target.Add(job);
            jobsById[job.id] = job;
        }
    }

    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        if (result == GameState.RoundAdvanceResult.Graduated)
        {
            return;
        }

        if (!string.IsNullOrEmpty(currentSideHustleId))
        {
            consecutiveHustleRounds++;
            if (consecutiveHustleRounds >= 3 && PlayerAttributes.Instance != null)
            {
                PlayerAttributes.Instance.Darkness += 2;
            }
        }
        else
        {
            consecutiveHustleRounds = 0;
        }

        if (!string.IsNullOrEmpty(currentInternshipId))
        {
            consecutiveInternRounds++;
            totalInternshipCount++;
        }
        else
        {
            consecutiveInternRounds = 0;
        }

        currentSideHustleId = "";
        currentInternshipId = "";
    }

    public List<JobDefinitionData> GetJobs(bool internship)
    {
        return internship
            ? new List<JobDefinitionData>(internships)
            : new List<JobDefinitionData>(sideHustles);
    }

    public JobDefinitionData GetJob(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        jobsById.TryGetValue(id, out JobDefinitionData job);
        return job;
    }

    public bool IsInternshipUnlocked()
    {
        if (GameState.Instance == null)
        {
            return false;
        }

        bool isTimeReached = GameState.Instance.CurrentYear > 2 ||
            (GameState.Instance.CurrentYear == 2 && GameState.Instance.CurrentSemester == 2);
        if (!isTimeReached)
        {
            return false;
        }

        bool passedCET4 = false;
        float gpa = 0f;
        if (ExamSystem.Instance != null)
        {
            passedCET4 = ExamSystem.Instance.IsCET4Passed;
            gpa = ExamSystem.Instance.GetCumulativeGPA();
        }

        return passedCET4 || gpa >= 2.5f;
    }

    private void ShowJobNotification(string title, string message, float duration = 3f)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, new Color(0.82f, 0.38f, 0.30f), duration);
        }
    }

    public bool IsSideHustleUnlocked()
    {
        if (GameState.Instance == null || PlayerAttributes.Instance == null)
        {
            return false;
        }

        if (GameState.Instance.CurrentYear < 2)
        {
            return false;
        }

        int darkness = PlayerAttributes.Instance.Darkness;
        int friendCount = AffinitySystem.Instance != null ? AffinitySystem.Instance.GetFriendOrAboveCount() : 0;
        return darkness >= 15 || friendCount >= 3;
    }

    public bool CanExecuteJob(JobDefinitionData job, out string failReason)
    {
        failReason = string.Empty;

        if (job == null)
        {
            failReason = "岗位无效";
            return false;
        }

        if (job.isInternship)
        {
            if (!IsInternshipUnlocked())
            {
                failReason = "实习未解锁";
                return false;
            }
        }
        else if (!IsSideHustleUnlocked())
        {
            failReason = "副业未解锁";
            return false;
        }

        if (GameState.Instance == null || PlayerAttributes.Instance == null)
        {
            failReason = "核心系统未初始化";
            return false;
        }

        if (job.apCost > GameState.Instance.ActionPoints)
        {
            failReason = "行动点不足";
            return false;
        }

        if (job.isInternship && job.unlockSemester > 0 && GetOverallSemesterIndex() < job.unlockSemester)
        {
            failReason = $"第{job.unlockSemester}学期后开放";
            return false;
        }

        for (int i = 0; i < job.requirements.Count; i++)
        {
            if (!EvaluateRequirement(job.requirements[i], out failReason))
            {
                return false;
            }
        }

        return true;
    }

    public bool ExecuteJob(JobDefinitionData job, out string failReason)
    {
        if (!CanExecuteJob(job, out failReason))
        {
            return false;
        }

        if (!GameState.Instance.ConsumeActionPoint(job.apCost))
        {
            failReason = "行动点扣除失败";
            return false;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.Earn(job.baseIncome, TransactionRecord.TransactionType.PartTimeJob, $"{job.name}收入");
        }
        else
        {
            GameState.Instance.AddMoney(job.baseIncome);
        }

        for (int i = 0; i < job.effects.Count; i++)
        {
            ApplyEffect(job.effects[i]);
        }

        if (job.isInternship)
        {
            RecordInternshipExecution(job.id);
        }
        else
        {
            RecordSideHustleExecution(job.id);
        }

        RecordJobProgress(job);

        failReason = string.Empty;
        return true;
    }

    public void RecordInternshipExecution(string internId)
    {
        currentInternshipId = internId;
    }

    public void RecordSideHustleExecution(string hustleId)
    {
        currentSideHustleId = hustleId;
    }

    private bool EvaluateRequirement(JobRequirementData requirement, out string failReason)
    {
        failReason = string.Empty;
        if (requirement == null)
        {
            return true;
        }

        string type = string.IsNullOrWhiteSpace(requirement.type) ? "attribute" : requirement.type.Trim().ToLowerInvariant();
        switch (type)
        {
            case "attribute":
                int value = GetAttributeValue(requirement.target);
                if (value >= requirement.min)
                {
                    return true;
                }
                failReason = $"{GetRequirementDisplayName(requirement.target)}不足";
                return false;

            case "semester":
                if (GetOverallSemesterIndex() >= requirement.min)
                {
                    return true;
                }
                failReason = $"需达到第{requirement.min}学期";
                return false;

            case "cet4":
                if (ExamSystem.Instance != null && ExamSystem.Instance.IsCET4Passed)
                {
                    return true;
                }
                failReason = "需先通过四级";
                return false;

            case "gpa":
                float gpa = ExamSystem.Instance != null ? ExamSystem.Instance.GetCumulativeGPA() : 0f;
                if (gpa >= requirement.min / 100f)
                {
                    return true;
                }
                failReason = "GPA不足";
                return false;

            default:
                return true;
        }
    }

    private void ApplyEffect(JobEffectData effect)
    {
        if (effect == null)
        {
            return;
        }

        string type = string.IsNullOrWhiteSpace(effect.type) ? "attribute" : effect.type.Trim().ToLowerInvariant();
        switch (type)
        {
            case "attribute":
                if (PlayerAttributes.Instance != null)
                {
                    PlayerAttributes.Instance.AddAttribute(GetLocalizedAttributeName(effect.target), effect.value);
                }
                break;

            case "money":
                if (effect.value >= 0)
                {
                    if (EconomyManager.Instance != null)
                    {
                        EconomyManager.Instance.Earn(effect.value, TransactionRecord.TransactionType.OtherIncome, "工作额外奖励");
                    }
                    else if (GameState.Instance != null)
                    {
                        GameState.Instance.AddMoney(effect.value);
                    }
                }
                else if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.Spend(-effect.value, TransactionRecord.TransactionType.OtherExpense, "工作额外支出");
                }
                else if (GameState.Instance != null)
                {
                    GameState.Instance.AddMoney(effect.value);
                }
                break;
        }
    }

    private int GetOverallSemesterIndex()
    {
        if (GameState.Instance == null)
        {
            return 0;
        }

        return (GameState.Instance.CurrentYear - 1) * 2 + GameState.Instance.CurrentSemester;
    }

    private int GetAttributeValue(string target)
    {
        if (PlayerAttributes.Instance == null)
        {
            return 0;
        }

        switch (target)
        {
            case "Study": return PlayerAttributes.Instance.Study;
            case "Charm": return PlayerAttributes.Instance.Charm;
            case "Physique": return PlayerAttributes.Instance.Physique;
            case "Leadership": return PlayerAttributes.Instance.Leadership;
            case "Stress": return PlayerAttributes.Instance.Stress;
            case "Mood": return PlayerAttributes.Instance.Mood;
            case "Darkness": return PlayerAttributes.Instance.Darkness;
            case "Guilt": return PlayerAttributes.Instance.Guilt;
            case "Luck": return PlayerAttributes.Instance.Luck;
            default: return 0;
        }
    }

    private string GetRequirementDisplayName(string target)
    {
        return GetLocalizedAttributeName(target);
    }

    private string GetLocalizedAttributeName(string target)
    {
        switch (target)
        {
            case "Study": return "学力";
            case "Charm": return "魅力";
            case "Physique": return "体魄";
            case "Leadership": return "领导力";
            case "Stress": return "压力";
            case "Mood": return "心情";
            case "Darkness": return "黑暗值";
            case "Guilt": return "负罪感";
            case "Luck": return "幸运";
            default: return target;
        }
    }

    private void RecordJobProgress(JobDefinitionData job)
    {
        if (job == null || EventHistory.Instance == null)
        {
            return;
        }

        string categoryEventId = job.isInternship
            ? "job_internship_completed"
            : "job_side_hustle_completed";
        string jobEventId = $"job_{job.id}_completed";

        EventHistory.Instance.RecordEvent(categoryEventId, -1);
        EventHistory.Instance.RecordEvent(jobEventId, -1);
        EventHistory.Instance.SetFlag(jobEventId, true);
    }

    public void SaveToData(SaveData data)
    {
        data.totalInternshipCount = totalInternshipCount;
        data.currentInternshipId = currentInternshipId;
        data.consecutiveInternRounds = consecutiveInternRounds;
        data.currentSideHustleId = currentSideHustleId;
        data.consecutiveHustleRounds = consecutiveHustleRounds;
    }

    public void LoadFromData(SaveData data)
    {
        totalInternshipCount = data.totalInternshipCount;
        currentInternshipId = data.currentInternshipId ?? "";
        consecutiveInternRounds = data.consecutiveInternRounds;
        currentSideHustleId = data.currentSideHustleId ?? "";
        consecutiveHustleRounds = data.consecutiveHustleRounds;

        if (totalInternshipCount > 0)
        {
            return;
        }

        for (int i = 0; i < data.eventFlags.Count; i++)
        {
            StringBoolPair flag = data.eventFlags[i];
            if (flag == null || !flag.key.StartsWith("job_intern_count_"))
            {
                continue;
            }

            if (int.TryParse(flag.key.Substring("job_intern_count_".Length), out int value))
            {
                totalInternshipCount = value;
                break;
            }
        }
    }
}
