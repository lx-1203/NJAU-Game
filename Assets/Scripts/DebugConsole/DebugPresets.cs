#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public static class DebugPresets
{
    private static readonly int[] StepOptions = { 1, 5, 10, 25 };
    private static readonly Dictionary<string, Func<int>> Getters = new Dictionary<string, Func<int>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Study"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Study : 0,
        ["Charm"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Charm : 0,
        ["Physique"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Physique : 0,
        ["Leadership"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Leadership : 0,
        ["Stress"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Stress : 0,
        ["Mood"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Mood : 0,
        ["Darkness"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Darkness : 0,
        ["Guilt"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Guilt : 0,
        ["Luck"] = () => PlayerAttributes.Instance != null ? PlayerAttributes.Instance.Luck : 0,
        ["Money"] = () => GameState.Instance != null ? GameState.Instance.Money : 0,
    };

    private static readonly Dictionary<string, Action<int>> Setters = new Dictionary<string, Action<int>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Study"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Study = Mathf.Clamp(value, 0, 999); },
        ["Charm"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Charm = Mathf.Clamp(value, 0, 999); },
        ["Physique"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Physique = Mathf.Clamp(value, 0, 999); },
        ["Leadership"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Leadership = Mathf.Clamp(value, 0, 999); },
        ["Stress"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Stress = Mathf.Clamp(value, 0, PlayerAttributes.MaxStatusValue); },
        ["Mood"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Mood = Mathf.Clamp(value, 0, PlayerAttributes.MaxStatusValue); },
        ["Darkness"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Darkness = Mathf.Clamp(value, 0, 999); },
        ["Guilt"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Guilt = Mathf.Clamp(value, 0, PlayerAttributes.MaxStatusValue); },
        ["Luck"] = value => { if (PlayerAttributes.Instance != null) PlayerAttributes.Instance.Luck = Mathf.Clamp(value, 0, PlayerAttributes.MaxStatusValue); },
        ["Money"] = value => { if (GameState.Instance != null) GameState.Instance.Money = value; },
    };

    public static int CurrentStep => StepOptions[CurrentStepIndex];
    public static int CurrentStepIndex => ZhongshanDeckToolStateBridge.GetStepIndex(1);

    public static int[] GetStepOptions()
    {
        return StepOptions;
    }

    public static void SetStepIndex(int index)
    {
        ZhongshanDeckToolStateBridge.SetStepIndex(index);
        DebugConsoleManager.Log("Adjust", $"Step -> {CurrentStep}");
    }

    public static void AdjustAttribute(string attrName, bool positive)
    {
        if (!TryResolveKey(attrName, out string canonicalKey))
        {
            Debug.LogWarning($"[DebugPresets] Unknown attribute: {attrName}");
            return;
        }

        int delta = canonicalKey == "Money"
            ? (positive ? CurrentStep * 100 : -CurrentStep * 100)
            : (positive ? CurrentStep : -CurrentStep);

        SetAttributeValue(canonicalKey, GetAttributeValue(canonicalKey) + delta);
        DebugConsoleManager.Log("Adjust", $"{canonicalKey} {(delta >= 0 ? "+" : string.Empty)}{delta} -> {GetAttributeValue(canonicalKey)}");
    }

    public static int GetAttributeValue(string attrName)
    {
        return TryResolveKey(attrName, out string canonicalKey) && Getters.TryGetValue(canonicalKey, out Func<int> getter)
            ? getter()
            : 0;
    }

    public static void SetAttributeValue(string attrName, int value)
    {
        if (!TryResolveKey(attrName, out string canonicalKey))
        {
            Debug.LogWarning($"[DebugPresets] Unknown attribute: {attrName}");
            return;
        }

        if (Setters.TryGetValue(canonicalKey, out Action<int> setter))
        {
            setter(value);
        }
    }

    private static bool TryResolveKey(string attrName, out string canonicalKey)
    {
        canonicalKey = attrName;
        if (string.IsNullOrWhiteSpace(attrName))
        {
            return false;
        }

        switch (attrName.Trim())
        {
            case "Study":
            case "学力":
                canonicalKey = "Study";
                return true;
            case "Charm":
            case "魅力":
                canonicalKey = "Charm";
                return true;
            case "Physique":
            case "体魄":
                canonicalKey = "Physique";
                return true;
            case "Leadership":
            case "领导力":
                canonicalKey = "Leadership";
                return true;
            case "Stress":
            case "压力":
                canonicalKey = "Stress";
                return true;
            case "Mood":
            case "心情":
                canonicalKey = "Mood";
                return true;
            case "Darkness":
            case "黑暗值":
                canonicalKey = "Darkness";
                return true;
            case "Guilt":
            case "负罪感":
                canonicalKey = "Guilt";
                return true;
            case "Luck":
            case "幸运":
                canonicalKey = "Luck";
                return true;
            case "Money":
            case "金钱":
                canonicalKey = "Money";
                return true;
            default:
                return false;
        }
    }
}
#endif
