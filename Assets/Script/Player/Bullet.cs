using System;
using UnityEngine;

/// <summary>
/// Gắn script này vào prefab "Bullet". PlayerController sẽ gọi Init() ngay sau khi Instantiate
/// để thiết lập hướng bay, tốc độ và sát thương cho viên đạn.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Tooltip("Thời gian (giây) trước khi đạn tự huỷ nếu không trúng gì")]
    public float lifeTime = 3f;

    [Tooltip("Layer chứa các object địch mà đạn có thể gây sát thương")]
    public LayerMask enemyLayer;

    // Callback tuỳ chọn: PlayerController gán sau khi Instantiate để biết chính xác
    // thời điểm và enemy nào bị trúng đạn (dùng cho cộng chiến ý, teleport skill, v.v.)
    // Không bị serialize trong Inspector, chỉ gán bằng code lúc runtime.
    [NonSerialized] public Action<Enemy> onHitEnemy;

    private Rigidbody2D rb;
    private int damage;
    private Vector2 direction;

    // Đếm giờ thủ công thay vì dùng Destroy(gameObject, lifeTime), để mỗi lần Init()
    // được gọi lại (ví dụ đạn bị phản bởi melee attack) sẽ có trọn vẹn lifeTime giây
    // mới, không bị lệnh Destroy hẹn giờ từ lần Init() trước đó huỷ sớm.
    private float lifeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // đạn bay thẳng, không rơi theo trọng lực
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Được gọi từ PlayerController ngay sau khi Instantiate, hoặc gọi lại khi đạn
    /// bị phản (reflect) bởi melee attack để tái khởi tạo hướng/tốc độ/sát thương.
    /// </summary>
    public void Init(Vector2 dir, float speed, int bulletDamage)
    {
        direction = dir.normalized;
        damage = bulletDamage;

        rb.linearVelocity = direction * speed;

        // Xoay đạn theo hướng bay (nếu sprite đạn hướng sang phải mặc định)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Reset trọn vẹn lifeTime giây mỗi lần Init() được gọi
        lifeTimer = lifeTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
        // Nếu object không thuộc enemyLayer thì bỏ qua (đạn vẫn có thể va chạm tường v.v. tuỳ setup collider)
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            onHitEnemy?.Invoke(enemy);
        }

        Destroy(gameObject);
    }
}