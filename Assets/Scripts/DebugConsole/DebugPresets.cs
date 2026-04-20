#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 调试快捷增减器 —— 对指定属性进行 +/- 步长调节
/// 替代旧的固定预设系统，支持可切换步长 (1/5/10/25)
/// </summary>
public static class DebugPresets
{
    // ========== 步长档位 ==========
    private static readonly int[] StepOptions = { 1, 5, 10, 25 };
    private static int currentStepIndex = 1; // 默认步长 5

    /// <summary>当前步长值</summary>
    public static int CurrentStep => StepOptions[currentStepIndex];

    /// <summary>所有可选步长</summary>
    public static int[] GetStepOptions() => StepOptions;

    /// <summary>当前步长索引</summary>
    public static int CurrentStepIndex => currentStepIndex;

    /// <summary>切换到指定步长索引</summary>
    public static void SetStepIndex(int index)
    {
        currentStepIndex = Mathf.Clamp(index, 0, StepOptions.Length - 1);
        DebugConsoleManager.Log("增减", $"步长切换 → {CurrentStep}");
    }

    // ========== 属性增减 ==========

    /// <summary>
    /// 对指定属性增减当前步长
    /// </summary>
    /// <param name="attrName">属性中文名 (学力/魅力/体魄/领导力/压力/心情/黑暗值/负罪感/幸运/金钱)</param>
    /// <param name="positive">true=加, false=减</param>
    public static void AdjustAttribute(string attrName, bool positive)
    {
        int delta = positive ? CurrentStep : -CurrentStep;

        if (attrName == "金钱")
        {
            if (GameState.Instance != null)
            {
                int moneyDelta = positive ? CurrentStep * 100 : -CurrentStep * 100;
                GameState.Instance.Money += moneyDelta;
                DebugConsoleManager.Log("增减", $"金钱 {(moneyDelta >= 0 ? "+" : "")}{moneyDelta} → {GameState.Instance.Money}");
            }
            return;
        }

        if (PlayerAttributes.Instance == null) return;

        var pa = PlayerAttributes.Instance;
        int oldVal, newVal;

        switch (attrName)
        {
            case "学力":
                oldVal = pa.Study;
                pa.Study = Mathf.Clamp(pa.Study + delta, 0, 100);
                newVal = pa.Study;
                break;
            case "魅力":
                oldVal = pa.Charm;
                pa.Charm = Mathf.Clamp(pa.Charm + delta, 0, 100);
                newVal = pa.Charm;
                break;
            case "体魄":
                oldVal = pa.Physique;
                pa.Physique = Mathf.Clamp(pa.Physique + delta, 0, 100);
                newVal = pa.Physique;
                break;
            case "领导力":
                oldVal = pa.Leadership;
                pa.Leadership = Mathf.Clamp(pa.Leadership + delta, 0, 100);
                newVal = pa.Leadership;
                break;
            case "压力":
                oldVal = pa.Stress;
                pa.Stress = Mathf.Clamp(pa.Stress + delta, 0, 100);
                newVal = pa.Stress;
                break;
            case "心情":
                oldVal = pa.Mood;
                pa.Mood = Mathf.Clamp(pa.Mood + delta, 0, 100);
                newVal = pa.Mood;
                break;
            case "黑暗值":
                oldVal = pa.Darkness;
                pa.Darkness = Mathf.Clamp(pa.Darkness + delta, 0, 100);
                newVal = pa.Darkness;
                break;
            case "负罪感":
                oldVal = pa.Guilt;
                pa.Guilt = Mathf.Clamp(pa.Guilt + delta, 0, 100);
                newVal = pa.Guilt;
                break;
            case "幸运":
                oldVal = pa.Luck;
                pa.Luck = Mathf.Clamp(pa.Luck + delta, 0, 100);
                newVal = pa.Luck;
                break;
            default:
                Debug.LogWarning($"[DebugPresets] 未知属性: {attrName}");
                return;
        }

        DebugConsoleManager.Log("增减", $"{attrName} {(delta >= 0 ? "+" : "")}{delta} → {newVal}");
    }

    /// <summary>获取属性当前值</summary>
    public static int GetAttributeValue(string attrName)
    {
        if (attrName == "金钱")
            return GameState.Instance != null ? GameState.Instance.Money : 0;

        if (PlayerAttributes.Instance == null) return 0;
        var pa = PlayerAttributes.Instance;

        switch (attrName)
        {
            case "学力":   return pa.Study;
            case "魅力":   return pa.Charm;
            case "体魄":   return pa.Physique;
            case "领导力": return pa.Leadership;
            case "压力":   return pa.Stress;
            case "心情":   return pa.Mood;
            case "黑暗值": return pa.Darkness;
            case "负罪感": return pa.Guilt;
            case "幸运":   return pa.Luck;
            default:       return 0;
        }
    }
}
#endif
