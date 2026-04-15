using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 考试 UI 构建器 —— 纯代码动态创建考试答题界面和成绩单界面
/// </summary>
public class ExamUIBuilder : MonoBehaviour
{
    // ========== 颜色方案 ==========

    public static readonly Color ExamPanelBg    = new Color(0.08f, 0.08f, 0.12f, 0.98f);
    public static readonly Color ExamHeaderBg   = new Color(0.12f, 0.12f, 0.18f, 1.0f);
    public static readonly Color OptionNormal   = new Color(0.15f, 0.15f, 0.22f, 1.0f);
    public static readonly Color OptionCorrect  = new Color(0.20f, 0.80f, 0.30f, 1.0f);
    public static readonly Color OptionWrong    = new Color(0.90f, 0.20f, 0.20f, 1.0f);
    public static readonly Color OptionHover    = new Color(0.20f, 0.20f, 0.30f, 1.0f);
    public static readonly Color TextWhite      = Color.white;
    public static readonly Color TextGold       = new Color(1.0f, 0.85f, 0.3f, 1.0f);
    public static readonly Color TextFail       = new Color(1.0f, 0.3f, 0.3f, 1.0f);
    public static readonly Color CheatBtnColor  = new Color(0.4f, 0.2f, 0.5f, 1.0f);
    public static readonly Color ConfirmBtnColor = new Color(0.20f, 0.50f, 0.85f, 1.0f);
    public static readonly Color ScorecardRowAlt = new Color(0.10f, 0.10f, 0.15f, 1.0f);
    public static readonly Color DividerColor   = new Color(0.3f, 0.3f, 0.4f, 0.5f);

    // ========== 公共引用 ==========

    /// <summary>考试画布根对象</summary>
    public GameObject examCanvas;

    /// <summary>答题面板</summary>
    public GameObject questionPanel;

    /// <summary>成绩单面板</summary>
    public GameObject scorecardPanel;

    /// <summary>科目过渡面板</summary>
    public GameObject transitionPanel;

    /// <summary>作弊被抓面板</summary>
    public GameObject cheatCaughtPanel;

    // ========== 答题面板引用 ==========
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public Image[] optionImages;
    public Button cheatButton;

    // ========== 成绩单面板引用 ==========
    public TextMeshProUGUI scorecardTitle;
    public Transform scorecardContent;
    public TextMeshProUGUI semesterGPAText;
    public TextMeshProUGUI cumulativeGPAText;
    public Button confirmButton;

    // ========== 过渡面板引用 ==========
    public TextMeshProUGUI transitionSubjectText;
    public TextMeshProUGUI transitionHintText;

    // ========== 作弊被抓面板引用 ==========
    public TextMeshProUGUI cheatCaughtText;
    public TextMeshProUGUI cheatPenaltyText;

    // ========== 构建入口 ==========

    /// <summary>
    /// 构建整个考试 UI（所有面板默认隐藏）
    /// </summary>
    public void BuildExamUI()
    {
        CreateExamCanvas();
        BuildQuestionPanel();
        BuildScorecardPanel();
        BuildTransitionPanel();
        BuildCheatCaughtPanel();

        // 所有面板默认隐藏
        questionPanel.SetActive(false);
        scorecardPanel.SetActive(false);
        transitionPanel.SetActive(false);
        cheatCaughtPanel.SetActive(false);

        Debug.Log("[ExamUIBuilder] 考试 UI 构建完成");
    }

    // ========== Canvas 创建 ==========

    private void CreateExamCanvas()
    {
        examCanvas = new GameObject("ExamCanvas");
        examCanvas.transform.SetParent(transform, false);

        Canvas canvas = examCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // 高于 HUD(100) 和对话框(100)

        CanvasScaler scaler = examCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        examCanvas.AddComponent<GraphicRaycaster>();
    }

    // ========== 答题面板 ==========

    private void BuildQuestionPanel()
    {
        // 主面板 - 全屏半透明背景
        questionPanel = CreatePanel("QuestionPanel", examCanvas.transform,
            Vector2.zero, Vector2.one, ExamPanelBg);

        // ===== 内容区域 (居中 70% 宽度) =====
        GameObject contentArea = CreatePanel("ContentArea", questionPanel.transform,
            new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.95f), new Color(0, 0, 0, 0));

        // ===== 头部区域 =====
        GameObject headerArea = CreatePanel("HeaderArea", contentArea.transform,
            new Vector2(0f, 0.88f), new Vector2(1f, 1f), ExamHeaderBg);

        // 科目名
        headerText = CreateTMPText("HeaderText", headerArea.transform,
            new Vector2(0.02f, 0f), new Vector2(0.7f, 1f), 28, TextWhite, TextAlignmentOptions.MidlineLeft);
        headerText.text = "高等数学I · 期末考试";

        // 进度
        progressText = CreateTMPText("ProgressText", headerArea.transform,
            new Vector2(0.7f, 0f), new Vector2(0.98f, 1f), 24, TextGold, TextAlignmentOptions.MidlineRight);
        progressText.text = "第 1/3 题";

        // ===== 题干区域 =====
        GameObject questionArea = CreatePanel("QuestionArea", contentArea.transform,
            new Vector2(0f, 0.55f), new Vector2(1f, 0.85f), new Color(0, 0, 0, 0));

        questionText = CreateTMPText("QuestionText", questionArea.transform,
            new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), 30, TextWhite, TextAlignmentOptions.Center);
        questionText.text = "题目内容";
        questionText.enableWordWrapping = true;

        // ===== 选项区域 =====
        optionButtons = new Button[4];
        optionTexts = new TextMeshProUGUI[4];
        optionImages = new Image[4];

        string[] prefixes = { "A", "B", "C", "D" };
        float optionHeight = 0.10f;
        float optionGap = 0.02f;
        float optionStartY = 0.50f;

        for (int i = 0; i < 4; i++)
        {
            float top = optionStartY - i * (optionHeight + optionGap);
            float bottom = top - optionHeight;

            GameObject optionObj = CreatePanel($"Option_{prefixes[i]}", contentArea.transform,
                new Vector2(0.05f, bottom), new Vector2(0.95f, top), OptionNormal);

            // 添加按钮组件
            Button btn = optionObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.3f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            btn.colors = colors;

            // 选项文本
            TextMeshProUGUI optText = CreateTMPText($"OptionText_{prefixes[i]}", optionObj.transform,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), 24, TextWhite, TextAlignmentOptions.MidlineLeft);
            optText.text = $"{prefixes[i]}. 选项内容";

            optionButtons[i] = btn;
            optionTexts[i] = optText;
            optionImages[i] = optionObj.GetComponent<Image>();
        }

        // ===== 作弊按钮 =====
        float cheatBtnBottom = optionStartY - 4 * (optionHeight + optionGap) - 0.02f;
        GameObject cheatObj = CreatePanel("CheatButton", contentArea.transform,
            new Vector2(0.30f, cheatBtnBottom - 0.06f), new Vector2(0.70f, cheatBtnBottom), CheatBtnColor);

        cheatButton = cheatObj.AddComponent<Button>();
        ColorBlock cheatColors = cheatButton.colors;
        cheatColors.normalColor = Color.white;
        cheatColors.highlightedColor = new Color(1.1f, 1.0f, 1.2f, 1f);
        cheatButton.colors = cheatColors;

        TextMeshProUGUI cheatText = CreateTMPText("CheatText", cheatObj.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), 22, TextWhite, TextAlignmentOptions.Center);
        cheatText.text = "\U0001f440 偷看旁边的";
    }

    // ========== 成绩单面板 ==========

    private void BuildScorecardPanel()
    {
        // 全屏背景
        scorecardPanel = CreatePanel("ScorecardPanel", examCanvas.transform,
            Vector2.zero, Vector2.one, ExamPanelBg);

        // 内容区域
        GameObject contentArea = CreatePanel("ScorecardContent", scorecardPanel.transform,
            new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.95f), new Color(0, 0, 0, 0));

        // 标题
        scorecardTitle = CreateTMPText("ScorecardTitle", contentArea.transform,
            new Vector2(0f, 0.90f), new Vector2(1f, 1f), 36, TextGold, TextAlignmentOptions.Center);
        scorecardTitle.text = "成绩单";

        // 表头
        GameObject headerRow = CreatePanel("HeaderRow", contentArea.transform,
            new Vector2(0f, 0.83f), new Vector2(1f, 0.89f), ExamHeaderBg);

        CreateTMPText("H_Name", headerRow.transform,
            new Vector2(0.02f, 0f), new Vector2(0.40f, 1f), 22, TextGold, TextAlignmentOptions.MidlineLeft).text = "课程名称";
        CreateTMPText("H_Credits", headerRow.transform,
            new Vector2(0.40f, 0f), new Vector2(0.55f, 1f), 22, TextGold, TextAlignmentOptions.Center).text = "学分";
        CreateTMPText("H_Score", headerRow.transform,
            new Vector2(0.55f, 0f), new Vector2(0.75f, 1f), 22, TextGold, TextAlignmentOptions.Center).text = "分数";
        CreateTMPText("H_GPA", headerRow.transform,
            new Vector2(0.75f, 0f), new Vector2(1f, 1f), 22, TextGold, TextAlignmentOptions.Center).text = "绩点";

        // 成绩行滚动区域（预留空间，实际成绩行动态创建）
        scorecardContent = new GameObject("ScoreRows", typeof(RectTransform)).transform;
        scorecardContent.SetParent(contentArea.transform, false);
        RectTransform rowsRT = scorecardContent.GetComponent<RectTransform>();
        rowsRT.anchorMin = new Vector2(0f, 0.20f);
        rowsRT.anchorMax = new Vector2(1f, 0.82f);
        rowsRT.offsetMin = Vector2.zero;
        rowsRT.offsetMax = Vector2.zero;

        // 分隔线
        CreatePanel("Divider", contentArea.transform,
            new Vector2(0f, 0.18f), new Vector2(1f, 0.19f), DividerColor);

        // 学期 GPA
        semesterGPAText = CreateTMPText("SemesterGPA", contentArea.transform,
            new Vector2(0f, 0.11f), new Vector2(1f, 0.17f), 28, TextWhite, TextAlignmentOptions.Center);
        semesterGPAText.text = "学期 GPA: 0.00";

        // 累积 GPA
        cumulativeGPAText = CreateTMPText("CumulativeGPA", contentArea.transform,
            new Vector2(0f, 0.05f), new Vector2(1f, 0.11f), 28, TextGold, TextAlignmentOptions.Center);
        cumulativeGPAText.text = "累积 GPA: 0.00";

        // 确认按钮
        GameObject confirmObj = CreatePanel("ConfirmBtn", contentArea.transform,
            new Vector2(0.35f, 0.00f), new Vector2(0.65f, 0.05f), ConfirmBtnColor);

        confirmButton = confirmObj.AddComponent<Button>();

        TextMeshProUGUI confirmText = CreateTMPText("ConfirmText", confirmObj.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), 24, TextWhite, TextAlignmentOptions.Center);
        confirmText.text = "确认";
    }

    /// <summary>
    /// 动态创建一行成绩记录
    /// </summary>
    public GameObject CreateScoreRow(Transform parent, ExamResult result, int index, float yTop, float yBottom)
    {
        Color rowBg = (index % 2 == 0) ? new Color(0, 0, 0, 0) : ScorecardRowAlt;
        GameObject row = CreatePanel($"ScoreRow_{index}", parent,
            new Vector2(0f, yBottom), new Vector2(1f, yTop), rowBg);

        // 课程名
        CreateTMPText($"R_Name_{index}", row.transform,
            new Vector2(0.02f, 0f), new Vector2(0.40f, 1f), 20, TextWhite, TextAlignmentOptions.MidlineLeft).text = result.courseName;

        // 学分
        CreateTMPText($"R_Credits_{index}", row.transform,
            new Vector2(0.40f, 0f), new Vector2(0.55f, 1f), 20, TextWhite, TextAlignmentOptions.Center).text = result.credits.ToString();

        // 分数（挂科红色）
        Color scoreColor = result.score < 60 ? TextFail : TextWhite;
        CreateTMPText($"R_Score_{index}", row.transform,
            new Vector2(0.55f, 0f), new Vector2(0.75f, 1f), 20, scoreColor, TextAlignmentOptions.Center).text = result.score.ToString();

        // 绩点（挂科红色 + ❌）
        string gpText = result.gradePoint > 0 ? result.gradePoint.ToString("F1") : "0 \u274C";
        Color gpColor = result.gradePoint > 0 ? TextGold : TextFail;
        CreateTMPText($"R_GPA_{index}", row.transform,
            new Vector2(0.75f, 0f), new Vector2(1f, 1f), 20, gpColor, TextAlignmentOptions.Center).text = gpText;

        return row;
    }

    // ========== 过渡面板 ==========

    private void BuildTransitionPanel()
    {
        transitionPanel = CreatePanel("TransitionPanel", examCanvas.transform,
            Vector2.zero, Vector2.one, ExamPanelBg);

        transitionSubjectText = CreateTMPText("TransitionSubject", transitionPanel.transform,
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.65f), 42, TextGold, TextAlignmentOptions.Center);
        transitionSubjectText.text = "高等数学I";

        transitionHintText = CreateTMPText("TransitionHint", transitionPanel.transform,
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.45f), 24, TextWhite, TextAlignmentOptions.Center);
        transitionHintText.text = "准备开始答题...";
    }

    // ========== 作弊被抓面板 ==========

    private void BuildCheatCaughtPanel()
    {
        cheatCaughtPanel = CreatePanel("CheatCaughtPanel", examCanvas.transform,
            Vector2.zero, Vector2.one, new Color(0.15f, 0.02f, 0.02f, 0.95f));

        cheatCaughtText = CreateTMPText("CheatCaughtText", cheatCaughtPanel.transform,
            new Vector2(0.1f, 0.50f), new Vector2(0.9f, 0.70f), 38, TextFail, TextAlignmentOptions.Center);
        cheatCaughtText.text = "被监考老师发现了！";

        cheatPenaltyText = CreateTMPText("CheatPenaltyText", cheatCaughtPanel.transform,
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.50f), 24, TextWhite, TextAlignmentOptions.Center);
        cheatPenaltyText.text = "该科目记 0 分\n黑暗值+10  负罪感+15  压力+20";
    }

    // ========== 工具方法 ==========

    /// <summary>
    /// 创建带 Image 的面板
    /// </summary>
    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = bgColor;

        return panel;
    }

    /// <summary>
    /// 创建 TextMeshProUGUI 文本
    /// </summary>
    private TextMeshProUGUI CreateTMPText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        // 使用 FontManager 的中文字体
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            tmp.font = FontManager.Instance.ChineseFont;
        }

        return tmp;
    }
}
