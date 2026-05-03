using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 课程表界面：每回合开始时强制弹出，安排必修课与空余时间。
/// </summary>
public class CourseScheduleUI : MonoBehaviour
{
    public static CourseScheduleUI Instance { get; private set; }

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

    private readonly LeisureEvent[] leisureEvents = new LeisureEvent[]
    {
        new LeisureEvent("窗边慢读", "你带着一杯热饮坐到窗边，读了点闲书，脑子松了下来。", 6, -4, 1, -12),
        new LeisureEvent("校园散步", "午后的风把人吹清醒了，绕着校园慢慢走了一圈。", 5, -5, 0, 0),
        new LeisureEvent("宿舍补眠", "你把手机一扣，老老实实眯了一会儿，状态回暖不少。", 4, -6, 0, 0),
        new LeisureEvent("轻松观影", "摸了一部短片当奖励，心情好了，学习欲也回来了。", 7, -3, 1, -8)
    };

    private Canvas canvas;
    private GameObject canvasRoot;
    private GameObject panelObj;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI requiredCourseText;
    private TextMeshProUGUI semesterCourseListText;
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
    private CourseDecision courseDecision = CourseDecision.None;
    private FreeTimeDecision freeTimeDecision = FreeTimeDecision.None;
    private int currentYear;
    private int currentSemester;
    private int currentRound;

    public bool IsOpen => canvasRoot != null && canvasRoot.activeSelf;

    private readonly Color panelColor = new Color(0.08f, 0.1f, 0.15f, 0.96f);
    private readonly Color cardColor = new Color(0.14f, 0.17f, 0.24f, 1f);
    private readonly Color cardAltColor = new Color(0.12f, 0.14f, 0.2f, 1f);
    private readonly Color accentColor = new Color(0.91f, 0.79f, 0.45f, 1f);
    private readonly Color normalTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);
    private readonly Color subTextColor = new Color(0.68f, 0.73f, 0.82f, 1f);
    private readonly Color actionColor = new Color(0.25f, 0.42f, 0.72f, 1f);
    private readonly Color warningColor = new Color(0.72f, 0.32f, 0.32f, 1f);
    private readonly Color selectedColor = new Color(0.28f, 0.55f, 0.86f, 1f);
    private readonly Color confirmColor = new Color(0.29f, 0.62f, 0.42f, 1f);
    private readonly Color disabledColor = new Color(0.28f, 0.3f, 0.34f, 1f);

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

    public void ShowSchedule(int year, int semester, int round, Action completedCallback)
    {
        currentYear = year;
        currentSemester = semester;
        currentRound = round;
        onCompleted = completedCallback;
        semesterCourses = ExamSystem.Instance != null
            ? ExamSystem.Instance.GetCoursesForSemester(year, semester)
            : Array.Empty<CourseDefinition>();
        currentCourse = ResolveRequiredCourse(semesterCourses, round);

        courseDecision = CourseDecision.None;
        freeTimeDecision = FreeTimeDecision.None;

        RefreshContent();
        canvasRoot.SetActive(true);
    }

    private CourseDefinition ResolveRequiredCourse(CourseDefinition[] courses, int round)
    {
        if (courses == null || courses.Length == 0)
        {
            return new CourseDefinition
            {
                id = $"TEMP_{round}",
                courseName = "通识必修课",
                credits = 2,
                subjectTag = "general",
                year = currentYear,
                semester = currentSemester
            };
        }

        int index = Mathf.Clamp(round - 1, 0, courses.Length - 1);
        return courses[index];
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
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot.AddComponent<GraphicRaycaster>();

        GameObject overlay = CreateUIObject("Overlay", canvasRoot.transform);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
        StretchFullScreen(overlay.GetComponent<RectTransform>());

        panelObj = CreateUIObject("Panel", canvasRoot.transform);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = panelColor;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1360f, 820f);

        VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(32, 32, 28, 28);
        panelLayout.spacing = 18f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        GameObject header = CreateUIObject("Header", panelObj.transform);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.minHeight = 96f;
        titleText = CreateText("Title", header.transform, "课程表", 42, accentColor, TextAlignmentOptions.TopLeft);
        titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;

        subtitleText = CreateText("Subtitle", header.transform, "", 24, subTextColor, TextAlignmentOptions.BottomLeft);
        subtitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
        subtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitleText.rectTransform.offsetMin = new Vector2(0f, 0f);
        subtitleText.rectTransform.offsetMax = new Vector2(0f, -6f);

        GameObject body = CreateUIObject("Body", panelObj.transform);
        LayoutElement bodyLayout = body.AddComponent<LayoutElement>();
        bodyLayout.minHeight = 560f;
        HorizontalLayoutGroup bodyGroup = body.AddComponent<HorizontalLayoutGroup>();
        bodyGroup.spacing = 20f;
        bodyGroup.childControlWidth = true;
        bodyGroup.childControlHeight = true;
        bodyGroup.childForceExpandWidth = true;
        bodyGroup.childForceExpandHeight = true;

        GameObject leftColumn = CreateCard("LeftColumn", body.transform, cardColor);
        LayoutElement leftLayout = leftColumn.AddComponent<LayoutElement>();
        leftLayout.preferredWidth = 760f;
        VerticalLayoutGroup leftGroup = leftColumn.AddComponent<VerticalLayoutGroup>();
        leftGroup.padding = new RectOffset(24, 24, 24, 24);
        leftGroup.spacing = 18f;
        leftGroup.childControlHeight = false;
        leftGroup.childControlWidth = true;
        leftGroup.childForceExpandWidth = true;
        leftGroup.childForceExpandHeight = false;

        CreateSectionTitle(leftColumn.transform, "本回合必修课");
        requiredCourseText = CreateText("RequiredCourse", leftColumn.transform, "", 28, normalTextColor, TextAlignmentOptions.TopLeft);
        LayoutElement requiredTextLayout = requiredCourseText.gameObject.AddComponent<LayoutElement>();
        requiredTextLayout.minHeight = 150f;

        GameObject decisionRow = CreateUIObject("DecisionRow", leftColumn.transform);
        HorizontalLayoutGroup decisionGroup = decisionRow.AddComponent<HorizontalLayoutGroup>();
        decisionGroup.spacing = 14f;
        decisionGroup.childControlWidth = true;
        decisionGroup.childControlHeight = true;
        decisionGroup.childForceExpandWidth = true;
        decisionGroup.childForceExpandHeight = false;
        LayoutElement decisionLayout = decisionRow.AddComponent<LayoutElement>();
        decisionLayout.minHeight = 92f;

        attendButton = CreateActionButton(decisionRow.transform, "BtnAttend", "正常上课", actionColor, () =>
        {
            courseDecision = CourseDecision.Attend;
            RefreshSelectionState();
        });
        skipButton = CreateActionButton(decisionRow.transform, "BtnSkip", "逃课摸鱼", warningColor, () =>
        {
            courseDecision = CourseDecision.Skip;
            RefreshSelectionState();
        });

        CreateSectionTitle(leftColumn.transform, "空余时间安排");

        GameObject freeRow = CreateUIObject("FreeRow", leftColumn.transform);
        HorizontalLayoutGroup freeGroup = freeRow.AddComponent<HorizontalLayoutGroup>();
        freeGroup.spacing = 14f;
        freeGroup.childControlWidth = true;
        freeGroup.childControlHeight = true;
        freeGroup.childForceExpandWidth = true;
        freeGroup.childForceExpandHeight = false;
        LayoutElement freeLayout = freeRow.AddComponent<LayoutElement>();
        freeLayout.minHeight = 108f;

        studyButton = CreateActionButton(freeRow.transform, "BtnStudy", "图书馆自习\n学力+7 压力+5 AP-2", actionColor, () =>
        {
            freeTimeDecision = FreeTimeDecision.Study;
            RefreshSelectionState();
        });
        memorizeButton = CreateActionButton(freeRow.transform, "BtnMemorize", "背单词\n学力+1 压力+2 AP-1", new Color(0.23f, 0.49f, 0.64f, 1f), () =>
        {
            freeTimeDecision = FreeTimeDecision.Memorize;
            RefreshSelectionState();
        });
        leisureButton = CreateActionButton(freeRow.transform, "BtnLeisure", "轻松安排\n随机休闲事件 AP-1", new Color(0.45f, 0.36f, 0.66f, 1f), () =>
        {
            freeTimeDecision = FreeTimeDecision.Leisure;
            RefreshSelectionState();
        });

        CreateSectionTitle(leftColumn.transform, "安排预览");
        previewText = CreateText("Preview", leftColumn.transform, "", 24, subTextColor, TextAlignmentOptions.TopLeft);
        LayoutElement previewLayout = previewText.gameObject.AddComponent<LayoutElement>();
        previewLayout.minHeight = 180f;

        GameObject rightColumn = CreateCard("RightColumn", body.transform, cardAltColor);
        LayoutElement rightLayout = rightColumn.AddComponent<LayoutElement>();
        rightLayout.preferredWidth = 520f;
        VerticalLayoutGroup rightGroup = rightColumn.AddComponent<VerticalLayoutGroup>();
        rightGroup.padding = new RectOffset(24, 24, 24, 24);
        rightGroup.spacing = 16f;
        rightGroup.childControlWidth = true;
        rightGroup.childControlHeight = false;
        rightGroup.childForceExpandWidth = true;
        rightGroup.childForceExpandHeight = false;

        CreateSectionTitle(rightColumn.transform, "本学期课程一览");
        semesterCourseListText = CreateText("SemesterCourseList", rightColumn.transform, "", 24, normalTextColor, TextAlignmentOptions.TopLeft);
        semesterCourseListText.enableWordWrapping = true;

        GameObject footer = CreateUIObject("Footer", panelObj.transform);
        LayoutElement footerLayout = footer.AddComponent<LayoutElement>();
        footerLayout.minHeight = 88f;
        HorizontalLayoutGroup footerGroup = footer.AddComponent<HorizontalLayoutGroup>();
        footerGroup.spacing = 16f;
        footerGroup.childControlWidth = true;
        footerGroup.childControlHeight = true;
        footerGroup.childForceExpandWidth = false;
        footerGroup.childForceExpandHeight = false;
        footerGroup.childAlignment = TextAnchor.MiddleRight;

        confirmButton = CreateActionButton(footer.transform, "BtnConfirm", "开始本回合", confirmColor, CommitSelection);
        LayoutElement confirmLayout = confirmButton.gameObject.GetComponent<LayoutElement>();
        confirmLayout.preferredWidth = 320f;
        confirmLayout.preferredHeight = 72f;
        confirmButtonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void RefreshContent()
    {
        string yearName = currentYear switch
        {
            1 => "大一",
            2 => "大二",
            3 => "大三",
            4 => "大四",
            _ => "大学"
        };

        subtitleText.text = $"{yearName}{(currentSemester == 1 ? "上" : "下")} · 回合{currentRound} · 先把今天排明白";
        titleText.text = "课程表";
        requiredCourseText.text =
            $"《{currentCourse.courseName}》\n" +
            $"课程标签：{GetSubjectDisplayName(currentCourse.subjectTag)}\n" +
            $"学分：{currentCourse.credits}\n\n" +
            "这门课是本回合的必修安排。你可以老老实实去上，也可以拐个弯直接失踪。";

        if (semesterCourses == null || semesterCourses.Length == 0)
        {
            semesterCourseListText.text = "本学期课程数据暂未就绪，先按通识必修处理。";
        }
        else
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < semesterCourses.Length; i++)
            {
                CourseDefinition course = semesterCourses[i];
                string prefix = course.id == currentCourse.id ? ">>" : "  ";
                lines.Add($"{prefix} {course.courseName}  ·  {course.credits}学分");
            }
            semesterCourseListText.text = string.Join("\n", lines);
        }

        RefreshSelectionState();
    }

    private void RefreshSelectionState()
    {
        SetButtonSelected(attendButton, courseDecision == CourseDecision.Attend, actionColor);
        SetButtonSelected(skipButton, courseDecision == CourseDecision.Skip, warningColor);
        SetButtonSelected(studyButton, freeTimeDecision == FreeTimeDecision.Study, actionColor);
        SetButtonSelected(memorizeButton, freeTimeDecision == FreeTimeDecision.Memorize, new Color(0.23f, 0.49f, 0.64f, 1f));
        SetButtonSelected(leisureButton, freeTimeDecision == FreeTimeDecision.Leisure, new Color(0.45f, 0.36f, 0.66f, 1f));

        previewText.text = BuildPreviewText();

        bool ready = courseDecision != CourseDecision.None && freeTimeDecision != FreeTimeDecision.None;
        confirmButton.interactable = ready;
        Image confirmImage = confirmButton.GetComponent<Image>();
        if (confirmImage != null)
        {
            confirmImage.color = ready ? confirmColor : disabledColor;
        }
        if (confirmButtonText != null)
        {
            confirmButtonText.text = ready ? "开始本回合" : "先安排课程和空余时间";
        }
    }

    private string BuildPreviewText()
    {
        List<string> lines = new List<string>();

        if (courseDecision == CourseDecision.None)
        {
            lines.Add("1. 先决定今天这门必修课是去上，还是逃。");
        }
        else if (courseDecision == CourseDecision.Attend)
        {
            lines.Add($"1. 上《{currentCourse.courseName}》：学力+5，压力+3，消耗2AP。");
        }
        else
        {
            lines.Add($"1. 逃掉《{currentCourse.courseName}》：心情+4，压力-1，负罪感+3，黑暗值+1。");
        }

        if (freeTimeDecision == FreeTimeDecision.None)
        {
            lines.Add("2. 再给空余时间选个安排。");
        }
        else if (freeTimeDecision == FreeTimeDecision.Study)
        {
            lines.Add("2. 图书馆自习：学力+7，压力+5，消耗2AP，并计入考试自习次数。");
        }
        else if (freeTimeDecision == FreeTimeDecision.Memorize)
        {
            lines.Add("2. 背单词：学力+1，压力+2，消耗1AP。");
        }
        else
        {
            lines.Add("2. 轻松安排：触发随机休闲事件，消耗1AP。");
        }

        return string.Join("\n", lines);
    }

    private void CommitSelection()
    {
        if (courseDecision == CourseDecision.None || freeTimeDecision == FreeTimeDecision.None)
        {
            return;
        }

        ApplyCourseDecision();
        ApplyFreeTimeDecision();

        Debug.Log($"[CourseScheduleUI] 已完成课程安排 —— {currentYear}-{currentSemester}-R{currentRound}");

        HideImmediate();
        Action completed = onCompleted;
        onCompleted = null;
        completed?.Invoke();
    }

    private void ApplyCourseDecision()
    {
        if (courseDecision == CourseDecision.Attend)
        {
            if (!TryExecuteAction("attend_class"))
            {
                ApplyFallbackAction(2, 0, new AttributeEffect("学力", 5), new AttributeEffect("压力", 3));
            }
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

    private void ApplyFreeTimeDecision()
    {
        switch (freeTimeDecision)
        {
            case FreeTimeDecision.Study:
                if (!TryExecuteAction("study"))
                {
                    ApplyFallbackAction(2, 0, new AttributeEffect("学力", 7), new AttributeEffect("压力", 5));
                    if (ExamSystem.Instance != null)
                    {
                        ExamSystem.Instance.RegisterScheduleStudySession();
                    }
                }
                break;

            case FreeTimeDecision.Memorize:
                if (!TryExecuteAction("memorize_words"))
                {
                    ApplyFallbackAction(1, 0, new AttributeEffect("学力", 1), new AttributeEffect("压力", 2));
                }
                break;

            case FreeTimeDecision.Leisure:
                ApplyLeisureEvent();
                break;
        }
    }

    private void ApplyLeisureEvent()
    {
        if (GameState.Instance != null)
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

    private bool TryExecuteAction(string actionId)
    {
        if (ActionSystem.Instance == null)
        {
            return false;
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

    private void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private GameObject CreateCard(string name, Transform parent, Color color)
    {
        GameObject card = CreateUIObject(name, parent);
        Image image = card.AddComponent<Image>();
        image.color = color;
        return card;
    }

    private void CreateSectionTitle(Transform parent, string text)
    {
        TextMeshProUGUI title = CreateText("SectionTitle", parent, text, 28, accentColor, TextAlignmentOptions.TopLeft);
        LayoutElement layout = title.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 40f;
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

    private Button CreateActionButton(Transform parent, string name, string label, Color color, Action onClick)
    {
        GameObject obj = CreateUIObject(name, parent);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredWidth = 220f;
        layout.preferredHeight = 88f;

        Image image = obj.AddComponent<Image>();
        image.color = color;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        TextMeshProUGUI text = CreateText("Label", obj.transform, label, 22, normalTextColor, TextAlignmentOptions.Center);
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 8f);
        rect.offsetMax = new Vector2(-10f, -8f);

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
            image.color = selected ? selectedColor : baseColor;
        }
    }
}
