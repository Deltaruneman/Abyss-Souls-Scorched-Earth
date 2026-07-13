using UnityEngine;

/// <summary>
/// Gắn script này vào Main Camera. Camera sẽ bám theo target (thường là Player)
/// với chuyển động làm mượt (SmoothDamp), có thể giới hạn trong bounds của map.
///
/// Việc xác nhận một object có đúng là Player hay không luôn dựa vào layer
/// (playerLayer) thay vì tag, để đồng bộ với cách PlayerController/Enemy/Bullet
/// trong project đang lọc Player qua LayerMask.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Đối tượng camera sẽ bám theo. Có thể để trống và để script tự tìm trong Awake bằng playerLayer.")]
    public Transform target;
    [Tooltip("Layer dùng để xác nhận (hoặc tự tìm) Player, tránh gán nhầm object khác vào target")]
    public LayerMask playerLayer;

    [Header("Follow")]
    [Tooltip("Độ lệch vị trí camera so với target. Trục Z nên để âm để camera lùi ra sau (2D)")]
    public Vector3 offset = new Vector3(0f, 1f, -10f);
    [Tooltip("Thời gian làm mượt chuyển động camera (giây). Giá trị nhỏ = bám sát hơn, giá trị lớn = mượt/trễ hơn")]
    public float smoothTime = 0.15f;
    [Tooltip("Có bám theo trục X hay không")]
    public bool followX = true;
    [Tooltip("Có bám theo trục Y hay không")]
    public bool followY = true;

    [Header("Bounds (tuỳ chọn)")]
    [Tooltip("Giới hạn camera trong phạm vi map, tránh lộ khoảng trống ngoài rìa map")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Vector3 velocity;

    private void Awake()
    {
        ValidateOrFindTarget();
    }

    /// <summary>
    /// Nếu đã gán target trong Inspector: kiểm tra target có thuộc playerLayer không,
    /// cảnh báo nếu gán nhầm object khác. Nếu chưa gán target: tự động quét scene
    /// để tìm object đầu tiên thuộc playerLayer.
    /// </summary>
    private void ValidateOrFindTarget()
    {
        if (target != null)
        {
            if (!IsOnPlayerLayer(target.gameObject))
            {
                Debug.LogWarning($"CameraFollow: object '{target.name}' được gán làm target nhưng không thuộc playerLayer. " +
                                  "Kiểm tra lại layer của object hoặc field playerLayer trên CameraFollow.", this);
            }
            return;
        }

        foreach (Transform candidate in FindObjectsOfType<Transform>())
        {
            if (IsOnPlayerLayer(candidate.gameObject))
            {
                target = candidate;
                return;
            }
        }

        Debug.LogError("CameraFollow: không có target được gán và không tìm thấy object nào thuộc playerLayer trong scene.", this);
    }

    /// <summary>
    /// Gọi hàm này (thay vì gán trực tiếp field target) khi cần đổi target lúc runtime
    /// (ví dụ Player respawn ra object mới). Object mới sẽ được xác nhận qua layer
    /// trước khi được chấp nhận làm target.
    /// </summary>
    public bool SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            target = null;
            return true;
        }

        if (!IsOnPlayerLayer(newTarget.gameObject))
        {
            Debug.LogWarning($"CameraFollow: từ chối set target '{newTarget.name}' vì không thuộc playerLayer.", this);
            return false;
        }

        target = newTarget;
        return true;
    }

    private bool IsOnPlayerLayer(GameObject obj)
    {
        return ((1 << obj.layer) & playerLayer) != 0;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = transform.position;
        Vector3 targetPosition = target.position + offset;

        if (followX) desiredPosition.x = targetPosition.x;
        if (followY) desiredPosition.y = targetPosition.y;
        desiredPosition.z = offset.z; // giữ khoảng cách camera cố định theo trục Z (chuẩn cho 2D)

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}