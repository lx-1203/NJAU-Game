#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// NPC 调试模块 —— NPC 关系管理器
/// 显示所有 NPC 好感度、等级、恋爱状态，支持滑块实时调节好感度
/// </summary>
public class NPCModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGray  = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color PanelBg   = new Color(0.18f, 0.18f, 0.22f, 0.6f);
    private static readonly Color SliderBg  = new Color(0.25f, 0.25f, 0.30f);
    private static readonly Color SliderFill = new Color(0.4f, 0.75f, 0.95f);

    // 每个 NPC 对应的 UI 控件缓存
    private class NPCUIEntry
    {
        public string npcId;
        public Slider affinitySlider;
        public TextMeshProUGUI affinityValueLabel;
        public TextMeshProUGUI levelLabel;
        public TextMeshProUGUI romanceLabel;
        public bool isSuppressingCallback; // 防止 Refresh 时触发 onValueChanged
    }

    private List<NPCUIEntry> npcEntries = new List<NPCUIEntry>();
    private Transform contentRoot;

    public void Init(RectTransform parent)
    {
        // ========== ScrollRect 容器 ==========
        GameObject scrollObj = CreateUIElement("NPCScroll", parent);
        RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
        StretchFull(scrollRT);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // Viewport (带 Mask)
        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        StretchFull(vpRT);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = vpRT;

        // Content (纵向布局)
        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        scrollRect.content = contentRT;
        contentRoot = content.transform;

        // ========== 标题 ==========
        CreateLabel(contentRoot, "— NPC 管理 —", 18f, TextGold, 30f);

        // ========== 构建 NPC 区块 ==========
        BuildNPCBlocks();
    }

    /// <summary>
    /// 构建所有 NPC 的 UI 区块
    /// </summary>
    private void BuildNPCBlocks()
    {
        npcEntries.Clear();

        if (NPCDatabase.Instance == null)
        {
            CreateLabel(contentRoot, "NPC数据库未初始化", 14f, TextGray, 30f);
            return;
        }

        NPCData[] allNPCs = NPCDatabase.Instance.GetAllNPCs();
        if (allNPCs == null || allNPCs.Length == 0)
        {
            CreateLabel(contentRoot, "未找到任何NPC数据", 14f, TextGray, 30f);
            return;
        }

        for (int i = 0; i < allNPCs.Length; i++)
        {
            BuildSingleNPCBlock(allNPCs[i]);
        }
    }

    /// <summary>
    /// 构建单个 NPC 的调试 UI 区块
    /// </summary>
    private void BuildSingleNPCBlock(NPCData npc)
    {
        NPCUIEntry entry = new NPCUIEntry();
        entry.npcId = npc.id;

        // NPC 区块容器 (带半透明背景)
        GameObject block = CreateUIElement($"NPC_{npc.id}", contentRoot);
        RectTransform blockRT = block.GetComponent<RectTransform>();
        blockRT.sizeDelta = new Vector2(0, 140f);

        Image blockBg = block.AddComponent<Image>();
        blockBg.color = PanelBg;

        VerticalLayoutGroup blockVLG = block.AddComponent<VerticalLayoutGroup>();
        blockVLG.spacing = 4f;
        blockVLG.padding = new RectOffset(12, 12, 8, 8);
        blockVLG.childAlignment = TextAnchor.UpperLeft;
        blockVLG.childControlWidth = true;
        blockVLG.childControlHeight = false;
        blockVLG.childForceExpandWidth = true;
        blockVLG.childForceExpandHeight = false;

        // --- 行1: NPC 名称 ---
        CreateLabel(block.transform, npc.displayName, 16f, TextGold, 24f);

        // --- 行2: 好感度 数值 + Slider ---
        GameObject affinityRow = CreateUIElement("AffinityRow", block.transform);
        RectTransform affinityRowRT = affinityRow.GetComponent<RectTransform>();
        affinityRowRT.sizeDelta = new Vector2(0, 30f);

        HorizontalLayoutGroup affinityHLG = affinityRow.AddComponent<HorizontalLayoutGroup>();
        affinityHLG.spacing = 8f;
        affinityHLG.childAlignment = TextAnchor.MiddleLeft;
        affinityHLG.childControlWidth = false;
        affinityHLG.childControlHeight = true;
        affinityHLG.childForceExpandWidth = false;
        affinityHLG.childForceExpandHeight = true;

        // "好感度:" 标签
        TextMeshProUGUI affinityLabel = CreateLabel(affinityRow.transform, "好感度:", 13f, TextWhite, 26f);
        LayoutElement labelLE = affinityLabel.gameObject.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 60f;

        // 数值 Label
        entry.affinityValueLabel = CreateLabel(affinityRow.transform, "0", 13f, TextWhite, 26f);
        LayoutElement valueLE = entry.affinityValueLabel.gameObject.AddComponent<LayoutElement>();
        valueLE.preferredWidth = 30f;
        entry.affinityValueLabel.alignment = TextAlignmentOptions.Center;

        // Slider
        Slider slider = CreateAffinitySlider(affinityRow.transform, npc.id, entry);
        entry.affinitySlider = slider;
        LayoutElement sliderLE = slider.gameObject.AddComponent<LayoutElement>();
        sliderLE.flexibleWidth = 1f;
        sliderLE.preferredHeight = 20f;

        // --- 行3: 好感等级 ---
        GameObject levelRow = CreateUIElement("LevelRow", block.transform);
        RectTransform levelRowRT = levelRow.GetComponent<RectTransform>();
        levelRowRT.sizeDelta = new Vector2(0, 22f);

        HorizontalLayoutGroup levelHLG = levelRow.AddComponent<HorizontalLayoutGroup>();
        levelHLG.spacing = 8f;
        levelHLG.childAlignment = TextAnchor.MiddleLeft;
        levelHLG.childControlWidth = false;
        levelHLG.childControlHeight = true;
        levelHLG.childForceExpandWidth = false;
        levelHLG.childForceExpandHeight = true;

        TextMeshProUGUI levelTitle = CreateLabel(levelRow.transform, "等级:", 13f, TextWhite, 22f);
        LayoutElement levelTitleLE = levelTitle.gameObject.AddComponent<LayoutElement>();
        levelTitleLE.preferredWidth = 60f;

        entry.levelLabel = CreateLabel(levelRow.transform, "Stranger", 13f, TextGold, 22f);
        LayoutElement levelValueLE = entry.levelLabel.gameObject.AddComponent<LayoutElement>();
        levelValueLE.preferredWidth = 200f;

        // --- 行4: 恋爱状态 ---
        GameObject romanceRow = CreateUIElement("RomanceRow", block.transform);
        RectTransform romanceRowRT = romanceRow.GetComponent<RectTransform>();
        romanceRowRT.sizeDelta = new Vector2(0, 22f);

        HorizontalLayoutGroup romanceHLG = romanceRow.AddComponent<HorizontalLayoutGroup>();
        romanceHLG.spacing = 8f;
        romanceHLG.childAlignment = TextAnchor.MiddleLeft;
        romanceHLG.childControlWidth = false;
        romanceHLG.childControlHeight = true;
        romanceHLG.childForceExpandWidth = false;
        romanceHLG.childForceExpandHeight = true;

        TextMeshProUGUI romanceTitle = CreateLabel(romanceRow.transform, "恋爱:", 13f, TextWhite, 22f);
        LayoutElement romanceTitleLE = romanceTitle.gameObject.AddComponent<LayoutElement>();
        romanceTitleLE.preferredWidth = 60f;

        entry.romanceLabel = CreateLabel(romanceRow.transform, "None", 13f, TextGray, 22f);
        LayoutElement romanceValueLE = entry.romanceLabel.gameObject.AddComponent<LayoutElement>();
        romanceValueLE.preferredWidth = 200f;

        npcEntries.Add(entry);
    }

    /// <summary>
    /// 创建好感度 Slider (0~100)
    /// </summary>
    private Slider CreateAffinitySlider(Transform parent, string npcId, NPCUIEntry entry)
    {
        GameObject sliderObj = CreateUIElement($"Slider_{npcId}", parent);
        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(0, 20f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.wholeNumbers = true;

        // Background
        GameObject bg = CreateUIElement("Background", sliderObj.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = SliderBg;
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        StretchFull(bgRT);

        // Fill Area
        GameObject fillArea = CreateUIElement("FillArea", sliderObj.transform);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        StretchFull(fillAreaRT);
        fillAreaRT.offsetMin = new Vector2(0, 0);
        fillAreaRT.offsetMax = new Vector2(0, 0);

        // Fill
        GameObject fill = CreateUIElement("Fill", fillArea.transform);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = SliderFill;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        slider.fillRect = fillRT;

        // Handle (小方块)
        GameObject handleArea = CreateUIElement("HandleSlideArea", sliderObj.transform);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        StretchFull(handleAreaRT);

        GameObject handle = CreateUIElement("Handle", handleArea.transform);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(10f, 20f);

        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        // Slider onValueChanged 直接修改 AffinitySystem
        string capturedNpcId = npcId;
        NPCUIEntry capturedEntry = entry;
        slider.onValueChanged.AddListener((float val) =>
        {
            if (capturedEntry.isSuppressingCallback) return;

            if (AffinitySystem.Instance != null)
            {
                NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(capturedNpcId);
                rel.affinity = Mathf.RoundToInt(val);
            }

            // 刷新数值显示
            if (capturedEntry.affinityValueLabel != null)
                capturedEntry.affinityValueLabel.text = Mathf.RoundToInt(val).ToString();

            // 刷新等级和恋爱状态
            RefreshEntry(capturedEntry);
        });

        return slider;
    }

    public void Refresh()
    {
        if (AffinitySystem.Instance == null) return;

        for (int i = 0; i < npcEntries.Count; i++)
        {
            NPCUIEntry entry = npcEntries[i];
            NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(entry.npcId);

            // 更新 Slider（抑制回调）
            if (entry.affinitySlider != null)
            {
                entry.isSuppressingCallback = true;
                entry.affinitySlider.value = rel.affinity;
                entry.isSuppressingCallback = false;
            }

            // 更新数值 Label
            if (entry.affinityValueLabel != null)
                entry.affinityValueLabel.text = rel.affinity.ToString();

            // 更新等级和恋爱状态
            RefreshEntry(entry);
        }
    }

    /// <summary>
    /// 刷新单个 NPC 条目的等级和恋爱状态显示
    /// </summary>
    private void RefreshEntry(NPCUIEntry entry)
    {
        if (AffinitySystem.Instance == null) return;

        NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(entry.npcId);

        // 等级显示
        if (entry.levelLabel != null)
        {
            entry.levelLabel.text = GetAffinityLevelChinese(rel.level);
            entry.levelLabel.color = GetAffinityLevelColor(rel.level);
        }

        // 恋爱状态显示
        if (entry.romanceLabel != null)
        {
            RomanceState romanceState = RomanceState.None;
            if (RomanceSystem.Instance != null)
                romanceState = RomanceSystem.Instance.GetRomanceState(entry.npcId);

            entry.romanceLabel.text = GetRomanceStateChinese(romanceState);
            entry.romanceLabel.color = GetRomanceStateColor(romanceState);
        }
    }

    // ========== 显示辅助 ==========

    private string GetAffinityLevelChinese(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Stranger:     return "陌生人";
            case AffinityLevel.Acquaintance: return "认识";
            case AffinityLevel.Friend:       return "朋友";
            case AffinityLevel.CloseFriend:  return "密友";
            case AffinityLevel.BestFriend:   return "挚友";
            case AffinityLevel.Lover:        return "恋人";
            default: return level.ToString();
        }
    }

    private Color GetAffinityLevelColor(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Stranger:     return TextGray;
            case AffinityLevel.Acquaintance: return TextWhite;
            case AffinityLevel.Friend:       return new Color(0.5f, 0.85f, 0.5f);
            case AffinityLevel.CloseFriend:  return new Color(0.3f, 0.7f, 1.0f);
            case AffinityLevel.BestFriend:   return TextGold;
            case AffinityLevel.Lover:        return new Color(1.0f, 0.5f, 0.7f);
            default: return TextWhite;
        }
    }

    private string GetRomanceStateChinese(RomanceState state)
    {
        switch (state)
        {
            case RomanceState.None:      return "无";
            case RomanceState.Crushing:  return "暗恋中";
            case RomanceState.Cooldown:  return "冷却中";
            case RomanceState.Dating:    return "恋爱中";
            case RomanceState.BrokenUp:  return "已分手";
            case RomanceState.Hostile:   return "敌对";
            default: return state.ToString();
        }
    }

    private Color GetRomanceStateColor(RomanceState state)
    {
        switch (state)
        {
            case RomanceState.None:      return TextGray;
            case RomanceState.Crushing:  return new Color(1.0f, 0.7f, 0.8f);
            case RomanceState.Cooldown:  return new Color(0.6f, 0.6f, 0.8f);
            case RomanceState.Dating:    return new Color(1.0f, 0.4f, 0.6f);
            case RomanceState.BrokenUp:  return new Color(0.7f, 0.4f, 0.4f);
            case RomanceState.Hostile:   return new Color(0.9f, 0.2f, 0.2f);
            default: return TextWhite;
        }
    }

    // ========== 工具方法 ==========

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height)
    {
        GameObject obj = CreateUIElement("Label", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            tmp.font = FontManager.Instance.ChineseFont;

        return tmp;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        return go;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
