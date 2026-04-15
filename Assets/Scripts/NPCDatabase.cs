using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NPC 数据库 —— 从 JSON 加载所有 NPC 静态数据和社交行动定义
/// 单例模式，在 GameSceneInitializer 中初始化
/// </summary>
public class NPCDatabase : MonoBehaviour
{
    // ========== 单例 ==========
    public static NPCDatabase Instance { get; private set; }

    // ========== 内部字段 ==========
    private Dictionary<string, NPCData> npcMap = new Dictionary<string, NPCData>();
    private Dictionary<string, SocialActionDefinition> socialActionMap = new Dictionary<string, SocialActionDefinition>();
    private NPCData[] allNPCs;
    private SocialActionDefinition[] allSocialActions;

    // ========== 公共方法 ==========

    /// <summary>获取所有 NPC 数据</summary>
    public NPCData[] GetAllNPCs()
    {
        return allNPCs ?? new NPCData[0];
    }

    /// <summary>根据 ID 获取 NPC 数据</summary>
    public NPCData GetNPC(string npcId)
    {
        npcMap.TryGetValue(npcId, out NPCData data);
        return data;
    }

    /// <summary>获取所有社交行动定义</summary>
    public SocialActionDefinition[] GetAllSocialActions()
    {
        return allSocialActions ?? new SocialActionDefinition[0];
    }

    /// <summary>根据 ID 获取社交行动定义</summary>
    public SocialActionDefinition GetSocialAction(string actionId)
    {
        socialActionMap.TryGetValue(actionId, out SocialActionDefinition def);
        return def;
    }

    /// <summary>
    /// 获取指定 NPC 在给定时间段的出现地点，若不在日程中返回 null
    /// </summary>
    public string GetNPCLocation(string npcId, TimeSlot slot)
    {
        NPCData npc = GetNPC(npcId);
        if (npc == null || npc.schedule == null) return null;

        for (int i = 0; i < npc.schedule.Length; i++)
        {
            if (npc.schedule[i].GetTimeSlot() == slot)
                return npc.schedule[i].location;
        }
        return null;
    }

    // ========== 初始化 ==========

    private void LoadDatabase()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/npc_database");
        if (jsonAsset == null)
        {
            Debug.LogError("[NPCDatabase] 找不到 Resources/Data/npc_database.json");
            allNPCs = new NPCData[0];
            allSocialActions = new SocialActionDefinition[0];
            return;
        }

        NPCDatabaseRoot root = JsonUtility.FromJson<NPCDatabaseRoot>(jsonAsset.text);

        // 建立 NPC 索引
        allNPCs = root.npcs ?? new NPCData[0];
        npcMap.Clear();
        for (int i = 0; i < allNPCs.Length; i++)
        {
            if (!string.IsNullOrEmpty(allNPCs[i].id))
            {
                npcMap[allNPCs[i].id] = allNPCs[i];
            }
        }

        // 建立社交行动索引
        allSocialActions = root.socialActions ?? new SocialActionDefinition[0];
        socialActionMap.Clear();
        for (int i = 0; i < allSocialActions.Length; i++)
        {
            if (!string.IsNullOrEmpty(allSocialActions[i].id))
            {
                socialActionMap[allSocialActions[i].id] = allSocialActions[i];
            }
        }

        Debug.Log($"[NPCDatabase] 加载完成：{allNPCs.Length} 个NPC，{allSocialActions.Length} 种社交行动");
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

        LoadDatabase();
    }
}
