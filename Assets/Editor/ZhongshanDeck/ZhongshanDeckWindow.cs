using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ZhongshanDeckWindow : EditorWindow
{
    public enum Tab
    {
        Overview,
        Attributes,
        Time,
        Endings,
        Events,
        NPC,
        Economy,
        Formula,
        Snapshots,
        Logs
    }

    private static readonly string[] TabLabels =
    {
        "总览", "属性", "时间", "结局", "事件", "NPC", "经济", "公式", "快照", "日志"
    };

    private static readonly (string Label, string Key, int Min, int Max)[] AttributeDefs =
    {
        ("学力", "Study", 0, 999),
        ("魅力", "Charm", 0, 999),
        ("体魄", "Physique", 0, 999),
        ("领导力", "Leadership", 0, 999),
        ("压力", "Stress", 0, PlayerAttributes.MaxStatusValue),
        ("心情", "Mood", 0, PlayerAttributes.MaxStatusValue),
        ("黑暗值", "Darkness", 0, 999),
        ("负罪感", "Guilt", 0, PlayerAttributes.MaxStatusValue),
        ("幸运", "Luck", 0, PlayerAttributes.MaxStatusValue),
        ("金钱", "Money", -999999, 999999)
    };

    private Tab currentTab;
    private Vector2 scrollPosition;
    private readonly Dictionary<string, int> attributeInputs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private string eventIdInput = string.Empty;
    private string flagInput = string.Empty;
    private string snapshotNameInput = string.Empty;
    private string npcIdInput = string.Empty;
    private int npcAffinityValue;
    private int npcHealthValue = 70;
    private int npcCooldownValue;
    private RomanceState npcRomanceState = RomanceState.None;
    private int yearInput = 1;
    private int semesterInput = 1;
    private int roundInput = 1;
    private int moneyInput = 8000;
    private int formulaTypeIndex;
    private int formulaA = 10;
    private int formulaB = 80;
    private int formulaC = 2;
    private string formulaResult = string.Empty;
    private string logFilter = string.Empty;

    [InitializeOnLoadMethod]
    private static void AutoEnsureStateAsset()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isCompiling)
            {
                ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
            }
        };
    }

    public static void Open(Tab tab = Tab.Overview)
    {
        ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        ZhongshanDeckWindow window = GetWindow<ZhongshanDeckWindow>("钟山台");
        window.minSize = new Vector2(900f, 620f);
        window.currentTab = tab;
        window.Show();
    }

    private void OnEnable()
    {
        ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        SyncRuntimeValues();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode || change == PlayModeStateChange.EnteredEditMode)
        {
            SyncRuntimeValues();
            Repaint();
        }
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (currentTab)
        {
            case Tab.Overview: DrawOverview(); break;
            case Tab.Attributes: DrawAttributes(); break;
            case Tab.Time: DrawTime(); break;
            case Tab.Endings: DrawEndings(); break;
            case Tab.Events: DrawEvents(); break;
            case Tab.NPC: DrawNPC(); break;
            case Tab.Economy: DrawEconomy(); break;
            case Tab.Formula: DrawFormula(); break;
            case Tab.Snapshots: DrawSnapshots(); break;
            case Tab.Logs: DrawLogs(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        int selected = GUILayout.Toolbar((int)currentTab, TabLabels, EditorStyles.toolbarButton);
        if (selected != (int)currentTab)
        {
            currentTab = (Tab)selected;
            SyncRuntimeValues();
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            SyncRuntimeValues();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOverview()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("钟山台总控", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里是 Unity 导航栏对应的局外调试台。运行时钟山台和这里共享同一份工具状态，编辑器内数据优先。", MessageType.Info);

        EditorGUILayout.LabelField("启动流", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        bool skipSplash = EditorGUILayout.Toggle("跳过开屏", StartupFlowSettings.SkipSplashLogo);
        bool skipCreate = EditorGUILayout.Toggle("跳过建角", StartupFlowSettings.SkipCharacterCreation);
        bool skipIntro = EditorGUILayout.Toggle("跳过开场", StartupFlowSettings.SkipOpeningStory);
        bool skipTitle = EditorGUILayout.Toggle("跳过首页", StartupFlowSettings.SkipTitleScreen);
        string defaultName = EditorGUILayout.TextField("默认姓名", StartupFlowSettings.DefaultPlayerName);
        int defaultGender = EditorGUILayout.Popup("默认性别", StartupFlowSettings.DefaultPlayerGender, new[] { "男", "女" });
        string defaultMajor = EditorGUILayout.TextField("默认专业", StartupFlowSettings.DefaultPlayerMajor);
        if (EditorGUI.EndChangeCheck())
        {
            StartupFlowSettings.SkipSplashLogo = skipSplash;
            StartupFlowSettings.SkipCharacterCreation = skipCreate;
            StartupFlowSettings.SkipOpeningStory = skipIntro;
            StartupFlowSettings.SkipTitleScreen = skipTitle;
            StartupFlowSettings.DefaultPlayerName = defaultName;
            StartupFlowSettings.DefaultPlayerGender = defaultGender;
            StartupFlowSettings.DefaultPlayerMajor = defaultMajor;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用快速开局", GUILayout.Width(120f)))
        {
            StartupFlowSettings.ApplyQuickStartPreset();
        }
        if (GUILayout.Button("定位状态资产", GUILayout.Width(120f)))
        {
            Selection.activeObject = ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("运行时钟山台", EditorStyles.boldLabel);
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后，可在这里直接打开、关闭或驱动游戏内钟山台。", MessageType.None);
        }
        else
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开游戏内钟山台", GUILayout.Width(140f)))
                {
                    DebugConsoleManager.Instance?.Open();
                }
                if (GUILayout.Button("关闭游戏内钟山台", GUILayout.Width(140f)))
                {
                    DebugConsoleManager.Instance?.Close();
                }
                if (GUILayout.Button("上一句对话", GUILayout.Width(120f)))
                {
                    DialogueSystem.Instance?.DebugStepBackOneLine();
                }
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("模块捷径", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("剧情与事件编辑器", GUILayout.Width(160f)))
        {
            EditorApplication.ExecuteMenuItem("钟山台/造物主 (Creator Toolkit)/剧情与事件编辑器");
        }
        if (GUILayout.Button("属性", GUILayout.Width(70f))) currentTab = Tab.Attributes;
        if (GUILayout.Button("时间", GUILayout.Width(70f))) currentTab = Tab.Time;
        if (GUILayout.Button("事件", GUILayout.Width(70f))) currentTab = Tab.Events;
        if (GUILayout.Button("NPC", GUILayout.Width(70f))) currentTab = Tab.NPC;
        if (GUILayout.Button("快照", GUILayout.Width(70f))) currentTab = Tab.Snapshots;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAttributes()
    {
        EditorGUILayout.LabelField("属性调试", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("步进", GUILayout.Width(40f));
        int[] steps = DebugPresets.GetStepOptions();
        for (int i = 0; i < steps.Length; i++)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = i == DebugPresets.CurrentStepIndex ? new Color(0.35f, 0.6f, 0.9f) : Color.white;
            if (GUILayout.Button(steps[i].ToString(), GUILayout.Width(50f)))
            {
                DebugPresets.SetStepIndex(i);
            }
            GUI.backgroundColor = oldColor;
        }
        EditorGUILayout.EndHorizontal();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("属性修改需要在 Play 模式中执行。步进设置已同步到工具状态资产。", MessageType.None);
            return;
        }

        foreach (var item in AttributeDefs)
        {
            attributeInputs[item.Key] = DebugPresets.GetAttributeValue(item.Key);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.Label, GUILayout.Width(80f));
            if (GUILayout.Button("-", GUILayout.Width(28f)))
            {
                DebugPresets.AdjustAttribute(item.Key, false);
            }

            int nextValue = EditorGUILayout.IntField(attributeInputs[item.Key], GUILayout.Width(90f));
            nextValue = Mathf.Clamp(nextValue, item.Min, item.Max);
            if (nextValue != attributeInputs[item.Key])
            {
                attributeInputs[item.Key] = nextValue;
                DebugPresets.SetAttributeValue(item.Key, nextValue);
            }

            if (GUILayout.Button("+", GUILayout.Width(28f)))
            {
                DebugPresets.AdjustAttribute(item.Key, true);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("预设：新生", GUILayout.Width(100f)))
        {
            PlayerAttributes.Instance?.SetAll(12, 8, 10, 6, 15, 80, 0, 0, 55);
            if (GameState.Instance != null) GameState.Instance.Money = 8000;
        }
        if (GUILayout.Button("预设：封顶", GUILayout.Width(100f)))
        {
            PlayerAttributes.Instance?.SetAll(100, 100, 100, 100, 0, 100, 100, 0, 100);
            if (GameState.Instance != null) GameState.Instance.Money = 999999;
        }
        if (GUILayout.Button("状态归位", GUILayout.Width(100f)) && PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.Stress = Mathf.Clamp(PlayerAttributes.Instance.Stress, 0, PlayerAttributes.MaxStatusValue);
            PlayerAttributes.Instance.Mood = Mathf.Clamp(PlayerAttributes.Instance.Mood, 0, PlayerAttributes.MaxStatusValue);
            PlayerAttributes.Instance.Guilt = Mathf.Clamp(PlayerAttributes.Instance.Guilt, 0, PlayerAttributes.MaxStatusValue);
            PlayerAttributes.Instance.Luck = Mathf.Clamp(PlayerAttributes.Instance.Luck, 0, PlayerAttributes.MaxStatusValue);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTime()
    {
        EditorGUILayout.LabelField("时间调试", EditorStyles.boldLabel);
        if (!EditorApplication.isPlaying || GameState.Instance == null)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后可调整学年、学期和回合。", MessageType.None);
            return;
        }

        yearInput = EditorGUILayout.IntField("学年", yearInput);
        semesterInput = EditorGUILayout.IntField("学期", semesterInput);
        roundInput = EditorGUILayout.IntField("回合", roundInput);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("跳转到指定时间", GUILayout.Width(140f)))
        {
            int year = Mathf.Clamp(yearInput, 1, 4);
            int semester = Mathf.Clamp(semesterInput, 1, 2);
            int round = Mathf.Clamp(roundInput, 1, GameState.MaxRoundsPerSemester);
            int month = GameState.CalculateMonth(semester, round);
            GameState.Instance.SetState(year, semester, round, month, GameState.Instance.Money, GameState.Instance.ActionPoints);
            DebugConsoleManager.Log("Time", $"Jumped to Y{year} S{semester} R{round}");
            SyncRuntimeValues();
        }
        if (GUILayout.Button("前进 1 回合", GUILayout.Width(100f)))
        {
            GameState.Instance.AdvanceRound();
            SyncRuntimeValues();
        }
        if (GUILayout.Button("前进 5 回合", GUILayout.Width(100f)))
        {
            for (int i = 0; i < 5; i++)
            {
                if (GameState.Instance.AdvanceRound() == GameState.RoundAdvanceResult.Graduated)
                    break;
            }
            SyncRuntimeValues();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox($"当前：{GameState.Instance.GetTimeDescription()}", MessageType.None);
    }

    private void DrawEndings()
    {
        EditorGUILayout.LabelField("结局模拟", EditorStyles.boldLabel);
        if (!EditorApplication.isPlaying || EndingDeterminer.Instance == null)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后显示当前结局预测。", MessageType.None);
            return;
        }

        EndingResult result = EndingDeterminer.Instance.DetermineEnding();
        if (result == null || result.ending == null)
        {
            EditorGUILayout.HelpBox("暂无结局结果。", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField("当前结局", result.ending.name);
        EditorGUILayout.LabelField("层级", $"{result.ending.layer}");
        EditorGUILayout.LabelField("星级", $"{result.ending.stars}");
        EditorGUILayout.LabelField("结算分", result.finalScore.ToString("F1"));
        EditorGUILayout.LabelField("天赋点", result.talentPoints.ToString());
        EditorGUILayout.Space(6f);

        List<EndingDefinition> matches = EndingDeterminer.Instance.GetMatchingEndings(5);
        EditorGUILayout.LabelField("候选结局", EditorStyles.boldLabel);
        for (int i = 0; i < matches.Count; i++)
        {
            EditorGUILayout.LabelField($"{i + 1}. {matches[i].name} | 层级 {matches[i].layer} | {matches[i].stars} 星");
        }
    }

    private void DrawEvents()
    {
        EditorGUILayout.LabelField("事件调试", EditorStyles.boldLabel);
        eventIdInput = EditorGUILayout.TextField("事件 ID", eventIdInput);
        flagInput = EditorGUILayout.TextField("标记名", flagInput);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("触发事件", GUILayout.Width(100f)) && EventScheduler.Instance != null && !string.IsNullOrWhiteSpace(eventIdInput))
            {
                EventScheduler.Instance.EnqueueEvent(eventIdInput.Trim());
                DebugConsoleManager.Log("Event", $"Force trigger {eventIdInput.Trim()}");
            }
            if (GUILayout.Button("跳过事件", GUILayout.Width(100f)) && EventHistory.Instance != null && !string.IsNullOrWhiteSpace(eventIdInput))
            {
                EventHistory.Instance.RecordEvent(eventIdInput.Trim(), -1);
                DebugConsoleManager.Log("Event", $"Skip event {eventIdInput.Trim()}");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("标记设为真", GUILayout.Width(100f)) && EventHistory.Instance != null && !string.IsNullOrWhiteSpace(flagInput))
            {
                EventHistory.Instance.SetFlag(flagInput.Trim(), true);
                DebugConsoleManager.Log("Event", $"Flag {flagInput.Trim()} -> true");
            }
            if (GUILayout.Button("标记设为假", GUILayout.Width(100f)) && EventHistory.Instance != null && !string.IsNullOrWhiteSpace(flagInput))
            {
                EventHistory.Instance.SetFlag(flagInput.Trim(), false);
                DebugConsoleManager.Log("Event", $"Flag {flagInput.Trim()} -> false");
            }
            EditorGUILayout.EndHorizontal();
        }

        if (EditorApplication.isPlaying && EventScheduler.Instance != null)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField($"已加载事件: {EventScheduler.Instance.GetLoadedEventCount()}");
            EditorGUILayout.LabelField($"待处理队列: {EventScheduler.Instance.GetPendingEventCount()}");
        }
    }

    private void DrawNPC()
    {
        EditorGUILayout.LabelField("NPC 调试", EditorStyles.boldLabel);
        if (!EditorApplication.isPlaying || NPCDatabase.Instance == null)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后可直接调整 NPC 好感、恋爱状态和冷却。", MessageType.None);
            return;
        }

        NPCData[] allNPCs = NPCDatabase.Instance.GetAllNPCs();
        if (allNPCs == null || allNPCs.Length == 0)
        {
            EditorGUILayout.HelpBox("当前没有可用 NPC。", MessageType.None);
            return;
        }

        string[] npcNames = new string[allNPCs.Length];
        int currentIndex = 0;
        for (int i = 0; i < allNPCs.Length; i++)
        {
            npcNames[i] = $"{allNPCs[i].displayName} ({allNPCs[i].id})";
            if (allNPCs[i].id == npcIdInput)
            {
                currentIndex = i;
            }
        }

        if (string.IsNullOrEmpty(npcIdInput))
        {
            npcIdInput = allNPCs[0].id;
        }

        int nextIndex = EditorGUILayout.Popup("NPC", currentIndex, npcNames);
        npcIdInput = allNPCs[Mathf.Clamp(nextIndex, 0, allNPCs.Length - 1)].id;

        NPCRelationshipData relationship = AffinitySystem.Instance != null ? AffinitySystem.Instance.GetRelationship(npcIdInput) : null;
        RomanceRecord record = RomanceSystem.Instance != null ? RomanceSystem.Instance.DebugGetRecord(npcIdInput) : null;
        if (relationship == null || record == null)
        {
            EditorGUILayout.HelpBox("AffinitySystem 或 RomanceSystem 尚未就绪。", MessageType.Warning);
            return;
        }

        npcAffinityValue = EditorGUILayout.IntSlider("好感度", relationship.affinity, 0, 100);
        npcHealthValue = EditorGUILayout.IntSlider("恋爱健康度", record.healthScore, 0, 100);
        npcCooldownValue = EditorGUILayout.IntField("冷却回合", record.cooldownRoundsLeft);
        npcRomanceState = (RomanceState)EditorGUILayout.EnumPopup("恋爱状态", record.state);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用好感", GUILayout.Width(90f)))
            {
                AffinitySystem.Instance.DebugSetAffinity(npcIdInput, npcAffinityValue);
            }
            if (GUILayout.Button("应用恋爱状态", GUILayout.Width(120f)))
            {
                RomanceSystem.Instance.DebugSetRomanceState(npcIdInput, npcRomanceState, npcHealthValue, npcCooldownValue);
            }
        }

        EditorGUILayout.LabelField("关系等级", relationship.level.ToString());
        EditorGUILayout.LabelField("上次互动", string.IsNullOrEmpty(relationship.lastInteractionActionId) ? "-" : relationship.lastInteractionActionId);
        EditorGUILayout.LabelField("连续未互动", relationship.consecutiveNoInteractionTurns.ToString());
    }

    private void DrawEconomy()
    {
        EditorGUILayout.LabelField("经济调试", EditorStyles.boldLabel);
        if (!EditorApplication.isPlaying || GameState.Instance == null)
        {
            EditorGUILayout.HelpBox("进入 Play 模式后可直接调整金钱。", MessageType.None);
            return;
        }

        moneyInput = EditorGUILayout.IntField("金钱", moneyInput);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用", GUILayout.Width(90f)))
        {
            GameState.Instance.Money = moneyInput;
            DebugConsoleManager.Log("Economy", $"Money -> {moneyInput}");
        }
        if (GUILayout.Button("+1000", GUILayout.Width(80f)))
        {
            GameState.Instance.AddMoney(1000);
            moneyInput = GameState.Instance.Money;
        }
        if (GUILayout.Button("-1000", GUILayout.Width(80f)))
        {
            GameState.Instance.AddMoney(-1000);
            moneyInput = GameState.Instance.Money;
        }
        if (GUILayout.Button("+10000", GUILayout.Width(80f)))
        {
            GameState.Instance.AddMoney(10000);
            moneyInput = GameState.Instance.Money;
        }
        if (GUILayout.Button("-10000", GUILayout.Width(80f)))
        {
            GameState.Instance.AddMoney(-10000);
            moneyInput = GameState.Instance.Money;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFormula()
    {
        EditorGUILayout.LabelField("公式工具", EditorStyles.boldLabel);
        formulaTypeIndex = GUILayout.Toolbar(formulaTypeIndex, new[] { "属性", "好感", "金钱" });
        formulaA = EditorGUILayout.IntField("A", formulaA);
        formulaB = EditorGUILayout.IntField("B", formulaB);
        if (formulaTypeIndex != 2)
        {
            formulaC = EditorGUILayout.IntField("C", formulaC);
        }

        if (GUILayout.Button("计算", GUILayout.Width(100f)))
        {
            switch (formulaTypeIndex)
            {
                case 0:
                    float rate = NewGamePlusData.GetInheritRate(Mathf.Max(formulaC, 2));
                    formulaResult =
                        $"继承比例：{rate:P0}\n" +
                        $"继承属性：{NewGamePlusData.CalcInheritedAttribute(formulaA, formulaB, rate)}";
                    break;
                case 1:
                    formulaResult =
                        $"继承好感：{NewGamePlusData.CalcInheritedAffinity(formulaA, formulaC > 0)}";
                    break;
                case 2:
                    formulaResult =
                        $"周目奖励：{NewGamePlusData.GetBonusMoney(Mathf.Max(formulaB, 2))}\n" +
                        $"继承金钱：{NewGamePlusData.CalcInheritedMoney(formulaA, Mathf.Max(formulaB, 2))}";
                    break;
            }
        }

        EditorGUILayout.HelpBox(string.IsNullOrEmpty(formulaResult) ? "计算结果会显示在这里。" : formulaResult, MessageType.None);
    }

    private void DrawSnapshots()
    {
        EditorGUILayout.LabelField("快照", EditorStyles.boldLabel);
        snapshotNameInput = EditorGUILayout.TextField("名称", snapshotNameInput);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || DebugConsoleManager.Instance == null))
        {
            if (GUILayout.Button("保存当前快照", GUILayout.Width(120f)) && !string.IsNullOrWhiteSpace(snapshotNameInput))
            {
                DebugConsoleManager.Instance.SaveSnapshot(snapshotNameInput.Trim());
                snapshotNameInput = string.Empty;
            }
        }

        List<string> snapshotNames = ZhongshanDeckToolStateBridge.GetSnapshotNames();
        if (snapshotNames.Count == 0)
        {
            EditorGUILayout.HelpBox("还没有快照。", MessageType.None);
            return;
        }

        for (int i = 0; i < snapshotNames.Count; i++)
        {
            string snapshotName = snapshotNames[i];
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField(snapshotName);
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || DebugConsoleManager.Instance == null))
                {
                    if (GUILayout.Button("载入", GUILayout.Width(60f)))
                    {
                        DebugConsoleManager.Instance.LoadSnapshot(snapshotName);
                    }
                }

                if (GUILayout.Button("删除", GUILayout.Width(60f)))
                {
                    ZhongshanDeckToolStateBridge.DeleteSnapshot(snapshotName);
                }
            }
        }
    }

    private void DrawLogs()
    {
        EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("全部", GUILayout.Width(60f))) logFilter = string.Empty;
            if (GUILayout.Button("属性", GUILayout.Width(60f))) logFilter = "Attributes";
            if (GUILayout.Button("时间", GUILayout.Width(60f))) logFilter = "Time";
            if (GUILayout.Button("经济", GUILayout.Width(60f))) logFilter = "Economy";
            if (GUILayout.Button("事件", GUILayout.Width(60f))) logFilter = "Event";
            if (GUILayout.Button("清空", GUILayout.Width(60f))) DebugConsoleManager.ClearLogs();
        }

        List<DebugLogEntry> logs = DebugConsoleManager.GetLogEntries();
        StringBuilder builder = new StringBuilder();
        for (int i = logs.Count - 1; i >= 0 && i >= logs.Count - 200; i--)
        {
            DebugLogEntry entry = logs[i];
            if (!MatchesLogFilter(entry))
            {
                continue;
            }

            builder.Append('[').Append(entry.timestamp).Append("] ");
            builder.Append('[').Append(entry.category).Append("] ");
            builder.AppendLine(entry.message);
        }

        EditorGUILayout.TextArea(builder.Length == 0 ? "当前筛选下还没有日志。" : builder.ToString(), GUILayout.ExpandHeight(true), GUILayout.MinHeight(420f));
    }

    private bool MatchesLogFilter(DebugLogEntry entry)
    {
        if (string.IsNullOrEmpty(logFilter))
        {
            return true;
        }

        if (logFilter == "Attributes")
        {
            return entry.category == "Attributes" || entry.category == "Adjust";
        }

        return entry.category == logFilter;
    }

    private void SyncRuntimeValues()
    {
        if (!EditorApplication.isPlaying || GameState.Instance == null)
        {
            return;
        }

        yearInput = GameState.Instance.CurrentYear;
        semesterInput = GameState.Instance.CurrentSemester;
        roundInput = GameState.Instance.CurrentRound;
        moneyInput = GameState.Instance.Money;

        foreach (var item in AttributeDefs)
        {
            attributeInputs[item.Key] = DebugPresets.GetAttributeValue(item.Key);
        }

        if (NPCDatabase.Instance != null)
        {
            NPCData[] allNPCs = NPCDatabase.Instance.GetAllNPCs();
            if ((string.IsNullOrEmpty(npcIdInput) || NPCDatabase.Instance.GetNPC(npcIdInput) == null) && allNPCs.Length > 0)
            {
                npcIdInput = allNPCs[0].id;
            }
        }
    }
}
