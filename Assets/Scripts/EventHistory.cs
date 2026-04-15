using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 事件历史与标记系统单例，记录所有已触发事件及其选择，管理全局标记和黑暗值。
/// </summary>
public class EventHistory : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========

    /// <summary>全局单例实例。</summary>
    public static EventHistory Instance { get; private set; }

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

    // ========== 内嵌数据类 ==========

    /// <summary>
    /// 事件记录，保存事件触发时的上下文信息。
    /// </summary>
    [Serializable]
    public class EventRecord
    {
        /// <summary>事件唯一标识。</summary>
        public string eventId;

        /// <summary>触发时的学年。</summary>
        public int triggerYear;

        /// <summary>触发时的学期。</summary>
        public int triggerSemester;

        /// <summary>触发时的回合。</summary>
        public int triggerRound;

        /// <summary>玩家选择的选项索引，无选项时为 -1。</summary>
        public int choiceIndex;
    }

    // ========== 事件 ==========

    /// <summary>当事件被记录时触发，参数为事件 ID。</summary>
    public event Action<string> OnEventRecorded;

    // ========== 内部存储 ==========

    /// <summary>所有事件记录列表。</summary>
    private List<EventRecord> records = new List<EventRecord>();

    /// <summary>全局标记字典。</summary>
    private Dictionary<string, bool> flags = new Dictionary<string, bool>();

    /// <summary>黑暗值，范围 0-100。</summary>
    private int darknessValue;

    // ========== 公开属性 ==========

    /// <summary>当前黑暗值（只读）。</summary>
    public int DarknessValue => darknessValue;

    // ========== 事件记录方法 ==========

    /// <summary>
    /// 记录一次事件触发，自动从 GameState 获取当前时间。
    /// </summary>
    /// <param name="eventId">事件唯一标识。</param>
    /// <param name="choiceIndex">玩家选择的选项索引，无选项时传 -1。</param>
    public void RecordEvent(string eventId, int choiceIndex)
    {
        var record = new EventRecord
        {
            eventId = eventId,
            choiceIndex = choiceIndex,
            triggerYear = GameState.Instance.CurrentYear,
            triggerSemester = GameState.Instance.CurrentSemester,
            triggerRound = GameState.Instance.CurrentRound
        };

        records.Add(record);
        Debug.Log($"[EventHistory] 记录事件: {eventId}, 选项: {choiceIndex}, " +
                  $"时间: 第{record.triggerYear}学年 第{record.triggerSemester}学期 第{record.triggerRound}回合");

        OnEventRecorded?.Invoke(eventId);
    }

    /// <summary>
    /// 检查某事件是否曾经触发过。
    /// </summary>
    /// <param name="eventId">事件唯一标识。</param>
    /// <returns>如果该事件曾触发过返回 true，否则返回 false。</returns>
    public bool HasTriggered(string eventId)
    {
        return records.Any(r => r.eventId == eventId);
    }

    /// <summary>
    /// 获取某事件最后一次触发时的选项索引。
    /// </summary>
    /// <param name="eventId">事件唯一标识。</param>
    /// <returns>最后一次选择的索引，若事件未触发过则返回 -1。</returns>
    public int GetChoiceForEvent(string eventId)
    {
        for (int i = records.Count - 1; i >= 0; i--)
        {
            if (records[i].eventId == eventId)
                return records[i].choiceIndex;
        }
        return -1;
    }

    /// <summary>
    /// 获取所有事件记录的副本。
    /// </summary>
    /// <returns>事件记录列表的浅拷贝。</returns>
    public List<EventRecord> GetAllRecords()
    {
        return new List<EventRecord>(records);
    }

    // ========== 标记系统 ==========

    /// <summary>
    /// 设置全局标记。
    /// </summary>
    /// <param name="flagName">标记名称。</param>
    /// <param name="value">标记值。</param>
    public void SetFlag(string flagName, bool value)
    {
        flags[flagName] = value;
        Debug.Log($"[EventHistory] 设置标记: {flagName} = {value}");
    }

    /// <summary>
    /// 获取全局标记的值。
    /// </summary>
    /// <param name="flagName">标记名称。</param>
    /// <returns>标记的值，不存在时返回 false。</returns>
    public bool GetFlag(string flagName)
    {
        return flags.TryGetValue(flagName, out bool value) && value;
    }

    // ========== 黑暗值 ==========

    /// <summary>
    /// 增加黑暗值，结果被限制在 0-100 范围内。
    /// </summary>
    /// <param name="amount">增加的量（可为负数以减少）。</param>
    public void AddDarkness(int amount)
    {
        int oldValue = darknessValue;
        darknessValue = Mathf.Clamp(darknessValue + amount, 0, 100);
        Debug.Log($"[EventHistory] 黑暗值变化: {oldValue} → {darknessValue} (变化量: {amount})");
    }

    // ========== ISaveable 实现 ==========

    /// <summary>将事件历史写入存档数据</summary>
    public void SaveToData(SaveData data)
    {
        // 完整事件记录
        data.eventRecords = new List<EventHistoryRecord>();
        data.triggeredEventIds = new List<string>();
        for (int i = 0; i < records.Count; i++)
        {
            EventRecord r = records[i];
            data.eventRecords.Add(new EventHistoryRecord
            {
                eventId = r.eventId,
                triggerYear = r.triggerYear,
                triggerSemester = r.triggerSemester,
                triggerRound = r.triggerRound,
                choiceIndex = r.choiceIndex
            });
            // 同时保留简化版以兼容旧读档逻辑
            if (!data.triggeredEventIds.Contains(r.eventId))
                data.triggeredEventIds.Add(r.eventId);
        }

        // 标记字典
        data.eventFlags = new List<StringBoolPair>();
        foreach (var kvp in flags)
        {
            data.eventFlags.Add(new StringBoolPair(kvp.Key, kvp.Value));
        }

        // 黑暗值
        data.darknessValue = darknessValue;

        Debug.Log($"[EventHistory] 存档保存: {records.Count} 条记录, {flags.Count} 个标记, 黑暗值={darknessValue}");
    }

    /// <summary>从存档数据恢复事件历史</summary>
    public void LoadFromData(SaveData data)
    {
        records.Clear();
        flags.Clear();

        // 优先从完整记录恢复
        if (data.eventRecords != null && data.eventRecords.Count > 0)
        {
            for (int i = 0; i < data.eventRecords.Count; i++)
            {
                EventHistoryRecord r = data.eventRecords[i];
                records.Add(new EventRecord
                {
                    eventId = r.eventId,
                    triggerYear = r.triggerYear,
                    triggerSemester = r.triggerSemester,
                    triggerRound = r.triggerRound,
                    choiceIndex = r.choiceIndex
                });
            }
        }
        else if (data.triggeredEventIds != null && data.triggeredEventIds.Count > 0)
        {
            // 旧存档兼容：仅有 triggeredEventIds，降级恢复（无时间信息）
            for (int i = 0; i < data.triggeredEventIds.Count; i++)
            {
                records.Add(new EventRecord
                {
                    eventId = data.triggeredEventIds[i],
                    triggerYear = 0,
                    triggerSemester = 0,
                    triggerRound = 0,
                    choiceIndex = -1
                });
            }
        }

        // 恢复标记
        if (data.eventFlags != null)
        {
            for (int i = 0; i < data.eventFlags.Count; i++)
            {
                StringBoolPair pair = data.eventFlags[i];
                flags[pair.key] = pair.value;
            }
        }

        // 恢复黑暗值
        darknessValue = data.darknessValue;

        Debug.Log($"[EventHistory] 存档加载: {records.Count} 条记录, {flags.Count} 个标记, 黑暗值={darknessValue}");
    }
}
