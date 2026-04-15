using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 对话 JSON 加载器 + 条件表达式解析器（静态工具类）
/// </summary>
public static class DialogueParser
{
    /// <summary>对话数据缓存，key = dialogueId</summary>
    private static Dictionary<string, DialogueData> cache = new Dictionary<string, DialogueData>();

    /// <summary>条件表达式中支持的运算符（按长度降序排列，优先匹配双字符运算符）</summary>
    private static readonly string[] Operators = { ">=", "<=", "!=", "==", ">", "<" };

    /// <summary>条件运算符正则：匹配 "属性名 运算符 数值" 格式</summary>
    private static readonly Regex ConditionRegex = new Regex(
        @"^\s*(.+?)\s*(>=|<=|!=|==|>|<)\s*(-?\d+)\s*$",
        RegexOptions.Compiled
    );

    // ──────────────────────────────────────────────
    //  加载 & 查询
    // ──────────────────────────────────────────────

    /// <summary>
    /// 加载 StreamingAssets/Dialogues/ 目录下所有 JSON 对话文件并缓存。
    /// 应在游戏初始化阶段调用一次。
    /// </summary>
    public static void LoadAllDialogues()
    {
        cache.Clear();

        string dialogueDir = Path.Combine(Application.streamingAssetsPath, "Dialogues");

        // 目录不存在时自动创建并警告
        if (!Directory.Exists(dialogueDir))
        {
            Directory.CreateDirectory(dialogueDir);
            Debug.LogWarning($"[DialogueParser] 对话目录不存在，已自动创建: {dialogueDir}");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(dialogueDir, "*.json");

        if (jsonFiles.Length == 0)
        {
            Debug.LogWarning($"[DialogueParser] 对话目录为空，未找到任何 .json 文件: {dialogueDir}");
            return;
        }

        foreach (string filePath in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                DialogueData data = JsonUtility.FromJson<DialogueData>(json);

                if (data == null || string.IsNullOrEmpty(data.id))
                {
                    Debug.LogWarning($"[DialogueParser] 跳过无效对话文件（缺少 id）: {filePath}");
                    continue;
                }

                if (cache.ContainsKey(data.id))
                {
                    Debug.LogWarning($"[DialogueParser] 对话 ID 重复，后者覆盖前者: {data.id} ({filePath})");
                }

                cache[data.id] = data;
                Debug.Log($"[DialogueParser] 已加载对话: {data.id} ({data.nodes?.Length ?? 0} 个节点)");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueParser] 加载对话文件失败: {filePath}\n{e.Message}");
            }
        }

        Debug.Log($"[DialogueParser] 对话加载完成，共 {cache.Count} 个对话");
    }

    /// <summary>
    /// 根据对话 ID 从缓存中获取对话数据（O(1) 查找）。
    /// </summary>
    /// <param name="dialogueId">对话 ID，如 "npc_classmate_01"</param>
    /// <returns>对话数据；未找到返回 null</returns>
    public static DialogueData GetDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            Debug.LogWarning("[DialogueParser] GetDialogue 传入了空 ID");
            return null;
        }

        if (cache.TryGetValue(dialogueId, out DialogueData data))
        {
            return data;
        }

        Debug.LogWarning($"[DialogueParser] 未找到对话: {dialogueId}");
        return null;
    }

    // ──────────────────────────────────────────────
    //  条件表达式解析
    // ──────────────────────────────────────────────

    /// <summary>
    /// 解析并执行条件表达式。
    /// <para>支持 AND / OR 逻辑（AND 优先级高于 OR）。</para>
    /// <para>示例: "学力>=80 AND 心情>50"、"魅力>=60 OR 领导力>=60"</para>
    /// <para>混合示例: "学力>=80 AND 魅力>=60 OR 领导力>=90"
    /// → 解析为 (学力>=80 AND 魅力>=60) OR (领导力>=90)</para>
    /// </summary>
    /// <param name="expression">条件表达式字符串</param>
    /// <returns>条件是否满足</returns>
    public static bool EvaluateCondition(string expression)
    {
        // 空表达式视为无条件，直接通过
        if (string.IsNullOrEmpty(expression))
            return true;

        // 按 OR 分割为多组（OR 优先级低）
        string[] orGroups = expression.Split(new string[] { " OR " }, StringSplitOptions.None);

        foreach (string orGroup in orGroups)
        {
            // 每组内按 AND 分割（AND 优先级高）
            string[] andConditions = orGroup.Split(new string[] { " AND " }, StringSplitOptions.None);

            bool allAndMet = true;
            foreach (string cond in andConditions)
            {
                if (!EvaluateSingleCondition(cond.Trim()))
                {
                    allAndMet = false;
                    break;
                }
            }

            // 任意一个 OR 组满足即可
            if (allAndMet)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 解析并执行单个条件，格式: "属性名运算符数值"，如 "学力>=80"。
    /// </summary>
    /// <param name="cond">单个条件字符串</param>
    /// <returns>条件是否满足</returns>
    public static bool EvaluateSingleCondition(string cond)
    {
        if (string.IsNullOrEmpty(cond))
            return true;

        Match match = ConditionRegex.Match(cond);
        if (!match.Success)
        {
            Debug.LogWarning($"[DialogueParser] 无法解析条件表达式: \"{cond}\"");
            return false;
        }

        string attrName = match.Groups[1].Value.Trim();
        string op = match.Groups[2].Value;
        int targetValue = int.Parse(match.Groups[3].Value);

        int currentValue = GetAttributeValue(attrName);

        switch (op)
        {
            case ">=": return currentValue >= targetValue;
            case "<=": return currentValue <= targetValue;
            case ">":  return currentValue > targetValue;
            case "<":  return currentValue < targetValue;
            case "==": return currentValue == targetValue;
            case "!=": return currentValue != targetValue;
            default:
                Debug.LogWarning($"[DialogueParser] 未知运算符: \"{op}\"");
                return false;
        }
    }

    // ──────────────────────────────────────────────
    //  属性值查询
    // ──────────────────────────────────────────────

    /// <summary>
    /// 根据中文属性名查询当前属性值。
    /// 统一映射 PlayerAttributes 和 GameState 的属性。
    /// </summary>
    /// <param name="attrName">中文属性名，如 "学力"、"金钱"、"学年"</param>
    /// <returns>属性当前值；未识别的属性返回 0</returns>
    public static int GetAttributeValue(string attrName)
    {
        switch (attrName)
        {
            // —— PlayerAttributes ——
            case "学力":  return PlayerAttributes.Instance.Study;
            case "魅力":  return PlayerAttributes.Instance.Charm;
            case "体魄":  return PlayerAttributes.Instance.Physique;
            case "领导力": return PlayerAttributes.Instance.Leadership;
            case "压力":  return PlayerAttributes.Instance.Stress;
            case "心情":  return PlayerAttributes.Instance.Mood;

            // —— GameState ——
            case "金钱":  return GameState.Instance.Money;
            case "学年":  return GameState.Instance.CurrentYear;
            case "学期":  return GameState.Instance.CurrentSemester;
            case "回合":  return GameState.Instance.CurrentRound;
            case "月份":  return GameState.Instance.CurrentMonth;

            default:
                Debug.LogWarning($"[DialogueParser] 未知属性名: \"{attrName}\"");
                return 0;
        }
    }
}
