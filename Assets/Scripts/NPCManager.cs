using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// NPC 管理器 —— 根据时间段和当前地点刷新场景中的 NPC，管理活跃 NPC 实例
/// 单例模式，在 GameSceneInitializer 中初始化
/// </summary>
[ExecuteAlways]
public class NPCManager : MonoBehaviour
{
    [System.Serializable]
    private class NPCPreviewDatabase
    {
        public NPCData[] npcs;
    }

    // ========== 单例 ==========
    public static NPCManager Instance { get; private set; }

    // ========== 内部字段 ==========
    private Dictionary<string, NPCController> activeNPCs = new Dictionary<string, NPCController>();
    // ========== NPC 生成参数 ==========
    private const float PositionOffsetX = 3.0f;
    private const string AnchorRootName = "_NPCSceneAnchors";

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] private bool syncPreviewTimeSlotWithStartupState = true;
    [SerializeField] private TimeSlot previewTimeSlot = TimeSlot.Evening;
#if UNITY_EDITOR
    private bool editorRefreshQueued;
#endif
    private readonly HashSet<string> notifiedIssues = new HashSet<string>();

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
            ShowNPCManagerNotificationOnce("missing-db", "NPC 未刷新", "NPC 数据库还没有准备好，当前场景的人物不会正常出现。");
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
                    Vector3? scaleOverride = null;
                    Vector3 position;

                    if (TryGetSceneAnchor(data.id, currentLoc, currentSlot, out NPCSceneAnchor anchor))
                    {
                        position = anchor.transform.position;
                        scaleOverride = anchor.transform.localScale;
                    }
                    else
                    {
                        float x = centerX + (spawnIndex - (newCount - 1) * 0.5f) * PositionOffsetX;
                        x = Mathf.Clamp(x, minX, maxX);
                        position = new Vector3(x, spawnY, 0f);
                        spawnIndex++;
                    }

                    CreateNPCObject(data, position, scaleOverride);
                }
            }
        }
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
    private void CreateNPCObject(NPCData data, Vector3 position, Vector3? scaleOverride = null)
    {
        GameObject npcObj = new GameObject($"NPC_{data.id}");
        npcObj.transform.position = position;

        SpriteRenderer sr = npcObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        NPCController controller = npcObj.AddComponent<NPCController>();
        controller.Initialize(data, scaleOverride);

        activeNPCs[data.id] = controller;
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

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            RefreshEditorSceneAnchors();
            return;
        }

        SetAnchorPreviewVisibility(false);

        // 订阅回合推进事件
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;
        }
        else
        {
            Debug.LogWarning("[NPCManager] TurnManager 实例不存在，无法订阅回合推进事件");
            ShowNPCManagerNotificationOnce("missing-turn-manager", "NPC 时段同步异常", "时间推进系统没有就绪，NPC 的时段刷新可能不会自动更新。");
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

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            QueueEditorPreviewRefresh();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            QueueEditorPreviewRefresh();
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            RefreshEditorSceneAnchors();
        }
    }

    public void RefreshEditorSceneAnchors()
    {
        if (!previewInEditMode)
        {
            SetAnchorPreviewVisibility(false);
            return;
        }

        LocationSceneController sceneController = FindFirstObjectByType<LocationSceneController>();
        if (sceneController == null)
        {
            SetAnchorPreviewVisibility(false);
            return;
        }

        LocationSceneController.LocationSceneProfile profile = sceneController.GetPreviewProfile();
        if (profile == null)
        {
            SetAnchorPreviewVisibility(false);
            return;
        }

        NPCData[] previewNpcs = LoadPreviewNPCData();
        if (previewNpcs == null || previewNpcs.Length == 0)
        {
            SetAnchorPreviewVisibility(false);
            return;
        }

        int previewPlayerGender = Mathf.Clamp(StartupFlowSettings.DefaultPlayerGender, 0, 1);
        TimeSlot activePreviewTimeSlot = ResolveEditorPreviewTimeSlot();
        Transform anchorRoot = GetOrCreateSceneAnchorRoot();
        HashSet<string> visibleAnchorKeys = new HashSet<string>();
        List<NPCData> visibleNpcs = new List<NPCData>();

        for (int i = 0; i < previewNpcs.Length; i++)
        {
            NPCData npc = previewNpcs[i];
            if (npc == null || npc.schedule == null || !npc.IsAvailableForPlayerGender(previewPlayerGender))
            {
                continue;
            }

            for (int j = 0; j < npc.schedule.Length; j++)
            {
                NPCScheduleEntry entry = npc.schedule[j];
                if (entry == null || !System.Enum.TryParse(entry.timeSlot, true, out TimeSlot slot) || slot != activePreviewTimeSlot)
                {
                    continue;
                }

                LocationId? resolved = LocationManager.ResolveScheduleLocation(entry.location);
                if (resolved.HasValue && resolved.Value == profile.locationId)
                {
                    visibleNpcs.Add(npc);
                    break;
                }
            }
        }

        float centerX = (profile.worldMinX + profile.worldMaxX) * 0.5f;
        float minX = profile.worldMinX + 2f;
        float maxX = profile.worldMaxX - 2f;
        float spawnY = profile.spawnY;

        for (int i = 0; i < visibleNpcs.Count; i++)
        {
            NPCData npc = visibleNpcs[i];
            float x = centerX + (i - (visibleNpcs.Count - 1) * 0.5f) * PositionOffsetX;
            x = Mathf.Clamp(x, minX, maxX);
            Vector3 defaultPosition = new Vector3(x, spawnY, 0f);

            bool created;
            NPCSceneAnchor anchor = GetOrCreateSceneAnchor(anchorRoot, npc.id, profile.locationId, activePreviewTimeSlot, out created);
            visibleAnchorKeys.Add(BuildAnchorKey(npc.id, profile.locationId, activePreviewTimeSlot));

            if (created)
            {
                anchor.transform.position = defaultPosition;
                anchor.ApplyDefaultScale(npc);
            }

            anchor.gameObject.SetActive(true);
            anchor.ApplyPreviewVisual(npc);
        }

        SetAnchorPreviewVisibility(true, profile.locationId, activePreviewTimeSlot, visibleAnchorKeys);
    }

    private TimeSlot ResolveEditorPreviewTimeSlot()
    {
        if (!syncPreviewTimeSlotWithStartupState)
        {
            return previewTimeSlot;
        }

        int startupActionPoints = Mathf.Max(0, StartupFlowSettings.InitialActionPoints);
        if (startupActionPoints >= 4) return TimeSlot.Morning;
        if (startupActionPoints >= 2) return TimeSlot.Afternoon;
        return TimeSlot.Evening;
    }

    private void QueueEditorPreviewRefresh()
    {
        if (Application.isPlaying)
        {
            return;
        }

#if UNITY_EDITOR
        if (editorRefreshQueued)
        {
            return;
        }

        editorRefreshQueued = true;
        EditorApplication.delayCall += ProcessQueuedEditorPreviewRefresh;
#endif
    }

#if UNITY_EDITOR
    private void ProcessQueuedEditorPreviewRefresh()
    {
        EditorApplication.delayCall -= ProcessQueuedEditorPreviewRefresh;
        editorRefreshQueued = false;

        if (this == null || Application.isPlaying)
        {
            return;
        }

        RefreshEditorSceneAnchors();
    }
#endif

    private NPCData[] LoadPreviewNPCData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/npc_database");
        if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
        {
            return null;
        }

        NPCPreviewDatabase root = JsonUtility.FromJson<NPCPreviewDatabase>(jsonAsset.text);
        return root != null ? root.npcs : null;
    }

    public Transform GetSceneAnchorRoot()
    {
        return transform.Find(AnchorRootName);
    }

    private Transform GetOrCreateSceneAnchorRoot()
    {
        Transform root = GetSceneAnchorRoot();
        if (root != null)
        {
            return root;
        }

        GameObject rootObject = new GameObject(AnchorRootName);
        rootObject.transform.SetParent(transform, false);
        return rootObject.transform;
    }

    private NPCSceneAnchor GetOrCreateSceneAnchor(Transform anchorRoot, string npcId, LocationId locationId, TimeSlot timeSlot, out bool created)
    {
        Transform locationRoot = GetOrCreateAnchorGroup(anchorRoot, locationId.ToString());
        Transform timeRoot = GetOrCreateAnchorGroup(locationRoot, timeSlot.ToString());

        NPCSceneAnchor[] anchors = timeRoot.GetComponentsInChildren<NPCSceneAnchor>(true);
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null && anchors[i].Matches(npcId, locationId, timeSlot))
            {
                created = false;
                return anchors[i];
            }
        }

        GameObject anchorObject = new GameObject();
        anchorObject.transform.SetParent(timeRoot, false);

        NPCSceneAnchor anchor = anchorObject.AddComponent<NPCSceneAnchor>();
        anchor.Configure(npcId, locationId, timeSlot);
        created = true;
        return anchor;
    }

    private static Transform GetOrCreateAnchorGroup(Transform parent, string groupName)
    {
        Transform group = parent.Find(groupName);
        if (group != null)
        {
            return group;
        }

        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(parent, false);
        return groupObject.transform;
    }

    private bool TryGetSceneAnchor(string npcId, LocationId locationId, TimeSlot timeSlot, out NPCSceneAnchor anchor)
    {
        NPCSceneAnchor[] anchors = GetComponentsInChildren<NPCSceneAnchor>(true);
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null && anchors[i].Matches(npcId, locationId, timeSlot))
            {
                anchor = anchors[i];
                return true;
            }
        }

        anchor = null;
        return false;
    }

    private void SetAnchorPreviewVisibility(bool isVisible, LocationId activeLocationId = LocationId.Dormitory, TimeSlot activeTimeSlot = TimeSlot.Evening, HashSet<string> activeKeys = null)
    {
        Transform root = GetSceneAnchorRoot();
        if (root != null)
        {
            root.gameObject.SetActive(isVisible);
        }

        NPCSceneAnchor[] anchors = GetComponentsInChildren<NPCSceneAnchor>(true);
        for (int i = 0; i < anchors.Length; i++)
        {
            NPCSceneAnchor anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            bool shouldShow = isVisible &&
                anchor.LocationId == activeLocationId &&
                anchor.TimeSlot == activeTimeSlot &&
                (activeKeys == null || activeKeys.Contains(BuildAnchorKey(anchor.NpcId, anchor.LocationId, anchor.TimeSlot)));

            anchor.gameObject.SetActive(shouldShow);
        }
    }

    private static string BuildAnchorKey(string npcId, LocationId locationId, TimeSlot timeSlot)
    {
        return $"{locationId}|{timeSlot}|{npcId}";
    }

    private void Reset()
    {
        if (!Application.isPlaying)
        {
            RefreshEditorSceneAnchors();
        }
    }

    private void ShowNPCManagerNotificationOnce(string key, string title, string message, float duration = 3f)
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
