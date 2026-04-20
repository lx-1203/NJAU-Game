using UnityEngine;
using System;

/// <summary>
/// 作弊系统 —— 管理考试中作弊的概率判定、惩罚执行与累计追踪
/// </summary>
public class CheatingSystem : MonoBehaviour, ISaveable
{
    // ========== 单例 ==========

    public static CheatingSystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>作弊尝试后触发，参数: true=被抓, false=成功偷看</summary>
    public event Action<bool> OnCheatAttempted;

    /// <summary>触发开除结局时触发</summary>
    public event Action OnExpulsionTriggered;

    // ========== 常量 ==========

    /// <summary>作弊被抓概率 (30%)</summary>
    public const float CatchProbability = 0.3f;

    /// <summary>累计被抓达到此次数触发开除</summary>
    public const int ExpulsionThreshold = 2;

    // ========== 未被抓时的惩罚 ==========
    private const int SuccessDarknessAdd = 5;
    private const int SuccessGuiltAdd = 8;

    // ========== 被抓时的惩罚 ==========
    private const int CaughtDarknessAdd = 10;
    private const int CaughtGuiltAdd = 15;
    private const int CaughtStressAdd = 20;

    // ========== 内部字段 ==========

    [Header("作弊记录")]
    [SerializeField] private int caughtCount = 0;
    [SerializeField] private int totalCheatAttempts = 0;

    // ========== 属性访问器 ==========

    /// <summary>被抓总次数</summary>
    public int CaughtCount => caughtCount;

    /// <summary>总作弊尝试次数</summary>
    public int TotalCheatAttempts => totalCheatAttempts;

    // ========== 核心方法 ==========

    /// <summary>
    /// 尝试作弊：30% 概率被抓。
    /// 无论结果都会修改玩家属性。
    /// </summary>
    /// <returns>CheatResult.Success 或 CheatResult.Caught</returns>
    public CheatResult AttemptCheat()
    {
        totalCheatAttempts++;

        bool caught = UnityEngine.Random.value < CatchProbability;

        if (caught)
        {
            // 被抓
            caughtCount++;
            Debug.Log($"[CheatingSystem] 作弊被抓！累计被抓次数: {caughtCount}");

            ApplyPenalty(CaughtDarknessAdd, CaughtGuiltAdd, CaughtStressAdd);

            OnCheatAttempted?.Invoke(true);

            // 检查是否触发开除
            if (ShouldTriggerExpulsion())
            {
                Debug.Log("[CheatingSystem] 累计被抓次数达到上限，触发开除！");
                OnExpulsionTriggered?.Invoke();
            }

            return CheatResult.Caught;
        }
        else
        {
            // 未被抓
            Debug.Log("[CheatingSystem] 作弊成功，未被发现");

            ApplyPenalty(SuccessDarknessAdd, SuccessGuiltAdd, 0);

            OnCheatAttempted?.Invoke(false);

            return CheatResult.Success;
        }
    }

    /// <summary>
    /// 是否应触发开除结局（累计被抓 >= 2 次）
    /// </summary>
    public bool ShouldTriggerExpulsion()
    {
        return caughtCount >= ExpulsionThreshold;
    }

    // ========== 内部方法 ==========

    /// <summary>
    /// 应用属性惩罚
    /// </summary>
    private void ApplyPenalty(int darknessAmount, int guiltAmount, int stressAmount)
    {
        if (PlayerAttributes.Instance == null)
        {
            Debug.LogWarning("[CheatingSystem] PlayerAttributes 实例不存在，无法应用惩罚");
            return;
        }

        if (darknessAmount > 0)
            PlayerAttributes.Instance.AddAttribute("黑暗值", darknessAmount);

        if (guiltAmount > 0)
            PlayerAttributes.Instance.AddAttribute("负罪感", guiltAmount);

        if (stressAmount > 0)
            PlayerAttributes.Instance.AddAttribute("压力", stressAmount);
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

    // ========== ISaveable 实现 ==========

    public void SaveToData(SaveData data)
    {
        data.cheatCaughtCount = caughtCount;
        data.cheatTotalAttempts = totalCheatAttempts;
    }

    public void LoadFromData(SaveData data)
    {
        caughtCount = data.cheatCaughtCount;
        totalCheatAttempts = data.cheatTotalAttempts;
    }
}
