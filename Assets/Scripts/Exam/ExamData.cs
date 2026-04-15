using UnityEngine;
using System;
using System.Collections.Generic;

// ========== 枚举定义 ==========

/// <summary>考试类型</summary>
public enum ExamType { Final, Midterm, CET4, CET6, ComputerLevel, Makeup }

/// <summary>作弊结果</summary>
public enum CheatResult { Success, Caught }

// ========== 课程与题目数据 ==========

/// <summary>课程定义 —— 描述一门课的基本信息</summary>
[Serializable]
public class CourseDefinition
{
    public string id;           // "COURSE_MATH_1_1" 格式
    public string courseName;   // "高等数学I"
    public int credits;         // 学分 2-5
    public string subjectTag;   // "math" 等
    public int year;            // 1-4
    public int semester;        // 1-2
}

/// <summary>单道考试题目</summary>
[Serializable]
public class ExamQuestion
{
    public string question;     // 题干
    public string[] options;    // 4个选项
    public int correctIndex;    // 正确答案索引 0-3
    public string subjectTag;   // 科目标签
}

/// <summary>题组 (每组3题)</summary>
[Serializable]
public class QuestionGroup
{
    public string subjectTag;
    public ExamQuestion[] questions; // 固定3题
}

// ========== 考试结果数据 ==========

/// <summary>单科考试结果</summary>
[Serializable]
public class ExamResult
{
    public string courseId;
    public string courseName;
    public int credits;
    public int score;           // 0-100
    public float gradePoint;    // 0-4.0
    public int correctCount;    // 答对题数 0-3
    public bool cheated;        // 是否使用过作弊
    public bool cheatCaught;    // 是否作弊被抓
    public ExamType examType;
}

/// <summary>学期GPA记录</summary>
[Serializable]
public class SemesterGPA
{
    public int year;
    public int semester;
    public float gpa;
    public float cumulativeGPA;
    public List<ExamResult> results;
    public int failedCount;

    public SemesterGPA()
    {
        results = new List<ExamResult>();
    }
}

// ========== JSON 反序列化包装类 ==========

/// <summary>题库 JSON 包装</summary>
[Serializable]
public class QuestionBankData
{
    public List<QuestionGroup> questionGroups;
}

/// <summary>课程表 JSON 包装</summary>
[Serializable]
public class CourseScheduleData
{
    public List<CourseDefinition> courses;
}
