using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 学期总结系统 —— 负责学期结算评分、属性快照管理、行动统计、Provider 调度
/// </summary>
public class SemesterSummarySystem : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========
    public static SemesterSummarySystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>学期总结生成完毕后触发</summary>
    public event Action<SemesterSummaryData> OnSemesterSummaryReady;

    // ========== Provider 引用 ==========

    private IGPAProvider gpaProvider;
    private INPCRelationshipProvider npcProvider;
    private IClubMembershipProvider clubProvider;
    private IEconomyProvider economyProvider;
    private IRomanceProvider romanceProvider;

    // ========== 快照 ==========

    /// <summary>当前学期开始时的属性快照</summary>
    private AttributeSnapshot semesterStartSnapshot;

    // ========== 历史数据 ==========

    /// <summary>所有已完成学期的总结 (key = "year_semester")</summary>
    private Dictionary<string, SemesterSummaryData> semesterSummaries = new Dictionary<string, SemesterSummaryData>();

    // ========== 行动统计 ==========

    private int studyCount;
    private int socialCount;
    private int goOutCount;
    private int sleepCount;
    private int totalMoneySpent;

    /// <summary>总学习次数</summary>
    public int StudyCount => studyCount;

    /// <summary>总社交次数</summary>
    public int SocialCount => socialCount;

    /// <summary>总出校门次数</summary>
    public int GoOutCount => goOutCount;

    /// <summary>总睡觉次数</summary>
    public int SleepCount => sleepCount;

    /// <summary>总花费金额</summary>
    public int TotalMoneySpent => totalMoneySpent;

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

        // 初始化默认 Provider
        gpaProvider = new DefaultGPAProvider();
        npcProvider = new DefaultNPCProvider();
        clubProvider = new DefaultClubProvider();
        economyProvider = new DefaultEconomyProvider();
        romanceProvider = new DefaultRomanceProvider();
    }

    private void Start()
    {
        // 拍摄初始快照
        BeginSemesterTracking();

        // 注入真实 RomanceProvider (Bug6 修复)
        if (RomanceSystem.Instance != null)
        {
            SetRomanceProvider(RomanceSystem.Instance);
            Debug.Log("[SemesterSummarySystem] 已注入 RomanceSystem 作为 IRomanceProvider");
        }

        // 订阅回合推进事件
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced += HandleRoundAdvanced;
            Debug.Log("[SemesterSummarySystem] 已订阅 TurnManager.OnRoundAdvanced");
        }
        else
        {
            Debug.LogWarning("[SemesterSummarySystem] TurnManager 实例不存在，无法订阅事件");
        }

        // 订阅行动执行事件
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted += HandleActionExecuted;
            Debug.Log("[SemesterSummarySystem] 已订阅 ActionSystem.OnActionExecuted");
        }
        else
        {
            Debug.LogWarning("[SemesterSummarySystem] ActionSystem 实例不存在，无法订阅事件");
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundAdvanced -= HandleRoundAdvanced;
        }
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted -= HandleActionExecuted;
        }
    }

    // ========== Provider 注入 ==========

    /// <summary>
    /// 替换 GPA 数据提供者（当课程系统实装后调用）
    /// </summary>
    public void SetGPAProvider(IGPAProvider provider)
    {
        if (provider != null) gpaProvider = provider;
    }

    /// <summary>
    /// 替换 NPC 关系数据提供者
    /// </summary>
    public void SetNPCProvider(INPCRelationshipProvider provider)
    {
        if (provider != null) npcProvider = provider;
    }

    /// <summary>
    /// 替换社团数据提供者
    /// </summary>
    public void SetClubProvider(IClubMembershipProvider provider)
    {
        if (provider != null) clubProvider = provider;
    }

    /// <summary>
    /// 替换经济数据提供者
    /// </summary>
    public void SetEconomyProvider(IEconomyProvider provider)
    {
        if (provider != null) economyProvider = provider;
    }

    /// <summary>
    /// 替换恋爱数据提供者
    /// </summary>
    public void SetRomanceProvider(IRomanceProvider provider)
    {
        if (provider != null) romanceProvider = provider;
    }

    /// <summary>
    /// 一次性注入所有真实 Provider，在 GameSceneInitializer 初始化完成后调用
    /// </summary>
    public void InjectRealProviders()
    {
        SetGPAProvider(new RealGPAProvider());
        SetNPCProvider(new RealNPCProvider());
        SetClubProvider(new RealClubProvider());
        SetEconomyProvider(new RealEconomyProvider());
        SetRomanceProvider(new RealRomanceProvider());
        Debug.Log("[SemesterSummarySystem] 已注入所有真实 Provider");
    }

    // ========== 快照管理 ==========

    /// <summary>
    /// 开始新学期的属性追踪 —— 保存当前属性快照作为学期起点
    /// </summary>
    public void BeginSemesterTracking()
    {
        semesterStartSnapshot = AttributeSnapshot.CaptureNow();
        Debug.Log($"[SemesterSummarySystem] 学期开始快照已拍摄 " +
                  $"(year={semesterStartSnapshot.year}, semester={semesterStartSnapshot.semester})");
    }

    // ========== 事件处理 ==========

    /// <summary>
    /// 回合推进回调 —— 在学期/学年切换时自动生成总结并拍摄新快照
    /// </summary>
    private void HandleRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        switch (result)
        {
            case GameState.RoundAdvanceResult.NextSemester:
                // 上学期结束，当前已切换到下学期 → 生成上学期总结
                GenerateAndStoreSummary(GameState.Instance.CurrentYear, 1);
                BeginSemesterTracking();
                break;

            case GameState.RoundAdvanceResult.NextYear:
                // 下学期结束，当前已切换到新学年 → 生成上学年下学期总结
                GenerateAndStoreSummary(GameState.Instance.CurrentYear - 1, 2);
                BeginSemesterTracking();
                break;

            case GameState.RoundAdvanceResult.Graduated:
                // 大四下学期结束 → 生成最后一学期总结
                GenerateAndStoreSummary(4, 2);
                break;
        }
    }

    /// <summary>
    /// 行动执行回调 —— 累计行动统计
    /// </summary>
    private void HandleActionExecuted(ActionDefinition action)
    {
        if (action == null) return;

        switch (action.id)
        {
            case "study":
                studyCount++;
                break;
            case "social":
                socialCount++;
                break;
            case "goout":
                goOutCount++;
                totalMoneySpent += action.moneyCost;
                break;
            case "sleep":
                sleepCount++;
                break;
        }
    }

    // ========== 总结生成 ==========

    /// <summary>
    /// 生成指定学期的总结并存储，同时触发事件
    /// </summary>
    private void GenerateAndStoreSummary(int year, int semester)
    {
        SemesterSummaryData summary = GenerateSemesterSummary(year, semester);
        string key = $"{year}_{semester}";
        semesterSummaries[key] = summary;

        Debug.Log($"[SemesterSummarySystem] 学期总结已生成: {summary.yearName}{summary.semesterName} " +
                  $"总分={summary.totalScore} 评级={summary.grade}");

        // 发放天赋点奖励
        if (TalentSystem.Instance != null)
        {
            float gpa = gpaProvider != null ? gpaProvider.GetSemesterGPA(year, semester) : 0f;
            string ptGrade = "";
            bool cet4 = ExamSystem.Instance != null && ExamSystem.Instance.IsCET4Passed;
            bool cet6 = ExamSystem.Instance != null && ExamSystem.Instance.IsCET6Passed;
            TalentSystem.Instance.ProcessSemesterReward(gpa, ptGrade, cet4, cet6);
        }

        OnSemesterSummaryReady?.Invoke(summary);
    }

    /// <summary>
    /// 生成指定学期的总结数据
    /// </summary>
    public SemesterSummaryData GenerateSemesterSummary(int year, int semester)
    {
        SemesterSummaryData data = new SemesterSummaryData();

        // ========== 学期标识 ==========
        data.year = year;
        data.semester = semester;
        data.yearName = GetYearName(year);
        data.semesterName = semester == 1 ? "上学期" : "下学期";

        // ========== 属性快照 ==========
        AttributeSnapshot endSnapshot = AttributeSnapshot.CaptureNow();

        // 起始属性
        if (semesterStartSnapshot != null)
        {
            data.startAttributes = semesterStartSnapshot.ToDictionary();
        }
        else
        {
            data.startAttributes = new Dictionary<string, int>();
        }

        // 结束属性
        data.endAttributes = endSnapshot.ToDictionary();

        // 属性变化 (end - start)
        data.attributeChanges = CalculateAttributeChanges(data.startAttributes, data.endAttributes);

        // ========== Provider 数据 ==========
        data.gpa = gpaProvider.GetSemesterGPA(year, semester);
        data.courses = gpaProvider.GetSemesterCourses(year, semester);
        data.npcRelations = npcProvider.GetAllRelations();

        // ========== 评分计算 ==========
        int totalFriendship = npcProvider.GetTotalFriendship();

        // 学业分 = GPA × 1000
        data.academicScore = Mathf.RoundToInt(data.gpa * 1000f);

        // 人际分 = NPC好感总和
        data.socialScore = totalFriendship;

        // 体育分 = min(physique * 2.5, 200)
        int physique = PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Physique : 0;
        data.sportsScore = Mathf.Min(Mathf.RoundToInt(physique * 2.5f), 200);

        // 成就分 (从 AchievementSystem 获取)
        if (AchievementSystem.Instance != null)
        {
            data.achievementScore = AchievementSystem.Instance.GetTotalAchievementScore();
            data.unlockedAchievements = AchievementSystem.Instance.GetSemesterAchievements();
        }
        else
        {
            data.achievementScore = 0;
        }

        // 扣分项 = stress × 2
        int stress = PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Stress : 0;
        data.penaltyScore = stress * 2;

        // 总分
        data.totalScore = CalculateSemesterScore(data);

        // 评级
        data.grade = DetermineGrade(data.totalScore);

        return data;
    }

    /// <summary>
    /// 计算属性变化 (end - start)
    /// </summary>
    private Dictionary<string, int> CalculateAttributeChanges(
        Dictionary<string, int> startAttrs,
        Dictionary<string, int> endAttrs)
    {
        Dictionary<string, int> changes = new Dictionary<string, int>();

        foreach (var kvp in endAttrs)
        {
            int startVal = 0;
            if (startAttrs.ContainsKey(kvp.Key))
            {
                startVal = startAttrs[kvp.Key];
            }
            changes[kvp.Key] = kvp.Value - startVal;
        }

        return changes;
    }

    // ========== 评分公式 ==========

    /// <summary>
    /// 计算学期总分
    /// 总分 = 学业分(GPA×1000) + 人际分(NPC好感总和) + 体育分(min(physique*2.5, 200)) + 成就分 - 扣分项(stress*2)
    /// </summary>
    public int CalculateSemesterScore(SemesterSummaryData data)
    {
        int score = data.academicScore
                  + data.socialScore
                  + data.sportsScore
                  + data.achievementScore
                  - data.penaltyScore;
        return Mathf.Max(0, score);
    }

    /// <summary>
    /// 根据分数判定等级: S(≥6000) / A(≥4500) / B(≥3000) / C(≥1500) / D(&lt;1500)
    /// </summary>
    public SemesterGrade DetermineGrade(int score)
    {
        if (score >= 6000) return SemesterGrade.S;
        if (score >= 4500) return SemesterGrade.A;
        if (score >= 3000) return SemesterGrade.B;
        if (score >= 1500) return SemesterGrade.C;
        return SemesterGrade.D;
    }

    // ========== 毕业总评 ==========

    /// <summary>
    /// 计算毕业总评分 = Σ(学期分 × 年度权重)
    /// 年度权重: 大一×1.0, 大二×1.5, 大三×2.0, 大四×2.5
    /// </summary>
    public float CalculateGraduationScore()
    {
        float totalScore = 0f;

        foreach (var kvp in semesterSummaries)
        {
            SemesterSummaryData data = kvp.Value;
            float weight = GetYearWeight(data.year);
            totalScore += data.totalScore * weight;
        }

        return totalScore;
    }

    /// <summary>
    /// 获取学年权重
    /// </summary>
    private float GetYearWeight(int year)
    {
        switch (year)
        {
            case 1: return 1.0f;
            case 2: return 1.5f;
            case 3: return 2.0f;
            case 4: return 2.5f;
            default: return 1.0f;
        }
    }

    // ========== 数据查询 ==========

    /// <summary>
    /// 获取指定学期的总结数据，不存在返回 null
    /// </summary>
    public SemesterSummaryData GetSemesterSummary(int year, int semester)
    {
        string key = $"{year}_{semester}";
        if (semesterSummaries.TryGetValue(key, out SemesterSummaryData data))
        {
            return data;
        }
        return null;
    }

    /// <summary>
    /// 获取所有已生成的学期总结
    /// </summary>
    public Dictionary<string, SemesterSummaryData> GetAllSummaries()
    {
        return new Dictionary<string, SemesterSummaryData>(semesterSummaries);
    }

    /// <summary>
    /// 获取已完成的学期数量
    /// </summary>
    public int GetCompletedSemesterCount()
    {
        return semesterSummaries.Count;
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 获取学年中文名
    /// </summary>
    private string GetYearName(int year)
    {
        switch (year)
        {
            case 1: return "大一";
            case 2: return "大二";
            case 3: return "大三";
            case 4: return "大四";
            default: return "大" + year;
        }
    }

    // ==========================================================================
    // 默认 Provider 实现 (内部类) —— 在子系统未实装前提供模拟数据
    // ==========================================================================

    /// <summary>
    /// 默认 GPA 提供者 —— 基于 PlayerAttributes.Study 模拟 GPA 和课程成绩
    /// </summary>
    private class DefaultGPAProvider : IGPAProvider
    {
        // 模拟课程列表 (按学年分组)
        private static readonly string[][] coursesByYear = new string[][]
        {
            new string[] { "高等数学", "大学英语", "思想道德与法治", "体育", "计算机基础" },
            new string[] { "线性代数", "概率论", "大学物理", "数据结构", "马克思主义基本原理" },
            new string[] { "操作系统", "计算机网络", "数据库原理", "软件工程", "专业选修课" },
            new string[] { "毕业设计", "专业实习", "综合实验", "就业指导", "形势与政策" }
        };

        public float GetCurrentGPA()
        {
            if (PlayerAttributes.Instance == null) return 2.0f;
            float gpa = PlayerAttributes.Instance.Study / 25f;
            return Mathf.Clamp(gpa, 0f, 4.0f);
        }

        public float GetSemesterGPA(int year, int semester)
        {
            return GetCurrentGPA();
        }

        public List<CourseGrade> GetSemesterCourses(int year, int semester)
        {
            List<CourseGrade> courses = new List<CourseGrade>();
            int yearIndex = Mathf.Clamp(year - 1, 0, coursesByYear.Length - 1);
            string[] courseNames = coursesByYear[yearIndex];

            float baseGPA = GetCurrentGPA();

            for (int i = 0; i < courseNames.Length; i++)
            {
                // 每门课在基础 GPA 上加一点随机偏移
                float gp = Mathf.Clamp(baseGPA + UnityEngine.Random.Range(-0.3f, 0.3f), 0f, 4.0f);
                float score = GPAToScore(gp);
                int credits = (i == 0 || i == 3) ? 4 : 3; // 第1/4门课4学分，其余3学分
                courses.Add(new CourseGrade(courseNames[i], score, gp, credits));
            }

            return courses;
        }

        /// <summary>
        /// 绩点转百分制分数 (近似)
        /// </summary>
        private float GPAToScore(float gpa)
        {
            // 4.0→95, 3.0→82, 2.0→72, 1.0→62, 0→50
            return Mathf.Clamp(50f + gpa * 11.25f, 0f, 100f);
        }
    }

    /// <summary>
    /// 默认 NPC 关系提供者 —— 返回模拟 NPC 数据
    /// </summary>
    private class DefaultNPCProvider : INPCRelationshipProvider
    {
        private static readonly string[] defaultNPCs = new string[]
        {
            "室友小张", "同学小李", "学长王哥", "辅导员刘老师"
        };

        public int GetTotalFriendship()
        {
            // 基于魅力模拟好感总和
            if (PlayerAttributes.Instance == null) return 100;
            return Mathf.Clamp(PlayerAttributes.Instance.Charm * 10, 0, 400);
        }

        public List<NPCRelationInfo> GetAllRelations()
        {
            List<NPCRelationInfo> relations = new List<NPCRelationInfo>();
            int charm = PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Charm : 10;

            for (int i = 0; i < defaultNPCs.Length; i++)
            {
                int baseFriendship = Mathf.Clamp(charm * 2 + i * 5, 0, 100);
                int change = UnityEngine.Random.Range(-5, 15);
                relations.Add(new NPCRelationInfo(defaultNPCs[i], baseFriendship, change));
            }

            return relations;
        }
    }

    /// <summary>
    /// 默认社团提供者 —— 返回默认值
    /// </summary>
    private class DefaultClubProvider : IClubMembershipProvider
    {
        public bool IsStudentCouncilPresident() => false;
        public bool IsPartyMember() => false;
        public List<string> GetJoinedClubs() => new List<string>();
    }

    /// <summary>
    /// 默认经济提供者 —— 从 GameState 读取基本数据
    /// </summary>
    private class DefaultEconomyProvider : IEconomyProvider
    {
        public int GetCurrentMoney()
        {
            return GameState.Instance != null ? GameState.Instance.Money : 0;
        }

        public int GetTotalEarned() => 0;

        public int GetTotalSpent()
        {
            return Instance != null ? Instance.totalMoneySpent : 0;
        }
    }

    /// <summary>
    /// 默认恋爱提供者 —— 返回默认值
    /// </summary>
    private class DefaultRomanceProvider : IRomanceProvider
    {
        public int GetRomanceLevel() => 0;
        public string GetPartnerName() => "";
        public bool HasPartner() => false;
    }

    // ==========================================================================
    // 真实 Provider 实现 (内部类) —— 从已实装的子系统读取真实数据
    // ==========================================================================

    /// <summary>
    /// 真实 GPA 提供者 —— 从 ExamSystem 读取考试成绩
    /// </summary>
    private class RealGPAProvider : IGPAProvider
    {
        public float GetCurrentGPA()
        {
            if (ExamSystem.Instance == null) return FallbackGPA();
            float gpa = ExamSystem.Instance.GetCumulativeGPA();
            return gpa > 0f ? gpa : FallbackGPA();
        }

        public float GetSemesterGPA(int year, int semester)
        {
            if (ExamSystem.Instance == null) return FallbackGPA();

            var results = ExamSystem.Instance.GetResultsBySemester(year, semester);
            if (results == null || results.Length == 0) return FallbackGPA();

            // 计算该学期的GPA
            float totalGP = 0f;
            int totalCredits = 0;
            foreach (var r in results)
            {
                int credits = r.credits > 0 ? r.credits : 3;
                totalGP += r.gradePoint * credits;
                totalCredits += credits;
            }
            return totalCredits > 0 ? totalGP / totalCredits : FallbackGPA();
        }

        public List<CourseGrade> GetSemesterCourses(int year, int semester)
        {
            List<CourseGrade> courses = new List<CourseGrade>();

            if (ExamSystem.Instance == null) return courses;

            var results = ExamSystem.Instance.GetResultsBySemester(year, semester);
            if (results == null) return courses;

            foreach (var r in results)
            {
                courses.Add(new CourseGrade(
                    r.courseName,
                    r.score,
                    r.gradePoint,
                    r.credits > 0 ? r.credits : 3
                ));
            }
            return courses;
        }

        private float FallbackGPA()
        {
            if (PlayerAttributes.Instance == null) return 2.0f;
            float gpa = PlayerAttributes.Instance.Study / 25f;
            return Mathf.Clamp(gpa, 0f, 4.0f);
        }
    }

    /// <summary>
    /// 真实 NPC 关系提供者 —— 从 AffinitySystem 读取好感度数据
    /// </summary>
    private class RealNPCProvider : INPCRelationshipProvider
    {
        public int GetTotalFriendship()
        {
            if (AffinitySystem.Instance == null) return 0;

            var allRelations = AffinitySystem.Instance.GetAllRelationships();
            int total = 0;
            foreach (var kvp in allRelations)
            {
                total += kvp.Value.affinity;
            }
            return total;
        }

        public List<NPCRelationInfo> GetAllRelations()
        {
            List<NPCRelationInfo> relations = new List<NPCRelationInfo>();

            if (AffinitySystem.Instance == null) return relations;

            var allRelations = AffinitySystem.Instance.GetAllRelationships();
            foreach (var kvp in allRelations)
            {
                string npcName = kvp.Key;
                // 尝试从 NPCDatabase 获取中文名
                if (NPCDatabase.Instance != null)
                {
                    var npcData = NPCDatabase.Instance.GetNPC(kvp.Key);
                    if (npcData != null) npcName = npcData.displayName;
                }

                relations.Add(new NPCRelationInfo(
                    npcName,
                    kvp.Value.affinity,
                    0  // 变化量暂不追踪，后续可扩展
                ));
            }
            return relations;
        }
    }

    /// <summary>
    /// 真实社团提供者 —— 从 ClubSystem 读取社团和入党数据
    /// </summary>
    private class RealClubProvider : IClubMembershipProvider
    {
        public bool IsStudentCouncilPresident()
        {
            if (ClubSystem.Instance == null) return false;

            var membership = ClubSystem.Instance.GetMembership("student_union");
            if (membership == null) return false;

            var currentRank = ClubSystem.Instance.GetCurrentRank("student_union");
            var nextRank = ClubSystem.Instance.GetNextRank("student_union");

            return currentRank != null && nextRank == null;
        }

        public bool IsPartyMember()
        {
            if (ClubSystem.Instance == null) return false;
            int stageCount = ClubSystem.Instance.PartyStageCount;
            if (stageCount <= 0) return false;
            return ClubSystem.Instance.CurrentPartyStage >= stageCount - 1;
        }

        public List<string> GetJoinedClubs()
        {
            List<string> clubs = new List<string>();
            if (ClubSystem.Instance == null) return clubs;

            var joined = ClubSystem.Instance.GetJoinedClubs();
            foreach (var m in joined)
            {
                var clubDef = ClubSystem.Instance.GetClub(m.clubId);
                clubs.Add(clubDef != null ? clubDef.name : m.clubId);
            }
            return clubs;
        }
    }

    /// <summary>
    /// 真实经济提供者 —— 从 EconomyManager 和 GameState 读取经济数据
    /// </summary>
    private class RealEconomyProvider : IEconomyProvider
    {
        public int GetCurrentMoney()
        {
            return GameState.Instance != null ? GameState.Instance.Money : 0;
        }

        public int GetTotalEarned()
        {
            if (EconomyManager.Instance == null) return 0;

            var log = EconomyManager.Instance.GetTransactionLog();
            int total = 0;
            foreach (var record in log)
            {
                if (record.amount > 0) total += record.amount;
            }
            return total;
        }

        public int GetTotalSpent()
        {
            if (EconomyManager.Instance == null)
                return Instance != null ? Instance.totalMoneySpent : 0;

            var log = EconomyManager.Instance.GetTransactionLog();
            int total = 0;
            foreach (var record in log)
            {
                if (record.amount < 0) total += -record.amount;
            }
            return total;
        }
    }

    /// <summary>
    /// 真实恋爱提供者 —— 从 RomanceSystem 读取恋爱状态
    /// </summary>
    private class RealRomanceProvider : IRomanceProvider
    {
        public int GetRomanceLevel()
        {
            if (RomanceSystem.Instance == null || NPCDatabase.Instance == null) return 0;

            var allNPCs = NPCDatabase.Instance.GetAllNPCs();
            if (allNPCs == null) return 0;

            int maxHealth = 0;
            foreach (var npc in allNPCs)
            {
                if (RomanceSystem.Instance.GetRomanceState(npc.id) == RomanceState.Dating)
                {
                    int health = RomanceSystem.Instance.GetRomanceHealth(npc.id);
                    if (health > maxHealth) maxHealth = health;
                }
            }
            // 映射健康度到等级: 80+ → 5, 60+ → 4, 40+ → 3, 20+ → 2, 1+ → 1
            if (maxHealth >= 80) return 5;
            if (maxHealth >= 60) return 4;
            if (maxHealth >= 40) return 3;
            if (maxHealth >= 20) return 2;
            if (maxHealth > 0) return 1;
            return 0;
        }

        public string GetPartnerName()
        {
            if (RomanceSystem.Instance == null || NPCDatabase.Instance == null) return "";

            var allNPCs = NPCDatabase.Instance.GetAllNPCs();
            if (allNPCs == null) return "";

            foreach (var npc in allNPCs)
            {
                if (RomanceSystem.Instance.GetRomanceState(npc.id) == RomanceState.Dating)
                    return npc.displayName;
            }
            return "";
        }

        public bool HasPartner()
        {
            if (RomanceSystem.Instance == null || NPCDatabase.Instance == null) return false;

            var allNPCs = NPCDatabase.Instance.GetAllNPCs();
            if (allNPCs == null) return false;

            foreach (var npc in allNPCs)
            {
                if (RomanceSystem.Instance.GetRomanceState(npc.id) == RomanceState.Dating)
                    return true;
            }
            return false;
        }
    }

    // ========== ISaveable 实现 ==========

    public void SaveToData(SaveData data)
    {
        data.studyCount = studyCount;
        data.socialCount = socialCount;
        data.goOutCount = goOutCount;
        data.sleepCount = sleepCount;
        data.totalMoneySpent = totalMoneySpent;
    }

    public void LoadFromData(SaveData data)
    {
        studyCount = data.studyCount;
        socialCount = data.socialCount;
        goOutCount = data.goOutCount;
        sleepCount = data.sleepCount;
        totalMoneySpent = data.totalMoneySpent;
    }
}
