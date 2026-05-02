using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    private Canvas inventoryCanvas;
    private GameObject overlayObject;
    private GameObject panelRoot;
    private Transform itemListContent;
    private TextMeshProUGUI summaryText;
    private TextMeshProUGUI itemNameText;
    private TextMeshProUGUI itemCategoryText;
    private TextMeshProUGUI itemDescriptionText;
    private TextMeshProUGUI itemEffectText;
    private TextMeshProUGUI itemCountText;
    private Button useButton;
    private TextMeshProUGUI useButtonText;

    private string selectedItemId;

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.55f);
    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.12f, 0.96f);
    private static readonly Color HeaderColor = new Color(0.10f, 0.10f, 0.16f, 0.96f);
    private static readonly Color CardColor = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    private static readonly Color AccentColor = new Color(0.26f, 0.46f, 0.76f, 1f);
    private static readonly Color AccentPressedColor = new Color(0.20f, 0.34f, 0.58f, 1f);
    private static readonly Color DisabledColor = new Color(0.28f, 0.28f, 0.32f, 1f);
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color TextGray = new Color(0.70f, 0.72f, 0.76f, 1f);
    private static readonly Color TextGold = new Color(1f, 0.84f, 0.32f, 1f);

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnEnable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            TogglePanel();
        }
        else if (IsOpen && PauseMenuUI.ShouldBlockUnderlyingEscape())
        {
            return;
        }
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }
    }

    public void TogglePanel()
    {
        if (IsOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    public void OpenPanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        panelRoot.SetActive(true);
        overlayObject.SetActive(true);
        Refresh();
    }

    public void ClosePanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        panelRoot.SetActive(false);
        overlayObject.SetActive(false);
    }

    public void Refresh()
    {
        if (panelRoot == null || InventorySystem.Instance == null)
        {
            return;
        }

        foreach (Transform child in itemListContent)
        {
            Destroy(child.gameObject);
        }

        List<InventorySystem.InventoryEntry> entries = InventorySystem.Instance.GetAllEntries();
        summaryText.text = $"Total {InventorySystem.Instance.GetTotalItemCount()}";

        if (entries.Count == 0)
        {
            selectedItemId = null;
            CreateListHint("Inventory is empty");
            RefreshDetails(null);
            return;
        }

        bool hasSelection = false;
        foreach (InventorySystem.InventoryEntry entry in entries)
        {
            if (entry.definition.id == selectedItemId)
            {
                hasSelection = true;
            }

            CreateItemRow(entry);
        }

        if (!hasSelection)
        {
            selectedItemId = entries[0].definition.id;
        }

        RefreshDetails(ShopSystem.Instance != null ? ShopSystem.Instance.GetItemDefinition(selectedItemId) : null);
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("InventoryCanvas");
        canvasObject.transform.SetParent(transform, false);

        inventoryCanvas = canvasObject.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        overlayObject = CreatePanel("Overlay", inventoryCanvas.transform, OverlayColor);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayObject.AddComponent<Button>().onClick.AddListener(ClosePanel);

        panelRoot = CreatePanel("InventoryPanel", inventoryCanvas.transform, PanelColor);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1100f, 680f);

        BuildHeader();
        BuildColumns();

        panelRoot.SetActive(false);
        overlayObject.SetActive(false);
    }

    private void BuildHeader()
    {
        GameObject header = CreatePanel("Header", panelRoot.transform, HeaderColor);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 64f);

        TextMeshProUGUI titleText = CreateText("Title", header.transform, "Inventory", 26f, TextGold, TextAlignmentOptions.Left, new Vector2(240f, 40f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(0f, 0.5f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.anchoredPosition = new Vector2(28f, 0f);

        summaryText = CreateText("Summary", header.transform, "Total 0", 18f, TextWhite, TextAlignmentOptions.Right, new Vector2(220f, 32f));
        RectTransform summaryRect = summaryText.rectTransform;
        summaryRect.anchorMin = new Vector2(1f, 0.5f);
        summaryRect.anchorMax = new Vector2(1f, 0.5f);
        summaryRect.pivot = new Vector2(1f, 0.5f);
        summaryRect.anchoredPosition = new Vector2(-88f, 0f);

        Button closeButton = CreateButton("CloseButton", header.transform, "X", new Vector2(44f, 44f), AccentColor, AccentPressedColor);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(-20f, 0f);
        closeButton.onClick.AddListener(ClosePanel);
    }

    private void BuildColumns()
    {
        GameObject leftPanel = CreatePanel("LeftPanel", panelRoot.transform, new Color(0.09f, 0.09f, 0.14f, 0.96f));
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0f, 1f);
        leftRect.offsetMin = new Vector2(0f, 0f);
        leftRect.offsetMax = new Vector2(320f, -64f);

        TextMeshProUGUI leftTitle = CreateText("ListTitle", leftPanel.transform, "Items", 20f, TextWhite, TextAlignmentOptions.Left, new Vector2(200f, 30f));
        leftTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        leftTitle.rectTransform.anchorMax = new Vector2(0f, 1f);
        leftTitle.rectTransform.pivot = new Vector2(0f, 1f);
        leftTitle.rectTransform.anchoredPosition = new Vector2(20f, -18f);

        ScrollRect scrollRect = CreateScrollView("ItemScroll", leftPanel.transform, new Vector2(0f, 0f));
        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(12f, 12f);
        scrollRectTransform.offsetMax = new Vector2(-12f, -56f);
        itemListContent = scrollRect.content;

        GameObject rightPanel = CreatePanel("RightPanel", panelRoot.transform, CardColor);
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.offsetMin = new Vector2(336f, 0f);
        rightRect.offsetMax = new Vector2(0f, -64f);

        VerticalLayoutGroup layout = rightPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 28, 28);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        itemNameText = CreateText("ItemName", rightPanel.transform, "No item selected", 28f, TextGold, TextAlignmentOptions.Left, new Vector2(700f, 38f));
        itemCategoryText = CreateText("ItemCategory", rightPanel.transform, "", 18f, TextGray, TextAlignmentOptions.Left, new Vector2(700f, 28f));
        itemCountText = CreateText("ItemCount", rightPanel.transform, "", 18f, TextWhite, TextAlignmentOptions.Left, new Vector2(700f, 28f));
        itemDescriptionText = CreateText("ItemDescription", rightPanel.transform, "", 19f, TextWhite, TextAlignmentOptions.TopLeft, new Vector2(700f, 110f));
        itemDescriptionText.enableWordWrapping = true;
        itemEffectText = CreateText("ItemEffects", rightPanel.transform, "", 18f, TextWhite, TextAlignmentOptions.TopLeft, new Vector2(700f, 180f));
        itemEffectText.enableWordWrapping = true;

        GameObject buttonRow = new GameObject("ButtonRow");
        buttonRow.transform.SetParent(rightPanel.transform, false);
        RectTransform buttonRect = buttonRow.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(700f, 56f);

        useButton = CreateButton("UseButton", buttonRow.transform, "Use", new Vector2(180f, 50f), AccentColor, AccentPressedColor);
        useButton.onClick.AddListener(OnUseButtonClicked);
        useButtonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
        RectTransform useRect = useButton.GetComponent<RectTransform>();
        useRect.anchorMin = new Vector2(0f, 0.5f);
        useRect.anchorMax = new Vector2(0f, 0.5f);
        useRect.pivot = new Vector2(0f, 0.5f);
        useRect.anchoredPosition = new Vector2(0f, 0f);
    }

    private void CreateItemRow(InventorySystem.InventoryEntry entry)
    {
        Button button = CreateButton($"Item_{entry.definition.id}", itemListContent, "", new Vector2(0f, 72f), CardColor, AccentPressedColor);
        LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 72f;

        Image image = button.GetComponent<Image>();
        image.color = entry.definition.id == selectedItemId ? AccentColor : CardColor;

        HorizontalLayoutGroup layout = button.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 10, 10);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateText("Name", button.transform, entry.definition.displayName, 18f, TextWhite, TextAlignmentOptions.Left, new Vector2(150f, 26f));
        CreateText("Category", button.transform, ShopSystem.Instance.GetCategoryDisplayName(entry.definition.category), 16f, TextGray, TextAlignmentOptions.Left, new Vector2(90f, 24f));
        CreateText("Count", button.transform, $"x{entry.quantity}", 18f, TextGold, TextAlignmentOptions.Right, new Vector2(60f, 26f));

        string itemId = entry.definition.id;
        button.onClick.AddListener(() =>
        {
            selectedItemId = itemId;
            Refresh();
        });
    }

    private void CreateListHint(string message)
    {
        TextMeshProUGUI hint = CreateText("EmptyHint", itemListContent, message, 18f, TextGray, TextAlignmentOptions.Center, new Vector2(220f, 40f));
        LayoutElement layoutElement = hint.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 56f;
    }

    private void RefreshDetails(ShopItemDefinition definition)
    {
        if (definition == null)
        {
            itemNameText.text = "No item selected";
            itemCategoryText.text = "";
            itemCountText.text = "";
            itemDescriptionText.text = "Items bought from the shop will be stored here.";
            itemEffectText.text = "";
            SetUseButtonState(false, "Use");
            return;
        }

        int quantity = InventorySystem.Instance != null ? InventorySystem.Instance.GetItemCount(definition.id) : 0;
        itemNameText.text = definition.displayName;
        itemCategoryText.text = $"Category: {ShopSystem.Instance.GetCategoryDisplayName(definition.category)}";
        itemCountText.text = $"Owned: x{quantity}";
        itemDescriptionText.text = definition.description;
        itemEffectText.text = BuildEffectText(definition.effects);
        SetUseButtonState(quantity > 0 && definition.canUse, definition.useVerb);
    }

    private void OnUseButtonClicked()
    {
        if (InventorySystem.Instance == null || string.IsNullOrEmpty(selectedItemId))
        {
            return;
        }

        if (!InventorySystem.Instance.UseItem(selectedItemId))
        {
            return;
        }

        ShopItemDefinition definition = ShopSystem.Instance != null
            ? ShopSystem.Instance.GetItemDefinition(selectedItemId)
            : null;

        if (definition != null)
        {
            itemDescriptionText.text = $"{definition.description}\n\nUsed 1 item.";
        }

        Refresh();
    }

    private void SetUseButtonState(bool interactable, string verb)
    {
        if (useButton == null)
        {
            return;
        }

        useButton.interactable = interactable;
        useButton.GetComponent<Image>().color = interactable ? AccentColor : DisabledColor;
        useButtonText.text = string.IsNullOrEmpty(verb) ? "Use" : verb;
    }

    private string BuildEffectText(AttributeEffect[] effects)
    {
        if (effects == null || effects.Length == 0)
        {
            return "No attribute effect";
        }

        List<string> parts = new List<string>();
        foreach (AttributeEffect effect in effects)
        {
            string sign = effect.amount >= 0 ? "+" : "";
            parts.Add($"{effect.attributeName}{sign}{effect.amount}");
        }

        return $"Effect: {string.Join("  ", parts)}";
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 size, Color normalColor, Color pressedColor)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = normalColor;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color(
            Mathf.Clamp01(normalColor.r + 0.08f),
            Mathf.Clamp01(normalColor.g + 0.08f),
            Mathf.Clamp01(normalColor.b + 0.08f),
            normalColor.a);
        colors.pressedColor = pressedColor;
        colors.selectedColor = normalColor;
        colors.disabledColor = DisabledColor;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 18f, TextWhite, TextAlignmentOptions.Center, size);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }

        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private ScrollRect CreateScrollView(string name, Transform parent, Vector2 size)
    {
        GameObject scrollObject = new GameObject(name);
        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
        scrollRect.sizeDelta = size;

        Image background = scrollObject.AddComponent<Image>();
        background.color = new Color(0.06f, 0.06f, 0.10f, 0.70f);

        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -8f);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewportRect;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        return scroll;
    }
}
