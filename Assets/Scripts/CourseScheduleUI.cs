using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 课程表界面：每回合开始时强制弹出，安排当月课程与空余时间。
/// </summary>
public class CourseScheduleUI : MonoBehaviour
{
    public static CourseScheduleUI Instance { get; private set; }
    private const float PanelVisualHeight = 940f;
    private const float ScheduleHeaderHeight = 72f;
    private const float ScheduleGridWidth = 574f;
    private const float ScheduleSlotHeight = 60f;

    private enum CourseDecision
    {
        None,
        Attend,
        Skip
    }

    private enum FreeTimeDecision
    {
        None,
        Study,
        Memorize,
        Leisure
    }

    private struct LeisureEvent
    {
        public string title;
        public string description;
        public int moodDelta;
        public int stressDelta;
        public int studyDelta;
        public int moneyDelta;

        public LeisureEvent(string title, string description, int moodDelta, int stressDelta, int studyDelta, int moneyDelta)
        {
            this.title = title;
            this.description = description;
            this.moodDelta = moodDelta;
            this.stressDelta = stressDelta;
            this.studyDelta = studyDelta;
            this.moneyDelta = moneyDelta;
        }
    }

    private struct ScheduleBlockData
    {
        public CourseDefinition course;
        public int dayIndex;
        public int startSlot;
        public int duration;
        public string accentLabel;
        public Color color;
        public bool isSelected;
    }

    private readonly LeisureEvent[] leisureEvents = new LeisureEvent[]
    {
        new LeisureEvent("窗边慢读", "你带着一杯热饮坐到窗边，读了点闲书，脑子松了下来。", 6, -4, 1, -12),
        new LeisureEvent("校园散步", "午后的风把人吹清醒了，绕着校园慢慢走了一圈。", 5, -5, 0, 0),
        new LeisureEvent("宿舍补眠", "你把手机一扣，老老实实眯了一会儿，状态回暖不少。", 4, -6, 0, 0),
        new LeisureEvent("轻松观影", "摸了一部短片当奖励，心情好了，学习欲也回来了。", 7, -3, 1, -8)
    };

    private static readonly string[] DayLabels = { "一", "二", "三", "四", "五", "六", "日" };
    private static readonly string[] TimeRanges =
    {
        "08:00\n08:45",
        "08:50\n09:35",
        "09:50\n10:35",
        "10:40\n11:25",
        "11:30\n12:15",
        "14:00\n14:45",
        "14:50\n15:35",
        "15:50\n16:35",
        "16:40\n17:25",
        "19:00\n19:45"
    };

    private readonly Color shellColor = new Color(0.90f, 0.91f, 0.98f, 1f);
    private readonly Color shellPanelColor = new Color(0.96f, 0.97f, 1f, 0.72f);
    private readonly Color textPrimary = new Color(0.08f, 0.09f, 0.15f, 1f);
    private readonly Color textSecondary = new Color(0.38f, 0.40f, 0.51f, 1f);
    private readonly Color textMuted = new Color(0.60f, 0.62f, 0.72f, 1f);
    private readonly Color controlPanelColor = new Color(0.13f, 0.15f, 0.23f, 0.97f);
    private readonly Color controlCardColor = new Color(0.19f, 0.22f, 0.31f, 1f);
    private readonly Color controlCardAltColor = new Color(0.16f, 0.18f, 0.27f, 1f);
    private readonly Color controlTextColor = new Color(0.95f, 0.96f, 0.99f, 1f);
    private readonly Color controlSubTextColor = new Color(0.71f, 0.75f, 0.85f, 1f);
    private readonly Color attendColor = new Color(0.34f, 0.58f, 0.93f, 1f);
    private readonly Color skipColor = new Color(0.90f, 0.47f, 0.58f, 1f);
    private readonly Color studyColor = new Color(0.29f, 0.68f, 0.58f, 1f);
    private readonly Color memorizeColor = new Color(0.32f, 0.55f, 0.84f, 1f);
    private readonly Color leisureColor = new Color(0.71f, 0.56f, 0.88f, 1f);
    private readonly Color selectedColor = new Color(0.95f, 0.97f, 1f, 1f);
    private readonly Color confirmColor = new Color(0.31f, 0.79f, 0.59f, 1f);
    private readonly Color disabledColor = new Color(0.28f, 0.31f, 0.39f, 1f);

    private Canvas canvas;
    private GameObject canvasRoot;
    private GameObject rootPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI roundDateText;
    private RectTransform dayHeaderContainer;
    private RectTransform timeRailContainer;
    private RectTransform scheduleGridContainer;
    private TextMeshProUGUI requiredCourseText;
    private TextMeshProUGUI courseDecisionHintText;
    private TextMeshProUGUI freeTimeHintText;
    private TextMeshProUGUI detailSectionTitleText;
    private TextMeshProUGUI semesterCourseListText;
    private TextMeshProUGUI detailHintText;
    private RectTransform detailScrollContent;
    private TextMeshProUGUI previewText;
    private Button attendButton;
    private Button skipButton;
    private Button studyButton;
    private Button memorizeButton;
    private Button leisureButton;
    private Button confirmButton;
    private TextMeshProUGUI confirmButtonText;

    private Action onCompleted;
    private CourseDefinition currentCourse;
    private CourseDefinition[] semesterCourses = Array.Empty<CourseDefinition>();
    private readonly List<ScheduleBlockData> scheduleBlocks = new List<ScheduleBlockData>();
    private readonly Dictionary<string, CourseDecision> courseDecisions = new Dictionary<string, CourseDecision>();
    private FreeTimeDecision freeTimeDecision = FreeTimeDecision.None;
    private bool studyCourseSelectionActive;
    private string inspectedCourseId = string.Empty;
    private string selectedCourseId = string.Empty;
    private string selectedStudyCourseId = string.Empty;
    private string selectedStudyCourseName = string.Empty;
    private int selectedEmptyDayIndex = -1;
    private int selectedEmptySlotIndex = -1;
    private int currentYear;
    private int currentSemester;
    private int currentRound;

    public bool IsOpen => canvasRoot != null && canvasRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            CourseScheduleUI replacement = null;
            CourseScheduleUI[] all = FindObjectsByType<CourseScheduleUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != this)
                {
                    replacement = all[i];
                    break;
                }
            }

            Instance = replacement;
        }
    }

    public void ShowSchedule(int year, int semester, int round, Action completedCallback)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CourseScheduleUI] ShowSchedule 请求 —— {year}-{semester}-R{round}\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
        #endif

        if (IsOpen &&
            currentYear == year &&
            currentSemester == semester &&
            currentRound == round)
        {
            Debug.Log($"[CourseScheduleUI] 忽略同回合重复刷新 —— {year}-{semester}-R{round}");
            onCompleted = completedCallback;
            return;
        }

        if (!UIFlowGuard.PrepareForExclusiveWindow(UIFlowGuard.WindowCourseSchedule)) return;

        currentYear = year;
        currentSemester = semester;
        currentRound = round;
        onCompleted = completedCallback;
        semesterCourses = ExamSystem.Instance != null
            ? ExamSystem.Instance.GetCoursesForSemester(year, semester)
            : Array.Empty<CourseDefinition>();
        currentCourse = ResolveRequiredCourse(semesterCourses, round);

        courseDecisions.Clear();
        CourseDefinition[] monthlyCourses = semesterCourses != null && semesterCourses.Length > 0
            ? semesterCourses
            : new[] { currentCourse };
        for (int i = 0; i < monthlyCourses.Length; i++)
        {
            if (!string.IsNullOrEmpty(monthlyCourses[i].id))
            {
                courseDecisions[monthlyCourses[i].id] = CourseDecision.Attend;
            }
        }

        freeTimeDecision = FreeTimeDecision.None;
        studyCourseSelectionActive = false;
        inspectedCourseId = currentCourse.id;
        selectedCourseId = currentCourse.id;
        selectedStudyCourseId = string.Empty;
        selectedStudyCourseName = string.Empty;
        selectedEmptyDayIndex = -1;
        selectedEmptySlotIndex = -1;

        RefreshContent();
        canvasRoot.SetActive(true);
    }

    public void HideForSceneTransition()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CourseScheduleUI] HideForSceneTransition\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
        #endif
        onCompleted = null;
        HideImmediate();
    }

    private CourseDefinition ResolveRequiredCourse(CourseDefinition[] courses, int round)
    {
        if (courses == null || courses.Length == 0)
        {
            return new CourseDefinition
            {
                id = $"TEMP_{round}",
                courseName = "通识课程",
                credits = 2,
                subjectTag = "general",
                year = currentYear,
                semester = currentSemester
            };
        }

        return courses[0];
    }

    private void BuildUI()
    {
        canvasRoot = new GameObject("CourseScheduleCanvas");
        canvasRoot.transform.SetParent(transform, false);

        canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 520;

        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot.AddComponent<GraphicRaycaster>();

        GameObject overlay = CreateUIObject("Overlay", canvasRoot.transform);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0.05f, 0.06f, 0.10f, 0.78f);
        StretchFullScreen(overlay.GetComponent<RectTransform>());

        rootPanel = CreateUIObject("RootPanel", canvasRoot.transform);
        RectTransform rootRect = rootPanel.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(1620f, PanelVisualHeight);

        HorizontalLayoutGroup rootLayout = rootPanel.AddComponent<HorizontalLayoutGroup>();
        rootLayout.spacing = 30f;
        rootLayout.childAlignment = TextAnchor.MiddleCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;

        BuildPhoneShell(rootPanel.transform);
        BuildControlPanel(rootPanel.transform);
    }

    private void BuildPhoneShell(Transform parent)
    {
        GameObject phoneShell = CreateCard("PhoneShell", parent, shellColor);
        LayoutElement shellLayout = phoneShell.AddComponent<LayoutElement>();
        shellLayout.preferredWidth = 700f;
        shellLayout.minHeight = PanelVisualHeight;
        shellLayout.preferredHeight = PanelVisualHeight;

        Outline shellOutline = phoneShell.AddComponent<Outline>();
        shellOutline.effectColor = new Color(1f, 1f, 1f, 0.16f);
        shellOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup shellGroup = phoneShell.AddComponent<VerticalLayoutGroup>();
        shellGroup.padding = new RectOffset(34, 34, 26, 22);
        shellGroup.spacing = 18f;
        shellGroup.childControlWidth = true;
        shellGroup.childControlHeight = false;
        shellGroup.childForceExpandWidth = true;
        shellGroup.childForceExpandHeight = false;

        GameObject header = CreateUIObject("Header", phoneShell.transform);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.minHeight = 122f;
        titleText = CreateText("Title", header.transform, "第1回合", 54, textPrimary, TextAlignmentOptions.TopLeft);
        titleText.fontStyle = FontStyles.Bold;
        titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = new Vector2(-228f, 0f);

        subtitleText = CreateText("Subtitle", header.transform, "大一上学期", 28, textSecondary, TextAlignmentOptions.BottomLeft);
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
        subtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitleText.rectTransform.offsetMin = new Vector2(0f, 0f);
        subtitleText.rectTransform.offsetMax = new Vector2(-228f, -4f);

        roundDateText = CreateText("RoundDate", header.transform, "", 24, textSecondary, TextAlignmentOptions.TopRight);
        roundDateText.fontStyle = FontStyles.Bold;
        roundDateText.rectTransform.anchorMin = new Vector2(1f, 0f);
        roundDateText.rectTransform.anchorMax = new Vector2(1f, 1f);
        roundDateText.rectTransform.pivot = new Vector2(1f, 1f);
        roundDateText.rectTransform.sizeDelta = new Vector2(220f, 0f);
        roundDateText.rectTransform.anchoredPosition = new Vector2(0f, -6f);

        GameObject scheduleStage = CreateCard("ScheduleStage", phoneShell.transform, new Color(1f, 1f, 1f, 0.14f));
        Button scheduleStageButton = scheduleStage.AddComponent<Button>();
        scheduleStageButton.transition = Selectable.Transition.None;
        scheduleStageButton.onClick.AddListener(HandleScheduleStageClicked);
        LayoutElement stageLayout = scheduleStage.AddComponent<LayoutElement>();
        stageLayout.minHeight = 742f;

        RectTransform stageRect = scheduleStage.GetComponent<RectTransform>();
        stageRect.sizeDelta = new Vector2(0f, 742f);

        GameObject gridContainer = CreateUIObject("GridContainer", scheduleStage.transform);
        RectTransform gridRect = gridContainer.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 0f);
        gridRect.anchorMax = new Vector2(1f, 1f);
        gridRect.offsetMin = new Vector2(94f, 18f);
        gridRect.offsetMax = new Vector2(-16f, -16f);

        GameObject gridHeader = CreateUIObject("GridHeader", gridContainer.transform);
        RectTransform headerRect = gridHeader.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, ScheduleHeaderHeight);
        headerRect.anchoredPosition = Vector2.zero;
        dayHeaderContainer = gridHeader.GetComponent<RectTransform>();
        dayHeaderContainer.gameObject.SetActive(false);

        GameObject scrollRoot = CreateUIObject("ScheduleScrollRoot", gridContainer.transform);
        RectTransform scrollRootRect = scrollRoot.GetComponent<RectTransform>();
        scrollRootRect.anchorMin = new Vector2(0f, 0f);
        scrollRootRect.anchorMax = new Vector2(1f, 1f);
        scrollRootRect.offsetMin = Vector2.zero;
        scrollRootRect.offsetMax = new Vector2(0f, -(ScheduleHeaderHeight + 6f));

        ScrollRect scheduleScroll = scrollRoot.AddComponent<ScrollRect>();
        scheduleScroll.horizontal = false;
        scheduleScroll.movementType = ScrollRect.MovementType.Clamped;
        scheduleScroll.scrollSensitivity = 30f;

        GameObject viewport = CreateUIObject("Viewport", scrollRoot.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        StretchToFill(viewportRect, Vector2.zero, Vector2.zero);

        GameObject scrollContent = CreateUIObject("ScrollContent", viewport.transform);
        RectTransform scrollContentRect = scrollContent.GetComponent<RectTransform>();
        scrollContentRect.anchorMin = new Vector2(0f, 1f);
        scrollContentRect.anchorMax = new Vector2(1f, 1f);
        scrollContentRect.pivot = new Vector2(0.5f, 1f);
        scrollContentRect.sizeDelta = new Vector2(0f, TimeRanges.Length * ScheduleSlotHeight + 12f);
        scrollContentRect.anchoredPosition = Vector2.zero;

        scheduleScroll.viewport = viewportRect;
        scheduleScroll.content = scrollContentRect;

        GameObject timeRail = CreateUIObject("TimeRail", scrollContent.transform);
        timeRailContainer = timeRail.GetComponent<RectTransform>();
        timeRailContainer.anchorMin = new Vector2(0f, 1f);
        timeRailContainer.anchorMax = new Vector2(0f, 1f);
        timeRailContainer.pivot = new Vector2(0f, 1f);
        timeRailContainer.sizeDelta = new Vector2(74f, TimeRanges.Length * ScheduleSlotHeight);
        timeRailContainer.anchoredPosition = new Vector2(12f, -8f);

        GameObject gridBody = CreateUIObject("GridBody", scrollContent.transform);
        RectTransform gridBodyRect = gridBody.GetComponent<RectTransform>();
        gridBodyRect.anchorMin = new Vector2(0f, 1f);
        gridBodyRect.anchorMax = new Vector2(0f, 1f);
        gridBodyRect.pivot = new Vector2(0f, 1f);
        gridBodyRect.sizeDelta = new Vector2(ScheduleGridWidth, TimeRanges.Length * ScheduleSlotHeight);
        gridBodyRect.anchoredPosition = new Vector2(94f, -8f);

        Image gridBackground = gridBody.AddComponent<Image>();
        gridBackground.color = new Color(1f, 1f, 1f, 0.14f);

        scheduleGridContainer = gridBody.GetComponent<RectTransform>();

    }

    private void BuildControlPanel(Transform parent)
    {
        GameObject controlPanel = CreateCard("ControlPanel", parent, controlPanelColor);
        Button controlPanelButton = controlPanel.AddComponent<Button>();
        controlPanelButton.transition = Selectable.Transition.None;
        controlPanelButton.onClick.AddListener(HandleControlPanelClicked);
        LayoutElement panelLayout = controlPanel.AddComponent<LayoutElement>();
        panelLayout.preferredWidth = 810f;
        panelLayout.minHeight = PanelVisualHeight;
        panelLayout.preferredHeight = PanelVisualHeight;

        VerticalLayoutGroup controlGroup = controlPanel.AddComponent<VerticalLayoutGroup>();
        controlGroup.padding = new RectOffset(28, 28, 28, 28);
        controlGroup.spacing = 14f;
        controlGroup.childControlWidth = true;
        controlGroup.childControlHeight = true;
        controlGroup.childForceExpandWidth = true;
        controlGroup.childForceExpandHeight = false;

        GameObject introCard = CreateControlCard(controlPanel.transform, 156f);
        CreateSectionTitle(introCard.transform, "本月安排");
        requiredCourseText = CreateText("RequiredCourse", introCard.transform, "", 22, controlTextColor, TextAlignmentOptions.TopLeft);
        requiredCourseText.fontStyle = FontStyles.Bold;
        courseDecisionHintText = CreateText("CourseDecisionHint", introCard.transform, "", 16, controlSubTextColor, TextAlignmentOptions.TopLeft);

        GameObject attendCard = CreateControlCard(controlPanel.transform, 112f);
        CreateSectionTitle(attendCard.transform, "课程处理");
        GameObject attendRow = CreateButtonRow(attendCard.transform, 2, 68f);
        attendButton = CreateActionButton(attendRow.transform, "BtnAttend", "正常上课", attendColor, () =>
        {
            SetDecisionForSelectedCourse(CourseDecision.Attend);
            RefreshSelectionState();
            RebuildScheduleGrid();
        });
        skipButton = CreateActionButton(attendRow.transform, "BtnSkip", "直接逃课", skipColor, () =>
        {
            SetDecisionForSelectedCourse(CourseDecision.Skip);
            RefreshSelectionState();
            RebuildScheduleGrid();
        });

        GameObject freeCard = CreateControlCard(controlPanel.transform, 138f);
        CreateSectionTitle(freeCard.transform, "空余时间");
        GameObject freeRow = CreateButtonRow(freeCard.transform, 3, 68f);
        studyButton = CreateActionButton(freeRow.transform, "BtnStudy", "自习", studyColor, () =>
        {
            BeginStudySelection();
            RefreshSelectionState();
            RebuildScheduleGrid();
        });
        memorizeButton = CreateActionButton(freeRow.transform, "BtnMemorize", "背单词", memorizeColor, () =>
        {
            freeTimeDecision = FreeTimeDecision.Memorize;
            studyCourseSelectionActive = false;
            selectedStudyCourseId = string.Empty;
            selectedStudyCourseName = string.Empty;
            selectedEmptyDayIndex = -1;
            selectedEmptySlotIndex = -1;
            RefreshSelectionState();
            RebuildScheduleGrid();
        });
        leisureButton = CreateActionButton(freeRow.transform, "BtnLeisure", "轻松安排", leisureColor, () =>
        {
            freeTimeDecision = FreeTimeDecision.Leisure;
            studyCourseSelectionActive = false;
            selectedStudyCourseId = string.Empty;
            selectedStudyCourseName = string.Empty;
            selectedEmptyDayIndex = -1;
            selectedEmptySlotIndex = -1;
            RefreshSelectionState();
            RebuildScheduleGrid();
        });

        freeTimeHintText = CreateText("FreeTimeHint", freeCard.transform, "", 16, controlSubTextColor, TextAlignmentOptions.TopLeft);

        GameObject previewCard = CreateControlCard(controlPanel.transform, 132f);
        CreateSectionTitle(previewCard.transform, "本月预览");
        previewText = CreateText("PreviewText", previewCard.transform, "", 17, controlSubTextColor, TextAlignmentOptions.TopLeft);

        GameObject detailCard = CreateControlCard(controlPanel.transform, 230f);
        LayoutElement detailLayout = detailCard.GetComponent<LayoutElement>();
        detailLayout.flexibleHeight = 1f;
        detailSectionTitleText = CreateText("DetailTitle", detailCard.transform, "本学期排课", 22, controlTextColor, TextAlignmentOptions.TopLeft);
        detailSectionTitleText.fontStyle = FontStyles.Bold;
        detailHintText = CreateText("DetailHint", detailCard.transform, "", 15, controlSubTextColor, TextAlignmentOptions.TopLeft);
        CreateDetailScrollArea(detailCard.transform);

        GameObject footer = CreateUIObject("Footer", controlPanel.transform);
        LayoutElement footerLayout = footer.AddComponent<LayoutElement>();
        footerLayout.minHeight = 76f;
        HorizontalLayoutGroup footerGroup = footer.AddComponent<HorizontalLayoutGroup>();
        footerGroup.childAlignment = TextAnchor.MiddleRight;
        footerGroup.childControlWidth = false;
        footerGroup.childControlHeight = true;
        footerGroup.childForceExpandWidth = false;
        footerGroup.childForceExpandHeight = false;

        confirmButton = CreateActionButton(footer.transform, "BtnConfirm", "确认本月安排", confirmColor, CommitSelection);
        LayoutElement confirmLayout = confirmButton.gameObject.GetComponent<LayoutElement>();
        confirmLayout.preferredWidth = 360f;
        confirmLayout.preferredHeight = 74f;
        confirmButtonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();

        ConfigureFlowText(requiredCourseText, 56f);
        ConfigureFlowText(courseDecisionHintText, 32f);
        ConfigureFlowText(freeTimeHintText, 32f);
        ConfigureFlowText(previewText, 56f);
        ConfigureFlowText(detailSectionTitleText, 30f);
        ConfigureFlowText(detailHintText, 28f);
    }

    private void RefreshContent()
    {
        titleText.text = BuildRoundTitle();
        subtitleText.text = $"{GetYearDisplayName(currentYear)}{(currentSemester == 1 ? "上学期" : "下学期")}  ·  第{currentRound}回合";
        if (roundDateText != null)
        {
            roundDateText.text = BuildCurrentDateLabel();
        }

        BuildScheduleBlocks();
        RebuildWeekHeader();
        RebuildTimeRail();
        RebuildScheduleGrid();

        requiredCourseText.text =
            $"{BuildCurrentDateLabel()}课程安排\n" +
            $"本月共有 {scheduleBlocks.Count} 门课，默认全部正常上课；如要逃课，再单独点课程修改。";
        courseDecisionHintText.text = $"先点一门课程，可将它改为逃课。当前选中：《{GetSelectedCourseDisplayName()}》。";

        RefreshDetailPanel();
        RefreshSelectionState();
    }

    private void BuildScheduleBlocks()
    {
        scheduleBlocks.Clear();

        if (semesterCourses == null || semesterCourses.Length == 0)
        {
            scheduleBlocks.Add(new ScheduleBlockData
            {
                course = currentCourse,
                dayIndex = 0,
                startSlot = 2,
                duration = 2,
                accentLabel = BuildCourseAccentLabel(currentCourse.id),
                color = GetCourseColor(currentCourse.subjectTag),
                isSelected = currentCourse.id == selectedCourseId
            });
            return;
        }

        bool[,] occupied = new bool[7, TimeRanges.Length];
        for (int i = 0; i < semesterCourses.Length; i++)
        {
            CourseDefinition course = semesterCourses[i];
            int preferredDay = GetPreferredDayIndex(course, i);
            int preferredSlot = GetPreferredStartSlot(course, i);
            int duration = GetCourseDuration(course);

            ResolveSchedulePosition(occupied, preferredDay, preferredSlot, duration, out int finalDay, out int finalSlot);
            MarkOccupied(occupied, finalDay, finalSlot, duration);

            scheduleBlocks.Add(new ScheduleBlockData
            {
                course = course,
                dayIndex = finalDay,
                startSlot = finalSlot,
                duration = duration,
                accentLabel = BuildCourseAccentLabel(course.id),
                color = GetCourseColor(course.subjectTag),
                isSelected = course.id == selectedCourseId
            });
        }

        scheduleBlocks.Sort((a, b) =>
        {
            int dayCompare = a.dayIndex.CompareTo(b.dayIndex);
            return dayCompare != 0 ? dayCompare : a.startSlot.CompareTo(b.startSlot);
        });
    }

    private void ResolveSchedulePosition(bool[,] occupied, int preferredDay, int preferredSlot, int duration, out int finalDay, out int finalSlot)
    {
        for (int dayOffset = 0; dayOffset < DayLabels.Length; dayOffset++)
        {
            int day = (preferredDay + dayOffset) % DayLabels.Length;
            for (int slotOffset = 0; slotOffset < TimeRanges.Length; slotOffset++)
            {
                int slot = (preferredSlot + slotOffset) % TimeRanges.Length;
                if (CanPlace(occupied, day, slot, duration))
                {
                    finalDay = day;
                    finalSlot = slot;
                    return;
                }
            }
        }

        finalDay = Mathf.Clamp(preferredDay, 0, DayLabels.Length - 1);
        finalSlot = Mathf.Clamp(preferredSlot, 0, TimeRanges.Length - duration);
    }

    private bool CanPlace(bool[,] occupied, int day, int slot, int duration)
    {
        if (slot + duration > TimeRanges.Length)
        {
            return false;
        }

        for (int i = 0; i < duration; i++)
        {
            if (occupied[day, slot + i])
            {
                return false;
            }
        }

        return true;
    }

    private void MarkOccupied(bool[,] occupied, int day, int slot, int duration)
    {
        for (int i = 0; i < duration && slot + i < TimeRanges.Length; i++)
        {
            occupied[day, slot + i] = true;
        }
    }

    private int GetPreferredDayIndex(CourseDefinition course, int index)
    {
        int baseDay = course.subjectTag switch
        {
            "pe" => 0,
            "math" => 1,
            "english" => 2,
            "cs" => 3,
            "physics" => 4,
            "politics" => 4,
            "history" => 5,
            "economics" => 6,
            _ => index % DayLabels.Length
        };

        return (baseDay + Mathf.Max(0, currentSemester - 1)) % DayLabels.Length;
    }

    private int GetPreferredStartSlot(CourseDefinition course, int index)
    {
        int baseSlot = course.subjectTag switch
        {
            "pe" => 0,
            "math" => 2,
            "english" => 2,
            "cs" => 5,
            "physics" => 6,
            "politics" => 5,
            "history" => 7,
            "economics" => 8,
            _ => (index * 2) % 8
        };

        return Mathf.Clamp(baseSlot + (index % 2), 0, TimeRanges.Length - 2);
    }

    private int GetCourseDuration(CourseDefinition course)
    {
        if (course == null)
        {
            return 2;
        }

        if (course.subjectTag == "pe")
        {
            return 2;
        }

        if (course.credits >= 4)
        {
            return 3;
        }

        return 2;
    }

    private void RebuildWeekHeader()
    {
        if (dayHeaderContainer == null || !dayHeaderContainer.gameObject.activeSelf)
        {
            return;
        }

        ClearChildren(dayHeaderContainer);

        for (int i = 0; i < DayLabels.Length; i++)
        {
            GameObject item = CreateUIObject($"Day_{i}", dayHeaderContainer);
            LayoutElement layout = item.AddComponent<LayoutElement>();
            layout.preferredWidth = 82f;

            VerticalLayoutGroup group = item.AddComponent<VerticalLayoutGroup>();
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = false;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.spacing = 6f;

            TextMeshProUGUI label = CreateText("DayLabel", item.transform, DayLabels[i], 26, textMuted, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;

            TextMeshProUGUI date = CreateText("DateLabel", item.transform, GetPseudoDateLabel(i), 18, textMuted, TextAlignmentOptions.Center);
        }
    }

    private void RebuildTimeRail()
    {
        ClearChildren(timeRailContainer);

        for (int i = 0; i < TimeRanges.Length; i++)
        {
            GameObject slot = CreateUIObject($"Time_{i}", timeRailContainer);
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, ScheduleSlotHeight - 4f);
            rect.anchoredPosition = new Vector2(0f, -(i * ScheduleSlotHeight));

            TextMeshProUGUI indexText = CreateText("Index", slot.transform, (i + 1).ToString(), 20, textPrimary, TextAlignmentOptions.TopLeft);
            indexText.fontStyle = FontStyles.Bold;
            indexText.enableWordWrapping = false;
            indexText.overflowMode = TextOverflowModes.Overflow;
            indexText.rectTransform.anchorMin = new Vector2(0f, 0f);
            indexText.rectTransform.anchorMax = new Vector2(0f, 1f);
            indexText.rectTransform.sizeDelta = new Vector2(30f, 0f);
            indexText.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            TextMeshProUGUI timeText = CreateText("Time", slot.transform, TimeRanges[i], 15, textSecondary, TextAlignmentOptions.TopRight);
            timeText.rectTransform.anchorMin = new Vector2(0f, 0f);
            timeText.rectTransform.anchorMax = new Vector2(1f, 1f);
            timeText.rectTransform.offsetMin = new Vector2(18f, 0f);
            timeText.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private void RebuildScheduleGrid()
    {
        ClearChildren(scheduleGridContainer);

        RectTransform gridRect = scheduleGridContainer;
        float width = ScheduleGridWidth;
        float height = TimeRanges.Length * ScheduleSlotHeight;
        float dayWidth = width / DayLabels.Length;
        float slotHeight = height / TimeRanges.Length;

        for (int row = 0; row <= TimeRanges.Length; row++)
        {
            GameObject line = CreateUIObject($"HLine_{row}", gridRect);
            Image image = line.AddComponent<Image>();
            image.color = new Color(0.58f, 0.61f, 0.72f, row == 0 ? 0.28f : 0.18f);
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(0f, 1f);
            lineRect.pivot = new Vector2(0f, 1f);
            lineRect.sizeDelta = new Vector2(width, 1f);
            lineRect.anchoredPosition = new Vector2(0f, -(row * slotHeight));
        }

        for (int col = 0; col <= DayLabels.Length; col++)
        {
            GameObject line = CreateUIObject($"VLine_{col}", gridRect);
            Image image = line.AddComponent<Image>();
            image.color = new Color(0.58f, 0.61f, 0.72f, col == 0 ? 0.28f : 0.18f);
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(0f, 1f);
            lineRect.pivot = new Vector2(0f, 1f);
            lineRect.sizeDelta = new Vector2(1f, height);
            lineRect.anchoredPosition = new Vector2(col * dayWidth, 0f);
        }

        for (int day = 0; day < DayLabels.Length; day++)
        {
            for (int slot = 0; slot < TimeRanges.Length; slot++)
            {
                if (IsSlotOccupied(day, slot))
                {
                    continue;
                }

                GameObject emptyCell = CreateUIObject($"Empty_{day}_{slot}", gridRect);
                Image emptyImage = emptyCell.AddComponent<Image>();
                bool isSelectedEmpty = day == selectedEmptyDayIndex && slot == selectedEmptySlotIndex;
                emptyImage.color = isSelectedEmpty
                    ? new Color(0.31f, 0.62f, 0.97f, 0.24f)
                    : new Color(1f, 1f, 1f, 0.001f);
                Button emptyButton = emptyCell.AddComponent<Button>();
                emptyButton.targetGraphic = emptyImage;
                int capturedDay = day;
                int capturedSlot = slot;
                emptyButton.onClick.AddListener(() => OnEmptySlotClicked(capturedDay, capturedSlot));

                if (isSelectedEmpty)
                {
                    Outline outline = emptyCell.AddComponent<Outline>();
                    outline.effectColor = new Color(0.22f, 0.48f, 0.95f, 0.95f);
                    outline.effectDistance = new Vector2(2f, -2f);
                }

                RectTransform cellRect = emptyCell.GetComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(0f, 1f);
                cellRect.pivot = new Vector2(0f, 1f);
                cellRect.sizeDelta = new Vector2(dayWidth - 6f, slotHeight - 6f);
                cellRect.anchoredPosition = new Vector2(day * dayWidth + 3f, -(slot * slotHeight + 3f));
            }
        }

        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            ScheduleBlockData block = scheduleBlocks[i];
            if (ShouldHideScheduleBlock(block))
            {
                continue;
            }

            bool isInspected = block.course.id == inspectedCourseId;

            GameObject card = CreateUIObject($"CourseBlock_{i}", gridRect);
            Image image = card.AddComponent<Image>();
            CourseDecision blockDecision = GetDecisionForCourse(block.course.id);
            Color blockColor = block.color;
            if (blockDecision == CourseDecision.Skip)
            {
                blockColor.a = 0.42f;
            }
            image.color = blockColor;
            card.AddComponent<RectMask2D>();

            Button button = card.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnCourseBlockClicked(block));

            if (block.isSelected || isInspected)
            {
                Outline outline = card.AddComponent<Outline>();
                outline.effectColor = block.isSelected
                    ? new Color(1f, 0.86f, 0.34f, 0.98f)
                    : new Color(1f, 1f, 1f, 0.92f);
                outline.effectDistance = block.isSelected
                    ? new Vector2(4f, -4f)
                    : new Vector2(2f, -2f);
            }

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(dayWidth - 10f, slotHeight * block.duration - 8f);
            rect.anchoredPosition = new Vector2(block.dayIndex * dayWidth + 5f, -(block.startSlot * slotHeight + 4f));

            VerticalLayoutGroup contentGroup = card.AddComponent<VerticalLayoutGroup>();
            contentGroup.padding = new RectOffset(12, 10, 10, 10);
            contentGroup.spacing = 6f;
            contentGroup.childControlWidth = true;
            contentGroup.childControlHeight = false;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childForceExpandHeight = false;

            TextMeshProUGUI name = CreateText("CourseName", card.transform, BuildCourseBlockTitle(block.course.courseName), 18, Color.white, TextAlignmentOptions.TopLeft);
            name.fontStyle = FontStyles.Bold;
            name.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI accent = CreateText("Accent", card.transform, block.accentLabel, 13, new Color(1f, 1f, 1f, 0.86f), TextAlignmentOptions.BottomLeft);
            accent.enableWordWrapping = false;
            accent.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (selectedEmptyDayIndex >= 0 && selectedEmptySlotIndex >= 0 && freeTimeDecision != FreeTimeDecision.None)
        {
            CreatePlannedFreeTimeCard(gridRect, dayWidth, slotHeight);
        }
    }

    private void CreatePlannedFreeTimeCard(RectTransform gridRect, float dayWidth, float slotHeight)
    {
        string title;
        string subtitle;
        Color color;

        switch (freeTimeDecision)
        {
            case FreeTimeDecision.Study:
                title = string.IsNullOrEmpty(selectedStudyCourseName) ? "待选自习" : selectedStudyCourseName;
                subtitle = string.IsNullOrEmpty(selectedStudyCourseName) ? "自习安排" : "自习";
                color = studyColor;
                break;

            case FreeTimeDecision.Memorize:
                title = "背单词";
                subtitle = "空余安排";
                color = memorizeColor;
                break;

            case FreeTimeDecision.Leisure:
                title = "轻松安排";
                subtitle = "空余安排";
                color = leisureColor;
                break;

            default:
                return;
        }

        GameObject card = CreateUIObject("PlannedFreeTimeCard", gridRect);
        Image image = card.AddComponent<Image>();
        image.color = color;
        card.AddComponent<RectMask2D>();

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(dayWidth - 10f, slotHeight - 8f);
        rect.anchoredPosition = new Vector2(selectedEmptyDayIndex * dayWidth + 5f, -(selectedEmptySlotIndex * slotHeight + 4f));

        VerticalLayoutGroup contentGroup = card.AddComponent<VerticalLayoutGroup>();
        contentGroup.padding = new RectOffset(10, 10, 8, 8);
        contentGroup.spacing = 4f;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = false;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;

        TextMeshProUGUI name = CreateText("PlanTitle", card.transform, BuildCourseBlockTitle(title), 17, Color.white, TextAlignmentOptions.TopLeft);
        name.fontStyle = FontStyles.Bold;
        name.enableAutoSizing = true;
        name.fontSizeMin = 12f;
        name.fontSizeMax = 17f;
        name.overflowMode = TextOverflowModes.Ellipsis;

        TextMeshProUGUI meta = CreateText("PlanMeta", card.transform, subtitle, 13, new Color(1f, 1f, 1f, 0.92f), TextAlignmentOptions.BottomLeft);
        meta.enableWordWrapping = false;
        meta.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void RefreshSelectionState()
    {
        bool hasSelectedCourse = !string.IsNullOrEmpty(selectedCourseId);
        CourseDecision selectedDecision = GetDecisionForCourse(selectedCourseId);
        attendButton.interactable = hasSelectedCourse;
        skipButton.interactable = hasSelectedCourse;

        SetButtonSelected(attendButton, hasSelectedCourse && selectedDecision == CourseDecision.Attend, attendColor);
        SetButtonSelected(skipButton, hasSelectedCourse && selectedDecision == CourseDecision.Skip, skipColor);
        SetButtonSelected(studyButton, freeTimeDecision == FreeTimeDecision.Study, studyColor);
        SetButtonSelected(memorizeButton, freeTimeDecision == FreeTimeDecision.Memorize, memorizeColor);
        SetButtonSelected(leisureButton, freeTimeDecision == FreeTimeDecision.Leisure, leisureColor);
        RefreshDetailPanel();

        if (freeTimeHintText != null)
        {
            if (freeTimeDecision == FreeTimeDecision.Study)
            {
                freeTimeHintText.text = string.IsNullOrEmpty(selectedStudyCourseName)
                    ? "已切到自习，请在下方选目标课程。"
                    : $"自习目标：{selectedStudyCourseName}";
            }
            else
            {
                freeTimeHintText.text = "空余时间可不安排；想安排的话，可点空白课时进入自习选课，或直接选背单词/轻松安排。";
            }
        }

        previewText.text = BuildPreviewText();

        bool studyReady = freeTimeDecision != FreeTimeDecision.Study ||
                          ((selectedEmptyDayIndex < 0 && selectedEmptySlotIndex < 0) || !string.IsNullOrEmpty(selectedStudyCourseId));
        int finalActionPointCost = GetFinalScheduleActionPointCost();
        bool affordable = CanAffordFinalSchedule(finalActionPointCost);
        bool ready = AllCoursesDecided() &&
                     studyReady &&
                     affordable;
        confirmButton.interactable = ready;
        Image confirmImage = confirmButton.GetComponent<Image>();
        if (confirmImage != null)
        {
            confirmImage.color = ready ? confirmColor : disabledColor;
        }

        if (confirmButtonText != null)
        {
            confirmButtonText.text = ready
                ? $"确认安排 (-{finalActionPointCost}行动点)"
                : $"行动点不足 (-{finalActionPointCost}行动点)";
        }
    }

    private string BuildPreviewText()
    {
        List<string> lines = new List<string>();
        int attendCount = 0;
        int skipCount = 0;
        int undecidedCount = 0;
        List<string> pendingCourses = new List<string>();
        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            CourseDefinition course = scheduleBlocks[i].course;
            CourseDecision decision = GetDecisionForCourse(course.id);
            switch (decision)
            {
                case CourseDecision.Attend:
                    attendCount++;
                    break;
                case CourseDecision.Skip:
                    skipCount++;
                    break;
                default:
                    undecidedCount++;
                    pendingCourses.Add(course.courseName);
                    break;
            }
        }

        if (undecidedCount > 0)
        {
            string pendingSummary = string.Join("、", pendingCourses.Take(3));
            if (pendingCourses.Count > 3)
            {
                pendingSummary += $" 等{pendingCourses.Count}门";
            }

            lines.Add($"1. 还有 {undecidedCount} 门课未决定：{pendingSummary}。");
        }
        else
        {
            lines.Add($"1. 本月课程已排完：上课 {attendCount} 门，逃课 {skipCount} 门。");
        }

        if (freeTimeDecision == FreeTimeDecision.None)
        {
            lines.Add("2. 本月空余时间未额外安排，按默认流程处理：不上自习。");
        }
        else if (freeTimeDecision == FreeTimeDecision.Study)
        {
            if (string.IsNullOrEmpty(selectedStudyCourseName))
            {
                lines.Add("2. 已选自习，但还没定具体要补哪门课。");
            }
            else
            {
                lines.Add($"2. 自习《{selectedStudyCourseName}》：学力+7，压力+5，消耗2行动点，并计入考试自习次数。");
            }
        }
        else if (freeTimeDecision == FreeTimeDecision.Memorize)
        {
            lines.Add("2. 背单词：学力+1，压力+2，消耗1行动点。");
        }
        else
        {
            lines.Add("2. 轻松安排：随机休闲事件，消耗1行动点。");
        }

        return string.Join("\n", lines);
    }

    private void OnCourseBlockClicked(ScheduleBlockData block)
    {
        inspectedCourseId = block.course.id;
        selectedCourseId = block.course.id;
        CourseDecision currentDecision = GetDecisionForCourse(block.course.id);
        string decisionText = currentDecision switch
        {
            CourseDecision.Attend => "当前决定：正常上课。",
            CourseDecision.Skip => "当前决定：本月逃课。",
            _ => "当前未单独改动，按默认正常上课处理。"
        };
        courseDecisionHintText.text = $"已选中《{block.course.courseName}》。{decisionText}";
        RebuildScheduleGrid();
        RefreshSelectionState();
    }

    private void OnEmptySlotClicked(int dayIndex, int slotIndex)
    {
        BeginStudySelection();
        selectedEmptyDayIndex = dayIndex;
        selectedEmptySlotIndex = slotIndex;
        if (freeTimeHintText != null)
        {
            freeTimeHintText.text = $"已选中周{DayLabels[dayIndex]}第{slotIndex + 1}节空档。请在右侧选一门自习课程。";
        }
        RebuildScheduleGrid();
        RefreshSelectionState();
    }

    private void HandleScheduleStageClicked()
    {
        if (freeTimeDecision == FreeTimeDecision.None)
        {
            BeginStudySelection();
        }

        if (freeTimeHintText != null)
        {
            freeTimeHintText.text = selectedEmptyDayIndex >= 0 && selectedEmptySlotIndex >= 0
                ? $"当前已选周{DayLabels[selectedEmptyDayIndex]}第{selectedEmptySlotIndex + 1}节空档，可继续在右侧选择安排。"
                : "已进入空余时间安排。点课表空白节次，或直接在右侧选择自习/背单词/轻松安排。";
        }

        RefreshSelectionState();
    }

    private void HandleControlPanelClicked()
    {
        if (freeTimeHintText != null && selectedEmptyDayIndex < 0)
        {
            freeTimeHintText.text = "右侧已激活。空余时间可以不安排；若要安排自习，请先在左侧点一节空白课时。";
        }

        if (courseDecisionHintText != null && string.IsNullOrEmpty(selectedCourseId))
        {
            courseDecisionHintText.text = $"先点左侧任意课程；默认都是正常上课，只在需要逃课时修改。当前选中：《{GetSelectedCourseDisplayName()}》。";
        }
    }

    private void BeginStudySelection()
    {
        freeTimeDecision = FreeTimeDecision.Study;
        studyCourseSelectionActive = true;
        RebuildStudyCourseList();
    }

    private void RebuildStudyCourseList()
    {
        RefreshDetailPanel();
    }

    private void RefreshDetailPanel()
    {
        if (detailScrollContent == null)
        {
            return;
        }

        ClearChildren(detailScrollContent);

        bool showStudyList = studyCourseSelectionActive || freeTimeDecision == FreeTimeDecision.Study;
        if (detailSectionTitleText != null)
        {
            detailSectionTitleText.text = showStudyList ? "可自习课程" : "本学期排课";
        }
        if (detailHintText != null)
        {
            detailHintText.text = showStudyList
                ? "在这里选一门自习目标；已选内容会保留，直到你改成别的安排。"
                : BuildCourseBrowseHint();
        }

        if (!showStudyList)
        {
            semesterCourseListText = CreateText("SemesterCourseList", detailScrollContent, BuildSemesterCourseInspectionText(), 18, controlSubTextColor, TextAlignmentOptions.TopLeft);
            ConfigureFlowText(semesterCourseListText, 140f);
            return;
        }

        CourseDefinition[] source = semesterCourses != null && semesterCourses.Length > 0
            ? semesterCourses
            : new[] { currentCourse };

        for (int i = 0; i < source.Length; i++)
        {
            CourseDefinition course = source[i];
            Button button = CreateStudyCourseButton(detailScrollContent, course, course.id == selectedStudyCourseId);
            CourseDefinition captured = course;
            button.onClick.AddListener(() =>
            {
                freeTimeDecision = FreeTimeDecision.Study;
                studyCourseSelectionActive = true;
                selectedStudyCourseId = captured.id;
                selectedStudyCourseName = captured.courseName;
                RefreshDetailPanel();
                RefreshSelectionState();
                RebuildScheduleGrid();
            });
        }
    }

    private string BuildCourseBrowseHint()
    {
        ScheduleBlockData inspectedBlock = GetInspectedScheduleBlock();
        if (string.IsNullOrEmpty(inspectedBlock.course.courseName))
        {
            return "点左侧课程卡片可查看该课程信息。";
        }

        if (inspectedBlock.course.id == selectedCourseId)
        {
            return $"当前正在安排《{inspectedBlock.course.courseName}》。{BuildDecisionSummary(inspectedBlock.course.id)}";
        }

        return $"当前查看《{inspectedBlock.course.courseName}》。{BuildDecisionSummary(inspectedBlock.course.id)}";
    }

    private string BuildSemesterCourseInspectionText()
    {
        if (scheduleBlocks.Count == 0)
        {
            return "本学期课程数据暂未就绪，先按通识课程处理。";
        }

        List<string> lines = new List<string>();
        ScheduleBlockData inspectedBlock = GetInspectedScheduleBlock();
        if (!string.IsNullOrEmpty(inspectedBlock.course.courseName))
        {
            string inspectedDay = $"周{DayLabels[inspectedBlock.dayIndex]}";
            string inspectedSpan = $"{inspectedBlock.startSlot + 1}-{inspectedBlock.startSlot + inspectedBlock.duration}节";
            string inspectedRole = inspectedBlock.course.id == selectedCourseId ? "当前安排课程" : "课程浏览";
            lines.Add($"{inspectedRole}");
            lines.Add($"《{inspectedBlock.course.courseName}》");
            lines.Add($"{GetSubjectDisplayName(inspectedBlock.course.subjectTag)} · {inspectedBlock.course.credits}学分");
            lines.Add($"{inspectedDay} {inspectedSpan}");
            lines.Add(BuildDecisionSummary(inspectedBlock.course.id));
            lines.Add(string.Empty);
            lines.Add("本月课程总览");
        }

        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            ScheduleBlockData block = scheduleBlocks[i];
            string marker = block.course.id == inspectedCourseId ? "▌" : (block.course.id == selectedCourseId ? "●" : "○");
            string day = $"周{DayLabels[block.dayIndex]}";
            string span = $"{block.startSlot + 1}-{block.startSlot + block.duration}节";
            lines.Add($"{marker} {day} {span}  {block.course.courseName}  ·  {BuildDecisionLabel(GetDecisionForCourse(block.course.id))}");
        }

        return string.Join("\n", lines);
    }

    private ScheduleBlockData GetInspectedScheduleBlock()
    {
        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            if (scheduleBlocks[i].course.id == inspectedCourseId)
            {
                return scheduleBlocks[i];
            }
        }

        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            if (scheduleBlocks[i].course.id == selectedCourseId)
            {
                return scheduleBlocks[i];
            }
        }

        return default;
    }

    private void CommitSelection()
    {
        if (!AllCoursesDecided())
        {
            return;
        }

        int finalActionPointCost = GetFinalScheduleActionPointCost();
        if (!CanAffordFinalSchedule(finalActionPointCost))
        {
            if (freeTimeHintText != null)
            {
                freeTimeHintText.text = $"当前课表共 {finalActionPointCost} 张安排卡，行动点不足，无法确认。";
            }
            return;
        }

        ApplyCourseDecisions(false);
        ApplyFreeTimeDecision(false);
        ConsumeFinalScheduleActionPoints(finalActionPointCost);

        Debug.Log($"[CourseScheduleUI] 已完成课程安排 —— {currentYear}-{currentSemester}-R{currentRound}");

        HideImmediate();
        Action completed = onCompleted;
        onCompleted = null;
        completed?.Invoke();
    }

    private void ApplyCourseDecisions(bool consumeActionPoints)
    {
        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            CourseDefinition course = scheduleBlocks[i].course;
            ApplyCourseDecision(course, GetDecisionForCourse(course.id), consumeActionPoints);
        }
    }

    private void ApplyCourseDecision(CourseDefinition course, CourseDecision decision, bool consumeActionPoints)
    {
        if (decision == CourseDecision.Attend)
        {
            if (!TryExecuteAction("attend_class", consumeActionPoints))
            {
                ApplyFallbackAction(consumeActionPoints ? 1 : 0, 0, new AttributeEffect("学力", 5), new AttributeEffect("压力", 3));
            }
            return;
        }

        if (decision == CourseDecision.None)
        {
            return;
        }

        PlayerAttributes attrs = PlayerAttributes.Instance;
        if (attrs != null)
        {
            attrs.AddAttribute("心情", 4);
            attrs.AddAttribute("压力", -1);
            attrs.AddAttribute("负罪感", 3);
            attrs.AddAttribute("黑暗值", 1);
        }
    }

    private void ApplyFreeTimeDecision(bool consumeActionPoints)
    {
        switch (freeTimeDecision)
        {
            case FreeTimeDecision.Study:
                if (!string.IsNullOrEmpty(selectedStudyCourseName))
                {
                    Debug.Log($"[CourseScheduleUI] 自习课程：{selectedStudyCourseName}");
                }
                if (!TryExecuteAction("study", consumeActionPoints))
                {
                    ApplyFallbackAction(consumeActionPoints ? 2 : 0, 0, new AttributeEffect("学力", 7), new AttributeEffect("压力", 5));
                    if (ExamSystem.Instance != null)
                    {
                        ExamSystem.Instance.RegisterScheduleStudySession();
                    }
                }
                break;

            case FreeTimeDecision.Memorize:
                if (!TryExecuteAction("memorize_words", consumeActionPoints))
                {
                    ApplyFallbackAction(consumeActionPoints ? 1 : 0, 0, new AttributeEffect("学力", 1), new AttributeEffect("压力", 2));
                }
                break;

            case FreeTimeDecision.Leisure:
                ApplyLeisureEvent(consumeActionPoints);
                break;
        }
    }

    private void ApplyLeisureEvent(bool consumeActionPoints)
    {
        if (consumeActionPoints && GameState.Instance != null)
        {
            GameState.Instance.ConsumeActionPoint(1);
        }

        LeisureEvent evt = leisureEvents[UnityEngine.Random.Range(0, leisureEvents.Length)];
        PlayerAttributes attrs = PlayerAttributes.Instance;
        if (attrs != null)
        {
            attrs.AddAttribute("心情", evt.moodDelta);
            attrs.AddAttribute("压力", evt.stressDelta);
            if (evt.studyDelta != 0)
            {
                attrs.AddAttribute("学力", evt.studyDelta);
            }
        }

        if (evt.moneyDelta != 0)
        {
            if (EconomyManager.Instance != null && evt.moneyDelta < 0)
            {
                EconomyManager.Instance.Spend(-evt.moneyDelta, TransactionRecord.TransactionType.OtherExpense, $"课程表休闲: {evt.title}");
            }
            else if (GameState.Instance != null)
            {
                GameState.Instance.AddMoney(evt.moneyDelta);
            }
        }

        Debug.Log($"[CourseScheduleUI] 休闲事件：{evt.title} - {evt.description}");
    }

    private bool TryExecuteAction(string actionId, bool consumeActionPoints = true)
    {
        if (ActionSystem.Instance == null)
        {
            return false;
        }

        if (!consumeActionPoints)
        {
            ActionDefinition action = ActionSystem.Instance.GetAction(actionId);
            if (action == null)
            {
                return false;
            }

            if (action.moneyCost > 0)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.Spend(action.moneyCost,
                        TransactionRecord.TransactionType.OtherExpense,
                        $"行动消费: {action.displayName}");
                }
                else if (GameState.Instance != null)
                {
                    GameState.Instance.AddMoney(-action.moneyCost);
                }
            }

            if (action.effects != null && PlayerAttributes.Instance != null)
            {
                for (int i = 0; i < action.effects.Length; i++)
                {
                    PlayerAttributes.Instance.AddAttribute(action.effects[i].attributeName, action.effects[i].amount);
                }
            }

            return true;
        }

        return ActionSystem.Instance.ExecuteAction(actionId);
    }

    private void ApplyFallbackAction(int apCost, int moneyCost, params AttributeEffect[] effects)
    {
        if (GameState.Instance != null && apCost > 0)
        {
            GameState.Instance.ConsumeActionPoint(apCost);
        }

        if (moneyCost != 0 && GameState.Instance != null)
        {
            GameState.Instance.AddMoney(-moneyCost);
        }

        if (PlayerAttributes.Instance != null && effects != null)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                PlayerAttributes.Instance.AddAttribute(effects[i].attributeName, effects[i].amount);
            }
        }
    }

    private int GetFinalScheduleActionPointCost()
    {
        int courseCost = 0;
        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            if (GetDecisionForCourse(scheduleBlocks[i].course.id) == CourseDecision.Attend)
            {
                courseCost += 1;
            }
        }

        return courseCost + GetFreeTimeActionPointCost();
    }

    private int GetFreeTimeActionPointCost()
    {
        return freeTimeDecision switch
        {
            FreeTimeDecision.Study => 2,
            FreeTimeDecision.Memorize => 1,
            FreeTimeDecision.Leisure => 1,
            _ => 0
        };
    }

    private bool ShouldShowPlannedFreeTimeCard()
    {
        return selectedEmptyDayIndex >= 0 &&
               selectedEmptySlotIndex >= 0 &&
               freeTimeDecision != FreeTimeDecision.None;
    }

    private bool CanAffordFinalSchedule(int cost)
    {
        return GameState.Instance == null || GameState.Instance.ActionPoints >= cost;
    }

    private void ConsumeFinalScheduleActionPoints(int cost)
    {
        if (GameState.Instance != null && cost > 0)
        {
            GameState.Instance.ConsumeActionPoint(cost);
        }
    }

    private CourseDecision GetDecisionForCourse(string courseId)
    {
        if (string.IsNullOrEmpty(courseId))
        {
            return CourseDecision.None;
        }

        return courseDecisions.TryGetValue(courseId, out CourseDecision decision)
            ? decision
            : CourseDecision.None;
    }

    private void SetDecisionForSelectedCourse(CourseDecision decision)
    {
        if (string.IsNullOrEmpty(selectedCourseId))
        {
            return;
        }

        courseDecisions[selectedCourseId] = decision;

        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            if (scheduleBlocks[i].course.id == selectedCourseId)
            {
                courseDecisionHintText.text = $"已选中《{scheduleBlocks[i].course.courseName}》。{BuildDecisionSummary(selectedCourseId)}";
                break;
            }
        }
    }

    private bool AllCoursesDecided()
    {
        if (scheduleBlocks.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            if (GetDecisionForCourse(scheduleBlocks[i].course.id) == CourseDecision.None)
            {
                return false;
            }
        }

        return true;
    }

    private string BuildCourseAccentLabel(string courseId)
    {
        return BuildDecisionLabel(GetDecisionForCourse(courseId));
    }

    private string BuildDecisionLabel(CourseDecision decision)
    {
        return decision switch
        {
            CourseDecision.Attend => "本月上课",
            CourseDecision.Skip => "本月逃课",
            _ => "默认上课"
        };
    }

    private string BuildDecisionSummary(string courseId)
    {
        return GetDecisionForCourse(courseId) switch
        {
            CourseDecision.Attend => "当前决定：正常上课。",
            CourseDecision.Skip => "当前决定：本月逃课。",
            _ => "当前未单独改动，按默认正常上课处理。"
        };
    }

    private string GetSelectedCourseDisplayName()
    {
        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            if (scheduleBlocks[i].course.id == selectedCourseId)
            {
                return scheduleBlocks[i].course.courseName;
            }
        }

        return string.IsNullOrEmpty(currentCourse.courseName) ? "未选择课程" : currentCourse.courseName;
    }

    private string BuildRoundTitle()
    {
        int maxRounds = GameState.MaxRoundsPerSemester > 0 ? GameState.MaxRoundsPerSemester : 5;
        string suffix = currentRound >= maxRounds ? "（学期结束）" : string.Empty;
        return $"第{currentRound}回合{suffix}";
    }

    private string BuildCurrentDateLabel()
    {
        int month = GameState.Instance != null
            ? GameState.Instance.CurrentMonth
            : GameState.CalculateMonth(currentSemester, currentRound);
        return $"{month}月";
    }

    private string BuildCourseBlockTitle(string courseName)
    {
        if (string.IsNullOrEmpty(courseName))
        {
            return "未命名课程";
        }

        return courseName.Replace(" ", "\n");
    }

    private string GetPseudoDateLabel(int dayIndex)
    {
        int baseDate = 3 + (currentRound - 1) * 7;
        return (baseDate + dayIndex).ToString();
    }

    private string GetYearDisplayName(int year)
    {
        return year switch
        {
            1 => "大一",
            2 => "大二",
            3 => "大三",
            4 => "大四",
            _ => "大学"
        };
    }

    private Color GetCourseColor(string subjectTag)
    {
        return subjectTag switch
        {
            "math" => new Color(0.94f, 0.73f, 0.42f, 0.96f),
            "english" => new Color(0.92f, 0.55f, 0.44f, 0.96f),
            "politics" => new Color(0.89f, 0.45f, 0.60f, 0.96f),
            "cs" => new Color(0.41f, 0.84f, 0.78f, 0.96f),
            "pe" => new Color(0.93f, 0.46f, 0.63f, 0.96f),
            "history" => new Color(0.68f, 0.59f, 0.92f, 0.96f),
            "physics" => new Color(0.42f, 0.64f, 0.89f, 0.96f),
            "economics" => new Color(0.54f, 0.73f, 0.97f, 0.96f),
            _ => new Color(0.62f, 0.71f, 0.92f, 0.96f)
        };
    }

    private string GetSubjectDisplayName(string subjectTag)
    {
        return subjectTag switch
        {
            "math" => "数学类",
            "english" => "英语类",
            "politics" => "思政类",
            "cs" => "计算机类",
            "pe" => "体育类",
            "history" => "历史类",
            "physics" => "物理类",
            "economics" => "经济类",
            _ => "综合类"
        };
    }

    private void HideImmediate()
    {
        if (canvasRoot != null)
        {
            canvasRoot.SetActive(false);
        }
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private GameObject CreateCard(string name, Transform parent, Color color)
    {
        GameObject card = CreateUIObject(name, parent);
        Image image = card.AddComponent<Image>();
        image.color = color;
        return card;
    }

    private GameObject CreateControlCard(Transform parent, float minHeight)
    {
        GameObject card = CreateCard("ControlCard", parent, controlCardColor);
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.minHeight = minHeight;

        VerticalLayoutGroup group = card.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(20, 20, 18, 18);
        group.spacing = 8f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        return card;
    }

    private GameObject CreateButtonRow(Transform parent, int count, float preferredHeight)
    {
        GameObject row = CreateUIObject("ButtonRow", parent);
        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 12f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
        return row;
    }

    private Button CreateStudyCourseButton(Transform parent, CourseDefinition course, bool selected)
    {
        string label = $"{course.courseName}  ·  {GetSubjectDisplayName(course.subjectTag)}  ·  {course.credits}学分";
        Button button = CreateActionButton(parent, $"Study_{course.id}", label, selected ? selectedColor : controlCardAltColor, null);
        LayoutElement layout = button.GetComponent<LayoutElement>();
        layout.preferredHeight = 48f;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = selected ? textPrimary : controlTextColor;
            text.enableAutoSizing = true;
            text.fontSizeMin = 13f;
            text.fontSizeMax = 16f;
            RectTransform rect = text.rectTransform;
            rect.offsetMin = new Vector2(14f, 6f);
            rect.offsetMax = new Vector2(-14f, -6f);
        }

        return button;
    }

    private void CreateDetailScrollArea(Transform parent)
    {
        GameObject scrollRoot = CreateCard("DetailScrollRoot", parent, controlCardAltColor);
        LayoutElement rootLayout = scrollRoot.AddComponent<LayoutElement>();
        rootLayout.minHeight = 164f;
        rootLayout.flexibleHeight = 1f;

        ScrollRect scrollRect = scrollRoot.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewport = CreateUIObject("Viewport", scrollRoot.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        StretchToFill(viewportRect, Vector2.zero, Vector2.zero);

        GameObject content = CreateUIObject("Content", viewport.transform);
        detailScrollContent = content.GetComponent<RectTransform>();
        detailScrollContent.anchorMin = new Vector2(0f, 1f);
        detailScrollContent.anchorMax = new Vector2(1f, 1f);
        detailScrollContent.pivot = new Vector2(0.5f, 1f);
        detailScrollContent.offsetMin = new Vector2(14f, 0f);
        detailScrollContent.offsetMax = new Vector2(-14f, 0f);
        detailScrollContent.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup contentGroup = content.AddComponent<VerticalLayoutGroup>();
        contentGroup.padding = new RectOffset(0, 0, 10, 10);
        contentGroup.spacing = 8f;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = true;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = detailScrollContent;
    }

    private void CreateToolbarPill(Transform parent, string label, bool selected)
    {
        GameObject pill = CreateCard($"Pill_{label}", parent, selected ? new Color(0.48f, 0.82f, 1f, 0.30f) : new Color(1f, 1f, 1f, 0.22f));
        LayoutElement layout = pill.AddComponent<LayoutElement>();
        layout.preferredHeight = 52f;

        TextMeshProUGUI text = CreateText("Label", pill.transform, label, 22, textPrimary, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        StretchToFill(text.rectTransform, new Vector2(10f, 6f), new Vector2(-10f, -6f));
    }

    private void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void StretchToFill(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void CreateSectionTitle(Transform parent, string text)
    {
        TextMeshProUGUI title = CreateText("SectionTitle", parent, text, 24, controlTextColor, TextAlignmentOptions.TopLeft);
        title.fontSize = 22;
        title.fontStyle = FontStyles.Bold;
        ConfigureFlowText(title, 28f);
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private void ConfigureFlowText(TextMeshProUGUI text, float minHeight = 0f)
    {
        if (text == null)
        {
            return;
        }

        text.overflowMode = TextOverflowModes.Overflow;

        ContentSizeFitter fitter = text.gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = text.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement layout = text.gameObject.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = text.gameObject.AddComponent<LayoutElement>();
        }

        layout.minHeight = minHeight;
        layout.flexibleHeight = 0f;
    }

    private Button CreateActionButton(Transform parent, string name, string label, Color color, Action onClick)
    {
        GameObject obj = CreateUIObject(name, parent);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredWidth = 220f;
        layout.preferredHeight = 68f;

        Image image = obj.AddComponent<Image>();
        image.color = color;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        TextMeshProUGUI text = CreateText("Label", obj.transform, label, 16, controlTextColor, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 6f);
        rect.offsetMax = new Vector2(-10f, -6f);

        return button;
    }

    private void SetButtonSelected(Button button, bool selected, Color baseColor)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = !button.interactable
                ? disabledColor
                : (selected ? selectedColor : baseColor);
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.color = !button.interactable
                ? new Color(controlSubTextColor.r, controlSubTextColor.g, controlSubTextColor.b, 0.85f)
                : (selected ? textPrimary : controlTextColor);
        }
    }

    private bool IsSlotOccupied(int dayIndex, int slotIndex)
    {
        for (int i = 0; i < scheduleBlocks.Count; i++)
        {
            ScheduleBlockData block = scheduleBlocks[i];
            if (block.dayIndex != dayIndex)
            {
                continue;
            }

            if (slotIndex >= block.startSlot && slotIndex < block.startSlot + block.duration)
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldHideScheduleBlock(ScheduleBlockData block)
    {
        return false;
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
