#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 钟山台 —— 开发者调试控制台管理器
/// 激活方式: Ctrl+Shift+D 或连续输入三个波浪号 ~~~
/// </summary>
public class DebugConsoleManager : MonoBehaviour
{
    // ========== 单例 ==========
    public static DebugConsoleManager Instance { get; private set; }

    // ========== 状态 ==========
    private bool isOpen;
    private DebugConsoleUI consoleUI;

    // ========== 波浪号追踪 ==========
    private int tildeCount;
    private float lastTildeTime;
    private const float TildeTimeout = 0.5f;
    private const int TildeThreshold = 3;

    // ========== 快照系统 ==========
    private readonly Dictionary<string, string> snapshots = new Dictionary<string, string>();

    // ========== 日志系统 ==========
    private static readonly List<DebugLogEntry> logEntries = new List<DebugLogEntry>();

    /// <summary>新日志条目添加时触发</summary>
    public static event Action<DebugLogEntry> OnLogAdded;

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

    private void Update()
    {
        // 快捷键: Ctrl+Shift+D
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D))
        {
            Toggle();
            return;
        }

        // 波浪号追踪: 连续 3 个 ~ 激活
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            if (Time.unscaledTime - lastTildeTime > TildeTimeout)
            {
                tildeCount = 0;
            }

            tildeCount++;
            lastTildeTime = Time.unscaledTime;

            if (tildeCount >= TildeThreshold)
            {
                tildeCount = 0;
                Toggle();
            }
        }
    }

    // ========== 开关控制 ==========

    /// <summary>切换控制台显示/隐藏</summary>
    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    /// <summary>打开控制台</summary>
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (consoleUI == null)
        {
            GameObject uiObj = new GameObject("DebugConsoleUI");
            uiObj.transform.SetParent(transform, false);
            consoleUI = uiObj.AddComponent<DebugConsoleUI>();
        }

        DebugConsoleUI.Show();
        Log("系统", "钟山台已打开");
    }

    /// <summary>关闭控制台</summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        DebugConsoleUI.Hide();
        Log("系统", "钟山台已关闭");
    }

    /// <summary>控制台是否处于打开状态</summary>
    public bool IsOpen => isOpen;

    // ========== 快照系统 ==========

    /// <summary>保存当前游戏状态快照</summary>
    public void SaveSnapshot(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Log("快照", "快照名称不能为空");
            return;
        }

        var data = new SaveData();

        if (GameState.Instance != null)
            GameState.Instance.SaveToData(data);

        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.SaveToData(data);

        string json = JsonUtility.ToJson(data, true);
        snapshots[name] = json;
        Log("快照", $"已保存快照: {name}");
    }

    /// <summary>加载指定名称的快照</summary>
    public void LoadSnapshot(string name)
    {
        if (!snapshots.ContainsKey(name))
        {
            Log("快照", $"快照不存在: {name}");
            return;
        }

        string json = snapshots[name];
        var data = JsonUtility.FromJson<SaveData>(json);

        if (GameState.Instance != null)
            GameState.Instance.LoadFromData(data);

        if (PlayerAttributes.Instance != null)
            PlayerAttributes.Instance.LoadFromData(data);

        Log("快照", $"已加载快照: {name}");
    }

    /// <summary>删除指定名称的快照</summary>
    public void DeleteSnapshot(string name)
    {
        if (snapshots.Remove(name))
        {
            Log("快照", $"已删除快照: {name}");
        }
    }

    /// <summary>获取所有快照名称</summary>
    public List<string> GetSnapshotNames()
    {
        return new List<string>(snapshots.Keys);
    }

    // ========== 日志系统 ==========

    /// <summary>添加一条调试日志</summary>
    public static void Log(string category, string message)
    {
        var entry = new DebugLogEntry
        {
            timestamp = DateTime.Now.ToString("HH:mm:ss"),
            category = category,
            message = message
        };

        logEntries.Add(entry);

        // 限制日志条数，防止内存溢出
        if (logEntries.Count > 500)
        {
            logEntries.RemoveAt(0);
        }

        OnLogAdded?.Invoke(entry);
        Debug.Log($"[钟山台] [{category}] {message}");
    }

    /// <summary>获取所有日志条目</summary>
    public static List<DebugLogEntry> GetLogEntries()
    {
        return logEntries;
    }

    /// <summary>清空所有日志</summary>
    public static void ClearLogs()
    {
        logEntries.Clear();
    }
}

/// <summary>调试日志条目</summary>
[Serializable]
public struct DebugLogEntry
{
    public string timestamp;
    public string category;
    public string message;

    public override string ToString()
    {
        return $"[{timestamp}] [{category}] {message}";
    }
}
#endif
