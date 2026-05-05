using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 任务系统核心单例
/// 负责任务加载、触发、进度追踪、完成判定
/// </summary>
public class MissionSystem : MonoBehaviour, ISaveable
{
    public static MissionSystem Instance { get; private set; }

    // 事件
    public event Action<MissionDefinition> OnMissionUnlocked;
    public event Action<MissionDefinition> OnMissionAccepted;
    public event Action<MissionDefinition, MissionObjective> OnObjectiveUpdated;
    public event Action<MissionDefinition> OnMissionCompleted;
    public event Action<MissionDefinition> OnMissionFailed;

    private Dictionary<string, MissionDefinition> allMissions = new Dictionary<string, MissionDefinition>();
    private HashSet<string> availableMissionIds = new HashSet<string>();
    private Dictionary<string, MissionRuntimeData> activeMissions = new Dictionary<string, MissionRuntimeData>();
    private HashSet<string> completedMissionIds = new HashSet<string>();
    private HashSet<string> failedMissionIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        LoadMissions();
        SubscribeToEvents();
        RefreshMissionState();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// 加载任务配置
    /// </summary>
    private void LoadMissions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/missions");
        if (jsonFile == null)
        {
            Debug.LogWarning("[MissionSystem] missions.json not found in Resources/Data/");
            ShowMissionNotification("任务系统未就绪", "没有找到任务配置文件，这一轮将先不生成任务。", new Color(0.82f, 0.38f, 0.30f), 3f);
            return;
        }

        try
        {
            MissionListWrapper wrapper = JsonUtility.FromJson<MissionListWrapper>(jsonFile.text);
            foreach (var mission in wrapper.missions)
            {
                allMissions[mission.missionId] = mission;
            }
            Debug.Log($"[MissionSystem] Loaded {allMissions.Count} missions");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MissionSystem] Failed to parse missions.json: {e.Message}");
            ShowMissionNotification("任务加载失败", "任务配置解析失败，这一轮将暂时跳过任务系统。", new Color(0.82f, 0.38f, 0.30f), 3f);
        }
    }

    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    private void SubscribeToEvents()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced += CheckMissionTriggers;
            TurnManager.Instance.OnRoundAdvanced += CheckMissionTimeouts;
        }

        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted += OnActionExecuted;
        }

        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged += OnAffinityChanged;
        }

        if (ClubSystem.Instance != null)
        {
            ClubSystem.Instance.OnClubJoined += OnClubJoined;
        }

        if (ExamSystem.Instance != null)
        {
            ExamSystem.Instance.OnSingleExamFinished += OnExamFinished;
        }

        if (EventHistory.Instance != null)
        {
            EventHistory.Instance.OnEventRecorded += OnEventRecorded;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= CheckMissionTriggers;
            TurnManager.Instance.OnRoundAdvanced -= CheckMissionTimeouts;
        }

        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted -= OnActionExecuted;
        }

        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged -= OnAffinityChanged;
        }

        if (ClubSystem.Instance != null)
        {
            ClubSystem.Instance.OnClubJoined -= OnClubJoined;
        }

        if (ExamSystem.Instance != null)
        {
            ExamSystem.Instance.OnSingleExamFinished -= OnExamFinished;
        }

        if (EventHistory.Instance != null)
        {
            EventHistory.Instance.OnEventRecorded -= OnEventRecorded;
        }
    }

    public void RefreshMissionState()
    {
        CheckMissionTriggers();
        CheckAllObjectives();
    }

    /// <summary>
    /// 检查任务触发条件
    /// </summary>
    private void CheckMissionTriggers(GameState.RoundAdvanceResult result)
    {
        CheckMissionTriggers();
    }

    private void CheckMissionTriggers()
    {
        foreach (var mission in allMissions.Values)
        {
            // 跳过已完成/失败/进行中的任务
            if (completedMissionIds.Contains(mission.missionId) ||
                failedMissionIds.Contains(mission.missionId) ||
                availableMissionIds.Contains(mission.missionId) ||
                activeMissions.ContainsKey(mission.missionId))
            {
                continue;
            }

            // 检查前置任务
            if (mission.prerequisiteMissions != null && mission.prerequisiteMissions.Count > 0)
            {
                bool prerequisitesMet = mission.prerequisiteMissions.All(id => completedMissionIds.Contains(id));
                if (!prerequisitesMet) continue;
            }

            // 检查触发条件
            if (CheckTriggerConditions(mission.triggerConditions))
            {
                UnlockMission(mission);
            }
        }
    }

    /// <summary>
    /// 检查触发条件
    /// </summary>
    private bool CheckTriggerConditions(List<MissionTriggerCondition> conditions)
    {
        if (conditions == null || conditions.Count == 0) return true;

        foreach (var condition in conditions)
        {
            if (!EvaluateCondition(condition))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 评估单个条件
    /// </summary>
    private bool EvaluateCondition(MissionTriggerCondition condition)
    {
        if (condition == null)
        {
            return true;
        }

        if (GameState.Instance == null)
        {
            return false;
        }

        switch (condition.conditionType)
        {
            case "Round":
                return CompareValue(GetOverallRoundIndex(), condition.minValue, condition.comparisonOperator);

            case "Semester":
                return CompareValue(GetOverallSemesterIndex(), condition.minValue, condition.comparisonOperator);

            case "Attribute":
                int attrValue = GetAttributeValue(condition.targetId);
                return CompareValue(attrValue, condition.minValue, condition.comparisonOperator);

            case "Money":
                return CompareValue(GameState.Instance.Money, condition.minValue, condition.comparisonOperator);

            case "NPCAffinity":
                if (AffinitySystem.Instance == null)
                {
                    return false;
                }
                var rel = AffinitySystem.Instance.GetRelationship(condition.targetId);
                int affinity = rel != null ? rel.affinity : 0;
                return CompareValue(affinity, condition.minValue, condition.comparisonOperator);

            case "Event":
                if (EventHistory.Instance == null)
                {
                    return false;
                }
                return EventHistory.Instance.HasTriggered(condition.targetId);

            default:
                Debug.LogWarning($"[MissionSystem] Unknown condition type: {condition.conditionType}");
                return false;
        }
    }

    private bool CompareValue(int actual, int target, string op)
    {
        switch (op)
        {
            case ">=": return actual >= target;
            case "<=": return actual <= target;
            case ">": return actual > target;
            case "<": return actual < target;
            case "==": return actual == target;
            case "!=": return actual != target;
            default: return actual >= target;
        }
    }

    private int GetAttributeValue(string attributeName)
    {
        var attrs = PlayerAttributes.Instance;
        if (attrs == null)
        {
            return 0;
        }

        switch (attributeName)
        {
            case "Study": return attrs.Study;
            case "Charm": return attrs.Charm;
            case "Physique": return attrs.Physique;
            case "Leadership": return attrs.Leadership;
            case "Stress": return attrs.Stress;
            case "Mood": return attrs.Mood;
            case "Darkness": return attrs.Darkness;
            case "Guilt": return attrs.Guilt;
            case "Luck": return attrs.Luck;
            default: return 0;
        }
    }

    /// <summary>
    /// 解锁任务
    /// </summary>
    private void UnlockMission(MissionDefinition mission)
    {
        if (mission == null || availableMissionIds.Contains(mission.missionId) || activeMissions.ContainsKey(mission.missionId))
        {
            return;
        }

        Debug.Log($"[MissionSystem] Mission unlocked: {mission.missionName}");
        availableMissionIds.Add(mission.missionId);
        OnMissionUnlocked?.Invoke(mission);

        if (mission.autoAccept)
        {
            AcceptMission(mission.missionId);
        }
    }

    /// <summary>
    /// 接取任务
    /// </summary>
    public bool AcceptMission(string missionId)
    {
        if (!allMissions.TryGetValue(missionId, out var mission))
        {
            Debug.LogWarning($"[MissionSystem] Mission not found: {missionId}");
            ShowMissionNotification("无法接取任务", "没有找到这条任务数据。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return false;
        }

        if (activeMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"[MissionSystem] Mission already active: {missionId}");
            ShowMissionNotification("任务已在进行中", $"“{mission.missionName}”已经在追踪列表里了。", new Color(0.86f, 0.62f, 0.24f), 2.6f);
            return false;
        }

        if (completedMissionIds.Contains(missionId) || failedMissionIds.Contains(missionId))
        {
            return false;
        }

        if (!mission.autoAccept && !availableMissionIds.Contains(missionId))
        {
            Debug.LogWarning($"[MissionSystem] Mission not unlocked yet: {missionId}");
            ShowMissionNotification("暂时无法接取", $"“{mission.missionName}”还没有正式解锁。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return false;
        }

        var runtimeData = new MissionRuntimeData(mission);
        runtimeData.status = MissionStatus.Active;
        runtimeData.acceptedRound = GetOverallRoundIndex();
        availableMissionIds.Remove(missionId);
        activeMissions[missionId] = runtimeData;

        SyncMissionObjectives(missionId, runtimeData, mission);

        Debug.Log($"[MissionSystem] Mission accepted: {mission.missionName}");
        OnMissionAccepted?.Invoke(mission);
        ShowMissionNotification(
            "任务开始",
            BuildMissionAcceptedSummary(mission),
            new Color(0.28f, 0.72f, 0.86f),
            3f);

        if (runtimeData.objectives.All(o => o.isCompleted))
        {
            CompleteMission(missionId);
        }

        return true;
    }

    /// <summary>
    /// 更新任务目标进度
    /// </summary>
    public void UpdateObjectiveProgress(string missionId, string objectiveId, int increment = 1)
    {
        if (!activeMissions.TryGetValue(missionId, out var runtimeData)) return;
        if (!allMissions.TryGetValue(missionId, out var definition)) return;

        var objective = runtimeData.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
        if (objective == null || objective.isCompleted) return;

        objective.currentValue += increment;
        if (objective.currentValue >= objective.targetValue)
        {
            objective.isCompleted = true;
            Debug.Log($"[MissionSystem] Objective completed: {objective.description}");
        }

        OnObjectiveUpdated?.Invoke(definition, objective);

        // 检查任务是否完成
        if (runtimeData.objectives.All(o => o.isCompleted))
        {
            CompleteMission(missionId);
        }
    }

    /// <summary>
    /// 检查并更新所有任务目标
    /// </summary>
    private void CheckAllObjectives()
    {
        foreach (var kvp in activeMissions.ToList())
        {
            var missionId = kvp.Key;
            var runtimeData = kvp.Value;
            var definition = allMissions[missionId];

            SyncMissionObjectives(missionId, runtimeData, definition);

            // 检查任务是否完成
            if (runtimeData.objectives.All(o => o.isCompleted))
            {
                CompleteMission(missionId);
            }
        }
    }

    private void SyncMissionObjectives(string missionId, MissionRuntimeData runtimeData, MissionDefinition definition)
    {
        if (runtimeData == null || definition == null || runtimeData.objectives == null)
        {
            return;
        }

        foreach (var objective in runtimeData.objectives)
        {
            if (objective == null || objective.isCompleted) continue;

            int currentValue = GetObjectiveCurrentValue(objective);
            bool stateChanged = currentValue != objective.currentValue;

            objective.currentValue = currentValue;
            if (objective.currentValue >= objective.targetValue)
            {
                objective.isCompleted = true;
                stateChanged = true;
            }

            if (stateChanged)
            {
                OnObjectiveUpdated?.Invoke(definition, objective);
            }
        }
    }

    private int GetObjectiveCurrentValue(MissionObjective objective)
    {
        if (objective == null || GameState.Instance == null)
        {
            return 0;
        }

        switch (objective.type)
        {
            case MissionObjectiveType.ReachRound:
                return GetOverallRoundIndex();

            case MissionObjectiveType.ReachSemester:
                return GetOverallSemesterIndex();

            case MissionObjectiveType.AttributeThreshold:
                return GetAttributeValue(objective.targetId);

            case MissionObjectiveType.MoneyThreshold:
                return GameState.Instance.Money;

            case MissionObjectiveType.NPCAffinityThreshold:
                if (AffinitySystem.Instance == null)
                {
                    return 0;
                }
                var r = AffinitySystem.Instance.GetRelationship(objective.targetId);
                return r != null ? r.affinity : 0;

            case MissionObjectiveType.JoinClub:
                if (ClubSystem.Instance == null)
                {
                    return objective.currentValue;
                }

                if (string.IsNullOrEmpty(objective.targetId))
                {
                    return ClubSystem.Instance.GetJoinedClubs().Count;
                }

                return ClubSystem.Instance.GetMembership(objective.targetId) != null ? 1 : 0;

            case MissionObjectiveType.PassExam:
                return GetPassedExamCount(objective.targetId);

            case MissionObjectiveType.CompleteEvent:
                return GetCompletedEventCount(objective.targetId);

            default:
                return objective.currentValue;
        }
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    private void CompleteMission(string missionId)
    {
        if (!activeMissions.TryGetValue(missionId, out var runtimeData)) return;
        if (!allMissions.TryGetValue(missionId, out var definition)) return;

        activeMissions.Remove(missionId);
        completedMissionIds.Add(missionId);

        // 发放奖励
        GrantRewards(definition.rewards);

        Debug.Log($"[MissionSystem] Mission completed: {definition.missionName}");
        OnMissionCompleted?.Invoke(definition);
        ShowMissionNotification(
            "任务完成",
            BuildMissionRewardSummary(definition),
            new Color(0.30f, 0.80f, 0.42f),
            3.4f);
    }

    /// <summary>
    /// 发放奖励
    /// </summary>
    private void GrantRewards(List<MissionReward> rewards)
    {
        if (rewards == null) return;

        foreach (var reward in rewards)
        {
            switch (reward.type)
            {
                case MissionRewardType.Money:
                    if (EconomyManager.Instance != null)
                    {
                        EconomyManager.Instance.Earn(reward.value, TransactionRecord.TransactionType.OtherIncome, "任务奖励");
                    }
                    else if (GameState.Instance != null)
                    {
                        GameState.Instance.AddMoney(reward.value);
                    }
                    break;

                case MissionRewardType.Attribute:
                    ModifyAttribute(reward.targetId, reward.value);
                    break;

                case MissionRewardType.Unlock:
                    if (EventHistory.Instance != null && !string.IsNullOrEmpty(reward.targetId))
                    {
                        EventHistory.Instance.SetFlag(reward.targetId, true);
                    }
                    break;

                case MissionRewardType.Item:
                    if (InventorySystem.Instance != null && !string.IsNullOrEmpty(reward.targetId))
                    {
                        InventorySystem.Instance.AddItem(reward.targetId, Mathf.Max(1, reward.value));
                    }
                    break;
            }
        }
    }

    private void ModifyAttribute(string attributeName, int value)
    {
        var attrs = PlayerAttributes.Instance;
        if (attrs == null)
        {
            return;
        }

        switch (attributeName)
        {
            case "Study": attrs.Study += value; break;
            case "Charm": attrs.Charm += value; break;
            case "Physique": attrs.Physique += value; break;
            case "Leadership": attrs.Leadership += value; break;
            case "Stress": attrs.Stress += value; break;
            case "Mood": attrs.Mood += value; break;
            case "Darkness": attrs.Darkness += value; break;
            case "Guilt": attrs.Guilt += value; break;
            case "Luck": attrs.Luck += value; break;
        }
    }

    /// <summary>
    /// 检查任务超时
    /// </summary>
    private void CheckMissionTimeouts(GameState.RoundAdvanceResult result)
    {
        foreach (var kvp in activeMissions.ToList())
        {
            var missionId = kvp.Key;
            var runtimeData = kvp.Value;
            var definition = allMissions[missionId];

            if (definition.timeLimit > 0)
            {
                int elapsedRounds = GetOverallRoundIndex() - runtimeData.acceptedRound;
                if (elapsedRounds >= definition.timeLimit)
                {
                    FailMission(missionId);
                }
            }
        }

        CheckAllObjectives();
    }

    /// <summary>
    /// 任务失败
    /// </summary>
    private void FailMission(string missionId)
    {
        if (!activeMissions.TryGetValue(missionId, out var runtimeData)) return;
        if (!allMissions.TryGetValue(missionId, out var definition)) return;

        activeMissions.Remove(missionId);
        failedMissionIds.Add(missionId);

        Debug.Log($"[MissionSystem] Mission failed: {definition.missionName}");
        OnMissionFailed?.Invoke(definition);
        ShowMissionNotification(
            "任务失败",
            definition.timeLimit > 0
                ? $"“{definition.missionName}”未能在时限内完成。"
                : $"“{definition.missionName}”已从进行中列表移出。",
            new Color(0.82f, 0.38f, 0.30f),
            3f);
    }

    /// <summary>
    /// 放弃任务
    /// </summary>
    public bool AbandonMission(string missionId)
    {
        if (!activeMissions.TryGetValue(missionId, out var runtimeData)) return false;
        if (!allMissions.TryGetValue(missionId, out var definition)) return false;

        if (!definition.canAbandon)
        {
            Debug.LogWarning($"[MissionSystem] Cannot abandon mission: {definition.missionName}");
            ShowMissionNotification("无法放弃任务", $"“{definition.missionName}”属于不可放弃任务。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            return false;
        }

        FailMission(missionId);
        return true;
    }

    // 事件回调
    private void OnActionExecuted(ActionDefinition action)
    {
        foreach (var kvp in activeMissions.ToList())
        {
            var runtimeData = kvp.Value;
            foreach (var objective in runtimeData.objectives)
            {
                if (objective.type == MissionObjectiveType.ActionCount && objective.targetId == action.id)
                {
                    UpdateObjectiveProgress(kvp.Key, objective.objectiveId, 1);
                }
            }
        }
    }

    private void OnAffinityChanged(string npcId, int oldValue, int newValue, int delta)
    {
        CheckAllObjectives();
    }

    private void OnClubJoined(string clubId)
    {
        foreach (var kvp in activeMissions.ToList())
        {
            var runtimeData = kvp.Value;
            foreach (var objective in runtimeData.objectives)
            {
                if (objective.type == MissionObjectiveType.JoinClub && objective.targetId == clubId)
                {
                    UpdateObjectiveProgress(kvp.Key, objective.objectiveId, 1);
                }
            }
        }
    }

    private void OnExamFinished(ExamResult result)
    {
        if (result.score < 60) return;

        foreach (var kvp in activeMissions.ToList())
        {
            var runtimeData = kvp.Value;
            foreach (var objective in runtimeData.objectives)
            {
                if (objective.type == MissionObjectiveType.PassExam && objective.targetId == result.courseId)
                {
                    UpdateObjectiveProgress(kvp.Key, objective.objectiveId, 1);
                }
            }
        }
    }

    private void OnEventRecorded(string eventId)
    {
        foreach (var kvp in activeMissions.ToList())
        {
            var runtimeData = kvp.Value;
            foreach (var objective in runtimeData.objectives)
            {
                if (objective.type == MissionObjectiveType.CompleteEvent && objective.targetId == eventId)
                {
                    UpdateObjectiveProgress(kvp.Key, objective.objectiveId, 1);
                }
            }
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

    private int GetOverallRoundIndex()
    {
        if (GameState.Instance == null)
        {
            return 0;
        }

        return (GetOverallSemesterIndex() - 1) * GameState.MaxRoundsPerSemester + GameState.Instance.CurrentRound;
    }

    private int GetPassedExamCount(string targetId)
    {
        if (ExamSystem.Instance == null || string.IsNullOrEmpty(targetId))
        {
            return 0;
        }

        switch (targetId)
        {
            case "CERT_CET4":
            case "cet4":
                return ExamSystem.Instance.IsCET4Passed ? 1 : 0;
            case "CERT_CET6":
            case "cet6":
                return ExamSystem.Instance.IsCET6Passed ? 1 : 0;
            case "CERT_COMPUTER":
            case "computer_level":
                return ExamSystem.Instance.IsComputerLevelPassed ? 1 : 0;
        }

        ExamResult[] allResults = ExamSystem.Instance.GetAllResults();
        int passedCount = 0;
        for (int i = 0; i < allResults.Length; i++)
        {
            ExamResult result = allResults[i];
            if (result != null && result.courseId == targetId && result.score >= 60)
            {
                passedCount++;
            }
        }

        return passedCount;
    }

    private int GetCompletedEventCount(string targetId)
    {
        if (EventHistory.Instance == null || string.IsNullOrEmpty(targetId))
        {
            return 0;
        }

        return EventHistory.Instance.GetTriggerCount(targetId);
    }

    private string BuildMissionAcceptedSummary(MissionDefinition mission)
    {
        if (mission == null)
        {
            return "新的目标已经加入追踪列表。";
        }

        string objectiveText = mission.objectives != null && mission.objectives.Count > 0
            ? $"目标数 {mission.objectives.Count}"
            : "任务目标已记录";
        return $"“{mission.missionName}”已加入追踪。\n{objectiveText}，记得随时查看任务面板。";
    }

    private string BuildMissionRewardSummary(MissionDefinition mission)
    {
        if (mission == null)
        {
            return "任务奖励已经发放。";
        }

        List<string> rewardParts = new List<string>();
        if (mission.rewards != null)
        {
            for (int i = 0; i < mission.rewards.Count; i++)
            {
                MissionReward reward = mission.rewards[i];
                if (reward == null)
                {
                    continue;
                }

                switch (reward.type)
                {
                    case MissionRewardType.Money:
                        rewardParts.Add($"金钱+{reward.value}");
                        break;
                    case MissionRewardType.Attribute:
                        rewardParts.Add($"{reward.targetId}+{reward.value}");
                        break;
                    case MissionRewardType.Unlock:
                        rewardParts.Add($"解锁：{reward.targetId}");
                        break;
                    case MissionRewardType.Item:
                        rewardParts.Add($"物品：{reward.targetId} x{Mathf.Max(1, reward.value)}");
                        break;
                }
            }
        }

        string rewardSummary = rewardParts.Count > 0 ? string.Join("，", rewardParts) : "奖励已结算";
        return $"“{mission.missionName}”已完成。\n{rewardSummary}";
    }

    private void ShowMissionNotification(string title, string message, Color color, float duration)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, color, duration);
        }
    }

    // 查询接口
    public List<MissionDefinition> GetActiveMissions()
    {
        return activeMissions.Keys.Select(id => allMissions[id]).ToList();
    }

    public List<MissionDefinition> GetAvailableMissions()
    {
        return availableMissionIds
            .Where(id => allMissions.ContainsKey(id))
            .Select(id => allMissions[id])
            .OrderBy(mission => mission.priority)
            .ToList();
    }

    public List<MissionDefinition> GetCompletedMissions()
    {
        return completedMissionIds.Select(id => allMissions[id]).ToList();
    }

    public List<MissionDefinition> GetFailedMissions()
    {
        return failedMissionIds
            .Where(id => allMissions.ContainsKey(id))
            .Select(id => allMissions[id])
            .OrderBy(mission => mission.priority)
            .ToList();
    }

    public MissionRuntimeData GetMissionRuntimeData(string missionId)
    {
        return activeMissions.TryGetValue(missionId, out var data) ? data : null;
    }

    public bool IsMissionCompleted(string missionId)
    {
        return completedMissionIds.Contains(missionId);
    }

    // ISaveable 实现
    public void SaveToData(SaveData saveData)
    {
        saveData.missionData = new MissionSaveData
        {
            availableMissionIds = availableMissionIds.ToList(),
            activeMissions = activeMissions.Values.ToList(),
            completedMissionIds = completedMissionIds.ToList(),
            failedMissionIds = failedMissionIds.ToList()
        };
    }

    public void LoadFromData(SaveData saveData)
    {
        if (saveData.missionData == null) return;
        saveData.missionData.EnsureInitialized();

        availableMissionIds.Clear();
        activeMissions.Clear();
        completedMissionIds.Clear();
        failedMissionIds.Clear();

        foreach (var id in saveData.missionData.availableMissionIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                availableMissionIds.Add(id);
            }
        }

        foreach (var data in saveData.missionData.activeMissions)
        {
            if (data == null || string.IsNullOrEmpty(data.missionId)) continue;
            data.objectives ??= new List<MissionObjective>();
            activeMissions[data.missionId] = data;
        }

        foreach (var id in saveData.missionData.completedMissionIds)
        {
            completedMissionIds.Add(id);
        }

        foreach (var id in saveData.missionData.failedMissionIds)
        {
            failedMissionIds.Add(id);
        }
    }
}

[Serializable]
public class MissionListWrapper
{
    public List<MissionDefinition> missions;
}
