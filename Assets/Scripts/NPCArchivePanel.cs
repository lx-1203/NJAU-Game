using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// In-game NPC archive panel. Shows public NPC profile data plus runtime relationship state.
/// </summary>
public class NPCArchivePanel : MonoBehaviour
{
    public static NPCArchivePanel Instance { get; private set; }

    private Canvas canvas;
    private GameObject root;
    private RectTransform npcListContent;
    private RectTransform detailContent;
    private readonly List<Button> npcButtons = new List<Button>();

    private string selectedNpcId;

    private static readonly Color OverlayColor = new Color(0.04f, 0.05f, 0.06f, 0.62f);
    private static readonly Color PanelColor = new Color(0.98f, 0.95f, 0.88f, 0.98f);
    private static readonly Color SubtlePanelColor = new Color(0.92f, 0.87f, 0.76f, 0.95f);
    private static readonly Color DarkText = new Color(0.18f, 0.13f, 0.09f, 1f);
    private static readonly Color MutedText = new Color(0.42f, 0.35f, 0.28f, 1f);
    private static readonly Color AccentColor = new Color(0.74f, 0.48f, 0.24f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged += OnAffinityChanged;
        }
    }

    private void OnDisable()
    {
        if (AffinitySystem.Instance != null)
        {
            AffinitySystem.Instance.OnAffinityChanged -= OnAffinityChanged;
        }
    }

    public void Initialize()
    {
        if (root != null)
        {
            return;
        }

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        BuildRoot();
        root.SetActive(false);
    }

    public void Open()
    {
        Initialize();
        root.SetActive(true);
        RefreshList();

        NPCData[] npcs = NPCDatabase.Instance != null ? NPCDatabase.Instance.GetAllNPCs() : new NPCData[0];
        if (string.IsNullOrEmpty(selectedNpcId) && npcs.Length > 0)
        {
            selectedNpcId = npcs[0].id;
        }

        RefreshDetail();
    }

    public void Close()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void Toggle()
    {
        Initialize();
        if (root.activeSelf)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public bool IsOpen()
    {
        return root != null && root.activeSelf;
    }

    private void BuildRoot()
    {
        root = new GameObject("NPCArchiveRoot");
        root.transform.SetParent(transform, false);
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image overlay = root.AddComponent<Image>();
        overlay.color = OverlayColor;

        GameObject panel = CreatePanel("NPCArchivePanel", root.transform, PanelColor);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1320f, 760f);

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(28, 28, 24, 28);
        panelLayout.spacing = 18f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        RectTransform headerRT = header.AddComponent<RectTransform>();
        headerRT.sizeDelta = new Vector2(0f, 58f);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;

        TextMeshProUGUI title = CreateText("Title", header.transform, "NPC档案", 34f, FontStyles.Bold, DarkText, TextAlignmentOptions.Left);
        title.rectTransform.sizeDelta = new Vector2(260f, 58f);

        TextMeshProUGUI hint = CreateText("Hint", header.transform, "查看人物介绍、好感度、日程与背景资料", 18f, FontStyles.Normal, MutedText, TextAlignmentOptions.Left);
        hint.rectTransform.sizeDelta = new Vector2(810f, 58f);

        Button closeButton = CreateTextButton("CloseButton", header.transform, "关闭", new Vector2(110f, 46f), AccentColor, Color.white);
        closeButton.onClick.AddListener(Close);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(panel.transform, false);
        RectTransform bodyRT = body.AddComponent<RectTransform>();
        bodyRT.sizeDelta = new Vector2(0f, 620f);
        HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 18f;
        bodyLayout.childControlWidth = false;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;

        npcListContent = CreateScrollArea("NPCList", body.transform, new Vector2(330f, 620f), out _);
        detailContent = CreateScrollArea("NPCDetail", body.transform, new Vector2(900f, 620f), out _);
    }

    private void RefreshList()
    {
        ClearChildren(npcListContent);
        npcButtons.Clear();

        if (NPCDatabase.Instance == null)
        {
            CreateSectionText(npcListContent, "NPC数据库尚未初始化。");
            return;
        }

        NPCData[] npcs = NPCDatabase.Instance.GetAllNPCs();
        for (int i = 0; i < npcs.Length; i++)
        {
            NPCData npc = npcs[i];
            if (npc == null || string.IsNullOrEmpty(npc.id))
            {
                continue;
            }

            Button button = CreateNPCButton(npc);
            npcButtons.Add(button);
        }
    }

    private Button CreateNPCButton(NPCData npc)
    {
        GameObject obj = CreatePanel("NPCButton_" + npc.id, npcListContent, npc.id == selectedNpcId ? new Color(0.84f, 0.72f, 0.55f, 1f) : SubtlePanelColor);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(290f, 88f);

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = npc.id == selectedNpcId ? new Color(0.84f, 0.72f, 0.55f, 1f) : SubtlePanelColor;
        colors.highlightedColor = new Color(0.90f, 0.78f, 0.58f, 1f);
        colors.pressedColor = new Color(0.72f, 0.58f, 0.38f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 8);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        NPCRelationshipData rel = AffinitySystem.Instance != null ? AffinitySystem.Instance.GetRelationship(npc.id) : null;
        string level = rel != null ? GetAffinityLevelName(rel.level) : "陌生";
        int affinity = rel != null ? rel.affinity : 0;

        TextMeshProUGUI name = CreateText("Name", obj.transform, SafeText(npc.displayName, npc.id), 22f, FontStyles.Bold, DarkText, TextAlignmentOptions.Left);
        name.rectTransform.sizeDelta = new Vector2(250f, 28f);

        string subtitle = $"{GetNPCTypeName(npc.GetNPCType())} · {GetPersonalityName(npc.GetPersonality())} · 好感 {affinity}";
        TextMeshProUGUI info = CreateText("Info", obj.transform, subtitle, 15f, FontStyles.Normal, MutedText, TextAlignmentOptions.Left);
        info.rectTransform.sizeDelta = new Vector2(250f, 22f);

        TextMeshProUGUI relText = CreateText("Level", obj.transform, level, 14f, FontStyles.Normal, AccentColor, TextAlignmentOptions.Left);
        relText.rectTransform.sizeDelta = new Vector2(250f, 20f);

        string npcId = npc.id;
        button.onClick.AddListener(() =>
        {
            selectedNpcId = npcId;
            RefreshList();
            RefreshDetail();
        });

        return button;
    }

    private void RefreshDetail()
    {
        ClearChildren(detailContent);

        if (NPCDatabase.Instance == null)
        {
            CreateSectionText(detailContent, "NPC数据库尚未初始化。");
            return;
        }

        NPCData npc = string.IsNullOrEmpty(selectedNpcId) ? null : NPCDatabase.Instance.GetNPC(selectedNpcId);
        if (npc == null)
        {
            CreateSectionText(detailContent, "暂无可查看的NPC。");
            return;
        }

        NPCRelationshipData rel = AffinitySystem.Instance != null ? AffinitySystem.Instance.GetRelationship(npc.id) : null;

        GameObject top = new GameObject("TopProfile");
        top.transform.SetParent(detailContent, false);
        RectTransform topRT = top.AddComponent<RectTransform>();
        topRT.sizeDelta = new Vector2(840f, 260f);
        HorizontalLayoutGroup topLayout = top.AddComponent<HorizontalLayoutGroup>();
        topLayout.spacing = 22f;
        topLayout.childControlWidth = false;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = true;

        CreatePortrait(top.transform, npc);
        CreateProfileSummary(top.transform, npc, rel);

        CreateSection(detailContent, "人物介绍", BuildIntroText(npc));
        CreateSection(detailContent, "当前关系", BuildRelationshipText(rel));
        CreateSection(detailContent, "出现场所", BuildScheduleText(npc));
        CreateSection(detailContent, "喜好与雷区", BuildPreferenceText(npc));
        CreateSection(detailContent, "剧情定位", BuildStoryHookText(npc));
        CreateSection(detailContent, "好感方向", SafeText(npc.affinityGuide, "暂无好感互动说明。"));
        CreateSection(detailContent, "外观设计", SafeText(npc.visualDesign, "暂无外观设计说明。"));
        CreateSection(detailContent, "语言风格", SafeText(npc.languageStyle, "暂无语言风格说明。"));
        CreateSection(detailContent, "背景资料", BuildBackgroundText(npc));
        CreateSection(detailContent, "最近互动", BuildMemoryText(rel));
    }

    private void CreatePortrait(Transform parent, NPCData npc)
    {
        GameObject portraitPanel = CreatePanel("PortraitPanel", parent, SubtlePanelColor);
        RectTransform panelRT = portraitPanel.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(240f, 260f);

        VerticalLayoutGroup layout = portraitPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject imageObj = new GameObject("Portrait");
        imageObj.transform.SetParent(portraitPanel.transform, false);
        RectTransform imageRT = imageObj.AddComponent<RectTransform>();
        imageRT.sizeDelta = new Vector2(200f, 190f);

        Image image = imageObj.AddComponent<Image>();
        Sprite sprite = LoadPortraitSprite(npc);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
        }
        else
        {
            image.color = new Color(0.76f, 0.69f, 0.58f, 1f);
            TextMeshProUGUI placeholder = CreateText("PortraitPlaceholder", imageObj.transform, "立绘\n未配置", 26f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = Vector2.zero;
            placeholder.rectTransform.offsetMax = Vector2.zero;
        }

        string resourceName = !string.IsNullOrWhiteSpace(npc.portraitId) ? npc.portraitId : npc.id;
        TextMeshProUGUI pathHint = CreateText("PathHint", portraitPanel.transform, $"Resources/NPCPortraits/{resourceName}", 13f, FontStyles.Normal, MutedText, TextAlignmentOptions.Center);
        pathHint.rectTransform.sizeDelta = new Vector2(200f, 34f);
        pathHint.enableWordWrapping = true;
    }

    private void CreateProfileSummary(Transform parent, NPCData npc, NPCRelationshipData rel)
    {
        GameObject summary = new GameObject("ProfileSummary");
        summary.transform.SetParent(parent, false);
        RectTransform rt = summary.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(560f, 260f);

        VerticalLayoutGroup layout = summary.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI name = CreateText("NPCName", summary.transform, SafeText(npc.displayName, npc.id), 36f, FontStyles.Bold, DarkText, TextAlignmentOptions.Left);
        name.rectTransform.sizeDelta = new Vector2(560f, 46f);

        TextMeshProUGUI tags = CreateText("Tags", summary.transform, $"{GetNPCTypeName(npc.GetNPCType())} · {GetPersonalityName(npc.GetPersonality())}", 20f, FontStyles.Normal, AccentColor, TextAlignmentOptions.Left);
        tags.rectTransform.sizeDelta = new Vector2(560f, 28f);

        CreateAffinityBar(summary.transform, rel);

        string locationText = ResolveCurrentLocationText(npc);
        TextMeshProUGUI location = CreateText("Location", summary.transform, $"当前可能在：{locationText}", 18f, FontStyles.Normal, MutedText, TextAlignmentOptions.Left);
        location.rectTransform.sizeDelta = new Vector2(560f, 32f);

        string dialogueText = string.IsNullOrWhiteSpace(npc.dialogueId) ? "暂无专属对话ID" : $"对话ID：{npc.dialogueId}";
        TextMeshProUGUI dialogue = CreateText("DialogueId", summary.transform, dialogueText, 16f, FontStyles.Normal, MutedText, TextAlignmentOptions.Left);
        dialogue.rectTransform.sizeDelta = new Vector2(560f, 28f);
    }

    private void CreateAffinityBar(Transform parent, NPCRelationshipData rel)
    {
        GameObject wrap = new GameObject("AffinityWrap");
        wrap.transform.SetParent(parent, false);
        RectTransform wrapRT = wrap.AddComponent<RectTransform>();
        wrapRT.sizeDelta = new Vector2(560f, 62f);

        VerticalLayoutGroup layout = wrap.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        int affinity = rel != null ? rel.affinity : 0;
        string level = rel != null ? GetAffinityLevelName(rel.level) : "陌生";
        TextMeshProUGUI label = CreateText("AffinityLabel", wrap.transform, $"好感度 {affinity}/100 · {level}", 18f, FontStyles.Bold, DarkText, TextAlignmentOptions.Left);
        label.rectTransform.sizeDelta = new Vector2(560f, 24f);

        GameObject barBg = new GameObject("AffinityBarBg");
        barBg.transform.SetParent(wrap.transform, false);
        RectTransform barRT = barBg.AddComponent<RectTransform>();
        barRT.sizeDelta = new Vector2(520f, 18f);
        Image bg = barBg.AddComponent<Image>();
        bg.color = new Color(0.74f, 0.68f, 0.58f, 0.65f);

        GameObject fillObj = new GameObject("AffinityFill");
        fillObj.transform.SetParent(barBg.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(Mathf.Clamp01(affinity / 100f), 1f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fill = fillObj.AddComponent<Image>();
        fill.color = new Color(0.90f, 0.45f, 0.35f, 1f);
    }

    private void CreateSection(Transform parent, string title, string body)
    {
        GameObject section = CreatePanel("Section_" + title, parent, new Color(1f, 0.98f, 0.92f, 0.88f));
        RectTransform rt = section.GetComponent<RectTransform>();
        float bodyHeight = EstimateTextHeight(body, 800f, 17f);
        rt.sizeDelta = new Vector2(840f, bodyHeight + 64f);

        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI titleText = CreateText("Title", section.transform, title, 21f, FontStyles.Bold, AccentColor, TextAlignmentOptions.Left);
        titleText.rectTransform.sizeDelta = new Vector2(800f, 28f);

        TextMeshProUGUI bodyText = CreateText("Body", section.transform, body, 17f, FontStyles.Normal, DarkText, TextAlignmentOptions.Left);
        bodyText.rectTransform.sizeDelta = new Vector2(800f, bodyHeight);
        bodyText.enableWordWrapping = true;
    }

    private float EstimateTextHeight(string text, float width, float fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 40f;
        }

        int explicitLines = text.Split('\n').Length;
        float charactersPerLine = Mathf.Max(16f, width / (fontSize * 0.9f));
        float wrappedLines = Mathf.Ceil(text.Length / charactersPerLine);
        float lines = Mathf.Max(explicitLines, wrappedLines);
        return Mathf.Clamp(lines * (fontSize + 7f), 48f, 260f);
    }

    private TextMeshProUGUI CreateSectionText(Transform parent, string text)
    {
        TextMeshProUGUI tmp = CreateText("EmptyText", parent, text, 18f, FontStyles.Normal, MutedText, TextAlignmentOptions.Center);
        tmp.rectTransform.sizeDelta = new Vector2(780f, 80f);
        return tmp;
    }

    private RectTransform CreateScrollArea(string name, Transform parent, Vector2 size, out ScrollRect scrollRect)
    {
        GameObject viewportRoot = CreatePanel(name, parent, new Color(0.87f, 0.82f, 0.72f, 0.82f));
        RectTransform rootRT = viewportRoot.GetComponent<RectTransform>();
        rootRT.sizeDelta = size;

        scrollRect = viewportRoot.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 24f;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(viewportRoot.transform, false);
        RectTransform viewportRT = viewport.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(12f, 12f);
        viewportRT.offsetMax = new Vector2(-12f, -12f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;
        return contentRT;
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = Vector2.zero;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return obj;
    }

    private Button CreateTextButton(string name, Transform parent, string label, Vector2 size, Color bgColor, Color textColor)
    {
        GameObject obj = CreatePanel(name, parent, bgColor);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = new Color(Mathf.Min(bgColor.r + 0.08f, 1f), Mathf.Min(bgColor.g + 0.08f, 1f), Mathf.Min(bgColor.b + 0.08f, 1f), bgColor.a);
        colors.pressedColor = new Color(bgColor.r * 0.82f, bgColor.g * 0.82f, bgColor.b * 0.82f, bgColor.a);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", obj.transform, label, 19f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 30f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(tmp);
        }

        return tmp;
    }

    private Sprite LoadPortraitSprite(NPCData npc)
    {
        string portraitId = !string.IsNullOrWhiteSpace(npc.portraitId) ? npc.portraitId : npc.id;
        Sprite sprite = Resources.Load<Sprite>("NPCPortraits/" + portraitId);
        if (sprite == null && !string.Equals(portraitId, npc.id))
        {
            sprite = Resources.Load<Sprite>("NPCPortraits/" + npc.id);
        }
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>("NPCSprite");
        }
        return sprite;
    }

    private string BuildRelationshipText(NPCRelationshipData rel)
    {
        if (rel == null)
        {
            return "好感度：0/100\n关系等级：陌生\n恋爱状态：无";
        }

        return $"好感度：{rel.affinity}/100\n关系等级：{GetAffinityLevelName(rel.level)}\n恋爱状态：{GetRomanceStateName(rel.romanceState)}\n连续未互动回合：{rel.consecutiveNoInteractionTurns}";
    }

    private string BuildIntroText(NPCData npc)
    {
        string basicInfo = BuildBasicInfoText(npc);
        string summary = SafeText(npc.profileSummary, npc.description);
        string keywords = SafeText(npc.personalityKeywords, string.Empty);
        if (string.IsNullOrWhiteSpace(keywords))
        {
            return $"{basicInfo}{SafeText(summary, "暂未填写人物介绍。")}";
        }

        return $"{basicInfo}{SafeText(summary, "暂未填写人物介绍。")}\n关键词：{keywords}";
    }

    private string BuildBasicInfoText(NPCData npc)
    {
        List<string> lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(npc.grade))
        {
            lines.Add($"年级：{npc.grade}");
        }
        if (!string.IsNullOrWhiteSpace(npc.identity))
        {
            lines.Add($"身份：{npc.identity}");
        }
        if (!string.IsNullOrWhiteSpace(npc.roleType))
        {
            lines.Add($"角色类型：{npc.roleType}");
        }
        if (!string.IsNullOrWhiteSpace(npc.mainActivityArea))
        {
            lines.Add($"主要活动区域：{npc.mainActivityArea}");
        }

        return lines.Count == 0 ? string.Empty : string.Join("\n", lines) + "\n\n";
    }

    private string BuildScheduleText(NPCData npc)
    {
        if (npc.schedule == null || npc.schedule.Length == 0)
        {
            return "暂无日程信息。";
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < npc.schedule.Length; i++)
        {
            NPCScheduleEntry entry = npc.schedule[i];
            if (entry == null)
            {
                continue;
            }

            lines.Add($"{GetTimeSlotName(entry.GetTimeSlot())}：{SafeText(entry.location, "未知地点")}");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "暂无日程信息。";
    }

    private string BuildPreferenceText(NPCData npc)
    {
        string likes = BuildActionNameList(npc.likedActionIds);
        string dislikes = BuildActionNameList(npc.dislikedActionIds);
        string likeDetail = SafeText(npc.likesDescription, "暂无详细喜好说明。");
        string dislikeDetail = SafeText(npc.dislikesDescription, "暂无详细雷区说明。");
        return $"喜欢行动：{likes}\n不喜欢行动：{dislikes}\n\n喜好：{likeDetail}\n雷区：{dislikeDetail}";
    }

    private string BuildStoryHookText(NPCData npc)
    {
        return SafeText(npc.storyHooks, "暂无剧情定位说明。");
    }

    private string BuildBackgroundText(NPCData npc)
    {
        string portrait = !string.IsNullOrWhiteSpace(npc.portraitId) ? npc.portraitId : npc.id;
        string description = SafeText(npc.description, "暂无背景资料。");
        string basicInfo = BuildBasicInfoText(npc);
        return $"{description}\n\n{basicInfo}NPC ID：{npc.id}\n立绘资源名：{portrait}\n类型：{GetNPCTypeName(npc.GetNPCType())}\n性格：{GetPersonalityName(npc.GetPersonality())}";
    }

    private string BuildMemoryText(NPCRelationshipData rel)
    {
        if (rel == null || rel.memories == null || rel.memories.Count == 0)
        {
            return "暂无互动记录。";
        }

        List<string> lines = new List<string>();
        int start = Mathf.Max(0, rel.memories.Count - 5);
        for (int i = rel.memories.Count - 1; i >= start; i--)
        {
            lines.Add(rel.memories[i]);
        }

        return string.Join("\n", lines);
    }

    private string BuildActionNameList(string[] actionIds)
    {
        if (actionIds == null || actionIds.Length == 0)
        {
            return "暂无";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < actionIds.Length; i++)
        {
            string id = actionIds[i];
            SocialActionDefinition action = NPCDatabase.Instance != null ? NPCDatabase.Instance.GetSocialAction(id) : null;
            names.Add(action != null ? SafeText(action.displayName, id) : id);
        }

        return string.Join("、", names);
    }

    private string ResolveCurrentLocationText(NPCData npc)
    {
        if (NPCDatabase.Instance == null)
        {
            return "未知";
        }

        TimeSlot slot = ResolveCurrentTimeSlot();
        string location = NPCDatabase.Instance.GetNPCLocation(npc.id, slot);
        return string.IsNullOrWhiteSpace(location) ? "暂无当前时段日程" : location;
    }

    private TimeSlot ResolveCurrentTimeSlot()
    {
        int ap = GameState.Instance != null ? GameState.Instance.ActionPoints : 5;
        if (ap >= 4) return TimeSlot.Morning;
        if (ap >= 2) return TimeSlot.Afternoon;
        return TimeSlot.Evening;
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void OnAffinityChanged(string npcId, int oldAffinity, int newAffinity, int delta)
    {
        if (!IsOpen())
        {
            return;
        }

        RefreshList();
        if (npcId == selectedNpcId)
        {
            RefreshDetail();
        }
    }

    private string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private string GetNPCTypeName(NPCType type)
    {
        switch (type)
        {
            case NPCType.Roommate: return "室友";
            case NPCType.Senior: return "学长/学姐";
            case NPCType.Classmate: return "同学";
            case NPCType.Teacher: return "老师";
            default: return "其他";
        }
    }

    private string GetPersonalityName(NPCPersonality personality)
    {
        switch (personality)
        {
            case NPCPersonality.Introvert: return "内向";
            case NPCPersonality.Extrovert: return "外向";
            case NPCPersonality.Easygoing: return "随和";
            case NPCPersonality.Mysterious: return "神秘";
            case NPCPersonality.Cheerful: return "开朗";
            case NPCPersonality.Serious: return "认真";
            default: return "普通";
        }
    }

    private string GetAffinityLevelName(AffinityLevel level)
    {
        switch (level)
        {
            case AffinityLevel.Acquaintance: return "认识";
            case AffinityLevel.Friend: return "朋友";
            case AffinityLevel.CloseFriend: return "亲近";
            case AffinityLevel.BestFriend: return "挚友";
            case AffinityLevel.Lover: return "恋人";
            default: return "陌生";
        }
    }

    private string GetRomanceStateName(RomanceState state)
    {
        switch (state)
        {
            case RomanceState.Crushing: return "暧昧";
            case RomanceState.Dating: return "交往中";
            case RomanceState.BrokenUp: return "已分手";
            case RomanceState.Hostile: return "关系破裂";
            default: return "无";
        }
    }

    private string GetTimeSlotName(TimeSlot slot)
    {
        switch (slot)
        {
            case TimeSlot.Morning: return "上午";
            case TimeSlot.Afternoon: return "下午";
            case TimeSlot.Evening: return "晚上";
            default: return "未知";
        }
    }
}
