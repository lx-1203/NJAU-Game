using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 商店 UI 构建器 —— 用纯代码构建商店界面的所有 UI 元素
/// 布局参考：
/// ┌──────────────────────────────────────────────────────────┐
/// │  [X 关闭]         教超 / 商店              余额：¥8000   │ ← 标题栏
/// ├────────────┬─────────────────────────────────────────────┤
/// │  饮食      │  ┌────────────────────────────────────────┐ │
/// │  日用品    │  │ 食堂套餐     ¥12     心情+1   [购买]  │ │
/// │  服装      │  ├────────────────────────────────────────┤ │
/// │  学习      │  │ 外卖         ¥30     心情+5   [购买]  │ │
/// │  娱乐      │  ├────────────────────────────────────────┤ │
/// │  社交      │  │ ...                                    │ │
/// │            │  └────────────────────────────────────────┘ │
/// ├────────────┴─────────────────────────────────────────────┤
/// │ 最近交易：食堂套餐 -¥12  余额 ¥7988                      │ ← 底部信息栏
/// └──────────────────────────────────────────────────────────┘
/// </summary>
public class ShopUIBuilder : MonoBehaviour
{
    // ========== 布局常量 ==========

    private const float TitleBarHeight = 60f;
    private const float BottomBarHeight = 40f;
    private const float CategoryPanelWidth = 160f;
    private const float ItemCardHeight = 60f;
    private const float ItemCardSpacing = 5f;

    // ========== 颜色方案（复用 HUDBuilder） ==========

    private static readonly Color PanelBgColor       = new Color(0.08f, 0.08f, 0.12f, 0.90f);
    private static readonly Color TopBarColor        = new Color(0.10f, 0.10f, 0.16f, 0.95f);
    private static readonly Color ButtonNormalColor   = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ButtonHoverColor    = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color ButtonPressedColor  = new Color(0.15f, 0.25f, 0.50f, 1.0f);
    private static readonly Color TextWhite           = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold            = new Color(1.0f, 0.85f, 0.30f);

    private static readonly Color CategoryNormalColor   = new Color(0.12f, 0.12f, 0.18f, 1.0f);
    private static readonly Color CategorySelectedColor = new Color(0.25f, 0.40f, 0.65f, 1.0f);
    private static readonly Color ItemCardColor         = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    private static readonly Color ButtonDisabledColor   = new Color(0.30f, 0.30f, 0.35f, 1.0f);
    private static readonly Color PopupBgColor          = new Color(0.06f, 0.08f, 0.16f, 0.92f);

    // ========== UI 引用 ==========

    private Canvas shopCanvas;
    private GameObject shopPanel;
    private TextMeshProUGUI balanceText;
    private TextMeshProUGUI bottomInfoText;
    private Transform itemListContent;
    private string currentCategory = "food";
    private Dictionary<string, Button> categoryButtons = new Dictionary<string, Button>();

    // ========== 分类定义 ==========

    private readonly string[] categoryKeys = { "food", "daily", "clothing", "study", "entertainment", "social" };
    private readonly string[] categoryNames = { "饮食", "日用品", "服装", "学习", "娱乐", "社交" };

    // ========== 公共属性 ==========

    /// <summary>当前商店是否处于打开状态</summary>
    public bool IsShopOpen => shopPanel != null && shopPanel.activeSelf;

    // ========== 公共接口 ==========

    /// <summary>构建完整商店 UI（创建独立 Canvas, sortingOrder=200）</summary>
    public void BuildShopUI()
    {
        CreateShopCanvas();
        CreateTitleBar();
        CreateCategoryPanel();
        CreateItemListPanel();
        CreateBottomBar();

        // 默认隐藏
        shopPanel.SetActive(false);
    }

    /// <summary>显示商店面板</summary>
    public void ShowShop()
    {
        if (shopPanel == null) return;

        shopPanel.SetActive(true);
        RefreshBalance();
        SelectCategory("food");
    }

    /// <summary>隐藏商店面板</summary>
    public void HideShop()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(false);
    }

    /// <summary>
    /// 交易成功弹窗，1.5s 自动消失
    /// </summary>
    /// <param name="itemName">商品名称</param>
    /// <param name="cost">花费金额</param>
    /// <param name="newBalance">购买后余额</param>
    public void ShowTransactionPopup(string itemName, int cost, int newBalance)
    {
        StartCoroutine(TransactionPopupCoroutine(itemName, cost, newBalance));
    }

    // ====================================================================
    //  Canvas
    // ====================================================================

    /// <summary>创建商店独立 Canvas</summary>
    private void CreateShopCanvas()
    {
        GameObject canvasObj = new GameObject("ShopCanvas");
        canvasObj.transform.SetParent(transform, false);

        shopCanvas = canvasObj.AddComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 主面板（全屏遮罩）
        shopPanel = CreatePanel("ShopPanel", canvasObj.transform, PanelBgColor);
        RectTransform panelRT = shopPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
    }

    // ====================================================================
    //  标题栏
    // ====================================================================

    /// <summary>创建标题栏（关闭按钮 + 标题 + 余额）</summary>
    private void CreateTitleBar()
    {
        GameObject titleBar = CreatePanel("TitleBar", shopPanel.transform, TopBarColor);
        RectTransform titleRT = titleBar.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(0, TitleBarHeight);

        // — 关闭按钮 [X] —
        Button closeBtn = CreateShopButton("BtnClose", titleBar.transform, "X", new Vector2(50, 40));
        RectTransform closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0, 0.5f);
        closeBtnRT.anchorMax = new Vector2(0, 0.5f);
        closeBtnRT.pivot = new Vector2(0, 0.5f);
        closeBtnRT.anchoredPosition = new Vector2(10, 0);
        closeBtn.onClick.AddListener(HideShop);

        // — 标题文字 —
        TextMeshProUGUI titleText = CreateTMPText("TitleText", titleBar.transform,
            "教超 / 商店", 24f, TextWhite, TextAlignmentOptions.Center,
            new Vector2(300, TitleBarHeight));
        RectTransform titleTextRT = titleText.GetComponent<RectTransform>();
        titleTextRT.anchorMin = new Vector2(0.5f, 0);
        titleTextRT.anchorMax = new Vector2(0.5f, 1);
        titleTextRT.pivot = new Vector2(0.5f, 0.5f);
        titleTextRT.anchoredPosition = Vector2.zero;
        titleTextRT.sizeDelta = new Vector2(300, 0);

        // — 余额显示 —
        balanceText = CreateTMPText("BalanceText", titleBar.transform,
            "余额：¥0", 20f, TextGold, TextAlignmentOptions.Right,
            new Vector2(250, TitleBarHeight));
        RectTransform balanceRT = balanceText.GetComponent<RectTransform>();
        balanceRT.anchorMin = new Vector2(1, 0);
        balanceRT.anchorMax = new Vector2(1, 1);
        balanceRT.pivot = new Vector2(1, 0.5f);
        balanceRT.anchoredPosition = new Vector2(-15, 0);
        balanceRT.sizeDelta = new Vector2(250, 0);
    }

    // ====================================================================
    //  左侧分类栏
    // ====================================================================

    /// <summary>创建左侧分类按钮栏</summary>
    private void CreateCategoryPanel()
    {
        GameObject catPanel = CreatePanel("CategoryPanel", shopPanel.transform, new Color(0.09f, 0.09f, 0.14f, 0.95f));
        RectTransform catRT = catPanel.GetComponent<RectTransform>();
        catRT.anchorMin = new Vector2(0, 0);
        catRT.anchorMax = new Vector2(0, 1);
        catRT.pivot = new Vector2(0, 0.5f);
        catRT.anchoredPosition = new Vector2(0, (BottomBarHeight - TitleBarHeight) / 2f);
        catRT.sizeDelta = new Vector2(CategoryPanelWidth, -(TitleBarHeight + BottomBarHeight));

        // 垂直布局
        VerticalLayoutGroup vlg = catPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(8, 8, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        categoryButtons.Clear();

        for (int i = 0; i < categoryKeys.Length; i++)
        {
            string key = categoryKeys[i];
            string label = categoryNames[i];

            Button catBtn = CreateShopButton("CatBtn_" + key, catPanel.transform, label, new Vector2(CategoryPanelWidth - 16, 50));
            catBtn.onClick.AddListener(() => SelectCategory(key));
            categoryButtons[key] = catBtn;
        }
    }

    // ====================================================================
    //  右侧商品列表
    // ====================================================================

    /// <summary>创建右侧商品列表区域（ScrollView）</summary>
    private void CreateItemListPanel()
    {
        // — ScrollView 容器 —
        GameObject scrollObj = new GameObject("ItemScrollView");
        scrollObj.transform.SetParent(shopPanel.transform, false);

        RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(CategoryPanelWidth, BottomBarHeight);
        scrollRT.offsetMax = new Vector2(0, -TitleBarHeight);

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.06f, 0.06f, 0.10f, 0.85f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // — Viewport —
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);

        RectTransform vpRT = viewport.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(10, 5);
        vpRT.offsetMax = new Vector2(-10, -5);

        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        scrollRect.viewport = vpRT;

        // — Content —
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = ItemCardSpacing;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        itemListContent = content.transform;
    }

    // ====================================================================
    //  底部信息栏
    // ====================================================================

    /// <summary>创建底部信息栏</summary>
    private void CreateBottomBar()
    {
        GameObject bottomBar = CreatePanel("BottomBar", shopPanel.transform, TopBarColor);
        RectTransform botRT = bottomBar.GetComponent<RectTransform>();
        botRT.anchorMin = new Vector2(0, 0);
        botRT.anchorMax = new Vector2(1, 0);
        botRT.pivot = new Vector2(0.5f, 0);
        botRT.anchoredPosition = Vector2.zero;
        botRT.sizeDelta = new Vector2(0, BottomBarHeight);

        bottomInfoText = CreateTMPText("BottomInfoText", bottomBar.transform,
            "最近交易：无", 16f, TextWhite, TextAlignmentOptions.Left,
            new Vector2(800, BottomBarHeight));
        RectTransform infoRT = bottomInfoText.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0, 0);
        infoRT.anchorMax = new Vector2(1, 1);
        infoRT.pivot = new Vector2(0, 0.5f);
        infoRT.anchoredPosition = Vector2.zero;
        infoRT.offsetMin = new Vector2(15, 0);
        infoRT.offsetMax = new Vector2(-15, 0);
    }

    // ====================================================================
    //  分类切换
    // ====================================================================

    /// <summary>选中指定分类，刷新商品列表</summary>
    private void SelectCategory(string category)
    {
        currentCategory = category;

        // 更新分类按钮高亮
        foreach (var kvp in categoryButtons)
        {
            Image btnImg = kvp.Value.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = (kvp.Key == category) ? CategorySelectedColor : CategoryNormalColor;
            }

            // 更新 ColorBlock 以保持选中态
            ColorBlock cb = kvp.Value.colors;
            cb.normalColor = (kvp.Key == category) ? CategorySelectedColor : CategoryNormalColor;
            cb.highlightedColor = (kvp.Key == category) ? CategorySelectedColor : ButtonHoverColor;
            kvp.Value.colors = cb;
        }

        RefreshItemList();
    }

    /// <summary>刷新商品列表（清空旧卡片 → 创建新卡片）</summary>
    private void RefreshItemList()
    {
        if (itemListContent == null) return;

        // 清空旧卡片
        for (int i = itemListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemListContent.GetChild(i).gameObject);
        }

        // 获取当前分类的商品
        if (ShopSystem.Instance == null) return;

        ShopItemDefinition[] items = ShopSystem.Instance.GetAvailableItems(currentCategory);

        foreach (ShopItemDefinition item in items)
        {
            CreateItemCard(item);
        }
    }

    // ====================================================================
    //  商品卡片
    // ====================================================================

    /// <summary>创建单个商品卡片</summary>
    private void CreateItemCard(ShopItemDefinition item)
    {
        GameObject card = CreatePanel("ItemCard_" + item.id, itemListContent, ItemCardColor);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0, ItemCardHeight);

        // 水平布局
        HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(15, 10, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // — 商品名 —
        CreateTMPText("ItemName", card.transform, item.displayName,
            18f, TextWhite, TextAlignmentOptions.Left, new Vector2(160, ItemCardHeight));

        // — 价格 —
        CreateTMPText("ItemPrice", card.transform, $"¥{item.price}",
            18f, TextGold, TextAlignmentOptions.Center, new Vector2(80, ItemCardHeight));

        // — 效果描述 —
        string effectStr = BuildEffectString(item.effects);
        CreateTMPText("ItemEffect", card.transform, effectStr,
            16f, TextWhite, TextAlignmentOptions.Left, new Vector2(260, ItemCardHeight));

        // — 购买按钮 —
        bool canBuy = ShopSystem.Instance.CanBuyItem(item.id);
        Button buyBtn = CreateShopButton("BtnBuy_" + item.id, card.transform, "购买", new Vector2(90, 40));

        if (!canBuy)
        {
            buyBtn.interactable = false;
            Image buyBtnImg = buyBtn.GetComponent<Image>();
            if (buyBtnImg != null) buyBtnImg.color = ButtonDisabledColor;
        }

        string itemId = item.id;
        string itemName = item.displayName;
        int itemPrice = item.price;

        buyBtn.onClick.AddListener(() => OnBuyClicked(itemId, itemName, itemPrice));
    }

    /// <summary>拼接属性效果描述字符串</summary>
    private string BuildEffectString(AttributeEffect[] effects)
    {
        if (effects == null || effects.Length == 0) return "";

        List<string> parts = new List<string>();
        foreach (AttributeEffect effect in effects)
        {
            string sign = effect.amount >= 0 ? "+" : "";
            parts.Add($"{effect.attributeName}{sign}{effect.amount}");
        }
        return string.Join("  ", parts);
    }

    // ====================================================================
    //  购买逻辑
    // ====================================================================

    /// <summary>购买按钮点击回调</summary>
    private void OnBuyClicked(string itemId, string itemName, int itemPrice)
    {
        if (ShopSystem.Instance == null) return;

        bool success = ShopSystem.Instance.BuyItem(itemId);
        if (success)
        {
            int newBalance = EconomyManager.Instance != null
                ? EconomyManager.Instance.GetBalance() : 0;

            // 更新底部信息栏
            bottomInfoText.text = $"最近交易：{itemName} -¥{itemPrice}  余额 ¥{newBalance}";

            // 刷新余额 & 商品列表（购买力可能变化）
            RefreshBalance();
            RefreshItemList();

            // 弹窗
            ShowTransactionPopup(itemName, itemPrice, newBalance);
        }
    }

    // ====================================================================
    //  余额刷新
    // ====================================================================

    /// <summary>刷新标题栏余额显示</summary>
    private void RefreshBalance()
    {
        if (balanceText == null) return;

        int balance = EconomyManager.Instance != null
            ? EconomyManager.Instance.GetBalance() : 0;
        balanceText.text = $"余额：¥{balance}";
    }

    // ====================================================================
    //  交易弹窗
    // ====================================================================

    /// <summary>交易成功弹窗协程，1.5s 后自动淡出消失</summary>
    private IEnumerator TransactionPopupCoroutine(string itemName, int cost, int newBalance)
    {
        // 创建弹窗
        GameObject popup = CreatePanel("TransactionPopup", shopPanel.transform, PopupBgColor);
        RectTransform popupRT = popup.GetComponent<RectTransform>();
        popupRT.anchorMin = new Vector2(0.5f, 0.5f);
        popupRT.anchorMax = new Vector2(0.5f, 0.5f);
        popupRT.pivot = new Vector2(0.5f, 0.5f);
        popupRT.anchoredPosition = Vector2.zero;
        popupRT.sizeDelta = new Vector2(300, 120);

        CanvasGroup cg = popup.AddComponent<CanvasGroup>();

        // 垂直布局
        VerticalLayoutGroup vlg = popup.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(15, 15, 12, 12);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 标题
        CreateTMPText("PopupTitle", popup.transform, "购买成功！",
            20f, TextWhite, TextAlignmentOptions.Center, new Vector2(270, 30));

        // 商品信息
        CreateTMPText("PopupItem", popup.transform, $"{itemName} -¥{cost}",
            18f, TextGold, TextAlignmentOptions.Center, new Vector2(270, 28));

        // 余额
        CreateTMPText("PopupBalance", popup.transform, $"余额：¥{newBalance}",
            16f, TextWhite, TextAlignmentOptions.Center, new Vector2(270, 26));

        // 等待 1s，然后 0.5s 淡出
        yield return new WaitForSeconds(1.0f);

        float fadeDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        Destroy(popup);
    }

    // ====================================================================
    //  辅助方法
    // ====================================================================

    /// <summary>创建带背景色的面板</summary>
    private GameObject CreatePanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();

        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;

        return panel;
    }

    /// <summary>创建 TextMeshPro 文本</summary>
    private TextMeshProUGUI CreateTMPText(string name, Transform parent, string text,
        float fontSize, Color color, TextAlignmentOptions alignment, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return tmp;
    }

    /// <summary>创建商店通用按钮</summary>
    private Button CreateShopButton(string name, Transform parent, string label, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = ButtonNormalColor;

        Button btn = btnObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = ButtonNormalColor;
        cb.highlightedColor = ButtonHoverColor;
        cb.pressedColor = ButtonPressedColor;
        cb.selectedColor = ButtonNormalColor;
        cb.disabledColor = ButtonDisabledColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // 按钮文字
        TextMeshProUGUI btnText = CreateTMPText(name + "Label", btnObj.transform, label,
            18f, TextWhite, TextAlignmentOptions.Center, size);
        RectTransform textRT = btnText.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btn;
    }
}
