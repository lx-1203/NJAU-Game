using UnityEngine;

/// <summary>
/// 为加载界面提供可选的外部实时进度桥接。
/// 可用于文件读取、接口请求、资源下载等字节级加载流程。
/// </summary>
public static class LoadingProgressBridge
{
    public struct Snapshot
    {
        public bool isActive;
        public bool isDone;
        public float normalizedProgress;
        public long loadedBytes;
        public long totalBytes;
        public string statusLabel;
        public string detailLabel;
    }

    private static Snapshot current;

    public static void Reset()
    {
        current = default;
    }

    public static void ReportNormalized(float normalizedProgress, bool isDone = false,
        string statusLabel = null, string detailLabel = null)
    {
        current.isActive = true;
        current.isDone = isDone;
        current.normalizedProgress = Mathf.Clamp01(normalizedProgress);
        current.statusLabel = statusLabel;
        current.detailLabel = detailLabel;
    }

    public static void ReportBytes(long loadedBytes, long totalBytes, bool isDone = false,
        string statusLabel = null, string detailLabel = null)
    {
        current.isActive = true;
        current.isDone = isDone;
        current.loadedBytes = System.Math.Max(0L, loadedBytes);
        current.totalBytes = System.Math.Max(0L, totalBytes);
        current.normalizedProgress = current.totalBytes > 0L
            ? Mathf.Clamp01((float)current.loadedBytes / current.totalBytes)
            : 0f;
        current.statusLabel = statusLabel;
        current.detailLabel = detailLabel;
    }

    public static bool TryGetSnapshot(out Snapshot snapshot)
    {
        snapshot = current;
        return snapshot.isActive;
    }
}
