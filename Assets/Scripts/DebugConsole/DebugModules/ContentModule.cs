#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color Panel = new Color(0.11f, 0.11f, 0.17f, 0.92f);
    private static readonly Color Row = new Color(0.13f, 0.13f, 0.20f, 0.96f);
    private static readonly Color BtnBlue = new Color(0.23f, 0.43f, 0.72f, 1.0f);
    private static readonly Color BtnGreen = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnRed = new Color(0.60f, 0.20f, 0.20f, 1.0f);
    private static readonly Color InputBg = new Color(0.09f, 0.09f, 0.14f, 1f);

    private ZhongshanDeckTitleContent editingContent;
    private TextMeshProUGUI statusText;

    private TMP_InputField creditsPanelTitleInput;
    private TMP_InputField creditsTabLabelInput;
    private TMP_InputField creditsFooterInput;
    private TMP_Dropdown creditsEntryDropdown;
    private TMP_InputField creditsEntryTitleInput;
    private TMP_InputField creditsEntryRoleInput;
    private TMP_InputField creditsEntryDescriptionInput;
    private TMP_InputField creditsEntryTagsInput;

    private TMP_InputField changelogPanelTitleInput;
    private TMP_Dropdown changelogSectionDropdown;
    private TMP_InputField changelogHeadingInput;
    private TMP_InputField changelogNoteInput;
    private TMP_InputField changelogBulletsInput;

    private TMP_InputField tutorialPanelTitleInput;
    private TMP_Dropdown tutorialCategoryDropdown;
    private TMP_Dropdown tutorialEntryDropdown;
    private TMP_InputField tutorialCategoryNameInput;
    private TMP_InputField tutorialEntryTitleInput;
    private TMP_InputField tutorialEntryLeadInput;
    private TMP_InputField tutorialEntryDescriptionInput;
    private TMP_InputField tutorialEntryHighlightsInput;

    private TMP_InputField homepageHintInput;
    private TMP_InputField homepageChangelogButtonInput;
    private TMP_InputField homepageSettingsTitleInput;
    private TMP_InputField homepageSettingsBackInput;
    private TMP_InputField homepageContinueLabelInput;
    private TMP_InputField homepageStartLabelInput;
    private TMP_InputField homepageLoadLabelInput;
    private TMP_InputField homepageSettingsLabelInput;
    private TMP_InputField homepageQuitLabelInput;
    private TMP_InputField homepageTutorialIconLabelInput;
    private TMP_InputField homepageTutorialIconTipInput;
    private TMP_InputField homepageAchievementIconLabelInput;
    private TMP_InputField homepageAchievementIconTipInput;
    private TMP_InputField homepageGalleryIconLabelInput;
    private TMP_InputField homepageGalleryIconTipInput;
    private TMP_InputField homepageCreditsIconLabelInput;
    private TMP_InputField homepageCreditsIconTipInput;

    private int selectedCreditsIndex;
    private int selectedChangelogIndex;
    private int selectedTutorialCategoryIndex;
    private int selectedTutorialEntryIndex;

    public void Init(RectTransform parent)
    {
        GameObject scrollObj = CreateUIElement("ContentScrollView", parent);
        StretchFull(scrollObj.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        CreateLabel(content.transform, "标题内容速编", 18f, TextGold, 30f);
        statusText = CreateLabel(content.transform, "这里编辑首页的更新日志与制作组名单。保存后，Unity 钟山台与标题界面会读取同一份内容。", 13f, TextGray, 42f);
        statusText.enableWordWrapping = true;

        BuildTopActions(content.transform);
        BuildHomepagePanel(content.transform);
        BuildCreditsPanel(content.transform);
        BuildChangelogPanel(content.transform);
        BuildTutorialPanel(content.transform);

        LoadFromState();
    }

    public void Refresh()
    {
        if (editingContent == null)
        {
            LoadFromState();
        }
    }

    private void BuildTopActions(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 110f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateLabel(panel.transform, "共享内容资产", 15f, TextGold, 24f);
        GameObject row = CreateRow(panel.transform, 36f);
        CreateButton(row.transform, "重新载入", 110f, BtnBlue, LoadFromState);
        CreateButton(row.transform, "保存到资产", 120f, BtnGreen, SaveToState);
        CreateButton(row.transform, "回刷标题界面", 120f, BtnBlue, NotifyTitleScreenReload);
    }

    private void BuildHomepagePanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 0f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateLabel(panel.transform, "首页固定文案", 15f, TextGold, 24f);
        homepageHintInput = CreateLabeledInput(panel.transform, "点击提示", "点击任意位置继续", 32f);
        homepageChangelogButtonInput = CreateLabeledInput(panel.transform, "日志按钮", "更新日志", 32f);
        homepageSettingsTitleInput = CreateLabeledInput(panel.transform, "设置标题", "设置", 32f);
        homepageSettingsBackInput = CreateLabeledInput(panel.transform, "设置返回", "返回", 32f);

        CreateLabel(panel.transform, "主菜单按钮", 13f, TextGray, 20f);
        homepageContinueLabelInput = CreateLabeledInput(panel.transform, "继续游戏", "继续游戏", 32f);
        homepageStartLabelInput = CreateLabeledInput(panel.transform, "开始游戏", "开始游戏", 32f);
        homepageLoadLabelInput = CreateLabeledInput(panel.transform, "载入游戏", "载入游戏", 32f);
        homepageSettingsLabelInput = CreateLabeledInput(panel.transform, "设置按钮", "设  置", 32f);
        homepageQuitLabelInput = CreateLabeledInput(panel.transform, "退出游戏", "退出游戏", 32f);

        CreateLabel(panel.transform, "右上角入口", 13f, TextGray, 20f);
        homepageTutorialIconLabelInput = CreateLabeledInput(panel.transform, "教程标签", "教程", 32f);
        homepageTutorialIconTipInput = CreateLabeledInput(panel.transform, "教程提示", "游戏教程", 32f);
        homepageAchievementIconLabelInput = CreateLabeledInput(panel.transform, "成就标签", "成就", 32f);
        homepageAchievementIconTipInput = CreateLabeledInput(panel.transform, "成就提示", "成就", 32f);
        homepageGalleryIconLabelInput = CreateLabeledInput(panel.transform, "CG标签", "CG", 32f);
        homepageGalleryIconTipInput = CreateLabeledInput(panel.transform, "CG提示", "游戏CG", 32f);
        homepageCreditsIconLabelInput = CreateLabeledInput(panel.transform, "制作组标签", "制作人", 32f);
        homepageCreditsIconTipInput = CreateLabeledInput(panel.transform, "制作组提示", "制作人详情", 32f);
    }

    private void BuildCreditsPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 0f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateLabel(panel.transform, "制作组名单", 15f, TextGold, 24f);

        GameObject metaRow = CreateRow(panel.transform, 34f);
        CreateLabel(metaRow.transform, "面板标题", 13f, TextWhite, 28f, 70f);
        creditsPanelTitleInput = CreateInputField(metaRow.transform, "制作人", 210f, 30f);
        CreateLabel(metaRow.transform, "页签", 13f, TextWhite, 28f, 40f);
        creditsTabLabelInput = CreateInputField(metaRow.transform, "STAFF", 110f, 30f);

        CreateLabel(panel.transform, "页脚文案", 13f, TextGray, 20f);
        creditsFooterInput = CreateInputField(panel.transform, "页脚文案", 0f, 70f);
        creditsFooterInput.lineType = TMP_InputField.LineType.MultiLineNewline;

        GameObject selectorRow = CreateRow(panel.transform, 36f);
        CreateLabel(selectorRow.transform, "条目", 13f, TextWhite, 28f, 40f);
        creditsEntryDropdown = CreateDropdown(selectorRow.transform, new[] { "暂无条目" }, 240f, 30f);
        creditsEntryDropdown.onValueChanged.AddListener(OnCreditsDropdownChanged);
        CreateButton(selectorRow.transform, "新增", 72f, BtnGreen, AddCreditsEntry);
        CreateButton(selectorRow.transform, "删除", 72f, BtnRed, DeleteCreditsEntry);
        CreateButton(selectorRow.transform, "上移", 72f, BtnBlue, () => MoveCreditsEntry(-1));
        CreateButton(selectorRow.transform, "下移", 72f, BtnBlue, () => MoveCreditsEntry(1));

        creditsEntryTitleInput = CreateLabeledInput(panel.transform, "标题", "总制作", 32f);
        creditsEntryRoleInput = CreateLabeledInput(panel.transform, "身份 / 职责", "项目策划 / 系统统筹", 32f);
        CreateLabel(panel.transform, "简介", 13f, TextGray, 20f);
        creditsEntryDescriptionInput = CreateInputField(panel.transform, "条目简介", 0f, 92f);
        creditsEntryDescriptionInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        creditsEntryDescriptionInput.textComponent.alignment = TextAlignmentOptions.TopLeft;

        CreateLabel(panel.transform, "标签（用英文逗号分隔）", 13f, TextGray, 20f);
        creditsEntryTagsInput = CreateInputField(panel.transform, "玩法框架, 系统节奏, 内容整合", 0f, 58f);
    }

    private void BuildChangelogPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 0f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateLabel(panel.transform, "更新日志", 15f, TextGold, 24f);

        changelogPanelTitleInput = CreateLabeledInput(panel.transform, "面板标题", "更新日志", 32f);

        GameObject selectorRow = CreateRow(panel.transform, 36f);
        CreateLabel(selectorRow.transform, "分段", 13f, TextWhite, 28f, 40f);
        changelogSectionDropdown = CreateDropdown(selectorRow.transform, new[] { "暂无分段" }, 260f, 30f);
        changelogSectionDropdown.onValueChanged.AddListener(OnChangelogDropdownChanged);
        CreateButton(selectorRow.transform, "新增", 72f, BtnGreen, AddChangelogSection);
        CreateButton(selectorRow.transform, "删除", 72f, BtnRed, DeleteChangelogSection);
        CreateButton(selectorRow.transform, "上移", 72f, BtnBlue, () => MoveChangelogSection(-1));
        CreateButton(selectorRow.transform, "下移", 72f, BtnBlue, () => MoveChangelogSection(1));

        changelogHeadingInput = CreateLabeledInput(panel.transform, "版本标题", "更新补丁 1.90", 32f);

        CreateLabel(panel.transform, "更新项（每行一条）", 13f, TextGray, 20f);
        changelogBulletsInput = CreateInputField(panel.transform, "每行一条更新内容", 0f, 140f);
        changelogBulletsInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        changelogBulletsInput.textComponent.alignment = TextAlignmentOptions.TopLeft;

        CreateLabel(panel.transform, "补充说明 / 尾注", 13f, TextGray, 20f);
        changelogNoteInput = CreateInputField(panel.transform, "例如：后续版本会继续补充首页入口、同学录和系统体验优化。", 0f, 84f);
        changelogNoteInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        changelogNoteInput.textComponent.alignment = TextAlignmentOptions.TopLeft;
    }

    private void BuildTutorialPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 0f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        CreateLabel(panel.transform, "教程 / 新生手册", 15f, TextGold, 24f);
        tutorialPanelTitleInput = CreateLabeledInput(panel.transform, "面板标题", "新生手册", 32f);

        GameObject categoryRow = CreateRow(panel.transform, 36f);
        CreateLabel(categoryRow.transform, "分类", 13f, TextWhite, 28f, 40f);
        tutorialCategoryDropdown = CreateDropdown(categoryRow.transform, new[] { "暂无分类" }, 220f, 30f);
        tutorialCategoryDropdown.onValueChanged.AddListener(OnTutorialCategoryChanged);
        CreateButton(categoryRow.transform, "新增", 72f, BtnGreen, AddTutorialCategory);
        CreateButton(categoryRow.transform, "删除", 72f, BtnRed, DeleteTutorialCategory);
        CreateButton(categoryRow.transform, "上移", 72f, BtnBlue, () => MoveTutorialCategory(-1));
        CreateButton(categoryRow.transform, "下移", 72f, BtnBlue, () => MoveTutorialCategory(1));

        tutorialCategoryNameInput = CreateLabeledInput(panel.transform, "分类名", "属性", 32f);

        GameObject entryRow = CreateRow(panel.transform, 36f);
        CreateLabel(entryRow.transform, "条目", 13f, TextWhite, 28f, 40f);
        tutorialEntryDropdown = CreateDropdown(entryRow.transform, new[] { "暂无条目" }, 220f, 30f);
        tutorialEntryDropdown.onValueChanged.AddListener(OnTutorialEntryChanged);
        CreateButton(entryRow.transform, "新增", 72f, BtnGreen, AddTutorialEntry);
        CreateButton(entryRow.transform, "删除", 72f, BtnRed, DeleteTutorialEntry);
        CreateButton(entryRow.transform, "上移", 72f, BtnBlue, () => MoveTutorialEntry(-1));
        CreateButton(entryRow.transform, "下移", 72f, BtnBlue, () => MoveTutorialEntry(1));

        tutorialEntryTitleInput = CreateLabeledInput(panel.transform, "标题", "智力", 32f);
        tutorialEntryLeadInput = CreateLabeledInput(panel.transform, "导语", "决定课程学习、考试通过率与部分学术事件。", 32f);

        CreateLabel(panel.transform, "正文", 13f, TextGray, 20f);
        tutorialEntryDescriptionInput = CreateInputField(panel.transform, "详细说明", 0f, 110f);
        tutorialEntryDescriptionInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        tutorialEntryDescriptionInput.textComponent.alignment = TextAlignmentOptions.TopLeft;

        CreateLabel(panel.transform, "高亮标签（每行一条）", 13f, TextGray, 20f);
        tutorialEntryHighlightsInput = CreateInputField(panel.transform, "课程成绩\n考试修正\n学术事件", 0f, 92f);
        tutorialEntryHighlightsInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        tutorialEntryHighlightsInput.textComponent.alignment = TextAlignmentOptions.TopLeft;
    }

    private void LoadFromState()
    {
        editingContent = ZhongshanDeckToolStateBridge.GetTitleContent();
        editingContent ??= new ZhongshanDeckTitleContent();
        editingContent.EnsureInitialized();

        selectedCreditsIndex = Mathf.Clamp(selectedCreditsIndex, 0, Mathf.Max(0, editingContent.credits.entries.Count - 1));
        selectedChangelogIndex = Mathf.Clamp(selectedChangelogIndex, 0, Mathf.Max(0, editingContent.changelog.sections.Count - 1));
        selectedTutorialCategoryIndex = Mathf.Clamp(selectedTutorialCategoryIndex, 0, Mathf.Max(0, editingContent.tutorial.categories.Count - 1));
        selectedTutorialEntryIndex = GetSelectedTutorialEntryCount() > 0 ? Mathf.Clamp(selectedTutorialEntryIndex, 0, GetSelectedTutorialEntryCount() - 1) : 0;
        RefreshEditorsFromDraft();
        SetStatus("已载入共享标题内容。");
    }

    private void SaveToState()
    {
        CommitEditorsToDraft();
        ZhongshanDeckToolStateBridge.SaveTitleContent(editingContent);
        NotifyTitleScreenReload();
        SetStatus("已保存标题内容到钟山台状态资产。");
        DebugConsoleManager.Log("Content", "Saved title authored content");
    }

    private void NotifyTitleScreenReload()
    {
        TitleScreenManager titleScreen = FindObjectOfType<TitleScreenManager>();
        if (titleScreen != null)
        {
            titleScreen.DebugReloadAuthoredTitleContent();
            SetStatus("已通知当前标题界面重载内容。");
        }
    }

    private void CommitEditorsToDraft()
    {
        if (editingContent == null)
        {
            editingContent = new ZhongshanDeckTitleContent();
        }

        editingContent.EnsureInitialized();
        editingContent.homepage.hintMessage = GetSafeText(homepageHintInput, "点击任意位置继续");
        editingContent.homepage.changelogButtonLabel = GetSafeText(homepageChangelogButtonInput, "更新日志");
        editingContent.homepage.settingsPanelTitle = GetSafeText(homepageSettingsTitleInput, "设置");
        editingContent.homepage.settingsBackButtonLabel = GetSafeText(homepageSettingsBackInput, "返回");
        UpsertHomepageMenuLabel("continue", GetSafeText(homepageContinueLabelInput, "继续游戏"));
        UpsertHomepageMenuLabel("start", GetSafeText(homepageStartLabelInput, "开始游戏"));
        UpsertHomepageMenuLabel("load", GetSafeText(homepageLoadLabelInput, "载入游戏"));
        UpsertHomepageMenuLabel("settings", GetSafeText(homepageSettingsLabelInput, "设  置"));
        UpsertHomepageMenuLabel("quit", GetSafeText(homepageQuitLabelInput, "退出游戏"));
        UpsertHomepageTopIcon("tutorial", GetSafeText(homepageTutorialIconLabelInput, "教程"), GetSafeText(homepageTutorialIconTipInput, "游戏教程"));
        UpsertHomepageTopIcon("achievement", GetSafeText(homepageAchievementIconLabelInput, "成就"), GetSafeText(homepageAchievementIconTipInput, "成就"));
        UpsertHomepageTopIcon("gallery", GetSafeText(homepageGalleryIconLabelInput, "CG"), GetSafeText(homepageGalleryIconTipInput, "游戏CG"));
        UpsertHomepageTopIcon("credits", GetSafeText(homepageCreditsIconLabelInput, "制作人"), GetSafeText(homepageCreditsIconTipInput, "制作人详情"));
        editingContent.credits.panelTitle = GetSafeText(creditsPanelTitleInput, "制作人");
        editingContent.credits.tabLabel = GetSafeText(creditsTabLabelInput, "STAFF");
        editingContent.credits.footerText = GetSafeText(creditsFooterInput, string.Empty);

        if (editingContent.credits.entries.Count > 0 && selectedCreditsIndex >= 0 && selectedCreditsIndex < editingContent.credits.entries.Count)
        {
            ZhongshanDeckCreditsEntry entry = editingContent.credits.entries[selectedCreditsIndex];
            entry.title = GetSafeText(creditsEntryTitleInput, "未命名条目");
            entry.role = GetSafeText(creditsEntryRoleInput, string.Empty);
            entry.description = GetSafeText(creditsEntryDescriptionInput, string.Empty);
            entry.tags = ParseCsvTags(creditsEntryTagsInput != null ? creditsEntryTagsInput.text : string.Empty);
        }

        editingContent.changelog.panelTitle = GetSafeText(changelogPanelTitleInput, "更新日志");
        if (editingContent.changelog.sections.Count > 0 && selectedChangelogIndex >= 0 && selectedChangelogIndex < editingContent.changelog.sections.Count)
        {
            ZhongshanDeckChangelogSection section = editingContent.changelog.sections[selectedChangelogIndex];
            section.heading = GetSafeText(changelogHeadingInput, "未命名版本");
            section.note = GetSafeText(changelogNoteInput, string.Empty);
            section.bulletLines = ParseMultiline(changelogBulletsInput != null ? changelogBulletsInput.text : string.Empty);
        }

        editingContent.tutorial.panelTitle = GetSafeText(tutorialPanelTitleInput, "新生手册");
        if (editingContent.tutorial.categories.Count > 0 && selectedTutorialCategoryIndex >= 0 && selectedTutorialCategoryIndex < editingContent.tutorial.categories.Count)
        {
            ZhongshanDeckTutorialCategoryData category = editingContent.tutorial.categories[selectedTutorialCategoryIndex];
            category.name = GetSafeText(tutorialCategoryNameInput, "未命名分类");

            if (category.entries.Count > 0 && selectedTutorialEntryIndex >= 0 && selectedTutorialEntryIndex < category.entries.Count)
            {
                ZhongshanDeckTutorialEntryData entry = category.entries[selectedTutorialEntryIndex];
                entry.title = GetSafeText(tutorialEntryTitleInput, "未命名条目");
                entry.lead = GetSafeText(tutorialEntryLeadInput, string.Empty);
                entry.description = GetSafeText(tutorialEntryDescriptionInput, string.Empty);
                entry.highlights = ParseMultiline(tutorialEntryHighlightsInput != null ? tutorialEntryHighlightsInput.text : string.Empty);
            }
        }
    }

    private void RefreshEditorsFromDraft()
    {
        homepageHintInput?.SetTextWithoutNotify(editingContent.homepage.hintMessage);
        homepageChangelogButtonInput?.SetTextWithoutNotify(editingContent.homepage.changelogButtonLabel);
        homepageSettingsTitleInput?.SetTextWithoutNotify(editingContent.homepage.settingsPanelTitle);
        homepageSettingsBackInput?.SetTextWithoutNotify(editingContent.homepage.settingsBackButtonLabel);
        homepageContinueLabelInput?.SetTextWithoutNotify(GetHomepageMenuLabel("continue", "继续游戏"));
        homepageStartLabelInput?.SetTextWithoutNotify(GetHomepageMenuLabel("start", "开始游戏"));
        homepageLoadLabelInput?.SetTextWithoutNotify(GetHomepageMenuLabel("load", "载入游戏"));
        homepageSettingsLabelInput?.SetTextWithoutNotify(GetHomepageMenuLabel("settings", "设  置"));
        homepageQuitLabelInput?.SetTextWithoutNotify(GetHomepageMenuLabel("quit", "退出游戏"));
        homepageTutorialIconLabelInput?.SetTextWithoutNotify(GetHomepageIconLabel("tutorial", "教程"));
        homepageTutorialIconTipInput?.SetTextWithoutNotify(GetHomepageIconTooltip("tutorial", "游戏教程"));
        homepageAchievementIconLabelInput?.SetTextWithoutNotify(GetHomepageIconLabel("achievement", "成就"));
        homepageAchievementIconTipInput?.SetTextWithoutNotify(GetHomepageIconTooltip("achievement", "成就"));
        homepageGalleryIconLabelInput?.SetTextWithoutNotify(GetHomepageIconLabel("gallery", "CG"));
        homepageGalleryIconTipInput?.SetTextWithoutNotify(GetHomepageIconTooltip("gallery", "游戏CG"));
        homepageCreditsIconLabelInput?.SetTextWithoutNotify(GetHomepageIconLabel("credits", "制作人"));
        homepageCreditsIconTipInput?.SetTextWithoutNotify(GetHomepageIconTooltip("credits", "制作人详情"));
        creditsPanelTitleInput?.SetTextWithoutNotify(editingContent.credits.panelTitle);
        creditsTabLabelInput?.SetTextWithoutNotify(editingContent.credits.tabLabel);
        creditsFooterInput?.SetTextWithoutNotify(editingContent.credits.footerText);
        changelogPanelTitleInput?.SetTextWithoutNotify(editingContent.changelog.panelTitle);
        tutorialPanelTitleInput?.SetTextWithoutNotify(editingContent.tutorial.panelTitle);

        RefreshCreditsDropdown();
        RefreshChangelogDropdown();
        RefreshTutorialCategoryDropdown();
        RefreshTutorialEntryDropdown();
        LoadSelectedCreditsEntryIntoEditors();
        LoadSelectedChangelogSectionIntoEditors();
        LoadSelectedTutorialIntoEditors();
    }

    private void RefreshCreditsDropdown()
    {
        List<string> options = new List<string>();
        for (int i = 0; i < editingContent.credits.entries.Count; i++)
        {
            ZhongshanDeckCreditsEntry entry = editingContent.credits.entries[i];
            options.Add(string.IsNullOrWhiteSpace(entry.title) ? $"条目 {i + 1}" : entry.title);
        }

        if (options.Count == 0)
        {
            options.Add("暂无条目");
        }

        creditsEntryDropdown.ClearOptions();
        creditsEntryDropdown.AddOptions(options);
        creditsEntryDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedCreditsIndex, 0, options.Count - 1));
    }

    private void RefreshChangelogDropdown()
    {
        List<string> options = new List<string>();
        for (int i = 0; i < editingContent.changelog.sections.Count; i++)
        {
            ZhongshanDeckChangelogSection section = editingContent.changelog.sections[i];
            options.Add(string.IsNullOrWhiteSpace(section.heading) ? $"分段 {i + 1}" : section.heading);
        }

        if (options.Count == 0)
        {
            options.Add("暂无分段");
        }

        changelogSectionDropdown.ClearOptions();
        changelogSectionDropdown.AddOptions(options);
        changelogSectionDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedChangelogIndex, 0, options.Count - 1));
    }

    private void LoadSelectedCreditsEntryIntoEditors()
    {
        bool hasEntry = editingContent.credits.entries.Count > 0 && selectedCreditsIndex >= 0 && selectedCreditsIndex < editingContent.credits.entries.Count;
        ZhongshanDeckCreditsEntry entry = hasEntry ? editingContent.credits.entries[selectedCreditsIndex] : null;

        creditsEntryTitleInput?.SetTextWithoutNotify(entry != null ? entry.title : string.Empty);
        creditsEntryRoleInput?.SetTextWithoutNotify(entry != null ? entry.role : string.Empty);
        creditsEntryDescriptionInput?.SetTextWithoutNotify(entry != null ? entry.description : string.Empty);
        creditsEntryTagsInput?.SetTextWithoutNotify(entry != null ? string.Join(", ", entry.tags) : string.Empty);
    }

    private void LoadSelectedChangelogSectionIntoEditors()
    {
        bool hasSection = editingContent.changelog.sections.Count > 0 && selectedChangelogIndex >= 0 && selectedChangelogIndex < editingContent.changelog.sections.Count;
        ZhongshanDeckChangelogSection section = hasSection ? editingContent.changelog.sections[selectedChangelogIndex] : null;

        changelogHeadingInput?.SetTextWithoutNotify(section != null ? section.heading : string.Empty);
        changelogNoteInput?.SetTextWithoutNotify(section != null ? section.note : string.Empty);
        changelogBulletsInput?.SetTextWithoutNotify(section != null ? string.Join("\n", section.bulletLines) : string.Empty);
    }

    private void RefreshTutorialCategoryDropdown()
    {
        List<string> options = new List<string>();
        for (int i = 0; i < editingContent.tutorial.categories.Count; i++)
        {
            ZhongshanDeckTutorialCategoryData category = editingContent.tutorial.categories[i];
            options.Add(string.IsNullOrWhiteSpace(category.name) ? $"分类 {i + 1}" : category.name);
        }

        if (options.Count == 0)
        {
            options.Add("暂无分类");
        }

        tutorialCategoryDropdown.ClearOptions();
        tutorialCategoryDropdown.AddOptions(options);
        tutorialCategoryDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedTutorialCategoryIndex, 0, options.Count - 1));
    }

    private void RefreshTutorialEntryDropdown()
    {
        List<string> options = new List<string>();
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        if (category != null)
        {
            for (int i = 0; i < category.entries.Count; i++)
            {
                ZhongshanDeckTutorialEntryData entry = category.entries[i];
                options.Add(string.IsNullOrWhiteSpace(entry.title) ? $"条目 {i + 1}" : entry.title);
            }
        }

        if (options.Count == 0)
        {
            options.Add("暂无条目");
        }

        tutorialEntryDropdown.ClearOptions();
        tutorialEntryDropdown.AddOptions(options);
        tutorialEntryDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedTutorialEntryIndex, 0, options.Count - 1));
    }

    private void LoadSelectedTutorialIntoEditors()
    {
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        tutorialCategoryNameInput?.SetTextWithoutNotify(category != null ? category.name : string.Empty);

        ZhongshanDeckTutorialEntryData entry = GetSelectedTutorialEntry();
        tutorialEntryTitleInput?.SetTextWithoutNotify(entry != null ? entry.title : string.Empty);
        tutorialEntryLeadInput?.SetTextWithoutNotify(entry != null ? entry.lead : string.Empty);
        tutorialEntryDescriptionInput?.SetTextWithoutNotify(entry != null ? entry.description : string.Empty);
        tutorialEntryHighlightsInput?.SetTextWithoutNotify(entry != null ? string.Join("\n", entry.highlights) : string.Empty);
    }

    private void AddCreditsEntry()
    {
        CommitEditorsToDraft();
        editingContent.credits.entries.Add(new ZhongshanDeckCreditsEntry
        {
            title = "新条目",
            role = "请填写职责",
            description = "请填写制作组说明。",
            tags = new List<string>()
        });
        selectedCreditsIndex = editingContent.credits.entries.Count - 1;
        RefreshEditorsFromDraft();
    }

    private void DeleteCreditsEntry()
    {
        if (editingContent.credits.entries.Count == 0)
        {
            return;
        }

        editingContent.credits.entries.RemoveAt(selectedCreditsIndex);
        selectedCreditsIndex = Mathf.Clamp(selectedCreditsIndex, 0, Mathf.Max(0, editingContent.credits.entries.Count - 1));
        RefreshEditorsFromDraft();
    }

    private void MoveCreditsEntry(int direction)
    {
        if (editingContent.credits.entries.Count <= 1)
        {
            return;
        }

        CommitEditorsToDraft();
        int target = Mathf.Clamp(selectedCreditsIndex + direction, 0, editingContent.credits.entries.Count - 1);
        if (target == selectedCreditsIndex)
        {
            return;
        }

        ZhongshanDeckCreditsEntry entry = editingContent.credits.entries[selectedCreditsIndex];
        editingContent.credits.entries.RemoveAt(selectedCreditsIndex);
        editingContent.credits.entries.Insert(target, entry);
        selectedCreditsIndex = target;
        RefreshEditorsFromDraft();
    }

    private void AddChangelogSection()
    {
        CommitEditorsToDraft();
        editingContent.changelog.sections.Add(new ZhongshanDeckChangelogSection
        {
            heading = "更新补丁",
            bulletLines = new List<string> { "请填写第一条更新内容" },
            note = string.Empty
        });
        selectedChangelogIndex = editingContent.changelog.sections.Count - 1;
        RefreshEditorsFromDraft();
    }

    private void DeleteChangelogSection()
    {
        if (editingContent.changelog.sections.Count == 0)
        {
            return;
        }

        editingContent.changelog.sections.RemoveAt(selectedChangelogIndex);
        selectedChangelogIndex = Mathf.Clamp(selectedChangelogIndex, 0, Mathf.Max(0, editingContent.changelog.sections.Count - 1));
        RefreshEditorsFromDraft();
    }

    private void MoveChangelogSection(int direction)
    {
        if (editingContent.changelog.sections.Count <= 1)
        {
            return;
        }

        CommitEditorsToDraft();
        int target = Mathf.Clamp(selectedChangelogIndex + direction, 0, editingContent.changelog.sections.Count - 1);
        if (target == selectedChangelogIndex)
        {
            return;
        }

        ZhongshanDeckChangelogSection section = editingContent.changelog.sections[selectedChangelogIndex];
        editingContent.changelog.sections.RemoveAt(selectedChangelogIndex);
        editingContent.changelog.sections.Insert(target, section);
        selectedChangelogIndex = target;
        RefreshEditorsFromDraft();
    }

    private void AddTutorialCategory()
    {
        CommitEditorsToDraft();
        editingContent.tutorial.categories.Add(new ZhongshanDeckTutorialCategoryData
        {
            name = "新分类",
            entries = new List<ZhongshanDeckTutorialEntryData>()
        });
        selectedTutorialCategoryIndex = editingContent.tutorial.categories.Count - 1;
        selectedTutorialEntryIndex = 0;
        RefreshEditorsFromDraft();
    }

    private void DeleteTutorialCategory()
    {
        if (editingContent.tutorial.categories.Count == 0)
        {
            return;
        }

        editingContent.tutorial.categories.RemoveAt(selectedTutorialCategoryIndex);
        selectedTutorialCategoryIndex = Mathf.Clamp(selectedTutorialCategoryIndex, 0, Mathf.Max(0, editingContent.tutorial.categories.Count - 1));
        selectedTutorialEntryIndex = 0;
        RefreshEditorsFromDraft();
    }

    private void MoveTutorialCategory(int direction)
    {
        if (editingContent.tutorial.categories.Count <= 1)
        {
            return;
        }

        CommitEditorsToDraft();
        int target = Mathf.Clamp(selectedTutorialCategoryIndex + direction, 0, editingContent.tutorial.categories.Count - 1);
        if (target == selectedTutorialCategoryIndex)
        {
            return;
        }

        ZhongshanDeckTutorialCategoryData category = editingContent.tutorial.categories[selectedTutorialCategoryIndex];
        editingContent.tutorial.categories.RemoveAt(selectedTutorialCategoryIndex);
        editingContent.tutorial.categories.Insert(target, category);
        selectedTutorialCategoryIndex = target;
        selectedTutorialEntryIndex = 0;
        RefreshEditorsFromDraft();
    }

    private void AddTutorialEntry()
    {
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        if (category == null)
        {
            AddTutorialCategory();
            category = GetSelectedTutorialCategory();
        }

        CommitEditorsToDraft();
        category.entries.Add(new ZhongshanDeckTutorialEntryData
        {
            title = "新条目",
            lead = "请填写导语",
            description = "请填写教程正文。",
            highlights = new List<string>()
        });
        selectedTutorialEntryIndex = category.entries.Count - 1;
        RefreshEditorsFromDraft();
    }

    private void DeleteTutorialEntry()
    {
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        if (category == null || category.entries.Count == 0)
        {
            return;
        }

        category.entries.RemoveAt(selectedTutorialEntryIndex);
        selectedTutorialEntryIndex = Mathf.Clamp(selectedTutorialEntryIndex, 0, Mathf.Max(0, category.entries.Count - 1));
        RefreshEditorsFromDraft();
    }

    private void MoveTutorialEntry(int direction)
    {
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        if (category == null || category.entries.Count <= 1)
        {
            return;
        }

        CommitEditorsToDraft();
        int target = Mathf.Clamp(selectedTutorialEntryIndex + direction, 0, category.entries.Count - 1);
        if (target == selectedTutorialEntryIndex)
        {
            return;
        }

        ZhongshanDeckTutorialEntryData entry = category.entries[selectedTutorialEntryIndex];
        category.entries.RemoveAt(selectedTutorialEntryIndex);
        category.entries.Insert(target, entry);
        selectedTutorialEntryIndex = target;
        RefreshEditorsFromDraft();
    }

    private void OnCreditsDropdownChanged(int index)
    {
        CommitEditorsToDraft();
        selectedCreditsIndex = Mathf.Clamp(index, 0, Mathf.Max(0, editingContent.credits.entries.Count - 1));
        RefreshCreditsDropdown();
        LoadSelectedCreditsEntryIntoEditors();
    }

    private void OnChangelogDropdownChanged(int index)
    {
        CommitEditorsToDraft();
        selectedChangelogIndex = Mathf.Clamp(index, 0, Mathf.Max(0, editingContent.changelog.sections.Count - 1));
        RefreshChangelogDropdown();
        LoadSelectedChangelogSectionIntoEditors();
    }

    private void OnTutorialCategoryChanged(int index)
    {
        CommitEditorsToDraft();
        selectedTutorialCategoryIndex = Mathf.Clamp(index, 0, Mathf.Max(0, editingContent.tutorial.categories.Count - 1));
        selectedTutorialEntryIndex = 0;
        RefreshTutorialCategoryDropdown();
        RefreshTutorialEntryDropdown();
        LoadSelectedTutorialIntoEditors();
    }

    private void OnTutorialEntryChanged(int index)
    {
        CommitEditorsToDraft();
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        int count = category != null ? category.entries.Count : 0;
        selectedTutorialEntryIndex = Mathf.Clamp(index, 0, Mathf.Max(0, count - 1));
        RefreshTutorialEntryDropdown();
        LoadSelectedTutorialIntoEditors();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private static string GetSafeText(TMP_InputField field, string fallback)
    {
        if (field == null)
        {
            return fallback;
        }

        string value = string.IsNullOrWhiteSpace(field.text) ? fallback : field.text.Trim();
        return value ?? fallback;
    }

    private static List<string> ParseCsvTags(string raw)
    {
        List<string> tags = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return tags;
        }

        string[] parts = raw.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            string item = parts[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(item))
            {
                tags.Add(item);
            }
        }

        return tags;
    }

    private static List<string> ParseMultiline(string raw)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return lines;
        }

        string[] split = raw.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < split.Length; i++)
        {
            string line = split[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private ZhongshanDeckTutorialCategoryData GetSelectedTutorialCategory()
    {
        if (editingContent == null || editingContent.tutorial == null)
        {
            return null;
        }

        if (selectedTutorialCategoryIndex < 0 || selectedTutorialCategoryIndex >= editingContent.tutorial.categories.Count)
        {
            return null;
        }

        return editingContent.tutorial.categories[selectedTutorialCategoryIndex];
    }

    private ZhongshanDeckTutorialEntryData GetSelectedTutorialEntry()
    {
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        if (category == null || selectedTutorialEntryIndex < 0 || selectedTutorialEntryIndex >= category.entries.Count)
        {
            return null;
        }

        return category.entries[selectedTutorialEntryIndex];
    }

    private int GetSelectedTutorialEntryCount()
    {
        ZhongshanDeckTutorialCategoryData category = GetSelectedTutorialCategory();
        return category != null ? category.entries.Count : 0;
    }

    private void UpsertHomepageMenuLabel(string actionId, string label)
    {
        for (int i = 0; i < editingContent.homepage.mainMenuItems.Count; i++)
        {
            ZhongshanDeckMenuActionLabel item = editingContent.homepage.mainMenuItems[i];
            if (item != null && item.actionId == actionId)
            {
                item.label = label;
                return;
            }
        }

        editingContent.homepage.mainMenuItems.Add(new ZhongshanDeckMenuActionLabel { actionId = actionId, label = label });
    }

    private void UpsertHomepageTopIcon(string actionId, string label, string tooltip)
    {
        for (int i = 0; i < editingContent.homepage.topIcons.Count; i++)
        {
            ZhongshanDeckIconEntry item = editingContent.homepage.topIcons[i];
            if (item != null && item.actionId == actionId)
            {
                item.label = label;
                item.tooltip = tooltip;
                return;
            }
        }

        editingContent.homepage.topIcons.Add(new ZhongshanDeckIconEntry { actionId = actionId, label = label, tooltip = tooltip });
    }

    private string GetHomepageMenuLabel(string actionId, string fallback)
    {
        for (int i = 0; i < editingContent.homepage.mainMenuItems.Count; i++)
        {
            ZhongshanDeckMenuActionLabel item = editingContent.homepage.mainMenuItems[i];
            if (item != null && item.actionId == actionId)
            {
                return string.IsNullOrWhiteSpace(item.label) ? fallback : item.label;
            }
        }

        return fallback;
    }

    private string GetHomepageIconLabel(string actionId, string fallback)
    {
        for (int i = 0; i < editingContent.homepage.topIcons.Count; i++)
        {
            ZhongshanDeckIconEntry item = editingContent.homepage.topIcons[i];
            if (item != null && item.actionId == actionId)
            {
                return string.IsNullOrWhiteSpace(item.label) ? fallback : item.label;
            }
        }

        return fallback;
    }

    private string GetHomepageIconTooltip(string actionId, string fallback)
    {
        for (int i = 0; i < editingContent.homepage.topIcons.Count; i++)
        {
            ZhongshanDeckIconEntry item = editingContent.homepage.topIcons[i];
            if (item != null && item.actionId == actionId)
            {
                return string.IsNullOrWhiteSpace(item.tooltip) ? fallback : item.tooltip;
            }
        }

        return fallback;
    }

    private GameObject CreatePanel(Transform parent, float minHeight)
    {
        GameObject panel = CreateUIElement("Panel", parent);
        panel.AddComponent<Image>().color = Panel;
        LayoutElement layout = panel.AddComponent<LayoutElement>();
        if (minHeight > 0f)
        {
            layout.minHeight = minHeight;
        }

        return panel;
    }

    private GameObject CreateRow(Transform parent, float height)
    {
        GameObject row = CreateUIElement("Row", parent);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        return row;
    }

    private TMP_InputField CreateLabeledInput(Transform parent, string label, string placeholder, float height)
    {
        GameObject row = CreateRow(parent, height);
        CreateLabel(row.transform, label, 13f, TextWhite, height - 2f, 88f);
        TMP_InputField field = CreateInputField(row.transform, placeholder, 0f, Mathf.Max(30f, height - 2f));
        LayoutElement le = field.gameObject.GetComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        return field;
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height)
    {
        GameObject root = CreateUIElement("InputField", parent);
        Image bg = root.AddComponent<Image>();
        bg.color = InputBg;
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
        }
        else
        {
            layout.flexibleWidth = 1f;
        }

        TMP_InputField input = root.AddComponent<TMP_InputField>();
        input.textViewport = root.GetComponent<RectTransform>();

        TextMeshProUGUI text = CreateText(root.transform, "Text", string.Empty, 13f, TextWhite, TextAlignmentOptions.MidlineLeft);
        text.margin = new Vector4(10f, 6f, 10f, 6f);
        StretchFull(text.rectTransform);

        TextMeshProUGUI placeholderText = CreateText(root.transform, "Placeholder", placeholder, 13f, TextGray, TextAlignmentOptions.MidlineLeft);
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.margin = new Vector4(10f, 6f, 10f, 6f);
        StretchFull(placeholderText.rectTransform);

        input.textComponent = text;
        input.placeholder = placeholderText;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.textViewport = root.GetComponent<RectTransform>();
        return input;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string[] options, float width, float height)
    {
        GameObject root = CreateUIElement("Dropdown", parent);
        Image bg = root.AddComponent<Image>();
        bg.color = InputBg;
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();

        TextMeshProUGUI caption = CreateText(root.transform, "Label", options.Length > 0 ? options[0] : string.Empty, 13f, TextWhite, TextAlignmentOptions.MidlineLeft);
        caption.margin = new Vector4(10f, 4f, 30f, 4f);
        StretchFull(caption.rectTransform);
        dropdown.captionText = caption;

        GameObject arrow = CreateUIElement("Arrow", root.transform);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(24f, 24f);
        arrowRect.anchoredPosition = new Vector2(-4f, 0f);
        CreateText(arrow.transform, "Text", "▼", 12f, TextGold, TextAlignmentOptions.Center);

        GameObject template = CreateUIElement("Template", root.transform);
        template.SetActive(false);
        Image templateBg = template.AddComponent<Image>();
        templateBg.color = Row;
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, 2f);
        templateRect.sizeDelta = new Vector2(0f, 150f);

        GameObject viewport = CreateUIElement("Viewport", template.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        GameObject item = CreateUIElement("Item", content.transform);
        Toggle itemToggle = item.AddComponent<Toggle>();
        item.AddComponent<Image>().color = Row;
        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.preferredHeight = 28f;
        TextMeshProUGUI itemLabel = CreateText(item.transform, "Item Label", "Option", 13f, TextWhite, TextAlignmentOptions.MidlineLeft);
        itemLabel.margin = new Vector4(10f, 2f, 10f, 2f);
        StretchFull(itemLabel.rectTransform);

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        dropdown.captionText = caption;
        dropdown.options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < options.Length; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(options[i]));
        }

        return dropdown;
    }

    private Button CreateButton(Transform parent, string label, float width, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject root = CreateUIElement(label + "Button", parent);
        Image bg = root.AddComponent<Image>();
        bg.color = color;
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 32f;

        Button button = root.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(root.transform, "Label", label, 13f, TextWhite, TextAlignmentOptions.Center);
        StretchFull(text.rectTransform);
        return button;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height, float width = 0f)
    {
        GameObject root = CreateUIElement("Label", parent);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
        }
        else
        {
            layout.flexibleWidth = 1f;
        }

        return CreateText(root.transform, "Text", text, fontSize, color, TextAlignmentOptions.MidlineLeft);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject root = CreateUIElement(name, parent);
        TextMeshProUGUI tmp = root.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        StretchFull(tmp.rectTransform);
        return tmp;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
