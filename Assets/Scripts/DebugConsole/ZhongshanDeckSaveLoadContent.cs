using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ZhongshanDeckSaveLoadContent
{
    public List<ZhongshanDeckSaveLoadLayoutItem> layoutItems = new List<ZhongshanDeckSaveLoadLayoutItem>();

    public void EnsureInitialized()
    {
        layoutItems ??= new List<ZhongshanDeckSaveLoadLayoutItem>();
        ZhongshanDeckSaveLoadContentDefaults.EnsureLayoutItems(layoutItems);

        for (int i = 0; i < layoutItems.Count; i++)
        {
            layoutItems[i]?.EnsureInitialized();
        }
    }

    public ZhongshanDeckSaveLoadContent Clone()
    {
        ZhongshanDeckSaveLoadContent clone = new ZhongshanDeckSaveLoadContent
        {
            layoutItems = new List<ZhongshanDeckSaveLoadLayoutItem>()
        };

        for (int i = 0; i < layoutItems.Count; i++)
        {
            if (layoutItems[i] != null)
            {
                clone.layoutItems.Add(layoutItems[i].Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public class ZhongshanDeckSaveLoadLayoutItem
{
    public string key;
    public string displayName;
    public ZhongshanDeckLayoutAnchor anchor = ZhongshanDeckLayoutAnchor.Center;
    public Vector2 anchoredPosition;
    public Vector2 size = new Vector2(100f, 100f);
    public bool visible = true;
    public bool locked;

    public void EnsureInitialized()
    {
        key ??= string.Empty;
        displayName ??= key ?? string.Empty;
        size.x = Mathf.Max(24f, size.x);
        size.y = Mathf.Max(24f, size.y);
    }

    public ZhongshanDeckSaveLoadLayoutItem Clone()
    {
        return new ZhongshanDeckSaveLoadLayoutItem
        {
            key = key,
            displayName = displayName,
            anchor = anchor,
            anchoredPosition = anchoredPosition,
            size = size,
            visible = visible,
            locked = locked
        };
    }
}

public static class ZhongshanDeckSaveLoadContentDefaults
{
    public const string LayoutBoard = "save_board";
    public const string LayoutTopOverlay = "top_overlay";
    public const string LayoutPrevPageButton = "prev_page_button";
    public const string LayoutNextPageButton = "next_page_button";
    public const string LayoutAutoSlot = "slot_auto";
    public const string LayoutSlot01 = "slot_01";
    public const string LayoutSlot02 = "slot_02";
    public const string LayoutSlot03 = "slot_03";
    public const string LayoutReturnButton = "return_button";

    public static string GetSlotPhotoKey(int slot) => $"slot_{slot}_photo";
    public static string GetSlotInfoKey(int slot) => $"slot_{slot}_info";
    public static string GetSlotButtonsKey(int slot) => $"slot_{slot}_buttons";
    public static string GetSlotPrimaryButtonKey(int slot) => $"slot_{slot}_button_primary";
    public static string GetSlotLeftButtonKey(int slot) => $"slot_{slot}_button_left";
    public static string GetSlotRightButtonKey(int slot) => $"slot_{slot}_button_right";

    public static void EnsureLayoutItems(List<ZhongshanDeckSaveLoadLayoutItem> layoutItems)
    {
        if (layoutItems == null)
        {
            return;
        }

        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutBoard, "存档板", ZhongshanDeckLayoutAnchor.Center, Vector2.zero, new Vector2(1520f, 848f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutTopOverlay, "顶部装饰", ZhongshanDeckLayoutAnchor.Center, Vector2.zero, new Vector2(1520f, 848f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutPrevPageButton, "左翻页按钮", ZhongshanDeckLayoutAnchor.Center, new Vector2(-619f, -40f), new Vector2(48f, 48f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutNextPageButton, "右翻页按钮", ZhongshanDeckLayoutAnchor.Center, new Vector2(751f, -40f), new Vector2(48f, 48f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutAutoSlot, "自动存档", ZhongshanDeckLayoutAnchor.Center, new Vector2(-268f, 96f), new Vector2(470f, 210f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutSlot01, "存档 01", ZhongshanDeckLayoutAnchor.Center, new Vector2(284f, 96f), new Vector2(470f, 210f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutSlot02, "存档 02", ZhongshanDeckLayoutAnchor.Center, new Vector2(-268f, -206f), new Vector2(470f, 210f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutSlot03, "存档 03", ZhongshanDeckLayoutAnchor.Center, new Vector2(284f, -206f), new Vector2(470f, 210f)));
        EnsureSlotChildren(layoutItems, 0, "自动存档");
        EnsureSlotChildren(layoutItems, 1, "存档 01");
        EnsureSlotChildren(layoutItems, 2, "存档 02");
        EnsureSlotChildren(layoutItems, 3, "存档 03");
        EnsureLayoutItem(layoutItems, CreateLayoutItem(LayoutReturnButton, "返回按钮", ZhongshanDeckLayoutAnchor.TopRight, new Vector2(-126f, -126f), new Vector2(170f, 86f)));
    }

    private static void EnsureSlotChildren(List<ZhongshanDeckSaveLoadLayoutItem> layoutItems, int slot, string slotLabel)
    {
        EnsureLayoutItem(layoutItems, CreateLayoutItem(GetSlotPhotoKey(slot), $"{slotLabel}·截图区", ZhongshanDeckLayoutAnchor.Center, new Vector2(-95f, -5f), new Vector2(116f, 90f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(GetSlotInfoKey(slot), $"{slotLabel}·信息区", ZhongshanDeckLayoutAnchor.Center, new Vector2(87f, -8f), new Vector2(300f, 156f)));
        EnsureLayoutItem(layoutItems, CreateLayoutItem(GetSlotButtonsKey(slot), $"{slotLabel}·按钮区", ZhongshanDeckLayoutAnchor.Center, new Vector2(-25f, 63f), new Vector2(190f, 44f)));

        if (slot == 0)
        {
            EnsureLayoutItem(layoutItems, CreateLayoutItem(GetSlotPrimaryButtonKey(slot), $"{slotLabel}·主按钮", ZhongshanDeckLayoutAnchor.Center, Vector2.zero, new Vector2(190f, 44f)));
        }
        else
        {
            EnsureLayoutItem(layoutItems, CreateLayoutItem(GetSlotLeftButtonKey(slot), $"{slotLabel}·左按钮", ZhongshanDeckLayoutAnchor.Center, new Vector2(-49f, 0f), new Vector2(91f, 44f)));
            EnsureLayoutItem(layoutItems, CreateLayoutItem(GetSlotRightButtonKey(slot), $"{slotLabel}·右按钮", ZhongshanDeckLayoutAnchor.Center, new Vector2(49f, 0f), new Vector2(91f, 44f)));
        }
    }

    private static ZhongshanDeckSaveLoadLayoutItem CreateLayoutItem(string key, string displayName, ZhongshanDeckLayoutAnchor anchor, Vector2 anchoredPosition, Vector2 size)
    {
        return new ZhongshanDeckSaveLoadLayoutItem
        {
            key = key,
            displayName = displayName,
            anchor = anchor,
            anchoredPosition = anchoredPosition,
            size = size,
            visible = true
        };
    }

    private static void EnsureLayoutItem(List<ZhongshanDeckSaveLoadLayoutItem> layoutItems, ZhongshanDeckSaveLoadLayoutItem fallback)
    {
        for (int i = 0; i < layoutItems.Count; i++)
        {
            ZhongshanDeckSaveLoadLayoutItem existing = layoutItems[i];
            if (existing != null && string.Equals(existing.key, fallback.key, StringComparison.Ordinal))
            {
                existing.displayName = string.IsNullOrWhiteSpace(existing.displayName) ? fallback.displayName : existing.displayName;
                existing.size.x = Mathf.Max(24f, existing.size.x);
                existing.size.y = Mathf.Max(24f, existing.size.y);
                return;
            }
        }

        layoutItems.Add(fallback);
    }
}
