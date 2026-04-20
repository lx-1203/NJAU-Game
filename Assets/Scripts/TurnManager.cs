using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 回合管理器 —— 监听行动完成后检查行动点是否耗尽，并驱动回合推进
/// </summary>
public class TurnManager : MonoBehaviour
{
    // ========== 单例 ==========

    public static TurnManager Instance { get; private set; }
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

    // ========== 事件 ==========

    /// <summary>回合推进完成后触发</summary>
    public event Action<GameState.RoundAdvanceResult> OnRoundAdvanced;

    /// <summary>游戏结束（毕业）时触发</summary>
    public event Action OnGameCompleted;

    // ========== 初始化 ==========

    /// <summary>
    /// 在 Start 中订阅事件，确保 ActionSystem 已完成初始化
    /// </summary>
    private void Start()
    {
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted += HandleActionExecuted;
            Debug.Log("[TurnManager] 初始化完成，已订阅 OnActionExecuted");
        }
        else
        {
            Debug.LogError("[TurnManager] ActionSystem 实例不存在，无法订阅事件");
        }
    }

    // ========== 清理 ==========

    /// <summary>
    /// 销毁时取消订阅，防止空引用
    /// </summary>
    private void OnDestroy()
    {
        if (ActionSystem.Instance != null)
        {
            ActionSystem.Instance.OnActionExecuted -= HandleActionExecuted;
        }
    }

    // ========== 行动回调 ==========

    /// <summary>
    /// 行动执行后的回调：通知事件系统，检查行动点是否耗尽，若耗尽则延迟推进回合
    /// </summary>
    private void HandleActionExecuted(ActionDefinition action)
    {
        if (GameState.Instance == null) return;

        // 行动完成后通知事件系统检查
        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.CheckAndTriggerEvents(TriggerPhase.ActionComplete);
        }

        int remaining = GameState.Instance.ActionPoints;
        Debug.Log($"[TurnManager] 行动完成，剩余行动点: {remaining}");

        if (remaining == 0)
        {
            Debug.Log("[TurnManager] 行动点耗尽，准备推进回合...");
            StartCoroutine(DelayedAdvanceRound());
        }
    }

    // ========== 考试系统拦截 ==========

    /// <summary>是否正在等待考试完成</summary>
    private bool waitingForExam = false;

    /// <summary>暂存的回合推进结果（考试完成后继续处理）</summary>
    private GameState.RoundAdvanceResult pendingAdvanceResult;

    // ========== 回合推进 ==========

    /// <summary>
    /// 延迟推进回合，给 UI 一个反应时间
    /// </summary>
    private IEnumerator DelayedAdvanceRound()
    {
        yield return new WaitForSeconds(0.5f);
        DoAdvanceRound();
    }

    /// <summary>
    /// 核心回合推进逻辑：调用 GameState 推进并触发相应事件
    /// 包含考试拦截、经济结算（生活费、学费、债务惩罚）和事件系统调度
    /// </summary>
    private void DoAdvanceRound()
    {
        if (GameState.Instance == null) return;

        // ========== 期中考试拦截（每学期第3回合） ==========

        int midtermRound = 3; // 每学期5回合，期中在第3回合
        if (GameState.Instance.CurrentRound == midtermRound && !waitingForExam)
        {
            if (ExamSystem.Instance != null && ExamSystem.Instance.IsDataLoaded)
            {
                int year = GameState.Instance.CurrentYear;
                int semester = GameState.Instance.CurrentSemester;

                Debug.Log($"[TurnManager] 学期中间检测到，触发期中考试 —— {GameState.Instance.GetYearName()}{GameState.Instance.GetSemesterName()}");

                waitingForExam = true;

                // 订阅考试完成事件
                if (ExamUIManager.Instance != null)
                {
                    ExamUIManager.Instance.OnExamUICompleted += HandleMidtermCompleted;
                }
                else
                {
                    GameObject examUIObj = new GameObject("ExamUIManager");
                    ExamUIManager examUI = examUIObj.AddComponent<ExamUIManager>();
                    examUI.OnExamUICompleted += HandleMidtermCompleted;
                }

                ExamSystem.Instance.StartMidtermExam(year, semester);
                return; // 暂停推进，等期中考试完成
            }
        }

        // ========== 学期末考试拦截（学期最后一回合） ==========

        // 如果当前是学期最后一回合，触发期末考试
        if (GameState.Instance.CurrentRound == GameState.MaxRoundsPerSemester && !waitingForExam)
        {
            if (ExamSystem.Instance != null && ExamSystem.Instance.IsDataLoaded)
            {
                int year = GameState.Instance.CurrentYear;
                int semester = GameState.Instance.CurrentSemester;

                Debug.Log($"[TurnManager] 学期末检测到，触发期末考试 —— {GameState.Instance.GetYearName()}{GameState.Instance.GetSemesterName()}");

                waitingForExam = true;

                // 订阅考试完成事件
                if (ExamUIManager.Instance != null)
                {
                    ExamUIManager.Instance.OnExamUICompleted += HandleExamCompleted;
                }
                else
                {
                    // 如果 ExamUIManager 不存在，动态创建
                    GameObject examUIObj = new GameObject("ExamUIManager");
                    ExamUIManager examUI = examUIObj.AddComponent<ExamUIManager>();
                    examUI.OnExamUICompleted += HandleExamCompleted;
                }

                ExamSystem.Instance.StartSemesterExam(year, semester);
                return; // 暂停推进，等考试完成
            }
        }

        ContinueAdvanceRound();
    }

    /// <summary>
    /// 考试完成后的回调：执行体测，然后继续回合推进
    /// </summary>
    private void HandleExamCompleted()
    {
        Debug.Log("[TurnManager] 考试完成，执行体测");

        // 取消订阅
        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.OnExamUICompleted -= HandleExamCompleted;
        }

        waitingForExam = false;

        // 期末考试后执行体测（使用默认策略，后续可接入UI选择）
        if (PhysicalTestSystem.Instance != null)
        {
            var ptResult = PhysicalTestSystem.Instance.ExecuteTestDefault();
            Debug.Log($"[TurnManager] 体测完成: {ptResult.grade} ({ptResult.totalScore}分)");
        }

        // 继续推进
        ContinueAdvanceRound();
    }

    /// <summary>
    /// 期中考试完成后的回调：结算期中成绩，继续回合推进
    /// </summary>
    private void HandleMidtermCompleted()
    {
        Debug.Log("[TurnManager] 期中考试完成，继续回合推进");

        // 取消订阅
        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.OnExamUICompleted -= HandleMidtermCompleted;
        }

        // 期中考试结算（不影响 GPA）
        if (ExamSystem.Instance != null)
        {
            ExamSystem.Instance.FinalizeMidtermExam();
        }

        waitingForExam = false;

        // 继续推进
        ContinueAdvanceRound();
    }

    /// <summary>
    /// 继续执行回合推进（考试完成后或无需考试时直接执行）
    /// </summary>
    private void ContinueAdvanceRound()
    {
        if (GameState.Instance == null) return;

        // ========== 回合结算阶段（推进前） ==========

        // 回合结算事件检查
        if (EventScheduler.Instance != null)
        {
            EventScheduler.Instance.CheckAndTriggerEvents(TriggerPhase.RoundEnd);
        }

        GameState.RoundAdvanceResult result = GameState.Instance.AdvanceRound();

        // ========== 经济结算 ==========

        // 每回合收入结算（生活费等）
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.ProcessRoundIncome();
        }

        // 学期/学年切换时扣除学费
        if (result == GameState.RoundAdvanceResult.NextSemester ||
            result == GameState.RoundAdvanceResult.NextYear)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.ProcessSemesterStart();
            }
        }

        // 每回合债务惩罚（透支状态下压力+10）
        if (DebtSystem.Instance != null)
        {
            DebtSystem.Instance.ProcessRoundDebtPenalty();
        }

        // 社团系统回合结算（晋升、入党阶段推进等）
        if (ClubSystem.Instance != null)
        {
            ClubSystem.Instance.OnRoundEnd();
        }

        // 学期切换时检查校园跑完成情况
        if (result == GameState.RoundAdvanceResult.NextSemester ||
            result == GameState.RoundAdvanceResult.NextYear)
        {
            if (CampusRunSystem.Instance != null)
            {
                float completionRate = CampusRunSystem.Instance.GetCompletionRate();
                if (completionRate < 1f && PlayerAttributes.Instance != null)
                {
                    // 未完成校园跑：压力+5，体魄-3
                    PlayerAttributes.Instance.Stress += 5;
                    PlayerAttributes.Instance.Physique -= 3;
                    Debug.Log($"[TurnManager] 校园跑未完成(完成率{completionRate:P0})，压力+5，体魄-3");
                }
            }
        }

        // ========== 日志输出 ==========

        switch (result)
        {
            case GameState.RoundAdvanceResult.NextRound:
                Debug.Log($"=== 回合推进 === 进入 {GameState.Instance.GetTimeDescription()}");
                break;
            case GameState.RoundAdvanceResult.NextSemester:
                Debug.Log($"=== 学期推进 === 进入 {GameState.Instance.GetTimeDescription()}");
                break;
            case GameState.RoundAdvanceResult.NextYear:
                Debug.Log($"=== 学年推进 === 进入 {GameState.Instance.GetTimeDescription()}");
                break;
            case GameState.RoundAdvanceResult.Graduated:
                Debug.Log("=== 毕业！=== 恭喜完成大学四年！");
                break;
        }

        OnRoundAdvanced?.Invoke(result);

        // ========== 自动存档 ==========

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();
        }

        // ========== 新回合开始阶段（推进后） ==========

        // 回合开始事件检查（强制阶段：主线事件、固定事件等）
        if (result != GameState.RoundAdvanceResult.Graduated && EventScheduler.Instance != null)
        {
            EventScheduler.Instance.CheckAndTriggerEvents(TriggerPhase.RoundStart);
        }

        // ========== 补考检查（新学期第1回合） ==========

        if (GameState.Instance.CurrentRound == 1 && ExamSystem.Instance != null && ExamSystem.Instance.HasPendingMakeup())
        {
            Debug.Log("[TurnManager] 新学期第1回合，检测到挂科课程，触发补考");
            var failedCourses = ExamSystem.Instance.GetFailedCourses();

            if (ExamUIManager.Instance != null)
            {
                ExamUIManager.Instance.OnExamUICompleted += HandleMakeupCompleted;
            }

            ExamSystem.Instance.StartMakeupExam(failedCourses);
        }

        // ========== 证书考试自动触发 ==========
        // CET4: 大一下学期第30回合起（每学期尝试一次）; CET6: CET4通过后; 计算机等级: 大二上起
        TryCertificateExams();

        if (result == GameState.RoundAdvanceResult.Graduated)
        {
            OnGameCompleted?.Invoke();
        }
    }

    /// <summary>
    /// 补考完成后的回调
    /// </summary>
    private void HandleMakeupCompleted()
    {
        Debug.Log("[TurnManager] 补考完成");

        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.OnExamUICompleted -= HandleMakeupCompleted;
        }
    }

    // ========== 证书考试自动触发 ==========

    /// <summary>
    /// 检查并触发证书考试（CET4/CET6/计算机等级）
    /// 触发时机: 每学期第30回合（期中之后、期末之前的窗口期）
    /// - CET4: 大一下学期(year=1,semester=2)起, 每学期可尝试一次直到通过
    /// - CET6: CET4通过后，下一学期起可报考
    /// - 计算机等级: 大二上学期(year=2,semester=1)起可报考
    /// </summary>
    private void TryCertificateExams()
    {
        if (ExamSystem.Instance == null || GameState.Instance == null) return;

        int year = GameState.Instance.CurrentYear;
        int semester = GameState.Instance.CurrentSemester;
        int round = GameState.Instance.CurrentRound;

        // 证书考试在每学期第4回合触发（期中考后、期末考前的窗口）
        if (round != 4) return;

        Debug.Log($"[TurnManager] 证书考试窗口期 —— {GameState.Instance.GetYearName()}{GameState.Instance.GetSemesterName()} 第{round}回合");

        // CET4: 大一下学期起（year>=1 && semester>=2, 或 year>=2）
        bool cet4Eligible = (year == 1 && semester == 2) || year >= 2;
        if (cet4Eligible && !ExamSystem.Instance.IsCET4Passed)
        {
            Debug.Log("[TurnManager] 触发 CET4 (大学英语四级) 考试");
            waitingForExam = true;

            if (ExamUIManager.Instance != null)
            {
                ExamUIManager.Instance.OnExamUICompleted += HandleCertExamCompleted;
            }
            else
            {
                GameObject examUIObj = new GameObject("ExamUIManager");
                ExamUIManager examUI = examUIObj.AddComponent<ExamUIManager>();
                examUI.OnExamUICompleted += HandleCertExamCompleted;
            }

            ExamSystem.Instance.StartCET4Exam();
            return; // 一次只触发一个证书考试
        }

        // CET6: CET4通过后可报考
        if (ExamSystem.Instance.IsCET4Passed && !ExamSystem.Instance.IsCET6Passed)
        {
            Debug.Log("[TurnManager] 触发 CET6 (大学英语六级) 考试");
            waitingForExam = true;

            if (ExamUIManager.Instance != null)
            {
                ExamUIManager.Instance.OnExamUICompleted += HandleCertExamCompleted;
            }
            else
            {
                GameObject examUIObj = new GameObject("ExamUIManager");
                ExamUIManager examUI = examUIObj.AddComponent<ExamUIManager>();
                examUI.OnExamUICompleted += HandleCertExamCompleted;
            }

            ExamSystem.Instance.StartCET6Exam();
            return;
        }

        // 计算机等级: 大二上起
        bool computerEligible = year >= 2;
        if (computerEligible && !ExamSystem.Instance.IsComputerLevelPassed)
        {
            Debug.Log("[TurnManager] 触发计算机等级考试");
            waitingForExam = true;

            if (ExamUIManager.Instance != null)
            {
                ExamUIManager.Instance.OnExamUICompleted += HandleCertExamCompleted;
            }
            else
            {
                GameObject examUIObj = new GameObject("ExamUIManager");
                ExamUIManager examUI = examUIObj.AddComponent<ExamUIManager>();
                examUI.OnExamUICompleted += HandleCertExamCompleted;
            }

            ExamSystem.Instance.StartComputerLevelExam();
            return;
        }
    }

    /// <summary>
    /// 证书考试完成回调
    /// </summary>
    private void HandleCertExamCompleted()
    {
        Debug.Log("[TurnManager] 证书考试完成");
        if (ExamUIManager.Instance != null)
        {
            ExamUIManager.Instance.OnExamUICompleted -= HandleCertExamCompleted;
        }
        waitingForExam = false;
    }
}
