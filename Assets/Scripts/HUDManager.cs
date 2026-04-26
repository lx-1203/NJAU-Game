using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HUD 管理器 —— 负责初始化 HUD、绑定数据、刷新显示、处理按钮事件
/// 适配新版参考界面布局
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

    // ========== 行动按钮图标映射 ==========
    private static readonly Dictionary<string, string> ActionIcons = new Dictionary<string, string>
    {
        { "study", "\uD83D\uDCDA" },       // 书本
        { "attend_class", "\uD83C\uDFEB" }, // 学校
        { "social", "\uD83D\uDC65" },       // 人群
        { "play_game", "\uD83C\uDFAE" },    // 游戏
        { "sleep", "\uD83D\uDE34" },         // 睡觉
        { "goout", "\uD83D\uDEB6" },         // 出门
        { "eat", "\uD83C\uDF5C" },           // 食物
        { "exercise", "\uD83C\uDFC3" },      // 跑步
        { "sports_test", "\uD83C\uDFC5" },   // 奖牌
        { "shop", "\uD83D\uDECD" },          // 购物
        { "pickup_express", "\uD83D\uDCE6" },// 包裹
        { "order_takeout", "\uD83C\uDF54" }, // 外卖
        { "memorize_words", "\uD83D\uDCD6" } // 背单词
    };

    private static readonly Dictionary<string, Color> ActionColors = new Dictionary<string, Color>
    {
        { "study", new Color(0.30f, 0.55f, 0.85f) },
        { "attend_class", new Color(0.40f, 0.60f, 0.80f) },
        { "social", new Color(0.85f, 0.50f, 0.60f) },
        { "play_game", new Color(0.70f, 0.50f, 0.85f) },
        { "sleep", new Color(0.45f, 0.55f, 0.70f) },
        { "goout", new Color(0.50f, 0.75f, 0.55f) },
        { "eat", new Color(0.90f, 0.65f, 0.30f) },
        { "exercise", new Color(0.40f, 0.80f, 0.45f) },
        { "sports_test", new Color(0.85f, 0.70f, 0.25f) },
        { "shop", new Color(0.80f, 0.55f, 0.70f) },
        { "pickup_express", new Color(0.65f, 0.55f, 0.40f) },
        { "order_takeout", new Color(0.90f, 0.50f, 0.35f) },
        { "memorize_words", new Color(0.35f, 0.65f, 0.80f) }
    };

    // ========== 初始化 ==========

    private void Start()
    {
        SanitizeActionIcons();

        EnsureDataInstances();

        builder = gameObject.AddComponent<HUDBuilder>();
        builder.BuildHUD();

        InitMapUI();
        CreateAttributeBars();
        RefreshBottomBar();

        // 绑定系统按钮
        BindSystemButtons();

        if (gameState != null) gameState.OnStateChanged += RefreshTopBar;
        if (playerAttributes != null) playerAttributes.OnAttributesChanged += RefreshAttributes;

        StartCoroutine(DeferredSubscriptions());

        clubPanelManager = builder.clubPanelManager;

        RefreshAll();
        StartCoroutine(PlayEntryAnimation());
        StartCoroutine(ShowInitialNews());
    }

    private void SanitizeActionIcons()
    {
        ActionIcons["study"] = "ST";
        ActionIcons["attend_class"] = "CL";
        ActionIcons["social"] = "SO";
        ActionIcons["play_game"] = "GM";
        ActionIcons["sleep"] = "SL";
        ActionIcons["goout"] = "GO";
        ActionIcons["eat"] = "EA";
        ActionIcons["exercise"] = "EX";
        ActionIcons["sports_test"] = "SP";
        ActionIcons["shop"] = "SH";
        ActionIcons["pickup_express"] = "PK";
        ActionIcons["order_takeout"] = "TO";
        ActionIcons["memorize_words"] = "WD";
    }

    private void Update()
    {
        if (!CanProcessHotkeys())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInfoPanel();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OpenSocialPanel();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleTalentPanel();
        }
    }

    private void BindSystemButtons()
    {
        // 商店按钮
        if (builder.btnShop != null && builder.shopUIBuilder != null)
        {
            builder.btnShop.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnShop);
                builder.shopUIBuilder.ShowShop();
            });
        }

        // 社团按钮
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

        // 存档按钮
        if (builder.btnSave != null)
        {
            builder.btnSave.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnSave);
                SaveLoadUI.Show(true);
            });
        }

        // 天赋按钮
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

        // 地图按钮
        if (builder.btnMap != null)
        {
            builder.btnMap.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnMap);
                ToggleMapOverlay();
            });
        }

        // 右下角功能按钮（社交互动）
        if (builder.btnFeature != null && builder.npcInteractionMenu != null)
        {
            builder.btnFeature.onClick.AddListener(() =>
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(builder.btnFeature);
                builder.npcInteractionMenu.ShowForNPC(null);
            });
        }
    }

    private IEnumerator DeferredSubscriptions()
    {
        yield return null;

        if (ExamSystem.Instance != null)
            ExamSystem.Instance.OnExamCompleted += OnExamCompleted;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnRoundAdvanced += OnRoundAdvanced;

        if (NPCEventHub.Instance != null)
            NPCEventHub.Instance.OnSocialInteractionFeedback += OnSocialFeedback;

        if (LocationManager.Instance != null)
            LocationManager.Instance.OnLocationChanged += OnLocationChangedHandler;

        if (RomanceSystem.Instance != null)
        {
            RomanceSystem.Instance.OnRomanceStateChanged += OnRomanceStateChanged;
            RomanceSystem.Instance.OnRomanceHealthChanged += OnRomanceHealthChanged;
        }
        if (ConfessionSystem.Instance != null)
            ConfessionSystem.Instance.OnConfessionResult += OnConfessionResult;

        if (DebtSystem.Instance != null)
            DebtSystem.Instance.OnDebtLevelChanged += OnDebtLevelChanged;

        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.OnEventTriggered += OnGameEventTriggered;
            EventScheduler.Instance.OnEventCompleted += OnGameEventCompleted;
        }
    }

    private void OnDestroy()
    {
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
        if (campusMapUI != null) campusMapUI.Destroy();
    }

    private bool CanProcessHotkeys()
    {
        if (IsModalOpen)
        {
            return false;
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive)
        {
            return false;
        }

        if (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting)
        {
            return false;
        }

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen)
        {
            return false;
        }

        return true;
    }

    private void ToggleInfoPanel()
    {
        if (InfoPanelManager.Instance == null)
        {
            return;
        }

        if (InfoPanelManager.Instance.IsOpen)
        {
            InfoPanelManager.Instance.ClosePanel();
        }
        else
        {
            InfoPanelManager.Instance.OpenPanel(0);
        }
    }

    private void OpenSocialPanel()
    {
        if (builder == null || builder.npcInteractionMenu == null)
        {
            return;
        }

        if (builder.hudAnimator != null && builder.btnFeature != null)
        {
            builder.hudAnimator.ButtonPressEffect(builder.btnFeature);
        }

        builder.npcInteractionMenu.ShowForNPC(null);
    }

    private void ToggleTalentPanel()
    {
        if (TalentUI.Instance == null)
        {
            return;
        }

        if (builder != null && builder.hudAnimator != null && builder.btnTalent != null)
        {
            builder.hudAnimator.ButtonPressEffect(builder.btnTalent);
        }

        if (TalentUI.Instance.IsOpen)
        {
            TalentUI.Instance.ClosePanel();
        }
        else
        {
            TalentUI.Instance.ShowPanel();
        }
    }

    private void EnsureDataInstances()
    {
        gameState = GameState.Instance;
        if (gameState == null)
        {
            GameObject gsObj = new GameObject("GameState");
            gameState = gsObj.AddComponent<GameState>();
        }

        playerAttributes = PlayerAttributes.Instance;
        if (playerAttributes == null)
        {
            GameObject paObj = new GameObject("PlayerAttributes");
            playerAttributes = paObj.AddComponent<PlayerAttributes>();
        }
    }

    private void CreateAttributeBars()
    {
        // 只在角色卡片中创建前3个核心属性（学力/魅力/体魄），参照截图
        PlayerAttributes.AttributeInfo[] attrs = playerAttributes.GetAllAttributes();
        attrBars.Clear();

        // 显示前3个属性（学力/魅力/体魄），其余在详情面板中查看
        int displayCount = Mathf.Min(3, attrs.Length);
        for (int i = 0; i < displayCount; i++)
        {
            AttributeBar bar = builder.AddAttributeBar();
            if (bar != null)
            {
                bar.SetAttributeImmediate(attrs[i]);
                attrBars.Add(bar);
            }
        }
    }

    // ========== 数据刷新 ==========

    public void RefreshAll()
    {
        RefreshTopBar();
        RefreshAttributes();
    }

    public void RefreshTopBar()
    {
        if (builder == null || gameState == null) return;

        // 左上角：季节+年份
        RefreshSeasonDisplay();

        // 中央：AP 进度条
        RefreshAPBar();

        // 右上角：属性快捷显示
        RefreshQuickStats();

        // 左下角：角色卡片信息
        RefreshCharacterCard();

        // GPA 显示
        RefreshGPA();
    }

    private void RefreshSeasonDisplay()
    {
        if (builder.seasonText == null || builder.yearAgeText == null) return;

        int month = gameState.CurrentMonth;
        string season;
        Color seasonColor;

        if (month >= 3 && month <= 5)
        {
            season = "春";
            seasonColor = new Color(0.45f, 0.78f, 0.45f);
        }
        else if (month >= 6 && month <= 8)
        {
            season = "夏";
            seasonColor = new Color(0.95f, 0.55f, 0.20f);
        }
        else if (month >= 9 && month <= 11)
        {
            season = "秋";
            seasonColor = new Color(0.85f, 0.60f, 0.25f);
        }
        else
        {
            season = "冬";
            seasonColor = new Color(0.50f, 0.70f, 0.90f);
        }

        builder.seasonText.text = season;
        if (builder.seasonIcon != null)
            builder.seasonIcon.color = seasonColor;

        // 年份 + 学年
        int baseYear = 2024; // 入学年份
        int yearOffset = gameState.CurrentYear - 1;
        if (gameState.CurrentSemester == 2 && gameState.CurrentMonth >= 2 && gameState.CurrentMonth <= 6)
            yearOffset++; // 下学期跨年

        builder.yearAgeText.text = $"{baseYear + yearOffset}\n<size=16>{gameState.GetYearName()}</size>";
    }

    private void RefreshAPBar()
    {
        if (builder.apBarFill == null || builder.apText == null) return;

        int current = gameState.ActionPoints;
        int max = gameState.EffectiveMaxActionPoints;

        float ratio = max > 0 ? (float)current / max : 0;
        builder.apBarFill.fillAmount = ratio;
        builder.apText.text = $"{current}";

        // AP不足时变色
        if (ratio <= 0.2f)
            builder.apBarFill.color = new Color(0.95f, 0.35f, 0.25f); // 红色
        else if (ratio <= 0.4f)
            builder.apBarFill.color = new Color(0.95f, 0.70f, 0.20f); // 橙色
        else
            builder.apBarFill.color = new Color(1.0f, 0.82f, 0.0f);   // 黄色
    }

    private void RefreshQuickStats()
    {
        if (playerAttributes == null) return;

        // 金钱
        if (builder.moneyStatText != null)
        {
            int money = gameState.Money;
            if (money >= 10000)
                builder.moneyStatText.text = $"{money / 10000f:F1}w";
            else
                builder.moneyStatText.text = $"{money}";

            // 透支变红
            if (money < 0)
                builder.moneyStatText.color = new Color(1f, 0.3f, 0.3f);
            else if (money < 200)
                builder.moneyStatText.color = new Color(1f, 0.65f, 0.2f);
            else
                builder.moneyStatText.color = new Color(0.20f, 0.15f, 0.10f);
        }

        // 魅力/人气（带变化量显示）
        if (builder.popularityStatText != null)
        {
            builder.popularityStatText.text = $"{playerAttributes.Charm}";
        }

        // 心情
        if (builder.moodStatText != null)
        {
            builder.moodStatText.text = $"{playerAttributes.Mood}";
        }

        // 压力（显示为体力指标）
        if (builder.energyStatText != null)
        {
            builder.energyStatText.text = $"{playerAttributes.Stress}";
            // 压力高时变红
            if (playerAttributes.Stress >= 80)
                builder.energyStatText.color = new Color(0.95f, 0.30f, 0.30f);
            else
                builder.energyStatText.color = new Color(0.20f, 0.15f, 0.10f);
        }
    }

    private void RefreshCharacterCard()
    {
        if (builder.playerNameText != null)
            builder.playerNameText.text = gameState.PlayerName;

        if (builder.playerGradeText != null)
            builder.playerGradeText.text = gameState.GetYearName();
    }

    public void RefreshGPA()
    {
        if (builder == null || builder.gpaText == null) return;

        if (ExamSystem.Instance != null && ExamSystem.Instance.GetCumulativeGPA() > 0)
        {
            float gpa = ExamSystem.Instance.GetCumulativeGPA();
            builder.gpaText.text = $"GPA {gpa:F2}";
            builder.gpaText.gameObject.SetActive(true);

            if (gpa >= 3.5f)
                builder.gpaText.color = new Color(0.85f, 0.65f, 0.10f);
            else if (gpa >= 2.0f)
                builder.gpaText.color = new Color(0.20f, 0.15f, 0.10f);
            else
                builder.gpaText.color = new Color(1.0f, 0.3f, 0.3f);
        }
    }

    private void OnExamCompleted(SemesterGPA semesterGPA)
    {
        Debug.Log($"[HUDManager] 收到考试完成事件，学期 GPA={semesterGPA.gpa:F2}");
        RefreshGPA();
    }

    public void RefreshAttributes()
    {
        if (playerAttributes == null) return;

        PlayerAttributes.AttributeInfo[] attrs = playerAttributes.GetAllAttributes();
        for (int i = 0; i < attrBars.Count && i < attrs.Length; i++)
        {
            attrBars[i].SetAttribute(attrs[i]);
        }

        // 同时更新右上角快捷属性
        RefreshQuickStats();
    }

    // ========== 行动点显示 ==========

    private string BuildActionPointsDisplay()
    {
        int current = gameState.ActionPoints;
        int max = gameState.EffectiveMaxActionPoints;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < max; i++)
        {
            sb.Append(i < current ? "\u25CF" : "\u25CB");
        }
        return sb.ToString();
    }

    // ========== 按钮事件 ==========

    private void BindButtonEvents()
    {
        RefreshBottomBar();
    }

    private void InitMapUI()
    {
        if (builder.hudCanvas == null) return;

        campusMapUI = new CampusMapUI();
        campusMapUI.BuildMapOverlay(builder.hudCanvas);
        campusMapUI.OnLocationNodeClicked += OnLocationNodeClicked;

        detailPanel = new LocationDetailPanel();
        detailPanel.Build(campusMapUI.OverlayRoot);
        detailPanel.OnNavigated += OnLocationNavigated;
    }

    // ========== 地图事件 ==========

    private void OnLocationNodeClicked(LocationId locationId)
    {
        if (detailPanel != null)
            detailPanel.Show(locationId);
    }

    private void OnLocationNavigated(LocationId targetLocation)
    {
        if (LocationManager.Instance != null)
        {
            Vector3 targetPos = LocationManager.Instance.GetLocationWorldCenter(targetLocation);
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.transform.position = targetPos;
        }

        if (campusMapUI != null) campusMapUI.HideOverlay();
        if (campusMapUI != null) campusMapUI.RefreshMap();
        RefreshBottomBar();
        RefreshTopBar();
    }

    private void OnLocationChangedHandler(LocationId from, LocationId to)
    {
        if (campusMapUI != null) campusMapUI.RefreshMap();
        RefreshBottomBar();

        // 显示地点通知
        ShowLocationNotice(to);
    }

    private void ToggleMapOverlay()
    {
        if (campusMapUI == null) return;
        if (campusMapUI.IsVisible)
            campusMapUI.HideOverlay();
        else
            campusMapUI.ShowOverlay();
    }

    // ========== 地点通知 ==========

    private void ShowLocationNotice(LocationId location)
    {
        if (builder.locationNotice == null) return;

        string locName = location.ToString();
        if (LocationManager.Instance != null)
        {
            var locDef = LocationManager.Instance.GetLocation(location);
            if (locDef != null) locName = locDef.displayName;
        }

        builder.locationNoticeText.text = $"{locName}已到达";
        builder.locationNotice.SetActive(true);

        StartCoroutine(HideLocationNoticeAfterDelay(2.5f));
    }

    private IEnumerator HideLocationNoticeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (builder.locationNotice != null)
            builder.locationNotice.SetActive(false);
    }

    // ========== 动态底栏（行动图标按钮） ==========

    private void RefreshBottomBar()
    {
        ClearDynamicButtons();
        HideStaticButtons();

        LocationId currentLoc = GameState.Instance != null ?
            GameState.Instance.CurrentLocation : LocationId.Dormitory;

        ActionDefinition[] actions;
        if (LocationManager.Instance != null)
            actions = LocationManager.Instance.GetAvailableActions(currentLoc);
        else if (ActionSystem.Instance != null)
            actions = ActionSystem.Instance.GetAllActions();
        else
            return;

        Transform actionRow = builder.actionButtonRow != null ?
            builder.actionButtonRow.transform : null;
        if (actionRow == null) return;

        foreach (var action in actions)
        {
            Button btn = CreateDynamicActionButton(actionRow, action);
            dynamicActionButtons.Add(btn);
        }
    }

    private void ClearDynamicButtons()
    {
        foreach (var btn in dynamicActionButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        dynamicActionButtons.Clear();
    }

    private Button CreateDynamicActionButton(Transform parent, ActionDefinition action)
    {
        // 获取图标和颜色
        string icon = ActionIcons.ContainsKey(action.id) ? ActionIcons[action.id] : "\u2B50";
        Color iconColor = ActionColors.ContainsKey(action.id) ? ActionColors[action.id] :
            new Color(0.50f, 0.50f, 0.60f);

        Button btn = builder.CreateActionIconButton(parent, "Btn_" + action.id, icon, action.displayName, iconColor);

        // 如果有警告（AP不足等），显示红点
        if (gameState != null && action.actionPointCost > gameState.ActionPoints)
        {
            // 添加红色警告点
            GameObject alertDot = new GameObject("AlertDot");
            alertDot.transform.SetParent(btn.transform, false);
            RectTransform adRT = alertDot.AddComponent<RectTransform>();
            adRT.anchorMin = new Vector2(1, 1);
            adRT.anchorMax = new Vector2(1, 1);
            adRT.pivot = new Vector2(0.5f, 0.5f);
            adRT.anchoredPosition = new Vector2(-5, -5);
            adRT.sizeDelta = new Vector2(16, 16);
            Image adImg = alertDot.AddComponent<Image>();
            adImg.color = new Color(0.95f, 0.25f, 0.25f);

            TextMeshProUGUI adText = alertDot.AddComponent<TextMeshProUGUI>();
            adText.text = "!";
            adText.fontSize = 12f;
            adText.color = Color.white;
            adText.alignment = TextAlignmentOptions.Center;
        }

        string actionId = action.id;
        btn.onClick.AddListener(() => OnDynamicActionButtonClicked(actionId, btn));

        return btn;
    }

    private void OnDynamicActionButtonClicked(string actionId, Button button)
    {
        if (ActionSystem.Instance == null || !ActionSystem.Instance.CanExecuteAction(actionId))
        {
            Debug.Log($"[HUD] 无法执行行动: {actionId}");
            StartCoroutine(ShakeButtonCoroutine(button));
            return;
        }

        if (builder.hudAnimator != null)
            builder.hudAnimator.ButtonPressEffect(button);

        ActionSystem.Instance.ExecuteAction(actionId);
    }

    private static readonly Dictionary<string, string> ActionIdMap = new Dictionary<string, string>
    {
        { "自习", "study" },
        { "社交", "social" },
        { "出校门", "goout" },
        { "睡觉", "sleep" }
    };

    private void OnActionButtonClicked(string actionName, Button button)
    {
        if (actionName == "社交")
        {
            if (builder.npcInteractionMenu != null)
            {
                if (builder.hudAnimator != null)
                    builder.hudAnimator.ButtonPressEffect(button);
                builder.npcInteractionMenu.ShowForNPC(null);
            }
            return;
        }

        if (!ActionIdMap.TryGetValue(actionName, out string actionId))
        {
            Debug.LogWarning($"[HUD] 未知行动名称: {actionName}");
            return;
        }

        if (ActionSystem.Instance == null || !ActionSystem.Instance.CanExecuteAction(actionId))
        {
            Debug.Log($"[HUD] 无法执行行动: {actionName}（行动点或金钱不足）");
            StartCoroutine(ShakeButtonCoroutine(button));
            return;
        }

        if (builder.hudAnimator != null)
            builder.hudAnimator.ButtonPressEffect(button);

        ActionSystem.Instance.ExecuteAction(actionId);
    }

    // ========== 模态控制 ==========

    public bool IsModalOpen { get; private set; }

    private void SetModalState(bool modal)
    {
        IsModalOpen = modal;
        foreach (var btn in dynamicActionButtons)
        {
            if (btn != null) btn.interactable = !modal;
        }
    }

    // ========== 回合推进响应 ==========

    private void OnRoundAdvanced(GameState.RoundAdvanceResult result)
    {
        switch (result)
        {
            case GameState.RoundAdvanceResult.NextRound:
                Debug.Log($"[HUD] 新回合开始 — {gameState.GetTimeDescription()}");
                ShowRoundNews();
                break;
            case GameState.RoundAdvanceResult.NextSemester:
                Debug.Log($"[HUD] 新学期开始 — {gameState.GetTimeDescription()}");
                ShowSemesterSummary();
                break;
            case GameState.RoundAdvanceResult.NextYear:
                Debug.Log($"[HUD] 新学年开始 — {gameState.GetTimeDescription()}");
                ShowSemesterSummary();
                break;
            case GameState.RoundAdvanceResult.Graduated:
                Debug.Log("[HUD] 恭喜毕业！游戏结束。");
                ShowGraduationEnding();
                break;
        }

        if (campusMapUI != null) campusMapUI.RefreshMap();
        RefreshBottomBar();
    }

    private void ShowRoundNews()
    {
        if (NewsSystem.Instance == null) return;

        SetActionButtonsInteractable(false);
        NewsSystem.Instance.OnNewsDismissed += OnNewsDismissed;
        NewsSystem.Instance.ShowNews();
    }

    private void OnNewsDismissed()
    {
        if (NewsSystem.Instance != null)
            NewsSystem.Instance.OnNewsDismissed -= OnNewsDismissed;

        SetActionButtonsInteractable(true);
    }

    private void OnSocialFeedback(string npcId, string actionName, int delta)
    {
        string sign = delta >= 0 ? "+" : "";
        Debug.Log($"[HUD] 社交反馈: NPC={npcId}, 行动={actionName}, 好感度{sign}{delta}");
        RefreshAll();
    }

    private void OnDebtLevelChanged(DebtSystem.DebtLevel newLevel)
    {
        Debug.Log($"[HUD] 债务等级变化: {newLevel}");
        RefreshTopBar();
    }

    // ========== 恋爱系统响应 ==========

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

    private void OnRomanceHealthChanged(string npcId)
    {
        int health = RomanceSystem.Instance != null ? RomanceSystem.Instance.GetRomanceHealth(npcId) : 0;
        Debug.Log($"[HUD] 恋爱健康度变化: {npcId} -> {health}/100");
    }

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

    private void OnGameEventTriggered(EventDefinition evt)
    {
        Debug.Log($"[HUD] 游戏事件触发: {evt.id} - {evt.title}，锁定行动按钮");
        SetActionButtonsInteractable(false);
    }

    private void OnGameEventCompleted(EventDefinition evt)
    {
        if (EventScheduler.Instance == null || !EventScheduler.Instance.HasPendingEvents())
        {
            Debug.Log($"[HUD] 游戏事件完成: {evt.id}，恢复行动按钮");
            SetActionButtonsInteractable(true);
        }
    }

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

    // ========== 协程动画 ==========

    private IEnumerator ShowInitialNews()
    {
        yield return new WaitForSeconds(1.5f);
        ShowRoundNews();
    }

    private IEnumerator PlayEntryAnimation()
    {
        if (builder == null || builder.hudCanvas == null) yield break;

        Transform canvasRoot = builder.hudCanvas.transform;
        List<CanvasGroup> panels = new List<CanvasGroup>();

        foreach (Transform child in canvasRoot)
        {
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            panels.Add(cg);
        }

        yield return null;

        float delay = 0.06f;
        float duration = 0.3f;

        foreach (CanvasGroup cg in panels)
        {
            StartCoroutine(FadeInCoroutine(cg, duration));
            RectTransform rt = cg.GetComponent<RectTransform>();
            if (rt != null)
                StartCoroutine(SlideInCoroutine(rt, duration));
            yield return new WaitForSeconds(delay);
        }
    }

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

    private IEnumerator SlideInCoroutine(RectTransform rt, float duration)
    {
        Vector2 originalPos = rt.anchoredPosition;
        Vector2 startPos = originalPos + new Vector2(0, -20f);
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
            float offset = Mathf.Sin(progress * Mathf.PI * 6f) * shakeIntensity * (1f - progress);
            rt.anchoredPosition = originalPos + new Vector2(offset, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = originalPos;
    }

    // ========== 静态按钮控制 ==========

    private void HideStaticButtons()
    {
        if (builder == null) return;
        if (builder.btnStudy != null) builder.btnStudy.gameObject.SetActive(false);
        if (builder.btnSocial != null && builder.btnSocial != builder.btnFeature)
            builder.btnSocial.gameObject.SetActive(false);
        if (builder.btnGoOut != null) builder.btnGoOut.gameObject.SetActive(false);
        if (builder.btnSleep != null) builder.btnSleep.gameObject.SetActive(false);
    }

    // ========== 学期总结 / 结局面板 ==========

    private void ShowSemesterSummary()
    {
        SetModalState(true);

        if (SemesterSummarySystem.Instance != null && gameState != null)
        {
            int prevYear = gameState.CurrentYear;
            int prevSemester = gameState.CurrentSemester == 1 ? 2 : 1;
            if (gameState.CurrentSemester == 1) prevYear = Mathf.Max(1, prevYear - 1);

            var summaryData = SemesterSummarySystem.Instance.GenerateSemesterSummary(prevYear, prevSemester);
            Debug.Log($"[HUD] 学期总结: {summaryData.yearName}{summaryData.semesterName} GPA={summaryData.gpa:F2} 总分={summaryData.totalScore} 评级={summaryData.grade}");

            if (SemesterSummaryUI.Instance == null)
            {
                GameObject uiObj = new GameObject("SemesterSummaryUI");
                uiObj.AddComponent<SemesterSummaryUI>();
            }

            SemesterSummaryUI.Instance.Show(summaryData);
            StartCoroutine(WaitForSummaryClose());
        }
        else
        {
            SetModalState(false);
        }
    }

    private void ShowGraduationEnding()
    {
        SetModalState(true);

        if (EndingDeterminer.Instance != null)
        {
            EndingResult result = EndingDeterminer.Instance.DetermineEnding();
            Debug.Log($"[HUD] 毕业结局: {result.ending.name} ({result.ending.stars}★) — {result.ending.description}");

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

    private IEnumerator WaitForSummaryClose()
    {
        while (SemesterSummaryUI.Instance != null && SemesterSummaryUI.Instance.isShowing)
        {
            yield return null;
        }
        SetModalState(false);
    }

}
