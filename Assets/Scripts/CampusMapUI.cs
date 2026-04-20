using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 校园地图 UI —— 弹窗式覆盖地图（非 MonoBehaviour）
/// 点击"地图"按钮打开，选择地点后关闭
/// 由 HUDManager 管理生命周期
/// </summary>
public class CampusMapUI
{
    // ========== 事件 ==========

    /// <summary>地点节点被点击时触发，参数为目标地点 ID</summary>
    public event Action<LocationId> OnLocationNodeClicked;

    // ========== 颜色方案 ==========

    private static readonly Color OverlayBgColor     = new Color(0f, 0f, 0f, 0.70f);
    private static readonly Color PanelBgColor       = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color ButtonNormalColor   = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color TextWhite           = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold            = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color CurrentNodeColor    = new Color(0.85f, 0.65f, 0.15f, 1.0f);
    private static readonly Color CurrentNodeBorder   = new Color(1.0f, 0.85f, 0.30f, 1.0f);
    private static readonly Color LinkLineColor       = new Color(0.35f, 0.40f, 0.55f, 0.6f);
    private static readonly Color NPCIndicatorColor   = new Color(0.40f, 0.85f, 0.50f, 1.0f);
    private static readonly Color CloseButtonColor    = new Color(0.50f, 0.20f, 0.20f, 1.0f);
    private static readonly Color CloseHoverColor     = new Color(0.65f, 0.30f, 0.30f, 1.0f);

    // ========== 布局常量 ==========

    private const float NodeWidth      = 130f;
    private const float NodeHeight     = 60f;
    private const float LinkLineHeight = 2f;
    private const float TitleHeight    = 40f;
    private const float MapPadding     = 40f;

    // ========== 内部引用 ==========

    private GameObject overlayRoot;     // 全屏覆盖层根节点
    private GameObject mapPanel;        // 地图面板（居中）
    private TextMeshProUGUI titleText;
    private GameObject nodesContainer;
    private GameObject linksContainer;

    /// <summary>覆盖层根节点（供 LocationDetailPanel 作为父级）</summary>
    public GameObject OverlayRoot => overlayRoot;

    // 节点数据缓存
    private Dictionary<LocationId, NodeEntry> nodeEntries = new Dictionary<LocationId, NodeEntry>();

    private class NodeEntry
    {
        public GameObject root;
        public Button button;
        public Image bgImage;
        public Image borderImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI npcText;
        public LocationDefinition locationDef;
    }

    // ========== 可见性 ==========

    public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;

    public void ShowOverlay()
    {
        if (overlayRoot != null)
        {
            RefreshMap();
            overlayRoot.SetActive(true);
        }
    }

    public void HideOverlay()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    // ========== 构建 ==========

    /// <summary>
    /// 在指定 Canvas 上构建弹窗式地图覆盖层
    /// </summary>
    public void BuildMapOverlay(Canvas canvas)
    {
        if (canvas == null)
        {
            Debug.LogError("[CampusMapUI] Canvas 为 null，无法构建地图");
            return;
        }

        // --- 全屏覆盖层 ---
        overlayRoot = new GameObject("CampusMapOverlay");
        overlayRoot.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = overlayRoot.AddComponent<RectTransform>();
        StretchFill(overlayRT);
        // 确保在最前面
        overlayRoot.transform.SetAsLastSibling();

        // 半透明黑底（点击关闭）
        Image overlayBg = overlayRoot.AddComponent<Image>();
        overlayBg.color = OverlayBgColor;
        Button overlayBtn = overlayRoot.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(HideOverlay);

        // --- 居中地图面板（80% 屏幕大小） ---
        mapPanel = new GameObject("MapPanel");
        mapPanel.transform.SetParent(overlayRoot.transform, false);
        RectTransform panelRT = mapPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.1f, 0.08f);
        panelRT.anchorMax = new Vector2(0.9f, 0.92f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        Image panelBg = mapPanel.AddComponent<Image>();
        panelBg.color = PanelBgColor;
        panelBg.raycastTarget = true; // 阻止点击穿透到覆盖层关闭按钮

        // 地图面板内的点击不触发关闭
        Button panelBlocker = mapPanel.AddComponent<Button>();
        panelBlocker.transition = Selectable.Transition.None;
        // 不绑定任何事件，仅拦截点击

        // --- 关闭按钮 ---
        CreateCloseButton();

        // --- 标题 ---
        CreateTitle();

        // --- 连接线容器 ---
        linksContainer = new GameObject("LinksContainer");
        linksContainer.transform.SetParent(mapPanel.transform, false);
        RectTransform linksRT = linksContainer.AddComponent<RectTransform>();
        StretchFill(linksRT);

        // --- 节点容器 ---
        nodesContainer = new GameObject("NodesContainer");
        nodesContainer.transform.SetParent(mapPanel.transform, false);
        RectTransform nodesRT = nodesContainer.AddComponent<RectTransform>();
        StretchFill(nodesRT);

        // --- 创建地点节点和连线 ---
        if (LocationManager.Instance != null)
        {
            LocationDefinition[] allLocations = LocationManager.Instance.GetAllLocations();
            foreach (var locDef in allLocations)
            {
                CreateLocationNode(locDef);
            }

            List<LocationLink> links = LocationManager.Instance.GetAllLinks();
            foreach (var link in links)
            {
                CreateLinkLine(link);
            }
        }

        // 初始隐藏
        overlayRoot.SetActive(false);
    }

    // ========== 刷新 ==========

    public void RefreshMap()
    {
        if (LocationManager.Instance == null || GameState.Instance == null) return;

        LocationId currentLoc = GameState.Instance.CurrentLocation;

        // 更新标题
        LocationDefinition currentDef = LocationManager.Instance.GetLocation(currentLoc);
        if (titleText != null && currentDef != null)
        {
            titleText.text = $"当前位置：{currentDef.displayName}";
        }

        // 更新每个节点的状态
        foreach (var kvp in nodeEntries)
        {
            LocationId locId = kvp.Key;
            NodeEntry entry = kvp.Value;
            bool isCurrent = (locId == currentLoc);

            if (isCurrent)
            {
                entry.bgImage.color = CurrentNodeColor;
                entry.borderImage.color = CurrentNodeBorder;
                entry.button.interactable = false;
                entry.nameText.color = PanelBgColor;
            }
            else
            {
                entry.bgImage.color = ButtonNormalColor;
                entry.borderImage.color = new Color(0f, 0f, 0f, 0f);
                entry.button.interactable = true;
                entry.nameText.color = TextWhite;
            }

            // NPC 数量
            string[] npcs = LocationManager.Instance.GetNPCsAtLocation(locId);
            if (npcs != null && npcs.Length > 0)
            {
                entry.npcText.text = $"NPC: {npcs.Length}";
                entry.npcText.gameObject.SetActive(true);
            }
            else
            {
                entry.npcText.gameObject.SetActive(false);
            }
        }
    }

    public void Destroy()
    {
        if (overlayRoot != null)
        {
            UnityEngine.Object.Destroy(overlayRoot);
        }
        nodeEntries.Clear();
    }

    // ========== 内部构建方法 ==========

    private void CreateCloseButton()
    {
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(mapPanel.transform, false);

        RectTransform closeRT = closeBtnObj.AddComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-8, -8);
        closeRT.sizeDelta = new Vector2(40, 40);

        Image closeBg = closeBtnObj.AddComponent<Image>();
        closeBg.color = CloseButtonColor;

        Button closeBtn = closeBtnObj.AddComponent<Button>();
        ColorBlock cb = closeBtn.colors;
        cb.normalColor = CloseButtonColor;
        cb.highlightedColor = CloseHoverColor;
        cb.pressedColor = new Color(0.40f, 0.15f, 0.15f, 1.0f);
        cb.selectedColor = CloseButtonColor;
        closeBtn.colors = cb;
        closeBtn.onClick.AddListener(HideOverlay);

        // X 文字
        GameObject xObj = new GameObject("XText");
        xObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform xRT = xObj.AddComponent<RectTransform>();
        StretchFill(xRT);
        TextMeshProUGUI xText = xObj.AddComponent<TextMeshProUGUI>();
        xText.text = "✕";
        xText.fontSize = 22f;
        xText.color = TextWhite;
        xText.alignment = TextAlignmentOptions.Center;
        xText.raycastTarget = false;
    }

    private void CreateTitle()
    {
        GameObject titleObj = new GameObject("MapTitle");
        titleObj.transform.SetParent(mapPanel.transform, false);

        RectTransform rt = titleObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -8f);
        rt.sizeDelta = new Vector2(0, TitleHeight);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "当前位置：---";
        titleText.fontSize = 22f;
        titleText.color = TextGold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.raycastTarget = false;
    }

    private void CreateLocationNode(LocationDefinition locDef)
    {
        // --- 外层边框 ---
        GameObject borderObj = new GameObject($"Node_{locDef.id}_Border");
        borderObj.transform.SetParent(nodesContainer.transform, false);

        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.sizeDelta = new Vector2(NodeWidth + 4f, NodeHeight + 4f);
        borderRT.anchorMin = new Vector2(0, 0);
        borderRT.anchorMax = new Vector2(0, 0);
        borderRT.pivot = new Vector2(0.5f, 0.5f);

        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0f, 0f, 0f, 0f);

        // --- 内层背景 + 按钮 ---
        GameObject nodeObj = new GameObject($"Node_{locDef.id}");
        nodeObj.transform.SetParent(borderObj.transform, false);

        RectTransform nodeRT = nodeObj.AddComponent<RectTransform>();
        StretchFill(nodeRT, 2f);

        Image bgImage = nodeObj.AddComponent<Image>();
        bgImage.color = ButtonNormalColor;

        Button button = nodeObj.AddComponent<Button>();
        ColorBlock bcb = button.colors;
        bcb.normalColor = Color.white;
        bcb.highlightedColor = new Color(1f, 1f, 1f, 1f);
        bcb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        bcb.selectedColor = Color.white;
        bcb.fadeDuration = 0.1f;
        button.colors = bcb;
        button.targetGraphic = bgImage;

        LocationId capturedId = locDef.id;
        button.onClick.AddListener(() => OnNodeClicked(capturedId));

        // --- 地点名称 ---
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(nodeObj.transform, false);

        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0.3f);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.offsetMin = new Vector2(4f, 0);
        nameRT.offsetMax = new Vector2(-4f, -2f);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = $"{locDef.iconChar} {locDef.displayName}";
        nameText.fontSize = 16f;
        nameText.color = TextWhite;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.raycastTarget = false;

        // --- NPC 指示 ---
        GameObject npcObj = new GameObject("NPCText");
        npcObj.transform.SetParent(nodeObj.transform, false);

        RectTransform npcRT = npcObj.AddComponent<RectTransform>();
        npcRT.anchorMin = new Vector2(0, 0);
        npcRT.anchorMax = new Vector2(1, 0.35f);
        npcRT.offsetMin = new Vector2(4f, 2f);
        npcRT.offsetMax = new Vector2(-4f, 0);

        TextMeshProUGUI npcText = npcObj.AddComponent<TextMeshProUGUI>();
        npcText.text = "";
        npcText.fontSize = 11f;
        npcText.color = NPCIndicatorColor;
        npcText.alignment = TextAlignmentOptions.Center;
        npcText.enableWordWrapping = false;
        npcText.raycastTarget = false;
        npcObj.SetActive(false);

        PositionNode(borderRT, locDef.mapPosition);

        nodeEntries[locDef.id] = new NodeEntry
        {
            root = borderObj,
            button = button,
            bgImage = bgImage,
            borderImage = borderImage,
            nameText = nameText,
            npcText = npcText,
            locationDef = locDef
        };
    }

    private void PositionNode(RectTransform nodeRT, Vector2 mapPosition)
    {
        float normalizedX = mapPosition.x;
        float normalizedY = mapPosition.y;

        nodeRT.anchorMin = new Vector2(normalizedX, normalizedY);
        nodeRT.anchorMax = new Vector2(normalizedX, normalizedY);

        float xOffset = Mathf.Lerp(MapPadding, -MapPadding, normalizedX);
        float yOffset = Mathf.Lerp(MapPadding, -(MapPadding + TitleHeight), normalizedY);
        nodeRT.anchoredPosition = new Vector2(xOffset, yOffset);
    }

    private void CreateLinkLine(LocationLink link)
    {
        if (!nodeEntries.ContainsKey(link.from) || !nodeEntries.ContainsKey(link.to))
            return;

        NodeEntry fromEntry = nodeEntries[link.from];
        NodeEntry toEntry = nodeEntries[link.to];

        Vector2 fromPos = GetNodeWorldCenter(fromEntry.locationDef.mapPosition);
        Vector2 toPos = GetNodeWorldCenter(toEntry.locationDef.mapPosition);

        GameObject lineObj = new GameObject($"Link_{link.from}_{link.to}");
        lineObj.transform.SetParent(linksContainer.transform, false);

        RectTransform lineRT = lineObj.AddComponent<RectTransform>();

        Vector2 diff = toPos - fromPos;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        float midNormX = (fromEntry.locationDef.mapPosition.x + toEntry.locationDef.mapPosition.x) / 2f;
        float midNormY = (fromEntry.locationDef.mapPosition.y + toEntry.locationDef.mapPosition.y) / 2f;

        lineRT.anchorMin = new Vector2(midNormX, midNormY);
        lineRT.anchorMax = new Vector2(midNormX, midNormY);
        float xOffset = Mathf.Lerp(MapPadding, -MapPadding, midNormX);
        float yOffset = Mathf.Lerp(MapPadding, -(MapPadding + TitleHeight), midNormY);
        lineRT.anchoredPosition = new Vector2(xOffset, yOffset);

        lineRT.sizeDelta = new Vector2(distance, LinkLineHeight);
        lineRT.pivot = new Vector2(0.5f, 0.5f);
        lineRT.localRotation = Quaternion.Euler(0, 0, angle);

        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = LinkLineColor;
        lineImage.raycastTarget = false;
    }

    private Vector2 GetNodeWorldCenter(Vector2 mapPosition)
    {
        // 使用地图面板的参考大小估算
        float refWidth = 1536f;  // 80% of 1920
        float refHeight = 907f;  // 84% of 1080

        float usableWidth = refWidth - MapPadding * 2f;
        float usableHeight = refHeight - MapPadding * 2f - TitleHeight;

        float x = MapPadding + mapPosition.x * usableWidth;
        float y = MapPadding + mapPosition.y * usableHeight;

        return new Vector2(x, y);
    }

    // ========== 事件处理 ==========

    private void OnNodeClicked(LocationId locationId)
    {
        OnLocationNodeClicked?.Invoke(locationId);
    }

    // ========== 工具方法 ==========

    private void StretchFill(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
