using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 兼职/实习选择界面。数据由 JobSystem 从 jobs.json 加载。
/// </summary>
public class JobSelectionUI : MonoBehaviour
{
    public static JobSelectionUI Instance { get; private set; }

    private GameObject canvasObj;
    private Button btnInternship;
    private Button btnSideHustle;
    private TextMeshProUGUI txtLockMessage;
    private RectTransform contentContainer;
    private ScrollRect scrollRect;
    private bool isShowingInternship = true;

    private readonly Color PanelBgColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
    private readonly Color TopBarColor = new Color(0.08f, 0.08f, 0.1f, 1f);
    private readonly Color CardBgColor = new Color(0.18f, 0.18f, 0.22f, 1f);
    private readonly Color TextTitleColor = new Color(0.95f, 0.85f, 0.6f, 1f);
    private readonly Color TextNormalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    private readonly Color TextSubColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    private readonly Color TextWarningColor = new Color(0.9f, 0.4f, 0.4f, 1f);
    private readonly Color TextIncomeColor = new Color(0.4f, 0.9f, 0.4f, 1f);
    private readonly Color TabActiveColor = new Color(0.2f, 0.35f, 0.6f, 1f);
    private readonly Color TabInactiveColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private readonly Color BtnExecColor = new Color(0.3f, 0.5f, 0.8f, 1f);
    private readonly Color BtnDisabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);

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
        Hide();
    }

    public void Show()
    {
        if (!UIFlowGuard.PrepareForExclusiveWindow(UIFlowGuard.WindowJobSelection))
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("无法打开工作面板", "当前有其他独占界面正在占用输入，稍后再试。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }

        canvasObj.SetActive(true);
        SwitchTab(isShowingInternship);
    }

    public void Hide()
    {
        if (canvasObj != null)
        {
            canvasObj.SetActive(false);
        }
    }

    public bool IsOpen => canvasObj != null && canvasObj.activeSelf;

    private void BuildUI()
    {
        canvasObj = new GameObject("JobSelectionCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject bgObj = CreatePanel("Background", canvasObj.transform, new Color(0f, 0f, 0f, 0.7f));
        StretchFull(bgObj.GetComponent<RectTransform>());
        bgObj.AddComponent<Button>().onClick.AddListener(Hide);

        GameObject panelObj = CreatePanel("MainPanel", canvasObj.transform, PanelBgColor);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1300, 800);
        panelObj.AddComponent<Button>();

        GameObject titleBarObj = CreatePanel("TitleBar", panelObj.transform, TopBarColor);
        RectTransform titleBarRect = titleBarObj.GetComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0, 1);
        titleBarRect.anchorMax = new Vector2(1, 1);
        titleBarRect.pivot = new Vector2(0.5f, 1f);
        titleBarRect.sizeDelta = new Vector2(0, 80);
        titleBarRect.anchoredPosition = Vector2.zero;
        CreateText("Title", titleBarObj.transform, "兼职与实习", 36, TextTitleColor, TextAlignmentOptions.Center, new Vector2(400, 80), Vector2.zero);

        Button closeButton = CreateButton("Close", titleBarObj.transform, "X", new Vector2(60, 60), new Color(0.8f, 0.3f, 0.3f, 1f), 30f);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(-40, 0);
        closeButton.onClick.AddListener(Hide);

        GameObject tabBarObj = new GameObject("TabBar");
        tabBarObj.transform.SetParent(panelObj.transform, false);
        RectTransform tabBarRect = tabBarObj.AddComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0, 1);
        tabBarRect.anchorMax = new Vector2(1, 1);
        tabBarRect.pivot = new Vector2(0.5f, 1f);
        tabBarRect.sizeDelta = new Vector2(0, 60);
        tabBarRect.anchoredPosition = new Vector2(0, -80);
        HorizontalLayoutGroup tabLayout = tabBarObj.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;

        btnInternship = CreateTabButton("btnTabIntern", tabBarObj.transform, "实习项目");
        btnSideHustle = CreateTabButton("btnTabHustle", tabBarObj.transform, "副业兼职");
        btnInternship.onClick.AddListener(() => SwitchTab(true));
        btnSideHustle.onClick.AddListener(() => SwitchTab(false));

        GameObject lockMsgObj = new GameObject("LockMessage");
        lockMsgObj.transform.SetParent(panelObj.transform, false);
        RectTransform lockRect = lockMsgObj.AddComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0, 0);
        lockRect.anchorMax = new Vector2(1, 1);
        lockRect.offsetMin = Vector2.zero;
        lockRect.offsetMax = new Vector2(0, -140);
        txtLockMessage = CreateText("txtLock", lockMsgObj.transform, string.Empty, 30, TextSubColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
        txtLockMessage.rectTransform.anchorMin = Vector2.zero;
        txtLockMessage.rectTransform.anchorMax = Vector2.one;

        GameObject scrollObj = CreatePanel("ScrollView", panelObj.transform, new Color(0f, 0f, 0f, 0.2f));
        RectTransform scrollRootRect = scrollObj.GetComponent<RectTransform>();
        scrollRootRect.anchorMin = new Vector2(0, 0);
        scrollRootRect.anchorMax = new Vector2(1, 1);
        scrollRootRect.offsetMin = new Vector2(20, 20);
        scrollRootRect.offsetMax = new Vector2(-20, -150);

        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportObj.AddComponent<RectMask2D>();

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        contentContainer = contentObj.AddComponent<RectTransform>();
        contentContainer.anchorMin = new Vector2(0, 1);
        contentContainer.anchorMax = new Vector2(1, 1);
        contentContainer.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 15;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentContainer;
    }

    private void SwitchTab(bool showInternship)
    {
        isShowingInternship = showInternship;
        btnInternship.GetComponent<Image>().color = isShowingInternship ? TabActiveColor : TabInactiveColor;
        btnSideHustle.GetComponent<Image>().color = !isShowingInternship ? TabActiveColor : TabInactiveColor;

        bool unlocked = JobSystem.Instance != null &&
            (isShowingInternship ? JobSystem.Instance.IsInternshipUnlocked() : JobSystem.Instance.IsSideHustleUnlocked());

        if (!unlocked)
        {
            txtLockMessage.gameObject.SetActive(true);
            scrollRect.gameObject.SetActive(false);
            txtLockMessage.text = isShowingInternship
                ? "实习尚未解锁。\n\n要求：大二下学期，并且通过英语四级或 GPA≥2.5"
                : "副业尚未解锁。\n\n要求：大二及以上，并且黑暗值≥15或朋友(好感≥40)人数≥3";
            return;
        }

        txtLockMessage.gameObject.SetActive(false);
        scrollRect.gameObject.SetActive(true);
        RefreshJobList();
    }

    private void RefreshJobList()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        if (JobSystem.Instance == null)
        {
            CreateEmptyHint("工作情报暂时还没汇总到这里。\n先推进学期、提升属性，新的工作机会会逐步整理出来。");
            return;
        }

        List<JobDefinitionData> jobs = JobSystem.Instance.GetJobs(isShowingInternship);
        if (jobs.Count == 0)
        {
            CreateEmptyHint(isShowingInternship
                ? "这一阶段还没有可执行的实习项目。\n先继续提升学业或证书进度，后续会出现更正式的岗位。"
                : "这一阶段还没有合适的副业兼职。\n多推进人际、黑暗值或学年进度，新的赚钱路子会慢慢打开。");
            return;
        }

        for (int i = 0; i < jobs.Count; i++)
        {
            CreateJobCard(jobs[i]);
        }
    }

    private void CreateJobCard(JobDefinitionData job)
    {
        GameObject cardObj = CreatePanel($"Card_{job.id}", contentContainer, CardBgColor);
        LayoutElement le = cardObj.AddComponent<LayoutElement>();
        le.minHeight = 168;
        le.preferredHeight = 168;

        CreateText("Name", cardObj.transform, job.name, 28, TextTitleColor, TextAlignmentOptions.Left, new Vector2(320, 40), new Vector2(30, 48))
            .rectTransform.pivot = new Vector2(0f, 0.5f);
        CreateText("Description", cardObj.transform, job.description, 18, TextSubColor, TextAlignmentOptions.Left, new Vector2(640, 48), new Vector2(30, 10))
            .enableWordWrapping = true;

        CreateText("Income", cardObj.transform, $"收益: +{job.baseIncome}元", 24, TextIncomeColor, TextAlignmentOptions.Left, new Vector2(200, 30), new Vector2(30, -34))
            .rectTransform.pivot = new Vector2(0f, 0.5f);
        CreateText("AP", cardObj.transform, $"消耗: {job.apCost} AP", 24, TextWarningColor, TextAlignmentOptions.Left, new Vector2(180, 30), new Vector2(250, -34))
            .rectTransform.pivot = new Vector2(0f, 0.5f);

        CreateText("Req", cardObj.transform, $"要求: {BuildRequirementText(job)}", 20, TextSubColor, TextAlignmentOptions.Left, new Vector2(620, 28), new Vector2(30, -70))
            .rectTransform.pivot = new Vector2(0f, 0.5f);
        CreateText("Eff", cardObj.transform, $"效果: {BuildEffectText(job)}", 20, TextNormalColor, TextAlignmentOptions.Left, new Vector2(620, 28), new Vector2(30, -102))
            .rectTransform.pivot = new Vector2(0f, 0.5f);

        string failReason = string.Empty;
        bool canExecute = JobSystem.Instance != null && JobSystem.Instance.CanExecuteJob(job, out failReason);
        Button executeButton = CreateButton("Execute", cardObj.transform, canExecute ? "执行" : failReason, new Vector2(180, 64), canExecute ? BtnExecColor : BtnDisabledColor, 24f);
        RectTransform btnRect = executeButton.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 0.5f);
        btnRect.anchorMax = new Vector2(1f, 0.5f);
        btnRect.pivot = new Vector2(1f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-30, -4);
        executeButton.interactable = canExecute;
        if (canExecute)
        {
            executeButton.onClick.AddListener(() => ExecuteJob(job));
        }
    }

    private void ExecuteJob(JobDefinitionData job)
    {
        if (JobSystem.Instance == null)
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("工作系统不可用", "兼职与实习系统还没有准备好，现在暂时无法执行这个项目。", new Color(0.85f, 0.34f, 0.24f), 2.8f);
            }
            return;
        }

        if (!JobSystem.Instance.ExecuteJob(job, out string failReason))
        {
            if (MissionUI.Instance != null && !string.IsNullOrEmpty(failReason))
            {
                MissionUI.Instance.ShowSystemNotification("工作执行失败", failReason, new Color(0.85f, 0.34f, 0.24f), 2.8f);
            }
            SwitchTab(isShowingInternship);
            return;
        }

        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(
                job.name,
                BuildJobExecutionSummary(job),
                new Color(0.22f, 0.72f, 0.34f),
                3.2f);
        }

        Debug.Log($"[JobSelectionUI] 执行了 {job.name}");
        SwitchTab(isShowingInternship);
        Hide();
    }

    private string BuildRequirementText(JobDefinitionData job)
    {
        if (job == null || job.requirements == null || job.requirements.Count == 0)
        {
            return "无额外要求";
        }

        List<string> parts = new List<string>();
        for (int i = 0; i < job.requirements.Count; i++)
        {
            JobRequirementData requirement = job.requirements[i];
            if (requirement == null)
            {
                continue;
            }

            string type = string.IsNullOrWhiteSpace(requirement.type)
                ? "attribute"
                : requirement.type.Trim().ToLowerInvariant();

            if (type == "attribute")
            {
                parts.Add($"{GetDisplayName(requirement.target)}≥{requirement.min}");
            }
            else if (type == "cet4")
            {
                parts.Add("通过四级");
            }
            else if (type == "gpa")
            {
                parts.Add($"GPA≥{requirement.min / 100f:F1}");
            }
            else if (type == "semester")
            {
                parts.Add($"第{requirement.min}学期起");
            }
            else
            {
                parts.Add($"{requirement.type}:{requirement.min}");
            }
        }

        return parts.Count == 0 ? "无额外要求" : string.Join("，", parts);
    }

    private string BuildEffectText(JobDefinitionData job)
    {
        if (job == null || job.effects == null || job.effects.Count == 0)
        {
            return "主要获得收入";
        }

        List<string> parts = new List<string>();
        for (int i = 0; i < job.effects.Count; i++)
        {
            JobEffectData effect = job.effects[i];
            if (effect == null)
            {
                continue;
            }

            string type = string.IsNullOrWhiteSpace(effect.type)
                ? "attribute"
                : effect.type.Trim().ToLowerInvariant();

            if (type == "attribute")
            {
                string sign = effect.value >= 0 ? "+" : string.Empty;
                parts.Add($"{GetDisplayName(effect.target)}{sign}{effect.value}");
            }
            else if (type == "money")
            {
                string sign = effect.value >= 0 ? "+" : string.Empty;
                parts.Add($"金钱{sign}{effect.value}");
            }
        }

        return parts.Count == 0 ? "主要获得收入" : string.Join("，", parts);
    }

    private string BuildJobExecutionSummary(JobDefinitionData job)
    {
        if (job == null)
        {
            return "已完成本回合工作安排。";
        }

        List<string> parts = new List<string>
        {
            $"收入 +{job.baseIncome}"
        };

        if (job.apCost > 0)
        {
            parts.Add($"消耗 {job.apCost} 行动点");
        }

        string effectText = BuildEffectText(job);
        if (!string.IsNullOrEmpty(effectText) && effectText != "主要获得收入")
        {
            parts.Add(effectText);
        }

        return string.Join("，", parts) + "。";
    }

    private void CreateEmptyHint(string text)
    {
        TextMeshProUGUI tmp = CreateText("Empty", contentContainer, text, 24, TextSubColor, TextAlignmentOptions.Center, new Vector2(600, 120), Vector2.zero);
        LayoutElement le = tmp.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 160;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    private Button CreateTabButton(string name, Transform parent, string text)
    {
        Button btn = CreateButton(name, parent, text, Vector2.zero, TabInactiveColor, 28f);
        btn.GetComponentInChildren<TextMeshProUGUI>().color = TextNormalColor;
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        return btn;
    }

    private Button CreateButton(string name, Transform parent, string text, Vector2 size, Color bgColor, float fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = obj.AddComponent<Button>();
        TextMeshProUGUI label = CreateText("Text", obj.transform, text, (int)fontSize, Color.white, TextAlignmentOptions.Center, size, Vector2.zero);
        StretchFull(label.rectTransform);
        return btn;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;

        if (FontManager.Instance != null)
        {
            tmp.font = FontManager.Instance.ChineseFont;
        }

        return tmp;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private string GetDisplayName(string target)
    {
        switch (target)
        {
            case "Study": return "学力";
            case "Charm": return "魅力";
            case "Physique": return "体魄";
            case "Leadership": return "领导力";
            case "Stress": return "压力";
            case "Mood": return "心情";
            case "Darkness": return "黑暗值";
            case "Guilt": return "负罪感";
            case "Luck": return "幸运";
            default: return target;
        }
    }

    public static GameObject AddJobButton(Transform parent)
    {
        bool shouldShow = JobSystem.Instance != null &&
            (JobSystem.Instance.IsInternshipUnlocked() || JobSystem.Instance.IsSideHustleUnlocked());
        if (!shouldShow)
        {
            return null;
        }

        GameObject btnObj = new GameObject("btnJob");
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.6f, 1f);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 100);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (Instance != null)
            {
                Instance.Show();
            }
        });

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "兼职\n实习";
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        if (FontManager.Instance != null)
        {
            tmp.font = FontManager.Instance.ChineseFont;
        }

        return btnObj;
    }
}
