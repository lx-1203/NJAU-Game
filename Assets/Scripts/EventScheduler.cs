using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 事件调度器 —— 负责加载事件定义、检查触发条件、管理事件队列并驱动执行。
/// </summary>
public class EventScheduler : MonoBehaviour
{
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
        int totalLoaded = 0;

        foreach (string fileName in eventFileNames)
        {
            string path = $"Data/Events/{fileName}";
            TextAsset textAsset = Resources.Load<TextAsset>(path);

            if (textAsset == null)
            {
                Debug.LogWarning($"[EventScheduler] 未找到事件文件: {path}");
                continue;
            }

            EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(textAsset.text);

            if (root == null || root.events == null)
            {
                Debug.LogWarning($"[EventScheduler] 事件文件解析失败或为空: {path}");
                continue;
            }

            int fileCount = 0;
            foreach (EventDefinition evt in root.events)
            {
                if (string.IsNullOrEmpty(evt.id))
                {
                    Debug.LogWarning($"[EventScheduler] 发现无 id 的事件定义，已跳过 (文件: {fileName})");
                    continue;
                }

                if (allEvents.ContainsKey(evt.id))
                {
                    Debug.LogWarning($"[EventScheduler] 事件 id 重复: {evt.id}，后者覆盖前者");
                }

                allEvents[evt.id] = evt;
                fileCount++;
            }

            Debug.Log($"[EventScheduler] 已加载 {fileName}: {fileCount} 个事件");
            totalLoaded += fileCount;
        }

        Debug.Log($"[EventScheduler] 事件加载完毕，共 {totalLoaded} 个事件定义");
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

        // --- 时间条件 (0 表示不限) ---
        if (trigger.year > 0 && gs.CurrentYear != trigger.year)
            return false;

        if (trigger.semester > 0 && gs.CurrentSemester != trigger.semester)
            return false;

        if (trigger.roundMin > 0 && gs.CurrentRound < trigger.roundMin)
            return false;

        if (trigger.roundMax > 0 && gs.CurrentRound > trigger.roundMax)
            return false;

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
        EventHistory history = EventHistory.Instance;

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

        // --- 黑暗值条件 ---
        if (trigger.minDarkness > 0 && history.DarknessValue < trigger.minDarkness)
            return false;

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
        List<EventDefinition> candidates = new List<EventDefinition>();

        foreach (EventDefinition evt in allEvents.Values)
        {
            // 阶段筛选
            if (evt.GetTriggerPhase() != phase)
                continue;

            // 可重复性检查
            if (!evt.isRepeatable && EventHistory.Instance.HasTriggered(evt.id))
                continue;

            // 条件检查
            if (!CheckCondition(evt.trigger))
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

    // ========== 行为通知 ==========

    /// <summary>
    /// 遍历所有 Dark 类事件，将 triggerBehavior 匹配的事件加入队列。
    /// </summary>
    public void NotifyBehavior(string behavior)
    {
        if (string.IsNullOrEmpty(behavior)) return;

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

            // 条件检查
            if (!CheckCondition(evt.trigger))
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
    }
}
