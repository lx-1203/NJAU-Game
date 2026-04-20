using UnityEngine;
using System;
using System.Collections.Generic;

// ========================================================================
//  天赋系统 —— 天赋点获取、天赋树激活、被动增益效果
//  4大分支（学业/社交/体魄/心境）× 4层，共 44 个天赋节点
//  满级消耗 104 天赋点，预计玩家获取 60~100 天赋点
// ========================================================================

#region 数据模型

public enum TalentBranch
{
    Academic,   // 学业线（智识）
    Social,     // 社交线（人脉）
    Physical,   // 体魄线（意志）
    Mindset     // 心境线（心态）
}

[Serializable]
public class TalentDefinition
{
    public string id;               // 唯一ID, 如 "academic_1_1"
    public string name;             // 显示名
    public string description;      // 效果描述
    public TalentBranch branch;     // 所属分支
    public int layer;               // 层数 (1~4)
    public int cost;                // 天赋点消耗
    public string effectType;       // 效果类型标识 (供其他系统查询)
    public float effectValue;       // 效果数值
}

[Serializable]
public class TalentSaveData
{
    public int availablePoints;
    public List<string> activatedTalentIds = new List<string>();
}

#endregion

/// <summary>
/// 天赋系统 —— 管理天赋点获取、天赋激活、效果查询
/// </summary>
public class TalentSystem : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========
    public static TalentSystem Instance { get; private set; }

    // ========== 事件 ==========
    public event Action OnTalentPointsChanged;
    public event Action<TalentDefinition> OnTalentActivated;

    // ========== 运行时状态 ==========
    private int availablePoints = 0;
    private HashSet<string> activatedTalentIds = new HashSet<string>();
    private List<TalentDefinition> allTalents = new List<TalentDefinition>();

    // ========== 属性 ==========
    public int AvailablePoints => availablePoints;
    public int TotalSpentPoints { get; private set; }

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

        InitializeTalentTree();
    }

    // ========== 天赋树初始化 ==========

    private void InitializeTalentTree()
    {
        allTalents.Clear();

        // ===== 学业线 =====
        // 第一层 (cost=1)
        AddTalent("academic_1_1", "专注阅读", "图书馆自习获得的学力+15%", TalentBranch.Academic, 1, 1, "study_library_bonus", 0.15f);
        AddTalent("academic_1_2", "课堂笔记", "上课获得的学力+15%", TalentBranch.Academic, 1, 1, "study_class_bonus", 0.15f);
        AddTalent("academic_1_3", "考前突击", "期末复习事件的通过率加成+10%", TalentBranch.Academic, 1, 1, "exam_pass_bonus", 0.10f);
        // 第二层 (cost=2)
        AddTalent("academic_2_1", "触类旁通", "自习时相邻学科同步获得30%学力", TalentBranch.Academic, 2, 2, "study_adjacent_bonus", 0.30f);
        AddTalent("academic_2_2", "记忆强化", "考试系统答题容错次数+1", TalentBranch.Academic, 2, 2, "exam_tolerance", 1f);
        AddTalent("academic_2_3", "时间管理", "自习行动点消耗-1（最低1）", TalentBranch.Academic, 2, 2, "study_ap_reduction", 1f);
        // 第三层 (cost=3)
        AddTalent("academic_3_1", "竞赛天赋", "参加学科竞赛时，成功率+20%", TalentBranch.Academic, 3, 3, "competition_bonus", 0.20f);
        AddTalent("academic_3_2", "科研直觉", "解锁「联系导师」特殊选项", TalentBranch.Academic, 3, 3, "unlock_mentor", 1f);
        AddTalent("academic_3_3", "绩点守护", "期末考试时，挂科率额外-15%", TalentBranch.Academic, 3, 3, "exam_fail_reduction", 0.15f);
        // 第四层 (cost=4)
        AddTalent("academic_4_1", "保研预感", "保研相关事件成功率+25%", TalentBranch.Academic, 4, 4, "postgrad_bonus", 0.25f);
        AddTalent("academic_4_2", "学术传承", "担任学习委员或助教时，领导力+2/回合", TalentBranch.Academic, 4, 4, "academic_leadership", 2f);

        // ===== 社交线 =====
        // 第一层 (cost=1)
        AddTalent("social_1_1", "主动搭讪", "主动与NPC对话获得的好感度+15%", TalentBranch.Social, 1, 1, "affinity_gain_bonus", 0.15f);
        AddTalent("social_1_2", "察言观色", "NPC好感度变化时，系统提示倾向性", TalentBranch.Social, 1, 1, "affinity_hint", 1f);
        AddTalent("social_1_3", "朋友圈", "每认识5个NPC，心情上限+2", TalentBranch.Social, 1, 1, "mood_cap_per_npc", 2f);
        // 第二层 (cost=2)
        AddTalent("social_2_1", "礼物精通", "赠送礼物的好感度收益+25%", TalentBranch.Social, 2, 2, "gift_bonus", 0.25f);
        AddTalent("social_2_2", "话题达人", "约会事件成功率+20%", TalentBranch.Social, 2, 2, "date_success_bonus", 0.20f);
        AddTalent("social_2_3", "人情世故", "请求NPC帮助时，好感度消耗-20%", TalentBranch.Social, 2, 2, "help_cost_reduction", 0.20f);
        // 第三层 (cost=3)
        AddTalent("social_3_1", "魅力四射", "魅力值对好感度的加成效果额外+30%", TalentBranch.Social, 3, 3, "charm_affinity_bonus", 0.30f);
        AddTalent("social_3_2", "社交直觉", "可提前感知NPC即将触发的个人事件", TalentBranch.Social, 3, 3, "npc_event_hint", 1f);
        AddTalent("social_3_3", "羁绊加深", "好感度>=80后，每周自动+1好感", TalentBranch.Social, 3, 3, "auto_affinity_gain", 1f);
        // 第四层 (cost=4)
        AddTalent("social_4_1", "一呼百应", "3个好友状态NPC时，领导力获取+30%", TalentBranch.Social, 4, 4, "leadership_friend_bonus", 0.30f);
        AddTalent("social_4_2", "情场高手", "告白成功率+25%，分手负面效果-50%", TalentBranch.Social, 4, 4, "romance_master", 0.25f);

        // ===== 体魄线 =====
        // 第一层 (cost=1)
        AddTalent("physical_1_1", "晨跑习惯", "校园跑每次完成获得的体魄+15%", TalentBranch.Physical, 1, 1, "run_physique_bonus", 0.15f);
        AddTalent("physical_1_2", "体能储备", "体测事件中的策略选择容错+1", TalentBranch.Physical, 1, 1, "pt_tolerance", 1f);
        AddTalent("physical_1_3", "早睡早起", "宿舍休息恢复的心情+15%", TalentBranch.Physical, 1, 1, "sleep_mood_bonus", 0.15f);
        // 第二层 (cost=2)
        AddTalent("physical_2_1", "运动健将", "操场锻炼获得的体魄+25%", TalentBranch.Physical, 2, 2, "exercise_physique_bonus", 0.25f);
        AddTalent("physical_2_2", "意志坚定", "压力增长速度-10%", TalentBranch.Physical, 2, 2, "stress_growth_reduction", 0.10f);
        AddTalent("physical_2_3", "恢复力强", "心情<30时，每回合自动恢复+3", TalentBranch.Physical, 2, 2, "auto_mood_recovery", 3f);
        // 第三层 (cost=3)
        AddTalent("physical_3_1", "引体向上", "体测力量项目必定获得良好以上", TalentBranch.Physical, 3, 3, "pt_strength_guarantee", 1f);
        AddTalent("physical_3_2", "耐力惊人", "体测跑步项目必定获得良好以上", TalentBranch.Physical, 3, 3, "pt_run_guarantee", 1f);
        AddTalent("physical_3_3", "压力转化", "压力>60时，自习效率反而+20%", TalentBranch.Physical, 3, 3, "stress_study_bonus", 0.20f);
        // 第四层 (cost=4)
        AddTalent("physical_4_1", "铁人意志", "压力上限提升至120，>100不触发负面事件", TalentBranch.Physical, 4, 4, "stress_cap_increase", 120f);
        AddTalent("physical_4_2", "体育特招", "解锁「体育特长生」隐藏路线", TalentBranch.Physical, 4, 4, "unlock_sports_route", 1f);

        // ===== 心境线 =====
        // 第一层 (cost=1)
        AddTalent("mindset_1_1", "自我调节", "娱乐活动降低的压力额外+15%", TalentBranch.Mindset, 1, 1, "entertainment_stress_bonus", 0.15f);
        AddTalent("mindset_1_2", "知足常乐", "心情上限提升至110", TalentBranch.Mindset, 1, 1, "mood_cap_increase", 110f);
        AddTalent("mindset_1_3", "及时止损", "负罪感增长速度-10%", TalentBranch.Mindset, 1, 1, "guilt_growth_reduction", 0.10f);
        // 第二层 (cost=2)
        AddTalent("mindset_2_1", "心理疏导", "心理健康值<50时，每回合自动恢复+2", TalentBranch.Mindset, 2, 2, "auto_mental_recovery", 2f);
        AddTalent("mindset_2_2", "问心无愧", "轻度负罪感收益惩罚从8折降至9折", TalentBranch.Mindset, 2, 2, "guilt_penalty_reduction", 0.10f);
        AddTalent("mindset_2_3", "佛系心态", "连续2回合不自习不再增加摆烂值", TalentBranch.Mindset, 2, 2, "no_slacking_penalty", 1f);
        // 第三层 (cost=3)
        AddTalent("mindset_3_1", "情绪隔离", "中度负罪感不再触发额外行动点消耗", TalentBranch.Mindset, 3, 3, "guilt_ap_immunity", 1f);
        AddTalent("mindset_3_2", "自我和解", "负罪感降低行为的收益+30%", TalentBranch.Mindset, 3, 3, "guilt_reduction_bonus", 0.30f);
        AddTalent("mindset_3_3", "抗逆能力", "被负面事件触发时，50%概率免疫", TalentBranch.Mindset, 3, 3, "negative_event_immunity", 0.50f);
        // 第四层 (cost=4)
        AddTalent("mindset_4_1", "心如止水", "负罪感/摆烂值/黑暗值不会进入重度区间", TalentBranch.Mindset, 4, 4, "negative_cap", 1f);
        AddTalent("mindset_4_2", "涅盘重生", "心理健康曾降至0又恢复后，全属性获取+15%", TalentBranch.Mindset, 4, 4, "rebirth_bonus", 0.15f);
    }

    private void AddTalent(string id, string name, string desc, TalentBranch branch, int layer, int cost, string effectType, float effectValue)
    {
        allTalents.Add(new TalentDefinition
        {
            id = id,
            name = name,
            description = desc,
            branch = branch,
            layer = layer,
            cost = cost,
            effectType = effectType,
            effectValue = effectValue
        });
    }

    // ========== 天赋点获取 ==========

    /// <summary>增加天赋点（由外部系统调用）</summary>
    public void AddTalentPoints(int points, string reason = "")
    {
        if (points <= 0) return;
        availablePoints += points;
        Debug.Log($"[TalentSystem] 获得{points}天赋点 ({reason})，当前可用: {availablePoints}");
        OnTalentPointsChanged?.Invoke();
    }

    /// <summary>学期结算时自动发放天赋点（由SemesterSummarySystem调用）</summary>
    public void ProcessSemesterReward(float gpa, string physicalTestGrade, bool cet4PassedThisSemester, bool cet6PassedThisSemester)
    {
        int earned = 0;

        // GPA 奖励: GPA × 2
        int gpaReward = Mathf.FloorToInt(gpa * 2f);
        if (gpaReward > 0)
        {
            earned += gpaReward;
            Debug.Log($"[TalentSystem] GPA {gpa:F2} → +{gpaReward}TP");
        }

        // 体测优秀: +3
        if (physicalTestGrade == "Excellent")
        {
            earned += 3;
            Debug.Log("[TalentSystem] 体测优秀 → +3TP");
        }

        // 四级通过: +3
        if (cet4PassedThisSemester)
        {
            earned += 3;
            Debug.Log("[TalentSystem] 四级通过 → +3TP");
        }

        // 六级通过: +5
        if (cet6PassedThisSemester)
        {
            earned += 5;
            Debug.Log("[TalentSystem] 六级通过 → +5TP");
        }

        if (earned > 0)
        {
            AddTalentPoints(earned, "学期结算");
        }
    }

    /// <summary>成就奖励天赋点</summary>
    public void ProcessAchievementReward(int points)
    {
        if (points > 0)
            AddTalentPoints(points, "成就奖励");
    }

    // ========== 天赋激活 ==========

    /// <summary>检查天赋是否可以激活</summary>
    public bool CanActivateTalent(string talentId)
    {
        if (activatedTalentIds.Contains(talentId)) return false;

        TalentDefinition talent = GetTalent(talentId);
        if (talent == null) return false;
        if (availablePoints < talent.cost) return false;

        // 检查层数解锁：需要该分支已投入足够点数
        int pointsInBranch = GetPointsInBranch(talent.branch);
        int requiredPoints = GetRequiredPointsForLayer(talent.layer);
        return pointsInBranch >= requiredPoints;
    }

    /// <summary>激活天赋</summary>
    public bool ActivateTalent(string talentId)
    {
        if (!CanActivateTalent(talentId))
        {
            Debug.LogWarning($"[TalentSystem] 无法激活天赋: {talentId}");
            return false;
        }

        TalentDefinition talent = GetTalent(talentId);
        availablePoints -= talent.cost;
        TotalSpentPoints += talent.cost;
        activatedTalentIds.Add(talentId);

        Debug.Log($"[TalentSystem] 激活天赋: {talent.name} (消耗{talent.cost}TP, 剩余{availablePoints}TP)");

        OnTalentActivated?.Invoke(talent);
        OnTalentPointsChanged?.Invoke();
        return true;
    }

    /// <summary>重置天赋树（每学期一次，花费500金）</summary>
    public bool ResetTalents()
    {
        if (GameState.Instance == null || GameState.Instance.Money < 500)
        {
            Debug.LogWarning("[TalentSystem] 重置天赋失败：金钱不足500");
            return false;
        }

        // 扣钱
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.Spend(500, TransactionRecord.TransactionType.OtherExpense, "天赋重置");
        else
            GameState.Instance.AddMoney(-500);

        // 退还所有天赋点
        availablePoints += TotalSpentPoints;
        TotalSpentPoints = 0;
        activatedTalentIds.Clear();

        Debug.Log($"[TalentSystem] 天赋树已重置，可用天赋点: {availablePoints}");
        OnTalentPointsChanged?.Invoke();
        return true;
    }

    // ========== 查询 ==========

    /// <summary>获取天赋定义</summary>
    public TalentDefinition GetTalent(string talentId)
    {
        return allTalents.Find(t => t.id == talentId);
    }

    /// <summary>获取所有天赋</summary>
    public List<TalentDefinition> GetAllTalents()
    {
        return allTalents;
    }

    /// <summary>获取指定分支的所有天赋</summary>
    public List<TalentDefinition> GetTalentsByBranch(TalentBranch branch)
    {
        return allTalents.FindAll(t => t.branch == branch);
    }

    /// <summary>天赋是否已激活</summary>
    public bool IsTalentActivated(string talentId)
    {
        return activatedTalentIds.Contains(talentId);
    }

    /// <summary>检查是否拥有指定效果类型的天赋</summary>
    public bool HasTalentEffect(string effectType)
    {
        foreach (string id in activatedTalentIds)
        {
            TalentDefinition t = GetTalent(id);
            if (t != null && t.effectType == effectType) return true;
        }
        return false;
    }

    /// <summary>获取指定效果类型的天赋数值</summary>
    public float GetTalentEffectValue(string effectType)
    {
        foreach (string id in activatedTalentIds)
        {
            TalentDefinition t = GetTalent(id);
            if (t != null && t.effectType == effectType) return t.effectValue;
        }
        return 0f;
    }

    /// <summary>获取某分支已投入的天赋点总数</summary>
    public int GetPointsInBranch(TalentBranch branch)
    {
        int total = 0;
        foreach (string id in activatedTalentIds)
        {
            TalentDefinition t = GetTalent(id);
            if (t != null && t.branch == branch) total += t.cost;
        }
        return total;
    }

    /// <summary>解锁某层所需的分支点数</summary>
    private int GetRequiredPointsForLayer(int layer)
    {
        // 第1层: 0点即可; 第2层: 需1+点; 第3层: 需3+点(1+2); 第4层: 需6+点(1+2+3)
        switch (layer)
        {
            case 1: return 0;
            case 2: return 1;   // 至少激活1个第一层
            case 3: return 3;   // 至少激活1个一层+1个二层
            case 4: return 6;   // 至少激活到第三层
            default: return 0;
        }
    }

    /// <summary>获取某分支已激活的天赋数量</summary>
    public int GetActivatedCountInBranch(TalentBranch branch)
    {
        int count = 0;
        foreach (string id in activatedTalentIds)
        {
            TalentDefinition t = GetTalent(id);
            if (t != null && t.branch == branch) count++;
        }
        return count;
    }

    // ========== ISaveable ==========

    public void SaveToData(SaveData data)
    {
        // 天赋数据存入SaveData的扩展字段
        // 使用eventFlags来存储（复用现有的StringBoolPair）
        // 前缀 "tp_" 标记天赋相关数据
        data.eventFlags.RemoveAll(f => f.key.StartsWith("tp_"));

        // 保存可用点数
        data.eventFlags.Add(new StringBoolPair($"tp_points_{availablePoints}", true));

        // 保存已激活天赋
        foreach (string id in activatedTalentIds)
        {
            data.eventFlags.Add(new StringBoolPair($"tp_active_{id}", true));
        }
    }

    public void LoadFromData(SaveData data)
    {
        availablePoints = 0;
        TotalSpentPoints = 0;
        activatedTalentIds.Clear();

        foreach (var pair in data.eventFlags)
        {
            if (pair.key.StartsWith("tp_points_"))
            {
                string pointStr = pair.key.Substring("tp_points_".Length);
                if (int.TryParse(pointStr, out int pts))
                    availablePoints = pts;
            }
            else if (pair.key.StartsWith("tp_active_"))
            {
                string talentId = pair.key.Substring("tp_active_".Length);
                activatedTalentIds.Add(talentId);
                TalentDefinition t = GetTalent(talentId);
                if (t != null) TotalSpentPoints += t.cost;
            }
        }

        OnTalentPointsChanged?.Invoke();
    }
}
