using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 地点管理器 —— 管理校园地点数据、导航逻辑、事件广播
/// 单例模式，DontDestroyOnLoad
/// </summary>
public class LocationManager : MonoBehaviour
{
    // ========== 单例 ==========
    public static LocationManager Instance { get; private set; }

    // ========== 事件 ==========
    /// <summary>地点切换完成后触发，参数为 (旧地点, 新地点)</summary>
    public event Action<LocationId, LocationId> OnLocationChanged;

    // ========== 内部数据 ==========
    private Dictionary<LocationId, LocationDefinition> locationDefs = new Dictionary<LocationId, LocationDefinition>();
    private List<LocationLink> locationLinks = new List<LocationLink>();

    // NPC 地点配置（静态，后续由 NPCScheduleManager 动态管理）
    private Dictionary<LocationId, List<string>> npcAtLocation = new Dictionary<LocationId, List<string>>();

    // ========== 初始化 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitDefaultLocations();
        InitDefaultLinks();
        InitDefaultNPCs();
    }

    private void Start()
    {
        // 订阅回合推进事件，用于刷新 NPC 位置等
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
        }
    }

    // ========== 默认数据初始化 ==========

    private void InitDefaultLocations()
    {
        locationDefs.Clear();

        // 宿舍
        Register(new LocationDefinition(
            LocationId.Dormitory,
            "宿舍", "你的大学宿舍，温馨的小窝。可以休息、社交或打游戏。",
            "🏠", new Vector2(0.55f, 0.45f),
            new string[] { "sleep", "social", "play_game" },
            new LocationId[] { LocationId.Canteen, LocationId.Playground, LocationId.TeachingBuilding }
        ));

        // 教学楼
        Register(new LocationDefinition(
            LocationId.TeachingBuilding,
            "教学楼", "课程和考试的主要场所。上课是大学生活的核心。",
            "🏫", new Vector2(0.30f, 0.75f),
            new string[] { "attend_class", "study" },
            new LocationId[] { LocationId.Library, LocationId.Canteen, LocationId.Dormitory },
            "08:00-22:00"
        ));

        // 图书馆
        Register(new LocationDefinition(
            LocationId.Library,
            "图书馆", "安静的学习圣地。自习效率最高的地方。",
            "📚", new Vector2(0.55f, 0.80f),
            new string[] { "study" },
            new LocationId[] { LocationId.TeachingBuilding, LocationId.Playground },
            "07:00-22:30"
        ));

        // 食堂
        Register(new LocationDefinition(
            LocationId.Canteen,
            "食堂", "各种美食应有尽有。吃饭是每天的固定活动。",
            "🍜", new Vector2(0.30f, 0.45f),
            new string[] { "eat" },
            new LocationId[] { LocationId.TeachingBuilding, LocationId.Dormitory, LocationId.Store },
            "06:30-21:00"
        ));

        // 操场
        Register(new LocationDefinition(
            LocationId.Playground,
            "操场", "跑步、锻炼和体测的场所。保持健康很重要！",
            "🏃", new Vector2(0.75f, 0.65f),
            new string[] { "exercise", "sports_test" },
            new LocationId[] { LocationId.Dormitory, LocationId.Library }
        ));

        // 教超
        Register(new LocationDefinition(
            LocationId.Store,
            "教超", "校内超市，日用品和零食的天堂。",
            "🏪", new Vector2(0.15f, 0.25f),
            new string[] { "shop" },
            new LocationId[] { LocationId.Canteen, LocationId.ExpressStation }
        ));

        // 快递站
        Register(new LocationDefinition(
            LocationId.ExpressStation,
            "快递站", "收发快递的地方。网购到货别忘了取！",
            "📦", new Vector2(0.40f, 0.15f),
            new string[] { "pickup_express" },
            new LocationId[] { LocationId.Store, LocationId.TakeoutStation }
        ));

        // 外卖站
        Register(new LocationDefinition(
            LocationId.TakeoutStation,
            "外卖站", "点外卖的取餐点。懒得去食堂时的救星。",
            "🛵", new Vector2(0.65f, 0.15f),
            new string[] { "order_takeout" },
            new LocationId[] { LocationId.ExpressStation, LocationId.Dormitory }
        ));
    }

    private void Register(LocationDefinition def)
    {
        locationDefs[def.id] = def;
    }

    private void InitDefaultLinks()
    {
        locationLinks.Clear();

        // 从邻接关系自动生成连接（去重）
        HashSet<string> added = new HashSet<string>();
        foreach (var def in locationDefs.Values)
        {
            foreach (var adj in def.adjacentLocations)
            {
                string key = GetLinkKey(def.id, adj);
                if (!added.Contains(key))
                {
                    locationLinks.Add(new LocationLink(def.id, adj));
                    added.Add(key);
                }
            }
        }
    }

    private string GetLinkKey(LocationId a, LocationId b)
    {
        // 确保双向连接只生成一条
        return a < b ? $"{a}-{b}" : $"{b}-{a}";
    }

    private void InitDefaultNPCs()
    {
        npcAtLocation.Clear();

        // 初始 NPC 分布（静态配置，后续由 NPCScheduleManager 动态管理）
        SetNPCs(LocationId.Dormitory, new string[] { "室友" });
        SetNPCs(LocationId.TeachingBuilding, new string[] { "老师" });
        SetNPCs(LocationId.Library, new string[] { "学长" });
        SetNPCs(LocationId.Canteen, new string[] { "同学" });
    }

    private void SetNPCs(LocationId loc, string[] names)
    {
        npcAtLocation[loc] = new List<string>(names);
    }

    // ========== 查询 API ==========

    /// <summary>根据 ID 获取地点定义</summary>
    public LocationDefinition GetLocation(LocationId id)
    {
        locationDefs.TryGetValue(id, out LocationDefinition def);
        return def;
    }

    /// <summary>获取所有地点定义</summary>
    public LocationDefinition[] GetAllLocations()
    {
        LocationDefinition[] result = new LocationDefinition[locationDefs.Count];
        int i = 0;
        foreach (var def in locationDefs.Values)
        {
            result[i++] = def;
        }
        return result;
    }

    /// <summary>获取所有地点连接（用于绘制连线）</summary>
    public List<LocationLink> GetAllLinks()
    {
        return locationLinks;
    }

    /// <summary>获取当前所在地点的定义</summary>
    public LocationDefinition GetCurrentLocationDef()
    {
        if (GameState.Instance == null) return null;
        return GetLocation(GameState.Instance.CurrentLocation);
    }

    /// <summary>判断两个地点是否相邻</summary>
    public bool IsAdjacent(LocationId a, LocationId b)
    {
        LocationDefinition defA = GetLocation(a);
        if (defA == null) return false;

        for (int i = 0; i < defA.adjacentLocations.Length; i++)
        {
            if (defA.adjacentLocations[i] == b)
                return true;
        }
        return false;
    }

    /// <summary>获取从 from 到 to 的移动 AP 消耗（相邻0, 远距离1）</summary>
    public int GetMoveCost(LocationId from, LocationId to)
    {
        if (from == to) return 0;
        return IsAdjacent(from, to) ? 0 : 1;
    }

    /// <summary>获取指定地点的可用行动列表</summary>
    public ActionDefinition[] GetAvailableActions(LocationId locationId)
    {
        LocationDefinition locDef = GetLocation(locationId);
        if (locDef == null || ActionSystem.Instance == null)
            return new ActionDefinition[0];

        List<ActionDefinition> result = new List<ActionDefinition>();

        // 添加该地点定义的行动
        foreach (string actionId in locDef.availableActionIds)
        {
            ActionDefinition action = ActionSystem.Instance.GetAction(actionId);
            if (action != null)
            {
                result.Add(action);
            }
        }

        // 添加全局行动（如睡觉，只在宿舍以外也保留）
        ActionDefinition[] allActions = ActionSystem.Instance.GetAllActions();
        foreach (var action in allActions)
        {
            if (action.isGlobal && !result.Contains(action))
            {
                result.Add(action);
            }
        }

        return result.ToArray();
    }

    /// <summary>获取指定地点当前的 NPC 列表</summary>
    public string[] GetNPCsAtLocation(LocationId locationId)
    {
        if (npcAtLocation.TryGetValue(locationId, out List<string> names))
        {
            return names.ToArray();
        }
        return new string[0];
    }

    // ========== 导航 API ==========

    /// <summary>检查是否可以移动到目标地点（AP 足够 + 不在同一地点）</summary>
    public bool CanMoveTo(LocationId target)
    {
        if (GameState.Instance == null) return false;

        LocationId current = GameState.Instance.CurrentLocation;
        if (current == target) return false;

        int cost = GetMoveCost(current, target);
        return GameState.Instance.ActionPoints >= cost;
    }

    /// <summary>
    /// 执行移动到目标地点：扣除 AP、更新 GameState、触发事件
    /// </summary>
    public bool MoveTo(LocationId target)
    {
        if (!CanMoveTo(target))
        {
            Debug.LogWarning($"[LocationManager] 无法移动到 {target}");
            return false;
        }

        GameState gs = GameState.Instance;
        LocationId from = gs.CurrentLocation;
        int cost = GetMoveCost(from, target);

        // 扣除行动点
        if (cost > 0)
        {
            gs.ConsumeActionPoint(cost);
        }

        // 更新当前地点
        gs.CurrentLocation = target;

        Debug.Log($"[LocationManager] 移动: {from} → {target}, 消耗 {cost} AP");

        // 触发事件
        OnLocationChanged?.Invoke(from, target);

        return true;
    }

    // ========== 回合刷新 ==========

    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        // 回合推进时刷新 NPC 分布（目前为静态配置，后续可接入 NPCScheduleManager）
        Debug.Log("[LocationManager] 回合推进，刷新地点状态");
    }
}
