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

    private sealed class ThresholdRow
    {
        public int index;
        public Slider slider;
        public TMP_InputField input;
    }

    private static readonly Color TextColor = new Color32(0xF1, 0xEA, 0xDB, 0xFF);
    private static readonly Color AccentColor = new Color32(0xF2, 0xC5, 0x68, 0xFF);
    private static readonly Color FieldColor = new Color32(0x2B, 0x24, 0x33, 0xF2);
    private static readonly Color SliderBackgroundColor = new Color32(0x24, 0x1E, 0x2A, 0xF2);
    private static readonly Color SliderFillColor = new Color32(0x9E, 0x75, 0x48, 0xFF);
    private static readonly Color ButtonColor = new Color32(0x7A, 0x56, 0x35, 0xFF);
    private static Sprite _sliderHandleSprite;
    private static Sprite SliderHandleSprite
    {
        get
        {
            if (_sliderHandleSprite == null)
                _sliderHandleSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            return _sliderHandleSprite;
        }
    }

    private static readonly AttributeDefinition[] Definitions =
    {
        new AttributeDefinition("学力", "Study", 0, 999),
        new AttributeDefinition("魅力", "Charm", 0, 999),
        new AttributeDefinition("体魄", "Physique", 0, 999),
        new AttributeDefinition("领导力", "Leadership", 0, 999),
        new AttributeDefinition("压力", "Stress", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("心情", "Mood", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("黑暗值", "Darkness", 0, 999),
        new AttributeDefinition("负罪感", "Guilt", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("幸运", "Luck", 0, PlayerAttributes.MaxStatusValue),
        new AttributeDefinition("金钱", "Money", -999999, 999999),
        new AttributeDefinition("行动点", "ActionPoints", 0, 999),
    };

    private readonly List<AttributeRow> rows = new List<AttributeRow>();
    private readonly List<ThresholdRow> thresholdRows = new List<ThresholdRow>();
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

        CreateLabel(content.transform, "属性调整", 20f, AccentColor, 34f);
        CreateLabel(content.transform, "可以直接输入精确数值，也可以拖动滑条快速微调。", 14f, TextColor, 26f);

        foreach (AttributeDefinition definition in Definitions)
        {
            rows.Add(CreateAttributeRow(content.transform, definition));
        }

        CreateSpacer(content.transform, 12f);
        CreateLabel(content.transform, "属性评级阈值", 18f, AccentColor, 30f);
        CreateLabel(content.transform, "D/C/B/A/S 阈值会立刻影响属性条等级与“还差多少”的显示，支持手动输入。", 13f, TextColor, 24f);

        for (int i = 0; i < AttributeGradeSettings.TierCount; i++)
        {
            thresholdRows.Add(CreateThresholdRow(content.transform, i));
        }

        CreateSpacer(content.transform, 8f);
        apText = CreateLabel(content.transform, "行动点：- / -", 15f, TextColor, 28f);

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

        for (int i = 0; i < thresholdRows.Count; i++)
        {
            int threshold = AttributeGradeSettings.GetThreshold(i);
            thresholdRows[i].slider.SetValueWithoutNotify(threshold);
            thresholdRows[i].input.SetTextWithoutNotify(threshold.ToString());
        }

        if (apText != null && GameState.Instance != null)
        {
            apText.text = $"行动点：{GameState.Instance.ActionPoints} / {GameState.Instance.EffectiveMaxActionPoints}";
        }

        isRefreshing = false;
    }

    private AttributeRow CreateAttributeRow(Transform parent, AttributeDefinition definition)
    {
        GameObject rowObject = CreateRect($"{definition.key}Row", parent).gameObject;
        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 50f;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI nameText = CreateLabel(rowObject.transform, definition.label, 15f, TextColor, 46f);
        LayoutElement nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
        nameLayout.preferredWidth = 84f;

        Button minusButton = CreateTinyButton(rowObject.transform, "-", () => NudgeValue(definition, -DebugPresets.CurrentStep));
        SetFixedSize(minusButton.gameObject, 36f, 36f);

        TMP_InputField input = CreateInputField(rowObject.transform, 92f);
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.onEndEdit.AddListener(value => ApplyTypedValue(definition, value));

        Button plusButton = CreateTinyButton(rowObject.transform, "+", () => NudgeValue(definition, DebugPresets.CurrentStep));
        SetFixedSize(plusButton.gameObject, 36f, 36f);

        Slider slider = CreateSlider(rowObject.transform, definition.min, definition.max);
        LayoutElement sliderLayout = slider.gameObject.GetComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.preferredHeight = 28f;
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

    private ThresholdRow CreateThresholdRow(Transform parent, int index)
    {
        GameObject rowObject = CreateRect($"Threshold_{index}", parent).gameObject;
        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 50f;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        string label = $"{AttributeGradeSettings.GetTierLabel(index)}档";
        TextMeshProUGUI nameText = CreateLabel(rowObject.transform, label, 15f, TextColor, 46f);
        LayoutElement nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
        nameLayout.preferredWidth = 84f;

        TMP_InputField input = CreateInputField(rowObject.transform, 92f);
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.onEndEdit.AddListener(value => ApplyThresholdInput(index, value));

        Slider slider = CreateSlider(rowObject.transform, 1, 999);
        LayoutElement sliderLayout = slider.gameObject.GetComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.preferredHeight = 28f;
        slider.onValueChanged.AddListener(value =>
        {
            if (isRefreshing)
            {
                return;
            }

            int targetValue = Mathf.RoundToInt(value);
            AttributeGradeSettings.SetThreshold(index, targetValue);
            DebugConsoleManager.Log("Attributes", $"Threshold {AttributeGradeSettings.GetTierLabel(index)} -> {AttributeGradeSettings.GetThreshold(index)}");
            Refresh();
        });

        return new ThresholdRow
        {
            index = index,
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

    private void ApplyThresholdInput(int index, string value)
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

        AttributeGradeSettings.SetThreshold(index, parsed);
        DebugConsoleManager.Log("Attributes", $"Threshold {AttributeGradeSettings.GetTierLabel(index)} -> {AttributeGradeSettings.GetThreshold(index)}");
        Refresh();
    }

    private void CreateButtonRow(Transform parent)
    {
        GameObject rowObject = CreateRect("ButtonRow", parent).gameObject;
        LayoutElement rowLayout = rowObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 44f;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateWideButton(rowObject.transform, "预设：新生", () =>
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

        CreateWideButton(rowObject.transform, "预设：封顶", () =>
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

        CreateWideButton(rowObject.transform, "状态归位", () =>
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

        CreateWideButton(rowObject.transform, "重置评级阈值", () =>
        {
            AttributeGradeSettings.ResetDefaults();
            DebugConsoleManager.Log("Attributes", "Reset attribute grade thresholds");
            Refresh();
        });
    }

    private Slider CreateSlider(Transform parent, int min, int max)
    {
        GameObject sliderObject = CreateRect("Slider", parent).gameObject;
        LayoutElement sliderLayout = sliderObject.GetComponent<LayoutElement>();
        if (sliderLayout == null)
        {
            sliderLayout = sliderObject.AddComponent<LayoutElement>();
        }
        sliderLayout.minHeight = 28f;
        Image background = sliderObject.AddComponent<Image>();
        background.color = SliderBackgroundColor;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = CreateRect("FillArea", sliderObject.transform).gameObject;
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.sizeDelta = new Vector2(-18f, 12f);
        fillAreaRect.anchoredPosition = Vector2.zero;

        GameObject fill = CreateRect("Fill", fillArea.transform).gameObject;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = SliderFillColor;
        slider.fillRect = fillRect;

        GameObject handleArea = CreateRect("HandleArea", sliderObject.transform).gameObject;
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = CreateRect("Handle", handleArea.transform).gameObject;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(14f, 14f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        handleImage.sprite = SliderHandleSprite;
        handleImage.type = Image.Type.Sliced;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private TMP_InputField CreateInputField(Transform parent, float width)
    {
        GameObject inputObject = CreateRect("Input", parent).gameObject;
        SetFixedSize(inputObject, width, 38f);
        Image background = inputObject.AddComponent<Image>();
        background.color = FieldColor;

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();

        GameObject viewport = CreateRect("Viewport", inputObject.transform).gameObject;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 4f);
        viewportRect.offsetMax = new Vector2(-10f, -4f);
        viewport.AddComponent<RectMask2D>();

        TextMeshProUGUI text = CreateLabel(viewport.transform, string.Empty, 15f, TextColor, 30f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI placeholder = CreateLabel(viewport.transform, "0", 15f, new Color(0.55f, 0.55f, 0.6f), 30f);
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

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 18f, Color.white, 34f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private Button CreateWideButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        Button button = CreateTinyButton(parent, label, onClick);
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 162f;
        layout.preferredHeight = 40f;
        button.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14f;
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height)
    {
        GameObject labelObject = CreateRect("Label", parent).gameObject;
        LayoutElement layout = labelObject.AddComponent<LayoutElement>();
        float resolvedHeight = Mathf.Max(height, fontSize + 14f);
        layout.preferredHeight = resolvedHeight;
        layout.minHeight = resolvedHeight;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(2f, 4f, 2f, 4f);
        label.extraPadding = true;

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
