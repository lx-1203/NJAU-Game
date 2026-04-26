using TMPro;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private float interactionRange = 2.5f;

    private string npcId;
    private NPCData npcData;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D clickCollider;
    private Transform player;
    private PlayerController playerController;
    private bool playerInRange;
    private GameObject interactionHint;
    private float hintBobTime;

    public string NpcId => npcId;
    public string NPCName => npcData != null ? npcData.displayName : string.Empty;
    public bool IsPlayerInRange => playerInRange;

    public void Initialize(NPCData data)
    {
        npcId = data.id;
        npcData = data;

        spriteRenderer = GetComponent<SpriteRenderer>();
        clickCollider = GetComponent<BoxCollider2D>();

        LoadNPCSprite();
        ConfigureClickCollider();
        CreateInteractionHint();

        Debug.Log($"[NPCController] Initialized NPC {npcData.displayName} ({npcId})");
    }

    private void Start()
    {
        CachePlayerReferences();
    }

    private void CachePlayerReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            return;
        }

        player = playerObj.transform;
        playerController = playerObj.GetComponent<PlayerController>();
    }

    private void LoadNPCSprite()
    {
        if (npcData == null || spriteRenderer == null)
        {
            return;
        }

        string spritePath = !string.IsNullOrEmpty(npcData.portraitId) ? npcData.portraitId : "NPCSprite";
        Sprite npcSprite = Resources.Load<Sprite>(spritePath);
        if (npcSprite == null)
        {
            Debug.LogWarning($"[NPCController] Failed to load sprite for {npcId} from Resources/{spritePath}");
            return;
        }

        spriteRenderer.sprite = npcSprite;
        spriteRenderer.color = Color.white;

        float targetHeight = 3f;
        float spriteHeight = npcSprite.bounds.size.y;
        if (spriteHeight > 0f)
        {
            float scale = targetHeight / spriteHeight;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void ConfigureClickCollider()
    {
        if (clickCollider == null)
        {
            clickCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            clickCollider.size = new Vector2(spriteSize.x * 0.6f, spriteSize.y * 0.95f);
            clickCollider.offset = new Vector2(0f, spriteSize.y * 0.48f);
        }
        else
        {
            clickCollider.size = new Vector2(1.2f, 2.8f);
            clickCollider.offset = new Vector2(0f, 1.4f);
        }
    }

    private void CreateInteractionHint()
    {
        interactionHint = new GameObject("InteractionHint");
        interactionHint.transform.SetParent(transform);
        interactionHint.transform.localPosition = new Vector3(0f, 1.6f / transform.localScale.y, 0f);

        GameObject bgObj = new GameObject("HintBG", typeof(SpriteRenderer));
        bgObj.transform.SetParent(interactionHint.transform, false);
        SpriteRenderer bgRenderer = bgObj.GetComponent<SpriteRenderer>();
        bgRenderer.sprite = CreateRoundSprite();
        bgRenderer.color = new Color(0f, 0f, 0f, 0.55f);
        bgRenderer.sortingOrder = 9;
        bgObj.transform.localScale = new Vector3(1.8f, 0.6f, 1f);

        GameObject keyObj = new GameObject("KeyIcon");
        keyObj.transform.SetParent(interactionHint.transform, false);
        keyObj.transform.localPosition = new Vector3(-0.28f, 0.02f, 0f);

        TextMeshPro keyText = keyObj.AddComponent<TextMeshPro>();
        keyText.text = "E";
        keyText.fontSize = 6f;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = new Color(1f, 0.85f, 0.3f);
        keyText.fontStyle = FontStyles.Bold;
        keyText.sortingOrder = 11;

        GameObject keyBgObj = new GameObject("KeyBG", typeof(SpriteRenderer));
        keyBgObj.transform.SetParent(interactionHint.transform, false);
        keyBgObj.transform.localPosition = new Vector3(-0.28f, 0.02f, 0f);

        SpriteRenderer keyBgRenderer = keyBgObj.GetComponent<SpriteRenderer>();
        keyBgRenderer.sprite = CreateRoundSprite();
        keyBgRenderer.color = new Color(1f, 0.85f, 0.3f, 0.25f);
        keyBgRenderer.sortingOrder = 10;
        keyBgObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        string hintLabel = npcData != null ? npcData.displayName : "Interact";
        GameObject textObj = new GameObject("HintLabel");
        textObj.transform.SetParent(interactionHint.transform, false);
        textObj.transform.localPosition = new Vector3(0.15f, 0.02f, 0f);

        TextMeshPro labelText = textObj.AddComponent<TextMeshPro>();
        labelText.text = hintLabel;
        labelText.fontSize = 5f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.sortingOrder = 11;
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyChineseFont(labelText);
        }

        interactionHint.transform.localScale = new Vector3(
            1f / transform.localScale.x,
            1f / transform.localScale.y,
            1f);

        interactionHint.SetActive(false);
    }

    private Sprite CreateRoundSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size);
        float center = size / 2f;
        float radius = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance < radius - 1f)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else if (distance < radius)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, radius - distance));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Update()
    {
        if (player == null)
        {
            CachePlayerReferences();
            if (player == null)
            {
                return;
            }
        }

        float distance = Vector2.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRange;

        if (interactionHint != null)
        {
            bool dialogueActive = DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive;
            interactionHint.SetActive(playerInRange && !dialogueActive);

            if (interactionHint.activeSelf)
            {
                hintBobTime += Time.deltaTime * 2.5f;
                float baseY = 1.6f / transform.localScale.y;
                float bobOffset = Mathf.Sin(hintBobTime) * 0.08f;
                interactionHint.transform.localPosition = new Vector3(0f, baseY + bobOffset, 0f);
            }
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            bool dialogueActive = DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive;
            if (!dialogueActive)
            {
                HandleInteraction();
            }
        }
    }

    private void OnMouseDown()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive)
        {
            return;
        }

        if (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting)
        {
            return;
        }

        if (player == null || playerController == null)
        {
            CachePlayerReferences();
        }

        if (player == null || playerController == null)
        {
            return;
        }

        if (playerInRange)
        {
            HandleInteraction();
            return;
        }

        playerController.MoveToInteractionTarget(transform, interactionRange * 0.75f, TryInteractAfterApproach);
    }

    private void TryInteractAfterApproach()
    {
        if (player == null)
        {
            return;
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive)
        {
            return;
        }

        if (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= interactionRange + 0.1f)
        {
            playerInRange = true;
            HandleInteraction();
        }
    }

    private void HandleInteraction()
    {
        if (interactionHint != null)
        {
            interactionHint.SetActive(false);
        }

        if (NPCInteractionMenu.Instance != null)
        {
            NPCInteractionMenu.Instance.ShowForNPC(npcId);
            return;
        }

        if (NPCEventHub.Instance != null &&
            npcData != null &&
            npcData.greetingLines != null &&
            npcData.greetingLines.Length > 0)
        {
            NPCEventHub.DialogueRequest request = new NPCEventHub.DialogueRequest(
                npcId,
                npcData.displayName,
                npcData.greetingLines,
                npcData.portraitId);
            NPCEventHub.Instance.RaiseDialogueRequested(request);
            return;
        }

        if (DialogueSystem.Instance == null)
        {
            return;
        }

        if (npcData != null && !string.IsNullOrEmpty(npcData.dialogueId))
        {
            DialogueSystem.Instance.StartDialogue(npcData.dialogueId);
        }
        else
        {
            string speakerName = npcData != null ? npcData.displayName : string.Empty;
            string[] lines = npcData != null && npcData.greetingLines != null && npcData.greetingLines.Length > 0
                ? npcData.greetingLines
                : new[] { "..." };
            DialogueSystem.Instance.StartDialogue(speakerName, lines);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
