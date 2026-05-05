using UnityEngine;

/// <summary>
/// 统一管理游戏结局触发流程，避免各系统各自弹 UI 导致状态不一致。
/// </summary>
public class GameEndingManager : MonoBehaviour
{
    public static GameEndingManager Instance { get; private set; }

    public bool IsEndingActive => isEndingActive;

    private bool isEndingActive;

    private const string AnyEndingAchievementId = "ACH_021";
    private const string ExpelledEndingAchievementId = "ACH_022";

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 统一触发结局展示与周目收尾逻辑。
    /// </summary>
    public bool TriggerEnding(string reason = null)
    {
        if (isEndingActive)
        {
            Debug.Log($"[GameEndingManager] 结局流程已在进行中，忽略重复触发: {reason}");
            return false;
        }

        if (EndingDeterminer.Instance == null)
        {
            Debug.LogError($"[GameEndingManager] EndingDeterminer 未初始化，无法触发结局: {reason}");
            ShowEndingNotification("无法触发结局", "结局判定器尚未准备好，这次流程没法继续收束。", new Color(0.82f, 0.38f, 0.30f), 3f);
            return false;
        }

        UIFlowGuard.CleanupBlockingUI();
        Time.timeScale = 1f;

        EndingResult result = EndingDeterminer.Instance.DetermineEnding();
        return TriggerEnding(result, reason);
    }

    /// <summary>
    /// 使用已计算的结果直接触发结局。
    /// </summary>
    public bool TriggerEnding(EndingResult result, string reason = null)
    {
        if (isEndingActive)
        {
            Debug.Log($"[GameEndingManager] 结局流程已在进行中，忽略重复触发: {reason}");
            return false;
        }

        if (result == null || result.ending == null)
        {
            Debug.LogError($"[GameEndingManager] 结局结果为空，无法展示: {reason}");
            ShowEndingNotification("结局未完成", "这段流程暂时没能生成有效的结局结果。", new Color(0.82f, 0.38f, 0.30f), 3f);
            return false;
        }

        isEndingActive = true;

        UIFlowGuard.CleanupBlockingUI();
        Time.timeScale = 1f;
        TerminateGameplayFlowsForEnding();

        if (NewGamePlusManager.Instance != null)
        {
            NewGamePlusManager.Instance.RecordEndOfCycle(result.ending.id);
        }

        UnlockEndingAchievements(result);

        if (EndingUI.Instance == null)
        {
            GameObject uiObj = new GameObject("EndingUI");
            uiObj.AddComponent<EndingUI>();
        }

        Debug.Log($"[GameEndingManager] 触发结局: {result.ending.name} ({result.ending.id})，原因: {reason}");
        ShowEndingNotification(
            "结局达成",
            string.IsNullOrWhiteSpace(reason) ? $"即将进入结局《{result.ending.name}》。" : $"即将进入结局《{result.ending.name}》。\n触发原因：{reason}",
            new Color(0.86f, 0.62f, 0.24f),
            3.4f);
        EndingUI.Instance.Show(result, reason);
        return true;
    }

    public bool TriggerSpecificEnding(string endingId, string reason = null)
    {
        if (EndingDeterminer.Instance == null)
        {
            Debug.LogError($"[GameEndingManager] EndingDeterminer 未初始化，无法定向触发结局: {endingId}");
            ShowEndingNotification("无法触发结局", "结局系统尚未准备好，当前不能定向进入结局。", new Color(0.82f, 0.38f, 0.30f), 3f);
            return false;
        }

        EndingDefinition ending = EndingDeterminer.Instance.GetEndingById(endingId);
        if (ending == null)
        {
            Debug.LogError($"[GameEndingManager] 未找到指定结局: {endingId}");
            ShowEndingNotification("结局不存在", $"没有找到编号为 {endingId} 的结局定义。", new Color(0.82f, 0.38f, 0.30f), 3f);
            return false;
        }

        EndingResult result = EndingDeterminer.Instance.BuildResultForEnding(ending);
        return TriggerEnding(result, reason ?? $"Debug specific ending: {endingId}");
    }

    public void ResetEndingState()
    {
        isEndingActive = false;
    }

    private void TerminateGameplayFlowsForEnding()
    {
        if (ExamUIManager.Instance != null && ExamUIManager.Instance.IsExamActive)
        {
            ExamUIManager.Instance.ForceTerminateSequence();
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive)
        {
            DialogueSystem.Instance.SendMessage("EndDialogue", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void UnlockEndingAchievements(EndingResult result)
    {
        if (AchievementSystem.Instance == null || result == null || result.ending == null)
        {
            return;
        }

        AchievementSystem.Instance.UnlockAchievementById(AnyEndingAchievementId);

        if (result.ending.id == "END_004")
        {
            AchievementSystem.Instance.UnlockAchievementById(ExpelledEndingAchievementId);
        }
    }

    private void ShowEndingNotification(string title, string message, Color color, float duration)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, color, duration);
        }
    }
}
