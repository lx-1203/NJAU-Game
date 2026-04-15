using System;
using System.Collections.Generic;

// ==========================================================================
// 游戏数据提供者接口 —— 各子系统通过这些接口向结算/成就/结局系统提供数据
// 当前版本使用内置默认实现，后续可替换为真正的课程/NPC/社团/经济/恋爱系统
// ==========================================================================

// ========== 辅助数据结构 ==========

/// <summary>
/// 单科成绩
/// </summary>
[Serializable]
public class CourseGrade
{
    public string courseName;   // 课程名
    public float score;         // 百分制成绩
    public float gradePoint;    // 绩点 (0.0 ~ 4.0)
    public int credits;         // 学分

    public CourseGrade() { }

    public CourseGrade(string name, float score, float gp, int credits)
    {
        this.courseName = name;
        this.score = score;
        this.gradePoint = gp;
        this.credits = credits;
    }
}

/// <summary>
/// NPC 关系信息
/// </summary>
[Serializable]
public class NPCRelationInfo
{
    public string npcName;          // NPC名称
    public int friendship;          // 当前好感度
    public int friendshipChange;    // 本学期变化量

    public NPCRelationInfo() { }

    public NPCRelationInfo(string name, int friendship, int change)
    {
        this.npcName = name;
        this.friendship = friendship;
        this.friendshipChange = change;
    }
}

// ========== Provider 接口 ==========

/// <summary>
/// 绩点数据提供者
/// </summary>
public interface IGPAProvider
{
    /// <summary>获取当前累计GPA</summary>
    float GetCurrentGPA();

    /// <summary>获取指定学期的GPA</summary>
    float GetSemesterGPA(int year, int semester);

    /// <summary>获取指定学期的各科成绩</summary>
    List<CourseGrade> GetSemesterCourses(int year, int semester);
}

/// <summary>
/// NPC 关系数据提供者
/// </summary>
public interface INPCRelationshipProvider
{
    /// <summary>获取所有NPC好感度总和</summary>
    int GetTotalFriendship();

    /// <summary>获取所有NPC的关系信息列表</summary>
    List<NPCRelationInfo> GetAllRelations();
}

/// <summary>
/// 社团/组织成员数据提供者
/// </summary>
public interface IClubMembershipProvider
{
    /// <summary>是否为学生会主席</summary>
    bool IsStudentCouncilPresident();

    /// <summary>是否已入党</summary>
    bool IsPartyMember();

    /// <summary>获取已加入的社团列表</summary>
    List<string> GetJoinedClubs();
}

/// <summary>
/// 经济数据提供者
/// </summary>
public interface IEconomyProvider
{
    /// <summary>获取当前金钱</summary>
    int GetCurrentMoney();

    /// <summary>获取历史总收入</summary>
    int GetTotalEarned();

    /// <summary>获取历史总支出</summary>
    int GetTotalSpent();
}

/// <summary>
/// 恋爱数据提供者
/// </summary>
public interface IRomanceProvider
{
    /// <summary>获取恋爱等级 (0-5星)</summary>
    int GetRomanceLevel();

    /// <summary>获取恋人名称</summary>
    string GetPartnerName();

    /// <summary>是否有恋人</summary>
    bool HasPartner();
}
