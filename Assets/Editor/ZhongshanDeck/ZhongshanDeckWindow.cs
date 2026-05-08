using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ZhongshanDeckWindow : EditorWindow
{
    private sealed class EventListEntry
    {
        public EventDefinition Event;
        public bool IsDraft;
        public string SourceLabel;
    }

    [Serializable]
    private sealed class EventAttributeConditionDraft
    {
        public int attributeIndex;
        public int comparisonIndex;
        public int value;
    }

    [Serializable]
    private sealed class EventEffectDraft
    {
        public int effectTypeIndex;
        public int attributeIndex;
        public string targetText = string.Empty;
        public int value;
    }

    [Serializable]
    private sealed class EventChoiceDraft
    {
        public string text = string.Empty;
        public string nextEventId = string.Empty;
        public List<EventEffectDraft> effects = new List<EventEffectDraft>();
    }

    private const string SplashScenePath = "Assets/Scenes/SplashScreen.unity";
    private const string LoadingScenePath = "Assets/Scenes/LoadingScreen.unity";
    private const string TitleScenePath = "Assets/Scenes/TitleScreen.unity";
    private const string SaveLoadPreviewScenePath = "Assets/Scenes/SaveLoadPreview.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene/GameScene.unity";
    private const string EndingsDataPath = "Assets/Resources/Data/endings.json";
    private const int SceneJumpColumnCount = 3;

    public enum Tab
    {
        Overview,
        SceneJump,
        Attributes,
        Time,
        Content,
        News,
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
        "总览", "场景跳转", "属性", "时间", "内容", "新闻", "结局", "事件", "NPC", "经济", "公式", "快照", "日志"
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
        ("金钱", "Money", -999999, 999999),
        ("行动点", "ActionPoints", 0, 999)
    };

    private static readonly string[] EndingCategoryOrder =
    {
        "特殊/强制",
        "巅峰",
        "学术",
        "仕途",
        "创业",
        "职场",
        "留学",
        "文体/特长",
        "新兴职业/自由",
        "黑暗",
        "保底"
    };

    private static readonly int[] EndingStarOrder = { 7, 6, 5, 4, 3, 2, 1, 0 };

    private Tab currentTab;
    private Vector2 scrollPosition;
    private readonly Dictionary<string, int> attributeInputs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private string eventIdInput = string.Empty;
    private string flagInput = string.Empty;
    private string eventTitleInput = string.Empty;
    private string eventDescriptionInput = string.Empty;
    private int eventTypeIndex = 0;
    private int eventPhaseIndex = 0;
    private int eventPriorityInput = 2;
    private bool eventForcedInput;
    private bool eventRepeatableInput = true;
    private int eventYearTriggerInput;
    private int eventSemesterTriggerInput;
    private int eventRoundMinInput;
    private int eventRoundMaxInput;
    private string eventSpecificRoundsInput = string.Empty;
    private float eventProbabilityInput = 1f;
    private string eventTriggerBehaviorInput = string.Empty;
    private readonly List<EventAttributeConditionDraft> eventAttributeConditionDrafts = new List<EventAttributeConditionDraft>();
    private int eventMinMoneyInput;
    private int eventMaxMoneyInput;
    private int eventMinDarknessInput;
    private readonly List<string> eventRequiredEventIds = new List<string>();
    private readonly List<string> eventExcludedEventIds = new List<string>();
    private int eventRequiredAddIndex;
    private int eventExcludedAddIndex;
    private string eventSpeakerInput = string.Empty;
    private string eventPortraitInput = string.Empty;
    private readonly List<string> eventDialogueLines = new List<string>();
    private readonly List<EventEffectDraft> eventDefaultEffectDrafts = new List<EventEffectDraft>();
    private readonly List<string> eventChainEventIds = new List<string>();
    private int eventChainAddIndex;
    private readonly List<EventChoiceDraft> eventChoiceDrafts = new List<EventChoiceDraft>();
    private string eventAuthoringStatus = string.Empty;
    private int eventTimelineYearInput = 1;
    private int eventTimelineSemesterInput = 1;
    private int eventTimelineRoundInput = 1;
    private int eventTimelinePhaseIndex;
    private int eventRoundAddSelectionIndex;
    private string eventLibrarySearchInput = string.Empty;
    private string eventEditorModeText = "请从上方事件列表右侧的编辑按钮进入详情编辑";
    private string editingEventId = string.Empty;
    private string editingEventSource = string.Empty;
    private bool isCreatingNewEvent;
    private string snapshotNameInput = string.Empty;
    private int newsYearInput = 1;
    private int newsSemesterInput = 1;
    private int newsRoundInput = 1;
    private string newsEditorStatus = string.Empty;
    private readonly List<NewsItem> editingNewsItems = new List<NewsItem>();
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
    private string selectedHomepageLayoutKey = ZhongshanDeckTitleContentDefaults.LayoutLogo;
    private string selectedSaveLoadLayoutKey = ZhongshanDeckSaveLoadContentDefaults.LayoutBoard;
    private bool isHomepageLayoutDragging;
    private bool isHomepageLayoutResizing;
    private Vector2 homepageLayoutDragStartMouse;
    private Vector2 homepageLayoutDragStartPosition;
    private Vector2 homepageLayoutDragStartSize;
    private bool isSaveLoadLayoutDragging;
    private bool isSaveLoadLayoutResizing;
    private Vector2 saveLoadLayoutDragStartMouse;
    private Vector2 saveLoadLayoutDragStartPosition;
    private Vector2 saveLoadLayoutDragStartSize;
    private string endingSearchInput = string.Empty;
    private string endingEditorStatus = string.Empty;
    private string editingEndingId = string.Empty;
    private string endingEditName = string.Empty;
    private string endingEditDescription = string.Empty;
    private string endingEditCgId = string.Empty;
    private List<EndingDefinition> endingEditorCache = new List<EndingDefinition>();
    private readonly List<EndingCondition> endingConditionDrafts = new List<EndingCondition>();
    private readonly Dictionary<string, bool> endingCategoryFoldouts = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
    {
        { "特殊/强制", true },
        { "巅峰", true },
        { "学术", true },
        { "仕途", true },
        { "创业", true },
        { "职场", true },
        { "留学", true },
        { "文体/特长", true },
        { "新兴职业/自由", true },
        { "黑暗", true },
        { "保底", true }
    };
    private readonly Dictionary<int, bool> endingStarFoldouts = new Dictionary<int, bool>
    {
        { 7, true },
        { 6, true },
        { 5, true },
        { 4, true },
        { 3, true },
        { 2, true },
        { 1, true },
        { 0, true }
    };
    private static readonly string[] EventTypeOptions = { "Fixed", "MainStory", "Conditional", "Dark" };
    private static readonly string[] EventTypeDisplayOptions = { "固定事件", "主线事件", "条件事件", "黑暗事件" };
    private static readonly string[] EventPhaseOptions = { "RoundStart", "ActionComplete", "RoundEnd" };
    private static readonly string[] EventPhaseDisplayOptions = { "回合开始", "行动完成后", "回合结束" };
    private static readonly string[] EventTimelinePhaseOptions = { "All", "RoundStart", "ActionComplete", "RoundEnd" };
    private static readonly string[] EventTimelinePhaseDisplayOptions = { "全部阶段", "回合开始", "行动完成后", "回合结束" };
    private static readonly string[] EventAttributeOptions = { "学力", "魅力", "体魄", "领导力", "压力", "心情", "黑暗值", "负罪感", "幸运" };
    private static readonly string[] EventComparisonOptions = { ">=", "<=", "==", "!=", ">", "<" };
    private static readonly string[] EventEffectTypeOptions = { "attribute", "money", "flag", "darkness", "unlock" };
    private static readonly string[] EventEffectTypeDisplayOptions = { "属性变化", "金钱变化", "标记开关", "黑暗值变化", "解锁内容" };
    private static readonly string[] EndingConditionTypeOptions = Enum.GetNames(typeof(EndingConditionType));
    private static readonly string[] EndingConditionTypeDisplayOptions =
    {
        "GPA >= 阈值",
        "GPA < 阈值",
        "学力 >= 阈值",
        "魅力 >= 阈值",
        "体魄 >= 阈值",
        "领导力 >= 阈值",
        "压力 >= 阈值",
        "学力 < 阈值",
        "心情 == 阈值",
        "心情 < 阈值",
        "金钱 < 阈值",
        "金钱 >= 阈值",
        "负罪感 <= 阈值",
        "黑暗值 >= 阈值",
        "有恋人",
        "无恋人",
        "恋爱等级 >= 阈值",
        "好友数 >= 阈值",
        "学生会主席",
        "正式党员",
        "玩家性别 == 阈值",
        "获得国奖",
        "作弊次数 >= 阈值",
        "摆烂值 >= 阈值",
        "心理健康 == 阈值",
        "通过四级",
        "通过六级",
        "总学习次数 >= 阈值",
        "总社交次数 >= 阈值",
        "毕业总评分 >= 阈值",
        "实习次数 >= 阈值",
        "始终满足"
    };

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

    public static bool OpenTitleScenePreview()
    {
        if (!OpenEditorScene(TitleScenePath))
        {
            return false;
        }

        ZhongshanDeckSaveLoadScenePreview.SetPreviewVisible(false);
        ZhongshanDeckTitleScenePreview.SetPreviewVisible(true);
        SceneView.RepaintAll();
        return true;
    }

    public static bool OpenSaveLoadScenePreview()
    {
        if (!OpenEditorScene(SaveLoadPreviewScenePath))
        {
            return false;
        }

        ZhongshanDeckTitleScenePreview.SetPreviewVisible(false);
        ZhongshanDeckSaveLoadScenePreview.SetPreviewVisible(true);
        SceneView.RepaintAll();
        return true;
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
            case Tab.SceneJump: DrawSceneJump(); break;
            case Tab.Attributes: DrawAttributes(); break;
            case Tab.Time: DrawTime(); break;
            case Tab.Content: DrawContent(); break;
            case Tab.News: DrawNews(); break;
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
        int selected = GUILayout.Toolbar((int)currentTab, TabLabels, EditorStyles.toolbarButton, GUILayout.Width(700f));
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
        if (GUILayout.Button("内容", GUILayout.Width(70f))) currentTab = Tab.Content;
        if (GUILayout.Button("新闻", GUILayout.Width(70f))) currentTab = Tab.News;
        if (GUILayout.Button("事件", GUILayout.Width(70f))) currentTab = Tab.Events;
        if (GUILayout.Button("NPC", GUILayout.Width(70f))) currentTab = Tab.NPC;
        if (GUILayout.Button("快照", GUILayout.Width(70f))) currentTab = Tab.Snapshots;
        EditorGUILayout.EndHorizontal();

    }

    private void DrawSceneJump()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("场景跳转", EditorStyles.boldLabel);
        SceneAsset currentPlayStartScene = EditorSceneManager.playModeStartScene;
        string playStartSceneLabel = currentPlayStartScene != null ? AssetDatabase.GetAssetPath(currentPlayStartScene) : "未设置";
        EditorGUILayout.HelpBox($"这里是 Unity 编辑器里的钟山台场景跳转，不是局内调试面板。当前 Play 起始场景：{playStartSceneLabel}", MessageType.None);
        EditorGUILayout.HelpBox("标题页文字、按钮和排版不是预制在场景里的，而是由 TitleScreenManager 在运行时动态创建。要做标题页可视化排版，请切到“内容”页里的“首页布局可视化编辑”。", MessageType.Info);
        EditorGUILayout.HelpBox("存档界面现在使用独立的 SaveLoadPreview 场景做真实预览。主菜单和存档已经拆开，避免两个编辑层继续叠在同一个场景里。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("打开标题布局编辑", GUILayout.Width(160f)))
            {
                currentTab = Tab.Content;
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("打开标题页场景", GUILayout.Width(140f)) && HasSceneAsset(TitleScenePath))
            {
                OpenSceneByPath(TitleScenePath);
                ZhongshanDeckSaveLoadScenePreview.SetPreviewVisible(false);
                ZhongshanDeckTitleScenePreview.SetPreviewVisible(true);
            }

            if (GUILayout.Button("打开存档界面预览", GUILayout.Width(160f)) && HasSceneAsset(SaveLoadPreviewScenePath))
            {
                OpenSceneByPath(SaveLoadPreviewScenePath);
                ZhongshanDeckSaveLoadScenePreview.SetPreviewVisible(true);
                SceneView.RepaintAll();
            }

            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("项目场景", EditorStyles.boldLabel);
        DrawSceneButtonGrid(GetAllScenePaths());

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("快速入口", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(GetSceneDisplayNameByPath(SplashScenePath), GUILayout.Width(140f)) && HasSceneAsset(SplashScenePath))
            {
                OpenSceneByPath(SplashScenePath);
            }

            if (GUILayout.Button(GetSceneDisplayNameByPath(LoadingScenePath), GUILayout.Width(140f)) && HasSceneAsset(LoadingScenePath))
            {
                OpenSceneByPath(LoadingScenePath);
            }

            if (GUILayout.Button(GetSceneDisplayNameByPath(TitleScenePath), GUILayout.Width(140f)) && HasSceneAsset(TitleScenePath))
            {
                OpenSceneByPath(TitleScenePath);
            }

            if (GUILayout.Button(GetSceneDisplayNameByPath(GameScenePath), GUILayout.Width(140f)) && HasSceneAsset(GameScenePath))
            {
                OpenSceneByPath(GameScenePath);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button($"设 {GetSceneDisplayNameByPath(SplashScenePath)} 为 Play 起始", GUILayout.Width(180f)) && HasSceneAsset(SplashScenePath))
            {
                SetPlayModeStartScene(SplashScenePath);
            }

            if (GUILayout.Button($"设 {GetSceneDisplayNameByPath(TitleScenePath)} 为 Play 起始", GUILayout.Width(180f)) && HasSceneAsset(TitleScenePath))
            {
                SetPlayModeStartScene(TitleScenePath);
            }

            if (GUILayout.Button($"设 {GetSceneDisplayNameByPath(GameScenePath)} 为 Play 起始", GUILayout.Width(180f)) && HasSceneAsset(GameScenePath))
            {
                SetPlayModeStartScene(GameScenePath);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button($"打开并设为 {GetSceneDisplayNameByPath(TitleScenePath)} 起始", GUILayout.Width(180f)) && HasSceneAsset(TitleScenePath))
            {
                OpenSceneAndSetPlayStart(TitleScenePath);
            }

            if (GUILayout.Button($"打开并设为 {GetSceneDisplayNameByPath(GameScenePath)} 起始", GUILayout.Width(180f)) && HasSceneAsset(GameScenePath))
            {
                OpenSceneAndSetPlayStart(GameScenePath);
            }
        }

        EditorGUILayout.Space(8f);
        DrawLocationPreviewTools();
    }

    private void DrawAttributes()
    {
        EditorGUILayout.LabelField("属性调试", EditorStyles.boldLabel);
        DrawStartupAttributeSettings();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("运行中属性", EditorStyles.boldLabel);
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
            EditorGUILayout.HelpBox("上面是新游戏默认属性；下面的运行中属性修改需要在 Play 模式中执行。步进设置已同步到工具状态资产。", MessageType.None);
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

    private bool OpenSceneByPath(string scenePath)
    {
        return OpenEditorScene(scenePath);
    }

    private static bool OpenEditorScene(string scenePath)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            EditorUtility.DisplayDialog("钟山台", $"找不到场景：{scenePath}", "确定");
            return false;
        }

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        return true;
    }

    private bool OpenSceneAndSetPlayStart(string scenePath)
    {
        if (!OpenSceneByPath(scenePath))
        {
            return false;
        }

        SetPlayModeStartScene(scenePath);
        return true;
    }

    private void SetPlayModeStartScene(string scenePath)
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset == null)
        {
            EditorUtility.DisplayDialog("钟山台", $"找不到场景：{scenePath}", "确定");
            return;
        }

        EditorSceneManager.playModeStartScene = sceneAsset;
        Repaint();
    }

    private void OpenDormitoryPreview(int previewGender)
    {
        OpenLocationPreview(LocationId.Dormitory, previewGender);
    }

    private void DrawLocationPreviewTools()
    {
        EditorGUILayout.LabelField("地点可视化编辑", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("以下入口会打开 GameScene，把 LocationSceneController 切到对应地点预览，并同步 NPC 锚点。打开后可直接在 Scene 视图里拖地面、边界、障碍和 NPC 站位。", MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("男生宿舍", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.Dormitory, 0);
            }

            if (GUILayout.Button("女生宿舍", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.Dormitory, 1);
            }

            if (GUILayout.Button("教学楼", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.TeachingBuilding);
            }

            if (GUILayout.Button("图书馆", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.Library);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("食堂", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.Canteen);
            }

            if (GUILayout.Button("操场", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.Playground);
            }

            if (GUILayout.Button("教超", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.Store);
            }

            if (GUILayout.Button("快递站", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.ExpressStation);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("外卖站", GUILayout.Width(140f)))
            {
                OpenLocationPreview(LocationId.TakeoutStation);
            }

            if (GUILayout.Button("补全全部地点 Profile", GUILayout.Width(180f)))
            {
                EnsureAllLocationProfiles();
            }

            GUILayout.FlexibleSpace();
        }
    }

    private void EnsureAllLocationProfiles()
    {
        if (!OpenSceneByPath(GameScenePath))
        {
            return;
        }

        LocationSceneController controller = GetOrCreateLocationSceneController();
        Undo.RecordObject(controller, "Add Missing Location Profiles");
        controller.AddMissingProfiles();
        controller.RebuildScene();
        EditorUtility.SetDirty(controller);
        Selection.activeObject = controller.gameObject;
        EditorGUIUtility.PingObject(controller.gameObject);
    }

    private void OpenLocationPreview(LocationId locationId, int? previewGender = null)
    {
        if (!OpenSceneByPath(GameScenePath))
        {
            return;
        }

        if (previewGender.HasValue)
        {
            int gender = Mathf.Clamp(previewGender.Value, 0, 1);
            StartupFlowSettings.SetEditorPreviewPlayerGenderOverride(gender);
            StartupFlowSettings.DefaultPlayerGender = gender;
        }

        LocationSceneController controller = GetOrCreateLocationSceneController();
        Undo.RecordObject(controller, $"Open {locationId} Preview");
        controller.EnsureProfile(locationId);
        controller.SetPreviewLocation(locationId);
        if (previewGender.HasValue)
        {
            controller.SetPreviewPlayerGenderInEditMode(previewGender.Value);
        }
        controller.RebuildScene();
        EditorUtility.SetDirty(controller);

        NPCManager npcManager = UnityEngine.Object.FindFirstObjectByType<NPCManager>();
        if (npcManager != null)
        {
            npcManager.RefreshEditorSceneAnchors();
            EditorUtility.SetDirty(npcManager);
        }

        PlayerController playerController = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Undo.RecordObject(playerController, "Apply Preview Character");
            playerController.SendMessage("ApplyEditorPreviewSprite", SendMessageOptions.DontRequireReceiver);
            EditorUtility.SetDirty(playerController);
            Selection.activeObject = playerController.gameObject;
            EditorGUIUtility.PingObject(playerController.gameObject);
        }
        else
        {
            Selection.activeObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
        }

        SceneView.lastActiveSceneView?.FrameSelected();
        SceneView.RepaintAll();
    }

    private static LocationSceneController GetOrCreateLocationSceneController()
    {
        LocationSceneController controller = UnityEngine.Object.FindFirstObjectByType<LocationSceneController>();
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new GameObject("LocationSceneController");
        controller = controllerObject.AddComponent<LocationSceneController>();
        controller.EnsureDefaultProfiles();
        return controller;
    }

    private void DrawTime()
    {
        EditorGUILayout.LabelField("时间调试", EditorStyles.boldLabel);

        DrawStartupTimeSettings();

        EditorGUILayout.Space(8f);

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

        int totalRounds = GetTotalRounds(
            GameState.Instance.CurrentYear,
            GameState.Instance.CurrentSemester,
            GameState.Instance.CurrentRound);
        EditorGUILayout.HelpBox($"当前：{GameState.Instance.GetTimeDescription()}", MessageType.None);
        EditorGUILayout.HelpBox($"当前总回合数：{totalRounds}", MessageType.None);
    }

    private void DrawStartupTimeSettings()
    {
        EditorGUILayout.LabelField("新游戏起始时间", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里配置的是进游戏前的新档起始时间。只影响新游戏，不影响读档。", MessageType.None);

        bool useOverride = StartupFlowSettings.UseStartupTimeOverride;
        bool newUseOverride = EditorGUILayout.Toggle("启用启动前时间覆盖", useOverride);
        if (newUseOverride != useOverride)
        {
            StartupFlowSettings.UseStartupTimeOverride = newUseOverride;
        }

        int semesterRoundCount = EditorGUILayout.IntField("每学期回合数", StartupFlowSettings.SemesterRoundCount);
        semesterRoundCount = Mathf.Clamp(semesterRoundCount, 3, 12);
        if (semesterRoundCount != StartupFlowSettings.SemesterRoundCount)
        {
            StartupFlowSettings.SemesterRoundCount = semesterRoundCount;
        }

        int startupYear = EditorGUILayout.IntField("起始学年", StartupFlowSettings.StartupYear);
        int startupSemester = EditorGUILayout.IntField("起始学期", StartupFlowSettings.StartupSemester);
        int startupRound = EditorGUILayout.IntField("起始回合", StartupFlowSettings.StartupRound);

        startupYear = Mathf.Clamp(startupYear, 1, 4);
        startupSemester = Mathf.Clamp(startupSemester, 1, 2);
        startupRound = Mathf.Clamp(startupRound, 1, StartupFlowSettings.SemesterRoundCount);

        if (startupYear != StartupFlowSettings.StartupYear)
        {
            StartupFlowSettings.StartupYear = startupYear;
        }

        if (startupSemester != StartupFlowSettings.StartupSemester)
        {
            StartupFlowSettings.StartupSemester = startupSemester;
        }

        if (startupRound != StartupFlowSettings.StartupRound)
        {
            StartupFlowSettings.StartupRound = startupRound;
        }

        int startupMonth = GameState.CalculateMonth(StartupFlowSettings.StartupSemester, StartupFlowSettings.StartupRound);
        string semesterLabel = StartupFlowSettings.StartupSemester == 1 ? "上" : "下";
        string startupSummary = StartupFlowSettings.UseStartupTimeOverride
            ? $"当前启动配置：每学期{StartupFlowSettings.SemesterRoundCount}回合 | 大{StartupFlowSettings.StartupYear}{semesterLabel} · 回合{StartupFlowSettings.StartupRound} · {startupMonth}月"
            : "当前启动配置：使用默认开局时间";
        EditorGUILayout.HelpBox(startupSummary, MessageType.None);
        EditorGUILayout.HelpBox("考试回合当前先写死处理：期中第3回合，证书考试第4回合，期末为学期最后一回合。", MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("用当前运行时间写入", GUILayout.Width(160f)))
        {
            if (EditorApplication.isPlaying && GameState.Instance != null)
            {
                StartupFlowSettings.StartupYear = GameState.Instance.CurrentYear;
                StartupFlowSettings.StartupSemester = GameState.Instance.CurrentSemester;
                StartupFlowSettings.StartupRound = GameState.Instance.CurrentRound;
                StartupFlowSettings.UseStartupTimeOverride = true;
                ZhongshanDeckToolStateBridge.SaveState();
            }
            else
            {
                EditorUtility.DisplayDialog("钟山台", "当前不在 Play 模式，无法读取运行中的时间。", "确定");
            }
        }

        if (GUILayout.Button("清除启动前覆盖", GUILayout.Width(140f)))
        {
            StartupFlowSettings.UseStartupTimeOverride = false;
            ZhongshanDeckToolStateBridge.SaveState();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStartupAttributeSettings()
    {
        EditorGUILayout.HelpBox("这里配置新游戏默认属性。会写入钟山台状态资产，对新开档生效，不影响读档。", MessageType.None);

        foreach (var item in AttributeDefs)
        {
            int currentValue = GetStartupAttributeValue(item.Key);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.Label, GUILayout.Width(80f));
            int nextValue = EditorGUILayout.IntField(currentValue, GUILayout.Width(90f));
            nextValue = Mathf.Clamp(nextValue, item.Min, item.Max);
            if (nextValue != currentValue)
            {
                SetStartupAttributeValue(item.Key, nextValue);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("当前运行值写入开局", GUILayout.Width(140f)))
        {
            if (EditorApplication.isPlaying)
            {
                CaptureRuntimeAttributesAsStartup();
            }
            else
            {
                EditorUtility.DisplayDialog("钟山台", "当前不在 Play 模式，无法读取运行中的属性。", "确定");
            }
        }

        if (GUILayout.Button("重置为默认开局", GUILayout.Width(140f)))
        {
            ResetStartupAttributesToDefaults();
        }
        EditorGUILayout.EndHorizontal();
    }

    private int GetStartupAttributeValue(string key)
    {
        switch (key)
        {
            case "Study": return StartupFlowSettings.InitialStudy;
            case "Charm": return StartupFlowSettings.InitialCharm;
            case "Physique": return StartupFlowSettings.InitialPhysique;
            case "Leadership": return StartupFlowSettings.InitialLeadership;
            case "Stress": return StartupFlowSettings.InitialStress;
            case "Mood": return StartupFlowSettings.InitialMood;
            case "Darkness": return StartupFlowSettings.InitialDarkness;
            case "Guilt": return StartupFlowSettings.InitialGuilt;
            case "Luck": return StartupFlowSettings.InitialLuck;
            case "Money": return StartupFlowSettings.InitialMoney;
            case "ActionPoints": return StartupFlowSettings.InitialActionPoints;
            default: return 0;
        }
    }

    private void SetStartupAttributeValue(string key, int value)
    {
        switch (key)
        {
            case "Study": StartupFlowSettings.InitialStudy = value; break;
            case "Charm": StartupFlowSettings.InitialCharm = value; break;
            case "Physique": StartupFlowSettings.InitialPhysique = value; break;
            case "Leadership": StartupFlowSettings.InitialLeadership = value; break;
            case "Stress": StartupFlowSettings.InitialStress = value; break;
            case "Mood": StartupFlowSettings.InitialMood = value; break;
            case "Darkness": StartupFlowSettings.InitialDarkness = value; break;
            case "Guilt": StartupFlowSettings.InitialGuilt = value; break;
            case "Luck": StartupFlowSettings.InitialLuck = value; break;
            case "Money": StartupFlowSettings.InitialMoney = value; break;
            case "ActionPoints": StartupFlowSettings.InitialActionPoints = value; break;
        }
    }

    private void CaptureRuntimeAttributesAsStartup()
    {
        if (PlayerAttributes.Instance != null)
        {
            StartupFlowSettings.InitialStudy = PlayerAttributes.Instance.Study;
            StartupFlowSettings.InitialCharm = PlayerAttributes.Instance.Charm;
            StartupFlowSettings.InitialPhysique = PlayerAttributes.Instance.Physique;
            StartupFlowSettings.InitialLeadership = PlayerAttributes.Instance.Leadership;
            StartupFlowSettings.InitialStress = PlayerAttributes.Instance.Stress;
            StartupFlowSettings.InitialMood = PlayerAttributes.Instance.Mood;
            StartupFlowSettings.InitialDarkness = PlayerAttributes.Instance.Darkness;
            StartupFlowSettings.InitialGuilt = PlayerAttributes.Instance.Guilt;
            StartupFlowSettings.InitialLuck = PlayerAttributes.Instance.Luck;
        }

        if (GameState.Instance != null)
        {
            StartupFlowSettings.InitialMoney = GameState.Instance.Money;
            StartupFlowSettings.InitialActionPoints = GameState.Instance.EffectiveMaxActionPoints + GameState.Instance.PositionAPCost;
        }
    }

    private void ResetStartupAttributesToDefaults()
    {
        StartupFlowSettings.InitialStudy = 10;
        StartupFlowSettings.InitialCharm = 5;
        StartupFlowSettings.InitialPhysique = 8;
        StartupFlowSettings.InitialLeadership = 3;
        StartupFlowSettings.InitialStress = 20;
        StartupFlowSettings.InitialMood = 70;
        StartupFlowSettings.InitialDarkness = 0;
        StartupFlowSettings.InitialGuilt = 0;
        StartupFlowSettings.InitialLuck = 50;
        StartupFlowSettings.InitialMoney = 8000;
        StartupFlowSettings.InitialActionPoints = 20;
    }

    private void DrawEndings()
    {
        EditorGUILayout.LabelField("结局总览 / 直接触发", EditorStyles.boldLabel);
        EnsureEndingEditorDataLoaded();

        if (EditorApplication.isPlaying && EndingDeterminer.Instance != null)
        {
            EndingResult result = EndingDeterminer.Instance.DetermineEndingPreview();
            if (result != null && result.ending != null)
            {
                EditorGUILayout.LabelField("当前结局", result.ending.name);
                EditorGUILayout.LabelField("层级", $"{result.ending.layer}");
                EditorGUILayout.LabelField("星级", $"{result.ending.stars}");
                EditorGUILayout.LabelField("结算分", result.finalScore.ToString("F1"));
                EditorGUILayout.LabelField("天赋点", result.talentPoints.ToString());
                EditorGUILayout.Space(6f);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("当前不在 Play 模式。你现在仍然可以在这里直接编辑结局文字、剧情描述和触发条件，并保存到 endings.json。", MessageType.Info);
        }

        if (endingEditorCache == null || endingEditorCache.Count == 0)
        {
            EditorGUILayout.HelpBox("未能读取任何结局数据。", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("搜索", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            endingSearchInput = EditorGUILayout.TextField(endingSearchInput);
            if (GUILayout.Button("清空", GUILayout.Width(60f)))
            {
                endingSearchInput = string.Empty;
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("刷新", GUILayout.Width(60f)))
            {
                LoadEndingEditorData(true);
            }
        }

        EditorGUILayout.Space(6f);
        DrawEndingEditorPanel();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("分类浏览", EditorStyles.boldLabel);

        List<EndingDefinition> endings = endingEditorCache
            .OrderBy(e => e.layer)
            .ThenByDescending(e => e.stars)
            .ThenBy(e => e.id)
            .ToList();
        string keyword = (endingSearchInput ?? string.Empty).Trim();
        int visibleCount = 0;
        var groupedEndings = new Dictionary<string, List<EndingDefinition>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < endings.Count; i++)
        {
            EndingDefinition ending = endings[i];
            if (!MatchesEndingKeyword(ending, keyword))
            {
                continue;
            }

            visibleCount++;
            string category = GetEndingDesignCategory(ending);
            if (!groupedEndings.TryGetValue(category, out List<EndingDefinition> layerList))
            {
                layerList = new List<EndingDefinition>();
                groupedEndings[category] = layerList;
            }
            layerList.Add(ending);
        }

        DrawEndingCategoryToolbar();

        for (int categoryIndex = 0; categoryIndex < EndingCategoryOrder.Length; categoryIndex++)
        {
            string category = EndingCategoryOrder[categoryIndex];
            if (!groupedEndings.TryGetValue(category, out List<EndingDefinition> layerEndings) || layerEndings.Count == 0)
            {
                continue;
            }

            endingCategoryFoldouts.TryGetValue(category, out bool isExpanded);
            string categoryLabel = $"{category}  ({layerEndings.Count})";
            isExpanded = EditorGUILayout.Foldout(isExpanded, categoryLabel, true, EditorStyles.foldoutHeader);
            endingCategoryFoldouts[category] = isExpanded;

            if (!isExpanded)
            {
                EditorGUILayout.Space(2f);
                continue;
            }

            EditorGUI.indentLevel++;
            DrawEndingStars(layerEndings);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4f);
        }

        if (visibleCount == 0)
        {
            EditorGUILayout.HelpBox("没有匹配的结局。", MessageType.None);
        }
    }

    private void DrawEndingStars(List<EndingDefinition> categoryEndings)
    {
        Dictionary<int, List<EndingDefinition>> endingsByStar = GroupEndingsByStar(categoryEndings);

        for (int starIndex = 0; starIndex < EndingStarOrder.Length; starIndex++)
        {
            int star = EndingStarOrder[starIndex];
            if (!endingsByStar.TryGetValue(star, out List<EndingDefinition> starEndings) || starEndings.Count == 0)
            {
                continue;
            }

            endingStarFoldouts.TryGetValue(star, out bool isExpanded);
            string starLabel = $"{BuildStarLabel(star)}  ({starEndings.Count})";
            isExpanded = EditorGUILayout.Foldout(isExpanded, starLabel, true);
            endingStarFoldouts[star] = isExpanded;

            if (!isExpanded)
            {
                continue;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < starEndings.Count; i++)
            {
                DrawEndingEntry(starEndings[i]);
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawEndingCategoryToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("分类", GUILayout.Width(36f));
            if (GUILayout.Button("全部展开", GUILayout.Width(90f)))
            {
                SetAllEndingCategoryFoldouts(true);
            }
            if (GUILayout.Button("全部收起", GUILayout.Width(90f)))
            {
                SetAllEndingCategoryFoldouts(false);
            }
            GUILayout.Space(8f);
            EditorGUILayout.LabelField("星级", GUILayout.Width(36f));
            if (GUILayout.Button("全部展开", GUILayout.Width(90f)))
            {
                SetAllEndingStarFoldouts(true);
            }
            if (GUILayout.Button("全部收起", GUILayout.Width(90f)))
            {
                SetAllEndingStarFoldouts(false);
            }
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.Space(4f);
    }

    private void DrawEndingEditorPanel()
    {
        EditorGUILayout.LabelField("结局编辑器", EditorStyles.boldLabel);

        if (string.IsNullOrWhiteSpace(editingEndingId))
        {
            EditorGUILayout.HelpBox("从下方列表点“编辑”后，可手动修改可见文字、剧情描述和触发条件。", MessageType.None);
            return;
        }

        EndingDefinition current = FindEndingInCache(editingEndingId);
        if (current == null)
        {
            EditorGUILayout.HelpBox($"未找到正在编辑的结局：{editingEndingId}", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"{current.id} | 层级 {current.layer} | {current.stars} 星", EditorStyles.boldLabel);
        endingEditName = EditorGUILayout.TextField("可见标题", endingEditName ?? string.Empty);
        endingEditCgId = EditorGUILayout.TextField("CG ID", endingEditCgId ?? string.Empty);

        EditorGUILayout.LabelField("剧情 / 描述");
        endingEditDescription = EditorGUILayout.TextArea(endingEditDescription ?? string.Empty, GUILayout.MinHeight(80f));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("触发条件", EditorStyles.miniBoldLabel);
        for (int i = 0; i < endingConditionDrafts.Count; i++)
        {
            EndingCondition condition = endingConditionDrafts[i] ?? new EndingCondition();
            EditorGUILayout.BeginHorizontal();

            int selectedIndex = Mathf.Max(0, Array.IndexOf(EndingConditionTypeOptions, condition.type ?? string.Empty));
            selectedIndex = EditorGUILayout.Popup(selectedIndex, EndingConditionTypeDisplayOptions, GUILayout.Width(240f));
            condition.type = EndingConditionTypeOptions[Mathf.Clamp(selectedIndex, 0, EndingConditionTypeOptions.Length - 1)];

            string valueText = EditorGUILayout.TextField(condition.value.ToString("0.##"), GUILayout.Width(100f));
            if (float.TryParse(valueText, out float parsedValue))
            {
                condition.value = parsedValue;
            }

            if (GUILayout.Button("删除", GUILayout.Width(60f)))
            {
                endingConditionDrafts.RemoveAt(i);
                i--;
            }
            else
            {
                endingConditionDrafts[i] = condition;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("新增条件", GUILayout.Width(100f)))
        {
            endingConditionDrafts.Add(new EndingCondition(
                EndingConditionTypeOptions.Length > 0 ? EndingConditionTypeOptions[0] : EndingConditionType.AlwaysTrue.ToString(),
                0f));
        }

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存到 endings.json", GUILayout.Width(150f)))
            {
                SaveEditingEndingToAsset();
            }

            if (GUILayout.Button("重载此结局", GUILayout.Width(100f)))
            {
                BeginEditEnding(current);
                endingEditorStatus = $"已重载 {current.id} 的当前文件内容。";
            }

            if (GUILayout.Button("放弃编辑", GUILayout.Width(100f)))
            {
                ClearEndingEditorSelection();
                endingEditorStatus = "已取消当前结局编辑。";
            }
        }

        if (!string.IsNullOrWhiteSpace(endingEditorStatus))
        {
            EditorGUILayout.HelpBox(endingEditorStatus, MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    private void SetAllEndingCategoryFoldouts(bool expanded)
    {
        for (int i = 0; i < EndingCategoryOrder.Length; i++)
        {
            endingCategoryFoldouts[EndingCategoryOrder[i]] = expanded;
        }
    }

    private void SetAllEndingStarFoldouts(bool expanded)
    {
        for (int i = 0; i < EndingStarOrder.Length; i++)
        {
            endingStarFoldouts[EndingStarOrder[i]] = expanded;
        }
    }

    private void DrawEndingEntry(EndingDefinition ending)
    {
        bool matched = IsEndingMatched(ending);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        if (matched)
        {
            titleStyle.normal.textColor = new Color(0.95f, 0.78f, 0.24f);
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"{ending.id} | {ending.name}", titleStyle);
        EditorGUILayout.LabelField($"层级 {ending.layer} | {GetEndingLayerText(ending.GetLayer())} | {ending.stars} 星");

        if (!string.IsNullOrEmpty(ending.description))
        {
            EditorGUILayout.LabelField(ending.description, EditorStyles.wordWrappedLabel);
        }

        if (ending.conditions != null && ending.conditions.Count > 0)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("条件", EditorStyles.miniBoldLabel);
            for (int conditionIndex = 0; conditionIndex < ending.conditions.Count; conditionIndex++)
            {
                EndingCondition condition = ending.conditions[conditionIndex];
                if (EditorApplication.isPlaying && EndingDeterminer.Instance != null)
                {
                    bool conditionMatched = EndingDeterminer.Instance.EvaluateCondition(condition);
                    string prefix = conditionMatched ? "[满足]" : "[未满足]";
                    EditorGUILayout.LabelField($"- {prefix} {EndingDeterminer.Instance.DescribeCondition(condition)}", EditorStyles.miniLabel);
                }
                else
                {
                    string conditionType = GetEndingConditionTypeDisplayName(condition != null ? condition.type : EndingConditionType.AlwaysTrue.ToString());
                    float conditionValue = condition != null ? condition.value : 0f;
                    EditorGUILayout.LabelField($"- {conditionType} | 数值 {conditionValue:0.##}", EditorStyles.miniLabel);
                }
            }
        }

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("编辑", GUILayout.Width(80f)))
            {
                BeginEditEnding(ending);
            }

            GUI.enabled = EditorApplication.isPlaying && (GameEndingManager.Instance != null || EndingDeterminer.Instance != null);
            if (GUILayout.Button("进入结局", GUILayout.Width(120f)))
            {
                if (GameEndingManager.Instance == null)
                {
                    GameObject managerObject = new GameObject("GameEndingManager");
                    managerObject.AddComponent<GameEndingManager>();
                }

                bool success = GameEndingManager.Instance != null &&
                               GameEndingManager.Instance.TriggerSpecificEnding(ending.id, $"ZhongshanDeck:{ending.id}");

                if (success)
                {
                    DebugConsoleManager.Log("Ending", $"Editor window triggered ending: {ending.id} {ending.name}");
                }
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            string statusText = EditorApplication.isPlaying
                ? (matched ? "当前条件已满足" : "当前条件未满足")
                : "编辑器模式";
            EditorGUILayout.LabelField(statusText, GUILayout.Width(120f));
        }
        EditorGUILayout.EndVertical();
    }

    private bool MatchesEndingKeyword(EndingDefinition ending, string keyword)
    {
        if (ending == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        return (!string.IsNullOrEmpty(ending.id) && ending.id.IndexOf(keyword, comparison) >= 0)
               || (!string.IsNullOrEmpty(ending.name) && ending.name.IndexOf(keyword, comparison) >= 0)
               || (!string.IsNullOrEmpty(ending.description) && ending.description.IndexOf(keyword, comparison) >= 0)
               || GetEndingDesignCategory(ending).IndexOf(keyword, comparison) >= 0
               || ending.layer.ToString().IndexOf(keyword, comparison) >= 0
               || ending.stars.ToString().IndexOf(keyword, comparison) >= 0
               || GetEndingLayerText(ending.GetLayer()).IndexOf(keyword, comparison) >= 0
               || BuildStarLabel(ending.stars).IndexOf(keyword, comparison) >= 0;
    }

    private bool IsEndingMatched(EndingDefinition ending)
    {
        if (ending == null || EndingDeterminer.Instance == null || !EditorApplication.isPlaying)
        {
            return false;
        }

        if (ending.conditions == null || ending.conditions.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < ending.conditions.Count; i++)
        {
            if (!EndingDeterminer.Instance.EvaluateCondition(ending.conditions[i]))
            {
                return false;
            }
        }

        return true;
    }

    private string GetEndingConditionTypeDisplayName(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return "未知条件";
        }

        int index = Array.IndexOf(EndingConditionTypeOptions, type);
        if (index >= 0 && index < EndingConditionTypeDisplayOptions.Length)
        {
            return EndingConditionTypeDisplayOptions[index];
        }

        return type;
    }

    private string GetEndingLayerText(EndingLayer layer)
    {
        switch (layer)
        {
            case EndingLayer.ForcedEnding: return "强制";
            case EndingLayer.PeakEnding: return "巅峰";
            case EndingLayer.PlannedPath: return "规划内";
            case EndingLayer.UnplannedPath: return "规划外";
            case EndingLayer.DarkEnding: return "黑暗";
            case EndingLayer.SpecialEnding: return "特殊";
            case EndingLayer.NewCareer: return "新职业";
            case EndingLayer.FallbackEnding: return "兜底";
            default: return layer.ToString();
        }
    }

    private string GetEndingDesignCategory(EndingDefinition ending)
    {
        if (ending == null || string.IsNullOrWhiteSpace(ending.id))
        {
            return "保底";
        }

        switch (ending.id)
        {
            case "END_001":
            case "END_002":
            case "END_003":
            case "END_004":
                return "特殊/强制";

            case "END_005":
            case "END_006":
                return "巅峰";

            case "END_007":
            case "END_008":
            case "END_009":
            case "END_025":
            case "END_026":
                return "学术";

            case "END_010":
            case "END_011":
            case "END_014":
            case "END_032":
                return "仕途";

            case "END_012":
            case "END_020":
            case "END_021":
            case "END_027":
            case "END_028":
            case "END_033":
                return "创业";

            case "END_013":
            case "END_029":
            case "END_030":
            case "END_031":
                return "职场";

            case "END_018":
                return "文体/特长";

            case "END_015":
                return "新兴职业/自由";

            case "END_016":
            case "END_017":
                return "黑暗";

            case "END_019":
            case "END_022":
            case "END_023":
            case "END_024":
                return "保底";
        }

        if (ending.GetLayer() == EndingLayer.ForcedEnding)
            return "特殊/强制";
        if (ending.GetLayer() == EndingLayer.PeakEnding)
            return "巅峰";
        if (ending.GetLayer() == EndingLayer.DarkEnding)
            return "黑暗";
        if (ending.GetLayer() == EndingLayer.FallbackEnding)
            return "保底";
        if (ending.GetLayer() == EndingLayer.SpecialEnding)
            return "文体/特长";
        if (ending.GetLayer() == EndingLayer.NewCareer)
            return "新兴职业/自由";

        return "保底";
    }

    private Dictionary<int, List<EndingDefinition>> GroupEndingsByStar(List<EndingDefinition> endings)
    {
        Dictionary<int, List<EndingDefinition>> grouped = new Dictionary<int, List<EndingDefinition>>();
        if (endings == null)
        {
            return grouped;
        }

        for (int i = 0; i < endings.Count; i++)
        {
            EndingDefinition ending = endings[i];
            int star = Mathf.Clamp(ending != null ? ending.stars : 0, 0, 7);
            if (!grouped.TryGetValue(star, out List<EndingDefinition> starList))
            {
                starList = new List<EndingDefinition>();
                grouped[star] = starList;
            }

            if (ending != null)
            {
                starList.Add(ending);
            }
        }

        return grouped;
    }

    private string BuildStarLabel(int star)
    {
        int clampedStar = Mathf.Clamp(star, 0, 7);
        if (clampedStar <= 0)
        {
            return "0 星";
        }

        return $"{clampedStar} 星  [{new string('*', clampedStar)}]";
    }

    private void DrawEvents()
    {
        EditorGUILayout.LabelField("事件管理", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("上方是两个主界面：1) 查询某回合会判定哪些事件并增减事件；2) 查看和搜索当前已有事件。只有点击每行最右侧“编辑”才进入详细编辑。新增事件只在事件列表里。", MessageType.Info);

        DrawRoundEventQueryPanel();
        EditorGUILayout.Space(10f);
        DrawEventLibraryPanel();
        EditorGUILayout.Space(10f);
        DrawEventAuthoringTool();
        EditorGUILayout.Space(12f);
        DrawEventQuickControls();
    }

    private int GetTotalRounds(int year, int semester, int round)
    {
        int roundsPerSemester = Mathf.Max(1, GameState.MaxRoundsPerSemester);
        int clampedYear = Mathf.Clamp(year, 1, 4);
        int clampedSemester = Mathf.Clamp(semester, 1, 2);
        int clampedRound = Mathf.Clamp(round, 1, roundsPerSemester);
        return (clampedYear - 1) * 2 * roundsPerSemester
            + (clampedSemester - 1) * roundsPerSemester
            + clampedRound;
    }

    private void DrawEventAuthoringTool()
    {
        EditorGUILayout.LabelField("事件详细编辑", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(eventEditorModeText, MessageType.None);

        if (!isCreatingNewEvent && string.IsNullOrEmpty(editingEventId))
        {
            return;
        }

        eventIdInput = EditorGUILayout.TextField("事件 ID", eventIdInput);
        eventTitleInput = EditorGUILayout.TextField("事件标题", eventTitleInput);
        eventDescriptionInput = EditorGUILayout.TextField("事件描述", eventDescriptionInput);
        eventTypeIndex = EditorGUILayout.Popup("事件类型", eventTypeIndex, EventTypeDisplayOptions);
        eventPhaseIndex = EditorGUILayout.Popup("触发阶段", eventPhaseIndex, EventPhaseDisplayOptions);
        eventPriorityInput = EditorGUILayout.IntField("优先级", eventPriorityInput);
        eventForcedInput = EditorGUILayout.Toggle("强制触发", eventForcedInput);
        eventRepeatableInput = EditorGUILayout.Toggle("可重复触发", eventRepeatableInput);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("触发条件", EditorStyles.boldLabel);
        eventYearTriggerInput = EditorGUILayout.IntField("学年", eventYearTriggerInput);
        eventSemesterTriggerInput = EditorGUILayout.IntField("学期", eventSemesterTriggerInput);
        eventRoundMinInput = EditorGUILayout.IntField("回合下限", eventRoundMinInput);
        eventRoundMaxInput = EditorGUILayout.IntField("回合上限", eventRoundMaxInput);
        eventSpecificRoundsInput = EditorGUILayout.TextField("指定回合", eventSpecificRoundsInput);
        eventProbabilityInput = EditorGUILayout.Slider("触发概率", eventProbabilityInput, 0f, 1f);
        eventTriggerBehaviorInput = EditorGUILayout.TextField("行为触发键", eventTriggerBehaviorInput);
        eventMinMoneyInput = EditorGUILayout.IntField("最低金钱", eventMinMoneyInput);
        eventMaxMoneyInput = EditorGUILayout.IntField("最高金钱", eventMaxMoneyInput);
        eventMinDarknessInput = EditorGUILayout.IntField("最低黑暗值", eventMinDarknessInput);
        DrawEventReferenceSelector("前置事件", eventRequiredEventIds, ref eventRequiredAddIndex);
        DrawEventReferenceSelector("排除事件", eventExcludedEventIds, ref eventExcludedAddIndex);
        DrawEventAttributeConditionEditor();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("对话流", EditorStyles.boldLabel);
        eventSpeakerInput = EditorGUILayout.TextField("说话人", eventSpeakerInput);
        eventPortraitInput = EditorGUILayout.TextField("头像 ID", eventPortraitInput);
        DrawDialogueLineEditor();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("选项与效果", EditorStyles.boldLabel);
        DrawChoiceEditor();

        EditorGUILayout.LabelField("默认效果");
        DrawEffectDraftList(eventDefaultEffectDrafts, "添加默认效果");
        DrawEventReferenceSelector("事件链", eventChainEventIds, ref eventChainAddIndex);
        EditorGUILayout.HelpBox(
            "格式提示：\n" +
            "指定回合：1,5,8\n" +
            "属性条件和属性效果请直接从下拉框里选属性名，避免输错。\n" +
            "剧情支持一句一句添加，选项支持单独增删和修改。",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存草稿", GUILayout.Width(100f)))
        {
            SaveEventDraft();
        }
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || EventScheduler.Instance == null))
        {
            if (GUILayout.Button("注册到当前运行", GUILayout.Width(140f)))
            {
                RegisterDraftToRuntime();
            }
        }
        if (GUILayout.Button("清空编辑器", GUILayout.Width(100f)))
        {
            ClearEventAuthoringInputs();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(eventAuthoringStatus))
        {
            EditorGUILayout.HelpBox(eventAuthoringStatus, MessageType.Info);
        }

        DrawEventDraftSummary();
        DrawEventDraftPreview();
    }

    private void DrawEventAttributeConditionEditor()
    {
        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("属性条件", EditorStyles.miniBoldLabel);

        if (eventAttributeConditionDrafts.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无属性条件。", MessageType.None);
        }

        for (int i = 0; i < eventAttributeConditionDrafts.Count; i++)
        {
            EventAttributeConditionDraft draft = eventAttributeConditionDrafts[i];
            if (draft == null)
            {
                continue;
            }

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                draft.attributeIndex = EditorGUILayout.Popup(Mathf.Clamp(draft.attributeIndex, 0, EventAttributeOptions.Length - 1), EventAttributeOptions, GUILayout.Width(120f));
                draft.comparisonIndex = EditorGUILayout.Popup(Mathf.Clamp(draft.comparisonIndex, 0, EventComparisonOptions.Length - 1), EventComparisonOptions, GUILayout.Width(70f));
                draft.value = EditorGUILayout.IntField(draft.value, GUILayout.Width(80f));
                if (GUILayout.Button("删除", GUILayout.Width(60f)))
                {
                    eventAttributeConditionDrafts.RemoveAt(i);
                    i--;
                }
            }
        }

        if (GUILayout.Button("添加属性条件", GUILayout.Width(120f)))
        {
            eventAttributeConditionDrafts.Add(new EventAttributeConditionDraft());
        }
    }

    private void DrawEventReferenceSelector(string label, List<string> selectedIds, ref int addIndex)
    {
        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        List<EventListEntry> availableEntries = GetMergedEventEntries()
            .Where(entry => entry != null && entry.Event != null)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Event.id))
            .Where(entry => entry.Event.id != eventIdInput)
            .OrderBy(entry => entry.Event.id)
            .ToList();

        List<EventListEntry> addableEntries = availableEntries
            .Where(entry => !selectedIds.Contains(entry.Event.id))
            .ToList();

        if (selectedIds.Count == 0)
        {
            EditorGUILayout.HelpBox($"暂无{label}。", MessageType.None);
        }
        else
        {
            for (int i = 0; i < selectedIds.Count; i++)
            {
                string selectedId = selectedIds[i];
                EventListEntry selectedEntry = availableEntries.FirstOrDefault(entry => entry.Event.id == selectedId);
                string display = selectedEntry != null
                    ? $"{selectedEntry.Event.id} | {selectedEntry.Event.title} | {GetEventTypeDisplayName(selectedEntry.Event.eventType)}"
                    : selectedId;

                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    EditorGUILayout.LabelField(display);
                    if (GUILayout.Button("移除", GUILayout.Width(60f)))
                    {
                        selectedIds.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        if (addableEntries.Count == 0)
        {
            EditorGUILayout.HelpBox($"没有可添加到{label}的事件。", MessageType.None);
            return;
        }

        string[] options = addableEntries
            .Select(entry => $"{entry.Event.id} | {entry.Event.title} | {GetEventTypeDisplayName(entry.Event.eventType)}")
            .ToArray();
        addIndex = Mathf.Clamp(addIndex, 0, Mathf.Max(0, options.Length - 1));

        using (new EditorGUILayout.HorizontalScope())
        {
            addIndex = EditorGUILayout.Popup(addIndex, options);
            if (GUILayout.Button("添加", GUILayout.Width(60f)))
            {
                string eventRefId = addableEntries[addIndex].Event.id;
                if (!selectedIds.Contains(eventRefId))
                {
                    selectedIds.Add(eventRefId);
                }
            }
        }
    }

    private void DrawSingleEventSelector(string label, ref string selectedEventId)
    {
        List<EventListEntry> availableEntries = GetMergedEventEntries()
            .Where(entry => entry != null && entry.Event != null)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Event.id))
            .Where(entry => entry.Event.id != eventIdInput)
            .OrderBy(entry => entry.Event.id)
            .ToList();

        string currentDisplay = string.IsNullOrWhiteSpace(selectedEventId)
            ? "无"
            : BuildEventReferenceLabel(selectedEventId, availableEntries);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        EditorGUILayout.LabelField(currentDisplay, EditorStyles.helpBox, GUILayout.Height(20f));
        if (GUILayout.Button("清空", GUILayout.Width(50f)))
        {
            selectedEventId = string.Empty;
        }
        EditorGUILayout.EndHorizontal();

        if (availableEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("没有可选事件。", MessageType.None);
            return;
        }

        int selectedIndex = 0;
        string[] options = new string[availableEntries.Count + 1];
        options[0] = "无";
        for (int i = 0; i < availableEntries.Count; i++)
        {
            options[i + 1] = BuildEventReferenceLabel(availableEntries[i].Event.id, availableEntries);
            if (!string.IsNullOrWhiteSpace(selectedEventId) && availableEntries[i].Event.id == selectedEventId)
            {
                selectedIndex = i + 1;
            }
        }

        int nextIndex = EditorGUILayout.Popup(selectedIndex, options);
        selectedEventId = nextIndex <= 0 ? string.Empty : availableEntries[nextIndex - 1].Event.id;
    }

    private string BuildEventReferenceLabel(string eventId, List<EventListEntry> availableEntries = null)
    {
        List<EventListEntry> entries = availableEntries ?? GetMergedEventEntries();
        EventListEntry entry = entries.FirstOrDefault(item => item != null && item.Event != null && item.Event.id == eventId);
        return entry != null
            ? $"{entry.Event.id} | {entry.Event.title} | {GetEventTypeDisplayName(entry.Event.eventType)}"
            : eventId;
    }

    private void DrawDialogueLineEditor()
    {
        EnsureEventDraftCollections();

        if (eventDialogueLines.Count == 0)
        {
            eventDialogueLines.Add(string.Empty);
        }

        for (int i = 0; i < eventDialogueLines.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField($"第 {i + 1} 句", GUILayout.Width(52f));
                eventDialogueLines[i] = EditorGUILayout.TextField(eventDialogueLines[i]);
                using (new EditorGUI.DisabledScope(i <= 0))
                {
                    if (GUILayout.Button("上移", GUILayout.Width(50f)))
                    {
                        string current = eventDialogueLines[i];
                        eventDialogueLines[i] = eventDialogueLines[i - 1];
                        eventDialogueLines[i - 1] = current;
                    }
                }
                using (new EditorGUI.DisabledScope(i >= eventDialogueLines.Count - 1))
                {
                    if (GUILayout.Button("下移", GUILayout.Width(50f)))
                    {
                        string current = eventDialogueLines[i];
                        eventDialogueLines[i] = eventDialogueLines[i + 1];
                        eventDialogueLines[i + 1] = current;
                    }
                }
                if (GUILayout.Button("删除", GUILayout.Width(60f)))
                {
                    eventDialogueLines.RemoveAt(i);
                    i--;
                }
            }
        }

        if (GUILayout.Button("添加一句对话", GUILayout.Width(120f)))
        {
            eventDialogueLines.Add(string.Empty);
        }
    }

    private void DrawChoiceEditor()
    {
        EnsureEventDraftCollections();

        if (eventChoiceDrafts.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无选项。没有选项时事件会直接结算默认效果。", MessageType.None);
        }

        for (int i = 0; i < eventChoiceDrafts.Count; i++)
        {
            EventChoiceDraft draft = eventChoiceDrafts[i];
            if (draft == null)
            {
                continue;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"选项 {i + 1}", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("删除选项", GUILayout.Width(80f)))
            {
                eventChoiceDrafts.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                i--;
                continue;
            }
            EditorGUILayout.EndHorizontal();

            draft.text = EditorGUILayout.TextField("文本", draft.text);
            DrawSingleEventSelector("后续事件", ref draft.nextEventId);
            EditorGUILayout.LabelField("选项效果", EditorStyles.miniBoldLabel);
            DrawEffectDraftList(draft.effects, "添加选项效果");
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("添加选项", GUILayout.Width(100f)))
        {
            eventChoiceDrafts.Add(new EventChoiceDraft());
        }
    }

    private void DrawEffectDraftList(List<EventEffectDraft> drafts, string addButtonLabel)
    {
        EnsureEventDraftCollections();
        if (drafts == null)
        {
            return;
        }

        if (drafts.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无效果。", MessageType.None);
        }

        for (int i = 0; i < drafts.Count; i++)
        {
            EventEffectDraft draft = drafts[i];
            if (draft == null)
            {
                continue;
            }

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                draft.effectTypeIndex = EditorGUILayout.Popup(Mathf.Clamp(draft.effectTypeIndex, 0, EventEffectTypeDisplayOptions.Length - 1), EventEffectTypeDisplayOptions, GUILayout.Width(100f));
                string effectType = EventEffectTypeOptions[Mathf.Clamp(draft.effectTypeIndex, 0, EventEffectTypeOptions.Length - 1)];
                if (effectType == "attribute")
                {
                    draft.attributeIndex = EditorGUILayout.Popup(Mathf.Clamp(draft.attributeIndex, 0, EventAttributeOptions.Length - 1), EventAttributeOptions, GUILayout.Width(120f));
                }
                else
                {
                    draft.targetText = EditorGUILayout.TextField(draft.targetText ?? string.Empty, GUILayout.Width(140f));
                }

                draft.value = EditorGUILayout.IntField(draft.value, GUILayout.Width(80f));
                if (GUILayout.Button("删除", GUILayout.Width(60f)))
                {
                    drafts.RemoveAt(i);
                    i--;
                }
            }
        }

        if (GUILayout.Button(addButtonLabel, GUILayout.Width(120f)))
        {
            drafts.Add(new EventEffectDraft());
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

    private void DrawRoundEventQueryPanel()
    {
        EditorGUILayout.LabelField("一、回合事件视图", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        eventTimelineYearInput = EditorGUILayout.IntField("学年", eventTimelineYearInput, GUILayout.Width(180f));
        eventTimelineSemesterInput = EditorGUILayout.IntField("学期", eventTimelineSemesterInput, GUILayout.Width(180f));
        eventTimelineRoundInput = EditorGUILayout.IntField("回合", eventTimelineRoundInput, GUILayout.Width(180f));
        eventTimelinePhaseIndex = EditorGUILayout.Popup("阶段", eventTimelinePhaseIndex, EventTimelinePhaseDisplayOptions, GUILayout.Width(240f));
        if (GUILayout.Button("当前时间", GUILayout.Width(80f)) && GameState.Instance != null)
        {
            eventTimelineYearInput = GameState.Instance.CurrentYear;
            eventTimelineSemesterInput = GameState.Instance.CurrentSemester;
            eventTimelineRoundInput = GameState.Instance.CurrentRound;
        }
        EditorGUILayout.EndHorizontal();

        List<EventListEntry> allEntries = GetMergedEventEntries();
        List<EventListEntry> roundCandidates = GetRoundCandidates(allEntries, eventTimelineYearInput, eventTimelineSemesterInput, eventTimelineRoundInput);
        string[] addOptions = roundCandidates.Count == 0
            ? new[] { "没有可加入的事件" }
            : roundCandidates.Select(entry => $"{entry.Event.id} | {entry.Event.title} | {GetEventTypeDisplayName(entry.Event.eventType)}").ToArray();
        eventRoundAddSelectionIndex = Mathf.Clamp(eventRoundAddSelectionIndex, 0, Mathf.Max(0, addOptions.Length - 1));

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(addOptions.Length == 0))
        {
            eventRoundAddSelectionIndex = EditorGUILayout.Popup("加入当前回合", eventRoundAddSelectionIndex, addOptions);
        }
        using (new EditorGUI.DisabledScope(roundCandidates.Count == 0))
        {
            if (GUILayout.Button("加入", GUILayout.Width(80f)))
            {
                AddEventToQueriedRound(roundCandidates[eventRoundAddSelectionIndex].Event.id);
            }
        }
        EditorGUILayout.EndHorizontal();

        string selectedPhase = EventTimelinePhaseOptions[Mathf.Clamp(eventTimelinePhaseIndex, 0, EventTimelinePhaseOptions.Length - 1)];
        string selectedPhaseDisplay = EventTimelinePhaseDisplayOptions[Mathf.Clamp(eventTimelinePhaseIndex, 0, EventTimelinePhaseDisplayOptions.Length - 1)];
        List<EventListEntry> queried = allEntries
            .Where(entry => entry.Event != null)
            .Where(entry => selectedPhase == "All" || string.Equals(GetEventPhase(entry.Event), selectedPhase, StringComparison.Ordinal))
            .Where(entry => MatchesTimelineWindow(entry.Event, eventTimelineYearInput, eventTimelineSemesterInput, eventTimelineRoundInput))
            .OrderBy(entry => entry.Event.priority)
            .ThenBy(entry => GetPrimaryRound(entry.Event.trigger))
            .ThenBy(entry => entry.Event.id)
            .ToList();

        EditorGUILayout.HelpBox($"当前查询 Y{eventTimelineYearInput} S{eventTimelineSemesterInput} R{eventTimelineRoundInput} / {selectedPhaseDisplay}：共 {queried.Count} 条事件会进入判定。", MessageType.None);

        if (queried.Count == 0)
        {
            EditorGUILayout.HelpBox("当前回合没有会进入判定的事件。", MessageType.None);
        }
        else
        {
            DrawGroupedEventEntries(queried, DrawRoundEventRow);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEventLibraryPanel()
    {
        EditorGUILayout.LabelField("二、事件列表", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        eventLibrarySearchInput = EditorGUILayout.TextField("搜索", eventLibrarySearchInput);
        if (GUILayout.Button("新增事件", GUILayout.Width(100f)))
        {
            BeginCreateEvent();
        }
        EditorGUILayout.EndHorizontal();

        List<EventListEntry> entries = GetMergedEventEntries()
            .Where(entry => MatchesEventSearch(entry, eventLibrarySearchInput))
            .OrderBy(entry => entry.Event.eventType)
            .ThenBy(entry => entry.Event.id)
            .ToList();

        EditorGUILayout.HelpBox($"当前已有事件：{entries.Count} 条。这里可以查看、搜索，并通过最右侧按钮进入详细编辑。", MessageType.None);

        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("没有匹配到事件。", MessageType.None);
        }
        else
        {
            DrawGroupedEventEntries(entries, DrawLibraryEventRow);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawEventQuickControls()
    {
        EditorGUILayout.LabelField("快速控制（运行时）", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
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
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"已加载事件: {EventScheduler.Instance.GetLoadedEventCount()}");
            EditorGUILayout.LabelField($"待处理队列: {EventScheduler.Instance.GetPendingEventCount()}");
        }

        EditorGUILayout.EndVertical();
    }

    private void SaveEventDraft()
    {
        if (!TryBuildEditorEventDefinition(out EventDefinition evt, out string error))
        {
            eventAuthoringStatus = error;
            return;
        }

        string json = JsonUtility.ToJson(new EventDatabaseRoot { events = new[] { evt } }, true);
        ZhongshanDeckToolStateBridge.SaveAuthoredEvent(evt.id, evt.title, json);
        if (EditorApplication.isPlaying && EventScheduler.Instance != null)
        {
            EventScheduler.Instance.RegisterOrReplaceRuntimeEvent(CloneEvent(evt));
        }
        editingEventId = evt.id;
        editingEventSource = "草稿";
        isCreatingNewEvent = false;
        UpdateEventEditorModeText();
        eventAuthoringStatus = $"已保存草稿：{evt.id}";
        Repaint();
    }

    private void LoadEventDraft()
    {
        if (string.IsNullOrWhiteSpace(eventIdInput))
        {
            eventAuthoringStatus = "请输入要载入的事件 ID";
            return;
        }

        if (!ZhongshanDeckToolStateBridge.TryGetAuthoredEvent(eventIdInput.Trim(), out ZhongshanDeckEventEntry entry))
        {
            eventAuthoringStatus = $"未找到草稿：{eventIdInput.Trim()}";
            return;
        }

        EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(entry.json);
        if (root == null || root.events == null || root.events.Length == 0)
        {
            eventAuthoringStatus = $"草稿解析失败：{eventIdInput.Trim()}";
            return;
        }

        FillEventEditor(root.events[0]);
        editingEventId = root.events[0].id ?? eventIdInput.Trim();
        editingEventSource = "草稿";
        isCreatingNewEvent = false;
        UpdateEventEditorModeText();
        eventAuthoringStatus = $"已载入草稿：{eventIdInput.Trim()}";
    }

    private void DeleteEventDraft()
    {
        if (string.IsNullOrWhiteSpace(eventIdInput))
        {
            eventAuthoringStatus = "请输入要删除的事件 ID";
            return;
        }

        bool deleted = ZhongshanDeckToolStateBridge.DeleteAuthoredEvent(eventIdInput.Trim());
        eventAuthoringStatus = deleted ? $"已删除草稿：{eventIdInput.Trim()}" : $"未找到草稿：{eventIdInput.Trim()}";
    }

    private void RegisterDraftToRuntime()
    {
        if (!TryBuildEditorEventDefinition(out EventDefinition evt, out string error))
        {
            eventAuthoringStatus = error;
            return;
        }

        EventScheduler.Instance.RegisterOrReplaceRuntimeEvent(evt);
        editingEventId = evt.id;
        editingEventSource = "运行时";
        isCreatingNewEvent = false;
        UpdateEventEditorModeText();
        eventAuthoringStatus = $"已注册到当前运行：{evt.id}";
        DebugConsoleManager.Log("Event", $"Editor window registered runtime event: {evt.id}");
    }

    private void ClearEventAuthoringInputs()
    {
        eventIdInput = string.Empty;
        eventTitleInput = string.Empty;
        eventDescriptionInput = string.Empty;
        eventTypeIndex = 0;
        eventPhaseIndex = 0;
        eventPriorityInput = 2;
        eventForcedInput = false;
        eventRepeatableInput = true;
        eventYearTriggerInput = 0;
        eventSemesterTriggerInput = 0;
        eventRoundMinInput = 0;
        eventRoundMaxInput = 0;
        eventSpecificRoundsInput = string.Empty;
        eventProbabilityInput = 1f;
        eventTriggerBehaviorInput = string.Empty;
        eventAttributeConditionDrafts.Clear();
        eventMinMoneyInput = 0;
        eventMaxMoneyInput = 0;
        eventMinDarknessInput = 0;
        eventRequiredEventIds.Clear();
        eventExcludedEventIds.Clear();
        eventRequiredAddIndex = 0;
        eventExcludedAddIndex = 0;
        eventSpeakerInput = string.Empty;
        eventPortraitInput = string.Empty;
        eventDialogueLines.Clear();
        eventDialogueLines.Add(string.Empty);
        eventDefaultEffectDrafts.Clear();
        eventChainEventIds.Clear();
        eventChainAddIndex = 0;
        eventChoiceDrafts.Clear();

        editingEventId = string.Empty;
        editingEventSource = string.Empty;
        isCreatingNewEvent = false;
        UpdateEventEditorModeText();
        eventAuthoringStatus = "已清空剧情编辑器";
    }

    private bool TryBuildEditorEventDefinition(out EventDefinition evt, out string error)
    {
        evt = null;
        error = null;

        string eventId = string.IsNullOrWhiteSpace(eventIdInput) ? string.Empty : eventIdInput.Trim();
        if (string.IsNullOrEmpty(eventId))
        {
            error = "事件 ID 不能为空";
            return false;
        }

        string title = string.IsNullOrWhiteSpace(eventTitleInput) ? string.Empty : eventTitleInput.Trim();
        if (string.IsNullOrEmpty(title))
        {
            error = "事件标题不能为空";
            return false;
        }

        EnsureEventDraftCollections();
        string[] dialogueLines = eventDialogueLines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToArray();
        if (dialogueLines.Length == 0)
        {
            error = "至少填写一行对话";
            return false;
        }

        evt = new EventDefinition
        {
            id = eventId,
            eventType = EventTypeOptions[Mathf.Clamp(eventTypeIndex, 0, EventTypeOptions.Length - 1)],
            title = title,
            description = eventDescriptionInput?.Trim() ?? string.Empty,
            priority = eventPriorityInput,
            isForced = eventForcedInput,
            isRepeatable = eventRepeatableInput,
            trigger = new EventTriggerCondition
            {
                year = Mathf.Max(0, eventYearTriggerInput),
                semester = Mathf.Max(0, eventSemesterTriggerInput),
                roundMin = Mathf.Max(0, eventRoundMinInput),
                roundMax = Mathf.Max(0, eventRoundMaxInput),
                specificRounds = ParseIntList(eventSpecificRoundsInput),
                attributeConditions = BuildAttributeConditions(),
                minMoney = eventMinMoneyInput,
                maxMoney = eventMaxMoneyInput,
                affinityConditions = Array.Empty<AffinityCondition>(),
                requiredEventIds = eventRequiredEventIds.ToArray(),
                excludedEventIds = eventExcludedEventIds.ToArray(),
                minDarkness = Mathf.Max(0, eventMinDarknessInput),
                triggerBehavior = eventTriggerBehaviorInput?.Trim() ?? string.Empty,
                triggerChance = Mathf.Clamp01(eventProbabilityInput),
                phase = EventPhaseOptions[Mathf.Clamp(eventPhaseIndex, 0, EventPhaseOptions.Length - 1)]
            },
            dialogues = new[]
            {
                new EventDialogue
                {
                    speaker = eventSpeakerInput?.Trim() ?? string.Empty,
                    lines = dialogueLines,
                    portraitId = eventPortraitInput?.Trim() ?? string.Empty
                }
            },
            choices = BuildEditorChoices(),
            defaultEffects = BuildEventEffects(eventDefaultEffectDrafts),
            chainEventIds = eventChainEventIds.ToArray()
        };

        return true;
    }

    private void EnsureEndingEditorDataLoaded()
    {
        if (endingEditorCache == null || endingEditorCache.Count == 0)
        {
            LoadEndingEditorData(false);
        }
    }

    private void LoadEndingEditorData(bool preserveSelection)
    {
        string selection = preserveSelection ? editingEndingId : string.Empty;
        endingEditorCache = LoadEndingDefinitionsFromAsset();

        if (endingEditorCache.Count == 0)
        {
            ClearEndingEditorSelection();
            endingEditorStatus = $"读取失败：{EndingsDataPath}";
            return;
        }

        if (!string.IsNullOrWhiteSpace(selection))
        {
            EndingDefinition selected = FindEndingInCache(selection);
            if (selected != null)
            {
                BeginEditEnding(selected);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(editingEndingId) && endingEditorCache.Count > 0)
        {
            BeginEditEnding(endingEditorCache[0]);
        }
    }

    private List<EndingDefinition> LoadEndingDefinitionsFromAsset()
    {
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(EndingsDataPath);
        if (textAsset == null)
        {
            return new List<EndingDefinition>();
        }

        try
        {
            EndingDataRoot root = JsonUtility.FromJson<EndingDataRoot>(textAsset.text);
            if (root == null || root.endings == null)
            {
                return new List<EndingDefinition>();
            }

            List<EndingDefinition> result = new List<EndingDefinition>(root.endings.Count);
            for (int i = 0; i < root.endings.Count; i++)
            {
                if (root.endings[i] != null)
                {
                    result.Add(root.endings[i].Clone());
                }
            }
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ZhongshanDeck] 读取 endings.json 失败: {e.Message}");
            return new List<EndingDefinition>();
        }
    }

    private void BeginEditEnding(EndingDefinition ending)
    {
        if (ending == null)
        {
            return;
        }

        editingEndingId = ending.id ?? string.Empty;
        endingEditName = ending.name ?? string.Empty;
        endingEditDescription = ending.description ?? string.Empty;
        endingEditCgId = ending.cgId ?? string.Empty;
        endingConditionDrafts.Clear();

        if (ending.conditions != null)
        {
            for (int i = 0; i < ending.conditions.Count; i++)
            {
                endingConditionDrafts.Add(ending.conditions[i] != null ? ending.conditions[i].Clone() : new EndingCondition());
            }
        }

        if (endingConditionDrafts.Count == 0)
        {
            endingConditionDrafts.Add(new EndingCondition(
                EndingConditionTypeOptions.Length > 0 ? EndingConditionTypeOptions[0] : EndingConditionType.AlwaysTrue.ToString(),
                0f));
        }

        endingEditorStatus = $"正在编辑 {editingEndingId}";
    }

    private void ClearEndingEditorSelection()
    {
        editingEndingId = string.Empty;
        endingEditName = string.Empty;
        endingEditDescription = string.Empty;
        endingEditCgId = string.Empty;
        endingConditionDrafts.Clear();
    }

    private EndingDefinition FindEndingInCache(string endingId)
    {
        if (string.IsNullOrWhiteSpace(endingId) || endingEditorCache == null)
        {
            return null;
        }

        for (int i = 0; i < endingEditorCache.Count; i++)
        {
            EndingDefinition ending = endingEditorCache[i];
            if (ending != null && string.Equals(ending.id, endingId, StringComparison.OrdinalIgnoreCase))
            {
                return ending;
            }
        }

        return null;
    }

    private void SaveEditingEndingToAsset()
    {
        if (string.IsNullOrWhiteSpace(editingEndingId))
        {
            endingEditorStatus = "请先选择一个结局。";
            return;
        }

        EndingDefinition target = FindEndingInCache(editingEndingId);
        if (target == null)
        {
            endingEditorStatus = $"未找到结局 {editingEndingId}";
            return;
        }

        if (string.IsNullOrWhiteSpace(endingEditName))
        {
            endingEditorStatus = "结局标题不能为空。";
            return;
        }

        target.name = endingEditName.Trim();
        target.description = (endingEditDescription ?? string.Empty).Trim();
        target.cgId = (endingEditCgId ?? string.Empty).Trim();
        target.conditions = new List<EndingCondition>();

        for (int i = 0; i < endingConditionDrafts.Count; i++)
        {
            EndingCondition condition = endingConditionDrafts[i];
            if (condition == null || string.IsNullOrWhiteSpace(condition.type))
            {
                continue;
            }

            target.conditions.Add(condition.Clone());
        }

        try
        {
            EndingDataRoot root = new EndingDataRoot { endings = endingEditorCache.Select(e => e != null ? e.Clone() : null).Where(e => e != null).ToList() };
            string json = JsonUtility.ToJson(root, true);
            File.WriteAllText(EndingsDataPath, json, new UTF8Encoding(true));
            AssetDatabase.Refresh();

            if (EditorApplication.isPlaying && EndingDeterminer.Instance != null)
            {
                EndingDeterminer.Instance.UpdateEndingDefinition(target);
            }

            endingEditorStatus = $"已保存 {editingEndingId} 到 endings.json";
            BeginEditEnding(target);
        }
        catch (Exception e)
        {
            endingEditorStatus = $"保存失败: {e.Message}";
            Debug.LogError($"[ZhongshanDeck] 保存 endings.json 失败: {e}");
        }
    }

    private EventChoice[] BuildEditorChoices()
    {
        List<EventChoice> choices = new List<EventChoice>();
        for (int i = 0; i < eventChoiceDrafts.Count; i++)
        {
            EventChoiceDraft draft = eventChoiceDrafts[i];
            if (draft == null || string.IsNullOrWhiteSpace(draft.text))
            {
                continue;
            }

            choices.Add(new EventChoice
            {
                text = draft.text.Trim(),
                effects = BuildEventEffects(draft.effects),
                triggerEventId = string.IsNullOrWhiteSpace(draft.nextEventId) ? string.Empty : draft.nextEventId.Trim(),
                showConditions = Array.Empty<AttributeCondition>()
            });
        }

        return choices.ToArray();
    }

    private void FillEventEditor(EventDefinition evt)
    {
        if (evt == null)
        {
            return;
        }

        eventIdInput = evt.id ?? string.Empty;
        eventTitleInput = evt.title ?? string.Empty;
        eventDescriptionInput = evt.description ?? string.Empty;
        eventTypeIndex = Mathf.Max(0, Array.IndexOf(EventTypeOptions, string.IsNullOrEmpty(evt.eventType) ? "Fixed" : evt.eventType));
        eventPriorityInput = evt.priority;
        eventForcedInput = evt.isForced;
        eventRepeatableInput = evt.isRepeatable;

        EventTriggerCondition trigger = evt.trigger ?? new EventTriggerCondition();
        eventPhaseIndex = Mathf.Max(0, Array.IndexOf(EventPhaseOptions, string.IsNullOrEmpty(trigger.phase) ? "RoundStart" : trigger.phase));
        eventYearTriggerInput = trigger.year;
        eventSemesterTriggerInput = trigger.semester;
        eventRoundMinInput = trigger.roundMin;
        eventRoundMaxInput = trigger.roundMax;
        eventSpecificRoundsInput = string.Join(",", trigger.specificRounds ?? Array.Empty<int>());
        eventProbabilityInput = trigger.triggerChance <= 0f ? 0f : trigger.triggerChance;
        eventTriggerBehaviorInput = trigger.triggerBehavior ?? string.Empty;
        FillAttributeConditionDrafts(trigger.attributeConditions);
        eventMinMoneyInput = trigger.minMoney;
        eventMaxMoneyInput = trigger.maxMoney;
        eventMinDarknessInput = trigger.minDarkness;
        eventRequiredEventIds.Clear();
        eventExcludedEventIds.Clear();
        eventRequiredAddIndex = 0;
        eventExcludedAddIndex = 0;
        if (trigger.requiredEventIds != null)
        {
            eventRequiredEventIds.AddRange(trigger.requiredEventIds.Where(id => !string.IsNullOrWhiteSpace(id)));
        }
        if (trigger.excludedEventIds != null)
        {
            eventExcludedEventIds.AddRange(trigger.excludedEventIds.Where(id => !string.IsNullOrWhiteSpace(id)));
        }

        EventDialogue dialogue = evt.dialogues != null && evt.dialogues.Length > 0 ? evt.dialogues[0] : null;
        eventSpeakerInput = dialogue != null ? dialogue.speaker ?? string.Empty : string.Empty;
        eventPortraitInput = dialogue != null ? dialogue.portraitId ?? string.Empty : string.Empty;
        eventDialogueLines.Clear();
        if (dialogue != null && dialogue.lines != null && dialogue.lines.Length > 0)
        {
            for (int i = 0; i < dialogue.lines.Length; i++)
            {
                eventDialogueLines.Add(dialogue.lines[i] ?? string.Empty);
            }
        }
        if (eventDialogueLines.Count == 0)
        {
            eventDialogueLines.Add(string.Empty);
        }
        FillEffectDrafts(eventDefaultEffectDrafts, evt.defaultEffects);
        eventChainEventIds.Clear();
        eventChainAddIndex = 0;
        if (evt.chainEventIds != null)
        {
            eventChainEventIds.AddRange(evt.chainEventIds.Where(id => !string.IsNullOrWhiteSpace(id)));
        }

        eventChoiceDrafts.Clear();
        if (evt.choices != null)
        {
            for (int i = 0; i < evt.choices.Length; i++)
            {
                EventChoice choice = evt.choices[i];
                if (choice == null)
                {
                    continue;
                }

                EventChoiceDraft draft = new EventChoiceDraft
                {
                    text = choice.text ?? string.Empty,
                    nextEventId = choice.triggerEventId ?? string.Empty
                };
                FillEffectDrafts(draft.effects, choice.effects);
                eventChoiceDrafts.Add(draft);
            }
        }

        editingEventId = evt.id ?? string.Empty;
        editingEventSource = ResolveEventSourceLabel(evt.id);
        isCreatingNewEvent = false;
        UpdateEventEditorModeText();
    }

    private void BeginCreateEvent()
    {
        ClearEventAuthoringInputs();
        isCreatingNewEvent = true;
        editingEventSource = "新建事件";
        UpdateEventEditorModeText();
        eventAuthoringStatus = "正在新建事件";
    }

    private void UpdateEventEditorModeText()
    {
        if (isCreatingNewEvent)
        {
            eventEditorModeText = "正在新建事件。新增入口只在上方“事件列表”里。";
            return;
        }

        if (!string.IsNullOrEmpty(editingEventId))
        {
            eventEditorModeText = $"正在编辑：{editingEventId}  来源：{editingEventSource}";
            return;
        }

        eventEditorModeText = "请从上方事件列表右侧的编辑按钮进入详情编辑";
    }

    private void DrawGroupedEventEntries(List<EventListEntry> entries, Action<EventListEntry> drawRow)
    {
        if (entries == null || entries.Count == 0 || drawRow == null)
        {
            return;
        }

        for (int typeIndex = 0; typeIndex < EventTypeOptions.Length; typeIndex++)
        {
            string eventType = EventTypeOptions[typeIndex];
            List<EventListEntry> group = entries
                .Where(entry => entry != null && entry.Event != null && string.Equals(entry.Event.eventType, eventType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (group.Count == 0)
            {
                continue;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(GetEventTypeDisplayName(eventType), EditorStyles.boldLabel);
            for (int i = 0; i < group.Count; i++)
            {
                drawRow(group[i]);
            }
        }
    }

    private void DrawSceneButtonGrid(List<string> scenePaths)
    {
        if (scenePaths == null || scenePaths.Count == 0)
        {
            EditorGUILayout.HelpBox("当前项目下没有找到 .unity 场景。", MessageType.Warning);
            return;
        }

        for (int i = 0; i < scenePaths.Count; i += SceneJumpColumnCount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int column = 0; column < SceneJumpColumnCount; column++)
                {
                    int index = i + column;
                    if (index >= scenePaths.Count)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    string scenePath = scenePaths[index];
                    string label = GetSceneJumpLabel(scenePath);
                    if (GUILayout.Button(label, GUILayout.Width(220f), GUILayout.Height(42f)))
                    {
                        OpenSceneByPath(scenePath);
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }
    }

    private List<string> GetAllScenePaths()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        List<string> scenePaths = new List<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                scenePaths.Add(path);
            }
        }

        scenePaths.Sort(StringComparer.OrdinalIgnoreCase);
        return scenePaths;
    }

    private bool HasSceneAsset(string scenePath)
    {
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null;
    }

    private string GetSceneJumpLabel(string scenePath)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        string folderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(scenePath));
        string sceneLabel = GetSceneDisplayName(fileName);
        string folderLabel = GetSceneFolderDisplayName(folderName);

        if (string.Equals(folderName, "Scenes", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(folderName))
        {
            return sceneLabel;
        }

        return $"{sceneLabel}\n({folderLabel})";
    }

    private string GetSceneDisplayNameByPath(string scenePath)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        return GetSceneDisplayName(fileName);
    }

    private string GetSceneDisplayName(string sceneName)
    {
        switch (sceneName)
        {
            case "SplashScreen":
                return "启动页";
            case "LoadingScreen":
                return "加载页";
            case "TitleScreen":
                return "标题页";
            case "GameScene":
                return "游戏主场景";
            case "SampleScene":
                return "示例空场景";
            default:
                return sceneName;
        }
    }

    private string GetSceneFolderDisplayName(string folderName)
    {
        switch (folderName)
        {
            case "StartMenu":
                return "开始菜单示例";
            case "GameScene":
                return "游戏场景";
            default:
                return folderName;
        }
    }

    private string GetEventTypeDisplayName(string eventType)
    {
        switch (eventType)
        {
            case "Fixed":
                return "固定事件";
            case "MainStory":
                return "主线事件";
            case "Conditional":
                return "条件事件";
            case "Dark":
                return "黑暗事件";
            default:
                return eventType;
        }
    }

    private string GetEventPhaseDisplayName(string phase)
    {
        switch (phase)
        {
            case "RoundStart":
                return "回合开始";
            case "ActionComplete":
                return "行动完成后";
            case "RoundEnd":
                return "回合结束";
            default:
                return string.IsNullOrWhiteSpace(phase) ? "未设置" : phase;
        }
    }

    private void DrawRoundEventRow(EventListEntry entry)
    {
        EventDefinition evt = entry.Event;
        if (evt == null)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope("box"))
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField($"{evt.id} | {evt.title}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"阶段 {GetEventPhase(evt)}    优先级 P{evt.priority}    时间 {BuildTimeWindowLabel(evt.trigger)}");
                EditorGUILayout.LabelField($"来源 {entry.SourceLabel}");
            }

            if (GUILayout.Button("编辑", GUILayout.Width(70f)))
            {
                OpenEventForEdit(entry);
            }

            if (GUILayout.Button("移出本回合", GUILayout.Width(90f)))
            {
                RemoveEventFromQueriedRound(evt.id);
            }
        }
    }

    private void DrawLibraryEventRow(EventListEntry entry)
    {
        EventDefinition evt = entry.Event;
        if (evt == null)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope("box"))
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField($"{evt.id} | {evt.title}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"阶段 {GetEventPhase(evt)}    时间 {BuildTimeWindowLabel(evt.trigger)}");
                EditorGUILayout.LabelField($"来源 {entry.SourceLabel}");
            }

            if (GUILayout.Button("编辑", GUILayout.Width(70f)))
            {
                OpenEventForEdit(entry);
            }

            if (entry.IsDraft && GUILayout.Button("删草稿", GUILayout.Width(80f)))
            {
                DeleteDraftById(evt.id);
            }
        }
    }

    private void OpenEventForEdit(EventListEntry entry)
    {
        if (entry == null || entry.Event == null)
        {
            return;
        }

        FillEventEditor(CloneEvent(entry.Event));
        eventAuthoringStatus = $"已载入事件：{entry.Event.id}";
        Repaint();
    }

    private void AddEventToQueriedRound(string eventId)
    {
        EventListEntry entry = GetMergedEventEntries().FirstOrDefault(candidate => candidate.Event != null && candidate.Event.id == eventId);
        if (entry == null || entry.Event == null)
        {
            eventAuthoringStatus = $"未找到事件：{eventId}";
            return;
        }

        EventDefinition clone = CloneEvent(entry.Event);
        EnsureRoundBinding(clone, eventTimelineYearInput, eventTimelineSemesterInput, eventTimelineRoundInput, GetSelectedTimelinePhase());
        SaveEventDraftFromDefinition(clone);
        eventAuthoringStatus = $"已将 {eventId} 加入 Y{eventTimelineYearInput} S{eventTimelineSemesterInput} R{eventTimelineRoundInput}";
        Repaint();
    }

    private void RemoveEventFromQueriedRound(string eventId)
    {
        EventListEntry entry = GetMergedEventEntries().FirstOrDefault(candidate => candidate.Event != null && candidate.Event.id == eventId);
        if (entry == null || entry.Event == null)
        {
            eventAuthoringStatus = $"未找到事件：{eventId}";
            return;
        }

        EventDefinition clone = CloneEvent(entry.Event);
        RemoveRoundBinding(clone, eventTimelineYearInput, eventTimelineSemesterInput, eventTimelineRoundInput);
        SaveEventDraftFromDefinition(clone);
        eventAuthoringStatus = $"已将 {eventId} 从 Y{eventTimelineYearInput} S{eventTimelineSemesterInput} R{eventTimelineRoundInput} 移出";
        Repaint();
    }

    private void SaveEventDraftFromDefinition(EventDefinition evt)
    {
        if (evt == null || string.IsNullOrWhiteSpace(evt.id))
        {
            return;
        }

        string json = JsonUtility.ToJson(new EventDatabaseRoot { events = new[] { evt } }, true);
        ZhongshanDeckToolStateBridge.SaveAuthoredEvent(evt.id, evt.title, json);
        if (EditorApplication.isPlaying && EventScheduler.Instance != null)
        {
            EventScheduler.Instance.RegisterOrReplaceRuntimeEvent(CloneEvent(evt));
        }
    }

    private void DeleteDraftById(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        bool deleted = ZhongshanDeckToolStateBridge.DeleteAuthoredEvent(eventId);
        eventAuthoringStatus = deleted ? $"已删除草稿：{eventId}" : $"未找到草稿：{eventId}";
        if (editingEventId == eventId)
        {
            editingEventId = string.Empty;
            editingEventSource = string.Empty;
            isCreatingNewEvent = false;
            UpdateEventEditorModeText();
        }
        Repaint();
    }

    private List<EventListEntry> GetMergedEventEntries()
    {
        Dictionary<string, EventListEntry> merged = new Dictionary<string, EventListEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (EventDefinition evt in LoadEditorResourceEvents())
        {
            if (evt == null || string.IsNullOrWhiteSpace(evt.id))
            {
                continue;
            }

            merged[evt.id] = new EventListEntry
            {
                Event = evt,
                IsDraft = false,
                SourceLabel = "资源事件"
            };
        }

        List<ZhongshanDeckEventEntry> drafts = ZhongshanDeckToolStateBridge.GetAuthoredEvents();
        for (int i = 0; i < drafts.Count; i++)
        {
            ZhongshanDeckEventEntry draft = drafts[i];
            if (draft == null || string.IsNullOrWhiteSpace(draft.json))
            {
                continue;
            }

            EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(draft.json);
            EventDefinition evt = root != null && root.events != null && root.events.Length > 0 ? root.events[0] : null;
            if (evt == null || string.IsNullOrWhiteSpace(evt.id))
            {
                continue;
            }

            merged[evt.id] = new EventListEntry
            {
                Event = evt,
                IsDraft = true,
                SourceLabel = "草稿覆盖"
            };
        }

        if (EditorApplication.isPlaying && EventScheduler.Instance != null)
        {
            List<EventDefinition> runtimeEvents = EventScheduler.Instance.GetAllEventsSnapshot();
            for (int i = 0; i < runtimeEvents.Count; i++)
            {
                EventDefinition evt = runtimeEvents[i];
                if (evt == null || string.IsNullOrWhiteSpace(evt.id) || merged.ContainsKey(evt.id))
                {
                    continue;
                }

                merged[evt.id] = new EventListEntry
                {
                    Event = CloneEvent(evt),
                    IsDraft = false,
                    SourceLabel = "运行时"
                };
            }
        }

        return merged.Values.OrderBy(entry => entry.Event.id).ToList();
    }

    private List<EventDefinition> LoadEditorResourceEvents()
    {
        string[] assetPaths =
        {
            "Assets/Resources/Data/Events/main_events.json",
            "Assets/Resources/Data/Events/fixed_events.json",
            "Assets/Resources/Data/Events/conditional_events.json",
            "Assets/Resources/Data/Events/dark_events.json"
        };

        List<EventDefinition> results = new List<EventDefinition>();
        for (int i = 0; i < assetPaths.Length; i++)
        {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPaths[i]);
            if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
            {
                continue;
            }

            EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(textAsset.text);
            if (root == null || root.events == null)
            {
                continue;
            }

            for (int j = 0; j < root.events.Length; j++)
            {
                EventDefinition evt = root.events[j];
                if (evt != null)
                {
                    results.Add(CloneEvent(evt));
                }
            }
        }

        return results;
    }

    private List<EventListEntry> GetRoundCandidates(List<EventListEntry> allEntries, int year, int semester, int round)
    {
        return allEntries
            .Where(entry => entry.Event != null)
            .Where(entry => !MatchesTimelineWindow(entry.Event, year, semester, round))
            .OrderBy(entry => entry.Event.eventType)
            .ThenBy(entry => entry.Event.id)
            .ToList();
    }

    private bool MatchesEventSearch(EventListEntry entry, string keyword)
    {
        if (entry == null || entry.Event == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        string haystack = $"{entry.Event.id} {entry.Event.title} {entry.Event.eventType} {GetEventTypeDisplayName(entry.Event.eventType)} {entry.SourceLabel}".ToLowerInvariant();
        return haystack.Contains(keyword.Trim().ToLowerInvariant());
    }

    private string ResolveEventSourceLabel(string eventId)
    {
        if (ZhongshanDeckToolStateBridge.TryGetAuthoredEvent(eventId, out _))
        {
            return "草稿";
        }

        return EditorApplication.isPlaying && EventScheduler.Instance != null && EventScheduler.Instance.GetEvent(eventId) != null
            ? "运行时"
            : "资源事件";
    }

    private string GetSelectedTimelinePhase()
    {
        string selected = EventTimelinePhaseOptions[Mathf.Clamp(eventTimelinePhaseIndex, 0, EventTimelinePhaseOptions.Length - 1)];
        return selected == "All" ? EventPhaseOptions[0] : selected;
    }

    private string GetEventPhase(EventDefinition evt)
    {
        if (evt == null)
        {
            return "RoundStart";
        }

        if (evt.trigger != null && !string.IsNullOrWhiteSpace(evt.trigger.phase))
        {
            return evt.trigger.phase;
        }

        return evt.eventType == "Conditional" || evt.eventType == "Dark" ? "RoundEnd" : "RoundStart";
    }

    private bool MatchesTimelineWindow(EventDefinition evt, int year, int semester, int round)
    {
        if (evt == null)
        {
            return false;
        }

        EventTriggerCondition trigger = evt.trigger;
        if (trigger == null)
        {
            return true;
        }

        if (trigger.year > 0 && trigger.year != year) return false;
        if (trigger.semester > 0 && trigger.semester != semester) return false;
        if (trigger.roundMin > 0 && round < trigger.roundMin) return false;
        if (trigger.roundMax > 0 && round > trigger.roundMax) return false;
        if (trigger.specificRounds != null && trigger.specificRounds.Length > 0 && !trigger.specificRounds.Contains(round)) return false;
        return true;
    }

    private void EnsureRoundBinding(EventDefinition evt, int year, int semester, int round, string phase)
    {
        if (evt.trigger == null)
        {
            evt.trigger = new EventTriggerCondition();
        }

        evt.trigger.year = year;
        evt.trigger.semester = semester;
        evt.trigger.phase = phase;
        evt.trigger.roundMin = 0;
        evt.trigger.roundMax = 0;

        List<int> rounds = evt.trigger.specificRounds != null ? new List<int>(evt.trigger.specificRounds) : new List<int>();
        if (!rounds.Contains(round))
        {
            rounds.Add(round);
        }

        rounds.Sort();
        evt.trigger.specificRounds = rounds.ToArray();
    }

    private void RemoveRoundBinding(EventDefinition evt, int year, int semester, int round)
    {
        if (evt == null || evt.trigger == null)
        {
            return;
        }

        if (evt.trigger.year > 0 && evt.trigger.year != year) return;
        if (evt.trigger.semester > 0 && evt.trigger.semester != semester) return;

        List<int> rounds = evt.trigger.specificRounds != null ? new List<int>(evt.trigger.specificRounds) : new List<int>();
        rounds.RemoveAll(value => value == round);
        evt.trigger.specificRounds = rounds.ToArray();
    }

    private string BuildTimeWindowLabel(EventTriggerCondition trigger)
    {
        if (trigger == null)
        {
            return "不限";
        }

        List<string> bits = new List<string>();
        if (trigger.year > 0) bits.Add($"Y{trigger.year}");
        if (trigger.semester > 0) bits.Add($"S{trigger.semester}");
        if (trigger.specificRounds != null && trigger.specificRounds.Length > 0)
        {
            bits.Add($"R[{string.Join(",", trigger.specificRounds)}]");
        }
        else if (trigger.roundMin > 0 || trigger.roundMax > 0)
        {
            bits.Add($"R{trigger.roundMin}-{trigger.roundMax}");
        }

        return bits.Count == 0 ? "不限" : string.Join(" ", bits);
    }

    private int GetPrimaryRound(EventTriggerCondition trigger)
    {
        if (trigger == null)
        {
            return int.MaxValue;
        }

        if (trigger.specificRounds != null && trigger.specificRounds.Length > 0)
        {
            return trigger.specificRounds.Min();
        }

        return trigger.roundMin > 0 ? trigger.roundMin : int.MaxValue - 1;
    }

    private EventDefinition CloneEvent(EventDefinition evt)
    {
        if (evt == null)
        {
            return null;
        }

        string json = JsonUtility.ToJson(new EventDatabaseRoot { events = new[] { evt } });
        EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(json);
        return root != null && root.events != null && root.events.Length > 0 ? root.events[0] : evt;
    }

    private void DrawEventDraftSummary()
    {
        List<ZhongshanDeckEventEntry> drafts = ZhongshanDeckToolStateBridge.GetAuthoredEvents();
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("已有草稿（列表入口请回到上方事件列表）", EditorStyles.boldLabel);

        if (drafts.Count == 0)
        {
            EditorGUILayout.HelpBox("还没有剧情草稿。", MessageType.None);
            return;
        }

        for (int i = 0; i < drafts.Count; i++)
        {
            ZhongshanDeckEventEntry draft = drafts[i];
            if (draft == null)
            {
                continue;
            }

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                EditorGUILayout.LabelField($"{draft.eventId} | {draft.title}");
            }
        }
    }

    private void DrawEventDraftPreview()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("当前草稿预览", EditorStyles.boldLabel);
        if (!TryBuildEditorEventDefinition(out EventDefinition evt, out string error))
        {
            EditorGUILayout.HelpBox(error, MessageType.None);
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"{evt.id} | {evt.title}");
        builder.AppendLine($"类型 {GetEventTypeDisplayName(evt.eventType)} | 阶段 {GetEventPhaseDisplayName(evt.trigger.phase)} | 优先级 {evt.priority}");
        builder.AppendLine($"指定回合 {string.Join(",", evt.trigger.specificRounds ?? Array.Empty<int>())} | 概率 {evt.trigger.triggerChance:0.##}");
        builder.AppendLine($"属性条件 {FormatAttributeConditions(evt.trigger.attributeConditions)}");
        builder.AppendLine($"前置 {string.Join(",", evt.trigger.requiredEventIds ?? Array.Empty<string>())}");
        builder.AppendLine($"排除 {string.Join(",", evt.trigger.excludedEventIds ?? Array.Empty<string>())}");
        builder.AppendLine("对话");

        if (evt.dialogues != null && evt.dialogues.Length > 0)
        {
            for (int i = 0; i < evt.dialogues[0].lines.Length; i++)
            {
                builder.AppendLine($"- {evt.dialogues[0].speaker}: {evt.dialogues[0].lines[i]}");
            }
        }

        builder.AppendLine("选项");
        if (evt.choices == null || evt.choices.Length == 0)
        {
            builder.AppendLine("- 无，走默认效果");
        }
        else
        {
            for (int i = 0; i < evt.choices.Length; i++)
            {
                builder.AppendLine($"- {evt.choices[i].text} => {FormatEventEffects(evt.choices[i].effects)}");
            }
        }

        builder.AppendLine($"默认效果 {FormatEventEffects(evt.defaultEffects)}");
        EditorGUILayout.TextArea(builder.ToString(), GUILayout.MinHeight(180f));
    }

    private string[] ParseCommaList(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string>();
        }

        string[] raw = input.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> values = new List<string>();
        for (int i = 0; i < raw.Length; i++)
        {
            string trimmed = raw[i].Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                values.Add(trimmed);
            }
        }

        return values.ToArray();
    }

    private int[] ParseIntList(string input)
    {
        string[] raw = ParseCommaList(input);
        List<int> values = new List<int>();
        for (int i = 0; i < raw.Length; i++)
        {
            if (int.TryParse(raw[i], out int value))
            {
                values.Add(value);
            }
        }

        return values.ToArray();
    }

    private void EnsureEventDraftCollections()
    {
        if (eventDialogueLines.Count == 0)
        {
            eventDialogueLines.Add(string.Empty);
        }

        for (int i = 0; i < eventChoiceDrafts.Count; i++)
        {
            if (eventChoiceDrafts[i] != null && eventChoiceDrafts[i].effects == null)
            {
                eventChoiceDrafts[i].effects = new List<EventEffectDraft>();
            }
        }
    }

    private AttributeCondition[] BuildAttributeConditions()
    {
        if (eventAttributeConditionDrafts.Count == 0)
        {
            return Array.Empty<AttributeCondition>();
        }

        List<AttributeCondition> conditions = new List<AttributeCondition>();
        for (int i = 0; i < eventAttributeConditionDrafts.Count; i++)
        {
            EventAttributeConditionDraft draft = eventAttributeConditionDrafts[i];
            if (draft == null)
            {
                continue;
            }

            conditions.Add(new AttributeCondition
            {
                attributeName = EventAttributeOptions[Mathf.Clamp(draft.attributeIndex, 0, EventAttributeOptions.Length - 1)],
                comparison = EventComparisonOptions[Mathf.Clamp(draft.comparisonIndex, 0, EventComparisonOptions.Length - 1)],
                value = draft.value
            });
        }

        return conditions.ToArray();
    }

    private EventEffect[] BuildEventEffects(List<EventEffectDraft> drafts)
    {
        if (drafts == null || drafts.Count == 0)
        {
            return Array.Empty<EventEffect>();
        }

        List<EventEffect> effects = new List<EventEffect>();
        for (int i = 0; i < drafts.Count; i++)
        {
            EventEffectDraft draft = drafts[i];
            if (draft == null)
            {
                continue;
            }

            string type = EventEffectTypeOptions[Mathf.Clamp(draft.effectTypeIndex, 0, EventEffectTypeOptions.Length - 1)];
            string target = type == "attribute"
                ? EventAttributeOptions[Mathf.Clamp(draft.attributeIndex, 0, EventAttributeOptions.Length - 1)]
                : (draft.targetText ?? string.Empty).Trim();

            effects.Add(new EventEffect
            {
                type = type,
                target = target,
                value = draft.value,
                description = BuildEffectDescription(type, target, draft.value)
            });
        }

        return effects.ToArray();
    }

    private void FillAttributeConditionDrafts(AttributeCondition[] conditions)
    {
        eventAttributeConditionDrafts.Clear();
        if (conditions == null)
        {
            return;
        }

        for (int i = 0; i < conditions.Length; i++)
        {
            AttributeCondition condition = conditions[i];
            if (condition == null)
            {
                continue;
            }

            eventAttributeConditionDrafts.Add(new EventAttributeConditionDraft
            {
                attributeIndex = Mathf.Max(0, Array.IndexOf(EventAttributeOptions, condition.attributeName ?? string.Empty)),
                comparisonIndex = Mathf.Max(0, Array.IndexOf(EventComparisonOptions, condition.comparison ?? string.Empty)),
                value = condition.value
            });
        }
    }

    private void FillEffectDrafts(List<EventEffectDraft> drafts, EventEffect[] effects)
    {
        drafts.Clear();
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Length; i++)
        {
            EventEffect effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            string type = string.IsNullOrWhiteSpace(effect.type) ? "attribute" : effect.type;
            EventEffectDraft draft = new EventEffectDraft
            {
                effectTypeIndex = Mathf.Max(0, Array.IndexOf(EventEffectTypeOptions, type)),
                value = effect.value
            };

            if (type == "attribute")
            {
                draft.attributeIndex = Mathf.Max(0, Array.IndexOf(EventAttributeOptions, effect.target ?? string.Empty));
            }
            else
            {
                draft.targetText = effect.target ?? string.Empty;
            }

            drafts.Add(draft);
        }
    }

    private string BuildEffectDescription(string type, string target, int value)
    {
        switch (type)
        {
            case "attribute":
                return $"{target} {(value >= 0 ? "+" : string.Empty)}{value}";
            case "money":
                return $"金钱 {(value >= 0 ? "+" : string.Empty)}{value}";
            case "flag":
                return $"标记 {target} = {(value != 0)}";
            case "darkness":
                return $"黑暗值 {(value >= 0 ? "+" : string.Empty)}{value}";
            case "unlock":
                return $"解锁 {target}";
            default:
                return $"{type}:{target}:{value}";
        }
    }

    private string FormatAttributeConditions(AttributeCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
        {
            return "-";
        }

        List<string> items = new List<string>();
        for (int i = 0; i < conditions.Length; i++)
        {
            AttributeCondition condition = conditions[i];
            if (condition == null)
            {
                continue;
            }

            items.Add($"{condition.attributeName}{condition.comparison}{condition.value}");
        }

        return items.Count == 0 ? "-" : string.Join(", ", items);
    }

    private string FormatEventEffects(EventEffect[] effects)
    {
        if (effects == null || effects.Length == 0)
        {
            return "无";
        }

        List<string> items = new List<string>();
        for (int i = 0; i < effects.Length; i++)
        {
            EventEffect effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            items.Add(BuildEffectDescription(effect.type, effect.target, effect.value));
        }

        return items.Count == 0 ? "无" : string.Join("; ", items);
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

    private void DrawNews()
    {
        EditorGUILayout.LabelField("每月新闻编辑", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里维护的是“月度新闻覆盖稿”。保存后，游戏运行到该学年/学期/回合时，会优先使用这里的新闻内容。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            newsYearInput = EditorGUILayout.IntField("学年", newsYearInput, GUILayout.Width(170f));
            newsSemesterInput = EditorGUILayout.IntField("学期", newsSemesterInput, GUILayout.Width(170f));
            newsRoundInput = EditorGUILayout.IntField("回合", newsRoundInput, GUILayout.Width(170f));
        }

        newsYearInput = Mathf.Clamp(newsYearInput, 1, 4);
        newsSemesterInput = Mathf.Clamp(newsSemesterInput, 1, 2);
        newsRoundInput = Mathf.Clamp(newsRoundInput, 1, 5);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("使用当前时间", GUILayout.Width(120f)))
            {
                UseCurrentNewsTime();
                LoadMonthlyNewsOverrideOrGenerate();
            }

            if (GUILayout.Button("载入草稿", GUILayout.Width(100f)))
            {
                LoadMonthlyNewsOverrideOrGenerate();
            }

            if (GUILayout.Button("导入默认稿", GUILayout.Width(100f)))
            {
                ImportGeneratedNewsForEditor();
            }

            if (GUILayout.Button("新增条目", GUILayout.Width(100f)))
            {
                editingNewsItems.Add(new NewsItem(NewsType.Headline, "新头条", "请填写本月新闻内容。"));
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存本月覆盖", GUILayout.Width(120f)))
            {
                SaveMonthlyNewsOverride();
            }

            if (GUILayout.Button("删除本月覆盖", GUILayout.Width(120f)))
            {
                DeleteMonthlyNewsOverride();
            }
        }

        DrawNewsRoundPicker();

        if (!string.IsNullOrEmpty(newsEditorStatus))
        {
            EditorGUILayout.HelpBox(newsEditorStatus, MessageType.None);
        }

        if (editingNewsItems.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有新闻条目。可以先“载入草稿”或“导入默认稿”。", MessageType.None);
            return;
        }

        DrawNewsVisualPreview();

        for (int i = 0; i < editingNewsItems.Count; i++)
        {
            DrawNewsItemEditor(i);
        }
    }

    private void DrawContent()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("场景感知内容编辑", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里会根据当前打开的场景，自动切换到标题页布局编辑或存档布局编辑。锁定、位置、尺寸等字段统一放在这里管理。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("打开 TitleScreen", GUILayout.Width(140f)) && HasSceneAsset(TitleScenePath))
            {
                OpenSceneByPath(TitleScenePath);
            }

            if (GUILayout.Button("打开 SaveLoadPreview", GUILayout.Width(160f)) && HasSceneAsset(SaveLoadPreviewScenePath))
            {
                OpenSceneByPath(SaveLoadPreviewScenePath);
            }

            if (GUILayout.Button("定位状态资产", GUILayout.Width(140f)))
            {
                Selection.activeObject = ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            if (GUILayout.Button("恢复默认内容", GUILayout.Width(140f)))
            {
                ZhongshanDeckToolState state = ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
                state.titleContent = new ZhongshanDeckTitleContent();
                state.titleContent.EnsureInitialized();
                EditorUtility.SetDirty(state);
                AssetDatabase.SaveAssets();
                Repaint();
            }
        }

        ZhongshanDeckToolState asset = ZhongshanDeckEditorStateUtility.GetOrCreateStateAsset();
        SerializedObject serializedObject = new SerializedObject(asset);
        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        SerializedProperty activeContentProperty = null;
        GUIContent activeContentLabel = null;

        if (string.Equals(activeScenePath, TitleScenePath, StringComparison.OrdinalIgnoreCase))
        {
            activeContentProperty = serializedObject.FindProperty("titleContent");
            activeContentLabel = new GUIContent("标题页共享内容");
        }
        else if (string.Equals(activeScenePath, SaveLoadPreviewScenePath, StringComparison.OrdinalIgnoreCase))
        {
            activeContentProperty = serializedObject.FindProperty("saveLoadContent");
            activeContentLabel = new GUIContent("存档页共享内容");
        }
        else
        {
            activeContentProperty = serializedObject.FindProperty("titleContent");
            activeContentLabel = new GUIContent("标题页共享内容");
        }

        if (activeContentProperty == null)
        {
            EditorGUILayout.HelpBox("未找到当前场景对应的内容字段。", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(6f);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(activeContentProperty, activeContentLabel, true);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            asset.EnsureInitialized();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
        else
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        asset.EnsureInitialized();

        EditorGUILayout.Space(10f);
        DrawSceneAwareLayoutEditor(asset);

        EditorGUILayout.Space(8f);
        if (EditorApplication.isPlaying)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("通知标题界面立即重载", GUILayout.Width(180f)))
                {
                    TitleScreenManager[] managers = FindObjectsOfType<TitleScreenManager>();
                    for (int i = 0; i < managers.Length; i++)
                    {
                        managers[i].DebugReloadAuthoredTitleContent();
                    }
                }

                if (GUILayout.Button("打开游戏内内容模块", GUILayout.Width(180f)))
                {
                    DebugConsoleManager.Instance?.Open();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("进入 Play 模式后，可以在这里通知当前标题界面即时回刷内容。", MessageType.None);
        }
    }

    private void DrawSceneAwareLayoutEditor(ZhongshanDeckToolState asset)
    {
        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.Equals(activeScenePath, TitleScenePath, StringComparison.OrdinalIgnoreCase))
        {
            DrawHomepageLayoutEditor(asset);
            return;
        }

        if (string.Equals(activeScenePath, SaveLoadPreviewScenePath, StringComparison.OrdinalIgnoreCase))
        {
            DrawSaveLoadLayoutEditor(asset);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("布局编辑入口", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("当前场景没有对应的布局编辑内容。切到 TitleScreen 或 SaveLoadPreview 后，这里会自动显示对应字段。", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("切到标题布局", GUILayout.Width(120f)))
                {
                    OpenTitleScenePreview();
                }

                if (GUILayout.Button("切到存档布局", GUILayout.Width(120f)))
                {
                    OpenSaveLoadScenePreview();
                }
            }
        }
    }

    private void DrawHomepageLayoutEditor(ZhongshanDeckToolState asset)
    {
        if (asset == null)
        {
            return;
        }

        asset.EnsureInitialized();
        ZhongshanDeckHomepageContent homepage = asset.titleContent?.homepage;
        if (homepage == null)
        {
            EditorGUILayout.HelpBox("未找到首页内容。", MessageType.Warning);
            return;
        }

        ZhongshanDeckTitleContentDefaults.EnsureHomepageLayoutItems(homepage.layoutItems);
        ZhongshanDeckHomepageLayoutItem selectedItem = GetSelectedHomepageLayoutItem(homepage);
        if (selectedItem == null && homepage.layoutItems.Count > 0)
        {
            selectedItem = homepage.layoutItems[0];
            selectedHomepageLayoutKey = selectedItem.key;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("首页布局可视化编辑", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("拖动物块可改位置，拖右下角小方块可改尺寸。坐标基于 1920x1080 标题页参考分辨率。", EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重置首页布局", GUILayout.Width(120f)))
                {
                    homepage.layoutItems.Clear();
                    ZhongshanDeckTitleContentDefaults.EnsureHomepageLayoutItems(homepage.layoutItems);
                    selectedHomepageLayoutKey = ZhongshanDeckTitleContentDefaults.LayoutLogo;
                    SaveTitleAsset(asset);
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("定位 TitleScreen 场景", GUILayout.Width(140f)) && HasSceneAsset(TitleScenePath))
                {
                    OpenSceneByPath(TitleScenePath);
                }

                GUILayout.FlexibleSpace();
            }

            Rect previewRect = GUILayoutUtility.GetRect(720f, 450f, GUILayout.ExpandWidth(true));
            DrawHomepageLayoutPreview(previewRect, homepage, asset);
            selectedItem = GetSelectedHomepageLayoutItem(homepage);

            EditorGUILayout.Space(8f);
            DrawHomepageLayoutInspector(selectedItem, asset);
        }
    }

    private void DrawSaveLoadLayoutEditor(ZhongshanDeckToolState asset)
    {
        if (asset == null)
        {
            return;
        }

        asset.EnsureInitialized();
        ZhongshanDeckSaveLoadContent content = asset.saveLoadContent;
        if (content == null)
        {
            EditorGUILayout.HelpBox("未找到存档布局内容。", MessageType.Warning);
            return;
        }

        content.EnsureInitialized();
        ZhongshanDeckSaveLoadLayoutItem selectedItem = GetSelectedSaveLoadLayoutItem(content);
        if (selectedItem == null && content.layoutItems.Count > 0)
        {
            selectedItem = content.layoutItems[0];
            selectedSaveLoadLayoutKey = selectedItem.key;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("存档布局可视化编辑", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前处于 SaveLoadPreview 场景。位置锁定、显示、锚点、尺寸统一在这里编辑。", EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重置存档布局", GUILayout.Width(120f)))
                {
                    content.layoutItems.Clear();
                    ZhongshanDeckSaveLoadContentDefaults.EnsureLayoutItems(content.layoutItems);
                    selectedSaveLoadLayoutKey = ZhongshanDeckSaveLoadContentDefaults.LayoutBoard;
                    SaveToolStateAsset(asset);
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("定位 SaveLoadPreview 场景", GUILayout.Width(170f)) && HasSceneAsset(SaveLoadPreviewScenePath))
                {
                    OpenSceneByPath(SaveLoadPreviewScenePath);
                }

                GUILayout.FlexibleSpace();
            }

            Rect previewRect = GUILayoutUtility.GetRect(720f, 450f, GUILayout.ExpandWidth(true));
            DrawSaveLoadLayoutPreview(previewRect, content, asset);
            selectedItem = GetSelectedSaveLoadLayoutItem(content);

            EditorGUILayout.Space(8f);
            DrawSaveLoadLayoutInspector(selectedItem, asset);
        }
    }

    private void DrawHomepageLayoutPreview(Rect previewRect, ZhongshanDeckHomepageContent homepage, ZhongshanDeckToolState asset)
    {
        Rect canvasRect = FitRectWithAspect(previewRect, 16f / 9f);
        EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.11f, 0.14f, 1f));
        EditorGUI.DrawRect(canvasRect, new Color(0.17f, 0.2f, 0.24f, 1f));
        DrawPreviewGrid(canvasRect, 6, 4, new Color(1f, 1f, 1f, 0.08f));
        GUI.Box(canvasRect, GUIContent.none);

        Event evt = Event.current;
        float scale = canvasRect.width / 1920f;
        ZhongshanDeckHomepageLayoutItem clickedItem = null;
        bool changed = false;

        for (int i = homepage.layoutItems.Count - 1; i >= 0; i--)
        {
            ZhongshanDeckHomepageLayoutItem item = homepage.layoutItems[i];
            if (item == null)
            {
                continue;
            }

            item.EnsureInitialized();
            Rect itemRect = GetHomepagePreviewRect(item, canvasRect);
            bool isSelected = string.Equals(selectedHomepageLayoutKey, item.key, StringComparison.Ordinal);
            Color fill = isSelected ? new Color(0.98f, 0.72f, 0.22f, 0.35f) : new Color(0.3f, 0.6f, 0.95f, 0.25f);
            Color outline = isSelected ? new Color(0.98f, 0.8f, 0.32f, 1f) : new Color(0.48f, 0.72f, 0.98f, 0.9f);
            EditorGUI.DrawRect(itemRect, fill);
            Handles.color = outline;
            Handles.DrawSolidRectangleWithOutline(itemRect, Color.clear, outline);
            DrawPreviewLabel(itemRect, string.IsNullOrWhiteSpace(item.displayName) ? item.key : item.displayName, isSelected);

            Rect resizeHandle = new Rect(itemRect.xMax - 12f, itemRect.yMax - 12f, 12f, 12f);
            EditorGUI.DrawRect(resizeHandle, outline);

            if (evt.type == UnityEngine.EventType.MouseDown && evt.button == 0)
            {
                if (resizeHandle.Contains(evt.mousePosition))
                {
                    clickedItem = item;
                    selectedHomepageLayoutKey = item.key;
                    isHomepageLayoutDragging = false;
                    isHomepageLayoutResizing = true;
                    homepageLayoutDragStartMouse = evt.mousePosition;
                    homepageLayoutDragStartSize = item.size;
                    evt.Use();
                }
                else if (itemRect.Contains(evt.mousePosition))
                {
                    clickedItem = item;
                    selectedHomepageLayoutKey = item.key;
                    isHomepageLayoutDragging = true;
                    isHomepageLayoutResizing = false;
                    homepageLayoutDragStartMouse = evt.mousePosition;
                    homepageLayoutDragStartPosition = item.anchoredPosition;
                    evt.Use();
                }
            }
        }

        ZhongshanDeckHomepageLayoutItem selectedItem = GetSelectedHomepageLayoutItem(homepage);
        if (selectedItem != null)
        {
            if (evt.type == UnityEngine.EventType.MouseDrag && (isHomepageLayoutDragging || isHomepageLayoutResizing))
            {
                Vector2 delta = (evt.mousePosition - homepageLayoutDragStartMouse) / scale;
                if (isHomepageLayoutDragging)
                {
                    selectedItem.anchoredPosition = homepageLayoutDragStartPosition + new Vector2(delta.x, -delta.y);
                }
                else if (isHomepageLayoutResizing)
                {
                    selectedItem.size = new Vector2(
                        Mathf.Max(24f, homepageLayoutDragStartSize.x + delta.x),
                        Mathf.Max(24f, homepageLayoutDragStartSize.y + delta.y));
                }

                changed = true;
                evt.Use();
            }

            if (evt.type == UnityEngine.EventType.MouseUp)
            {
                isHomepageLayoutDragging = false;
                isHomepageLayoutResizing = false;
            }
        }

        if (clickedItem == null && evt.type == UnityEngine.EventType.MouseDown && evt.button == 0 && canvasRect.Contains(evt.mousePosition))
        {
            Repaint();
        }

        if (changed)
        {
            SaveTitleAsset(asset);
            Repaint();
        }
    }

    private void DrawHomepageLayoutInspector(ZhongshanDeckHomepageLayoutItem selectedItem, ZhongshanDeckToolState asset)
    {
        if (selectedItem == null)
        {
            EditorGUILayout.HelpBox("先在上面的预览里点一个模块。", MessageType.None);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField($"当前模块：{selectedItem.displayName}", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedItem.locked = EditorGUILayout.Toggle("锁定编辑", selectedItem.locked);
            selectedItem.visible = EditorGUILayout.Toggle("显示", selectedItem.visible);
            selectedItem.anchor = (ZhongshanDeckLayoutAnchor)EditorGUILayout.EnumPopup("锚点", selectedItem.anchor);
            selectedItem.anchoredPosition = EditorGUILayout.Vector2Field("位置", selectedItem.anchoredPosition);
            selectedItem.size = EditorGUILayout.Vector2Field("尺寸", selectedItem.size);
            selectedItem.size = new Vector2(Mathf.Max(24f, selectedItem.size.x), Mathf.Max(24f, selectedItem.size.y));
            if (EditorGUI.EndChangeCheck())
            {
                SaveTitleAsset(asset);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("锁定其他项", GUILayout.Width(100f)))
                {
                    SetOnlyHomepageItemEditable(asset, selectedItem.key);
                }

                if (GUILayout.Button("全部解锁", GUILayout.Width(100f)))
                {
                    SetAllHomepageItemsLocked(asset, false);
                }

                if (GUILayout.Button("保存布局", GUILayout.Width(100f)))
                {
                    SaveTitleAsset(asset);
                }

                if (GUILayout.Button("播放中通知标题页刷新", GUILayout.Width(160f)))
                {
                    if (EditorApplication.isPlaying)
                    {
                        TitleScreenManager[] managers = FindObjectsOfType<TitleScreenManager>();
                        for (int i = 0; i < managers.Length; i++)
                        {
                            managers[i].DebugReloadAuthoredTitleContent();
                        }
                    }
                }
            }
        }
    }

    private void DrawSaveLoadLayoutPreview(Rect previewRect, ZhongshanDeckSaveLoadContent content, ZhongshanDeckToolState asset)
    {
        Rect canvasRect = FitRectWithAspect(previewRect, 16f / 9f);
        EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.11f, 0.14f, 1f));
        EditorGUI.DrawRect(canvasRect, new Color(0.17f, 0.2f, 0.24f, 1f));
        DrawPreviewGrid(canvasRect, 6, 4, new Color(1f, 1f, 1f, 0.08f));
        GUI.Box(canvasRect, GUIContent.none);

        Event evt = Event.current;
        float scale = canvasRect.width / 1920f;
        ZhongshanDeckSaveLoadLayoutItem clickedItem = null;
        bool changed = false;

        for (int i = content.layoutItems.Count - 1; i >= 0; i--)
        {
            ZhongshanDeckSaveLoadLayoutItem item = content.layoutItems[i];
            if (item == null)
            {
                continue;
            }

            item.EnsureInitialized();
            Rect itemRect = GetSaveLoadPreviewRect(item, canvasRect);
            bool isSelected = string.Equals(selectedSaveLoadLayoutKey, item.key, StringComparison.Ordinal);
            bool isLocked = item.locked;
            Color fill = isSelected ? new Color(0.98f, 0.72f, 0.22f, 0.35f) : new Color(0.3f, 0.6f, 0.95f, 0.25f);
            Color outline = isLocked
                ? new Color(0.95f, 0.42f, 0.42f, 0.95f)
                : (isSelected ? new Color(0.98f, 0.8f, 0.32f, 1f) : new Color(0.48f, 0.72f, 0.98f, 0.9f));
            EditorGUI.DrawRect(itemRect, fill);
            Handles.color = outline;
            Handles.DrawSolidRectangleWithOutline(itemRect, Color.clear, outline);
            DrawPreviewLabel(itemRect, isLocked ? $"{GetSaveLoadPreviewLabel(item)} [锁]" : GetSaveLoadPreviewLabel(item), isSelected);

            Rect resizeHandle = new Rect(itemRect.xMax - 12f, itemRect.yMax - 12f, 12f, 12f);
            if (!isLocked)
            {
                EditorGUI.DrawRect(resizeHandle, outline);
            }

            if (evt.type == UnityEngine.EventType.MouseDown && evt.button == 0)
            {
                if (resizeHandle.Contains(evt.mousePosition) && !isLocked)
                {
                    clickedItem = item;
                    selectedSaveLoadLayoutKey = item.key;
                    isSaveLoadLayoutDragging = false;
                    isSaveLoadLayoutResizing = true;
                    saveLoadLayoutDragStartMouse = evt.mousePosition;
                    saveLoadLayoutDragStartSize = item.size;
                    evt.Use();
                }
                else if (itemRect.Contains(evt.mousePosition))
                {
                    clickedItem = item;
                    selectedSaveLoadLayoutKey = item.key;
                    if (!isLocked)
                    {
                        isSaveLoadLayoutDragging = true;
                        isSaveLoadLayoutResizing = false;
                        saveLoadLayoutDragStartMouse = evt.mousePosition;
                        saveLoadLayoutDragStartPosition = item.anchoredPosition;
                    }
                    evt.Use();
                }
            }
        }

        ZhongshanDeckSaveLoadLayoutItem selectedItem = GetSelectedSaveLoadLayoutItem(content);
        if (selectedItem != null)
        {
            if (evt.type == UnityEngine.EventType.MouseDrag && (isSaveLoadLayoutDragging || isSaveLoadLayoutResizing))
            {
                Vector2 delta = (evt.mousePosition - saveLoadLayoutDragStartMouse) / scale;
                if (isSaveLoadLayoutDragging)
                {
                    selectedItem.anchoredPosition = saveLoadLayoutDragStartPosition + new Vector2(delta.x, -delta.y);
                }
                else if (isSaveLoadLayoutResizing)
                {
                    selectedItem.size = new Vector2(
                        Mathf.Max(24f, saveLoadLayoutDragStartSize.x + delta.x),
                        Mathf.Max(24f, saveLoadLayoutDragStartSize.y + delta.y));
                }

                changed = true;
                evt.Use();
            }

            if (evt.type == UnityEngine.EventType.MouseUp)
            {
                isSaveLoadLayoutDragging = false;
                isSaveLoadLayoutResizing = false;
            }
        }

        if (clickedItem == null && evt.type == UnityEngine.EventType.MouseDown && evt.button == 0 && canvasRect.Contains(evt.mousePosition))
        {
            Repaint();
        }

        if (changed)
        {
            SaveToolStateAsset(asset);
            Repaint();
        }
    }

    private void DrawSaveLoadLayoutInspector(ZhongshanDeckSaveLoadLayoutItem selectedItem, ZhongshanDeckToolState asset)
    {
        if (selectedItem == null)
        {
            EditorGUILayout.HelpBox("先在上面的预览里点一个模块。", MessageType.None);
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField($"当前模块：{selectedItem.displayName}", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedItem.locked = EditorGUILayout.Toggle("锁定编辑", selectedItem.locked);
            selectedItem.visible = EditorGUILayout.Toggle("显示", selectedItem.visible);
            selectedItem.anchor = (ZhongshanDeckLayoutAnchor)EditorGUILayout.EnumPopup("锚点", selectedItem.anchor);
            selectedItem.anchoredPosition = EditorGUILayout.Vector2Field("位置", selectedItem.anchoredPosition);
            selectedItem.size = EditorGUILayout.Vector2Field("尺寸", selectedItem.size);
            selectedItem.size = new Vector2(Mathf.Max(24f, selectedItem.size.x), Mathf.Max(24f, selectedItem.size.y));
            if (EditorGUI.EndChangeCheck())
            {
                SaveToolStateAsset(asset);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("锁定其他项", GUILayout.Width(100f)))
                {
                    SetOnlySaveLoadItemEditable(asset, selectedItem.key);
                }

                if (GUILayout.Button("全部解锁", GUILayout.Width(100f)))
                {
                    SetAllSaveLoadItemsLocked(asset, false);
                }

                if (GUILayout.Button("保存布局", GUILayout.Width(100f)))
                {
                    SaveToolStateAsset(asset);
                }
            }
        }
    }

    private ZhongshanDeckHomepageLayoutItem GetSelectedHomepageLayoutItem(ZhongshanDeckHomepageContent homepage)
    {
        if (homepage?.layoutItems == null)
        {
            return null;
        }

        for (int i = 0; i < homepage.layoutItems.Count; i++)
        {
            ZhongshanDeckHomepageLayoutItem item = homepage.layoutItems[i];
            if (item != null && string.Equals(item.key, selectedHomepageLayoutKey, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private ZhongshanDeckSaveLoadLayoutItem GetSelectedSaveLoadLayoutItem(ZhongshanDeckSaveLoadContent content)
    {
        if (content?.layoutItems == null)
        {
            return null;
        }

        for (int i = 0; i < content.layoutItems.Count; i++)
        {
            ZhongshanDeckSaveLoadLayoutItem item = content.layoutItems[i];
            if (item != null && string.Equals(item.key, selectedSaveLoadLayoutKey, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private Rect GetHomepagePreviewRect(ZhongshanDeckHomepageLayoutItem item, Rect canvasRect)
    {
        Vector2 pivot = GetLayoutPivot(item.anchor);
        Vector2 anchorPoint = GetPreviewAnchorPoint(item.anchor, canvasRect);
        float scale = canvasRect.width / 1920f;
        Vector2 size = item.size * scale;
        Vector2 pivotGui = new Vector2(anchorPoint.x + item.anchoredPosition.x * scale, anchorPoint.y - item.anchoredPosition.y * scale);
        float left = pivotGui.x - size.x * pivot.x;
        float top = pivotGui.y - size.y * (1f - pivot.y);
        return new Rect(left, top, size.x, size.y);
    }

    private Rect GetSaveLoadPreviewRect(ZhongshanDeckSaveLoadLayoutItem item, Rect canvasRect)
    {
        Vector2 pivot = GetLayoutPivot(item.anchor);
        Vector2 anchorPoint = GetPreviewAnchorPoint(item.anchor, canvasRect);
        float scale = canvasRect.width / 1920f;
        Vector2 size = item.size * scale;
        Vector2 pivotGui = new Vector2(anchorPoint.x + item.anchoredPosition.x * scale, anchorPoint.y - item.anchoredPosition.y * scale);
        float left = pivotGui.x - size.x * pivot.x;
        float top = pivotGui.y - size.y * (1f - pivot.y);
        return new Rect(left, top, size.x, size.y);
    }

    private Rect FitRectWithAspect(Rect rect, float aspect)
    {
        float width = rect.width;
        float height = width / aspect;
        if (height > rect.height)
        {
            height = rect.height;
            width = height * aspect;
        }

        return new Rect(
            rect.x + (rect.width - width) * 0.5f,
            rect.y + (rect.height - height) * 0.5f,
            width,
            height);
    }

    private void DrawPreviewGrid(Rect rect, int columns, int rows, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;

        for (int i = 1; i < columns; i++)
        {
            float x = Mathf.Lerp(rect.xMin, rect.xMax, i / (float)columns);
            Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
        }

        for (int i = 1; i < rows; i++)
        {
            float y = Mathf.Lerp(rect.yMin, rect.yMax, i / (float)rows);
            Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
        }

        Handles.EndGUI();
    }

    private void DrawPreviewLabel(Rect rect, string label, bool selected)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = selected ? Color.white : new Color(0.96f, 0.96f, 0.96f, 0.92f);
        GUI.Label(rect, label, style);
    }

    private Vector2 GetPreviewAnchorPoint(ZhongshanDeckLayoutAnchor anchor, Rect canvasRect)
    {
        switch (anchor)
        {
            case ZhongshanDeckLayoutAnchor.TopLeft: return new Vector2(canvasRect.xMin, canvasRect.yMin);
            case ZhongshanDeckLayoutAnchor.TopCenter: return new Vector2(canvasRect.center.x, canvasRect.yMin);
            case ZhongshanDeckLayoutAnchor.TopRight: return new Vector2(canvasRect.xMax, canvasRect.yMin);
            case ZhongshanDeckLayoutAnchor.LeftCenter: return new Vector2(canvasRect.xMin, canvasRect.center.y);
            case ZhongshanDeckLayoutAnchor.RightCenter: return new Vector2(canvasRect.xMax, canvasRect.center.y);
            case ZhongshanDeckLayoutAnchor.BottomLeft: return new Vector2(canvasRect.xMin, canvasRect.yMax);
            case ZhongshanDeckLayoutAnchor.BottomCenter: return new Vector2(canvasRect.center.x, canvasRect.yMax);
            case ZhongshanDeckLayoutAnchor.BottomRight: return new Vector2(canvasRect.xMax, canvasRect.yMax);
            default: return canvasRect.center;
        }
    }

    private Vector2 GetLayoutPivot(ZhongshanDeckLayoutAnchor anchor)
    {
        switch (anchor)
        {
            case ZhongshanDeckLayoutAnchor.TopLeft: return new Vector2(0f, 1f);
            case ZhongshanDeckLayoutAnchor.TopCenter: return new Vector2(0.5f, 1f);
            case ZhongshanDeckLayoutAnchor.TopRight: return new Vector2(1f, 1f);
            case ZhongshanDeckLayoutAnchor.LeftCenter: return new Vector2(0f, 0.5f);
            case ZhongshanDeckLayoutAnchor.RightCenter: return new Vector2(1f, 0.5f);
            case ZhongshanDeckLayoutAnchor.BottomLeft: return new Vector2(0f, 0f);
            case ZhongshanDeckLayoutAnchor.BottomCenter: return new Vector2(0.5f, 0f);
            case ZhongshanDeckLayoutAnchor.BottomRight: return new Vector2(1f, 0f);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    private void SaveTitleAsset(ZhongshanDeckToolState asset)
    {
        if (asset == null)
        {
            return;
        }

        asset.EnsureInitialized();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private void SaveToolStateAsset(ZhongshanDeckToolState asset)
    {
        if (asset == null)
        {
            return;
        }

        asset.EnsureInitialized();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private void SetOnlyHomepageItemEditable(ZhongshanDeckToolState asset, string editableKey)
    {
        ZhongshanDeckHomepageContent homepage = asset?.titleContent?.homepage;
        if (homepage?.layoutItems == null)
        {
            return;
        }

        for (int i = 0; i < homepage.layoutItems.Count; i++)
        {
            ZhongshanDeckHomepageLayoutItem item = homepage.layoutItems[i];
            if (item != null)
            {
                item.locked = !string.Equals(item.key, editableKey, StringComparison.Ordinal);
            }
        }

        SaveToolStateAsset(asset);
    }

    private void SetAllHomepageItemsLocked(ZhongshanDeckToolState asset, bool locked)
    {
        ZhongshanDeckHomepageContent homepage = asset?.titleContent?.homepage;
        if (homepage?.layoutItems == null)
        {
            return;
        }

        for (int i = 0; i < homepage.layoutItems.Count; i++)
        {
            if (homepage.layoutItems[i] != null)
            {
                homepage.layoutItems[i].locked = locked;
            }
        }

        SaveToolStateAsset(asset);
    }

    private void SetOnlySaveLoadItemEditable(ZhongshanDeckToolState asset, string editableKey)
    {
        ZhongshanDeckSaveLoadContent content = asset?.saveLoadContent;
        if (content?.layoutItems == null)
        {
            return;
        }

        for (int i = 0; i < content.layoutItems.Count; i++)
        {
            ZhongshanDeckSaveLoadLayoutItem item = content.layoutItems[i];
            if (item != null)
            {
                item.locked = !string.Equals(item.key, editableKey, StringComparison.Ordinal);
            }
        }

        SaveToolStateAsset(asset);
    }

    private void SetAllSaveLoadItemsLocked(ZhongshanDeckToolState asset, bool locked)
    {
        ZhongshanDeckSaveLoadContent content = asset?.saveLoadContent;
        if (content?.layoutItems == null)
        {
            return;
        }

        for (int i = 0; i < content.layoutItems.Count; i++)
        {
            if (content.layoutItems[i] != null)
            {
                content.layoutItems[i].locked = locked;
            }
        }

        SaveToolStateAsset(asset);
    }

    private string GetSaveLoadPreviewLabel(ZhongshanDeckSaveLoadLayoutItem item)
    {
        return string.IsNullOrWhiteSpace(item?.displayName) ? item?.key ?? string.Empty : item.displayName;
    }

    private void DrawNewsItemEditor(int index)
    {
        if (index < 0 || index >= editingNewsItems.Count)
        {
            return;
        }

        NewsItem item = editingNewsItems[index] ?? new NewsItem();
        editingNewsItems[index] = item;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"条目 {index + 1}", EditorStyles.boldLabel, GUILayout.Width(60f));
                item.type = (NewsType)EditorGUILayout.EnumPopup(item.type, GUILayout.Width(120f));
                GUI.enabled = index > 0;
                if (GUILayout.Button("上移", GUILayout.Width(60f)))
                {
                    MoveNewsItem(index, -1);
                    return;
                }
                GUI.enabled = index < editingNewsItems.Count - 1;
                if (GUILayout.Button("下移", GUILayout.Width(60f)))
                {
                    MoveNewsItem(index, 1);
                    return;
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("删除", GUILayout.Width(70f)))
                {
                    editingNewsItems.RemoveAt(index);
                    return;
                }
            }

            item.title = EditorGUILayout.TextField("标题", item.title ?? string.Empty);
            EditorGUILayout.LabelField("内容");
            item.content = EditorGUILayout.TextArea(item.content ?? string.Empty, GUILayout.MinHeight(72f));

            using (new EditorGUILayout.HorizontalScope())
            {
                item.author = EditorGUILayout.TextField("作者", item.author ?? string.Empty);
                item.anonymousId = EditorGUILayout.TextField("匿名ID", item.anonymousId ?? string.Empty);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                item.hotTag = EditorGUILayout.TextField("热搜标签", item.hotTag ?? string.Empty);
                item.hotValue = EditorGUILayout.FloatField("热度", item.hotValue);
                item.likes = EditorGUILayout.IntField("点赞", item.likes);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                item.seriesId = EditorGUILayout.TextField("连载ID", item.seriesId ?? string.Empty);
                item.seriesOrder = EditorGUILayout.IntField("连载序号", item.seriesOrder);
            }
        }
    }

    private void DrawNewsVisualPreview()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("排版预览", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("下面是接近游戏里“校园新闻”呈现方式的可视化预览。编辑条目顺序会直接影响最终版面顺序。", MessageType.None);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            for (int i = 0; i < editingNewsItems.Count; i++)
            {
                NewsItem item = editingNewsItems[i];
                if (item == null)
                {
                    continue;
                }

                GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = item.type == NewsType.Headline ? 14 : 12,
                    wordWrap = true
                };
                GUIStyle bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    richText = true
                };

                using (new EditorGUILayout.VerticalScope("helpbox"))
                {
                    EditorGUILayout.LabelField($"{i + 1}. {GetNewsTypeDisplayName(item.type)}", EditorStyles.miniBoldLabel);

                    if (!string.IsNullOrWhiteSpace(item.title))
                    {
                        EditorGUILayout.LabelField(item.title, headerStyle);
                    }

                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(item.content) ? "暂无内容" : item.content, bodyStyle, GUILayout.MinHeight(32f));

                    string meta = BuildNewsPreviewMeta(item);
                    if (!string.IsNullOrWhiteSpace(meta))
                    {
                        EditorGUILayout.LabelField(meta, EditorStyles.centeredGreyMiniLabel);
                    }
                }
            }
        }
    }

    private void DrawNewsRoundPicker()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("点选回合", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("金色=当前选中，绿色=已有覆盖稿，蓝色=当前游戏回合。", MessageType.None);

        for (int year = 1; year <= 4; year++)
        {
            EditorGUILayout.LabelField($"大{ToChineseYear(year)}", EditorStyles.miniBoldLabel);
            for (int semester = 1; semester <= 2; semester++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(semester == 1 ? "上" : "下", GUILayout.Width(24f));
                    for (int round = 1; round <= 5; round++)
                    {
                        Color oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = GetNewsRoundButtonColor(year, semester, round);
                        if (GUILayout.Button($"R{round}", GUILayout.Width(48f)))
                        {
                            newsYearInput = year;
                            newsSemesterInput = semester;
                            newsRoundInput = round;
                            LoadMonthlyNewsOverrideOrGenerate();
                        }
                        GUI.backgroundColor = oldColor;
                    }
                }
            }
        }
    }

    private void UseCurrentNewsTime()
    {
        if (EditorApplication.isPlaying && GameState.Instance != null)
        {
            newsYearInput = GameState.Instance.CurrentYear;
            newsSemesterInput = GameState.Instance.CurrentSemester;
            newsRoundInput = GameState.Instance.CurrentRound;
            return;
        }

        newsEditorStatus = "当前不在 Play 模式，已保留你手动填写的学年/学期/回合。";
    }

    private void LoadMonthlyNewsOverrideOrGenerate()
    {
        if (ZhongshanDeckToolStateBridge.TryGetMonthlyNewsOverride(newsYearInput, newsSemesterInput, newsRoundInput, out ZhongshanDeckNewsRoundEntry entry) &&
            entry != null &&
            entry.items != null &&
            entry.items.Count > 0)
        {
            editingNewsItems.Clear();
            for (int i = 0; i < entry.items.Count; i++)
            {
                NewsItem item = entry.items[i];
                if (item != null)
                {
                    editingNewsItems.Add(item.Clone());
                }
            }

            newsEditorStatus = $"已载入 Y{newsYearInput} S{newsSemesterInput} R{newsRoundInput} 的新闻覆盖稿。";
            return;
        }

        ImportGeneratedNewsForEditor();
    }

    private void ImportGeneratedNewsForEditor()
    {
        editingNewsItems.Clear();

        if (EditorApplication.isPlaying && NewsSystem.Instance != null)
        {
            List<NewsItem> generated = NewsSystem.Instance.BuildEditableNewsForRound(newsYearInput, newsSemesterInput, newsRoundInput, true);
            for (int i = 0; i < generated.Count; i++)
            {
                NewsItem item = generated[i];
                if (item != null)
                {
                    editingNewsItems.Add(item.Clone());
                }
            }

            newsEditorStatus = $"已导入 Y{newsYearInput} S{newsSemesterInput} R{newsRoundInput} 的默认生成稿。";
            return;
        }

        editingNewsItems.Add(new NewsItem(NewsType.Headline, "新头条", "请填写本月头条。"));
        editingNewsItems.Add(new NewsItem(NewsType.Notice, "【通知】", "请填写本月通知。"));
        newsEditorStatus = "当前不在 Play 模式，无法读取运行时生成稿；已创建基础模板。";
    }

    private void SaveMonthlyNewsOverride()
    {
        List<NewsItem> items = new List<NewsItem>();
        for (int i = 0; i < editingNewsItems.Count; i++)
        {
            NewsItem item = editingNewsItems[i];
            if (item == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.title) && string.IsNullOrWhiteSpace(item.content))
            {
                continue;
            }

            items.Add(item.Clone());
        }

        if (items.Count == 0)
        {
            newsEditorStatus = "至少保留一条有标题或内容的新闻再保存。";
            return;
        }

        ZhongshanDeckToolStateBridge.SaveMonthlyNewsOverride(new ZhongshanDeckNewsRoundEntry
        {
            year = newsYearInput,
            semester = newsSemesterInput,
            round = newsRoundInput,
            items = items
        });

        newsEditorStatus = $"已保存 Y{newsYearInput} S{newsSemesterInput} R{newsRoundInput} 的新闻覆盖稿，共 {items.Count} 条。";
    }

    private void DeleteMonthlyNewsOverride()
    {
        bool deleted = ZhongshanDeckToolStateBridge.DeleteMonthlyNewsOverride(newsYearInput, newsSemesterInput, newsRoundInput);
        if (!deleted)
        {
            newsEditorStatus = "该月份还没有保存过覆盖稿。";
            return;
        }

        ImportGeneratedNewsForEditor();
        newsEditorStatus = $"已删除 Y{newsYearInput} S{newsSemesterInput} R{newsRoundInput} 的新闻覆盖稿。";
    }

    private Color GetNewsRoundButtonColor(int year, int semester, int round)
    {
        bool isSelected = newsYearInput == year && newsSemesterInput == semester && newsRoundInput == round;
        bool isCurrent = EditorApplication.isPlaying &&
                         GameState.Instance != null &&
                         GameState.Instance.CurrentYear == year &&
                         GameState.Instance.CurrentSemester == semester &&
                         GameState.Instance.CurrentRound == round;
        bool hasOverride = ZhongshanDeckToolStateBridge.TryGetMonthlyNewsOverride(year, semester, round, out _);

        if (isSelected) return new Color(0.85f, 0.63f, 0.18f, 1f);
        if (hasOverride) return new Color(0.25f, 0.56f, 0.36f, 1f);
        if (isCurrent) return new Color(0.28f, 0.46f, 0.76f, 1f);
        return new Color(0.78f, 0.78f, 0.78f, 1f);
    }

    private string ToChineseYear(int year)
    {
        switch (year)
        {
            case 1: return "一";
            case 2: return "二";
            case 3: return "三";
            case 4: return "四";
            default: return year.ToString();
        }
    }

    private void MoveNewsItem(int index, int direction)
    {
        int targetIndex = Mathf.Clamp(index + direction, 0, editingNewsItems.Count - 1);
        if (targetIndex == index)
        {
            return;
        }

        NewsItem item = editingNewsItems[index];
        editingNewsItems.RemoveAt(index);
        editingNewsItems.Insert(targetIndex, item);
    }

    private string GetNewsTypeDisplayName(NewsType type)
    {
        switch (type)
        {
            case NewsType.Headline: return "头条";
            case NewsType.Trending: return "热搜";
            case NewsType.Gossip: return "树洞";
            case NewsType.Notice: return "通知";
            case NewsType.Ad: return "推广";
            default: return "新闻";
        }
    }

    private string BuildNewsPreviewMeta(NewsItem item)
    {
        List<string> parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.author))
        {
            parts.Add(item.author);
        }
        if (!string.IsNullOrWhiteSpace(item.anonymousId))
        {
            parts.Add(item.anonymousId);
        }
        if (item.likes > 0)
        {
            parts.Add($"{item.likes}赞");
        }
        if (item.hotValue > 0f)
        {
            parts.Add($"{item.hotValue:0.0}万");
        }
        if (!string.IsNullOrWhiteSpace(item.hotTag))
        {
            parts.Add($"标签 {item.hotTag}");
        }
        if (!string.IsNullOrWhiteSpace(item.seriesId))
        {
            parts.Add($"连载 {item.seriesId}#{item.seriesOrder}");
        }

        return string.Join("  |  ", parts);
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
        LoadEndingEditorData(true);

        if (!EditorApplication.isPlaying || GameState.Instance == null)
        {
            return;
        }

        yearInput = GameState.Instance.CurrentYear;
        semesterInput = GameState.Instance.CurrentSemester;
        roundInput = GameState.Instance.CurrentRound;
        newsYearInput = GameState.Instance.CurrentYear;
        newsSemesterInput = GameState.Instance.CurrentSemester;
        newsRoundInput = GameState.Instance.CurrentRound;
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
