using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GPA 计算器 —— 纯静态工具类，无状态
/// </summary>
public static class GPACalculator
{
    /// <summary>
    /// 百分制分数转绩点
    /// 90+ → 4.0, 80+ → 3.0, 70+ → 2.0, 60+ → 1.0, &lt;60 → 0
    /// </summary>
    public static float ScoreToGradePoint(int score)
    {
        if (score >= 90) return 4.0f;
        if (score >= 80) return 3.0f;
        if (score >= 70) return 2.0f;
        if (score >= 60) return 1.0f;
        return 0f;
    }

    /// <summary>
    /// 计算学期 GPA = Σ(单科绩点×学分) / Σ学分
    /// </summary>
    public static float CalcSemesterGPA(List<ExamResult> results)
    {
        if (results == null || results.Count == 0) return 0f;

        float totalWeighted = 0f;
        int totalCredits = 0;

        for (int i = 0; i < results.Count; i++)
        {
            totalWeighted += results[i].gradePoint * results[i].credits;
            totalCredits += results[i].credits;
        }

        return totalCredits > 0 ? totalWeighted / totalCredits : 0f;
    }

    /// <summary>
    /// 计算累积 GPA（所有学期的加权平均）
    /// </summary>
    public static float CalcCumulativeGPA(List<SemesterGPA> allSemesters)
    {
        if (allSemesters == null || allSemesters.Count == 0) return 0f;

        float totalWeighted = 0f;
        int totalCredits = 0;

        for (int i = 0; i < allSemesters.Count; i++)
        {
            SemesterGPA sg = allSemesters[i];
            if (sg.results == null) continue;

            for (int j = 0; j < sg.results.Count; j++)
            {
                ExamResult r = sg.results[j];
                totalWeighted += r.gradePoint * r.credits;
                totalCredits += r.credits;
            }
        }

        return totalCredits > 0 ? totalWeighted / totalCredits : 0f;
    }
}
