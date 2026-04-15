/// <summary>
/// 考试结果查询接口 —— 供结局系统和天赋系统使用
/// </summary>
public interface IExamResultProvider
{
    /// <summary>获取最近一个学期的 GPA</summary>
    float GetLatestSemesterGPA();

    /// <summary>获取累积 GPA</summary>
    float GetCumulativeGPA();

    /// <summary>获取所有考试结果</summary>
    ExamResult[] GetAllResults();

    /// <summary>获取指定学期的考试结果</summary>
    ExamResult[] GetResultsBySemester(int year, int semester);

    /// <summary>是否有挂科课程</summary>
    bool HasFailedCourses();

    /// <summary>总挂科门数</summary>
    int GetTotalFailedCount();

    /// <summary>作弊被抓总次数</summary>
    int GetCheatCaughtCount();
}
