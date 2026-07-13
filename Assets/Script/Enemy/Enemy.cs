using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Máu tối đa của địch")]
    public int maxHealth = 30;
    [SerializeField] private int currentHealth;

    [Header("Hit Feedback")]
    [Tooltip("Thời gian (giây) bất tử ngay sau khi trúng đòn, tránh bị trừ máu nhiều lần trong 1 lần tấn công")]
    public float invulnerabilityTime = 0.1f;
    [Tooltip("Màu chớp khi trúng đòn (để trống nếu không có SpriteRenderer)")]
    public Color hitFlashColor = Color.white;
    [Tooltip("Thời gian chớp màu khi trúng đòn")]
    public float hitFlashDuration = 0.08f;

    [Header("Events (tuỳ chọn, kéo thả trong Inspector)")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    [Header("Contact Damage")]
    [Tooltip("Sát thương gây cho Player khi va chạm với địch")]
    public int contactDamage = 10;
    [Tooltip("Layer chứa object Player (để lọc bớt trước khi lấy component)")]
    public LayerMask playerLayer;

    private float invulnerabilityTimer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Gọi hàm này từ script tấn công (ví dụ PlayerController) để gây sát thương lên địch.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDead || invulnerabilityTimer > 0f) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityTime;

        onDamaged?.Invoke();

        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(HitFlash());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Dùng cả 2 cặp callback (Collision/Trigger) vì tuỳ setup Collider2D của Enemy
    // là dạng va chạm vật lý hay dạng Trigger mà chỉ 1 trong 2 cặp sẽ thực sự được gọi.
    // "Stay" giúp Player vẫn bị trừ máu nếu đứng ì trong địch, việc chống bị trừ
    // máu liên tục đã được xử lý bằng invulnerabilityTimer bên trong PlayerController.TakeDamage.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void TryDamagePlayer(GameObject obj)
    {
        if (isDead) return;

        // Lọc theo layer trước cho nhanh, tránh gọi GetComponentInParent không cần thiết
        if (((1 << obj.layer) & playerLayer) == 0) return;

        // Dùng GetComponentInParent phòng trường hợp collider nằm trên object con của Player
        PlayerController player = obj.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
        }
    }

    private System.Collections.IEnumerator HitFlash()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        onDeath?.Invoke();

        // Tuỳ chỉnh hành vi khi chết tại đây (phát animation, hiệu ứng, drop item, v.v.)
        // Mặc định: hủy object sau một khoảng ngắn để kịp hiển thị hiệu ứng nếu có.
        Destroy(gameObject, 0.05f);
    }
}