using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// 角色创建UI —— 纯代码构建
/// 开始游戏后显示，用于选择性别、姓名和专业
/// </summary>
public class CharacterCreationUI : MonoBehaviour
{
    public static CharacterCreationUI Instance { get; private set; }

    public event Action OnCreationComplete;

    private GameObject canvasObj;
    private TMP_InputField nameInput;
    private Toggle maleToggle;
    private Toggle femaleToggle;
    private TMP_Dropdown majorDropdown;
    private Button confirmBtn;

    private static readonly Color BgColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    private static readonly Color InputBgColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    private static readonly Color ButtonColor = new Color(0.2f, 0.6f, 0.3f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Show()
    {
        if (canvasObj != null) return;
        BuildUI();
    }

    private void BuildUI()
    {
        canvasObj = new GameObject("CharacterCreationCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // 最顶层
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // 背景
        GameObject panelObj = new GameObject("Panel", typeof(RectTransform));
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = BgColor;

        // 居中容器
        GameObject containerObj = new GameObject("Container", typeof(RectTransform));
        containerObj.transform.SetParent(panelObj.transform, false);
        RectTransform containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(600, 500);
        containerRt.anchoredPosition = Vector2.zero;
        Image containerImg = containerObj.AddComponent<Image>();
        containerImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        // 垂直布局
        VerticalLayoutGroup vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.spacing = 30;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // 标题
        TextMeshProUGUI title = CreateTMP(containerObj.transform, "Title", "新生入学登记");
        title.fontSize = 36;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.85f, 0.3f);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);

        // 姓名输入组
        GameObject nameGroup = new GameObject("NameGroup", typeof(RectTransform));
        nameGroup.transform.SetParent(containerObj.transform, false);
        nameGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
        HorizontalLayoutGroup hlgName = nameGroup.AddComponent<HorizontalLayoutGroup>();
        hlgName.spacing = 20;

        TextMeshProUGUI nameLabel = CreateTMP(nameGroup.transform, "Label", "姓名:");
        nameLabel.fontSize = 24;
        nameLabel.alignment = TextAlignmentOptions.MidlineRight;
        nameLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);

        GameObject inputObj = new GameObject("Input", typeof(RectTransform));
        inputObj.transform.SetParent(nameGroup.transform, false);
        inputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);
        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = InputBgColor;
        nameInput = inputObj.AddComponent<TMP_InputField>();

        TextMeshProUGUI textArea = CreateTMP(inputObj.transform, "Text", "");
        textArea.fontSize = 24;
        textArea.alignment = TextAlignmentOptions.MidlineLeft;
        textArea.color = Color.white;
        RectTransform textRt = textArea.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 0);
        textRt.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI placeholder = CreateTMP(inputObj.transform, "Placeholder", "请输入姓名...");
        placeholder.fontSize = 24;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f);
        RectTransform phRt = placeholder.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10, 0);
        phRt.offsetMax = new Vector2(-10, 0);

        nameInput.textComponent = textArea;
        nameInput.placeholder = placeholder;
        nameInput.characterLimit = 6;
        nameInput.onValueChanged.AddListener(OnInputChanged);

        // 性别选择组
        GameObject genderGroup = new GameObject("GenderGroup", typeof(RectTransform));
        genderGroup.transform.SetParent(containerObj.transform, false);
        genderGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
        HorizontalLayoutGroup hlgGender = genderGroup.AddComponent<HorizontalLayoutGroup>();
        hlgGender.spacing = 20;

        TextMeshProUGUI genderLabel = CreateTMP(genderGroup.transform, "Label", "性别:");
        genderLabel.fontSize = 24;
        genderLabel.alignment = TextAlignmentOptions.MidlineRight;
        genderLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);

        ToggleGroup tg = genderGroup.AddComponent<ToggleGroup>();

        maleToggle = CreateToggle(genderGroup.transform, "男", tg, true);
        femaleToggle = CreateToggle(genderGroup.transform, "女", tg, false);

        // 专业选择组
        GameObject majorGroup = new GameObject("MajorGroup", typeof(RectTransform));
        majorGroup.transform.SetParent(containerObj.transform, false);
        majorGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
        HorizontalLayoutGroup hlgMajor = majorGroup.AddComponent<HorizontalLayoutGroup>();
        hlgMajor.spacing = 20;

        TextMeshProUGUI majorLabel = CreateTMP(majorGroup.transform, "Label", "专业:");
        majorLabel.fontSize = 24;
        majorLabel.alignment = TextAlignmentOptions.MidlineRight;
        majorLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);

        GameObject dropdownObj = new GameObject("Dropdown", typeof(RectTransform));
        dropdownObj.transform.SetParent(majorGroup.transform, false);
        dropdownObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);
        Image dropdownBg = dropdownObj.AddComponent<Image>();
        dropdownBg.color = InputBgColor;
        majorDropdown = dropdownObj.AddComponent<TMP_Dropdown>();

        TextMeshProUGUI dpLabel = CreateTMP(dropdownObj.transform, "Label", "生物科学");
        dpLabel.fontSize = 24;
        dpLabel.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform dpLabelRt = dpLabel.GetComponent<RectTransform>();
        dpLabelRt.anchorMin = Vector2.zero;
        dpLabelRt.anchorMax = Vector2.one;
        dpLabelRt.offsetMin = new Vector2(10, 0);
        dpLabelRt.offsetMax = new Vector2(-30, 0);

        majorDropdown.targetGraphic = dropdownBg;
        majorDropdown.captionText = dpLabel;
        majorDropdown.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("生物科学"),
            new TMP_Dropdown.OptionData("农学"),
            new TMP_Dropdown.OptionData("动物医学"),
            new TMP_Dropdown.OptionData("计算机科学"),
            new TMP_Dropdown.OptionData("汉语言文学")
        };

        // 确认按钮
        GameObject btnObj = new GameObject("ConfirmBtn", typeof(RectTransform));
        btnObj.transform.SetParent(containerObj.transform, false);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = ButtonColor;
        confirmBtn = btnObj.AddComponent<Button>();
        confirmBtn.onClick.AddListener(OnConfirmClicked);
        confirmBtn.interactable = false;

        TextMeshProUGUI btnText = CreateTMP(btnObj.transform, "Text", "开启大学生活");
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        RectTransform btnTextRt = btnText.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;
    }

    private Toggle CreateToggle(Transform parent, string label, ToggleGroup group, bool isOn)
    {
        GameObject toggleObj = new GameObject($"Toggle_{label}", typeof(RectTransform));
        toggleObj.transform.SetParent(parent, false);
        toggleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 50);

        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.group = group;
        toggle.isOn = isOn;

        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(toggleObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.5f);
        bgRt.anchorMax = new Vector2(0, 0.5f);
        bgRt.sizeDelta = new Vector2(30, 30);
        bgRt.anchoredPosition = new Vector2(15, 0);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = InputBgColor;

        GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform));
        checkObj.transform.SetParent(bgObj.transform, false);
        RectTransform checkRt = checkObj.GetComponent<RectTransform>();
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.sizeDelta = new Vector2(-10, -10);
        checkRt.anchoredPosition = Vector2.zero;
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(1f, 0.85f, 0.3f);

        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;

        TextMeshProUGUI text = CreateTMP(toggleObj.transform, "Label", label);
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(40, 0);
        textRt.offsetMax = Vector2.zero;

        return toggle;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = Color.white;
        return tmp;
    }

    private void OnInputChanged(string text)
    {
        confirmBtn.interactable = !string.IsNullOrWhiteSpace(text);
    }

    private void OnConfirmClicked()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.PlayerName = nameInput.text.Trim();
            GameState.Instance.PlayerGender = femaleToggle.isOn ? 1 : 0;
            GameState.Instance.PlayerMajor = majorDropdown.options[majorDropdown.value].text;

            Debug.Log($"[CharacterCreation] 角色创建完成: {GameState.Instance.PlayerName}, {(GameState.Instance.PlayerGender == 0 ? "男" : "女")}, {GameState.Instance.PlayerMajor}");
        }

        Destroy(canvasObj);
        canvasObj = null;

        OnCreationComplete?.Invoke();
    }
}
