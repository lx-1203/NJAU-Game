using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class InventoryItemSaveData
{
    public string itemId;
    public int quantity;
}

public class InventorySystem : MonoBehaviour, ISaveable
{
    public static InventorySystem Instance { get; private set; }

    public event Action OnInventoryChanged;
    public event Action<string, int> OnItemAdded;
    public event Action<string, int> OnItemRemoved;
    public event Action<ShopItemDefinition> OnItemUsed;

    private readonly Dictionary<string, int> itemQuantities = new Dictionary<string, int>();

    public class InventoryEntry
    {
        public ShopItemDefinition definition;
        public int quantity;
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

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        return itemQuantities.TryGetValue(itemId, out int quantity) ? quantity : 0;
    }

    public int GetTotalItemCount()
    {
        return itemQuantities.Values.Sum();
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return GetItemCount(itemId) >= Mathf.Max(1, amount);
    }

    public bool AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
        {
            return false;
        }

        ShopItemDefinition definition = ShopSystem.Instance != null
            ? ShopSystem.Instance.GetItemDefinition(itemId)
            : null;

        if (definition == null || !definition.canStore)
        {
            Debug.LogWarning($"[InventorySystem] Cannot store item: {itemId}");
            return false;
        }

        int oldQuantity = GetItemCount(itemId);
        int newQuantity = Mathf.Min(oldQuantity + amount, Mathf.Max(1, definition.maxStack));
        int delta = newQuantity - oldQuantity;
        if (delta <= 0)
        {
            return false;
        }

        itemQuantities[itemId] = newQuantity;
        OnItemAdded?.Invoke(itemId, delta);
        NotifyInventoryChanged();
        return true;
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0 || !itemQuantities.TryGetValue(itemId, out int quantity))
        {
            return false;
        }

        int remaining = quantity - amount;
        if (remaining > 0)
        {
            itemQuantities[itemId] = remaining;
        }
        else
        {
            itemQuantities.Remove(itemId);
        }

        OnItemRemoved?.Invoke(itemId, Mathf.Min(amount, quantity));
        NotifyInventoryChanged();
        return true;
    }

    public bool CanUseItem(string itemId)
    {
        if (!HasItem(itemId))
        {
            return false;
        }

        ShopItemDefinition definition = ShopSystem.Instance != null
            ? ShopSystem.Instance.GetItemDefinition(itemId)
            : null;

        return definition != null && definition.canUse;
    }

    public bool UseItem(string itemId)
    {
        if (!CanUseItem(itemId))
        {
            return false;
        }

        ShopItemDefinition definition = ShopSystem.Instance.GetItemDefinition(itemId);
        if (definition.effects != null && PlayerAttributes.Instance != null)
        {
            foreach (AttributeEffect effect in definition.effects)
            {
                PlayerAttributes.Instance.AddAttribute(effect.attributeName, effect.amount);
            }
        }

        if (definition.consumeOnUse)
        {
            RemoveItem(itemId, 1);
        }
        else
        {
            NotifyInventoryChanged();
        }

        OnItemUsed?.Invoke(definition);
        return true;
    }

    public List<InventoryEntry> GetAllEntries()
    {
        List<InventoryEntry> entries = new List<InventoryEntry>();

        foreach (var pair in itemQuantities)
        {
            ShopItemDefinition definition = ShopSystem.Instance != null
                ? ShopSystem.Instance.GetItemDefinition(pair.Key)
                : null;

            if (definition == null)
            {
                continue;
            }

            entries.Add(new InventoryEntry
            {
                definition = definition,
                quantity = pair.Value
            });
        }

        return entries
            .OrderBy(entry => entry.definition.category)
            .ThenBy(entry => entry.definition.displayName)
            .ToList();
    }

    public void SaveToData(SaveData data)
    {
        data.inventoryItems = itemQuantities
            .Where(pair => pair.Value > 0)
            .Select(pair => new InventoryItemSaveData
            {
                itemId = pair.Key,
                quantity = pair.Value
            })
            .ToList();
    }

    public void LoadFromData(SaveData data)
    {
        itemQuantities.Clear();

        if (data.inventoryItems != null)
        {
            foreach (InventoryItemSaveData item in data.inventoryItems)
            {
                if (item == null || string.IsNullOrEmpty(item.itemId) || item.quantity <= 0)
                {
                    continue;
                }

                itemQuantities[item.itemId] = item.quantity;
            }
        }

        NotifyInventoryChanged();
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
