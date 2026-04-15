using UnityEngine;
using System;
using System.IO;

/// <summary>
/// 多周目传承管理器 —— 管理跨周目数据的记录、存储和继承应用
/// 独立于普通存档，数据存储在 persistentDataPath/newgameplus.json
/// </summary>
public class NewGamePlusManager : MonoBehaviour
{
    // ========== 单例 ==========

    public static NewGamePlusManager Instance { get; private set; }

    // ========== 数据 ==========

    private NewGamePlusData data;
    private string filePath;

    /// <summary>当前多周目传承数据</summary>
    public NewGamePlusData Data => data;

    /// <summary>是否存在多周目数据（至少完成过一次游戏）</summary>
    public bool HasNewGamePlusData => data != null && data.cycleCount > 1;

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

        filePath = Path.Combine(Application.persistentDataPath, "newgameplus.json");
        LoadData();
    }

    // ========== 公共方法 ==========

    /// <summary>
    /// 记录本周目终值（游戏结束/毕业时调用）
    /// </summary>
    /// <param name="endingId">本周目达成的结局ID</param>
    public void RecordEndOfCycle(string endingId)
    {
        if (data == null)
        {
            data = new NewGamePlusData();
        }

        // 快照当前 GameState
        if (GameState.Instance != null)
        {
            data.lastMoney = GameState.Instance.Money;
        }

        // 快照当前 PlayerAttributes
        if (PlayerAttributes.Instance != null)
        {
            data.lastStudy = PlayerAttributes.Instance.Study;
            data.lastCharm = PlayerAttributes.Instance.Charm;
            data.lastPhysique = PlayerAttributes.Instance.Physique;
            data.lastLeadership = PlayerAttributes.Instance.Leadership;
            data.lastStress = PlayerAttributes.Instance.Stress;
            data.lastMood = PlayerAttributes.Instance.Mood;
        }

        data.lastEnding = endingId ?? "";

        // 快照 NPC 关系数据
        data.lastRelationships.Clear();
        if (NPCManager.Instance != null)
        {
            var allRelationships = NPCManager.Instance.GetAllRelationships();
            if (allRelationships != null)
            {
                foreach (var rd in allRelationships)
                {
                    data.lastRelationships.Add(NPCRelationshipSaveData.FromRuntime(rd));
                }
            }
        }

        // 递增周目数
        data.cycleCount++;

        Debug.Log($"[NewGamePlus] 记录周目结束: 结局={endingId}, 即将进入第{data.cycleCount}周目");

        // 保存到文件
        SaveData();
    }

    /// <summary>
    /// 在新游戏开始时应用传承加成
    /// </summary>
    public void ApplyInheritance()
    {
        if (data == null || data.cycleCount <= 1)
        {
            Debug.Log("[NewGamePlus] 首周目，无传承数据可应用");
            return;
        }

        int nextCycle = data.cycleCount;
        float inheritRate = NewGamePlusData.GetInheritRate(nextCycle);

        Debug.Log($"[NewGamePlus] 应用第{nextCycle}周目传承 (继承率: {inheritRate:P0})");

        // 属性传承
        if (PlayerAttributes.Instance != null)
        {
            int study = NewGamePlusData.CalcInheritedAttribute(10, data.lastStudy, inheritRate);
            int charm = NewGamePlusData.CalcInheritedAttribute(5, data.lastCharm, inheritRate);
            int physique = NewGamePlusData.CalcInheritedAttribute(8, data.lastPhysique, inheritRate);
            int leadership = NewGamePlusData.CalcInheritedAttribute(3, data.lastLeadership, inheritRate);

            // 压力和心情使用固定初始值（不继承负面状态）
            PlayerAttributes.Instance.SetAll(study, charm, physique, leadership, 20, 70);

            Debug.Log($"[NewGamePlus] 传承属性: 学力={study}, 魅力={charm}, 体魄={physique}, 领导力={leadership}");
        }

        // 金钱传承
        if (GameState.Instance != null)
        {
            int inheritedMoney = NewGamePlusData.CalcInheritedMoney(data.lastMoney, nextCycle);
            GameState.Instance.Money = inheritedMoney;

            Debug.Log($"[NewGamePlus] 传承金钱: ¥{inheritedMoney}");
        }

        // NPC 好感度传承
        if (NPCManager.Instance != null && data.lastRelationships != null)
        {
            foreach (var savedRel in data.lastRelationships)
            {
                bool wasLover = savedRel.romanceState == "Lover" || savedRel.romanceState == "Married";
                int inheritedAffinity = NewGamePlusData.CalcInheritedAffinity(savedRel.affinity, wasLover);

                if (inheritedAffinity > 0)
                {
                    var currentRel = NPCManager.Instance.GetRelationship(savedRel.npcId);
                    if (currentRel != null)
                    {
                        currentRel.affinity = inheritedAffinity;
                        Debug.Log($"[NewGamePlus] NPC {savedRel.npcId} 传承好感度: {inheritedAffinity}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查指定功能在当前周目是否解锁
    /// </summary>
    public bool IsFeatureUnlocked(string key)
    {
        if (data == null) return false;
        return data.IsFeatureUnlocked(key);
    }

    // ========== 内部方法 ==========

    /// <summary>
    /// 从文件加载多周目数据
    /// </summary>
    private void LoadData()
    {
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                data = JsonUtility.FromJson<NewGamePlusData>(json);
                Debug.Log($"[NewGamePlus] 加载成功: 当前第{data.cycleCount}周目");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NewGamePlus] 加载失败: {e.Message}");
                data = new NewGamePlusData();
            }
        }
        else
        {
            data = new NewGamePlusData();
            Debug.Log("[NewGamePlus] 无多周目数据文件，初始化为首周目");
        }
    }

    /// <summary>
    /// 保存多周目数据到文件
    /// </summary>
    private void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[NewGamePlus] 保存成功: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NewGamePlus] 保存失败: {e.Message}");
        }
    }
}
