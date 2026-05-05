using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 事件调度器 —— 负责加载事件定义、检查触发条件、管理事件队列并驱动执行。
/// </summary>
public class EventScheduler : MonoBehaviour
{
    public sealed class EventValidationIssue
    {
        public string eventId;
        public string severity;
        public string message;
    }
    // ========== 单例 ==========

    public static EventScheduler Instance { get; private set; }

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

    // ========== 事件 ==========

    /// <summary>
    /// 当一个事件开始执行时触发。
    /// </summary>
    public event Action<EventDefinition> OnEventTriggered;

    /// <summary>
    /// 当一个事件执行完毕时触发。
    /// </summary>
    public event Action<EventDefinition> OnEventCompleted;

    // ========== 内部字段 ==========

    /// <summary>
    /// 所有已加载的事件定义，按 id 索引。
    /// </summary>
    private Dictionary<string, EventDefinition> allEvents = new Dictionary<string, EventDefinition>();

    /// <summary>
    /// 待执行的事件队列。
    /// </summary>
    private Queue<EventDefinition> eventQueue = new Queue<EventDefinition>();

    /// <summary>
    /// 标记当前是否正在逐个处理队列中的事件。
    /// </summary>
    private bool isProcessingQueue = false;
    private readonly HashSet<string> notifiedIssues = new HashSet<string>();
    private readonly List<EventValidationIssue> validationIssues = new List<EventValidationIssue>();

    // ========== JSON 文件名 ==========

    /// <summary>
    /// Resources/Data/Events/ 目录下需要加载的 JSON 文件名（不含扩展名）。
    /// </summary>
    private static readonly string[] eventFileNames =
    {
        "main_events",
        "fixed_events",
        "conditional_events",
        "dark_events"
    };

    // ========== 加载 ==========

    /// <summary>
    /// 从 Resources/Data/Events/ 加载全部事件 JSON 并合并到 allEvents 字典。
    /// </summary>
    public void LoadEvents()
    {
        allEvents.Clear();
        validationIssues.Clear();
        int totalLoaded = 0;

        foreach (string fileName in eventFileNames)
        {
            string path = $"Data/Events/{fileName}";
            TextAsset textAsset = Resources.Load<TextAsset>(path);

            if (textAsset == null)
            {
                Debug.LogWarning($"[EventScheduler] 未找到事件文件: {path}");
                ShowEventSchedulerNotificationOnce($"missing:{path}", "事件资源缺失", $"没有找到事件数据 {fileName}，相关剧情链路本局可能不会触发。");
                continue;
            }

            EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(textAsset.text);

            if (root == null || root.events == null)
            {
                Debug.LogWarning($"[EventScheduler] 事件文件解析失败或为空: {path}");
                ShowEventSchedulerNotificationOnce($"invalid:{path}", "事件数据异常", $"{fileName} 没有成功解析，相关事件将暂时被跳过。");
                continue;
            }

            int fileCount = 0;
            foreach (EventDefinition evt in root.events)
            {
                if (string.IsNullOrEmpty(evt.id))
                {
                    Debug.LogWarning($"[EventScheduler] 发现无 id 的事件定义，已跳过 (文件: {fileName})");
                    ShowEventSchedulerNotificationOnce($"empty-id:{fileName}", "事件配置不完整", $"{fileName} 中存在缺少编号的事件条目，系统已自动跳过异常数据。");
                    continue;
                }

                NormalizeEventDefinition(evt, treatZeroChanceAsLegacyDefault: true);

                if (allEvents.ContainsKey(evt.id))
                {
                    Debug.LogWarning($"[EventScheduler] 事件 id 重复: {evt.id}，后者覆盖前者");
                    ShowEventSchedulerNotificationOnce($"duplicate:{evt.id}", "事件编号重复", $"事件 {evt.id} 出现了重复配置，系统已采用最后一份定义。");
                }

                allEvents[evt.id] = evt;
                fileCount++;
            }

            Debug.Log($"[EventScheduler] 已加载 {fileName}: {fileCount} 个事件");
            totalLoaded += fileCount;
        }

        LoadAuthoredEvents(ref totalLoaded);
        RevalidateAllEvents();

        Debug.Log($"[EventScheduler] 事件加载完毕，共 {totalLoaded} 个事件定义");
    }

    private void LoadAuthoredEvents(ref int totalLoaded)
    {
        List<ZhongshanDeckEventEntry> authoredEntries = ZhongshanDeckToolStateBridge.GetAuthoredEvents();
        for (int i = 0; i < authoredEntries.Count; i++)
        {
            ZhongshanDeckEventEntry entry = authoredEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.json))
            {
                continue;
            }

            EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(entry.json);
            if (root == null || root.events == null || root.events.Length == 0)
            {
                ShowEventSchedulerNotificationOnce($"authored-invalid:{entry.eventId}", "事件草稿异常", $"钟山台草稿 {entry.eventId} 没有成功解析，系统已跳过这条配置。");
                continue;
            }

            for (int j = 0; j < root.events.Length; j++)
            {
                EventDefinition evt = root.events[j];
                if (evt == null || string.IsNullOrWhiteSpace(evt.id))
                {
                    continue;
                }

                NormalizeEventDefinition(evt, treatZeroChanceAsLegacyDefault: false);
                allEvents[evt.id] = evt;
                totalLoaded++;
            }
        }
    }

    // ========== 查询 ==========

    /// <summary>
    /// 按 ID 查询事件定义。找不到时返回 null。
    /// </summary>
    public EventDefinition GetEvent(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        allEvents.TryGetValue(id, out EventDefinition evt);
        return evt;
    }

    // ========== 条件检查 ==========

    /// <summary>
    /// 检查指定触发条件是否在当前游戏状态下全部满足。
    /// </summary>
    public bool CheckCondition(EventTriggerCondition trigger)
    {
        if (trigger == null) return true;

        GameState gs = GameState.Instance;
        PlayerAttributes pa = PlayerAttributes.Instance;
        EventHistory history = EventHistory.Instance;

        if (gs == null || pa == null || history == null)
        {
            return false;
        }

        // --- 时间条件 (0 表示不限) ---
        if (trigger.year > 0 && gs.CurrentYear != trigger.year)
            return false;

        if (trigger.semester > 0 && gs.CurrentSemester != trigger.semester)
            return false;

        if (trigger.roundMin > 0 && gs.CurrentRound < trigger.roundMin)
            return false;

        if (trigger.roundMax > 0 && gs.CurrentRound > trigger.roundMax)
            return false;

        if (trigger.specificRounds != null && trigger.specificRounds.Length > 0)
        {
            bool matchedRound = false;
            for (int i = 0; i < trigger.specificRounds.Length; i++)
            {
                if (gs.CurrentRound == trigger.specificRounds[i])
                {
                    matchedRound = true;
                    break;
                }
            }

            if (!matchedRound)
                return false;
        }

        // --- 属性条件 ---
        if (trigger.attributeConditions != null)
        {
            foreach (AttributeCondition ac in trigger.attributeConditions)
            {
                int attrValue = GetAttributeValue(ac.attributeName);
                if (!CompareValue(attrValue, ac.comparison, ac.value))
                    return false;
            }
        }

        // --- 金钱条件 (0 表示不限) ---
        if (trigger.minMoney > 0 && gs.Money < trigger.minMoney)
            return false;

        if (trigger.maxMoney > 0 && gs.Money > trigger.maxMoney)
            return false;

        // --- 前置事件条件 ---
        if (trigger.requiredEventIds != null)
        {
            foreach (string reqId in trigger.requiredEventIds)
            {
                if (!string.IsNullOrEmpty(reqId) && !history.HasTriggered(reqId))
                    return false;
            }
        }

        if (trigger.excludedEventIds != null)
        {
            foreach (string exId in trigger.excludedEventIds)
            {
                if (!string.IsNullOrEmpty(exId) && history.HasTriggered(exId))
                    return false;
            }
        }

        if (trigger.requiredFlags != null)
        {
            foreach (string flag in trigger.requiredFlags)
            {
                if (!string.IsNullOrEmpty(flag) && !history.GetFlag(flag))
                    return false;
            }
        }

        if (trigger.excludedFlags != null)
        {
            foreach (string flag in trigger.excludedFlags)
            {
                if (!string.IsNullOrEmpty(flag) && history.GetFlag(flag))
                    return false;
            }
        }

        // --- 黑暗值条件 ---
        if (trigger.minDarkness > 0 && history.DarknessValue < trigger.minDarkness)
            return false;

        // --- 地点条件 ---
        if (!string.IsNullOrWhiteSpace(trigger.requiredLocationId))
        {
            if (!Enum.TryParse(trigger.requiredLocationId, true, out LocationId requiredLocation))
            {
                return false;
            }

            if (gs.CurrentLocation != requiredLocation)
                return false;
        }

        // --- 好感度条件 ---
        if (trigger.affinityConditions != null && AffinitySystem.Instance != null)
        {
            foreach (AffinityCondition ac in trigger.affinityConditions)
            {
                if (string.IsNullOrEmpty(ac.npcId)) continue;

                NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(ac.npcId);

                // 数值条件
                if (ac.minValue > 0 && rel.affinity < ac.minValue)
                    return false;

                // 等级条件
                if (!string.IsNullOrEmpty(ac.minLevel))
                {
                    if (System.Enum.TryParse<AffinityLevel>(ac.minLevel, true, out AffinityLevel requiredLevel))
                    {
                        if (rel.level < requiredLevel)
                            return false;
                    }
                }
            }
        }

        if (trigger.romanceConditions != null && RomanceSystem.Instance != null)
        {
            foreach (RomanceCondition rc in trigger.romanceConditions)
            {
                if (rc == null)
                    continue;

                if (rc.requireAnyPartner && !RomanceSystem.Instance.HasPartner())
                    return false;

                if (string.IsNullOrWhiteSpace(rc.npcId))
                    continue;

                RomanceState currentState = RomanceSystem.Instance.GetRomanceState(rc.npcId);
                if (!string.IsNullOrWhiteSpace(rc.requiredState) &&
                    Enum.TryParse(rc.requiredState, true, out RomanceState requiredState) &&
                    currentState != requiredState)
                {
                    return false;
                }

                if (rc.minHealth > 0 && RomanceSystem.Instance.GetRomanceHealth(rc.npcId) < rc.minHealth)
                    return false;
            }
        }

        if (trigger.clubConditions != null && ClubSystem.Instance != null)
        {
            foreach (ClubCondition cc in trigger.clubConditions)
            {
                if (cc == null)
                    continue;

                if (cc.minPartyStage > 0 && ClubSystem.Instance.CurrentPartyStage < cc.minPartyStage)
                    return false;

                if (string.IsNullOrWhiteSpace(cc.clubId))
                    continue;

                ClubMembership membership = ClubSystem.Instance.GetMembership(cc.clubId);
                if (cc.requireJoined && membership == null)
                    return false;

                if (membership == null)
                    continue;

                if (cc.minRank > 0 && membership.currentRank < cc.minRank)
                    return false;

                if (cc.minRoundsInClub > 0 && membership.roundsInClub < cc.minRoundsInClub)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 根据属性名称从 PlayerAttributes 获取对应数值。
    /// </summary>
    private int GetAttributeValue(string attrName)
    {
        PlayerAttributes pa = PlayerAttributes.Instance;

        switch (attrName)
        {
            case "学力":   return pa.Study;
            case "魅力":   return pa.Charm;
            case "体魄":   return pa.Physique;
            case "领导力": return pa.Leadership;
            case "压力":   return pa.Stress;
            case "心情":   return pa.Mood;
            case "黑暗值": return pa.Darkness;
            case "负罪感": return pa.Guilt;
            case "幸运":   return pa.Luck;
            default:
                Debug.LogWarning($"[EventScheduler] 未知属性名称: {attrName}");
                return 0;
        }
    }

    /// <summary>
    /// 按比较运算符比较两个整数。
    /// </summary>
    private bool CompareValue(int actual, string comparison, int target)
    {
        switch (comparison)
        {
            case ">=": return actual >= target;
            case "<=": return actual <= target;
            case ">":  return actual > target;
            case "<":  return actual < target;
            case "==": return actual == target;
            case "!=": return actual != target;
            default:
                Debug.LogWarning($"[EventScheduler] 未知比较运算符: {comparison}");
                return false;
        }
    }

    // ========== 调度 ==========

    /// <summary>
    /// 在指定阶段检查所有事件的触发条件，将满足条件的事件按优先级入队并开始执行。
    /// </summary>
    public void CheckAndTriggerEvents(TriggerPhase phase)
    {
        if (GameState.Instance == null || EventHistory.Instance == null)
        {
            return;
        }

        List<EventDefinition> candidates = new List<EventDefinition>();

        foreach (EventDefinition evt in allEvents.Values)
        {
            // 阶段筛选
            if (evt.GetTriggerPhase() != phase)
                continue;

            // 行为驱动事件只能通过 NotifyBehavior 进入，避免在常规阶段扫描时误触发。
            if (evt.trigger != null && !string.IsNullOrEmpty(evt.trigger.triggerBehavior))
                continue;

            // 可重复性检查
            if (!evt.isRepeatable && EventHistory.Instance.HasTriggered(evt.id))
                continue;

            // 每回合触发上限检查
            if (evt.maxTriggersPerRound > 0 &&
                EventHistory.Instance.GetTriggerCountForRound(evt.id, GameState.Instance.CurrentYear, GameState.Instance.CurrentSemester, GameState.Instance.CurrentRound) >= evt.maxTriggersPerRound)
            {
                continue;
            }

            // 条件检查
            if (!CheckCondition(evt.trigger))
                continue;

            if (!PassProbabilityCheck(evt))
                continue;

            candidates.Add(evt);
        }

        // 按 priority 升序排序（数值越小优先级越高: Forced=0 > MainStory=1 > ...）
        candidates.Sort((a, b) => a.priority.CompareTo(b.priority));

        foreach (EventDefinition evt in candidates)
        {
            eventQueue.Enqueue(evt);
        }

        if (eventQueue.Count > 0 && !isProcessingQueue)
        {
            ProcessNextEvent();
        }
    }

    // ========== 队列处理 ==========

    /// <summary>
    /// 从队列中取出下一个事件并交由 EventExecutor 执行。
    /// 执行完毕后自动处理队列中的后续事件。
    /// </summary>
    private void ProcessNextEvent()
    {
        if (eventQueue.Count == 0)
        {
            isProcessingQueue = false;
            return;
        }

        isProcessingQueue = true;
        EventDefinition evt = eventQueue.Dequeue();

        OnEventTriggered?.Invoke(evt);
        Debug.Log($"[EventScheduler] 开始执行事件: {evt.id} - {evt.title}");

        if (EventExecutor.Instance != null)
        {
            EventExecutor.Instance.Execute(evt, () =>
            {
                OnEventCompleted?.Invoke(evt);
                Debug.Log($"[EventScheduler] 事件执行完毕: {evt.id}");
                ProcessNextEvent();
            });
        }
        else
        {
            Debug.LogWarning("[EventScheduler] EventExecutor.Instance 为 null，跳过事件执行");
            ShowEventSchedulerNotificationOnce("executor-missing", "事件执行器缺失", "有事件已经排到队列里，但执行模块没有就绪，这一段剧情会被自动跳过。");
            OnEventCompleted?.Invoke(evt);
            ProcessNextEvent();
        }
    }

    // ========== 外部入队 ==========

    /// <summary>
    /// 按事件 ID 查找定义并加入执行队列。若当前无事件在执行则立即开始处理。
    /// </summary>
    public void EnqueueEvent(string eventId)
    {
        EventDefinition evt = GetEvent(eventId);

        if (evt == null)
        {
            Debug.LogWarning($"[EventScheduler] 尝试入队未知事件: {eventId}");
            ShowEventSchedulerNotificationOnce($"unknown:{eventId}", "事件未找到", $"系统尝试触发事件 {eventId}，但没有找到对应定义。");
            return;
        }

        eventQueue.Enqueue(evt);
        Debug.Log($"[EventScheduler] 事件已入队: {eventId}");

        if (!isProcessingQueue)
        {
            ProcessNextEvent();
        }
    }

    /// <summary>
    /// 返回事件队列中是否还有待处理的事件。
    /// </summary>
    public bool HasPendingEvents()
    {
        return eventQueue.Count > 0;
    }

    public int GetPendingEventCount()
    {
        return eventQueue.Count;
    }

    public List<EventDefinition> GetPendingEventsSnapshot()
    {
        return new List<EventDefinition>(eventQueue.ToArray());
    }

    public int GetLoadedEventCount()
    {
        return allEvents.Count;
    }

    public List<EventDefinition> GetAllEventsSnapshot()
    {
        return allEvents.Values
            .OrderBy(evt => evt.priority)
            .ThenBy(evt => evt.id)
            .ToList();
    }

    public void RegisterOrReplaceRuntimeEvent(EventDefinition evt)
    {
        if (evt == null || string.IsNullOrWhiteSpace(evt.id))
        {
            Debug.LogWarning("[EventScheduler] 尝试注册空事件或无 ID 事件");
            ShowEventSchedulerNotificationOnce("runtime-empty", "运行时事件无效", "有一条运行时事件缺少编号，系统没有将它加入事件池。");
            return;
        }

        NormalizeEventDefinition(evt, treatZeroChanceAsLegacyDefault: false);
        allEvents[evt.id] = evt;
        RevalidateAllEvents();
        NotifyValidationIssuesForEvent(evt.id);
        Debug.Log($"[EventScheduler] 已注册运行时事件: {evt.id} - {evt.title}");
    }

    public bool RemoveRuntimeEvent(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        bool removed = allEvents.Remove(eventId);
        if (removed)
        {
            RevalidateAllEvents();
            Debug.Log($"[EventScheduler] 已移除运行时事件: {eventId}");
        }

        return removed;
    }

    // ========== 行为通知 ==========

    /// <summary>
    /// 遍历所有 Dark 类事件，将 triggerBehavior 匹配的事件加入队列。
    /// </summary>
    public int NotifyBehavior(string behavior)
    {
        if (string.IsNullOrEmpty(behavior)) return 0;
        if (GameState.Instance == null || EventHistory.Instance == null)
            return 0;

        List<EventDefinition> matched = new List<EventDefinition>();

        foreach (EventDefinition evt in allEvents.Values)
        {
            if (evt.GetEventType() != EventType.Dark)
                continue;

            if (evt.trigger == null || string.IsNullOrEmpty(evt.trigger.triggerBehavior))
                continue;

            if (evt.trigger.triggerBehavior != behavior)
                continue;

            // 可重复性检查
            if (!evt.isRepeatable && EventHistory.Instance.HasTriggered(evt.id))
                continue;

            if (evt.maxTriggersPerRound > 0 &&
                EventHistory.Instance.GetTriggerCountForRound(evt.id, GameState.Instance.CurrentYear, GameState.Instance.CurrentSemester, GameState.Instance.CurrentRound) >= evt.maxTriggersPerRound)
            {
                continue;
            }

            // 条件检查
            if (!CheckCondition(evt.trigger))
                continue;

            if (!PassProbabilityCheck(evt))
                continue;

            matched.Add(evt);
        }

        // 按 priority 排序后入队
        matched.Sort((a, b) => a.priority.CompareTo(b.priority));

        foreach (EventDefinition evt in matched)
        {
            eventQueue.Enqueue(evt);
            Debug.Log($"[EventScheduler] 行为 \"{behavior}\" 触发 Dark 事件: {evt.id}");
        }

        if (eventQueue.Count > 0 && !isProcessingQueue)
        {
            ProcessNextEvent();
        }

        return matched.Count;
    }

    private bool PassProbabilityCheck(EventDefinition evt)
    {
        if (evt == null || evt.trigger == null)
            return true;

        float chance = evt.trigger.triggerChance;
        if (chance <= 0f)
            return false;

        if (chance >= 1f)
            return true;

        bool passed = UnityEngine.Random.value <= chance;
        if (!passed)
        {
            Debug.Log($"[EventScheduler] 事件 {evt.id} 未通过概率检定: {chance:P0}");
        }

        return passed;
    }

    private void NormalizeEventDefinition(EventDefinition evt, bool treatZeroChanceAsLegacyDefault)
    {
        if (evt == null)
            return;

        if (evt.trigger == null)
            evt.trigger = new EventTriggerCondition();

        if (evt.trigger.specificRounds == null)
            evt.trigger.specificRounds = Array.Empty<int>();

        evt.trigger.requiredLocationId = evt.trigger.requiredLocationId ?? string.Empty;

        if (evt.trigger.attributeConditions == null)
            evt.trigger.attributeConditions = Array.Empty<AttributeCondition>();

        if (evt.trigger.affinityConditions == null)
            evt.trigger.affinityConditions = Array.Empty<AffinityCondition>();

        if (evt.trigger.romanceConditions == null)
            evt.trigger.romanceConditions = Array.Empty<RomanceCondition>();

        if (evt.trigger.clubConditions == null)
            evt.trigger.clubConditions = Array.Empty<ClubCondition>();

        if (evt.trigger.requiredEventIds == null)
            evt.trigger.requiredEventIds = Array.Empty<string>();

        if (evt.trigger.excludedEventIds == null)
            evt.trigger.excludedEventIds = Array.Empty<string>();

        if (evt.trigger.requiredFlags == null)
            evt.trigger.requiredFlags = Array.Empty<string>();

        if (evt.trigger.excludedFlags == null)
            evt.trigger.excludedFlags = Array.Empty<string>();

        if (treatZeroChanceAsLegacyDefault && evt.trigger.triggerChance <= 0f)
            evt.trigger.triggerChance = 1f;

        if (evt.dialogues == null)
            evt.dialogues = Array.Empty<EventDialogue>();

        if (evt.choices == null)
            evt.choices = Array.Empty<EventChoice>();

        if (evt.defaultEffects == null)
            evt.defaultEffects = Array.Empty<EventEffect>();

        if (evt.chainEventIds == null)
            evt.chainEventIds = Array.Empty<string>();

        if (evt.presentation != null)
        {
            evt.presentation.sceneKey = evt.presentation.sceneKey ?? string.Empty;
            evt.presentation.sceneDisplayName = evt.presentation.sceneDisplayName ?? string.Empty;
            evt.presentation.locationId = evt.presentation.locationId ?? string.Empty;
            evt.presentation.backgroundResourcePath = evt.presentation.backgroundResourcePath ?? string.Empty;
            evt.presentation.protagonistPortraitResourcePath = evt.presentation.protagonistPortraitResourcePath ?? string.Empty;
            evt.presentation.npcPortraitResourcePath = evt.presentation.npcPortraitResourcePath ?? string.Empty;
            evt.presentation.backgroundSlotName = evt.presentation.backgroundSlotName ?? string.Empty;
            evt.presentation.protagonistSlotName = evt.presentation.protagonistSlotName ?? string.Empty;
            evt.presentation.npcSlotName = evt.presentation.npcSlotName ?? string.Empty;
        }

        for (int i = 0; i < evt.choices.Length; i++)
        {
            EventChoice choice = evt.choices[i];
            if (choice == null)
                continue;

            if (choice.effects == null)
                choice.effects = Array.Empty<EventEffect>();
            if (choice.showConditions == null)
                choice.showConditions = Array.Empty<AttributeCondition>();
        }
    }

    public List<EventValidationIssue> GetValidationIssuesSnapshot()
    {
        return validationIssues
            .Select(issue => new EventValidationIssue
            {
                eventId = issue.eventId,
                severity = issue.severity,
                message = issue.message
            })
            .ToList();
    }

    private void RevalidateAllEvents()
    {
        validationIssues.Clear();
        foreach (EventDefinition evt in allEvents.Values)
        {
            ValidateEventDefinition(evt);
        }
    }

    private void ValidateEventDefinition(EventDefinition evt)
    {
        if (evt == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(evt.title))
        {
            AddValidationIssue(evt.id, "warning", "事件标题为空。");
        }

        if ((evt.dialogues == null || evt.dialogues.Length == 0) &&
            (evt.choices == null || evt.choices.Length == 0) &&
            (evt.defaultEffects == null || evt.defaultEffects.Length == 0))
        {
            AddValidationIssue(evt.id, "warning", "事件没有对话、选项或默认效果，触发后几乎不会产生内容。");
        }

        if (evt.dialogues != null)
        {
            for (int i = 0; i < evt.dialogues.Length; i++)
            {
                EventDialogue dialogue = evt.dialogues[i];
                if (dialogue == null)
                {
                    AddValidationIssue(evt.id, "warning", $"对话段 {i + 1} 为空引用。");
                    continue;
                }

                if (dialogue.lines == null || dialogue.lines.Length == 0)
                {
                    AddValidationIssue(evt.id, "warning", $"对话段 {i + 1} 没有台词内容。");
                    continue;
                }

                for (int j = 0; j < dialogue.lines.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(dialogue.lines[j]))
                    {
                        AddValidationIssue(evt.id, "info", $"对话段 {i + 1} 的第 {j + 1} 行为空。");
                    }
                }
            }
        }

        if (evt.trigger != null)
        {
            if (!Enum.TryParse(evt.trigger.phase, true, out TriggerPhase _))
            {
                AddValidationIssue(evt.id, "warning", $"触发阶段 {evt.trigger.phase} 无法识别，运行时会按默认阶段处理。");
            }

            if (evt.trigger.roundMin > 0 && evt.trigger.roundMax > 0 && evt.trigger.roundMin > evt.trigger.roundMax)
            {
                AddValidationIssue(evt.id, "warning", $"回合区间配置异常：roundMin {evt.trigger.roundMin} 大于 roundMax {evt.trigger.roundMax}。");
            }

            if (!string.IsNullOrWhiteSpace(evt.trigger.requiredLocationId) &&
                !Enum.TryParse(evt.trigger.requiredLocationId, true, out LocationId _))
            {
                AddValidationIssue(evt.id, "warning", $"触发地点 {evt.trigger.requiredLocationId} 不是有效的 LocationId。");
            }

            if (evt.trigger.specificRounds != null)
            {
                for (int i = 0; i < evt.trigger.specificRounds.Length; i++)
                {
                    int specificRound = evt.trigger.specificRounds[i];
                    if (specificRound <= 0)
                    {
                        AddValidationIssue(evt.id, "warning", $"指定回合 {specificRound} 无效，回合数应大于 0。");
                    }

                    if (evt.trigger.roundMin > 0 && specificRound < evt.trigger.roundMin)
                    {
                        AddValidationIssue(evt.id, "warning", $"指定回合 {specificRound} 小于 roundMin {evt.trigger.roundMin}。");
                    }

                    if (evt.trigger.roundMax > 0 && specificRound > evt.trigger.roundMax)
                    {
                        AddValidationIssue(evt.id, "warning", $"指定回合 {specificRound} 大于 roundMax {evt.trigger.roundMax}。");
                    }
                }
            }

            ValidateLinkedIds(evt.id, evt.trigger.requiredEventIds, "前置事件");
            ValidateLinkedIds(evt.id, evt.trigger.excludedEventIds, "排除事件");

            if (evt.trigger.romanceConditions != null)
            {
                for (int i = 0; i < evt.trigger.romanceConditions.Length; i++)
                {
                    RomanceCondition romanceCondition = evt.trigger.romanceConditions[i];
                    if (romanceCondition == null)
                    {
                        AddValidationIssue(evt.id, "warning", $"恋爱条件 {i + 1} 为空引用。");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(romanceCondition.requiredState) &&
                        !Enum.TryParse(romanceCondition.requiredState, true, out RomanceState _))
                    {
                        AddValidationIssue(evt.id, "warning", $"恋爱条件 {i + 1} 的状态 {romanceCondition.requiredState} 无法识别。");
                    }
                }
            }

            if (evt.trigger.clubConditions != null)
            {
                for (int i = 0; i < evt.trigger.clubConditions.Length; i++)
                {
                    ClubCondition clubCondition = evt.trigger.clubConditions[i];
                    if (clubCondition == null)
                    {
                        AddValidationIssue(evt.id, "warning", $"社团条件 {i + 1} 为空引用。");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(clubCondition.clubId) &&
                        ClubSystem.Instance != null &&
                        ClubSystem.Instance.GetClub(clubCondition.clubId) == null)
                    {
                        AddValidationIssue(evt.id, "warning", $"社团条件 {i + 1} 指向的社团 {clubCondition.clubId} 不存在。");
                    }
                }
            }
        }

        ValidateLinkedIds(evt.id, evt.chainEventIds, "事件链");

        if (evt.choices != null)
        {
            for (int i = 0; i < evt.choices.Length; i++)
            {
                EventChoice choice = evt.choices[i];
                if (choice == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(choice.triggerEventId) && !allEvents.ContainsKey(choice.triggerEventId))
                {
                    AddValidationIssue(evt.id, "warning", $"选项 {i + 1} 指向的后续事件 {choice.triggerEventId} 不存在。");
                }

                if (!string.IsNullOrWhiteSpace(choice.triggerEventId) && choice.triggerEventId == evt.id)
                {
                    AddValidationIssue(evt.id, "warning", $"选项 {i + 1} 指向自己，可能造成重复触发循环。");
                }

                if ((choice.effects == null || choice.effects.Length == 0) &&
                    choice.actionPointCost == 0 &&
                    choice.moneyCost == 0 &&
                    string.IsNullOrWhiteSpace(choice.triggerEventId))
                {
                    AddValidationIssue(evt.id, "info", $"选项 {i + 1} 没有任何数值、状态或后续事件影响。");
                }
            }
        }

        if (evt.presentation != null)
        {
            if (!string.IsNullOrWhiteSpace(evt.presentation.locationId) &&
                !Enum.TryParse(evt.presentation.locationId, true, out LocationId _))
            {
                AddValidationIssue(evt.id, "warning", $"演出地点 {evt.presentation.locationId} 不是有效的 LocationId。");
            }

            if (string.IsNullOrWhiteSpace(evt.presentation.sceneKey) &&
                string.IsNullOrWhiteSpace(evt.presentation.sceneDisplayName) &&
                string.IsNullOrWhiteSpace(evt.presentation.backgroundResourcePath))
            {
                AddValidationIssue(evt.id, "info", "已启用事件演出配置，但场景键、场景名和背景资源仍为空。");
            }

            if (!string.IsNullOrWhiteSpace(evt.presentation.backgroundResourcePath) &&
                string.IsNullOrWhiteSpace(evt.presentation.backgroundSlotName))
            {
                AddValidationIssue(evt.id, "info", "背景资源已填写，但背景占位名为空，后续美术接入时不方便定位。");
            }

            if (!string.IsNullOrWhiteSpace(evt.presentation.protagonistPortraitResourcePath) &&
                string.IsNullOrWhiteSpace(evt.presentation.protagonistSlotName))
            {
                AddValidationIssue(evt.id, "info", "主角立绘资源已填写，但主角占位名为空。");
            }

            if (!string.IsNullOrWhiteSpace(evt.presentation.npcPortraitResourcePath) &&
                string.IsNullOrWhiteSpace(evt.presentation.npcSlotName))
            {
                AddValidationIssue(evt.id, "info", "NPC 立绘资源已填写，但 NPC 占位名为空。");
            }
        }
    }

    private void ValidateLinkedIds(string ownerEventId, string[] linkedIds, string label)
    {
        if (linkedIds == null)
        {
            return;
        }

        for (int i = 0; i < linkedIds.Length; i++)
        {
            string linkedId = linkedIds[i];
            if (string.IsNullOrWhiteSpace(linkedId))
            {
                continue;
            }

            if (!allEvents.ContainsKey(linkedId))
            {
                AddValidationIssue(ownerEventId, "warning", $"{label} {linkedId} 不存在。");
            }
            else if (linkedId == ownerEventId)
            {
                AddValidationIssue(ownerEventId, "warning", $"{label} {linkedId} 指向自身，可能造成循环。");
            }
        }
    }

    private void AddValidationIssue(string eventId, string severity, string message)
    {
        validationIssues.Add(new EventValidationIssue
        {
            eventId = eventId,
            severity = severity,
            message = message
        });
    }

    private void NotifyValidationIssuesForEvent(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        EventValidationIssue issue = validationIssues.FirstOrDefault(item =>
            item != null &&
            item.eventId == eventId &&
            string.Equals(item.severity, "warning", StringComparison.OrdinalIgnoreCase));

        if (issue == null)
        {
            return;
        }

        ShowEventSchedulerNotificationOnce(
            $"validation:{eventId}:{issue.message}",
            "事件校验提醒",
            $"{eventId} 存在配置风险：{issue.message}",
            3.6f);
    }

    private void ShowEventSchedulerNotificationOnce(string key, string title, string message, float duration = 3f)
    {
        if (notifiedIssues.Contains(key))
        {
            return;
        }

        notifiedIssues.Add(key);

        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, new Color(0.82f, 0.38f, 0.30f), duration);
        }
    }
}
