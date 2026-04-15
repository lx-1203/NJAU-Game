#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 事件调试模块（占位）—— 事件触发/跳过
/// </summary>
public class EventModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray  = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color BtnColor  = new Color(0.20f, 0.35f, 0.60f, 1.0f);

    private TMP_InputField eventIdInput;
    private TextMeshProUGUI statusText;

    public void Init(RectTransform parent)
    {
        GameObject content = CreateUIElement("Content", parent);
        StretchFull(content.GetComponent<RectTransform>());

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 标题
        CreateLabel(content.transform, "— 事件调试 —", 18f, TextGold, 30f);

        // 状态提示
        statusText = CreateLabel(content.transform, "事件系统待接入", 15f, TextGray, 28f);

        // 事件ID输入
        GameObject inputRow = CreateUIElement("EventIdRow", content.transform);
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

        CreateLabel(inputRow.transform, "事件ID", 15f, TextWhite, 36f, 80f);
        eventIdInput = CreateInputField(inputRow.transform, "输入事件ID", 260f, 32f);

        // 按钮行
        GameObject btnRow = CreateUIElement("BtnRow", content.transform);
        RectTransform btnRowRT = btnRow.GetComponent<RectTransform>();
        btnRowRT.sizeDelta = new Vector2(0, 44f);

        HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHlg.spacing = 12f;
        btnHlg.padding = new RectOffset(4, 4, 2, 2);
        btnHlg.childAlignment = TextAnchor.MiddleCenter;
        btnHlg.childControlWidth = false;
        btnHlg.childControlHeight = true;
        btnHlg.childForceExpandWidth = false;
        btnHlg.childForceExpandHeight = true;

        CreateButton(btnRow.transform, "强制触发", 150f, () =>
        {
            string id = eventIdInput != null ? eventIdInput.text : "";
            if (string.IsNullOrEmpty(id))
            {
                statusText.text = "请输入事件ID";
                return;
            }
            if (EventScheduler.Instance != null)
            {
                EventScheduler.Instance.EnqueueEvent(id);
                DebugConsoleManager.Log("事件", $"已强制触发事件: {id}");
                statusText.text = $"已触发: {id}";
            }
            else
            {
                statusText.text = "EventScheduler 未初始化";
            }
        });

        CreateButton(btnRow.transform, "跳过事件", 150f, () =>
        {
            string id = eventIdInput != null ? eventIdInput.text : "";
            if (string.IsNullOrEmpty(id))
            {
                statusText.text = "请输入事件ID";
                return;
            }
            if (EventHistory.Instance != null)
            {
                EventHistory.Instance.RecordEvent(id, -1);
                DebugConsoleManager.Log("事件", $"已跳过事件: {id} (标记为已触发)");
                statusText.text = $"已跳过: {id}";
            }
            else
            {
                statusText.text = "EventHistory 未初始化";
            }
        });
    }

    public void Refresh()
    {
        if (statusText == null) return;

        if (EventScheduler.Instance != null)
        {
            bool hasPending = EventScheduler.Instance.HasPendingEvents();
            statusText.text = hasPending ? "事件队列中有待处理事件" : "事件系统就绪";
        }
        else
        {
            statusText.text = "EventScheduler 未初始化";
        }
    }

    // ========== 工具方法 ==========

    private void CreateButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 40f);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = width;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = BtnColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI txt = CreateLabel(btnObj.transform, label, 14f, TextWhite, 40f);
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
