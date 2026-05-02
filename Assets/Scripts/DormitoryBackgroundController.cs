using UnityEngine;

/// <summary>
/// Creates a replaceable background segment for the dormitory area in GameScene.
/// Swap the image by replacing the sprites under Resources/Backgrounds.
/// </summary>
public class DormitoryBackgroundController : MonoBehaviour
{
    [SerializeField] private string maleSpriteResourcePath = "Backgrounds/DormitoryTemporaryBackground";
    [SerializeField] private string femaleSpriteResourcePath = "Backgrounds/DormitoryFemaleBackground";
    [SerializeField] private string backgroundObjectName = "DormitoryBackground";
    [SerializeField] private int sortingOrder = -10;
    [SerializeField] private float verticalPadding = 1f;
    [SerializeField] private float fallbackHeight = 12f;
    [SerializeField] private float zPosition = 0f;

    [Header("Collision")]
    [SerializeField] private bool buildBoundaryColliders = true;
    [SerializeField] private string leftBoundaryObjectName = "DormitoryLeftBoundary";
    [SerializeField] private string rightBoundaryObjectName = "DormitoryRightBoundary";
    [SerializeField] private string doorBlockerObjectName = "DormitoryDoorBlocker";
    [SerializeField] private float boundaryThickness = 0.6f;
    [SerializeField] private float boundaryHeight = 12f;
    [SerializeField] private Vector2 leftBoundaryOffset = Vector2.zero;
    [SerializeField] private Vector2 rightBoundaryOffset = Vector2.zero;
    [SerializeField] private bool createDoorBlocker = false;
    [SerializeField] [Range(0f, 1f)] private float doorCenterNormalizedX = 0.355f;
    [SerializeField] [Range(0.01f, 0.3f)] private float doorWidthNormalized = 0.075f;
    [SerializeField] private float doorHeight = 4.2f;
    [SerializeField] private float doorBottomOffset = 0.1f;
    [SerializeField] private Vector2 doorOffset = Vector2.zero;

    private SpriteRenderer backgroundRenderer;
    private BoxCollider2D leftBoundaryCollider;
    private BoxCollider2D rightBoundaryCollider;
    private BoxCollider2D doorBlockerCollider;

    private void Start()
    {
        BuildOrRefreshBackground();
    }

    private void OnValidate()
    {
        boundaryThickness = Mathf.Max(0.05f, boundaryThickness);
        boundaryHeight = Mathf.Max(0.1f, boundaryHeight);
        doorHeight = Mathf.Max(0.1f, doorHeight);
        doorBottomOffset = Mathf.Max(0f, doorBottomOffset);
        doorWidthNormalized = Mathf.Max(0.01f, doorWidthNormalized);
    }

    public void BuildOrRefreshBackground()
    {
        if (LocationManager.Instance == null)
        {
            Debug.LogWarning("[DormitoryBackground] LocationManager not ready.");
            return;
        }

        string spriteResourcePath = ResolveSpriteResourcePath();
        Sprite backgroundSprite = Resources.Load<Sprite>(spriteResourcePath);
        if (backgroundSprite == null)
        {
            Debug.LogWarning("[DormitoryBackground] Missing sprite at Resources/" + spriteResourcePath);
            return;
        }

        LocationDefinition dormitory = LocationManager.Instance.GetLocation(LocationId.Dormitory);
        if (dormitory == null)
        {
            Debug.LogWarning("[DormitoryBackground] Dormitory location definition not found.");
            return;
        }

        EnsureRenderer();
        ApplySprite(backgroundSprite, dormitory);
        RefreshBoundaryColliders();
    }

    private string ResolveSpriteResourcePath()
    {
        int gender = GameState.Instance != null ? GameState.Instance.PlayerGender : 0;
        bool useFemaleSprite = gender == 1 && !string.IsNullOrWhiteSpace(femaleSpriteResourcePath);
        return useFemaleSprite ? femaleSpriteResourcePath : maleSpriteResourcePath;
    }

    private void EnsureRenderer()
    {
        if (backgroundRenderer != null)
        {
            return;
        }

        Transform child = transform.Find(backgroundObjectName);
        if (child == null)
        {
            GameObject backgroundObject = new GameObject(backgroundObjectName);
            child = backgroundObject.transform;
            child.SetParent(transform, false);
        }

        backgroundRenderer = child.GetComponent<SpriteRenderer>();
        if (backgroundRenderer == null)
        {
            backgroundRenderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        backgroundRenderer.sortingOrder = sortingOrder;
    }

    private void ApplySprite(Sprite sprite, LocationDefinition dormitory)
    {
        backgroundRenderer.sprite = sprite;

        Camera sceneCamera = Camera.main;
        float targetWidth = Mathf.Max(1f, dormitory.worldMaxX - dormitory.worldMinX);
        float targetHeight = GetTargetHeight(sceneCamera);
        float uniformScale = GetUniformScale(sprite, targetWidth, targetHeight);

        Transform backgroundTransform = backgroundRenderer.transform;
        backgroundTransform.position = new Vector3(
            dormitory.worldCenterX,
            sceneCamera != null ? sceneCamera.transform.position.y : -2.5f,
            zPosition);
        backgroundTransform.localScale = new Vector3(uniformScale, uniformScale, 1f);
    }

    private void RefreshBoundaryColliders()
    {
        if (!buildBoundaryColliders || !TryGetBackgroundBounds(out Bounds bounds))
        {
            return;
        }

        EnsureBoundaryCollider(ref leftBoundaryCollider, leftBoundaryObjectName);
        EnsureBoundaryCollider(ref rightBoundaryCollider, rightBoundaryObjectName);

        float colliderHeight = Mathf.Max(boundaryHeight, bounds.size.y + 2f);
        float centerY = bounds.center.y;

        ApplyBoundaryCollider(
            leftBoundaryCollider,
            bounds.min.x - boundaryThickness * 0.5f + leftBoundaryOffset.x,
            centerY + leftBoundaryOffset.y,
            boundaryThickness,
            colliderHeight);
        ApplyBoundaryCollider(
            rightBoundaryCollider,
            bounds.max.x + boundaryThickness * 0.5f + rightBoundaryOffset.x,
            centerY + rightBoundaryOffset.y,
            boundaryThickness,
            colliderHeight);

        if (createDoorBlocker)
        {
            EnsureBoundaryCollider(ref doorBlockerCollider, doorBlockerObjectName);

            float doorCenterX = Mathf.Lerp(bounds.min.x, bounds.max.x, doorCenterNormalizedX) + doorOffset.x;
            float doorWidth = Mathf.Max(0.5f, bounds.size.x * doorWidthNormalized);
            float doorCenterY = bounds.min.y + doorBottomOffset + doorHeight * 0.5f + doorOffset.y;
            ApplyBoundaryCollider(doorBlockerCollider, doorCenterX, doorCenterY, doorWidth, doorHeight);
        }
        else
        {
            DisableDoorBlocker();
        }
    }

    public bool TryGetBackgroundBounds(out Bounds bounds)
    {
        bounds = default;
        if (backgroundRenderer == null || backgroundRenderer.sprite == null)
        {
            return false;
        }

        bounds = backgroundRenderer.bounds;
        return true;
    }

    private float GetTargetHeight(Camera sceneCamera)
    {
        if (sceneCamera != null && sceneCamera.orthographic)
        {
            return sceneCamera.orthographicSize * 2f + verticalPadding;
        }

        return fallbackHeight;
    }

    private static float GetUniformScale(Sprite sprite, float targetWidth, float targetHeight)
    {
        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return 1f;
        }

        float scaleX = targetWidth / spriteSize.x;
        float scaleY = targetHeight / spriteSize.y;
        return Mathf.Max(scaleX, scaleY);
    }

    private void EnsureBoundaryCollider(ref BoxCollider2D collider2D, string objectName)
    {
        if (collider2D != null)
        {
            return;
        }

        Transform child = transform.Find(objectName);
        if (child == null)
        {
            GameObject blocker = new GameObject(objectName);
            child = blocker.transform;
            child.SetParent(transform, false);
        }

        collider2D = child.GetComponent<BoxCollider2D>();
        if (collider2D == null)
        {
            collider2D = child.gameObject.AddComponent<BoxCollider2D>();
        }

        collider2D.isTrigger = false;
    }

    private void DisableDoorBlocker()
    {
        if (doorBlockerCollider == null)
        {
            Transform existing = transform.Find(doorBlockerObjectName);
            if (existing != null)
            {
                doorBlockerCollider = existing.GetComponent<BoxCollider2D>();
            }
        }

        if (doorBlockerCollider != null)
        {
            doorBlockerCollider.enabled = false;
        }
    }

    private static void ApplyBoundaryCollider(BoxCollider2D collider2D, float centerX, float centerY, float width, float height)
    {
        Transform colliderTransform = collider2D.transform;
        colliderTransform.position = new Vector3(centerX, centerY, 0f);
        colliderTransform.localScale = Vector3.one;
        collider2D.offset = Vector2.zero;
        collider2D.size = new Vector2(width, height);
        collider2D.enabled = true;
    }

    private void OnDrawGizmos()
    {
        if (!buildBoundaryColliders || !TryGetBackgroundBounds(out Bounds bounds))
        {
            return;
        }

        float colliderHeight = Mathf.Max(boundaryHeight, bounds.size.y + 2f);
        float centerY = bounds.center.y;

        DrawColliderPreview(
            bounds.min.x - boundaryThickness * 0.5f + leftBoundaryOffset.x,
            centerY + leftBoundaryOffset.y,
            boundaryThickness,
            colliderHeight,
            Color.yellow);
        DrawColliderPreview(
            bounds.max.x + boundaryThickness * 0.5f + rightBoundaryOffset.x,
            centerY + rightBoundaryOffset.y,
            boundaryThickness,
            colliderHeight,
            Color.yellow);

        if (createDoorBlocker)
        {
            float doorCenterX = Mathf.Lerp(bounds.min.x, bounds.max.x, doorCenterNormalizedX) + doorOffset.x;
            float doorWidth = Mathf.Max(0.5f, bounds.size.x * doorWidthNormalized);
            float doorCenterY = bounds.min.y + doorBottomOffset + doorHeight * 0.5f + doorOffset.y;
            DrawColliderPreview(doorCenterX, doorCenterY, doorWidth, doorHeight, new Color(1f, 0.5f, 0f));
        }
    }

    private static void DrawColliderPreview(float centerX, float centerY, float width, float height, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(new Vector3(centerX, centerY, 0f), new Vector3(width, height, 0.05f));
    }
}
