using UnityEngine;

/// <summary>
/// Gắn vào 1 GameObject có Collider2D (isTrigger = true) đánh dấu 1 khu vực trên map.
/// Khi Player đi vào vùng này, hội thoại từ startNode sẽ TỰ ĐỘNG bắt đầu, không cần bấm phím
/// (khác với NPCDialogue là phải đứng gần + bấm interactKey).
/// Dùng cho các tình huống kiểu: bước vào phòng thì nhân vật tự lẩm bẩm, đi qua cổng làng
/// thì có thoại giới thiệu, vào vùng nguy hiểm thì có cảnh báo, v.v.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AreaDialogueTrigger : MonoBehaviour, IDialogueSource
{
    [Header("Dialogue")]
    [Tooltip("Node bắt đầu hội thoại khi Player đi vào vùng này")]
    public DialogueNode startNode;

    [Header("Interaction")]
    [Tooltip("Layer chứa object Player, dùng để lọc trigger (đồng bộ với các script khác trong project)")]
    public LayerMask playerLayer;

    [Header("Settings")]
    [Tooltip("true: chỉ tự động kích hoạt 1 lần duy nhất trong suốt phiên chơi (ví dụ thoại giới thiệu khu vực). " +
             "false: mỗi lần Player đi vào vùng (sau khi đã rời ra) đều kích hoạt lại.")]
    public bool triggerOnce = true;

    [Tooltip("Nếu bật: Player rời khỏi vùng giữa lúc đang thoại thì tự động kết thúc hội thoại luôn, " +
             "giống cơ chế của NPCDialogue. Nên bật nếu vùng nhỏ/Player có thể đi xuyên qua nhanh.")]
    public bool endDialogueWhenPlayerLeaves = true;

    private bool hasTriggered;

    private void Reset()
    {
        // Tự động bật isTrigger khi lần đầu add component, tránh quên cấu hình
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (startNode == null) return;
        if (triggerOnce && hasTriggered) return;

        // Nếu đang có hội thoại khác diễn ra (ví dụ đang nói chuyện với NPC) thì không chen ngang
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsDialogueActive) return;

        hasTriggered = true;
        DialogueManager.Instance.StartDialogue(startNode, this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (!endDialogueWhenPlayerLeaves) return;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ForceEndDialogue(this);
        }
    }

    /// <summary>Được DialogueManager gọi khi hội thoại do vùng này khởi động đã kết thúc.</summary>
    public void OnDialogueEnded()
    {
        // Hiện chưa cần xử lý gì thêm, để trống sẵn phòng trường hợp sau này cần
        // (ví dụ: bật lại 1 hiệu ứng, đánh dấu quest, v.v.)
    }

    private bool IsPlayer(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return false;
        return other.GetComponentInParent<PlayerController>() != null;
    }
}