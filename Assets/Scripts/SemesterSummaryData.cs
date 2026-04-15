using System;
using System.Collections.Generic;

// ==========================================================================
// 学期总结数据模型 —— 保存单学期的评分、课程、属性变化、成就等结算数据
// ==========================================================================

/// <summary>
/// 学期评级等级
/// </summary>
public enum SemesterGrade
{
    D,  // < 1500
    C,  // >= 1500
    B,  // >= 3000
    A,  // >= 4500
    S   // >= 6000
}

/// <summary>
/// 学期总结数据 —— 一个学期的完整结算快照
/// </summary>
[Serializable]
public class SemesterSummaryData
{
    // ========== 学期标识 ==========
    public int year;                // 学年 (1-4)
    public int semester;            // 学期 (1=上, 2=下)
    public string yearName;         // "大一" ~ "大四"
    public string semesterName;     // "上学期" / "下学期"

    // ========== 评分明细 ==========
    public float gpa;               // 本学期GPA
    public int academicScore;       // 学业分 = GPA × 1000
    public int socialScore;         // 人际分 = NPC好感总和
    public int sportsScore;         // 体育分 (max 200)
    public int achievementScore;    // 成就分
    public int penaltyScore;        // 扣分项
    public int totalScore;          // 总分
    public SemesterGrade grade;     // 评级 S/A/B/C/D

    // ========== 详细数据 ==========
    public List<CourseGrade> courses;                       // 各科成绩
    public Dictionary<string, int> attributeChanges;        // 属性增减 (属性名 -> 变化量)
    public List<NPCRelationInfo> npcRelations;              // NPC关系
    public List<string> unlockedAchievements;               // 本学期解锁的成就ID

    // ========== 属性快照 ==========
    public Dictionary<string, int> startAttributes;         // 学期开始时属性
    public Dictionary<string, int> endAttributes;           // 学期结束时属性

    public SemesterSummaryData()
    {
        courses = new List<CourseGrade>();
        attributeChanges = new Dictionary<string, int>();
        npcRelations = new List<NPCRelationInfo>();
        unlockedAchievements = new List<string>();
        startAttributes = new Dictionary<string, int>();
        endAttributes = new Dictionary<string, int>();
    }

    /// <summary>
    /// 获取评级的中文显示字符串
    /// </summary>
    public string GetGradeDisplay()
    {
        return grade.ToString();
    }

    /// <summary>
    /// 获取评级对应的颜色 (用于 UI 显示)
    /// </summary>
    public UnityEngine.Color GetGradeColor()
    {
        switch (grade)
        {
            case SemesterGrade.S: return new UnityEngine.Color(1.00f, 0.84f, 0.00f); // 金色
            case SemesterGrade.A: return new UnityEngine.Color(0.60f, 0.20f, 0.80f); // 紫色
            case SemesterGrade.B: return new UnityEngine.Color(0.20f, 0.60f, 1.00f); // 蓝色
            case SemesterGrade.C: return new UnityEngine.Color(0.30f, 0.80f, 0.30f); // 绿色
            case SemesterGrade.D: return new UnityEngine.Color(0.60f, 0.60f, 0.60f); // 灰色
            default: return UnityEngine.Color.white;
        }
    }
}

/// <summary>
/// 学期属性快照 —— 记录某一时刻的所有属性值
/// </summary>
[Serializable]
public class AttributeSnapshot
{
    public int study;
    public int charm;
    public int physique;
    public int leadership;
    public int stress;
    public int mood;
    public int money;
    public int year;
    public int semester;

    /// <summary>
    /// 从当前 PlayerAttributes 和 GameState 拍摄快照
    /// </summary>
    public static AttributeSnapshot CaptureNow()
    {
        var snapshot = new AttributeSnapshot();

        if (PlayerAttributes.Instance != null)
        {
            snapshot.study = PlayerAttributes.Instance.Study;
            snapshot.charm = PlayerAttributes.Instance.Charm;
            snapshot.physique = PlayerAttributes.Instance.Physique;
            snapshot.leadership = PlayerAttributes.Instance.Leadership;
            snapshot.stress = PlayerAttributes.Instance.Stress;
            snapshot.mood = PlayerAttributes.Instance.Mood;
        }

        if (GameState.Instance != null)
        {
            snapshot.money = GameState.Instance.Money;
            snapshot.year = GameState.Instance.CurrentYear;
            snapshot.semester = GameState.Instance.CurrentSemester;
        }

        return snapshot;
    }

    /// <summary>
    /// 转换为字典形式 (属性名 -> 值)
    /// </summary>
    public Dictionary<string, int> ToDictionary()
    {
        return new Dictionary<string, int>
        {
            { "学力", study },
            { "魅力", charm },
            { "体魄", physique },
            { "领导力", leadership },
            { "压力", stress },
            { "心情", mood }
        };
    }
}
