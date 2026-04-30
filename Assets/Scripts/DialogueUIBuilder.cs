using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIBuilder : MonoBehaviour
{
    [HideInInspector] public Canvas dialogueCanvas;
    [HideInInspector] public GameObject dialoguePanel;
    [HideInInspector] public Image portraitImage;
    [HideInInspector] public GameObject portraitContainer;
    [HideInInspector] public GameObject nameContainer;
    [HideInInspector] public TextMeshProUGUI nameText;
    [HideInInspector] public TextMeshProUGUI contentText;
    [HideInInspector] public TextMeshProUGUI hintText;
    [HideInInspector] public Button previousButton;
    [HideInInspector] public TextMeshProUGUI previousButtonText;

    [HideInInspector] public GameObject choicePanel;
    [HideInInspector] public Button[] choiceButtons = new Button[4];
    [HideInInspector] public TextMeshProUGUI[] choiceTexts = new TextMeshProUGUI[4];
    [HideInInspector] public TextMeshProUGUI[] choiceHints = new TextMeshProUGUI[4];

    private static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.12f, 0.92f);
    private static readonly Color ChoicePanelColor = new Color(0.05f, 0.05f, 0.12f, 0.88f);
    private static readonly Color NameColor = new Color(0.45f, 0.85f, 1f);
    private static readonly Color HintColor = new Color(0.72f, 0.72f, 0.78f, 0.92f);
    private static readonly Color AccentColor = new Color(0.35f, 0.55f, 0.85f, 1f);
    private static readonly Color ChoiceNormalColor = new Color(0.2f, 0.35f, 0.6f, 1f);
    private static readonly Color ChoiceHoverColor = new Color(0.28f, 0.43f, 0.72f, 1f);
    private static readonly Color ChoicePressedColor = new Color(0.14f, 0.24f, 0.46f, 1f);
    private static readonly Color ChoiceDisabledColor = new Color(0.25f, 0.25f, 0.3f, 0.8f);
    private static readonly Color ChoiceDisabledTextColor = new Color(0.58f, 0.58f, 0.62f);
    private static readonly Color PortraitPlaceholderColor = new Color(0.2f, 0.2f, 0.3f, 0.85f);

    public void BuildDialogueUI()
    {
        CreateCanvas();
        CreateDialoguePanel();
        CreateChoicePanel();

        dialoguePanel.SetActive(false);
        choicePanel.SetActive(false);
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("DialogueCanvas");
        canvasObject.transform.SetParent(transform, false);

        dialogueCanvas = canvasObject.AddComponent<Canvas>();
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void CreateDialoguePanel()
    {
        dialoguePanel = CreatePanel("DialoguePanel", dialogueCanvas.transform, PanelColor);
        RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.02f);
        panelRect.anchorMax = new Vector2(0.95f, 0.33f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        TMP_FontAsset font = GetPreferredFont();

        portraitContainer = CreateRectObject("PortraitContainer", dialoguePanel.transform).gameObject;
        RectTransform portraitRect = portraitContainer.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.1f);
        portraitRect.anchorMax = new Vector2(0f, 0.9f);
        portraitRect.sizeDelta = new Vector2(130f, 0f);
        portraitRect.anchoredPosition = new Vector2(80f, 0f);

        GameObject portraitObject = CreateRectObject("PortraitImage", portraitContainer.transform).gameObject;
        StretchFull(portraitObject.GetComponent<RectTransform>());
        portraitImage = portraitObject.AddComponent<Image>();
        portraitImage.color = PortraitPlaceholderColor;
        portraitImage.preserveAspect = true;

        nameContainer = CreateRectObject("NameContainer", dialoguePanel.transform).gameObject;
        RectTransform nameRect = nameContainer.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.12f, 0.73f);
        nameRect.anchorMax = new Vector2(0.95f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        nameText = CreateTMPText(nameContainer.transform, "NameText", 30f, NameColor, TextAlignmentOptions.MidlineLeft, font);
        RectTransform nameTextRect = nameText.GetComponent<RectTransform>();
        nameTextRect.anchorMin = new Vector2(0f, 0.1f);
        nameTextRect.anchorMax = new Vector2(0.6f, 1f);
        nameTextRect.offsetMin = Vector2.zero;
        nameTextRect.offsetMax = Vector2.zero;
        nameText.fontStyle = FontStyles.Bold;

        GameObject lineObject = CreatePanel("NameLine", nameContainer.transform, new Color(NameColor.r, NameColor.g, NameColor.b, 0.32f));
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 0.05f);
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;

        contentText = CreateTMPText(dialoguePanel.transform, "ContentText", 26f, Color.white, TextAlignmentOptions.TopLeft, font);
        RectTransform contentRect = contentText.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.12f, 0.08f);
        contentRect.anchorMax = new Vector2(0.95f, 0.7f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentText.lineSpacing = 10f;

        hintText = CreateTMPText(dialoguePanel.transform, "HintText", 18f, HintColor, TextAlignmentOptions.MidlineRight, font);
        RectTransform hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.55f, 0.02f);
        hintRect.anchorMax = new Vector2(0.98f, 0.16f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        hintText.text = "Space / Click to continue";

        previousButton = CreateButton("PreviousButton", dialoguePanel.transform, "Previous", AccentColor, font);
        RectTransform previousRect = previousButton.GetComponent<RectTransform>();
        previousRect.anchorMin = new Vector2(0.02f, 0.03f);
        previousRect.anchorMax = new Vector2(0.16f, 0.17f);
        previousRect.offsetMin = Vector2.zero;
        previousRect.offsetMax = Vector2.zero;
        previousButtonText = previousButton.GetComponentInChildren<TextMeshProUGUI>();
        previousButton.gameObject.SetActive(false);
    }

    private void CreateChoicePanel()
    {
        choicePanel = CreatePanel("ChoicePanel", dialogueCanvas.transform, ChoicePanelColor);
        RectTransform choiceRect = choicePanel.GetComponent<RectTransform>();
        choiceRect.anchorMin = new Vector2(0.15f, 0.34f);
        choiceRect.anchorMax = new Vector2(0.85f, 0.75f);
        choiceRect.offsetMin = Vector2.zero;
        choiceRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = choicePanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_FontAsset font = GetPreferredFont();
        for (int i = 0; i < 4; i++)
        {
            CreateChoiceButton(i, font);
        }
    }

    private void CreateChoiceButton(int index, TMP_FontAsset font)
    {
        GameObject buttonObject = CreateRectObject($"ChoiceButton_{index}", choicePanel.transform).gameObject;
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0f, 64f);

        Image background = buttonObject.AddComponent<Image>();
        background.color = ChoiceNormalColor;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ChoiceNormalColor;
        colors.highlightedColor = ChoiceHoverColor;
        colors.pressedColor = ChoicePressedColor;
        colors.selectedColor = ChoiceNormalColor;
        colors.disabledColor = ChoiceDisabledColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        VerticalLayoutGroup layout = buttonObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.padding = new RectOffset(10, 10, 6, 6);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI mainText = CreateTMPText(buttonObject.transform, "ChoiceText", 22f, Color.white, TextAlignmentOptions.Center, font);
        mainText.rectTransform.sizeDelta = new Vector2(0f, 30f);

        TextMeshProUGUI hint = CreateTMPText(buttonObject.transform, "ChoiceHint", 14f, ChoiceDisabledTextColor, TextAlignmentOptions.Center, font);
        hint.rectTransform.sizeDelta = new Vector2(0f, 18f);
        hint.gameObject.SetActive(false);

        choiceButtons[index] = button;
        choiceTexts[index] = mainText;
        choiceHints[index] = hint;
    }

    private Button CreateButton(string name, Transform parent, string label, Color background, TMP_FontAsset font)
    {
        GameObject buttonObject = CreateRectObject(name, parent).gameObject;
        Image image = buttonObject.AddComponent<Image>();
        image.color = background;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = background * 1.1f;
        colors.pressedColor = background * 0.85f;
        button.colors = colors;

        TextMeshProUGUI text = CreateTMPText(buttonObject.transform, "Label", 18f, Color.white, TextAlignmentOptions.Center, font);
        StretchFull(text.rectTransform);
        text.text = label;
        return button;
    }

    private GameObject CreatePanel(string name, Transform parent, Color backgroundColor)
    {
        GameObject panel = CreateRectObject(name, parent).gameObject;
        Image background = panel.AddComponent<Image>();
        background.color = backgroundColor;
        return panel;
    }

    private RectTransform CreateRectObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private TextMeshProUGUI CreateTMPText(
        Transform parent,
        string name,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        TMP_FontAsset font)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        if (font != null)
        {
            text.font = font;
        }

        return text;
    }

    private TMP_FontAsset GetPreferredFont()
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            return FontManager.Instance.ChineseFont;
        }

        return null;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
