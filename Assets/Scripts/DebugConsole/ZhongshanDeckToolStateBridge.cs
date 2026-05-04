using System.Collections.Generic;
using UnityEngine;

public static class ZhongshanDeckToolStateBridge
{
    private const string ResourcePath = "Debug/ZhongshanDeckToolState";
    private static ZhongshanDeckToolState cachedState;
    private static ZhongshanDeckToolState fallbackState;

    public static ZhongshanDeckToolState GetState()
    {
        if (cachedState != null)
        {
            cachedState.EnsureInitialized();
            return cachedState;
        }

        cachedState = Resources.Load<ZhongshanDeckToolState>(ResourcePath);
        if (cachedState == null)
        {
            if (fallbackState == null)
            {
                fallbackState = ScriptableObject.CreateInstance<ZhongshanDeckToolState>();
                fallbackState.hideFlags = HideFlags.HideAndDontSave;
                fallbackState.EnsureInitialized();
            }

            return fallbackState;
        }

        cachedState.EnsureInitialized();
        return cachedState;
    }

    public static int GetStepIndex(int fallback)
    {
        ZhongshanDeckToolState state = GetState();
        return state != null ? Mathf.Clamp(state.stepIndex, 0, 3) : Mathf.Clamp(fallback, 0, 3);
    }

    public static void SetStepIndex(int index)
    {
        ZhongshanDeckToolState state = GetState();
        state.stepIndex = Mathf.Clamp(index, 0, 3);
        SaveState();
    }

    public static List<string> GetSnapshotNames()
    {
        ZhongshanDeckToolState state = GetState();
        List<string> names = new List<string>();
        for (int i = 0; i < state.snapshots.Count; i++)
        {
            ZhongshanDeckSnapshotEntry entry = state.snapshots[i];
            if (entry != null && !string.IsNullOrWhiteSpace(entry.name))
            {
                names.Add(entry.name);
            }
        }

        return names;
    }

    public static bool TryGetSnapshotJson(string name, out string json)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.snapshots.Count; i++)
        {
            ZhongshanDeckSnapshotEntry entry = state.snapshots[i];
            if (entry != null && entry.name == name)
            {
                json = entry.json;
                return true;
            }
        }

        json = null;
        return false;
    }

    public static void SaveSnapshot(string name, string json)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.snapshots.Count; i++)
        {
            ZhongshanDeckSnapshotEntry entry = state.snapshots[i];
            if (entry != null && entry.name == name)
            {
                entry.json = json;
                SaveState();
                return;
            }
        }

        state.snapshots.Add(new ZhongshanDeckSnapshotEntry
        {
            name = name,
            json = json
        });
        SaveState();
    }

    public static List<ZhongshanDeckEventEntry> GetAuthoredEvents()
    {
        ZhongshanDeckToolState state = GetState();
        return new List<ZhongshanDeckEventEntry>(state.authoredEvents);
    }

    public static bool TryGetAuthoredEvent(string eventId, out ZhongshanDeckEventEntry entry)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.authoredEvents.Count; i++)
        {
            ZhongshanDeckEventEntry current = state.authoredEvents[i];
            if (current != null && current.eventId == eventId)
            {
                entry = current;
                return true;
            }
        }

        entry = null;
        return false;
    }

    public static void SaveAuthoredEvent(string eventId, string title, string json)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.authoredEvents.Count; i++)
        {
            ZhongshanDeckEventEntry current = state.authoredEvents[i];
            if (current != null && current.eventId == eventId)
            {
                current.title = title;
                current.json = json;
                SaveState();
                return;
            }
        }

        state.authoredEvents.Add(new ZhongshanDeckEventEntry
        {
            eventId = eventId,
            title = title,
            json = json
        });
        SaveState();
    }

    public static bool DeleteAuthoredEvent(string eventId)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.authoredEvents.Count; i++)
        {
            ZhongshanDeckEventEntry current = state.authoredEvents[i];
            if (current != null && current.eventId == eventId)
            {
                state.authoredEvents.RemoveAt(i);
                SaveState();
                return true;
            }
        }

        return false;
    }

    public static bool DeleteSnapshot(string name)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.snapshots.Count; i++)
        {
            ZhongshanDeckSnapshotEntry entry = state.snapshots[i];
            if (entry != null && entry.name == name)
            {
                state.snapshots.RemoveAt(i);
                SaveState();
                return true;
            }
        }

        return false;
    }

    public static bool TryGetMonthlyNewsOverride(int year, int semester, int round, out ZhongshanDeckNewsRoundEntry entry)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.monthlyNewsOverrides.Count; i++)
        {
            ZhongshanDeckNewsRoundEntry current = state.monthlyNewsOverrides[i];
            if (current != null &&
                current.year == year &&
                current.semester == semester &&
                current.round == round)
            {
                entry = CloneNewsRoundEntry(current);
                return true;
            }
        }

        entry = null;
        return false;
    }

    public static List<ZhongshanDeckNewsRoundEntry> GetMonthlyNewsOverrides()
    {
        ZhongshanDeckToolState state = GetState();
        List<ZhongshanDeckNewsRoundEntry> copies = new List<ZhongshanDeckNewsRoundEntry>();
        for (int i = 0; i < state.monthlyNewsOverrides.Count; i++)
        {
            ZhongshanDeckNewsRoundEntry current = state.monthlyNewsOverrides[i];
            if (current != null)
            {
                copies.Add(CloneNewsRoundEntry(current));
            }
        }

        return copies;
    }

    public static void SaveMonthlyNewsOverride(ZhongshanDeckNewsRoundEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.monthlyNewsOverrides.Count; i++)
        {
            ZhongshanDeckNewsRoundEntry current = state.monthlyNewsOverrides[i];
            if (current != null &&
                current.year == entry.year &&
                current.semester == entry.semester &&
                current.round == entry.round)
            {
                state.monthlyNewsOverrides[i] = CloneNewsRoundEntry(entry);
                SaveState();
                return;
            }
        }

        state.monthlyNewsOverrides.Add(CloneNewsRoundEntry(entry));
        SaveState();
    }

    public static bool DeleteMonthlyNewsOverride(int year, int semester, int round)
    {
        ZhongshanDeckToolState state = GetState();
        for (int i = 0; i < state.monthlyNewsOverrides.Count; i++)
        {
            ZhongshanDeckNewsRoundEntry current = state.monthlyNewsOverrides[i];
            if (current != null &&
                current.year == year &&
                current.semester == semester &&
                current.round == round)
            {
                state.monthlyNewsOverrides.RemoveAt(i);
                SaveState();
                return true;
            }
        }

        return false;
    }

    private static ZhongshanDeckNewsRoundEntry CloneNewsRoundEntry(ZhongshanDeckNewsRoundEntry source)
    {
        ZhongshanDeckNewsRoundEntry clone = new ZhongshanDeckNewsRoundEntry
        {
            year = source != null ? source.year : 1,
            semester = source != null ? source.semester : 1,
            round = source != null ? source.round : 1,
            items = new List<NewsItem>()
        };

        if (source?.items != null)
        {
            for (int i = 0; i < source.items.Count; i++)
            {
                NewsItem item = source.items[i];
                if (item != null)
                {
                    clone.items.Add(item.Clone());
                }
            }
        }

        return clone;
    }

    public static void SaveState()
    {
        ZhongshanDeckToolState state = GetState();
        state.EnsureInitialized();

#if UNITY_EDITOR
        if (state != null)
        {
            UnityEditor.EditorUtility.SetDirty(state);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
