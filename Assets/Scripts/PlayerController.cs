using UnityEngine;

/// <summary>
/// 横版2D游戏 - 玩家控制器
/// 支持左右移动、跳跃、站立动画
/// 支持键盘 (A/D, 方向键) 和鼠标点击两种操控方式
/// 键盘输入会立即打断鼠标点击移动
/// 依赖 SpriteSheetAnimator 组件
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteSheetAnimator))]
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("地面检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("动画帧率")]
    [SerializeField] private float idleFrameRate = 4f;
    [SerializeField] private float walkFrameRate = 8f;

    [Header("鼠标点击移动")]
    [SerializeField] private float clickMoveStopDistance = 0.15f; // 到达目标点的判定距离

    private Rigidbody2D rb;
    private SpriteSheetAnimator animator;
    private bool isGrounded;
    private float moveInput;
    private bool facingRight = true;

    // 鼠标点击移动状态
    private bool isClickMoving = false;       // 是否正在鼠标点击移动中
    private float clickTargetX;               // 鼠标点击的目标X坐标
    private Camera mainCamera;

    // 动画状态
    private const string ANIM_IDLE = "Idle";
    private const string ANIM_WALK = "Walk";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<SpriteSheetAnimator>();
        mainCamera = Camera.main;

        // 如果没有手动设置 groundCheck，自动创建一个
        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = check.transform;
        }
    }

    private void Start()
    {
        // 设置动画帧率
        ConfigureAnimations();

        // 播放待机动画
        animator.Play(ANIM_IDLE);
    }

    private void ConfigureAnimations()
    {
        // 从 Resources/PlayerWalkSprites 自动加载切片好的精灵帧
        // Idle 和 Walk 暂时共用同一套帧（Idle 慢播，Walk 快播）
        animator.LoadFromResources("PlayerWalkSprites", ANIM_IDLE, idleFrameRate, true);
        animator.LoadFromResources("PlayerWalkSprites", ANIM_WALK, walkFrameRate, true);

        // 调整角色大小
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            sr.color = Color.white;
            float targetHeight = 3f;
            float spriteHeight = sr.sprite.bounds.size.y;
            if (spriteHeight > 0)
            {
                float scale = targetHeight / spriteHeight;
                transform.localScale = new Vector3(scale, scale, 1f);
            }

            // 调整碰撞体
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                float aspectRatio = sr.sprite.bounds.size.x / sr.sprite.bounds.size.y;
                col.size = new Vector2(aspectRatio * 0.6f, 0.9f);
                col.offset = Vector2.zero;
            }
        }
    }

    private void Update()
    {
        // 对话进行中或事件执行中时，禁止移动
        if ((DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) ||
            (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting))
        {
            moveInput = 0;
            isClickMoving = false;
            UpdateAnimation(0f);
            return;
        }

        // 获取键盘水平输入 (A/D 或 左/右方向键)
        float keyboardInput = Input.GetAxisRaw("Horizontal");

        // 键盘输入优先：如果有键盘输入，立即打断鼠标点击移动
        if (Mathf.Abs(keyboardInput) > 0.01f)
        {
            isClickMoving = false;
            moveInput = keyboardInput;
        }
        // 鼠标左键点击：设置目标点
        else if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            clickTargetX = mouseWorldPos.x;
            isClickMoving = true;
        }

        // 处理鼠标点击移动逻辑
        if (isClickMoving)
        {
            float diff = clickTargetX - transform.position.x;

            // 到达目标点，停止移动
            if (Mathf.Abs(diff) <= clickMoveStopDistance)
            {
                isClickMoving = false;
                moveInput = 0;
            }
            else
            {
                // 朝目标方向移动 (-1 或 +1)
                moveInput = Mathf.Sign(diff);
            }
        }
        else if (Mathf.Abs(keyboardInput) < 0.01f)
        {
            // 既没有键盘输入也没有点击移动，停止
            moveInput = 0;
        }

        // 地面检测
        // 如果 groundLayer 未设置 (为0)，则使用射线检测地面（忽略 Layer 过滤）
        if (groundLayer.value != 0)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            // groundLayer 未配置时，向下发射短射线检测是否站在任何碰撞体上
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.1f);
            isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;
        }

        // 跳跃 (空格键 或 鼠标右键)
        if ((Input.GetButtonDown("Jump") || Input.GetMouseButtonDown(1)) && isGrounded)
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

        // 更新动画状态
        UpdateAnimation(moveInput);
    }

    private void UpdateAnimation(float input)
    {
        float absInput = Mathf.Abs(input);

        if (absInput < 0.1f)
        {
            // 待机
            if (animator.CurrentClipName != ANIM_IDLE)
                animator.Play(ANIM_IDLE, true);
        }
        else
        {
            // 行走
            if (animator.CurrentClipName != ANIM_WALK)
                animator.Play(ANIM_WALK, true);
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

        // 方案1: 翻转 SpriteRenderer（简单，但方向反转）
        // 精灵帧如果是侧面朝右的，需要用这个
        GetComponent<SpriteRenderer>().flipX = !GetComponent<SpriteRenderer>().flipX;

        // 方案2: 如果精灵是正面图，需要旋转或换图：
        // transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    // ==================== 公开方法，供外部调用 ====================

    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    public float MoveSpeed => moveSpeed;

    /// <summary>
    /// 是否面朝右
    /// </summary>
    public bool FacingRight => facingRight;
}
