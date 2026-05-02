using UnityEngine;

/// <summary>
/// Smoothly follows the player on the X axis and snaps immediately after long teleports.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float snapDistance = 12f;

    [Header("Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private bool useCurrentLocationBounds = true;
    [SerializeField] private float minX = -15f;
    [SerializeField] private float maxX = 220f;

    private Transform target;
    private float fixedY;
    private bool hasFixedY;
    private Camera attachedCamera;
    private bool hasWarnedMissingPlayer;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
        CaptureFixedY();
    }

    private void Start()
    {
        attachedCamera = GetComponent<Camera>();
        CaptureFixedY();
        RefreshBoundsForCurrentLocation();
        FindTarget();
    }

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            hasWarnedMissingPlayer = false;
        }
        else if (!hasWarnedMissingPlayer)
        {
            Debug.LogWarning("[CameraFollow] Player tag not found, retrying in LateUpdate.");
            hasWarnedMissingPlayer = true;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        RefreshBoundsForCurrentLocation();
        Vector3 desiredPosition = GetDesiredPosition();

        if (Mathf.Abs(transform.position.x - desiredPosition.x) >= snapDistance)
        {
            transform.position = desiredPosition;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    public void SnapToTarget()
    {
        CaptureFixedY();
        RefreshBoundsForCurrentLocation();

        if (target == null)
        {
            FindTarget();
        }

        if (target == null)
        {
            return;
        }

        transform.position = GetDesiredPosition();
    }

    public void SnapToWorldPosition(Vector3 worldPosition)
    {
        CaptureFixedY();
        RefreshBoundsForCurrentLocation();

        float targetX = worldPosition.x + offset.x;

        if (useBounds)
        {
            ResolveActiveBounds(out float activeMinX, out float activeMaxX);
            targetX = Mathf.Clamp(targetX, activeMinX, activeMaxX);
        }

        transform.position = new Vector3(targetX, fixedY, offset.z);
    }

    private Vector3 GetDesiredPosition()
    {
        CaptureFixedY();

        float targetX = target.position.x + offset.x;

        if (useBounds)
        {
            ResolveActiveBounds(out float activeMinX, out float activeMaxX);
            targetX = Mathf.Clamp(targetX, activeMinX, activeMaxX);
        }

        return new Vector3(targetX, fixedY, offset.z);
    }

    private void CaptureFixedY()
    {
        if (hasFixedY)
        {
            return;
        }

        fixedY = transform.position.y;
        hasFixedY = true;
    }

    private void RefreshBoundsForCurrentLocation()
    {
        if (!useCurrentLocationBounds || LocationManager.Instance == null || GameState.Instance == null)
        {
            return;
        }

        LocationDefinition location = LocationManager.Instance.GetLocation(GameState.Instance.CurrentLocation);
        if (location == null)
        {
            return;
        }

        float halfViewWidth = GetCameraHalfWidth();
        float cameraMinX = location.worldMinX + halfViewWidth;
        float cameraMaxX = location.worldMaxX - halfViewWidth;

        if (cameraMinX > cameraMaxX)
        {
            float center = (location.worldMinX + location.worldMaxX) * 0.5f;
            minX = center;
            maxX = center;
        }
        else
        {
            minX = cameraMinX;
            maxX = cameraMaxX;
        }

        useBounds = true;
    }

    private void ResolveActiveBounds(out float activeMinX, out float activeMaxX)
    {
        activeMinX = minX;
        activeMaxX = maxX;

        if (!useCurrentLocationBounds)
        {
            return;
        }

        RefreshBoundsForCurrentLocation();
        activeMinX = minX;
        activeMaxX = maxX;
    }

    private float GetCameraHalfWidth()
    {
        Camera cameraToUse = attachedCamera != null ? attachedCamera : GetComponent<Camera>();
        if (cameraToUse == null)
        {
            cameraToUse = Camera.main;
        }

        if (cameraToUse != null && cameraToUse.orthographic)
        {
            return cameraToUse.orthographicSize * cameraToUse.aspect;
        }

        return 9f;
    }
}
