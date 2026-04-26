using UnityEngine;
using System;
using System.IO;

/// <summary>
/// 存档管理器 —— 负责存档的读写、删除、自动存档等
/// 使用 JsonUtility 序列化，文件存储在 persistentDataPath/saves/
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ========== 单例 ==========

    public static SaveManager Instance { get; private set; }

    // ========== 跨场景加载数据 ==========

    /// <summary>
    /// 待加载的存档数据（跨场景传递）。
    /// 在标题界面读档后设置，由 GameSceneInitializer 消费并清空。
    /// </summary>
    public static SaveData PendingLoadData { get; set; }

    // ========== 事件 ==========

    /// <summary>存档完成后触发，参数为槽位索引</summary>
    public event Action<int> OnSaveCompleted;

    /// <summary>读档完成后触发，参数为槽位索引</summary>
    public event Action<int> OnLoadCompleted;

    // ========== 路径与命名 ==========

    private string saveFolderPath;

    /// <summary>槽位数量: 0=autosave, 1-3=手动</summary>
    private const int SlotCount = 4;

    // ========== 游戏时长追踪 ==========

    private float sessionStartTime;
    private float previousTotalPlayTime;

    // ========== 生命周期 ==========

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFolderPath = Path.Combine(Application.persistentDataPath, "saves");
        EnsureSaveFolder();
    }

    private void Start()
    {
        sessionStartTime = Time.realtimeSinceStartup;
        previousTotalPlayTime = 0f;
    }

    // ========== 公共方法 ==========

    /// <summary>
    /// 保存到指定槽位
    /// </summary>
    /// <param name="slot">0=自动存档, 1-3=手动存档</param>
    public void SaveToSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogWarning($"[SaveManager] 无效的存档槽位: {slot}");
            return;
        }

        SaveData data = new SaveData();

        // 收集所有 ISaveable 组件的数据
        MonoBehaviour[] allMB = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour mb in allMB)
        {
            if (mb is ISaveable saveable)
            {
                saveable.SaveToData(data);
            }
        }

        // 填充元信息
        data.meta.slotIndex = slot;
        data.meta.saveTime = DateTime.Now.ToString("o"); // ISO 8601
        data.meta.playTimeSeconds = GetTotalPlayTime();

        if (GameState.Instance != null)
        {
            data.meta.progressDesc = GameState.Instance.GetTimeDescription();
        }
        else
        {
            data.meta.progressDesc = "未知进度";
        }

        // 记录总游戏时长
        data.totalPlayTimeSeconds = data.meta.playTimeSeconds;

        // 序列化并写入文件
        string json = JsonUtility.ToJson(data, true);
        string filePath = GetSlotFilePath(slot);
        EnsureSaveFolder();

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"[SaveManager] 存档成功: 槽位{slot} -> {filePath}");
            OnSaveCompleted?.Invoke(slot);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 存档写入失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从指定槽位读取存档数据
    /// </summary>
    /// <param name="slot">0=自动存档, 1-3=手动存档</param>
    /// <returns>存档数据，读取失败返回 null</returns>
    public SaveData LoadFromSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogWarning($"[SaveManager] 无效的存档槽位: {slot}");
            return null;
        }

        string filePath = GetSlotFilePath(slot);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveManager] 存档文件不存在: {filePath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            data?.EnsureInitialized();
            Debug.Log($"[SaveManager] 读档成功: 槽位{slot}");
            OnLoadCompleted?.Invoke(slot);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 读档失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将加载的存档数据应用到所有 ISaveable 系统
    /// </summary>
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveManager] 无法应用空的存档数据");
            return;
        }

        // 恢复游戏时长追踪
        previousTotalPlayTime = data.totalPlayTimeSeconds;
        sessionStartTime = Time.realtimeSinceStartup;

        // 将数据应用到所有 ISaveable 组件
        MonoBehaviour[] allMB = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour mb in allMB)
        {
            if (mb is ISaveable saveable)
            {
                saveable.LoadFromData(data);
            }
        }

        Debug.Log("[SaveManager] 存档数据已应用到所有系统");
    }

    /// <summary>
    /// 删除指定槽位的存档
    /// </summary>
    public void DeleteSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogWarning($"[SaveManager] 无效的存档槽位: {slot}");
            return;
        }

        string filePath = GetSlotFilePath(slot);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"[SaveManager] 已删除存档: 槽位{slot}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 删除存档失败: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 检查指定槽位是否有存档
    /// </summary>
    public bool HasSaveData(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return false;
        return File.Exists(GetSlotFilePath(slot));
    }

    /// <summary>
    /// 获取指定槽位的元信息
    /// </summary>
    /// <returns>元信息，无存档返回 null</returns>
    public SaveMetaInfo GetSlotMeta(int slot)
    {
        if (!HasSaveData(slot)) return null;

        SaveData data = LoadFromSlot(slot);
        return data?.meta;
    }

    /// <summary>
    /// 获取所有槽位的元信息（无存档的槽位为 null）
    /// </summary>
    public SaveMetaInfo[] GetAllSlotMetas()
    {
        SaveMetaInfo[] metas = new SaveMetaInfo[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            metas[i] = HasSaveData(i) ? GetSlotMeta(i) : null;
        }
        return metas;
    }

    /// <summary>
    /// 自动存档（保存到槽位 0）
    /// </summary>
    public void AutoSave()
    {
        SaveToSlot(0);
    }

    // ========== 内部方法 ==========

    /// <summary>
    /// 获取槽位对应的文件路径
    /// </summary>
    private string GetSlotFilePath(int slot)
    {
        string fileName = slot == 0 ? "autosave.json" : $"save_{slot}.json";
        return Path.Combine(saveFolderPath, fileName);
    }

    /// <summary>
    /// 确保存档文件夹存在
    /// </summary>
    private void EnsureSaveFolder()
    {
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            Debug.Log($"[SaveManager] 创建存档目录: {saveFolderPath}");
        }
    }

    /// <summary>
    /// 获取总游戏时长（之前累积 + 本次会话）
    /// </summary>
    private float GetTotalPlayTime()
    {
        float sessionTime = Time.realtimeSinceStartup - sessionStartTime;
        return previousTotalPlayTime + sessionTime;
    }
}
