#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 时间调试模块 —— 学年/学期/回合跳转 + 快进功能
/// </summary>
public class TimeModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color BtnColor  = new Color(0.20f, 0.35f, 0.60f, 1.0f);

    private TMP_InputField yearInput;
    private TMP_InputField semesterInput;
    private TMP_InputField roundInput;
    private TextMeshProUGUI statusText;

    public void Init(RectTransform parent)
    {
        // 滚动容器
        GameObject scrollObj = CreateUIElement("ScrollView", parent);
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
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // 标题
        CreateLabel(content.transform, "— 时间控制 —", 18f, TextGold, 30f);

        // 当前状态
        statusText = CreateLabel(content.transform, "当前: 加载中...", 16f, TextWhite, 28f);

        // 学年输入 (1-4)
        yearInput = CreateInputRow(content.transform, "学年 (1-4)", "1");
        yearInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        yearInput.characterLimit = 1;

        // 学期输入 (1-2)
        semesterInput = CreateInputRow(content.transform, "学期 (1-2)", "1");
        semesterInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        semesterInput.characterLimit = 1;

        // 回合输入 (1-5)
        roundInput = CreateInputRow(content.transform, "回合 (1-5)", "1");
        roundInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        roundInput.characterLimit = 2;

        // 跳转按钮
        CreateButton(content.transform, "跳转", () =>
        {
            if (GameState.Instance == null) return;

            int year = 1, semester = 1, round = 1;
            if (yearInput != null) int.TryParse(yearInput.text, out year);
            if (semesterInput != null) int.TryParse(semesterInput.text, out semester);
            if (roundInput != null) int.TryParse(roundInput.text, out round);

            int month = GameState.CalculateMonth(Mathf.Clamp(semester, 1, 2), Mathf.Clamp(round, 1, GameState.MaxRoundsPerSemester));
            GameState.Instance.SetState(year, semester, round, month,
                GameState.Instance.Money, GameState.Instance.ActionPoints);

            DebugConsoleManager.Log("时间", $"已跳转: 大{year}{(semester == 1 ? "上" : "下")} 回合{round}");
            Refresh();
        });

        // 分割线
        CreateSeparator(content.transform);

        // 快进区域
        CreateLabel(content.transform, "— 快进 —", 16f, TextGold, 26f);

        // 普通快进
        CreateButton(content.transform, "普通快进 (1回合, 0.5s延迟)", () =>
        {
            if (GameState.Instance == null) return;
            StartCoroutine(AdvanceWithDelay(1, 0.5f));
        });

        // 加速快进
        CreateButton(content.transform, "加速快进 (5回合, 瞬时)", () =>
        {
            if (GameState.Instance == null) return;
            for (int i = 0; i < 5; i++)
            {
                var result = GameState.Instance.AdvanceRound();
                if (result == GameState.RoundAdvanceResult.Graduated)
                {
                    DebugConsoleManager.Log("时间", "已毕业，无法继续快进");
                    break;
                }
            }
            DebugConsoleManager.Log("时间", "已加速快进5回合");
            Refresh();
        });

        // 瞬移
        CreateButton(content.transform, "瞬移到指定时间", () =>
        {
            if (GameState.Instance == null) return;

            int targetYear = 1, targetSemester = 1, targetRound = 1;
            if (yearInput != null) int.TryParse(yearInput.text, out targetYear);
            if (semesterInput != null) int.TryParse(semesterInput.text, out targetSemester);
            if (roundInput != null) int.TryParse(roundInput.text, out targetRound);

            targetYear = Mathf.Clamp(targetYear, 1, 4);
            targetSemester = Mathf.Clamp(targetSemester, 1, 2);
            targetRound = Mathf.Clamp(targetRound, 1, GameState.MaxRoundsPerSemester);

            // 循环推进到目标时间
            int safetyCount = 0;
            while (safetyCount < 50)
            {
                if (GameState.Instance.CurrentYear == targetYear &&
                    GameState.Instance.CurrentSemester == targetSemester &&
                    GameState.Instance.CurrentRound == targetRound)
                    break;

                var result = GameState.Instance.AdvanceRound();
                if (result == GameState.RoundAdvanceResult.Graduated)
                {
                    DebugConsoleManager.Log("时间", "到达毕业，无法继续推进");
                    break;
                }
                safetyCount++;
            }

            DebugConsoleManager.Log("时间", $"已瞬移到: {GameState.Instance.GetTimeDescription()}");
            Refresh();
        });
    }

    public void Refresh()
    {
        if (GameState.Instance == null) return;

        if (statusText != null)
        {
            statusText.text = $"当前: {GameState.Instance.GetTimeDescription()}";
        }

        if (yearInput != null) yearInput.SetTextWithoutNotify(GameState.Instance.CurrentYear.ToString());
        if (semesterInput != null) semesterInput.SetTextWithoutNotify(GameState.Instance.CurrentSemester.ToString());
        if (roundInput != null) roundInput.SetTextWithoutNotify(GameState.Instance.CurrentRound.ToString());
    }

    private IEnumerator AdvanceWithDelay(int rounds, float delay)
    {
        for (int i = 0; i < rounds; i++)
        {
            yield return new WaitForSeconds(delay);
            var result = GameState.Instance.AdvanceRound();
            DebugConsoleManager.Log("时间", $"推进1回合 → {GameState.Instance.GetTimeDescription()}");
            Refresh();
            if (result == GameState.RoundAdvanceResult.Graduated)
            {
                DebugConsoleManager.Log("时间", "已毕业");
                break;
            }
        }
    }

    // ========== 工具方法 ==========

    private TMP_InputField CreateInputRow(Transform parent, string label, string defaultVal)
    {
        GameObject row = CreateUIElement(label + "Row", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 40f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateLabel(row.transform, label, 15f, TextWhite, 36f, 160f);
        return CreateInputField(row.transform, defaultVal, 120f, 32f);
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40f);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = BtnColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI txt = CreateLabel(btnObj.transform, label, 15f, TextWhite, 40f);
        txt.alignment = TextAlignmentOptions.Center;
        StretchFull(txt.GetComponent<RectTransform>());
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height)
    {
        GameObject inputObj = CreateUIElement("InputField", parent);
        RectTransform rt = inputObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        LayoutElement le = inputObj.AddComponent<LayoutElement>();
        le.preferredWidth = width;

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
        text.fontSize = 14f;
        text.color = TextWhite;
        text.alignment = TextAlignmentOptions.Left;
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            text.font = FontManager.Instance.ChineseFont;

        GameObject phObj = CreateUIElement("Placeholder", textArea.transform);
        StretchFull(phObj.GetComponent<RectTransform>());
        TextMeshProUGUI phText = phObj.AddComponent<TextMeshProUGUI>();
        phText.text = placeholder;
        phText.fontSize = 14f;
        phText.fontStyle = FontStyles.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            phText.font = FontManager.Instance.ChineseFont;

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRT;
        inputField.textComponent = text;
        inputField.placeholder = phText;
        inputField.fontAsset = FontManager.Instance != null ? FontManager.Instance.ChineseFont : null;

        return inputField;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color,
        float height, float width = 0f)
    {
        GameObject obj = CreateUIElement("Label_" + text, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        if (width > 0f)
        {
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
        }

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;

        if (FontManager.Instance != null && FontManager.Instance.ChineseFont != null)
            tmp.font = FontManager.Instance.ChineseFont;

        return tmp;
    }

    private void CreateSeparator(Transform parent)
    {
        GameObject sep = CreateUIElement("Separator", parent);
        RectTransform rt = sep.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 2f);
        Image img = sep.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.flexibleWidth = 1f;
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
