using UnityEngine;

public static class UIBackActionRouter
{
    public static bool TryHandleBackAction()
    {
        if (ConfirmDialogUI.Instance != null && ConfirmDialogUI.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            ConfirmDialogUI.Instance.Hide();
            return true;
        }

        SaveLoadUI saveLoadUI = Object.FindObjectOfType<SaveLoadUI>();
        if (saveLoadUI != null)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            Object.Destroy(saveLoadUI.gameObject);
            return true;
        }

        if (SettingsUIBuilder.Instance != null && SettingsUIBuilder.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            SettingsUIBuilder.HideSettings();
            return true;
        }

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            PauseMenuUI.Instance.Close();
            return true;
        }

        if (DebugConsoleManager.Instance != null && DebugConsoleManager.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            DebugConsoleManager.Instance.Close();
            return true;
        }

        if (NPCInteractionMenu.Instance != null && NPCInteractionMenu.Instance.IsMenuOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            NPCInteractionMenu.Instance.CloseMenu();
            return true;
        }

        ClubPanelManager clubPanelManager = Object.FindObjectOfType<ClubPanelManager>();
        if (clubPanelManager != null && clubPanelManager.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            clubPanelManager.ClosePanel();
            return true;
        }

        if (InfoPanelManager.Instance != null && InfoPanelManager.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            InfoPanelManager.Instance.ClosePanel();
            return true;
        }

        if (TalentUI.Instance != null && TalentUI.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            TalentUI.Instance.ClosePanel();
            return true;
        }

        if (MissionPanelBuilder.Instance != null && MissionPanelBuilder.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            MissionPanelBuilder.Instance.ClosePanel();
            return true;
        }

        if (JobSelectionUI.Instance != null && JobSelectionUI.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            JobSelectionUI.Instance.Hide();
            return true;
        }

        if (PhysicalTestUI.Instance != null && PhysicalTestUI.Instance.IsOpen)
        {
            PauseMenuUI.MarkEscapeHandledThisFrame();
            PhysicalTestUI.Instance.Hide();
            return true;
        }

        return false;
    }
}
