#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 公式调试模块 —— 多周目传承公式计算器
/// </summary>
public class FormulaModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray  = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color BtnColor  = new Color(0.20f, 0.35f, 0.60f, 1.0f);

    private TMP_Dropdown formulaDropdown;
    private TMP_InputField param1Input;
    private TMP_InputField param2Input;
    private TMP_InputField param3Input;
    private TextMeshProUGUI param1Label;
    private TextMeshProUGUI param2Label;
    private TextMeshProUGUI param3Label;
    private TextMeshProUGUI resultText;
    private GameObject param3Row;

    // 公式类型
    private enum FormulaType { AttributeInherit, AffinityInherit, MoneyInherit }
    private FormulaType currentFormula = FormulaType.AttributeInherit;

    public void Init(RectTransform parent)
    {
        // 滚动容器
        GameObject scrollObj = CreateUIElement("ScrollView", parent);
        StretchFull(scrollObj.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

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
        vlg.spacing = 10f;
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
        CreateLabel(content.transform, "— 传承公式计算器 —", 18f, TextGold, 30f);

        // 公式选择下拉框
        CreateDropdown(content.transform);

        // 参数输入1
        param1Label = null;
        param1Input = CreateParamRow(content.transform, "基础值", "10", out param1Label);

        // 参数输入2
        param2Label = null;
        param2Input = CreateParamRow(content.transform, "上周目终值", "50", out param2Label);

        // 参数输入3（条件显示）
        param3Row = CreateUIElement("Param3Row", content.transform);
        RectTransform p3RT = param3Row.GetComponent<RectTransform>();
        p3RT.sizeDelta = new Vector2(0, 40f);

        HorizontalLayoutGroup p3hlg = param3Row.AddComponent<HorizontalLayoutGroup>();
        p3hlg.spacing = 10f;
        p3hlg.padding = new RectOffset(4, 4, 2, 2);
        p3hlg.childAlignment = TextAnchor.MiddleLeft;
        p3hlg.childControlWidth = false;
        p3hlg.childControlHeight = true;
        p3hlg.childForceExpandWidth = false;
        p3hlg.childForceExpandHeight = true;

        param3Label = CreateLabel(param3Row.transform, "周目数", 15f, TextWhite, 36f, 120f);
        param3Input = CreateInputField(param3Row.transform, "2", 160f, 32f);
        param3Input.contentType = TMP_InputField.ContentType.IntegerNumber;

        // 计算按钮
        CreateButton(content.transform, "计算", () => Calculate());

        // 结果
        resultText = CreateLabel(content.transform, "结果将显示在此处", 15f, TextGray, 120f);
        resultText.enableWordWrapping = true;

        // 初始配置
        UpdateParamLabels();
    }

    public void Refresh()
    {
        // 无特殊刷新需求
    }

    private void CreateDropdown(Transform parent)
    {
        GameObject ddRow = CreateUIElement("DropdownRow", parent);
        RectTransform rowRT = ddRow.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 44f);

        HorizontalLayoutGroup hlg = ddRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateLabel(ddRow.transform, "公式类型", 15f, TextWhite, 40f, 100f);

        // TMP Dropdown
        GameObject ddObj = CreateUIElement("Dropdown", ddRow.transform);
        RectTransform ddRT = ddObj.GetComponent<RectTransform>();
        ddRT.sizeDelta = new Vector2(240f, 36f);

        LayoutElement ddLE = ddObj.AddComponent<LayoutElement>();
        ddLE.preferredWidth = 240f;

        Image ddBg = ddObj.AddComponent<Image>();
        ddBg.color = new Color(0.12f, 0.12f, 0.18f, 0.90f);

        // Label
        GameObject labelObj = CreateUIElement("Label", ddObj.transform);
        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10, 2);
        labelRT.offsetMax = new Vector2(-30, -2);
        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.fontSize = 14f;
        labelTMP.color = TextWhite;
        labelTMP.alignment = TextAlignmentOptions.Left;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            labelTMP.font = FontManager.Instance.ChineseFont;

        // Arrow
        GameObject arrowObj = CreateUIElement("Arrow", ddObj.transform);
        RectTransform arrowRT = arrowObj.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.pivot = new Vector2(1, 0.5f);
        arrowRT.sizeDelta = new Vector2(20, 20);
        arrowRT.anchoredPosition = new Vector2(-8, 0);
        TextMeshProUGUI arrowTxt = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowTxt.text = "▼";
        arrowTxt.fontSize = 12f;
        arrowTxt.color = TextWhite;
        arrowTxt.alignment = TextAlignmentOptions.Center;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            arrowTxt.font = FontManager.Instance.ChineseFont;

        // Template
        GameObject templateObj = CreateUIElement("Template", ddObj.transform);
        RectTransform templateRT = templateObj.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = Vector2.zero;
        templateRT.sizeDelta = new Vector2(0, 120);

        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = new Color(0.10f, 0.10f, 0.16f, 0.95f);

        ScrollRect templateScroll = templateObj.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;

        // Viewport in template
        GameObject tViewport = CreateUIElement("Viewport", templateObj.transform);
        RectTransform tViewportRT = tViewport.GetComponent<RectTransform>();
        tViewportRT.anchorMin = Vector2.zero;
        tViewportRT.anchorMax = Vector2.one;
        tViewportRT.offsetMin = new Vector2(2, 2);
        tViewportRT.offsetMax = new Vector2(-2, -2);
        tViewport.AddComponent<RectMask2D>();
        templateScroll.viewport = tViewportRT;

        // Content in template viewport
        GameObject tContent = CreateUIElement("Content", tViewport.transform);
        RectTransform tContentRT = tContent.GetComponent<RectTransform>();
        tContentRT.anchorMin = new Vector2(0, 1);
        tContentRT.anchorMax = new Vector2(1, 1);
        tContentRT.pivot = new Vector2(0.5f, 1);
        tContentRT.anchoredPosition = Vector2.zero;
        templateScroll.content = tContentRT;

        // Item
        GameObject itemObj = CreateUIElement("Item", tContent.transform);
        RectTransform itemRT = itemObj.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0, 30);
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);

        Toggle itemToggle = itemObj.AddComponent<Toggle>();

        // Item background
        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.15f, 0.15f, 0.22f, 0.5f);

        // Item checkmark (invisible)
        GameObject checkObj = CreateUIElement("ItemCheckmark", itemObj.transform);
        RectTransform checkRT = checkObj.GetComponent<RectTransform>();
        checkRT.sizeDelta = new Vector2(0, 0);
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = Color.clear;
        itemToggle.graphic = checkImg;
        itemToggle.targetGraphic = itemBg;

        // Item label
        GameObject itemLabelObj = CreateUIElement("ItemLabel", itemObj.transform);
        StretchFull(itemLabelObj.GetComponent<RectTransform>());
        RectTransform ilRT = itemLabelObj.GetComponent<RectTransform>();
        ilRT.offsetMin = new Vector2(10, 0);
        TextMeshProUGUI itemLabelTMP = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabelTMP.fontSize = 14f;
        itemLabelTMP.color = TextWhite;
        itemLabelTMP.alignment = TextAlignmentOptions.Left;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            itemLabelTMP.font = FontManager.Instance.ChineseFont;

        templateObj.SetActive(false);

        // Create dropdown
        formulaDropdown = ddObj.AddComponent<TMP_Dropdown>();
        formulaDropdown.template = templateRT;
        formulaDropdown.captionText = labelTMP;
        formulaDropdown.itemText = itemLabelTMP;
        formulaDropdown.targetGraphic = ddBg;

        formulaDropdown.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("属性传承"),
            new TMP_Dropdown.OptionData("好感传承"),
            new TMP_Dropdown.OptionData("金钱传承"),
        };

        formulaDropdown.value = 0;
        formulaDropdown.onValueChanged.AddListener((idx) =>
        {
            currentFormula = (FormulaType)idx;
            UpdateParamLabels();
        });
    }

    private void UpdateParamLabels()
    {
        switch (currentFormula)
        {
            case FormulaType.AttributeInherit:
                if (param1Label != null) param1Label.text = "基础值";
                if (param2Label != null) param2Label.text = "上周目终值";
                if (param3Label != null) param3Label.text = "目标周目";
                if (param3Row != null) param3Row.SetActive(true);
                break;

            case FormulaType.AffinityInherit:
                if (param1Label != null) param1Label.text = "上周目好感";
                if (param2Label != null) param2Label.text = "是否恋人(0/1)";
                if (param3Row != null) param3Row.SetActive(false);
                break;

            case FormulaType.MoneyInherit:
                if (param1Label != null) param1Label.text = "上周目金钱";
                if (param2Label != null) param2Label.text = "目标周目";
                if (param3Row != null) param3Row.SetActive(false);
                break;
        }
    }

    private void Calculate()
    {
        int p1 = 0, p2 = 0, p3 = 2;
        if (param1Input != null) int.TryParse(param1Input.text, out p1);
        if (param2Input != null) int.TryParse(param2Input.text, out p2);
        if (param3Input != null) int.TryParse(param3Input.text, out p3);

        string result = "";

        switch (currentFormula)
        {
            case FormulaType.AttributeInherit:
                float rate = NewGamePlusData.GetInheritRate(p3);
                int inherited = NewGamePlusData.CalcInheritedAttribute(p1, p2, rate);
                result = $"属性传承计算:\n" +
                         $"  基础值: {p1}\n" +
                         $"  上周目终值: {p2}\n" +
                         $"  目标周目: {p3}\n" +
                         $"  传承比例: {rate:P0}\n" +
                         $"  传承加成: min({p2}×{rate:P0}, 30) = {Mathf.Min((int)(p2 * rate), 30)}\n" +
                         $"  最终初始值: {inherited}";
                break;

            case FormulaType.AffinityInherit:
                bool wasLover = p2 != 0;
                int affResult = NewGamePlusData.CalcInheritedAffinity(p1, wasLover);
                result = $"好感传承计算:\n" +
                         $"  上周目好感: {p1}\n" +
                         $"  是否恋人: {(wasLover ? "是" : "否")}\n" +
                         $"  传承比例: {(wasLover ? "30%" : "20%")}\n" +
                         $"  传承好感: min({(int)(p1 * (wasLover ? 0.3f : 0.2f))}, 60) = {affResult}";
                break;

            case FormulaType.MoneyInherit:
                int moneyResult = NewGamePlusData.CalcInheritedMoney(p1, p2);
                int bonusMoney = NewGamePlusData.GetBonusMoney(p2);
                result = $"金钱传承计算:\n" +
                         $"  上周目金钱: ¥{p1}\n" +
                         $"  目标周目: {p2}\n" +
                         $"  基础: ¥8000\n" +
                         $"  上周目传承: max({p1}×5%, 0) = ¥{Mathf.Max((int)(p1 * 0.05f), 0)}\n" +
                         $"  周目奖励: ¥{bonusMoney}\n" +
                         $"  最终(上限15000): ¥{moneyResult}";
                break;
        }

        if (resultText != null)
            resultText.text = result;

        DebugConsoleManager.Log("公式", $"计算完成: {currentFormula}");
    }

    // ========== 工具方法 ==========

    private TMP_InputField CreateParamRow(Transform parent, string label, string defaultVal,
        out TextMeshProUGUI labelRef)
    {
        GameObject row = CreateUIElement(label + "Row", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 40f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        labelRef = CreateLabel(row.transform, label, 15f, TextWhite, 36f, 120f);
        TMP_InputField input = CreateInputField(row.transform, defaultVal, 160f, 32f);
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        return input;
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40f);

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

        TextMeshProUGUI txt = CreateLabel(btnObj.transform, label, 15f, TextWhite, 40f);
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
