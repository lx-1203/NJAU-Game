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

        CreateLabel(content.transform, "Ending Sim", 18f, TextGold, 30f);
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
        builder.AppendLine("Current State");
        builder.AppendLine($"Study {pa.Study}  Charm {pa.Charm}  Physique {pa.Physique}  Lead {pa.Leadership}");
        builder.AppendLine($"Stress {pa.Stress}  Mood {pa.Mood}  Dark {pa.Darkness}  Guilt {pa.Guilt}  Luck {pa.Luck}");

        if (GameState.Instance != null)
        {
            builder.AppendLine($"Time {GameState.Instance.GetTimeDescription()}");
            builder.AppendLine($"Money {GameState.Instance.Money}  AP {GameState.Instance.ActionPoints}/{GameState.Instance.EffectiveMaxActionPoints}");
        }

        if (ExamSystem.Instance != null)
            builder.AppendLine($"GPA {ExamSystem.Instance.GetCumulativeGPA():F2}");

        if (SemesterSummarySystem.Instance != null)
        {
            builder.AppendLine(
                $"StudyCount {SemesterSummarySystem.Instance.StudyCount}  SocialCount {SemesterSummarySystem.Instance.SocialCount}  " +
                $"GoOut {SemesterSummarySystem.Instance.GoOutCount}  Sleep {SemesterSummarySystem.Instance.SleepCount}");
            builder.AppendLine($"GradScore {SemesterSummarySystem.Instance.CalculateGraduationScore():F1}");
        }

        summaryText.text = builder.ToString().TrimEnd();
    }

    private void RefreshCurrentEnding()
    {
        if (EndingDeterminer.Instance == null)
        {
            currentEndingText.text = "EndingDeterminer not ready";
            currentEndingText.color = TextGray;
            return;
        }

        EndingResult result = EndingDeterminer.Instance.DetermineEnding();
        if (result == null || result.ending == null)
        {
            currentEndingText.text = "No ending result";
            currentEndingText.color = TextGray;
            return;
        }

        EndingDefinition ending = result.ending;
        string stars = new string('*', Mathf.Max(0, ending.stars));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Current Ending: {ending.name}");
        builder.AppendLine($"Stars {stars} ({ending.stars})");
        builder.AppendLine($"Layer {GetLayerText(ending.GetLayer())} / {ending.layer}");
        builder.AppendLine($"Talent {result.talentPoints}  FinalScore {result.finalScore:F1}");

        if (!string.IsNullOrEmpty(ending.description))
            builder.AppendLine($"Desc {ending.description}");

        if (ending.conditions != null && ending.conditions.Count > 0)
        {
            builder.AppendLine("Matched Conditions");
            for (int i = 0; i < ending.conditions.Count; i++)
            {
                EndingCondition condition = ending.conditions[i];
                bool matched = EndingDeterminer.Instance.EvaluateCondition(condition);
                builder.AppendLine($"- {(matched ? "[OK]" : "[NO]")} {EndingDeterminer.Instance.DescribeCondition(condition)}");
            }
        }
        else
        {
            builder.AppendLine("Matched Conditions: AlwaysTrue");
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
            candidatesText.text = "Candidates: none";
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Candidate Endings");
        for (int i = 0; i < matches.Count; i++)
        {
            EndingDefinition ending = matches[i];
            builder.AppendLine($"{i + 1}. {ending.name} | Layer {ending.layer} | {ending.stars}*");
        }

        candidatesText.text = builder.ToString().TrimEnd();
    }

    private string GetLayerText(EndingLayer layer)
    {
        switch (layer)
        {
            case EndingLayer.ForcedEnding: return "Forced";
            case EndingLayer.PeakEnding: return "Peak";
            case EndingLayer.PlannedPath: return "Planned";
            case EndingLayer.UnplannedPath: return "Unplanned";
            case EndingLayer.DarkEnding: return "Dark";
            case EndingLayer.SpecialEnding: return "Special";
            case EndingLayer.NewCareer: return "NewCareer";
            case EndingLayer.FallbackEnding: return "Fallback";
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
