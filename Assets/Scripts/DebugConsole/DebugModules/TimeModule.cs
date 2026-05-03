#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color AccentColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.42f, 0.72f, 1f);
    private static readonly Color FieldColor = new Color(0.16f, 0.16f, 0.22f, 0.95f);

    private TMP_InputField yearInput;
    private TMP_InputField semesterInput;
    private TMP_InputField roundInput;
    private TextMeshProUGUI statusText;
    private Toggle startupOverrideToggle;
    private TMP_InputField startupMaxRoundsInput;
    private TMP_InputField startupYearInput;
    private TMP_InputField startupSemesterInput;
    private TMP_InputField startupRoundInput;
    private TextMeshProUGUI startupStatusText;

    public void Init(RectTransform parent)
    {
        Transform content = CreateScrollableContent(parent);

        CreateLabel(content, "时间控制", 20f, AccentColor, 34f);
        statusText = CreateLabel(content, "当前：--", 15f, TextColor, 28f);

        yearInput = CreateInputRow(content, "学年", "1", 1);
        semesterInput = CreateInputRow(content, "学期", "1", 1);
        roundInput = CreateInputRow(content, "回合", "1", 2);

        CreateButton(content, "跳转到指定时间", () =>
        {
            if (GameState.Instance == null)
            {
                return;
            }

            int year = ParseInput(yearInput, GameState.Instance.CurrentYear, 1, 4);
            int semester = ParseInput(semesterInput, GameState.Instance.CurrentSemester, 1, 2);
            int round = ParseInput(roundInput, GameState.Instance.CurrentRound, 1, GameState.MaxRoundsPerSemester);
            int month = GameState.CalculateMonth(semester, round);

            GameState.Instance.SetState(year, semester, round, month, GameState.Instance.Money, GameState.Instance.ActionPoints);
            DebugConsoleManager.Log("Time", $"Jumped to Y{year} S{semester} R{round}");
            Refresh();
        });

        CreateSpacer(content, 8f);
        CreateButton(content, "前进 1 回合", () =>
        {
            if (GameState.Instance == null)
            {
                return;
            }

            GameState.RoundAdvanceResult result = GameState.Instance.AdvanceRound();
            DebugConsoleManager.Log("Time", $"Advanced to {GameState.Instance.GetTimeDescription()}");
            Refresh();
            if (result == GameState.RoundAdvanceResult.Graduated)
            {
                DebugConsoleManager.Log("Time", "Reached graduation");
            }
        });

        CreateButton(content, "前进 5 回合", () =>
        {
            if (GameState.Instance == null)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                GameState.RoundAdvanceResult result = GameState.Instance.AdvanceRound();
                if (result == GameState.RoundAdvanceResult.Graduated)
                {
                    break;
                }
            }

            DebugConsoleManager.Log("Time", $"Fast-forwarded to {GameState.Instance.GetTimeDescription()}");
            Refresh();
        });

        CreateButton(content, "0.5 秒后前进 1 回合", () =>
        {
            if (GameState.Instance != null)
            {
                StartCoroutine(AdvanceAfterDelay());
            }
        });

        CreateSpacer(content, 18f);
        CreateLabel(content, "新游戏起始时间", 20f, AccentColor, 34f);
        startupStatusText = CreateLabel(content, "启动前配置：使用默认开局", 15f, TextColor, 28f);
        startupOverrideToggle = CreateToggleRow(content, "启用启动前时间覆盖", StartupFlowSettings.UseStartupTimeOverride, OnStartupOverrideChanged);
        startupMaxRoundsInput = CreateInputRow(content, "每学期回合数", StartupFlowSettings.SemesterRoundCount.ToString(), 2);
        startupYearInput = CreateInputRow(content, "起始学年", StartupFlowSettings.StartupYear.ToString(), 1);
        startupSemesterInput = CreateInputRow(content, "起始学期", StartupFlowSettings.StartupSemester.ToString(), 1);
        startupRoundInput = CreateInputRow(content, "起始回合", StartupFlowSettings.StartupRound.ToString(), 1);

        CreateButton(content, "保存为新游戏起始时间", () => SaveStartupTimeSettings(startupMaxRoundsInput));
        CreateButton(content, "用当前时间写入启动配置", CaptureCurrentTimeAsStartup);
        CreateButton(content, "清除启动前时间覆盖", ClearStartupTimeOverride);
    }

    public void Refresh()
    {
        if (GameState.Instance != null)
        {
            statusText.text = $"当前：{GameState.Instance.GetTimeDescription()} | 总回合 {GetTotalRounds(GameState.Instance.CurrentYear, GameState.Instance.CurrentSemester, GameState.Instance.CurrentRound)}";
            yearInput.SetTextWithoutNotify(GameState.Instance.CurrentYear.ToString());
            semesterInput.SetTextWithoutNotify(GameState.Instance.CurrentSemester.ToString());
            roundInput.SetTextWithoutNotify(GameState.Instance.CurrentRound.ToString());
        }

        if (startupOverrideToggle != null)
        {
            startupOverrideToggle.SetIsOnWithoutNotify(StartupFlowSettings.UseStartupTimeOverride);
            UpdateToggleVisual(startupOverrideToggle, StartupFlowSettings.UseStartupTimeOverride);
        }

        if (startupMaxRoundsInput != null)
        {
            startupMaxRoundsInput.SetTextWithoutNotify(StartupFlowSettings.SemesterRoundCount.ToString());
        }

        if (startupYearInput != null)
        {
            startupYearInput.SetTextWithoutNotify(StartupFlowSettings.StartupYear.ToString());
        }

        if (startupSemesterInput != null)
        {
            startupSemesterInput.SetTextWithoutNotify(StartupFlowSettings.StartupSemester.ToString());
        }

        if (startupRoundInput != null)
        {
            startupRoundInput.SetTextWithoutNotify(StartupFlowSettings.StartupRound.ToString());
        }

        RefreshStartupStatus();
    }

    private IEnumerator AdvanceAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        if (GameState.Instance == null)
        {
            yield break;
        }

        GameState.RoundAdvanceResult result = GameState.Instance.AdvanceRound();
        DebugConsoleManager.Log("Time", $"Delayed advance -> {GameState.Instance.GetTimeDescription()}");
        Refresh();
        if (result == GameState.RoundAdvanceResult.Graduated)
        {
            DebugConsoleManager.Log("Time", "Reached graduation");
        }
    }

    private int ParseInput(TMP_InputField input, int fallback, int min, int max)
    {
        if (input == null || !int.TryParse(input.text, out int value))
        {
            return fallback;
        }

        return Mathf.Clamp(value, min, max);
    }

    private int GetTotalRounds(int year, int semester, int round)
    {
        int roundsPerSemester = Mathf.Max(1, GameState.MaxRoundsPerSemester);
        int clampedYear = Mathf.Clamp(year, 1, 4);
        int clampedSemester = Mathf.Clamp(semester, 1, 2);
        int clampedRound = Mathf.Clamp(round, 1, roundsPerSemester);
        return (clampedYear - 1) * 2 * roundsPerSemester
            + (clampedSemester - 1) * roundsPerSemester
            + clampedRound;
    }

    private void OnStartupOverrideChanged(bool value)
    {
        StartupFlowSettings.UseStartupTimeOverride = value;
        UpdateToggleVisual(startupOverrideToggle, value);
        RefreshStartupStatus();
    }

    private void SaveStartupTimeSettings(TMP_InputField startupMaxRoundsInput)
    {
        int maxRounds = ParseInput(startupMaxRoundsInput, StartupFlowSettings.SemesterRoundCount, 3, 12);
        StartupFlowSettings.SemesterRoundCount = maxRounds;

        int year = ParseInput(startupYearInput, StartupFlowSettings.StartupYear, 1, 4);
        int semester = ParseInput(startupSemesterInput, StartupFlowSettings.StartupSemester, 1, 2);
        int round = ParseInput(startupRoundInput, StartupFlowSettings.StartupRound, 1, StartupFlowSettings.SemesterRoundCount);

        StartupFlowSettings.StartupYear = year;
        StartupFlowSettings.StartupSemester = semester;
        StartupFlowSettings.StartupRound = round;
        StartupFlowSettings.UseStartupTimeOverride = startupOverrideToggle != null && startupOverrideToggle.isOn;

        DebugConsoleManager.Log("Time", $"Saved startup time override: rounds={maxRounds}, Y{year} S{semester} R{round}");
        Refresh();
    }

    private void CaptureCurrentTimeAsStartup()
    {
        if (GameState.Instance == null)
        {
            RefreshStartupStatus("当前没有运行中的 GameState，无法提取当前时间。");
            return;
        }

        StartupFlowSettings.StartupYear = GameState.Instance.CurrentYear;
        StartupFlowSettings.StartupSemester = GameState.Instance.CurrentSemester;
        StartupFlowSettings.StartupRound = GameState.Instance.CurrentRound;
        StartupFlowSettings.UseStartupTimeOverride = true;

        DebugConsoleManager.Log("Time", $"Captured current time as startup override: Y{GameState.Instance.CurrentYear} S{GameState.Instance.CurrentSemester} R{GameState.Instance.CurrentRound}");
        Refresh();
    }

    private void ClearStartupTimeOverride()
    {
        StartupFlowSettings.UseStartupTimeOverride = false;
        DebugConsoleManager.Log("Time", "Cleared startup time override");
        Refresh();
    }

    private void RefreshStartupStatus(string overrideText = null)
    {
        if (startupStatusText == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(overrideText))
        {
            startupStatusText.text = overrideText;
            return;
        }

        if (!StartupFlowSettings.UseStartupTimeOverride)
        {
            startupStatusText.text = "启动前配置：使用默认开局";
            return;
        }

        int year = StartupFlowSettings.StartupYear;
        int semester = StartupFlowSettings.StartupSemester;
        int round = StartupFlowSettings.StartupRound;
        int month = GameState.CalculateMonth(semester, round);
        string semesterLabel = semester == 1 ? "上" : "下";
        startupStatusText.text = $"启动前配置：每学期{StartupFlowSettings.SemesterRoundCount}回合 | 大{year}{semesterLabel} · 回合{round} · {month}月";
    }

    private Transform CreateScrollableContent(RectTransform parent)
    {
        GameObject scrollObject = CreateRect("ScrollView", parent).gameObject;
        StretchFull(scrollObject.GetComponent<RectTransform>());

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewport = CreateRect("Viewport", scrollObject.transform).gameObject;
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject contentObject = CreateRect("Content", viewport.transform).gameObject;
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(20, 20, 18, 18);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;
        return contentObject.transform;
    }

    private TMP_InputField CreateInputRow(Transform parent, string label, string defaultValue, int charLimit)
    {
        GameObject rowObject = CreateRect($"{label}Row", parent).gameObject;
        rowObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TextMeshProUGUI labelText = CreateLabel(rowObject.transform, label, 15f, TextColor, 36f);
        labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

        TMP_InputField input = CreateInputField(rowObject.transform, 120f);
        input.characterLimit = charLimit;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.SetTextWithoutNotify(defaultValue);
        return input;
    }

    private TMP_InputField CreateInputField(Transform parent, float width)
    {
        GameObject inputObject = CreateRect("Input", parent).gameObject;
        LayoutElement layout = inputObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 34f;

        Image background = inputObject.AddComponent<Image>();
        background.color = FieldColor;

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();

        GameObject viewport = CreateRect("Viewport", inputObject.transform).gameObject;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 2f);
        viewportRect.offsetMax = new Vector2(-10f, -2f);
        viewport.AddComponent<RectMask2D>();

        TextMeshProUGUI text = CreateLabel(viewport.transform, string.Empty, 15f, TextColor, 28f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI placeholder = CreateLabel(viewport.transform, "0", 15f, new Color(0.55f, 0.55f, 0.6f), 28f);
        StretchFull(placeholder.rectTransform);
        placeholder.alignment = TextAlignmentOptions.Center;

        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private Toggle CreateToggleRow(Transform parent, string label, bool initialValue, UnityEngine.Events.UnityAction<bool> onChanged)
    {
        GameObject rowObject = CreateRect($"{label}Row", parent).gameObject;
        rowObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI labelText = CreateLabel(rowObject.transform, label, 15f, TextColor, 36f);
        labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;

        GameObject toggleObject = CreateRect("Toggle", rowObject.transform).gameObject;
        LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
        toggleLayout.preferredWidth = 56f;
        toggleLayout.preferredHeight = 24f;

        Image background = toggleObject.AddComponent<Image>();
        Toggle toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;

        GameObject checkmarkObject = CreateRect("Checkmark", toggleObject.transform).gameObject;
        RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0f, 0f);
        checkmarkRect.anchorMax = new Vector2(0f, 1f);
        checkmarkRect.pivot = new Vector2(0f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(24f, 0f);
        Image checkmark = checkmarkObject.AddComponent<Image>();
        checkmark.color = Color.white;
        toggle.graphic = checkmark;
        toggle.isOn = initialValue;
        UpdateToggleVisual(toggle, initialValue);
        toggle.onValueChanged.AddListener(value =>
        {
            UpdateToggleVisual(toggle, value);
            onChanged?.Invoke(value);
        });

        return toggle;
    }

    private void UpdateToggleVisual(Toggle toggle, bool value)
    {
        if (toggle == null)
        {
            return;
        }

        Image background = toggle.targetGraphic as Image;
        if (background != null)
        {
            background.color = value ? ButtonColor : FieldColor;
        }
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateRect($"Button_{label}", parent).gameObject;
        buttonObject.AddComponent<LayoutElement>().preferredHeight = 38f;

        Image background = buttonObject.AddComponent<Image>();
        background.color = ButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateLabel(buttonObject.transform, label, 14f, Color.white, 38f);
        StretchFull(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string textValue, float fontSize, Color color, float height)
    {
        GameObject textObject = CreateRect("Label", parent).gameObject;
        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        float resolvedHeight = Mathf.Max(height, fontSize + 14f);
        layout.preferredHeight = resolvedHeight;
        layout.minHeight = resolvedHeight;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(2f, 4f, 2f, 4f);
        text.extraPadding = true;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
        {
            text.font = FontManager.Instance.ChineseFont;
        }

        return text;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateRect("Spacer", parent).gameObject;
        spacer.AddComponent<LayoutElement>().preferredHeight = height;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject objectRef = new GameObject(name, typeof(RectTransform));
        objectRef.transform.SetParent(parent, false);
        return objectRef.GetComponent<RectTransform>();
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
