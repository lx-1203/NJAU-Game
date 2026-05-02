using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class ShopItemDefinition
{
    public string id;
    public string displayName;
    public string description;
    public int price;
    public string category;
    public AttributeEffect[] effects;
    public bool canStore;
    public bool canUse;
    public bool consumeOnUse;
    public int maxStack;
    public string useVerb;

    public ShopItemDefinition(
        string id,
        string displayName,
        string description,
        int price,
        string category,
        AttributeEffect[] effects,
        bool canStore = true,
        bool canUse = true,
        bool consumeOnUse = true,
        int maxStack = 99,
        string useVerb = "使用")
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.price = price;
        this.category = category;
        this.effects = effects;
        this.canStore = canStore;
        this.canUse = canUse;
        this.consumeOnUse = consumeOnUse;
        this.maxStack = maxStack;
        this.useVerb = useVerb;
    }
}

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }

    public event Action<ShopItemDefinition> OnItemPurchased;

    private readonly List<ShopItemDefinition> allItems = new List<ShopItemDefinition>();

    private readonly Dictionary<string, string> categoryDisplayNames = new Dictionary<string, string>
    {
        { "food", "饮食" },
        { "daily", "日用品" },
        { "clothing", "服装" },
        { "entertainment", "娱乐" },
        { "study", "学习" },
        { "social", "社交" }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitShopItems();
    }

    private void InitShopItems()
    {
        allItems.Clear();

        allItems.Add(new ShopItemDefinition(
            "food_instant_noodles", "泡面", "便宜但吃多了心情不好",
            3, "food",
            new[] { new AttributeEffect("心情", -2) },
            useVerb: "食用"));
        allItems.Add(new ShopItemDefinition(
            "food_canteen", "食堂套餐", "学校食堂的标准套餐",
            12, "food",
            new[] { new AttributeEffect("心情", 1) },
            useVerb: "食用"));
        allItems.Add(new ShopItemDefinition(
            "food_huangjiaoshou", "黄教授烧饼", "校园周边的人气小吃",
            8, "food",
            new[] { new AttributeEffect("心情", 3) },
            useVerb: "食用"));
        allItems.Add(new ShopItemDefinition(
            "food_roast_chicken", "南农烤鸡", "南农特色美食，吃了身体倍儿棒",
            15, "food",
            new[] { new AttributeEffect("体魄", 2) },
            useVerb: "食用"));
        allItems.Add(new ShopItemDefinition(
            "food_delivery", "外卖", "想吃什么点什么，心情大好",
            30, "food",
            new[] { new AttributeEffect("心情", 5) },
            useVerb: "食用"));

        allItems.Add(new ShopItemDefinition(
            "daily_supplies", "日用品补给", "补充日常生活所需物品",
            80, "daily",
            new[] { new AttributeEffect("心情", 2) },
            useVerb: "整理"));
        allItems.Add(new ShopItemDefinition(
            "daily_milk_tea", "奶茶", "来一杯续命奶茶",
            15, "daily",
            new[] { new AttributeEffect("心情", 2) },
            useVerb: "饮用"));
        allItems.Add(new ShopItemDefinition(
            "daily_coffee", "咖啡", "提神醒脑，缓解压力",
            20, "daily",
            new[] { new AttributeEffect("压力", -3) },
            useVerb: "饮用"));

        allItems.Add(new ShopItemDefinition(
            "clothing_haircut", "理发", "换个发型换个心情",
            50, "clothing",
            new[] { new AttributeEffect("魅力", 2) },
            useVerb: "使用"));
        allItems.Add(new ShopItemDefinition(
            "clothing_seasonal", "换季置装", "每个季节都要穿得体面",
            500, "clothing",
            new[] { new AttributeEffect("魅力", 5) },
            useVerb: "换装"));
        allItems.Add(new ShopItemDefinition(
            "clothing_shoes", "球鞋", "一双好鞋走遍校园",
            400, "clothing",
            new[]
            {
                new AttributeEffect("魅力", 3),
                new AttributeEffect("体魄", 1)
            },
            useVerb: "换装"));

        allItems.Add(new ShopItemDefinition(
            "study_materials", "学习资料", "课外辅导书与参考资料",
            150, "study",
            new[] { new AttributeEffect("学力", 3) },
            useVerb: "研读"));
        allItems.Add(new ShopItemDefinition(
            "study_printing", "打印复印", "打印课件和复习资料",
            30, "study",
            new[] { new AttributeEffect("学力", 1) },
            useVerb: "研读"));

        allItems.Add(new ShopItemDefinition(
            "ent_movie", "电影票", "去电影院看一场电影放松一下",
            50, "entertainment",
            new[]
            {
                new AttributeEffect("心情", 8),
                new AttributeEffect("压力", -5)
            },
            useVerb: "观影"));
        allItems.Add(new ShopItemDefinition(
            "ent_game_topup", "游戏点卡", "给喜欢的游戏充个值",
            100, "entertainment",
            new[]
            {
                new AttributeEffect("心情", 10),
                new AttributeEffect("压力", -8)
            },
            useVerb: "开玩"));

        allItems.Add(new ShopItemDefinition(
            "social_treat", "请客吃饭", "请朋友吃顿好的，联络感情",
            80, "social",
            new[]
            {
                new AttributeEffect("魅力", 2),
                new AttributeEffect("心情", 3)
            },
            useVerb: "安排"));
        allItems.Add(new ShopItemDefinition(
            "social_gift", "礼物", "精心挑选的一份礼物",
            150, "social",
            new[]
            {
                new AttributeEffect("魅力", 3),
                new AttributeEffect("心情", 2)
            },
            useVerb: "赠送"));

        Debug.Log($"[ShopSystem] Initialized {allItems.Count} shop items.");
    }

    public ShopItemDefinition[] GetAllItems()
    {
        return allItems.ToArray();
    }

    public string[] GetCategories()
    {
        return allItems.Select(item => item.category).Distinct().ToArray();
    }

    public string GetCategoryDisplayName(string category)
    {
        return categoryDisplayNames.TryGetValue(category, out string displayName) ? displayName : category;
    }

    public ShopItemDefinition GetItemDefinition(string itemId)
    {
        return allItems.Find(item => item.id == itemId);
    }

    public ShopItemDefinition[] GetAvailableItems(string category)
    {
        bool isFoodRestricted = DebtSystem.Instance != null && DebtSystem.Instance.IsFoodRestricted;
        bool isOverdrafted = DebtSystem.Instance != null && DebtSystem.Instance.IsOverdrafted;

        if (isOverdrafted && category != "food" && category != "study")
        {
            return Array.Empty<ShopItemDefinition>();
        }

        List<ShopItemDefinition> result = allItems.Where(item => item.category == category).ToList();
        if (isFoodRestricted && category == "food")
        {
            result = result.Where(item => item.price <= 3).ToList();
        }

        return result.ToArray();
    }

    public bool CanBuyItem(string itemId)
    {
        ShopItemDefinition item = GetItemDefinition(itemId);
        if (item == null)
        {
            return false;
        }

        if (EconomyManager.Instance == null || !EconomyManager.Instance.CanAfford(item.price))
        {
            return false;
        }

        if (InventorySystem.Instance != null && item.canStore)
        {
            int ownedCount = InventorySystem.Instance.GetItemCount(itemId);
            if (ownedCount >= Mathf.Max(1, item.maxStack))
            {
                return false;
            }
        }

        bool isFoodRestricted = DebtSystem.Instance != null && DebtSystem.Instance.IsFoodRestricted;
        bool isOverdrafted = DebtSystem.Instance != null && DebtSystem.Instance.IsOverdrafted;

        if (isFoodRestricted && item.category == "food" && item.price > 3)
        {
            return false;
        }

        if (isOverdrafted && item.category != "food" && item.category != "study")
        {
            return false;
        }

        return true;
    }

    public bool BuyItem(string itemId)
    {
        if (!CanBuyItem(itemId))
        {
            Debug.LogWarning($"[ShopSystem] Cannot buy item: {itemId}");
            return false;
        }

        ShopItemDefinition item = GetItemDefinition(itemId);
        EconomyManager.Instance.Spend(item.price, CategoryToTransactionType(item.category), item.displayName);

        if (InventorySystem.Instance == null || !InventorySystem.Instance.AddItem(item.id, 1))
        {
            Debug.LogWarning($"[ShopSystem] Failed to add item to inventory: {item.id}");
            return false;
        }

        OnItemPurchased?.Invoke(item);
        return true;
    }

    private TransactionRecord.TransactionType CategoryToTransactionType(string category)
    {
        switch (category)
        {
            case "food": return TransactionRecord.TransactionType.Food;
            case "daily": return TransactionRecord.TransactionType.DailyNecessities;
            case "clothing": return TransactionRecord.TransactionType.Clothing;
            case "study": return TransactionRecord.TransactionType.StudyMaterial;
            case "entertainment": return TransactionRecord.TransactionType.Entertainment;
            case "social": return TransactionRecord.TransactionType.SocialExpense;
            default: return TransactionRecord.TransactionType.OtherExpense;
        }
    }
}
