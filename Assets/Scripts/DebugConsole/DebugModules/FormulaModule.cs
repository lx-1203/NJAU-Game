#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormulaModule : MonoBehaviour, IDebugModule
{
    private enum FormulaType
    {
        AttributeInherit,
        AffinityInherit,
        MoneyInherit
    }

    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.72f, 1f);
    private static readonly Color FieldColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

    private TMP_InputField inputA;
    private TMP_InputField inputB;
    private TMP_InputField inputC;
    private TextMeshProUGUI labelA;
    private TextMeshProUGUI labelB;
    private TextMeshProUGUI labelC;
    private TextMeshProUGUI resultText;
    private GameObject thirdRow;
    private FormulaType currentFormula;

    public void Init(RectTransform parent)
    {
        Transform content = CreateScrollableContent(parent);

        CreateLabel(content, "公式工具", 20f, AccentColor, 34f);
        CreateDropdown(content);

        inputA = CreateInputRow(content, "基础值", "10", out labelA);
        inputB = CreateInputRow(content, "上一周目值", "80", out labelB);
        inputC = CreateInputRow(content, "下一周目", "2", out labelC, out thirdRow);

        CreateButton(content, "计算", () =>
        {
            Calculate();
        });

        resultText = CreateLabel(content, "计算结果会显示在这里。", 14f, TextColor, 120f);
        resultText.enableWordWrapping = true;
        resultText.overflowMode = TextOverflowModes.Overflow;

        UpdateFieldLabels();
    }

    public void Refresh()
    {
    }

    private void Calculate()
    {
        int a = ParseInt(inputA, 0);
        int b = ParseInt(inputB, 0);
        int c = ParseInt(inputC, 0);

        switch (currentFormula)
        {
            case FormulaType.AttributeInherit:
                float rate = NewGamePlusData.GetInheritRate(Mathf.Max(c, 2));
                int attributeResult = NewGamePlusData.CalcInheritedAttribute(a, b, rate);
                resultText.text =
                    $"继承比例：{rate:P0}\n" +
                    $"基础值：{a}\n" +
                    $"上一周目最终值：{b}\n" +
                    $"本周目继承初始值：{attributeResult}";
                break;

            case FormulaType.AffinityInherit:
                bool wasLover = c > 0;
                int affinityResult = NewGamePlusData.CalcInheritedAffinity(a, wasLover);
                resultText.text =
                    $"上周目好感：{a}\n" +
                    $"是否恋人：{(wasLover ? "是" : "否")}\n" +
                    $"继承好感：{affinityResult}";
                break;

            case FormulaType.MoneyInherit:
                int moneyResult = NewGamePlusData.CalcInheritedMoney(a, Mathf.Max(b, 2));
                int bonusMoney = NewGamePlusData.GetBonusMoney(Mathf.Max(b, 2));
                resultText.text =
                    $"上周目金钱：{a}\n" +
                    $"下一周目：{Mathf.Max(b, 2)}\n" +
                    $"周目奖励：{bonusMoney}\n" +
                    $"继承金钱：{moneyResult}";
                break;
        }

        DebugConsoleManager.Log("Formula", $"Calculated {currentFormula}");
    }

    private void CreateDropdown(Transform parent)
    {
        GameObject row = CreateRect("DropdownRow", parent).gameObject;
        row.AddComponent<LayoutElement>().preferredHeight = 40f;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI label = CreateLabel(row.transform, "类型", 15f, TextColor, 36f);
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

        Transform buttonRow = CreateRect("ModeButtons", row.transform);
        HorizontalLayoutGroup buttonLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 8f;
        buttonLayout.childAlignment = TextAnchor.MiddleLeft;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        CreateModeButton(buttonRow, "属性", FormulaType.AttributeInherit);
        CreateModeButton(buttonRow, "好感", FormulaType.AffinityInherit);
        CreateModeButton(buttonRow, "金钱", FormulaType.MoneyInherit);
    }

    private TMP_InputField CreateInputRow(Transform parent, string label, string defaultValue, out TextMeshProUGUI labelText)
    {
        return CreateInputRow(parent, label, defaultValue, out labelText, out _);
    }

    private TMP_InputField CreateInputRow(Transform parent, string label, string defaultValue, out TextMeshProUGUI labelText, out GameObject rowObject)
    {
        rowObject = CreateRect($"{label}Row", parent).gameObject;
        rowObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        labelText = CreateLabel(rowObject.transform, label, 15f, TextColor, 36f);
        labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

        TMP_InputField input = CreateInputField(rowObject.transform, 180f);
        input.SetTextWithoutNotify(defaultValue);
        return input;
    }

    private void UpdateFieldLabels()
    {
        switch (currentFormula)
        {
            case FormulaType.AttributeInherit:
                labelA.text = "基础值";
                labelB.text = "上一周目值";
                labelC.text = "下一周目";
                thirdRow.SetActive(true);
                inputA.SetTextWithoutNotify("10");
                inputB.SetTextWithoutNotify("80");
                inputC.SetTextWithoutNotify("2");
                break;
            case FormulaType.AffinityInherit:
                labelA.text = "上周目好感";
                labelB.text = "保留字段";
                labelC.text = "是否恋人 1/0";
                thirdRow.SetActive(true);
                inputA.SetTextWithoutNotify("80");
                inputB.SetTextWithoutNotify("0");
                inputC.SetTextWithoutNotify("1");
                break;
            case FormulaType.MoneyInherit:
                labelA.text = "上周目金钱";
                labelB.text = "下一周目";
                thirdRow.SetActive(false);
                inputA.SetTextWithoutNotify("12000");
                inputB.SetTextWithoutNotify("2");
                break;
        }
    }

    private int ParseInt(TMP_InputField input, int fallback)
    {
        return input != null && int.TryParse(input.text, out int value) ? value : fallback;
    }

    private void CreateModeButton(Transform parent, string label, FormulaType type)
    {
        GameObject buttonObject = CreateRect($"Mode_{label}", parent).gameObject;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 80f;
        layout.preferredHeight = 32f;

        buttonObject.AddComponent<Image>().color = FieldColor;
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            currentFormula = type;
            UpdateFieldLabels();
        });

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 13f, Color.white, 32f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
    }

    private Transform CreateScrollableContent(RectTransform parent)
    {
        GameObject scrollObject = CreateRect("ScrollView", parent).gameObject;
        StretchFull(scrollObject.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

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
        layout.spacing = 10f;
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

        inputObject.AddComponent<Image>().color = FieldColor;
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
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        return input;
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        buttonObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        buttonObject.AddComponent<Image>().color = ButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 14f, Color.white, 38f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string value, float fontSize, Color color, float height)
    {
        GameObject textObject = CreateRect("Label", parent).gameObject;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        float resolvedHeight = Mathf.Max(height, fontSize + 14f);
        layout.preferredHeight = resolvedHeight;
        layout.minHeight = resolvedHeight;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
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
