using UnityEngine;

/// <summary>
/// 区域检测器 —— 挂载在 Player 上，根据当前地点同步玩家所处区域缓存
/// 当前项目主要通过地图/按钮显式切换地点，此组件不再主动发起 MoveTo，
/// 以免和传送逻辑互相覆盖导致地点立刻跳回。
/// </summary>
public class LocationZoneDetector : MonoBehaviour
{
    private LocationId? lastDetectedLocation;

    private void Start()
    {
        if (GameState.Instance != null)
        {
            lastDetectedLocation = GameState.Instance.CurrentLocation;
        }

        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged += OnExternalLocationChanged;
        }
    }

    private void Update()
    {
        if (LocationManager.Instance == null)
        {
            return;
        }

        float x = transform.position.x;
        LocationId? detected = LocationManager.Instance.GetLocationAtWorldX(x);
        if (!detected.HasValue)
        {
            return;
        }

        lastDetectedLocation = detected.Value;
    }

    private void OnExternalLocationChanged(LocationId from, LocationId to)
    {
        lastDetectedLocation = to;
    }

    private void OnDestroy()
    {
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChanged -= OnExternalLocationChanged;
        }
    }
}
