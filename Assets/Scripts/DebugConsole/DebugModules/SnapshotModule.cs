#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 快照调试模块 —— 内存中的游戏状态快照管理
/// </summary>
public class SnapshotModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color BtnColor  = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color BtnGreen  = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnRed    = new Color(0.60f, 0.20f, 0.20f, 1.0f);

    private TMP_InputField nameInput;
    private Transform snapshotListContainer;
    private TextMeshProUGUI emptyHint;

    public void Init(RectTransform parent)
    {
        // 滚动容器
        GameObject scrollObj = CreateUIElement("ScrollView", parent);
        StretchFull(scrollObj.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // 标题
        CreateLabel(content.transform, "— 状态快照 —", 18f, TextGold, 30f);

        // 输入行
        GameObject inputRow = CreateUIElement("InputRow", content.transform);
        RectTransform rowRT = inputRow.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 40f);

        HorizontalLayoutGroup hlg = inputRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateLabel(inputRow.transform, "名称", 15f, TextWhite, 36f, 50f);
        nameInput = CreateInputField(inputRow.transform, "输入快照名称", 240f, 32f);

        CreateButton(inputRow.transform, "保存快照", 100f, BtnGreen, () =>
        {
            if (DebugConsoleManager.Instance == null || nameInput == null) return;
            string name = nameInput.text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                DebugConsoleManager.Log("快照", "快照名称不能为空");
                return;
            }
            DebugConsoleManager.Instance.SaveSnapshot(name);
            nameInput.text = "";
            Refresh();
        });

        // 分割线
        CreateSeparator(content.transform);

        // 快照列表标题
        CreateLabel(content.transform, "已保存的快照:", 15f, TextWhite, 24f);

        // 空提示
        emptyHint = CreateLabel(content.transform, "暂无快照", 14f, new Color(0.5f, 0.5f, 0.55f), 24f);

        // 快照列表容器
        GameObject listObj = CreateUIElement("SnapshotList", content.transform);
        RectTransform listRT = listObj.GetComponent<RectTransform>();
        listRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup listVlg = listObj.AddComponent<VerticalLayoutGroup>();
        listVlg.spacing = 6f;
        listVlg.childAlignment = TextAnchor.UpperCenter;
        listVlg.childControlWidth = true;
        listVlg.childControlHeight = false;
        listVlg.childForceExpandWidth = true;
        listVlg.childForceExpandHeight = false;

        ContentSizeFitter listCsf = listObj.AddComponent<ContentSizeFitter>();
        listCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        snapshotListContainer = listObj.transform;
    }

    public void Refresh()
    {
        if (DebugConsoleManager.Instance == null || snapshotListContainer == null) return;

        // 清空旧列表
        for (int i = snapshotListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(snapshotListContainer.GetChild(i).gameObject);
        }

        List<string> names = DebugConsoleManager.Instance.GetSnapshotNames();

        if (emptyHint != null)
        {
            emptyHint.gameObject.SetActive(names.Count == 0);
        }

        foreach (string name in names)
        {
            CreateSnapshotEntry(name);
        }
    }

    private void CreateSnapshotEntry(string snapshotName)
    {
        GameObject row = CreateUIElement("Snapshot_" + snapshotName, snapshotListContainer);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 36f);

        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.10f, 0.10f, 0.16f, 0.60f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(8, 8, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 名称文本
        TextMeshProUGUI nameLabel = CreateLabel(row.transform, snapshotName, 14f, TextWhite, 32f, 200f);
        LayoutElement nameLe = nameLabel.GetComponent<LayoutElement>();
        if (nameLe == null) nameLe = nameLabel.gameObject.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1f;

        // 加载按钮
        string capturedName = snapshotName;
        CreateButton(row.transform, "加载", 70f, BtnColor, () =>
        {
            if (DebugConsoleManager.Instance == null) return;
            DebugConsoleManager.Instance.LoadSnapshot(capturedName);
        });

        // 删除按钮
        CreateButton(row.transform, "删除", 70f, BtnRed, () =>
        {
            if (DebugConsoleManager.Instance == null) return;
            DebugConsoleManager.Instance.DeleteSnapshot(capturedName);
            Refresh();
        });
    }

    // ========== 工具方法 ==========

    private void CreateButton(Transform parent, string label, float width, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 32f);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = width;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI txt = CreateLabel(btnObj.transform, label, 13f, TextWhite, 32f);
        txt.alignment = TextAlignmentOptions.Center;
        StretchFull(txt.GetComponent<RectTransform>());
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height)
    {
        GameObject inputObj = CreateUIElement("InputField", parent);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement le = inputObj.AddComponent<LayoutElement>();
        le.preferredWidth = width;

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
        text.fontSize = 14f;
        text.color = TextWhite;
        text.alignment = TextAlignmentOptions.Left;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;

        GameObject phObj = CreateUIElement("Placeholder", textArea.transform);
        StretchFull(phObj.GetComponent<RectTransform>());
        TextMeshProUGUI phText = phObj.AddComponent<TextMeshProUGUI>();
        phText.text = placeholder;
        phText.fontSize = 14f;
        phText.fontStyle = FontStyles.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            phText.font = FontManager.Instance.ChineseFont;

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRT;
        inputField.textComponent = text;
        inputField.placeholder = phText;
        inputField.fontAsset = FontManager.Instance != null ? FontManager.Instance.ChineseFont : null;

        return inputField;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color,
        float height, float width = 0f)
    {
        GameObject obj = CreateUIElement("Label_" + text, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        if (width > 0f)
        {
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
        }

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

    private void CreateSeparator(Transform parent)
    {
        GameObject sep = CreateUIElement("Separator", parent);
        RectTransform rt = sep.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 2f);
        Image img = sep.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.flexibleWidth = 1f;
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
