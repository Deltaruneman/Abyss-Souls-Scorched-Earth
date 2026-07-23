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
    // ===================== MOVEMENT =====================
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 60f;
    public float deceleration = 70f;
    public float turnSpeedMultiplier = 1.6f;
    [Range(0f, 1f)] public float airControlMultiplier = 0.8f;
    public float airDrag = 5f; // giu quan tinh khi roi neu khong co input

    // ===================== JUMP =====================
    [Header("Jump")]
    public float jumpForce = 14f;
    public float doubleJumpForce = 12f;
    public int maxJumpCount = 2;
    public float fallGravityMultiplier = 2.2f;
    public float lowJumpGravityMultiplier = 2.8f;
    public float maxFallSpeed = 20f;

    [Space(8)]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Dash")]
    public KeyCode dashKey = KeyCode.K;
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public bool allowAirDash = true;
    [Range(0f, 1f)] public float dashVerticalMultiplier = 0.5f;
    public int dashDamage = 15;

    [Space(8)]
    public LayerMask dashBounceLayer;
    public float dashBounceForce = 10f;
    public int dashBounceJumpBonus = 1;
    public float bounceLockDuration = 0.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;

    // ===================== WEAPON / COMBAT =====================
    [Header("Weapon")]
    public KeyCode switchWeaponKey = KeyCode.U;
    public WeaponType currentWeapon = WeaponType.Melee;

    [Space(8)]
    public KeyCode attackKey = KeyCode.J;
    public Transform attackPoint;
    public Vector2 attackHitboxSize = new Vector2(0.8f, 0.6f);
    public int attackDamage = 10;
    public float attackCooldown = 0.4f;
    public LayerMask enemyLayer;
    public float attackKnockbackForce = 6f;
    [Range(0f, 1f)] public float attackKnockbackUpward = 0.25f;
    public float attackKnockbackDuration = 0.15f;

    [Space(8)]
    public LayerMask bulletLayer;
    public string bulletTag = "Bullet";
    public float bulletReflectSpeedMultiplier = 1.2f;
    public int reflectedBulletDamage = 15;

    [Space(8)]
    public GameObject bulletPrefab;
    public float bulletSpeed = 15f;
    public int bulletDamage = 10;
    public Transform firePoint; // rong thi dung attackPoint

    // ===================== SKILL (phim I) =====================
    [Header("Skill - chung")]
    public KeyCode skillKey = KeyCode.I;

    [Space(8)]
    [Header("Chien Y (combo stack tich luy tu don danh thuong)")]
    public int maxWillStack = 10;
    [SerializeField] private int currentWillStack;
    public int willStackTier1 = 3;  // moc mo khoa skill yeu nhat, duoi moc nay khong dung duoc skill
    public int willStackTier2 = 5;  // moc thu 2
    public int willStackTier3 = 10; // moc toi da, skill manh nhat
    public int CurrentWillStack => currentWillStack;

    [Space(8)]
    [Header("Skill - Melee Tier 1 (3-4 chien y: khong luot, chi knockback)")]
    public Vector2 meleeSkillTier1HitboxSize = new Vector2(1.1f, 0.8f);
    public int meleeSkillTier1Damage = 14;
    public float meleeSkillTier1KnockbackForce = 26f;
    [Range(0f, 1f)] public float meleeSkillTier1KnockbackUpward = 0.3f;
    public float meleeSkillTier1KnockbackDuration = 0.35f;

    [Space(8)]
    [Header("Skill - Melee Tier 2 & 3 (5+ chien y: co luot + knockback)")]
    public float meleeSkillDashSpeed = 26f;
    public float meleeSkillDashDuration = 0.18f;
    public Vector2 meleeSkillHitboxSize = new Vector2(1.3f, 0.9f);
    public int meleeSkillTier2Damage = 22; // 5-9 chien y, thap hon tier 3
    public int meleeSkillTier3Damage = 34; // du 10 chien y, sat thuong cao nhat
    public float meleeSkillKnockbackForce = 20f;
    [Range(0f, 1f)] public float meleeSkillKnockbackUpward = 0.35f;
    public float meleeSkillKnockbackDuration = 0.3f;
    public float meleeSkillCooldown = 3f;

    [Space(8)]
    [Header("Skill - Ranged Tier 1 (3-4 chien y: ban 1 phat, damage thap hon)")]
    public GameObject skillArrowPrefab;
    public float skillArrowSpeed = 32f;
    public float skillArrowRadius = 0.15f;
    public float skillArrowMaxDistance = 18f;
    public int skillArrowDamageTier1 = 18;
    public LayerMask skillArrowObstacleLayer;
    public float rangedSkillCooldown = 3f;

    [Space(8)]
    [Header("Skill - Ranged Tier 2 (5-9 chien y: ban + teleport toi enemy neu trung)")]
    public int skillArrowDamageTier2 = 26;
    public float skillTeleportOffset = 0.6f; // dung cach enemy 1 doan sau khi teleport, tranh lot vao trong enemy

    [Space(8)]
    [Header("Skill - Ranged Tier 3 (10 chien y: ban beam ton tai 1s, damage lon)")]
    public GameObject skillBeamVfxPrefab; // optional, hieu ung hinh anh cho beam
    public float skillBeamDuration = 1f;
    public float skillBeamLength = 12f;
    public float skillBeamWidth = 0.6f;
    public float skillBeamTickInterval = 0.1f;
    public int skillBeamDamagePerTick = 12;

    [Space(8)]
    public float skillRecoilForce = 16f;
    [Range(0f, 1f)] public float skillRecoilUpward = 0.25f;
    public float skillRecoilLockDuration = 0.15f;

    // ===================== HEALTH / EVENTS =====================
    [Header("Health")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public float invulnerabilityTime = 0.5f;

    [Space(8)]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    // ===================== DEBUG =====================
    [Header("Debug (chi doc)")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private int jumpsRemaining;

    // ===================== RUNTIME STATE =====================
    private Rigidbody2D rb;
    private Collider2D col;

    private float moveInput;
    private bool facingRight = true;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpHeld;
    private bool jumpPressedThisFrame;
    private bool jumpReleasedThisFrame;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    private float bounceLockTimer;
    private readonly Collider2D[] dashHitResults = new Collider2D[8];
    private readonly HashSet<Enemy> enemiesHitThisDash = new HashSet<Enemy>();

    private readonly Collider2D[] groundCheckResults = new Collider2D[8];

    private float attackCooldownTimer;
    private readonly Collider2D[] attackHitResults = new Collider2D[8];
    private readonly Collider2D[] bulletReflectResults = new Collider2D[8];

    private float invulnerabilityTimer;
    private bool isDead;

    private bool skillActive;
    private bool isMeleeSkillDashing;
    private float meleeSkillDashTimer;
    private Vector2 meleeSkillDashDir;
    private int pendingMeleeSkillTier;
    private float meleeSkillCooldownTimer;
    private float rangedSkillCooldownTimer;
    private float skillRecoilLockTimer;
    private readonly Collider2D[] skillHitResults = new Collider2D[8];

    // -- beam (ranged skill tier 3) --
    private bool isBeamActive;
    private float beamTimer;
    private float beamTickTimer;
    private Vector2 beamOrigin;
    private Vector2 beamDirection;
    private readonly Collider2D[] beamHitResults = new Collider2D[16];

    // ===================== UNITY LIFECYCLE =====================
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
        if (Input.GetButtonUp("Jump")) jumpReleasedThisFrame = true;

        jumpHeld = Input.GetButton("Jump");
        coyoteTimeCounter = isGrounded ? coyoteTime : coyoteTimeCounter - Time.deltaTime;

        // dem nguoc cac timer chung
        Tick(ref jumpBufferCounter);
        Tick(ref attackCooldownTimer);
        Tick(ref dashCooldownTimer);
        Tick(ref invulnerabilityTimer);
        Tick(ref bounceLockTimer);
        Tick(ref meleeSkillCooldownTimer);
        Tick(ref rangedSkillCooldownTimer);
        Tick(ref skillRecoilLockTimer);

        if (Input.GetKeyDown(switchWeaponKey)) SwitchWeapon();

        if (Input.GetKeyDown(attackKey) && attackCooldownTimer <= 0f)
        {
            PerformAttack();
            attackCooldownTimer = attackCooldown;
        }

        if (Input.GetKeyDown(skillKey) && !skillActive && !isDashing) TryUseSkill();

        if (isMeleeSkillDashing)
        {
            meleeSkillDashTimer -= Time.deltaTime;
            if (meleeSkillDashTimer <= 0f) FinishMeleeSkillDash();
        }

        if (isBeamActive)
        {
            beamTimer -= Time.deltaTime;
            beamTickTimer -= Time.deltaTime;

            if (beamTickTimer <= 0f)
            {
                DealBeamDamageTick();
                beamTickTimer = skillBeamTickInterval;
            }

            if (beamTimer <= 0f) FinishSkillBeam();
        }

        if (Input.GetKeyDown(dashKey) && dashCooldownTimer <= 0f && !isDashing && !isMeleeSkillDashing && (allowAirDash || isGrounded))
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
        }

        HandleJumpLogic();
        HandleFlip();

        jumpPressedThisFrame = false;
        jumpReleasedThisFrame = false;
    }

    // giam timer ve 0, khong am
    private static void Tick(ref float timer)
    {
        if (timer > 0f) timer = Mathf.Max(0f, timer - Time.deltaTime);
    }

    private void FixedUpdate()
    {
        CheckGrounded();

        if (isDashing)
        {
            HandleDashMovement();
            CheckDashDamage();
        }
        else if (isMeleeSkillDashing)
        {
            HandleMeleeSkillDashMovement();
        }
        else
        {
            // bo qua input di chuyen khi vua bi bounce/recoil de luc day khong bi triet tieu ngay
            if (bounceLockTimer <= 0f && skillRecoilLockTimer <= 0f) HandleMovement();
            HandleGravity();
        }
    }

    // ===================== GROUND CHECK =====================
    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        int count = Physics2D.OverlapBoxNonAlloc(groundCheck.position, groundCheckSize, 0f, groundCheckResults, groundLayer);
        isGrounded = count > 0;

        if (isGrounded && !wasGrounded) jumpsRemaining = maxJumpCount;
    }

    // ===================== MOVEMENT =====================
    private void HandleMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate;

        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            bool isTurning = Mathf.Sign(targetSpeed) != Mathf.Sign(rb.linearVelocity.x) && Mathf.Abs(rb.linearVelocity.x) > 0.01f;
            accelRate = isTurning ? acceleration * turnSpeedMultiplier : acceleration;
            if (!isGrounded) accelRate *= airControlMultiplier;
        }
        else
        {
            // khong co input: tren dat dung deceleration, tren khong dung airDrag de giu quan tinh
            accelRate = isGrounded ? deceleration : airDrag;
        }

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    private void HandleFlip()
    {
        if (moveInput > 0.01f && !facingRight) Flip();
        else if (moveInput < -0.01f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private Vector2 FacingDir => facingRight ? Vector2.right : Vector2.left;

    // ===================== JUMP =====================
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

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // reset y truoc khi nhay
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        jumpsRemaining--;
    }

    private void HandleGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    // ===================== DASH =====================
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
            dir = FacingDir; // khong co input -> luot theo huong dang nhin
        }
        else if (Mathf.Abs(h) < 0.01f)
        {
            dir.x = facingRight ? 1f : -1f; // khong cho luot thang dung, ep ve huong cheo
        }

        dashDirection = dir.normalized; // 6 huong hop le, khong bao gio thang len/xuong
    }

    private void HandleDashMovement()
    {
        // giu van toc co dinh theo huong dash, bo qua trong luc
        rb.linearVelocity = new Vector2(
            dashDirection.x * dashSpeed,
            dashDirection.y * dashSpeed * dashVerticalMultiplier
        );
    }

    private void CheckDashDamage()
    {
        if (col == null) return;

        Vector2 center = col.bounds.center;
        Vector2 size = col.bounds.size;
        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, dashHitResults, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = dashHitResults[i];
            if (hit == null) continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            // Add tra ve false neu da trung roi -> khong gay sat thuong lap lai nhieu frame
            if (enemy != null && enemiesHitThisDash.Add(enemy)) enemy.TakeDamage(dashDamage);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;
        if (((1 << collision.gameObject.layer) & dashBounceLayer) == 0) return;

        BounceFromDash(collision.GetContact(0).normal);
    }

    private void BounceFromDash(Vector2 contactNormal)
    {
        isDashing = false;
        dashTimer = 0f;

        bool isDiagonalDash = Mathf.Abs(dashDirection.x) > 0.01f && Mathf.Abs(dashDirection.y) > 0.01f;
        Vector2 bounceDir;

        if (isDiagonalDash)
        {
            // dash cheo -> bat vuong goc 90 do, chon huong hop voi phap tuyen va cham hon
            Vector2 rotatedCW = new Vector2(dashDirection.y, -dashDirection.x);
            Vector2 rotatedCCW = new Vector2(-dashDirection.y, dashDirection.x);
            bounceDir = Vector2.Dot(rotatedCW, contactNormal) >= Vector2.Dot(rotatedCCW, contactNormal)
                ? rotatedCW
                : rotatedCCW;
        }
        else
        {
            bounceDir = -dashDirection; // dash ngang -> bat nguoc 180 do
        }

        rb.linearVelocity = bounceDir.normalized * dashBounceForce;
        bounceLockTimer = bounceLockDuration;
        jumpsRemaining = Mathf.Min(jumpsRemaining + dashBounceJumpBonus, maxJumpCount);
    }

    // ===================== WEAPON / COMBAT =====================
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

        int count = Physics2D.OverlapBoxNonAlloc(attackPoint.position, attackHitboxSize, 0f, attackHitResults, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hitCol = attackHitResults[i];
            if (hitCol == null) continue;

            Enemy enemy = hitCol.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
                ApplyMeleeKnockback(enemy);
                AddWillStack();
            }
        }

        ReflectBullets(); // cung luc, phan lai dan trong hitbox
    }

    // goi ham nay tu Bullet.cs khi 1 vien dan thuong (khong phai dan skill, khong phai dan phan lai)
    // do player ban trung enemy, de dong bo cong don chien y cho vu khi Ranged
    public void NotifyRangedNormalHit()
    {
        AddWillStack();
    }

    private void ApplyMeleeKnockback(Enemy enemy)
    {
        if (attackKnockbackForce <= 0f) return;

        Vector2 knockbackDir = FacingDir + Vector2.up * attackKnockbackUpward;
        enemy.ApplyKnockback(knockbackDir.normalized * attackKnockbackForce, attackKnockbackDuration);
    }

    private void ReflectBullets()
    {
        int count = Physics2D.OverlapBoxNonAlloc(attackPoint.position, attackHitboxSize, 0f, bulletReflectResults, bulletLayer);
        for (int i = 0; i < count; i++)
        {
            Collider2D bulletCol = bulletReflectResults[i];
            if (bulletCol == null || !bulletCol.CompareTag(bulletTag)) continue;

            ReflectSingleBullet(bulletCol);
        }
    }

    private void ReflectSingleBullet(Collider2D bulletCol)
    {
        Rigidbody2D bulletRb = bulletCol.GetComponent<Rigidbody2D>();

        // dao nguoc huong bay hien tai; neu khong xac dinh duoc thi coi nhu dan bay nguoc huong player nhin
        Vector2 incomingDir = (bulletRb != null && bulletRb.linearVelocity.sqrMagnitude > 0.01f)
            ? bulletRb.linearVelocity.normalized
            : -FacingDir;

        Vector2 reflectDir = -incomingDir;
        float reflectSpeed = bulletSpeed * bulletReflectSpeedMultiplier;

        Bullet bullet = bulletCol.GetComponentInParent<Bullet>();
        if (bullet != null)
        {
            // gan lai enemyLayer de dan phan huong ve dich thay vi ve player
            bullet.enemyLayer = enemyLayer;
            bullet.Init(reflectDir, reflectSpeed, reflectedBulletDamage);
            return;
        }

        if (bulletRb != null) bulletRb.linearVelocity = reflectDir * reflectSpeed;
    }

    private void PerformRangedAttack()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerController: chua gan bulletPrefab.");
            return;
        }

        Transform spawnPoint = firePoint != null ? firePoint : attackPoint;
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.onHitEnemy = _ => NotifyRangedNormalHit();
            bullet.Init(FacingDir, bulletSpeed, bulletDamage);
            return;
        }

        Rigidbody2D bulletRb = bulletObj.GetComponent<Rigidbody2D>();
        if (bulletRb != null) bulletRb.linearVelocity = FacingDir * bulletSpeed;
    }

    // ===================== SKILL (phim I) =====================

    // cong don chien y, toi da maxWillStack (goi khi don danh THUONG trung enemy)
    private void AddWillStack()
    {
        currentWillStack = Mathf.Min(currentWillStack + 1, maxWillStack);
    }

    // tieu het chien y sau khi dung skill
    private void ConsumeWillStack()
    {
        currentWillStack = 0;
    }

    // tra ve moc chien y hien tai: 0 = chua du dieu kien, hoac willStackTier1/2/3
    private int GetSkillTier()
    {
        if (currentWillStack >= willStackTier3) return willStackTier3;
        if (currentWillStack >= willStackTier2) return willStackTier2;
        if (currentWillStack >= willStackTier1) return willStackTier1;
        return 0;
    }

    private void TryUseSkill()
    {
        int tier = GetSkillTier();
        if (tier == 0)
        {
            Debug.Log($"Chua du chien y de dung skill ({currentWillStack}/{willStackTier1} toi thieu).");
            return;
        }

        if (currentWeapon == WeaponType.Melee)
        {
            if (meleeSkillCooldownTimer > 0f) return;
            StartMeleeSkill(tier);
        }
        else
        {
            if (rangedSkillCooldownTimer > 0f) return;
            UseRangedSkill(tier);
        }

        ConsumeWillStack();
    }

    // -- melee skill --
    // tier1 (3-4 chien y): khong luot, chi 1 don knockback tai cho
    // tier2/tier3 (5+ / 10 chien y): luot nhanh roi tung 1 don knockback, damage khac nhau theo tier
    private void StartMeleeSkill(int tier)
    {
        pendingMeleeSkillTier = tier;
        meleeSkillCooldownTimer = meleeSkillCooldown;

        if (tier == willStackTier1)
        {
            PerformMeleeSkillHitTier1();
            return;
        }

        skillActive = true;
        isMeleeSkillDashing = true;
        meleeSkillDashTimer = meleeSkillDashDuration;
        meleeSkillDashDir = FacingDir;
    }

    private void HandleMeleeSkillDashMovement()
    {
        rb.linearVelocity = meleeSkillDashDir * meleeSkillDashSpeed;
    }

    private void FinishMeleeSkillDash()
    {
        isMeleeSkillDashing = false;
        meleeSkillDashTimer = 0f;

        PerformMeleeSkillHitTier2Or3(pendingMeleeSkillTier);
        skillActive = false;
    }

    // tier 1: khong luot, hitbox va knockback rieng, khong di chuyen player
    private void PerformMeleeSkillHitTier1()
    {
        if (attackPoint == null) return;

        int count = Physics2D.OverlapBoxNonAlloc(attackPoint.position, meleeSkillTier1HitboxSize, 0f, skillHitResults, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hitCol = skillHitResults[i];
            if (hitCol == null) continue;

            Enemy enemy = hitCol.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(meleeSkillTier1Damage);

                Vector2 knockbackDir = FacingDir + Vector2.up * meleeSkillTier1KnockbackUpward;
                enemy.ApplyKnockback(knockbackDir.normalized * meleeSkillTier1KnockbackForce, meleeSkillTier1KnockbackDuration);
            }
        }
    }

    // tier 2 & 3: dung sau khi luot xong, damage phu thuoc tier, knockback chung
    private void PerformMeleeSkillHitTier2Or3(int tier)
    {
        if (attackPoint == null) return;

        int damage = tier == willStackTier3 ? meleeSkillTier3Damage : meleeSkillTier2Damage;

        int count = Physics2D.OverlapBoxNonAlloc(attackPoint.position, meleeSkillHitboxSize, 0f, skillHitResults, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hitCol = skillHitResults[i];
            if (hitCol == null) continue;

            Enemy enemy = hitCol.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                Vector2 knockbackDir = FacingDir + Vector2.up * meleeSkillKnockbackUpward;
                enemy.ApplyKnockback(knockbackDir.normalized * meleeSkillKnockbackForce, meleeSkillKnockbackDuration);
            }
        }
    }

    // -- ranged skill --
    // tier1 (3-4 chien y): ban 1 phat nhu binh thuong nhung damage thap hon
    // tier2 (5-9 chien y): ban giong tier1 nhung teleport player toi enemy neu trung
    // tier3 (10 chien y): ban 1 beam ton tai 1s, damage lon theo tick
    private void UseRangedSkill(int tier)
    {
        rangedSkillCooldownTimer = rangedSkillCooldown;

        Transform spawnPoint = firePoint != null ? firePoint : attackPoint;
        if (spawnPoint == null) return;

        Vector2 direction = FacingDir;

        if (tier == willStackTier3)
        {
            FireSkillBeam(spawnPoint, direction);
            return;
        }

        if (skillArrowPrefab == null)
        {
            Debug.LogWarning("PlayerController: chua gan skillArrowPrefab.");
            return;
        }

        ApplySkillRecoil(direction);

        int damage = tier == willStackTier2 ? skillArrowDamageTier2 : skillArrowDamageTier1;

        GameObject skillBulletObj = Instantiate(skillArrowPrefab, spawnPoint.position, Quaternion.identity);
        Bullet bullet = skillBulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.enemyLayer = enemyLayer;

            // tier 2: khi dan thuc su cham enemy (callback tu Bullet), teleport player toi ngay do
            if (tier == willStackTier2)
            {
                bullet.onHitEnemy = enemy => TeleportToEnemy(enemy);
            }

            bullet.Init(direction, skillArrowSpeed, damage);
        }
        else
        {
            Rigidbody2D bulletRb = skillBulletObj.GetComponent<Rigidbody2D>();
            if (bulletRb != null) bulletRb.linearVelocity = direction * skillArrowSpeed;
        }
    }

    // teleport player toi vi tri enemy vua bi trung boi skill arrow tier 2, lui lai 1 khoang
    // (skillTeleportOffset) theo huong ban de khong bi lot vao trong hitbox enemy
    private void TeleportToEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        Vector2 dirToEnemy = ((Vector2)enemy.transform.position - rb.position);
        Vector2 dir = dirToEnemy.sqrMagnitude > 0.0001f ? dirToEnemy.normalized : FacingDir;

        rb.linearVelocity = Vector2.zero;
        rb.position = (Vector2)enemy.transform.position - dir * skillTeleportOffset;
    }

    // -- beam (ranged tier 3): mot vung sat thuong keo dai theo huong nhin, ton tai skillBeamDuration giay --
    private void FireSkillBeam(Transform spawnPoint, Vector2 direction)
    {
        isBeamActive = true;
        skillActive = true;
        beamTimer = skillBeamDuration;
        beamTickTimer = 0f; // tick ngay frame dau tien
        beamOrigin = spawnPoint.position;
        beamDirection = direction;

        if (skillBeamVfxPrefab != null)
        {
            GameObject vfx = Instantiate(skillBeamVfxPrefab, beamOrigin, Quaternion.identity);
            Destroy(vfx, skillBeamDuration);
        }
    }

    private void DealBeamDamageTick()
    {
        Vector2 center = beamOrigin + beamDirection * (skillBeamLength * 0.5f);
        Vector2 size = new Vector2(skillBeamLength, skillBeamWidth);
        float angle = Vector2.SignedAngle(Vector2.right, beamDirection);

        int count = Physics2D.OverlapBoxNonAlloc(center, size, angle, beamHitResults, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            Collider2D hitCol = beamHitResults[i];
            if (hitCol == null) continue;

            Enemy enemy = hitCol.GetComponentInParent<Enemy>();
            if (enemy != null) enemy.TakeDamage(skillBeamDamagePerTick);
        }
    }

    private void FinishSkillBeam()
    {
        isBeamActive = false;
        beamTimer = 0f;
        skillActive = false;
    }

    // bat player nguoc huong ban, reset van toc truoc de luc giat luon nhat quan
    private void ApplySkillRecoil(Vector2 fireDirection)
    {
        Vector2 recoilDir = -fireDirection + Vector2.up * skillRecoilUpward;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(recoilDir.normalized * skillRecoilForce, ForceMode2D.Impulse);

        skillRecoilLockTimer = skillRecoilLockDuration;
    }

    // ===================== HEALTH =====================
    public void TakeDamage(int amount)
    {
        if (isDead || isDashing || invulnerabilityTimer > 0f) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityTime;

        onDamaged?.Invoke();

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        onDeath?.Invoke();
        // TODO: animation chet, disable input, load lai scene, man hinh game over...
    }

    // ===================== GIZMOS =====================
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

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(attackPoint.position, meleeSkillHitboxSize);

            Gizmos.color = new Color(1f, 0.5f, 0f); // cam: hitbox skill tier 1 (khong luot)
            Gizmos.DrawWireCube(attackPoint.position, meleeSkillTier1HitboxSize);
        }
    }
}