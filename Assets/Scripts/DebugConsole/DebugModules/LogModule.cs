#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 日志调试模块 —— 可滚动日志列表 + 分类过滤
/// </summary>
public class LogModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray  = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color BtnColor  = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color BtnActive = new Color(0.30f, 0.50f, 0.75f, 1.0f);

    private TextMeshProUGUI logText;
    private ScrollRect scrollRect;
    private string currentFilter = "";  // 空字符串 = 全部

    private readonly string[] filterLabels = { "全部", "属性", "状态", "行动", "回合" };
    private readonly List<Image> filterBtnBgs = new List<Image>();
    private int activeFilterIndex = 0;

    public void Init(RectTransform parent)
    {
        // 主容器
        GameObject mainContent = CreateUIElement("MainContent", parent);
        StretchFull(mainContent.GetComponent<RectTransform>());

        // 过滤按钮行（顶部）
        GameObject filterRow = CreateUIElement("FilterRow", mainContent.transform);
        RectTransform filterRT = filterRow.GetComponent<RectTransform>();
        filterRT.anchorMin = new Vector2(0, 1);
        filterRT.anchorMax = new Vector2(1, 1);
        filterRT.pivot = new Vector2(0.5f, 1);
        filterRT.anchoredPosition = Vector2.zero;
        filterRT.sizeDelta = new Vector2(0, 40f);

        HorizontalLayoutGroup hlg = filterRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.padding = new RectOffset(12, 12, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 过滤按钮
        for (int i = 0; i < filterLabels.Length; i++)
        {
            int idx = i;
            string filterCategory = i == 0 ? "" : filterLabels[i];
            CreateFilterButton(filterRow.transform, filterLabels[i], idx, () =>
            {
                currentFilter = filterCategory;
                activeFilterIndex = idx;
                UpdateFilterHighlights();
                RefreshLogDisplay();
            });
        }

        // 清空按钮
        CreateActionButton(filterRow.transform, "清空", () =>
        {
            DebugConsoleManager.ClearLogs();
            RefreshLogDisplay();
        });

        // 日志滚动区域
        GameObject scrollObj = CreateUIElement("LogScrollView", mainContent.transform);
        RectTransform scrollObjRT = scrollObj.GetComponent<RectTransform>();
        scrollObjRT.anchorMin = Vector2.zero;
        scrollObjRT.anchorMax = Vector2.one;
        scrollObjRT.offsetMin = new Vector2(0, 0);
        scrollObjRT.offsetMax = new Vector2(0, -44f);

        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        // Viewport
        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // Log Content
        GameObject logContent = CreateUIElement("LogContent", viewport.transform);
        RectTransform logContentRT = logContent.GetComponent<RectTransform>();
        logContentRT.anchorMin = new Vector2(0, 1);
        logContentRT.anchorMax = new Vector2(1, 1);
        logContentRT.pivot = new Vector2(0, 1);
        logContentRT.anchoredPosition = Vector2.zero;
        logContentRT.sizeDelta = new Vector2(0, 100);

        ContentSizeFitter csf = logContent.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = logContentRT;

        // 日志文本
        GameObject textObj = CreateUIElement("LogText", logContent.transform);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 1);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0, 1);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(0, 100);

        logText = textObj.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 13f;
        logText.color = TextGray;
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.enableWordWrapping = true;
        logText.overflowMode = TextOverflowModes.Overflow;
        logText.richText = true;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            logText.font = FontManager.Instance.ChineseFont;

        // 使文本自适应高度
        ContentSizeFitter textCsf = textObj.AddComponent<ContentSizeFitter>();
        textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement textLE = textObj.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1f;

        // 初始高亮
        UpdateFilterHighlights();
    }

    public void Refresh()
    {
        RefreshLogDisplay();

        // 订阅新日志事件
        DebugConsoleManager.OnLogAdded -= OnNewLogEntry;
        DebugConsoleManager.OnLogAdded += OnNewLogEntry;
    }

    private void OnDestroy()
    {
        DebugConsoleManager.OnLogAdded -= OnNewLogEntry;
    }

    private void OnNewLogEntry(DebugLogEntry entry)
    {
        // 如果当前面板激活，立即刷新
        if (gameObject.activeInHierarchy)
        {
            RefreshLogDisplay();
        }
    }

    private void RefreshLogDisplay()
    {
        if (logText == null) return;

        var entries = DebugConsoleManager.GetLogEntries();
        var sb = new System.Text.StringBuilder();

        int displayCount = 0;
        for (int i = entries.Count - 1; i >= 0 && displayCount < 200; i--)
        {
            var entry = entries[i];

            // 过滤
            if (!string.IsNullOrEmpty(currentFilter) && entry.category != currentFilter)
                continue;

            // 格式化
            sb.Append($"<color=#666666>[{entry.timestamp}]</color> ");
            sb.Append($"<color=#4488CC>[{entry.category}]</color> ");
            sb.Append($"<color=#DDDDDD>{entry.message}</color>\n");
            displayCount++;
        }

        if (displayCount == 0)
        {
            sb.Append("<color=#555555>暂无日志记录</color>");
        }

        logText.text = sb.ToString();

        // 自动滚动到底部（最新的在顶部，所以滚动到顶部）
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0, 1);
        }
    }

    private void CreateFilterButton(Transform parent, string label, int index,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Filter_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 30f);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 60f;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = BtnColor;
        filterBtnBgs.Add(bg);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI txt = CreateLabel(btnObj.transform, label, 13f, TextWhite, 30f);
        txt.alignment = TextAlignmentOptions.Center;
        StretchFull(txt.GetComponent<RectTransform>());
    }

    private void CreateActionButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50f, 30f);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 50f;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.60f, 0.20f, 0.20f, 1.0f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI txt = CreateLabel(btnObj.transform, label, 12f, TextWhite, 30f);
        txt.alignment = TextAlignmentOptions.Center;
        StretchFull(txt.GetComponent<RectTransform>());
    }

    private void UpdateFilterHighlights()
    {
        for (int i = 0; i < filterBtnBgs.Count; i++)
        {
            filterBtnBgs[i].color = (i == activeFilterIndex) ? BtnActive : BtnColor;
        }
    }

    // ========== 工具方法 ==========

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height)
    {
        GameObject obj = CreateUIElement("Label", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            tmp.font = FontManager.Instance.ChineseFont;

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

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
