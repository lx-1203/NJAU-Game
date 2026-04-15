using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// NPC 社交互动浮层菜单 —— 纯代码创建
/// 点击"社交"按钮后弹出，展示可互动的 NPC 列表或指定 NPC 的社交行动
/// 单例（不 DontDestroyOnLoad，跟 HUD Canvas 同生命周期）
/// </summary>
public class NPCInteractionMenu : MonoBehaviour
{
    // ========== 单例 ==========
    public static NPCInteractionMenu Instance { get; private set; }

    // ========== UI 引用 ==========
    private Canvas menuCanvas;
    private GameObject menuPanel;
    private GameObject maskObj;

    // ========== 状态 ==========
    private string currentNpcId;
    private bool isMenuOpen;

    // ========== 运行时缓存 ==========
    private Transform contentContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI levelText;
    private Image affinityFill;
    private GameObject headerGroup;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    // ========== 颜色方案（与 HUDBuilder 一致） ==========
    private static readonly Color PanelBg          = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color ButtonNormal     = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ButtonHover      = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color ButtonPressed    = new Color(0.15f, 0.25f, 0.50f, 1.0f);
    private static readonly Color TextWhite        = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold         = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color MaskColor        = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color BarBgColor       = new Color(0.15f, 0.15f, 0.20f, 1.0f);
    private static readonly Color BarFillColor     = new Color(0.30f, 0.70f, 0.95f, 1.0f);
    private static readonly Color CloseButtonColor = new Color(0.50f, 0.20f, 0.20f, 1.0f);
    private static readonly Color CloseHoverColor  = new Color(0.65f, 0.30f, 0.30f, 1.0f);
    private static readonly Color RomanceButtonColor  = new Color(0.60f, 0.25f, 0.45f, 1.0f);
    private static readonly Color RomanceHoverColor   = new Color(0.70f, 0.35f, 0.55f, 1.0f);
    private static readonly Color RomancePressedColor = new Color(0.50f, 0.18f, 0.38f, 1.0f);
    private static readonly Color DisabledTextColor   = new Color(0.45f, 0.45f, 0.50f);
    private static readonly Color InfoLabelBg         = new Color(0.15f, 0.12f, 0.20f, 0.80f);

    // ========== 布局常量 ==========
    private const float PanelWidth = 600f;
    private const float PanelHeight = 500f;
    private const float ButtonHeight = 56f;
    private const float ButtonSpacing = 6f;

    // ========== 属性 ==========
    public bool IsMenuOpen => isMenuOpen;

    // ========== 生命周期 ==========

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== 对外接口 ==========

    /// <summary>
    /// 初始化：接收 HUD Canvas，创建菜单面板（默认隐藏）
    /// </summary>
    public void Initialize(Canvas parentCanvas)
    {
        menuCanvas = parentCanvas;
        CreateMenuUI();
    }

    /// <summary>
    /// 打开菜单
    /// npcId == null → 显示 NPC 列表选择模式
    /// npcId != null → 直接显示该 NPC 的社交行动
    /// </summary>
    public void ShowForNPC(string npcId)
    {
        if (npcId == null)
        {
            // NPC 列表选择模式
            ShowNPCList();
        }
        else
        {
            // 指定 NPC 的社交行动
            ShowNPCActions(npcId);
        }

        maskObj.SetActive(true);
        menuPanel.SetActive(true);
        isMenuOpen = true;
    }

    /// <summary>
    /// 关闭菜单
    /// </summary>
    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (maskObj != null) maskObj.SetActive(false);
        isMenuOpen = false;
        currentNpcId = null;
    }

    // ====================================================================
    //  UI 构建
    // ====================================================================

    private void CreateMenuUI()
    {
        // ===== 全屏半透明遮罩 =====
        maskObj = new GameObject("InteractionMenuMask");
        maskObj.transform.SetParent(menuCanvas.transform, false);

        RectTransform maskRT = maskObj.AddComponent<RectTransform>();
        maskRT.anchorMin = Vector2.zero;
        maskRT.anchorMax = Vector2.one;
        maskRT.offsetMin = Vector2.zero;
        maskRT.offsetMax = Vector2.zero;

        Image maskImg = maskObj.AddComponent<Image>();
        maskImg.color = MaskColor;

        // 遮罩点击关闭
        Button maskBtn = maskObj.AddComponent<Button>();
        maskBtn.transition = Selectable.Transition.None;
        maskBtn.onClick.AddListener(CloseMenu);

        // ===== 中央面板 =====
        menuPanel = new GameObject("InteractionMenuPanel");
        menuPanel.transform.SetParent(menuCanvas.transform, false);

        RectTransform panelRT = menuPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = menuPanel.AddComponent<Image>();
        panelBg.color = PanelBg;

        // 面板垂直布局
        VerticalLayoutGroup vlg = menuPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // ----- 顶部：标题行（NPC名字 + 好感度） -----
        headerGroup = new GameObject("HeaderGroup");
        headerGroup.transform.SetParent(menuPanel.transform, false);
        RectTransform headerRT = headerGroup.AddComponent<RectTransform>();
        headerRT.sizeDelta = new Vector2(0, 70f);

        // NPC 名字
        titleText = CreateTMPText("TitleText", headerGroup.transform, "选择NPC",
            26f, TextGold, TextAlignmentOptions.Left, new Vector2(300, 32f));
        RectTransform titleRT = titleText.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.55f);
        titleRT.anchorMax = new Vector2(0.6f, 1f);
        titleRT.offsetMin = new Vector2(4, 0);
        titleRT.offsetMax = Vector2.zero;

        // 好感等级文字
        levelText = CreateTMPText("LevelText", headerGroup.transform, "",
            18f, TextWhite, TextAlignmentOptions.Right, new Vector2(200, 28f));
        RectTransform levelRT = levelText.GetComponent<RectTransform>();
        levelRT.anchorMin = new Vector2(0.6f, 0.55f);
        levelRT.anchorMax = new Vector2(1f, 1f);
        levelRT.offsetMin = Vector2.zero;
        levelRT.offsetMax = new Vector2(-4, 0);

        // 好感度条背景
        GameObject barBg = new GameObject("AffinityBarBg");
        barBg.transform.SetParent(headerGroup.transform, false);
        RectTransform barBgRT = barBg.AddComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0, 0.05f);
        barBgRT.anchorMax = new Vector2(1, 0.40f);
        barBgRT.offsetMin = new Vector2(4, 0);
        barBgRT.offsetMax = new Vector2(-4, 0);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = BarBgColor;

        // 好感度条填充
        GameObject barFillObj = new GameObject("AffinityBarFill");
        barFillObj.transform.SetParent(barBg.transform, false);
        RectTransform fillRT = barFillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        affinityFill = barFillObj.AddComponent<Image>();
        affinityFill.color = BarFillColor;

        // ----- 中部：ScrollView -----
        CreateScrollView();

        // ----- 底部：关闭按钮 -----
        CreateCloseButton();

        // 默认隐藏
        maskObj.SetActive(false);
        menuPanel.SetActive(false);
    }

    private void CreateScrollView()
    {
        // ScrollView 根
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(menuPanel.transform, false);

        RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.sizeDelta = new Vector2(0, 320f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.06f, 0.06f, 0.10f, 0.60f);

        // Mask
        Mask mask = scrollObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        scrollRect.viewport = vpRT;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup cvlg = content.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = ButtonSpacing;
        cvlg.padding = new RectOffset(8, 8, 8, 8);
        cvlg.childAlignment = TextAnchor.UpperCenter;
        cvlg.childControlWidth = true;
        cvlg.childControlHeight = false;
        cvlg.childForceExpandWidth = true;
        cvlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        contentContainer = content.transform;
    }

    private void CreateCloseButton()
    {
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(menuPanel.transform, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 42f);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = CloseButtonColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = CloseButtonColor;
        cb.highlightedColor = CloseHoverColor;
        cb.pressedColor = new Color(0.40f, 0.15f, 0.15f, 1.0f);
        cb.selectedColor = CloseButtonColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(CloseMenu);

        TextMeshProUGUI btnText = CreateTMPText("CloseLabel", btnObj.transform, "关闭",
            20f, TextWhite, TextAlignmentOptions.Center, new Vector2(0, 42f));
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        ApplyChineseFont(btnText);
    }

    // ====================================================================
    //  NPC 列表模式
    // ====================================================================

    private void ShowNPCList()
    {
        currentNpcId = null;

        // 设置标题
        titleText.text = "选择NPC";
        levelText.text = "";
        SetAffinityBar(0);
        headerGroup.SetActive(true);

        ClearButtons();

        if (NPCDatabase.Instance == null)
        {
            Debug.LogWarning("[NPCInteractionMenu] NPCDatabase 未初始化");
            return;
        }

        NPCData[] allNPCs = NPCDatabase.Instance.GetAllNPCs();
        if (allNPCs.Length == 0)
        {
            CreateInfoLabel("暂无可互动的NPC");
            return;
        }

        for (int i = 0; i < allNPCs.Length; i++)
        {
            NPCData npc = allNPCs[i];

            // 获取好感度信息
            string affinityInfo = "";
            if (AffinitySystem.Instance != null)
            {
                NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(npc.id);
                affinityInfo = $"  好感:{rel.affinity}  [{GetLevelDisplayName(rel.level)}]";
            }

            string label = $"{npc.displayName}{affinityInfo}";
            string capturedId = npc.id; // 闭包捕获

            CreateListButton(label, () =>
            {
                ShowForNPC(capturedId);
            });
        }
    }

    // ====================================================================
    //  NPC 社交行动模式
    // ====================================================================

    private void ShowNPCActions(string npcId)
    {
        currentNpcId = npcId;

        if (NPCDatabase.Instance == null || AffinitySystem.Instance == null)
        {
            Debug.LogWarning("[NPCInteractionMenu] 系统未就绪");
            return;
        }

        NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
        if (npcData == null)
        {
            Debug.LogWarning($"[NPCInteractionMenu] 未找到NPC: {npcId}");
            return;
        }

        NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(npcId);

        // 更新顶部信息
        titleText.text = npcData.displayName;
        levelText.text = GetLevelDisplayName(rel.level);
        SetAffinityBar(rel.affinity / 100f);
        headerGroup.SetActive(true);

        ClearButtons();

        // 返回按钮（回到NPC列表）
        CreateListButton("← 返回NPC列表", () =>
        {
            ShowNPCList();
        });

        // 获取可用社交行动
        List<SocialActionDefinition> actions = AffinitySystem.Instance.GetAvailableSocialActions(npcId);

        if (actions.Count == 0)
        {
            CreateInfoLabel("当前无可用社交行动\n（行动点/金钱不足或好感等级不够）");
            return;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            SocialActionDefinition action = actions[i];
            string capturedActionId = action.id;
            string capturedNpcId = npcId;

            // 按钮文字：行动名 + AP消耗 + 金钱消耗 + 预期好感范围
            string costInfo = $"AP:{action.actionPointCost}";
            if (action.moneyCost > 0)
            {
                costInfo += $"  ￥{action.moneyCost}";
            }
            string affinityRange = $"好感+{action.baseAffinityMin}~{action.baseAffinityMax}";
            string label = $"{action.displayName}    {costInfo}    {affinityRange}";

            CreateActionButton(label, () =>
            {
                OnSocialActionClicked(capturedNpcId, capturedActionId);
            });
        }

        // ===== 恋爱系统按钮 =====
        AddRomanceButtons(npcId);
    }

    // ====================================================================
    //  恋爱系统按钮
    // ====================================================================

    /// <summary>
    /// 根据 RomanceSystem 的状态为当前 NPC 添加恋爱相关按钮
    /// </summary>
    private void AddRomanceButtons(string npcId)
    {
        if (RomanceSystem.Instance == null) return;

        RomanceState state = RomanceSystem.Instance.GetRomanceState(npcId);

        switch (state)
        {
            case RomanceState.None:
                // 无恋爱关系，不显示特殊按钮
                break;

            case RomanceState.Crushing:
                // 暗恋状态 → 显示告白按钮（条件：好感≥80 + AP≥2）
                AddConfessionButton(npcId);
                break;

            case RomanceState.Dating:
                // 恋爱中 → 显示约会按钮 + 恋爱状态信息
                AddDatingButtons(npcId);
                break;

            case RomanceState.BrokenUp:
                // 分手状态 → 显示复合按钮（条件满足时）
                AddReunionButton(npcId);
                break;

            case RomanceState.Cooldown:
                // 冷却中 → 显示冷却提示
                AddCooldownInfo(npcId);
                break;

            case RomanceState.Hostile:
                // 敌对 → 显示敌对提示
                CreateRomanceInfoLabel("关系已破裂，无法互动");
                break;
        }
    }

    /// <summary>告白按钮（Crushing 态）</summary>
    private void AddConfessionButton(string npcId)
    {
        bool canConfess = RomanceSystem.Instance.CanConfess(npcId);

        if (canConfess)
        {
            // 显示成功率
            float rate = 0f;
            if (ConfessionSystem.Instance != null)
            {
                string locId = GameState.Instance != null ? GameState.Instance.CurrentLocation.ToString() : null;
                rate = ConfessionSystem.Instance.CalculateSuccessRate(npcId, locId);
            }

            string label = $"告白    AP:2    成功率:{rate:P0}";
            string capturedNpcId = npcId;

            CreateRomanceButton(label, true, () =>
            {
                OnConfessionClicked(capturedNpcId, false);
            });
        }
        else
        {
            // 显示为什么不能告白
            int affinity = AffinitySystem.Instance != null ?
                AffinitySystem.Instance.GetRelationship(npcId).affinity : 0;
            int ap = GameState.Instance != null ? GameState.Instance.ActionPoints : 0;

            string hint = "";
            if (affinity < 80) hint = $"好感度不足（当前:{affinity}/需要:80）";
            else if (ap < 2) hint = "行动点不足（需要2AP）";

            CreateRomanceButton($"告白    {hint}", false, null);
        }
    }

    /// <summary>约会按钮（Dating 态）</summary>
    private void AddDatingButtons(string npcId)
    {
        // 恋爱状态信息
        int health = RomanceSystem.Instance.GetRomanceHealth(npcId);
        CreateRomanceInfoLabel($"恋爱中    健康度: {health}/100");

        // 约会按钮
        string capturedNpcId = npcId;
        CreateRomanceButton("约会    AP:1", true, () =>
        {
            OnDateClicked(capturedNpcId);
        });

        // 分手按钮 (Bug3 修复: 玩家主动分手入口)
        CreateBreakupButton(npcId);
    }

    /// <summary>复合按钮（BrokenUp 态）</summary>
    private void AddReunionButton(string npcId)
    {
        bool canReunite = RomanceSystem.Instance.CanReunite(npcId);

        if (canReunite)
        {
            float rate = 0f;
            if (ConfessionSystem.Instance != null)
            {
                string locId = GameState.Instance != null ? GameState.Instance.CurrentLocation.ToString() : null;
                rate = ConfessionSystem.Instance.CalculateReunionRate(npcId, locId);
            }

            string label = $"复合告白    AP:2    成功率:{rate:P0}";
            string capturedNpcId = npcId;

            CreateRomanceButton(label, true, () =>
            {
                OnConfessionClicked(capturedNpcId, true);
            });
        }
        else
        {
            RomanceRecord record = RomanceSystem.Instance.GetOrCreateRecord(npcId);
            string hint = "";

            if (record.cooldownRoundsLeft > 0)
                hint = $"冷却中（剩余{record.cooldownRoundsLeft}回合）";
            else if (record.hasReunited)
                hint = "已复合过，无法再次复合";
            else if (record.breakupCount >= 2)
                hint = "分手次数过多";
            else
            {
                int affinity = AffinitySystem.Instance != null ?
                    AffinitySystem.Instance.GetRelationship(npcId).affinity : 0;
                if (affinity < 70) hint = $"好感度不足（当前:{affinity}/需要:70）";
            }

            CreateRomanceButton($"复合告白    {hint}", false, null);
        }
    }

    /// <summary>冷却提示（Cooldown 态）</summary>
    private void AddCooldownInfo(string npcId)
    {
        RomanceRecord record = RomanceSystem.Instance.GetOrCreateRecord(npcId);
        CreateRomanceInfoLabel($"冷却中（剩余 {record.cooldownRoundsLeft} 回合）");
    }

    // ====================================================================
    //  恋爱按钮点击处理
    // ====================================================================

    /// <summary>告白/复合按钮点击</summary>
    private void OnConfessionClicked(string npcId, bool isReunion)
    {
        if (ConfessionSystem.Instance == null)
        {
            Debug.LogWarning("[NPCInteractionMenu] ConfessionSystem 未初始化");
            return;
        }

        // 关闭菜单
        CloseMenu();

        // 执行告白
        ConfessionSystem.Instance.ExecuteConfession(npcId, isReunion, (success) =>
        {
            Debug.Log($"[NPCInteractionMenu] {npcId} {(isReunion ? "复合" : "告白")} {(success ? "成功" : "失败")}");
        });
    }

    /// <summary>约会按钮点击</summary>
    private void OnDateClicked(string npcId)
    {
        // 扣除行动点
        if (GameState.Instance != null)
        {
            if (!GameState.Instance.ConsumeActionPoint(1))
            {
                Debug.Log("[NPCInteractionMenu] 行动点不足，无法约会");
                return;
            }
        }

        // 标记互动
        RomanceSystem.Instance.MarkInteractedThisRound(npcId);

        // 约会效果：健康度 +10，心情 +5
        RomanceSystem.Instance.ModifyHealth(npcId, 10);
        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.Mood += 5;
        }

        Debug.Log($"[NPCInteractionMenu] {npcId} 约会完成！健康度+10，心情+5");

        // 关闭菜单
        CloseMenu();

        // 触发约会对话
        TriggerDateDialogue(npcId);
    }

    /// <summary>创建分手按钮（红色调）</summary>
    private void CreateBreakupButton(string npcId)
    {
        string capturedNpcId = npcId;

        // 使用自定义红色调按钮（区别于恋爱粉紫色）
        GameObject btnObj = new GameObject("BreakupBtn");
        btnObj.transform.SetParent(contentContainer, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, ButtonHeight);

        Color breakupColor = new Color(0.55f, 0.18f, 0.18f, 1.0f);
        Color breakupHover = new Color(0.65f, 0.25f, 0.25f, 1.0f);
        Color breakupPressed = new Color(0.45f, 0.12f, 0.12f, 1.0f);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = breakupColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = breakupColor;
        cb.highlightedColor = breakupHover;
        cb.pressedColor = breakupPressed;
        cb.selectedColor = breakupColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(() => OnBreakupClicked(capturedNpcId));

        TextMeshProUGUI btnText = CreateTMPText("Label", btnObj.transform, "分手",
            18f, TextWhite, TextAlignmentOptions.Center, new Vector2(0, ButtonHeight));
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12, 0);
        textRT.offsetMax = new Vector2(-12, 0);
        ApplyChineseFont(btnText);

        btnObj.AddComponent<CanvasGroup>();
        spawnedButtons.Add(btnObj);
    }

    /// <summary>分手按钮点击回调</summary>
    private void OnBreakupClicked(string npcId)
    {
        if (RomanceSystem.Instance == null) return;

        // 直接执行分手（不弹确认窗，保持代码简洁）
        RomanceSystem.Instance.TriggerPlayerBreakup(npcId);

        string displayName = "对方";
        if (NPCDatabase.Instance != null)
        {
            NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
            if (npcData != null) displayName = npcData.displayName;
        }

        Debug.Log($"[NPCInteractionMenu] 玩家主动与 {displayName} 分手");

        // 关闭菜单
        CloseMenu();

        // 触发分手对话
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue("旁白",
                new string[] { $"你与{displayName}的关系结束了......" });
        }
    }

    /// <summary>触发约会对话</summary>
    private void TriggerDateDialogue(string npcId)

    {
        if (DialogueSystem.Instance == null) return;

        // 尝试加载 JSON 对话
        string dialogueId = $"{npcId}_date";
        DialogueData data = DialogueParser.GetDialogue(dialogueId);

        if (data != null)
        {
            DialogueSystem.Instance.StartDialogue(dialogueId);
        }
        else
        {
            // 回退到简单文本
            string speakerName = "旁白";
            if (NPCDatabase.Instance != null)
            {
                NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
                if (npcData != null) speakerName = npcData.displayName;
            }

            DialogueSystem.Instance.StartDialogue(speakerName,
                new string[] { "今天的约会很愉快呢。" });
        }
    }

    // ====================================================================
    //  恋爱 UI 工厂方法
    // ====================================================================

    /// <summary>创建恋爱专用按钮（粉紫色调）</summary>
    private void CreateRomanceButton(string label, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("RomanceBtn");
        btnObj.transform.SetParent(contentContainer, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, ButtonHeight);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = interactable ? RomanceButtonColor : new Color(0.25f, 0.20f, 0.25f, 0.7f);

        Button btn = btnObj.AddComponent<Button>();
        btn.interactable = interactable;

        ColorBlock cb = btn.colors;
        cb.normalColor = RomanceButtonColor;
        cb.highlightedColor = RomanceHoverColor;
        cb.pressedColor = RomancePressedColor;
        cb.disabledColor = new Color(0.25f, 0.20f, 0.25f, 0.7f);
        cb.selectedColor = RomanceButtonColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        if (interactable && onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }

        TextMeshProUGUI btnText = CreateTMPText("Label", btnObj.transform, label,
            18f, interactable ? TextWhite : DisabledTextColor,
            TextAlignmentOptions.Center, new Vector2(0, ButtonHeight));
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12, 0);
        textRT.offsetMax = new Vector2(-12, 0);
        ApplyChineseFont(btnText);

        btnObj.AddComponent<CanvasGroup>();
        spawnedButtons.Add(btnObj);
    }

    /// <summary>创建恋爱状态信息标签（紫色调背景）</summary>
    private void CreateRomanceInfoLabel(string text)
    {
        GameObject labelObj = new GameObject("RomanceInfoLabel");
        labelObj.transform.SetParent(contentContainer, false);

        RectTransform rt = labelObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 44f);

        Image bg = labelObj.AddComponent<Image>();
        bg.color = InfoLabelBg;

        TextMeshProUGUI tmp = CreateTMPText("InfoText", labelObj.transform, text,
            17f, new Color(0.80f, 0.65f, 0.85f), TextAlignmentOptions.Center, new Vector2(0, 44f));
        RectTransform textRT = tmp.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        ApplyChineseFont(tmp);

        spawnedButtons.Add(labelObj);
    }

    // ====================================================================
    //  社交行动执行
    // ====================================================================

    private void OnSocialActionClicked(string npcId, string actionId)
    {
        if (AffinitySystem.Instance == null) return;

        // 执行互动
        int delta = AffinitySystem.Instance.ExecuteInteraction(npcId, actionId);

        // 获取 NPC 数据
        NPCData npcData = NPCDatabase.Instance != null ? NPCDatabase.Instance.GetNPC(npcId) : null;
        SocialActionDefinition actionDef = NPCDatabase.Instance != null ? NPCDatabase.Instance.GetSocialAction(actionId) : null;

        // 通过 NPCEventHub 发布对话请求
        if (NPCEventHub.Instance != null && npcData != null)
        {
            // 从 greetingLines 中随机选一句
            string[] lines = null;
            if (npcData.greetingLines != null && npcData.greetingLines.Length > 0)
            {
                string randomLine = npcData.greetingLines[Random.Range(0, npcData.greetingLines.Length)];
                lines = new string[] { randomLine };
            }
            else
            {
                lines = new string[] { "……" };
            }

            NPCEventHub.DialogueRequest request = new NPCEventHub.DialogueRequest(
                npcId, npcData.displayName, lines, npcData.portraitId);
            NPCEventHub.Instance.RaiseDialogueRequested(request);

            // 发布社交反馈
            string actionDisplayName = actionDef != null ? actionDef.displayName : actionId;
            NPCEventHub.Instance.RaiseSocialFeedback(npcId, actionDisplayName, delta);
        }

        // 关闭菜单
        CloseMenu();
    }

    // ====================================================================
    //  UI 工具方法
    // ====================================================================

    private void ClearButtons()
    {
        for (int i = spawnedButtons.Count - 1; i >= 0; i--)
        {
            Destroy(spawnedButtons[i]);
        }
        spawnedButtons.Clear();
    }

    private void SetAffinityBar(float normalized)
    {
        if (affinityFill == null) return;
        RectTransform fillRT = affinityFill.GetComponent<RectTransform>();
        fillRT.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1);
    }

    /// <summary>创建列表按钮（NPC 选择 / 返回）</summary>
    private void CreateListButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("ListBtn");
        btnObj.transform.SetParent(contentContainer, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, ButtonHeight);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = ButtonNormal;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = ButtonNormal;
        cb.highlightedColor = ButtonHover;
        cb.pressedColor = ButtonPressed;
        cb.selectedColor = ButtonNormal;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI btnText = CreateTMPText("Label", btnObj.transform, label,
            19f, TextWhite, TextAlignmentOptions.Center, new Vector2(0, ButtonHeight));
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12, 0);
        textRT.offsetMax = new Vector2(-12, 0);
        ApplyChineseFont(btnText);

        btnObj.AddComponent<CanvasGroup>();
        spawnedButtons.Add(btnObj);
    }

    /// <summary>创建社交行动按钮</summary>
    private void CreateActionButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("ActionBtn");
        btnObj.transform.SetParent(contentContainer, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, ButtonHeight);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = ButtonNormal;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = ButtonNormal;
        cb.highlightedColor = ButtonHover;
        cb.pressedColor = ButtonPressed;
        cb.selectedColor = ButtonNormal;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI btnText = CreateTMPText("Label", btnObj.transform, label,
            18f, TextWhite, TextAlignmentOptions.Center, new Vector2(0, ButtonHeight));
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12, 0);
        textRT.offsetMax = new Vector2(-12, 0);
        ApplyChineseFont(btnText);

        btnObj.AddComponent<CanvasGroup>();
        spawnedButtons.Add(btnObj);
    }

    /// <summary>创建信息提示标签（无可用行动时）</summary>
    private void CreateInfoLabel(string text)
    {
        GameObject labelObj = new GameObject("InfoLabel");
        labelObj.transform.SetParent(contentContainer, false);

        RectTransform rt = labelObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80f);

        TextMeshProUGUI tmp = CreateTMPText("InfoText", labelObj.transform, text,
            20f, new Color(0.6f, 0.6f, 0.65f), TextAlignmentOptions.Center, new Vector2(0, 80f));
        RectTransform textRT = tmp.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        ApplyChineseFont(tmp);

        spawnedButtons.Add(labelObj);
    }

    /// <summary>创建 TextMeshPro 文本（与 HUDBuilder 风格一致）</summary>
    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string text,
        float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    /// <summary>应用中文字体</summary>
    private void ApplyChineseFont(TextMeshProUGUI tmp)
    {
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(tmp);
        }
    }

    /// <summary>好感等级中文显示名</summary>
    private string GetLevelDisplayName(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Stranger:     return "陌生人";
            case AffinityLevel.Acquaintance: return "认识";
            case AffinityLevel.Friend:       return "朋友";
            case AffinityLevel.CloseFriend:  return "好友";
            case AffinityLevel.BestFriend:   return "挚友";
            case AffinityLevel.Lover:        return "恋人";
            default:                         return "未知";
        }
    }
}
