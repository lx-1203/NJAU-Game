using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Builds lightweight runtime scenery for each map location.
/// Custom art can override the generated placeholders through Resources/LocationScenes.
/// </summary>
public class LocationSceneBuilder : MonoBehaviour
{
    public static LocationSceneBuilder Instance { get; private set; }

    private const string RootName = "GeneratedLocationScenes";
    private const string PrefabPathPrefix = "LocationScenes/Prefabs/";
    private const string BackgroundPathPrefix = "LocationScenes/Backgrounds/";
    private const string ForegroundPathPrefix = "LocationScenes/Foregrounds/";
    private const int GeneratedScenerySortingOffset = -50;
    private const float FullScreenBackgroundY = -2.5f;
    private const float FullScreenBackgroundHeight = 11.2f;
    private const float GroundColliderHeight = 1.1f;
    private const float GroundCheckClearance = 0.6f;

    private static Sprite solidSprite;
    private readonly Dictionary<LocationId, Transform> locationRoots = new Dictionary<LocationId, Transform>();

    private Transform root;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        BuildAllLocations();
        ConfigureCamera();
    }

    public void BuildAllLocations()
    {
        if (LocationManager.Instance == null)
        {
            Debug.LogWarning("[LocationSceneBuilder] LocationManager is missing, scenery was not generated.");
            return;
        }

        ClearExistingRoot();
        root = new GameObject(RootName).transform;

        LocationDefinition[] locations = LocationManager.Instance.GetAllLocations();
        foreach (LocationDefinition location in locations)
        {
            BuildLocation(location);
        }
    }

    private void BuildLocation(LocationDefinition location)
    {
        if (location == null) return;

        GameObject locationObj = new GameObject($"LocationScene_{location.id}");
        locationObj.transform.SetParent(root);
        locationObj.transform.position = Vector3.zero;
        locationRoots[location.id] = locationObj.transform;

        GameObject prefab = Resources.Load<GameObject>(PrefabPathPrefix + location.id);
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, locationObj.transform);
            instance.name = $"ArtPrefab_{location.id}";
            instance.transform.position = new Vector3(location.worldCenterX, 0f, 0f);
        }
        else
        {
            BuildGeneratedLocation(location, locationObj.transform);
        }

        BuildAirWalls(location, locationObj.transform);
    }

    private void BuildGeneratedLocation(LocationDefinition location, Transform parent)
    {
        ScenePalette palette = GetPalette(location.id);
        float minX = location.worldMinX;
        float maxX = location.worldMaxX;
        float centerX = location.worldCenterX;
        float width = Mathf.Max(18f, maxX - minX);

        Sprite customBackground = LoadLocationSprite(BackgroundPathPrefix, location.id);
        if (customBackground != null)
        {
            CreateSprite("CustomBackground", parent, customBackground, new Vector3(centerX, FullScreenBackgroundY, 8f), new Vector2(width, FullScreenBackgroundHeight), 0, Color.white, true);
        }
        else
        {
            CreateRect("Sky", parent, new Vector3(centerX, FullScreenBackgroundY, 8f), new Vector2(width, FullScreenBackgroundHeight), palette.sky, 0);
            CreateRect("Horizon", parent, new Vector3(centerX, -2.1f, 7f), new Vector2(width, 5.4f), palette.horizon, 1);
            BuildPlaceholderLandmarks(location.id, parent, centerX, palette);
        }

        BuildGround(parent, location, palette);

        Sprite customForeground = LoadLocationSprite(ForegroundPathPrefix, location.id);
        if (customForeground != null)
        {
            CreateSprite("CustomForeground", parent, customForeground, new Vector3(centerX, FullScreenBackgroundY, -1f), new Vector2(width, FullScreenBackgroundHeight), 8, Color.white, true);
        }

        CreateLocationMarker(parent, location, palette);
    }

    private Sprite LoadLocationSprite(string preferredPathPrefix, LocationId locationId)
    {
        string locationName = locationId.ToString();

        Sprite sprite = Resources.Load<Sprite>(preferredPathPrefix + locationName);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = FindSpriteByName("LocationScenes", locationName);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(preferredPathPrefix + locationName);
        if (texture == null)
        {
            texture = FindTextureByName("LocationScenes", locationName);
        }

        if (texture == null)
        {
            Debug.LogWarning($"[LocationSceneBuilder] No custom art found for {locationName}. Expected a sprite or texture named {locationName} under Assets/Resources/LocationScenes.");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite FindSpriteByName(string resourceFolder, string spriteName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourceFolder);
        foreach (Sprite sprite in sprites)
        {
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }

    private Texture2D FindTextureByName(string resourceFolder, string textureName)
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourceFolder);
        foreach (Texture2D texture in textures)
        {
            if (texture != null && texture.name == textureName)
            {
                return texture;
            }
        }

        return null;
    }

    private void BuildGround(Transform parent, LocationDefinition location, ScenePalette palette)
    {
        float width = Mathf.Max(18f, location.worldMaxX - location.worldMinX);
        float groundTopY = location.worldSpawnY - GroundCheckClearance;
        Vector3 position = new Vector3(location.worldCenterX, groundTopY - GroundColliderHeight * 0.5f, 0f);

        GameObject ground = new GameObject("GroundCollider");
        ground.transform.SetParent(parent);
        ground.transform.position = position;

        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, GroundColliderHeight);
        collider.offset = Vector2.zero;
    }

    private void BuildAirWalls(LocationDefinition location, Transform parent)
    {
        CreateAirWall("LeftAirWall", parent, location.worldMinX);
        CreateAirWall("RightAirWall", parent, location.worldMaxX);
    }

    private void CreateAirWall(string name, Transform parent, float worldX)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = new Vector3(worldX, 0f, 0f);

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 18f);
        collider.offset = Vector2.zero;
    }

    private void BuildPlaceholderLandmarks(LocationId id, Transform parent, float centerX, ScenePalette palette)
    {
        switch (id)
        {
            case LocationId.Dormitory:
                CreateBuilding(parent, centerX - 2.6f, -1.1f, 5.8f, 4.8f, palette.primary, palette.trim, 12, 4, 2);
                CreateRect("DormDoor", parent, new Vector3(centerX - 2.6f, -3.2f, -0.5f), new Vector2(0.9f, 1.6f), palette.accent, 14);
                CreateRect("DeskLamp", parent, new Vector3(centerX + 3.4f, -2.65f, -0.5f), new Vector2(1.8f, 0.25f), palette.accent, 14);
                break;
            case LocationId.Canteen:
                CreateBuilding(parent, centerX, -1.2f, 7f, 4.4f, palette.primary, palette.trim, 12, 5, 1);
                CreateRect("ServiceCounter", parent, new Vector3(centerX, -3.05f, -0.5f), new Vector2(5.4f, 0.75f), palette.accent, 14);
                CreateRect("Awning", parent, new Vector3(centerX, 0.95f, -0.5f), new Vector2(7.8f, 0.45f), palette.accent, 14);
                break;
            case LocationId.Store:
                CreateBuilding(parent, centerX - 0.5f, -1.25f, 6.2f, 4.2f, palette.primary, palette.trim, 12, 4, 1);
                CreateRect("StoreSign", parent, new Vector3(centerX - 0.5f, 0.95f, -0.5f), new Vector2(4.6f, 0.55f), palette.accent, 14);
                CreateRect("ShelfA", parent, new Vector3(centerX - 2.2f, -2.75f, -0.5f), new Vector2(0.8f, 1.5f), palette.trim, 14);
                CreateRect("ShelfB", parent, new Vector3(centerX + 1.2f, -2.75f, -0.5f), new Vector2(0.8f, 1.5f), palette.trim, 14);
                break;
            case LocationId.TeachingBuilding:
                CreateBuilding(parent, centerX, -0.9f, 8.5f, 5.1f, palette.primary, palette.trim, 12, 6, 2);
                CreateRect("MainSteps", parent, new Vector3(centerX, -3.4f, -0.5f), new Vector2(5.2f, 0.5f), palette.accent, 14);
                CreateRect("Clock", parent, new Vector3(centerX, 1.65f, -0.5f), new Vector2(1.1f, 1.1f), palette.accent, 14);
                break;
            case LocationId.Library:
                CreateBuilding(parent, centerX, -1.05f, 8.1f, 4.8f, palette.primary, palette.trim, 12, 5, 2);
                CreateRect("LibraryColumnsA", parent, new Vector3(centerX - 2.4f, -2f, -0.5f), new Vector2(0.35f, 2.5f), palette.accent, 14);
                CreateRect("LibraryColumnsB", parent, new Vector3(centerX, -2f, -0.5f), new Vector2(0.35f, 2.5f), palette.accent, 14);
                CreateRect("LibraryColumnsC", parent, new Vector3(centerX + 2.4f, -2f, -0.5f), new Vector2(0.35f, 2.5f), palette.accent, 14);
                break;
            case LocationId.Playground:
                CreateRect("Track", parent, new Vector3(centerX, -3.58f, -0.5f), new Vector2(11f, 0.6f), palette.primary, 13);
                CreateRect("TrackLineA", parent, new Vector3(centerX, -3.46f, -0.6f), new Vector2(11f, 0.05f), Color.white, 14);
                CreateRect("GoalPostLeft", parent, new Vector3(centerX - 4.6f, -2.6f, -0.5f), new Vector2(0.16f, 1.9f), palette.accent, 14);
                CreateRect("GoalPostTop", parent, new Vector3(centerX - 3.75f, -1.72f, -0.5f), new Vector2(1.85f, 0.16f), palette.accent, 14);
                break;
            case LocationId.ExpressStation:
                CreateBuilding(parent, centerX - 0.8f, -1.55f, 5.2f, 3.6f, palette.primary, palette.trim, 12, 3, 1);
                CreateRect("ParcelStackA", parent, new Vector3(centerX + 2.5f, -3.1f, -0.5f), new Vector2(1.1f, 0.8f), palette.accent, 14);
                CreateRect("ParcelStackB", parent, new Vector3(centerX + 3.25f, -2.65f, -0.5f), new Vector2(0.9f, 0.7f), palette.trim, 14);
                break;
            case LocationId.TakeoutStation:
                CreateBuilding(parent, centerX, -1.65f, 5.8f, 3.4f, palette.primary, palette.trim, 12, 3, 1);
                CreateRect("PickupShelter", parent, new Vector3(centerX + 2.9f, -2.55f, -0.5f), new Vector2(2.1f, 1.9f), palette.accent, 14);
                CreateRect("PickupRoof", parent, new Vector3(centerX + 2.9f, -1.55f, -0.5f), new Vector2(2.5f, 0.25f), palette.trim, 15);
                break;
        }
    }

    private void CreateBuilding(Transform parent, float x, float y, float width, float height, Color wall, Color trim, int order, int columns, int rows)
    {
        CreateRect("Building", parent, new Vector3(x, y, 0f), new Vector2(width, height), wall, order);
        CreateRect("Roof", parent, new Vector3(x, y + height * 0.5f + 0.22f, -0.2f), new Vector2(width + 0.8f, 0.45f), trim, order + 1);

        float startX = x - width * 0.35f;
        float stepX = columns > 1 ? width * 0.7f / (columns - 1) : 0f;
        float startY = y + height * 0.18f;
        float stepY = rows > 1 ? height * 0.28f : 0f;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                CreateRect("Window", parent, new Vector3(startX + column * stepX, startY - row * stepY, -0.3f), new Vector2(0.55f, 0.45f), new Color(0.82f, 0.9f, 0.95f, 1f), order + 2);
            }
        }
    }

    private void CreateLocationMarker(Transform parent, LocationDefinition location, ScenePalette palette)
    {
        GameObject label = new GameObject("LocationMarker");
        label.transform.SetParent(parent);
        label.transform.position = new Vector3(location.worldCenterX, 2.75f, -1f);

        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.text = location.id.ToString();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 4.2f;
        text.color = palette.label;
        text.sortingOrder = 30 + GeneratedScenerySortingOffset;

        label.transform.localScale = Vector3.one * 0.18f;
    }

    private GameObject CreateRect(string name, Transform parent, Vector3 position, Vector2 size, Color color, int sortingOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSolidSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder + GeneratedScenerySortingOffset;
        return obj;
    }

    private GameObject CreateSprite(string name, Transform parent, Sprite sprite, Vector3 position, Vector2 targetSize, int sortingOrder, Color color, bool preserveAspectCover = false)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = position;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder + GeneratedScenerySortingOffset;

        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x > 0f && spriteSize.y > 0f)
        {
            if (preserveAspectCover)
            {
                float scale = Mathf.Max(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y);
                obj.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                obj.transform.localScale = new Vector3(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y, 1f);
            }
        }

        return obj;
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null) return solidSprite;

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.name = "RuntimeSolidPixel";
        solidSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        solidSprite.name = "RuntimeSolidSprite";
        return solidSprite;
    }

    private ScenePalette GetPalette(LocationId id)
    {
        switch (id)
        {
            case LocationId.Dormitory:
                return new ScenePalette(new Color(0.74f, 0.84f, 0.9f), new Color(0.86f, 0.76f, 0.63f), new Color(0.56f, 0.39f, 0.35f), new Color(0.35f, 0.23f, 0.21f), new Color(0.92f, 0.63f, 0.38f), new Color(0.18f, 0.13f, 0.11f));
            case LocationId.Canteen:
                return new ScenePalette(new Color(0.83f, 0.91f, 0.82f), new Color(0.93f, 0.82f, 0.58f), new Color(0.67f, 0.42f, 0.25f), new Color(0.41f, 0.26f, 0.18f), new Color(0.9f, 0.43f, 0.25f), new Color(0.16f, 0.12f, 0.09f));
            case LocationId.Store:
                return new ScenePalette(new Color(0.79f, 0.88f, 0.93f), new Color(0.68f, 0.78f, 0.74f), new Color(0.38f, 0.58f, 0.61f), new Color(0.25f, 0.38f, 0.42f), new Color(0.98f, 0.78f, 0.28f), new Color(0.08f, 0.17f, 0.2f));
            case LocationId.TeachingBuilding:
                return new ScenePalette(new Color(0.75f, 0.86f, 0.96f), new Color(0.79f, 0.82f, 0.78f), new Color(0.5f, 0.62f, 0.72f), new Color(0.31f, 0.39f, 0.48f), new Color(0.83f, 0.86f, 0.78f), new Color(0.1f, 0.14f, 0.18f));
            case LocationId.Library:
                return new ScenePalette(new Color(0.68f, 0.79f, 0.86f), new Color(0.73f, 0.7f, 0.62f), new Color(0.45f, 0.36f, 0.3f), new Color(0.25f, 0.21f, 0.18f), new Color(0.78f, 0.62f, 0.42f), new Color(0.12f, 0.09f, 0.07f));
            case LocationId.Playground:
                return new ScenePalette(new Color(0.64f, 0.83f, 0.94f), new Color(0.58f, 0.78f, 0.55f), new Color(0.65f, 0.28f, 0.2f), new Color(0.2f, 0.48f, 0.24f), new Color(0.95f, 0.95f, 0.86f), new Color(0.04f, 0.16f, 0.08f));
            case LocationId.ExpressStation:
                return new ScenePalette(new Color(0.75f, 0.82f, 0.88f), new Color(0.67f, 0.67f, 0.62f), new Color(0.47f, 0.47f, 0.42f), new Color(0.29f, 0.29f, 0.27f), new Color(0.85f, 0.55f, 0.25f), new Color(0.12f, 0.12f, 0.1f));
            case LocationId.TakeoutStation:
                return new ScenePalette(new Color(0.79f, 0.86f, 0.9f), new Color(0.69f, 0.77f, 0.65f), new Color(0.44f, 0.48f, 0.45f), new Color(0.23f, 0.28f, 0.25f), new Color(0.32f, 0.7f, 0.48f), new Color(0.08f, 0.15f, 0.11f));
            default:
                return new ScenePalette(new Color(0.74f, 0.84f, 0.9f), new Color(0.75f, 0.75f, 0.7f), new Color(0.45f, 0.48f, 0.5f), new Color(0.25f, 0.28f, 0.3f), new Color(0.75f, 0.65f, 0.45f), Color.black);
        }
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.72f, 0.83f, 0.9f);
    }

    private void ClearExistingRoot()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Destroy(existing);
        }

        locationRoots.Clear();
    }

    private struct ScenePalette
    {
        public readonly Color sky;
        public readonly Color horizon;
        public readonly Color primary;
        public readonly Color ground;
        public readonly Color trim;
        public readonly Color accent;
        public readonly Color label;

        public ScenePalette(Color sky, Color horizon, Color primary, Color ground, Color accent, Color label)
        {
            this.sky = sky;
            this.horizon = horizon;
            this.primary = primary;
            this.ground = ground;
            this.trim = Color.Lerp(primary, ground, 0.35f);
            this.accent = accent;
            this.label = label;
        }
    }
}
