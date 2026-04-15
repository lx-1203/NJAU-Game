using UnityEngine;
using System;

/// <summary>
/// NPC 事件中枢 —— 解耦 NPC 互动与 DialogueSystem
/// 所有对话请求通过本类事件发布，DialogueSystem 订阅后显示
/// </summary>
public class NPCEventHub : MonoBehaviour
{
    // ========== 单例 ==========
    public static NPCEventHub Instance { get; private set; }

    // ========== 对话请求数据 ==========

    /// <summary>对话请求</summary>
    public class DialogueRequest
    {
        public string npcId;
        public string speakerName;
        public string[] lines;
        public string portraitId;

        public DialogueRequest(string npcId, string speakerName, string[] lines, string portraitId = null)
        {
            this.npcId = npcId;
            this.speakerName = speakerName;
            this.lines = lines;
            this.portraitId = portraitId;
        }
    }

    // ========== 事件 ==========

    /// <summary>对话请求事件</summary>
    public event Action<DialogueRequest> OnDialogueRequested;

    /// <summary>社交互动完成通知（UI 反馈用）</summary>
    public event Action<string, string, int> OnSocialInteractionFeedback; // npcId, actionDisplayName, affinityDelta

    // ========== 公共方法 ==========

    /// <summary>发布对话请求</summary>
    public void RaiseDialogueRequested(DialogueRequest request)
    {
        if (request == null || request.lines == null || request.lines.Length == 0) return;

        Debug.Log($"[NPCEventHub] 对话请求: {request.speakerName} ({request.lines.Length}句)");
        OnDialogueRequested?.Invoke(request);
    }

    /// <summary>发布社交互动反馈</summary>
    public void RaiseSocialFeedback(string npcId, string actionDisplayName, int affinityDelta)
    {
        OnSocialInteractionFeedback?.Invoke(npcId, actionDisplayName, affinityDelta);
    }

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
}
