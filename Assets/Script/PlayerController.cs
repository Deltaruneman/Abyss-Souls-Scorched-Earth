using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển tối đa")]
    public float moveSpeed = 8f;
    [Tooltip("Tốc độ tăng tốc khi bắt đầu di chuyển (đơn vị/giây^2)")]
    public float acceleration = 60f;
    [Tooltip("Tốc độ giảm tốc khi dừng di chuyển")]
    public float deceleration = 70f;
    [Tooltip("Giảm tốc khi đổi hướng đột ngột (giúp điều khiển nhạy hơn)")]
    public float turnSpeedMultiplier = 1.6f;
    [Tooltip("Hệ số giảm khả năng điều khiển khi đang ở trên không")]
    [Range(0f, 1f)] public float airControlMultiplier = 0.8f;
    [Tooltip("Độ giảm tốc theo phương ngang khi đang ở trên không và KHÔNG có input (giá trị càng nhỏ, nhân vật càng giữ quán tính khi rơi/nhảy)")]
    public float airDrag = 5f;

    [Header("Jump")]
    [Tooltip("Lực nhảy ban đầu")]
    public float jumpForce = 14f;
    [Tooltip("Lực nhảy lần 2 (double jump), thường thấp hơn lần đầu")]
    public float doubleJumpForce = 12f;
    [Tooltip("Số lần nhảy tối đa (2 = double jump)")]
    public int maxJumpCount = 2;
    [Tooltip("Trọng lực nhân thêm khi đang rơi xuống (rơi nhanh hơn cho cảm giác 'chắc tay')")]
    public float fallGravityMultiplier = 2.2f;
    [Tooltip("Trọng lực nhân thêm khi thả nút nhảy sớm (nhảy thấp hơn)")]
    public float lowJumpGravityMultiplier = 2.8f;
    [Tooltip("Tốc độ rơi tối đa")]
    public float maxFallSpeed = 20f;

    [Header("Jump Feel - Coyote Time & Buffer")]
    [Tooltip("Thời gian (giây) vẫn cho phép nhảy sau khi rời khỏi mặt đất")]
    public float coyoteTime = 0.12f;
    [Tooltip("Thời gian (giây) lưu lại input nhảy trước khi chạm đất")]
    public float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;
    [Tooltip("Tag của object được coi là mặt đất")]
    public string groundTag = "Ground";

    // Buffer tái sử dụng để tránh cấp phát bộ nhớ (GC) mỗi frame khi gọi OverlapBox
    private readonly Collider2D[] groundCheckResults = new Collider2D[8];

    [Header("Debug (chỉ đọc)")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private int jumpsRemaining;

    private Rigidbody2D rb;
    private float moveInput;
    private bool facingRight = true;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld;
    private bool jumpPressedThisFrame;
    private bool jumpReleasedThisFrame;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        jumpsRemaining = maxJumpCount;
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal"); 

        if (Input.GetButtonDown("Jump"))
        {
            jumpPressedThisFrame = true;
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetButtonUp("Jump"))
        {
            jumpReleasedThisFrame = true;
        }

        jumpHeld = Input.GetButton("Jump");
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        HandleJumpLogic();
        HandleFlip();

        jumpPressedThisFrame = false;
        jumpReleasedThisFrame = false;
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
        HandleGravity();
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;

        // Lấy tất cả collider (dạng box) đang chạm/giao với vùng groundCheck,
        // lọc theo layer (groundLayer) rồi kiểm tra thêm tag "Ground" để chắc chắn
        // đó đúng là mặt đất, không phải object khác vô tình nằm cùng layer.
        int count = Physics2D.OverlapBoxNonAlloc(groundCheck.position, groundCheckSize, 0f, groundCheckResults, groundLayer);

        isGrounded = false;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = groundCheckResults[i];
            if (col != null && col.CompareTag(groundTag))
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = maxJumpCount;
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate;

        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            // Có input -> tăng tốc về hướng target như bình thường
            bool isTurning = Mathf.Sign(targetSpeed) != Mathf.Sign(rb.linearVelocity.x) && Mathf.Abs(rb.linearVelocity.x) > 0.01f;
            accelRate = isTurning ? acceleration * turnSpeedMultiplier : acceleration;
            if (!isGrounded)
            {
                accelRate *= airControlMultiplier;
            }
        }
        else
        {
            // Không có input:
            // - Trên mặt đất: giảm tốc bình thường để dừng lại (deceleration)
            // - Trên không: dùng airDrag (thấp) thay vì deceleration để GIỮ QUÁN TÍNH
            //   -> nhân vật tiếp tục bay/rơi theo hướng đang di chuyển thay vì rơi thẳng đứng
            accelRate = isGrounded ? deceleration : airDrag;
        }

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    private void HandleJumpLogic()
    {
        bool canCoyoteJump = coyoteTimeCounter > 0f && jumpsRemaining == maxJumpCount;
        bool wantsToJump = jumpPressedThisFrame || jumpBufferCounter > 0f;

        if (wantsToJump && (canCoyoteJump || jumpsRemaining > 0))
        {
            PerformJump(isFirstJump: canCoyoteJump);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    private void PerformJump(bool isFirstJump)
    {
        float force = isFirstJump ? jumpForce : doubleJumpForce;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // reset vận tốc y trước khi nhảy để lực nhảy nhất quán
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        jumpsRemaining--;

       
    }

    private void HandleGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            // Đang rơi -> rơi nhanh hơn
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            // Đang bay lên nhưng đã thả nút nhảy -> nhảy thấp hơn (variable jump height)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    private void HandleFlip()
    {
        if (moveInput > 0.01f && !facingRight)
        {
            Flip();
        }
        else if (moveInput < -0.01f && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}