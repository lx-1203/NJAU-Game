#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 调试预设集 —— 8 种快速设定角色状态的预设方案
/// </summary>
public static class DebugPresets
{
    /// <summary>满属性: 全满，无压力，金钱充裕</summary>
    public static void ApplyMax()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(100, 100, 100, 100, 0, 100, 0, 0, 100);

        if (GameState.Instance != null)
            GameState.Instance.Money = 99999;

        DebugConsoleManager.Log("预设", "已应用预设: 满属性");
    }

    /// <summary>巅峰: 高属性、低压力、高心情</summary>
    public static void ApplyPeak()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(80, 80, 80, 80, 10, 90, 0, 0, 70);

        if (GameState.Instance != null)
            GameState.Instance.Money = 20000;

        DebugConsoleManager.Log("预设", "已应用预设: 巅峰");
    }

    /// <summary>黑暗: 极低属性、极高压力、极低心情、满黑暗值</summary>
    public static void ApplyDark()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(5, 5, 5, 5, 95, 5, 100, 80, 10);

        if (GameState.Instance != null)
            GameState.Instance.Money = 0;

        DebugConsoleManager.Log("预设", "已应用预设: 黑暗");
    }

    /// <summary>摆烂: 低属性、中高压力、低心情</summary>
    public static void ApplySlacker()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(20, 20, 20, 20, 60, 30, 30, 20, 40);

        if (GameState.Instance != null)
            GameState.Instance.Money = 500;

        DebugConsoleManager.Log("预设", "已应用预设: 摆烂");
    }

    /// <summary>贫困: 中低属性、中等压力、低金钱</summary>
    public static void ApplyPoor()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(30, 30, 30, 30, 50, 40, 10, 10, 30);

        if (GameState.Instance != null)
            GameState.Instance.Money = 0;

        DebugConsoleManager.Log("预设", "已应用预设: 贫困");
    }

    /// <summary>恋爱: 高魅力、高心情、低压力</summary>
    public static void ApplyRomance()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(50, 90, 50, 50, 5, 95, 0, 0, 80);

        if (GameState.Instance != null)
            GameState.Instance.Money = 5000;

        DebugConsoleManager.Log("预设", "已应用预设: 恋爱");
    }

    /// <summary>新手: 重置为初始默认值</summary>
    public static void ApplyNewbie()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(10, 5, 8, 3, 20, 70, 0, 0, 50);

        if (GameState.Instance != null)
            GameState.Instance.Money = 8000;

        DebugConsoleManager.Log("预设", "已应用预设: 新手");
    }

    /// <summary>空白: 全部归零</summary>
    public static void ApplyBlank()
    {
        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SetAll(0, 0, 0, 0, 0, 0, 0, 0, 0);

        if (GameState.Instance != null)
            GameState.Instance.Money = 0;

        DebugConsoleManager.Log("预设", "已应用预设: 空白");
    }
}
#endif
