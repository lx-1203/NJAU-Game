using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 体测系统 —— 每学期一次体能测试（期末考试周最后项目）
/// 三项测试：中长跑(1000m/800m)、力量(引体向上/仰卧起坐)、立定跳远
/// 每项3个策略阶段选择，综合评分
/// </summary>
public class PhysicalTestSystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static PhysicalTestSystem Instance { get; private set; }

    // ========== 事件 ==========
    public event Action<PhysicalTestResult> OnTestCompleted;

    // ========== 数据模型 ==========

    [Serializable]
    public class PhysicalTestResult
    {
        public int runScore;
        public int strengthScore;
        public int jumpScore;
        public int totalScore;
        public string grade; // Excellent/Good/Pass/Fail
    }

    public enum TestStrategy
    {
        Conservative,  // 保守/稳妥
        Aggressive,    // 激进/冲刺
        Passive        // 消极/放弃
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

    // ========== 体测入口 ==========

    /// <summary>
    /// 执行完整体测流程（由TurnManager在考试周触发）
    /// strategies: 9个策略选择 [跑步3阶段, 力量3阶段, 跳远3次尝试]
    /// </summary>
    public PhysicalTestResult ExecuteTest(TestStrategy[] strategies)
    {
        int physique = PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Physique : 50;

        var result = new PhysicalTestResult();
        result.runScore = CalculateRunScore(physique, strategies[0], strategies[1], strategies[2]);
        result.strengthScore = CalculateStrengthScore(physique, strategies[3], strategies[4], strategies[5]);
        result.jumpScore = CalculateJumpScore(physique, strategies[6], strategies[7], strategies[8]);
        result.totalScore = (result.runScore + result.strengthScore + result.jumpScore) / 3;

        // 评级
        if (result.totalScore >= 90) result.grade = "Excellent";
        else if (result.totalScore >= 75) result.grade = "Good";
        else if (result.totalScore >= 60) result.grade = "Pass";
        else result.grade = "Fail";

        // 属性效果
        ApplyTestEffects(result);

        Debug.Log($"[PhysicalTestSystem] 体测完成: 跑步={result.runScore} 力量={result.strengthScore} " +
                  $"跳远={result.jumpScore} 总分={result.totalScore} 评级={result.grade}");

        OnTestCompleted?.Invoke(result);
        return result;
    }

    /// <summary>
    /// 使用默认策略（全部保守）执行体测
    /// </summary>
    public PhysicalTestResult ExecuteTestDefault()
    {
        return ExecuteTest(new TestStrategy[]
        {
            TestStrategy.Conservative, TestStrategy.Conservative, TestStrategy.Conservative,
            TestStrategy.Conservative, TestStrategy.Conservative, TestStrategy.Conservative,
            TestStrategy.Conservative, TestStrategy.Conservative, TestStrategy.Conservative
        });
    }

    // ========== 中长跑评分 ==========

    /// <summary>
    /// 中长跑评分（3阶段策略选择）
    /// 基础分 = physique >= 120 → 80; 80-119 → 65; 50-79 → 50; <50 → 30
    /// 策略加成范围: -30 ~ +35
    /// </summary>
    private int CalculateRunScore(int physique, TestStrategy phase1, TestStrategy phase2, TestStrategy phase3)
    {
        // 基础分
        int baseScore;
        if (physique >= 120) baseScore = 80;
        else if (physique >= 80) baseScore = 65;
        else if (physique >= 50) baseScore = 50;
        else baseScore = 30;

        int bonus = 0;
        bool sideStitch = false;

        // 阶段1（起跑）
        switch (phase1)
        {
            case TestStrategy.Conservative: bonus += 0; break;
            case TestStrategy.Aggressive: bonus += 10; sideStitch = true; break; // 有岔气风险
            case TestStrategy.Passive: bonus -= 10; break;
        }

        // 阶段2（途中跑）
        switch (phase2)
        {
            case TestStrategy.Conservative: bonus += 5; break;
            case TestStrategy.Aggressive:
                if (sideStitch)
                {
                    // 阶段1激进 + 阶段2激进 = 70%成功+15, 30%岔气-20
                    if (UnityEngine.Random.value < 0.7f)
                        bonus += 15;
                    else
                        bonus -= 20;
                }
                else
                {
                    bonus += 10;
                }
                break;
            case TestStrategy.Passive: bonus -= 5; break;
        }

        // 阶段3（冲刺）
        switch (phase3)
        {
            case TestStrategy.Conservative: bonus += 5; break;
            case TestStrategy.Aggressive: bonus += 10; break;
            case TestStrategy.Passive: bonus -= 15; break;
        }

        return Mathf.Clamp(baseScore + bonus, 0, 100);
    }

    // ========== 力量评分 ==========

    /// <summary>
    /// 力量测试评分（引体向上/仰卧起坐）
    /// </summary>
    private int CalculateStrengthScore(int physique, TestStrategy start, TestStrategy mid, TestStrategy end)
    {
        int baseScore;
        if (physique >= 120) baseScore = 80;
        else if (physique >= 80) baseScore = 65;
        else if (physique >= 50) baseScore = 50;
        else baseScore = 30;

        int bonus = 0;

        // 开始（标准/快速/等待）
        switch (start)
        {
            case TestStrategy.Conservative: bonus += 5; break;
            case TestStrategy.Aggressive: bonus += 10; break;
            case TestStrategy.Passive: bonus -= 5; break;
        }

        // 中段（坚持/休息/放弃）
        switch (mid)
        {
            case TestStrategy.Conservative: bonus += 5; break;
            case TestStrategy.Aggressive:
                // 坚持：70%成功+10, 30%力竭-10
                if (UnityEngine.Random.value < 0.7f) bonus += 10;
                else bonus -= 10;
                break;
            case TestStrategy.Passive: bonus -= 10; break;
        }

        // 结尾（再做一个/安全停/找人帮忙作弊）
        switch (end)
        {
            case TestStrategy.Conservative: bonus += 3; break;
            case TestStrategy.Aggressive:
                // 找人帮忙：70%成功+2+guilt+3, 30%被发现重测-5
                if (UnityEngine.Random.value < 0.7f)
                {
                    bonus += 2;
                    if (PlayerAttributes.Instance != null)
                        PlayerAttributes.Instance.Guilt += 3;
                }
                else
                {
                    bonus -= 5;
                    if (PlayerAttributes.Instance != null)
                        PlayerAttributes.Instance.Mood -= 5;
                }
                break;
            case TestStrategy.Passive: bonus -= 5; break;
        }

        return Mathf.Clamp(baseScore + bonus, 0, 100);
    }

    // ========== 立定跳远评分 ==========

    /// <summary>
    /// 立定跳远评分（3次尝试取最好）
    /// 基础距离 = physique * 1.5 + 50 cm
    /// </summary>
    private int CalculateJumpScore(int physique, TestStrategy attempt1, TestStrategy attempt2, TestStrategy attempt3)
    {
        float baseDistance = physique * 1.5f + 50f; // cm

        float[] distances = new float[3];
        TestStrategy[] attempts = { attempt1, attempt2, attempt3 };

        for (int i = 0; i < 3; i++)
        {
            switch (attempts[i])
            {
                case TestStrategy.Conservative:
                    // 正常跳：基础距离 ± 5cm
                    distances[i] = baseDistance + UnityEngine.Random.Range(-5f, 5f);
                    break;
                case TestStrategy.Aggressive:
                    // 深蹲发力跳：70%成功+15cm, 30%犯规0分
                    if (UnityEngine.Random.value < 0.7f)
                        distances[i] = baseDistance + 15f;
                    else
                        distances[i] = 0f; // 犯规
                    break;
                case TestStrategy.Passive:
                    // 随意跳：-20cm
                    distances[i] = baseDistance - 20f;
                    break;
            }
        }

        // 取最好成绩
        float bestDistance = Mathf.Max(distances[0], Mathf.Max(distances[1], distances[2]));

        // 距离→分数映射
        // 男: 263cm=100, 248cm=90, 208cm=60, <208cm按比例
        // 简化映射
        int score;
        if (bestDistance >= 263) score = 100;
        else if (bestDistance >= 248) score = 90;
        else if (bestDistance >= 228) score = 75;
        else if (bestDistance >= 208) score = 60;
        else if (bestDistance >= 188) score = 40;
        else score = 20;

        return score;
    }

    // ========== 效果应用 ==========

    private void ApplyTestEffects(PhysicalTestResult result)
    {
        if (PlayerAttributes.Instance == null) return;

        switch (result.grade)
        {
            case "Excellent":
                PlayerAttributes.Instance.Mood += 5;
                Debug.Log("[PhysicalTestSystem] 优秀！心情+5");
                break;
            case "Good":
                // 良好，无额外效果
                break;
            case "Pass":
                PlayerAttributes.Instance.Stress += 3;
                Debug.Log("[PhysicalTestSystem] 及格，压力+3");
                break;
            case "Fail":
                PlayerAttributes.Instance.Stress += 10;
                PlayerAttributes.Instance.Mood -= 8;
                Debug.Log("[PhysicalTestSystem] 不及格！压力+10，心情-8");
                break;
        }
    }
}
