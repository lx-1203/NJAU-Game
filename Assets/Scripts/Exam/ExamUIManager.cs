using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 考试 UI 管理器 —— 控制考试答题界面的交互逻辑、动画和状态流转
/// </summary>
public class ExamUIManager : MonoBehaviour
{
    // ========== 单例 ==========

    public static ExamUIManager Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>所有考试 UI 流程结束后触发</summary>
    public event Action OnExamUICompleted;

    // ========== 状态机 ==========

    private enum ExamUIState
    {
        Idle,               // 空闲
        SubjectTransition,  // 科目过渡
        Answering,          // 答题中
        Feedback,           // 反馈动画中
        CheatCaught,        // 作弊被抓
        Scorecard,          // 成绩单显示
        Done                // 已完成
    }

    private ExamUIState currentState = ExamUIState.Idle;

    // ========== 内部字段 ==========

    private ExamUIBuilder builder;

    // 考试序列数据
    private CourseDefinition[] examCourses;
    private ExamType currentExamType;
    private int currentCourseIndex = 0;

    // 当前科目数据
    private QuestionGroup currentQuestionGroup;
    private int currentQuestionIndex = 0;
    private int correctCountThisCourse = 0;
    private bool cheatedThisCourse = false;

    // 学期考试结果
    private List<ExamResult> semesterResults = new List<ExamResult>();

    // 当前考试的学期信息
    private int examYear;
    private int examSemester;

    // ========== 属性 ==========

    /// <summary>是否正在进行考试</summary>
    public bool IsExamActive => currentState != ExamUIState.Idle && currentState != ExamUIState.Done;

    // ========== 考试序列控制 ==========

    /// <summary>
    /// 启动考试序列（由 ExamSystem 调用）
    /// </summary>
    public void StartExamSequence(CourseDefinition[] courses, ExamType examType)
    {
        if (courses == null || courses.Length == 0)
        {
            Debug.LogWarning("[ExamUIManager] 无课程，跳过考试");
            OnExamUICompleted?.Invoke();
            return;
        }

        Debug.Log($"[ExamUIManager] 开始考试序列，共 {courses.Length} 门课");

        examCourses = courses;
        currentExamType = examType;
        currentCourseIndex = 0;
        semesterResults.Clear();

        // 记录当前学期信息
        if (GameState.Instance != null)
        {
            examYear = GameState.Instance.CurrentYear;
            examSemester = GameState.Instance.CurrentSemester;
        }

        // 确保 UI 已构建
        EnsureUIBuilt();

        // 显示 Canvas
        builder.examCanvas.SetActive(true);

        // 开始第一门课的过渡
        StartCourseTransition();
    }

    // ========== 科目过渡 ==========

    private void StartCourseTransition()
    {
        if (currentCourseIndex >= examCourses.Length)
        {
            // 所有科目考完，显示成绩单
            ShowScorecard();
            return;
        }

        currentState = ExamUIState.SubjectTransition;
        CourseDefinition course = examCourses[currentCourseIndex];

        // 隐藏其他面板
        builder.questionPanel.SetActive(false);
        builder.cheatCaughtPanel.SetActive(false);

        // 显示过渡面板
        builder.transitionPanel.SetActive(true);
        builder.transitionSubjectText.text = course.courseName;

        string examTypeName = GetExamTypeName(currentExamType);
        builder.transitionHintText.text = $"—— {examTypeName} ——\n准备开始答题...";

        // 1 秒后自动进入答题
        StartCoroutine(DelayedAction(1.0f, () =>
        {
            builder.transitionPanel.SetActive(false);
            StartCourseQuestions();
        }));
    }

    // ========== 答题流程 ==========

    private void StartCourseQuestions()
    {
        CourseDefinition course = examCourses[currentCourseIndex];
        currentQuestionIndex = 0;
        correctCountThisCourse = 0;
        cheatedThisCourse = false;

        // 从题库抽取一组题目
        if (ExamSystem.Instance != null)
        {
            currentQuestionGroup = ExamSystem.Instance.GetRandomQuestionGroup(course.subjectTag);
        }
        else
        {
            // Fallback: 创建默认题组
            currentQuestionGroup = CreateFallbackQuestionGroup();
        }

        // 显示第一题
        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        currentState = ExamUIState.Answering;

        CourseDefinition course = examCourses[currentCourseIndex];
        ExamQuestion question = currentQuestionGroup.questions[currentQuestionIndex];
        int totalQuestions = currentQuestionGroup.questions.Length;

        // 更新头部
        string examTypeName = GetExamTypeName(currentExamType);
        builder.headerText.text = $"{course.courseName} · {examTypeName}";
        builder.progressText.text = $"第 {currentQuestionIndex + 1}/{totalQuestions} 题";

        // 更新题干
        builder.questionText.text = question.question;

        // 更新选项
        string[] prefixes = { "A", "B", "C", "D" };
        for (int i = 0; i < 4; i++)
        {
            builder.optionTexts[i].text = $"{prefixes[i]}. {question.options[i]}";
            builder.optionImages[i].color = ExamUIBuilder.OptionNormal;
            builder.optionButtons[i].interactable = true;

            // 重新绑定按钮事件
            int index = i;
            builder.optionButtons[i].onClick.RemoveAllListeners();
            builder.optionButtons[i].onClick.AddListener(() => HandleOptionClick(index));
        }

        // 作弊按钮
        builder.cheatButton.onClick.RemoveAllListeners();
        builder.cheatButton.onClick.AddListener(HandleCheatClick);
        builder.cheatButton.interactable = true;

        // 显示答题面板
        builder.questionPanel.SetActive(true);
    }

    // ========== 选项点击 ==========

    private void HandleOptionClick(int selectedIndex)
    {
        if (currentState != ExamUIState.Answering) return;

        currentState = ExamUIState.Feedback;

        // 禁用所有按钮
        DisableAllButtons();

        ExamQuestion question = currentQuestionGroup.questions[currentQuestionIndex];
        bool isCorrect = (selectedIndex == question.correctIndex);

        if (isCorrect)
        {
            correctCountThisCourse++;
            StartCoroutine(PlayCorrectAnimation(selectedIndex));
        }
        else
        {
            StartCoroutine(PlayWrongAnimation(selectedIndex, question.correctIndex));
        }
    }

    // ========== 作弊点击 ==========

    private void HandleCheatClick()
    {
        if (currentState != ExamUIState.Answering) return;

        cheatedThisCourse = true;

        if (CheatingSystem.Instance == null)
        {
            Debug.LogWarning("[ExamUIManager] CheatingSystem 不存在");
            return;
        }

        CheatResult result = CheatingSystem.Instance.AttemptCheat();

        if (result == CheatResult.Success)
        {
            // 作弊成功：自动选择正确答案
            ExamQuestion question = currentQuestionGroup.questions[currentQuestionIndex];
            correctCountThisCourse++;

            currentState = ExamUIState.Feedback;
            DisableAllButtons();
            StartCoroutine(PlayCorrectAnimation(question.correctIndex));
        }
        else
        {
            // 作弊被抓
            currentState = ExamUIState.CheatCaught;
            DisableAllButtons();

            // 检查是否触发开除
            if (CheatingSystem.Instance.ShouldTriggerExpulsion())
            {
                ShowCheatCaughtAndExpulsion();
            }
            else
            {
                ShowCheatCaught();
            }
        }
    }

    // ========== 作弊被抓显示 ==========

    private void ShowCheatCaught()
    {
        builder.questionPanel.SetActive(false);
        builder.cheatCaughtPanel.SetActive(true);

        builder.cheatCaughtText.text = "被监考老师发现了！";
        builder.cheatPenaltyText.text = "该科目记 0 分\n黑暗值+10  负罪感+15  压力+20";

        // 提交该科目 0 分结果
        SubmitCurrentCourseResult(0, true);

        // 2 秒后进入下一科
        StartCoroutine(DelayedAction(2.0f, () =>
        {
            builder.cheatCaughtPanel.SetActive(false);
            currentCourseIndex++;
            StartCourseTransition();
        }));
    }

    private void ShowCheatCaughtAndExpulsion()
    {
        builder.questionPanel.SetActive(false);
        builder.cheatCaughtPanel.SetActive(true);

        builder.cheatCaughtText.text = "被监考老师发现了！";
        builder.cheatPenaltyText.text = "累计作弊被抓达到 2 次\n\n<color=#FF4444><size=32>学术不端 · 开除学籍</size></color>";

        // 提交该科目 0 分
        SubmitCurrentCourseResult(0, true);

        // 3 秒后触发开除结局
        StartCoroutine(DelayedAction(3.0f, () =>
        {
            builder.cheatCaughtPanel.SetActive(false);
            builder.examCanvas.SetActive(false);
            currentState = ExamUIState.Done;

            // 由 ExamSystem 处理开除结局事件
        }));
    }

    // ========== 答题动画 ==========

    /// <summary>正确答案动画：选项变绿色 + 缩放回弹</summary>
    private IEnumerator PlayCorrectAnimation(int correctIndex)
    {
        Image correctImage = builder.optionImages[correctIndex];
        RectTransform correctRT = correctImage.GetComponent<RectTransform>();

        // 变绿
        correctImage.color = ExamUIBuilder.OptionCorrect;

        // 缩放回弹
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 originalScale = correctRT.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + 0.05f * Mathf.Sin(t * Mathf.PI);
            correctRT.localScale = originalScale * scale;
            yield return null;
        }
        correctRT.localScale = originalScale;

        // 等待剩余时间 (总共 0.8 秒)
        yield return new WaitForSeconds(0.5f);

        // 进入下一题
        AdvanceToNextQuestion();
    }

    /// <summary>错误答案动画：选中项变红 + 抖动，正确项高亮绿</summary>
    private IEnumerator PlayWrongAnimation(int selectedIndex, int correctIndex)
    {
        Image selectedImage = builder.optionImages[selectedIndex];
        Image correctImage = builder.optionImages[correctIndex];
        RectTransform selectedRT = selectedImage.GetComponent<RectTransform>();

        // 选中项变红
        selectedImage.color = ExamUIBuilder.OptionWrong;
        // 正确项变绿
        correctImage.color = ExamUIBuilder.OptionCorrect;

        // 抖动效果 (3 次)
        Vector3 originalPos = selectedRT.localPosition;
        float shakeAmount = 10f;

        for (int i = 0; i < 3; i++)
        {
            selectedRT.localPosition = originalPos + Vector3.right * shakeAmount;
            yield return new WaitForSeconds(0.05f);
            selectedRT.localPosition = originalPos - Vector3.right * shakeAmount;
            yield return new WaitForSeconds(0.05f);
        }
        selectedRT.localPosition = originalPos;

        // 等待剩余时间 (总共 1.2 秒)
        yield return new WaitForSeconds(0.9f);

        // 进入下一题
        AdvanceToNextQuestion();
    }

    // ========== 题目推进 ==========

    private void AdvanceToNextQuestion()
    {
        currentQuestionIndex++;

        if (currentQuestionIndex < currentQuestionGroup.questions.Length)
        {
            // 还有题目
            ShowCurrentQuestion();
        }
        else
        {
            // 本科目答题完成
            FinishCurrentCourse();
        }
    }

    private void FinishCurrentCourse()
    {
        CourseDefinition course = examCourses[currentCourseIndex];
        int totalQuestions = currentQuestionGroup.questions.Length;

        // 计算通过率和分数
        float passRate = 0f;
        if (ExamSystem.Instance != null)
        {
            passRate = ExamSystem.Instance.CalculatePassRate(currentExamType, correctCountThisCourse, totalQuestions);
        }
        else
        {
            passRate = 0.5f + correctCountThisCourse * 0.1f;
        }

        int score = ExamSystem.Instance != null
            ? ExamSystem.Instance.CalculateScore(passRate)
            : Mathf.RoundToInt(passRate * 100f);

        SubmitCurrentCourseResult(score, false);

        // 进入下一科
        currentCourseIndex++;
        StartCourseTransition();
    }

    // ========== 成绩提交 ==========

    private void SubmitCurrentCourseResult(int score, bool cheatCaught)
    {
        CourseDefinition course = examCourses[currentCourseIndex];

        ExamResult result = new ExamResult
        {
            courseId = course.id,
            courseName = course.courseName,
            credits = course.credits,
            score = score,
            gradePoint = GPACalculator.ScoreToGradePoint(score),
            correctCount = correctCountThisCourse,
            cheated = cheatedThisCourse,
            cheatCaught = cheatCaught,
            examType = currentExamType
        };

        semesterResults.Add(result);

        // 通知 ExamSystem
        if (ExamSystem.Instance != null)
        {
            ExamSystem.Instance.SubmitExamResult(result);
        }
    }

    // ========== 成绩单显示 ==========

    private void ShowScorecard()
    {
        currentState = ExamUIState.Scorecard;

        builder.questionPanel.SetActive(false);
        builder.transitionPanel.SetActive(false);
        builder.cheatCaughtPanel.SetActive(false);

        // 清除旧的成绩行
        ClearScorecardRows();

        // 设置标题
        string yearName = GameState.Instance != null ? GameState.Instance.GetYearName() : "大一";
        string semName = GameState.Instance != null ? GameState.Instance.GetSemesterName() : "上";
        string examTypeName = GetExamTypeName(currentExamType);
        builder.scorecardTitle.text = $"{yearName}{semName}学期 · {examTypeName}成绩单";

        // 创建成绩行
        float rowHeight = 1f / Mathf.Max(semesterResults.Count, 1);
        for (int i = 0; i < semesterResults.Count; i++)
        {
            float yTop = 1f - i * rowHeight;
            float yBottom = yTop - rowHeight;
            builder.CreateScoreRow(builder.scorecardContent, semesterResults[i], i, yTop, yBottom);
        }

        // 计算 GPA
        float semGPA = GPACalculator.CalcSemesterGPA(semesterResults);
        float cumGPA = ExamSystem.Instance != null ? ExamSystem.Instance.GetCumulativeGPA() : semGPA;

        builder.semesterGPAText.text = $"学期 GPA: {semGPA:F2}";
        builder.cumulativeGPAText.text = $"累积 GPA: {cumGPA:F2}";

        // 绑定确认按钮
        builder.confirmButton.onClick.RemoveAllListeners();
        builder.confirmButton.onClick.AddListener(HandleConfirmClick);

        builder.scorecardPanel.SetActive(true);
    }

    private void ClearScorecardRows()
    {
        if (builder.scorecardContent == null) return;

        for (int i = builder.scorecardContent.childCount - 1; i >= 0; i--)
        {
            Destroy(builder.scorecardContent.GetChild(i).gameObject);
        }
    }

    // ========== 确认按钮 ==========

    private void HandleConfirmClick()
    {
        Debug.Log("[ExamUIManager] 考试流程结束，关闭考试 UI");

        builder.scorecardPanel.SetActive(false);
        builder.examCanvas.SetActive(false);
        currentState = ExamUIState.Done;

        // 通知 ExamSystem 完成结算
        if (ExamSystem.Instance != null)
        {
            ExamSystem.Instance.FinalizeSemesterExam(examYear, examSemester);
        }

        OnExamUICompleted?.Invoke();
    }

    // ========== 辅助方法 ==========

    private void EnsureUIBuilt()
    {
        if (builder == null)
        {
            builder = gameObject.GetComponent<ExamUIBuilder>();
            if (builder == null)
            {
                builder = gameObject.AddComponent<ExamUIBuilder>();
            }
        }

        if (builder.examCanvas == null)
        {
            builder.BuildExamUI();
        }
    }

    private void DisableAllButtons()
    {
        for (int i = 0; i < builder.optionButtons.Length; i++)
        {
            builder.optionButtons[i].interactable = false;
        }
        builder.cheatButton.interactable = false;
    }

    private IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    private string GetExamTypeName(ExamType type)
    {
        switch (type)
        {
            case ExamType.Final:         return "期末考试";
            case ExamType.Midterm:       return "期中考试";
            case ExamType.CET4:          return "四级考试";
            case ExamType.CET6:          return "六级考试";
            case ExamType.ComputerLevel: return "计算机等级考试";
            case ExamType.Makeup:        return "补考";
            default:                     return "考试";
        }
    }

    private QuestionGroup CreateFallbackQuestionGroup()
    {
        return new QuestionGroup
        {
            subjectTag = "general",
            questions = new ExamQuestion[]
            {
                new ExamQuestion { question = "1 + 1 = ?", options = new[] { "1", "2", "3", "4" }, correctIndex = 1, subjectTag = "general" },
                new ExamQuestion { question = "2 × 5 = ?", options = new[] { "8", "9", "10", "12" }, correctIndex = 2, subjectTag = "general" },
                new ExamQuestion { question = "9 - 3 = ?", options = new[] { "5", "6", "7", "8" }, correctIndex = 1, subjectTag = "general" }
            }
        };
    }

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
