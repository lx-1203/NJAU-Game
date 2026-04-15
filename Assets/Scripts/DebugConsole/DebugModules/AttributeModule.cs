#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 属性调试模块 —— 9 个属性滑块 + 金钱输入 + 行动点显示
/// </summary>
public class AttributeModule : MonoBehaviour, IDebugModule
{
    // ========== 颜色 ==========
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color SliderBg  = new Color(0.15f, 0.15f, 0.22f, 0.80f);
    private static readonly Color SliderFill = new Color(0.25f, 0.50f, 0.80f, 1.0f);

    // ========== 属性定义 ==========
    private struct AttrDef
    {
        public string label;
        public string propertyName;
        public int maxValue;

        public AttrDef(string label, string propertyName, int maxValue = 100)
        {
            this.label = label;
            this.propertyName = propertyName;
            this.maxValue = maxValue;
        }
    }

    private static readonly AttrDef[] attrDefs =
    {
        new AttrDef("学力", "Study"),
        new AttrDef("魅力", "Charm"),
        new AttrDef("体魄", "Physique"),
        new AttrDef("领导力", "Leadership"),
        new AttrDef("压力", "Stress"),
        new AttrDef("心情", "Mood"),
        new AttrDef("黑暗值", "Darkness"),
        new AttrDef("负罪感", "Guilt"),
        new AttrDef("幸运", "Luck"),
    };

    // ========== UI 引用 ==========
    private readonly List<Slider> attrSliders = new List<Slider>();
    private readonly List<TextMeshProUGUI> valueTexts = new List<TextMeshProUGUI>();
    private TMP_InputField moneyInput;
    private TextMeshProUGUI apText;
    private bool isRefreshing;

    // ========== IDebugModule ==========

    public void Init(RectTransform parent)
    {
        // 创建可滚动的内容区域
        GameObject scrollObj = CreateUIElement("ScrollView", parent);
        StretchFull(scrollObj.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        // Viewport
        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // Content
        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
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
        CreateLabel(content.transform, "— 属性调节器 —", 18f, TextGold, 30f);

        // 9 个属性滑块
        for (int i = 0; i < attrDefs.Length; i++)
        {
            int idx = i;
            CreateAttributeSlider(content.transform, attrDefs[i], idx);
        }

        // 分割线
        CreateSeparator(content.transform);

        // 金钱输入
        CreateMoneyRow(content.transform);

        // 行动点显示
        CreateAPRow(content.transform);
    }

    public void Refresh()
    {
        if (PlayerAttributes.Instance == null) return;

        isRefreshing = true;

        var pa = PlayerAttributes.Instance;
        int[] values = { pa.Study, pa.Charm, pa.Physique, pa.Leadership,
                         pa.Stress, pa.Mood, pa.Darkness, pa.Guilt, pa.Luck };

        for (int i = 0; i < attrSliders.Count && i < values.Length; i++)
        {
            attrSliders[i].SetValueWithoutNotify(values[i]);
            valueTexts[i].text = values[i].ToString();
        }

        if (moneyInput != null && GameState.Instance != null)
        {
            moneyInput.SetTextWithoutNotify(GameState.Instance.Money.ToString());
        }

        if (apText != null && GameState.Instance != null)
        {
            apText.text = $"行动点: {GameState.Instance.ActionPoints} / {GameState.Instance.EffectiveMaxActionPoints}";
        }

        isRefreshing = false;
    }

    // ========== 滑块创建 ==========

    private void CreateAttributeSlider(Transform parent, AttrDef def, int index)
    {
        GameObject row = CreateUIElement(def.label + "Row", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 36f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 属性名标签
        CreateLabel(row.transform, def.label, 15f, TextWhite, 36f, 70f);

        // 滑块
        Slider slider = CreateSlider(row.transform, 0, def.maxValue);
        LayoutElement sliderLE = slider.gameObject.AddComponent<LayoutElement>();
        sliderLE.flexibleWidth = 1f;
        sliderLE.preferredHeight = 24f;

        // 数值文本
        TextMeshProUGUI valText = CreateLabel(row.transform, "0", 14f, TextGold, 36f, 50f);
        valText.alignment = TextAlignmentOptions.Right;

        attrSliders.Add(slider);
        valueTexts.Add(valText);

        // 值变化回调
        slider.onValueChanged.AddListener((val) =>
        {
            if (isRefreshing) return;

            int intVal = Mathf.RoundToInt(val);
            valText.text = intVal.ToString();

            if (PlayerAttributes.Instance == null) return;

            switch (index)
            {
                case 0: PlayerAttributes.Instance.Study = intVal; break;
                case 1: PlayerAttributes.Instance.Charm = intVal; break;
                case 2: PlayerAttributes.Instance.Physique = intVal; break;
                case 3: PlayerAttributes.Instance.Leadership = intVal; break;
                case 4: PlayerAttributes.Instance.Stress = intVal; break;
                case 5: PlayerAttributes.Instance.Mood = intVal; break;
                case 6: PlayerAttributes.Instance.Darkness = intVal; break;
                case 7: PlayerAttributes.Instance.Guilt = intVal; break;
                case 8: PlayerAttributes.Instance.Luck = intVal; break;
            }

            DebugConsoleManager.Log("属性", $"{def.label} → {intVal}");
        });
    }

    private void CreateMoneyRow(Transform parent)
    {
        GameObject row = CreateUIElement("MoneyRow", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 40f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateLabel(row.transform, "金钱", 15f, TextGold, 40f, 70f);

        // InputField
        moneyInput = CreateInputField(row.transform, "0", 300f, 36f);
        moneyInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        moneyInput.onEndEdit.AddListener((val) =>
        {
            if (isRefreshing || GameState.Instance == null) return;
            if (int.TryParse(val, out int money))
            {
                GameState.Instance.Money = money;
                DebugConsoleManager.Log("属性", $"金钱 → {money}");
            }
        });
    }

    private void CreateAPRow(Transform parent)
    {
        GameObject row = CreateUIElement("APRow", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 36f);

        apText = CreateLabel(row.transform, "行动点: - / -", 15f, TextWhite, 36f, 300f);
    }

    // ========== 工具方法 ==========

    private Slider CreateSlider(Transform parent, float min, float max)
    {
        GameObject sliderObj = CreateUIElement("Slider", parent);
        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(300f, 24f);

        Image bg = sliderObj.AddComponent<Image>();
        bg.color = SliderBg;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;

        // Fill Area
        GameObject fillArea = CreateUIElement("FillArea", sliderObj.transform);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(4f, 4f);
        fillAreaRT.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = CreateUIElement("Fill", fillArea.transform);
        StretchFull(fill.GetComponent<RectTransform>());
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = SliderFill;

        // Handle Area
        GameObject handleArea = CreateUIElement("HandleArea", sliderObj.transform);
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUIElement("Handle", handleArea.transform);
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(16f, 16f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        return slider;
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height)
    {
        GameObject inputObj = CreateUIElement("InputField", parent);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.90f);

        // Text Area
        GameObject textArea = CreateUIElement("TextArea", inputObj.transform);
        RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(8, 2);
        textAreaRT.offsetMax = new Vector2(-8, -2);
        textArea.AddComponent<RectMask2D>();

        // Text
        GameObject textObj = CreateUIElement("Text", textArea.transform);
        StretchFull(textObj.GetComponent<RectTransform>());
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14f;
        text.color = TextWhite;
        text.alignment = TextAlignmentOptions.Left;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;

        // Placeholder
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
        tmp.overflowMode = TextOverflowModes.Ellipsis;

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
