#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 结局模拟模块 —— 显示当前属性概要 + 实时结局预测
/// </summary>
public class EndingSimModule : MonoBehaviour, IDebugModule
{
    private static readonly Color TextWhite = new Color(0.92f, 0.92f, 0.92f);
    private static readonly Color TextGold  = new Color(1.0f, 0.85f, 0.30f);
    private static readonly Color TextGray  = new Color(0.6f, 0.6f, 0.65f);

    private TextMeshProUGUI summaryText;
    private TextMeshProUGUI predictionText;

    public void Init(RectTransform parent)
    {
        GameObject content = CreateUIElement("Content", parent);
        StretchFull(content.GetComponent<RectTransform>());

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 标题
        CreateLabel(content.transform, "— 结局模拟器 —", 18f, TextGold, 30f);

        // 属性概要
        summaryText = CreateLabel(content.transform, "属性概要加载中...", 14f, TextWhite, 200f);
        summaryText.enableWordWrapping = true;

        // 分割线
        CreateSeparator(content.transform);

        // 结局预测
        predictionText = CreateLabel(content.transform,
            "结局预测: 加载中...",
            14f, TextWhite, 180f);
        predictionText.enableWordWrapping = true;
    }

    public void Refresh()
    {
        if (PlayerAttributes.Instance == null || summaryText == null) return;

        var pa = PlayerAttributes.Instance;
        string summary = "当前属性概要:\n" +
            $"  学力: {pa.Study}    魅力: {pa.Charm}\n" +
            $"  体魄: {pa.Physique}    领导力: {pa.Leadership}\n" +
            $"  压力: {pa.Stress}    心情: {pa.Mood}\n" +
            $"  黑暗值: {pa.Darkness}    负罪感: {pa.Guilt}\n" +
            $"  幸运: {pa.Luck}";

        if (GameState.Instance != null)
        {
            summary += $"\n\n  时间: {GameState.Instance.GetTimeDescription()}";
            summary += $"\n  金钱: ¥{GameState.Instance.Money}";
        }

        summaryText.text = summary;

        // ========== 结局预测 ==========
        RefreshEndingPrediction();
    }

    /// <summary>
    /// 刷新结局预测区域
    /// </summary>
    private void RefreshEndingPrediction()
    {
        if (predictionText == null) return;

        if (EndingDeterminer.Instance == null)
        {
            predictionText.text = "结局系统未初始化";
            predictionText.color = TextGray;
            return;
        }

        try
        {
            EndingResult result = EndingDeterminer.Instance.DetermineEnding();

            if (result == null || result.ending == null)
            {
                predictionText.text = "结局预测: 无法判定\n当前状态未匹配任何结局条件。";
                predictionText.color = TextGray;
                return;
            }

            EndingDefinition ending = result.ending;
            string stars = new string('★', ending.stars) + new string('☆', 7 - ending.stars);
            string layerName = GetLayerChinese(ending.GetLayer());

            string prediction =
                $"结局预测: <color=#FFD94A>{ending.name}</color>\n" +
                $"  星级: {stars} ({ending.stars}★)\n" +
                $"  层级: {layerName} (Layer {ending.layer})\n" +
                $"  天赋点: {result.talentPoints}\n" +
                $"  总评分: {result.finalScore:F1}";

            if (!string.IsNullOrEmpty(ending.description))
            {
                // 截取描述前60字符避免过长
                string desc = ending.description.Length > 60
                    ? ending.description.Substring(0, 60) + "..."
                    : ending.description;
                prediction += $"\n  描述: {desc}";
            }

            predictionText.text = prediction;
            predictionText.color = TextWhite;
        }
        catch (System.Exception e)
        {
            predictionText.text = $"结局预测出错:\n{e.Message}";
            predictionText.color = new Color(0.9f, 0.4f, 0.4f);
            Debug.LogWarning($"[EndingSimModule] 结局预测异常: {e}");
        }
    }

    /// <summary>
    /// 结局层级中文名
    /// </summary>
    private string GetLayerChinese(EndingLayer layer)
    {
        switch (layer)
        {
            case EndingLayer.ForcedEnding:  return "强制结局";
            case EndingLayer.PeakEnding:    return "巅峰结局";
            case EndingLayer.PlannedPath:   return "规划路径";
            case EndingLayer.UnplannedPath: return "非规划路径";
            case EndingLayer.DarkEnding:    return "黑暗结局";
            case EndingLayer.SpecialEnding: return "特殊结局";
            case EndingLayer.NewCareer:     return "新兴职业";
            case EndingLayer.FallbackEnding:return "兜底结局";
            default: return layer.ToString();
        }
    }

    // ========== 工具方法 ==========

    private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color, float height)
    {
        GameObject obj = CreateUIElement("Label", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

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
