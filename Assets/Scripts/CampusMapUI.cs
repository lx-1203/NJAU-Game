using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    public event Action<LocationId> OnLocationNodeClicked;

    private static readonly Color OverlayBgColor = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color PanelBgColor = new Color(0.05f, 0.07f, 0.10f, 0.96f);
    private static readonly Color MapFrameColor = new Color(0.10f, 0.12f, 0.17f, 1.0f);
    private static readonly Color HotspotHitColor = new Color(1f, 1f, 1f, 0.015f);
    private static readonly Color LabelNormalColor = new Color(0.11f, 0.18f, 0.28f, 0.92f);
    private static readonly Color LabelHoveredColor = new Color(0.18f, 0.42f, 0.53f, 0.96f);
    private static readonly Color LabelCurrentColor = new Color(0.98f, 0.83f, 0.34f, 0.98f);
    private static readonly Color MarkerNormalColor = new Color(0.72f, 0.88f, 1.0f, 0.95f);
    private static readonly Color MarkerHoveredColor = new Color(0.48f, 0.92f, 0.88f, 1.0f);
    private static readonly Color MarkerCurrentColor = new Color(1.0f, 0.90f, 0.42f, 1.0f);
    private static readonly Color TextWhite = new Color(0.95f, 0.96f, 0.98f);
    private static readonly Color TextDark = new Color(0.14f, 0.13f, 0.10f);
    private static readonly Color TextGold = new Color(1.0f, 0.86f, 0.35f);
    private static readonly Color NPCIndicatorColor = new Color(0.56f, 0.95f, 0.80f, 1.0f);
    private static readonly Color CloseButtonColor = new Color(0.50f, 0.20f, 0.20f, 1.0f);
    private static readonly Color CloseHoverColor = new Color(0.65f, 0.30f, 0.30f, 1.0f);

    private const string BaseMapResourcePath = "Maps/Campus/campus_map";
    private const float SourceMapWidth = 2752f;
    private const float SourceMapHeight = 1536f;
    private const float SourceMapAspect = SourceMapWidth / SourceMapHeight;
    private const float TitleHeight = 42f;
    private const float LabelWidth = 92f;
    private const float LabelHeight = 30f;
    private const float MarkerSize = 18f;

    private static readonly Dictionary<LocationId, string> HoverOverlayPaths = new Dictionary<LocationId, string>
    {
        { LocationId.Dormitory, "Maps/Campus/hover_dormitory" },
        { LocationId.Canteen, "Maps/Campus/hover_canteen" },
        { LocationId.Library, "Maps/Campus/hover_library" },
        { LocationId.Playground, "Maps/Campus/hover_playground" },
        { LocationId.Store, "Maps/Campus/hover_store" },
        { LocationId.TakeoutStation, "Maps/Campus/hover_gymnasium" }
    };

    private static readonly Dictionary<LocationId, Rect> HoverOverlayRects = new Dictionary<LocationId, Rect>
    {
        { LocationId.Dormitory, ToNormalizedRect(1713f, 919f, 2023f, 1227f) },
        { LocationId.Canteen, ToNormalizedRect(1986f, 851f, 2370f, 1177f) },
        { LocationId.Library, ToNormalizedRect(1675f, 649f, 1924f, 839f) },
        { LocationId.Playground, ToNormalizedRect(1435f, 793f, 1790f, 1058f) },
        { LocationId.Store, ToNormalizedRect(2016f, 914f, 2177f, 1075f) },
        { LocationId.TakeoutStation, ToNormalizedRect(1200f, 1040f, 1525f, 1342f) }
    };

    private GameObject overlayRoot;
    private GameObject mapPanel;
    private TextMeshProUGUI titleText;
    private GameObject mapContentRoot;
    private GameObject nodesContainer;
    private Image mapBaseImage;
    private Image hoverOverlayImage;
    private Sprite baseMapSprite;
    private readonly Dictionary<LocationId, Sprite> hoverSprites = new Dictionary<LocationId, Sprite>();
    private readonly Dictionary<LocationId, NodeEntry> nodeEntries = new Dictionary<LocationId, NodeEntry>();
    private LocationId? hoveredLocationId;
    private readonly HashSet<LocationId> missingHoverOverlayNotified = new HashSet<LocationId>();

    public GameObject OverlayRoot => overlayRoot;
    public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;

    private class NodeEntry
    {
        public GameObject root;
        public Button button;
        public Image markerImage;
        public Image labelBgImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI npcText;
        public LocationDefinition locationDef;
    }

    public void ShowOverlay()
    {
        if (overlayRoot == null)
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("地图不可用", "校园地图覆盖层还没有成功创建，现在暂时无法打开。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }

        ClearHoveredLocation();
        RefreshMap();
        overlayRoot.SetActive(true);
    }

    public void HideOverlay()
    {
        if (overlayRoot == null)
        {
            return;
        }

        ClearHoveredLocation();
        overlayRoot.SetActive(false);
    }

    public void BuildMapOverlay(Canvas canvas)
    {
        if (canvas == null)
        {
            Debug.LogError("[CampusMapUI] Canvas 为 null，无法构建地图");
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("地图构建失败", "校园地图缺少挂载画布，这一轮无法正常生成地图界面。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }

        LoadMapSprites();

        overlayRoot = new GameObject("CampusMapOverlay");
        overlayRoot.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = overlayRoot.AddComponent<RectTransform>();
        StretchFill(overlayRT);
        overlayRoot.transform.SetAsLastSibling();

        Image overlayBg = overlayRoot.AddComponent<Image>();
        overlayBg.color = OverlayBgColor;
        Button overlayBtn = overlayRoot.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(HideOverlay);

        mapPanel = new GameObject("MapPanel");
        mapPanel.transform.SetParent(overlayRoot.transform, false);
        RectTransform panelRT = mapPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.055f, 0.055f);
        panelRT.anchorMax = new Vector2(0.945f, 0.945f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        Image panelBg = mapPanel.AddComponent<Image>();
        panelBg.color = PanelBgColor;
        panelBg.raycastTarget = true;

        Button panelBlocker = mapPanel.AddComponent<Button>();
        panelBlocker.transition = Selectable.Transition.None;

        CreateCloseButton();
        CreateTitle();
        CreateMapContent();

        if (LocationManager.Instance != null)
        {
            LocationDefinition[] allLocations = LocationManager.Instance.GetAllLocations();
            foreach (LocationDefinition locDef in allLocations)
            {
                CreateLocationNode(locDef);
            }
        }

        overlayRoot.SetActive(false);
    }

    public void RefreshMap()
    {
        if (LocationManager.Instance == null || GameState.Instance == null)
        {
            if (MissionUI.Instance != null)
            {
                MissionUI.Instance.ShowSystemNotification("地图未刷新", "地点或时间状态还没有准备好，这次无法更新校园地图信息。", new Color(0.82f, 0.38f, 0.30f), 2.8f);
            }
            return;
        }

        LocationId currentLoc = GameState.Instance.CurrentLocation;
        RefreshTitle(currentLoc);

        foreach (KeyValuePair<LocationId, NodeEntry> kvp in nodeEntries)
        {
            LocationId locId = kvp.Key;
            NodeEntry entry = kvp.Value;
            bool isCurrent = locId == currentLoc;
            bool isHovered = hoveredLocationId.HasValue && hoveredLocationId.Value == locId;

            ApplyNodeVisual(entry, isCurrent, isHovered);

            string[] npcs = LocationManager.Instance.GetNPCsAtLocation(locId);
            if (npcs != null && npcs.Length > 0)
            {
                entry.npcText.text = $"人物 {npcs.Length}";
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
        hoverSprites.Clear();
        hoveredLocationId = null;
    }

    private void CreateCloseButton()
    {
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(mapPanel.transform, false);

        RectTransform closeRT = closeBtnObj.AddComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 1f);
        closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-12f, -12f);
        closeRT.sizeDelta = new Vector2(40f, 40f);

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

        GameObject xObj = new GameObject("XText");
        xObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform xRT = xObj.AddComponent<RectTransform>();
        StretchFill(xRT);

        TextMeshProUGUI xText = xObj.AddComponent<TextMeshProUGUI>();
        xText.text = "X";
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
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -12f);
        rt.sizeDelta = new Vector2(0f, TitleHeight);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "当前位置：---";
        titleText.fontSize = 22f;
        titleText.color = TextGold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.raycastTarget = false;
    }

    private void CreateMapContent()
    {
        GameObject frameObj = new GameObject("MapFrame");
        frameObj.transform.SetParent(mapPanel.transform, false);

        RectTransform frameRT = frameObj.AddComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0f, 0f);
        frameRT.anchorMax = new Vector2(1f, 1f);
        frameRT.offsetMin = new Vector2(18f, 18f);
        frameRT.offsetMax = new Vector2(-18f, -(TitleHeight + 26f));

        Image frameImage = frameObj.AddComponent<Image>();
        frameImage.color = MapFrameColor;

        mapContentRoot = new GameObject("MapContent");
        mapContentRoot.transform.SetParent(frameObj.transform, false);

        RectTransform contentRT = mapContentRoot.AddComponent<RectTransform>();
        StretchFill(contentRT, 12f);

        AspectRatioFitter fitter = mapContentRoot.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = SourceMapAspect;

        GameObject baseMapObj = new GameObject("BaseMap");
        baseMapObj.transform.SetParent(mapContentRoot.transform, false);
        RectTransform baseRT = baseMapObj.AddComponent<RectTransform>();
        StretchFill(baseRT);

        mapBaseImage = baseMapObj.AddComponent<Image>();
        mapBaseImage.raycastTarget = false;
        mapBaseImage.sprite = baseMapSprite;
        mapBaseImage.preserveAspect = false;
        mapBaseImage.color = baseMapSprite != null ? Color.white : new Color(0.18f, 0.22f, 0.28f, 1f);

        GameObject hoverObj = new GameObject("HoverOverlay");
        hoverObj.transform.SetParent(mapContentRoot.transform, false);
        RectTransform hoverRT = hoverObj.AddComponent<RectTransform>();
        StretchFill(hoverRT);

        hoverOverlayImage = hoverObj.AddComponent<Image>();
        hoverOverlayImage.raycastTarget = false;
        hoverOverlayImage.preserveAspect = false;
        hoverOverlayImage.enabled = false;

        nodesContainer = new GameObject("NodesContainer");
        nodesContainer.transform.SetParent(mapContentRoot.transform, false);
        RectTransform nodesRT = nodesContainer.AddComponent<RectTransform>();
        StretchFill(nodesRT);
    }

    private void CreateLocationNode(LocationDefinition locDef)
    {
        Rect hotspotRect = GetHotspotRect(locDef.id, locDef.mapPosition);

        GameObject rootObj = new GameObject($"Node_{locDef.id}");
        rootObj.transform.SetParent(nodesContainer.transform, false);

        RectTransform rootRT = rootObj.AddComponent<RectTransform>();
        rootRT.anchorMin = hotspotRect.min;
        rootRT.anchorMax = hotspotRect.max;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image hitImage = rootObj.AddComponent<Image>();
        hitImage.color = HotspotHitColor;

        Button button = rootObj.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = hitImage;

        LocationId capturedId = locDef.id;
        button.onClick.AddListener(() => OnNodeClicked(capturedId));
        AttachHoverHandlers(rootObj, capturedId);

        GameObject markerObj = new GameObject("Marker");
        markerObj.transform.SetParent(rootObj.transform, false);
        RectTransform markerRT = markerObj.AddComponent<RectTransform>();
        markerRT.anchorMin = new Vector2(0.5f, 0.5f);
        markerRT.anchorMax = new Vector2(0.5f, 0.5f);
        markerRT.pivot = new Vector2(0.5f, 0.5f);
        markerRT.anchoredPosition = Vector2.zero;
        markerRT.sizeDelta = new Vector2(MarkerSize, MarkerSize);

        Image markerImage = markerObj.AddComponent<Image>();
        markerImage.color = MarkerNormalColor;
        markerImage.raycastTarget = false;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rootObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 1f);
        labelRT.anchorMax = new Vector2(0.5f, 1f);
        labelRT.pivot = new Vector2(0.5f, 1f);
        labelRT.anchoredPosition = new Vector2(0f, -4f);
        labelRT.sizeDelta = new Vector2(LabelWidth, LabelHeight);

        Image labelBgImage = labelObj.AddComponent<Image>();
        labelBgImage.color = LabelNormalColor;
        labelBgImage.raycastTarget = false;

        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(labelObj.transform, false);
        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        StretchFill(nameRT, 4f);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = locDef.displayName;
        nameText.fontSize = 15f;
        nameText.color = TextWhite;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.raycastTarget = false;

        GameObject npcObj = new GameObject("NPCText");
        npcObj.transform.SetParent(rootObj.transform, false);
        RectTransform npcRT = npcObj.AddComponent<RectTransform>();
        npcRT.anchorMin = new Vector2(0.5f, 0f);
        npcRT.anchorMax = new Vector2(0.5f, 0f);
        npcRT.pivot = new Vector2(0.5f, 0f);
        npcRT.anchoredPosition = new Vector2(0f, 2f);
        npcRT.sizeDelta = new Vector2(90f, 18f);

        TextMeshProUGUI npcText = npcObj.AddComponent<TextMeshProUGUI>();
        npcText.text = "";
        npcText.fontSize = 11f;
        npcText.color = NPCIndicatorColor;
        npcText.alignment = TextAlignmentOptions.Center;
        npcText.enableWordWrapping = false;
        npcText.raycastTarget = false;
        npcObj.SetActive(false);

        nodeEntries[locDef.id] = new NodeEntry
        {
            root = rootObj,
            button = button,
            markerImage = markerImage,
            labelBgImage = labelBgImage,
            nameText = nameText,
            npcText = npcText,
            locationDef = locDef
        };
    }

    private void ApplyNodeVisual(NodeEntry entry, bool isCurrent, bool isHovered)
    {
        if (isCurrent)
        {
            entry.markerImage.color = MarkerCurrentColor;
            entry.labelBgImage.color = LabelCurrentColor;
            entry.nameText.color = TextDark;
            return;
        }

        if (isHovered)
        {
            entry.markerImage.color = MarkerHoveredColor;
            entry.labelBgImage.color = LabelHoveredColor;
            entry.nameText.color = TextWhite;
            return;
        }

        entry.markerImage.color = MarkerNormalColor;
        entry.labelBgImage.color = LabelNormalColor;
        entry.nameText.color = TextWhite;
    }

    private void AttachHoverHandlers(GameObject target, LocationId locationId)
    {
        EventTrigger trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener(_ => SetHoveredLocation(locationId));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener(_ =>
        {
            if (hoveredLocationId.HasValue && hoveredLocationId.Value == locationId)
            {
                ClearHoveredLocation();
            }
        });
        trigger.triggers.Add(exitEntry);
    }

    private void SetHoveredLocation(LocationId locationId)
    {
        hoveredLocationId = locationId;

        if (hoverOverlayImage != null && hoverSprites.TryGetValue(locationId, out Sprite overlaySprite) && overlaySprite != null)
        {
            hoverOverlayImage.sprite = overlaySprite;
            hoverOverlayImage.enabled = true;
        }
        else if (hoverOverlayImage != null)
        {
            hoverOverlayImage.sprite = null;
            hoverOverlayImage.enabled = false;

            if (!missingHoverOverlayNotified.Contains(locationId) && MissionUI.Instance != null)
            {
                missingHoverOverlayNotified.Add(locationId);
                LocationDefinition location = LocationManager.Instance != null ? LocationManager.Instance.GetLocation(locationId) : null;
                string locationName = location != null ? location.displayName : locationId.ToString();
                MissionUI.Instance.ShowSystemNotification("地图高亮缺失", $"{locationName} 的悬浮高亮资源还没有加载成功，先继续使用基础地图浏览。", new Color(0.86f, 0.62f, 0.24f), 3f);
            }
        }

        RefreshMap();
    }

    private void ClearHoveredLocation()
    {
        hoveredLocationId = null;

        if (hoverOverlayImage != null)
        {
            hoverOverlayImage.sprite = null;
            hoverOverlayImage.enabled = false;
        }

        if (overlayRoot != null && overlayRoot.activeSelf)
        {
            RefreshMap();
        }
    }

    private void RefreshTitle(LocationId currentLoc)
    {
        if (titleText == null || LocationManager.Instance == null)
        {
            return;
        }

        LocationDefinition currentDef = LocationManager.Instance.GetLocation(currentLoc);
        string currentName = currentDef != null ? currentDef.displayName : currentLoc.ToString();

        if (hoveredLocationId.HasValue && hoveredLocationId.Value != currentLoc)
        {
            LocationDefinition hoveredDef = LocationManager.Instance.GetLocation(hoveredLocationId.Value);
            string hoveredName = hoveredDef != null ? hoveredDef.displayName : hoveredLocationId.Value.ToString();
            titleText.text = $"当前位置：{currentName}    查看：{hoveredName}";
            return;
        }

        titleText.text = $"当前位置：{currentName}";
    }

    private void LoadMapSprites()
    {
        if (baseMapSprite == null)
        {
            baseMapSprite = Resources.Load<Sprite>(BaseMapResourcePath);
            if (baseMapSprite == null)
            {
                Debug.LogWarning("[CampusMapUI] 未找到校园底图 Resources/" + BaseMapResourcePath);
                if (MissionUI.Instance != null)
                {
                    MissionUI.Instance.ShowSystemNotification("地图底图缺失", "校园地图底图资源没有加载成功，界面会继续使用简化底色显示。", new Color(0.86f, 0.62f, 0.24f), 3f);
                }
            }
        }

        foreach (KeyValuePair<LocationId, string> kvp in HoverOverlayPaths)
        {
            if (hoverSprites.ContainsKey(kvp.Key))
            {
                continue;
            }

            Sprite sprite = Resources.Load<Sprite>(kvp.Value);
            if (sprite != null)
            {
                hoverSprites[kvp.Key] = sprite;
            }
            else
            {
                Debug.LogWarning("[CampusMapUI] 未找到地点悬浮图 Resources/" + kvp.Value);
            }
        }
    }

    private Rect GetHotspotRect(LocationId locationId, Vector2 fallbackPosition)
    {
        if (HoverOverlayRects.TryGetValue(locationId, out Rect rect))
        {
            return ExpandRect(rect, 0.012f, 0.016f);
        }

        return CreateFallbackRect(fallbackPosition, 0.08f, 0.10f);
    }

    private static Rect CreateFallbackRect(Vector2 center, float width, float height)
    {
        float xMin = Mathf.Clamp01(center.x - width * 0.5f);
        float yMin = Mathf.Clamp01(center.y - height * 0.5f);
        float xMax = Mathf.Clamp01(center.x + width * 0.5f);
        float yMax = Mathf.Clamp01(center.y + height * 0.5f);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Rect ExpandRect(Rect rect, float xPadding, float yPadding)
    {
        return Rect.MinMaxRect(
            Mathf.Clamp01(rect.xMin - xPadding),
            Mathf.Clamp01(rect.yMin - yPadding),
            Mathf.Clamp01(rect.xMax + xPadding),
            Mathf.Clamp01(rect.yMax + yPadding));
    }

    private static Rect ToNormalizedRect(float minX, float minY, float maxX, float maxY)
    {
        float xMin = minX / SourceMapWidth;
        float xMax = maxX / SourceMapWidth;
        float yMin = 1f - (maxY / SourceMapHeight);
        float yMax = 1f - (minY / SourceMapHeight);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private void OnNodeClicked(LocationId locationId)
    {
        OnLocationNodeClicked?.Invoke(locationId);
    }

    private void StretchFill(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
