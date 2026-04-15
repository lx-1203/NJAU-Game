using System;

/// <summary>
/// 对话数据模型 —— 定义 JSON 对话文件的 C# 映射结构
/// </summary>

/// <summary>
/// 对话效果（属性变化 / 金钱变化）
/// </summary>
[Serializable]
public class DialogueEffect
{
    /// <summary>效果类型: "attribute" | "money"</summary>
    public string type;
    /// <summary>属性名（type=attribute时使用），如 "学力", "魅力"</summary>
    public string target;
    /// <summary>变化量（正为增加，负为减少）</summary>
    public int amount;
}

/// <summary>
/// 对话选项（分支选择）
/// </summary>
[Serializable]
public class DialogueChoice
{
    /// <summary>选项显示文本</summary>
    public string text;
    /// <summary>跳转目标节点 ID</summary>
    public string next;
    /// <summary>条件表达式，如 "学力>=80 AND 心情>50"（可为空）</summary>
    public string condition;
    /// <summary>条件不满足时的提示文字（可为空）</summary>
    public string conditionHint;
    /// <summary>选择后触发的效果（可为空）</summary>
    public DialogueEffect[] effects;
}

/// <summary>
/// 对话节点（一条对话内容）
/// </summary>
[Serializable]
public class DialogueNode
{
    /// <summary>节点 ID，格式 "D001"</summary>
    public string id;
    /// <summary>说话人名字（空=""旁白, "_inner"=内心独白）</summary>
    public string speaker;
    /// <summary>头像 Resources 路径（可为空）</summary>
    public string portrait;
    /// <summary>对话文本内容</summary>
    public string content;
    /// <summary>下一节点 ID（无 choices 时使用，null 表示对话结束）</summary>
    public string next;
    /// <summary>分支选项（可为空，有则忽略 next）</summary>
    public DialogueChoice[] choices;
}

/// <summary>
/// 完整对话数据（对应一个 JSON 文件）
/// </summary>
[Serializable]
public class DialogueData
{
    /// <summary>对话 ID，如 "npc_classmate_01"</summary>
    public string id;
    /// <summary>所有对话节点</summary>
    public DialogueNode[] nodes;
}

/// <summary>
/// JSON 反序列化包装类（JsonUtility 不直接支持顶层数组）
/// </summary>
[Serializable]
public class DialogueDataWrapper
{
    public DialogueData[] dialogues;
}
