using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LocationSceneController : MonoBehaviour
{
    private static Sprite fallbackSolidSprite;

    [Serializable]
    public class ObstacleBox
    {
        public string name = "Obstacle";
        public bool enabled = true;
        public Vector2 center = Vector2.zero;
        public Vector2 size = Vector2.one;
    }

    [Serializable]
    public class LocationSceneProfile
    {
        public LocationId locationId = LocationId.Dormitory;

        [Header("Background")]
        public string backgroundResourcePath = string.Empty;
        public string femaleBackgroundResourcePath = string.Empty;
        public int sortingOrder = -10;
        public float backgroundCenterY = -2.5f;
        public float backgroundZ = 0f;
        public float verticalPadding = 1f;
        public float fallbackHeight = 12f;

        [Header("Location Bounds")]
        public float worldMinX = -10f;
        public float worldMaxX = 20f;
        public float spawnY = -3.5f;

        [Header("Ground")]
        public float floorY = -5.5f;
        public float groundThickness = 1f;

        [Header("Boundaries")]
        public bool createSideBoundaries = true;
        public float sideBoundaryThickness = 0.6f;
        public float sideBoundaryHeight = 12f;
        public Vector2 leftBoundaryCenter = new Vector2(-10.3f, 0.5f);
        public Vector2 leftBoundarySize = new Vector2(0.6f, 12f);
        public Vector2 rightBoundaryCenter = new Vector2(20.3f, 0.5f);
        public Vector2 rightBoundarySize = new Vector2(0.6f, 12f);

        [Header("Obstacles")]
        public List<ObstacleBox> obstacles = new List<ObstacleBox>();
    }

    private const string GeneratedRootName = "_GeneratedLocationScene";
    private const string BackgroundObjectName = "Background";
    private const string BackgroundLabelObjectName = "BackgroundLabel";
    private const string CollisionRootName = "Collisions";
    private const string GroundObjectName = "Ground";
    private const string LeftBoundaryName = "LeftBoundary";
    private const string RightBoundaryName = "RightBoundary";

    [Header("Preview")]
    [SerializeField] private LocationId previewLocation = LocationId.Dormitory;
    [SerializeField] private bool useCurrentLocationInPlayMode = true;
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] [Range(0, 1)] private int previewPlayerGenderInEditMode;

    [Header("Legacy Scene")]
    [SerializeField] private bool disableLegacyGround = true;
    [SerializeField] private string legacyGroundObjectName = "Ground";

    [Header("Profiles")]
    [SerializeField] private List<LocationSceneProfile> profiles = new List<LocationSceneProfile>();

    private LocationManager subscribedLocationManager;
#if UNITY_EDITOR
    private bool editorRefreshQueued;
#endif

    private void Reset()
    {
        EnsureDefaultProfiles();
        EnsureEditorNPCManager();
        RebuildScene();
    }

    private void OnEnable()
    {
        EnsureDefaultProfiles();
        SubscribeToLocationChanges();
        EnsureEditorNPCManager();

        if (!Application.isPlaying && previewInEditMode)
        {
            RebuildScene();
        }
    }

    private void Start()
    {
        EnsureDefaultProfiles();
        SubscribeToLocationChanges();
        RebuildScene();
    }

    private void OnDisable()
    {
        UnsubscribeFromLocationChanges();
    }

    private void OnDestroy()
    {
        UnsubscribeFromLocationChanges();
    }

    private void OnValidate()
    {
        ClampProfiles();
        QueueEditorRefresh();
    }

    public void EnsureDefaultProfiles()
    {
        if (profiles.Count > 0)
        {
            return;
        }

        AddMissingProfiles();
    }

    private void EnsureEditorNPCManager()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (FindFirstObjectByType<NPCManager>() != null)
        {
            return;
        }

        GameObject npcManagerObject = new GameObject("NPCManager");
        npcManagerObject.AddComponent<NPCManager>();
    }

    private void QueueEditorRefresh()
    {
        if (Application.isPlaying)
        {
            return;
        }

#if UNITY_EDITOR
        if (editorRefreshQueued)
        {
            return;
        }

        editorRefreshQueued = true;
        EditorApplication.delayCall += ProcessQueuedEditorRefresh;
#endif
    }

#if UNITY_EDITOR
    private void ProcessQueuedEditorRefresh()
    {
        EditorApplication.delayCall -= ProcessQueuedEditorRefresh;
        editorRefreshQueued = false;

        if (this == null)
        {
            return;
        }

        ClampProfiles();
        EnsureDefaultProfiles();
        EnsureEditorNPCManager();

        if (previewInEditMode)
        {
            RebuildScene();
        }
    }
#endif

    public void RebuildScene()
    {
        EnsureDefaultProfiles();
        ClampProfiles();
        SyncLocationDefinitions();
        UpdateLegacyGroundVisibility();

        LocationSceneProfile profile = GetActiveProfile();
        if (profile == null)
        {
            ClearGeneratedScene();
            return;
        }

        Transform generatedRoot = GetOrCreateChild(transform, GeneratedRootName);
        generatedRoot.gameObject.hideFlags = HideFlags.DontSave;
        BuildBackground(generatedRoot, profile);
        BuildCollisions(generatedRoot, profile);
    }

    public bool HasProfile(LocationId locationId)
    {
        return FindProfile(locationId) != null;
    }

    public LocationSceneProfile GetPreviewProfile()
    {
        return FindProfile(previewLocation);
    }

    public void SetPreviewLocation(LocationId locationId)
    {
        previewLocation = locationId;

        if (!Application.isPlaying && previewInEditMode)
        {
            RebuildScene();
        }
    }

    public void SetPreviewPlayerGenderInEditMode(int gender)
    {
        previewPlayerGenderInEditMode = Mathf.Clamp(gender, 0, 1);

        if (!Application.isPlaying && previewInEditMode)
        {
            RebuildScene();
        }
    }

    public void AddProfile(LocationId locationId)
    {
        if (HasProfile(locationId))
        {
            return;
        }

        LocationSceneProfile profile = CreateProfileFromLocation(locationId);
        profiles.Add(profile);
    }

    public void EnsureProfile(LocationId locationId)
    {
        AddProfile(locationId);
    }

    public void AddMissingProfiles()
    {
        Array allLocationIds = Enum.GetValues(typeof(LocationId));
        for (int i = 0; i < allLocationIds.Length; i++)
        {
            AddProfile((LocationId)allLocationIds.GetValue(i));
        }
    }

    public void SnapActiveProfileWallsToBounds()
    {
        LocationSceneProfile profile = GetActiveProfile();
        if (profile == null)
        {
            return;
        }

        float boundaryCenterY = profile.floorY + profile.sideBoundaryHeight * 0.5f;
        profile.leftBoundaryCenter = new Vector2(profile.worldMinX - profile.sideBoundaryThickness * 0.5f, boundaryCenterY);
        profile.leftBoundarySize = new Vector2(profile.sideBoundaryThickness, profile.sideBoundaryHeight);
        profile.rightBoundaryCenter = new Vector2(profile.worldMaxX + profile.sideBoundaryThickness * 0.5f, boundaryCenterY);
        profile.rightBoundarySize = new Vector2(profile.sideBoundaryThickness, profile.sideBoundaryHeight);
    }

    public void SyncActiveBoundsFromWalls()
    {
        LocationSceneProfile profile = GetActiveProfile();
        if (profile == null)
        {
            return;
        }

        profile.worldMinX = profile.leftBoundaryCenter.x + profile.leftBoundarySize.x * 0.5f;
        profile.worldMaxX = profile.rightBoundaryCenter.x - profile.rightBoundarySize.x * 0.5f;
    }

    public void SnapActiveSpawnToGround(float offset = 1.5f)
    {
        LocationSceneProfile profile = GetActiveProfile();
        if (profile == null)
        {
            return;
        }

        profile.spawnY = profile.floorY + Mathf.Max(0f, offset);
    }

    private void SubscribeToLocationChanges()
    {
        if (!Application.isPlaying || LocationManager.Instance == null || subscribedLocationManager == LocationManager.Instance)
        {
            return;
        }

        if (subscribedLocationManager != null)
        {
            subscribedLocationManager.OnLocationChanged -= HandleLocationChanged;
        }

        subscribedLocationManager = LocationManager.Instance;
        subscribedLocationManager.OnLocationChanged += HandleLocationChanged;
    }

    private void UnsubscribeFromLocationChanges()
    {
        if (subscribedLocationManager == null)
        {
            return;
        }

        subscribedLocationManager.OnLocationChanged -= HandleLocationChanged;
        subscribedLocationManager = null;
    }

    private void HandleLocationChanged(LocationId from, LocationId to)
    {
        RebuildScene();
    }

    private void SyncLocationDefinitions()
    {
        if (LocationManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < profiles.Count; i++)
        {
            LocationSceneProfile profile = profiles[i];
            LocationDefinition location = LocationManager.Instance.GetLocation(profile.locationId);
            if (location == null)
            {
                continue;
            }

            location.worldMinX = profile.worldMinX;
            location.worldMaxX = profile.worldMaxX;
            location.worldCenterX = (profile.worldMinX + profile.worldMaxX) * 0.5f;
            location.worldSpawnY = profile.spawnY;
        }
    }

    private LocationSceneProfile GetActiveProfile()
    {
        LocationId activeLocation = previewLocation;

        if (Application.isPlaying && useCurrentLocationInPlayMode && GameState.Instance != null)
        {
            activeLocation = GameState.Instance.CurrentLocation;
        }

        return FindProfile(activeLocation) ?? GetPreviewProfile();
    }

    private LocationSceneProfile FindProfile(LocationId locationId)
    {
        for (int i = 0; i < profiles.Count; i++)
        {
            if (profiles[i].locationId == locationId)
            {
                return profiles[i];
            }
        }

        return null;
    }

    private void BuildBackground(Transform root, LocationSceneProfile profile)
    {
        Transform backgroundTransform = GetOrCreateChild(root, BackgroundObjectName);
        SpriteRenderer spriteRenderer = backgroundTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = backgroundTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        Sprite sprite = LoadBackgroundSprite(profile);
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = profile.sortingOrder;
        backgroundTransform.position = new Vector3(
            (profile.worldMinX + profile.worldMaxX) * 0.5f,
            profile.backgroundCenterY,
            profile.backgroundZ);

        if (sprite == null)
        {
            BuildFallbackBackground(backgroundTransform, spriteRenderer, profile);
            return;
        }

        DestroyChildIfExists(backgroundTransform, BackgroundLabelObjectName);
        spriteRenderer.color = Color.white;
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.size = Vector2.one;
        float targetWidth = Mathf.Max(1f, profile.worldMaxX - profile.worldMinX);
        float targetHeight = GetTargetHeight(profile);
        float scale = GetUniformScale(sprite, targetWidth, targetHeight);
        backgroundTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void BuildFallbackBackground(Transform backgroundTransform, SpriteRenderer spriteRenderer, LocationSceneProfile profile)
    {
        float targetWidth = Mathf.Max(1f, profile.worldMaxX - profile.worldMinX);
        float targetHeight = Mathf.Max(6f, GetTargetHeight(profile));

        spriteRenderer.sprite = GetFallbackSolidSprite();
        spriteRenderer.color = GetFallbackBackgroundColor(profile.locationId);
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.size = Vector2.one;
        backgroundTransform.localScale = new Vector3(targetWidth * 0.5f, targetHeight * 0.5f, 1f);

        Transform labelTransform = GetOrCreateChild(backgroundTransform, BackgroundLabelObjectName);
        labelTransform.localPosition = new Vector3(0f, targetHeight * 0.22f, -0.1f);

        TextMesh textMesh = labelTransform.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = labelTransform.gameObject.AddComponent<TextMesh>();
        }

        textMesh.text = $"{GetLocationDisplayName(profile.locationId)} 预览";
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.18f;
        textMesh.fontSize = 48;
        textMesh.color = new Color(1f, 1f, 1f, 0.88f);

        MeshRenderer textRenderer = labelTransform.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingOrder = profile.sortingOrder + 1;
        }
    }

    private void BuildCollisions(Transform root, LocationSceneProfile profile)
    {
        Transform collisionRoot = GetOrCreateChild(root, CollisionRootName);
        int layer = ResolveCollisionLayer();

        float width = Mathf.Max(0.1f, profile.worldMaxX - profile.worldMinX);
        float centerX = (profile.worldMinX + profile.worldMaxX) * 0.5f;
        float groundCenterY = profile.floorY - profile.groundThickness * 0.5f;

        BuildBoxCollider(collisionRoot, GroundObjectName, layer, new Vector2(centerX, groundCenterY), new Vector2(width, profile.groundThickness));

        if (profile.createSideBoundaries)
        {
            EnsureBoundaryLayout(profile);
            BuildBoxCollider(
                collisionRoot,
                LeftBoundaryName,
                layer,
                profile.leftBoundaryCenter,
                profile.leftBoundarySize);
            BuildBoxCollider(
                collisionRoot,
                RightBoundaryName,
                layer,
                profile.rightBoundaryCenter,
                profile.rightBoundarySize);
        }
        else
        {
            DestroyChildIfExists(collisionRoot, LeftBoundaryName);
            DestroyChildIfExists(collisionRoot, RightBoundaryName);
        }

        HashSet<string> activeObstacleNames = new HashSet<string>();
        for (int i = 0; i < profile.obstacles.Count; i++)
        {
            ObstacleBox obstacle = profile.obstacles[i];
            string obstacleName = GetObstacleObjectName(i, obstacle.name);
            activeObstacleNames.Add(obstacleName);

            if (!obstacle.enabled)
            {
                DestroyChildIfExists(collisionRoot, obstacleName);
                continue;
            }

            BuildBoxCollider(collisionRoot, obstacleName, layer, obstacle.center, obstacle.size);
        }

        CleanupUnusedObstacleObjects(collisionRoot, activeObstacleNames);
    }

    private void BuildBoxCollider(Transform parent, string objectName, int layer, Vector2 center, Vector2 size)
    {
        Transform colliderTransform = GetOrCreateChild(parent, objectName);
        colliderTransform.gameObject.layer = layer;
        colliderTransform.position = new Vector3(center.x, center.y, 0f);
        colliderTransform.localScale = Vector3.one;

        BoxCollider2D collider2D = colliderTransform.GetComponent<BoxCollider2D>();
        if (collider2D == null)
        {
            collider2D = colliderTransform.gameObject.AddComponent<BoxCollider2D>();
        }

        collider2D.offset = Vector2.zero;
        collider2D.size = new Vector2(Mathf.Max(0.05f, size.x), Mathf.Max(0.05f, size.y));
        collider2D.enabled = true;
    }

    private void CleanupUnusedObstacleObjects(Transform collisionRoot, HashSet<string> activeObstacleNames)
    {
        List<string> staleChildren = new List<string>();
        for (int i = 0; i < collisionRoot.childCount; i++)
        {
            string childName = collisionRoot.GetChild(i).name;
            bool isReserved = childName == GroundObjectName || childName == LeftBoundaryName || childName == RightBoundaryName;
            if (!isReserved && !activeObstacleNames.Contains(childName))
            {
                staleChildren.Add(childName);
            }
        }

        for (int i = 0; i < staleChildren.Count; i++)
        {
            DestroyChildIfExists(collisionRoot, staleChildren[i]);
        }
    }

    private void UpdateLegacyGroundVisibility()
    {
        GameObject legacyGround = GameObject.Find(legacyGroundObjectName);
        if (legacyGround == null)
        {
            return;
        }

        bool shouldEnable = !disableLegacyGround;
        SpriteRenderer spriteRenderer = legacyGround.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = shouldEnable;
        }

        BoxCollider2D boxCollider = legacyGround.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = shouldEnable;
        }
    }

    private void ClearGeneratedScene()
    {
        Transform generatedRoot = transform.Find(GeneratedRootName);
        if (generatedRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedRoot.gameObject);
        }
        else
        {
            DestroyImmediate(generatedRoot.gameObject);
        }
    }

    private Sprite LoadBackgroundSprite(LocationSceneProfile profile)
    {
        string resourcePath = profile.backgroundResourcePath;
        bool useFemale = ResolvePreviewPlayerGender() == 1 &&
                         !string.IsNullOrWhiteSpace(profile.femaleBackgroundResourcePath);
        if (useFemale)
        {
            resourcePath = profile.femaleBackgroundResourcePath;
        }

        return string.IsNullOrWhiteSpace(resourcePath) ? null : Resources.Load<Sprite>(resourcePath);
    }

    private int ResolvePreviewPlayerGender()
    {
        if (Application.isPlaying && GameState.Instance != null)
        {
            return Mathf.Clamp(GameState.Instance.PlayerGender, 0, 1);
        }

        return Mathf.Clamp(previewPlayerGenderInEditMode, 0, 1);
    }

    private float GetTargetHeight(LocationSceneProfile profile)
    {
        Camera sceneCamera = Camera.main;
        if (sceneCamera != null && sceneCamera.orthographic)
        {
            return sceneCamera.orthographicSize * 2f + profile.verticalPadding;
        }

        return profile.fallbackHeight;
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

    private int ResolveCollisionLayer()
    {
        GameObject legacyGround = GameObject.Find(legacyGroundObjectName);
        return legacyGround != null ? legacyGround.layer : gameObject.layer;
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        Transform childTransform = childObject.transform;
        childTransform.SetParent(parent, false);
        return childTransform;
    }

    private static void DestroyChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(child.gameObject);
        }
        else
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private static string GetObstacleObjectName(int index, string customName)
    {
        string suffix = string.IsNullOrWhiteSpace(customName) ? "Obstacle" : customName.Trim();
        return $"Obstacle_{index}_{suffix}";
    }

    private void ClampProfiles()
    {
        for (int i = 0; i < profiles.Count; i++)
        {
            LocationSceneProfile profile = profiles[i];
            if (profile.worldMaxX < profile.worldMinX)
            {
                float swap = profile.worldMinX;
                profile.worldMinX = profile.worldMaxX;
                profile.worldMaxX = swap;
            }

            profile.groundThickness = Mathf.Max(0.05f, profile.groundThickness);
            profile.sideBoundaryThickness = Mathf.Max(0.05f, profile.sideBoundaryThickness);
            profile.sideBoundaryHeight = Mathf.Max(0.1f, profile.sideBoundaryHeight);
            EnsureBoundaryLayout(profile);
            profile.leftBoundarySize.x = Mathf.Max(0.05f, profile.leftBoundarySize.x);
            profile.leftBoundarySize.y = Mathf.Max(0.1f, profile.leftBoundarySize.y);
            profile.rightBoundarySize.x = Mathf.Max(0.05f, profile.rightBoundarySize.x);
            profile.rightBoundarySize.y = Mathf.Max(0.1f, profile.rightBoundarySize.y);

            for (int j = 0; j < profile.obstacles.Count; j++)
            {
                profile.obstacles[j].size.x = Mathf.Max(0.05f, profile.obstacles[j].size.x);
                profile.obstacles[j].size.y = Mathf.Max(0.05f, profile.obstacles[j].size.y);
            }
        }
    }

    private static void EnsureBoundaryLayout(LocationSceneProfile profile)
    {
        float defaultBoundaryCenterY = profile.floorY + profile.sideBoundaryHeight * 0.5f;
        Vector2 defaultLeftCenter = new Vector2(profile.worldMinX - profile.sideBoundaryThickness * 0.5f, defaultBoundaryCenterY);
        Vector2 defaultRightCenter = new Vector2(profile.worldMaxX + profile.sideBoundaryThickness * 0.5f, defaultBoundaryCenterY);
        Vector2 defaultSize = new Vector2(profile.sideBoundaryThickness, profile.sideBoundaryHeight);

        if (profile.leftBoundarySize.x <= 0f || profile.leftBoundarySize.y <= 0f)
        {
            profile.leftBoundarySize = defaultSize;
        }

        if (profile.rightBoundarySize.x <= 0f || profile.rightBoundarySize.y <= 0f)
        {
            profile.rightBoundarySize = defaultSize;
        }

        if (profile.leftBoundaryCenter == Vector2.zero)
        {
            profile.leftBoundaryCenter = defaultLeftCenter;
        }

        if (profile.rightBoundaryCenter == Vector2.zero)
        {
            profile.rightBoundaryCenter = defaultRightCenter;
        }
    }

    private static LocationSceneProfile CreateDormitoryProfile()
    {
        return new LocationSceneProfile
        {
            locationId = LocationId.Dormitory,
            backgroundResourcePath = "Backgrounds/DormitoryTemporaryBackground",
            femaleBackgroundResourcePath = "Backgrounds/DormitoryFemaleBackground",
            sortingOrder = -10,
            backgroundCenterY = -2.5f,
            backgroundZ = 0f,
            verticalPadding = 1f,
            fallbackHeight = 12f,
            worldMinX = -10f,
            worldMaxX = 20f,
            spawnY = -3.5f,
            floorY = -5f,
            groundThickness = 1f,
            createSideBoundaries = true,
            sideBoundaryThickness = 0.6f,
            sideBoundaryHeight = 12f,
            leftBoundaryCenter = new Vector2(-10.3f, 1f),
            leftBoundarySize = new Vector2(0.6f, 12f),
            rightBoundaryCenter = new Vector2(20.3f, 1f),
            rightBoundarySize = new Vector2(0.6f, 12f),
            obstacles = new List<ObstacleBox>
            {
                new ObstacleBox
                {
                    name = "Bed",
                    center = new Vector2(12f, -2.75f),
                    size = new Vector2(4.2f, 5.5f)
                }
            }
        };
    }

    private LocationSceneProfile CreateProfileFromLocation(LocationId locationId)
    {
        LocationDefinition definition = LocationManager.Instance != null
            ? LocationManager.Instance.GetLocation(locationId)
            : null;

        float minX = definition != null ? definition.worldMinX : -10f;
        float maxX = definition != null ? definition.worldMaxX : 20f;
        float spawnY = definition != null ? definition.worldSpawnY : -3.5f;
        float floorY = spawnY - 1.5f;
        float boundaryCenterY = floorY + 6f;

        return new LocationSceneProfile
        {
            locationId = locationId,
            backgroundResourcePath = GetDefaultBackgroundResourcePath(locationId),
            backgroundCenterY = -2.5f,
            backgroundZ = 0f,
            verticalPadding = 1f,
            fallbackHeight = GetDefaultFallbackHeight(locationId),
            worldMinX = minX,
            worldMaxX = maxX,
            spawnY = spawnY,
            floorY = floorY,
            groundThickness = 1f,
            sideBoundaryThickness = 0.6f,
            sideBoundaryHeight = 12f,
            leftBoundaryCenter = new Vector2(
                minX - 0.3f,
                boundaryCenterY),
            leftBoundarySize = new Vector2(0.6f, 12f),
            rightBoundaryCenter = new Vector2(
                maxX + 0.3f,
                boundaryCenterY),
            rightBoundarySize = new Vector2(0.6f, 12f)
        };
    }

    private static string GetDefaultBackgroundResourcePath(LocationId locationId)
    {
        switch (locationId)
        {
            case LocationId.Dormitory:
                return "Backgrounds/DormitoryTemporaryBackground";
            case LocationId.TeachingBuilding:
                return "LocationScenes/TeachingBuildings/TeachingBuilding";
            case LocationId.Library:
                return "LocationScenes/Librarys/Library";
            case LocationId.Canteen:
                return "LocationScenes/Canteens/Canteen";
            case LocationId.Playground:
                return "LocationScenes/Playgrounds/Playground";
            case LocationId.Store:
                return "LocationScenes/Stores/Store";
            case LocationId.ExpressStation:
                return "LocationScenes/ExpressStation/ExpressStation";
            case LocationId.TakeoutStation:
            default:
                return string.Empty;
        }
    }

    private static float GetDefaultFallbackHeight(LocationId locationId)
    {
        switch (locationId)
        {
            case LocationId.Playground:
                return 10f;
            case LocationId.ExpressStation:
                return 9.5f;
            default:
                return 12f;
        }
    }

    private static Color GetFallbackBackgroundColor(LocationId locationId)
    {
        switch (locationId)
        {
            case LocationId.Dormitory:
                return new Color(0.30f, 0.36f, 0.50f, 0.95f);
            case LocationId.TeachingBuilding:
                return new Color(0.40f, 0.52f, 0.70f, 0.95f);
            case LocationId.Library:
                return new Color(0.34f, 0.44f, 0.56f, 0.95f);
            case LocationId.Canteen:
                return new Color(0.52f, 0.42f, 0.34f, 0.95f);
            case LocationId.Playground:
                return new Color(0.28f, 0.52f, 0.42f, 0.95f);
            case LocationId.Store:
                return new Color(0.50f, 0.44f, 0.34f, 0.95f);
            case LocationId.ExpressStation:
                return new Color(0.44f, 0.40f, 0.34f, 0.95f);
            case LocationId.TakeoutStation:
                return new Color(0.38f, 0.46f, 0.54f, 0.95f);
            default:
                return new Color(0.35f, 0.40f, 0.48f, 0.95f);
        }
    }

    private static string GetLocationDisplayName(LocationId locationId)
    {
        switch (locationId)
        {
            case LocationId.Dormitory: return "宿舍";
            case LocationId.TeachingBuilding: return "教学楼";
            case LocationId.Library: return "图书馆";
            case LocationId.Canteen: return "食堂";
            case LocationId.Playground: return "操场";
            case LocationId.Store: return "教超";
            case LocationId.ExpressStation: return "快递站";
            case LocationId.TakeoutStation: return "外卖站";
            default: return locationId.ToString();
        }
    }

    private static Sprite GetFallbackSolidSprite()
    {
        if (fallbackSolidSprite != null)
        {
            return fallbackSolidSprite;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[4] { Color.white, Color.white, Color.white, Color.white };
        texture.SetPixels(pixels);
        texture.Apply();

        fallbackSolidSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 1f);
        fallbackSolidSprite.name = "LocationSceneFallbackSprite";
        return fallbackSolidSprite;
    }

    private void OnDrawGizmos()
    {
        LocationSceneProfile profile = GetActiveProfile();
        if (profile == null)
        {
            return;
        }

        float centerX = (profile.worldMinX + profile.worldMaxX) * 0.5f;
        float width = profile.worldMaxX - profile.worldMinX;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawLine(new Vector3(profile.worldMinX, profile.floorY, 0f), new Vector3(profile.worldMaxX, profile.floorY, 0f));
        Gizmos.DrawWireCube(new Vector3(centerX, profile.floorY - profile.groundThickness * 0.5f, 0f), new Vector3(width, profile.groundThickness, 0.05f));

        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.9f);
        Gizmos.DrawSphere(new Vector3(centerX, profile.spawnY, 0f), 0.18f);

        if (profile.createSideBoundaries)
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            EnsureBoundaryLayout(profile);
            Gizmos.DrawWireCube(
                new Vector3(profile.leftBoundaryCenter.x, profile.leftBoundaryCenter.y, 0f),
                new Vector3(profile.leftBoundarySize.x, profile.leftBoundarySize.y, 0.05f));
            Gizmos.DrawWireCube(
                new Vector3(profile.rightBoundaryCenter.x, profile.rightBoundaryCenter.y, 0f),
                new Vector3(profile.rightBoundarySize.x, profile.rightBoundarySize.y, 0.05f));
        }

        Gizmos.color = new Color(1f, 0.45f, 0.2f, 0.9f);
        for (int i = 0; i < profile.obstacles.Count; i++)
        {
            ObstacleBox obstacle = profile.obstacles[i];
            if (!obstacle.enabled)
            {
                continue;
            }

            Gizmos.DrawWireCube(new Vector3(obstacle.center.x, obstacle.center.y, 0f), new Vector3(obstacle.size.x, obstacle.size.y, 0.05f));
        }
    }
}
