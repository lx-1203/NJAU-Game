using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// ========== 数据类 ==========

/// <summary>商店商品定义</summary>
[System.Serializable]
public class ShopItemDefinition
{
    public string id;
    public string displayName;
    public string description;
    public int price;
    public string category; // "food", "daily", "clothing", "entertainment", "study", "social"
    public AttributeEffect[] effects;

    public ShopItemDefinition(string id, string displayName, string description,
        int price, string category, AttributeEffect[] effects)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.price = price;
        this.category = category;
        this.effects = effects;
    }
}

// ========== 商店系统 ==========

/// <summary>
/// 商店系统 —— 管理商品定义、购买校验与购买执行
/// </summary>
public class ShopSystem : MonoBehaviour
{
    // ========== 单例 ==========
    public static ShopSystem Instance { get; private set; }

    // ========== 事件 ==========

    /// <summary>商品购买成功后触发</summary>
    public event Action<ShopItemDefinition> OnItemPurchased;

    // ========== 内部字段 ==========

    private List<ShopItemDefinition> allItems = new List<ShopItemDefinition>();

    /// <summary>分类英文名 → 中文显示名映射</summary>
    private readonly Dictionary<string, string> categoryDisplayNames = new Dictionary<string, string>
    {
        { "food",          "饮食" },
        { "daily",         "日用品" },
        { "clothing",      "服装" },
        { "entertainment", "娱乐" },
        { "study",         "学习" },
        { "social",        "社交" }
    };

    // ========== 生命周期 ==========

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

    // ========== 初始化 ==========

    /// <summary>初始化全部商品列表</summary>
    private void InitShopItems()
    {
        allItems.Clear();

        // ---- 饮食 (food) ----
        allItems.Add(new ShopItemDefinition(
            "food_instant_noodles", "泡面",
            "便宜但吃多了心情不好",
            3, "food",
            new AttributeEffect[] { new AttributeEffect("心情", -2) }
        ));
        allItems.Add(new ShopItemDefinition(
            "food_canteen", "食堂套餐",
            "学校食堂的标准套餐",
            12, "food",
            new AttributeEffect[] { new AttributeEffect("心情", 1) }
        ));
        allItems.Add(new ShopItemDefinition(
            "food_huangjiaoshou", "黄教授烧饼",
            "校园周边的人气小吃",
            8, "food",
            new AttributeEffect[] { new AttributeEffect("心情", 3) }
        ));
        allItems.Add(new ShopItemDefinition(
            "food_roast_chicken", "南农烧鸡",
            "南农特色美食，吃了身体倍儿棒",
            15, "food",
            new AttributeEffect[] { new AttributeEffect("体魄", 2) }
        ));
        allItems.Add(new ShopItemDefinition(
            "food_delivery", "外卖",
            "想吃什么点什么，心情大好",
            30, "food",
            new AttributeEffect[] { new AttributeEffect("心情", 5) }
        ));

        // ---- 日用品 (daily) ----
        allItems.Add(new ShopItemDefinition(
            "daily_supplies", "日用品补给",
            "补充日常生活所需物品",
            80, "daily",
            new AttributeEffect[] { new AttributeEffect("心情", 2) }
        ));
        allItems.Add(new ShopItemDefinition(
            "daily_milk_tea", "奶茶",
            "来一杯续命奶茶",
            15, "daily",
            new AttributeEffect[] { new AttributeEffect("心情", 2) }
        ));
        allItems.Add(new ShopItemDefinition(
            "daily_coffee", "咖啡",
            "提神醒脑，缓解压力",
            20, "daily",
            new AttributeEffect[] { new AttributeEffect("压力", -3) }
        ));

        // ---- 服装 (clothing) ----
        allItems.Add(new ShopItemDefinition(
            "clothing_haircut", "理发",
            "换个发型换个心情",
            50, "clothing",
            new AttributeEffect[] { new AttributeEffect("魅力", 2) }
        ));
        allItems.Add(new ShopItemDefinition(
            "clothing_seasonal", "换季置装",
            "每个季节都要穿得体面",
            500, "clothing",
            new AttributeEffect[] { new AttributeEffect("魅力", 5) }
        ));
        allItems.Add(new ShopItemDefinition(
            "clothing_shoes", "买鞋",
            "一双好鞋走遍天下",
            400, "clothing",
            new AttributeEffect[]
            {
                new AttributeEffect("魅力", 3),
                new AttributeEffect("体魄", 1)
            }
        ));

        // ---- 学习 (study) ----
        allItems.Add(new ShopItemDefinition(
            "study_materials", "学习资料",
            "课外辅导书与参考资料",
            150, "study",
            new AttributeEffect[] { new AttributeEffect("学力", 3) }
        ));
        allItems.Add(new ShopItemDefinition(
            "study_printing", "打印复印",
            "打印课件和复习资料",
            30, "study",
            new AttributeEffect[] { new AttributeEffect("学力", 1) }
        ));

        // ---- 娱乐 (entertainment) ----
        allItems.Add(new ShopItemDefinition(
            "ent_movie", "看电影",
            "去电影院看一场电影放松一下",
            50, "entertainment",
            new AttributeEffect[]
            {
                new AttributeEffect("心情", 8),
                new AttributeEffect("压力", -5)
            }
        ));
        allItems.Add(new ShopItemDefinition(
            "ent_game_topup", "游戏充值",
            "给喜欢的游戏充个值",
            100, "entertainment",
            new AttributeEffect[]
            {
                new AttributeEffect("心情", 10),
                new AttributeEffect("压力", -8)
            }
        ));

        // ---- 社交 (social) ----
        allItems.Add(new ShopItemDefinition(
            "social_treat", "请客吃饭",
            "请朋友吃顿好的，联络感情",
            80, "social",
            new AttributeEffect[]
            {
                new AttributeEffect("魅力", 2),
                new AttributeEffect("心情", 3)
            }
        ));
        allItems.Add(new ShopItemDefinition(
            "social_gift", "送礼物",
            "精心挑选一份礼物送给朋友",
            150, "social",
            new AttributeEffect[]
            {
                new AttributeEffect("魅力", 3),
                new AttributeEffect("心情", 2)
            }
        ));

        Debug.Log($"[ShopSystem] 商品初始化完成，共 {allItems.Count} 件商品");
    }

    // ========== 查询方法 ==========

    /// <summary>返回所有商品</summary>
    public ShopItemDefinition[] GetAllItems()
    {
        return allItems.ToArray();
    }

    /// <summary>返回所有分类名（英文 key）</summary>
    public string[] GetCategories()
    {
        return allItems.Select(item => item.category).Distinct().ToArray();
    }

    /// <summary>获取分类中文显示名</summary>
    public string GetCategoryDisplayName(string category)
    {
        if (categoryDisplayNames.TryGetValue(category, out string displayName))
            return displayName;
        return category;
    }

    /// <summary>
    /// 按分类筛选可用商品，考虑债务限制：
    /// - IsFoodRestricted 时：food 分类只返回价格 ≤ 3 的商品
    /// - IsOverdrafted 时：非 food 且非 study 的分类返回空数组
    /// </summary>
    public ShopItemDefinition[] GetAvailableItems(string category)
    {
        bool isFoodRestricted = DebtSystem.Instance != null && DebtSystem.Instance.IsFoodRestricted;
        bool isOverdrafted = DebtSystem.Instance != null && DebtSystem.Instance.IsOverdrafted;

        // 透支状态下，非 food/study 分类直接返回空
        if (isOverdrafted && category != "food" && category != "study")
        {
            return new ShopItemDefinition[0];
        }

        List<ShopItemDefinition> result = allItems.Where(item => item.category == category).ToList();

        // 饮食受限时，food 分类只保留最便宜的（价格 ≤ 3）
        if (isFoodRestricted && category == "food")
        {
            result = result.Where(item => item.price <= 3).ToList();
        }

        return result.ToArray();
    }

    // ========== 购买方法 ==========

    /// <summary>
    /// 检查指定商品是否可以购买（余额 + 债务限制）
    /// </summary>
    public bool CanBuyItem(string itemId)
    {
        ShopItemDefinition item = FindItem(itemId);
        if (item == null) return false;

        // 余额检查
        if (EconomyManager.Instance == null || !EconomyManager.Instance.CanAfford(item.price))
            return false;

        // 债务限制检查
        bool isFoodRestricted = DebtSystem.Instance != null && DebtSystem.Instance.IsFoodRestricted;
        bool isOverdrafted = DebtSystem.Instance != null && DebtSystem.Instance.IsOverdrafted;

        // 饮食受限：只能买价格 ≤ 3 的食物
        if (isFoodRestricted && item.category == "food" && item.price > 3)
            return false;

        // 透支状态：非 food/study 禁止购买
        if (isOverdrafted && item.category != "food" && item.category != "study")
            return false;

        return true;
    }

    /// <summary>
    /// 购买指定商品：扣款 → 应用属性效果 → 触发事件
    /// </summary>
    /// <returns>购买是否成功</returns>
    public bool BuyItem(string itemId)
    {
        if (!CanBuyItem(itemId))
        {
            Debug.LogWarning($"[ShopSystem] 无法购买商品: {itemId}");
            return false;
        }

        ShopItemDefinition item = FindItem(itemId);

        // 扣款（通过 EconomyManager 记录流水）
        TransactionRecord.TransactionType txType = CategoryToTransactionType(item.category);
        EconomyManager.Instance.Spend(item.price, txType, item.displayName);

        // 应用属性效果
        if (item.effects != null && PlayerAttributes.Instance != null)
        {
            foreach (AttributeEffect effect in item.effects)
            {
                PlayerAttributes.Instance.AddAttribute(effect.attributeName, effect.amount);
            }
        }

        Debug.Log($"[ShopSystem] 购买成功: {item.displayName} (¥{item.price})");

        // 触发事件
        OnItemPurchased?.Invoke(item);

        return true;
    }

    // ========== 内部工具 ==========

    /// <summary>根据 ID 查找商品定义</summary>
    private ShopItemDefinition FindItem(string itemId)
    {
        return allItems.Find(item => item.id == itemId);
    }

    /// <summary>将商品分类映射到交易记录类型</summary>
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
