using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HUD 管理器 —— 负责初始化 HUD、绑定数据、刷新显示、处理按钮事件
/// 协程动画风格参考 UIAnimator.cs
/// </summary>
public class HUDManager : MonoBehaviour
{
    // ========== 引用 ==========
    private HUDBuilder builder;
    private GameState gameState;
    private PlayerAttributes playerAttributes;

    // ========== 地图 UI ==========
    private CampusMapUI campusMapUI;
    private LocationDetailPanel detailPanel;
    private List<Button> dynamicActionButtons = new List<Button>();

    // ========== 社团面板 ==========
    private ClubPanelManager clubPanelManager;

    // ========== 动画参数 ==========
    private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ========== 属性条缓存 ==========
    private List<AttributeBar> attrBars = new List<AttributeBar>();

    // ========== 初始化 ==========

    private void Start()
    {
        // 获取或创建数据单例
        EnsureDataInstances();

        // 构建 HUD UI
        builder = gameObject.AddComponent<HUDBuilder>();
        builder.BuildHUD();

        // 初始化地图 UI
        InitMapUI();

        // 创建属性条
        CreateAttributeBars();

        // 绑定按钮事件（动态生成底栏按钮）
        RefreshBottomBar();

        // 订阅数据变化事件
        if (gameState != null) gameState.OnStateChanged += RefreshTopBar;
        if (playerAttributes != null) playerAttributes.OnAttributesChanged += RefreshAttributes;

        // 延迟订阅子系统事件（确保所有单例已完成初始化）
        StartCoroutine(DeferredSubscriptions());

        // 绑定商店按钮
        if (builder.btnShop != null && builder.shopUIBuilder != null)
        {
            builder.btnShop.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnShop);
                builder.shopUIBuilder.ShowShop();
            });
        }

        // 绑定社团按钮
        if (builder.btnClub != null)
        {
            builder.btnClub.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnClub);
                if (builder.clubPanelManager != null)
                    builder.clubPanelManager.OpenPanel();
            });
        }

        // 绑定存档按钮
        if (builder.btnSave != null)
        {
            builder.btnSave.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnSave);
                SaveLoadUI.Show(true);
            });
        }

        // 绑定天赋按钮
        if (builder.btnTalent != null)
        {
            builder.btnTalent.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnTalent);
                if (TalentUI.Instance != null)
                {
                    if (TalentUI.Instance.IsOpen)
                        TalentUI.Instance.ClosePanel();
                    else
                        TalentUI.Instance.ShowPanel();
                }
            });
        }

        // 绑定地图按钮
        if (builder.btnMap != null)
        {
            builder.btnMap.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnMap);
                ToggleMapOverlay();
            });
        }

        // 缓存社团面板管理器引用
        clubPanelManager = builder.clubPanelManager;

        // 首次刷新
        RefreshAll();

        // 播放入场动画
        StartCoroutine(PlayEntryAnimation());

        // 显示第一回合新闻（延迟到入场动画之后）
        StartCoroutine(ShowInitialNews());
    }

    /// <summary>延迟订阅子系统事件，确保所有单例已初始化完毕</summary>
    private IEnumerator DeferredSubscriptions()
    {
        // 等待一帧，确保所有单例的 Start() 已执行
        yield return null;

        // 订阅考试完成事件
        if (ExamSystem.Instance != null)
            ExamSystem.Instance.OnExamCompleted += OnExamCompleted;

        // 订阅回合推进事件
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;

        // 订阅 NPC 社交互动反馈事件
        if (NPCEventHub.Instance != null)
            NPCEventHub.Instance.OnSocialInteractionFeedback += OnSocialFeedback;

        // 订阅地点变化事件
        if (LocationManager.Instance != null)
            LocationManager.Instance.OnLocationChanged += OnLocationChangedHandler;

        // 订阅恋爱系统事件
        if (RomanceSystem.Instance != null)
        {
            RomanceSystem.Instance.OnRomanceStateChanged += OnRomanceStateChanged;
            RomanceSystem.Instance.OnRomanceHealthChanged += OnRomanceHealthChanged;
        }
        if (ConfessionSystem.Instance != null)
            ConfessionSystem.Instance.OnConfessionResult += OnConfessionResult;

        // 订阅债务等级变化事件
        if (DebtSystem.Instance != null)
            DebtSystem.Instance.OnDebtLevelChanged += OnDebtLevelChanged;

        // 订阅事件系统
        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.OnEventTriggered += OnGameEventTriggered;
            EventScheduler.Instance.OnEventCompleted += OnGameEventCompleted;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (gameState != null) gameState.OnStateChanged -= RefreshTopBar;
        if (playerAttributes != null) playerAttributes.OnAttributesChanged -= RefreshAttributes;
        if (TurnManager.Instance != null) TurnManager.Instance.OnRoundAdvanced -= OnRoundAdvanced;
        if (NPCEventHub.Instance != null) NPCEventHub.Instance.OnSocialInteractionFeedback -= OnSocialFeedback;
        if (LocationManager.Instance != null) LocationManager.Instance.OnLocationChanged -= OnLocationChangedHandler;
        if (DebtSystem.Instance != null) DebtSystem.Instance.OnDebtLevelChanged -= OnDebtLevelChanged;
        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.OnEventTriggered -= OnGameEventTriggered;
            EventScheduler.Instance.OnEventCompleted -= OnGameEventCompleted;
        }
        if (ExamSystem.Instance != null) ExamSystem.Instance.OnExamCompleted -= OnExamCompleted;
        if (NewsSystem.Instance != null) NewsSystem.Instance.OnNewsDismissed -= OnNewsDismissed;
        if (RomanceSystem.Instance != null)
        {
            RomanceSystem.Instance.OnRomanceStateChanged -= OnRomanceStateChanged;
            RomanceSystem.Instance.OnRomanceHealthChanged -= OnRomanceHealthChanged;
        }
        if (ConfessionSystem.Instance != null) ConfessionSystem.Instance.OnConfessionResult -= OnConfessionResult;
    }

    /// <summary>确保数据单例存在</summary>
    private void EnsureDataInstances()
    {
        // GameState
        gameState = GameState.Instance;
        if (gameState == null)
        {
            GameObject gsObj = new GameObject("GameState");
            gameState = gsObj.AddComponent<GameState>();
        }

        // PlayerAttributes
        playerAttributes = PlayerAttributes.Instance;
        if (playerAttributes == null)
        {
            GameObject paObj = new GameObject("PlayerAttributes");
            playerAttributes = paObj.AddComponent<PlayerAttributes>();
        }
    }

    /// <summary>根据属性数量创建属性条</summary>
    private void CreateAttributeBars()
    {
        PlayerAttributes.AttributeInfo[] attrs = playerAttributes.GetAllAttributes();
        attrBars.Clear();

        foreach (var attr in attrs)
        {
            AttributeBar bar = builder.AddAttributeBar();
            if (bar != null)
            {
                bar.SetAttributeImmediate(attr);
                attrBars.Add(bar);
            }
        }
    }

    // ========== 数据刷新 ==========

    /// <summary>刷新所有 HUD 元素</summary>
    public void RefreshAll()
    {
        RefreshTopBar();
        RefreshAttributes();
    }

    /// <summary>刷新顶栏信息</summary>
    public void RefreshTopBar()
    {
        if (builder == null || gameState == null) return;

        // 时间描述
        if (builder.timeText != null)
        {
            builder.timeText.text = $"当前时间：{gameState.GetTimeDescription()}";
        }

        // 金钱（透支变红 + 警告图标）
        if (builder.moneyText != null)
        {
            int currentMoney = gameState.Money;
            builder.moneyText.text = $"金钱：￥{currentMoney}";

            // 透支状态 → 红色；余额不足200 → 橙色预警；正常 → 金色
            if (currentMoney < 0)
                builder.moneyText.color = new Color(1f, 0.3f, 0.3f); // 红色
            else if (currentMoney < 200)
                builder.moneyText.color = new Color(1f, 0.65f, 0.2f); // 橙色
            else
                builder.moneyText.color = new Color(1.0f, 0.85f, 0.30f); // TextGold

            // 警告图标：余额 < 200 时显示
            if (builder.moneyWarningText != null)
            {
                builder.moneyWarningText.gameObject.SetActive(currentMoney < 200);
            }
        }

        // 行动点（实心/空心圆）
        if (builder.actionPointsText != null)
        {
            builder.actionPointsText.text = $"行动点：{BuildActionPointsDisplay()}";
        }

        // GPA 显示
        RefreshGPA();
    }

    /// <summary>刷新 GPA 显示</summary>
    public void RefreshGPA()
    {
        if (builder == null || builder.gpaText == null) return;

        if (ExamSystem.Instance != null && ExamSystem.Instance.GetCumulativeGPA() > 0)
        {
            float gpa = ExamSystem.Instance.GetCumulativeGPA();
            builder.gpaText.text = $"GPA: {gpa:F2}";

            // 颜色：3.5+ 金色, 2.0+ 白色, <2.0 红色
            if (gpa >= 3.5f)
                builder.gpaText.color = new Color(1.0f, 0.85f, 0.30f); // 金色
            else if (gpa >= 2.0f)
                builder.gpaText.color = new Color(0.6f, 0.9f, 1.0f); // 浅蓝
            else
                builder.gpaText.color = new Color(1.0f, 0.3f, 0.3f); // 红色
        }
        else
        {
            builder.gpaText.text = "GPA: --";
        }
    }

    /// <summary>考试完成后回调</summary>
    private void OnExamCompleted(SemesterGPA semesterGPA)
    {
        Debug.Log($"[HUDManager] 收到考试完成事件，学期 GPA={semesterGPA.gpa:F2}");
        RefreshGPA();
    }

    /// <summary>刷新属性条</summary>
    public void RefreshAttributes()
    {
        if (playerAttributes == null) return;

        PlayerAttributes.AttributeInfo[] attrs = playerAttributes.GetAllAttributes();
        for (int i = 0; i < attrBars.Count && i < attrs.Length; i++)
        {
            attrBars[i].SetAttribute(attrs[i]);
        }
    }

    // ========== 行动点显示 ==========

    /// <summary>用实心●和空心○构建行动点显示字符串</summary>
    private string BuildActionPointsDisplay()
    {
        int current = gameState.ActionPoints;
        int max = gameState.EffectiveMaxActionPoints;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < max; i++)
        {
            sb.Append(i < current ? "●" : "○");
        }
        return sb.ToString();
    }

    // ========== 按钮事件 ==========

    private void BindButtonEvents()
    {
        RefreshBottomBar();
    }

    /// <summary>初始化地图 UI（校园地图覆盖层 + 详情面板）</summary>
    private void InitMapUI()
    {
        if (builder.hudCanvas == null) return;

        // 创建地图覆盖层（弹窗式，初始隐藏）
        campusMapUI = new CampusMapUI();
        campusMapUI.BuildMapOverlay(builder.hudCanvas);
        campusMapUI.OnLocationNodeClicked += OnLocationNodeClicked;

        // 创建详情面板（挂在覆盖层内）
        detailPanel = new LocationDetailPanel();
        detailPanel.Build(campusMapUI.OverlayRoot);
        detailPanel.OnNavigated += OnLocationNavigated;
    }

    // ========== 地图事件 ==========

    /// <summary>地图节点被点击 —— 显示地点详情面板</summary>
    private void OnLocationNodeClicked(LocationId locationId)
    {
        if (detailPanel != null)
        {
            detailPanel.Show(locationId);
        }
    }

    /// <summary>导航完成 —— 传送玩家、关闭地图覆盖层、刷新UI</summary>
    private void OnLocationNavigated(LocationId targetLocation)
    {
        // 传送玩家到目标地点的世界中心坐标
        if (LocationManager.Instance != null)
        {
            Vector3 targetPos = LocationManager.Instance.GetLocationWorldCenter(targetLocation);
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = targetPos;
            }
        }

        // 关闭地图覆盖层
        if (campusMapUI != null) campusMapUI.HideOverlay();

        // 刷新UI
        if (campusMapUI != null) campusMapUI.RefreshMap();
        RefreshBottomBar();
        RefreshTopBar();
    }

    /// <summary>地点变化回调 —— 刷新地图和底栏</summary>
    private void OnLocationChangedHandler(LocationId from, LocationId to)
    {
        if (campusMapUI != null) campusMapUI.RefreshMap();
        RefreshBottomBar();
    }

    /// <summary>切换地图覆盖层显示/隐藏</summary>
    private void ToggleMapOverlay()
    {
        if (campusMapUI == null) return;
        if (campusMapUI.IsVisible)
            campusMapUI.HideOverlay();
        else
            campusMapUI.ShowOverlay();
    }

    // ========== 动态底栏 ==========

    /// <summary>根据当前地点刷新底栏行动按钮</summary>
    private void RefreshBottomBar()
    {
        // 清除旧的动态按钮
        ClearDynamicButtons();

        // 隐藏 HUDBuilder 创建的静态按钮（被动态按钮取代）
        HideStaticButtons();

        // 获取当前地点可用行动
        LocationId currentLoc = GameState.Instance != null ?
            GameState.Instance.CurrentLocation : LocationId.Dormitory;

        ActionDefinition[] actions;
        if (LocationManager.Instance != null)
            actions = LocationManager.Instance.GetAvailableActions(currentLoc);
        else if (ActionSystem.Instance != null)
            actions = ActionSystem.Instance.GetAllActions();
        else
            return;

        // 在底栏动态创建按钮
        Transform bottomBar = builder.hudCanvas.transform.Find("BottomBar");
        if (bottomBar == null) return;

        foreach (var action in actions)
        {
            Button btn = CreateDynamicActionButton(bottomBar, action);
            dynamicActionButtons.Add(btn);
        }
    }

    /// <summary>清除所有动态行动按钮</summary>
    private void ClearDynamicButtons()
    {
        foreach (var btn in dynamicActionButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        dynamicActionButtons.Clear();
    }

    /// <summary>创建单个动态行动按钮（风格与 HUDBuilder.CreateActionButton 一致）</summary>
    private Button CreateDynamicActionButton(Transform parent, ActionDefinition action)
    {
        GameObject btnObj = new GameObject("Btn_" + action.id);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140, 50);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.20f, 0.35f, 0.60f, 1.0f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.20f, 0.35f, 0.60f, 1.0f);
        cb.highlightedColor = new Color(0.30f, 0.45f, 0.70f, 1.0f);
        cb.pressedColor = new Color(0.15f, 0.25f, 0.50f, 1.0f);
        cb.selectedColor = new Color(0.20f, 0.35f, 0.60f, 1.0f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // 按钮文字：显示名 + AP消耗 + 金钱消耗
        string label = action.displayName;
        List<string> costs = new List<string>();
        if (action.actionPointCost > 0)
            costs.Add($"{action.actionPointCost}AP");
        if (action.moneyCost > 0)
            costs.Add($"¥{action.moneyCost}");
        if (costs.Count > 0)
            label += $"({string.Join("/", costs)})";

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16f;
        tmp.color = new Color(0.92f, 0.92f, 0.92f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;

        btnObj.AddComponent<CanvasGroup>();

        // 绑定点击事件
        string actionId = action.id; // 闭包捕获
        btn.onClick.AddListener(() => OnDynamicActionButtonClicked(actionId, btn));

        return btn;
    }

    /// <summary>动态行动按钮被点击 —— 委托给 ActionSystem 执行</summary>
    private void OnDynamicActionButtonClicked(string actionId, Button button)
    {
        if (ActionSystem.Instance == null || !ActionSystem.Instance.CanExecuteAction(actionId))
        {
            Debug.Log($"[HUD] 无法执行行动: {actionId}");
            StartCoroutine(ShakeButtonCoroutine(button));
            return;
        }

        if (builder.hudAnimator != null)
        {
            builder.hudAnimator.ButtonPressEffect(button);
        }

        ActionSystem.Instance.ExecuteAction(actionId);
    }

    /// <summary>
    /// 行动按钮 actionId 与按钮显示名的映射表
    /// </summary>
    private static readonly Dictionary<string, string> ActionIdMap = new Dictionary<string, string>
    {
        { "自习", "study" },
        { "社交", "social" },
        { "出校门", "goout" },
        { "睡觉", "sleep" }
    };

    /// <summary>行动按钮被点击 —— 委托给 ActionSystem 执行</summary>
    private void OnActionButtonClicked(string actionName, Button button)
    {
        // ---- 社交按钮 → 打开 NPC 互动菜单 ----
        if (actionName == "社交")
        {
            if (builder.npcInteractionMenu != null)
            {
                // 播放按钮按压动画
                if (builder.hudAnimator != null)
                {
                    builder.hudAnimator.ButtonPressEffect(button);
                }
                builder.npcInteractionMenu.ShowForNPC(null); // null = NPC列表选择模式
            }
            else
            {
                Debug.LogWarning("[HUD] NPCInteractionMenu 未初始化");
            }
            return;
        }

        // ---- 其他行动按钮（自习/出校门/睡觉）走 ActionSystem ----

        // 获取 actionId
        if (!ActionIdMap.TryGetValue(actionName, out string actionId))
        {
            Debug.LogWarning($"[HUD] 未知行动名称: {actionName}");
            return;
        }

        // 检查是否可执行
        if (ActionSystem.Instance == null || !ActionSystem.Instance.CanExecuteAction(actionId))
        {
            Debug.Log($"[HUD] 无法执行行动: {actionName}（行动点或金钱不足）");
            StartCoroutine(ShakeButtonCoroutine(button));
            return;
        }

        // 播放按钮按压动画
        if (builder.hudAnimator != null)
        {
            builder.hudAnimator.ButtonPressEffect(button);
        }

        // 委托给 ActionSystem 执行（行动点扣除、属性效果、事件触发均在其中完成）
        ActionSystem.Instance.ExecuteAction(actionId);

        // 数据变化事件已自动触发 RefreshTopBar / RefreshAttributes，无需手动刷新
    }

    // ========== 模态控制 ==========

    /// <summary>是否有模态面板打开中（总结/结局面板），此时禁用行动按钮</summary>
    public bool IsModalOpen { get; private set; }

    /// <summary>设置模态状态，控制行动按钮的可交互性</summary>
    private void SetModalState(bool modal)
    {
        IsModalOpen = modal;
        // 禁用/启用所有动态行动按钮
        foreach (var btn in dynamicActionButtons)
        {
            if (btn != null) btn.interactable = !modal;
        }
    }

    // ========== 回合推进响应 ==========

    /// <summary>回合推进后的 UI 反馈 —— 接入学期总结和毕业结局面板</summary>
    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        switch (result)
        {
            case GameState.RoundAdvanceResult.NextRound:
                Debug.Log($"[HUD] 新回合开始 — {gameState.GetTimeDescription()}");
                // 显示校园新闻
                ShowRoundNews();
                break;
            case GameState.RoundAdvanceResult.NextSemester:
                Debug.Log($"[HUD] 新学期开始 — {gameState.GetTimeDescription()}");
                // 显示上学期总结面板
                ShowSemesterSummary();
                break;
            case GameState.RoundAdvanceResult.NextYear:
                Debug.Log($"[HUD] 新学年开始 — {gameState.GetTimeDescription()}");
                // 显示上学年下学期总结面板
                ShowSemesterSummary();
                break;
            case GameState.RoundAdvanceResult.Graduated:
                Debug.Log("[HUD] 恭喜毕业！游戏结束。");
                // 显示毕业结局面板
                ShowGraduationEnding();
                break;
        }

        // 回合推进后刷新地图和底栏
        if (campusMapUI != null) campusMapUI.RefreshMap();
        RefreshBottomBar();
    }

    /// <summary>显示回合开始新闻（锁定行动按钮，直到玩家关闭新闻）</summary>
    private void ShowRoundNews()
    {
        if (NewsSystem.Instance == null) return;

        SetActionButtonsInteractable(false);
        NewsSystem.Instance.OnNewsDismissed += OnNewsDismissed;
        NewsSystem.Instance.ShowNews();
    }

    /// <summary>新闻关闭后恢复行动按钮</summary>
    private void OnNewsDismissed()
    {
        if (NewsSystem.Instance != null)
            NewsSystem.Instance.OnNewsDismissed -= OnNewsDismissed;

        SetActionButtonsInteractable(true);
    }

    /// <summary>NPC 社交互动反馈回调</summary>
    private void OnSocialFeedback(string npcId, string actionName, int delta)
    {
        string sign = delta >= 0 ? "+" : "";
        Debug.Log($"[HUD] 社交反馈: NPC={npcId}, 行动={actionName}, 好感度{sign}{delta}");
        RefreshAll();
    }

    /// <summary>债务等级变化回调 —— 刷新顶栏金钱显示颜色</summary>
    private void OnDebtLevelChanged(DebtSystem.DebtLevel newLevel)
    {
        Debug.Log($"[HUD] 债务等级变化: {newLevel}");
        RefreshTopBar();
    }

    // ========== 恋爱系统响应 ==========

    /// <summary>恋爱状态变化回调</summary>
    private void OnRomanceStateChanged(string npcId, RomanceState oldState, RomanceState newState)
    {
        string npcName = npcId;
        if (NPCDatabase.Instance != null)
        {
            NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
            if (npcData != null) npcName = npcData.displayName;
        }
        Debug.Log($"[HUD] 恋爱状态变化: {npcName} {oldState} -> {newState}");
        RefreshAll();
    }

    /// <summary>恋爱健康度变化回调</summary>
    private void OnRomanceHealthChanged(string npcId)
    {
        int health = RomanceSystem.Instance != null ? RomanceSystem.Instance.GetRomanceHealth(npcId) : 0;
        Debug.Log($"[HUD] 恋爱健康度变化: {npcId} -> {health}/100");
    }

    /// <summary>告白结果回调</summary>
    private void OnConfessionResult(string npcId, bool success, float rate)
    {
        string npcName = npcId;
        if (NPCDatabase.Instance != null)
        {
            NPCData npcData = NPCDatabase.Instance.GetNPC(npcId);
            if (npcData != null) npcName = npcData.displayName;
        }
        Debug.Log($"[HUD] 告白结果: {npcName} {(success ? "成功" : "失败")} (成功率:{rate:P0})");
        RefreshAll();
    }

    // ========== 事件系统响应 ==========

    /// <summary>游戏事件开始时禁用行动按钮</summary>
    private void OnGameEventTriggered(EventDefinition evt)
    {
        Debug.Log($"[HUD] 游戏事件触发: {evt.id} - {evt.title}，锁定行动按钮");
        SetActionButtonsInteractable(false);
    }

    /// <summary>游戏事件结束时启用行动按钮</summary>
    private void OnGameEventCompleted(EventDefinition evt)
    {
        // 仅在事件队列清空后恢复按钮（避免连续事件中间短暂可点击）
        if (EventScheduler.Instance == null || !EventScheduler.Instance.HasPendingEvents())
        {
            Debug.Log($"[HUD] 游戏事件完成: {evt.id}，恢复行动按钮");
            SetActionButtonsInteractable(true);
        }
    }

    /// <summary>批量设置行动按钮可交互状态</summary>
    private void SetActionButtonsInteractable(bool interactable)
    {
        foreach (var btn in dynamicActionButtons)
        {
            if (btn != null) btn.interactable = interactable;
        }
        if (builder != null && builder.btnShop != null)
        {
            builder.btnShop.interactable = interactable;
        }
    }

    // ========== 协程动画（参考 UIAnimator 风格） ==========

    /// <summary>初始新闻显示（入场动画完成后）</summary>
    private IEnumerator ShowInitialNews()
    {
        // 等待入场动画完成
        yield return new WaitForSeconds(1.5f);
        ShowRoundNews();
    }

    /// <summary>入场动画：各区域依次淡入滑入</summary>
    private IEnumerator PlayEntryAnimation()
    {
        if (builder == null || builder.hudCanvas == null) yield break;

        // 收集需要动画的顶层面板
        Transform canvasRoot = builder.hudCanvas.transform;
        List<CanvasGroup> panels = new List<CanvasGroup>();

        foreach (Transform child in canvasRoot)
        {
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            panels.Add(cg);
        }

        // 等待一帧让布局稳定
        yield return null;

        // 依次淡入每个面板
        float delay = 0.08f;
        float duration = 0.35f;

        foreach (CanvasGroup cg in panels)
        {
            StartCoroutine(FadeInCoroutine(cg, duration));
            RectTransform rt = cg.GetComponent<RectTransform>();
            if (rt != null)
            {
                StartCoroutine(SlideInCoroutine(rt, duration));
            }
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>淡入动画</summary>
    private IEnumerator FadeInCoroutine(CanvasGroup cg, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = easingCurve.Evaluate(elapsedTime / duration);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1f;
    }

    /// <summary>从下方滑入动画</summary>
    private IEnumerator SlideInCoroutine(RectTransform rt, float duration)
    {
        Vector2 originalPos = rt.anchoredPosition;
        Vector2 startPos = originalPos + new Vector2(0, -30f);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = easingCurve.Evaluate(elapsedTime / duration);
            rt.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = originalPos;
    }

    /// <summary>按钮抖动动画（行动点不足时的反馈）</summary>
    private IEnumerator ShakeButtonCoroutine(Button button)
    {
        if (button == null) yield break;

        RectTransform rt = button.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 originalPos = rt.anchoredPosition;
        float shakeDuration = 0.3f;
        float shakeIntensity = 8f;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float progress = elapsedTime / shakeDuration;
            // 衰减抖动
            float offset = Mathf.Sin(progress * Mathf.PI * 6f) * shakeIntensity * (1f - progress);
            rt.anchoredPosition = originalPos + new Vector2(offset, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = originalPos;
    }

    // ========== 静态按钮控制 ==========

    /// <summary>隐藏 HUDBuilder 创建的静态行动按钮（被动态按钮取代）</summary>
    private void HideStaticButtons()
    {
        if (builder == null) return;
        if (builder.btnStudy != null) builder.btnStudy.gameObject.SetActive(false);
        if (builder.btnSocial != null) builder.btnSocial.gameObject.SetActive(false);
        if (builder.btnGoOut != null) builder.btnGoOut.gameObject.SetActive(false);
        if (builder.btnSleep != null) builder.btnSleep.gameObject.SetActive(false);
        // 商店和社团按钮保留，不由动态系统管理
    }

    // ========== 学期总结 / 结局面板 ==========

    /// <summary>显示学期总结面板</summary>
    private void ShowSemesterSummary()
    {
        SetModalState(true);

        if (SemesterSummarySystem.Instance != null && gameState != null)
        {
            // 上学期的 year/semester（当前已推进到新学期，需回溯）
            int prevYear = gameState.CurrentYear;
            int prevSemester = gameState.CurrentSemester == 1 ? 2 : 1;
            if (gameState.CurrentSemester == 1) prevYear = Mathf.Max(1, prevYear - 1);

            var summaryData = SemesterSummarySystem.Instance.GenerateSemesterSummary(prevYear, prevSemester);
            Debug.Log($"[HUD] 学期总结: {summaryData.yearName}{summaryData.semesterName} GPA={summaryData.gpa:F2} 总分={summaryData.totalScore} 评级={summaryData.grade}");

            // 确保 SemesterSummaryUI 实例存在
            if (SemesterSummaryUI.Instance == null)
            {
                GameObject uiObj = new GameObject("SemesterSummaryUI");
                uiObj.AddComponent<SemesterSummaryUI>();
            }

            SemesterSummaryUI.Instance.Show(summaryData);

            // 监听面板关闭以恢复行动按钮
            StartCoroutine(WaitForSummaryClose());
        }
        else
        {
            SetModalState(false);
        }
    }

    /// <summary>显示毕业结局面板</summary>
    private void ShowGraduationEnding()
    {
        SetModalState(true);

        if (EndingDeterminer.Instance != null)
        {
            EndingResult result = EndingDeterminer.Instance.DetermineEnding();
            Debug.Log($"[HUD] 毕业结局: {result.ending.name} ({result.ending.stars}★) — {result.ending.description}");

            // 确保 EndingUI 实例存在
            if (EndingUI.Instance == null)
            {
                GameObject uiObj = new GameObject("EndingUI");
                uiObj.AddComponent<EndingUI>();
            }

            EndingUI.Instance.Show(result);
        }
        else
        {
            Debug.Log("[HUD] 毕业！（EndingDeterminer 未初始化）");
        }
    }

    /// <summary>等待学期总结面板关闭后恢复行动按钮</summary>
    private IEnumerator WaitForSummaryClose()
    {
        while (SemesterSummaryUI.Instance != null && SemesterSummaryUI.Instance.isShowing)
        {
            yield return null;
        }
        SetModalState(false);
    }

}
