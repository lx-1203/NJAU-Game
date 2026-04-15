using UnityEngine;

/// <summary>
/// 相机跟随脚本 —— 平滑跟随玩家水平移动
/// 仅跟随X轴（横版游戏），Y轴保持固定
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("边界限制")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;

    private Transform target;
    private float fixedY;

    private void Start()
    {
        // 记录相机初始Y位置，横版游戏Y轴不跟随
        fixedY = transform.position.y;

        // 自动查找玩家
        FindTarget();
    }

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning("[CameraFollow] 未找到 Player，将在 Update 中重试");
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        // 目标位置：跟随玩家X，保持固定Y
        float targetX = target.position.x + offset.x;

        // 边界限制
        if (useBounds)
        {
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }

        Vector3 desiredPosition = new Vector3(targetX, fixedY, offset.z);

        // 平滑跟随
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
