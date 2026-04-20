using UnityEngine;

/// <summary>
/// 区域检测器 —— 挂载在 Player 上，根据世界X坐标自动检测所处地点
/// 当玩家走入新区域时自动调用 LocationManager.MoveTo() 切换地点
/// </summary>
public class LocationZoneDetector : MonoBehaviour
{
    private LocationId? lastDetectedLocation;
    private bool skipNextDetection;

    private void Start()
    {
        // 初始化当前地点
        if (GameState.Instance != null)
        {
            lastDetectedLocation = GameState.Instance.CurrentLocation;
        }

        // 订阅外部地点变更（地图传送时）跳过下一帧检测防止重复触发
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged += OnExternalLocationChanged;
        }
    }

    private void Update()
    {
        if (skipNextDetection)
        {
            skipNextDetection = false;
            return;
        }

        if (LocationManager.Instance == null) return;

        float x = transform.position.x;
        LocationId? detected = LocationManager.Instance.GetLocationAtWorldX(x);

        if (!detected.HasValue) return;
        if (lastDetectedLocation.HasValue && detected.Value == lastDetectedLocation.Value) return;

        // 走入新区域
        lastDetectedLocation = detected.Value;
        LocationManager.Instance.MoveTo(detected.Value);
    }

    private void OnExternalLocationChanged(LocationId from, LocationId to)
    {
        // 外部变更（地图传送）时更新缓存并跳过下一帧检测
        lastDetectedLocation = to;
        skipNextDetection = true;
    }

    private void OnDestroy()
    {
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged -= OnExternalLocationChanged;
        }
    }
}
