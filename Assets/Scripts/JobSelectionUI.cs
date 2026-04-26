using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 兼职实习选择界面，纯代码构建UI，挂载于独立Canvas (sortingOrder=200)
/// 包含实习和副业两个标签页，硬编码临时数据
/// </summary>
public class JobSelectionUI : MonoBehaviour
{
    public static JobSelectionUI Instance { get; private set; }

    [Header("UI Components")]
    private GameObject canvasObj;
    private Canvas canvas;
    private GraphicRaycaster raycaster;

    private GameObject panelObj;
    private Button btnInternship;
    private Button btnSideHustle;
    private TextMeshProUGUI txtInternship;
    private TextMeshProUGUI txtSideHustle;
    private TextMeshProUGUI txtLockMessage;
    private RectTransform contentContainer;
    private ScrollRect scrollRect;

    private bool isShowingInternship = true;

    // 颜色规范
    private readonly Color PanelBgColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
    private readonly Color TopBarColor = new Color(0.08f, 0.08f, 0.1f, 1f);
    private readonly Color CardBgColor = new Color(0.18f, 0.18f, 0.22f, 1f);
    private readonly Color TextTitleColor = new Color(0.95f, 0.85f, 0.6f, 1f); // Gold
    private readonly Color TextNormalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    private readonly Color TextSubColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    private readonly Color TextWarningColor = new Color(0.9f, 0.4f, 0.4f, 1f);
    private readonly Color TextIncomeColor = new Color(0.4f, 0.9f, 0.4f, 1f);
    private readonly Color TabActiveColor = new Color(0.2f, 0.35f, 0.6f, 1f);
    private readonly Color TabInactiveColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private readonly Color BtnExecColor = new Color(0.3f, 0.5f, 0.8f, 1f);
    private readonly Color BtnDisabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    // ================== 数据定义 ==================
    public struct JobDef
    {
        public string id;
        public string name;
        public bool isInternship;
        public int income;
        public int apCost;

        public int reqStudy;
        public int reqCharm;
        public int reqLeadership;
        public int reqDarkness;

        public int effectStudy;
        public int effectCharm;
        public int effectPhysique;
        public int effectLeadership;
        public int effectStress;
        public int effectMood;
        public int effectDarkness;
    }

    private List<JobDef> allJobs = new List<JobDef>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitJobData();
        BuildUI();
        Hide();
    }

    private void InitJobData()
    {
        // 实习
        allJobs.Add(new JobDef { id = "intern_tech", name = "技术开发实习", isInternship = true, income = 1500, apCost = 4, reqStudy = 70, effectStudy = 5, effectPhysique = -3, effectStress = 10 });
        allJobs.Add(new JobDef { id = "intern_finance", name = "金融助理实习", isInternship = true, income = 2000, apCost = 4, reqLeadership = 60, effectCharm = 3, effectLeadership = 5, effectStress = 15 });
        allJobs.Add(new JobDef { id = "intern_media", name = "新媒体运营", isInternship = true, income = 1200, apCost = 3, reqCharm = 60, effectCharm = 8, effectMood = -5 });
        allJobs.Add(new JobDef { id = "intern_research", name = "实验室助理", isInternship = true, income = 800, apCost = 3, reqStudy = 80, effectStudy = 10, effectStress = -5 });

        // 副业
        allJobs.Add(new JobDef { id = "side_tutor", name = "家教兼职", isInternship = false, income = 500, apCost = 2, reqStudy = 60, effectStudy = 2, effectStress = 5 });
        allJobs.Add(new JobDef { id = "side_delivery", name = "外卖骑手", isInternship = false, income = 300, apCost = 2, effectPhysique = 5, effectStress = 8 });
        allJobs.Add(new JobDef { id = "side_freelance", name = "自由撰稿人", isInternship = false, income = 800, apCost = 3, reqCharm = 50, effectCharm = 5, effectStress = -2 });
        allJobs.Add(new JobDef { id = "side_campus_agent", name = "校园代理", isInternship = false, income = 1000, apCost = 3, reqDarkness = 20, effectLeadership = 8, effectDarkness = 5 });
    }

    // ================== UI 构建 ==================
    private void BuildUI()
    {
        // 1. Canvas
        canvasObj = new GameObject("JobSelectionCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Background (Black overlay)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Button bgBtn = bgObj.AddComponent<Button>();
        bgBtn.onClick.AddListener(Hide);

        // 3. Main Panel (~70% screen)
        panelObj = new GameObject("MainPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = PanelBgColor;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1300, 800);

        // 阻止点击穿透到背景
        panelObj.AddComponent<Button>();

        // 4. Title Bar
        GameObject titleBarObj = new GameObject("TitleBar");
        titleBarObj.transform.SetParent(panelObj.transform, false);
        Image titleBg = titleBarObj.AddComponent<Image>();
        titleBg.color = TopBarColor;
        RectTransform titleBarRect = titleBarObj.GetComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0, 1);
        titleBarRect.anchorMax = new Vector2(1, 1);
        titleBarRect.pivot = new Vector2(0.5f, 1);
        titleBarRect.sizeDelta = new Vector2(0, 80);
        titleBarRect.anchoredPosition = Vector2.zero;

        CreateText("txtTitle", titleBarObj.transform, "兼职与实习", 36, TextTitleColor, TextAlignmentOptions.Center, new Vector2(400, 80), Vector2.zero);

        // Close Button
        GameObject closeBtnObj = new GameObject("btnClose");
        closeBtnObj.transform.SetParent(titleBarObj.transform, false);
        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.3f, 0.3f, 1f);
        RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0.5f);
        closeRect.anchorMax = new Vector2(1, 0.5f);
        closeRect.sizeDelta = new Vector2(60, 60);
        closeRect.anchoredPosition = new Vector2(-40, 0);
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(Hide);
        CreateText("txtX", closeBtnObj.transform, "X", 30, Color.white, TextAlignmentOptions.Center, new Vector2(60, 60), Vector2.zero);

        // 5. Tab Bar
        GameObject tabBarObj = new GameObject("TabBar");
        tabBarObj.transform.SetParent(panelObj.transform, false);
        RectTransform tabBarRect = tabBarObj.AddComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0, 1);
        tabBarRect.anchorMax = new Vector2(1, 1);
        tabBarRect.pivot = new Vector2(0.5f, 1);
        tabBarRect.sizeDelta = new Vector2(0, 60);
        tabBarRect.anchoredPosition = new Vector2(0, -80);
        HorizontalLayoutGroup tabLayout = tabBarObj.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;

        (btnInternship, txtInternship) = CreateTabButton("btnTabIntern", tabBarObj.transform, "实习项目");
        (btnSideHustle, txtSideHustle) = CreateTabButton("btnTabHustle", tabBarObj.transform, "副业兼职");

        btnInternship.onClick.AddListener(() => SwitchTab(true));
        btnSideHustle.onClick.AddListener(() => SwitchTab(false));

        // 6. Lock Message
        GameObject lockMsgObj = new GameObject("LockMessage");
        lockMsgObj.transform.SetParent(panelObj.transform, false);
        RectTransform lockRect = lockMsgObj.AddComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0, 0);
        lockRect.anchorMax = new Vector2(1, 1);
        lockRect.offsetMin = new Vector2(0, 0);
        lockRect.offsetMax = new Vector2(0, -140);
        txtLockMessage = CreateText("txtLock", lockMsgObj.transform, "该功能尚未解锁。\n\n实习要求：大二下学期，通过四级或GPA≥2.5\n副业要求：大二，黑暗值≥15或朋友≥3", 30, TextSubColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
        txtLockMessage.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        txtLockMessage.GetComponent<RectTransform>().anchorMax = Vector2.one;

        // 7. Scroll View
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panelObj.transform, false);
        RectTransform scrollRootRect = scrollObj.AddComponent<RectTransform>();
        scrollRootRect.anchorMin = new Vector2(0, 0);
        scrollRootRect.anchorMax = new Vector2(1, 1);
        scrollRootRect.offsetMin = new Vector2(20, 20); // bottom left
        scrollRootRect.offsetMax = new Vector2(-20, -150); // top right

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0.2f);
        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportObj.AddComponent<RectMask2D>();

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        contentContainer = contentObj.AddComponent<RectTransform>();
        contentContainer.anchorMin = new Vector2(0, 1);
        contentContainer.anchorMax = new Vector2(1, 1);
        contentContainer.pivot = new Vector2(0.5f, 1);
        contentContainer.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 15;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = (RectTransform)contentContainer;
    }

    private (Button, TextMeshProUGUI) CreateTabButton(string name, Transform parent, string text)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        Button btn = obj.AddComponent<Button>();
        TextMeshProUGUI tmp = CreateText("txt", obj.transform, text, 28, TextNormalColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
        tmp.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        tmp.GetComponent<RectTransform>().anchorMax = Vector2.one;
        return (btn, tmp);
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

        if (FontManager.Instance != null)
            tmp.font = FontManager.Instance.ChineseFont;

        return tmp;
    }

    // ================== 逻辑更新 ==================
    public void Show()
    {
        canvasObj.SetActive(true);
        SwitchTab(isShowingInternship); // 刷新当前tab内容
    }

    public void Hide()
    {
        canvasObj.SetActive(false);
    }

    public bool IsOpen => canvasObj != null && canvasObj.activeSelf;

    private void SwitchTab(bool showInternship)
    {
        isShowingInternship = showInternship;

        // 更新Tab外观
        btnInternship.GetComponent<Image>().color = isShowingInternship ? TabActiveColor : TabInactiveColor;
        btnSideHustle.GetComponent<Image>().color = !isShowingInternship ? TabActiveColor : TabInactiveColor;

        bool isUnlocked = false;
        if (JobSystem.Instance != null)
        {
            isUnlocked = isShowingInternship ? JobSystem.Instance.IsInternshipUnlocked() : JobSystem.Instance.IsSideHustleUnlocked();
        }

        if (!isUnlocked)
        {
            txtLockMessage.gameObject.SetActive(true);
            scrollRect.gameObject.SetActive(false);
            if (isShowingInternship)
            {
                txtLockMessage.text = "实习尚未解锁。\n\n要求：大二下学期，并且通过英语四级或GPA≥2.5";
            }
            else
            {
                txtLockMessage.text = "副业尚未解锁。\n\n要求：大二及以上，并且黑暗值≥15或朋友(好感≥40)人数≥3";
            }
        }
        else
        {
            txtLockMessage.gameObject.SetActive(false);
            scrollRect.gameObject.SetActive(true);
            RefreshJobList();
        }
    }

    private void RefreshJobList()
    {
        // 清理旧列表
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var job in allJobs)
        {
            if (job.isInternship == isShowingInternship)
            {
                CreateJobCard(job);
            }
        }
    }

    private void CreateJobCard(JobDef job)
    {
        GameObject cardObj = new GameObject($"Card_{job.id}");
        cardObj.transform.SetParent(contentContainer, false);
        Image bg = cardObj.AddComponent<Image>();
        bg.color = CardBgColor;

        LayoutElement le = cardObj.AddComponent<LayoutElement>();
        le.minHeight = 120;
        le.preferredHeight = 120;

        RectTransform cardRect = cardObj.GetComponent<RectTransform>();

        // 名称
        CreateText("txtName", cardObj.transform, job.name, 28, TextTitleColor, TextAlignmentOptions.Left, new Vector2(300, 40), new Vector2(30, 25))
            .GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

        // 收益
        CreateText("txtIncome", cardObj.transform, $"收益: +{job.income}元", 24, TextIncomeColor, TextAlignmentOptions.Left, new Vector2(200, 30), new Vector2(30, -20))
            .GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

        // 消耗
        CreateText("txtAP", cardObj.transform, $"消耗: {job.apCost} AP", 24, TextWarningColor, TextAlignmentOptions.Left, new Vector2(200, 30), new Vector2(250, -20))
            .GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

        // 需求与效果文字拼接
        string reqText = "要求: ";
        List<string> reqs = new List<string>();
        if (job.reqStudy > 0) reqs.Add($"学力>{job.reqStudy}");
        if (job.reqCharm > 0) reqs.Add($"魅力>{job.reqCharm}");
        if (job.reqLeadership > 0) reqs.Add($"领导力>{job.reqLeadership}");
        if (job.reqDarkness > 0) reqs.Add($"黑暗值>{job.reqDarkness}");
        reqText += reqs.Count > 0 ? string.Join(", ", reqs) : "无";

        string effText = "效果: ";
        List<string> effs = new List<string>();
        if (job.effectStudy != 0) effs.Add($"学力{(job.effectStudy > 0 ? "+" : "")}{job.effectStudy}");
        if (job.effectCharm != 0) effs.Add($"魅力{(job.effectCharm > 0 ? "+" : "")}{job.effectCharm}");
        if (job.effectPhysique != 0) effs.Add($"体魄{(job.effectPhysique > 0 ? "+" : "")}{job.effectPhysique}");
        if (job.effectLeadership != 0) effs.Add($"领导力{(job.effectLeadership > 0 ? "+" : "")}{job.effectLeadership}");
        if (job.effectStress != 0) effs.Add($"压力{(job.effectStress > 0 ? "+" : "")}{job.effectStress}");
        if (job.effectMood != 0) effs.Add($"心情{(job.effectMood > 0 ? "+" : "")}{job.effectMood}");
        if (job.effectDarkness != 0) effs.Add($"黑暗值{(job.effectDarkness > 0 ? "+" : "")}{job.effectDarkness}");
        effText += effs.Count > 0 ? string.Join(", ", effs) : "无";

        CreateText("txtReq", cardObj.transform, reqText, 22, TextSubColor, TextAlignmentOptions.Left, new Vector2(400, 30), new Vector2(400, 20))
            .GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

        CreateText("txtEff", cardObj.transform, effText, 22, TextNormalColor, TextAlignmentOptions.Left, new Vector2(400, 30), new Vector2(400, -20))
            .GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);

        // 判断是否满足要求
        bool canExecute = true;
        string failReason = "";

        if (GameState.Instance != null && GameState.Instance.ActionPoints < job.apCost)
        {
            canExecute = false;
            failReason = "行动点不足";
        }
        else if (PlayerAttributes.Instance != null)
        {
            if (job.reqStudy > 0 && PlayerAttributes.Instance.Study <= job.reqStudy) { canExecute = false; failReason = "学力不足"; }
            if (job.reqCharm > 0 && PlayerAttributes.Instance.Charm <= job.reqCharm) { canExecute = false; failReason = "魅力不足"; }
            if (job.reqLeadership > 0 && PlayerAttributes.Instance.Leadership <= job.reqLeadership) { canExecute = false; failReason = "领导力不足"; }
            if (job.reqDarkness > 0 && PlayerAttributes.Instance.Darkness <= job.reqDarkness) { canExecute = false; failReason = "黑暗值不足"; }
        }

        // 按钮
        GameObject btnObj = new GameObject("btnExecute");
        btnObj.transform.SetParent(cardObj.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = canExecute ? BtnExecColor : BtnDisabledColor;
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0.5f);
        btnRect.anchorMax = new Vector2(1, 0.5f);
        btnRect.pivot = new Vector2(1, 0.5f);
        btnRect.sizeDelta = new Vector2(160, 60);
        btnRect.anchoredPosition = new Vector2(-30, 0);

        Button btn = btnObj.AddComponent<Button>();
        CreateText("txtBtn", btnObj.transform, canExecute ? "执行" : failReason, 24, Color.white, TextAlignmentOptions.Center, btnRect.sizeDelta, Vector2.zero);

        if (canExecute)
        {
            btn.onClick.AddListener(() => ExecuteJob(job));
        }
        else
        {
            btn.interactable = false;
        }
    }

    private void ExecuteJob(JobDef job)
    {
        if (GameState.Instance == null || PlayerAttributes.Instance == null || EconomyManager.Instance == null) return;

        // 扣除AP
        GameState.Instance.ActionPoints -= job.apCost;

        // 发放金钱
        EconomyManager.Instance.Earn(job.income, TransactionRecord.TransactionType.PositionSalary, job.name + "收入");

        // 增加属性
        if (job.effectStudy != 0) PlayerAttributes.Instance.Study += job.effectStudy;
        if (job.effectCharm != 0) PlayerAttributes.Instance.Charm += job.effectCharm;
        if (job.effectPhysique != 0) PlayerAttributes.Instance.Physique += job.effectPhysique;
        if (job.effectLeadership != 0) PlayerAttributes.Instance.Leadership += job.effectLeadership;
        if (job.effectStress != 0) PlayerAttributes.Instance.Stress += job.effectStress;
        if (job.effectMood != 0) PlayerAttributes.Instance.Mood += job.effectMood;
        if (job.effectDarkness != 0) PlayerAttributes.Instance.Darkness += job.effectDarkness;

        // 记录系统
        if (JobSystem.Instance != null)
        {
            if (job.isInternship)
                JobSystem.Instance.RecordInternshipExecution(job.id);
            else
                JobSystem.Instance.RecordSideHustleExecution(job.id);
        }

        // 提示与关闭
        Debug.Log($"[JobSelectionUI] 执行了 {job.name}");
        Hide();
    }

    // ================== 给外部的 API ==================
    /// <summary>
    /// HUDManager 可调用此方法创建一个入口按钮
    /// </summary>
    public static GameObject AddJobButton(Transform parent)
    {
        // 检查解锁学期 (大二下及以上)
        bool isSemesterReached = false;
        if (GameState.Instance != null)
        {
            isSemesterReached = GameState.Instance.CurrentYear > 2 ||
                               (GameState.Instance.CurrentYear == 2 && GameState.Instance.CurrentSemester == 2);
        }

        if (!isSemesterReached) return null;

        GameObject btnObj = new GameObject("btnJob");
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.6f, 1f); // 独特颜色区分

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

        // 文本
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
            tmp.font = FontManager.Instance.ChineseFont;

        return btnObj;
    }
}
