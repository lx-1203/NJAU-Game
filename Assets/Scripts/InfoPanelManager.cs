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
    }

    // ========== 公共接口 ==========

    /// <summary>打开面板（默认显示个人信息标签）</summary>
    public void OpenPanel(int defaultTab = 0)
    {
        if (builder.panelRoot == null) return;

        if (builder.panelRoot.activeSelf)
        {
            SwitchTab(defaultTab);
            return;
        }

        builder.panelRoot.SetActive(true);
        builder.overlayObj.SetActive(true);

        SwitchTab(defaultTab);
        SubscribeEvents();
    }

    /// <summary>关闭面板</summary>
    public void ClosePanel()
    {
        if (builder.panelRoot == null) return;

        builder.panelRoot.SetActive(false);
        builder.overlayObj.SetActive(false);

        UnsubscribeEvents();
    }

    public bool IsOpen => builder != null && builder.panelRoot != null && builder.panelRoot.activeSelf;

    /// <summary>切换标签页</summary>
    public void SwitchTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= 3) return;

        currentTabIndex = tabIndex;

        // 隐藏所有子面板
        builder.playerInfoPanel.SetActive(false);
        builder.relationshipPanel.SetActive(false);
        builder.questPanel.SetActive(false);

        // 更新标签按钮状态
        for (int i = 0; i < builder.tabButtons.Length; i++)
        {
            ColorBlock cb = builder.tabButtons[i].colors;
            if (i == tabIndex)
            {
                cb.normalColor = new Color(0.30f, 0.50f, 0.80f, 1.0f); // TabActiveColor
            }
            else
            {
                cb.normalColor = new Color(0.15f, 0.15f, 0.20f, 1.0f); // TabInactiveColor
            }
            builder.tabButtons[i].colors = cb;
        }

        // 显示目标子面板并刷新数据
        switch (tabIndex)
        {
            case 0:
                builder.playerInfoPanel.SetActive(true);
                builder.txtTitle.text = "个人信息";
                RefreshPlayerInfo();
                break;
            case 1:
                builder.relationshipPanel.SetActive(true);
                builder.txtTitle.text = "人际关系";
                RefreshRelationships();
                break;
            case 2:
                builder.questPanel.SetActive(true);
                builder.txtTitle.text = "任务";
                RefreshQuests();
                break;
        }
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
        builder.txtPlayerInfo.text = $"性别：{gs.PlayerGender}    专业：{gs.PlayerMajor}";

        int age = 18 + (gs.CurrentYear - 1);
        builder.txtTimeInfo.text = $"{gs.GetTimeDescription()}    年龄：{age}岁";

        // 核心属性（使用AttributeBar）
        RefreshAttributeBars();

        // 状态值
        builder.txtStress.text = $"{pa.Stress}%";
        builder.imgStressBar.fillAmount = pa.Stress / 100f;

        builder.txtMood.text = $"{pa.Mood}%";
        builder.imgMoodBar.fillAmount = pa.Mood / 100f;

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

        builder.txtCredits.text = "已修学分：-- / 121";

        // 经济信息
        builder.txtMoney.text = $"当前金钱：¥{gs.Money}";

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
                AttributeBar bar = AttributeBar.Create(builder.attributeContainer);
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

        RectTransform txtRT = txt.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(10, 0);
        txtRT.offsetMax = new Vector2(-10, 0);
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
            rt.sizeDelta = new Vector2(820, 25);

            TextMeshProUGUI txt = noRecord.AddComponent<TextMeshProUGUI>();
            txt.text = "暂无互动记录";
            txt.fontSize = 14f;
            txt.color = new Color(0.55f, 0.55f, 0.60f);
            txt.alignment = TextAlignmentOptions.Left;
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
            CreateQuestPlaceholder("任务系统未初始化");
            return;
        }

        List<MissionDefinition> activeMissions = MissionSystem.Instance.GetActiveMissions();
        List<MissionDefinition> completedMissions = MissionSystem.Instance.GetCompletedMissions();

        if (activeMissions.Count == 0 && completedMissions.Count == 0)
        {
            CreateQuestPlaceholder("暂无任务");
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
    }

    private void CreateQuestPlaceholder(string message)
    {
        GameObject placeholder = new GameObject("QuestPlaceholder");
        placeholder.transform.SetParent(builder.questListContent, false);

        RectTransform rt = placeholder.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1150, 50);

        TextMeshProUGUI txt = placeholder.AddComponent<TextMeshProUGUI>();
        txt.text = message;
        txt.fontSize = 18f;
        txt.color = new Color(0.55f, 0.55f, 0.60f); // TextGray
        txt.alignment = TextAlignmentOptions.Center;
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

    private void CreateQuestCard(MissionDefinition mission, bool isCompleted)
    {
        // 卡片容器
        GameObject card = new GameObject($"QuestCard_{mission.missionId}");
        card.transform.SetParent(builder.questListContent, false);

        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(1150, 0); // 高度由ContentSizeFitter决定

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = isCompleted
            ? new Color(0.10f, 0.10f, 0.14f, 0.80f)
            : new Color(0.12f, 0.12f, 0.18f, 0.95f); // CardBgColor

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
        string completedMark = isCompleted ? " ✓" : "";
        nameTxt.text = mission.missionName + completedMark;
        nameTxt.fontSize = 17f;
        nameTxt.color = isCompleted
            ? new Color(0.55f, 0.55f, 0.60f)           // TextGray
            : new Color(1.0f, 0.85f, 0.30f);            // TextGold
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.enableWordWrapping = false;
        nameTxt.overflowMode = TextOverflowModes.Ellipsis;

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

    // ========== 按钮事件处理 ==========

    private void OnSocialInteractClicked()
    {
        if (string.IsNullOrEmpty(selectedNPCId)) return;

        // 打开NPCInteractionMenu
        if (NPCInteractionMenu.Instance != null)
        {
            NPCInteractionMenu.Instance.ShowForNPC(selectedNPCId);
        }
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

        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged += OnAffinityChanged;
        }

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionCompleted += OnMissionCompleted;
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

        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged -= OnAffinityChanged;
        }

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionCompleted -= OnMissionCompleted;
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

    private void OnObjectiveUpdated(MissionDefinition mission, MissionObjective objective)
    {
        if (currentTabIndex == 2 && builder.panelRoot.activeSelf)
        {
            RefreshQuests();
        }
    }
}
