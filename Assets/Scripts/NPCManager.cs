using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NPC 管理器 —— 根据时间段和当前地点刷新场景中的 NPC，管理活跃 NPC 实例
/// 单例模式，在 GameSceneInitializer 中初始化
/// </summary>
public class NPCManager : MonoBehaviour
{
    // ========== 单例 ==========
    public static NPCManager Instance { get; private set; }

    // ========== 内部字段 ==========
    private Dictionary<string, NPCController> activeNPCs = new Dictionary<string, NPCController>();

    // ========== NPC 生成参数 ==========
    private const float PositionOffsetX = 3.0f;

    // ========== 公共方法 ==========

    /// <summary>
    /// 根据当前时间段和当前地点刷新场景中的 NPC
    /// 只显示日程地点匹配 GameState.CurrentLocation 的 NPC
    /// </summary>
    public void RefreshNPCsForCurrentTimeSlot()
    {
        if (NPCDatabase.Instance == null)
        {
            Debug.LogWarning("[NPCManager] NPCDatabase 实例不存在，无法刷新NPC");
            return;
        }

        TimeSlot currentSlot = AffinitySystem.GetCurrentTimeSlot();
        NPCData[] allNPCs = NPCDatabase.Instance.GetAllNPCs();
        LocationId currentLoc = GameState.Instance != null
            ? GameState.Instance.CurrentLocation
            : LocationId.Dormitory;

        // 收集当前时间段 + 当前地点应在场的 NPC ID
        HashSet<string> shouldBeActive = new HashSet<string>();
        for (int i = 0; i < allNPCs.Length; i++)
        {
            string location = NPCDatabase.Instance.GetNPCLocation(allNPCs[i].id, currentSlot);
            if (string.IsNullOrEmpty(location)) continue;

            // 将日程地点字符串解析为 LocationId，与当前地点比对
            LocationId? resolved = LocationManager.ResolveScheduleLocation(location);
            if (resolved.HasValue && resolved.Value == currentLoc)
            {
                shouldBeActive.Add(allNPCs[i].id);
            }
        }

        // 移除不应在场的 NPC
        List<string> toRemove = new List<string>();
        foreach (var kvp in activeNPCs)
        {
            if (!shouldBeActive.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }
        for (int i = 0; i < toRemove.Count; i++)
        {
            if (activeNPCs.TryGetValue(toRemove[i], out NPCController controller))
            {
                if (controller != null && controller.gameObject != null)
                {
                    Debug.Log($"[NPCManager] 移除NPC: {toRemove[i]}");
                    Destroy(controller.gameObject);
                }
                activeNPCs.Remove(toRemove[i]);
            }
        }

        // 获取当前地点的世界空间区域信息
        LocationDefinition locDef = LocationManager.Instance != null
            ? LocationManager.Instance.GetLocation(currentLoc)
            : null;
        float centerX = locDef != null ? locDef.worldCenterX : 5f;
        float minX = locDef != null ? locDef.worldMinX + 2f : 0f;
        float maxX = locDef != null ? locDef.worldMaxX - 2f : 30f;
        float spawnY = locDef != null ? locDef.worldSpawnY : -3.5f;

        // 计算需要新创建的 NPC 数量用于居中分布
        int newCount = 0;
        foreach (string npcId in shouldBeActive)
        {
            if (!activeNPCs.ContainsKey(npcId)) newCount++;
        }

        // 创建应在场但尚未存在的 NPC（在区域中心附近对称分布）
        int spawnIndex = 0;
        foreach (string npcId in shouldBeActive)
        {
            if (!activeNPCs.ContainsKey(npcId))
            {
                NPCData data = NPCDatabase.Instance.GetNPC(npcId);
                if (data != null)
                {
                    float x = centerX + (spawnIndex - (newCount - 1) * 0.5f) * PositionOffsetX;
                    x = Mathf.Clamp(x, minX, maxX);
                    Vector3 position = new Vector3(x, spawnY, 0f);
                    CreateNPCObject(data, position);
                    spawnIndex++;
                }
            }
        }

        Debug.Log($"[NPCManager] NPC刷新完成，地点: {currentLoc}，时间段: {currentSlot}，活跃NPC数: {activeNPCs.Count}");
    }

    /// <summary>
    /// 获取当前场景中活跃的 NPC 数据列表
    /// </summary>
    public List<NPCData> GetAvailableNPCs()
    {
        List<NPCData> result = new List<NPCData>();

        if (NPCDatabase.Instance == null) return result;

        foreach (var kvp in activeNPCs)
        {
            if (kvp.Value != null)
            {
                NPCData data = NPCDatabase.Instance.GetNPC(kvp.Key);
                if (data != null)
                {
                    result.Add(data);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 根据 ID 获取活跃的 NPCController
    /// </summary>
    public NPCController GetNPCController(string npcId)
    {
        activeNPCs.TryGetValue(npcId, out NPCController controller);
        return controller;
    }

    // ========== NPC 创建 ==========

    /// <summary>
    /// 创建 NPC GameObject，添加 SpriteRenderer + NPCController 并初始化
    /// </summary>
    private void CreateNPCObject(NPCData data, Vector3 position)
    {
        GameObject npcObj = new GameObject($"NPC_{data.id}");
        npcObj.transform.position = position;

        SpriteRenderer sr = npcObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        NPCController controller = npcObj.AddComponent<NPCController>();
        controller.Initialize(data);

        activeNPCs[data.id] = controller;

        Debug.Log($"[NPCManager] 创建NPC: {data.displayName} (ID: {data.id}) 在位置: {position}");
    }

    // ========== 事件回调 ==========

    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        RefreshNPCsForCurrentTimeSlot();
    }

    private void OnLocationChanged(LocationId from, LocationId to)
    {
        RefreshNPCsForCurrentTimeSlot();
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

    private void Start()
    {
        // 订阅回合推进事件
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;
        }
        else
        {
            Debug.LogWarning("[NPCManager] TurnManager 实例不存在，无法订阅回合推进事件");
        }

        // 订阅地点切换事件 —— 切换地点时刷新NPC
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged += OnLocationChanged;
        }

        // 首次刷新
        RefreshNPCsForCurrentTimeSlot();
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
        }
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged -= OnLocationChanged;
        }
    }
}
