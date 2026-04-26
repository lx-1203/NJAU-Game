using UnityEngine;

public static class UIBackActionRouter
{
    public static bool TryHandleBackAction()
    {
        if (ConfirmDialogUI.Instance != null && ConfirmDialogUI.Instance.IsOpen)
        {
            ConfirmDialogUI.Instance.Hide();
            return true;
        }

        SaveLoadUI saveLoadUI = Object.FindObjectOfType<SaveLoadUI>();
        if (saveLoadUI != null)
        {
            Object.Destroy(saveLoadUI.gameObject);
            return true;
        }

        if (SettingsUIBuilder.Instance != null && SettingsUIBuilder.Instance.IsOpen)
        {
            SettingsUIBuilder.HideSettings();
            return true;
        }

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen)
        {
            PauseMenuUI.Instance.Close();
            return true;
        }

        if (DebugConsoleManager.Instance != null && DebugConsoleManager.Instance.IsOpen)
        {
            DebugConsoleManager.Instance.Close();
            return true;
        }

        if (NPCInteractionMenu.Instance != null && NPCInteractionMenu.Instance.IsMenuOpen)
        {
            NPCInteractionMenu.Instance.CloseMenu();
            return true;
        }

        ClubPanelManager clubPanelManager = Object.FindObjectOfType<ClubPanelManager>();
        if (clubPanelManager != null && clubPanelManager.IsOpen)
        {
            clubPanelManager.ClosePanel();
            return true;
        }

        if (InfoPanelManager.Instance != null && InfoPanelManager.Instance.IsOpen)
        {
            InfoPanelManager.Instance.ClosePanel();
            return true;
        }

        if (TalentUI.Instance != null && TalentUI.Instance.IsOpen)
        {
            TalentUI.Instance.ClosePanel();
            return true;
        }

        if (MissionPanelBuilder.Instance != null && MissionPanelBuilder.Instance.IsOpen)
        {
            MissionPanelBuilder.Instance.ClosePanel();
            return true;
        }

        if (JobSelectionUI.Instance != null && JobSelectionUI.Instance.IsOpen)
        {
            JobSelectionUI.Instance.Hide();
            return true;
        }

        if (PhysicalTestUI.Instance != null && PhysicalTestUI.Instance.IsOpen)
        {
            PhysicalTestUI.Instance.Hide();
            return true;
        }

        return false;
    }
}
