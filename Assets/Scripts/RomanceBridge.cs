using UnityEngine;

/// <summary>
/// 恋爱系统桥接器 —— 实现 IRelationshipExtension 接口
/// 连接 AffinitySystem 和 RomanceSystem，实现双向数据同步
/// 在 AffinitySystem.Start() 之后由 GameSceneInitializer 间接初始化
/// </summary>
public class RomanceBridge : MonoBehaviour, IRelationshipExtension
{
    // ========== 单例 ==========
    public static RomanceBridge Instance { get; private set; }

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
        // 注册为 AffinitySystem 的扩展
        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.RegisterExtension(this);
            Debug.Log("[RomanceBridge] 已注册为 AffinitySystem 扩展");
        }

        // 订阅好感度变化事件 → 同步到 RomanceSystem
        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged += OnAffinityChanged;
        }

        // 订阅恋爱状态变化事件 → 同步到 NPCRelationshipData
        if (RomanceSystem.Instance != null)
        {
            RomanceSystem.Instance.OnRomanceStateChanged += OnRomanceStateChanged;
        }

        Debug.Log("[RomanceBridge] 初始化完成，已建立 AffinitySystem <-> RomanceSystem 双向桥接");
    }

    private void OnDestroy()
    {
        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged -= OnAffinityChanged;
        }

        if (RomanceSystem.Instance != null)
        {
            RomanceSystem.Instance.OnRomanceStateChanged -= OnRomanceStateChanged;
        }
    }

    // ========== IRelationshipExtension 实现 ==========

    /// <summary>
    /// 好感度等级变化时的回调
    /// </summary>
    public void OnAffinityLevelChanged(string npcId, AffinityLevel oldLevel, AffinityLevel newLevel)
    {
        // 当好感度等级达到 CloseFriend (60+) 时通知 RomanceSystem 检查暗恋状态
        if (AffinitySystem.Instance != null)
        {
            int affinity = AffinitySystem.Instance.GetRelationship(npcId).affinity;
            if (RomanceSystem.Instance != null)
            {
                RomanceSystem.Instance.CheckAndUpdateCrushingState(npcId, affinity);
            }
        }
    }

    /// <summary>
    /// 社交互动完成时的回调 → 标记恋爱互动
    /// </summary>
    public void OnInteractionCompleted(string npcId, string socialActionId, int affinityDelta)
    {
        if (RomanceSystem.Instance == null) return;

        // 如果 NPC 处于 Dating 状态，标记本回合已互动
        RomanceState state = RomanceSystem.Instance.GetRomanceState(npcId);
        if (state == RomanceState.Dating)
        {
            RomanceSystem.Instance.MarkInteractedThisRound(npcId);
        }
    }

    /// <summary>
    /// 检查是否允许进入 Lover 等级
    /// 条件：RomanceSystem 中该 NPC 处于 Dating 状态
    /// </summary>
    public bool CanEnterLoverLevel(string npcId)
    {
        if (RomanceSystem.Instance == null) return false;
        return RomanceSystem.Instance.GetRomanceState(npcId) == RomanceState.Dating;
    }

    // ========== 事件处理：AffinitySystem → RomanceSystem ==========

    /// <summary>
    /// 好感度变化时同步到 RomanceSystem 的好感度缓存和暗恋状态检查
    /// </summary>
    private void OnAffinityChanged(string npcId, int oldAffinity, int newAffinity, int delta)
    {
        if (RomanceSystem.Instance != null)
        {
            // 同步好感度缓存
            RomanceSystem.Instance.UpdateAffinityCache(npcId, newAffinity);

            // 检查暗恋状态转换
            RomanceSystem.Instance.CheckAndUpdateCrushingState(npcId, newAffinity);
        }
    }

    // ========== 事件处理：RomanceSystem → NPCRelationshipData ==========

    /// <summary>
    /// 恋爱状态变化时同步到 NPCRelationshipData.romanceState
    /// </summary>
    private void OnRomanceStateChanged(string npcId, RomanceState oldState, RomanceState newState)
    {
        if (AffinitySystem.Instance != null)
        {
            NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(npcId);
            rel.romanceState = newState;
            Debug.Log($"[RomanceBridge] 同步 {npcId} romanceState: {oldState} -> {newState}");
        }
    }
}
