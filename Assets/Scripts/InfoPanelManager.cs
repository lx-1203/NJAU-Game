using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 信息面板管理器 - 管理数据绑定、事件订阅和交互逻辑
/// 单例模式，负责刷新三个子面板的数据显示
/// </summary>
public class InfoPanelManager : MonoBehaviour
{
    // ========== 单例 ==========
    public static InfoPanelManager Instance { get; private set; }

    // ========== Builder 引用 ==========
    private InfoPanelBuilder builder;

    // ========== 当前状态 ==========
    private int currentTabIndex = 0;
    private string selectedNPCId = null;
    private bool isSubscribed = false;
    private GameObject currentTabPanel;

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

        // 初始化Builder
        builder = gameObject.AddComponent<InfoPanelBuilder>();
        builder.BuildUI();

        // 绑定按钮事件
        BindButtonEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ========== 按钮事件绑定 ==========

    private void BindButtonEvents()
    {
        // 关闭按钮
        builder.btnClose.onClick.AddListener(ClosePanel);

        // 遮罩点击关闭
        Button overlayBtn = builder.overlayObj.GetComponent<Button>();
        if (overlayBtn != null)
        {
            overlayBtn.onClick.AddListener(ClosePanel);
        }

        // 标签按钮
        for (int i = 0; i < builder.tabButtons.Length; i++)
        {
            int index = i; // 闭包捕获
            builder.tabButtons[i].onClick.AddListener(() => SwitchTab(index));
        }

        // 社交互动按钮
        builder.btnSocialInteract.onClick.AddListener(OnSocialInteractClicked);

        if (builder.btnOpenMissionPanel != null)
        {
            builder.btnOpenMissionPanel.onClick.AddListener(OnOpenMissionPanelClicked);
        }
    }

    // ========== 公共接口 ==========

    /// <summary>打开面板（默认显示个人信息标签）</summary>
    public void OpenPanel(int defaultTab = 0)
    {
        if (builder.panelRoot == null)
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("信息面板不可用", "信息面板还没有成功构建，现在暂时无法打开。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }

        if (builder.panelRoot.activeSelf)
        {
            SwitchTab(defaultTab);
            return;
        }

        if (!UIFlowGuard.PrepareForExclusiveWindow(UIFlowGuard.WindowInfoPanel))
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("无法打开信息面板", "当前有其他独占界面正在占用输入，稍后再试。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }
        builder.panelRoot.SetActive(true);
        builder.overlayObj.SetActive(true);
        UITransitionUtility.Show(this, builder.overlayObj, Vector2.zero, 1f, 0.18f);
        UITransitionUtility.Show(this, builder.panelRoot, new Vector2(0f, 30f), 0.98f, 0.24f);

        SwitchTab(defaultTab);
        SubscribeEvents();
    }

    /// <summary>关闭面板</summary>
    public void ClosePanel()
    {
        if (builder.panelRoot == null) return;

        UnsubscribeEvents();
        currentTabPanel = null;
        UITransitionUtility.Hide(this, builder.overlayObj, Vector2.zero, 1f, 0.14f);
        UITransitionUtility.Hide(this, builder.panelRoot, new Vector2(0f, 18f), 0.985f, 0.16f);
    }

    public bool IsOpen => builder != null && builder.panelRoot != null && builder.panelRoot.activeSelf;

    /// <summary>切换标签页</summary>
    public void SwitchTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= 3) return;

        int previousTabIndex = currentTabIndex;
        currentTabIndex = tabIndex;

        // 更新标签按钮状态
        for (int i = 0; i < builder.tabButtons.Length; i++)
        {
            ColorBlock cb = builder.tabButtons[i].colors;
            if (i == tabIndex)
            {
                cb.normalColor = new Color(0.71f, 0.50f, 0.24f, 1.0f); // TabActiveColor
            }
            else
            {
                cb.normalColor = new Color(0.82f, 0.71f, 0.55f, 1.0f); // TabInactiveColor
            }
            builder.tabButtons[i].colors = cb;
        }

        GameObject targetPanel = null;

        // 显示目标子面板并刷新数据
        switch (tabIndex)
        {
            case 0:
                targetPanel = builder.playerInfoPanel;
                builder.txtTitle.text = "个人信息";
                RefreshPlayerInfo();
                break;
            case 1:
                targetPanel = builder.relationshipPanel;
                builder.txtTitle.text = "人际关系";
                RefreshRelationships();
                break;
            case 2:
                targetPanel = builder.questPanel;
                builder.txtTitle.text = "任务";
                RefreshQuests();
                break;
        }

        if (targetPanel == null)
        {
            return;
        }

        if (currentTabPanel == targetPanel)
        {
            targetPanel.SetActive(true);
            return;
        }

        GameObject previousPanel = currentTabPanel;
        currentTabPanel = targetPanel;

        GameObject[] allPanels =
        {
            builder.playerInfoPanel,
            builder.relationshipPanel,
            builder.questPanel
        };

        for (int i = 0; i < allPanels.Length; i++)
        {
            GameObject panel = allPanels[i];
            if (panel != null && panel != previousPanel && panel != targetPanel)
            {
                panel.SetActive(false);
            }
        }

        if (previousPanel != null)
        {
            float direction = tabIndex > previousTabIndex ? -24f : 24f;
            UITransitionUtility.Hide(this, previousPanel, new Vector2(direction, 0f), 0.995f, 0.12f);
        }

        targetPanel.SetActive(true);
        UITransitionUtility.Show(this, targetPanel, new Vector2(tabIndex > previousTabIndex ? 24f : -24f, 0f), 0.995f, 0.16f);
    }

    /// <summary>刷新所有数据</summary>
    public void RefreshAll()
    {
        switch (currentTabIndex)
        {
            case 0: RefreshPlayerInfo(); break;
            case 1: RefreshRelationships(); break;
            case 2: RefreshQuests(); break;
        }
    }

    // ========== 个人信息面板刷新 ==========

    public void RefreshPlayerInfo()
    {
        if (GameState.Instance == null || PlayerAttributes.Instance == null) return;

        GameState gs = GameState.Instance;
        PlayerAttributes pa = PlayerAttributes.Instance;

        // 基础信息
        builder.txtPlayerName.text = $"姓名：{gs.PlayerName}";
        string genderDisplay = gs.PlayerGender == 1 ? "女" : "男";
        builder.txtPlayerInfo.text = $"性别：{genderDisplay}    专业：{gs.PlayerMajor}";

        int age = 18 + (gs.CurrentYear - 1);
        builder.txtTimeInfo.text = $"{gs.GetTimeDescription()}    年龄：{age}岁";

        // 核心属性（使用AttributeBar）
        RefreshAttributeBars();

        // 状态值
        builder.txtStress.text = $"{pa.Stress}%";
        builder.imgStressBar.fillAmount = pa.Stress / 100f;
        builder.txtStress.color = pa.Stress >= 80
            ? new Color(0.78f, 0.18f, 0.14f)
            : new Color(0.20f, 0.14f, 0.10f);
        if (builder.txtStressMeta != null)
        {
            builder.txtStressMeta.text = BuildStatusMetaText("压力", pa.Stress, isInverseGood: true);
        }

        builder.txtMood.text = $"{pa.Mood}%";
        builder.imgMoodBar.fillAmount = pa.Mood / 100f;
        builder.txtMood.color = pa.Mood >= 70
            ? new Color(0.18f, 0.45f, 0.26f)
            : new Color(0.20f, 0.14f, 0.10f);
        if (builder.txtMoodMeta != null)
        {
            builder.txtMoodMeta.text = BuildStatusMetaText("心情", pa.Mood, isInverseGood: false);
        }

        // 隐性属性
        string hiddenText = $"黑暗值：{pa.Darkness}    负罪感：{pa.Guilt}    幸运：{pa.Luck}";
        if (PenaltySystem.Instance != null)
        {
            hiddenText += $"\n摆烂值：{PenaltySystem.Instance.SlackingValue}    心理健康：{PenaltySystem.Instance.MentalHealth}";
        }
        builder.txtHiddenAttrs.text = hiddenText;

        // 学业信息
        float cumulativeGPA = 0f;
        float semesterGPA = 0f;
        if (ExamSystem.Instance != null)
        {
            cumulativeGPA = ExamSystem.Instance.GetCumulativeGPA();
            semesterGPA = ExamSystem.Instance.GetLatestSemesterGPA();
        }
        else
        {
            // 回退：使用Study/25估算
            cumulativeGPA = pa.Study / 25f;
            semesterGPA = cumulativeGPA;
        }

        builder.txtGPA.text = $"累计GPA：{cumulativeGPA:F2}    本学期GPA：{semesterGPA:F2}";

        int earnedCredits = CalculateEarnedCredits();
        builder.txtCredits.text = $"已修学分：{earnedCredits} / 121";
        if (builder.txtCertificates != null)
        {
            builder.txtCertificates.text = BuildCertificateStatusText();
        }

        // 经济信息
        builder.txtMoney.text = $"当前金钱：¥{gs.Money}";
        builder.txtMoney.color = gs.Money < 0
            ? new Color(0.78f, 0.18f, 0.14f)
            : new Color(0.20f, 0.14f, 0.10f);

        string debtLevel = "正常";
        if (DebtSystem.Instance != null)
        {
            switch (DebtSystem.Instance.CurrentDebtLevel)
            {
                case DebtSystem.DebtLevel.FoodRestricted: debtLevel = "食品受限"; break;
                case DebtSystem.DebtLevel.Overdrafted: debtLevel = "透支"; break;
                case DebtSystem.DebtLevel.LoanTrigger: debtLevel = "贷款触发"; break;
                case DebtSystem.DebtLevel.Bankruptcy: debtLevel = "破产"; break;
            }
        }
        builder.txtDebt.text = $"债务等级：{debtLevel}";
        builder.txtDebt.color = debtLevel == "正常"
            ? new Color(0.44f, 0.31f, 0.22f)
            : new Color(0.78f, 0.24f, 0.16f);
        if (builder.txtJobProgress != null)
        {
            builder.txtJobProgress.text = BuildJobProgressText();
        }

        // 社团信息
        if (ClubSystem.Instance != null)
        {
            List<string> clubNames = new List<string>();
            var memberships = ClubSystem.Instance.GetJoinedClubs();
            foreach (var membership in memberships)
            {
                string roleName = GetRoleName(membership.currentRank);
                var clubDef = ClubSystem.Instance.GetClub(membership.clubId);
                string cName = clubDef != null ? clubDef.name : membership.clubId;
                clubNames.Add($"{cName}（{roleName}）");
            }

            if (clubNames.Count > 0)
            {
                builder.txtClubs.text = $"已加入：{string.Join("、", clubNames)}";
            }
            else
            {
                builder.txtClubs.text = "已加入：无";
            }

            // 入党进度
            int partyStage = ClubSystem.Instance.CurrentPartyStage;
            if (partyStage > 0)
            {
                builder.txtParty.text = $"入党进度：{ClubSystem.Instance.CurrentPartyStageName} ({partyStage}/5)";
            }
            else
            {
                builder.txtParty.text = "入党进度：未申请";
            }
        }
    }

    private void RefreshAttributeBars()
    {
        if (PlayerAttributes.Instance == null) return;

        // 清空旧的AttributeBar
        foreach (Transform child in builder.attributeContainer)
        {
            if (child.name.StartsWith("AttributeBar"))
            {
                Destroy(child.gameObject);
            }
        }

        // 创建新的AttributeBar（仅核心属性）
        string[] coreAttributes = { "学力", "魅力", "体魄", "领导力" };
        PlayerAttributes.AttributeInfo[] allAttrs = PlayerAttributes.Instance.GetAllAttributes();

        foreach (var attrInfo in allAttrs)
        {
            if (System.Array.IndexOf(coreAttributes, attrInfo.name) >= 0)
            {
                AttributeBar bar = AttributeBar.Create(builder.attributeContainer, detailed: true);
                bar.SetAttributeImmediate(attrInfo);
            }
        }
    }

    private string GetRoleName(int rank)
    {
        switch (rank)
        {
            case 0: return "干事";
            case 1: return "部长";
            case 2: return "副主席/副书记";
            case 3: return "主席/书记";
            default: return "成员";
        }
    }

    private int CalculateEarnedCredits()
    {
        if (ExamSystem.Instance == null)
        {
            return 0;
        }

        ExamResult[] allResults = ExamSystem.Instance.GetAllResults();
        if (allResults == null || allResults.Length == 0)
        {
            return 0;
        }

        HashSet<string> countedCourseIds = new HashSet<string>();
        int totalCredits = 0;

        for (int i = 0; i < allResults.Length; i++)
        {
            ExamResult result = allResults[i];
            if (result == null || string.IsNullOrEmpty(result.courseId))
            {
                continue;
            }

            if (result.credits <= 0 || result.score < 60 || !countedCourseIds.Add(result.courseId))
            {
                continue;
            }

            totalCredits += result.credits;
        }

        return totalCredits;
    }

    private string BuildCertificateStatusText()
    {
        if (ExamSystem.Instance == null || GameState.Instance == null)
        {
            return "证书：四级、六级与计算机等级会随学年推进逐步开放。";
        }

        int overallSemester = (GameState.Instance.CurrentYear - 1) * 2 + GameState.Instance.CurrentSemester;

        string cet4;
        if (ExamSystem.Instance.IsCET4Passed)
        {
            cet4 = "四级已过";
        }
        else if (overallSemester >= 2)
        {
            cet4 = "四级可报考";
        }
        else
        {
            cet4 = "四级待大一下后开放";
        }

        string cet6;
        if (ExamSystem.Instance.IsCET6Passed)
        {
            cet6 = "六级已过";
        }
        else if (ExamSystem.Instance.IsCET4Passed)
        {
            cet6 = "六级可报考";
        }
        else
        {
            cet6 = "六级待四级后开放";
        }

        string computer = ExamSystem.Instance.IsComputerLevelPassed
            ? "计算机已过"
            : GameState.Instance.CurrentYear >= 2 ? "计算机可报考" : "计算机待大二开放";

        return $"证书：{cet4} / {cet6}\n{computer}  ·  当前{GetYearSemesterLabel()}";
    }

    private string BuildJobProgressText()
    {
        if (JobSystem.Instance == null)
        {
            return "工作进度：达到解锁阶段后，这里会记录你的实习与副业安排。";
        }

        string currentPlan = "本回合未安排工作";
        if (!string.IsNullOrEmpty(JobSystem.Instance.currentInternshipId))
        {
            JobDefinitionData job = JobSystem.Instance.GetJob(JobSystem.Instance.currentInternshipId);
            currentPlan = $"本回合已安排实习：{(job != null ? job.name : JobSystem.Instance.currentInternshipId)}";
        }
        else if (!string.IsNullOrEmpty(JobSystem.Instance.currentSideHustleId))
        {
            JobDefinitionData job = JobSystem.Instance.GetJob(JobSystem.Instance.currentSideHustleId);
            currentPlan = $"本回合已安排副业：{(job != null ? job.name : JobSystem.Instance.currentSideHustleId)}";
        }

        string unlockHint;
        if (JobSystem.Instance.IsInternshipUnlocked() && JobSystem.Instance.IsSideHustleUnlocked())
        {
            unlockHint = "实习与副业都已开放，可按当前路线自由安排。";
        }
        else if (JobSystem.Instance.IsInternshipUnlocked())
        {
            unlockHint = "实习已开放；副业还需要继续推进人际或黑暗路线。";
        }
        else if (JobSystem.Instance.IsSideHustleUnlocked())
        {
            unlockHint = "副业已开放；实习还需要证书或 GPA 再往上走。";
        }
        else
        {
            unlockHint = "工作入口还在积累阶段，先提升学年、证书、GPA 或人际条件。";
        }

        return $"{currentPlan}\n累计实习 {JobSystem.Instance.totalInternshipCount} 次 / 连续副业 {JobSystem.Instance.consecutiveHustleRounds} 回合\n{unlockHint}";
    }

    private string GetYearSemesterLabel()
    {
        if (GameState.Instance == null)
        {
            return "当前学期";
        }

        string year = GameState.Instance.CurrentYear switch
        {
            1 => "大一",
            2 => "大二",
            3 => "大三",
            4 => "大四",
            _ => "大学"
        };
        string semester = GameState.Instance.CurrentSemester == 1 ? "上" : "下";
        return $"{year}{semester}";
    }

    // ========== 人际关系面板刷新 ==========

    public void RefreshRelationships()
    {
        if (AffinitySystem.Instance == null || NPCDatabase.Instance == null) return;

        // 清空旧列表
        foreach (Transform child in builder.npcListContent)
        {
            Destroy(child.gameObject);
        }

        // 获取所有NPC关系数据并按好感度降序排列
        var allRelationships = AffinitySystem.Instance.GetAllRelationships();
        List<NPCRelationshipData> sortedList = new List<NPCRelationshipData>(allRelationships.Values);
        sortedList.Sort((a, b) => b.affinity.CompareTo(a.affinity));

        // 创建NPC列表项
        foreach (var rel in sortedList)
        {
            NPCData npcData = NPCDatabase.Instance.GetNPC(rel.npcId);
            if (npcData == null) continue;

            CreateNPCListItem(rel, npcData);
        }

        // 默认选中第一个NPC
        if (sortedList.Count > 0)
        {
            SelectNPC(sortedList[0].npcId);
        }
    }

    private void CreateNPCListItem(NPCRelationshipData rel, NPCData npcData)
    {
        if (builder == null || builder.npcListContent == null || rel == null || npcData == null)
        {
            return;
        }

        if (TryCreateNPCListItemSafe(rel, npcData))
        {
            return;
        }

        GameObject item = new GameObject($"NPCItem_{rel.npcId}");
        item.transform.SetParent(builder.npcListContent, false);

        RectTransform rt = item.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 50);

        Image bg = item.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.20f, 1.0f);

        Button btn = item.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.15f, 0.15f, 0.20f, 1.0f);
        cb.highlightedColor = new Color(0.20f, 0.20f, 0.25f, 1.0f);
        cb.pressedColor = new Color(0.10f, 0.10f, 0.15f, 1.0f);
        btn.colors = cb;

        string npcId = rel.npcId;
        btn.onClick.AddListener(() => SelectNPC(npcId));

        // NPC名称 + 星级
        int stars = GetStarRating(rel.affinity);
        string starText = new string('★', stars);

        TextMeshProUGUI txt = item.AddComponent<TextMeshProUGUI>();
        txt.text = $"{npcData.displayName}  {starText}";
        txt.fontSize = 16f;
        txt.color = new Color(0.92f, 0.92f, 0.92f);
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform txtRT = txt.rectTransform;
        if (txtRT != null)
        {
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(10, 0);
            txtRT.offsetMax = new Vector2(-10, 0);
        }
    }

    private bool TryCreateNPCListItemSafe(NPCRelationshipData rel, NPCData npcData)
    {
        if (builder == null || builder.npcListContent == null || rel == null || npcData == null)
        {
            return false;
        }

        GameObject item = new GameObject($"NPCItem_{rel.npcId}");
        item.transform.SetParent(builder.npcListContent, false);

        RectTransform itemRT = item.AddComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(280, 50);

        Image bg = item.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.20f, 1.0f);

        Button btn = item.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.15f, 0.15f, 0.20f, 1.0f);
        cb.highlightedColor = new Color(0.20f, 0.20f, 0.25f, 1.0f);
        cb.pressedColor = new Color(0.10f, 0.10f, 0.15f, 1.0f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;

        string npcId = rel.npcId;
        btn.onClick.AddListener(() => SelectNPC(npcId));

        int stars = GetStarRating(rel.affinity);
        string starText = new string('\u2605', stars);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(item.transform, false);

        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10, 0);
        labelRT.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI txt = labelObj.AddComponent<TextMeshProUGUI>();
        txt.text = $"{npcData.displayName}  {starText}";
        txt.fontSize = 16f;
        txt.color = new Color(0.92f, 0.92f, 0.92f);
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = false;
        txt.overflowMode = TextOverflowModes.Ellipsis;

        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(txt);
        }

        return true;
    }

    private int GetStarRating(int affinity)
    {
        if (affinity >= 80) return 5;
        if (affinity >= 60) return 4;
        if (affinity >= 40) return 3;
        if (affinity >= 20) return 2;
        return 1;
    }

    private void SelectNPC(string npcId)
    {
        selectedNPCId = npcId;
        RefreshNPCDetail(npcId);
    }

    private void RefreshNPCDetail(string npcId)
    {
        if (AffinitySystem.Instance == null || NPCDatabase.Instance == null) return;

        NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(npcId);
        NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);

        if (rel == null || npcData == null) return;

        // NPC名称
        builder.txtNPCName.text = npcData.displayName;

        // 好感度
        builder.txtNPCAffinity.text = $"{rel.affinity} / 100";
        builder.imgAffinityBar.fillAmount = rel.affinity / 100f;
        if (builder.txtAffinityMeta != null)
        {
            builder.txtAffinityMeta.text = BuildAffinityMetaText(rel);
        }

        // 关系等级
        builder.txtAffinityLevel.text = $"关系等级：{GetAffinityLevelDisplayName(rel.level)}";

        // 恋爱状态
        builder.txtRomanceState.text = $"恋爱状态：{GetRomanceStateDisplayName(rel.romanceState)}";

        // 性格偏好
        List<string> likedActions = new List<string>();
        List<string> dislikedActions = new List<string>();

        if (npcData.likedActionIds != null)
        {
            foreach (string actionId in npcData.likedActionIds)
            {
                SocialActionDefinition action = NPCDatabase.Instance.GetSocialAction(actionId);
                if (action != null) likedActions.Add(action.displayName);
            }
        }

        if (npcData.dislikedActionIds != null)
        {
            foreach (string actionId in npcData.dislikedActionIds)
            {
                SocialActionDefinition action = NPCDatabase.Instance.GetSocialAction(actionId);
                if (action != null) dislikedActions.Add(action.displayName);
            }
        }

        string likedText = likedActions.Count > 0 ? string.Join("、", likedActions) : "无";
        string dislikedText = dislikedActions.Count > 0 ? string.Join("、", dislikedActions) : "无";
        builder.txtPreferences.text = $"喜欢：{likedText}\n不喜欢：{dislikedText}";

        // 互动记录
        RefreshInteractionRecords(npcId);
    }

    private void RefreshInteractionRecords(string npcId)
    {
        // 清空旧记录
        foreach (Transform child in builder.interactionRecordContainer)
        {
            if (child.name.StartsWith("Record"))
            {
                Destroy(child.gameObject);
            }
        }

        NPCRelationshipData relationship = AffinitySystem.Instance.GetRelationship(npcId);
        List<string> records = relationship != null && relationship.memories != null
            ? relationship.memories
            : null;

        if (records == null || records.Count == 0)
        {
            GameObject noRecord = new GameObject("NoRecord");
            noRecord.transform.SetParent(builder.interactionRecordContainer, false);
            RectTransform rt = noRecord.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(820, 42);

            TextMeshProUGUI txt = noRecord.AddComponent<TextMeshProUGUI>();
            txt.text = "最近还没有留下互动记录\n多去打招呼、聊天或一起行动，这里会慢慢记下你们的来往。";
            txt.fontSize = 14f;
            txt.color = new Color(0.55f, 0.55f, 0.60f);
            txt.alignment = TextAlignmentOptions.Left;
            txt.enableWordWrapping = true;
            return;
        }

        // 显示最近3条记录（倒序）
        int startIndex = Mathf.Max(0, records.Count - 3);
        for (int i = records.Count - 1; i >= startIndex; i--)
        {
            string record = records[i];

            GameObject recordObj = new GameObject($"Record{i}");
            recordObj.transform.SetParent(builder.interactionRecordContainer, false);
            RectTransform rt = recordObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(820, 25);

            TextMeshProUGUI txt = recordObj.AddComponent<TextMeshProUGUI>();
            txt.text = record;
            txt.fontSize = 14f;
            txt.color = new Color(0.75f, 0.75f, 0.80f);
            txt.alignment = TextAlignmentOptions.Left;
        }
    }

    private string GetAffinityLevelDisplayName(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Stranger: return "陌生人";
            case AffinityLevel.Acquaintance: return "熟人";
            case AffinityLevel.Friend: return "朋友";
            case AffinityLevel.CloseFriend: return "亲密朋友";
            case AffinityLevel.BestFriend: return "挚友";
            case AffinityLevel.Lover: return "恋人";
            default: return "未知";
        }
    }

    private string BuildStatusMetaText(string statusName, int value, bool isInverseGood)
    {
        if (isInverseGood)
        {
            if (value <= 20) return $"{statusName}很稳";
            if (value < 40) return $"再降低 {value - 20} 进入稳定";
            if (value < 60) return $"再降低 {value - 40} 回到可控";
            if (value < 80) return $"再升高 {80 - value} 进入高压";
            if (value < 100) return $"再升高 {100 - value} 逼近爆表";
            return $"{statusName}已爆表";
        }

        if (value < 20) return $"再提升 {20 - value} 摆脱低迷";
        if (value < 40) return $"再提升 {40 - value} 进入普通";
        if (value < 70) return $"再提升 {70 - value} 进入高涨";
        if (value < 90) return $"再提升 {90 - value} 接近最佳";
        if (value < 100) return $"再提升 {100 - value} 达到满值";
        return $"{statusName}已满";
    }

    private string BuildAffinityMetaText(NPCRelationshipData rel)
    {
        if (rel == null)
        {
            return string.Empty;
        }

        if (rel.romanceState == RomanceState.Dating)
        {
            return rel.affinity >= 100 ? "恋人关系已满" : $"距离恋人满值还差 {100 - rel.affinity}";
        }

        if (rel.affinity >= 80)
        {
            return rel.affinity >= 100 ? "挚友关系已满" : $"距离满值还差 {100 - rel.affinity}";
        }

        int nextThreshold = GetNextAffinityThreshold(rel.affinity, rel.romanceState);
        if (nextThreshold <= rel.affinity)
        {
            return "已达到当前最高关系档";
        }

        int remaining = nextThreshold - rel.affinity;
        string nextLevel = GetAffinityLevelDisplayName(GetNextAffinityLevel(rel.affinity, rel.romanceState));
        return $"还差 {remaining} 进入{nextLevel}";
    }

    private int GetNextAffinityThreshold(int affinity, RomanceState romanceState)
    {
        if (affinity < 20) return 20;
        if (affinity < 40) return 40;
        if (affinity < 60) return 60;
        if (affinity < 80) return 80;
        if (affinity < 100 && romanceState != RomanceState.Dating) return 100;
        return affinity;
    }

    private AffinityLevel GetNextAffinityLevel(int affinity, RomanceState romanceState)
    {
        if (affinity < 20) return AffinityLevel.Acquaintance;
        if (affinity < 40) return AffinityLevel.Friend;
        if (affinity < 60) return AffinityLevel.CloseFriend;
        if (affinity < 80) return AffinityLevel.BestFriend;
        return romanceState == RomanceState.Dating ? AffinityLevel.Lover : AffinityLevel.BestFriend;
    }

    private string GetRomanceStateDisplayName(RomanceState state)
    {
        switch (state)
        {
            case RomanceState.None: return "无";
            case RomanceState.Crushing: return "暗恋中";
            case RomanceState.Dating: return "恋爱中";
            case RomanceState.BrokenUp: return "已分手";
            case RomanceState.Hostile: return "敌对";
            default: return "未知";
        }
    }

    // ========== 任务面板刷新 ==========

    public void RefreshQuests()
    {
        if (builder.questListContent == null) return;

        // 清空旧内容
        foreach (Transform child in builder.questListContent)
        {
            Destroy(child.gameObject);
        }

        // 检查MissionSystem是否可用
        if (MissionSystem.Instance == null)
        {
            CreateQuestPlaceholder("任务情报正在整理中。\n先继续推进回合、提升属性或接触人物，新的目标会逐步出现。");
            return;
        }

        List<MissionDefinition> activeMissions = MissionSystem.Instance.GetActiveMissions();
        List<MissionDefinition> availableMissions = MissionSystem.Instance.GetAvailableMissions();
        List<MissionDefinition> completedMissions = MissionSystem.Instance.GetCompletedMissions();
        List<MissionDefinition> failedMissions = MissionSystem.Instance.GetFailedMissions();

        if (activeMissions.Count == 0 && availableMissions.Count == 0 &&
            completedMissions.Count == 0 && failedMissions.Count == 0)
        {
            CreateQuestPlaceholder("当前还没有可追踪任务。\n继续推进主线、社交、考试或社团路线，很快就会刷出新的目标。");
            return;
        }

        // 显示进行中的任务
        if (activeMissions.Count > 0)
        {
            CreateQuestSectionHeader("进行中");
            foreach (var mission in activeMissions)
            {
                CreateQuestCard(mission, false);
            }
        }

        if (availableMissions.Count > 0)
        {
            CreateQuestSectionHeader("可接取");
            foreach (var mission in availableMissions)
            {
                CreateQuestCard(mission, false, isAvailable: true);
            }
        }

        // 显示已完成的任务（最近5个）
        if (completedMissions.Count > 0)
        {
            CreateQuestSectionHeader("已完成");
            int showCount = Mathf.Min(completedMissions.Count, 5);
            for (int i = completedMissions.Count - showCount; i < completedMissions.Count; i++)
            {
                CreateQuestCard(completedMissions[i], true);
            }
        }

        if (failedMissions.Count > 0)
        {
            CreateQuestSectionHeader("已失败");
            int showCount = Mathf.Min(failedMissions.Count, 5);
            for (int i = failedMissions.Count - showCount; i < failedMissions.Count; i++)
            {
                CreateQuestCard(failedMissions[i], false, isAvailable: false, isFailed: true);
            }
        }
    }

    private void CreateQuestPlaceholder(string message)
    {
        GameObject placeholder = new GameObject("QuestPlaceholder");
        placeholder.transform.SetParent(builder.questListContent, false);

        RectTransform rt = placeholder.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1150, 96);

        TextMeshProUGUI txt = placeholder.AddComponent<TextMeshProUGUI>();
        txt.text = message;
        txt.fontSize = 18f;
        txt.color = new Color(0.55f, 0.55f, 0.60f); // TextGray
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = true;
    }

    private void CreateQuestSectionHeader(string title)
    {
        GameObject header = new GameObject($"QuestHeader_{title}");
        header.transform.SetParent(builder.questListContent, false);

        RectTransform rt = header.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1150, 30);

        TextMeshProUGUI txt = header.AddComponent<TextMeshProUGUI>();
        txt.text = $"【{title}】";
        txt.fontSize = 18f;
        txt.color = new Color(1.0f, 0.85f, 0.30f); // TextGold
        txt.alignment = TextAlignmentOptions.Left;
    }

    private void CreateQuestCard(MissionDefinition mission, bool isCompleted, bool isAvailable = false, bool isFailed = false)
    {
        if (mission == null || builder == null || builder.questListContent == null)
        {
            return;
        }

        // 卡片容器
        GameObject card = new GameObject($"QuestCard_{mission.missionId}");
        card.transform.SetParent(builder.questListContent, false);

        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(1150, 0); // 高度由ContentSizeFitter决定

        Image cardBg = card.AddComponent<Image>();
        if (isCompleted)
        {
            cardBg.color = new Color(0.10f, 0.10f, 0.14f, 0.80f);
        }
        else if (isFailed)
        {
            cardBg.color = new Color(0.20f, 0.11f, 0.11f, 0.92f);
        }
        else if (isAvailable)
        {
            cardBg.color = new Color(0.14f, 0.16f, 0.10f, 0.95f);
        }
        else
        {
            cardBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        }

        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = card.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 第一行：类型标签 + 任务名称
        GameObject titleRow = new GameObject("TitleRow");
        titleRow.transform.SetParent(card.transform, false);
        RectTransform titleRowRT = titleRow.AddComponent<RectTransform>();
        titleRowRT.sizeDelta = new Vector2(1126, 28);

        HorizontalLayoutGroup titleHLG = titleRow.AddComponent<HorizontalLayoutGroup>();
        titleHLG.spacing = 8;
        titleHLG.childAlignment = TextAnchor.MiddleLeft;
        titleHLG.childControlWidth = false;
        titleHLG.childControlHeight = false;

        // 类型标签
        GameObject typeLabel = new GameObject("TypeLabel");
        typeLabel.transform.SetParent(titleRow.transform, false);
        RectTransform typeLabelRT = typeLabel.AddComponent<RectTransform>();
        typeLabelRT.sizeDelta = new Vector2(50, 24);

        Image typeLabelBg = typeLabel.AddComponent<Image>();
        typeLabelBg.color = mission.type == MissionType.MainStory
            ? new Color(0.80f, 0.20f, 0.20f, 1.0f)  // 主线红色
            : new Color(0.20f, 0.40f, 0.80f, 1.0f);  // 支线蓝色

        TextMeshProUGUI typeTxt = new GameObject("TypeText").AddComponent<TextMeshProUGUI>();
        typeTxt.transform.SetParent(typeLabel.transform, false);
        RectTransform typeTxtRT = typeTxt.GetComponent<RectTransform>();
        typeTxtRT.anchorMin = Vector2.zero;
        typeTxtRT.anchorMax = Vector2.one;
        typeTxtRT.offsetMin = Vector2.zero;
        typeTxtRT.offsetMax = Vector2.zero;
        typeTxt.text = mission.type == MissionType.MainStory ? "主线" : "支线";
        typeTxt.fontSize = 13f;
        typeTxt.color = new Color(0.92f, 0.92f, 0.92f); // TextWhite
        typeTxt.alignment = TextAlignmentOptions.Center;

        // 任务名称
        GameObject nameObj = new GameObject("MissionName");
        nameObj.transform.SetParent(titleRow.transform, false);
        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.sizeDelta = new Vector2(900, 28);

        TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
        string completedMark = isCompleted ? " ✓" : isFailed ? " ✕" : isAvailable ? " [可接取]" : "";
        nameTxt.text = mission.missionName + completedMark;
        nameTxt.fontSize = 17f;
        if (isCompleted)
        {
            nameTxt.color = new Color(0.55f, 0.55f, 0.60f);
        }
        else if (isFailed)
        {
            nameTxt.color = new Color(1.0f, 0.68f, 0.68f);
        }
        else
        {
            nameTxt.color = new Color(1.0f, 0.85f, 0.30f);
        }
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.enableWordWrapping = false;
        nameTxt.overflowMode = TextOverflowModes.Ellipsis;

        GameObject descObj = new GameObject("MissionDescription");
        descObj.transform.SetParent(card.transform, false);
        RectTransform descRT = descObj.AddComponent<RectTransform>();
        descRT.sizeDelta = new Vector2(1126, 42);

        TextMeshProUGUI descTxt = descObj.AddComponent<TextMeshProUGUI>();
        descTxt.text = mission.description;
        descTxt.fontSize = 14f;
        descTxt.color = isFailed
            ? new Color(0.94f, 0.80f, 0.80f)
            : new Color(0.92f, 0.92f, 0.92f);
        descTxt.alignment = TextAlignmentOptions.Left;
        descTxt.enableWordWrapping = true;
        descTxt.overflowMode = TextOverflowModes.Ellipsis;
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(descTxt);
        }

        // 目标进度（仅进行中任务）
        if (!isCompleted)
        {
            MissionRuntimeData runtimeData = MissionSystem.Instance.GetMissionRuntimeData(mission.missionId);
            if (runtimeData != null && runtimeData.objectives != null)
            {
                foreach (var objective in runtimeData.objectives)
                {
                    CreateObjectiveRow(card.transform, objective);
                }
            }
            else if (mission.objectives != null)
            {
                foreach (var objective in mission.objectives)
                {
                    CreateObjectivePreviewRow(card.transform, objective, isFailed);
                }
            }
        }

        if (isAvailable)
        {
            CreateQuestActionButton(card.transform, "接取任务", new Color(0.86f, 0.66f, 0.22f, 1f), () =>
            {
                if (MissionSystem.Instance != null && MissionSystem.Instance.AcceptMission(mission.missionId))
                {
                    RefreshQuests();
                }
            });
        }
        else if (!isCompleted && !isFailed)
        {
            CreateQuestActionButton(card.transform, "完整任务面板", new Color(0.34f, 0.47f, 0.72f, 1f), () =>
            {
                OnOpenMissionPanelClicked();
            });
        }
    }

    private void CreateObjectivePreviewRow(Transform parent, MissionObjective objective, bool isFailed)
    {
        GameObject objRow = new GameObject("ObjectivePreviewRow");
        objRow.transform.SetParent(parent, false);
        RectTransform objRowRT = objRow.AddComponent<RectTransform>();
        objRowRT.sizeDelta = new Vector2(1126, 20);

        TextMeshProUGUI objTxt = objRow.AddComponent<TextMeshProUGUI>();
        objTxt.text = $"  {objective.description}";
        objTxt.fontSize = 14f;
        objTxt.color = isFailed
            ? new Color(0.90f, 0.72f, 0.72f)
            : new Color(0.88f, 0.88f, 0.84f);
        objTxt.alignment = TextAlignmentOptions.Left;
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(objTxt);
        }
    }

    private void CreateObjectiveRow(Transform parent, MissionObjective objective)
    {
        // 目标描述行
        GameObject objRow = new GameObject("ObjectiveRow");
        objRow.transform.SetParent(parent, false);
        RectTransform objRowRT = objRow.AddComponent<RectTransform>();
        objRowRT.sizeDelta = new Vector2(1126, 20);

        TextMeshProUGUI objTxt = objRow.AddComponent<TextMeshProUGUI>();
        string statusIcon = objective.isCompleted ? "✓ " : "  ";
        objTxt.text = $"{statusIcon}{objective.description}: {objective.currentValue}/{objective.targetValue}";
        objTxt.fontSize = 14f;
        objTxt.color = objective.isCompleted
            ? new Color(0.40f, 0.80f, 0.40f)  // 已完成绿色
            : new Color(0.92f, 0.92f, 0.92f); // TextWhite
        objTxt.alignment = TextAlignmentOptions.Left;

        // 进度条
        GameObject barRow = new GameObject("ProgressBarRow");
        barRow.transform.SetParent(parent, false);
        RectTransform barRowRT = barRow.AddComponent<RectTransform>();
        barRowRT.sizeDelta = new Vector2(1126, 10);

        // 进度条背景
        Image barBg = barRow.AddComponent<Image>();
        barBg.color = new Color(0.20f, 0.20f, 0.25f, 1.0f);

        // 进度条填充
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barRow.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0);
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.pivot = new Vector2(0, 0.5f);

        float progress = objective.targetValue > 0
            ? Mathf.Clamp01((float)objective.currentValue / objective.targetValue)
            : 0f;
        fillRT.sizeDelta = new Vector2(1126 * progress, 0);

        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = objective.isCompleted
            ? new Color(0.40f, 0.80f, 0.40f)  // 已完成绿色
            : new Color(0.30f, 0.70f, 0.30f);  // 进行中绿色
    }

    private void CreateQuestActionButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(label.Replace(" ", string.Empty));
        buttonObj.transform.SetParent(parent, false);

        RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.sizeDelta = new Vector2(180f, 34f);

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredWidth = 180f;
        layout.preferredHeight = 34f;

        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = color;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonBg;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        TextMeshProUGUI buttonText = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        buttonText.transform.SetParent(buttonObj.transform, false);
        RectTransform textRT = buttonText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        buttonText.text = label;
        buttonText.fontSize = 14f;
        buttonText.color = new Color(0.98f, 0.96f, 0.92f);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.enableWordWrapping = false;

        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(buttonText);
        }
    }

    // ========== 按钮事件处理 ==========

    private void OnSocialInteractClicked()
    {
        if (string.IsNullOrEmpty(selectedNPCId))
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("未选择互动对象", "先在左侧列表中选中一个角色，再打开社交互动。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }

        // 打开NPCInteractionMenu
        if (NPCInteractionMenu.Instance != null)
        {
            NPCInteractionMenu.Instance.ShowForNPC(selectedNPCId);
        }
        else if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification("互动菜单不可用", "NPC 互动菜单还没有成功初始化，现在暂时无法打开。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
        }
    }

    private void OnOpenMissionPanelClicked()
    {
        if (MissionPanelBuilder.Instance == null)
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("任务面板不可用", "任务列表还没有成功初始化，现在暂时无法从信息页跳转。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }

        ClosePanel();
        MissionPanelBuilder.Instance.OpenPanel();
    }

    // ========== 事件订阅 ==========

    private void SubscribeEvents()
    {
        if (isSubscribed) return;

        if (GameState.Instance != null)
        {
            GameState.Instance.OnStateChanged += OnGameStateChanged;
            GameState.Instance.OnMoneyChanged += OnMoneyChanged;
        }

        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.OnAttributesChanged += OnAttributesChanged;
        }

        AttributeGradeSettings.OnThresholdsChanged += OnAttributeGradeThresholdsChanged;

        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged += OnAffinityChanged;
        }

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionUnlocked += OnMissionStateChanged;
            MissionSystem.Instance.OnMissionAccepted += OnMissionStateChanged;
            MissionSystem.Instance.OnMissionCompleted += OnMissionCompleted;
            MissionSystem.Instance.OnMissionFailed += OnMissionStateChanged;
            MissionSystem.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
        }

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        if (GameState.Instance != null)
        {
            GameState.Instance.OnStateChanged -= OnGameStateChanged;
            GameState.Instance.OnMoneyChanged -= OnMoneyChanged;
        }

        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.OnAttributesChanged -= OnAttributesChanged;
        }

        AttributeGradeSettings.OnThresholdsChanged -= OnAttributeGradeThresholdsChanged;

        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged -= OnAffinityChanged;
        }

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionUnlocked -= OnMissionStateChanged;
            MissionSystem.Instance.OnMissionAccepted -= OnMissionStateChanged;
            MissionSystem.Instance.OnMissionCompleted -= OnMissionCompleted;
            MissionSystem.Instance.OnMissionFailed -= OnMissionStateChanged;
            MissionSystem.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
        }

        isSubscribed = false;
    }

    private void OnGameStateChanged()
    {
        if (currentTabIndex == 0 && builder.panelRoot.activeSelf)
        {
            RefreshPlayerInfo();
        }
    }

    private void OnMoneyChanged(int money)
    {
        if (currentTabIndex == 0 && builder.panelRoot.activeSelf)
        {
            RefreshPlayerInfo();
        }
    }

    private void OnAttributesChanged()
    {
        if (currentTabIndex == 0 && builder.panelRoot.activeSelf)
        {
            RefreshPlayerInfo();
        }
    }

    private void OnAttributeGradeThresholdsChanged()
    {
        if (builder != null && builder.panelRoot != null && builder.panelRoot.activeSelf && currentTabIndex == 0)
        {
            RefreshPlayerInfo();
        }
    }

    private void OnAffinityChanged(string npcId, int oldAffinity, int newAffinity, int delta)
    {
        if (currentTabIndex == 1 && builder.panelRoot.activeSelf)
        {
            RefreshRelationships();
        }
    }

    private void OnMissionCompleted(MissionDefinition mission)
    {
        if (currentTabIndex == 2 && builder.panelRoot.activeSelf)
        {
            RefreshQuests();
        }
    }

    private void OnMissionStateChanged(MissionDefinition mission)
    {
        if (currentTabIndex == 2 && builder.panelRoot.activeSelf)
        {
            RefreshQuests();
        }
    }

    private void OnObjectiveUpdated(MissionDefinition mission, MissionObjective objective)
    {
        if (currentTabIndex == 2 && builder.panelRoot.activeSelf)
        {
            RefreshQuests();
        }
    }
}
