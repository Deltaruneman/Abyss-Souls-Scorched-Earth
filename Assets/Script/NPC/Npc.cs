using UnityEngine;

/// <summary>
/// Hành vi cơ bản của NPC: đi tuần (patrol) qua các điểm cho sẵn khi rảnh,
/// tự dừng lại và quay mặt về phía Player khi đang trong 1 cuộc hội thoại
/// (được NPCDialogue gọi qua SetTalking()).
/// Không bắt buộc phải có Rigidbody2D: nếu không có, NPC vẫn patrol được
/// bằng cách di chuyển trực tiếp transform.position (dùng cho NPC không cần vật lý).
/// </summary>
public class NPC : MonoBehaviour
{
    [Header("Patrol (tuỳ chọn)")]
    [Tooltip("Các điểm NPC sẽ đi qua lần lượt, quay vòng. Để trống nếu muốn NPC đứng yên 1 chỗ.")]
    public Transform[] patrolPoints;
    [Tooltip("Tốc độ di chuyển khi patrol")]
    public float moveSpeed = 1.5f;
    [Tooltip("Thời gian (giây) NPC đứng chờ tại mỗi điểm trước khi đi tiếp điểm kế")]
    public float waitTimeAtPoint = 1.5f;
    [Tooltip("Khoảng cách được coi là 'đã tới' điểm patrol")]
    public float arriveThreshold = 0.1f;

    [Header("Facing")]
    [Tooltip("Tự động lật sprite (flip X) theo hướng di chuyển / hướng nhìn về Player")]
    public bool flipSpriteTowardsMovement = true;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private int currentPatrolIndex;
    private float waitTimer;
    private bool isTalking;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Đang hội thoại -> không patrol, đứng yên tại chỗ
        if (isTalking) return;

        Patrol();
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            StopMoving();
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            StopMoving();
            return;
        }

        Transform target = patrolPoints[currentPatrolIndex];
        if (target == null) return;

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        if (distance <= arriveThreshold)
        {
            // Tới điểm patrol -> đứng chờ rồi chuyển sang điểm kế tiếp (quay vòng)
            waitTimer = waitTimeAtPoint;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            StopMoving();
            return;
        }

        Vector2 direction = toTarget.normalized;

        if (rb != null)
        {
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            transform.position += (Vector3)(direction * moveSpeed * Time.fixedDeltaTime);
        }

        UpdateFacing(direction.x);
    }

    private void StopMoving()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void UpdateFacing(float xDirection)
    {
        if (!flipSpriteTowardsMovement || spriteRenderer == null) return;
        if (Mathf.Abs(xDirection) > 0.01f)
        {
            spriteRenderer.flipX = xDirection < 0f;
        }
    }

    /// <summary>
    /// Gọi từ NPCDialogue khi bắt đầu hội thoại: dừng patrol và quay mặt về phía Player.
    /// playerTransform để trống nếu chỉ cần dừng patrol mà không cần quay mặt.
    /// </summary>
    public void SetTalking(bool talking, Transform playerTransform = null)
    {
        isTalking = talking;
        StopMoving();

        if (talking && playerTransform != null)
        {
            float xDir = playerTransform.position.x - transform.position.x;
            UpdateFacing(xDir);
        }
    }
}