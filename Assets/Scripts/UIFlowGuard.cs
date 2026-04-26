using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 主流程 UI 兜底工具，负责恢复输入环境并关闭跨场景残留的阻塞界面。
/// </summary>
public static class UIFlowGuard
{
    public static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            return;
        }

        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    public static void RestoreInteractiveState()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public static void CleanupBlockingUI()
    {
        RestoreInteractiveState();
        EnsureEventSystem();

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen)
        {
            PauseMenuUI.Instance.Close();
        }

        if (ConfirmDialogUI.Instance != null && ConfirmDialogUI.Instance.IsOpen)
        {
            ConfirmDialogUI.Instance.Hide();
        }

        if (NewsSystem.Instance != null && NewsSystem.Instance.IsShowing)
        {
            NewsSystem.Instance.DismissNews();
        }

        if (AchievementUI.Instance != null && AchievementUI.Instance.isReviewShowing)
        {
            AchievementUI.Instance.HideReviewPanelImmediate();
        }

        if (PhysicalTestUI.Instance != null && PhysicalTestUI.Instance.IsOpen)
        {
            PhysicalTestUI.Instance.Hide();
        }

        if (TalentUI.Instance != null && TalentUI.Instance.IsOpen)
        {
            TalentUI.Instance.ClosePanel();
        }

        if (InfoPanelManager.Instance != null && InfoPanelManager.Instance.IsOpen)
        {
            InfoPanelManager.Instance.ClosePanel();
        }

        if (SettingsUIBuilder.Instance != null && SettingsUIBuilder.Instance.IsOpen)
        {
            SettingsUIBuilder.HideSettings();
        }
    }
}
