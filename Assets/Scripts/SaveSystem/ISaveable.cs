/// <summary>
/// 可存档接口 —— 各系统实现此接口以支持存档/读档
/// </summary>
public interface ISaveable
{
    /// <summary>将自身状态写入 SaveData</summary>
    void SaveToData(SaveData data);

    /// <summary>从 SaveData 恢复自身状态</summary>
    void LoadFromData(SaveData data);
}
