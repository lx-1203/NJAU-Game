using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 地点详情面板 —— 浮动面板，展示地点信息、可用行动、NPC 列表
/// 非 MonoBehaviour，由 HUDManager 管理生命周期
///
/// 功能：
/// - Show(LocationId) 打开面板，显示目标地点信息
/// - Hide() 关闭面板
/// - "前往"按钮执行导航、"取消"按钮关闭
/// - AP 不足时前往按钮灰显 + 点击抖动
/// </summary>
public class LocationDetailPanel
{
    // ========== 事件 ==========

    /// <summary>导航完成时触发，参数为目标地点</summary>
    public event Action<LocationId> OnNavigated;

    // ========== 颜色方案 ==========

    private static readonly Color PanelBgColor       = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color ButtonNormalColor   = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ButtonHoverColor    = new Color(0.30f, 0.45f, 0.70f, 1.0f);
    private static readonly Color ButtonPressedColor  = new Color(0.15f, 0.25f, 0.50f, 1.0f);
    private static readonly Color ButtonDisabledColor = new Color(0.30f, 0.30f, 0.35f, 0.8f);
    private static readonly Color CancelButtonColor   = new Color(0.45f, 0.25f, 0.25f, 1.0f);
    private static readonly Color TextWhite           = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold            = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray            = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color TextGreen           = new Color(0.40f, 0.85f, 0.50f);
    private static readonly Color DividerColor        = new Color(0.30f, 0.30f, 0.40f, 0.6f);

    // ========== 布局常量 ==========

    private const float PanelWidth  = 380f;
    private const float PanelHeight = 480f;

    // ========== 内部引用 ==========

    private GameObject panelRoot;
    private CanvasGroup canvasGroup;
    private GameObject contentContainer;

    // 动态内容引用（每次 Show 时重建）
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descText;
    private TextMeshProUGUI moveCostText;
    private Button goButton;
    private Button cancelButton;

    private LocationId currentTargetLocation;
    private bool isVisible = false;

    // ========== 对外接口 ==========

    /// <summary>
    /// 构建面板容器（初始隐藏），挂载在 centerPanel 上方
    /// </summary>
    public void Build(GameObject parentPanel)
    {
        if (parentPanel == null)
        {
            Debug.LogError("[LocationDetailPanel] parentPanel 为 null");
            return;
        }

        // 面板根节点
        panelRoot = new GameObject("LocationDetailPanel");
        panelRoot.transform.SetParent(parentPanel.transform, false);

        RectTransform rootRT = panelRoot.AddComponent<RectTransform>();
        // 居中定位
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rootRT.anchoredPosition = Vector2.zero;

        // 背景
        Image bgImage = panelRoot.AddComponent<Image>();
        bgImage.color = PanelBgColor;

        // CanvasGroup 用于淡入淡出动画
        canvasGroup = panelRoot.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 内容容器（带 Padding 的垂直布局）
        contentContainer = new GameObject("Content");
        contentContainer.transform.SetParent(panelRoot.transform, false);
        RectTransform contentRT = contentContainer.AddComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(20f, 20f);
        contentRT.offsetMax = new Vector2(-20f, -15f);

        VerticalLayoutGroup vlg = contentContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 初始隐藏
        panelRoot.SetActive(false);
    }

    /// <summary>
    /// 显示面板，展示指定地点的详细信息
    /// </summary>
    public void Show(LocationId targetLocation)
    {
        if (panelRoot == null || LocationManager.Instance == null) return;

        currentTargetLocation = targetLocation;

        // 清除旧内容
        ClearContent();

        // 获取地点数据
        LocationDefinition locDef = LocationManager.Instance.GetLocation(targetLocation);
        if (locDef == null)
        {
            Debug.LogWarning($"[LocationDetailPanel] 找不到地点定义: {targetLocation}");
            return;
        }

        bool isCurrentLocation = GameState.Instance != null &&
            GameState.Instance.CurrentLocation == targetLocation;

        // 构建内容
        BuildTitle(locDef);
        BuildDescription(locDef);
        BuildDivider();
        BuildActionList(targetLocation);
        BuildDivider();
        BuildNPCList(targetLocation);
        BuildDivider();
        BuildMoveCost(targetLocation, isCurrentLocation);
        BuildButtons(targetLocation, isCurrentLocation);

        // 显示面板
        panelRoot.SetActive(true);
        isVisible = true;

        // 淡入动画（简单版：直接设置 alpha）
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        if (panelRoot == null) return;

        isVisible = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        panelRoot.SetActive(false);
    }

    /// <summary>面板是否当前可见</summary>
    public bool IsVisible => isVisible;

    // ========== 内容构建方法 ==========

    /// <summary>标题：地点名称</summary>
    private void BuildTitle(LocationDefinition locDef)
    {
        titleText = CreateText("Title", $"{locDef.iconChar} {locDef.displayName}",
            24f, TextGold, TextAlignmentOptions.Center, 36f);
    }

    /// <summary>描述文本</summary>
    private void BuildDescription(LocationDefinition locDef)
    {
        descText = CreateText("Description", locDef.description,
            15f, TextGray, TextAlignmentOptions.Left, 50f);
    }

    /// <summary>分隔线</summary>
    private void BuildDivider()
    {
        GameObject divObj = new GameObject("Divider");
        divObj.transform.SetParent(contentContainer.transform, false);

        RectTransform rt = divObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 1f);

        LayoutElement le = divObj.AddComponent<LayoutElement>();
        le.preferredHeight = 1f;
        le.flexibleWidth = 1f;

        Image img = divObj.AddComponent<Image>();
        img.color = DividerColor;
    }

    /// <summary>可用行动列表</summary>
    private void BuildActionList(LocationId targetLocation)
    {
        CreateText("ActionHeader", "可用行动:", 16f, TextWhite, TextAlignmentOptions.Left, 24f);

        ActionDefinition[] actions = LocationManager.Instance.GetAvailableActions(targetLocation);

        if (actions.Length == 0)
        {
            CreateText("NoActions", "  暂无可用行动", 14f, TextGray, TextAlignmentOptions.Left, 20f);
        }
        else
        {
            foreach (var action in actions)
            {
                string costInfo = "";
                if (action.actionPointCost > 0)
                    costInfo += $" [{action.actionPointCost}AP]";
                if (action.moneyCost > 0)
                    costInfo += $" [¥{action.moneyCost}]";

                string line = $"  {action.displayName}{costInfo}";

                // 属性效果预览
                if (action.effects != null && action.effects.Length > 0)
                {
                    List<string> effectParts = new List<string>();
                    foreach (var eff in action.effects)
                    {
                        string sign = eff.amount >= 0 ? "+" : "";
                        effectParts.Add($"{eff.attributeName}{sign}{eff.amount}");
                    }
                    line += $"  ({string.Join(", ", effectParts)})";
                }

                CreateText($"Action_{action.id}", line, 13f, TextWhite, TextAlignmentOptions.Left, 18f);
            }
        }
    }

    /// <summary>NPC 列表</summary>
    private void BuildNPCList(LocationId targetLocation)
    {
        CreateText("NPCHeader", "当前NPC:", 16f, TextWhite, TextAlignmentOptions.Left, 24f);

        string[] npcs = LocationManager.Instance.GetNPCsAtLocation(targetLocation);

        if (npcs.Length == 0)
        {
            CreateText("NoNPCs", "  此处暂无 NPC", 14f, TextGray, TextAlignmentOptions.Left, 20f);
        }
        else
        {
            foreach (var npc in npcs)
            {
                CreateText($"NPC_{npc}", $"  {npc}", 14f, TextGreen, TextAlignmentOptions.Left, 20f);
            }
        }
    }

    /// <summary>移动提示（移动免费）</summary>
    private void BuildMoveCost(LocationId targetLocation, bool isCurrentLocation)
    {
        if (isCurrentLocation)
        {
            moveCostText = CreateText("MoveCost", "你已在此地点",
                15f, TextGold, TextAlignmentOptions.Center, 24f);
        }
        else
        {
            moveCostText = CreateText("MoveCost", "可自由前往",
                15f, TextGreen, TextAlignmentOptions.Center, 24f);
        }
    }

    /// <summary>底部按钮区域</summary>
    private void BuildButtons(LocationId targetLocation, bool isCurrentLocation)
    {
        // 按钮容器（水平布局）
        GameObject btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(contentContainer.transform, false);

        RectTransform containerRT = btnContainer.AddComponent<RectTransform>();
        containerRT.sizeDelta = new Vector2(0, 50f);

        LayoutElement containerLE = btnContainer.AddComponent<LayoutElement>();
        containerLE.preferredHeight = 50f;
        containerLE.flexibleWidth = 1f;

        HorizontalLayoutGroup hlg = btnContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        if (!isCurrentLocation)
        {
            // "前往"按钮（移动免费，始终可用）
            goButton = CreateButton(btnContainer.transform, "前往", 130f, 42f,
                ButtonNormalColor, () => OnGoClicked());
        }

        // "取消"按钮
        cancelButton = CreateButton(btnContainer.transform, "取消", 130f, 42f,
            CancelButtonColor, () => Hide());
    }

    // ========== 事件处理 ==========

    private void OnGoClicked()
    {
        if (LocationManager.Instance == null) return;

        if (!LocationManager.Instance.CanMoveTo(currentTargetLocation))
        {
            Debug.Log("[LocationDetailPanel] 行动点不足，无法移动");
            // 抖动效果需要 MonoBehaviour 协程，此处简单跳过
            return;
        }

        // 执行移动
        bool success = LocationManager.Instance.MoveTo(currentTargetLocation);
        if (success)
        {
            Hide();
            OnNavigated?.Invoke(currentTargetLocation);
        }
    }

    // ========== 工具方法 ==========

    /// <summary>清除内容容器中的所有子对象</summary>
    private void ClearContent()
    {
        if (contentContainer == null) return;
        for (int i = contentContainer.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(contentContainer.transform.GetChild(i).gameObject);
        }
    }

    /// <summary>创建文本对象并加入内容容器</summary>
    private TextMeshProUGUI CreateText(string name, string text, float fontSize,
        Color color, TextAlignmentOptions alignment, float height)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(contentContainer.transform, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1f;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;

        return tmp;
    }

    /// <summary>创建按钮</summary>
    private Button CreateButton(Transform parent, string label, float width, float height,
        Color bgColor, System.Action onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}");
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.2f;
        cb.pressedColor = bgColor * 0.8f;
        cb.selectedColor = bgColor;
        cb.disabledColor = ButtonDisabledColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // 按钮文字
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18f;
        tmp.color = TextWhite;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        // 点击事件
        btn.onClick.AddListener(() => onClick?.Invoke());

        return btn;
    }
}
