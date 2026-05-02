#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SnapshotModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.72f, 1f);
    private static readonly Color PositiveColor = new Color(0.2f, 0.58f, 0.34f, 1f);
    private static readonly Color NegativeColor = new Color(0.68f, 0.28f, 0.28f, 1f);
    private static readonly Color FieldColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

    private TMP_InputField nameInput;
    private Transform listRoot;
    private TextMeshProUGUI emptyHint;

    public void Init(RectTransform parent)
    {
        Transform content = CreateScrollableContent(parent);

        CreateLabel(content, "快照", 20f, AccentColor, 34f);

        GameObject row = CreateRect("SaveRow", content).gameObject;
        row.AddComponent<LayoutElement>().preferredHeight = 40f;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI label = CreateLabel(row.transform, "名称", 15f, TextColor, 36f);
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = 70f;

        nameInput = CreateInputField(row.transform, 220f);

        CreateButton(row.transform, "保存", PositiveColor, () =>
        {
            if (DebugConsoleManager.Instance == null || nameInput == null)
            {
                return;
            }

            string snapshotName = nameInput.text.Trim();
            if (string.IsNullOrEmpty(snapshotName))
            {
                DebugConsoleManager.Log("Snapshot", "Snapshot name is empty");
                return;
            }

            DebugConsoleManager.Instance.SaveSnapshot(snapshotName);
            nameInput.SetTextWithoutNotify(string.Empty);
            Refresh();
        });

        CreateSpacer(content, 8f);
        CreateLabel(content, "已保存快照", 15f, TextColor, 24f);
        emptyHint = CreateLabel(content, "还没有快照。", 14f, new Color(0.58f, 0.58f, 0.62f), 24f);

        GameObject listObject = CreateRect("ListRoot", content).gameObject;
        VerticalLayoutGroup listLayout = listObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 8f;
        listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        listRoot = listObject.transform;
    }

    public void Refresh()
    {
        if (DebugConsoleManager.Instance == null || listRoot == null)
        {
            return;
        }

        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(listRoot.GetChild(i).gameObject);
        }

        List<string> names = DebugConsoleManager.Instance.GetSnapshotNames();
        emptyHint.gameObject.SetActive(names.Count == 0);

        foreach (string name in names)
        {
            CreateSnapshotEntry(name);
        }
    }

    private void CreateSnapshotEntry(string name)
    {
        GameObject row = CreateRect($"Snapshot_{name}", listRoot).gameObject;
        row.AddComponent<LayoutElement>().preferredHeight = 38f;
        row.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.16f, 0.65f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 2, 2);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI nameLabel = CreateLabel(row.transform, name, 14f, TextColor, 34f);
        LayoutElement nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
        nameLayout.preferredWidth = 240f;
        nameLayout.flexibleWidth = 1f;

        CreateButton(row.transform, "载入", ButtonColor, () =>
        {
            DebugConsoleManager.Instance?.LoadSnapshot(name);
        }, 76f, 32f);

        CreateButton(row.transform, "删除", NegativeColor, () =>
        {
            DebugConsoleManager.Instance?.DeleteSnapshot(name);
            Refresh();
        }, 76f, 32f);
    }

    private Transform CreateScrollableContent(RectTransform parent)
    {
        GameObject scrollObject = CreateRect("ScrollView", parent).gameObject;
        StretchFull(scrollObject.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        GameObject viewport = CreateRect("Viewport", scrollObject.transform).gameObject;
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject contentObject = CreateRect("Content", viewport.transform).gameObject;
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;
        return contentObject.transform;
    }

    private TMP_InputField CreateInputField(Transform parent, float width)
    {
        GameObject inputObject = CreateRect("Input", parent).gameObject;
        LayoutElement layout = inputObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 34f;

        Image background = inputObject.AddComponent<Image>();
        background.color = FieldColor;

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();

        GameObject viewport = CreateRect("Viewport", inputObject.transform).gameObject;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 2f);
        viewportRect.offsetMax = new Vector2(-10f, -2f);
        viewport.AddComponent<RectMask2D>();

        TextMeshProUGUI text = CreateLabel(viewport.transform, string.Empty, 15f, TextColor, 28f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI placeholder = CreateLabel(viewport.transform, "输入快照名称", 13f, new Color(0.55f, 0.55f, 0.6f), 28f);
        StretchFull(placeholder.rectTransform);
        placeholder.alignment = TextAlignmentOptions.Center;

        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick, float width = 92f, float height = 36f)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        Image background = buttonObject.AddComponent<Image>();
        background.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 14f, Color.white, height);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string textValue, float fontSize, Color color, float height)
    {
        GameObject textObject = CreateRect("Label", parent).gameObject;
        textObject.AddComponent<LayoutElement>().preferredHeight = height;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }

        return text;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateRect("Spacer", parent).gameObject;
        spacer.AddComponent<LayoutElement>().preferredHeight = height;
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
