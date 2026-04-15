using System;
using UnityEngine;

/// <summary>
/// 事件执行器单例，负责按顺序播放事件对话、展示选项、应用效果并处理事件链。
/// </summary>
public class EventExecutor : MonoBehaviour
{
    // ========== 单例 ==========

    /// <summary>全局单例实例。</summary>
    public static EventExecutor Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ========== 状态 ==========

    /// <summary>当前是否正在执行事件。</summary>
    public bool IsExecuting => isExecuting;

    /// <summary>执行锁，防止递归调用。</summary>
    private bool isExecuting = false;

    /// <summary>对话触发器接口引用。</summary>
    private IDialogueTrigger dialogueTrigger;

    /// <summary>当前正在执行的事件定义。</summary>
    private EventDefinition currentEventDef;

    /// <summary>当前事件完成后的回调。</summary>
    private Action currentOnComplete;

    // ========== 初始化 ==========

    private void Start()
    {
        dialogueTrigger = DialogueSystem.Instance as IDialogueTrigger;

        if (dialogueTrigger == null)
        {
            Debug.LogError("[EventExecutor] DialogueSystem.Instance 未实现 IDialogueTrigger 接口！");
        }
    }

    // ========== 事件执行入口 ==========

    /// <summary>
    /// 执行一个事件定义，按顺序播放对话、展示选项、应用效果。
    /// </summary>
    /// <param name="eventDef">要执行的事件定义。</param>
    /// <param name="onComplete">事件执行完毕后的回调。</param>
    public void Execute(EventDefinition eventDef, Action onComplete)
    {
        if (isExecuting)
        {
            Debug.LogWarning($"[EventExecutor] 事件 {eventDef.id} 被忽略，当前正在执行另一个事件。");
            return;
        }

        isExecuting = true;
        currentEventDef = eventDef;
        currentOnComplete = onComplete;

        Debug.Log($"[EventExecutor] 开始执行事件: {eventDef.id}");

        PlayDialogueSequence(eventDef, 0);
    }

    // ========== 对话序列 ==========

    /// <summary>
    /// 递归播放事件对话序列。
    /// </summary>
    /// <param name="eventDef">事件定义。</param>
    /// <param name="dialogueIndex">当前对话索引。</param>
    private void PlayDialogueSequence(EventDefinition eventDef, int dialogueIndex)
    {
        // 所有对话播放完毕，进入选项/效果阶段
        if (eventDef.dialogues == null || dialogueIndex >= eventDef.dialogues.Length)
        {
            OnDialoguesFinished(eventDef);
            return;
        }

        EventDialogue dialogue = eventDef.dialogues[dialogueIndex];

        dialogueTrigger.ShowDialogue(dialogue.speaker, dialogue.lines, () =>
        {
            PlayDialogueSequence(eventDef, dialogueIndex + 1);
        });
    }

    // ========== 对话完成处理 ==========

    /// <summary>
    /// 所有对话播放完毕后，根据是否有选项进入不同分支。
    /// </summary>
    /// <param name="eventDef">事件定义。</param>
    private void OnDialoguesFinished(EventDefinition eventDef)
    {
        // 有选项 → 展示选择界面
        if (eventDef.choices != null && eventDef.choices.Length > 0)
        {
            dialogueTrigger.ShowChoices(eventDef.choices, (int selectedIndex) =>
            {
                OnChoiceSelected(eventDef, selectedIndex);
            });
        }
        else
        {
            // 无选项 → 应用默认效果
            if (eventDef.defaultEffects != null && eventDef.defaultEffects.Length > 0)
            {
                ApplyEffects(eventDef.defaultEffects);
            }

            EventHistory.Instance.RecordEvent(eventDef.id, -1);
            Debug.Log($"[EventExecutor] 事件 {eventDef.id} 无选项，已应用默认效果。");

            FinishExecution();
        }
    }

    /// <summary>
    /// 玩家选择选项后的处理：应用效果、记录历史、处理触发事件。
    /// </summary>
    /// <param name="eventDef">事件定义。</param>
    /// <param name="index">玩家选择的选项索引。</param>
    private void OnChoiceSelected(EventDefinition eventDef, int index)
    {
        EventChoice selectedChoice = eventDef.choices[index];

        Debug.Log($"[EventExecutor] 事件 {eventDef.id} 玩家选择了选项 {index}: {selectedChoice.text}");

        // 应用选项效果
        if (selectedChoice.effects != null && selectedChoice.effects.Length > 0)
        {
            ApplyEffects(selectedChoice.effects);
        }

        // 记录事件历史
        EventHistory.Instance.RecordEvent(eventDef.id, index);

        // 处理选项触发的链接事件
        if (!string.IsNullOrEmpty(selectedChoice.triggerEventId))
        {
            Debug.Log($"[EventExecutor] 选项触发链接事件: {selectedChoice.triggerEventId}");
            EventScheduler.Instance.EnqueueEvent(selectedChoice.triggerEventId);
        }

        FinishExecution();
    }

    // ========== 效果应用 ==========

    /// <summary>
    /// 遍历并应用一组事件效果。
    /// </summary>
    /// <param name="effects">要应用的效果数组。</param>
    private void ApplyEffects(EventEffect[] effects)
    {
        foreach (var effect in effects)
        {
            if (effect == null) continue;

            switch (effect.type)
            {
                case "attribute":
                    PlayerAttributes.Instance.AddAttribute(effect.target, effect.value);
                    Debug.Log($"[EventExecutor] 效果: 属性 {effect.target} {(effect.value >= 0 ? "+" : "")}{effect.value}");
                    break;

                case "money":
                    GameState.Instance.AddMoney(effect.value);
                    Debug.Log($"[EventExecutor] 效果: 金钱 {(effect.value >= 0 ? "+" : "")}{effect.value}");
                    break;

                case "flag":
                    EventHistory.Instance.SetFlag(effect.target, effect.value != 0);
                    Debug.Log($"[EventExecutor] 效果: 标记 {effect.target} = {(effect.value != 0)}");
                    break;

                case "darkness":
                    EventHistory.Instance.AddDarkness(effect.value);
                    Debug.Log($"[EventExecutor] 效果: 黑暗值 {(effect.value >= 0 ? "+" : "")}{effect.value}");
                    break;

                case "unlock":
                    EventHistory.Instance.SetFlag(effect.target, true);
                    Debug.Log($"[EventExecutor] 效果: 解锁 {effect.target}");
                    break;

                default:
                    Debug.LogWarning($"[EventExecutor] 未知效果类型: {effect.type}");
                    break;
            }
        }
    }

    // ========== 执行完成 ==========

    /// <summary>
    /// 完成当前事件执行：处理事件链、重置状态、触发回调。
    /// </summary>
    private void FinishExecution()
    {
        // 处理事件链
        if (currentEventDef.chainEventIds != null && currentEventDef.chainEventIds.Length > 0)
        {
            foreach (string chainId in currentEventDef.chainEventIds)
            {
                if (!string.IsNullOrEmpty(chainId))
                {
                    Debug.Log($"[EventExecutor] 事件链入队: {chainId}");
                    EventScheduler.Instance.EnqueueEvent(chainId);
                }
            }
        }

        Debug.Log($"[EventExecutor] 事件执行完毕: {currentEventDef.id}");

        // 重置执行状态
        isExecuting = false;

        // 触发完成回调
        Action callback = currentOnComplete;
        currentEventDef = null;
        currentOnComplete = null;

        callback?.Invoke();
    }
}
