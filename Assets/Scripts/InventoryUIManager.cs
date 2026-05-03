using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance { get; private set; }

    private const string FilterAll = "all";

    private Canvas inventoryCanvas;
    private GameObject overlayObject;
    private GameObject frameObject;
    private GameObject panelRoot;
    private Transform itemListContent;
    private TextMeshProUGUI summaryText;
    private TextMeshProUGUI collectionText;
    private TextMeshProUGUI itemNameText;
    private TextMeshProUGUI itemCategoryText;
    private TextMeshProUGUI itemDescriptionText;
    private TextMeshProUGUI itemEffectText;
    private TextMeshProUGUI itemCountText;
    private Button useButton;
    private TextMeshProUGUI useButtonText;

    private string selectedItemId;
    private string currentCategoryFilter = FilterAll;

    private readonly Dictionary<string, Button> filterButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, TextMeshProUGUI> filterButtonTexts = new Dictionary<string, TextMeshProUGUI>();

    private static readonly Color OverlayColor = new Color(0.20f, 0.15f, 0.10f, 0.42f);
    private static readonly Color FrameColor = new Color32(0xB7, 0xA4, 0x8D, 0xFF);
    private static readonly Color PaperColor = new Color32(0xF7, 0xF1, 0xE5, 0xFF);
    private static readonly Color PaperSoftColor = new Color32(0xFB, 0xF7, 0xEE, 0xFF);
    private static readonly Color GridLineColor = new Color32(0xE9, 0xDE, 0xCC, 0xB0);
    private static readonly Color TabIdleColor = new Color32(0xEE, 0xE8, 0xDF, 0xFF);
    private static readonly Color TabActiveColor = new Color32(0xF7, 0xE7, 0xAA, 0xFF);
    private static readonly Color DetailPanelColor = new Color32(0xFC, 0xF1, 0xCF, 0xFF);
    private static readonly Color CardColor = new Color32(0xFF, 0xFC, 0xF7, 0xFF);
    private static readonly Color CardSelectedColor = new Color32(0xFF, 0xF0, 0xBF, 0xFF);
    private static readonly Color AccentColor = new Color32(0xF5, 0xC3, 0x57, 0xFF);
    private static readonly Color AccentPressedColor = new Color32(0xE6, 0xAF, 0x45, 0xFF);
    private static readonly Color DisabledColor = new Color32(0xCF, 0xC4, 0xB4, 0xFF);
    private static readonly Color TextPrimary = new Color32(0x73, 0x56, 0x3C, 0xFF);
    private static readonly Color TextSecondary = new Color32(0x9B, 0x86, 0x72, 0xFF);
    private static readonly Color TextMuted = new Color32(0xB7, 0xAA, 0x98, 0xFF);
    private static readonly Color PreviewColor = new Color32(0x41, 0x40, 0x3D, 0xFF);

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
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

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
        if (panelRoot == null || frameObject == null)
        {
            return;
        }

        frameObject.SetActive(true);
        panelRoot.SetActive(true);
        overlayObject.SetActive(true);
        Refresh();
    }

    public void ClosePanel()
    {
        if (panelRoot == null || frameObject == null)
        {
            return;
        }

        frameObject.SetActive(false);
        panelRoot.SetActive(false);
        overlayObject.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClosePanel();
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

        List<InventorySystem.InventoryEntry> entries = InventorySystem.Instance
            .GetAllEntries()
            .OrderBy(entry => ShopSystem.Instance != null ? ShopSystem.Instance.GetCategoryDisplayName(entry.definition.category) : entry.definition.category)
            .ThenBy(entry => entry.definition.displayName)
            .ToList();

        List<InventorySystem.InventoryEntry> filteredEntries = entries
            .Where(entry => currentCategoryFilter == FilterAll || entry.definition.category == currentCategoryFilter)
            .ToList();

        summaryText.text = "背包";
        collectionText.text = $"已收集 {entries.Count}/{GetTotalCollectableCount()}";
        RefreshFilterVisuals(entries);

        if (filteredEntries.Count == 0)
        {
            if (entries.Count == 0)
            {
                selectedItemId = null;
            }
            else if (string.IsNullOrEmpty(selectedItemId) || entries.All(entry => entry.definition.id != selectedItemId))
            {
                selectedItemId = entries[0].definition.id;
            }

            CreateListHint(entries.Count == 0 ? "还没有收集到物品" : "当前分类下暂无物品");
            RefreshDetails(null);
            return;
        }

        bool hasSelection = false;
        foreach (InventorySystem.InventoryEntry entry in filteredEntries)
        {
            if (entry.definition.id == selectedItemId)
            {
                hasSelection = true;
            }

            CreateItemRow(entry);
        }

        if (!hasSelection)
        {
            selectedItemId = filteredEntries[0].definition.id;
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

        frameObject = CreatePanel("InventoryFrame", inventoryCanvas.transform, FrameColor);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(1280f, 780f);

        panelRoot = CreatePanel("InventoryPanel", frameObject.transform, PaperColor);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = new Vector2(18f, 18f);
        panelRect.offsetMax = new Vector2(-18f, -18f);

        BuildHeader();
        BuildBackgroundGrid();
        BuildTabs();
        BuildColumns();

        frameObject.SetActive(false);
        panelRoot.SetActive(false);
        overlayObject.SetActive(false);
    }

    private void BuildHeader()
    {
        GameObject header = CreatePanel("Header", panelRoot.transform, FrameColor);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 72f);

        TextMeshProUGUI titleText = CreateText("Title", header.transform, "背包 / 收藏", 28f, TextPrimary, TextAlignmentOptions.Left, new Vector2(320f, 46f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(0f, 0.5f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.anchoredPosition = new Vector2(28f, 0f);

        summaryText = CreateText("Summary", header.transform, "背包", 22f, TextPrimary, TextAlignmentOptions.Center, new Vector2(180f, 40f));
        RectTransform summaryRect = summaryText.rectTransform;
        summaryRect.anchorMin = new Vector2(0.5f, 0.5f);
        summaryRect.anchorMax = new Vector2(0.5f, 0.5f);
        summaryRect.pivot = new Vector2(0.5f, 0.5f);
        summaryRect.anchoredPosition = Vector2.zero;

        Button closeButton = CreateButton("CloseButton", header.transform, "×", new Vector2(52f, 52f), FrameColor, AccentPressedColor, 28f, TextPrimary);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(-18f, 0f);
        closeButton.onClick.AddListener(ClosePanel);
    }

    private void BuildBackgroundGrid()
    {
        GameObject grid = new GameObject("GridBackground");
        grid.transform.SetParent(panelRoot.transform, false);
        RectTransform rect = grid.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(0f, -72f);

        Image image = grid.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.02f);

        for (int i = 1; i < 10; i++)
        {
            CreateGridLine(grid.transform, true, i / 10f);
        }

        for (int i = 1; i < 7; i++)
        {
            CreateGridLine(grid.transform, false, i / 7f);
        }
    }

    private void BuildTabs()
    {
        GameObject tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(panelRoot.transform, false);
        RectTransform rect = tabRow.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 54f);
        rect.anchoredPosition = new Vector2(0f, -72f);

        HorizontalLayoutGroup layout = tabRow.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(260, 260, 0, 0);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateFilterTab(tabRow.transform, FilterAll, "全部");

        if (ShopSystem.Instance != null)
        {
            foreach (string category in ShopSystem.Instance.GetCategories())
            {
                CreateFilterTab(tabRow.transform, category, ShopSystem.Instance.GetCategoryDisplayName(category));
            }
        }
    }

    private void BuildColumns()
    {
        GameObject leftPanel = CreatePanel("LeftPanel", panelRoot.transform, new Color(1f, 1f, 1f, 0f));
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(1f, 1f);
        leftRect.offsetMin = new Vector2(0f, 0f);
        leftRect.offsetMax = new Vector2(-340f, -124f);

        ScrollRect scrollRect = CreateScrollView("ItemScroll", leftPanel.transform, new Vector2(0f, 0f));
        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(18f, 18f);
        scrollRectTransform.offsetMax = new Vector2(-18f, -18f);
        itemListContent = scrollRect.content;

        GameObject rightPanel = CreatePanel("RightPanel", panelRoot.transform, DetailPanelColor);
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.pivot = new Vector2(1f, 0.5f);
        rightRect.offsetMin = new Vector2(-328f, 18f);
        rightRect.offsetMax = new Vector2(-18f, -124f);

        VerticalLayoutGroup layout = rightPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("DetailTitle", rightPanel.transform, "物品详情", 22f, TextPrimary, TextAlignmentOptions.Center, new Vector2(240f, 34f));

        GameObject preview = CreatePanel("Preview", rightPanel.transform, PreviewColor);
        preview.AddComponent<LayoutElement>().preferredHeight = 216f;
        CreateText("PreviewText", preview.transform, "已收纳", 26f, new Color32(0xF4, 0xF1, 0xEA, 0xFF), TextAlignmentOptions.Center, new Vector2(220f, 42f));

        itemNameText = CreateText("ItemName", rightPanel.transform, "未选择物品", 30f, TextPrimary, TextAlignmentOptions.Center, new Vector2(240f, 40f));
        itemCategoryText = CreateText("ItemCategory", rightPanel.transform, "", 18f, TextSecondary, TextAlignmentOptions.Center, new Vector2(240f, 26f));
        itemCountText = CreateText("ItemCount", rightPanel.transform, "", 18f, TextPrimary, TextAlignmentOptions.Center, new Vector2(240f, 28f));
        itemDescriptionText = CreateText("ItemDescription", rightPanel.transform, "", 18f, TextPrimary, TextAlignmentOptions.TopLeft, new Vector2(240f, 150f));
        itemDescriptionText.enableWordWrapping = true;
        itemDescriptionText.overflowMode = TextOverflowModes.Ellipsis;
        itemEffectText = CreateText("ItemEffects", rightPanel.transform, "", 18f, TextSecondary, TextAlignmentOptions.TopLeft, new Vector2(240f, 110f));
        itemEffectText.enableWordWrapping = true;
        itemEffectText.overflowMode = TextOverflowModes.Ellipsis;

        GameObject buttonRow = new GameObject("ButtonRow");
        buttonRow.transform.SetParent(rightPanel.transform, false);
        buttonRow.AddComponent<RectTransform>().sizeDelta = new Vector2(240f, 56f);
        buttonRow.AddComponent<LayoutElement>().preferredHeight = 56f;

        useButton = CreateButton("UseButton", buttonRow.transform, "使用", new Vector2(150f, 46f), AccentColor, AccentPressedColor, 24f, Color.white);
        useButton.onClick.AddListener(OnUseButtonClicked);
        useButtonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
        RectTransform useRect = useButton.GetComponent<RectTransform>();
        useRect.anchorMin = new Vector2(0.5f, 0.5f);
        useRect.anchorMax = new Vector2(0.5f, 0.5f);
        useRect.pivot = new Vector2(0.5f, 0.5f);
        useRect.anchoredPosition = Vector2.zero;

        collectionText = CreateText("CollectionText", panelRoot.transform, "已收集 0/0", 18f, TextSecondary, TextAlignmentOptions.Right, new Vector2(240f, 30f));
        RectTransform collectionRect = collectionText.rectTransform;
        collectionRect.anchorMin = new Vector2(1f, 0f);
        collectionRect.anchorMax = new Vector2(1f, 0f);
        collectionRect.pivot = new Vector2(1f, 0f);
        collectionRect.anchoredPosition = new Vector2(-28f, 20f);
    }

    private void CreateItemRow(InventorySystem.InventoryEntry entry)
    {
        Button button = CreateButton($"Item_{entry.definition.id}", itemListContent, "", new Vector2(170f, 148f), CardColor, AccentPressedColor);
        LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 170f;
        layoutElement.preferredHeight = 148f;

        Image image = button.GetComponent<Image>();
        image.color = entry.definition.id == selectedItemId ? CardSelectedColor : CardColor;

        VerticalLayoutGroup layout = button.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject preview = CreatePanel("Thumb", button.transform, PreviewColor);
        preview.AddComponent<LayoutElement>().preferredHeight = 72f;
        CreateText("ThumbText", preview.transform, entry.definition.canUse ? "可用" : "收藏", 18f, new Color32(0xF2, 0xEF, 0xE7, 0xFF), TextAlignmentOptions.Center, new Vector2(90f, 26f));

        CreateText("Name", button.transform, entry.definition.displayName, 19f, TextPrimary, TextAlignmentOptions.Center, new Vector2(120f, 28f));
        CreateText("Category", button.transform, ShopSystem.Instance.GetCategoryDisplayName(entry.definition.category), 15f, TextSecondary, TextAlignmentOptions.Center, new Vector2(120f, 22f));
        CreateText("Count", button.transform, $"持有 x{entry.quantity}", 16f, TextPrimary, TextAlignmentOptions.Center, new Vector2(120f, 24f));

        string itemId = entry.definition.id;
        button.onClick.AddListener(() =>
        {
            selectedItemId = itemId;
            Refresh();
        });
    }

    private void CreateListHint(string message)
    {
        TextMeshProUGUI hint = CreateText("EmptyHint", itemListContent, message, 22f, TextMuted, TextAlignmentOptions.Center, new Vector2(520f, 52f));
        LayoutElement layoutElement = hint.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 520f;
        layoutElement.preferredHeight = 300f;
    }

    private void RefreshDetails(ShopItemDefinition definition)
    {
        if (definition == null)
        {
            itemNameText.text = "未选择物品";
            itemCategoryText.text = "";
            itemCountText.text = "";
            itemDescriptionText.text = "这里会展示物品说明。收集到的道具会按照分类陈列在左侧。";
            itemEffectText.text = "";
            SetUseButtonState(false, "使用");
            return;
        }

        int quantity = InventorySystem.Instance != null ? InventorySystem.Instance.GetItemCount(definition.id) : 0;
        itemNameText.text = definition.displayName;
        itemCategoryText.text = ShopSystem.Instance.GetCategoryDisplayName(definition.category);
        itemCountText.text = $"持有数量 x{quantity}";
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
            itemDescriptionText.text = $"{definition.description}\n\n已使用 1 个。";
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
        useButtonText.text = string.IsNullOrEmpty(verb) ? "使用" : verb;
    }

    private string BuildEffectText(AttributeEffect[] effects)
    {
        if (effects == null || effects.Length == 0)
        {
            return "效果：无属性变化";
        }

        List<string> parts = new List<string>();
        foreach (AttributeEffect effect in effects)
        {
            string sign = effect.amount >= 0 ? "+" : "";
            parts.Add($"{effect.attributeName}{sign}{effect.amount}");
        }

        return $"效果：{string.Join("  ", parts)}";
    }

    private int GetTotalCollectableCount()
    {
        return ShopSystem.Instance != null ? ShopSystem.Instance.GetAllItems().Count(item => item.canStore) : 0;
    }

    private void CreateFilterTab(Transform parent, string key, string label)
    {
        Button button = CreateButton($"Filter_{key}", parent, label, new Vector2(150f, 54f), TabIdleColor, TabActiveColor, 21f, TextPrimary);
        filterButtons[key] = button;
        filterButtonTexts[key] = button.GetComponentInChildren<TextMeshProUGUI>();
        button.onClick.AddListener(() =>
        {
            currentCategoryFilter = key;
            Refresh();
        });
    }

    private void RefreshFilterVisuals(List<InventorySystem.InventoryEntry> allEntries)
    {
        foreach (var pair in filterButtons)
        {
            bool isActive = pair.Key == currentCategoryFilter;
            Image image = pair.Value.GetComponent<Image>();
            image.color = isActive ? TabActiveColor : TabIdleColor;

            if (filterButtonTexts.TryGetValue(pair.Key, out TextMeshProUGUI text))
            {
                int count = pair.Key == FilterAll
                    ? allEntries.Count
                    : allEntries.Count(entry => entry.definition.category == pair.Key);

                string label = pair.Key == FilterAll
                    ? "全部"
                    : (ShopSystem.Instance != null ? ShopSystem.Instance.GetCategoryDisplayName(pair.Key) : pair.Key);

                text.text = $"{label} {count}";
                text.color = isActive ? TextPrimary : TextSecondary;
            }
        }
    }

    private void CreateGridLine(Transform parent, bool vertical, float normalizedPosition)
    {
        GameObject line = new GameObject(vertical ? "GridLineV" : "GridLineH");
        line.transform.SetParent(parent, false);
        RectTransform rect = line.AddComponent<RectTransform>();
        Image image = line.AddComponent<Image>();
        image.color = GridLineColor;

        if (vertical)
        {
            rect.anchorMin = new Vector2(normalizedPosition, 0f);
            rect.anchorMax = new Vector2(normalizedPosition, 1f);
            rect.sizeDelta = new Vector2(1f, 0f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, normalizedPosition);
            rect.anchorMax = new Vector2(1f, normalizedPosition);
            rect.sizeDelta = new Vector2(0f, 1f);
        }
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

    private Button CreateButton(string name, Transform parent, string label, Vector2 size, Color normalColor, Color pressedColor, float fontSize = 18f, Color? textColor = null)
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
            Mathf.Clamp01(normalColor.r + 0.05f),
            Mathf.Clamp01(normalColor.g + 0.05f),
            Mathf.Clamp01(normalColor.b + 0.05f),
            normalColor.a);
        colors.pressedColor = pressedColor;
        colors.selectedColor = normalColor;
        colors.disabledColor = DisabledColor;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, fontSize, textColor ?? TextPrimary, TextAlignmentOptions.Center, size);
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
        tmp.richText = true;
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(tmp);
        }
        return tmp;
    }

    private ScrollRect CreateScrollView(string name, Transform parent, Vector2 size)
    {
        GameObject scrollObject = new GameObject(name);
        scrollObject.transform.SetParent(parent, false);

        RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
        scrollRect.sizeDelta = size;

        Image background = scrollObject.AddComponent<Image>();
        background.color = new Color(PaperSoftColor.r, PaperSoftColor.g, PaperSoftColor.b, 0.72f);

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

        GridLayoutGroup layout = content.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(170f, 148f);
        layout.spacing = new Vector2(18f, 18f);
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.content = contentRect;
        return scroll;
    }
}
