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

    private void EnsureBuiltInSocialActions()
    {
        List<SocialActionDefinition> actions = new List<SocialActionDefinition>(allSocialActions ?? new SocialActionDefinition[0]);

        if (!socialActionMap.ContainsKey("debate"))
        {
            SocialActionDefinition debate = new SocialActionDefinition
            {
                id = "debate",
                displayName = "\u8fa9\u8bba",
                actionPointCost = 2,
                moneyCost = 0,
                minAffinityLevel = "Friend",
                baseAffinityMin = 6,
                baseAffinityMax = 12,
                attributeEffects = new[]
                {
                    new AttributeEffect("\u5b66\u529b", 1),
                    new AttributeEffect("\u9886\u5bfc\u529b", 2),
                    new AttributeEffect("\u538b\u529b", 1)
                }
            };

            actions.Add(debate);
            socialActionMap[debate.id] = debate;
        }

        allSocialActions = actions.ToArray();
    }

    // ========== 公共方法 ==========

    /// <summary>获取所有 NPC 数据</summary>
    public NPCData[] GetAllNPCs()
    {
        NPCData[] source = allNPCs ?? new NPCData[0];
        int playerGender = ResolveCurrentPlayerGender();
        List<NPCData> filtered = new List<NPCData>(source.Length);

        for (int i = 0; i < source.Length; i++)
        {
            NPCData npc = source[i];
            if (npc != null && npc.IsAvailableForPlayerGender(playerGender))
            {
                filtered.Add(npc);
            }
        }

        return filtered.ToArray();
    }

    /// <summary>根据 ID 获取 NPC 数据</summary>
    public NPCData GetNPC(string npcId)
    {
        npcMap.TryGetValue(npcId, out NPCData data);
        if (data == null)
        {
            return null;
        }

        return data.IsAvailableForPlayerGender(ResolveCurrentPlayerGender()) ? data : null;
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
            ShowDatabaseNotification("社交数据缺失", "NPC 与社交行动数据没有加载成功，本局的人际互动会暂时不可用。");
            return;
        }

        NPCDatabaseRoot root = JsonUtility.FromJson<NPCDatabaseRoot>(jsonAsset.text);
        if (root == null)
        {
            Debug.LogError("[NPCDatabase] npc_database.json 解析结果为空");
            allNPCs = new NPCData[0];
            allSocialActions = new SocialActionDefinition[0];
            ShowDatabaseNotification("社交数据损坏", "NPC 数据文件解析失败，本局的人际互动暂时无法正常开启。");
            return;
        }

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

        if (allNPCs.Length == 0)
        {
            ShowDatabaseNotification("没有可互动角色", "当前没有成功载入任何 NPC 数据，这一局的人际关系内容会保持为空。");
        }
        else if (allSocialActions.Length == 0)
        {
            ShowDatabaseNotification("社交行动为空", "NPC 已载入，但社交行动数据为空，互动菜单会缺少可执行选项。");
        }
    }

    private int ResolveCurrentPlayerGender()
    {
        if (GameState.Instance != null)
        {
            return Mathf.Clamp(GameState.Instance.PlayerGender, 0, 1);
        }

        if (SaveManager.PendingLoadData != null)
        {
            return Mathf.Clamp(SaveManager.PendingLoadData.playerGender, 0, 1);
        }

        if (CharacterCreationUI.HasPendingCharacter)
        {
            return Mathf.Clamp(CharacterCreationUI.PendingPlayerGender, 0, 1);
        }

        return Mathf.Clamp(StartupFlowSettings.DefaultPlayerGender, 0, 1);
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
        EnsureBuiltInSocialActions();
    }

    private void ShowDatabaseNotification(string title, string message, float duration = 3f)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, new Color(0.82f, 0.38f, 0.30f), duration);
        }
    }
}
