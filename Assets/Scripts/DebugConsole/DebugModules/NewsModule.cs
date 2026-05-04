#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewsModule : MonoBehaviour, IDebugModule
{
    private sealed class NewsItemEditor
    {
        public GameObject root;
        public TMP_Dropdown typeDropdown;
        public TMP_InputField titleInput;
        public TMP_InputField contentInput;
        public TMP_InputField authorInput;
        public TMP_InputField anonymousIdInput;
        public TMP_InputField likesInput;
        public TMP_InputField hotValueInput;
        public TMP_InputField hotTagInput;
        public TMP_InputField seriesIdInput;
        public TMP_InputField seriesOrderInput;
    }

    private sealed class RoundPickerButton
    {
        public int year;
        public int semester;
        public int round;
        public Image image;
    }

    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color Panel = new Color(0.11f, 0.11f, 0.17f, 0.92f);
    private static readonly Color Row = new Color(0.13f, 0.13f, 0.20f, 0.96f);
    private static readonly Color BtnBlue = new Color(0.23f, 0.43f, 0.72f, 1.0f);
    private static readonly Color BtnGreen = new Color(0.20f, 0.55f, 0.30f, 1.0f);
    private static readonly Color BtnRed = new Color(0.60f, 0.20f, 0.20f, 1.0f);
    private static readonly Color InputBg = new Color(0.09f, 0.09f, 0.14f, 1f);
    private static readonly string[] NewsTypeOptions = Enum.GetNames(typeof(NewsType));

    private TMP_InputField yearInput;
    private TMP_InputField semesterInput;
    private TMP_InputField roundInput;
    private TextMeshProUGUI statusText;
    private RectTransform previewContent;
    private RectTransform itemListContent;

    private readonly List<NewsItemEditor> itemEditors = new List<NewsItemEditor>();
    private readonly List<RoundPickerButton> roundPickerButtons = new List<RoundPickerButton>();

    public void Init(RectTransform parent)
    {
        GameObject scrollObj = CreateUIElement("NewsScrollView", parent);
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

        CreateLabel(content.transform, "每月新闻编辑", 18f, TextGold, 30f);
        statusText = CreateLabel(content.transform, "这里编辑的是月度新闻覆盖稿。保存后，该月份新闻会优先使用这里的内容。", 13f, TextGray, 44f);
        statusText.enableWordWrapping = true;

        BuildControlPanel(content.transform);
        BuildPreviewPanel(content.transform);
        BuildListPanel(content.transform);

        UseCurrentRound();
        LoadOverrideOrGenerated();
    }

    public void Refresh()
    {
        if (GameState.Instance != null && string.IsNullOrWhiteSpace(yearInput.text))
        {
            UseCurrentRound();
        }
    }

    private void BuildControlPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 320f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        CreateLabel(panel.transform, "选择月份并载入草稿 / 默认生成稿", 15f, TextGold, 24f);

        GameObject timeRow = CreateRow(panel.transform, 34f);
        CreateLabel(timeRow.transform, "学年", 14f, TextWhite, 30f, 44f);
        yearInput = CreateInputField(timeRow.transform, "1", 64f, 30f, "1");
        CreateLabel(timeRow.transform, "学期", 14f, TextWhite, 30f, 44f);
        semesterInput = CreateInputField(timeRow.transform, "1", 64f, 30f, "1");
        CreateLabel(timeRow.transform, "回合", 14f, TextWhite, 30f, 44f);
        roundInput = CreateInputField(timeRow.transform, "1", 64f, 30f, "1");

        GameObject actionRow = CreateRow(panel.transform, 36f);
        CreateButton(actionRow.transform, "使用当前时间", 110f, BtnBlue, () =>
        {
            UseCurrentRound();
            LoadOverrideOrGenerated();
        });
        CreateButton(actionRow.transform, "载入草稿", 96f, BtnBlue, LoadOverrideOrGenerated);
        CreateButton(actionRow.transform, "导入默认稿", 110f, BtnGreen, ImportGeneratedNews);
        CreateButton(actionRow.transform, "新增条目", 96f, BtnGreen, () => AddItemEditor(new NewsItem(NewsType.Headline, "新头条", "请填写本月新闻内容。")));

        GameObject saveRow = CreateRow(panel.transform, 36f);
        CreateButton(saveRow.transform, "保存本月覆盖", 120f, BtnGreen, SaveOverride);
        CreateButton(saveRow.transform, "删除本月覆盖", 120f, BtnRed, DeleteOverride);

        CreateLabel(panel.transform, "点选回合", 14f, TextGold, 22f);
        BuildRoundPicker(panel.transform);
    }

    private void BuildListPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 0f);
        LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
        panelLayout.flexibleHeight = 1f;
        panelLayout.minHeight = 360f;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        CreateLabel(panel.transform, "新闻条目", 15f, TextGold, 24f);

        GameObject scrollObj = CreateUIElement("ItemScrollView", panel.transform);
        LayoutElement scrollLayout = scrollObj.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 320f;

        Image bg = scrollObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.09f, 0.95f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        itemListContent = content.GetComponent<RectTransform>();
        itemListContent.anchorMin = new Vector2(0f, 1f);
        itemListContent.anchorMax = new Vector2(1f, 1f);
        itemListContent.pivot = new Vector2(0.5f, 1f);
        itemListContent.offsetMin = new Vector2(8f, 0f);
        itemListContent.offsetMax = new Vector2(-8f, 0f);

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
        scrollRect.content = itemListContent;
    }

    private void BuildPreviewPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, 0f);
        LayoutElement panelLayout = panel.GetComponent<LayoutElement>();
        panelLayout.minHeight = 240f;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        GameObject titleRow = CreateRow(panel.transform, 30f);
        CreateLabel(titleRow.transform, "排版预览", 15f, TextGold, 28f, 80f);
        CreateButton(titleRow.transform, "刷新预览", 96f, BtnBlue, RebuildPreview);

        GameObject scrollObj = CreateUIElement("PreviewScrollView", panel.transform);
        LayoutElement scrollLayout = scrollObj.AddComponent<LayoutElement>();
        scrollLayout.minHeight = 180f;

        Image bg = scrollObj.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.06f, 0.04f, 0.96f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateUIElement("Viewport", scrollObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        previewContent = content.GetComponent<RectTransform>();
        previewContent.anchorMin = new Vector2(0f, 1f);
        previewContent.anchorMax = new Vector2(1f, 1f);
        previewContent.pivot = new Vector2(0.5f, 1f);
        previewContent.offsetMin = new Vector2(10f, 0f);
        previewContent.offsetMax = new Vector2(-10f, 0f);

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
        scrollRect.content = previewContent;
    }

    private void UseCurrentRound()
    {
        int year = GameState.Instance != null ? GameState.Instance.CurrentYear : 1;
        int semester = GameState.Instance != null ? GameState.Instance.CurrentSemester : 1;
        int round = GameState.Instance != null ? GameState.Instance.CurrentRound : 1;

        yearInput.text = year.ToString();
        semesterInput.text = semester.ToString();
        roundInput.text = round.ToString();
    }

    private void LoadOverrideOrGenerated()
    {
        int year = ParseInt(yearInput, 1, 1, 4);
        int semester = ParseInt(semesterInput, 1, 1, 2);
        int round = ParseInt(roundInput, 1, 1, 5);

        if (ZhongshanDeckToolStateBridge.TryGetMonthlyNewsOverride(year, semester, round, out ZhongshanDeckNewsRoundEntry entry) &&
            entry != null &&
            entry.items != null &&
            entry.items.Count > 0)
        {
            RebuildEditors(entry.items);
            RefreshRoundPickerButtons();
            RebuildPreview();
            SetStatus($"已载入 Y{year} S{semester} R{round} 的新闻覆盖稿。");
            return;
        }

        ImportGeneratedNews();
    }

    private void ImportGeneratedNews()
    {
        int year = ParseInt(yearInput, 1, 1, 4);
        int semester = ParseInt(semesterInput, 1, 1, 2);
        int round = ParseInt(roundInput, 1, 1, 5);

        List<NewsItem> generated = null;
        if (NewsSystem.Instance != null)
        {
            generated = NewsSystem.Instance.BuildEditableNewsForRound(year, semester, round, true);
        }

        if (generated == null || generated.Count == 0)
        {
            generated = new List<NewsItem>
            {
                new NewsItem(NewsType.Headline, "新头条", "请填写本月头条。"),
                new NewsItem(NewsType.Notice, "【通知】", "请填写本月通知。")
            };
        }

        RebuildEditors(generated);
        RefreshRoundPickerButtons();
        RebuildPreview();
        SetStatus($"已导入 Y{year} S{semester} R{round} 的默认生成稿。");
    }

    private void SaveOverride()
    {
        int year = ParseInt(yearInput, 1, 1, 4);
        int semester = ParseInt(semesterInput, 1, 1, 2);
        int round = ParseInt(roundInput, 1, 1, 5);

        ZhongshanDeckNewsRoundEntry entry = new ZhongshanDeckNewsRoundEntry
        {
            year = year,
            semester = semester,
            round = round,
            items = CollectItems()
        };

        if (entry.items.Count == 0)
        {
            SetStatus("至少保留一条新闻再保存。", true);
            return;
        }

        ZhongshanDeckToolStateBridge.SaveMonthlyNewsOverride(entry);
        RefreshRoundPickerButtons();
        RebuildPreview();
        SetStatus($"已保存 Y{year} S{semester} R{round} 的新闻覆盖稿，共 {entry.items.Count} 条。");
        DebugConsoleManager.Log("News", $"Saved monthly news override Y{year} S{semester} R{round}");
    }

    private void DeleteOverride()
    {
        int year = ParseInt(yearInput, 1, 1, 4);
        int semester = ParseInt(semesterInput, 1, 1, 2);
        int round = ParseInt(roundInput, 1, 1, 5);

        bool deleted = ZhongshanDeckToolStateBridge.DeleteMonthlyNewsOverride(year, semester, round);
        if (deleted)
        {
            DebugConsoleManager.Log("News", $"Deleted monthly news override Y{year} S{semester} R{round}");
            ImportGeneratedNews();
            SetStatus($"已删除 Y{year} S{semester} R{round} 的新闻覆盖稿。");
            return;
        }

        SetStatus("该月份还没有保存过覆盖稿。", true);
    }

    private void RebuildPreview()
    {
        if (previewContent == null)
        {
            return;
        }

        for (int i = previewContent.childCount - 1; i >= 0; i--)
        {
            Destroy(previewContent.GetChild(i).gameObject);
        }

        List<NewsItem> items = CollectItems();
        if (items.Count == 0)
        {
            CreateLabel(previewContent, "暂无可预览的新闻条目。", 13f, TextGray, 28f);
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            BuildPreviewCard(items[i], i + 1);
        }
    }

    private void BuildRoundPicker(Transform parent)
    {
        for (int year = 1; year <= 4; year++)
        {
            CreateLabel(parent, $"大{ToChineseYear(year)}", 13f, TextWhite, 20f);
            for (int semester = 1; semester <= 2; semester++)
            {
                GameObject row = CreateRow(parent, 30f);
                CreateLabel(row.transform, semester == 1 ? "上" : "下", 12f, TextGray, 28f, 26f);
                for (int round = 1; round <= 5; round++)
                {
                    int capturedYear = year;
                    int capturedSemester = semester;
                    int capturedRound = round;
                    Button button = CreateButton(
                        row.transform,
                        $"R{round}",
                        46f,
                        GetRoundButtonColor(year, semester, round),
                        () =>
                        {
                            yearInput.text = capturedYear.ToString();
                            semesterInput.text = capturedSemester.ToString();
                            roundInput.text = capturedRound.ToString();
                            LoadOverrideOrGenerated();
                        });
                    roundPickerButtons.Add(new RoundPickerButton
                    {
                        year = year,
                        semester = semester,
                        round = round,
                        image = button.GetComponent<Image>()
                    });
                }
            }
        }

        RefreshRoundPickerButtons();
    }

    private Color GetRoundButtonColor(int year, int semester, int round)
    {
        bool isSelected = ParseInt(yearInput, 1, 1, 4) == year &&
                          ParseInt(semesterInput, 1, 1, 2) == semester &&
                          ParseInt(roundInput, 1, 1, 5) == round;
        bool isCurrent = GameState.Instance != null &&
                         GameState.Instance.CurrentYear == year &&
                         GameState.Instance.CurrentSemester == semester &&
                         GameState.Instance.CurrentRound == round;
        bool hasOverride = ZhongshanDeckToolStateBridge.TryGetMonthlyNewsOverride(year, semester, round, out _);

        if (isSelected) return new Color(0.85f, 0.63f, 0.18f, 1f);
        if (hasOverride) return new Color(0.25f, 0.56f, 0.36f, 1f);
        if (isCurrent) return new Color(0.28f, 0.46f, 0.76f, 1f);
        return new Color(0.22f, 0.22f, 0.28f, 1f);
    }

    private string ToChineseYear(int year)
    {
        switch (year)
        {
            case 1: return "一";
            case 2: return "二";
            case 3: return "三";
            case 4: return "四";
            default: return year.ToString();
        }
    }

    private void RefreshRoundPickerButtons()
    {
        for (int i = 0; i < roundPickerButtons.Count; i++)
        {
            RoundPickerButton button = roundPickerButtons[i];
            if (button?.image != null)
            {
                button.image.color = GetRoundButtonColor(button.year, button.semester, button.round);
            }
        }
    }

    private List<NewsItem> CollectItems()
    {
        List<NewsItem> items = new List<NewsItem>();
        for (int i = 0; i < itemEditors.Count; i++)
        {
            NewsItemEditor editor = itemEditors[i];
            if (editor == null)
            {
                continue;
            }

            NewsItem item = new NewsItem
            {
                type = (NewsType)Mathf.Clamp(editor.typeDropdown != null ? editor.typeDropdown.value : 0, 0, NewsTypeOptions.Length - 1),
                title = SafeText(editor.titleInput),
                content = SafeText(editor.contentInput),
                author = SafeText(editor.authorInput),
                anonymousId = SafeText(editor.anonymousIdInput),
                likes = ParseInt(editor.likesInput, 0, 0, 999999),
                hotValue = ParseFloat(editor.hotValueInput, 0f),
                hotTag = SafeText(editor.hotTagInput),
                seriesId = SafeText(editor.seriesIdInput),
                seriesOrder = ParseInt(editor.seriesOrderInput, 0, 0, 999)
            };

            if (string.IsNullOrWhiteSpace(item.title) && string.IsNullOrWhiteSpace(item.content))
            {
                continue;
            }

            items.Add(item);
        }

        return items;
    }

    private void RebuildEditors(List<NewsItem> items)
    {
        for (int i = 0; i < itemEditors.Count; i++)
        {
            if (itemEditors[i].root != null)
            {
                Destroy(itemEditors[i].root);
            }
        }
        itemEditors.Clear();

        if (items == null || items.Count == 0)
        {
            AddItemEditor(new NewsItem(NewsType.Headline, "新头条", "请填写本月新闻内容。"));
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            AddItemEditor(items[i]);
        }
    }

    private void AddItemEditor(NewsItem item)
    {
        if (itemListContent == null)
        {
            return;
        }

        GameObject card = CreateUIElement($"NewsItem_{itemEditors.Count}", itemListContent);
        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.preferredHeight = 250f;

        Image bg = card.AddComponent<Image>();
        bg.color = Row;

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        GameObject topRow = CreateRow(card.transform, 30f);
        CreateLabel(topRow.transform, $"条目 {itemEditors.Count + 1}", 14f, TextGold, 28f, 70f);
        TMP_Dropdown typeDropdown = CreateDropdown(topRow.transform, NewsTypeOptions, 140f, 28f);
        typeDropdown.value = Mathf.Clamp((int)item.type, 0, NewsTypeOptions.Length - 1);
        typeDropdown.RefreshShownValue();
        Button upButton = CreateButton(topRow.transform, "上移", 54f, BtnBlue, null);
        Button downButton = CreateButton(topRow.transform, "下移", 54f, BtnBlue, null);
        Button removeButton = CreateButton(topRow.transform, "删除条目", 88f, BtnRed, null);

        TMP_InputField titleInput = CreateInputField(card.transform, "标题", 0f, 30f, item.title);
        SetFlexibleWidth(titleInput.gameObject);
        TMP_InputField contentInput = CreateMultilineInput(card.transform, "内容", 92f, item.content);

        GameObject metaRow1 = CreateRow(card.transform, 30f);
        TMP_InputField authorInput = CreateInputField(metaRow1.transform, "作者", 140f, 28f, item.author);
        TMP_InputField anonymousIdInput = CreateInputField(metaRow1.transform, "匿名ID", 140f, 28f, item.anonymousId);
        TMP_InputField hotTagInput = CreateInputField(metaRow1.transform, "热搜标签", 100f, 28f, item.hotTag);
        TMP_InputField hotValueInput = CreateInputField(metaRow1.transform, "热度", 84f, 28f, item.hotValue > 0f ? item.hotValue.ToString("0.0") : string.Empty);

        GameObject metaRow2 = CreateRow(card.transform, 30f);
        TMP_InputField likesInput = CreateInputField(metaRow2.transform, "点赞", 90f, 28f, item.likes > 0 ? item.likes.ToString() : string.Empty);
        TMP_InputField seriesIdInput = CreateInputField(metaRow2.transform, "连载ID", 140f, 28f, item.seriesId);
        TMP_InputField seriesOrderInput = CreateInputField(metaRow2.transform, "连载序号", 100f, 28f, item.seriesOrder > 0 ? item.seriesOrder.ToString() : string.Empty);

        NewsItemEditor editor = new NewsItemEditor
        {
            root = card,
            typeDropdown = typeDropdown,
            titleInput = titleInput,
            contentInput = contentInput,
            authorInput = authorInput,
            anonymousIdInput = anonymousIdInput,
            likesInput = likesInput,
            hotValueInput = hotValueInput,
            hotTagInput = hotTagInput,
            seriesIdInput = seriesIdInput,
            seriesOrderInput = seriesOrderInput
        };

        removeButton.onClick.AddListener(() => RemoveItemEditor(editor));
        upButton.onClick.AddListener(() => MoveItemEditor(editor, -1));
        downButton.onClick.AddListener(() => MoveItemEditor(editor, 1));
        BindPreviewRefresh(typeDropdown);
        BindPreviewRefresh(titleInput);
        BindPreviewRefresh(contentInput);
        BindPreviewRefresh(authorInput);
        BindPreviewRefresh(anonymousIdInput);
        BindPreviewRefresh(likesInput);
        BindPreviewRefresh(hotValueInput);
        BindPreviewRefresh(hotTagInput);
        BindPreviewRefresh(seriesIdInput);
        BindPreviewRefresh(seriesOrderInput);
        itemEditors.Add(editor);
    }

    private void RemoveItemEditor(NewsItemEditor editor)
    {
        if (editor == null)
        {
            return;
        }

        itemEditors.Remove(editor);
        if (editor.root != null)
        {
            Destroy(editor.root);
        }

        RebuildPreview();
        SetStatus("已移除一条新闻条目。");
    }

    private void MoveItemEditor(NewsItemEditor editor, int direction)
    {
        int index = itemEditors.IndexOf(editor);
        if (index < 0)
        {
            return;
        }

        int targetIndex = Mathf.Clamp(index + direction, 0, itemEditors.Count - 1);
        if (targetIndex == index)
        {
            return;
        }

        itemEditors.RemoveAt(index);
        itemEditors.Insert(targetIndex, editor);
        editor.root.transform.SetSiblingIndex(targetIndex);
        RebuildPreview();
    }

    private void BindPreviewRefresh(TMP_InputField field)
    {
        if (field != null)
        {
            field.onValueChanged.AddListener(_ => RebuildPreview());
        }
    }

    private void BindPreviewRefresh(TMP_Dropdown dropdown)
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(_ => RebuildPreview());
        }
    }

    private void SetStatus(string text, bool isError = false)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = text;
        statusText.color = isError ? new Color(1f, 0.56f, 0.56f) : TextGray;
    }

    private string SafeText(TMP_InputField field)
    {
        return field != null ? (field.text ?? string.Empty).Trim() : string.Empty;
    }

    private int ParseInt(TMP_InputField field, int fallback, int min, int max)
    {
        int value;
        if (!int.TryParse(SafeText(field), out value))
        {
            value = fallback;
        }

        return Mathf.Clamp(value, min, max);
    }

    private float ParseFloat(TMP_InputField field, float fallback)
    {
        float value;
        if (!float.TryParse(SafeText(field), out value))
        {
            value = fallback;
        }

        return value;
    }

    private GameObject CreatePanel(Transform parent, float preferredHeight)
    {
        GameObject panel = CreateUIElement("Panel", parent);
        Image bg = panel.AddComponent<Image>();
        bg.color = Panel;
        LayoutElement layout = panel.AddComponent<LayoutElement>();
        if (preferredHeight > 0f)
        {
            layout.preferredHeight = preferredHeight;
        }
        return panel;
    }

    private GameObject CreateRow(Transform parent, float height)
    {
        GameObject row = CreateUIElement("Row", parent);
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 8f;
        group.childAlignment = TextAnchor.MiddleLeft;
        group.childControlWidth = false;
        group.childControlHeight = false;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        return row;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Color color, float height, float width = 0f)
    {
        GameObject obj = CreateUIElement("Label", parent);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
        }

        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = true;
        return label;
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholderText, float width, float height, string value = "")
    {
        GameObject inputObject = CreateUIElement("Input", parent);
        RectTransform rt = inputObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement layout = inputObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
        }

        Image bg = inputObject.AddComponent<Image>();
        bg.color = InputBg;

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        input.textViewport = CreateViewport(inputObject.transform);
        input.textComponent = CreateInputText(input.textViewport.transform, TextWhite);
        input.placeholder = CreatePlaceholder(input.textViewport.transform, placeholderText);
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.text = value ?? string.Empty;
        return input;
    }

    private TMP_InputField CreateMultilineInput(Transform parent, string placeholderText, float height, string value = "")
    {
        TMP_InputField input = CreateInputField(parent, placeholderText, 0f, height, value);
        LayoutElement layout = input.GetComponent<LayoutElement>();
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

        LayoutElement layout = dropdownObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

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
        templateObj.SetActive(false);
        Image bg = templateObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.16f, 0.98f);
        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        RectTransform templateRT = templateObj.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0f, 0f);
        templateRT.anchorMax = new Vector2(1f, 0f);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.anchoredPosition = new Vector2(0f, 2f);
        templateRT.sizeDelta = new Vector2(0f, 160f);

        GameObject viewport = CreateUIElement("Viewport", templateObj.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        GameObject item = CreateUIElement("Item", content.transform);
        Toggle toggle = item.AddComponent<Toggle>();
        Image itemBg = item.AddComponent<Image>();
        itemBg.color = new Color(0.16f, 0.16f, 0.22f, 1f);
        toggle.targetGraphic = itemBg;

        GameObject itemCheck = CreateUIElement("ItemCheckmark", item.transform);
        Image itemCheckImage = itemCheck.AddComponent<Image>();
        itemCheckImage.color = new Color(0.24f, 0.54f, 0.76f, 1f);
        toggle.graphic = itemCheckImage;

        TextMeshProUGUI itemLabel = CreateInputText(item.transform, TextWhite);
        itemLabel.name = "Item Label";
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabel.margin = new Vector4(20f, 2f, 4f, 2f);

        RectTransform itemRT = item.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0f, 28f);
        return templateRT;
    }

    private TextMeshProUGUI CreateDropdownCaption(Transform parent)
    {
        TextMeshProUGUI text = CreateInputText(parent, TextWhite);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(10f, 0f, 24f, 0f);
        StretchFull(text.rectTransform);
        return text;
    }

    private TextMeshProUGUI BuildDropdownItemTemplate(RectTransform template)
    {
        return template.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private List<TMP_Dropdown.OptionData> BuildDropdownOptions(string[] options)
    {
        List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < options.Length; i++)
        {
            list.Add(new TMP_Dropdown.OptionData(options[i]));
        }

        return list;
    }

    private RectTransform CreateViewport(Transform parent)
    {
        GameObject viewportObject = CreateUIElement("Viewport", parent);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(10f, 4f);
        viewport.offsetMax = new Vector2(-10f, -4f);
        viewportObject.AddComponent<RectMask2D>();
        return viewport;
    }

    private TextMeshProUGUI CreateInputText(Transform parent, Color color)
    {
        GameObject textObject = CreateUIElement("Text", parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 13f;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        StretchFull(text.rectTransform);
        return text;
    }

    private TextMeshProUGUI CreatePlaceholder(Transform parent, string value)
    {
        TextMeshProUGUI text = CreateInputText(parent, TextGray);
        text.text = value;
        return text;
    }

    private Button CreateButton(Transform parent, string label, float width, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUIElement("Button", parent);
        RectTransform rt = buttonObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 28f);

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 28f;

        Image background = buttonObject.AddComponent<Image>();
        background.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 12f, Color.white, 28f);
        text.alignment = TextAlignmentOptions.Center;
        StretchFull(text.rectTransform);
        return button;
    }

    private void BuildPreviewCard(NewsItem item, int order)
    {
        GameObject card = CreateUIElement($"Preview_{order}", previewContent);
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.preferredHeight = GetPreviewHeight(item);

        Image bg = card.AddComponent<Image>();
        bg.color = GetPreviewColor(item.type);

        VerticalLayoutGroup group = card.AddComponent<VerticalLayoutGroup>();
        group.spacing = 4f;
        group.padding = new RectOffset(12, 12, 10, 10);
        group.childControlWidth = true;
        group.childControlHeight = false;
        group.childForceExpandHeight = false;

        CreateLabel(card.transform, $"{order}. {GetPreviewHeader(item)}", 13f, new Color(0.35f, 0.25f, 0.05f), 22f);
        if (!string.IsNullOrWhiteSpace(item.title))
        {
            TextMeshProUGUI title = CreateLabel(card.transform, item.title, item.type == NewsType.Headline ? 18f : 14f, new Color(0.12f, 0.12f, 0.12f), item.type == NewsType.Headline ? 42f : 28f);
            title.fontStyle = FontStyles.Bold;
        }

        TextMeshProUGUI body = CreateLabel(card.transform, string.IsNullOrWhiteSpace(item.content) ? "暂无内容" : item.content, 13f, new Color(0.22f, 0.22f, 0.22f), Mathf.Max(46f, Mathf.Min(120f, 24f + (item.content ?? string.Empty).Length * 0.45f)));
        body.alignment = TextAlignmentOptions.TopLeft;

        string meta = BuildPreviewMeta(item);
        if (!string.IsNullOrWhiteSpace(meta))
        {
            CreateLabel(card.transform, meta, 12f, new Color(0.4f, 0.4f, 0.4f), 22f);
        }
    }

    private float GetPreviewHeight(NewsItem item)
    {
        int length = (item?.content ?? string.Empty).Length;
        return Mathf.Clamp(90f + length * 0.4f, 110f, 220f);
    }

    private Color GetPreviewColor(NewsType type)
    {
        switch (type)
        {
            case NewsType.Headline: return new Color(0.98f, 0.95f, 0.88f, 1f);
            case NewsType.Trending: return new Color(0.99f, 0.96f, 0.90f, 1f);
            case NewsType.Gossip: return new Color(0.94f, 0.95f, 0.99f, 1f);
            case NewsType.Notice: return new Color(1f, 0.94f, 0.94f, 1f);
            case NewsType.Ad: return new Color(0.93f, 0.93f, 0.93f, 1f);
            default: return new Color(0.96f, 0.96f, 0.96f, 1f);
        }
    }

    private string GetPreviewHeader(NewsItem item)
    {
        switch (item.type)
        {
            case NewsType.Headline: return "头条";
            case NewsType.Trending: return "热搜";
            case NewsType.Gossip: return "树洞";
            case NewsType.Notice: return "通知";
            case NewsType.Ad: return "推广";
            default: return "新闻";
        }
    }

    private string BuildPreviewMeta(NewsItem item)
    {
        List<string> parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.author))
        {
            parts.Add(item.author);
        }
        if (!string.IsNullOrWhiteSpace(item.anonymousId))
        {
            parts.Add(item.anonymousId);
        }
        if (item.likes > 0)
        {
            parts.Add($"{item.likes}赞");
        }
        if (item.hotValue > 0f)
        {
            parts.Add($"{item.hotValue:0.0}万");
        }
        if (!string.IsNullOrWhiteSpace(item.hotTag))
        {
            parts.Add($"标签 {item.hotTag}");
        }
        if (!string.IsNullOrWhiteSpace(item.seriesId))
        {
            parts.Add($"连载 {item.seriesId}#{item.seriesOrder}");
        }

        return string.Join("  |  ", parts);
    }

    private void SetFlexibleWidth(GameObject target)
    {
        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = target.AddComponent<LayoutElement>();
        }

        layout.flexibleWidth = 1f;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
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
