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
        EndingUI.Instance.Show(result, reason);
        return true;
    }

    public bool TriggerSpecificEnding(string endingId, string reason = null)
    {
        if (EndingDeterminer.Instance == null)
        {
            Debug.LogError($"[GameEndingManager] EndingDeterminer 未初始化，无法定向触发结局: {endingId}");
            return false;
        }

        EndingDefinition ending = EndingDeterminer.Instance.GetEndingById(endingId);
        if (ending == null)
        {
            Debug.LogError($"[GameEndingManager] 未找到指定结局: {endingId}");
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
}
