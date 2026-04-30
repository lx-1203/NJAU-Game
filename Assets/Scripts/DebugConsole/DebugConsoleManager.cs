#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugConsoleManager : MonoBehaviour
{
    public static DebugConsoleManager Instance { get; private set; }

    private bool isOpen;
    private DebugConsoleUI consoleUI;

    private int tildeCount;
    private float lastTildeTime;
    private const float TildeTimeout = 0.5f;
    private const int TildeThreshold = 3;

    private readonly Dictionary<string, string> snapshots = new Dictionary<string, string>();
    private static readonly List<DebugLogEntry> logEntries = new List<DebugLogEntry>();

    public static event Action<DebugLogEntry> OnLogAdded;

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
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D))
        {
            Toggle();
            return;
        }

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

    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        if (consoleUI == null)
        {
            GameObject uiObject = new GameObject("DebugConsoleUI");
            uiObject.transform.SetParent(transform, false);
            consoleUI = uiObject.AddComponent<DebugConsoleUI>();
        }

        DebugConsoleUI.Show();
        Log("System", "Debug console opened");
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        DebugConsoleUI.Hide();
        Log("System", "Debug console closed");
    }

    public bool IsOpen => isOpen;

    public void SaveSnapshot(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Log("Snapshot", "Snapshot name cannot be empty");
            return;
        }

        SaveData data = new SaveData();
        if (GameState.Instance != null)
        {
            GameState.Instance.SaveToData(data);
        }

        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.SaveToData(data);
        }

        snapshots[name] = JsonUtility.ToJson(data, true);
        Log("Snapshot", $"Saved snapshot: {name}");
    }

    public void LoadSnapshot(string name)
    {
        if (!snapshots.TryGetValue(name, out string json))
        {
            Log("Snapshot", $"Snapshot not found: {name}");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (GameState.Instance != null)
        {
            GameState.Instance.LoadFromData(data);
        }

        if (PlayerAttributes.Instance != null)
        {
            PlayerAttributes.Instance.LoadFromData(data);
        }

        Log("Snapshot", $"Loaded snapshot: {name}");
    }

    public void DeleteSnapshot(string name)
    {
        if (snapshots.Remove(name))
        {
            Log("Snapshot", $"Deleted snapshot: {name}");
        }
    }

    public List<string> GetSnapshotNames()
    {
        return new List<string>(snapshots.Keys);
    }

    public static void Log(string category, string message)
    {
        DebugLogEntry entry = new DebugLogEntry
        {
            timestamp = DateTime.Now.ToString("HH:mm:ss"),
            category = category,
            message = message
        };

        logEntries.Add(entry);
        if (logEntries.Count > 500)
        {
            logEntries.RemoveAt(0);
        }

        OnLogAdded?.Invoke(entry);
        Debug.Log($"[ZhongshanDeck] [{category}] {message}");
    }

    public static List<DebugLogEntry> GetLogEntries()
    {
        return logEntries;
    }

    public static void ClearLogs()
    {
        logEntries.Clear();
    }
}

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
