using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum WeaponType
{
    Melee,
    Ranged
}

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

    [Header("Dash")]
    [Tooltip("Phím dùng để lướt")]
    public KeyCode dashKey = KeyCode.K;
    [Tooltip("Tốc độ di chuyển ngang khi đang lướt")]
    public float dashSpeed = 20f;
    [Tooltip("Thời gian (giây) của một lần lướt")]
    public float dashDuration = 0.15f;
    [Tooltip("Thời gian chờ (giây) giữa 2 lần lướt")]
    public float dashCooldown = 0.8f;
    [Tooltip("Cho phép lướt khi đang ở trên không")]
    public bool allowAirDash = true;
    [Tooltip("Hệ số giảm tốc độ theo phương dọc khi lướt chéo (1 = cao như ngang, giá trị nhỏ hơn giúp dash chéo lên không bị bay quá cao)")]
    [Range(0f, 1f)] public float dashVerticalMultiplier = 0.5f;

    [Header("Dash Wall Bounce")]
    [Tooltip("Layer chứa các collider được coi là chướng ngại vật/tường (khi đang dash chạm vào sẽ bị bật ngược lại)")]
    public LayerMask dashBounceLayer;
    [Tooltip("Lực bật ngược lại theo hướng ngược với hướng dash ban đầu khi va chạm")]
    public float dashBounceForce = 10f;
    [Tooltip("Số lượt nhảy được cộng thêm khi bị bật ngược sau va chạm lúc dash (không vượt quá maxJumpCount)")]
    public int dashBounceJumpBonus = 1;
    [Tooltip("Thời gian (giây) khoá input di chuyển ngang sau khi bị bounce, để lực bounce không bị input di chuyển triệt tiêu ngay lập tức (áp dụng cả khi đang grounded lẫn trên không)")]
    public float bounceLockDuration = 0.2f;

    private float bounceLockTimer;

    [Header("Dash Damage")]
    [Tooltip("Sát thương gây ra cho mỗi địch mà player va chạm trong lúc đang dash")]
    public int dashDamage = 15;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    // Buffer tái sử dụng để tránh cấp phát bộ nhớ (GC) khi kiểm tra sát thương lúc dash
    private readonly Collider2D[] dashHitResults = new Collider2D[8];
    // Đảm bảo mỗi địch chỉ trúng sát thương dash 1 lần cho mỗi lượt dash (dù overlap nhiều frame liên tiếp)
    private readonly HashSet<Enemy> enemiesHitThisDash = new HashSet<Enemy>();

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;

    // Buffer tái sử dụng để tránh cấp phát bộ nhớ (GC) mỗi frame khi gọi OverlapBox
    private readonly Collider2D[] groundCheckResults = new Collider2D[8];

    [Header("Attack")]
    [Tooltip("Phím dùng để tấn công")]
    public KeyCode attackKey = KeyCode.J;
    [Tooltip("Điểm gốc của hitbox tấn công, nên đặt làm object con phía trước nhân vật")]
    public Transform attackPoint;
    [Tooltip("Kích thước vùng hitbox tấn công")]
    public Vector2 attackHitboxSize = new Vector2(0.8f, 0.6f);
    [Tooltip("Sát thương gây ra mỗi lần trúng đòn")]
    public int attackDamage = 10;
    [Tooltip("Thời gian chờ (giây) giữa 2 lần tấn công")]
    public float attackCooldown = 0.4f;
    [Tooltip("Layer chứa các object địch (để lọc bớt trước khi kiểm tra tag, có thể để 'Everything' nếu không dùng layer riêng)")]
    public LayerMask enemyLayer;

    // Buffer tái sử dụng để tránh cấp phát bộ nhớ (GC) mỗi lần tấn công
    private readonly Collider2D[] attackHitResults = new Collider2D[8];
    private float attackCooldownTimer;

    [Header("Weapon Switch")]
    [Tooltip("Phím dùng để chuyển đổi giữa vũ khí cận chiến và vũ khí tầm xa")]
    public KeyCode switchWeaponKey = KeyCode.U;
    [Tooltip("Loại vũ khí hiện tại (có thể set sẵn trong Inspector)")]
    public WeaponType currentWeapon = WeaponType.Melee;

    [Header("Ranged Weapon")]
    [Tooltip("Prefab đạn (Bullet) sẽ được bắn ra khi tấn công bằng vũ khí tầm xa")]
    public GameObject bulletPrefab;
    [Tooltip("Tốc độ bay của đạn")]
    public float bulletSpeed = 15f;
    [Tooltip("Sát thương của đạn")]
    public int bulletDamage = 10;
    [Tooltip("Điểm xuất phát của đạn, mặc định dùng chung attackPoint. Để trống nếu muốn dùng attackPoint.")]
    public Transform firePoint;

    [Header("Health")]
    [Tooltip("Máu tối đa của Player")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [Tooltip("Thời gian (giây) bất tử ngay sau khi trúng đòn, tránh bị trừ máu nhiều lần liên tiếp từ cùng 1 lần va chạm")]
    public float invulnerabilityTime = 0.5f;

    [Header("Events (tuỳ chọn, kéo thả trong Inspector)")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    private float invulnerabilityTimer;
    private bool isDead;

    [Header("Debug (chỉ đọc)")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private int jumpsRemaining;

    private Rigidbody2D rb;
    private Collider2D col;
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

    col = GetComponent<Collider2D>();
    col.sharedMaterial = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
}

    private void Start()
    {
        jumpsRemaining = maxJumpCount;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

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

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(switchWeaponKey))
        {
            SwitchWeapon();
        }

        if (Input.GetKeyDown(attackKey) && attackCooldownTimer <= 0f)
        {
            PerformAttack();
            attackCooldownTimer = attackCooldown;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }

        if (bounceLockTimer > 0f)
        {
            bounceLockTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(dashKey) && dashCooldownTimer <= 0f && !isDashing && (allowAirDash || isGrounded))
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }

        HandleJumpLogic();
        HandleFlip();

        jumpPressedThisFrame = false;
        jumpReleasedThisFrame = false;
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        if (isDashing)
        {
            HandleDashMovement();
            CheckDashDamage();
        }
        else
        {
            // Trong lúc bounceLockTimer > 0 (vừa bị bounce), bỏ qua input di chuyển
            // để lực bounce không bị acceleration/deceleration triệt tiêu ngay lập tức.
            // Áp dụng như nhau dù đang grounded hay trên không.
            if (bounceLockTimer <= 0f)
            {
                HandleMovement();
            }
            HandleGravity();
        }
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        int count = Physics2D.OverlapBoxNonAlloc(groundCheck.position, groundCheckSize, 0f, groundCheckResults, groundLayer);

        isGrounded = count > 0;

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

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        enemiesHitThisDash.Clear();

        float h = moveInput;
        float v = Input.GetAxisRaw("Vertical");

        Vector2 dir = new Vector2(h, v);

        if (dir.sqrMagnitude < 0.01f)
        {
            // Không có input hướng nào -> lướt theo hướng đang nhìn (ngang)
            dir = new Vector2(facingRight ? 1f : -1f, 0f);
        }
        else if (Mathf.Abs(h) < 0.01f)
        {
            // Chỉ có input dọc (lên/xuống) -> KHÔNG cho phép lướt thẳng đứng,
            // ép trục ngang về theo hướng đang nhìn để tạo hướng chéo hợp lệ
            dir.x = facingRight ? 1f : -1f;
        }

        // Kết quả: 6 hướng lướt hợp lệ (trái, phải, và 4 hướng chéo),
        // không bao giờ lướt thẳng lên hoặc thẳng xuống.
        dashDirection = dir.normalized;
    }

    private void HandleDashMovement()
    {
        // Khi đang lướt: giữ vận tốc cố định theo hướng lướt (có thể chéo) và bỏ qua
        // trọng lực để tạo cảm giác lướt thẳng, dứt khoát (không bị rơi giữa chừng).
        // Trục dọc được nhân thêm dashVerticalMultiplier để dash chéo lên không bị bay quá cao.
        rb.linearVelocity = new Vector2(
            dashDirection.x * dashSpeed,
            dashDirection.y * dashSpeed * dashVerticalMultiplier
        );
    }

    private void CheckDashDamage()
    {
        if (col == null) return;

        // Dùng chính bounds của collider player để quét địch đang chạm vào trong lúc dash
        Vector2 center = col.bounds.center;
        Vector2 size = col.bounds.size;

        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, dashHitResults, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = dashHitResults[i];
            if (hit == null) continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            // enemiesHitThisDash.Add trả về false nếu địch đã bị trúng ở lượt dash này rồi
            // -> tránh gây sát thương liên tục nhiều frame khi vẫn còn chạm vào nhau
            if (enemy != null && enemiesHitThisDash.Add(enemy))
            {
                enemy.TakeDamage(dashDamage);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;

        // Chỉ bật ngược nếu collider va chạm thuộc layer được chỉ định (ví dụ tường/chướng ngại vật)
        if (((1 << collision.gameObject.layer) & dashBounceLayer) == 0) return;

        Vector2 contactNormal = collision.GetContact(0).normal;
        BounceFromDash(contactNormal);
    }

    private void BounceFromDash(Vector2 contactNormal)
    {
        isDashing = false;
        dashTimer = 0f;

        bool isDiagonalDash = Mathf.Abs(dashDirection.x) > 0.01f && Mathf.Abs(dashDirection.y) > 0.01f;

        Vector2 bounceDir;
        if (isDiagonalDash)
        {
            // Dash chéo (trên-phải/dưới-phải/trên-trái/dưới-trái) -> bật vuông góc 90 độ
            // thay vì bật ngược thẳng 180 độ. Chọn 1 trong 2 hướng xoay 90 độ dựa trên
            // hướng nào "hợp" với pháp tuyến mặt va chạm hơn (tự nhiên hơn khi bật ra).
            Vector2 rotatedCW = new Vector2(dashDirection.y, -dashDirection.x);  // xoay -90°
            Vector2 rotatedCCW = new Vector2(-dashDirection.y, dashDirection.x); // xoay +90°

            bounceDir = Vector2.Dot(rotatedCW, contactNormal) >= Vector2.Dot(rotatedCCW, contactNormal)
                ? rotatedCW
                : rotatedCCW;
        }
        else
        {
            // Dash ngang thuần (trái/phải) -> vẫn bật ngược 180 độ như cũ
            bounceDir = -dashDirection;
        }

        rb.linearVelocity = bounceDir.normalized * dashBounceForce;

        // Khoá input di chuyển trong bounceLockDuration giây để lực bounce
        // không bị HandleMovement() (acceleration/deceleration) triệt tiêu ngay lập tức
        bounceLockTimer = bounceLockDuration;

        // Cộng thêm lượt nhảy, không vượt quá giới hạn tối đa
        jumpsRemaining = Mathf.Min(jumpsRemaining + dashBounceJumpBonus, maxJumpCount);
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

    /// <summary>
    /// Đổi giữa vũ khí cận chiến (Melee) và vũ khí tầm xa (Ranged).
    /// Có thể gọi hàm này từ UI (nút bấm) nếu cần, không chỉ từ phím tắt.
    /// </summary>
    public void SwitchWeapon()
    {
        currentWeapon = currentWeapon == WeaponType.Melee ? WeaponType.Ranged : WeaponType.Melee;
    }

    private void PerformAttack()
    {
        if (attackPoint == null) return;

        if (currentWeapon == WeaponType.Ranged)
        {
            PerformRangedAttack();
            return;
        }

        // Quét vùng hitbox tấn công, lọc theo layer (enemyLayer). Object nằm
        // đúng layer là được coi là địch, không cần kiểm tra thêm tag.
        int count = Physics2D.OverlapBoxNonAlloc(attackPoint.position, attackHitboxSize, 0f, attackHitResults, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = attackHitResults[i];
            if (col == null) continue;

            // Dùng GetComponentInParent phòng trường hợp collider nằm trên object con của địch
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    /// <summary>
    /// Bắn ra một viên đạn (prefab Bullet) theo hướng player đang nhìn.
    /// Yêu cầu bulletPrefab đã được gán trong Inspector và có script Bullet (hoặc Rigidbody2D) đính kèm.
    /// </summary>
    private void PerformRangedAttack()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerController: chưa gán bulletPrefab, không thể bắn.");
            return;
        }

        Transform spawnPoint = firePoint != null ? firePoint : attackPoint;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);

        // Ưu tiên dùng script Bullet nếu có (cách khuyến khích, dễ tuỳ chỉnh sát thương/tốc độ/hướng)
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Init(direction, bulletSpeed, bulletDamage);
            return;
        }

        // Fallback: nếu prefab chỉ có Rigidbody2D mà không có script Bullet,
        // vẫn cho đạn bay được bằng cách set thẳng vận tốc.
        Rigidbody2D bulletRb = bulletObj.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * bulletSpeed;
        }
    }

    /// <summary>
    /// Gọi hàm này từ script của địch (ví dụ Enemy) để gây sát thương lên Player.
    /// Trong lúc đang lướt (dash) hoặc đang bất tử tạm thời, Player sẽ không bị trừ máu.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDead || isDashing || invulnerabilityTimer > 0f) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityTime;

        onDamaged?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        onDeath?.Invoke();

        // Tuỳ chỉnh hành vi khi Player chết tại đây (phát animation, disable input,
        // load lại scene, hiện màn hình Game Over, v.v.)
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
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(attackPoint.position, attackHitboxSize);
        }
    }
}