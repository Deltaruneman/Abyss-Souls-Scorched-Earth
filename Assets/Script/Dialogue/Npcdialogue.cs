using UnityEngine;

/// <summary>
/// Gắn vào GameObject của NPC (cần Collider2D với isTrigger = true, kích thước
/// = vùng tương tác). Khi Player đứng trong vùng và bấm interactKey, hội thoại
/// bắt đầu từ startNode qua DialogueManager.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogue : MonoBehaviour, IDialogueSource
{
    [Header("Dialogue")]
    [Tooltip("Node bắt đầu hội thoại khi Player tương tác với NPC này")]
    public DialogueNode startNode;

    [Header("Interaction")]
    [Tooltip("Layer chứa object Player, dùng để lọc trigger (đồng bộ với các script khác trong project)")]
    public LayerMask playerLayer;
    [Tooltip("Phím tương tác để bắt đầu hội thoại khi Player đứng trong vùng")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("UI gợi ý hiện khi Player đứng gần (ví dụ icon 'Nhấn E'), để trống nếu không cần")]
    public GameObject interactPrompt;

    private bool playerInRange;
    private Transform playerTransform;
    private NPC npc;

    private void Awake()
    {
        // NPC.cs là tuỳ chọn: nếu NPC này không cần patrol/quay mặt thì để trống, mọi thứ vẫn hoạt động
        npc = GetComponent<NPC>();
    }

    private void Reset()
    {
        // Tự động bật isTrigger khi lần đầu add component, tránh quên cấu hình
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (startNode == null) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsDialogueActive) return;
        if (Time.frameCount == DialogueManager.Instance.DialogueEndFrame) return;

        if (Input.GetKeyDown(interactKey))
        {
            npc?.SetTalking(true, playerTransform);
            DialogueManager.Instance.StartDialogue(startNode, this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInRange = true;
        playerTransform = other.transform;
        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInRange = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        // Player rời vùng giữa lúc đang nói chuyện -> tự kết thúc hội thoại,
        // tránh trường hợp đứng xa NPC vẫn tiếp tục hội thoại được
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ForceEndDialogue(this);
        }
    }

    /// <summary>Được DialogueManager gọi khi hội thoại của NPC này kết thúc, bất kể kết thúc bằng cách nào.</summary>
    public void OnDialogueEnded()
    {
        npc?.SetTalking(false);
    }

    private bool IsPlayer(Collider2D other)
    {
        // Lọc theo layer trước cho nhanh, tránh gọi GetComponentInParent không cần thiết
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return false;

        // Dùng GetComponentInParent phòng trường hợp collider nằm trên object con của Player
        return other.GetComponentInParent<PlayerController>() != null;
    }
}