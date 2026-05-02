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
    }

    public void Refresh()
    {
        if (GameState.Instance == null)
        {
            return;
        }

        statusText.text = $"当前：{GameState.Instance.GetTimeDescription()}";
        yearInput.SetTextWithoutNotify(GameState.Instance.CurrentYear.ToString());
        semesterInput.SetTextWithoutNotify(GameState.Instance.CurrentSemester.ToString());
        roundInput.SetTextWithoutNotify(GameState.Instance.CurrentRound.ToString());
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
        textObject.AddComponent<LayoutElement>().preferredHeight = height;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;

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
