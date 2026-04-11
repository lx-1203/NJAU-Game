using UnityEngine;
using TMPro;

/// <summary>
/// NPC 控制器
/// 处理NPC图片加载、玩家靠近检测、显示互动提示
/// 已迁移到 TextMeshPro（3D世界空间版），通过 FontManager 自动获取中文字体
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("NPC 设置")]
    [SerializeField] private string npcName = "同学";
    [SerializeField] private float interactionRange = 2.5f;

    [Header("对话内容")]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "你好！欢迎来到这个世界。",
        "这里是一个充满冒险的地方。",
        "如果你需要帮助，随时可以来找我。",
        "祝你旅途愉快！"
    };

    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool playerInRange = false;
    private GameObject interactionHint;
    private float hintBobTime = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        LoadNPCSprite();
        CreateInteractionHint();
    }

    private void Start()
    {
        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void LoadNPCSprite()
    {
        Sprite npcSprite = Resources.Load<Sprite>("NPCSprite");
        if (npcSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = npcSprite;
            spriteRenderer.color = Color.white;

            // 调整NPC大小
            float targetHeight = 2f;
            float spriteHeight = npcSprite.bounds.size.y;
            float scale = targetHeight / spriteHeight;
            transform.localScale = new Vector3(scale, scale, 1f);

            Debug.Log("NPC图片加载成功！");
        }
    }

    private void CreateInteractionHint()
    {
        // 创建提示容器
        interactionHint = new GameObject("InteractionHint");
        interactionHint.transform.SetParent(transform);
        interactionHint.transform.localPosition = new Vector3(0, 1.6f / transform.localScale.y, 0);

        // --- 背景气泡 ---
        GameObject bgObj = new GameObject("HintBG", typeof(SpriteRenderer));
        bgObj.transform.SetParent(interactionHint.transform, false);
        bgObj.transform.localPosition = Vector3.zero;

        SpriteRenderer bgSr = bgObj.GetComponent<SpriteRenderer>();
        bgSr.sprite = CreateRoundSprite();
        bgSr.color = new Color(0f, 0f, 0f, 0.55f);
        bgSr.sortingOrder = 9;
        bgObj.transform.localScale = new Vector3(1.8f, 0.6f, 1f);

        // --- 按键图标 "E"（使用 TMP 3D 版本） ---
        GameObject keyObj = new GameObject("KeyIcon");
        keyObj.transform.SetParent(interactionHint.transform, false);
        keyObj.transform.localPosition = new Vector3(-0.28f, 0.02f, 0);

        TextMeshPro keyText = keyObj.AddComponent<TextMeshPro>();
        keyText.text = "E";
        keyText.fontSize = 6f;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = new Color(1f, 0.85f, 0.3f);
        keyText.fontStyle = FontStyles.Bold;
        keyText.sortingOrder = 11;
        // "E" 是英文字母，TMP 默认字体就能显示

        // --- 按键背景框 ---
        GameObject keyBgObj = new GameObject("KeyBG", typeof(SpriteRenderer));
        keyBgObj.transform.SetParent(interactionHint.transform, false);
        keyBgObj.transform.localPosition = new Vector3(-0.28f, 0.02f, 0);

        SpriteRenderer keyBgSr = keyBgObj.GetComponent<SpriteRenderer>();
        keyBgSr.sprite = CreateRoundSprite();
        keyBgSr.color = new Color(1f, 0.85f, 0.3f, 0.25f);
        keyBgSr.sortingOrder = 10;
        keyBgObj.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        // --- 文字 "对话"（使用 TMP 3D 版本 + 中文字体） ---
        GameObject textObj = new GameObject("HintLabel");
        textObj.transform.SetParent(interactionHint.transform, false);
        textObj.transform.localPosition = new Vector3(0.15f, 0.02f, 0);

        TextMeshPro labelText = textObj.AddComponent<TextMeshPro>();
        labelText.text = "对话";
        labelText.fontSize = 5f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.sortingOrder = 11;
        // 自动应用中文字体
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

    /// <summary>
    /// 创建一个简单的圆形/椭圆 Sprite 用作背景
    /// </summary>
    private Sprite CreateRoundSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;
        float radius = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < radius - 1)
                    tex.SetPixel(x, y, Color.white);
                else if (dist < radius)
                    tex.SetPixel(x, y, new Color(1, 1, 1, (radius - dist)));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRange;

        // 显示/隐藏互动提示
        if (interactionHint != null)
        {
            interactionHint.SetActive(playerInRange && !DialogueSystem.Instance.IsDialogueActive);

            // 提示气泡轻微上下浮动
            if (interactionHint.activeSelf)
            {
                hintBobTime += Time.deltaTime * 2.5f;
                float baseY = 1.6f / transform.localScale.y;
                float bobOffset = Mathf.Sin(hintBobTime) * 0.08f;
                interactionHint.transform.localPosition = new Vector3(0, baseY + bobOffset, 0);
            }
        }

        // 按E键触发对话
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!DialogueSystem.Instance.IsDialogueActive)
            {
                StartDialogue();
            }
        }
    }

    private void StartDialogue()
    {
        if (interactionHint != null)
        {
            interactionHint.SetActive(false);
        }

        DialogueSystem.Instance.StartDialogue(npcName, dialogueLines);
    }

    public string NPCName => npcName;
    public bool IsPlayerInRange => playerInRange;

    private void OnDrawGizmosSelected()
    {
        // 在编辑器中显示互动范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
