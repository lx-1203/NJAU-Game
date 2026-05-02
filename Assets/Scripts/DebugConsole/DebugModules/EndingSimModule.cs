#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingSimModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);

    private TextMeshProUGUI summaryText;
    private TextMeshProUGUI currentEndingText;
    private TextMeshProUGUI candidatesText;

    public void Init(RectTransform parent)
    {
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

        CreateLabel(content.transform, "结局模拟", 18f, TextGold, 30f);
        summaryText = CreateLabel(content.transform, string.Empty, 14f, TextWhite, 164f);
        currentEndingText = CreateLabel(content.transform, string.Empty, 14f, TextWhite, 172f);
        candidatesText = CreateLabel(content.transform, string.Empty, 13f, TextGray, 320f);

        summaryText.enableWordWrapping = true;
        currentEndingText.enableWordWrapping = true;
        candidatesText.enableWordWrapping = true;
    }

    public void Refresh()
    {
        RefreshSummary();
        RefreshCurrentEnding();
        RefreshCandidates();
    }

    private void RefreshSummary()
    {
        if (summaryText == null || PlayerAttributes.Instance == null)
            return;

        PlayerAttributes pa = PlayerAttributes.Instance;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("当前状态");
        builder.AppendLine($"学力 {pa.Study}  魅力 {pa.Charm}  体魄 {pa.Physique}  领导力 {pa.Leadership}");
        builder.AppendLine($"压力 {pa.Stress}  心情 {pa.Mood}  黑暗值 {pa.Darkness}  负罪感 {pa.Guilt}  幸运 {pa.Luck}");

        if (GameState.Instance != null)
        {
            builder.AppendLine($"时间 {GameState.Instance.GetTimeDescription()}");
            builder.AppendLine($"金钱 {GameState.Instance.Money}  行动点 {GameState.Instance.ActionPoints}/{GameState.Instance.EffectiveMaxActionPoints}");
        }

        if (ExamSystem.Instance != null)
            builder.AppendLine($"GPA {ExamSystem.Instance.GetCumulativeGPA():F2}");

        if (SemesterSummarySystem.Instance != null)
        {
            builder.AppendLine(
                $"学习次数 {SemesterSummarySystem.Instance.StudyCount}  社交次数 {SemesterSummarySystem.Instance.SocialCount}  " +
                $"外出次数 {SemesterSummarySystem.Instance.GoOutCount}  睡觉次数 {SemesterSummarySystem.Instance.SleepCount}");
            builder.AppendLine($"毕业总评 {SemesterSummarySystem.Instance.CalculateGraduationScore():F1}");
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

        EndingResult result = EndingDeterminer.Instance.DetermineEnding();
        if (result == null || result.ending == null)
        {
            currentEndingText.text = "暂无结局结果";
            currentEndingText.color = TextGray;
            return;
        }

        EndingDefinition ending = result.ending;
        string stars = new string('*', Mathf.Max(0, ending.stars));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"当前结局：{ending.name}");
        builder.AppendLine($"星级 {stars} ({ending.stars})");
        builder.AppendLine($"层级 {GetLayerText(ending.GetLayer())} / {ending.layer}");
        builder.AppendLine($"天赋点 {result.talentPoints}  结算分 {result.finalScore:F1}");

        if (!string.IsNullOrEmpty(ending.description))
            builder.AppendLine($"描述 {ending.description}");

        if (ending.conditions != null && ending.conditions.Count > 0)
        {
            builder.AppendLine("命中条件");
            for (int i = 0; i < ending.conditions.Count; i++)
            {
                EndingCondition condition = ending.conditions[i];
                bool matched = EndingDeterminer.Instance.EvaluateCondition(condition);
                builder.AppendLine($"- {(matched ? "[满足]" : "[未满足]")} {EndingDeterminer.Instance.DescribeCondition(condition)}");
            }
        }
        else
        {
            builder.AppendLine("命中条件：恒为真");
        }

        currentEndingText.text = builder.ToString().TrimEnd();
        currentEndingText.color = TextWhite;
    }

    private void RefreshCandidates()
    {
        if (EndingDeterminer.Instance == null)
        {
            candidatesText.text = string.Empty;
            return;
        }

        List<EndingDefinition> matches = EndingDeterminer.Instance.GetMatchingEndings(5);
        if (matches == null || matches.Count == 0)
        {
            candidatesText.text = "候选结局：无";
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("候选结局");
        for (int i = 0; i < matches.Count; i++)
        {
            EndingDefinition ending = matches[i];
            builder.AppendLine($"{i + 1}. {ending.name} | 层级 {ending.layer} | {ending.stars} 星");
        }

        candidatesText.text = builder.ToString().TrimEnd();
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
        rt.sizeDelta = new Vector2(0f, height);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
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
