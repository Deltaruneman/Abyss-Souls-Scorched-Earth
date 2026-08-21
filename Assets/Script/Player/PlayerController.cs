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

    // ===================== [MOI] FLUID MOVEMENT (CURVE-BASED) =====================
    // Chuyen doi tuy chon: thay vi noi suy tuyen tinh (linear) theo accel/decel (m/s^2),
    // he thong nay dung AnimationCurve de noi suy van toc theo thoi gian -> cam giac
    // tang/giam toc "co hon", giong Ori (nhanh o giua, muot o dau/cuoi).
    // Bat/tat bang useCurveAcceleration; neu tat, code cu (linear) hoat dong y het truoc gio.
    [Header("[Moi] Fluid Movement (Curve)")]
    public bool useCurveAcceleration = false;
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float accelerationCurveDuration = 0.22f; // thoi gian (s) de dat toc do toi da khi co input
    public AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float decelerationCurveDuration = 0.18f; // thoi gian (s) de dung han khi buong input

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

    // ===== [MOI] Dash tinh chinh: ease-out cuoi dash + bao toan mot phan quan tinh =====
    // dashSpeedCurve: he so nhan vao dashSpeed theo % thoi gian da dash (0 -> 1).
    // Mac dinh: giu ~100% toc do trong phan lon cu dash, roi giam nhe (ease-out) o doan cuoi
    // de nhan vat khong bi "khung" dot ngot khi dash ket thuc.
    public AnimationCurve dashSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.65f, 1f),
        new Keyframe(1f, 0.55f)
    );
    [Range(0f, 1f)] public float dashMomentumPreserved = 0.45f; // % van toc dash giu lai ngay sau khi dash xong

    [Space(8)]
    public LayerMask dashBounceLayer;
    public float dashBounceForce = 10f;
    public int dashBounceJumpBonus = 1;
    public float bounceLockDuration = 0.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;

    // ===================== [MOI] WALL INTERACTION =====================
    // He thong Wall Slide / Wall Jump kieu Ori: cham tuong tren khong -> co the truot cham
    // (giu phim huong vao tuong), hoac nhay bat ra. Phan biet 2 kieu wall jump:
    //  - Leap  : buong huong (hoac giu huong ra xa tuong) -> bat XA, thien ve ngang.
    //  - Climb : giu huong VAO tuong khi nhay -> bat LEN CAO, chi tach nhe khoi tuong.
    [Header("[Moi] Wall Interaction")]
    public Transform wallCheckFront; // rong thi tu dung transform cua player
    public float wallCheckDistance = 0.55f;
    public LayerMask wallLayer;
    public bool requireHoldTowardWallToSlide = true; // true: phai giu phim huong vao tuong moi truot

    [Space(8)]
    public float wallSlideSpeed = 3f;          // toc do roi toi da khi dang truot tuong
    public float wallSlideAcceleration = 25f;  // toc do noi suy den wallSlideSpeed

    [Space(8)]
    public float wallJumpLeapForceX = 15f;   // Leap: luc ngang, bat xa khoi tuong
    public float wallJumpLeapForceY = 11f;   // Leap: luc doc, thap hon climb
    public float wallJumpClimbForceX = 6f;   // Climb: luc ngang, chi du de tach khoi tuong
    public float wallJumpClimbForceY = 16f;  // Climb: luc doc, cao hon leap de "bam" len tren
    public float wallJumpLockDuration = 0.15f; // khoa dieu khien ngang ngan sau wall jump, tranh trieu tieu luc bat

    // ===================== [MOI] GLIDE (KURO'S FEATHER) =====================
    // Giu phim tren khong de giam trong luc xuong muc rat thap, cho phep luot xa hon
    // thay vi roi tu do. Khong anh huong toi cac trang thai dash / wall-slide.
    [Header("[Moi] Glide (Kuro's Feather)")]
    public KeyCode glideKey = KeyCode.LeftShift;
    [Range(0f, 1f)] public float glideGravityMultiplier = 0.15f; // % trong luc con lai khi luot gio
    public float glideMaxFallSpeed = 2.2f; // toc do roi toi da rieng cho glide (nho hon maxFallSpeed thuong)

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

    [Space(8)]
    [Header("Ranged Ammo")]
    public int maxAmmo = 4;
    public float ammoRegenInterval = 1f; // moi 1 giay tich them 1 vien
    [SerializeField] private int currentAmmo;
    private float ammoRegenTimer;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public UnityEvent<int> onAmmoChanged; // bao cho UI moi khi so dan thay doi

    // ===================== SKILL (phim I) =====================
    [Header("Skill")]
    public KeyCode skillKey = KeyCode.I;

    [Space(8)]
    public int maxWillStack = 10; // van cho phep cong don toi da 10, du skill chi co 2 moc (3 va 5)
    [SerializeField] private int currentWillStack;
    public int willStackTier1 = 3;  // moc mo khoa skill yeu nhat, duoi moc nay khong dung duoc skill
    public int willStackTier2 = 5;  // moc toi da, skill manh nhat (gop hieu ung cu cua moc 10)
    public int CurrentWillStack => currentWillStack;

    [Space(8)]
    public Vector2 meleeSkillTier1HitboxSize = new Vector2(1.1f, 0.8f);
    public int meleeSkillTier1Damage = 14;
    public float meleeSkillTier1KnockbackForce = 26f;
    [Range(0f, 1f)] public float meleeSkillTier1KnockbackUpward = 0.3f;
    public float meleeSkillTier1KnockbackDuration = 0.35f;

    [Space(8)]
    public float meleeSkillDashSpeed = 26f;
    public float meleeSkillDashDuration = 0.18f;
    public Vector2 meleeSkillHitboxSize = new Vector2(1.3f, 0.9f);
    public int meleeSkillTier2Damage = 34; // sat thuong cao nhat (truoc day la cua moc 10)
    public float meleeSkillKnockbackForce = 20f;
    [Range(0f, 1f)] public float meleeSkillKnockbackUpward = 0.35f;
    public float meleeSkillKnockbackDuration = 0.3f;
    public float meleeSkillCooldown = 3f;

    [Space(8)]
    public GameObject skillArrowPrefab;
    public float skillArrowSpeed = 32f;
    public float skillArrowRadius = 0.15f;
    public float skillArrowMaxDistance = 18f;
    public int skillArrowDamageTier1 = 18;
    public LayerMask skillArrowObstacleLayer;
    public float rangedSkillCooldown = 3f;

    [Space(8)]
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

    // ===================== ITEM / INVENTORY =====================
    [Header("Nhat item")]
    public KeyCode pickupKey = KeyCode.F;
    public float pickupRadius = 1f;
    public LayerMask itemLayer;
    public Transform pickupCheckPoint; // rong thi dung transform cua player

    // ===================== HEALTH / EVENTS =====================
    [Header("Health")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;
    public float invulnerabilityTime = 0.5f;
    public int CurrentHealth => currentHealth;

    [Space(8)]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    // ===================== DEBUG =====================
    [Header("Debug (chi doc)")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private int jumpsRemaining;
    [SerializeField] private bool isTouchingWall; // [Moi]
    [SerializeField] private bool isWallSliding;  // [Moi]
    [SerializeField] private bool isGliding;      // [Moi]

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

    // ===== [MOI] Runtime state: Wall Interaction =====
    private int wallSide;          // 1 = tuong o ben phai, -1 = ben trai, 0 = khong cham tuong
    private bool wasTouchingWall;  // de phat hien "vua cham tuong" -> hoi luot nhay
    private float wallJumpLockTimer;

    // ===== [MOI] Runtime state: Glide =====
    private bool glideHeld;

    // ===== [MOI] Runtime state: Curve-based acceleration =====
    private float moveCurveTimer;
    private float moveCurveStartSpeed;
    private float moveCurveTargetSpeed;

    private float attackCooldownTimer;
    private readonly Collider2D[] attackHitResults = new Collider2D[8];
    private readonly Collider2D[] bulletReflectResults = new Collider2D[8];

    private float invulnerabilityTimer;
    private bool isDead;

    private bool skillActive;
    private bool isMeleeSkillDashing;
    private float meleeSkillDashTimer;
    private Vector2 meleeSkillDashDir;
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

        currentAmmo = maxAmmo; // bat dau voi day dan
        onAmmoChanged?.Invoke(currentAmmo);
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

        glideHeld = Input.GetKey(glideKey); // [Moi] Glide: doc input tren Update, xu ly vat ly trong FixedUpdate

        // dem nguoc cac timer chung
        Tick(ref jumpBufferCounter);
        Tick(ref attackCooldownTimer);
        Tick(ref dashCooldownTimer);
        Tick(ref invulnerabilityTimer);
        Tick(ref bounceLockTimer);
        Tick(ref meleeSkillCooldownTimer);
        Tick(ref rangedSkillCooldownTimer);
        Tick(ref skillRecoilLockTimer);
        Tick(ref wallJumpLockTimer); // [Moi]

        HandleAmmoRegen();

        if (Input.GetKeyDown(switchWeaponKey)) SwitchWeapon();

        if (Input.GetKeyDown(attackKey) && attackCooldownTimer <= 0f)
        {
            // chi tinh cooldown neu don danh thuc su duoc thuc hien (vd: Ranged het dan thi khong ton cooldown)
            if (PerformAttack()) attackCooldownTimer = attackCooldown;
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

        if (Input.GetKeyDown(dashKey) && dashCooldownTimer <= 0f && !isDashing && !isMeleeSkillDashing
            && (allowAirDash || isGrounded) && IsUnlocked(SkillType.Dash))
        {
            StartDash();
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) FinishDash(); // [Moi] truoc day: isDashing = false; gio ease-out + giu quan tinh
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
            // [Moi] Wall check + wall slide chi co y nghia khi khong dash/khong o duoi dat
            CheckWall();
            HandleWallSlide();

            // bo qua input di chuyen khi vua bi bounce/recoil/wall-jump de luc day khong bi triet tieu ngay,
            // va khi dang wall slide thi khoa han truc ngang (da xu ly ben trong HandleWallSlide)
            bool inputLocked = bounceLockTimer > 0f || skillRecoilLockTimer > 0f || wallJumpLockTimer > 0f;
            if (!inputLocked && !isWallSliding) HandleMovement();

            // [Moi] Neu dang wall-slide thi truc Y da duoc HandleWallSlide dieu khien, khong ap dung gravity/glide nua
            if (!isWallSliding) HandleGlide();
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
        // [Moi] Cho phep chuyen sang gia toc theo AnimationCurve (cam giac fluid kieu Ori).
        // Neu tat useCurveAcceleration, toan bo logic linear ben duoi giu nguyen y het ban goc.
        if (useCurveAcceleration)
        {
            HandleMovementCurve();
            return;
        }

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

    // [Moi] Gia toc/giam toc theo AnimationCurve: noi suy van toc truc X tu diem bat dau (moveCurveStartSpeed)
    // den van toc muc tieu (moveCurveTargetSpeed) theo % thoi gian da trai qua, thay vi cong don tuyen tinh.
    // Timer/gia tri start duoc reset moi khi muc tieu doi (bat/tat/doi huong input), giup dash/wall-jump
    // ban giao van toc mot cach muot ma qua ResetMoveCurve().
    private void HandleMovementCurve()
    {
        float targetSpeed = moveInput * moveSpeed;
        bool hasInput = Mathf.Abs(targetSpeed) > 0.01f;

        if (Mathf.Abs(targetSpeed - moveCurveTargetSpeed) > 0.01f)
        {
            moveCurveStartSpeed = rb.linearVelocity.x;
            moveCurveTargetSpeed = targetSpeed;
            moveCurveTimer = 0f;
        }

        float duration = hasInput ? accelerationCurveDuration : decelerationCurveDuration;
        if (!isGrounded && hasInput) duration /= Mathf.Max(0.01f, airControlMultiplier); // tren khong: cham hon

        AnimationCurve curve = hasInput ? accelerationCurve : decelerationCurve;

        moveCurveTimer += Time.fixedDeltaTime;
        float t = duration > 0f ? Mathf.Clamp01(moveCurveTimer / duration) : 1f;
        float curveT = curve.Evaluate(t);

        float newX = Mathf.LerpUnclamped(moveCurveStartSpeed, moveCurveTargetSpeed, curveT);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    // [Moi] Ep duong cong gia toc "bat dau lai" tu van toc hien tai - goi sau dash/wall-jump
    // de qua trinh gia toc tiep theo khong bi giat do du lieu curve cu con luu lai.
    private void ResetMoveCurve()
    {
        moveCurveStartSpeed = rb.linearVelocity.x;
        moveCurveTargetSpeed = moveInput * moveSpeed;
        moveCurveTimer = 0f;
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
        bool wantsToJump = jumpPressedThisFrame || jumpBufferCounter > 0f;

        // [Moi] Wall Jump uu tien hon nhay thuong/coyote khi dang cham tuong tren khong
        if (wantsToJump && isTouchingWall && !isGrounded)
        {
            PerformWallJump();
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            return;
        }

        bool canCoyoteJump = coyoteTimeCounter > 0f && jumpsRemaining == maxJumpCount;

        if (wantsToJump && (canCoyoteJump || jumpsRemaining > 0))
        {
            PerformJump(isFirstJump: canCoyoteJump);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    // [Moi] Wall Jump: phan biet Leap (bat xa) va Climb (bat len cao) dua theo huong input luc nhay.
    //  - Giu huong VAO tuong (hoac khong giu phim nao) -> hieu la muon "bam" theo tuong -> Climb jump.
    //  - Giu huong RA XA tuong -> nguoi choi chu dong muon thoat xa -> Leap.
    private void PerformWallJump()
    {
        bool holdingAwayFromWall = (wallSide == 1 && moveInput < -0.1f) || (wallSide == -1 && moveInput > 0.1f);
        float pushDir = -wallSide; // luon day nguoi choi ra xa tuong

        if (holdingAwayFromWall)
        {
            // Leap: nhay bat xa khoi tuong, thien ve luc ngang
            rb.linearVelocity = new Vector2(pushDir * wallJumpLeapForceX, 0f);
            rb.AddForce(Vector2.up * wallJumpLeapForceY, ForceMode2D.Impulse);
        }
        else
        {
            // Climb jump: nhay bam len cao, chi tach nhe khoi tuong, thien ve luc doc
            rb.linearVelocity = new Vector2(pushDir * wallJumpClimbForceX, 0f);
            rb.AddForce(Vector2.up * wallJumpClimbForceY, ForceMode2D.Impulse);
        }

        isWallSliding = false;
        wallJumpLockTimer = wallJumpLockDuration;
        jumpsRemaining = Mathf.Max(jumpsRemaining, maxJumpCount - 1); // hoi lai it nhat 1 luot nhay tren khong, giong Ori

        ResetMoveCurve(); // [Moi] de che do curve-based ban giao muot tu van toc wall-jump vua ap
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

    // ===================== [MOI] WALL INTERACTION =====================
    // Ban 2 tia ngang (trai/phai) tu wallCheckFront de biet tuong dang o ben nao,
    // khong phu thuoc vao huong facing hien tai (tranh sai lech ngay sau khi Flip()).
    private void CheckWall()
    {
        if (isGrounded)
        {
            isTouchingWall = false;
            wallSide = 0;
            wasTouchingWall = false;
            return;
        }

        Vector2 origin = wallCheckFront != null ? (Vector2)wallCheckFront.position : (Vector2)transform.position;
        bool hitRight = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
        bool hitLeft = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer);

        if (hitRight && !hitLeft) { isTouchingWall = true; wallSide = 1; }
        else if (hitLeft && !hitRight) { isTouchingWall = true; wallSide = -1; }
        else { isTouchingWall = false; wallSide = 0; }

        // vua cham tuong (truoc do dang roi tu do) -> hoi lai luot nhay tren khong, giong co che coyote
        if (isTouchingWall && !wasTouchingWall) jumpsRemaining = maxJumpCount;
        wasTouchingWall = isTouchingWall;
    }

    // Truot cham tren tuong: khoa han truc X (dinh vao tuong) va cho truc Y tien dan ve -wallSlideSpeed
    // thay vi roi tu do binh thuong, tao cam giac "bam tuong" truoc khi wall-jump.
    private void HandleWallSlide()
    {
        bool holdingIntoWall = (wallSide == 1 && moveInput > 0.1f) || (wallSide == -1 && moveInput < -0.1f);

        isWallSliding = isTouchingWall
            && !isGrounded
            && rb.linearVelocity.y <= 0.01f
            && wallJumpLockTimer <= 0f
            && (!requireHoldTowardWallToSlide || holdingIntoWall);

        if (!isWallSliding) return;

        float newY = Mathf.MoveTowards(rb.linearVelocity.y, -wallSlideSpeed, wallSlideAcceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(0f, newY);
    }

    // ===================== [MOI] GLIDE (KURO'S FEATHER) =====================
    // Neu dang giu glideKey tren khong va dang roi thi giam trong luc xuong con
    // glideGravityMultiplier (vd 15%) va cham toc do roi o glideMaxFallSpeed rat thap.
    // Neu khong du dieu kien luot gio thi rot ve HandleGravity() nhu binh thuong.
    private void HandleGlide()
    {
        isGliding = glideHeld
            && !isGrounded
            && !isDashing
            && !isMeleeSkillDashing
            && rb.linearVelocity.y <= 0f;

        if (!isGliding)
        {
            HandleGravity();
            return;
        }

        // ap dung phan trong luc con lai (giong cach fallGravityMultiplier/lowJumpGravityMultiplier dang lam)
        rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (glideGravityMultiplier - 1f) * Time.fixedDeltaTime;

        if (rb.linearVelocity.y < -glideMaxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -glideMaxFallSpeed);
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
        // [Moi] Thay vi khoa cung van toc dash tuyet doi, nhan them he so tu dashSpeedCurve theo
        // % thoi gian da dash de tao ease-out nhe o cuoi cu dash, bo qua trong luc trong suot dash.
        float progress = dashDuration > 0f ? 1f - Mathf.Clamp01(dashTimer / dashDuration) : 1f;
        float curveMultiplier = dashSpeedCurve.Evaluate(progress);
        float currentDashSpeed = dashSpeed * curveMultiplier;

        rb.linearVelocity = new Vector2(
            dashDirection.x * currentDashSpeed,
            dashDirection.y * currentDashSpeed * dashVerticalMultiplier
        );
    }

    // [Moi] Ket thuc dash "mem": thay vi cat cung ve van toc di chuyen thuong ngay lap tuc,
    // giu lai mot phan quan tinh theo huong dash (dashMomentumPreserved) de chuyen doi muot hon,
    // roi de HandleMovement (linear hoac curve) tiep quan tu frame FixedUpdate ke tiep.
    private void FinishDash()
    {
        isDashing = false;
        dashTimer = 0f;

        Vector2 preservedVelocity = dashDirection * dashSpeed * dashMomentumPreserved;
        // truc Y: khong ep tang toc do roi/bay len ngoai y muon, chi lay gia tri nho hon giua 2 ben
        float finalY = dashDirection.y > 0f
            ? Mathf.Min(rb.linearVelocity.y, preservedVelocity.y)
            : rb.linearVelocity.y;

        rb.linearVelocity = new Vector2(preservedVelocity.x, finalY);

        ResetMoveCurve(); // [Moi] de che do curve-based bat dau lai muot tu van toc con lai sau dash
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

    // ===================== RANGED AMMO =====================
    // moi ammoRegenInterval giay tich them 1 vien, toi da maxAmmo. Dung lai khi da day.
    private void HandleAmmoRegen()
    {
        if (currentAmmo >= maxAmmo)
        {
            ammoRegenTimer = 0f;
            return;
        }

        ammoRegenTimer += Time.deltaTime;
        if (ammoRegenTimer >= ammoRegenInterval)
        {
            ammoRegenTimer -= ammoRegenInterval;
            currentAmmo = Mathf.Min(currentAmmo + 1, maxAmmo);
            onAmmoChanged?.Invoke(currentAmmo);
        }
    }

    // tru 1 vien dan, tra ve false neu khong con dan de ban
    private bool TryConsumeAmmo()
    {
        if (currentAmmo <= 0) return false;

        currentAmmo--;
        onAmmoChanged?.Invoke(currentAmmo);
        return true;
    }

    // ===================== WEAPON / COMBAT =====================
    public void SwitchWeapon()
    {
        // muon chuyen sang Ranged nhung chua mo khoa thi bo qua, giu nguyen Melee
        if (currentWeapon == WeaponType.Melee && !IsUnlocked(SkillType.RangedWeapon)) return;

        currentWeapon = currentWeapon == WeaponType.Melee ? WeaponType.Ranged : WeaponType.Melee;
    }

    // tra ve true neu don danh thuc su duoc thuc hien (dung de quyet dinh co tinh cooldown khong)
    private bool PerformAttack()
    {
        if (attackPoint == null) return false;

        if (currentWeapon == WeaponType.Ranged)
        {
            return PerformRangedAttack();
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
        return true;
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

    // chi ban duoc khi con dan; tra ve false neu het dan hoac thieu prefab (khong tinh cooldown)
    private bool PerformRangedAttack()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerController: chua gan bulletPrefab.");
            return false;
        }

        if (!TryConsumeAmmo())
        {
            Debug.Log("PlayerController: het dan, cho tich them.");
            return false;
        }

        Transform spawnPoint = firePoint != null ? firePoint : attackPoint;
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.onHitEnemy = _ => NotifyRangedNormalHit();
            bullet.Init(FacingDir, bulletSpeed, bulletDamage);
            return true;
        }

        Rigidbody2D bulletRb = bulletObj.GetComponent<Rigidbody2D>();
        if (bulletRb != null) bulletRb.linearVelocity = FacingDir * bulletSpeed;
        return true;
    }

    // ===================== SKILL (phim I) =====================

    // cong don chien y, toi da maxWillStack (goi khi don danh THUONG trung enemy)
    private void AddWillStack()
    {
        currentWillStack = Mathf.Min(currentWillStack + 1, maxWillStack);
    }

    // tru chien y theo moc da dung (khong reset ve 0 nua): tier2 (>=5) tru 5, tier1 (3-4) tru 3
    private void ConsumeWillStack(int tier)
    {
        currentWillStack = Mathf.Max(0, currentWillStack - tier);
    }

    // tra ve moc chien y hien tai: 0 = chua du dieu kien, hoac willStackTier1/2
    private int GetSkillTier()
    {
        if (currentWillStack >= willStackTier2) return willStackTier2;
        if (currentWillStack >= willStackTier1) return willStackTier1;
        return 0;
    }

    private void TryUseSkill()
    {
        SkillType requiredSkill = currentWeapon == WeaponType.Melee ? SkillType.MeleeSkill : SkillType.RangedSkill;
        if (!IsUnlocked(requiredSkill))
        {
            Debug.Log($"PlayerController: skill {requiredSkill} chua duoc mo khoa trong cot truyen.");
            return;
        }

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

        ConsumeWillStack(tier);
    }

    // tra ve trang thai mo khoa cua 1 skill. Neu scene chua co SkillUnlockManager (vd: dang test rieng le)
    // thi mac dinh coi nhu da mo khoa het de khong can setup them.
    private bool IsUnlocked(SkillType skill)
    {
        return SkillUnlockManager.Instance == null || SkillUnlockManager.Instance.IsUnlocked(skill);
    }

    // -- melee skill --
    // tier1 (3-4 chien y): khong luot, chi 1 don knockback tai cho
    // tier2 (5+ chien y): luot nhanh roi tung 1 don knockback, sat thuong cao nhat
    private void StartMeleeSkill(int tier)
    {
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

        PerformMeleeSkillHitTier2();
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

    // tier 2: dung sau khi luot xong, sat thuong cao nhat (truoc day la cua moc 10), knockback chung
    private void PerformMeleeSkillHitTier2()
    {
        if (attackPoint == null) return;

        int damage = meleeSkillTier2Damage;

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
    // tier2 (5+ chien y): ban 1 beam ton tai 1s, damage lon theo tick (truoc day la cua moc 10)
    private void UseRangedSkill(int tier)
    {
        rangedSkillCooldownTimer = rangedSkillCooldown;

        Transform spawnPoint = firePoint != null ? firePoint : attackPoint;
        if (spawnPoint == null) return;

        Vector2 direction = FacingDir;

        if (tier == willStackTier2)
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

        GameObject skillBulletObj = Instantiate(skillArrowPrefab, spawnPoint.position, Quaternion.identity);
        Bullet bullet = skillBulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.enemyLayer = enemyLayer;
            bullet.Init(direction, skillArrowSpeed, skillArrowDamageTier1);
        }
        else
        {
            Rigidbody2D bulletRb = skillBulletObj.GetComponent<Rigidbody2D>();
            if (bulletRb != null) bulletRb.linearVelocity = direction * skillArrowSpeed;
        }
    }

    // -- beam (ranged tier 2): mot vung sat thuong keo dai theo huong nhin, ton tai skillBeamDuration giay --
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

    // hoi mau, dung cho item loai Use (vd: thuoc hoi mau)
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // ===================== ITEM STAT BONUS (Equip) =====================
    // cong vinh vien mot chi so cua player, goi boi Inventory khi nhat item loai Equip
    public void ApplyStatBonus(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.MaxHealth:
                int healthAdd = Mathf.RoundToInt(value);
                maxHealth += healthAdd;
                currentHealth += healthAdd; // cong luon HP hien tai theo phan tang them
                break;
            case StatType.MoveSpeed:
                moveSpeed += value;
                break;
            case StatType.JumpForce:
                jumpForce += value;
                break;
            case StatType.AttackDamage:
                attackDamage += Mathf.RoundToInt(value);
                break;
            case StatType.BulletDamage:
                bulletDamage += Mathf.RoundToInt(value);
                break;
            case StatType.DashDamage:
                dashDamage += Mathf.RoundToInt(value);
                break;
            case StatType.MaxJumpCount:
                maxJumpCount += Mathf.RoundToInt(value);
                break;
        }
    }

    // ===================== GIZMOS =====================
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        // [Moi] Ve 2 tia wall check (trai/phai) tu wallCheckFront (hoac transform neu de trong)
        Vector3 wallOrigin = wallCheckFront != null ? wallCheckFront.position : transform.position;
        Gizmos.color = isTouchingWall ? Color.green : new Color(1f, 1f, 0f, 0.6f);
        Gizmos.DrawLine(wallOrigin, wallOrigin + Vector3.right * wallCheckDistance);
        Gizmos.DrawLine(wallOrigin, wallOrigin + Vector3.left * wallCheckDistance);

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