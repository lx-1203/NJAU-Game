/// <summary>
/// 对话触发接口 —— 事件系统通过此接口与对话系统解耦
/// DialogueSystem 实现此接口以提供对话表现能力
/// </summary>
public interface IDialogueTrigger
{
    /// <summary>是否正在显示对话</summary>
    bool IsActive { get; }

    /// <summary>
    /// 开始一段对话
    /// </summary>
    /// <param name="speakerName">说话人名字</param>
    /// <param name="lines">对话行列表</param>
    /// <param name="onComplete">对话结束后的回调</param>
    void ShowDialogue(string speakerName, string[] lines, System.Action onComplete);

    /// <summary>
    /// 显示选项供玩家选择（在对话结束后调用）
    /// </summary>
    /// <param name="choices">选项列表</param>
    /// <param name="onChoiceSelected">玩家选择后的回调，参数为选中的索引</param>
    void ShowChoices(EventChoice[] choices, System.Action<int> onChoiceSelected);
}
