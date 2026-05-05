using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在开启全新周目时清理上一局残留的常驻运行态，确保 GameScene 能按新局重新初始化。
/// </summary>
public static class GameplaySessionReset
{
    private static readonly HashSet<Type> ResettableTypes = new HashSet<Type>
    {
        typeof(ActionSystem),
        typeof(AffinitySystem),
        typeof(CampusRunSystem),
        typeof(ClubSystem),
        typeof(ConfirmDialogUI),
        typeof(ConfessionSystem),
        typeof(CourseScheduleUI),
        typeof(DebtSystem),
        typeof(DialogueSystem),
        typeof(EconomyManager),
        typeof(EndingDeterminer),
        typeof(EventExecutor),
        typeof(EventHistory),
        typeof(EventScheduler),
        typeof(ExamSystem),
        typeof(ExamUIManager),
        typeof(GameEndingManager),
        typeof(GameState),
        typeof(InfoPanelManager),
        typeof(InventorySystem),
        typeof(InventoryUIManager),
        typeof(JobSelectionUI),
        typeof(JobSystem),
        typeof(LocationManager),
        typeof(MissionUI),
        typeof(NPCDatabase),
        typeof(NPCEventHub),
        typeof(NPCManager),
        typeof(NewsSystem),
        typeof(PenaltySystem),
        typeof(PhysicalTestSystem),
        typeof(PhysicalTestUI),
        typeof(PlayerAttributes),
        typeof(RomanceBridge),
        typeof(RomanceSystem),
        typeof(SemesterSummarySystem),
        typeof(SettingsUIBuilder),
        typeof(ShopSystem),
        typeof(TalentSystem),
        typeof(TalentUI),
        typeof(TurnManager),
        typeof(CheatingSystem)
    };

    public static void ResetForFreshGame()
    {
        UIFlowGuard.CleanupBlockingUI();
        SaveManager.PendingLoadData = null;
        SaveManager.PendingLoadSlot = -1;

        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        HashSet<GameObject> targets = new HashSet<GameObject>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            if (!ResettableTypes.Contains(behaviour.GetType()))
            {
                continue;
            }

            targets.Add(behaviour.gameObject);
        }

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                UnityEngine.Object.Destroy(target);
            }
        }
    }
}
