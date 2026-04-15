using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 校园地图 UI —— 在 HUDBuilder 的 centerPanel 内构建节点式校园地图
/// 非 MonoBehaviour，由 HUDManager 管理生命周期
///
/// 功能：
/// - 按 LocationDefinition.mapPosition 归一化坐标定位地点节点
/// - 相邻地点之间绘制连接线
/// - 当前地点高亮显示（金色边框 + 不可点击）
/// - 每个节点显示地点名称 + NPC 数量指示
/// - 点击节点触发 OnLocationNodeClicked 事件
/// </summary>
public class CampusMapUI
{
    // ========== 事件 ==========

    /// <summary>地点节点被点击时触发，参数为目标地点 ID</summary>
    public event Action<LocationId> OnLocationNodeClicked;

    // ========== 颜色方案（复用 HUDBuilder 配色） ==========

    private static readonly Color PanelBgColor       = new Color(0.08f, 0.08f, 0.12f, 0.90f);
    private static readonly Color CenterPanelColor   = new Color(0.05f, 0.05f, 0.08f, 0.50f);
    private static readonly Color ButtonNormalColor   = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ButtonHoverColor    = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color ButtonPressedColor  = new Color(0.15f, 0.25f, 0.50f, 1.0f);
    private static readonly Color TextWhite           = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold            = new Color(1.0f, 0.85f, 0.30f);

    // 当前地点节点专用色
    private static readonly Color CurrentNodeColor    = new Color(0.85f, 0.65f, 0.15f, 1.0f);
    private static readonly Color CurrentNodeBorder   = new Color(1.0f, 0.85f, 0.30f, 1.0f);

    // 连接线颜色
    private static readonly Color LinkLineColor       = new Color(0.35f, 0.40f, 0.55f, 0.6f);

    // NPC 指示颜色
    private static readonly Color NPCIndicatorColor   = new Color(0.40f, 0.85f, 0.50f, 1.0f);

    // ========== 布局常量 ==========

    private const float NodeWidth      = 130f;
    private const float NodeHeight     = 60f;
    private const float LinkLineHeight = 2f;
    private const float TitleHeight    = 40f;
    private const float MapPadding     = 40f;  // 地图区域四周留白

    // ========== 内部引用 ==========

    private GameObject centerPanel;
    private GameObject mapRoot;          // 地图根节点（放在 centerPanel 内）
    private TextMeshProUGUI titleText;   // 顶部标题 "当前位置：XX"
    private GameObject nodesContainer;   // 节点容器
    private GameObject linksContainer;   // 连接线容器

    // 节点数据缓存
    private Dictionary<LocationId, NodeEntry> nodeEntries = new Dictionary<LocationId, NodeEntry>();

    /// <summary>单个地点节点的 UI 引用集合</summary>
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

    // ========== 对外接口 ==========

    /// <summary>
    /// 在指定的 centerPanel 内构建完整的校园地图 UI
    /// </summary>
    /// <param name="panel">HUDBuilder.centerPanel 引用</param>
    public void BuildMap(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError("[CampusMapUI] centerPanel 为 null，无法构建地图");
            return;
        }

        centerPanel = panel;

        // 清除 centerPanel 中的占位内容
        ClearChildren(centerPanel.transform);

        // 创建地图根节点
        mapRoot = new GameObject("CampusMap");
        mapRoot.transform.SetParent(centerPanel.transform, false);
        RectTransform mapRT = mapRoot.AddComponent<RectTransform>();
        StretchFill(mapRT);

        // 1. 创建顶部标题
        CreateTitle();

        // 2. 创建连接线容器（先创建，让线在节点下方）
        linksContainer = new GameObject("LinksContainer");
        linksContainer.transform.SetParent(mapRoot.transform, false);
        RectTransform linksRT = linksContainer.AddComponent<RectTransform>();
        StretchFill(linksRT);

        // 3. 创建节点容器
        nodesContainer = new GameObject("NodesContainer");
        nodesContainer.transform.SetParent(mapRoot.transform, false);
        RectTransform nodesRT = nodesContainer.AddComponent<RectTransform>();
        StretchFill(nodesRT);

        // 4. 创建所有地点节点
        if (LocationManager.Instance != null)
        {
            LocationDefinition[] allLocations = LocationManager.Instance.GetAllLocations();
            foreach (var locDef in allLocations)
            {
                CreateLocationNode(locDef);
            }

            // 5. 绘制连接线
            List<LocationLink> links = LocationManager.Instance.GetAllLinks();
            foreach (var link in links)
            {
                CreateLinkLine(link);
            }
        }
        else
        {
            Debug.LogWarning("[CampusMapUI] LocationManager 实例不存在，无法加载地点数据");
        }

        // 6. 首次刷新高亮状态
        RefreshMap();
    }

    /// <summary>
    /// 刷新地图状态：更新当前地点高亮、NPC 指示
    /// </summary>
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

            // 更新背景色和交互状态
            if (isCurrent)
            {
                // 当前地点：金色高亮，不可点击
                entry.bgImage.color = CurrentNodeColor;
                entry.borderImage.color = CurrentNodeBorder;
                entry.button.interactable = false;
                entry.nameText.color = PanelBgColor;
            }
            else
            {
                // 其他地点：普通蓝色，可点击
                entry.bgImage.color = ButtonNormalColor;
                entry.borderImage.color = new Color(0f, 0f, 0f, 0f); // 透明边框
                entry.button.interactable = true;
                entry.nameText.color = TextWhite;
            }

            // 更新 NPC 数量显示
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

    /// <summary>
    /// 销毁地图 UI（在需要重建时调用）
    /// </summary>
    public void Destroy()
    {
        if (mapRoot != null)
        {
            UnityEngine.Object.Destroy(mapRoot);
        }
        nodeEntries.Clear();
    }

    // ========== 内部构建方法 ==========

    /// <summary>创建顶部位置标题</summary>
    private void CreateTitle()
    {
        GameObject titleObj = new GameObject("MapTitle");
        titleObj.transform.SetParent(mapRoot.transform, false);

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
    }

    /// <summary>
    /// 创建单个地点节点按钮
    /// 节点结构：Border(Image) > Background(Image+Button) > NameText + NPCText
    /// </summary>
    private void CreateLocationNode(LocationDefinition locDef)
    {
        // --- 外层边框 ---
        GameObject borderObj = new GameObject($"Node_{locDef.id}_Border");
        borderObj.transform.SetParent(nodesContainer.transform, false);

        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.sizeDelta = new Vector2(NodeWidth + 4f, NodeHeight + 4f);
        // 使用左下角作为锚点基准，根据 mapPosition 定位
        borderRT.anchorMin = new Vector2(0, 0);
        borderRT.anchorMax = new Vector2(0, 0);
        borderRT.pivot = new Vector2(0.5f, 0.5f);
        // 实际位置将在 PositionNode 中设置

        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0f, 0f, 0f, 0f); // 默认透明

        // --- 内层背景 + 按钮 ---
        GameObject nodeObj = new GameObject($"Node_{locDef.id}");
        nodeObj.transform.SetParent(borderObj.transform, false);

        RectTransform nodeRT = nodeObj.AddComponent<RectTransform>();
        StretchFill(nodeRT, 2f); // 内缩 2px 形成边框效果

        Image bgImage = nodeObj.AddComponent<Image>();
        bgImage.color = ButtonNormalColor;

        Button button = nodeObj.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;  // 使用 Image.color 控制，Button tint 为白
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.1f;
        btn_SetTargetGraphic(button, bgImage);

        // 点击事件
        LocationId capturedId = locDef.id;
        button.onClick.AddListener(() => OnNodeClicked(capturedId));

        // --- 地点名称文本 ---
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

        // --- NPC 数量指示 ---
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

        // 定位节点
        PositionNode(borderRT, locDef.mapPosition);

        // 缓存
        NodeEntry entry = new NodeEntry
        {
            root = borderObj,
            button = button,
            bgImage = bgImage,
            borderImage = borderImage,
            nameText = nameText,
            npcText = npcText,
            locationDef = locDef
        };
        nodeEntries[locDef.id] = entry;
    }

    /// <summary>
    /// 根据归一化 mapPosition(0-1) 计算节点在地图区域内的锚定位置
    /// 地图区域 = centerPanel 减去标题栏和四周留白
    /// </summary>
    private void PositionNode(RectTransform nodeRT, Vector2 mapPosition)
    {
        // 使用百分比锚点定位：将 mapPosition 映射到可用区域
        // 可用区域：上方留出标题(TitleHeight+8)，四周留白 MapPadding
        // 使用 anchorMin/anchorMax 方式实现自适应布局

        // 计算归一化可用区域内的锚点
        // X: MapPadding ~ (1 - MapPadding/panelWidth) → 简化为直接用百分比 + offset
        // 改用锚点百分比方式更健壮：

        nodeRT.anchorMin = mapPosition;
        nodeRT.anchorMax = mapPosition;
        nodeRT.anchoredPosition = new Vector2(0, -(TitleHeight + 8f) * mapPosition.y);

        // 修正：不用上面的方式，改用全范围锚点 + 偏移
        // 让节点根据归一化坐标分布在地图区域
        // Y 轴翻转（UI 坐标系 Y 向上，但 mapPosition.y=0.8 表示偏上方）
        float normalizedX = mapPosition.x;
        float normalizedY = mapPosition.y;

        nodeRT.anchorMin = new Vector2(normalizedX, normalizedY);
        nodeRT.anchorMax = new Vector2(normalizedX, normalizedY);

        // 通过 padding 偏移避免标题遮挡和边缘裁剪
        // 将归一化坐标映射到 padding 范围内（用 anchoredPosition 微调）
        // 注：由于 anchor 已经占满 centerPanel，实际效果是自适应缩放
        float xOffset = Mathf.Lerp(MapPadding, -MapPadding, normalizedX);
        float yOffset = Mathf.Lerp(MapPadding, -(MapPadding + TitleHeight), normalizedY);
        nodeRT.anchoredPosition = new Vector2(xOffset, yOffset);
    }

    /// <summary>
    /// 创建两个地点之间的连接线（使用旋转的细 Image）
    /// </summary>
    private void CreateLinkLine(LocationLink link)
    {
        if (!nodeEntries.ContainsKey(link.from) || !nodeEntries.ContainsKey(link.to))
            return;

        NodeEntry fromEntry = nodeEntries[link.from];
        NodeEntry toEntry = nodeEntries[link.to];

        RectTransform fromRT = fromEntry.root.GetComponent<RectTransform>();
        RectTransform toRT = toEntry.root.GetComponent<RectTransform>();

        // 获取两个节点在 linksContainer 中的中心点位置
        // 由于节点和线都在 mapRoot 下，使用 world position 转换
        Vector2 fromPos = GetNodeWorldCenter(fromEntry.locationDef.mapPosition);
        Vector2 toPos = GetNodeWorldCenter(toEntry.locationDef.mapPosition);

        // 创建线条 Image
        GameObject lineObj = new GameObject($"Link_{link.from}_{link.to}");
        lineObj.transform.SetParent(linksContainer.transform, false);

        RectTransform lineRT = lineObj.AddComponent<RectTransform>();

        // 计算线条长度和角度
        Vector2 diff = toPos - fromPos;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        // 线条定位：中心点在两节点的中间
        Vector2 midPoint = (fromPos + toPos) / 2f;

        // 使用相同的锚点定位方式
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

    /// <summary>
    /// 根据归一化坐标估算节点在父容器中的近似像素位置（用于计算线条长度和角度）
    /// </summary>
    private Vector2 GetNodeWorldCenter(Vector2 mapPosition)
    {
        // 使用参考分辨率估算（CanvasScaler 参考 1920x1080）
        // centerPanel 大约占 1920-260=1660 宽, 1080-60-70=950 高
        float refWidth = 1660f;
        float refHeight = 950f;

        float usableWidth = refWidth - MapPadding * 2f;
        float usableHeight = refHeight - MapPadding * 2f - TitleHeight;

        float x = MapPadding + mapPosition.x * usableWidth;
        float y = MapPadding + mapPosition.y * usableHeight;

        return new Vector2(x, y);
    }

    // ========== 事件处理 ==========

    /// <summary>节点被点击时的处理</summary>
    private void OnNodeClicked(LocationId locationId)
    {
        OnLocationNodeClicked?.Invoke(locationId);
    }

    // ========== 工具方法 ==========

    /// <summary>清除指定 Transform 下的所有子对象</summary>
    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    /// <summary>设置 RectTransform 填满父容器</summary>
    private void StretchFill(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    /// <summary>设置 Button 的 targetGraphic</summary>
    private void btn_SetTargetGraphic(Button btn, Image img)
    {
        btn.targetGraphic = img;
    }
}
