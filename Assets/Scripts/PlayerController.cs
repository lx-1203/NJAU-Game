using UnityEngine;

/// <summary>
/// 横版2D游戏 - 玩家控制器
/// 支持左右移动和跳跃
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("地面检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private float moveInput;
    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 加载角色图片替换默认方块
        LoadPlayerSprite();

        // 如果没有手动设置 groundCheck，自动创建一个
        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = check.transform;
        }
    }

    private void LoadPlayerSprite()
    {
        Sprite playerSprite = Resources.Load<Sprite>("PlayerSprite");
        if (playerSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = playerSprite;
            spriteRenderer.color = Color.white; // 去掉蓝色滤镜，显示原图

            // 调整角色大小，让图片比例合适
            float targetHeight = 2f; // 角色高度（单位）
            float spriteHeight = playerSprite.bounds.size.y;
            float scale = targetHeight / spriteHeight;
            transform.localScale = new Vector3(scale, scale, 1f);

            // 调整碰撞体大小以匹配角色
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                float aspectRatio = playerSprite.bounds.size.x / playerSprite.bounds.size.y;
                col.size = new Vector2(aspectRatio * 0.6f, 0.9f);
                col.offset = new Vector2(0, 0);
            }

            Debug.Log("角色图片加载成功！");
        }
        else
        {
            Debug.LogWarning("未找到 Resources/PlayerSprite，使用默认方块");
        }
    }

    private void Update()
    {
        // 对话进行中时，禁止移动
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive)
        {
            moveInput = 0;
            return;
        }

        // 获取水平输入 (A/D 或 左/右方向键)
        moveInput = Input.GetAxisRaw("Horizontal");

        // 地面检测
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 跳跃
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // 翻转角色朝向
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        // 水平移动
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
