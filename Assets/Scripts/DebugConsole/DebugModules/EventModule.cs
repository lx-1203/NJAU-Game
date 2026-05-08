#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventModule : MonoBehaviour, IDebugModule
{
    private sealed class EventSelectionOption
    {
        public string eventId;
        public bool isAuthored;
        public string label;
    }

    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color BtnGreen = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnRed = new Color(0.60f, 0.20f, 0.20f, 1.0f);
    private static readonly Color BtnBlue = new Color(0.23f, 0.43f, 0.72f, 1.0f);
    private static readonly Color Panel = new Color(0.11f, 0.11f, 0.17f, 0.92f);

    private static readonly string[] EventTypeOptions = { "Fixed", "MainStory", "Conditional", "Random", "Dark" };
    private static readonly string[] PhaseOptions = { "RoundStart", "ActionComplete", "RoundEnd" };
    private static readonly string[] TimelinePhaseOptions = { "All", "RoundStart", "ActionComplete", "RoundEnd" };
    private static readonly string[] LibraryGroupOptions = { "全部", "随机事件", "主线", "黑暗", "考试结果", "证书考试", "补考", "NPC关系", "恋爱", "社团", "有场景演出" };

    private TMP_InputField eventIdInput;
    private TMP_InputField flagInput;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI queueText;
    private TextMeshProUGUI historyText;
    private TextMeshProUGUI roundPanelSummaryText;
    private TextMeshProUGUI eventCatalogText;
    private TextMeshProUGUI previewText;
    private TextMeshProUGUI editorModeText;

    private TMP_Dropdown eventTypeDropdown;
    private TMP_Dropdown phaseDropdown;
    private TMP_InputField titleInput;
    private TMP_InputField descriptionInput;
    private TMP_InputField priorityInput;
    private TMP_InputField eventActionPointCostInput;
    private TMP_InputField eventMoneyCostInput;
    private TMP_InputField maxTriggersPerRoundInput;
    private Toggle forcedToggle;
    private Toggle repeatableToggle;
    private TMP_InputField yearInput;
    private TMP_InputField semesterInput;
    private TMP_InputField roundMinInput;
    private TMP_InputField roundMaxInput;
    private TMP_InputField specificRoundsInput;
    private TMP_InputField probabilityInput;
    private TMP_InputField triggerBehaviorInput;
    private TMP_InputField attributeConditionsInput;
    private TMP_InputField affinityConditionsInput;
    private TMP_InputField romanceConditionsInput;
    private TMP_InputField clubConditionsInput;
    private TMP_InputField minMoneyInput;
    private TMP_InputField maxMoneyInput;
    private TMP_InputField minDarknessInput;
    private TMP_InputField requiredEventsInput;
    private TMP_InputField excludedEventsInput;
    private TMP_InputField requiredFlagsInput;
    private TMP_InputField excludedFlagsInput;
    private TMP_InputField speakerInput;
    private TMP_InputField portraitInput;
    private TMP_InputField dialogueLinesInput;
    private TMP_InputField sceneKeyInput;
    private TMP_InputField sceneDisplayNameInput;
    private TMP_InputField locationIdInput;
    private TMP_InputField backgroundResourceInput;
    private TMP_InputField protagonistPortraitInput;
    private TMP_InputField npcPortraitInput;
    private TMP_InputField backgroundSlotInput;
    private TMP_InputField protagonistSlotInput;
    private TMP_InputField npcSlotInput;
    private TMP_InputField defaultEffectsInput;
    private TMP_InputField chainEventsInput;
    private TMP_InputField choiceATextInput;
    private TMP_InputField choiceAAPCostInput;
    private TMP_InputField choiceAMoneyCostInput;
    private TMP_InputField choiceAEffectsInput;
    private TMP_InputField choiceANextInput;
    private TMP_InputField choiceAShowConditionsInput;
    private TMP_InputField choiceBTextInput;
    private TMP_InputField choiceBAPCostInput;
    private TMP_InputField choiceBMoneyCostInput;
    private TMP_InputField choiceBEffectsInput;
    private TMP_InputField choiceBNextInput;
    private TMP_InputField choiceBShowConditionsInput;
    private TMP_InputField choiceCTextInput;
    private TMP_InputField choiceCAPCostInput;
    private TMP_InputField choiceCMoneyCostInput;
    private TMP_InputField choiceCEffectsInput;
    private TMP_InputField choiceCNextInput;
    private TMP_InputField choiceCShowConditionsInput;
    private TMP_Dropdown roundEventDropdown;
    private TMP_Dropdown randomPhaseDropdown;
    private Toggle randomEventsEnabledToggle;
    private TMP_InputField timelineYearInput;
    private TMP_InputField timelineSemesterInput;
    private TMP_InputField timelineRoundInput;
    private TMP_Dropdown timelinePhaseDropdown;
    private TMP_InputField librarySearchInput;
    private TMP_Dropdown libraryGroupDropdown;
    private Transform roundEventListContent;
    private Transform libraryEventListContent;
    private Transform randomEventListContent;
    private TextMeshProUGUI randomEventSummaryText;

    private readonly List<EventSelectionOption> eventSelectionOptions = new List<EventSelectionOption>();
    private readonly List<EventSelectionOption> runtimeSelectionOptions = new List<EventSelectionOption>();
    private string editingEventId = string.Empty;
    private string editingSourceLabel = string.Empty;
    private bool isCreatingNewEvent;

    public void Init(RectTransform parent)
    {
        EnsureEventRuntime();

        GameObject scrollObj = CreateUIElement("ScrollView", parent);
        StretchFull(scrollObj.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        CreateLabel(content.transform, "事件调试与剧情速编 / 随机事件控制", 18f, TextGold, 30f);
        statusText = CreateLabel(content.transform, string.Empty, 14f, TextGray, 44f);

        BuildQuickActions(content.transform);
        BuildRandomEventControlPanel(content.transform);
        BuildBrowserPanel(content.transform);
        BuildAuthoringPanel(content.transform);

        queueText = CreateBlockLabel(content.transform, 170f);
        historyText = CreateBlockLabel(content.transform, 260f);
        previewText = CreateBlockLabel(content.transform, 260f);
    }

    public void Refresh()
    {
        EnsureEventRuntime();
        RefreshStatus();
        RefreshRuntimeEventDropdown();
        RefreshQueue();
        RefreshHistory();
        RefreshRoundEventPanel();
        RefreshRandomEventPanel();
        RefreshEventLibraryPanel();
        RefreshPreview();
    }

    private void EnsureEventRuntime()
    {
        if (EventHistory.Instance == null)
        {
            GameObject historyObj = new GameObject("EventHistory");
            historyObj.AddComponent<EventHistory>();
        }

        if (EventScheduler.Instance == null)
        {
            GameObject schedulerObj = new GameObject("EventScheduler");
            schedulerObj.AddComponent<EventScheduler>();
        }

        if (EventScheduler.Instance != null && EventScheduler.Instance.GetLoadedEventCount() == 0)
        {
            EventScheduler.Instance.LoadEvents();
        }

        if (EventExecutor.Instance == null)
        {
            GameObject executorObj = new GameObject("EventExecutor");
            executorObj.AddComponent<EventExecutor>();
        }
    }

    private void BuildQuickActions(Transform parent)
    {
        CreateSectionTitle(parent, "快速控制（可选）");

        GameObject eventRow = CreateRow(parent, 34f);
        CreateLabel(eventRow.transform, "事件 ID", 14f, TextWhite, 30f, 72f);
        eventIdInput = CreateInputField(eventRow.transform, "输入事件 ID", 260f, 30f);

        GameObject eventButtons = CreateRow(parent, 36f);
        CreateButton(eventButtons.transform, "触发", 100f, BtnGreen, ForceTriggerEvent);
        CreateButton(eventButtons.transform, "跳过", 100f, BtnRed, SkipEvent);

        GameObject flagRow = CreateRow(parent, 34f);
        CreateLabel(flagRow.transform, "标记", 14f, TextWhite, 30f, 72f);
        flagInput = CreateInputField(flagRow.transform, "输入标记名", 260f, 30f);

        GameObject flagButtons = CreateRow(parent, 36f);
        CreateButton(flagButtons.transform, "设为真", 100f, BtnGreen, () => ApplyFlag(true));
        CreateButton(flagButtons.transform, "设为假", 100f, BtnRed, () => ApplyFlag(false));
    }

    private void BuildBrowserPanel(Transform parent)
    {
        CreateSectionTitle(parent, "回合事件视图");

        GameObject roundPanel = CreatePanel(parent, 430f);
        VerticalLayoutGroup roundLayout = roundPanel.AddComponent<VerticalLayoutGroup>();
        roundLayout.spacing = 8f;
        roundLayout.padding = new RectOffset(12, 12, 12, 12);
        roundLayout.childControlWidth = true;
        roundLayout.childControlHeight = false;
        roundLayout.childForceExpandHeight = false;

        CreateLabel(roundPanel.transform, "查询某学年/学期/回合/阶段会被顺序判定的事件，并把已有事件加到这个回合或移出。", 13f, TextGray, 24f);

        GameObject roundQueryRow = CreateRow(roundPanel.transform, 34f);
        CreateLabel(roundQueryRow.transform, "时间", 14f, TextWhite, 30f, 44f);
        timelineYearInput = CreateInputField(roundQueryRow.transform, "学年", 60f, 30f, "1");
        timelineSemesterInput = CreateInputField(roundQueryRow.transform, "学期", 60f, 30f, "1");
        timelineRoundInput = CreateInputField(roundQueryRow.transform, "回合", 60f, 30f, "1");
        timelinePhaseDropdown = CreateDropdown(roundQueryRow.transform, TimelinePhaseOptions, 140f, 30f);

        GameObject roundQueryActions = CreateRow(roundPanel.transform, 34f);
        CreateButton(roundQueryActions.transform, "使用当前时间", 110f, BtnBlue, UseCurrentTimelineValues);
        CreateButton(roundQueryActions.transform, "刷新回合视图", 110f, BtnGreen, Refresh);

        GameObject roundAddRow = CreateRow(roundPanel.transform, 34f);
        CreateLabel(roundAddRow.transform, "加入回合", 14f, TextWhite, 30f, 68f);
        roundEventDropdown = CreateDropdown(roundAddRow.transform, new[] { "无事件" }, 360f, 30f);
        SetFlexibleWidth(roundEventDropdown.gameObject, 240f);
        CreateButton(roundAddRow.transform, "加入当前回合", 120f, BtnGreen, AddSelectedRuntimeEventToCurrentRound);

        roundPanelSummaryText = CreateLabel(roundPanel.transform, string.Empty, 13f, TextGray, 48f);
        roundPanelSummaryText.enableWordWrapping = true;
        roundPanelSummaryText.overflowMode = TextOverflowModes.Overflow;

        roundEventListContent = CreateListHost(roundPanel.transform, 230f);

        CreateSectionTitle(parent, "事件库视图");

        GameObject libraryPanel = CreatePanel(parent, 480f);
        VerticalLayoutGroup libraryLayout = libraryPanel.AddComponent<VerticalLayoutGroup>();
        libraryLayout.spacing = 8f;
        libraryLayout.padding = new RectOffset(12, 12, 12, 12);
        libraryLayout.childControlWidth = true;
        libraryLayout.childControlHeight = false;
        libraryLayout.childForceExpandHeight = false;

        CreateLabel(libraryPanel.transform, "搜索当前已有事件。新增事件功能只放在这里，点每行右侧“编辑”才会进入详细编辑。", 13f, TextGray, 24f);

        GameObject libraryRow = CreateRow(libraryPanel.transform, 34f);
        CreateLabel(libraryRow.transform, "搜索", 14f, TextWhite, 30f, 44f);
        librarySearchInput = CreateInputField(libraryRow.transform, "输入事件 ID / 标题 / 类型", 320f, 30f);
        SetFlexibleWidth(librarySearchInput.gameObject, 220f);
        libraryGroupDropdown = CreateDropdown(libraryRow.transform, LibraryGroupOptions, 130f, 30f);
        CreateButton(libraryRow.transform, "新增事件", 100f, BtnGreen, BeginCreateNewEvent);
        CreateButton(libraryRow.transform, "刷新列表", 100f, BtnBlue, Refresh);

        eventCatalogText = CreateLabel(libraryPanel.transform, string.Empty, 13f, TextGray, 44f);
        eventCatalogText.enableWordWrapping = true;
        eventCatalogText.overflowMode = TextOverflowModes.Overflow;

        libraryEventListContent = CreateListHost(libraryPanel.transform, 350f);
    }

    private void BuildRandomEventControlPanel(Transform parent)
    {
        CreateSectionTitle(parent, "随机事件控制");

        GameObject randomPanel = CreatePanel(parent, 360f);
        VerticalLayoutGroup randomLayout = randomPanel.AddComponent<VerticalLayoutGroup>();
        randomLayout.spacing = 8f;
        randomLayout.padding = new RectOffset(12, 12, 12, 12);
        randomLayout.childControlWidth = true;
        randomLayout.childControlHeight = false;
        randomLayout.childForceExpandHeight = false;

        CreateLabel(randomPanel.transform, "制作随机事件时用这里：可以临时关闭自然随机、查看当前地点可触发事件，或直接抽取一条进行测试。", 13f, TextGray, 24f);

        GameObject controlRow = CreateRow(randomPanel.transform, 34f);
        randomEventsEnabledToggle = CreateToggle(controlRow.transform, "自然随机", true);
        randomEventsEnabledToggle.onValueChanged.AddListener(value =>
        {
            if (EventScheduler.Instance != null)
            {
                EventScheduler.Instance.RandomEventsEnabled = value;
            }
            RefreshRandomEventPanel();
        });
        CreateLabel(controlRow.transform, "阶段", 14f, TextWhite, 30f, 44f);
        randomPhaseDropdown = CreateDropdown(controlRow.transform, PhaseOptions, 150f, 30f);
        CreateButton(controlRow.transform, "刷新", 80f, BtnBlue, Refresh);

        GameObject actionRow = CreateRow(randomPanel.transform, 34f);
        CreateButton(actionRow.transform, "按概率抽取", 110f, BtnGreen, () => DrawRandomEvent(includeProbabilityCheck: true));
        CreateButton(actionRow.transform, "忽略概率抽取", 120f, BtnBlue, () => DrawRandomEvent(includeProbabilityCheck: false));
        CreateButton(actionRow.transform, "执行阶段判定", 120f, BtnGreen, CheckSelectedRandomPhase);

        randomEventSummaryText = CreateLabel(randomPanel.transform, string.Empty, 13f, TextGray, 52f);
        randomEventSummaryText.enableWordWrapping = true;
        randomEventSummaryText.overflowMode = TextOverflowModes.Overflow;

        randomEventListContent = CreateListHost(randomPanel.transform, 200f);
    }

    private void BuildAuthoringPanel(Transform parent)
    {
        CreateSectionTitle(parent, "事件详细编辑");
        editorModeText = CreateLabel(parent, "请从上方“回合事件视图”或“事件库视图”的最右侧编辑按钮进入", 13f, TextGray, 24f);

        GameObject metaPanel = CreatePanel(parent, 260f);
        VerticalLayoutGroup metaLayout = metaPanel.AddComponent<VerticalLayoutGroup>();
        metaLayout.spacing = 8f;
        metaLayout.padding = new RectOffset(12, 12, 12, 12);
        metaLayout.childControlWidth = true;
        metaLayout.childControlHeight = false;
        metaLayout.childForceExpandHeight = false;

        GameObject row1 = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row1.transform, "标题", 14f, TextWhite, 30f, 52f);
        titleInput = CreateInputField(row1.transform, "事件标题", 230f, 30f);
        SetFlexibleWidth(titleInput.gameObject, 220f);

        GameObject row1b = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row1b.transform, "类型", 14f, TextWhite, 30f, 44f);
        eventTypeDropdown = CreateDropdown(row1b.transform, EventTypeOptions, 140f, 30f);
        CreateLabel(row1b.transform, "阶段", 14f, TextWhite, 30f, 44f);
        phaseDropdown = CreateDropdown(row1b.transform, PhaseOptions, 150f, 30f);

        GameObject row2 = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row2.transform, "优先级", 14f, TextWhite, 30f, 52f);
        priorityInput = CreateInputField(row2.transform, "0-9", 70f, 30f, "2");
        forcedToggle = CreateToggle(row2.transform, "强制");
        repeatableToggle = CreateToggle(row2.transform, "可重复", true);
        CreateLabel(row2.transform, "概率", 14f, TextWhite, 30f, 44f);
        probabilityInput = CreateInputField(row2.transform, "0~1", 80f, 30f, "1");
        CreateLabel(row2.transform, "行为", 14f, TextWhite, 30f, 44f);
        triggerBehaviorInput = CreateInputField(row2.transform, "Dark 事件行为键", 140f, 30f);

        GameObject row2b = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row2b.transform, "事件AP", 14f, TextWhite, 30f, 52f);
        eventActionPointCostInput = CreateInputField(row2b.transform, "0", 60f, 30f, "0");
        CreateLabel(row2b.transform, "事件金钱", 14f, TextWhite, 30f, 60f);
        eventMoneyCostInput = CreateInputField(row2b.transform, "0", 80f, 30f, "0");
        CreateLabel(row2b.transform, "每回合上限", 14f, TextWhite, 30f, 68f);
        maxTriggersPerRoundInput = CreateInputField(row2b.transform, "0=不限", 80f, 30f, "0");

        GameObject row3 = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row3.transform, "学年", 14f, TextWhite, 30f, 44f);
        yearInput = CreateInputField(row3.transform, "0", 56f, 30f, "0");
        CreateLabel(row3.transform, "学期", 14f, TextWhite, 30f, 44f);
        semesterInput = CreateInputField(row3.transform, "0", 56f, 30f, "0");
        CreateLabel(row3.transform, "回合区间", 14f, TextWhite, 30f, 68f);
        roundMinInput = CreateInputField(row3.transform, "min", 56f, 30f, "0");
        roundMaxInput = CreateInputField(row3.transform, "max", 56f, 30f, "0");

        GameObject row3b = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row3b.transform, "指定回合", 14f, TextWhite, 30f, 68f);
        specificRoundsInput = CreateInputField(row3b.transform, "1,5,8", 180f, 30f);
        SetFlexibleWidth(specificRoundsInput.gameObject, 180f);

        GameObject row4 = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row4.transform, "前置", 14f, TextWhite, 30f, 44f);
        requiredEventsInput = CreateInputField(row4.transform, "事件ID,事件ID", 220f, 30f);
        SetFlexibleWidth(requiredEventsInput.gameObject, 160f);
        CreateLabel(row4.transform, "排除", 14f, TextWhite, 30f, 44f);
        excludedEventsInput = CreateInputField(row4.transform, "事件ID,事件ID", 220f, 30f);
        SetFlexibleWidth(excludedEventsInput.gameObject, 160f);

        GameObject row4b = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row4b.transform, "前置标记", 14f, TextWhite, 30f, 68f);
        requiredFlagsInput = CreateInputField(row4b.transform, "flag_a,flag_b", 220f, 30f);
        SetFlexibleWidth(requiredFlagsInput.gameObject, 160f);
        CreateLabel(row4b.transform, "排除标记", 14f, TextWhite, 30f, 68f);
        excludedFlagsInput = CreateInputField(row4b.transform, "flag_x,flag_y", 220f, 30f);
        SetFlexibleWidth(excludedFlagsInput.gameObject, 160f);

        GameObject row5 = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row5.transform, "属性条件", 14f, TextWhite, 30f, 68f);
        attributeConditionsInput = CreateInputField(row5.transform, "学力>=60,心情<30", 240f, 30f);
        SetFlexibleWidth(attributeConditionsInput.gameObject, 180f);
        CreateLabel(row5.transform, "金钱", 14f, TextWhite, 30f, 36f);
        minMoneyInput = CreateInputField(row5.transform, "min", 64f, 30f, "0");
        maxMoneyInput = CreateInputField(row5.transform, "max", 64f, 30f, "0");
        CreateLabel(row5.transform, "黑暗值", 14f, TextWhite, 30f, 52f);
        minDarknessInput = CreateInputField(row5.transform, "min", 64f, 30f, "0");

        GameObject row5b = CreateRow(metaPanel.transform, 34f);
        CreateLabel(row5b.transform, "好感条件", 14f, TextWhite, 30f, 68f);
        affinityConditionsInput = CreateInputField(row5b.transform, "npc_id:Friend:40; npc_b::60", 380f, 30f);
        SetFlexibleWidth(affinityConditionsInput.gameObject, 240f);

        descriptionInput = CreateMultilineInput(parent, "剧情简介 / 备注", 92f);

        GameObject dialoguePanel = CreatePanel(parent, 250f);
        VerticalLayoutGroup dialogueLayout = dialoguePanel.AddComponent<VerticalLayoutGroup>();
        dialogueLayout.spacing = 8f;
        dialogueLayout.padding = new RectOffset(12, 12, 12, 12);
        dialogueLayout.childControlWidth = true;
        dialogueLayout.childControlHeight = false;
        dialogueLayout.childForceExpandHeight = false;

        CreateLabel(dialoguePanel.transform, "对话流", 16f, TextGold, 28f);
        GameObject speakerRow = CreateRow(dialoguePanel.transform, 34f);
        CreateLabel(speakerRow.transform, "说话人", 14f, TextWhite, 30f, 52f);
        speakerInput = CreateInputField(speakerRow.transform, "旁白 / NPC 名", 160f, 30f);
        SetFlexibleWidth(speakerInput.gameObject, 140f);
        CreateLabel(speakerRow.transform, "头像", 14f, TextWhite, 30f, 44f);
        portraitInput = CreateInputField(speakerRow.transform, "portraitId", 160f, 30f);
        SetFlexibleWidth(portraitInput.gameObject, 120f);
        dialogueLinesInput = CreateMultilineInput(dialoguePanel.transform, "每行一句台词，按顺序播放", 130f);

        GameObject presentationPanel = CreatePanel(parent, 240f);
        VerticalLayoutGroup presentationLayout = presentationPanel.AddComponent<VerticalLayoutGroup>();
        presentationLayout.spacing = 8f;
        presentationLayout.padding = new RectOffset(12, 12, 12, 12);
        presentationLayout.childControlWidth = true;
        presentationLayout.childControlHeight = false;
        presentationLayout.childForceExpandHeight = false;

        CreateLabel(presentationPanel.transform, "事件场景演出", 16f, TextGold, 28f);
        GameObject presentationRow1 = CreateRow(presentationPanel.transform, 34f);
        CreateLabel(presentationRow1.transform, "场景键", 14f, TextWhite, 30f, 52f);
        sceneKeyInput = CreateInputField(presentationRow1.transform, "evt_freshman_training", 160f, 30f);
        CreateLabel(presentationRow1.transform, "场景名", 14f, TextWhite, 30f, 52f);
        sceneDisplayNameInput = CreateInputField(presentationRow1.transform, "军训操场", 180f, 30f);
        CreateLabel(presentationRow1.transform, "地点", 14f, TextWhite, 30f, 44f);
        locationIdInput = CreateInputField(presentationRow1.transform, "Playground", 120f, 30f);

        GameObject presentationRow2 = CreateRow(presentationPanel.transform, 34f);
        CreateLabel(presentationRow2.transform, "背景", 14f, TextWhite, 30f, 44f);
        backgroundResourceInput = CreateInputField(presentationRow2.transform, "Backgrounds/Event/freshman_training", 220f, 30f);
        SetFlexibleWidth(backgroundResourceInput.gameObject, 180f);
        CreateLabel(presentationRow2.transform, "背景占位", 14f, TextWhite, 30f, 68f);
        backgroundSlotInput = CreateInputField(presentationRow2.transform, "BG_FreshmanTraining", 170f, 30f);

        GameObject presentationRow3 = CreateRow(presentationPanel.transform, 34f);
        CreateLabel(presentationRow3.transform, "主角立绘", 14f, TextWhite, 30f, 68f);
        protagonistPortraitInput = CreateInputField(presentationRow3.transform, "Portraits/Player/player_event_training", 210f, 30f);
        SetFlexibleWidth(protagonistPortraitInput.gameObject, 170f);
        CreateLabel(presentationRow3.transform, "主角占位", 14f, TextWhite, 30f, 68f);
        protagonistSlotInput = CreateInputField(presentationRow3.transform, "PC_Training_Default", 150f, 30f);

        GameObject presentationRow4 = CreateRow(presentationPanel.transform, 34f);
        CreateLabel(presentationRow4.transform, "NPC立绘", 14f, TextWhite, 30f, 60f);
        npcPortraitInput = CreateInputField(presentationRow4.transform, "Portraits/NPC/instructor_event_training", 210f, 30f);
        SetFlexibleWidth(npcPortraitInput.gameObject, 170f);
        CreateLabel(presentationRow4.transform, "NPC占位", 14f, TextWhite, 30f, 60f);
        npcSlotInput = CreateInputField(presentationRow4.transform, "NPC_Instructor_Training", 150f, 30f);

        GameObject choicePanel = CreatePanel(parent, 360f);
        VerticalLayoutGroup choiceLayout = choicePanel.AddComponent<VerticalLayoutGroup>();
        choiceLayout.spacing = 8f;
        choiceLayout.padding = new RectOffset(12, 12, 12, 12);
        choiceLayout.childControlWidth = true;
        choiceLayout.childControlHeight = false;
        choiceLayout.childForceExpandHeight = false;

        CreateLabel(choicePanel.transform, "选项与效果", 16f, TextGold, 28f);
        CreateChoiceEditor(choicePanel.transform, "选项 A", out choiceATextInput, out choiceAAPCostInput, out choiceAMoneyCostInput, out choiceAEffectsInput, out choiceANextInput, out choiceAShowConditionsInput);
        CreateChoiceEditor(choicePanel.transform, "选项 B", out choiceBTextInput, out choiceBAPCostInput, out choiceBMoneyCostInput, out choiceBEffectsInput, out choiceBNextInput, out choiceBShowConditionsInput);
        CreateChoiceEditor(choicePanel.transform, "选项 C", out choiceCTextInput, out choiceCAPCostInput, out choiceCMoneyCostInput, out choiceCEffectsInput, out choiceCNextInput, out choiceCShowConditionsInput);
        defaultEffectsInput = CreateMultilineInput(choicePanel.transform, "默认效果。格式：attribute:学力:5; money::200; actionPoint::1", 78f);
        chainEventsInput = CreateInputField(choicePanel.transform, "事件链：事件ID,事件ID", 320f, 30f);

        GameObject authorButtons = CreateRow(parent, 36f);
        CreateButton(authorButtons.transform, "保存草稿", 100f, BtnBlue, SaveAuthoredEvent);
        CreateButton(authorButtons.transform, "注册运行时", 110f, BtnGreen, RegisterRuntimeEvent);
        CreateButton(authorButtons.transform, "清空编辑器", 110f, BtnRed, ClearAuthoringForm);
    }

    private void CreateChoiceEditor(Transform parent, string title, out TMP_InputField textInput, out TMP_InputField apCostInput, out TMP_InputField moneyCostInput, out TMP_InputField effectsInput, out TMP_InputField nextInput, out TMP_InputField showConditionsInput)
    {
        GameObject block = CreatePanel(parent, 92f);
        VerticalLayoutGroup layout = block.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        CreateLabel(block.transform, title, 14f, TextWhite, 22f);
        GameObject row = CreateRow(block.transform, 30f);
        textInput = CreateInputField(row.transform, "选项文本", 170f, 28f);
        SetFlexibleWidth(textInput.gameObject, 150f);
        apCostInput = CreateInputField(row.transform, "AP", 48f, 28f, "0");
        moneyCostInput = CreateInputField(row.transform, "¥", 60f, 28f, "0");
        nextInput = CreateInputField(row.transform, "下一事件 ID", 110f, 28f);
        effectsInput = CreateInputField(block.transform, "效果格式：attribute:学力:5; money::-200; actionPoint::1", 400f, 28f);
        SetFlexibleWidth(effectsInput.gameObject, 220f);
        showConditionsInput = CreateInputField(block.transform, "显示条件：学力>=60,心情>20", 400f, 28f);
        SetFlexibleWidth(showConditionsInput.gameObject, 220f);
    }

    private void ForceTriggerEvent()
    {
        string eventId = SafeText(eventIdInput);
        if (string.IsNullOrEmpty(eventId))
        {
            statusText.text = "请输入事件 ID";
            return;
        }

        if (EventScheduler.Instance == null)
        {
            statusText.text = "EventScheduler 尚未就绪";
            return;
        }

        EventScheduler.Instance.EnqueueEvent(eventId);
        statusText.text = $"已加入触发队列：{eventId}";
        DebugConsoleManager.Log("Event", $"Force trigger {eventId}");
        Refresh();
    }

    private void DrawRandomEvent(bool includeProbabilityCheck)
    {
        if (EventScheduler.Instance == null)
        {
            statusText.text = "EventScheduler 尚未就绪";
            return;
        }

        TriggerPhase phase = GetSelectedRandomPhase();
        EventDefinition selected = EventScheduler.Instance.EnqueueRandomEvent(
            phase,
            includeProbabilityCheck,
            ignoreRandomEnabled: true);

        if (selected == null)
        {
            statusText.text = includeProbabilityCheck
                ? $"阶段 {phase} 没有通过概率与条件检定的随机事件"
                : $"阶段 {phase} 没有满足当前条件的随机事件";
            RefreshRandomEventPanel();
            return;
        }

        statusText.text = $"已抽取随机事件：{selected.id} | {selected.title}";
        DebugConsoleManager.Log("Event", $"Draw random event {selected.id} phase={phase} probability={includeProbabilityCheck}");
        Refresh();
    }

    private void CheckSelectedRandomPhase()
    {
        if (EventScheduler.Instance == null)
        {
            statusText.text = "EventScheduler 尚未就绪";
            return;
        }

        TriggerPhase phase = GetSelectedRandomPhase();
        EventScheduler.Instance.CheckAndTriggerEvents(phase);
        statusText.text = $"已执行阶段判定：{phase}";
        DebugConsoleManager.Log("Event", $"Check phase from random panel {phase}");
        Refresh();
    }

    private void SkipEvent()
    {
        string eventId = SafeText(eventIdInput);
        if (string.IsNullOrEmpty(eventId))
        {
            statusText.text = "请输入事件 ID";
            return;
        }

        if (EventHistory.Instance == null)
        {
            statusText.text = "EventHistory 尚未就绪";
            return;
        }

        EventHistory.Instance.RecordEvent(eventId, -1);
        statusText.text = $"已标记为跳过：{eventId}";
        DebugConsoleManager.Log("Event", $"Skip event {eventId}");
        Refresh();
    }

    private void ApplyFlag(bool value)
    {
        string flag = SafeText(flagInput);
        if (string.IsNullOrEmpty(flag))
        {
            statusText.text = "请输入标记名";
            return;
        }

        if (EventHistory.Instance == null)
        {
            statusText.text = "EventHistory 尚未就绪";
            return;
        }

        EventHistory.Instance.SetFlag(flag, value);
        statusText.text = $"标记 {flag} = {value}";
        DebugConsoleManager.Log("Event", $"Flag {flag} -> {value}");
        Refresh();
    }

    private void SaveAuthoredEvent()
    {
        if (!TryBuildEventDefinition(out EventDefinition evt, out string error))
        {
            statusText.text = error;
            return;
        }

        string json = JsonUtility.ToJson(new EventDatabaseRoot { events = new[] { evt } }, true);
        ZhongshanDeckToolStateBridge.SaveAuthoredEvent(evt.id, evt.title, json);
        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.RegisterOrReplaceRuntimeEvent(CloneEvent(evt));
        }
        editingEventId = evt.id;
        editingSourceLabel = "草稿";
        isCreatingNewEvent = false;
        UpdateEditorModeLabel();
        statusText.text = $"已保存剧情草稿并同步运行时：{evt.id}";
        DebugConsoleManager.Log("Event", $"Save authored event {evt.id}");
        Refresh();
    }

    private void LoadAuthoredEvent()
    {
        string eventId = SafeText(eventIdInput);
        if (string.IsNullOrEmpty(eventId))
        {
            statusText.text = "请输入要载入的事件 ID";
            return;
        }

        if (!ZhongshanDeckToolStateBridge.TryGetAuthoredEvent(eventId, out ZhongshanDeckEventEntry entry))
        {
            statusText.text = $"未找到草稿：{eventId}";
            return;
        }

        EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(entry.json);
        if (root == null || root.events == null || root.events.Length == 0)
        {
            statusText.text = $"草稿解析失败：{eventId}";
            return;
        }

        FillForm(root.events[0]);
        editingEventId = root.events[0].id ?? eventId;
        editingSourceLabel = "草稿";
        isCreatingNewEvent = false;
        UpdateEditorModeLabel();
        statusText.text = $"已载入草稿：{eventId}";
        Refresh();
    }

    private void LoadRuntimeEvent()
    {
        string eventId = SafeText(eventIdInput);
        if (string.IsNullOrEmpty(eventId))
        {
            statusText.text = "请输入要载入的运行时事件 ID";
            return;
        }

        if (EventScheduler.Instance == null)
        {
            statusText.text = "EventScheduler 尚未就绪";
            return;
        }

        EventDefinition evt = EventScheduler.Instance.GetEvent(eventId);
        if (evt == null)
        {
            statusText.text = $"未找到运行时事件：{eventId}";
            return;
        }

        FillForm(CloneEvent(evt));
        editingEventId = evt.id ?? eventId;
        editingSourceLabel = "运行时";
        isCreatingNewEvent = false;
        UpdateEditorModeLabel();
        statusText.text = $"已载入运行时事件：{eventId}";
        RefreshPreview();
    }

    private void DeleteAuthoredEvent()
    {
        string eventId = SafeText(eventIdInput);
        if (string.IsNullOrEmpty(eventId))
        {
            statusText.text = "请输入要删除的事件 ID";
            return;
        }

        bool deleted = ZhongshanDeckToolStateBridge.DeleteAuthoredEvent(eventId);
        bool removedRuntime = EventScheduler.Instance != null && EventScheduler.Instance.RemoveRuntimeEvent(eventId);
        statusText.text = deleted
            ? (removedRuntime ? $"已删除草稿并移除运行时事件：{eventId}" : $"已删除草稿：{eventId}")
            : $"未找到草稿：{eventId}";
        Refresh();
    }

    private void DeleteAuthoredEventById(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        bool deleted = ZhongshanDeckToolStateBridge.DeleteAuthoredEvent(eventId);
        bool removedRuntime = EventScheduler.Instance != null && EventScheduler.Instance.RemoveRuntimeEvent(eventId);
        statusText.text = deleted
            ? (removedRuntime ? $"已删除草稿并移除运行时事件：{eventId}" : $"已删除草稿：{eventId}")
            : $"未找到草稿：{eventId}";
        Refresh();
    }

    private void RegisterRuntimeEvent()
    {
        if (EventScheduler.Instance == null)
        {
            statusText.text = "EventScheduler 尚未就绪";
            return;
        }

        if (!TryBuildEventDefinition(out EventDefinition evt, out string error))
        {
            statusText.text = error;
            return;
        }

        EventScheduler.Instance.RegisterOrReplaceRuntimeEvent(evt);
        eventIdInput.text = evt.id;
        editingEventId = evt.id;
        editingSourceLabel = string.IsNullOrEmpty(editingSourceLabel) ? "运行时" : editingSourceLabel;
        isCreatingNewEvent = false;
        UpdateEditorModeLabel();
        statusText.text = $"已注册运行时事件：{evt.id}";
        DebugConsoleManager.Log("Event", $"Register runtime event {evt.id}");
        Refresh();
    }

    private void ClearAuthoringForm()
    {
        if (eventIdInput != null) eventIdInput.text = string.Empty;
        if (titleInput != null) titleInput.text = string.Empty;
        if (descriptionInput != null) descriptionInput.text = string.Empty;
        if (priorityInput != null) priorityInput.text = "2";
        if (eventActionPointCostInput != null) eventActionPointCostInput.text = "0";
        if (eventMoneyCostInput != null) eventMoneyCostInput.text = "0";
        if (maxTriggersPerRoundInput != null) maxTriggersPerRoundInput.text = "0";
        if (probabilityInput != null) probabilityInput.text = "1";
        if (yearInput != null) yearInput.text = "0";
        if (semesterInput != null) semesterInput.text = "0";
        if (roundMinInput != null) roundMinInput.text = "0";
        if (roundMaxInput != null) roundMaxInput.text = "0";
        if (specificRoundsInput != null) specificRoundsInput.text = string.Empty;
        if (triggerBehaviorInput != null) triggerBehaviorInput.text = string.Empty;
        if (attributeConditionsInput != null) attributeConditionsInput.text = string.Empty;
        if (affinityConditionsInput != null) affinityConditionsInput.text = string.Empty;
        if (minMoneyInput != null) minMoneyInput.text = "0";
        if (maxMoneyInput != null) maxMoneyInput.text = "0";
        if (minDarknessInput != null) minDarknessInput.text = "0";
        if (requiredEventsInput != null) requiredEventsInput.text = string.Empty;
        if (excludedEventsInput != null) excludedEventsInput.text = string.Empty;
        if (requiredFlagsInput != null) requiredFlagsInput.text = string.Empty;
        if (excludedFlagsInput != null) excludedFlagsInput.text = string.Empty;
        if (speakerInput != null) speakerInput.text = string.Empty;
        if (portraitInput != null) portraitInput.text = string.Empty;
        if (dialogueLinesInput != null) dialogueLinesInput.text = string.Empty;
        if (sceneKeyInput != null) sceneKeyInput.text = string.Empty;
        if (sceneDisplayNameInput != null) sceneDisplayNameInput.text = string.Empty;
        if (locationIdInput != null) locationIdInput.text = string.Empty;
        if (backgroundResourceInput != null) backgroundResourceInput.text = string.Empty;
        if (protagonistPortraitInput != null) protagonistPortraitInput.text = string.Empty;
        if (npcPortraitInput != null) npcPortraitInput.text = string.Empty;
        if (backgroundSlotInput != null) backgroundSlotInput.text = string.Empty;
        if (protagonistSlotInput != null) protagonistSlotInput.text = string.Empty;
        if (npcSlotInput != null) npcSlotInput.text = string.Empty;
        if (defaultEffectsInput != null) defaultEffectsInput.text = string.Empty;
        if (chainEventsInput != null) chainEventsInput.text = string.Empty;
        if (choiceATextInput != null) choiceATextInput.text = string.Empty;
        if (choiceAAPCostInput != null) choiceAAPCostInput.text = "0";
        if (choiceAMoneyCostInput != null) choiceAMoneyCostInput.text = "0";
        if (choiceAEffectsInput != null) choiceAEffectsInput.text = string.Empty;
        if (choiceANextInput != null) choiceANextInput.text = string.Empty;
        if (choiceAShowConditionsInput != null) choiceAShowConditionsInput.text = string.Empty;
        if (choiceBTextInput != null) choiceBTextInput.text = string.Empty;
        if (choiceBAPCostInput != null) choiceBAPCostInput.text = "0";
        if (choiceBMoneyCostInput != null) choiceBMoneyCostInput.text = "0";
        if (choiceBEffectsInput != null) choiceBEffectsInput.text = string.Empty;
        if (choiceBNextInput != null) choiceBNextInput.text = string.Empty;
        if (choiceBShowConditionsInput != null) choiceBShowConditionsInput.text = string.Empty;
        if (choiceCTextInput != null) choiceCTextInput.text = string.Empty;
        if (choiceCAPCostInput != null) choiceCAPCostInput.text = "0";
        if (choiceCMoneyCostInput != null) choiceCMoneyCostInput.text = "0";
        if (choiceCEffectsInput != null) choiceCEffectsInput.text = string.Empty;
        if (choiceCNextInput != null) choiceCNextInput.text = string.Empty;
        if (choiceCShowConditionsInput != null) choiceCShowConditionsInput.text = string.Empty;
        if (eventTypeDropdown != null) eventTypeDropdown.value = 0;
        if (phaseDropdown != null) phaseDropdown.value = 0;
        if (forcedToggle != null) forcedToggle.isOn = false;
        if (repeatableToggle != null) repeatableToggle.isOn = true;

        statusText.text = "剧情编辑器已清空";
        Refresh();
    }

    private void BeginCreateNewEvent()
    {
        ClearAuthoringForm();
        isCreatingNewEvent = true;
        editingEventId = string.Empty;
        editingSourceLabel = "新建事件";
        if (editorModeText != null)
        {
            editorModeText.text = "正在新建事件（从事件库入口创建）";
        }
    }

    private bool TryBuildEventDefinition(out EventDefinition evt, out string error)
    {
        evt = null;
        error = null;

        string eventId = SafeText(eventIdInput);
        if (string.IsNullOrEmpty(eventId))
        {
            error = "事件 ID 不能为空";
            return false;
        }

        string title = SafeText(titleInput);
        if (string.IsNullOrEmpty(title))
        {
            error = "标题不能为空";
            return false;
        }

        string[] lines = ParseMultiline(dialogueLinesInput);
        if (lines.Length == 0)
        {
            error = "至少填写一行对话";
            return false;
        }

        evt = new EventDefinition
        {
            id = eventId,
            eventType = EventTypeOptions[Mathf.Clamp(eventTypeDropdown.value, 0, EventTypeOptions.Length - 1)],
            title = title,
            description = SafeText(descriptionInput),
            priority = ParseInt(priorityInput, 2),
            isForced = forcedToggle != null && forcedToggle.isOn,
            isRepeatable = repeatableToggle == null || repeatableToggle.isOn,
            actionPointCost = ParseInt(eventActionPointCostInput, 0),
            moneyCost = ParseInt(eventMoneyCostInput, 0),
            maxTriggersPerRound = ParseInt(maxTriggersPerRoundInput, 0),
            trigger = new EventTriggerCondition
            {
                year = ParseInt(yearInput, 0),
                semester = ParseInt(semesterInput, 0),
                roundMin = ParseInt(roundMinInput, 0),
                roundMax = ParseInt(roundMaxInput, 0),
                specificRounds = ParseIntArray(SafeText(specificRoundsInput)),
                attributeConditions = ParseAttributeConditions(SafeText(attributeConditionsInput)),
                minMoney = ParseInt(minMoneyInput, 0),
                maxMoney = ParseInt(maxMoneyInput, 0),
                affinityConditions = ParseAffinityConditions(SafeText(affinityConditionsInput)),
                requiredEventIds = ParseCsv(SafeText(requiredEventsInput)),
                excludedEventIds = ParseCsv(SafeText(excludedEventsInput)),
                requiredFlags = ParseCsv(SafeText(requiredFlagsInput)),
                excludedFlags = ParseCsv(SafeText(excludedFlagsInput)),
                minDarkness = ParseInt(minDarknessInput, 0),
                triggerBehavior = SafeText(triggerBehaviorInput),
                triggerChance = Mathf.Clamp01(ParseFloat(probabilityInput, 1f)),
                phase = PhaseOptions[Mathf.Clamp(phaseDropdown.value, 0, PhaseOptions.Length - 1)]
            },
            dialogues = new[]
            {
                new EventDialogue
                {
                    speaker = SafeText(speakerInput),
                    lines = lines,
                    portraitId = SafeText(portraitInput)
                }
            },
            choices = BuildChoices(),
            defaultEffects = ParseEffects(SafeText(defaultEffectsInput)),
            chainEventIds = ParseCsv(SafeText(chainEventsInput)),
            presentation = BuildPresentation()
        };

        return true;
    }

    private EventPresentationDefinition BuildPresentation()
    {
        string sceneKey = SafeText(sceneKeyInput);
        string sceneDisplayName = SafeText(sceneDisplayNameInput);
        string locationId = SafeText(locationIdInput);
        string backgroundPath = SafeText(backgroundResourceInput);
        string protagonistPath = SafeText(protagonistPortraitInput);
        string npcPath = SafeText(npcPortraitInput);
        string backgroundSlot = SafeText(backgroundSlotInput);
        string protagonistSlot = SafeText(protagonistSlotInput);
        string npcSlot = SafeText(npcSlotInput);

        if (string.IsNullOrEmpty(sceneKey) &&
            string.IsNullOrEmpty(sceneDisplayName) &&
            string.IsNullOrEmpty(locationId) &&
            string.IsNullOrEmpty(backgroundPath) &&
            string.IsNullOrEmpty(protagonistPath) &&
            string.IsNullOrEmpty(npcPath) &&
            string.IsNullOrEmpty(backgroundSlot) &&
            string.IsNullOrEmpty(protagonistSlot) &&
            string.IsNullOrEmpty(npcSlot))
        {
            return null;
        }

        return new EventPresentationDefinition
        {
            sceneKey = sceneKey,
            sceneDisplayName = sceneDisplayName,
            locationId = locationId,
            backgroundResourcePath = backgroundPath,
            protagonistPortraitResourcePath = protagonistPath,
            npcPortraitResourcePath = npcPath,
            backgroundSlotName = backgroundSlot,
            protagonistSlotName = protagonistSlot,
            npcSlotName = npcSlot
        };
    }

    private EventChoice[] BuildChoices()
    {
        List<EventChoice> choices = new List<EventChoice>();
        TryAppendChoice(choices, choiceATextInput, choiceAAPCostInput, choiceAMoneyCostInput, choiceAEffectsInput, choiceANextInput, choiceAShowConditionsInput);
        TryAppendChoice(choices, choiceBTextInput, choiceBAPCostInput, choiceBMoneyCostInput, choiceBEffectsInput, choiceBNextInput, choiceBShowConditionsInput);
        TryAppendChoice(choices, choiceCTextInput, choiceCAPCostInput, choiceCMoneyCostInput, choiceCEffectsInput, choiceCNextInput, choiceCShowConditionsInput);
        return choices.ToArray();
    }

    private void TryAppendChoice(List<EventChoice> choices, TMP_InputField textField, TMP_InputField apCostField, TMP_InputField moneyCostField, TMP_InputField effectsField, TMP_InputField nextField, TMP_InputField showConditionsField)
    {
        string text = SafeText(textField);
        if (string.IsNullOrEmpty(text))
            return;

        choices.Add(new EventChoice
        {
            text = text,
            actionPointCost = ParseInt(apCostField, 0),
            moneyCost = ParseInt(moneyCostField, 0),
            effects = ParseEffects(SafeText(effectsField)),
            triggerEventId = SafeText(nextField),
            showConditions = ParseAttributeConditions(SafeText(showConditionsField))
        });
    }

    private void FillForm(EventDefinition evt)
    {
        if (evt == null)
            return;

        eventIdInput.text = evt.id ?? string.Empty;
        titleInput.text = evt.title ?? string.Empty;
        descriptionInput.text = evt.description ?? string.Empty;
        priorityInput.text = evt.priority.ToString();
        forcedToggle.isOn = evt.isForced;
        repeatableToggle.isOn = evt.isRepeatable;
        eventActionPointCostInput.text = evt.actionPointCost.ToString();
        eventMoneyCostInput.text = evt.moneyCost.ToString();
        maxTriggersPerRoundInput.text = evt.maxTriggersPerRound.ToString();
        eventTypeDropdown.value = Array.IndexOf(EventTypeOptions, string.IsNullOrEmpty(evt.eventType) ? "Fixed" : evt.eventType);
        if (eventTypeDropdown.value < 0) eventTypeDropdown.value = 0;
        phaseDropdown.value = Array.IndexOf(PhaseOptions, evt.trigger != null && !string.IsNullOrEmpty(evt.trigger.phase) ? evt.trigger.phase : "RoundStart");
        if (phaseDropdown.value < 0) phaseDropdown.value = 0;

        EventTriggerCondition trigger = evt.trigger ?? new EventTriggerCondition();
        yearInput.text = trigger.year.ToString();
        semesterInput.text = trigger.semester.ToString();
        roundMinInput.text = trigger.roundMin.ToString();
        roundMaxInput.text = trigger.roundMax.ToString();
        specificRoundsInput.text = JoinInts(trigger.specificRounds);
        probabilityInput.text = trigger.triggerChance <= 0f ? "0" : trigger.triggerChance.ToString("0.##");
        triggerBehaviorInput.text = trigger.triggerBehavior ?? string.Empty;
        attributeConditionsInput.text = FormatAttributeConditions(trigger.attributeConditions);
        affinityConditionsInput.text = FormatAffinityConditions(trigger.affinityConditions);
        minMoneyInput.text = trigger.minMoney.ToString();
        maxMoneyInput.text = trigger.maxMoney.ToString();
        minDarknessInput.text = trigger.minDarkness.ToString();
        requiredEventsInput.text = string.Join(",", trigger.requiredEventIds ?? Array.Empty<string>());
        excludedEventsInput.text = string.Join(",", trigger.excludedEventIds ?? Array.Empty<string>());
        requiredFlagsInput.text = string.Join(",", trigger.requiredFlags ?? Array.Empty<string>());
        excludedFlagsInput.text = string.Join(",", trigger.excludedFlags ?? Array.Empty<string>());

        EventDialogue dialogue = evt.dialogues != null && evt.dialogues.Length > 0 ? evt.dialogues[0] : null;
        speakerInput.text = dialogue != null ? dialogue.speaker ?? string.Empty : string.Empty;
        portraitInput.text = dialogue != null ? dialogue.portraitId ?? string.Empty : string.Empty;
        dialogueLinesInput.text = dialogue != null ? string.Join("\n", dialogue.lines ?? Array.Empty<string>()) : string.Empty;

        EventPresentationDefinition presentation = evt.presentation;
        sceneKeyInput.text = presentation != null ? presentation.sceneKey ?? string.Empty : string.Empty;
        sceneDisplayNameInput.text = presentation != null ? presentation.sceneDisplayName ?? string.Empty : string.Empty;
        locationIdInput.text = presentation != null ? presentation.locationId ?? string.Empty : string.Empty;
        backgroundResourceInput.text = presentation != null ? presentation.backgroundResourcePath ?? string.Empty : string.Empty;
        protagonistPortraitInput.text = presentation != null ? presentation.protagonistPortraitResourcePath ?? string.Empty : string.Empty;
        npcPortraitInput.text = presentation != null ? presentation.npcPortraitResourcePath ?? string.Empty : string.Empty;
        backgroundSlotInput.text = presentation != null ? presentation.backgroundSlotName ?? string.Empty : string.Empty;
        protagonistSlotInput.text = presentation != null ? presentation.protagonistSlotName ?? string.Empty : string.Empty;
        npcSlotInput.text = presentation != null ? presentation.npcSlotName ?? string.Empty : string.Empty;

        defaultEffectsInput.text = FormatEffects(evt.defaultEffects);
        chainEventsInput.text = string.Join(",", evt.chainEventIds ?? Array.Empty<string>());

        FillChoice(evt.choices, 0, choiceATextInput, choiceAAPCostInput, choiceAMoneyCostInput, choiceAEffectsInput, choiceANextInput, choiceAShowConditionsInput);
        FillChoice(evt.choices, 1, choiceBTextInput, choiceBAPCostInput, choiceBMoneyCostInput, choiceBEffectsInput, choiceBNextInput, choiceBShowConditionsInput);
        FillChoice(evt.choices, 2, choiceCTextInput, choiceCAPCostInput, choiceCMoneyCostInput, choiceCEffectsInput, choiceCNextInput, choiceCShowConditionsInput);
        UpdateEditorModeLabel();
    }

    private void FillChoice(EventChoice[] choices, int index, TMP_InputField textField, TMP_InputField apField, TMP_InputField moneyField, TMP_InputField effectsField, TMP_InputField nextField, TMP_InputField showConditionsField)
    {
        EventChoice choice = choices != null && index < choices.Length ? choices[index] : null;
        textField.text = choice != null ? choice.text ?? string.Empty : string.Empty;
        apField.text = choice != null ? choice.actionPointCost.ToString() : "0";
        moneyField.text = choice != null ? choice.moneyCost.ToString() : "0";
        effectsField.text = choice != null ? FormatEffects(choice.effects) : string.Empty;
        nextField.text = choice != null ? choice.triggerEventId ?? string.Empty : string.Empty;
        showConditionsField.text = choice != null ? FormatAttributeConditions(choice.showConditions) : string.Empty;
    }

    private void UpdateEditorModeLabel()
    {
        if (editorModeText == null)
        {
            return;
        }

        if (isCreatingNewEvent)
        {
            editorModeText.text = "正在新建事件（仅允许从事件库入口创建）";
            return;
        }

        if (!string.IsNullOrEmpty(editingEventId))
        {
            editorModeText.text = $"正在编辑：{editingEventId}  来源：{editingSourceLabel}";
            return;
        }

        editorModeText.text = "请从上方“回合事件视图”或“事件库视图”的最右侧编辑按钮进入";
    }

    private void RefreshStatus()
    {
        int loaded = EventScheduler.Instance != null ? EventScheduler.Instance.GetLoadedEventCount() : 0;
        int queued = EventScheduler.Instance != null ? EventScheduler.Instance.GetPendingEventCount() : 0;
        int historyCount = EventHistory.Instance != null ? EventHistory.Instance.GetAllRecords().Count : 0;
        int flagCount = EventHistory.Instance != null ? EventHistory.Instance.GetAllFlagsSnapshot().Count : 0;
        int darkness = EventHistory.Instance != null ? EventHistory.Instance.DarknessValue : 0;
        int authored = ZhongshanDeckToolStateBridge.GetAuthoredEvents().Count;

        statusText.text =
            $"已加载 {loaded}   队列 {queued}   历史 {historyCount}   标记 {flagCount}   草稿 {authored}\n" +
            $"黑暗值 {darkness}";
    }

    private void RefreshQueue()
    {
        if (EventScheduler.Instance == null)
        {
            queueText.text = "队列：EventScheduler 尚未就绪";
            return;
        }

        List<EventDefinition> pending = EventScheduler.Instance.GetPendingEventsSnapshot();
        if (pending.Count == 0)
        {
            queueText.text = "队列：空";
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("事件队列");
        for (int i = 0; i < pending.Count; i++)
        {
            EventDefinition evt = pending[i];
            float chance = evt.trigger != null ? evt.trigger.triggerChance : 1f;
            builder.AppendLine($"{i + 1}. {evt.id} | {evt.title} | {evt.GetTriggerPhase()} | p={chance:0.##}");
        }

        queueText.text = builder.ToString().TrimEnd();
    }

    private void RefreshHistory()
    {
        if (EventHistory.Instance == null)
        {
            historyText.text = "历史：EventHistory 尚未就绪";
            return;
        }

        List<EventHistory.EventRecord> records = EventHistory.Instance.GetAllRecords();
        Dictionary<string, bool> flags = EventHistory.Instance.GetAllFlagsSnapshot();
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("近期事件");

        if (records.Count == 0)
        {
            builder.AppendLine("- 无");
        }
        else
        {
            int start = Mathf.Max(0, records.Count - 6);
            for (int i = start; i < records.Count; i++)
            {
                EventHistory.EventRecord record = records[i];
                builder.AppendLine($"- {record.eventId} @ Y{record.triggerYear} S{record.triggerSemester} R{record.triggerRound} choice {record.choiceIndex}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("标记");
        if (flags.Count == 0)
        {
            builder.Append("- 无");
        }
        else
        {
            foreach (KeyValuePair<string, bool> pair in flags.Take(8))
            {
                builder.AppendLine($"- {pair.Key} = {pair.Value}");
            }
        }

        historyText.text = builder.ToString().TrimEnd();
    }

    private void RefreshRuntimeEventDropdown()
    {
        if (roundEventDropdown == null)
        {
            return;
        }

        runtimeSelectionOptions.Clear();

        if (EventScheduler.Instance != null)
        {
            foreach (EventDefinition evt in EventScheduler.Instance.GetAllEventsSnapshot())
            {
                runtimeSelectionOptions.Add(new EventSelectionOption
                {
                    eventId = evt.id,
                    isAuthored = false,
                    label = $"{evt.id} | {evt.title}"
                });
            }
        }

        roundEventDropdown.options = runtimeSelectionOptions.Count == 0
            ? new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("无事件") }
            : runtimeSelectionOptions.Select(option => new TMP_Dropdown.OptionData(option.label)).ToList();
        roundEventDropdown.value = 0;
        roundEventDropdown.RefreshShownValue();
    }

    private void RefreshRoundEventPanel()
    {
        if (roundPanelSummaryText == null || roundEventListContent == null)
        {
            return;
        }

        if (EventScheduler.Instance == null)
        {
            roundPanelSummaryText.text = "EventScheduler 尚未就绪";
            return;
        }

        int year = ParseInt(timelineYearInput, GameState.Instance != null ? GameState.Instance.CurrentYear : 1);
        int semester = ParseInt(timelineSemesterInput, GameState.Instance != null ? GameState.Instance.CurrentSemester : 1);
        int round = ParseInt(timelineRoundInput, GameState.Instance != null ? GameState.Instance.CurrentRound : 1);
        string phaseFilter = timelinePhaseDropdown != null && timelinePhaseDropdown.options.Count > 0
            ? timelinePhaseDropdown.options[timelinePhaseDropdown.value].text
            : "All";

        List<EventDefinition> allEvents = EventScheduler.Instance.GetAllEventsSnapshot();
        List<EventDefinition> visible = new List<EventDefinition>();
        Dictionary<string, string> reasons = new Dictionary<string, string>();
        string[] phases = phaseFilter == "All" ? PhaseOptions : new[] { phaseFilter };

        foreach (string phase in phases)
        {
            visible.AddRange(allEvents
                .Where(evt => evt.GetTriggerPhase().ToString() == phase)
                .Where(evt => MatchesTimelineWindow(evt, year, semester, round))
                .OrderBy(evt => evt.priority)
                .ThenBy(evt => GetPrimaryRound(evt.trigger))
                .ThenBy(evt => evt.id));
        }

        int matchedCount = 0;
        for (int i = 0; i < visible.Count; i++)
        {
            bool matched = EvaluateEventForTimeline(visible[i], year, semester, round, out string reason);
            if (matched)
            {
                matchedCount++;
            }
            reasons[visible[i].id] = matched ? $"可触发 | {reason}" : $"未满足 | {reason}";
        }

        roundPanelSummaryText.text = $"当前查询：Y{year} S{semester} R{round}  阶段：{phaseFilter}  共判定 {visible.Count} 条，当前可触发 {matchedCount} 条";

        ClearChildren(roundEventListContent);
        if (visible.Count == 0)
        {
            CreateListPlaceholder(roundEventListContent, "当前回合没有会进入判定的事件");
            return;
        }

        for (int i = 0; i < visible.Count; i++)
        {
            EventDefinition evt = visible[i];
            CreateEventRow(
                roundEventListContent,
                evt,
                $"{evt.eventType} | P{evt.priority} | {evt.GetTriggerPhase()} | {BuildTimeWindowLabel(evt.trigger)} | {BuildCostSummary(evt)} | {reasons[evt.id]}",
                () => EditRuntimeEventById(evt.id),
                () => RemoveEventFromCurrentRound(evt.id));
        }
    }

    private void RefreshRandomEventPanel()
    {
        if (randomEventSummaryText == null || randomEventListContent == null)
        {
            return;
        }

        ClearChildren(randomEventListContent);

        if (EventScheduler.Instance == null)
        {
            randomEventSummaryText.text = "EventScheduler 尚未就绪";
            CreateListPlaceholder(randomEventListContent, "随机事件系统尚未初始化");
            return;
        }

        if (randomEventsEnabledToggle != null && randomEventsEnabledToggle.isOn != EventScheduler.Instance.RandomEventsEnabled)
        {
            randomEventsEnabledToggle.SetIsOnWithoutNotify(EventScheduler.Instance.RandomEventsEnabled);
        }

        TriggerPhase phase = GetSelectedRandomPhase();
        List<EventDefinition> randomEvents = EventScheduler.Instance.GetAllEventsSnapshot()
            .Where(evt => evt.GetEventType() == EventType.Random)
            .ToList();
        List<EventDefinition> candidates = EventScheduler.Instance.GetRandomEventCandidates(phase, includeProbabilityCheck: false);
        string location = GameState.Instance != null ? GameState.Instance.CurrentLocation.ToString() : "未知";

        randomEventSummaryText.text =
            $"自然随机：{(EventScheduler.Instance.RandomEventsEnabled ? "开启" : "关闭")}   当前地点：{location}   阶段：{phase}\n" +
            $"随机事件总数 {randomEvents.Count} 条，当前条件可进入抽取池 {candidates.Count} 条。";

        if (candidates.Count == 0)
        {
            CreateListPlaceholder(randomEventListContent, "当前地点/状态没有可抽取的随机事件。可以切换阶段，或用事件库直接强制触发指定事件。");
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            EventDefinition evt = candidates[i];
            float chance = evt.trigger != null ? evt.trigger.triggerChance : 1f;
            CreateRandomEventRow(
                evt,
                $"{BuildRandomLocationLabel(evt.trigger)} | {BuildTimeWindowLabel(evt.trigger)} | p={chance:0.##} | {BuildCostSummary(evt)}");
        }
    }

    private void RefreshEventLibraryPanel()
    {
        if (eventCatalogText == null || libraryEventListContent == null)
        {
            return;
        }

        string keyword = SafeText(librarySearchInput).ToLowerInvariant();
        string group = libraryGroupDropdown != null && libraryGroupDropdown.options.Count > 0
            ? libraryGroupDropdown.options[libraryGroupDropdown.value].text
            : "全部";
        eventSelectionOptions.Clear();
        ClearChildren(libraryEventListContent);

        if (EventScheduler.Instance != null)
        {
            foreach (EventDefinition evt in EventScheduler.Instance.GetAllEventsSnapshot())
            {
                string source = GetEventSourceLabel(evt);
                if (MatchesLibraryGroup(evt, group) &&
                    MatchesLibrarySearch(evt.id, evt.title, $"{evt.eventType} {source}", keyword))
                {
                    eventSelectionOptions.Add(new EventSelectionOption
                    {
                        eventId = evt.id,
                        isAuthored = false,
                        label = $"[运行时] {evt.id} | {evt.title}"
                    });
                }
            }
        }

        List<ZhongshanDeckEventEntry> authoredEntries = ZhongshanDeckToolStateBridge.GetAuthoredEvents();
        for (int i = 0; i < authoredEntries.Count; i++)
        {
            ZhongshanDeckEventEntry entry = authoredEntries[i];
            if (entry == null || !MatchesLibrarySearch(entry.eventId, entry.title, "草稿 zhongshan_deck", keyword))
            {
                continue;
            }

            eventSelectionOptions.Add(new EventSelectionOption
            {
                eventId = entry.eventId,
                isAuthored = true,
                label = $"[草稿] {entry.eventId} | {entry.title}"
            });
        }

        eventCatalogText.text = $"事件库结果：{eventSelectionOptions.Count} 条   分组：{group}   提示：随机事件来自 random_events.json，事件 ID 以 R_ 开头";

        if (eventSelectionOptions.Count == 0)
        {
            CreateListPlaceholder(libraryEventListContent, "没有匹配到事件");
            return;
        }

        for (int i = 0; i < eventSelectionOptions.Count; i++)
        {
            EventSelectionOption option = eventSelectionOptions[i];
            string subtitle = BuildLibrarySubtitle(option);
            CreateLibraryRow(option, subtitle);
        }
    }

    private void RefreshPreview()
    {
        if (!TryBuildEventDefinition(out EventDefinition evt, out string error))
        {
            previewText.text = $"预览\n- {error}";
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("当前草稿预览");
        builder.AppendLine($"{evt.id} | {evt.title}");
        builder.AppendLine($"类型 {evt.eventType} | 阶段 {evt.trigger.phase} | 优先级 {evt.priority}");
        builder.AppendLine($"事件成本 AP {evt.actionPointCost} | 金钱 {evt.moneyCost} | 每回合上限 {evt.maxTriggersPerRound}");
        builder.AppendLine($"指定回合 {JoinInts(evt.trigger.specificRounds)} | 概率 {evt.trigger.triggerChance:0.##}");
        builder.AppendLine($"属性条件 {FormatAttributeConditions(evt.trigger.attributeConditions)}");
        builder.AppendLine($"好感条件 {FormatAffinityConditions(evt.trigger.affinityConditions)}");
        builder.AppendLine($"金钱 {evt.trigger.minMoney}~{evt.trigger.maxMoney} | 黑暗值 >= {evt.trigger.minDarkness}");
        builder.AppendLine($"前置 {string.Join(",", evt.trigger.requiredEventIds ?? Array.Empty<string>())}");
        builder.AppendLine($"排除 {string.Join(",", evt.trigger.excludedEventIds ?? Array.Empty<string>())}");
        builder.AppendLine($"前置标记 {string.Join(",", evt.trigger.requiredFlags ?? Array.Empty<string>())}");
        builder.AppendLine($"排除标记 {string.Join(",", evt.trigger.excludedFlags ?? Array.Empty<string>())}");
        builder.AppendLine($"场景演出 {FormatPresentation(evt.presentation)}");
        builder.AppendLine();
        builder.AppendLine("对话");

        EventDialogue dialogue = evt.dialogues[0];
        for (int i = 0; i < dialogue.lines.Length; i++)
        {
            builder.AppendLine($"- {dialogue.speaker}: {dialogue.lines[i]}");
        }

        builder.AppendLine();
        builder.AppendLine("选项");
        if (evt.choices == null || evt.choices.Length == 0)
        {
            builder.AppendLine("- 无，走默认效果");
        }
        else
        {
            for (int i = 0; i < evt.choices.Length; i++)
            {
                builder.AppendLine($"- {evt.choices[i].text} | AP {evt.choices[i].actionPointCost} | 金钱 {evt.choices[i].moneyCost} | 显示 {FormatAttributeConditions(evt.choices[i].showConditions)} => {FormatEffects(evt.choices[i].effects)}");
            }
        }

        builder.AppendLine();
        builder.Append($"默认效果 {FormatEffects(evt.defaultEffects)}");

        if (EventScheduler.Instance != null)
        {
            List<EventScheduler.EventValidationIssue> issues = EventScheduler.Instance.GetValidationIssuesSnapshot()
                .Where(issue => issue.eventId == evt.id)
                .ToList();
            if (issues.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine("校验提醒");
                for (int i = 0; i < issues.Count; i++)
                {
                    builder.AppendLine($"- [{issues[i].severity}] {issues[i].message}");
                }
            }
        }
        previewText.text = builder.ToString().TrimEnd();
    }

    private string[] ParseCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<string>();

        return csv.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrEmpty(item))
            .ToArray();
    }

    private int[] ParseIntArray(string csv)
    {
        string[] parts = ParseCsv(csv);
        List<int> values = new List<int>();
        for (int i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out int value))
                values.Add(value);
        }

        return values.ToArray();
    }

    private string[] ParseMultiline(TMP_InputField input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return Array.Empty<string>();

        return input.text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line))
            .ToArray();
    }

    private EventEffect[] ParseEffects(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return Array.Empty<EventEffect>();

        string[] entries = source.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        List<EventEffect> effects = new List<EventEffect>();

        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i].Trim();
            if (string.IsNullOrEmpty(entry))
                continue;

            string[] parts = entry.Split(':');
            if (parts.Length < 3)
                continue;

            int value = 0;
            int.TryParse(parts[2].Trim(), out value);
            string type = parts[0].Trim();
            string target = parts[1].Trim();

            effects.Add(new EventEffect
            {
                type = type,
                target = target,
                value = value,
                description = BuildEffectDescription(type, target, value)
            });
        }

        return effects.ToArray();
    }

    private AttributeCondition[] ParseAttributeConditions(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return Array.Empty<AttributeCondition>();

        string[] operators = { ">=", "<=", "==", "!=", ">", "<" };
        string[] entries = source.Split(new[] { ',', '，', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        List<AttributeCondition> conditions = new List<AttributeCondition>();

        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i].Trim();
            for (int j = 0; j < operators.Length; j++)
            {
                string op = operators[j];
                int opIndex = entry.IndexOf(op, StringComparison.Ordinal);
                if (opIndex < 0)
                    continue;

                string attrName = entry.Substring(0, opIndex).Trim();
                string valueText = entry.Substring(opIndex + op.Length).Trim();
                if (!int.TryParse(valueText, out int value) || string.IsNullOrEmpty(attrName))
                    break;

                conditions.Add(new AttributeCondition
                {
                    attributeName = attrName,
                    comparison = op,
                    value = value
                });
                break;
            }
        }

        return conditions.ToArray();
    }

    private AffinityCondition[] ParseAffinityConditions(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return Array.Empty<AffinityCondition>();

        string[] entries = source.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        List<AffinityCondition> conditions = new List<AffinityCondition>();

        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i].Trim();
            if (string.IsNullOrEmpty(entry))
                continue;

            string[] parts = entry.Split(':');
            string npcId = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            string minLevel = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            int minValue = 0;
            if (parts.Length > 2)
                int.TryParse(parts[2].Trim(), out minValue);

            if (string.IsNullOrEmpty(npcId))
                continue;

            conditions.Add(new AffinityCondition
            {
                npcId = npcId,
                minLevel = minLevel,
                minValue = minValue
            });
        }

        return conditions.ToArray();
    }

    private string BuildEffectDescription(string type, string target, int value)
    {
            switch (type)
            {
                case "attribute":
                    return $"{target} {(value >= 0 ? "+" : string.Empty)}{value}";
                case "money":
                    return $"金钱 {(value >= 0 ? "+" : string.Empty)}{value}";
                case "actionpoint":
                case "action_point":
                case "ap":
                    return $"行动点 {(value >= 0 ? "+" : string.Empty)}{value}";
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

    private string FormatEffects(EventEffect[] effects)
    {
        if (effects == null || effects.Length == 0)
            return "无";

        return string.Join("; ", effects.Select(effect => $"{effect.type}:{effect.target}:{effect.value}"));
    }

    private string FormatAttributeConditions(AttributeCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return "-";

        return string.Join(", ", conditions.Select(condition => $"{condition.attributeName}{condition.comparison}{condition.value}"));
    }

    private string FormatAffinityConditions(AffinityCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return "-";

        return string.Join("; ", conditions.Select(condition =>
        {
            string level = string.IsNullOrEmpty(condition.minLevel) ? string.Empty : condition.minLevel;
            string value = condition.minValue > 0 ? condition.minValue.ToString() : string.Empty;
            return $"{condition.npcId}:{level}:{value}".TrimEnd(':');
        }));
    }

    private int ParseInt(TMP_InputField input, int fallback)
    {
        return int.TryParse(SafeText(input), out int value) ? value : fallback;
    }

    private float ParseFloat(TMP_InputField input, float fallback)
    {
        return float.TryParse(SafeText(input), out float value) ? value : fallback;
    }

    private string SafeText(TMP_InputField input)
    {
        return input != null ? input.text.Trim() : string.Empty;
    }

    private string JoinInts(int[] values)
    {
        return values == null || values.Length == 0 ? "-" : string.Join(",", values);
    }

    private string BuildCostSummary(EventDefinition evt)
    {
        if (evt == null)
        {
            return "成本 -";
        }

        List<string> bits = new List<string>();
        if (evt.actionPointCost > 0)
            bits.Add($"AP {evt.actionPointCost}");
        if (evt.moneyCost != 0)
            bits.Add($"¥ {evt.moneyCost}");
        if (evt.maxTriggersPerRound > 0)
            bits.Add($"上限 {evt.maxTriggersPerRound}/回合");

        return bits.Count == 0 ? "无额外成本" : string.Join(" | ", bits);
    }

    private string BuildLibrarySubtitle(EventSelectionOption option)
    {
        if (option == null)
        {
            return string.Empty;
        }

        if (option.isAuthored && ZhongshanDeckToolStateBridge.TryGetAuthoredEvent(option.eventId, out ZhongshanDeckEventEntry entry))
        {
            EventDatabaseRoot root = JsonUtility.FromJson<EventDatabaseRoot>(entry.json);
            EventDefinition evt = root != null && root.events != null && root.events.Length > 0 ? root.events[0] : null;
            return evt == null ? "草稿事件" : $"草稿 | {evt.eventType} | {BuildCostSummary(evt)}";
        }

        EventDefinition runtime = EventScheduler.Instance != null ? EventScheduler.Instance.GetEvent(option.eventId) : null;
        return runtime == null ? "运行时事件" : $"运行时 | {runtime.eventType} | {GetEventSourceLabel(runtime)} | {BuildCostSummary(runtime)}";
    }

    private string GetEventSourceLabel(EventDefinition evt)
    {
        if (evt == null || string.IsNullOrWhiteSpace(evt.sourceFileName))
        {
            return "来源未知";
        }

        return evt.sourceFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? evt.sourceFileName
            : $"{evt.sourceFileName}.json";
    }

    private bool MatchesLibraryGroup(EventDefinition evt, string group)
    {
        if (evt == null || string.IsNullOrEmpty(group) || group == "全部")
        {
            return true;
        }

        EventType type = evt.GetEventType();
        switch (group)
        {
            case "随机事件":
                return type == EventType.Random || string.Equals(evt.sourceFileName, "random_events", StringComparison.OrdinalIgnoreCase);
            case "主线":
                return type == EventType.MainStory;
            case "黑暗":
                return type == EventType.Dark;
            case "有场景演出":
                return evt.presentation != null;
            case "考试结果":
                return ContainsEventKeyword(evt, "考试") || ContainsEventKeyword(evt, "exam");
            case "证书考试":
                return ContainsEventKeyword(evt, "证书") || ContainsEventKeyword(evt, "CET") || ContainsEventKeyword(evt, "四级") || ContainsEventKeyword(evt, "六级");
            case "补考":
                return ContainsEventKeyword(evt, "补考");
            case "NPC关系":
                return evt.trigger != null && evt.trigger.affinityConditions != null && evt.trigger.affinityConditions.Length > 0;
            case "恋爱":
                return evt.trigger != null && evt.trigger.romanceConditions != null && evt.trigger.romanceConditions.Length > 0;
            case "社团":
                return evt.trigger != null && evt.trigger.clubConditions != null && evt.trigger.clubConditions.Length > 0;
            default:
                return true;
        }
    }

    private bool ContainsEventKeyword(EventDefinition evt, string keyword)
    {
        if (evt == null || string.IsNullOrEmpty(keyword))
        {
            return false;
        }

        string haystack = $"{evt.id} {evt.title} {evt.description} {evt.eventType} {evt.sourceFileName}";
        return haystack.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void UseCurrentTimelineValues()
    {
        if (GameState.Instance == null)
        {
            statusText.text = "GameState 尚未就绪";
            return;
        }

        timelineYearInput.text = GameState.Instance.CurrentYear.ToString();
        timelineSemesterInput.text = GameState.Instance.CurrentSemester.ToString();
        timelineRoundInput.text = GameState.Instance.CurrentRound.ToString();
        Refresh();
    }

    private void AddSelectedRuntimeEventToCurrentRound()
    {
        if (EventScheduler.Instance == null || runtimeSelectionOptions.Count == 0)
        {
            statusText.text = "没有可加入的运行时事件";
            return;
        }

        int index = Mathf.Clamp(roundEventDropdown.value, 0, runtimeSelectionOptions.Count - 1);
        EventSelectionOption option = runtimeSelectionOptions[index];
        EventDefinition evt = EventScheduler.Instance.GetEvent(option.eventId);
        if (evt == null)
        {
            statusText.text = $"未找到运行时事件：{option.eventId}";
            return;
        }

        EventDefinition clone = CloneEvent(evt);
        EnsureRoundBinding(clone, ParseInt(timelineYearInput, 1), ParseInt(timelineSemesterInput, 1), ParseInt(timelineRoundInput, 1), GetSelectedTimelinePhase());
        EventScheduler.Instance.RegisterOrReplaceRuntimeEvent(clone);
        statusText.text = $"已将 {clone.id} 绑定到当前回合";
        Refresh();
    }

    private void RemoveEventFromCurrentRound(string eventId)
    {
        if (EventScheduler.Instance == null)
        {
            statusText.text = "EventScheduler 尚未就绪";
            return;
        }

        EventDefinition evt = EventScheduler.Instance.GetEvent(eventId);
        if (evt == null)
        {
            statusText.text = $"未找到事件：{eventId}";
            return;
        }

        EventDefinition clone = CloneEvent(evt);
        RemoveRoundBinding(clone, ParseInt(timelineYearInput, 1), ParseInt(timelineSemesterInput, 1), ParseInt(timelineRoundInput, 1));
        EventScheduler.Instance.RegisterOrReplaceRuntimeEvent(clone);
        statusText.text = $"已从当前回合移除 {clone.id}";
        Refresh();
    }

    private void EditRuntimeEventById(string eventId)
    {
        eventIdInput.text = eventId;
        LoadRuntimeEvent();
    }

    private void EditAuthoredEventById(string eventId)
    {
        eventIdInput.text = eventId;
        LoadAuthoredEvent();
    }

    private bool EvaluateEventForTimeline(EventDefinition evt, int year, int semester, int round, out string reason)
    {
        if (evt == null)
        {
            reason = "空事件";
            return false;
        }

        if (!evt.isRepeatable && EventHistory.Instance != null && EventHistory.Instance.HasTriggered(evt.id))
        {
            reason = "已触发且不可重复";
            return false;
        }

        if (evt.maxTriggersPerRound > 0 &&
            EventHistory.Instance != null &&
            EventHistory.Instance.GetTriggerCountForRound(evt.id, year, semester, round) >= evt.maxTriggersPerRound)
        {
            reason = $"本回合已达上限 {evt.maxTriggersPerRound}";
            return false;
        }

        EventTriggerCondition trigger = evt.trigger;
        if (trigger == null)
        {
            reason = "无触发条件";
            return true;
        }

        if (trigger.year > 0 && trigger.year != year)
        {
            reason = $"学年要求 {trigger.year}";
            return false;
        }

        if (trigger.semester > 0 && trigger.semester != semester)
        {
            reason = $"学期要求 {trigger.semester}";
            return false;
        }

        if (trigger.roundMin > 0 && round < trigger.roundMin)
        {
            reason = $"回合需 >= {trigger.roundMin}";
            return false;
        }

        if (trigger.roundMax > 0 && round > trigger.roundMax)
        {
            reason = $"回合需 <= {trigger.roundMax}";
            return false;
        }

        if (trigger.specificRounds != null && trigger.specificRounds.Length > 0 && !trigger.specificRounds.Contains(round))
        {
            reason = $"指定回合 {JoinInts(trigger.specificRounds)}";
            return false;
        }

        if (EventScheduler.Instance != null && !EventScheduler.Instance.CheckCondition(BuildConditionForTimeline(trigger, year, semester, round)))
        {
            reason = BuildFailureSummary(trigger);
            return false;
        }

        float chance = trigger.triggerChance <= 0f ? 0f : trigger.triggerChance;
        reason = chance >= 1f ? "条件满足" : $"条件满足，概率 {chance:0.##}";
        return true;
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

        if (trigger.year > 0 && trigger.year != year)
            return false;
        if (trigger.semester > 0 && trigger.semester != semester)
            return false;
        if (trigger.roundMin > 0 && round < trigger.roundMin)
            return false;
        if (trigger.roundMax > 0 && round > trigger.roundMax)
            return false;
        if (trigger.specificRounds != null && trigger.specificRounds.Length > 0 && !trigger.specificRounds.Contains(round))
            return false;

        return true;
    }

    private string GetSelectedTimelinePhase()
    {
        if (timelinePhaseDropdown == null || timelinePhaseDropdown.options.Count == 0)
        {
            return PhaseOptions[0];
        }

        string selected = timelinePhaseDropdown.options[timelinePhaseDropdown.value].text;
        return selected == "All" ? PhaseOptions[0] : selected;
    }

    private TriggerPhase GetSelectedRandomPhase()
    {
        string phaseName = PhaseOptions[0];
        if (randomPhaseDropdown != null && randomPhaseDropdown.options.Count > 0)
        {
            phaseName = randomPhaseDropdown.options[randomPhaseDropdown.value].text;
        }

        return Enum.TryParse(phaseName, true, out TriggerPhase phase)
            ? phase
            : TriggerPhase.ActionComplete;
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

        List<int> rounds = evt.trigger.specificRounds != null
            ? new List<int>(evt.trigger.specificRounds)
            : new List<int>();

        if (!rounds.Contains(round))
        {
            rounds.Add(round);
        }

        rounds.Sort();
        evt.trigger.specificRounds = rounds.ToArray();
    }

    private void RemoveRoundBinding(EventDefinition evt, int year, int semester, int round)
    {
        if (evt.trigger == null)
        {
            return;
        }

        if (evt.trigger.year > 0 && evt.trigger.year != year)
        {
            return;
        }

        if (evt.trigger.semester > 0 && evt.trigger.semester != semester)
        {
            return;
        }

        if (evt.trigger.specificRounds == null || evt.trigger.specificRounds.Length == 0)
        {
            evt.trigger.roundMin = 0;
            evt.trigger.roundMax = 0;
            return;
        }

        List<int> rounds = new List<int>(evt.trigger.specificRounds);
        rounds.RemoveAll(value => value == round);
        evt.trigger.specificRounds = rounds.ToArray();
    }

    private EventTriggerCondition BuildConditionForTimeline(EventTriggerCondition source, int year, int semester, int round)
    {
        return new EventTriggerCondition
        {
            year = 0,
            semester = 0,
            roundMin = 0,
            roundMax = 0,
            specificRounds = Array.Empty<int>(),
            attributeConditions = source != null ? source.attributeConditions : Array.Empty<AttributeCondition>(),
            minMoney = source != null ? source.minMoney : 0,
            maxMoney = source != null ? source.maxMoney : 0,
            affinityConditions = source != null ? source.affinityConditions : Array.Empty<AffinityCondition>(),
            requiredEventIds = source != null ? source.requiredEventIds : Array.Empty<string>(),
            excludedEventIds = source != null ? source.excludedEventIds : Array.Empty<string>(),
            requiredFlags = source != null ? source.requiredFlags : Array.Empty<string>(),
            excludedFlags = source != null ? source.excludedFlags : Array.Empty<string>(),
            minDarkness = source != null ? source.minDarkness : 0,
            triggerBehavior = source != null ? source.triggerBehavior : string.Empty,
            triggerChance = source != null ? source.triggerChance : 1f,
            requiredLocationId = source != null ? source.requiredLocationId : string.Empty,
            requiredLocationIds = source != null ? source.requiredLocationIds : Array.Empty<string>(),
            phase = source != null ? source.phase : string.Empty
        };
    }

    private string BuildFailureSummary(EventTriggerCondition trigger)
    {
        List<string> bits = new List<string>();
        if (trigger == null)
        {
            return "条件不满足";
        }

        if (trigger.attributeConditions != null && trigger.attributeConditions.Length > 0)
            bits.Add($"属性 {FormatAttributeConditions(trigger.attributeConditions)}");
        if (trigger.minMoney > 0 || trigger.maxMoney > 0)
            bits.Add($"金钱 {trigger.minMoney}~{trigger.maxMoney}");
        if (trigger.minDarkness > 0)
            bits.Add($"黑暗值>={trigger.minDarkness}");
        if (trigger.requiredEventIds != null && trigger.requiredEventIds.Length > 0)
            bits.Add($"前置 {string.Join(",", trigger.requiredEventIds)}");
        if (trigger.excludedEventIds != null && trigger.excludedEventIds.Length > 0)
            bits.Add($"排除 {string.Join(",", trigger.excludedEventIds)}");
        if (trigger.requiredFlags != null && trigger.requiredFlags.Length > 0)
            bits.Add($"前置标记 {string.Join(",", trigger.requiredFlags)}");
        if (trigger.excludedFlags != null && trigger.excludedFlags.Length > 0)
            bits.Add($"排除标记 {string.Join(",", trigger.excludedFlags)}");
        if (trigger.affinityConditions != null && trigger.affinityConditions.Length > 0)
            bits.Add("好感条件");
        string locationLabel = BuildRandomLocationLabel(trigger);
        if (locationLabel != "地点不限")
            bits.Add(locationLabel);

        return bits.Count > 0 ? string.Join(" | ", bits) : "条件不满足";
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
            bits.Add($"R[{JoinInts(trigger.specificRounds)}]");
        }
        else
        {
            if (trigger.roundMin > 0 || trigger.roundMax > 0)
                bits.Add($"R{trigger.roundMin}-{trigger.roundMax}");
        }

        return bits.Count > 0 ? string.Join(" ", bits) : "不限";
    }

    private int GetPrimaryRound(EventTriggerCondition trigger)
    {
        if (trigger == null)
            return int.MaxValue;

        if (trigger.specificRounds != null && trigger.specificRounds.Length > 0)
            return trigger.specificRounds.Min();

        if (trigger.roundMin > 0)
            return trigger.roundMin;

        return int.MaxValue - 1;
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

    private string FormatPresentation(EventPresentationDefinition presentation)
    {
        if (presentation == null)
        {
            return "未配置";
        }

        List<string> parts = new List<string>();
        if (!string.IsNullOrEmpty(presentation.sceneDisplayName))
            parts.Add(presentation.sceneDisplayName);
        else if (!string.IsNullOrEmpty(presentation.sceneKey))
            parts.Add(presentation.sceneKey);

        if (!string.IsNullOrEmpty(presentation.locationId))
            parts.Add($"地点 {presentation.locationId}");
        if (!string.IsNullOrEmpty(presentation.backgroundResourcePath) || !string.IsNullOrEmpty(presentation.backgroundSlotName))
            parts.Add($"背景 {(!string.IsNullOrEmpty(presentation.backgroundResourcePath) ? presentation.backgroundResourcePath : presentation.backgroundSlotName)}");
        if (!string.IsNullOrEmpty(presentation.protagonistPortraitResourcePath) || !string.IsNullOrEmpty(presentation.protagonistSlotName))
            parts.Add($"主角 {(!string.IsNullOrEmpty(presentation.protagonistPortraitResourcePath) ? presentation.protagonistPortraitResourcePath : presentation.protagonistSlotName)}");
        if (!string.IsNullOrEmpty(presentation.npcPortraitResourcePath) || !string.IsNullOrEmpty(presentation.npcSlotName))
            parts.Add($"NPC {(!string.IsNullOrEmpty(presentation.npcPortraitResourcePath) ? presentation.npcPortraitResourcePath : presentation.npcSlotName)}");

        return parts.Count > 0 ? string.Join(" | ", parts) : "未配置";
    }

    private string BuildRandomLocationLabel(EventTriggerCondition trigger)
    {
        if (trigger == null)
        {
            return "地点不限";
        }

        List<string> locations = new List<string>();
        if (!string.IsNullOrWhiteSpace(trigger.requiredLocationId))
        {
            locations.Add(trigger.requiredLocationId);
        }

        if (trigger.requiredLocationIds != null)
        {
            for (int i = 0; i < trigger.requiredLocationIds.Length; i++)
            {
                string locationId = trigger.requiredLocationIds[i];
                if (!string.IsNullOrWhiteSpace(locationId) && !locations.Contains(locationId))
                {
                    locations.Add(locationId);
                }
            }
        }

        return locations.Count == 0 ? "地点不限" : $"地点 {string.Join("/", locations)}";
    }

    private void CreateRandomEventRow(EventDefinition evt, string subtitle)
    {
        GameObject row = CreatePanel(randomEventListContent, 58f);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateLabel(row.transform, $"{evt.id} | {evt.title}", 13f, TextWhite, 20f, 360f);
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI meta = CreateLabel(row.transform, subtitle, 12f, TextGray, 20f, 300f);
        meta.enableWordWrapping = false;
        meta.overflowMode = TextOverflowModes.Ellipsis;

        CreateButton(row.transform, "触发", 70f, BtnGreen, () =>
        {
            eventIdInput.text = evt.id;
            ForceTriggerEvent();
        });
        CreateButton(row.transform, "编辑", 70f, BtnBlue, () => EditRuntimeEventById(evt.id));
    }

    private void CreateLibraryRow(EventSelectionOption option, string subtitle)
    {
        GameObject row = CreatePanel(libraryEventListContent, 58f);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateLabel(row.transform, option.label, 13f, TextWhite, 20f, 620f);
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI meta = CreateLabel(row.transform, subtitle, 12f, TextGray, 20f, 120f);
        meta.alignment = TextAlignmentOptions.Center;

        UnityEngine.Events.UnityAction onEdit = option.isAuthored
            ? new UnityEngine.Events.UnityAction(() => EditAuthoredEventById(option.eventId))
            : new UnityEngine.Events.UnityAction(() => EditRuntimeEventById(option.eventId));
        CreateButton(row.transform, "编辑", 70f, BtnBlue, onEdit);

        if (option.isAuthored)
        {
            CreateButton(row.transform, "删草稿", 78f, BtnRed, () => DeleteAuthoredEventById(option.eventId));
        }
    }

    private void CreateEventRow(Transform parent, EventDefinition evt, string subtitle, Action onEdit, Action onRemoveRound)
    {
        GameObject row = CreatePanel(parent, 62f);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateLabel(row.transform, $"{evt.id} | {evt.title}", 13f, TextWhite, 20f, 420f);
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI meta = CreateLabel(row.transform, subtitle, 12f, TextGray, 20f, 240f);
        meta.enableWordWrapping = false;
        meta.overflowMode = TextOverflowModes.Ellipsis;

        CreateButton(row.transform, "编辑", 70f, BtnBlue, () => onEdit?.Invoke());
        CreateButton(row.transform, "移出本回合", 96f, BtnRed, () => onRemoveRound?.Invoke());
    }

    private Transform CreateListHost(Transform parent, float height)
    {
        GameObject host = CreatePanel(parent, height);

        ScrollRect scrollRect = host.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 24f;

        GameObject viewport = CreateUIElement("Viewport", host.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;
        return content.transform;
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void CreateListPlaceholder(Transform parent, string text)
    {
        TextMeshProUGUI label = CreateLabel(parent, text, 13f, TextGray, 28f);
        label.alignment = TextAlignmentOptions.Center;
    }

    private bool MatchesLibrarySearch(string eventId, string title, string extra, string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return true;
        }

        string haystack = $"{eventId} {title} {extra}".ToLowerInvariant();
        return haystack.Contains(keyword);
    }

    private GameObject CreatePanel(Transform parent, float preferredHeight)
    {
        GameObject panel = CreateUIElement("Panel", parent);
        Image image = panel.AddComponent<Image>();
        image.color = Panel;
        LayoutElement element = panel.AddComponent<LayoutElement>();
        element.preferredHeight = preferredHeight;
        return panel;
    }

    private void CreateSectionTitle(Transform parent, string text)
    {
        CreateLabel(parent, text, 16f, TextGold, 26f);
    }

    private GameObject CreateRow(Transform parent, float height)
    {
        GameObject row = CreateUIElement("Row", parent);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        return row;
    }

    private void SetFlexibleWidth(GameObject target, float minWidth)
    {
        if (target == null)
        {
            return;
        }

        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = target.AddComponent<LayoutElement>();
        }

        layout.flexibleWidth = 1f;
        layout.minWidth = minWidth;
    }

    private void CreateButton(Transform parent, string label, float width, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = CreateUIElement($"Btn_{label}", parent);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 32f);

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;

        Image bg = buttonObj.AddComponent<Image>();
        bg.color = bgColor;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObj.transform, label, 13f, TextWhite, 32f);
        text.alignment = TextAlignmentOptions.Center;
        StretchFull(text.GetComponent<RectTransform>());
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height, string defaultValue = "")
    {
        GameObject inputObj = CreateUIElement("InputField", parent);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement layout = inputObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        Image bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.90f);

        GameObject textArea = CreateUIElement("TextArea", inputObj.transform);
        RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(8, 2);
        textAreaRT.offsetMax = new Vector2(-8, -2);
        textArea.AddComponent<RectMask2D>();

        GameObject textObj = CreateUIElement("Text", textArea.transform);
        StretchFull(textObj.GetComponent<RectTransform>());
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 13f;
        text.color = TextWhite;
        text.alignment = TextAlignmentOptions.Left;
        ApplyChineseFont(text);

        GameObject placeholderObj = CreateUIElement("Placeholder", textArea.transform);
        StretchFull(placeholderObj.GetComponent<RectTransform>());
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 13f;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        ApplyChineseFont(placeholderText);

        TMP_InputField input = inputObj.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRT;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.fontAsset = FontManager.Instance != null ? FontManager.Instance.ChineseFont : null;
        input.text = defaultValue;
        return input;
    }

    private TMP_InputField CreateMultilineInput(Transform parent, string placeholder, float height)
    {
        TMP_InputField input = CreateInputField(parent, placeholder, 0f, height);
        LayoutElement layout = input.GetComponent<LayoutElement>();
        layout.preferredWidth = -1f;
        layout.flexibleWidth = 1f;
        layout.preferredHeight = height;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.textComponent.enableWordWrapping = true;
        input.textViewport.offsetMin = new Vector2(8, 6);
        input.textViewport.offsetMax = new Vector2(-8, -6);
        return input;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string[] options, float width, float height)
    {
        GameObject dropdownObj = CreateUIElement("Dropdown", parent);
        RectTransform rt = dropdownObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement layout = dropdownObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        Image bg = dropdownObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

        TextMeshProUGUI caption = CreateLabel(dropdownObj.transform, options.Length > 0 ? options[0] : string.Empty, 13f, TextWhite, height);
        caption.margin = new Vector4(8f, 3f, 24f, 3f);
        dropdown.captionText = caption;

        GameObject arrowObj = CreateUIElement("Arrow", dropdownObj.transform);
        RectTransform arrowRt = arrowObj.GetComponent<RectTransform>();
        arrowRt.anchorMin = new Vector2(1f, 0.5f);
        arrowRt.anchorMax = new Vector2(1f, 0.5f);
        arrowRt.pivot = new Vector2(1f, 0.5f);
        arrowRt.sizeDelta = new Vector2(20f, height);
        arrowRt.anchoredPosition = new Vector2(-4f, 0f);
        TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "v";
        arrowText.fontSize = 13f;
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.color = TextGray;
        ApplyChineseFont(arrowText);

        GameObject template = CreateUIElement("Template", dropdownObj.transform);
        RectTransform templateRt = template.GetComponent<RectTransform>();
        templateRt.anchorMin = new Vector2(0f, 0f);
        templateRt.anchorMax = new Vector2(1f, 0f);
        templateRt.pivot = new Vector2(0.5f, 1f);
        templateRt.anchoredPosition = new Vector2(0f, -height);
        templateRt.sizeDelta = new Vector2(0f, Mathf.Max(80f, options.Length * 28f + 10f));
        Image templateBg = template.AddComponent<Image>();
        templateBg.color = new Color(0.10f, 0.10f, 0.14f, 0.98f);
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        template.SetActive(false);

        GameObject viewport = CreateUIElement("Viewport", template.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 2f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRt;

        GameObject item = CreateUIElement("Item", content.transform);
        Toggle itemToggle = item.AddComponent<Toggle>();
        Image itemBg = item.AddComponent<Image>();
        itemBg.color = new Color(0.18f, 0.18f, 0.24f, 1f);
        itemToggle.targetGraphic = itemBg;

        RectTransform itemRt = item.GetComponent<RectTransform>();
        itemRt.sizeDelta = new Vector2(0f, 24f);

        GameObject itemLabelObj = CreateUIElement("Item Label", item.transform);
        StretchFull(itemLabelObj.GetComponent<RectTransform>());
        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.fontSize = 13f;
        itemLabel.color = TextWhite;
        itemLabel.alignment = TextAlignmentOptions.Left;
        itemLabel.margin = new Vector4(8f, 2f, 8f, 2f);
        ApplyChineseFont(itemLabel);

        dropdown.template = templateRt;
        dropdown.itemText = itemLabel;
        dropdown.options = options.Select(option => new TMP_Dropdown.OptionData(option)).ToList();
        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private Toggle CreateToggle(Transform parent, string label, bool defaultValue = false)
    {
        GameObject toggleObj = CreateUIElement($"Toggle_{label}", parent);
        RectTransform rt = toggleObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(88f, 30f);

        LayoutElement layout = toggleObj.AddComponent<LayoutElement>();
        layout.preferredWidth = 88f;
        layout.preferredHeight = 30f;

        Toggle toggle = toggleObj.AddComponent<Toggle>();

        GameObject bgObj = CreateUIElement("Background", toggleObj.transform);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(0f, 0.5f);
        bgRt.pivot = new Vector2(0f, 0.5f);
        bgRt.sizeDelta = new Vector2(18f, 18f);
        bgRt.anchoredPosition = new Vector2(0f, 0f);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.16f, 0.22f, 1f);

        GameObject checkObj = CreateUIElement("Checkmark", bgObj.transform);
        StretchFull(checkObj.GetComponent<RectTransform>());
        Image check = checkObj.AddComponent<Image>();
        check.color = TextGold;

        toggle.graphic = check;
        toggle.targetGraphic = bg;

        TextMeshProUGUI text = CreateLabel(toggleObj.transform, label, 13f, TextWhite, 30f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(24f, 0f);
        textRt.offsetMax = Vector2.zero;

        toggle.isOn = defaultValue;
        return toggle;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height, float width = 0f)
    {
        GameObject obj = CreateUIElement("Label", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        float resolvedHeight = Mathf.Max(height, fontSize + 14f);
        rt.sizeDelta = new Vector2(width, resolvedHeight);

        LayoutElement layout = null;
        if (width > 0f)
        {
            layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = resolvedHeight;
            layout.minHeight = resolvedHeight;
        }

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(2f, 4f, 2f, 4f);
        tmp.extraPadding = true;
        ApplyChineseFont(tmp);
        return tmp;
    }

    private TextMeshProUGUI CreateBlockLabel(Transform parent, float height)
    {
        TextMeshProUGUI label = CreateLabel(parent, string.Empty, 13f, TextWhite, height);
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        return go;
    }

    private void ApplyChineseFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
