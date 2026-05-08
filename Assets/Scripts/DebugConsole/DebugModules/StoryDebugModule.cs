#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryDebugModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.62f, 0.62f, 0.68f);
    private static readonly Color FieldColor = new Color(0.14f, 0.14f, 0.20f, 0.96f);
    private static readonly Color BtnBlue = new Color(0.23f, 0.43f, 0.72f, 1f);
    private static readonly Color BtnGreen = new Color(0.20f, 0.55f, 0.30f, 1f);
    private static readonly Color BtnRed = new Color(0.60f, 0.20f, 0.20f, 1f);
    private static readonly Color PanelColor = new Color(0.11f, 0.11f, 0.17f, 0.92f);

    private TextMeshProUGUI statusText;
    private TMP_InputField yearInput;
    private TMP_InputField semesterInput;
    private TMP_InputField roundInput;
    private TMP_InputField eventIdInput;
    private TMP_InputField routeInput;
    private TMP_InputField tendencyNameInput;
    private TMP_InputField tendencyValueInput;
    private TMP_InputField flagInput;

    public void Init(RectTransform parent)
    {
        EnsureRuntime();

        Transform content = CreateScrollableContent(parent);
        CreateLabel(content, "剧情控制", 20f, TextGold, 34f);
        statusText = CreateBlockLabel(content, 210f);

        BuildStatusSection(content);
        BuildTimeSection(content);
        BuildEventSection(content);
        BuildPhaseSection(content);
        BuildRouteSection(content);
        BuildTendencySection(content);
        BuildFlagSection(content);
        BuildEndingSection(content);
        BuildPresetSection(content);

        Refresh();
    }

    public void Refresh()
    {
        PrintStoryState();
    }

    private void EnsureRuntime()
    {
        if (EventHistory.Instance == null)
        {
            new GameObject("EventHistory").AddComponent<EventHistory>();
        }

        if (EventScheduler.Instance == null)
        {
            new GameObject("EventScheduler").AddComponent<EventScheduler>();
        }

        if (EventScheduler.Instance != null && EventScheduler.Instance.GetLoadedEventCount() == 0)
        {
            EventScheduler.Instance.LoadEvents();
        }

        if (EventExecutor.Instance == null)
        {
            new GameObject("EventExecutor").AddComponent<EventExecutor>();
        }

        if (StoryRouteState.Instance == null)
        {
            new GameObject("StoryRouteState").AddComponent<StoryRouteState>();
        }

        if (StoryRouteResolver.Instance == null)
        {
            new GameObject("StoryRouteResolver").AddComponent<StoryRouteResolver>();
        }

        if (EndingEvaluator.Instance == null)
        {
            new GameObject("EndingEvaluator").AddComponent<EndingEvaluator>();
        }
    }

    private void BuildStatusSection(Transform parent)
    {
        CreateSectionTitle(parent, "一、当前剧情状态");
        GameObject row = CreateRow(parent, 36f);
        CreateButton(row.transform, "刷新剧情状态", 120f, BtnBlue, PrintStoryState);
        CreateButton(row.transform, "打印完整剧情日志", 150f, BtnGreen, PrintFullStoryDebugLog);
    }

    private void BuildTimeSection(Transform parent)
    {
        CreateSectionTitle(parent, "二、时间控制");

        GameObject row1 = CreateRow(parent, 34f);
        CreateLabel(row1.transform, "时间", 14f, TextWhite, 30f, 48f);
        yearInput = CreateInputField(row1.transform, "Year", 70f, 30f, "1");
        semesterInput = CreateInputField(row1.transform, "Semester", 82f, 30f, "1");
        roundInput = CreateInputField(row1.transform, "Round", 72f, 30f, "1");
        CreateButton(row1.transform, "设置时间", 92f, BtnGreen, SetTimeFromInput);

        GameObject row2 = CreateRow(parent, 36f);
        CreateButton(row2.transform, "大一上R1", 92f, BtnBlue, () => SetTime(1, 1, 1));
        CreateButton(row2.transform, "大三上R1", 92f, BtnBlue, () => SetTime(3, 1, 1));
        CreateButton(row2.transform, "大三下R5", 92f, BtnBlue, () => SetTime(3, 2, 5));
        CreateButton(row2.transform, "大四下R5", 92f, BtnBlue, () => SetTime(4, 2, 5));
    }

    private void BuildEventSection(Transform parent)
    {
        CreateSectionTitle(parent, "三、事件触发控制");

        GameObject row = CreateRow(parent, 34f);
        CreateLabel(row.transform, "Event ID", 14f, TextWhite, 30f, 70f);
        eventIdInput = CreateInputField(row.transform, "ME_016 / SP_006 / END_000", 260f, 30f, "ME_016");
        SetFlexibleWidth(eventIdInput.gameObject, 200f);

        GameObject buttons = CreateRow(parent, 36f);
        CreateButton(buttons.transform, "触发事件ID", 110f, BtnGreen, TriggerEventFromInput);
        CreateButton(buttons.transform, "触发主线事件", 110f, BtnBlue, () => TriggerEvent(SafeText(eventIdInput), "MainStory"));
        CreateButton(buttons.transform, "触发特殊剧情", 110f, BtnBlue, () => TriggerEvent(SafeText(eventIdInput), "Special"));
        CreateButton(buttons.transform, "触发END_000", 110f, BtnGreen, () => TriggerEvent("END_000", "Ending"));
    }

    private void BuildPhaseSection(Transform parent)
    {
        CreateSectionTitle(parent, "四、阶段检查");

        GameObject row = CreateRow(parent, 36f);
        CreateButton(row.transform, "检查RoundStart", 126f, BtnBlue, () => CheckPhase(TriggerPhase.RoundStart));
        CreateButton(row.transform, "检查ActionComplete", 152f, BtnBlue, () => CheckPhase(TriggerPhase.ActionComplete));
        CreateButton(row.transform, "检查RoundEnd", 120f, BtnBlue, () => CheckPhase(TriggerPhase.RoundEnd));
        CreateButton(row.transform, "检查全部阶段", 120f, BtnGreen, CheckAllPhases);
    }

    private void BuildRouteSection(Transform parent)
    {
        CreateSectionTitle(parent, "五、路线控制");

        GameObject row1 = CreateRow(parent, 34f);
        CreateLabel(row1.transform, "MainRoute", 14f, TextWhite, 30f, 78f);
        routeInput = CreateInputField(row1.transform, "GraduateExam", 220f, 30f, "GraduateExam");
        SetFlexibleWidth(routeInput.gameObject, 180f);
        CreateButton(row1.transform, "设置当前路线", 120f, BtnGreen, SetCurrentRoute);
        CreateButton(row1.transform, "锁定路线", 96f, BtnGreen, LockRoute);

        GameObject row2 = CreateRow(parent, 36f);
        CreateButton(row2.transform, "清空路线", 96f, BtnRed, ClearRoute);
        CreateButton(row2.transform, "执行大三路线锁定", 150f, BtnGreen, ResolveRoute);
        CreateButton(row2.transform, "GraduateExam", 118f, BtnBlue, () => routeInput.SetTextWithoutNotify("GraduateExam"));
        CreateButton(row2.transform, "Startup", 86f, BtnBlue, () => routeInput.SetTextWithoutNotify("Startup"));
        CreateButton(row2.transform, "Employment", 100f, BtnBlue, () => routeInput.SetTextWithoutNotify("Employment"));
    }

    private void BuildTendencySection(Transform parent)
    {
        CreateSectionTitle(parent, "六、路线倾向控制");

        GameObject row = CreateRow(parent, 34f);
        tendencyNameInput = CreateInputField(row.transform, "倾向名", 150f, 30f, "研究倾向");
        tendencyValueInput = CreateInputField(row.transform, "变化值", 80f, 30f, "10");
        CreateButton(row.transform, "增加倾向", 96f, BtnGreen, AddTendencyFromInput);

        GameObject row2 = CreateRow(parent, 36f);
        CreateButton(row2.transform, "研究+20", 80f, BtnBlue, () => AddTendency("研究倾向", 20));
        CreateButton(row2.transform, "公职+20", 80f, BtnBlue, () => AddTendency("公职倾向", 20));
        CreateButton(row2.transform, "创业+20", 80f, BtnBlue, () => AddTendency("创业倾向", 20));
        CreateButton(row2.transform, "教育+20", 80f, BtnBlue, () => AddTendency("教育倾向", 20));
        CreateButton(row2.transform, "就业+20", 80f, BtnBlue, () => AddTendency("就业倾向", 20));
        CreateButton(row2.transform, "迷茫+20", 80f, BtnBlue, () => AddTendency("迷茫倾向", 20));
    }

    private void BuildFlagSection(Transform parent)
    {
        CreateSectionTitle(parent, "七、Flag控制");

        GameObject row = CreateRow(parent, 34f);
        CreateLabel(row.transform, "Flag", 14f, TextWhite, 30f, 48f);
        flagInput = CreateInputField(row.transform, "startup_competition_finalist", 300f, 30f, "startup_competition_finalist");
        SetFlexibleWidth(flagInput.gameObject, 220f);
        CreateButton(row.transform, "添加Flag", 90f, BtnGreen, AddFlagFromInput);
        CreateButton(row.transform, "移除Flag", 90f, BtnRed, RemoveFlagFromInput);
        CreateButton(row.transform, "检查Flag", 90f, BtnBlue, CheckFlagFromInput);
        CreateButton(row.transform, "列出Flag", 90f, BtnBlue, ListFlags);
    }

    private void BuildEndingSection(Transform parent)
    {
        CreateSectionTitle(parent, "八、结局控制");

        GameObject row1 = CreateRow(parent, 36f);
        CreateButton(row1.transform, "立即判定结局", 126f, BtnBlue, EvaluateEndingOnly);
        CreateButton(row1.transform, "判定并触发结局", 142f, BtnGreen, EvaluateAndTriggerEnding);

        GameObject row2 = CreateRow(parent, 36f);
        CreateButton(row2.transform, "Graduate_S", 96f, BtnGreen, () => TriggerEvent("END_GRAD_S", "Ending"));
        CreateButton(row2.transform, "Civil_S", 78f, BtnGreen, () => TriggerEvent("END_CIVIL_S", "Ending"));
        CreateButton(row2.transform, "Startup_S", 90f, BtnGreen, () => TriggerEvent("END_STARTUP_S", "Ending"));
        CreateButton(row2.transform, "Education_S", 104f, BtnGreen, () => TriggerEvent("END_EDU_S", "Ending"));
        CreateButton(row2.transform, "Employment_S", 118f, BtnGreen, () => TriggerEvent("END_EMP_S", "Ending"));

        GameObject row3 = CreateRow(parent, 36f);
        CreateButton(row3.transform, "Open_A", 82f, BtnBlue, () => TriggerEvent("END_OPEN_A", "Ending"));
        CreateButton(row3.transform, "Balanced_B", 104f, BtnBlue, () => TriggerEvent("END_BALANCED_B", "Ending"));
        CreateButton(row3.transform, "Bad_C", 74f, BtnRed, () => TriggerEvent("END_BAD_C", "Ending"));
    }

    private void BuildPresetSection(Transform parent)
    {
        CreateSectionTitle(parent, "九、一键测试预设");

        GameObject row = CreateRow(parent, 36f);
        CreateButton(row.transform, "预设：考研S", 110f, BtnGreen, ApplyGraduateSPreset);
        CreateButton(row.transform, "预设：创业S", 110f, BtnGreen, ApplyStartupSPreset);
        CreateButton(row.transform, "预设：教育A", 110f, BtnGreen, ApplyEducationAPreset);
        CreateButton(row.transform, "预设：就业A", 110f, BtnGreen, ApplyEmploymentAPreset);
        CreateButton(row.transform, "预设：开放", 100f, BtnBlue, ApplyOpenPreset);
        CreateButton(row.transform, "预设：坏结局", 110f, BtnRed, ApplyBadPreset);
    }

    private void PrintStoryState()
    {
        EnsureRuntime();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("【剧情状态】");
        AppendTimeState(sb);
        AppendRouteState(sb);
        AppendAttributeState(sb);
        AppendLastEndingState(sb);

        SetStatus(sb.ToString());
        DebugConsoleManager.Log("Story", sb.ToString());
    }

    private void PrintFullStoryDebugLog()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== 剧情完整调试信息 ==========");
        AppendTimeState(sb);
        AppendRouteState(sb);
        AppendAttributeState(sb);
        AppendLastEndingState(sb);
        AppendFlagState(sb);
        sb.AppendLine("====================================");

        Debug.Log(sb.ToString());
        DebugConsoleManager.Log("Story", sb.ToString());
        SetStatus("完整剧情调试信息已打印到 Console 和钟山台日志。");
    }

    private void AppendTimeState(StringBuilder sb)
    {
        if (GameState.Instance == null)
        {
            sb.AppendLine("时间：GameState.Instance 不存在");
            return;
        }

        int totalRound = GetTotalRound(GameState.Instance.CurrentYear, GameState.Instance.CurrentSemester, GameState.Instance.CurrentRound);
        sb.AppendLine($"当前时间：第{GameState.Instance.CurrentYear}年 / 第{GameState.Instance.CurrentSemester}学期 / 第{GameState.Instance.CurrentRound}回合 / 总回合 {totalRound}");
    }

    private void AppendRouteState(StringBuilder sb)
    {
        if (StoryRouteState.Instance == null)
        {
            sb.AppendLine("路线：StoryRouteState.Instance 不存在");
            return;
        }

        StoryRouteState s = StoryRouteState.Instance;
        sb.AppendLine($"当前路线：{s.currentRoute}");
        sb.AppendLine($"锁定路线：{s.lockedRoute}");
        sb.AppendLine($"路线进度：{s.routeProgress}");
        sb.AppendLine($"结局修正：{s.endingQualityBonus}");
        sb.AppendLine($"研究倾向：{s.researchTendency}");
        sb.AppendLine($"公职倾向：{s.civilServiceTendency}");
        sb.AppendLine($"创业倾向：{s.startupTendency}");
        sb.AppendLine($"教育倾向：{s.educationTendency}");
        sb.AppendLine($"就业倾向：{s.employmentTendency}");
        sb.AppendLine($"迷茫倾向：{s.confusedTendency}");
    }

    private void AppendAttributeState(StringBuilder sb)
    {
        if (PlayerAttributes.Instance == null)
        {
            sb.AppendLine("属性：PlayerAttributes.Instance 不存在");
            return;
        }

        PlayerAttributes p = PlayerAttributes.Instance;
        sb.AppendLine($"学力：{p.Study}");
        sb.AppendLine($"魅力：{p.Charm}");
        sb.AppendLine($"体魄：{p.Physique}");
        sb.AppendLine($"领导力：{p.Leadership}");
        sb.AppendLine($"心情：{p.Mood}");
        sb.AppendLine($"压力：{p.Stress}");
    }

    private void AppendLastEndingState(StringBuilder sb)
    {
        if (EndingEvaluator.Instance == null)
        {
            sb.AppendLine("最近结局判定：EndingEvaluator.Instance 不存在");
            return;
        }

        sb.AppendLine($"最近结局判定：{EndingEvaluator.Instance.LastEvaluatedEnding}");
    }

    private void AppendFlagState(StringBuilder sb)
    {
        if (EventHistory.Instance == null)
        {
            sb.AppendLine("Flag：EventHistory.Instance 不存在");
            return;
        }

        Dictionary<string, bool> flags = EventHistory.Instance.GetAllFlagsSnapshot();
        sb.AppendLine($"Flag数量：{flags.Count}");
        foreach (KeyValuePair<string, bool> pair in flags)
        {
            sb.AppendLine($"{pair.Key} = {pair.Value}");
        }
    }

    private void SetTimeFromInput()
    {
        SetTime(
            ParseInt(yearInput, 1, 1, 4),
            ParseInt(semesterInput, 1, 1, 2),
            ParseInt(roundInput, 1, 1, GameState.MaxRoundsPerSemester));
    }

    private void SetTime(int year, int semester, int round)
    {
        if (GameState.Instance == null)
        {
            SetStatus("GameState.Instance 不存在，无法设置时间。");
            return;
        }

        int month = GameState.CalculateMonth(semester, round);
        GameState.Instance.SetState(year, semester, round, month, GameState.Instance.Money, GameState.Instance.ActionPoints);
        string msg = $"已设置时间：Year={year}, Semester={semester}, Round={round}";
        DebugConsoleManager.Log("Story", msg);
        RefreshWithStatus(msg);
    }

    private void TriggerEventFromInput()
    {
        TriggerEvent(SafeText(eventIdInput), "Manual");
    }

    private void TriggerEvent(string eventId, string source)
    {
        EnsureRuntime();

        if (string.IsNullOrWhiteSpace(eventId))
        {
            SetStatus("事件ID为空。");
            return;
        }

        if (EventScheduler.Instance == null)
        {
            SetStatus("EventScheduler.Instance 不存在。");
            return;
        }

        EventScheduler.Instance.EnqueueEvent(eventId.Trim());
        string msg = $"[{source}] 已触发事件：{eventId.Trim()}";
        SetStatus(msg);
        DebugConsoleManager.Log("Story", msg);
    }

    private void CheckPhase(TriggerPhase phase)
    {
        EnsureRuntime();

        if (EventScheduler.Instance == null)
        {
            SetStatus("EventScheduler.Instance 不存在。");
            return;
        }

        EventScheduler.Instance.CheckAndTriggerEvents(phase);
        string msg = $"已检查阶段：{phase}";
        SetStatus(msg);
        DebugConsoleManager.Log("Story", msg);
    }

    private void CheckAllPhases()
    {
        CheckPhase(TriggerPhase.RoundStart);
        CheckPhase(TriggerPhase.ActionComplete);
        CheckPhase(TriggerPhase.RoundEnd);
    }

    private void SetCurrentRoute()
    {
        if (!TryGetRouteFromInput(out MainRoute route))
            return;

        StoryRouteState.Instance.currentRoute = route;
        string msg = $"已设置当前路线：{route}";
        DebugConsoleManager.Log("Story", msg);
        RefreshWithStatus(msg);
    }

    private void LockRoute()
    {
        if (!TryGetRouteFromInput(out MainRoute route))
            return;

        StoryRouteState.Instance.lockedRoute = route;
        string msg = $"已锁定路线：{route}";
        DebugConsoleManager.Log("Story", msg);
        RefreshWithStatus(msg);
    }

    private bool TryGetRouteFromInput(out MainRoute route)
    {
        EnsureRuntime();
        string routeName = SafeText(routeInput);
        if (StoryRouteState.TryParseRoute(routeName, out route))
        {
            return true;
        }

        SetStatus($"未知路线：{routeName}");
        return false;
    }

    private void ClearRoute()
    {
        EnsureRuntime();
        if (StoryRouteState.Instance == null)
        {
            SetStatus("StoryRouteState.Instance 不存在。");
            return;
        }

        StoryRouteState.Instance.currentRoute = MainRoute.None;
        StoryRouteState.Instance.lockedRoute = MainRoute.None;
        string msg = "已清空当前路线和锁定路线。";
        DebugConsoleManager.Log("Story", msg);
        RefreshWithStatus(msg);
    }

    private void ResolveRoute()
    {
        EnsureRuntime();
        if (StoryRouteResolver.Instance == null)
        {
            SetStatus("StoryRouteResolver.Instance 不存在。");
            return;
        }

        StoryRouteResolver.Instance.ResolveAndLockRoute();
        string msg = "已执行大三路线锁定。";
        DebugConsoleManager.Log("Story", msg);
        RefreshWithStatus(msg);
    }

    private void AddTendencyFromInput()
    {
        AddTendency(SafeText(tendencyNameInput), ParseInt(tendencyValueInput, 0, -100, 100));
    }

    private void AddTendency(string tendencyName, int value)
    {
        EnsureRuntime();
        if (StoryRouteState.Instance == null)
        {
            SetStatus("StoryRouteState.Instance 不存在。");
            return;
        }

        if (!TryAddTendency(tendencyName, value))
        {
            SetStatus($"未知倾向：{tendencyName}");
            return;
        }

        string msg = $"已修改倾向：{tendencyName} {value:+#;-#;0}";
        DebugConsoleManager.Log("Story", msg);
        RefreshWithStatus(msg);
    }

    private bool TryAddTendency(string tendencyName, int value)
    {
        StoryRouteState s = StoryRouteState.Instance;
        switch ((tendencyName ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "研究倾向":
            case "研究":
            case "research":
                s.researchTendency = Mathf.Clamp(s.researchTendency + value, 0, 100);
                return true;
            case "公职倾向":
            case "公职":
            case "civil":
                s.civilServiceTendency = Mathf.Clamp(s.civilServiceTendency + value, 0, 100);
                return true;
            case "创业倾向":
            case "创业":
            case "startup":
                s.startupTendency = Mathf.Clamp(s.startupTendency + value, 0, 100);
                return true;
            case "教育倾向":
            case "教育":
            case "education":
                s.educationTendency = Mathf.Clamp(s.educationTendency + value, 0, 100);
                return true;
            case "就业倾向":
            case "就业":
            case "employment":
                s.employmentTendency = Mathf.Clamp(s.employmentTendency + value, 0, 100);
                return true;
            case "迷茫倾向":
            case "迷茫":
            case "confused":
                s.confusedTendency = Mathf.Clamp(s.confusedTendency + value, 0, 100);
                return true;
            default:
                return false;
        }
    }

    private void AddFlagFromInput()
    {
        SetFlagFromInput(true);
    }

    private void RemoveFlagFromInput()
    {
        SetFlagFromInput(false);
    }

    private void SetFlagFromInput(bool value)
    {
        EnsureRuntime();
        string flag = SafeText(flagInput);
        if (string.IsNullOrWhiteSpace(flag))
        {
            SetStatus("Flag为空。");
            return;
        }

        EventHistory.Instance.SetFlag(flag, value);
        string msg = $"Flag：{flag} = {value}";
        SetStatus(msg);
        DebugConsoleManager.Log("Story", msg);
    }

    private void CheckFlagFromInput()
    {
        EnsureRuntime();
        string flag = SafeText(flagInput);
        if (string.IsNullOrWhiteSpace(flag))
        {
            SetStatus("Flag为空。");
            return;
        }

        bool value = EventHistory.Instance.GetFlag(flag);
        string msg = $"Flag状态：{flag} = {value}";
        SetStatus(msg);
        DebugConsoleManager.Log("Story", msg);
    }

    private void ListFlags()
    {
        StringBuilder sb = new StringBuilder();
        AppendFlagState(sb);
        SetStatus(sb.ToString());
        DebugConsoleManager.Log("Story", sb.ToString());
    }

    private void EvaluateEndingOnly()
    {
        EnsureRuntime();
        if (EndingEvaluator.Instance == null)
        {
            SetStatus("EndingEvaluator.Instance 不存在。");
            return;
        }

        EndingId ending = EndingEvaluator.Instance.EvaluateEnding();
        string eventId = EndingEvaluator.Instance.ConvertEndingToEventId(ending);
        string msg = $"当前结局判定结果：{ending} -> {eventId}";
        SetStatus(msg);
        DebugConsoleManager.Log("Ending", msg);
    }

    private void EvaluateAndTriggerEnding()
    {
        EnsureRuntime();
        if (EndingEvaluator.Instance == null)
        {
            SetStatus("EndingEvaluator.Instance 不存在。");
            return;
        }

        EndingEvaluator.Instance.EvaluateAndTriggerEnding();
        string msg = $"已执行结局判定并触发：{EndingEvaluator.Instance.LastEvaluatedEnding}";
        SetStatus(msg);
        DebugConsoleManager.Log("Ending", msg);
    }

    private void ApplyGraduateSPreset()
    {
        ApplyRoute(MainRoute.GraduateExam, MainRoute.GraduateExam);
        SetTendencies(research: 90);
        SetAttributes(study: 90, charm: 60, physique: 60, leadership: 55, mood: 65, stress: 35);
        SetFlag("academic_award_won", true);
        CompletePreset("考研S");
    }

    private void ApplyStartupSPreset()
    {
        ApplyRoute(MainRoute.Startup, MainRoute.Startup);
        SetTendencies(startup: 90);
        SetAttributes(study: 60, charm: 70, physique: 90, leadership: 65, mood: 60, stress: 40);
        SetFlag("startup_competition_finalist", true);
        CompletePreset("创业S");
    }

    private void ApplyEducationAPreset()
    {
        ApplyRoute(MainRoute.Education, MainRoute.Education);
        SetTendencies(education: 70);
        SetAttributes(study: 65, charm: 60, physique: 55, leadership: 55, mood: 65, stress: 35);
        SetFlag("volunteer_teaching_completed", true);
        CompletePreset("教育A");
    }

    private void ApplyEmploymentAPreset()
    {
        ApplyRoute(MainRoute.Employment, MainRoute.Employment);
        SetTendencies(employment: 65);
        SetAttributes(study: 55, charm: 55, physique: 70, leadership: 45, mood: 55, stress: 45);
        SetFlag("internship_completed", true);
        CompletePreset("就业A");
    }

    private void ApplyOpenPreset()
    {
        ApplyRoute(MainRoute.Confused, MainRoute.Confused);
        SetTendencies(confused: 75);
        SetAttributes(study: 55, charm: 55, physique: 40, leadership: 40, mood: 70, stress: 65);
        SetFlag("low_point_recovered", true);
        CompletePreset("开放结局");
    }

    private void ApplyBadPreset()
    {
        ApplyRoute(MainRoute.None, MainRoute.None);
        SetTendencies();
        SetAttributes(study: 35, charm: 35, physique: 35, leadership: 35, mood: 30, stress: 90);
        CompletePreset("坏结局");
    }

    private void ApplyRoute(MainRoute current, MainRoute locked)
    {
        EnsureRuntime();
        StoryRouteState.Instance.currentRoute = current;
        StoryRouteState.Instance.lockedRoute = locked;
    }

    private void SetTendencies(int research = 0, int civil = 0, int startup = 0, int education = 0, int employment = 0, int confused = 0)
    {
        StoryRouteState s = StoryRouteState.Instance;
        s.researchTendency = research;
        s.civilServiceTendency = civil;
        s.startupTendency = startup;
        s.educationTendency = education;
        s.employmentTendency = employment;
        s.confusedTendency = confused;
        s.routeProgress = 0;
        s.endingQualityBonus = 0;
    }

    private void SetAttributes(int study, int charm, int physique, int leadership, int mood, int stress)
    {
        if (PlayerAttributes.Instance == null)
        {
            return;
        }

        PlayerAttributes.Instance.Study = study;
        PlayerAttributes.Instance.Charm = charm;
        PlayerAttributes.Instance.Physique = physique;
        PlayerAttributes.Instance.Leadership = leadership;
        PlayerAttributes.Instance.Mood = mood;
        PlayerAttributes.Instance.Stress = stress;
    }

    private void SetFlag(string flag, bool value)
    {
        if (EventHistory.Instance != null)
        {
            EventHistory.Instance.SetFlag(flag, value);
        }
    }

    private void CompletePreset(string presetName)
    {
        EndingId ending = EndingEvaluator.Instance != null ? EndingEvaluator.Instance.EvaluateEnding() : EndingId.None;
        string msg = $"已应用预设：{presetName}，当前判定：{ending}";
        DebugConsoleManager.Log("Story", msg);
        RefreshWithStatus(msg);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void RefreshWithStatus(string message)
    {
        PrintStoryState();
        if (statusText != null)
        {
            statusText.text = $"{message}\n\n{statusText.text}";
        }
    }

    private int GetTotalRound(int year, int semester, int round)
    {
        int roundsPerSemester = Mathf.Max(1, GameState.MaxRoundsPerSemester);
        return (Mathf.Clamp(year, 1, 4) - 1) * 2 * roundsPerSemester
            + (Mathf.Clamp(semester, 1, 2) - 1) * roundsPerSemester
            + Mathf.Clamp(round, 1, roundsPerSemester);
    }

    private int ParseInt(TMP_InputField input, int fallback, int min, int max)
    {
        if (input == null || !int.TryParse(input.text, out int value))
        {
            return fallback;
        }

        return Mathf.Clamp(value, min, max);
    }

    private string SafeText(TMP_InputField input)
    {
        return input != null ? input.text.Trim() : string.Empty;
    }

    private Transform CreateScrollableContent(RectTransform parent)
    {
        GameObject scrollObject = CreateRect("ScrollView", parent).gameObject;
        StretchFull(scrollObject.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateRect("Viewport", scrollObject.transform).gameObject;
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject contentObject = CreateRect("Content", viewport.transform).gameObject;
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;
        return contentObject.transform;
    }

    private void CreateSectionTitle(Transform parent, string title)
    {
        CreateLabel(parent, title, 17f, TextGold, 30f);
    }

    private GameObject CreateRow(Transform parent, float height)
    {
        GameObject rowObject = CreateRect("Row", parent).gameObject;
        rowObject.AddComponent<LayoutElement>().preferredHeight = height;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return rowObject;
    }

    private TextMeshProUGUI CreateBlockLabel(Transform parent, float height)
    {
        GameObject panel = CreateRect("StatusPanel", parent).gameObject;
        panel.AddComponent<LayoutElement>().preferredHeight = height;
        Image background = panel.AddComponent<Image>();
        background.color = PanelColor;

        TextMeshProUGUI text = CreateLabel(panel.transform, string.Empty, 14f, TextWhite, height);
        StretchFull(text.rectTransform);
        text.margin = new Vector4(12f, 10f, 12f, 10f);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height, string defaultValue = "")
    {
        GameObject inputObject = CreateRect("Input", parent).gameObject;
        LayoutElement layout = inputObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        Image background = inputObject.AddComponent<Image>();
        background.color = FieldColor;

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();

        GameObject viewport = CreateRect("Viewport", inputObject.transform).gameObject;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 2f);
        viewportRect.offsetMax = new Vector2(-8f, -2f);
        viewport.AddComponent<RectMask2D>();

        TextMeshProUGUI text = CreateLabel(viewport.transform, string.Empty, 14f, TextWhite, height - 2f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI ph = CreateLabel(viewport.transform, placeholder, 14f, TextGray, height - 2f);
        StretchFull(ph.rectTransform);
        ph.alignment = TextAlignmentOptions.MidlineLeft;

        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = ph;
        input.SetTextWithoutNotify(defaultValue);
        return input;
    }

    private Button CreateButton(Transform parent, string label, float width, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 32f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = bgColor;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 13f, Color.white, 32f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string value, float size, Color color, float height, float width = -1f)
    {
        GameObject textObject = CreateRect("Label", parent).gameObject;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
        }

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(2f, 4f, 2f, 4f);
        text.extraPadding = true;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }

        return text;
    }

    private void SetFlexibleWidth(GameObject target, float minWidth)
    {
        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = target.AddComponent<LayoutElement>();
        }

        layout.minWidth = minWidth;
        layout.flexibleWidth = 1f;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
