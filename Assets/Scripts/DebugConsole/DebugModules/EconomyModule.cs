#if DEVELOPMENT_BUILD || UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EconomyModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.72f, 1f);
    private static readonly Color PositiveColor = new Color(0.2f, 0.58f, 0.34f, 1f);
    private static readonly Color NegativeColor = new Color(0.68f, 0.28f, 0.28f, 1f);
    private static readonly Color FieldColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

    private TextMeshProUGUI moneyDisplay;
    private TMP_InputField amountInput;

    public void Init(RectTransform parent)
    {
        Transform content = CreateRoot(parent);

        CreateLabel(content, "经济调整", 20f, AccentColor, 34f);
        moneyDisplay = CreateLabel(content, "金钱：--", 18f, AccentColor, 30f);

        GameObject row = CreateRect("SetRow", content).gameObject;
        row.AddComponent<LayoutElement>().preferredHeight = 40f;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI label = CreateLabel(row.transform, "设为", 15f, TextColor, 36f);
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

        amountInput = CreateInputField(row.transform, 180f);
        amountInput.SetTextWithoutNotify("8000");

        CreateButton(row.transform, "应用", ButtonColor, () =>
        {
            if (GameState.Instance == null || amountInput == null)
            {
                return;
            }

            if (int.TryParse(amountInput.text, out int amount))
            {
                GameState.Instance.Money = amount;
                DebugConsoleManager.Log("Economy", $"Money -> {amount}");
                Refresh();
            }
        });

        CreateSpacer(content, 8f);
        Transform quickRow = CreateRect("QuickRow", content);
        quickRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        HorizontalLayoutGroup quickLayout = quickRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        quickLayout.spacing = 10f;
        quickLayout.childAlignment = TextAnchor.MiddleLeft;
        quickLayout.childControlWidth = false;
        quickLayout.childControlHeight = true;
        quickLayout.childForceExpandWidth = false;
        quickLayout.childForceExpandHeight = true;

        CreateButton(quickRow, "+1000", PositiveColor, () => AddMoney(1000));
        CreateButton(quickRow, "-1000", NegativeColor, () => AddMoney(-1000));
        CreateButton(quickRow, "+10000", PositiveColor, () => AddMoney(10000));
        CreateButton(quickRow, "-10000", NegativeColor, () => AddMoney(-10000));
    }

    public void Refresh()
    {
        if (GameState.Instance == null)
        {
            return;
        }

        moneyDisplay.text = $"金钱：{GameState.Instance.Money}";
        amountInput.SetTextWithoutNotify(GameState.Instance.Money.ToString());
    }

    private void AddMoney(int amount)
    {
        if (GameState.Instance == null)
        {
            return;
        }

        GameState.Instance.AddMoney(amount);
        DebugConsoleManager.Log("Economy", $"Money {(amount >= 0 ? "+" : string.Empty)}{amount} -> {GameState.Instance.Money}");
        Refresh();
    }

    private Transform CreateRoot(RectTransform parent)
    {
        Transform content = CreateRect("Content", parent);
        StretchFull(content.GetComponent<RectTransform>());

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return content;
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

        TextMeshProUGUI placeholder = CreateLabel(viewport.transform, "0", 15f, new Color(0.55f, 0.55f, 0.6f), 28f);
        StretchFull(placeholder.rectTransform);
        placeholder.alignment = TextAlignmentOptions.Center;

        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private Button CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 92f;
        layout.preferredHeight = 36f;

        Image background = buttonObject.AddComponent<Image>();
        background.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 14f, Color.white, 36f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string textValue, float fontSize, Color color, float height)
    {
        GameObject textObject = CreateRect("Label", parent).gameObject;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        float resolvedHeight = Mathf.Max(height, fontSize + 14f);
        layout.preferredHeight = resolvedHeight;
        layout.minHeight = resolvedHeight;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(2f, 4f, 2f, 4f);
        text.extraPadding = true;

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
