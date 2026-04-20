using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 考试系统 —— 管理题库加载、考试流程控制、通过率计算、成绩记录
/// 实现 IExamResultProvider 接口供结局系统和天赋系统使用
/// </summary>
public class ExamSystem : MonoBehaviour, IExamResultProvider, ISaveable
{
    // ========== 单例 ==========

    public static ExamSystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>单科考试完成后触发</summary>
    public event Action<ExamResult> OnSingleExamFinished;

    /// <summary>整个学期考试全部完成后触发</summary>
    public event Action<SemesterGPA> OnExamCompleted;

    /// <summary>触发开除结局时触发</summary>
    public event Action<string> OnExpulsionTriggered;

    // ========== 内部字段 ==========

    [Header("数据")]
    private QuestionBankData questionBankData;
    private CourseScheduleData courseScheduleData;
    private bool dataLoaded = false;

    [Header("成绩记录")]
    private List<SemesterGPA> semesterGPAHistory = new List<SemesterGPA>();
    private List<ExamResult> currentSemesterResults = new List<ExamResult>();
    private List<ExamResult> failedCourses = new List<ExamResult>();

    [Header("自习追踪")]
    private int studyCountThisSemester = 0;

    [Header("证书考试状态")]
    private bool cet4Passed = false;
    private bool cet6Passed = false;
    private bool computerLevelPassed = false;

    [Header("期中考试结果")]
    private List<ExamResult> lastMidtermResults = new List<ExamResult>();

    // ========== 题库索引 ==========

    /// <summary>按科目标签索引的题组 Dictionary</summary>
    private Dictionary<string, List<QuestionGroup>> questionsBySubject = new Dictionary<string, List<QuestionGroup>>();

    // ========== 属性访问 ==========

    /// <summary>数据是否已加载</summary>
    public bool IsDataLoaded => dataLoaded;

    /// <summary>本学期自习次数</summary>
    public int StudyCountThisSemester => studyCountThisSemester;

    /// <summary>CET4 是否已通过</summary>
    public bool IsCET4Passed => cet4Passed;

    /// <summary>CET6 是否已通过</summary>
    public bool IsCET6Passed => cet6Passed;

    /// <summary>计算机等级考试是否已通过</summary>
    public bool IsComputerLevelPassed => computerLevelPassed;

    // ========== 数据加载 ==========

    /// <summary>
    /// 从 Resources 加载题库和课程表 JSON 数据
    /// </summary>
    public void LoadExamData()
    {
        // 加载课程表
        TextAsset courseJson = Resources.Load<TextAsset>("ExamData/course_schedule");
        if (courseJson != null)
        {
            courseScheduleData = JsonUtility.FromJson<CourseScheduleData>(courseJson.text);
            Debug.Log($"[ExamSystem] 课程表加载成功，共 {courseScheduleData.courses.Count} 门课程");
        }
        else
        {
            Debug.LogError("[ExamSystem] 无法加载课程表: Resources/ExamData/course_schedule.json");
            courseScheduleData = new CourseScheduleData { courses = new List<CourseDefinition>() };
        }

        // 加载题库
        TextAsset questionJson = Resources.Load<TextAsset>("ExamData/question_bank");
        if (questionJson != null)
        {
            questionBankData = JsonUtility.FromJson<QuestionBankData>(questionJson.text);
            Debug.Log($"[ExamSystem] 题库加载成功，共 {questionBankData.questionGroups.Count} 组题目");

            // 建立科目索引
            BuildSubjectIndex();
        }
        else
        {
            Debug.LogError("[ExamSystem] 无法加载题库: Resources/ExamData/question_bank.json");
            questionBankData = new QuestionBankData { questionGroups = new List<QuestionGroup>() };
        }

        dataLoaded = true;
    }

    /// <summary>
    /// 按科目标签建立题组索引
    /// </summary>
    private void BuildSubjectIndex()
    {
        questionsBySubject.Clear();

        for (int i = 0; i < questionBankData.questionGroups.Count; i++)
        {
            QuestionGroup group = questionBankData.questionGroups[i];
            string tag = group.subjectTag;

            if (!questionsBySubject.ContainsKey(tag))
            {
                questionsBySubject[tag] = new List<QuestionGroup>();
            }
            questionsBySubject[tag].Add(group);
        }

        Debug.Log($"[ExamSystem] 题组索引建立完成，覆盖 {questionsBySubject.Count} 个科目");
    }

    // ========== 课程查询 ==========

    /// <summary>
    /// 获取指定学期的课程列表
    /// </summary>
    public CourseDefinition[] GetCoursesForSemester(int year, int semester)
    {
        if (courseScheduleData == null || courseScheduleData.courses == null)
            return new CourseDefinition[0];

        List<CourseDefinition> result = new List<CourseDefinition>();
        for (int i = 0; i < courseScheduleData.courses.Count; i++)
        {
            CourseDefinition c = courseScheduleData.courses[i];
            if (c.year == year && c.semester == semester)
            {
                result.Add(c);
            }
        }
        return result.ToArray();
    }

    // ========== 题目抽取 ==========

    /// <summary>
    /// 从指定科目的题组中随机抽取一组 (3题)
    /// </summary>
    public QuestionGroup GetRandomQuestionGroup(string subjectTag)
    {
        if (!questionsBySubject.ContainsKey(subjectTag) || questionsBySubject[subjectTag].Count == 0)
        {
            Debug.LogWarning($"[ExamSystem] 科目 '{subjectTag}' 无可用题组，使用默认题目");
            return CreateDefaultQuestionGroup(subjectTag);
        }

        List<QuestionGroup> groups = questionsBySubject[subjectTag];
        int randomIndex = UnityEngine.Random.Range(0, groups.Count);
        return groups[randomIndex];
    }

    /// <summary>
    /// 当找不到题库时创建默认题组
    /// </summary>
    private QuestionGroup CreateDefaultQuestionGroup(string subjectTag)
    {
        QuestionGroup defaultGroup = new QuestionGroup
        {
            subjectTag = subjectTag,
            questions = new ExamQuestion[]
            {
                new ExamQuestion
                {
                    question = "1 + 1 = ?",
                    options = new string[] { "1", "2", "3", "4" },
                    correctIndex = 1,
                    subjectTag = subjectTag
                },
                new ExamQuestion
                {
                    question = "2 × 3 = ?",
                    options = new string[] { "5", "6", "7", "8" },
                    correctIndex = 1,
                    subjectTag = subjectTag
                },
                new ExamQuestion
                {
                    question = "10 - 4 = ?",
                    options = new string[] { "4", "5", "6", "7" },
                    correctIndex = 2,
                    subjectTag = subjectTag
                }
            }
        };
        return defaultGroup;
    }

    // ========== 通过率计算 ==========

    /// <summary>
    /// 计算考试通过率（综合基础、答题、属性修正）
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="correctCount">答对题数 (0-3)</param>
    /// <param name="totalQuestions">总题数 (3)</param>
    /// <returns>通过率 0.05 ~ 0.99</returns>
    public float CalculatePassRate(ExamType examType, int correctCount, int totalQuestions)
    {
        // 1. 基础通过率（根据考试类型）
        float baseRate = GetBaseRate(examType);

        // 2. 自习修正: +10% per study action (封顶3次 = +30%)
        int cappedStudyCount = Mathf.Min(studyCountThisSemester, 3);
        float studyBonus = cappedStudyCount * 0.10f;
        baseRate += studyBonus;

        // 3. 答题修正
        float answerModifier = 0f;
        for (int i = 0; i < totalQuestions; i++)
        {
            if (i < correctCount)
                answerModifier += 0.05f;  // 答对 +5%
            else
                answerModifier -= 0.10f;  // 答错 -10%
        }
        // 全对额外加成
        if (correctCount == totalQuestions)
            answerModifier += 0.05f;

        // 4. 属性修正
        float attrModifier = 0f;
        if (PlayerAttributes.Instance != null)
        {
            attrModifier -= PlayerAttributes.Instance.Stress * 0.001f;   // -压力×0.1%
            attrModifier += PlayerAttributes.Instance.Luck * 0.001f;     // +幸运×0.1%
            attrModifier -= PlayerAttributes.Instance.Guilt * 0.0008f;   // -负罪感×0.08%
        }

        // 5. 综合
        float finalRate = baseRate + answerModifier + attrModifier;

        // 6. Clamp
        finalRate = Mathf.Clamp(finalRate, 0.05f, 0.99f);

        Debug.Log($"[ExamSystem] 通过率计算: 基础={baseRate:P0} 自习={studyBonus:P0} " +
                  $"答题={answerModifier:P0} 属性={attrModifier:P0} 最终={finalRate:P0}");

        return finalRate;
    }

    /// <summary>
    /// 根据考试类型获取基础通过率
    /// </summary>
    private float GetBaseRate(ExamType examType)
    {
        switch (examType)
        {
            case ExamType.Final:         return 0.50f; // 期末 50%
            case ExamType.Midterm:       return 0.50f; // 期中 50%
            case ExamType.CET4:          return 0.30f; // 四级 30%
            case ExamType.CET6:          return 0.20f; // 六级 20%
            case ExamType.ComputerLevel: return 0.40f; // 计算机等级 40%
            case ExamType.Makeup:        return 0.60f; // 补考 60%
            default:                     return 0.50f;
        }
    }

    /// <summary>
    /// 根据通过率计算百分制分数
    /// 通过率决定分数的期望值，加入随机波动
    /// </summary>
    public int CalculateScore(float passRate)
    {
        // 基于通过率生成分数
        // passRate 越高，分数期望越高
        float baseScore = passRate * 100f;

        // 添加 ±10 的随机波动
        float randomOffset = UnityEngine.Random.Range(-10f, 10f);
        int finalScore = Mathf.RoundToInt(baseScore + randomOffset);

        // Clamp 到 0-100
        finalScore = Mathf.Clamp(finalScore, 0, 100);

        return finalScore;
    }

    // ========== 考试流程控制 ==========

    /// <summary>
    /// 开始学期考试（由 TurnManager 在学期结束时调用）
    /// </summary>
    public void StartSemesterExam(int year, int semester)
    {
        Debug.Log($"[ExamSystem] 开始考试 —— {GetYearName(year)}{GetSemesterName(semester)}学期");

        currentSemesterResults.Clear();

        CourseDefinition[] courses = GetCoursesForSemester(year, semester);

        if (courses.Length == 0)
        {
            Debug.LogWarning($"[ExamSystem] 该学期无课程，跳过考试");
            FinalizeSemesterExam(year, semester);
            return;
        }

        // 通知 UI 开始考试序列
        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.StartExamSequence(courses, ExamType.Final);
        }
        else
        {
            Debug.LogWarning("[ExamSystem] ExamUIManager 不存在，自动生成考试结果");
            AutoGenerateResults(courses, ExamType.Final);
            FinalizeSemesterExam(year, semester);
        }
    }

    // ========== 期中考试 ==========

    /// <summary>
    /// 开始期中考试 —— 取当前学期一半课程，成绩不纳入学期GPA
    /// </summary>
    public void StartMidtermExam(int year, int semester)
    {
        Debug.Log($"[ExamSystem] 开始期中考试 —— {GetYearName(year)}{GetSemesterName(semester)}学期");

        currentSemesterResults.Clear();
        lastMidtermResults.Clear();

        CourseDefinition[] allCourses = GetCoursesForSemester(year, semester);

        if (allCourses.Length == 0)
        {
            Debug.LogWarning($"[ExamSystem] 该学期无课程，跳过期中考试");
            FinalizeMidtermExam();
            return;
        }

        // 取前 ceil(N/2) 门课程进行期中考试
        int midtermCount = Mathf.CeilToInt(allCourses.Length / 2f);
        CourseDefinition[] midtermCourses = new CourseDefinition[midtermCount];
        for (int i = 0; i < midtermCount; i++)
        {
            midtermCourses[i] = allCourses[i];
        }

        Debug.Log($"[ExamSystem] 期中考试选取 {midtermCount}/{allCourses.Length} 门课程");

        // 通知 UI 开始考试序列
        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.StartExamSequence(midtermCourses, ExamType.Midterm);
        }
        else
        {
            Debug.LogWarning("[ExamSystem] ExamUIManager 不存在，自动生成期中考试结果");
            AutoGenerateResults(midtermCourses, ExamType.Midterm);
            FinalizeMidtermExam();
        }
    }

    /// <summary>
    /// 期中考试结算 —— 独立记录，不纳入 semesterGPAHistory
    /// </summary>
    public void FinalizeMidtermExam()
    {
        lastMidtermResults.Clear();
        lastMidtermResults.AddRange(currentSemesterResults);

        int passCount = 0;
        int failCount = 0;
        for (int i = 0; i < currentSemesterResults.Count; i++)
        {
            if (currentSemesterResults[i].score >= 60)
                passCount++;
            else
                failCount++;
        }

        Debug.Log($"[ExamSystem] 期中考试结算完成: 通过={passCount}门 不及格={failCount}门 (不影响GPA)");

        // 注意：不调用 OnExamCompleted，不重置 studyCountThisSemester
        // 期中成绩独立记录，不影响学期 GPA 和自习计数
    }

    // ========== 证书考试 ==========

    /// <summary>
    /// 开始 CET4 (大学英语四级) 考试
    /// 可在大一下学期后触发，基础通过率 30%
    /// </summary>
    public void StartCET4Exam()
    {
        if (cet4Passed)
        {
            Debug.Log("[ExamSystem] CET4 已通过，无需再考");
            return;
        }

        Debug.Log("[ExamSystem] 开始 CET4 考试");
        StartCertificateExam("CERT_CET4", "大学英语四级", "english", ExamType.CET4);
    }

    /// <summary>
    /// 开始 CET6 (大学英语六级) 考试
    /// 需要 CET4 已通过，基础通过率 20%
    /// </summary>
    public void StartCET6Exam()
    {
        if (!cet4Passed)
        {
            Debug.LogWarning("[ExamSystem] CET6 需要先通过 CET4");
            return;
        }

        if (cet6Passed)
        {
            Debug.Log("[ExamSystem] CET6 已通过，无需再考");
            return;
        }

        Debug.Log("[ExamSystem] 开始 CET6 考试");
        StartCertificateExam("CERT_CET6", "大学英语六级", "english", ExamType.CET6);
    }

    /// <summary>
    /// 开始计算机等级考试
    /// 基础通过率 40%
    /// </summary>
    public void StartComputerLevelExam()
    {
        if (computerLevelPassed)
        {
            Debug.Log("[ExamSystem] 计算机等级考试已通过，无需再考");
            return;
        }

        Debug.Log("[ExamSystem] 开始计算机等级考试");
        StartCertificateExam("CERT_COMPUTER", "计算机等级考试", "computer", ExamType.ComputerLevel);
    }

    /// <summary>
    /// 通用证书考试启动方法 —— 构造虚拟课程定义，启动考试 UI
    /// </summary>
    private void StartCertificateExam(string courseId, string courseName, string subjectTag, ExamType examType)
    {
        currentSemesterResults.Clear();

        CourseDefinition certCourse = new CourseDefinition
        {
            id = courseId,
            courseName = courseName,
            credits = 0, // 证书考试不计学分
            subjectTag = subjectTag,
            year = GameState.Instance != null ? GameState.Instance.CurrentYear : 1,
            semester = GameState.Instance != null ? GameState.Instance.CurrentSemester : 1
        };

        CourseDefinition[] courses = new CourseDefinition[] { certCourse };

        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.StartExamSequence(courses, examType);
        }
        else
        {
            Debug.LogWarning("[ExamSystem] ExamUIManager 不存在，自动生成证书考试结果");
            AutoGenerateResults(courses, examType);
            FinalizeCertificateExam(examType);
        }
    }

    /// <summary>
    /// 证书考试结算 —— 更新通过状态，不纳入 semesterGPAHistory
    /// </summary>
    public void FinalizeCertificateExam(ExamType examType)
    {
        if (currentSemesterResults.Count > 0)
        {
            ExamResult result = currentSemesterResults[0];
            bool passed = result.score >= 60;

            switch (examType)
            {
                case ExamType.CET4:
                    if (passed) cet4Passed = true;
                    Debug.Log($"[ExamSystem] CET4 {(passed ? "通过" : "未通过")} 分数={result.score}");
                    break;
                case ExamType.CET6:
                    if (passed) cet6Passed = true;
                    Debug.Log($"[ExamSystem] CET6 {(passed ? "通过" : "未通过")} 分数={result.score}");
                    break;
                case ExamType.ComputerLevel:
                    if (passed) computerLevelPassed = true;
                    Debug.Log($"[ExamSystem] 计算机等级考试 {(passed ? "通过" : "未通过")} 分数={result.score}");
                    break;
            }
        }

        // 注意：证书考试不纳入 semesterGPAHistory，不重置 studyCountThisSemester
    }

    /// <summary>
    /// 开始补考（挂科课程）
    /// </summary>
    public void StartMakeupExam(List<ExamResult> failedResults)
    {
        Debug.Log($"[ExamSystem] 开始补考，共 {failedResults.Count} 门");

        currentSemesterResults.Clear();

        // 将挂科课程转为 CourseDefinition
        List<CourseDefinition> makeupCourses = new List<CourseDefinition>();
        for (int i = 0; i < failedResults.Count; i++)
        {
            ExamResult fr = failedResults[i];
            // 查找原课程定义
            CourseDefinition cd = FindCourseById(fr.courseId);
            if (cd != null)
            {
                makeupCourses.Add(cd);
            }
        }

        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.StartExamSequence(makeupCourses.ToArray(), ExamType.Makeup);
        }
        else
        {
            AutoGenerateResults(makeupCourses.ToArray(), ExamType.Makeup);
            // 补考结果直接更新到历史记录中
            ProcessMakeupResults();
        }
    }

    /// <summary>
    /// 提交单科考试结果（由 ExamUIManager 在每门课答完后调用）
    /// </summary>
    public void SubmitExamResult(ExamResult result)
    {
        // 计算绩点
        result.gradePoint = GPACalculator.ScoreToGradePoint(result.score);

        currentSemesterResults.Add(result);
        Debug.Log($"[ExamSystem] 提交成绩: {result.courseName} 分数={result.score} 绩点={result.gradePoint}");

        OnSingleExamFinished?.Invoke(result);
    }

    /// <summary>
    /// 完成本学期所有考试的结算
    /// </summary>
    public void FinalizeSemesterExam(int year, int semester)
    {
        // 计算学期 GPA
        float semGPA = GPACalculator.CalcSemesterGPA(currentSemesterResults);

        // 创建学期 GPA 记录
        SemesterGPA sgpa = new SemesterGPA
        {
            year = year,
            semester = semester,
            gpa = semGPA,
            results = new List<ExamResult>(currentSemesterResults),
            failedCount = 0
        };

        // 统计挂科
        failedCourses.Clear();
        for (int i = 0; i < currentSemesterResults.Count; i++)
        {
            if (currentSemesterResults[i].gradePoint == 0f && currentSemesterResults[i].score < 60)
            {
                sgpa.failedCount++;
                failedCourses.Add(currentSemesterResults[i]);
            }
        }

        // 添加到历史记录
        semesterGPAHistory.Add(sgpa);

        // 计算累积 GPA
        sgpa.cumulativeGPA = GPACalculator.CalcCumulativeGPA(semesterGPAHistory);

        // 重置本学期自习计数
        studyCountThisSemester = 0;

        Debug.Log($"[ExamSystem] 学期结算完成: GPA={semGPA:F2} 累积GPA={sgpa.cumulativeGPA:F2} 挂科={sgpa.failedCount}门");

        // 触发事件
        OnExamCompleted?.Invoke(sgpa);
    }

    /// <summary>
    /// 处理补考结果 —— 更新挂科记录并回写学期GPA历史
    /// </summary>
    private void ProcessMakeupResults()
    {
        for (int i = 0; i < currentSemesterResults.Count; i++)
        {
            ExamResult makeup = currentSemesterResults[i];
            if (makeup.score >= 60)
            {
                // 补考通过，从挂科列表中移除
                failedCourses.RemoveAll(f => f.courseId == makeup.courseId);

                // 更新 semesterGPAHistory 中对应课程的成绩
                UpdateSemesterGPAHistory(makeup);

                Debug.Log($"[ExamSystem] 补考通过: {makeup.courseName}");
            }
            else
            {
                Debug.Log($"[ExamSystem] 补考未通过: {makeup.courseName}");
            }
        }
    }

    /// <summary>
    /// 补考通过后更新历史学期GPA记录中的对应课程成绩
    /// 找到原始挂科记录，替换为补考通过的新成绩，并重新计算该学期GPA
    /// </summary>
    private void UpdateSemesterGPAHistory(ExamResult makeupResult)
    {
        for (int i = 0; i < semesterGPAHistory.Count; i++)
        {
            SemesterGPA sgpa = semesterGPAHistory[i];
            for (int j = 0; j < sgpa.results.Count; j++)
            {
                if (sgpa.results[j].courseId == makeupResult.courseId)
                {
                    // 替换为补考成绩
                    sgpa.results[j] = makeupResult;

                    // 重新统计挂科数
                    int newFailedCount = 0;
                    for (int k = 0; k < sgpa.results.Count; k++)
                    {
                        if (sgpa.results[k].score < 60)
                            newFailedCount++;
                    }
                    sgpa.failedCount = newFailedCount;

                    // 重新计算学期 GPA
                    sgpa.gpa = GPACalculator.CalcSemesterGPA(sgpa.results);

                    // 重新计算累积 GPA
                    sgpa.cumulativeGPA = GPACalculator.CalcCumulativeGPA(semesterGPAHistory);

                    Debug.Log($"[ExamSystem] 已更新学期GPA历史: {sgpa.year}年{sgpa.semester}学期 " +
                              $"新GPA={sgpa.gpa:F2} 挂科={sgpa.failedCount}门");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 获取当前待补考的课程
    /// </summary>
    public List<ExamResult> GetFailedCourses()
    {
        return new List<ExamResult>(failedCourses);
    }

    /// <summary>
    /// 是否有待补考课程
    /// </summary>
    public bool HasPendingMakeup()
    {
        return failedCourses.Count > 0;
    }

    // ========== IExamResultProvider 实现 ==========

    public float GetLatestSemesterGPA()
    {
        if (semesterGPAHistory.Count == 0) return 0f;
        return semesterGPAHistory[semesterGPAHistory.Count - 1].gpa;
    }

    public float GetCumulativeGPA()
    {
        if (semesterGPAHistory.Count == 0) return 0f;
        return semesterGPAHistory[semesterGPAHistory.Count - 1].cumulativeGPA;
    }

    public ExamResult[] GetAllResults()
    {
        List<ExamResult> all = new List<ExamResult>();
        for (int i = 0; i < semesterGPAHistory.Count; i++)
        {
            all.AddRange(semesterGPAHistory[i].results);
        }
        return all.ToArray();
    }

    public ExamResult[] GetResultsBySemester(int year, int semester)
    {
        for (int i = 0; i < semesterGPAHistory.Count; i++)
        {
            if (semesterGPAHistory[i].year == year && semesterGPAHistory[i].semester == semester)
            {
                return semesterGPAHistory[i].results.ToArray();
            }
        }
        return new ExamResult[0];
    }

    public bool HasFailedCourses()
    {
        return failedCourses.Count > 0;
    }

    public int GetTotalFailedCount()
    {
        int total = 0;
        for (int i = 0; i < semesterGPAHistory.Count; i++)
        {
            total += semesterGPAHistory[i].failedCount;
        }
        return total;
    }

    public int GetCheatCaughtCount()
    {
        return CheatingSystem.Instance != null ? CheatingSystem.Instance.CaughtCount : 0;
    }

    // ========== 自习追踪 ==========

    /// <summary>
    /// 监听行动执行事件，统计自习次数
    /// </summary>
    private void HandleActionExecuted(ActionDefinition action)
    {
        if (action.id == "study")
        {
            studyCountThisSemester++;
            Debug.Log($"[ExamSystem] 自习次数+1，本学期累计: {studyCountThisSemester}");
        }
    }

    // ========== 辅助方法 ==========

    /// <summary>根据课程ID查找课程定义</summary>
    private CourseDefinition FindCourseById(string courseId)
    {
        if (courseScheduleData == null || courseScheduleData.courses == null) return null;

        for (int i = 0; i < courseScheduleData.courses.Count; i++)
        {
            if (courseScheduleData.courses[i].id == courseId)
                return courseScheduleData.courses[i];
        }
        return null;
    }

    /// <summary>当无 UI 时自动生成考试结果</summary>
    private void AutoGenerateResults(CourseDefinition[] courses, ExamType examType)
    {
        for (int i = 0; i < courses.Length; i++)
        {
            CourseDefinition course = courses[i];
            float passRate = CalculatePassRate(examType, UnityEngine.Random.Range(0, 4), 3);
            int score = CalculateScore(passRate);

            ExamResult result = new ExamResult
            {
                courseId = course.id,
                courseName = course.courseName,
                credits = course.credits,
                score = score,
                gradePoint = GPACalculator.ScoreToGradePoint(score),
                correctCount = 0,
                cheated = false,
                cheatCaught = false,
                examType = examType
            };

            currentSemesterResults.Add(result);
        }
    }

    /// <summary>获取学年中文名</summary>
    private string GetYearName(int year)
    {
        switch (year)
        {
            case 1: return "大一";
            case 2: return "大二";
            case 3: return "大三";
            case 4: return "大四";
            default: return "大" + year;
        }
    }

    /// <summary>获取学期中文名</summary>
    private string GetSemesterName(int semester)
    {
        return semester == 1 ? "上" : "下";
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

        // 加载数据
        LoadExamData();
    }

    private void Start()
    {
        // 订阅行动执行事件，追踪自习次数
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted += HandleActionExecuted;
            Debug.Log("[ExamSystem] 已订阅 ActionSystem.OnActionExecuted");
        }

        // 订阅作弊系统开除事件
        if (CheatingSystem.Instance != null)
        {
            CheatingSystem.Instance.OnExpulsionTriggered += HandleExpulsion;
        }
    }

    private void OnDestroy()
    {
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted -= HandleActionExecuted;
        }

        if (CheatingSystem.Instance != null)
        {
            CheatingSystem.Instance.OnExpulsionTriggered -= HandleExpulsion;
        }
    }

    /// <summary>处理开除结局</summary>
    private void HandleExpulsion()
    {
        Debug.Log("[ExamSystem] 收到开除信号，触发强制结局「学术不端·开除」");
        OnExpulsionTriggered?.Invoke("学术不端·开除");
    }

    // ========== ISaveable 实现 ==========

    public void SaveToData(SaveData data)
    {
        data.semesterGPAHistory = new List<SemesterGPA>(semesterGPAHistory);
        data.failedCourses = new List<ExamResult>(failedCourses);
        data.studyCountThisSemester = studyCountThisSemester;
        data.cet4Passed = cet4Passed;
        data.cet6Passed = cet6Passed;
        data.computerLevelPassed = computerLevelPassed;
        data.lastMidtermResults = new List<ExamResult>(lastMidtermResults);
    }

    public void LoadFromData(SaveData data)
    {
        semesterGPAHistory = data.semesterGPAHistory ?? new List<SemesterGPA>();
        failedCourses = data.failedCourses ?? new List<ExamResult>();
        studyCountThisSemester = data.studyCountThisSemester;
        cet4Passed = data.cet4Passed;
        cet6Passed = data.cet6Passed;
        computerLevelPassed = data.computerLevelPassed;
        lastMidtermResults = data.lastMidtermResults ?? new List<ExamResult>();
    }
}
