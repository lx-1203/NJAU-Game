using UnityEngine;

[ExecuteAlways]
public class NPCSceneAnchor : MonoBehaviour
{
    private const string DefaultNPCSpriteResourcePath = "NPCSprite";
    private const float DefaultVisualHeight = 3f;

    [SerializeField] private string npcId;
    [SerializeField] private LocationId locationId = LocationId.Dormitory;
    [SerializeField] private TimeSlot timeSlot = TimeSlot.Evening;

    public string NpcId => npcId;
    public LocationId LocationId => locationId;
    public TimeSlot TimeSlot => timeSlot;

    public void Configure(string newNpcId, LocationId newLocationId, TimeSlot newTimeSlot)
    {
        npcId = newNpcId;
        locationId = newLocationId;
        timeSlot = newTimeSlot;
        UpdateObjectName();
    }

    public bool Matches(string targetNpcId, LocationId targetLocationId, TimeSlot targetTimeSlot)
    {
        return npcId == targetNpcId && locationId == targetLocationId && timeSlot == targetTimeSlot;
    }

    public void ApplyPreviewVisual(NPCData data)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sortingOrder = 1;
        renderer.enabled = true;
        renderer.color = Color.white;
        renderer.sprite = LoadSprite(data);

        if (renderer.sprite == null)
        {
            return;
        }
    }

    public void ApplyDefaultScale(NPCData data)
    {
        Sprite sprite = LoadSprite(data);
        if (sprite == null)
        {
            return;
        }

        float spriteHeight = sprite.bounds.size.y;
        if (spriteHeight <= 0f)
        {
            return;
        }

        float scale = DefaultVisualHeight / spriteHeight;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void OnValidate()
    {
        UpdateObjectName();
    }

    private void UpdateObjectName()
    {
        string resolvedNpcId = string.IsNullOrWhiteSpace(npcId) ? "Unassigned" : npcId;
        gameObject.name = $"NPC_{resolvedNpcId}";
    }

    private static Sprite LoadSprite(NPCData data)
    {
        if (data != null && !string.IsNullOrWhiteSpace(data.portraitId))
        {
            Sprite portrait = Resources.Load<Sprite>(data.portraitId);
            if (portrait != null)
            {
                return portrait;
            }
        }

        return Resources.Load<Sprite>(DefaultNPCSpriteResourcePath);
    }
}
