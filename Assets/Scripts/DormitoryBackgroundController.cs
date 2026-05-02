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

    private SpriteRenderer backgroundRenderer;

    private void Start()
    {
        BuildOrRefreshBackground();
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
}
