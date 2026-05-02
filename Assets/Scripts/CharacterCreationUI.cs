using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationUI : MonoBehaviour
{
    private const string MalePreviewResource = "MalePlayerIdleFrames/IdleFrame_00";
    private const string FemalePreviewResource = "PlayerSprite";

    public static CharacterCreationUI Instance { get; private set; }
    public static bool HasPendingCharacter { get; private set; }
    public static string PendingPlayerName { get; private set; } = "";
    public static int PendingPlayerGender { get; private set; }
    public static string PendingPlayerMajor { get; private set; } = "";

    public event Action OnCreationComplete;

    private GameObject canvasObj;
    private TMP_InputField nameInput;
    private Toggle maleToggle;
    private Toggle femaleToggle;
    private TMP_Dropdown majorDropdown;
    private Button confirmBtn;
    private Image maleCardBg;
    private Image femaleCardBg;

    private static readonly Color BgColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    private static readonly Color InputBgColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    private static readonly Color ButtonColor = new Color(0.2f, 0.6f, 0.3f, 1f);

    public static void ApplyPendingCharacter(string playerName, int playerGender, string playerMajor)
    {
        PendingPlayerName = string.IsNullOrWhiteSpace(playerName) ? StartupFlowSettings.DefaultPlayerName : playerName.Trim();
        PendingPlayerGender = Mathf.Clamp(playerGender, 0, 1);
        PendingPlayerMajor = string.IsNullOrWhiteSpace(playerMajor) ? StartupFlowSettings.DefaultPlayerMajor : playerMajor.Trim();
        HasPendingCharacter = true;

        ApplyPendingCharacterToGameState(GameState.Instance);
    }

    public static void ApplyDefaultPendingCharacter()
    {
        ApplyPendingCharacter(
            StartupFlowSettings.DefaultPlayerName,
            StartupFlowSettings.DefaultPlayerGender,
            StartupFlowSettings.DefaultPlayerMajor);
    }

    public static void ApplyPendingCharacterToGameState(GameState gameState)
    {
        if (gameState == null || !HasPendingCharacter)
        {
            return;
        }

        gameState.PlayerName = PendingPlayerName;
        gameState.PlayerGender = PendingPlayerGender;
        gameState.PlayerMajor = PendingPlayerMajor;
    }

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
        UIFlowGuard.EnsureEventSystem();
        if (canvasObj != null)
            return;

        BuildUI();
    }

    private void BuildUI()
    {
        canvasObj = new GameObject("CharacterCreationCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("Panel", typeof(RectTransform));
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = BgColor;

        GameObject containerObj = new GameObject("Container", typeof(RectTransform));
        containerObj.transform.SetParent(panelObj.transform, false);
        RectTransform containerRt = containerObj.GetComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(900, 700);
        containerRt.anchoredPosition = Vector2.zero;
        Image containerImg = containerObj.AddComponent<Image>();
        containerImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        VerticalLayoutGroup vlg = containerObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 40, 40);
        vlg.spacing = 30;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        TextMeshProUGUI title = CreateTMP(containerObj.transform, "Title", "新生入学登记");
        title.fontSize = 36;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.85f, 0.3f);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);

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

        GameObject inputVpObj = new GameObject("TextViewport", typeof(RectTransform));
        inputVpObj.transform.SetParent(inputObj.transform, false);
        RectTransform inputVpRt = inputVpObj.GetComponent<RectTransform>();
        inputVpRt.anchorMin = Vector2.zero;
        inputVpRt.anchorMax = Vector2.one;
        inputVpRt.offsetMin = new Vector2(10, 0);
        inputVpRt.offsetMax = new Vector2(-10, 0);
        inputVpObj.AddComponent<RectMask2D>();

        TextMeshProUGUI textArea = CreateTMP(inputVpObj.transform, "Text", "");
        textArea.fontSize = 24;
        textArea.alignment = TextAlignmentOptions.MidlineLeft;
        textArea.color = Color.white;
        StretchFull(textArea.GetComponent<RectTransform>());

        TextMeshProUGUI placeholder = CreateTMP(inputVpObj.transform, "Placeholder", "请输入姓名...");
        placeholder.fontSize = 24;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f);
        StretchFull(placeholder.GetComponent<RectTransform>());

        nameInput.textViewport = inputVpRt;
        nameInput.textComponent = textArea;
        nameInput.placeholder = placeholder;
        nameInput.characterLimit = 6;
        nameInput.onValueChanged.AddListener(OnInputChanged);
        nameInput.text = StartupFlowSettings.DefaultPlayerName;

        GameObject genderGroup = new GameObject("GenderGroup", typeof(RectTransform));
        genderGroup.transform.SetParent(containerObj.transform, false);
        genderGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 330);
        HorizontalLayoutGroup hlgGender = genderGroup.AddComponent<HorizontalLayoutGroup>();
        hlgGender.spacing = 28;
        hlgGender.childAlignment = TextAnchor.MiddleCenter;
        hlgGender.childControlWidth = false;
        hlgGender.childControlHeight = false;

        ToggleGroup tg = genderGroup.AddComponent<ToggleGroup>();
        maleToggle = CreateGenderCard(genderGroup.transform, "男", MalePreviewResource, tg, StartupFlowSettings.DefaultPlayerGender != 1, new Color(0.72f, 0.9f, 1f, 1f));
        femaleToggle = CreateGenderCard(genderGroup.transform, "女", FemalePreviewResource, tg, StartupFlowSettings.DefaultPlayerGender == 1, new Color(1f, 0.76f, 0.86f, 1f));
        maleCardBg = maleToggle.targetGraphic as Image;
        femaleCardBg = femaleToggle.targetGraphic as Image;
        maleToggle.onValueChanged.AddListener(_ => RefreshGenderCards());
        femaleToggle.onValueChanged.AddListener(_ => RefreshGenderCards());
        RefreshGenderCards();

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

        TextMeshProUGUI dpLabel = CreateTMP(dropdownObj.transform, "Label", StartupFlowSettings.DefaultPlayerMajor);
        dpLabel.fontSize = 24;
        dpLabel.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform dpLabelRt = dpLabel.GetComponent<RectTransform>();
        dpLabelRt.anchorMin = Vector2.zero;
        dpLabelRt.anchorMax = Vector2.one;
        dpLabelRt.offsetMin = new Vector2(10, 0);
        dpLabelRt.offsetMax = new Vector2(-30, 0);

        TextMeshProUGUI arrowTxt = CreateTMP(dropdownObj.transform, "Arrow", "▼");
        arrowTxt.fontSize = 16;
        arrowTxt.alignment = TextAlignmentOptions.Center;
        RectTransform arrowRt = arrowTxt.GetComponent<RectTransform>();
        arrowRt.anchorMin = new Vector2(1, 0.5f);
        arrowRt.anchorMax = new Vector2(1, 0.5f);
        arrowRt.pivot = new Vector2(1, 0.5f);
        arrowRt.sizeDelta = new Vector2(24, 24);
        arrowRt.anchoredPosition = new Vector2(-6, 0);

        GameObject templateObj = new GameObject("Template", typeof(RectTransform));
        templateObj.transform.SetParent(dropdownObj.transform, false);
        RectTransform templateRt = templateObj.GetComponent<RectTransform>();
        templateRt.anchorMin = new Vector2(0, 0);
        templateRt.anchorMax = new Vector2(1, 0);
        templateRt.pivot = new Vector2(0.5f, 1);
        templateRt.anchoredPosition = Vector2.zero;
        templateRt.sizeDelta = new Vector2(0, 200);
        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        ScrollRect templateScroll = templateObj.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(templateObj.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(2, 2);
        vpRt.offsetMax = new Vector2(-2, -2);
        viewport.AddComponent<RectMask2D>();
        templateScroll.viewport = vpRt;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        templateScroll.content = contentRt;

        GameObject itemObj = new GameObject("Item", typeof(RectTransform));
        itemObj.transform.SetParent(content.transform, false);
        RectTransform itemRt = itemObj.GetComponent<RectTransform>();
        itemRt.sizeDelta = new Vector2(0, 40);
        itemRt.anchorMin = new Vector2(0, 0.5f);
        itemRt.anchorMax = new Vector2(1, 0.5f);
        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.2f, 0.2f, 0.28f, 0.5f);
        Toggle itemToggle = itemObj.AddComponent<Toggle>();

        GameObject checkObj = new GameObject("ItemCheckmark", typeof(RectTransform));
        checkObj.transform.SetParent(itemObj.transform, false);
        checkObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = Color.clear;
        itemToggle.graphic = checkImg;
        itemToggle.targetGraphic = itemBg;

        TextMeshProUGUI itemLabel = CreateTMP(itemObj.transform, "ItemLabel", "");
        itemLabel.fontSize = 22;
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform ilRt = itemLabel.GetComponent<RectTransform>();
        ilRt.anchorMin = Vector2.zero;
        ilRt.anchorMax = Vector2.one;
        ilRt.offsetMin = new Vector2(10, 0);
        ilRt.offsetMax = Vector2.zero;

        templateObj.SetActive(false);

        majorDropdown.template = templateRt;
        majorDropdown.captionText = dpLabel;
        majorDropdown.itemText = itemLabel;
        majorDropdown.targetGraphic = dropdownBg;
        majorDropdown.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData(StartupFlowSettings.DefaultPlayerMajor)
        };
        majorDropdown.value = 0;

        GameObject btnObj = new GameObject("ConfirmBtn", typeof(RectTransform));
        btnObj.transform.SetParent(containerObj.transform, false);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = ButtonColor;
        confirmBtn = btnObj.AddComponent<Button>();
        confirmBtn.onClick.AddListener(OnConfirmClicked);
        confirmBtn.interactable = !string.IsNullOrWhiteSpace(nameInput.text);

        TextMeshProUGUI btnText = CreateTMP(btnObj.transform, "Text", "开启大学生活");
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        StretchFull(btnText.GetComponent<RectTransform>());

        UIInputHelper.FocusSelectable(confirmBtn);
    }

    private Toggle CreateGenderCard(Transform parent, string label, string spriteResource, ToggleGroup group, bool isOn, Color accentColor)
    {
        GameObject cardObj = new GameObject($"GenderCard_{label}", typeof(RectTransform));
        cardObj.transform.SetParent(parent, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(280, 320);

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.18f, 0.18f, 0.24f, 1f);

        Toggle toggle = cardObj.AddComponent<Toggle>();
        toggle.group = group;
        toggle.isOn = isOn;
        toggle.targetGraphic = cardBg;

        GameObject photoObj = new GameObject("Preview", typeof(RectTransform));
        photoObj.transform.SetParent(cardObj.transform, false);
        RectTransform photoRt = photoObj.GetComponent<RectTransform>();
        photoRt.anchorMin = new Vector2(0.5f, 1f);
        photoRt.anchorMax = new Vector2(0.5f, 1f);
        photoRt.pivot = new Vector2(0.5f, 1f);
        photoRt.sizeDelta = new Vector2(220, 235);
        photoRt.anchoredPosition = new Vector2(0, -24);
        Image photoBg = photoObj.AddComponent<Image>();
        photoBg.color = accentColor;

        GameObject portraitObj = new GameObject("Portrait", typeof(RectTransform));
        portraitObj.transform.SetParent(photoObj.transform, false);
        RectTransform portraitRt = portraitObj.GetComponent<RectTransform>();
        portraitRt.anchorMin = Vector2.zero;
        portraitRt.anchorMax = Vector2.one;
        portraitRt.offsetMin = new Vector2(18, 12);
        portraitRt.offsetMax = new Vector2(-18, -12);
        Image portraitImg = portraitObj.AddComponent<Image>();
        portraitImg.sprite = Resources.Load<Sprite>(spriteResource);
        portraitImg.preserveAspect = true;
        portraitImg.color = Color.white;

        GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform));
        checkObj.transform.SetParent(cardObj.transform, false);
        RectTransform checkRt = checkObj.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(1, 1);
        checkRt.anchorMax = new Vector2(1, 1);
        checkRt.pivot = new Vector2(1, 1);
        checkRt.sizeDelta = new Vector2(48, 48);
        checkRt.anchoredPosition = new Vector2(-14, -14);
        TextMeshProUGUI checkText = checkObj.AddComponent<TextMeshProUGUI>();
        checkText.text = "✓";
        checkText.fontSize = 38;
        checkText.fontStyle = FontStyles.Bold;
        checkText.alignment = TextAlignmentOptions.Center;
        checkText.color = new Color(1f, 0.85f, 0.3f);
        ApplyChineseFont(checkText);
        toggle.graphic = checkText;

        TextMeshProUGUI text = CreateTMP(cardObj.transform, "Label", label);
        text.fontSize = 28;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 0);
        textRt.pivot = new Vector2(0.5f, 0);
        textRt.sizeDelta = new Vector2(0, 58);
        textRt.anchoredPosition = Vector2.zero;

        return toggle;
    }

    private void RefreshGenderCards()
    {
        if (maleCardBg != null)
        {
            maleCardBg.color = maleToggle != null && maleToggle.isOn
                ? new Color(0.32f, 0.5f, 0.62f, 1f)
                : new Color(0.18f, 0.18f, 0.24f, 1f);
        }

        if (femaleCardBg != null)
        {
            femaleCardBg.color = femaleToggle != null && femaleToggle.isOn
                ? new Color(0.58f, 0.34f, 0.46f, 1f)
                : new Color(0.18f, 0.18f, 0.24f, 1f);
        }
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = Color.white;
        ApplyChineseFont(tmp);
        return tmp;
    }

    private void OnInputChanged(string text)
    {
        if (confirmBtn != null)
            confirmBtn.interactable = !string.IsNullOrWhiteSpace(text);
    }

    private void Update()
    {
        if (canvasObj == null || !UIInputHelper.IsConfirmPressed())
            return;

        if (confirmBtn != null && confirmBtn.interactable)
            OnConfirmClicked();
    }

    private void OnConfirmClicked()
    {
        ApplyPendingCharacter(
            nameInput.text.Trim(),
            femaleToggle != null && femaleToggle.isOn ? 1 : 0,
            majorDropdown.options[majorDropdown.value].text);

        Destroy(canvasObj);
        canvasObj = null;
        OnCreationComplete?.Invoke();
    }

    private void ApplyChineseFont(TMP_Text text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
