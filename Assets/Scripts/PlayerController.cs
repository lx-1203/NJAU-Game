using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteSheetAnimator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float minJumpForce = 6f;
    [SerializeField] private float maxJumpHoldTime = 0.2f;
    [SerializeField] private float jumpHoldForce = 25f;
    [SerializeField] [Range(0.1f, 1f)] private float jumpReleaseVelocityMultiplier = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animation Frame Rate")]
    [SerializeField] private float idleFrameRate = 4f;
    [SerializeField] private float walkFrameRate = 8f;
    [SerializeField] private float jumpFrameRate = 12f;

    [Header("Click Move")]
    [SerializeField] private float clickMoveStopDistance = 0.15f;

    private Rigidbody2D rb;
    private SpriteSheetAnimator animator;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;

    private bool isGrounded;
    private bool facingRight = true;
    private bool isClickMoving;
    private bool jumpHeld;
    private bool jumpRequested;
    private bool isJumping;
    private float moveInput;
    private float clickTargetX;
    private Action pendingArrivalAction;
    private Transform pendingInteractionTarget;
    private float interactionStopDistance;
    private float jumpHoldTimer;
    private bool rightMouseDrivingJump;

    private const string AnimIdle = "Idle";
    private const string AnimWalk = "Walk";
    private const string AnimJump = "Jump";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<SpriteSheetAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        if (groundCheck == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            groundCheck = check.transform;
        }
    }

    private void Start()
    {
        ConfigureAnimations();
        animator.Play(AnimIdle);
        SetFacingDirection(true);
    }

    private void ConfigureAnimations()
    {
        animator.LoadFromResources("PlayerIdleFrames", AnimIdle, idleFrameRate, true);
        animator.LoadFromResources("PlayerWalkFrames", AnimWalk, walkFrameRate, true, true);
        animator.LoadFromResources("PlayerJumpFrames", AnimJump, jumpFrameRate, false);

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            spriteRenderer.color = Color.white;

            float targetHeight = 3f;
            float spriteHeight = spriteRenderer.sprite.bounds.size.y;
            if (spriteHeight > 0f)
            {
                float scale = targetHeight / spriteHeight;
                transform.localScale = new Vector3(scale, scale, 1f);
            }

            BoxCollider2D collider2D = GetComponent<BoxCollider2D>();
            if (collider2D != null)
            {
                float aspectRatio = spriteRenderer.sprite.bounds.size.x / spriteRenderer.sprite.bounds.size.y;
                collider2D.size = new Vector2(aspectRatio * 0.6f, 0.9f);
                collider2D.offset = Vector2.zero;
            }
        }
    }

    private void Update()
    {
        if ((DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) ||
            (EventExecutor.Instance != null && EventExecutor.Instance.IsExecuting))
        {
            moveInput = 0f;
            isClickMoving = false;
            pendingArrivalAction = null;
            pendingInteractionTarget = null;
            jumpHeld = false;
            jumpRequested = false;
            UpdateAnimation(0f);
            return;
        }

        float keyboardInput = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(keyboardInput) > 0.01f)
        {
            isClickMoving = false;
            pendingArrivalAction = null;
            pendingInteractionTarget = null;
            moveInput = keyboardInput;
        }
        else if (Input.GetMouseButtonDown(0) &&
                 (UnityEngine.EventSystems.EventSystem.current == null ||
                  !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mouseWorldPoint2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPoint2D, Vector2.zero);

            if (hit.collider == null || hit.collider.GetComponent<NPCController>() == null)
            {
                pendingArrivalAction = null;
                pendingInteractionTarget = null;
                clickTargetX = mouseWorldPos.x;
                isClickMoving = true;
            }
        }

        if (isClickMoving)
        {
            if (pendingInteractionTarget != null)
            {
                float directionSign = Mathf.Sign(transform.position.x - pendingInteractionTarget.position.x);
                if (Mathf.Approximately(directionSign, 0f))
                {
                    directionSign = facingRight ? -1f : 1f;
                }

                clickTargetX = pendingInteractionTarget.position.x + directionSign * interactionStopDistance;
            }

            float diff = clickTargetX - transform.position.x;
            float stopDistance = pendingInteractionTarget != null
                ? Mathf.Max(clickMoveStopDistance, interactionStopDistance * 0.15f)
                : clickMoveStopDistance;

            if (Mathf.Abs(diff) <= stopDistance)
            {
                isClickMoving = false;
                moveInput = 0f;
                pendingInteractionTarget = null;

                Action arrivalAction = pendingArrivalAction;
                pendingArrivalAction = null;
                arrivalAction?.Invoke();
            }
            else
            {
                moveInput = Mathf.Sign(diff);
            }
        }
        else if (Mathf.Abs(keyboardInput) < 0.01f)
        {
            moveInput = 0f;
        }

        if (groundLayer.value != 0)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.1f);
            isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;
        }

        HandleJumpInput();

        if (moveInput > 0f)
        {
            SetFacingDirection(true);
        }
        else if (moveInput < 0f)
        {
            SetFacingDirection(false);
        }

        UpdateAnimation(moveInput);
    }

    private void UpdateAnimation(float input)
    {
        float absInput = Mathf.Abs(input);

        if (!isGrounded)
        {
            if (animator.CurrentClipName != AnimJump)
            {
                animator.Play(AnimJump, true);
            }
        }
        else if (absInput < 0.1f)
        {
            if (animator.CurrentClipName != AnimIdle)
            {
                animator.Play(AnimIdle, true);
            }
        }
        else
        {
            if (animator.CurrentClipName != AnimWalk)
            {
                animator.Play(AnimWalk, true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (jumpRequested && isGrounded)
        {
            StartJump();
        }

        if (isJumping && jumpHeld && rb.velocity.y > 0f && rb.velocity.y < jumpForce && jumpHoldTimer < maxJumpHoldTime)
        {
            rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(rb.velocity.y, jumpForce));
            jumpHoldTimer += Time.fixedDeltaTime;
        }

        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        if (isGrounded && rb.velocity.y <= 0f)
        {
            isJumping = false;
            jumpHoldTimer = 0f;
        }
    }

    private void SetFacingDirection(bool shouldFaceRight)
    {
        facingRight = shouldFaceRight;

        if (spriteRenderer != null)
        {
            // This sprite set faces left by default, so facing right needs flipX.
            spriteRenderer.flipX = shouldFaceRight;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    public float MoveSpeed => moveSpeed;
    public bool FacingRight => facingRight;

    private void HandleJumpInput()
    {
        bool keyboardJumpPressed = Input.GetButtonDown("Jump");
        bool keyboardJumpReleased = Input.GetButtonUp("Jump");
        bool keyboardJumpHeld = Input.GetButton("Jump");

        bool rightMousePressed = Input.GetMouseButtonDown(1);
        bool rightMouseReleased = Input.GetMouseButtonUp(1);
        bool rightMouseWasDrivingJump = rightMouseDrivingJump;

        if (rightMousePressed)
        {
            if (UIBackActionRouter.TryHandleBackAction())
            {
                rightMouseDrivingJump = false;
            }
            else
            {
                rightMouseDrivingJump = true;
            }
        }

        bool jumpPressed = keyboardJumpPressed || (rightMousePressed && rightMouseDrivingJump);
        bool jumpReleased = keyboardJumpReleased || (rightMouseReleased && rightMouseWasDrivingJump);
        jumpHeld = keyboardJumpHeld || rightMouseDrivingJump;

        if (rightMouseReleased)
        {
            rightMouseDrivingJump = false;
        }

        if (jumpPressed && isGrounded)
        {
            jumpRequested = true;
        }

        if (jumpReleased && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpReleaseVelocityMultiplier);
        }
    }

    private void StartJump()
    {
        jumpRequested = false;
        isJumping = true;
        jumpHoldTimer = 0f;
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(minJumpForce, jumpForce));
    }

    public void MoveToInteractionTarget(Transform target, float stopDistance, Action onArrived)
    {
        if (target == null)
        {
            return;
        }

        pendingInteractionTarget = target;
        interactionStopDistance = Mathf.Max(stopDistance, clickMoveStopDistance);
        pendingArrivalAction = onArrived;
        isClickMoving = true;

        float directionSign = Mathf.Sign(transform.position.x - target.position.x);
        if (Mathf.Approximately(directionSign, 0f))
        {
            directionSign = facingRight ? -1f : 1f;
        }

        clickTargetX = target.position.x + directionSign * interactionStopDistance;
    }
}
