using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 社团系统 —— 管理社团加入/退出、活动执行、晋升体系、入党流程
/// 不走 ActionSystem，独立处理行动点与属性变化
/// 实现 ISaveable 接口以支持存档/读档
/// </summary>
public class ClubSystem : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========
    public static ClubSystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>社团相关状态发生变化时触发（通用刷新）</summary>
    public event Action OnClubStateChanged;

    /// <summary>加入社团时触发，参数为 clubId</summary>
    public event Action<string> OnClubJoined;

    /// <summary>退出社团时触发，参数为 clubId</summary>
    public event Action<string> OnClubLeft;

    /// <summary>社团晋升时触发，参数为 clubId 和新等级</summary>
    public event Action<string, int> OnPromoted;

    /// <summary>入党阶段变化时触发，参数为新阶段序号</summary>
    public event Action<int> OnPartyStageChanged;

    // ========== 常量 ==========

    /// <summary>占名额社团的最大加入数量</summary>
    private const int MaxSlotClubs = 2;

    /// <summary>退出社团后重新加入的冷却回合数</summary>
    private const int ExitCooldownRounds = 2;

    /// <summary>连续未活动多少回合触发被动退出</summary>
    private const int InactiveKickRounds = 5;

    /// <summary>退出社团的领导力惩罚</summary>
    private const int LeaveLeadershipPenalty = 5;

    /// <summary>退出学生会的额外领导力惩罚</summary>
    private const int LeaveStudentUnionExtraPenalty = 10;

    /// <summary>被动退出的压力惩罚</summary>
    private const int InactiveKickStressPenalty = 5;

    // ========== 数据字典（从 JSON 加载） ==========

    private Dictionary<string, ClubDefinition> clubDict = new Dictionary<string, ClubDefinition>();
    private Dictionary<string, PromotionPath> promotionPathDict = new Dictionary<string, PromotionPath>();
    private PartyMembershipStage[] partyMembershipStages;
    private PartyRequirements partyRequirements;

    // ========== 运行时状态 ==========

    private List<ClubMembership> joinedClubs = new List<ClubMembership>();
    private int currentPartyStage = 0;
    private int partyApplicationRound = 0;

    /// <summary>每回合每社团活动计数 (clubId → 本回合已活动次数)</summary>
    private Dictionary<string, int> roundActivityCount = new Dictionary<string, int>();

    /// <summary>退出冷却记录 (clubId → 剩余冷却回合数)</summary>
    private Dictionary<string, int> exitCooldowns = new Dictionary<string, int>();

    /// <summary>连续未活动回合计数 (clubId → 连续未活动回合数)</summary>
    private Dictionary<string, int> inactiveRounds = new Dictionary<string, int>();

    /// <summary>本回合已活动的社团集合（用于不活跃追踪）</summary>
    private HashSet<string> activeThisRound = new HashSet<string>();

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

        LoadClubData();
    }

    // ========== 数据加载 ==========

    /// <summary>从 Resources/Data/ClubData.json 加载社团配置数据</summary>
    private void LoadClubData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/ClubData");
        if (jsonAsset == null)
        {
            Debug.LogError("[ClubSystem] 无法加载 Resources/Data/ClubData.json");
            return;
        }

        ClubDataWrapper wrapper = JsonUtility.FromJson<ClubDataWrapper>(jsonAsset.text);
        if (wrapper == null)
        {
            Debug.LogError("[ClubSystem] ClubData.json 反序列化失败");
            return;
        }

        // 构建社团字典
        clubDict.Clear();
        if (wrapper.clubs != null)
        {
            for (int i = 0; i < wrapper.clubs.Length; i++)
            {
                ClubDefinition club = wrapper.clubs[i];
                clubDict[club.id] = club;
            }
        }

        // 构建晋升路径字典
        promotionPathDict.Clear();
        if (wrapper.promotionPaths != null)
        {
            for (int i = 0; i < wrapper.promotionPaths.Length; i++)
            {
                PromotionPath path = wrapper.promotionPaths[i];
                promotionPathDict[path.pathName] = path;
            }
        }

        // 入党阶段与条件
        partyMembershipStages = wrapper.partyMembershipStages;
        partyRequirements = wrapper.partyRequirements;

        Debug.Log($"[ClubSystem] 数据加载完成: {clubDict.Count} 个社团, " +
                  $"{promotionPathDict.Count} 条晋升路径, " +
                  $"{(partyMembershipStages != null ? partyMembershipStages.Length : 0)} 个入党阶段");
    }

    // ========== 数据查询 ==========

    /// <summary>获取所有社团定义列表</summary>
    public List<ClubDefinition> GetAllClubs()
    {
        return new List<ClubDefinition>(clubDict.Values);
    }

    /// <summary>根据 ID 获取社团定义，找不到返回 null</summary>
    public ClubDefinition GetClub(string clubId)
    {
        clubDict.TryGetValue(clubId, out ClubDefinition club);
        return club;
    }

    /// <summary>获取已加入社团的成员状态列表（副本）</summary>
    public List<ClubMembership> GetJoinedClubs()
    {
        return new List<ClubMembership>(joinedClubs);
    }

    /// <summary>获取指定社团的成员状态，未加入返回 null</summary>
    public ClubMembership GetMembership(string clubId)
    {
        for (int i = 0; i < joinedClubs.Count; i++)
        {
            if (joinedClubs[i].clubId == clubId)
                return joinedClubs[i];
        }
        return null;
    }

    /// <summary>是否已加入指定社团</summary>
    public bool IsInClub(string clubId)
    {
        return GetMembership(clubId) != null;
    }

    /// <summary>获取已加入的占名额社团数量（仅 occupiesSlot=true）</summary>
    public int GetJoinedSlotCount()
    {
        int count = 0;
        for (int i = 0; i < joinedClubs.Count; i++)
        {
            ClubDefinition club = GetClub(joinedClubs[i].clubId);
            if (club != null && club.occupiesSlot)
                count++;
        }
        return count;
    }

    // ========== 社团加入/退出 ==========

    /// <summary>
    /// 检查是否可以加入指定社团
    /// 条件: 社团存在 + 未加入 + 名额未满 + 属性门槛 + 冷却期结束
    /// </summary>
    public bool CanJoinClub(string clubId)
    {
        return string.IsNullOrEmpty(GetJoinBlockReason(clubId));
    }

    /// <summary>返回不可加入社团的原因，可加入则返回空字符串</summary>
    public string GetJoinBlockReason(string clubId)
    {
        ClubDefinition club = GetClub(clubId);
        if (club == null)
            return "社团不存在";

        if (IsInClub(clubId))
            return "已加入该社团";

        if (club.occupiesSlot && GetJoinedSlotCount() >= MaxSlotClubs)
            return "社团名额已满（最多2个）";

        // 退出冷却期检查
        if (exitCooldowns.TryGetValue(clubId, out int cooldown) && cooldown > 0)
            return $"退出冷却中（剩余{cooldown}回合）";

        // 属性门槛检查（支持 OR 逻辑）
        if (club.joinRequirements != null && club.joinRequirements.Length > 0)
        {
            // 按 logic 分组: OR 组中任一满足即可，AND 组全部必须满足
            bool hasOrGroup = false;
            bool anyOrMet = false;
            List<string> andFailReasons = new List<string>();

            for (int i = 0; i < club.joinRequirements.Length; i++)
            {
                JoinRequirement req = club.joinRequirements[i];
                int currentVal = GetAttributeValue(req.attributeName);
                bool met = currentVal >= req.minValue;

                if (req.logic == "OR")
                {
                    hasOrGroup = true;
                    if (met) anyOrMet = true;
                }
                else // AND (默认)
                {
                    if (!met)
                        andFailReasons.Add($"{req.attributeName}≥{req.minValue}（当前{currentVal}）");
                }
            }

            // AND 条件有未满足的
            if (andFailReasons.Count > 0)
                return "属性不足: " + string.Join(", ", andFailReasons);

            // OR 组存在但全部不满足
            if (hasOrGroup && !anyOrMet)
            {
                List<string> orDescs = new List<string>();
                for (int i = 0; i < club.joinRequirements.Length; i++)
                {
                    if (club.joinRequirements[i].logic == "OR")
                        orDescs.Add($"{club.joinRequirements[i].attributeName}≥{club.joinRequirements[i].minValue}");
                }
                return "需满足其一: " + string.Join(" 或 ", orDescs);
            }
        }

        return "";
    }

    /// <summary>加入社团，创建成员状态并更新职务行动点扣减</summary>
    public void JoinClub(string clubId)
    {
        if (!CanJoinClub(clubId))
        {
            Debug.LogWarning($"[ClubSystem] 无法加入社团: {clubId}, 原因: {GetJoinBlockReason(clubId)}");
            return;
        }

        ClubMembership membership = new ClubMembership(clubId, GetGlobalRound());
        joinedClubs.Add(membership);

        // 初始化不活跃计数
        inactiveRounds[clubId] = 0;

        UpdatePositionAPCost();

        ClubDefinition club = GetClub(clubId);
        Debug.Log($"[ClubSystem] 加入社团: {club.name} (id={clubId})");

        OnClubJoined?.Invoke(clubId);
        OnClubStateChanged?.Invoke();
    }

    /// <summary>检查是否可以退出指定社团（官方组织不可退出）</summary>
    public bool CanLeaveClub(string clubId)
    {
        if (!IsInClub(clubId))
            return false;

        ClubDefinition club = GetClub(clubId);
        if (club == null)
            return false;

        // 官方组织（校团委/党建班）不可主动退出
        if (club.isOfficial)
            return false;

        return true;
    }

    /// <summary>
    /// 退出社团: 清除成员状态 + 退出惩罚 + 记录冷却期
    /// </summary>
    public void LeaveClub(string clubId)
    {
        ClubMembership membership = GetMembership(clubId);
        if (membership == null)
        {
            Debug.LogWarning($"[ClubSystem] 未加入该社团，无法退出: {clubId}");
            return;
        }

        ClubDefinition club = GetClub(clubId);

        // 官方组织不可退出
        if (club != null && club.isOfficial)
        {
            Debug.LogWarning($"[ClubSystem] 官方组织不可主动退出: {club.name}");
            return;
        }

        joinedClubs.Remove(membership);

        // 退出惩罚: 领导力-5
        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.AddAttribute("领导力", -LeaveLeadershipPenalty);
            Debug.Log($"[ClubSystem] 退出惩罚: 领导力-{LeaveLeadershipPenalty}");

            // 退出学生会额外惩罚: 领导力再-10
            if (clubId == "student_union")
            {
                PlayerAttributes.Instance.AddAttribute("领导力", -LeaveStudentUnionExtraPenalty);
                Debug.Log($"[ClubSystem] 学生会额外惩罚: 领导力-{LeaveStudentUnionExtraPenalty}");
            }
        }

        // 记录退出冷却期
        exitCooldowns[clubId] = ExitCooldownRounds;

        // 清理不活跃计数
        inactiveRounds.Remove(clubId);

        UpdatePositionAPCost();

        Debug.Log($"[ClubSystem] 退出社团: {(club != null ? club.name : clubId)} (id={clubId}), 冷却{ExitCooldownRounds}回合");

        OnClubLeft?.Invoke(clubId);
        OnClubStateChanged?.Invoke();
    }

    // ========== 社团活动 ==========

    /// <summary>查询本回合是否已在指定社团活动过</summary>
    public bool HasActivityThisRound(string clubId)
    {
        return roundActivityCount.TryGetValue(clubId, out int count) && count > 0;
    }

    /// <summary>
    /// 检查是否可以执行社团活动
    /// 条件: 已加入 + 本回合未活动 + 行动点足够 + 金钱足够
    /// </summary>
    public bool CanDoClubActivity(string clubId)
    {
        if (!IsInClub(clubId))
            return false;

        ClubDefinition club = GetClub(clubId);
        if (club == null)
            return false;

        // 每社团每回合最多活动1次
        if (HasActivityThisRound(clubId))
            return false;

        GameState gs = GameState.Instance;
        if (gs == null)
            return false;

        if (gs.ActionPoints < club.activityAPCost)
            return false;

        if (gs.Money < club.activityMoneyCost)
            return false;

        return true;
    }

    /// <summary>
    /// 执行社团活动: 扣行动点 → 扣金钱(走EconomyManager) → 应用属性效果
    ///   → 推进事件链 → 记录活动次数 → NPC好感联动 → 触发事件
    /// </summary>
    public void DoClubActivity(string clubId)
    {
        if (!CanDoClubActivity(clubId))
        {
            Debug.LogWarning($"[ClubSystem] 无法执行社团活动: {clubId}");
            return;
        }

        ClubDefinition club = GetClub(clubId);
        ClubMembership membership = GetMembership(clubId);
        GameState gs = GameState.Instance;

        // 1. 扣除行动点
        gs.ConsumeActionPoint(club.activityAPCost);

        // 2. 扣除金钱（通过 EconomyManager 记录交易流水）
        if (club.activityMoneyCost > 0)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Spend(club.activityMoneyCost,
                    TransactionRecord.TransactionType.DailyNecessities,
                    $"社团活动: {club.name}");
            }
            else
            {
                gs.AddMoney(-club.activityMoneyCost);
            }
        }

        // 3. 应用属性效果
        if (club.activityEffects != null && PlayerAttributes.Instance != null)
        {
            for (int i = 0; i < club.activityEffects.Length; i++)
            {
                AttributeEffect effect = club.activityEffects[i];
                PlayerAttributes.Instance.AddAttribute(effect.attributeName, effect.amount);
            }
        }

        // 4. 推进事件链（带上限保护）
        if (club.eventChainIds != null && membership.eventChainProgress < club.eventChainIds.Length)
        {
            membership.eventChainProgress++;
        }

        // 5. 记录本回合活动次数
        if (roundActivityCount.ContainsKey(clubId))
            roundActivityCount[clubId]++;
        else
            roundActivityCount[clubId] = 1;

        // 6. 标记本回合已活动（用于不活跃追踪）
        activeThisRound.Add(clubId);

        // 7. NPC 好感度联动: 同社团NPC好感+3~5
        if (!string.IsNullOrEmpty(club.npcId) && AffinitySystem.Instance != null)
        {
            NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(club.npcId);
            if (rel != null)
            {
                int oldAffinity = rel.affinity;
                int bonus = UnityEngine.Random.Range(3, 6); // 3~5
                rel.affinity = Mathf.Clamp(rel.affinity + bonus, 0, 100);
                Debug.Log($"[ClubSystem] NPC好感联动: {club.npcId} 好感 {oldAffinity} → {rel.affinity} (+{bonus})");
            }
        }

        // 8. 日志
        Debug.Log($"[ClubSystem] 执行社团活动: {club.name} (id={clubId}), " +
                  $"消耗AP={club.activityAPCost}, 消耗金钱={club.activityMoneyCost}, " +
                  $"事件链进度={membership.eventChainProgress}");

        // 9. 触发事件
        OnClubStateChanged?.Invoke();
    }

    // ========== 晋升系统 ==========

    /// <summary>获取指定社团的当前职务等级，未加入返回 null</summary>
    public PromotionRank GetCurrentRank(string clubId)
    {
        ClubMembership membership = GetMembership(clubId);
        if (membership == null)
            return null;

        ClubDefinition club = GetClub(clubId);
        if (club == null)
            return null;

        if (!promotionPathDict.TryGetValue(club.promotionPath, out PromotionPath path))
            return null;

        if (path.ranks == null || membership.currentRank >= path.ranks.Length)
            return null;

        return path.ranks[membership.currentRank];
    }

    /// <summary>获取指定社团的下一职务等级，已满级或未加入返回 null</summary>
    public PromotionRank GetNextRank(string clubId)
    {
        ClubMembership membership = GetMembership(clubId);
        if (membership == null)
            return null;

        ClubDefinition club = GetClub(clubId);
        if (club == null)
            return null;

        if (!promotionPathDict.TryGetValue(club.promotionPath, out PromotionPath path))
            return null;

        int nextRankIndex = membership.currentRank + 1;
        if (path.ranks == null || nextRankIndex >= path.ranks.Length)
            return null;

        return path.ranks[nextRankIndex];
    }

    /// <summary>
    /// 检查是否满足晋升条件
    /// 条件: 有下一级 + 社团内回合数足够 + 属性要求满足
    /// </summary>
    public bool CanPromote(string clubId)
    {
        ClubMembership membership = GetMembership(clubId);
        if (membership == null)
            return false;

        PromotionRank nextRank = GetNextRank(clubId);
        if (nextRank == null)
            return false;

        // 检查回合数
        if (membership.roundsInClub < nextRank.requiredRounds)
            return false;

        // 检查属性要求
        if (nextRank.requiredAttributes != null)
        {
            for (int i = 0; i < nextRank.requiredAttributes.Length; i++)
            {
                AttributeRequirement req = nextRank.requiredAttributes[i];
                if (GetAttributeValue(req.attributeName) < req.minValue)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 尝试晋升（概率机制）
    /// 成功率 = 基础50% + 相关属性系数 + 在社回合加成(每回合+1%,上限20%) + 领导力加成(每点0.3%)
    /// 竞选高级职位(rank≥2)成功率降低20%
    /// </summary>
    public void TryPromote(string clubId)
    {
        if (!CanPromote(clubId))
            return;

        ClubMembership membership = GetMembership(clubId);
        PromotionRank nextRank = GetNextRank(clubId);
        ClubDefinition club = GetClub(clubId);
        if (club == null) return;

        // ---- 计算晋升成功率 ----
        float baseRate = 50f;

        // 在社回合加成: 每超出要求1回合 +1%, 最多 +20%
        int extraRounds = Mathf.Max(0, membership.roundsInClub - nextRank.requiredRounds);
        float roundBonus = Mathf.Min(extraRounds * 1f, 20f);

        // 领导力加成: 每点 +0.3%
        float leadershipBonus = 0f;
        if (PlayerAttributes.Instance != null)
        {
            leadershipBonus = PlayerAttributes.Instance.Leadership * 0.3f;
        }

        // 属性门槛超出加成: 每超出要求10点 +5%
        float attrOverBonus = 0f;
        if (nextRank.requiredAttributes != null)
        {
            for (int i = 0; i < nextRank.requiredAttributes.Length; i++)
            {
                AttributeRequirement req = nextRank.requiredAttributes[i];
                int current = GetAttributeValue(req.attributeName);
                int over = Mathf.Max(0, current - req.minValue);
                attrOverBonus += (over / 10) * 5f;
            }
        }

        // 高级职位惩罚: rank >= 2 (副主席/社长级别) 成功率 -20%
        float highRankPenalty = nextRank.rank >= 2 ? 20f : 0f;

        float successRate = Mathf.Clamp(
            baseRate + roundBonus + leadershipBonus + attrOverBonus - highRankPenalty,
            15f, 95f);

        // ---- 概率判定 ----
        float roll = UnityEngine.Random.Range(0f, 100f);
        bool success = roll < successRate;

        Debug.Log($"[ClubSystem] 晋升判定 {club.name} → {nextRank.title}: " +
                  $"成功率={successRate:F1}% (base=50 +round={roundBonus:F0} +leader={leadershipBonus:F1} " +
                  $"+attr={attrOverBonus:F0} -highRank={highRankPenalty:F0}), roll={roll:F1}, " +
                  $"结果={( success ? "成功" : "失败" )}");

        if (!success)
        {
            // 晋升失败 — 压力 +3, 但不重置资格，下回合可再次尝试
            if (PlayerAttributes.Instance != null)
            {
                PlayerAttributes.Instance.AddAttribute("压力", 3);
            }
            return;
        }

        // ---- 晋升成功 ----
        membership.currentRank = nextRank.rank;

        UpdatePositionAPCost();

        Debug.Log($"[ClubSystem] 晋升成功! {club.name}: {nextRank.title} (rank={nextRank.rank})");

        OnPromoted?.Invoke(clubId, nextRank.rank);
        OnClubStateChanged?.Invoke();
    }

    // ========== 职务行动点扣减 ==========

    /// <summary>
    /// 计算所有已加入社团当前职务的行动点扣减总和
    /// </summary>
    public int GetTotalPositionAPCost()
    {
        int total = 0;
        for (int i = 0; i < joinedClubs.Count; i++)
        {
            PromotionRank rank = GetCurrentRank(joinedClubs[i].clubId);
            if (rank != null)
                total += rank.apCost;
        }
        return total;
    }

    // ========== 入党系统 ==========

    /// <summary>当前入党阶段序号 (0=未申请)</summary>
    public int CurrentPartyStage => currentPartyStage;

    /// <summary>当前入党阶段的中文名称</summary>
    public string CurrentPartyStageName
    {
        get
        {
            if (partyMembershipStages == null || currentPartyStage >= partyMembershipStages.Length)
                return "未知";
            return partyMembershipStages[currentPartyStage].name;
        }
    }

    /// <summary>入党阶段总数</summary>
    public int PartyStageCount => partyMembershipStages != null ? partyMembershipStages.Length : 0;

    /// <summary>
    /// 检查是否可以申请入党
    /// 条件: 阶段0 + 领导力>=60 + 学力>=60 + GPA>=2.5 + 负罪感<=30 + 全局回合>=12
    /// </summary>
    public bool CanApplyForParty()
    {
        return string.IsNullOrEmpty(GetPartyBlockReason());
    }

    /// <summary>返回不可申请入党的原因，可申请则返回空字符串</summary>
    public string GetPartyBlockReason()
    {
        if (currentPartyStage != 0)
            return "已提交过申请";

        if (partyRequirements == null)
            return "入党条件未加载";

        if (PlayerAttributes.Instance == null)
            return "";

        // 时间限制: 全局回合>=12
        if (GetGlobalRound() < 12)
            return "时间未到（需回合12后）";

        // 属性检查: 领导力 + 学力
        if (PlayerAttributes.Instance.Leadership < partyRequirements.minLeadership)
            return $"领导力不足（需≥{partyRequirements.minLeadership}）";

        if (PlayerAttributes.Instance.Study < partyRequirements.minStudy)
            return $"学力不足（需≥{partyRequirements.minStudy}）";

        // GPA 检查: >=2.5
        float gpa = GetCurrentGPA();
        if (gpa >= 0 && gpa < 2.5f)
            return $"GPA不足（需≥2.5，当前{gpa:F1}）";

        // 负罪感检查: <=30
        if (PlayerAttributes.Instance.Guilt > 30)
            return $"负罪感过高（需≤30，当前{PlayerAttributes.Instance.Guilt}）";

        return "";
    }

    /// <summary>提交入党申请，进入阶段1</summary>
    public void ApplyForParty()
    {
        if (!CanApplyForParty())
        {
            Debug.LogWarning("[ClubSystem] 不满足入党申请条件");
            return;
        }

        currentPartyStage = 1;
        partyApplicationRound = GetGlobalRound();

        Debug.Log($"[ClubSystem] 提交入党申请，当前全局回合={partyApplicationRound}");

        OnPartyStageChanged?.Invoke(currentPartyStage);
        OnClubStateChanged?.Invoke();
    }

    /// <summary>
    /// 检查是否可以推进入党阶段
    /// 条件: 领导力>=60 + 学力>=60 + 负罪感<=30 + 距申请回合数满足下一阶段要求
    /// </summary>
    public bool CanAdvancePartyStage()
    {
        if (partyMembershipStages == null || partyRequirements == null)
            return false;

        if (currentPartyStage <= 0)
            return false;

        int nextStage = currentPartyStage + 1;
        if (nextStage >= partyMembershipStages.Length)
            return false;

        if (PlayerAttributes.Instance == null)
            return false;

        if (PlayerAttributes.Instance.Leadership < partyRequirements.minLeadership)
            return false;

        if (PlayerAttributes.Instance.Study < partyRequirements.minStudy)
            return false;

        // 负罪感持续检查
        if (PlayerAttributes.Instance.Guilt > 30)
            return false;

        int roundsSinceApplication = GetGlobalRound() - partyApplicationRound;
        if (roundsSinceApplication < partyMembershipStages[nextStage].requiredRound)
            return false;

        return true;
    }

    /// <summary>尝试推进入党阶段</summary>
    public void TryAdvancePartyStage()
    {
        if (!CanAdvancePartyStage())
            return;

        currentPartyStage++;

        Debug.Log($"[ClubSystem] 入党阶段推进: {CurrentPartyStageName} (stage={currentPartyStage})");

        OnPartyStageChanged?.Invoke(currentPartyStage);
        OnClubStateChanged?.Invoke();
    }

    // ========== 回合结算钩子 ==========

    /// <summary>
    /// 回合结束时调用 —— 更新社团回合数、不活跃检查、被动退出、晋升、入党、重置活动计数、冷却递减
    /// 应由 TurnManager 或 HUDManager.OnRoundAdvanced 触发
    /// </summary>
    public void OnRoundEnd()
    {
        // 1. 累加每个社团的在社回合数 + 不活跃追踪
        for (int i = joinedClubs.Count - 1; i >= 0; i--)
        {
            string cid = joinedClubs[i].clubId;
            joinedClubs[i].roundsInClub++;

            // 不活跃追踪
            if (activeThisRound.Contains(cid))
            {
                inactiveRounds[cid] = 0;
            }
            else
            {
                if (!inactiveRounds.ContainsKey(cid))
                    inactiveRounds[cid] = 0;
                inactiveRounds[cid]++;
            }
        }

        // 2. 被动退出检查: 连续5回合未活动 → 自动踢出 + 压力+5
        for (int i = joinedClubs.Count - 1; i >= 0; i--)
        {
            string cid = joinedClubs[i].clubId;
            ClubDefinition club = GetClub(cid);

            // 官方组织不会被动退出
            if (club != null && club.isOfficial)
                continue;

            if (inactiveRounds.TryGetValue(cid, out int inactive) && inactive >= InactiveKickRounds)
            {
                string clubName = club != null ? club.name : cid;
                Debug.Log($"[ClubSystem] 被动退出: {clubName}，连续{inactive}回合未活动");

                joinedClubs.RemoveAt(i);
                inactiveRounds.Remove(cid);

                // 被动退出惩罚: 压力+5
                if (PlayerAttributes.Instance != null)
                {
                    PlayerAttributes.Instance.AddAttribute("压力", InactiveKickStressPenalty);
                }

                OnClubLeft?.Invoke(cid);
            }
        }

        // 3. 尝试每个社团的自动晋升
        for (int i = 0; i < joinedClubs.Count; i++)
        {
            TryPromote(joinedClubs[i].clubId);
        }

        // 4. 尝试推进入党阶段
        if (currentPartyStage > 0 && partyMembershipStages != null
            && currentPartyStage < partyMembershipStages.Length - 1)
        {
            TryAdvancePartyStage();
        }

        // 5. 重置本回合活动计数
        roundActivityCount.Clear();
        activeThisRound.Clear();

        // 6. 冷却期递减（先递减，再移除归零的）
        List<string> cooldownKeys = new List<string>(exitCooldowns.Keys);
        foreach (string key in cooldownKeys)
        {
            exitCooldowns[key]--;
        }
        List<string> expiredCooldowns = new List<string>();
        foreach (var kvp in exitCooldowns)
        {
            if (kvp.Value <= 0)
                expiredCooldowns.Add(kvp.Key);
        }
        foreach (string key in expiredCooldowns)
        {
            exitCooldowns.Remove(key);
        }

        // 7. 更新职务行动点
        UpdatePositionAPCost();

        // 8. 通知状态变化
        OnClubStateChanged?.Invoke();
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 计算全局回合数: (year-1)*80 + (semester-1)*40 + round
    /// </summary>
    private int GetGlobalRound()
    {
        GameState gs = GameState.Instance;
        if (gs == null)
            return 0;

        return (gs.CurrentYear - 1) * 80 + (gs.CurrentSemester - 1) * 40 + gs.CurrentRound;
    }

    /// <summary>通过属性名获取 PlayerAttributes 中对应属性的当前值</summary>
    private int GetAttributeValue(string attrName)
    {
        PlayerAttributes pa = PlayerAttributes.Instance;
        if (pa == null)
            return 0;

        switch (attrName)
        {
            case "学力":   return pa.Study;
            case "魅力":   return pa.Charm;
            case "体魄":   return pa.Physique;
            case "领导力": return pa.Leadership;
            case "压力":   return pa.Stress;
            case "心情":   return pa.Mood;
            default:
                Debug.LogWarning($"[ClubSystem] GetAttributeValue: 未知属性名 \"{attrName}\"");
                return 0;
        }
    }

    /// <summary>重新计算所有职务行动点扣减并同步到 GameState</summary>
    private void UpdatePositionAPCost()
    {
        int total = GetTotalPositionAPCost();
        GameState gs = GameState.Instance;
        if (gs != null)
        {
            gs.PositionAPCost = total;
            Debug.Log($"[ClubSystem] 职务行动点扣减更新: {total}");
        }
    }

    /// <summary>
    /// 获取当前 GPA（通过 IExamResultProvider 接口）
    /// 未找到则返回 -1（表示无数据，不阻塞入党）
    /// </summary>
    private float GetCurrentGPA()
    {
        // 尝试查找场景中实现了 IExamResultProvider 的组件
        var providers = FindObjectsOfType<MonoBehaviour>();
        for (int i = 0; i < providers.Length; i++)
        {
            if (providers[i] is IExamResultProvider provider)
            {
                return provider.GetCumulativeGPA();
            }
        }
        return -1f; // 无GPA数据，不阻塞
    }

    // ========== 存档接口 (ISaveable) ==========

    /// <summary>将社团系统状态写入存档数据</summary>
    public void SaveToData(SaveData data)
    {
        // 社团成员记录
        data.clubRecords.Clear();
        for (int i = 0; i < joinedClubs.Count; i++)
        {
            ClubMembership m = joinedClubs[i];
            ClubDefinition club = GetClub(m.clubId);
            PromotionRank rank = GetCurrentRank(m.clubId);

            ClubMemberRecord record = new ClubMemberRecord
            {
                clubId = m.clubId,
                clubName = club != null ? club.name : m.clubId,
                role = rank != null ? rank.title : "干事",
                currentRank = m.currentRank,
                joinedAtRound = m.joinedAtRound,
                roundsInClub = m.roundsInClub,
                eventChainProgress = m.eventChainProgress,
                inactiveRounds = inactiveRounds.ContainsKey(m.clubId) ? inactiveRounds[m.clubId] : 0
            };
            data.clubRecords.Add(record);
        }

        // 入党进度
        data.currentPartyStage = currentPartyStage;
        data.partyApplicationRound = partyApplicationRound;

        // 退出冷却
        data.clubExitCooldowns.Clear();
        foreach (var kvp in exitCooldowns)
        {
            data.clubExitCooldowns.Add(new StringIntPair(kvp.Key, kvp.Value));
        }

        Debug.Log($"[ClubSystem] 存档保存: {joinedClubs.Count}个社团, 入党阶段={currentPartyStage}");
    }

    /// <summary>从存档数据恢复社团系统状态</summary>
    public void LoadFromData(SaveData data)
    {
        // 恢复社团成员
        joinedClubs.Clear();
        inactiveRounds.Clear();
        if (data.clubRecords != null)
        {
            for (int i = 0; i < data.clubRecords.Count; i++)
            {
                ClubMemberRecord record = data.clubRecords[i];
                ClubMembership m = new ClubMembership(record.clubId, record.joinedAtRound);
                m.currentRank = record.currentRank;
                m.roundsInClub = record.roundsInClub;
                m.eventChainProgress = record.eventChainProgress;
                joinedClubs.Add(m);

                // 恢复不活跃计数
                if (record.inactiveRounds > 0)
                    inactiveRounds[record.clubId] = record.inactiveRounds;
            }
        }

        // 恢复入党进度
        currentPartyStage = data.currentPartyStage;
        partyApplicationRound = data.partyApplicationRound;

        // 恢复退出冷却
        exitCooldowns.Clear();
        if (data.clubExitCooldowns != null)
        {
            for (int i = 0; i < data.clubExitCooldowns.Count; i++)
            {
                StringIntPair pair = data.clubExitCooldowns[i];
                exitCooldowns[pair.key] = pair.value;
            }
        }

        // 清理回合临时数据
        roundActivityCount.Clear();
        activeThisRound.Clear();

        // 同步职务行动点
        UpdatePositionAPCost();

        Debug.Log($"[ClubSystem] 存档加载: {joinedClubs.Count}个社团, 入党阶段={currentPartyStage}");

        OnClubStateChanged?.Invoke();
    }
}
