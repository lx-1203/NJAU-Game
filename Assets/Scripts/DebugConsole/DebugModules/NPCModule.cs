#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color PanelBg = new Color(0.18f, 0.18f, 0.22f, 0.6f);
    private static readonly Color ButtonBlue = new Color(0.20f, 0.35f, 0.60f, 1.0f);
    private static readonly Color ButtonGreen = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color ButtonRed = new Color(0.60f, 0.20f, 0.20f, 1.0f);
    private static readonly Color ButtonPurple = new Color(0.48f, 0.28f, 0.62f, 1.0f);
    private static readonly Color ButtonPink = new Color(0.72f, 0.32f, 0.48f, 1.0f);
    private static readonly Sprite SliderHandleSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

    private readonly List<NPCUIEntry> npcEntries = new List<NPCUIEntry>();
    private Transform contentRoot;

    private class NPCUIEntry
    {
        public string npcId;
        public Slider affinitySlider;
        public Slider healthSlider;
        public TMP_InputField cooldownInput;
        public TextMeshProUGUI affinityValueText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI romanceText;
        public TextMeshProUGUI healthValueText;
        public TextMeshProUGUI metaText;
        public TextMeshProUGUI memoryText;
        public bool suppressCallbacks;
    }

    public void Init(RectTransform parent)
    {
        GameObject scrollObj = CreateUIElement("NPCScroll", parent);
        StretchFull(scrollObj.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        scrollRect.content = contentRT;
        contentRoot = content.transform;

        CreateLabel(contentRoot, "NPC 调试", 18f, TextGold, 30f);
        BuildNPCBlocks();
    }

    private void BuildNPCBlocks()
    {
        npcEntries.Clear();

        if (NPCDatabase.Instance == null)
        {
            CreateLabel(contentRoot, "NPCDatabase 尚未就绪", 14f, TextGray, 30f);
            return;
        }

        NPCData[] allNPCs = NPCDatabase.Instance.GetAllNPCs();
        if (allNPCs == null || allNPCs.Length == 0)
        {
            CreateLabel(contentRoot, "没有 NPC 数据", 14f, TextGray, 30f);
            return;
        }

        for (int i = 0; i < allNPCs.Length; i++)
            BuildNPCBlock(allNPCs[i]);
    }

    private void BuildNPCBlock(NPCData npc)
    {
        NPCUIEntry entry = new NPCUIEntry { npcId = npc.id };

        GameObject block = CreatePanel($"NPC_{npc.id}", contentRoot, PanelBg);
        LayoutElement blockLayout = block.AddComponent<LayoutElement>();
        blockLayout.preferredHeight = 268f;

        VerticalLayoutGroup vlg = block.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateLabel(block.transform, $"{npc.displayName} [{npc.id}]", 15f, TextGold, 24f);
        entry.metaText = CreateLabel(block.transform, string.Empty, 12f, TextGray, 34f);
        entry.metaText.enableWordWrapping = true;
        entry.metaText.overflowMode = TextOverflowModes.Ellipsis;

        GameObject affinityHeaderRow = CreateRow(block.transform, 22f, 8f);
        entry.affinityValueText = CreateLabel(affinityHeaderRow.transform, "好感度 0", 13f, TextWhite, 22f, 96f);
        entry.levelText = CreateLabel(affinityHeaderRow.transform, "关系等级：-", 13f, TextWhite, 22f);
        SetFlexibleWidth(entry.levelText.gameObject, 120f);
        entry.affinitySlider = CreateSliderRow(block.transform, "好感", 0, 100, value =>
        {
            if (entry.suppressCallbacks || AffinitySystem.Instance == null)
                return;

            AffinitySystem.Instance.DebugSetAffinity(entry.npcId, Mathf.RoundToInt(value));
            RefreshEntry(entry);
            DebugConsoleManager.Log("NPC", $"{entry.npcId} affinity -> {Mathf.RoundToInt(value)}");
        });

        GameObject romanceHeaderRow = CreateRow(block.transform, 22f, 8f);
        entry.healthValueText = CreateLabel(romanceHeaderRow.transform, "健康度 70", 13f, TextWhite, 22f, 96f);
        entry.romanceText = CreateLabel(romanceHeaderRow.transform, "恋爱状态：-", 13f, TextWhite, 22f);
        SetFlexibleWidth(entry.romanceText.gameObject, 120f);
        entry.healthSlider = CreateSliderRow(block.transform, "恋爱", 0, 100, value =>
        {
            if (entry.suppressCallbacks || RomanceSystem.Instance == null)
                return;

            RomanceState state = RomanceSystem.Instance.GetRomanceState(entry.npcId);
            int cooldown = ParseIntOrDefault(entry.cooldownInput != null ? entry.cooldownInput.text : string.Empty, 0);
            RomanceSystem.Instance.DebugSetRomanceState(entry.npcId, state, Mathf.RoundToInt(value), cooldown);
            RefreshEntry(entry);
            DebugConsoleManager.Log("NPC", $"{entry.npcId} romance health -> {Mathf.RoundToInt(value)}");
        });

        GameObject cooldownRow = CreateRow(block.transform, 28f, 6f);
        CreateLabel(cooldownRow.transform, "冷却", 13f, TextWhite, 28f, 44f);
        entry.cooldownInput = CreateInputField(cooldownRow.transform, "0", 80f, 28f);
        entry.cooldownInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        CreateButton(cooldownRow.transform, "应用", 72f, ButtonBlue, () =>
        {
            if (RomanceSystem.Instance == null)
                return;

            RomanceState state = RomanceSystem.Instance.GetRomanceState(entry.npcId);
            int health = RomanceSystem.Instance.GetRomanceHealth(entry.npcId);
            RomanceSystem.Instance.DebugSetRomanceState(entry.npcId, state, health, ParseIntOrDefault(entry.cooldownInput.text, 0));
            RefreshEntry(entry);
            DebugConsoleManager.Log("NPC", $"{entry.npcId} cooldown updated");
        });

        CreateLabel(block.transform, "好感快捷", 12f, TextGray, 18f);
        Transform affinityButtons = CreateButtonGrid(block.transform, 5, new Vector2(0f, 32f), new Vector2(6f, 6f));
        CreateButton(affinityButtons.transform, "0", 50f, ButtonRed, () => SetAffinity(entry, 0));
        CreateButton(affinityButtons.transform, "40", 50f, ButtonBlue, () => SetAffinity(entry, 40));
        CreateButton(affinityButtons.transform, "60", 50f, ButtonBlue, () => SetAffinity(entry, 60));
        CreateButton(affinityButtons.transform, "80", 50f, ButtonGreen, () => SetAffinity(entry, 80));
        CreateButton(affinityButtons.transform, "100", 56f, ButtonGreen, () => SetAffinity(entry, 100));

        CreateLabel(block.transform, "恋爱状态", 12f, TextGray, 18f);
        Transform romanceButtons = CreateButtonGrid(block.transform, 3, new Vector2(0f, 68f), new Vector2(6f, 6f));
        CreateButton(romanceButtons.transform, "无", 56f, ButtonBlue, () => SetRomanceState(entry, RomanceState.None));
        CreateButton(romanceButtons.transform, "心动", 56f, ButtonPurple, () => SetRomanceState(entry, RomanceState.Crushing));
        CreateButton(romanceButtons.transform, "冷却", 56f, ButtonBlue, () => SetRomanceState(entry, RomanceState.Cooldown));
        CreateButton(romanceButtons.transform, "交往", 56f, ButtonPink, () => SetRomanceState(entry, RomanceState.Dating));
        CreateButton(romanceButtons.transform, "分手", 56f, ButtonRed, () => SetRomanceState(entry, RomanceState.BrokenUp));
        CreateButton(romanceButtons.transform, "敌对", 56f, ButtonRed, () => SetRomanceState(entry, RomanceState.Hostile));

        entry.memoryText = CreateLabel(block.transform, string.Empty, 12f, TextGray, 44f);
        entry.memoryText.enableWordWrapping = true;
        entry.memoryText.overflowMode = TextOverflowModes.Ellipsis;

        npcEntries.Add(entry);
        RefreshEntry(entry);
    }

    public void Refresh()
    {
        for (int i = 0; i < npcEntries.Count; i++)
            RefreshEntry(npcEntries[i]);
    }

    private void RefreshEntry(NPCUIEntry entry)
    {
        if (AffinitySystem.Instance == null || RomanceSystem.Instance == null)
            return;

        NPCRelationshipData rel = AffinitySystem.Instance.GetRelationship(entry.npcId);
        RomanceRecord record = RomanceSystem.Instance.DebugGetRecord(entry.npcId);
        NPCData npc = NPCDatabase.Instance != null ? NPCDatabase.Instance.GetNPC(entry.npcId) : null;

        entry.suppressCallbacks = true;
        entry.affinitySlider.SetValueWithoutNotify(rel.affinity);
        entry.healthSlider.SetValueWithoutNotify(record.healthScore);
        if (entry.cooldownInput != null)
            entry.cooldownInput.SetTextWithoutNotify(record.cooldownRoundsLeft.ToString());
        entry.suppressCallbacks = false;

        entry.affinityValueText.text = $"好感度 {rel.affinity}";
        entry.levelText.text = $"关系等级：{GetAffinityLevelText(rel.level)}";
        entry.levelText.color = GetAffinityLevelColor(rel.level);
        entry.romanceText.text = $"恋爱状态：{GetRomanceStateText(record.state)}";
        entry.romanceText.color = GetRomanceStateColor(record.state);
        entry.healthValueText.text = $"健康度 {record.healthScore}";

        string personality = npc != null ? npc.GetPersonality().ToString() : "-";
        string lastAction = string.IsNullOrEmpty(rel.lastInteractionActionId) ? "-" : rel.lastInteractionActionId;
        entry.metaText.text =
            $"性格 {personality}   未互动 {rel.consecutiveNoInteractionTurns}   重复 {rel.repeatedActionCount}\n" +
            $"上次行动 {lastAction}   冷却 {record.cooldownRoundsLeft}   交往回合 {record.durationRounds}";

        entry.memoryText.text = BuildMemorySummary(rel.memories);
    }

    private void SetAffinity(NPCUIEntry entry, int value)
    {
        if (AffinitySystem.Instance == null)
            return;

        AffinitySystem.Instance.DebugSetAffinity(entry.npcId, value);
        RefreshEntry(entry);
        DebugConsoleManager.Log("NPC", $"{entry.npcId} affinity preset -> {value}");
    }

    private void SetRomanceState(NPCUIEntry entry, RomanceState state)
    {
        if (RomanceSystem.Instance == null)
            return;

        int health = state == RomanceState.Dating ? 70 : RomanceSystem.Instance.GetRomanceHealth(entry.npcId);
        int cooldown = (state == RomanceState.Cooldown || state == RomanceState.BrokenUp) ? 4 : 0;
        RomanceSystem.Instance.DebugSetRomanceState(entry.npcId, state, health, cooldown);
        RefreshEntry(entry);
        DebugConsoleManager.Log("NPC", $"{entry.npcId} romance state -> {state}");
    }

    private string BuildMemorySummary(List<string> memories)
    {
        if (memories == null || memories.Count == 0)
            return "Recent: none";

        int start = Mathf.Max(0, memories.Count - 3);
        StringBuilder builder = new StringBuilder("Recent:");
        for (int i = start; i < memories.Count; i++)
            builder.Append('\n').Append("- ").Append(memories[i]);

        return builder.ToString();
    }

    private string GetAffinityLevelText(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Stranger: return "Stranger";
            case AffinityLevel.Acquaintance: return "Acquaintance";
            case AffinityLevel.Friend: return "Friend";
            case AffinityLevel.CloseFriend: return "CloseFriend";
            case AffinityLevel.BestFriend: return "BestFriend";
            case AffinityLevel.Lover: return "Lover";
            default: return level.ToString();
        }
    }

    private Color GetAffinityLevelColor(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Stranger: return TextGray;
            case AffinityLevel.Acquaintance: return TextWhite;
            case AffinityLevel.Friend: return new Color(0.5f, 0.85f, 0.5f);
            case AffinityLevel.CloseFriend: return new Color(0.3f, 0.7f, 1.0f);
            case AffinityLevel.BestFriend: return TextGold;
            case AffinityLevel.Lover: return new Color(1.0f, 0.5f, 0.7f);
            default: return TextWhite;
        }
    }

    private string GetRomanceStateText(RomanceState state)
    {
        switch (state)
        {
            case RomanceState.None: return "None";
            case RomanceState.Crushing: return "Crushing";
            case RomanceState.Cooldown: return "Cooldown";
            case RomanceState.Dating: return "Dating";
            case RomanceState.BrokenUp: return "BrokenUp";
            case RomanceState.Hostile: return "Hostile";
            default: return state.ToString();
        }
    }

    private Color GetRomanceStateColor(RomanceState state)
    {
        switch (state)
        {
            case RomanceState.None: return TextGray;
            case RomanceState.Crushing: return new Color(1.0f, 0.7f, 0.8f);
            case RomanceState.Cooldown: return new Color(0.6f, 0.6f, 0.8f);
            case RomanceState.Dating: return new Color(1.0f, 0.4f, 0.6f);
            case RomanceState.BrokenUp: return new Color(0.7f, 0.4f, 0.4f);
            case RomanceState.Hostile: return new Color(0.9f, 0.2f, 0.2f);
            default: return TextWhite;
        }
    }

    private Slider CreateSliderRow(Transform parent, string label, int min, int max, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = CreateRow(parent, 28f, 6f);
        CreateLabel(row.transform, label, 13f, TextWhite, 28f, 44f);

        GameObject sliderObj = CreateUIElement($"{label}Slider", row.transform);
        LayoutElement sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.preferredHeight = 18f;
        sliderLayout.flexibleWidth = 1f;

        Image bg = sliderObj.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.22f, 0.85f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = CreateUIElement("FillArea", sliderObj.transform);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRT.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRT.sizeDelta = new Vector2(-16f, 8f);
        fillAreaRT.anchoredPosition = Vector2.zero;

        GameObject fill = CreateUIElement("Fill", fillArea.transform);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.25f, 0.50f, 0.80f, 1.0f);

        GameObject handleArea = CreateUIElement("HandleArea", sliderObj.transform);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(8f, 0f);
        handleAreaRT.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = CreateUIElement("Handle", handleArea.transform);
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0.5f);
        handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(12f, 12f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        handleImage.sprite = SliderHandleSprite;
        handleImage.type = Image.Type.Sliced;

        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    private GameObject CreateRow(Transform parent, float height, float spacing = 6f)
    {
        GameObject row = CreateUIElement("Row", parent);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        return row;
    }

    private Transform CreateButtonGrid(Transform parent, int columns, Vector2 sizeDelta, Vector2 spacing)
    {
        GameObject grid = CreateUIElement("ButtonGrid", parent);
        RectTransform rt = grid.GetComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;

        LayoutElement layout = grid.AddComponent<LayoutElement>();
        if (sizeDelta.y > 0f)
            layout.preferredHeight = sizeDelta.y;

        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(56f, 30f);
        gridLayout.spacing = spacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        return grid.transform;
    }

    private void SetFlexibleWidth(GameObject target, float minWidth)
    {
        if (target == null)
            return;

        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout == null)
            layout = target.AddComponent<LayoutElement>();

        layout.flexibleWidth = 1f;
        layout.minWidth = minWidth;
    }

    private void CreateButton(Transform parent, string label, float width, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement($"Btn_{label}", parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 30f);

        LayoutElement layout = btnObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button button = btnObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(btnObj.transform, label, 12f, TextWhite, 30f);
        text.alignment = TextAlignmentOptions.Center;
        StretchFull(text.GetComponent<RectTransform>());
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height)
    {
        GameObject inputObj = CreateUIElement("InputField", parent);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement layout = inputObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;

        Image bg = inputObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.18f, 0.90f);

        GameObject textArea = CreateUIElement("TextArea", inputObj.transform);
        RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(8, 2);
        textAreaRT.offsetMax = new Vector2(-8, -2);
        textArea.AddComponent<RectMask2D>();

        GameObject textObj = CreateUIElement("Text", textArea.transform);
        StretchFull(textObj.GetComponent<RectTransform>());
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 13f;
        text.color = TextWhite;
        text.alignment = TextAlignmentOptions.Left;
        ApplyChineseFont(text);

        GameObject placeholderObj = CreateUIElement("Placeholder", textArea.transform);
        StretchFull(placeholderObj.GetComponent<RectTransform>());
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 13f;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        ApplyChineseFont(placeholderText);

        TMP_InputField input = inputObj.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRT;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.fontAsset = FontManager.Instance != null ? FontManager.Instance.ChineseFont : null;
        return input;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height, float width = 0f)
    {
        GameObject obj = CreateUIElement("Label", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        if (width > 0f)
        {
            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
        }

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.margin = new Vector4(2f, 4f, 2f, 4f);
        tmp.extraPadding = true;
        ApplyChineseFont(tmp);
        return tmp;
    }

    private GameObject CreatePanel(string name, Transform parent, Color bgColor)
    {
        GameObject panel = CreateUIElement(name, parent);
        Image bg = panel.AddComponent<Image>();
        bg.color = bgColor;
        return panel;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        return go;
    }

    private void ApplyChineseFont(TextMeshProUGUI text)
    {
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;
    }

    private int ParseIntOrDefault(string raw, int fallback)
    {
        return int.TryParse(raw, out int value) ? value : fallback;
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
