#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.72f, 1f);
    private static readonly Color ActiveColor = new Color(0.28f, 0.5f, 0.78f, 1f);
    private static readonly Color DangerColor = new Color(0.68f, 0.28f, 0.28f, 1f);

    private readonly string[] filters = { "全部", "属性", "时间", "经济", "事件" };
    private readonly List<Image> filterBackgrounds = new List<Image>();

    private string currentFilter = string.Empty;
    private int activeFilterIndex;
    private TextMeshProUGUI logText;
    private ScrollRect scrollRect;

    public void Init(RectTransform parent)
    {
        GameObject root = CreateRect("Root", parent).gameObject;
        StretchFull(root.GetComponent<RectTransform>());

        Transform filterRow = CreateHorizontalRow("FilterRow", root.transform, 6f);
        RectTransform filterRect = filterRow.GetComponent<RectTransform>();
        filterRect.anchorMin = new Vector2(0f, 1f);
        filterRect.anchorMax = new Vector2(1f, 1f);
        filterRect.pivot = new Vector2(0.5f, 1f);
        filterRect.sizeDelta = new Vector2(0f, 42f);

        for (int i = 0; i < filters.Length; i++)
        {
            int capturedIndex = i;
            string filter = filters[i] == "全部" ? string.Empty : filters[i];
            Button button = CreateButton(filterRow, filters[i], ButtonColor, () =>
            {
                currentFilter = filter;
                activeFilterIndex = capturedIndex;
                RefreshHighlights();
                RefreshLogDisplay();
            }, 82f, 30f);
            filterBackgrounds.Add(button.GetComponent<Image>());
        }

        CreateButton(filterRow, "清空", DangerColor, () =>
        {
            DebugConsoleManager.ClearLogs();
            RefreshLogDisplay();
        }, 64f, 30f);

        GameObject scrollObject = CreateRect("ScrollView", root.transform).gameObject;
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = new Vector2(0f, -44f);

        scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        GameObject viewport = CreateRect("Viewport", scrollObject.transform).gameObject;
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateRect("Content", viewport.transform).gameObject;
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        GameObject textObject = CreateRect("LogText", content.transform).gameObject;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;

        logText = textObject.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 13f;
        logText.color = TextColor;
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.enableWordWrapping = true;
        logText.overflowMode = TextOverflowModes.Overflow;
        logText.richText = true;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            logText.font = FontManager.Instance.ChineseFont;
        }

        textObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RefreshHighlights();
    }

    public void Refresh()
    {
        DebugConsoleManager.OnLogAdded -= OnNewLogEntry;
        DebugConsoleManager.OnLogAdded += OnNewLogEntry;
        RefreshLogDisplay();
    }

    private void OnDestroy()
    {
        DebugConsoleManager.OnLogAdded -= OnNewLogEntry;
    }

    private void OnNewLogEntry(DebugLogEntry entry)
    {
        if (gameObject.activeInHierarchy)
        {
            RefreshLogDisplay();
        }
    }

    private void RefreshLogDisplay()
    {
        if (logText == null)
        {
            return;
        }

        List<DebugLogEntry> entries = DebugConsoleManager.GetLogEntries();
        StringBuilder builder = new StringBuilder();
        int shown = 0;

        for (int i = entries.Count - 1; i >= 0 && shown < 200; i--)
        {
            DebugLogEntry entry = entries[i];
            if (!MatchesFilter(entry))
            {
                continue;
            }

            builder.Append("<color=#6C6C75>[");
            builder.Append(entry.timestamp);
            builder.Append("]</color> ");
            builder.Append("<color=#6AB0FF>[");
            builder.Append(entry.category);
            builder.Append("]</color> ");
            builder.Append(entry.message);
            builder.Append('\n');
            shown++;
        }

        if (shown == 0)
        {
            builder.Append("<color=#73737D>这个筛选下还没有日志。</color>");
        }

        logText.text = builder.ToString();

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
    }

    private bool MatchesFilter(DebugLogEntry entry)
    {
        if (string.IsNullOrEmpty(currentFilter))
        {
            return true;
        }

        switch (currentFilter)
        {
            case "属性":
                return entry.category == "Attributes" || entry.category == "Adjust";
            case "时间":
                return entry.category == "Time";
            case "经济":
                return entry.category == "Economy";
            case "事件":
                return entry.category == "Event" || entry.category == "Events";
            default:
                return entry.category == currentFilter;
        }
    }

    private void RefreshHighlights()
    {
        for (int i = 0; i < filterBackgrounds.Count; i++)
        {
            filterBackgrounds[i].color = i == activeFilterIndex ? ActiveColor : ButtonColor;
        }
    }

    private Transform CreateHorizontalRow(string name, Transform parent, float spacing)
    {
        Transform row = CreateRect(name, parent);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(12, 12, 6, 6);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick, float width, float height)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        Image background = buttonObject.AddComponent<Image>();
        background.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(buttonObject.transform, label, 13f, Color.white, TextAlignmentOptions.Center);
        StretchFull(text.rectTransform);
        return button;
    }

    private TextMeshProUGUI CreateText(Transform parent, string value, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRect("Text", parent).gameObject;
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.margin = new Vector4(2f, 4f, 2f, 4f);
        text.extraPadding = true;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }

        return text;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject objectRef = new GameObject(name, typeof(RectTransform));
        objectRef.transform.SetParent(parent, false);
        return objectRef.GetComponent<RectTransform>();
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
