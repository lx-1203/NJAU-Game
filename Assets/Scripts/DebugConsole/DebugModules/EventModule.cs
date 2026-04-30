#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color BtnGreen = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnRed = new Color(0.60f, 0.20f, 0.20f, 1.0f);

    private TMP_InputField eventIdInput;
    private TMP_InputField flagInput;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI queueText;
    private TextMeshProUGUI historyText;

    public void Init(RectTransform parent)
    {
        GameObject scrollObj = CreateUIElement("ScrollView", parent);
        StretchFull(scrollObj.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        CreateLabel(content.transform, "Event Debug", 18f, TextGold, 30f);
        statusText = CreateLabel(content.transform, string.Empty, 14f, TextGray, 44f);

        GameObject eventRow = CreateRow(content.transform, 34f);
        CreateLabel(eventRow.transform, "Event ID", 14f, TextWhite, 30f, 72f);
        eventIdInput = CreateInputField(eventRow.transform, "event_id", 260f, 30f);

        GameObject eventButtons = CreateRow(content.transform, 36f);
        CreateButton(eventButtons.transform, "Trigger", 120f, BtnGreen, ForceTriggerEvent);
        CreateButton(eventButtons.transform, "Skip", 120f, BtnRed, SkipEvent);

        GameObject flagRow = CreateRow(content.transform, 34f);
        CreateLabel(flagRow.transform, "Flag", 14f, TextWhite, 30f, 72f);
        flagInput = CreateInputField(flagRow.transform, "flag_name", 260f, 30f);

        GameObject flagButtons = CreateRow(content.transform, 36f);
        CreateButton(flagButtons.transform, "Set True", 100f, BtnGreen, () => ApplyFlag(true));
        CreateButton(flagButtons.transform, "Set False", 100f, BtnRed, () => ApplyFlag(false));

        queueText = CreateLabel(content.transform, string.Empty, 13f, TextWhite, 150f);
        historyText = CreateLabel(content.transform, string.Empty, 13f, TextWhite, 260f);
        queueText.enableWordWrapping = true;
        historyText.enableWordWrapping = true;
    }

    public void Refresh()
    {
        RefreshStatus();
        RefreshQueue();
        RefreshHistory();
    }

    private void ForceTriggerEvent()
    {
        string eventId = eventIdInput != null ? eventIdInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(eventId))
        {
            statusText.text = "Enter an event id";
            return;
        }

        if (EventScheduler.Instance == null)
        {
            statusText.text = "EventScheduler not ready";
            return;
        }

        EventScheduler.Instance.EnqueueEvent(eventId);
        statusText.text = $"Queued trigger: {eventId}";
        DebugConsoleManager.Log("Event", $"Force trigger {eventId}");
        Refresh();
    }

    private void SkipEvent()
    {
        string eventId = eventIdInput != null ? eventIdInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(eventId))
        {
            statusText.text = "Enter an event id";
            return;
        }

        if (EventHistory.Instance == null)
        {
            statusText.text = "EventHistory not ready";
            return;
        }

        EventHistory.Instance.RecordEvent(eventId, -1);
        statusText.text = $"Marked skipped: {eventId}";
        DebugConsoleManager.Log("Event", $"Skip event {eventId}");
        Refresh();
    }

    private void ApplyFlag(bool value)
    {
        string flag = flagInput != null ? flagInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(flag))
        {
            statusText.text = "Enter a flag name";
            return;
        }

        if (EventHistory.Instance == null)
        {
            statusText.text = "EventHistory not ready";
            return;
        }

        EventHistory.Instance.SetFlag(flag, value);
        statusText.text = $"Flag {flag} = {value}";
        DebugConsoleManager.Log("Event", $"Flag {flag} -> {value}");
        Refresh();
    }

    private void RefreshStatus()
    {
        int loaded = EventScheduler.Instance != null ? EventScheduler.Instance.GetLoadedEventCount() : 0;
        int queued = EventScheduler.Instance != null ? EventScheduler.Instance.GetPendingEventCount() : 0;
        int historyCount = EventHistory.Instance != null ? EventHistory.Instance.GetAllRecords().Count : 0;
        int flagCount = EventHistory.Instance != null ? EventHistory.Instance.GetAllFlagsSnapshot().Count : 0;
        int darkness = EventHistory.Instance != null ? EventHistory.Instance.DarknessValue : 0;

        statusText.text =
            $"Loaded {loaded}   Queue {queued}   History {historyCount}   Flags {flagCount}\n" +
            $"Darkness {darkness}";
    }

    private void RefreshQueue()
    {
        if (EventScheduler.Instance == null)
        {
            queueText.text = "Queue: EventScheduler not ready";
            return;
        }

        List<EventDefinition> pending = EventScheduler.Instance.GetPendingEventsSnapshot();
        if (pending.Count == 0)
        {
            queueText.text = "Queue: empty";
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Queue");
        for (int i = 0; i < pending.Count; i++)
        {
            EventDefinition evt = pending[i];
            builder.AppendLine($"{i + 1}. {evt.id} | {evt.title}");
        }
        queueText.text = builder.ToString().TrimEnd();
    }

    private void RefreshHistory()
    {
        if (EventHistory.Instance == null)
        {
            historyText.text = "History: EventHistory not ready";
            return;
        }

        List<EventHistory.EventRecord> records = EventHistory.Instance.GetAllRecords();
        Dictionary<string, bool> flags = EventHistory.Instance.GetAllFlagsSnapshot();
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Recent Events");

        if (records.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            int start = Mathf.Max(0, records.Count - 6);
            for (int i = start; i < records.Count; i++)
            {
                EventHistory.EventRecord record = records[i];
                builder.AppendLine($"- {record.eventId} @ Y{record.triggerYear} S{record.triggerSemester} R{record.triggerRound} choice {record.choiceIndex}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Flags");

        if (flags.Count == 0)
        {
            builder.Append("- none");
        }
        else
        {
            int shown = 0;
            foreach (KeyValuePair<string, bool> pair in flags)
            {
                builder.AppendLine($"- {pair.Key} = {pair.Value}");
                shown++;
                if (shown >= 8)
                    break;
            }
        }

        historyText.text = builder.ToString().TrimEnd();
    }

    private GameObject CreateRow(Transform parent, float height)
    {
        GameObject row = CreateUIElement("Row", parent);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        return row;
    }

    private void CreateButton(Transform parent, string label, float width, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = CreateUIElement($"Btn_{label}", parent);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 32f);

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;

        Image bg = buttonObj.AddComponent<Image>();
        bg.color = bgColor;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObj.transform, label, 13f, TextWhite, 32f);
        text.alignment = TextAlignmentOptions.Center;
        StretchFull(text.GetComponent<RectTransform>());
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height)
    {
        GameObject inputObj = CreateUIElement("InputField", parent);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement layout = inputObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;

        Image bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.90f);

        GameObject textArea = CreateUIElement("TextArea", inputObj.transform);
        RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(8, 2);
        textAreaRT.offsetMax = new Vector2(-8, -2);
        textArea.AddComponent<RectMask2D>();

        GameObject textObj = CreateUIElement("Text", textArea.transform);
        StretchFull(textObj.GetComponent<RectTransform>());
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 13f;
        text.color = TextWhite;
        text.alignment = TextAlignmentOptions.Left;
        ApplyChineseFont(text);

        GameObject placeholderObj = CreateUIElement("Placeholder", textArea.transform);
        StretchFull(placeholderObj.GetComponent<RectTransform>());
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 13f;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        ApplyChineseFont(placeholderText);

        TMP_InputField input = inputObj.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRT;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.fontAsset = FontManager.Instance != null ? FontManager.Instance.ChineseFont : null;
        return input;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height, float width = 0f)
    {
        GameObject obj = CreateUIElement("Label", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        if (width > 0f)
        {
            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
        }

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        ApplyChineseFont(tmp);
        return tmp;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        return go;
    }

    private void ApplyChineseFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
