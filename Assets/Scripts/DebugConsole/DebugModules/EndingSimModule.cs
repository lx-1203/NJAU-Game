#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingSimModule : MonoBehaviour, IDebugModule
{
    private sealed class EndingEntryUI
    {
        public EndingDefinition ending;
        public GameObject root;
        public TextMeshProUGUI summaryText;
        public TextMeshProUGUI detailText;
        public Button editButton;
        public Button triggerButton;
    }

    private sealed class ConditionEditorUI
    {
        public TMP_Dropdown typeDropdown;
        public TMP_InputField valueInput;
        public Button removeButton;
        public GameObject root;
    }

    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.16f, 0.96f);
    private static readonly Color RowColor = new Color(0.13f, 0.13f, 0.20f, 0.96f);
    private static readonly Color ButtonBlue = new Color(0.22f, 0.42f, 0.72f, 1f);
    private static readonly Color ButtonGreen = new Color(0.22f, 0.58f, 0.34f, 1f);
    private static readonly Color InputBg = new Color(0.09f, 0.09f, 0.14f, 1f);

    private TextMeshProUGUI summaryText;
    private TextMeshProUGUI currentEndingText;
    private TextMeshProUGUI resultHintText;
    private TextMeshProUGUI editorTitleText;
    private TextMeshProUGUI editorHintText;
    private TMP_InputField searchInput;
    private TMP_InputField nameInput;
    private TMP_InputField cgIdInput;
    private TMP_InputField descriptionInput;
    private RectTransform listContent;
    private RectTransform conditionListContent;

    private readonly List<EndingEntryUI> entryUIs = new List<EndingEntryUI>();
    private readonly List<ConditionEditorUI> conditionEditors = new List<ConditionEditorUI>();
    private readonly string[] conditionTypeOptions = Enum.GetNames(typeof(EndingConditionType));
    private string currentSearch = string.Empty;
    private string editingEndingId = string.Empty;
    private EndingDefinition editingDraft;

    public void Init(RectTransform parent)
    {
        GameObject scrollObject = CreateUIElement("EndingModuleScroll", parent);
        StretchFull(scrollObject.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        GameObject viewport = CreateUIElement("Viewport", scrollObject.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject root = CreateUIElement("EndingModuleRoot", viewport.transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.spacing = 12f;
        rootLayout.padding = new RectOffset(20, 20, 16, 16);
        rootLayout.childAlignment = TextAnchor.UpperLeft;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = root.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = rootRect;

        CreateTitleSection(root.transform);
        CreateCurrentEndingSection(root.transform);
        CreateSearchSection(root.transform);
        CreateEditorSection(root.transform);
        CreateListSection(root.transform);
    }

    public void Refresh()
    {
        RefreshSummary();
        RefreshCurrentEnding();
        RefreshEndingList();
        if (string.IsNullOrEmpty(editingEndingId))
            TryAutoSelectCurrentEnding();
        else
            LoadEditorForEnding(editingEndingId);
    }

    private void CreateTitleSection(Transform parent)
    {
        GameObject section = CreateSection(parent, 220f);

        CreateLabel(section.transform, "结局总览 / 快速触发", 18f, TextGold, 30f);
        summaryText = CreateLabel(section.transform, string.Empty, 14f, TextWhite, 176f);
        summaryText.enableWordWrapping = true;
    }

    private void CreateCurrentEndingSection(Transform parent)
    {
        GameObject section = CreateSection(parent, 250f);

        CreateLabel(section.transform, "当前命中结局", 16f, TextGold, 28f);
        currentEndingText = CreateLabel(section.transform, string.Empty, 14f, TextWhite, 142f);
        currentEndingText.enableWordWrapping = true;

        resultHintText = CreateLabel(section.transform, "点击下方“进入结局”会直接走正式结局流程并回到标题界面。", 12f, TextGray, 24f);
        resultHintText.enableWordWrapping = true;
        CreateButton(section.transform, "立即判定结局", ButtonGreen, 128f, 32f, TriggerEvaluatedEndingEvent);
    }

    private void CreateEditorSection(Transform parent)
    {
        GameObject section = CreateSection(parent, 320f);

        CreateLabel(section.transform, "结局编辑器（运行时）", 16f, TextGold, 28f);
        editorTitleText = CreateLabel(section.transform, "未选择结局", 14f, TextWhite, 24f);

        GameObject metaRow = CreateUIElement("EditorMetaRow", section.transform);
        LayoutElement metaRowLayout = metaRow.AddComponent<LayoutElement>();
        metaRowLayout.preferredHeight = 34f;

        HorizontalLayoutGroup metaLayout = metaRow.AddComponent<HorizontalLayoutGroup>();
        metaLayout.spacing = 8f;
        metaLayout.childAlignment = TextAnchor.MiddleLeft;
        metaLayout.childControlWidth = false;
        metaLayout.childControlHeight = false;
        metaLayout.childForceExpandWidth = false;
        metaLayout.childForceExpandHeight = false;

        CreateMiniLabel(metaRow.transform, "标题");
        nameInput = CreateInputField(metaRow.transform, 210f, "结局可见标题");
        CreateMiniLabel(metaRow.transform, "CG");
        cgIdInput = CreateInputField(metaRow.transform, 170f, "cgId");

        GameObject descBlock = CreateUIElement("DescriptionBlock", section.transform);
        LayoutElement descLayout = descBlock.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 88f;

        VerticalLayoutGroup descGroup = descBlock.AddComponent<VerticalLayoutGroup>();
        descGroup.spacing = 4f;
        descGroup.childAlignment = TextAnchor.UpperLeft;
        descGroup.childControlWidth = true;
        descGroup.childControlHeight = false;
        descGroup.childForceExpandWidth = true;
        descGroup.childForceExpandHeight = false;

        CreateMiniLabel(descBlock.transform, "剧情 / 描述");
        descriptionInput = CreateMultilineInput(descBlock.transform, "输入结局展示文本", 60f);

        GameObject conditionHeader = CreateUIElement("ConditionHeader", section.transform);
        LayoutElement conditionHeaderLayout = conditionHeader.AddComponent<LayoutElement>();
        conditionHeaderLayout.preferredHeight = 34f;

        HorizontalLayoutGroup conditionHeaderGroup = conditionHeader.AddComponent<HorizontalLayoutGroup>();
        conditionHeaderGroup.spacing = 8f;
        conditionHeaderGroup.childAlignment = TextAnchor.MiddleLeft;
        conditionHeaderGroup.childControlWidth = false;
        conditionHeaderGroup.childControlHeight = false;
        conditionHeaderGroup.childForceExpandWidth = false;
        conditionHeaderGroup.childForceExpandHeight = false;

        CreateMiniLabel(conditionHeader.transform, "触发条件");
        CreateButton(conditionHeader.transform, "新增条件", ButtonBlue, 96f, 28f, AddConditionEditorRow);

        GameObject conditionScroll = CreateUIElement("ConditionScrollView", section.transform);
        LayoutElement conditionScrollLayout = conditionScroll.AddComponent<LayoutElement>();
        conditionScrollLayout.preferredHeight = 120f;
        conditionScrollLayout.minHeight = 120f;

        Image scrollBg = conditionScroll.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.09f, 0.95f);

        ScrollRect scrollRect = conditionScroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUIElement("Viewport", conditionScroll.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        conditionListContent = content.GetComponent<RectTransform>();
        conditionListContent.anchorMin = new Vector2(0f, 1f);
        conditionListContent.anchorMax = new Vector2(1f, 1f);
        conditionListContent.pivot = new Vector2(0.5f, 1f);
        conditionListContent.offsetMin = new Vector2(8f, 0f);
        conditionListContent.offsetMax = new Vector2(-8f, 0f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.padding = new RectOffset(0, 0, 8, 8);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = conditionListContent;

        GameObject actionsRow = CreateUIElement("EditorActionsRow", section.transform);
        LayoutElement actionsLayout = actionsRow.AddComponent<LayoutElement>();
        actionsLayout.preferredHeight = 34f;

        HorizontalLayoutGroup actionsGroup = actionsRow.AddComponent<HorizontalLayoutGroup>();
        actionsGroup.spacing = 8f;
        actionsGroup.childAlignment = TextAnchor.MiddleLeft;
        actionsGroup.childControlWidth = false;
        actionsGroup.childControlHeight = false;
        actionsGroup.childForceExpandWidth = false;
        actionsGroup.childForceExpandHeight = false;

        CreateButton(actionsRow.transform, "应用修改", ButtonGreen, 96f, 28f, ApplyEditorChanges);
        CreateButton(actionsRow.transform, "恢复此结局", ButtonBlue, 112f, 28f, ResetSelectedEnding);
        CreateButton(actionsRow.transform, "恢复全部结局", ButtonBlue, 126f, 28f, ResetAllEndings);

        editorHintText = CreateLabel(section.transform, "修改仅作用于当前运行时，适合在钟山台里调文案和触发条件。", 12f, TextGray, 24f);
        editorHintText.enableWordWrapping = true;
    }

    private void CreateSearchSection(Transform parent)
    {
        GameObject row = CreateUIElement("SearchRow", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 42f;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI label = CreateLabel(row.transform, "查询结局", 14f, TextGold, 32f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.rectTransform.sizeDelta = new Vector2(92f, 32f);

        searchInput = CreateInputField(row.transform, 320f, "输入结局名 / ID / 层级 / 星级");
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        CreateButton(row.transform, "清空", ButtonBlue, 84f, 32f, () =>
        {
            if (searchInput != null)
                searchInput.text = string.Empty;
            ApplyFilter(string.Empty);
        });

        CreateButton(row.transform, "刷新列表", ButtonBlue, 96f, 32f, RefreshEndingList);
    }

    private void CreateListSection(Transform parent)
    {
        GameObject section = CreateSection(parent, 0f);
        LayoutElement sectionLayout = section.GetComponent<LayoutElement>();
        sectionLayout.flexibleHeight = 1f;
        sectionLayout.minHeight = 260f;

        CreateLabel(section.transform, "全部结局", 16f, TextGold, 28f);

        GameObject scrollObj = CreateUIElement("EndingScrollView", section.transform);
        RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.sizeDelta = new Vector2(0f, 0f);
        LayoutElement scrollLayout = scrollObj.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 220f;

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.09f, 0.95f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        listContent = content.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.offsetMin = new Vector2(10f, 0f);
        listContent.offsetMax = new Vector2(-10f, 0f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 8f;
        contentLayout.padding = new RectOffset(0, 0, 8, 8);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = listContent;
    }

    private void RefreshSummary()
    {
        if (summaryText == null || PlayerAttributes.Instance == null)
            return;

        PlayerAttributes pa = PlayerAttributes.Instance;
        int totalEndingCount = EndingDeterminer.Instance != null ? EndingDeterminer.Instance.GetAllEndings().Count : 0;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"已载入结局 {totalEndingCount} 个");
        builder.AppendLine($"学力 {pa.Study}  魅力 {pa.Charm}  体魄 {pa.Physique}  领导力 {pa.Leadership}");
        builder.AppendLine($"压力 {pa.Stress}  心情 {pa.Mood}  黑暗值 {pa.Darkness}  负罪感 {pa.Guilt}  幸运 {pa.Luck}");

        if (GameState.Instance != null)
        {
            string genderText = GameState.Instance.PlayerGender == 1 ? "女主" : "男主";
            builder.AppendLine($"角色 {genderText}  时间 {GameState.Instance.GetTimeDescription()}");
            builder.AppendLine($"金钱 {GameState.Instance.Money}  行动点 {GameState.Instance.ActionPoints}/{GameState.Instance.EffectiveMaxActionPoints}");
        }

        if (ExamSystem.Instance != null)
        {
            builder.AppendLine(
                $"GPA {ExamSystem.Instance.GetCumulativeGPA():F2}  四级 {(ExamSystem.Instance.IsCET4Passed ? "已过" : "未过")}  六级 {(ExamSystem.Instance.IsCET6Passed ? "已过" : "未过")}");
        }

        if (JobSystem.Instance != null || AffinitySystem.Instance != null)
        {
            int internCount = JobSystem.Instance != null ? JobSystem.Instance.totalInternshipCount : 0;
            int friendCount = AffinitySystem.Instance != null ? AffinitySystem.Instance.GetFriendOrAboveCount() : 0;
            builder.AppendLine($"实习次数 {internCount}  好友数 {friendCount}");
        }

        if (SemesterSummarySystem.Instance != null)
        {
            builder.AppendLine(
                $"学习次数 {SemesterSummarySystem.Instance.StudyCount}  社交次数 {SemesterSummarySystem.Instance.SocialCount}  毕业总评 {SemesterSummarySystem.Instance.CalculateGraduationScore():F1}");
        }

        summaryText.text = builder.ToString().TrimEnd();
    }

    private void RefreshCurrentEnding()
    {
        if (EndingDeterminer.Instance == null)
        {
            currentEndingText.text = "EndingDeterminer 尚未就绪";
            currentEndingText.color = TextGray;
            return;
        }

        EndingResult result = EndingDeterminer.Instance.DetermineEndingPreview();
        if (result == null || result.ending == null)
        {
            currentEndingText.text = "暂无结局结果";
            currentEndingText.color = TextGray;
            return;
        }

        EndingDefinition ending = result.ending;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"{ending.name}  ({ending.id})");
        builder.AppendLine($"层级 {ending.layer} / {GetLayerText(ending.GetLayer())}  星级 {ending.stars}  天赋点 {result.talentPoints}");
        builder.AppendLine($"结算分 {result.finalScore:F1}");

        if (!string.IsNullOrEmpty(ending.description))
            builder.AppendLine(ending.description);

        currentEndingText.text = builder.ToString().TrimEnd();
        currentEndingText.color = TextWhite;
    }

    private void RefreshEndingList()
    {
        if (listContent == null)
            return;

        for (int i = 0; i < entryUIs.Count; i++)
        {
            if (entryUIs[i].root != null)
                Destroy(entryUIs[i].root);
        }
        entryUIs.Clear();

        if (EndingDeterminer.Instance == null)
            return;

        List<EndingDefinition> endings = EndingDeterminer.Instance.GetAllEndings();
        for (int i = 0; i < endings.Count; i++)
        {
            CreateEndingRow(endings[i]);
        }

        ApplyFilter(currentSearch);
    }

    private void CreateEndingRow(EndingDefinition ending)
    {
        GameObject row = CreateUIElement($"Ending_{ending.id}", listContent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 112f;

        Image rowBg = row.AddComponent<Image>();
        rowBg.color = RowColor;

        VerticalLayoutGroup rowGroup = row.AddComponent<VerticalLayoutGroup>();
        rowGroup.spacing = 6f;
        rowGroup.padding = new RectOffset(10, 10, 8, 8);
        rowGroup.childAlignment = TextAnchor.UpperLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = false;
        rowGroup.childForceExpandWidth = true;
        rowGroup.childForceExpandHeight = false;

        GameObject topRow = CreateUIElement("TopRow", row.transform);
        LayoutElement topLayout = topRow.AddComponent<LayoutElement>();
        topLayout.preferredHeight = 28f;

        HorizontalLayoutGroup topGroup = topRow.AddComponent<HorizontalLayoutGroup>();
        topGroup.spacing = 10f;
        topGroup.childAlignment = TextAnchor.MiddleLeft;
        topGroup.childControlWidth = false;
        topGroup.childControlHeight = false;
        topGroup.childForceExpandWidth = false;
        topGroup.childForceExpandHeight = false;

        TextMeshProUGUI summary = CreateLabel(topRow.transform,
            $"{ending.id} | {ending.name} | 层级 {ending.layer} | {ending.stars} 星",
            14f, TextWhite, 28f);
        summary.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement summaryLayout = summary.gameObject.AddComponent<LayoutElement>();
        summaryLayout.preferredWidth = 780f;
        summaryLayout.flexibleWidth = 1f;

        Button triggerButton = CreateButton(topRow.transform, "进入结局", ButtonGreen, 100f, 28f, () => TriggerSpecificEnding(ending));
        Button editButton = CreateButton(topRow.transform, "编辑", ButtonBlue, 72f, 28f, () => LoadEditorForEnding(ending.id));

        TextMeshProUGUI detail = CreateLabel(row.transform, BuildDetailText(ending), 12f, TextGray, 62f);
        detail.enableWordWrapping = true;
        detail.overflowMode = TextOverflowModes.Ellipsis;

        EndingEntryUI entry = new EndingEntryUI
        {
            ending = ending,
            root = row,
            summaryText = summary,
            detailText = detail,
            editButton = editButton,
            triggerButton = triggerButton
        };

        entryUIs.Add(entry);
        UpdateEntryVisual(entry);
    }

    private void TriggerSpecificEnding(EndingDefinition ending)
    {
        if (ending == null)
            return;

        if (GameEndingManager.Instance == null)
        {
            GameObject obj = new GameObject("GameEndingManager");
            obj.AddComponent<GameEndingManager>();
        }

        bool success = GameEndingManager.Instance != null &&
                       GameEndingManager.Instance.TriggerSpecificEnding(ending.id, $"DebugConsole:{ending.id}");

        if (success)
        {
            DebugConsoleManager.Log("Ending", $"Trigger specific ending: {ending.id} {ending.name}");
        }
        else
        {
            DebugConsoleManager.Log("Ending", $"Failed to trigger specific ending: {ending.id}");
        }
    }

    private void TriggerEvaluatedEndingEvent()
    {
        if (EndingEvaluator.Instance == null)
        {
            if (resultHintText != null)
                resultHintText.text = "EndingEvaluator 尚未就绪，无法判定新结局事件。";
            return;
        }

        EndingId ending = EndingEvaluator.Instance.EvaluateEnding();
        string eventId = EndingEvaluator.Instance.ConvertEndingToEventId(ending);

        if (resultHintText != null)
            resultHintText.text = $"新结局判定：{ending} -> {eventId}";

        DebugConsoleManager.Log("Ending", $"Evaluate narrative ending: {ending} -> {eventId}");
        EndingEvaluator.Instance.EvaluateAndTriggerEnding();
    }

    private void OnSearchChanged(string value)
    {
        ApplyFilter(value);
    }

    private void ApplyFilter(string value)
    {
        currentSearch = value ?? string.Empty;
        string keyword = currentSearch.Trim().ToLowerInvariant();

        for (int i = 0; i < entryUIs.Count; i++)
        {
            EndingEntryUI entry = entryUIs[i];
            bool visible = MatchesKeyword(entry.ending, keyword);
            if (entry.root != null)
                entry.root.SetActive(visible);
        }
    }

    private bool MatchesKeyword(EndingDefinition ending, string keyword)
    {
        if (ending == null)
            return false;

        if (string.IsNullOrEmpty(keyword))
            return true;

        if (!string.IsNullOrEmpty(ending.id) && ending.id.ToLowerInvariant().Contains(keyword))
            return true;
        if (!string.IsNullOrEmpty(ending.name) && ending.name.ToLowerInvariant().Contains(keyword))
            return true;
        if (!string.IsNullOrEmpty(ending.description) && ending.description.ToLowerInvariant().Contains(keyword))
            return true;
        if (ending.layer.ToString().Contains(keyword))
            return true;
        if (ending.stars.ToString().Contains(keyword))
            return true;
        if (GetLayerText(ending.GetLayer()).ToLowerInvariant().Contains(keyword))
            return true;

        return false;
    }

    private void UpdateEntryVisual(EndingEntryUI entry)
    {
        bool matched = EndingDeterminer.Instance != null && CheckEndingMatched(entry.ending);
        if (entry.summaryText != null)
            entry.summaryText.color = matched ? TextGold : TextWhite;
        if (entry.detailText != null)
            entry.detailText.color = matched ? new Color(0.9f, 0.86f, 0.55f) : TextGray;
        if (entry.editButton != null)
        {
            Image buttonImage = entry.editButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = string.Equals(entry.ending != null ? entry.ending.id : string.Empty, editingEndingId, StringComparison.OrdinalIgnoreCase)
                    ? TextGold
                    : ButtonBlue;
        }
    }

    private bool CheckEndingMatched(EndingDefinition ending)
    {
        if (ending == null || EndingDeterminer.Instance == null)
            return false;

        if (ending.conditions == null || ending.conditions.Count == 0)
            return true;

        for (int i = 0; i < ending.conditions.Count; i++)
        {
            if (!EndingDeterminer.Instance.EvaluateCondition(ending.conditions[i]))
                return false;
        }

        return true;
    }

    private string BuildDetailText(EndingDefinition ending)
    {
        if (ending == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        builder.Append(string.IsNullOrEmpty(ending.description) ? "无描述" : ending.description);

        if (ending.conditions != null && ending.conditions.Count > 0 && EndingDeterminer.Instance != null)
        {
            builder.Append("\n条件：");
            for (int i = 0; i < ending.conditions.Count; i++)
            {
                if (i > 0)
                    builder.Append("；");

                EndingCondition condition = ending.conditions[i];
                bool matched = EndingDeterminer.Instance.EvaluateCondition(condition);
                builder.Append(matched ? "[满足] " : "[未满足] ");
                builder.Append(EndingDeterminer.Instance.DescribeCondition(condition));
            }
        }

        return builder.ToString();
    }

    private void TryAutoSelectCurrentEnding()
    {
        if (EndingDeterminer.Instance == null)
            return;

        EndingResult result = EndingDeterminer.Instance.DetermineEndingPreview();
        if (result != null && result.ending != null)
            LoadEditorForEnding(result.ending.id);
    }

    private void LoadEditorForEnding(string endingId)
    {
        if (EndingDeterminer.Instance == null || string.IsNullOrWhiteSpace(endingId))
            return;

        EndingDefinition ending = EndingDeterminer.Instance.GetEndingById(endingId);
        if (ending == null)
            return;

        editingEndingId = ending.id;
        editingDraft = ending.Clone();

        if (editorTitleText != null)
            editorTitleText.text = $"{ending.id} | 层级 {ending.layer} | {ending.stars} 星";
        if (nameInput != null)
            nameInput.text = ending.name ?? string.Empty;
        if (cgIdInput != null)
            cgIdInput.text = ending.cgId ?? string.Empty;
        if (descriptionInput != null)
            descriptionInput.text = ending.description ?? string.Empty;

        RebuildConditionEditorRows(editingDraft.conditions);
        RefreshCurrentEnding();

        for (int i = 0; i < entryUIs.Count; i++)
            UpdateEntryVisual(entryUIs[i]);
    }

    private void RebuildConditionEditorRows(List<EndingCondition> conditions)
    {
        for (int i = 0; i < conditionEditors.Count; i++)
        {
            if (conditionEditors[i].root != null)
                Destroy(conditionEditors[i].root);
        }
        conditionEditors.Clear();

        if (conditionListContent == null)
            return;

        if (conditions == null || conditions.Count == 0)
        {
            AddConditionEditorRow();
            return;
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            EndingCondition condition = conditions[i] ?? new EndingCondition();
            AddConditionEditorRow(condition.type, condition.value);
        }
    }

    private void AddConditionEditorRow()
    {
        AddConditionEditorRow(conditionTypeOptions.Length > 0 ? conditionTypeOptions[0] : string.Empty, 0f);
    }

    private void AddConditionEditorRow(string type, float value)
    {
        if (conditionListContent == null)
            return;

        GameObject row = CreateUIElement("ConditionRow", conditionListContent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 32f;

        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 8f;
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = false;
        rowGroup.childControlHeight = false;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = false;

        TMP_Dropdown typeDropdown = CreateDropdown(row.transform, conditionTypeOptions, 270f, 28f);
        int index = Array.IndexOf(conditionTypeOptions, string.IsNullOrWhiteSpace(type) ? conditionTypeOptions[0] : type);
        typeDropdown.value = index >= 0 ? index : 0;
        typeDropdown.RefreshShownValue();

        TMP_InputField valueInput = CreateInputField(row.transform, 90f, "值");
        valueInput.text = value.ToString("0.##");

        ConditionEditorUI entry = null;
        Button removeButton = CreateButton(row.transform, "删除", ButtonBlue, 64f, 28f, () =>
        {
            if (entry != null)
                RemoveConditionEditorRow(entry);
        });

        entry = new ConditionEditorUI
        {
            root = row,
            typeDropdown = typeDropdown,
            valueInput = valueInput,
            removeButton = removeButton
        };

        conditionEditors.Add(entry);
    }

    private void RemoveConditionEditorRow(ConditionEditorUI entry)
    {
        if (entry == null)
            return;

        conditionEditors.Remove(entry);
        if (entry.root != null)
            Destroy(entry.root);
    }

    private void ApplyEditorChanges()
    {
        if (EndingDeterminer.Instance == null || editingDraft == null || string.IsNullOrWhiteSpace(editingEndingId))
        {
            SetEditorHint("请先从下方列表选择一个结局。", true);
            return;
        }

        EndingDefinition updated = editingDraft.Clone();
        updated.name = nameInput != null ? nameInput.text.Trim() : updated.name;
        updated.cgId = cgIdInput != null ? cgIdInput.text.Trim() : updated.cgId;
        updated.description = descriptionInput != null ? descriptionInput.text.Trim() : updated.description;
        updated.conditions = new List<EndingCondition>();

        for (int i = 0; i < conditionEditors.Count; i++)
        {
            ConditionEditorUI editor = conditionEditors[i];
            string type = GetDropdownValue(editor.typeDropdown);
            float value = 0f;
            if (editor.valueInput != null && !string.IsNullOrWhiteSpace(editor.valueInput.text) &&
                !float.TryParse(editor.valueInput.text.Trim(), out value))
            {
                SetEditorHint($"条件 {i + 1} 的数值无法解析。", true);
                return;
            }

            updated.conditions.Add(new EndingCondition(type, value));
        }

        if (string.IsNullOrWhiteSpace(updated.name))
        {
            SetEditorHint("结局标题不能为空。", true);
            return;
        }

        if (!EndingDeterminer.Instance.UpdateEndingDefinition(updated))
        {
            SetEditorHint("应用失败，未找到该结局。", true);
            return;
        }

        DebugConsoleManager.Log("Ending", $"Updated ending runtime override: {updated.id}");
        SetEditorHint($"已应用 {updated.id} 的运行时修改。", false);
        LoadEditorForEnding(updated.id);
        RefreshSummary();
        RefreshCurrentEnding();
        RefreshEndingList();
    }

    private void ResetSelectedEnding()
    {
        if (EndingDeterminer.Instance == null || string.IsNullOrWhiteSpace(editingEndingId))
        {
            SetEditorHint("当前没有选中的结局。", true);
            return;
        }

        if (!EndingDeterminer.Instance.ResetEndingToOriginal(editingEndingId))
        {
            SetEditorHint("恢复失败，未找到原始定义。", true);
            return;
        }

        DebugConsoleManager.Log("Ending", $"Reset ending runtime override: {editingEndingId}");
        SetEditorHint($"已恢复 {editingEndingId} 的原始定义。", false);
        LoadEditorForEnding(editingEndingId);
        RefreshSummary();
        RefreshCurrentEnding();
        RefreshEndingList();
    }

    private void ResetAllEndings()
    {
        if (EndingDeterminer.Instance == null)
            return;

        EndingDeterminer.Instance.ResetAllRuntimeOverrides();
        DebugConsoleManager.Log("Ending", "Reset all ending runtime overrides");
        SetEditorHint("已恢复全部结局到原始定义。", false);

        string keepEditingId = editingEndingId;
        editingDraft = null;
        editingEndingId = string.Empty;

        RefreshSummary();
        RefreshCurrentEnding();
        RefreshEndingList();

        if (!string.IsNullOrWhiteSpace(keepEditingId))
            LoadEditorForEnding(keepEditingId);
        else
            TryAutoSelectCurrentEnding();
    }

    private void SetEditorHint(string text, bool isError)
    {
        if (editorHintText == null)
            return;

        editorHintText.text = text;
        editorHintText.color = isError ? new Color(1f, 0.56f, 0.56f) : TextGray;
    }

    private GameObject CreateSection(Transform parent, float preferredHeight)
    {
        GameObject section = CreateUIElement("Section", parent);
        Image bg = section.AddComponent<Image>();
        bg.color = PanelColor;

        LayoutElement layout = section.AddComponent<LayoutElement>();
        if (preferredHeight > 0f)
            layout.preferredHeight = preferredHeight;

        VerticalLayoutGroup group = section.AddComponent<VerticalLayoutGroup>();
        group.spacing = 8f;
        group.padding = new RectOffset(12, 12, 10, 10);
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = false;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        return section;
    }

    private TMP_InputField CreateInputField(Transform parent, float width, string placeholderText)
    {
        GameObject inputObject = CreateUIElement("SearchInput", parent);
        RectTransform rt = inputObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 32f);

        Image bg = inputObject.AddComponent<Image>();
        bg.color = InputBg;

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        input.textViewport = CreateViewport(inputObject.transform);
        input.textComponent = CreateInputText(input.textViewport.transform, TextWhite);
        input.placeholder = CreatePlaceholder(input.textViewport.transform, placeholderText);
        input.lineType = TMP_InputField.LineType.SingleLine;

        return input;
    }

    private TMP_InputField CreateMultilineInput(Transform parent, string placeholderText, float height)
    {
        TMP_InputField input = CreateInputField(parent, 0f, placeholderText);
        RectTransform rt = input.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, height);
        LayoutElement layout = input.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;

        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.textComponent.enableWordWrapping = true;
        input.textComponent.alignment = TextAlignmentOptions.TopLeft;
        if (input.placeholder is TextMeshProUGUI placeholder)
        {
            placeholder.enableWordWrapping = true;
            placeholder.alignment = TextAlignmentOptions.TopLeft;
        }

        RectTransform viewport = input.textViewport;
        viewport.offsetMin = new Vector2(10f, 6f);
        viewport.offsetMax = new Vector2(-10f, -6f);
        return input;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string[] options, float width, float height)
    {
        GameObject dropdownObj = CreateUIElement("Dropdown", parent);
        RectTransform rt = dropdownObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image bg = dropdownObj.AddComponent<Image>();
        bg.color = InputBg;

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = bg;

        RectTransform template = BuildDropdownTemplate(dropdownObj.transform);
        TextMeshProUGUI caption = CreateDropdownCaption(dropdownObj.transform);
        TextMeshProUGUI itemLabel = BuildDropdownItemTemplate(template);

        dropdown.template = template;
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;
        dropdown.options = BuildDropdownOptions(options);
        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private RectTransform BuildDropdownTemplate(Transform parent)
    {
        GameObject templateObj = CreateUIElement("Template", parent);
        RectTransform templateRt = templateObj.GetComponent<RectTransform>();
        templateRt.anchorMin = new Vector2(0f, 1f);
        templateRt.anchorMax = new Vector2(1f, 1f);
        templateRt.pivot = new Vector2(0.5f, 1f);
        templateRt.anchoredPosition = new Vector2(0f, -32f);
        templateRt.sizeDelta = new Vector2(0f, 150f);
        templateObj.SetActive(false);

        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = PanelColor;
        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateUIElement("Viewport", templateObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRt;

        GameObject item = CreateUIElement("Item", content.transform);
        RectTransform itemRt = item.GetComponent<RectTransform>();
        itemRt.sizeDelta = new Vector2(0f, 26f);
        Toggle toggle = item.AddComponent<Toggle>();
        Image itemBg = item.AddComponent<Image>();
        itemBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

        TextMeshProUGUI label = CreateLabel(item.transform, "Option", 12f, TextWhite, 26f);
        StretchFull(label.rectTransform);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(8f, 0f, 8f, 0f);

        toggle.targetGraphic = itemBg;
        toggle.graphic = null;

        return templateRt;
    }

    private TextMeshProUGUI CreateDropdownCaption(Transform parent)
    {
        TextMeshProUGUI caption = CreateLabel(parent, string.Empty, 12f, TextWhite, 24f);
        StretchFull(caption.rectTransform);
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        caption.margin = new Vector4(10f, 0f, 22f, 0f);
        caption.enableWordWrapping = false;
        return caption;
    }

    private TextMeshProUGUI BuildDropdownItemTemplate(RectTransform template)
    {
        if (template == null)
            return null;

        Transform label = template.Find("Viewport/Content/Item/Option");
        return label != null ? label.GetComponent<TextMeshProUGUI>() : null;
    }

    private List<TMP_Dropdown.OptionData> BuildDropdownOptions(string[] options)
    {
        List<TMP_Dropdown.OptionData> result = new List<TMP_Dropdown.OptionData>();
        if (options == null)
            return result;

        for (int i = 0; i < options.Length; i++)
            result.Add(new TMP_Dropdown.OptionData(options[i]));

        return result;
    }

    private string GetDropdownValue(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
            return string.Empty;

        int index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
        return dropdown.options[index].text;
    }

    private TextMeshProUGUI CreateMiniLabel(Transform parent, string text)
    {
        TextMeshProUGUI label = CreateLabel(parent, text, 12f, TextGold, 24f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = Mathf.Max(44f, text.Length * 14f);
        return label;
    }

    private RectTransform CreateViewport(Transform parent)
    {
        GameObject viewport = CreateUIElement("Viewport", parent);
        RectTransform rt = viewport.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10f, 4f);
        rt.offsetMax = new Vector2(-10f, -4f);
        viewport.AddComponent<RectMask2D>();
        return rt;
    }

    private TextMeshProUGUI CreateInputText(Transform parent, Color color)
    {
        TextMeshProUGUI tmp = CreateLabel(parent, string.Empty, 13f, color, 24f);
        StretchFull(tmp.rectTransform);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private TextMeshProUGUI CreatePlaceholder(Transform parent, string text)
    {
        TextMeshProUGUI tmp = CreateLabel(parent, text, 13f, TextGray, 24f);
        StretchFull(tmp.rectTransform);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private Button CreateButton(Transform parent, string label, Color color, float width, float height, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUIElement("Button", parent);
        RectTransform rt = buttonObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        Image bg = buttonObject.AddComponent<Image>();
        bg.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 13f, Color.white, height);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private string GetLayerText(EndingLayer layer)
    {
        switch (layer)
        {
            case EndingLayer.ForcedEnding: return "强制";
            case EndingLayer.PeakEnding: return "巅峰";
            case EndingLayer.PlannedPath: return "规划内";
            case EndingLayer.UnplannedPath: return "规划外";
            case EndingLayer.DarkEnding: return "黑暗";
            case EndingLayer.SpecialEnding: return "特殊";
            case EndingLayer.NewCareer: return "新职业";
            case EndingLayer.FallbackEnding: return "兜底";
            default: return layer.ToString();
        }
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height)
    {
        GameObject obj = CreateUIElement("Label", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        float resolvedHeight = Mathf.Max(height, fontSize + 14f);
        rt.sizeDelta = new Vector2(0f, resolvedHeight);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(2f, 4f, 2f, 4f);
        tmp.extraPadding = true;
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
