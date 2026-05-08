using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;

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
    public static int PendingLoadSlot { get; set; } = -1;

    // ========== 事件 ==========

    /// <summary>存档完成后触发，参数为槽位索引</summary>
    public event Action<int> OnSaveCompleted;

    /// <summary>读档完成后触发，参数为槽位索引</summary>
    public event Action<int> OnLoadCompleted;

    // ========== 路径与命名 ==========

    private string saveFolderPath;
    private string thumbnailFolderPath;

    /// <summary>槽位数量: 0=autosave, 1-3=手动</summary>
    private const int SlotCount = 4;
    private const float LifecycleAutoSaveDebounceSeconds = 2f;

    // ========== 游戏时长追踪 ==========

    private float sessionStartTime;
    private float previousTotalPlayTime;
    private float lastLifecycleAutoSaveTime = -999f;
    private readonly Dictionary<int, SaveData> slotDataCache = new Dictionary<int, SaveData>();

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
        thumbnailFolderPath = Path.Combine(saveFolderPath, "thumbnails");
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
        SaveToSlot(slot, null);
    }

    public void SaveToSlot(int slot, Texture2D thumbnailTexture)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogWarning($"[SaveManager] 无效的存档槽位: {slot}");
            ShowSaveNotification("存档失败", "目标存档槽位无效，这次保存没有执行。");
            return;
        }

        SaveData existingData = GetSlotSaveData(slot);
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

        data.thumbnailFileName = !string.IsNullOrWhiteSpace(existingData?.thumbnailFileName)
            ? existingData.thumbnailFileName
            : GetThumbnailFileNameForSlot(slot);

        if (thumbnailTexture == null && string.IsNullOrWhiteSpace(existingData?.thumbnailFileName))
        {
            data.thumbnailFileName = string.Empty;
        }

        // 序列化并写入文件
        string json = JsonUtility.ToJson(data, true);
        string filePath = GetSlotFilePath(slot);
        EnsureSaveFolder();

        try
        {
            if (thumbnailTexture != null)
            {
                WriteThumbnail(slot, data.thumbnailFileName, thumbnailTexture);
            }
            else if (string.IsNullOrWhiteSpace(data.thumbnailFileName))
            {
                DeleteThumbnailFile(GetThumbnailFileNameForSlot(slot));
            }

            WriteSaveFileAtomically(filePath, json);
            CacheSlotData(slot, data);
            Debug.Log($"[SaveManager] 存档成功: 槽位{slot} -> {filePath}");
            OnSaveCompleted?.Invoke(slot);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 存档写入失败: {e.Message}");
            ShowSaveNotification("存档失败", "写入存档文件时发生异常，这次进度没有成功保存。");
        }
    }

    /// <summary>
    /// 从指定槽位读取存档数据
    /// </summary>
    /// <param name="slot">0=自动存档, 1-3=手动存档</param>
    /// <returns>存档数据，读取失败返回 null</returns>
    public SaveData LoadFromSlot(int slot)
    {
        return ReadSlotData(slot, true);
    }

    /// <summary>
    /// 将加载的存档数据应用到所有 ISaveable 系统
    /// </summary>
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveManager] 无法应用空的存档数据");
            ShowSaveNotification("读档失败", "读到的存档数据为空，当前进度不会被覆盖。");
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

        if (PendingLoadSlot >= 0)
        {
            OnLoadCompleted?.Invoke(PendingLoadSlot);
            PendingLoadSlot = -1;
        }
    }

    /// <summary>
    /// 删除指定槽位的存档
    /// </summary>
    public void DeleteSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogWarning($"[SaveManager] 无效的存档槽位: {slot}");
            ShowSaveNotification("删除失败", "目标存档槽位无效，这次删除没有执行。");
            return;
        }

        string filePath = GetSlotFilePath(slot);
        string backupPath = GetBackupFilePath(filePath);
        string tempPath = GetTempFilePath(filePath);
        SaveData existingData = GetSlotSaveData(slot);
        string thumbnailFileName = !string.IsNullOrWhiteSpace(existingData?.thumbnailFileName)
            ? existingData.thumbnailFileName
            : GetThumbnailFileNameForSlot(slot);
        string thumbnailPath = GetThumbnailPath(thumbnailFileName);

        if (File.Exists(filePath) || File.Exists(backupPath) || File.Exists(tempPath) || (!string.IsNullOrWhiteSpace(thumbnailPath) && File.Exists(thumbnailPath)))
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                DeleteThumbnailFile(thumbnailFileName);

                slotDataCache.Remove(slot);
                Debug.Log($"[SaveManager] 已删除存档: 槽位{slot}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 删除存档失败: {e.Message}");
                ShowSaveNotification("删除失败", "清理存档文件时发生异常，这个槽位暂时没有删除成功。");
            }
        }
    }

    /// <summary>
    /// 检查指定槽位是否有存档
    /// </summary>
    public bool HasSaveData(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return false;
        string filePath = GetSlotFilePath(slot);
        return File.Exists(filePath)
            || File.Exists(GetBackupFilePath(filePath))
            || File.Exists(GetTempFilePath(filePath));
    }

    /// <summary>
    /// 获取指定槽位的元信息
    /// </summary>
    /// <returns>元信息，无存档返回 null</returns>
    public SaveMetaInfo GetSlotMeta(int slot)
    {
        SaveData data = GetSlotSaveData(slot);
        return data?.meta;
    }

    /// <summary>
    /// 读取槽位的完整存档数据（仅供 UI 展示使用，不产生通知）
    /// </summary>
    /// <returns>完整存档数据，无存档返回 null</returns>
    public SaveData GetSlotSaveData(int slot)
    {
        if (slotDataCache.TryGetValue(slot, out SaveData cachedData))
        {
            return cachedData;
        }

        return ReadSlotData(slot, false);
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
        if (!CanAutoSaveCurrentSession())
        {
            return;
        }

        CaptureAndSaveToSlot(0);
    }

    public void AutoSaveImmediate(string reason = null, bool ignoreDebounce = false)
    {
        if (!CanAutoSaveCurrentSession())
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (!ignoreDebounce && now - lastLifecycleAutoSaveTime < LifecycleAutoSaveDebounceSeconds)
        {
            return;
        }

        lastLifecycleAutoSaveTime = now;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Debug.Log($"[SaveManager] 执行即时自动存档: {reason}");
        }

        SaveToSlot(0);
    }

    public int GetLatestSaveSlot()
    {
        int latestSlot = -1;
        DateTime latestTime = DateTime.MinValue;

        for (int slot = 0; slot < SlotCount; slot++)
        {
            SaveData data = GetSlotSaveData(slot);
            if (data == null)
            {
                continue;
            }

            DateTime saveTime = GetBestSaveTimestamp(slot, data);
            if (latestSlot < 0 || saveTime > latestTime)
            {
                latestSlot = slot;
                latestTime = saveTime;
            }
        }

        return latestSlot;
    }

    public Coroutine CaptureAndSaveToSlot(int slot, Action<bool> onCompleted = null)
    {
        return StartCoroutine(CaptureAndSaveRoutine(slot, onCompleted));
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

    private string GetBackupFilePath(string filePath)
    {
        return $"{filePath}.bak";
    }

    private string GetTempFilePath(string filePath)
    {
        return $"{filePath}.tmp";
    }

    private string GetThumbnailFileNameForSlot(int slot)
    {
        return $"thumb_{slot}.png";
    }

    private SaveData ReadSlotData(int slot, bool logSuccess)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogWarning($"[SaveManager] 无效的存档槽位: {slot}");
            ShowSaveNotification("读档失败", "目标存档槽位无效，无法读取这份进度。");
            return null;
        }

        string filePath = GetSlotFilePath(slot);
        string backupPath = GetBackupFilePath(filePath);
        string tempPath = GetTempFilePath(filePath);

        if (!File.Exists(filePath) && !File.Exists(backupPath) && !File.Exists(tempPath))
        {
            Debug.LogWarning($"[SaveManager] 存档文件不存在: {filePath}");
            if (logSuccess)
            {
                ShowSaveNotification("读档失败", "这个槽位还没有可读取的存档记录。");
            }
            return null;
        }

        try
        {
            SaveData data = TryReadSaveDataFromPath(filePath);
            if (data == null)
            {
                data = TryReadSaveDataFromPath(backupPath);
                if (data != null)
                {
                    Debug.LogWarning($"[SaveManager] 主存档读取失败，已回退到备份文件: {backupPath}");
                }
            }

            if (data == null)
            {
                data = TryReadSaveDataFromPath(tempPath);
                if (data != null)
                {
                    Debug.LogWarning($"[SaveManager] 主存档与备份不可用，已回退到临时文件: {tempPath}");
                }
            }

            if (data == null)
            {
                throw new InvalidDataException("未能从主文件、备份文件或临时文件中恢复有效存档。");
            }

            CacheSlotData(slot, data);
            if (logSuccess)
            {
                Debug.Log($"[SaveManager] 读档成功: 槽位{slot}");
            }
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 读档失败: {e.Message}");
            ShowSaveNotification("读档失败", "读取存档文件时发生异常，请换一个槽位再试。");
            return null;
        }
    }

    private SaveData TryReadSaveDataFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            data?.EnsureInitialized();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 读取存档文件失败，准备尝试回退文件: {filePath} | {e.Message}");
            return null;
        }
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

        if (!Directory.Exists(thumbnailFolderPath))
        {
            Directory.CreateDirectory(thumbnailFolderPath);
            Debug.Log($"[SaveManager] 创建缩略图目录: {thumbnailFolderPath}");
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

    private void CacheSlotData(int slot, SaveData data)
    {
        if (data == null)
        {
            slotDataCache.Remove(slot);
            return;
        }

        slotDataCache[slot] = data;
    }

    private void WriteSaveFileAtomically(string filePath, string json)
    {
        string tempPath = GetTempFilePath(filePath);
        string backupPath = GetBackupFilePath(filePath);

        File.WriteAllText(tempPath, json);

        if (File.Exists(filePath))
        {
            File.Replace(tempPath, filePath, backupPath, true);
            return;
        }

        File.Move(tempPath, filePath);
    }

    private bool CanAutoSaveCurrentSession()
    {
        return GameState.Instance != null && PlayerAttributes.Instance != null;
    }

    private DateTime GetBestSaveTimestamp(int slot, SaveData data)
    {
        if (DateTime.TryParse(data?.meta?.saveTime, out DateTime parsed))
        {
            return parsed.ToUniversalTime();
        }

        string filePath = GetSlotFilePath(slot);
        if (File.Exists(filePath))
        {
            return File.GetLastWriteTimeUtc(filePath);
        }

        return DateTime.MinValue;
    }

    private void ShowSaveNotification(string title, string message, float duration = 2.8f)
    {
        if (MissionUI.Instance != null)
        {
            MissionUI.Instance.ShowSystemNotification(title, message, new Color(0.82f, 0.38f, 0.30f), duration);
        }
    }

    public string GetThumbnailPath(string thumbnailFileName)
    {
        if (string.IsNullOrWhiteSpace(thumbnailFileName))
        {
            return null;
        }

        EnsureSaveFolder();
        return Path.Combine(thumbnailFolderPath, thumbnailFileName);
    }

    private IEnumerator CaptureAndSaveRoutine(int slot, Action<bool> onCompleted)
    {
        yield return new WaitForEndOfFrame();

        Texture2D capturedTexture = null;
        bool succeeded = true;

        try
        {
            capturedTexture = CaptureFramebufferTexture();
            SaveToSlot(slot, capturedTexture);
        }
        catch (Exception e)
        {
            succeeded = false;
            Debug.LogError($"[SaveManager] 截图存档失败: {e.Message}");
            SaveToSlot(slot);
        }
        finally
        {
            if (capturedTexture != null)
            {
                Destroy(capturedTexture);
            }
        }

        onCompleted?.Invoke(succeeded);
    }

    private Texture2D CaptureFramebufferTexture()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
        texture.Apply(false, false);
        return texture;
    }

    private void WriteThumbnail(int slot, string thumbnailFileName, Texture2D sourceTexture)
    {
        if (sourceTexture == null || string.IsNullOrWhiteSpace(thumbnailFileName))
        {
            return;
        }

        EnsureSaveFolder();

        Texture2D resizedTexture = ResizeTexture(sourceTexture, 480, 270);
        byte[] pngBytes = resizedTexture.EncodeToPNG();
        string thumbnailPath = GetThumbnailPath(thumbnailFileName);

        File.WriteAllBytes(thumbnailPath, pngBytes);

        if (resizedTexture != sourceTexture)
        {
            Destroy(resizedTexture);
        }
    }

    private void DeleteThumbnailFile(string thumbnailFileName)
    {
        string thumbnailPath = GetThumbnailPath(thumbnailFileName);
        if (string.IsNullOrWhiteSpace(thumbnailPath) || !File.Exists(thumbnailPath))
        {
            return;
        }

        File.Delete(thumbnailPath);
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        if (source == null)
        {
            return null;
        }

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D resized = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resized.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            AutoSaveImmediate("应用进入后台");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            AutoSaveImmediate("应用失去焦点");
        }
    }

    private void OnApplicationQuit()
    {
        AutoSaveImmediate("退出游戏", true);
    }
}
