#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 经济调试模块 —— 金钱查看/设置/快捷增减
/// </summary>
public class EconomyModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color BtnColor  = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color BtnGreen  = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnRed    = new Color(0.60f, 0.20f, 0.20f, 1.0f);

    private TextMeshProUGUI moneyDisplay;
    private TMP_InputField amountInput;

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
        CreateLabel(content.transform, "— 经济管理 —", 18f, TextGold, 30f);

        // 当前金钱
        moneyDisplay = CreateLabel(content.transform, "当前金钱: ¥--", 20f, TextGold, 36f);

        // 金额输入行
        GameObject inputRow = CreateUIElement("AmountRow", content.transform);
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

        CreateLabel(inputRow.transform, "金额", 15f, TextWhite, 36f, 60f);
        amountInput = CreateInputField(inputRow.transform, "输入金额", 200f, 32f);
        amountInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        CreateButton(inputRow.transform, "设置", 80f, BtnColor, () =>
        {
            if (GameState.Instance == null || amountInput == null) return;
            if (int.TryParse(amountInput.text, out int amount))
            {
                GameState.Instance.Money = amount;
                DebugConsoleManager.Log("经济", $"金钱设置为: ¥{amount}");
                Refresh();
            }
        });

        // 快捷按钮行
        GameObject quickRow = CreateUIElement("QuickRow", content.transform);
        RectTransform quickRT = quickRow.GetComponent<RectTransform>();
        quickRT.sizeDelta = new Vector2(0, 44f);

        HorizontalLayoutGroup qhlg = quickRow.AddComponent<HorizontalLayoutGroup>();
        qhlg.spacing = 10f;
        qhlg.padding = new RectOffset(4, 4, 2, 2);
        qhlg.childAlignment = TextAnchor.MiddleCenter;
        qhlg.childControlWidth = false;
        qhlg.childControlHeight = true;
        qhlg.childForceExpandWidth = false;
        qhlg.childForceExpandHeight = true;

        CreateButton(quickRow.transform, "+1000", 100f, BtnGreen, () => AddMoney(1000));
        CreateButton(quickRow.transform, "-1000", 100f, BtnRed, () => AddMoney(-1000));
        CreateButton(quickRow.transform, "+10000", 110f, BtnGreen, () => AddMoney(10000));
    }

    public void Refresh()
    {
        if (moneyDisplay != null && GameState.Instance != null)
        {
            moneyDisplay.text = $"当前金钱: ¥{GameState.Instance.Money}";
        }
    }

    private void AddMoney(int amount)
    {
        if (GameState.Instance == null) return;
        GameState.Instance.AddMoney(amount);
        DebugConsoleManager.Log("经济", $"金钱{(amount >= 0 ? "+" : "")}{amount} → ¥{GameState.Instance.Money}");
        Refresh();
    }

    // ========== 工具方法 ==========

    private void CreateButton(Transform parent, string label, float width, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 40f);

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
