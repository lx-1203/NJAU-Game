using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteSheetAnimator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    private const int MaleGenderValue = 0;
    private const int FemaleGenderValue = 1;

    private const string MaleIdleResourcePath = "MalePlayerIdleFrames";
    private const string MaleWalkResourcePath = "MalePlayerWalkFrames";
    private const string MaleJumpResourcePath = "MalePlayerJumpFrames";
    private const string MaleIdlePreviewResourcePath = "MalePlayerIdleFrames/IdleFrame_00";
    private const string FemaleIdleResourcePath = "PlayerIdleFrames";
    private const string FemaleWalkResourcePath = "PlayerWalkFrames";
    private const string FemaleJumpResourcePath = "PlayerJumpFrames";
    private const string FemaleIdlePreviewResourcePath = "PlayerIdleFrames/IdleFrame_00";

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

    [Header("Collision")]
    [SerializeField] private Vector2 colliderSize = new Vector2(0.6f, 0.9f);
    [SerializeField] private Vector2 colliderOffset = Vector2.zero;

    [Header("Animation Frame Rate")]
    [SerializeField] private float idleFrameRate = 4f;
    [SerializeField] private float walkFrameRate = 8f;
    [SerializeField] private float jumpFrameRate = 12f;

    [Header("Visual")]
    [SerializeField] private bool autoScaleToVisualHeight = true;
    [SerializeField] private float maleVisualHeight = 3f;
    [SerializeField] private float femaleVisualHeight = 3f;

    [Header("Click Move")]
    [SerializeField] private float clickMoveStopDistance = 0.15f;

    [Header("Scene Bounds")]
    [SerializeField] private bool constrainToCurrentLocation = true;
    [SerializeField] private float horizontalBoundsPadding = 0.05f;

    private Rigidbody2D rb;
    private SpriteSheetAnimator animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D playerCollider;
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
    private bool spriteFacesLeftByDefault = true;
    private int configuredGender = -1;
    private GameState subscribedGameState;
    private bool isSubscribedToLocationChanges;

    private const string AnimIdle = "Idle";
    private const string AnimWalk = "Walk";
    private const string AnimJump = "Jump";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<SpriteSheetAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<BoxCollider2D>();
        mainCamera = Camera.main;

        EnsureGroundCheckExists();
        ApplyColliderSettings();
        RepairGroundCheckIfOutOfRange();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureGroundCheckExists();
            ApplyColliderSettings();
            RepairGroundCheckIfOutOfRange();
            ApplyEditorPreviewSprite();
        }
    }

    private void Start()
    {
        TryBindGameState();
        ConfigureAnimations();
        SetFacingDirection(true);
        TrySubscribeToLocationChanges();
        SnapToCurrentLocation();
    }

    private void OnDestroy()
    {
        if (subscribedGameState != null)
        {
            subscribedGameState.OnStateChanged -= HandleGameStateChanged;
            subscribedGameState = null;
        }
    }

    private void ConfigureAnimations()
    {
        int gender = ResolveConfiguredGender();
        if (configuredGender == gender && !string.IsNullOrEmpty(animator.CurrentClipName))
        {
            return;
        }

        configuredGender = gender;
        bool useFemaleResources = gender == FemaleGenderValue;
        spriteFacesLeftByDefault = useFemaleResources;
        bool reverseWalkFrames = useFemaleResources;
        string clipToPlay = string.IsNullOrEmpty(animator.CurrentClipName) ? AnimIdle : animator.CurrentClipName;

        string idleResourcePath = useFemaleResources ? FemaleIdleResourcePath : MaleIdleResourcePath;
        string walkResourcePath = useFemaleResources ? FemaleWalkResourcePath : MaleWalkResourcePath;
        string jumpResourcePath = useFemaleResources ? FemaleJumpResourcePath : MaleJumpResourcePath;

        animator.LoadFromResources(idleResourcePath, AnimIdle, idleFrameRate, true);
        animator.LoadFromResources(walkResourcePath, AnimWalk, walkFrameRate, true, reverseWalkFrames);
        animator.LoadFromResources(jumpResourcePath, AnimJump, jumpFrameRate, false);
        animator.Play(clipToPlay, true);

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            spriteRenderer.color = Color.white;
            ApplyVisualScale(spriteRenderer.sprite);
        }

        ApplyColliderSettings();
    }

    private void HandleGameStateChanged()
    {
        ConfigureAnimations();
        SetFacingDirection(facingRight);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            RefreshEditorPreview();
            return;
        }

        TryBindGameState();

        if (!isSubscribedToLocationChanges)
        {
            TrySubscribeToLocationChanges();
        }

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
                clickTargetX = ClampTargetXToLocationBounds(mouseWorldPos.x);
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

    private void TryBindGameState()
    {
        if (subscribedGameState == GameState.Instance)
        {
            return;
        }

        if (subscribedGameState != null)
        {
            subscribedGameState.OnStateChanged -= HandleGameStateChanged;
        }

        subscribedGameState = GameState.Instance;
        if (subscribedGameState != null)
        {
            subscribedGameState.OnStateChanged += HandleGameStateChanged;
            ConfigureAnimations();
            SetFacingDirection(facingRight);
        }
    }

    private static int ResolveConfiguredGender()
    {
        if (GameState.Instance != null)
        {
            return Mathf.Clamp(GameState.Instance.PlayerGender, MaleGenderValue, FemaleGenderValue);
        }

        if (SaveManager.PendingLoadData != null)
        {
            return Mathf.Clamp(SaveManager.PendingLoadData.playerGender, MaleGenderValue, FemaleGenderValue);
        }

        if (CharacterCreationUI.HasPendingCharacter)
        {
            return Mathf.Clamp(CharacterCreationUI.PendingPlayerGender, MaleGenderValue, FemaleGenderValue);
        }

        return MaleGenderValue;
    }

    private void UpdateAnimation(float input)
    {
        float absInput = Mathf.Abs(input);

        if (!isGrounded)
        {
            PlayAnimationIfNeeded(AnimJump);
        }
        else if (absInput < 0.1f)
        {
            PlayAnimationIfNeeded(AnimIdle);
        }
        else
        {
            PlayAnimationIfNeeded(AnimWalk);
        }
    }

    private void PlayAnimationIfNeeded(string clipName)
    {
        if (animator == null)
        {
            return;
        }

        if (animator.CurrentClipName != clipName || !animator.IsPlaying)
        {
            animator.Play(clipName, true);
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

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
        ClampToCurrentLocationBounds();

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
            spriteRenderer.flipX = spriteFacesLeftByDefault
                ? shouldFaceRight
                : !shouldFaceRight;
        }
    }

    private void Reset()
    {
        EnsureGroundCheckExists();
        ApplyColliderSettings();
        SnapGroundCheckToColliderBottom(0.02f);
    }

    private void OnValidate()
    {
        groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
        colliderSize.x = Mathf.Max(0.01f, colliderSize.x);
        colliderSize.y = Mathf.Max(0.01f, colliderSize.y);
        maleVisualHeight = Mathf.Max(0.1f, maleVisualHeight);
        femaleVisualHeight = Mathf.Max(0.1f, femaleVisualHeight);

        if (!Application.isPlaying)
        {
            EnsureGroundCheckExists();
            ApplyColliderSettings();
            RepairGroundCheckIfOutOfRange();
            ApplyEditorPreviewSprite();
        }
    }

    private void OnDrawGizmos()
    {
        Transform groundCheckTransform = groundCheck;
        if (groundCheckTransform == null)
        {
            EnsureGroundCheckExists();
            groundCheckTransform = groundCheck;
        }

        if (groundCheckTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckTransform.position, groundCheckRadius);
            Gizmos.DrawSphere(groundCheckTransform.position, Mathf.Max(0.03f, groundCheckRadius * 0.15f));
        }

        BoxCollider2D collider2D = GetPlayerCollider();
        if (collider2D != null)
        {
            Gizmos.color = Color.cyan;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = collider2D.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(collider2D.offset, collider2D.size);
            Gizmos.DrawCube(collider2D.offset, new Vector3(collider2D.size.x, collider2D.size.y, 0.001f));
            Gizmos.matrix = previousMatrix;
        }
    }

    public float MoveSpeed => moveSpeed;
    public bool FacingRight => facingRight;
    public bool AutoScaleToVisualHeight
    {
        get => autoScaleToVisualHeight;
        set => autoScaleToVisualHeight = value;
    }

    public float VisualHeight
    {
        get => ResolveConfiguredGender() == FemaleGenderValue ? femaleVisualHeight : maleVisualHeight;
        set
        {
            float normalized = Mathf.Max(0.1f, value);
            if (ResolveConfiguredGender() == FemaleGenderValue)
            {
                femaleVisualHeight = normalized;
            }
            else
            {
                maleVisualHeight = normalized;
            }
        }
    }

    public SpriteRenderer GetSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        return spriteRenderer;
    }

    public void SetVisualHeight(float targetHeight)
    {
        VisualHeight = targetHeight;
        autoScaleToVisualHeight = true;

        SpriteRenderer renderer = GetSpriteRenderer();
        if (renderer != null && renderer.sprite != null)
        {
            ApplyVisualScale(renderer.sprite);
        }
    }

    public Transform GroundCheckTransform => groundCheck;
    public float GroundCheckRadius
    {
        get => groundCheckRadius;
        set => groundCheckRadius = Mathf.Max(0.01f, value);
    }

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
        clickTargetX = ClampTargetXToLocationBounds(clickTargetX);
    }

    private float ClampTargetXToLocationBounds(float targetX)
    {
        return TryGetCurrentHorizontalBounds(out float minX, out float maxX)
            ? Mathf.Clamp(targetX, minX, maxX)
            : targetX;
    }

    private void ClampToCurrentLocationBounds()
    {
        if (!TryGetCurrentHorizontalBounds(out float minX, out float maxX))
        {
            return;
        }

        Vector2 position = rb.position;
        float clampedX = Mathf.Clamp(position.x, minX, maxX);
        if (!Mathf.Approximately(clampedX, position.x))
        {
            rb.position = new Vector2(clampedX, position.y);

            if ((clampedX <= minX && rb.velocity.x < 0f) ||
                (clampedX >= maxX && rb.velocity.x > 0f))
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }

            if (isClickMoving && Mathf.Abs(clickTargetX - clampedX) <= clickMoveStopDistance)
            {
                isClickMoving = false;
                moveInput = 0f;
            }
        }
    }

    private bool TryGetCurrentHorizontalBounds(out float minX, out float maxX)
    {
        minX = float.NegativeInfinity;
        maxX = float.PositiveInfinity;

        if (!constrainToCurrentLocation || LocationManager.Instance == null || GameState.Instance == null)
        {
            return false;
        }

        LocationDefinition location = LocationManager.Instance.GetCurrentLocationDef();
        if (location == null)
        {
            return false;
        }

        float halfWidth = GetHalfColliderWidth();
        minX = location.worldMinX + halfWidth + horizontalBoundsPadding;
        maxX = location.worldMaxX - halfWidth - horizontalBoundsPadding;
        if (minX > maxX)
        {
            float center = (location.worldMinX + location.worldMaxX) * 0.5f;
            minX = center;
            maxX = center;
        }

        return true;
    }

    private float GetHalfColliderWidth()
    {
        if (playerCollider != null)
        {
            return playerCollider.bounds.extents.x;
        }

        return 0.2f;
    }

    public void SetGroundCheckLocalPosition(Vector3 localPosition)
    {
        EnsureGroundCheckExists();
        groundCheck.localPosition = localPosition;
    }

    public BoxCollider2D GetPlayerCollider()
    {
        if (playerCollider == null)
        {
            playerCollider = GetComponent<BoxCollider2D>();
        }

        return playerCollider;
    }

    public void SetColliderShape(Vector2 offset, Vector2 size)
    {
        colliderOffset = offset;
        colliderSize = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        ApplyColliderSettings();
    }

    public void SnapGroundCheckToColliderBottom(float extraOffset = 0f)
    {
        BoxCollider2D collider2D = GetPlayerCollider();
        if (collider2D == null)
        {
            return;
        }

        EnsureGroundCheckExists();
        Vector3 localPosition = groundCheck.localPosition;
        localPosition.x = collider2D.offset.x;
        localPosition.y = collider2D.offset.y - collider2D.size.y * 0.5f - extraOffset;
        localPosition.z = 0f;
        groundCheck.localPosition = localPosition;
    }

    private void RepairGroundCheckIfOutOfRange()
    {
        BoxCollider2D collider2D = GetPlayerCollider();
        if (collider2D == null)
        {
            return;
        }

        EnsureGroundCheckExists();

        Vector3 localPosition = groundCheck.localPosition;
        float expectedX = collider2D.offset.x;
        float expectedY = collider2D.offset.y - collider2D.size.y * 0.5f - 0.02f;

        float maxAllowedHorizontalDrift = Mathf.Max(0.75f, collider2D.size.x * 2f);
        float maxAllowedVerticalDrift = Mathf.Max(1.5f, collider2D.size.y * 2f);

        bool isOutOfRange =
            Mathf.Abs(localPosition.x - expectedX) > maxAllowedHorizontalDrift ||
            Mathf.Abs(localPosition.y - expectedY) > maxAllowedVerticalDrift ||
            Mathf.Abs(localPosition.z) > 0.01f;

        if (isOutOfRange)
        {
            SnapGroundCheckToColliderBottom(0.02f);
        }
    }

    private void EnsureGroundCheckExists()
    {
        if (groundCheck != null)
        {
            return;
        }

        Transform existing = transform.Find("GroundCheck");
        if (existing != null)
        {
            groundCheck = existing;
            return;
        }

        GameObject check = new GameObject("GroundCheck");
        check.transform.SetParent(transform);
        check.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        check.transform.localRotation = Quaternion.identity;
        check.transform.localScale = Vector3.one;
        groundCheck = check.transform;
    }

    private void ApplyColliderSettings()
    {
        BoxCollider2D collider2D = GetPlayerCollider();
        if (collider2D == null)
        {
            return;
        }

        collider2D.size = colliderSize;
        collider2D.offset = colliderOffset;
    }

    private void ApplyEditorPreviewSprite()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        int gender = ResolveConfiguredGender();
        bool useFemaleResources = gender == FemaleGenderValue;
        spriteFacesLeftByDefault = useFemaleResources;

        string previewResourcePath = useFemaleResources
            ? FemaleIdlePreviewResourcePath
            : MaleIdlePreviewResourcePath;

        Sprite previewSprite = Resources.Load<Sprite>(previewResourcePath);
        if (previewSprite == null)
        {
            return;
        }

        spriteRenderer.sprite = previewSprite;
        spriteRenderer.color = Color.white;
        ApplyVisualScale(previewSprite);

        SetFacingDirection(facingRight);
    }

    private void RefreshEditorPreview()
    {
        EnsureGroundCheckExists();
        ApplyColliderSettings();
        RepairGroundCheckIfOutOfRange();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (!spriteRenderer.enabled || spriteRenderer.sprite == null || configuredGender != ResolveConfiguredGender())
        {
            ApplyEditorPreviewSprite();
        }
        else
        {
            spriteRenderer.color = Color.white;
            ApplyVisualScale(spriteRenderer.sprite);
            SetFacingDirection(facingRight);
        }
    }

    private void ApplyVisualScale(Sprite sprite)
    {
        if (!autoScaleToVisualHeight || sprite == null)
        {
            return;
        }

        float spriteHeight = sprite.bounds.size.y;
        if (spriteHeight > 0f)
        {
            float scale = VisualHeight / spriteHeight;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void TrySubscribeToLocationChanges()
    {
        if (GameState.Instance == null)
        {
            return;
        }

        GameState.Instance.OnLocationChanged -= OnGameLocationChanged;
        GameState.Instance.OnLocationChanged += OnGameLocationChanged;
        isSubscribedToLocationChanges = true;
    }

    private void OnGameLocationChanged(LocationId location)
    {
        TeleportToLocation(location);
    }

    private void SnapToCurrentLocation()
    {
        if (GameState.Instance == null)
        {
            return;
        }

        TeleportToLocation(GameState.Instance.CurrentLocation);
    }

    public void TeleportToLocation(LocationId location)
    {
        if (LocationManager.Instance == null)
        {
            return;
        }

        Vector3 targetPosition = LocationManager.Instance.GetLocationEntryPoint(location);
        if (targetPosition == Vector3.zero && LocationManager.Instance.GetLocation(location) == null)
        {
            return;
        }

        isClickMoving = false;
        pendingArrivalAction = null;
        pendingInteractionTarget = null;
        moveInput = 0f;
        jumpRequested = false;
        jumpHeld = false;
        rightMouseDrivingJump = false;

        if (rb != null)
        {
            rb.position = new Vector2(targetPosition.x, targetPosition.y);
            rb.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        else
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        }

        UpdateAnimation(0f);

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : FindObjectOfType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SnapToWorldPosition(targetPosition);
        }
    }

    private void OnDisable()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnLocationChanged -= OnGameLocationChanged;
        }

        isSubscribedToLocationChanges = false;
    }
}
