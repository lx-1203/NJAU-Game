#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttributeModule : MonoBehaviour, IDebugModule
{
    private struct AttributeDefinition
    {
        public string label;
        public string key;
        public int min;
        public int max;

        public AttributeDefinition(string label, string key, int min, int max)
        {
            this.label = label;
            this.key = key;
            this.min = min;
            this.max = max;
        }
    }

    private sealed class AttributeRow
    {
        public AttributeDefinition definition;
        public Slider slider;
        public TMP_InputField input;
    }

    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color FieldColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);
    private static readonly Color SliderBackgroundColor = new Color(0.14f, 0.14f, 0.2f, 0.95f);
    private static readonly Color SliderFillColor = new Color(0.28f, 0.55f, 0.86f, 1f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.72f, 1f);

    private static readonly AttributeDefinition[] Definitions =
    {
        new AttributeDefinition("Study", "Study", 0, 999),
        new AttributeDefinition("Charm", "Charm", 0, 999),
        new AttributeDefinition("Physique", "Physique", 0, 999),
        new AttributeDefinition("Leadership", "Leadership", 0, 999),
        new AttributeDefinition("Stress", "Stress", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("Mood", "Mood", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("Darkness", "Darkness", 0, 999),
        new AttributeDefinition("Guilt", "Guilt", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("Luck", "Luck", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("Money", "Money", -999999, 999999),
    };

    private readonly List<AttributeRow> rows = new List<AttributeRow>();
    private TextMeshProUGUI apText;
    private bool isRefreshing;

    public void Init(RectTransform parent)
    {
        GameObject scrollObject = CreateRect("ScrollView", parent).gameObject;
        StretchFull(scrollObject.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 35f;

        GameObject viewport = CreateRect("Viewport", scrollObject.transform).gameObject;
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateRect("Content", viewport.transform).gameObject;
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        CreateLabel(content.transform, "Attributes", 20f, AccentColor, 34f);
        CreateLabel(content.transform, "Edit exact values directly, or drag the slider for quick tuning.", 14f, TextColor, 26f);

        foreach (AttributeDefinition definition in Definitions)
        {
            rows.Add(CreateAttributeRow(content.transform, definition));
        }

        CreateSpacer(content.transform, 8f);
        apText = CreateLabel(content.transform, "AP: - / -", 15f, TextColor, 28f);

        CreateButtonRow(content.transform);
    }

    public void Refresh()
    {
        isRefreshing = true;

        foreach (AttributeRow row in rows)
        {
            int value = DebugPresets.GetAttributeValue(row.definition.key);
            row.slider.minValue = row.definition.min;
            row.slider.maxValue = row.definition.max;
            row.slider.SetValueWithoutNotify(Mathf.Clamp(value, row.definition.min, row.definition.max));
            row.input.SetTextWithoutNotify(value.ToString());
        }

        if (apText != null && GameState.Instance != null)
        {
            apText.text = $"AP: {GameState.Instance.ActionPoints} / {GameState.Instance.EffectiveMaxActionPoints}";
        }

        isRefreshing = false;
    }

    private AttributeRow CreateAttributeRow(Transform parent, AttributeDefinition definition)
    {
        GameObject rowObject = CreateRect($"{definition.key}Row", parent).gameObject;
        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 42f;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI nameText = CreateLabel(rowObject.transform, definition.label, 15f, TextColor, 42f);
        LayoutElement nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
        nameLayout.preferredWidth = 120f;

        Button minusButton = CreateTinyButton(rowObject.transform, "-", () => NudgeValue(definition, -DebugPresets.CurrentStep));
        SetFixedSize(minusButton.gameObject, 34f, 34f);

        TMP_InputField input = CreateInputField(rowObject.transform, 90f);
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.onEndEdit.AddListener(value => ApplyTypedValue(definition, value));

        Button plusButton = CreateTinyButton(rowObject.transform, "+", () => NudgeValue(definition, DebugPresets.CurrentStep));
        SetFixedSize(plusButton.gameObject, 34f, 34f);

        Slider slider = CreateSlider(rowObject.transform, definition.min, definition.max);
        LayoutElement sliderLayout = slider.gameObject.AddComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.preferredHeight = 24f;
        slider.onValueChanged.AddListener(value =>
        {
            if (isRefreshing)
            {
                return;
            }

            int targetValue = Mathf.RoundToInt(value);
            DebugPresets.SetAttributeValue(definition.key, targetValue);
            input.SetTextWithoutNotify(DebugPresets.GetAttributeValue(definition.key).ToString());
            DebugConsoleManager.Log("Attributes", $"{definition.key} -> {DebugPresets.GetAttributeValue(definition.key)}");
            Refresh();
        });

        return new AttributeRow
        {
            definition = definition,
            slider = slider,
            input = input
        };
    }

    private void ApplyTypedValue(AttributeDefinition definition, string value)
    {
        if (isRefreshing)
        {
            return;
        }

        if (!int.TryParse(value, out int parsed))
        {
            Refresh();
            return;
        }

        parsed = Mathf.Clamp(parsed, definition.min, definition.max);
        DebugPresets.SetAttributeValue(definition.key, parsed);
        DebugConsoleManager.Log("Attributes", $"{definition.key} -> {parsed}");
        Refresh();
    }

    private void NudgeValue(AttributeDefinition definition, int delta)
    {
        int current = DebugPresets.GetAttributeValue(definition.key);
        int scaledDelta = definition.key == "Money" ? delta * 100 : delta;
        int target = Mathf.Clamp(current + scaledDelta, definition.min, definition.max);
        DebugPresets.SetAttributeValue(definition.key, target);
        DebugConsoleManager.Log("Attributes", $"{definition.key} -> {target}");
        Refresh();
    }

    private void CreateButtonRow(Transform parent)
    {
        GameObject rowObject = CreateRect("ButtonRow", parent).gameObject;
        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 40f;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateWideButton(rowObject.transform, "Preset: Freshman", () =>
        {
            if (PlayerAttributes.Instance == null)
            {
                return;
            }

            PlayerAttributes.Instance.SetAll(12, 8, 10, 6, 15, 80, 0, 0, 55);
            if (GameState.Instance != null)
            {
                GameState.Instance.Money = 8000;
            }

            DebugConsoleManager.Log("Attributes", "Applied freshman preset");
            Refresh();
        });

        CreateWideButton(rowObject.transform, "Preset: Max", () =>
        {
            if (PlayerAttributes.Instance == null)
            {
                return;
            }

            PlayerAttributes.Instance.SetAll(100, 100, 100, 100, 0, 100, 100, 0, 100);
            if (GameState.Instance != null)
            {
                GameState.Instance.Money = 999999;
            }

            DebugConsoleManager.Log("Attributes", "Applied max preset");
            Refresh();
        });

        CreateWideButton(rowObject.transform, "Clamp Status", () =>
        {
            if (PlayerAttributes.Instance == null)
            {
                return;
            }

            PlayerAttributes.Instance.Stress = Mathf.Clamp(PlayerAttributes.Instance.Stress, 0, PlayerAttributes.MaxStatusValue);
            PlayerAttributes.Instance.Mood = Mathf.Clamp(PlayerAttributes.Instance.Mood, 0, PlayerAttributes.MaxStatusValue);
            PlayerAttributes.Instance.Guilt = Mathf.Clamp(PlayerAttributes.Instance.Guilt, 0, PlayerAttributes.MaxStatusValue);
            PlayerAttributes.Instance.Luck = Mathf.Clamp(PlayerAttributes.Instance.Luck, 0, PlayerAttributes.MaxStatusValue);
            Refresh();
        });
    }

    private Slider CreateSlider(Transform parent, int min, int max)
    {
        GameObject sliderObject = CreateRect("Slider", parent).gameObject;
        Image background = sliderObject.AddComponent<Image>();
        background.color = SliderBackgroundColor;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;

        GameObject fillArea = CreateRect("FillArea", sliderObject.transform).gameObject;
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5f, 5f);
        fillAreaRect.offsetMax = new Vector2(-5f, -5f);

        GameObject fill = CreateRect("Fill", fillArea.transform).gameObject;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = SliderFillColor;
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handleArea = CreateRect("HandleArea", sliderObject.transform).gameObject;
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateRect("Handle", handleArea.transform).gameObject;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 28f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private TMP_InputField CreateInputField(Transform parent, float width)
    {
        GameObject inputObject = CreateRect("Input", parent).gameObject;
        SetFixedSize(inputObject, width, 34f);
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

    private Button CreateTinyButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        Image background = buttonObject.AddComponent<Image>();
        background.color = ButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonColor * 1.1f;
        colors.pressedColor = ButtonColor * 0.85f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 18f, Color.white, 30f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private Button CreateWideButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        Button button = CreateTinyButton(parent, label, onClick);
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 170f;
        layout.preferredHeight = 36f;
        button.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14f;
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height)
    {
        GameObject labelObject = CreateRect("Label", parent).gameObject;
        LayoutElement layout = labelObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            label.font = FontManager.Instance.ChineseFont;
        }

        return label;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateRect("Spacer", parent).gameObject;
        LayoutElement layout = spacer.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
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

    private void SetFixedSize(GameObject gameObject, float width, float height)
    {
        LayoutElement layout = gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
    }
}
#endif
