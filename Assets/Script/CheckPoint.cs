using UnityEngine;

/// <summary>
/// Gắn vào 1 GameObject có Collider2D (isTrigger = true) đặt tại vị trí muốn
/// lưu làm checkpoint. Khi Player đi qua, checkpoint này sẽ được báo cho
/// GameManager để lần respawn tiếp theo bắt đầu từ đây.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Layer chứa object Player (để lọc trigger, tránh phải GetComponent trên mọi va chạm)")]
    public LayerMask playerLayer;
    [Tooltip("Chỉ kích hoạt 1 lần duy nhất, các lần Player đi qua lại sau đó sẽ bị bỏ qua")]
    public bool activateOnlyOnce = true;

    [Header("Debug (chỉ đọc)")]
    [SerializeField] private bool isActivated;

    private void Reset()
    {
        // Tự động bật isTrigger khi lần đầu add component, tránh quên cấu hình
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activateOnlyOnce && isActivated) return;

        // Lọc theo layer trước cho nhanh, tránh gọi GetComponentInParent không cần thiết
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;

        // Dùng GetComponentInParent phòng trường hợp collider nằm trên object con của Player
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        isActivated = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(transform);
        }
        else
        {
            Debug.LogWarning("Checkpoint: không tìm thấy GameManager.Instance trong scene.");
        }
    }
}