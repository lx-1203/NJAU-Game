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

    /// <summary>
    /// 检查任务触发条件
    /// </summary>
    private void CheckMissionTriggers(GameState.RoundAdvanceResult result)
    {
        foreach (var mission in allMissions.Values)
        {
            // 跳过已完成/失败/进行中的任务
            if (completedMissionIds.Contains(mission.missionId) ||
                failedMissionIds.Contains(mission.missionId) ||
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
        switch (condition.conditionType)
        {
            case "Round":
                return CompareValue(GameState.Instance.CurrentRound, condition.minValue, condition.comparisonOperator);

            case "Semester":
                return CompareValue(GameState.Instance.CurrentSemester, condition.minValue, condition.comparisonOperator);

            case "Attribute":
                int attrValue = GetAttributeValue(condition.targetId);
                return CompareValue(attrValue, condition.minValue, condition.comparisonOperator);

            case "Money":
                return CompareValue(GameState.Instance.Money, condition.minValue, condition.comparisonOperator);

            case "NPCAffinity":
                var rel = AffinitySystem.Instance.GetRelationship(condition.targetId);
                int affinity = rel != null ? rel.affinity : 0;
                return CompareValue(affinity, condition.minValue, condition.comparisonOperator);

            case "Event":
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
        Debug.Log($"[MissionSystem] Mission unlocked: {mission.missionName}");
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
            return false;
        }

        if (activeMissions.ContainsKey(missionId))
        {
            Debug.LogWarning($"[MissionSystem] Mission already active: {missionId}");
            return false;
        }

        var runtimeData = new MissionRuntimeData(mission);
        runtimeData.status = MissionStatus.Active;
        runtimeData.acceptedRound = GameState.Instance.CurrentRound;
        activeMissions[missionId] = runtimeData;

        Debug.Log($"[MissionSystem] Mission accepted: {mission.missionName}");
        OnMissionAccepted?.Invoke(mission);

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

            foreach (var objective in runtimeData.objectives)
            {
                if (objective.isCompleted) continue;

                int currentValue = GetObjectiveCurrentValue(objective);
                if (currentValue != objective.currentValue)
                {
                    objective.currentValue = currentValue;
                    if (objective.currentValue >= objective.targetValue)
                    {
                        objective.isCompleted = true;
                    }
                    OnObjectiveUpdated?.Invoke(definition, objective);
                }
            }

            // 检查任务是否完成
            if (runtimeData.objectives.All(o => o.isCompleted))
            {
                CompleteMission(missionId);
            }
        }
    }

    private int GetObjectiveCurrentValue(MissionObjective objective)
    {
        switch (objective.type)
        {
            case MissionObjectiveType.ReachRound:
                return GameState.Instance.CurrentRound;

            case MissionObjectiveType.ReachSemester:
                return GameState.Instance.CurrentSemester;

            case MissionObjectiveType.AttributeThreshold:
                return GetAttributeValue(objective.targetId);

            case MissionObjectiveType.MoneyThreshold:
                return GameState.Instance.Money;

            case MissionObjectiveType.NPCAffinityThreshold:
                var r = AffinitySystem.Instance.GetRelationship(objective.targetId);
                return r != null ? r.affinity : 0;

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
                    EconomyManager.Instance.Earn(reward.value, TransactionRecord.TransactionType.OtherIncome, "任务奖励");
                    break;

                case MissionRewardType.Attribute:
                    ModifyAttribute(reward.targetId, reward.value);
                    break;

                case MissionRewardType.Unlock:
                    EventHistory.Instance.SetFlag(reward.targetId, true);
                    break;
            }
        }
    }

    private void ModifyAttribute(string attributeName, int value)
    {
        var attrs = PlayerAttributes.Instance;
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
                int elapsedRounds = GameState.Instance.CurrentRound - runtimeData.acceptedRound;
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

    // 查询接口
    public List<MissionDefinition> GetActiveMissions()
    {
        return activeMissions.Keys.Select(id => allMissions[id]).ToList();
    }

    public List<MissionDefinition> GetCompletedMissions()
    {
        return completedMissionIds.Select(id => allMissions[id]).ToList();
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
            activeMissions = activeMissions.Values.ToList(),
            completedMissionIds = completedMissionIds.ToList(),
            failedMissionIds = failedMissionIds.ToList()
        };
    }

    public void LoadFromData(SaveData saveData)
    {
        if (saveData.missionData == null) return;
        saveData.missionData.EnsureInitialized();

        activeMissions.Clear();
        completedMissionIds.Clear();
        failedMissionIds.Clear();

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
