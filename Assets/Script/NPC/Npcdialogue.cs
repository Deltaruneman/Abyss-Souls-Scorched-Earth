using UnityEngine;

/// <summary>
/// Gắn vào GameObject của NPC (cần Collider2D với isTrigger = true, kích thước
/// = vùng tương tác). Khi Player đứng trong vùng và bấm interactKey, hội thoại
/// bắt đầu từ dialogueSequence[talkCount] qua DialogueManager. Mỗi lần tương tác
/// thành công, talkCount tăng lên 1 để lần sau nói node tiếp theo trong danh sách.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogue : MonoBehaviour, IDialogueSource
{
    [Header("Dialogue")]
    [Tooltip("Danh sách node hội thoại theo thứ tự: lần tương tác thứ 1 dùng phần tử [0], " +
             "lần thứ 2 dùng [1], v.v. Khi hết danh sách, hành vi tuỳ theo 'repeatLastNode' bên dưới.")]
    public DialogueNode[] dialogueSequence;
    [Tooltip("Bật: khi đã nói hết danh sách, các lần tương tác sau sẽ lặp lại node CUỐI CÙNG mãi mãi.\n" +
             "Tắt: khi đã nói hết danh sách, NPC sẽ không phản hồi tương tác nữa (im lặng).")]
    public bool repeatLastNode = true;

    // Số lần Player đã tương tác thành công với NPC này, dùng làm index cho dialogueSequence
    private int talkCount;

    // Backward-compat: nếu ai vẫn set giá trị ở đây qua code cũ, nó sẽ được dùng làm phần tử [0]
    // khi dialogueSequence trống. Không hiện trong Inspector nữa để tránh nhầm lẫn với hệ thống mới.
    [HideInInspector] public DialogueNode startNode;

    [Header("Cutscene (tuỳ chọn)")]
    [Tooltip("Bật để phát 1 đoạn video (cutscene) ngay sau khi Player nghe xong node hội thoại CUỐI CÙNG " +
             "trong dialogueSequence. Chỉ kích hoạt khi hội thoại kết thúc TỰ NHIÊN (Player đọc hết), " +
             "không kích hoạt nếu Player rời vùng tương tác giữa chừng.")]
    public bool playCutsceneAfterFinalNode = false;
    [Tooltip("File video sẽ phát (kéo file .mp4/.mov đã import vào Unity dưới dạng VideoClip vào đây)")]
    public UnityEngine.Video.VideoClip endCutsceneClip;

    // Node vừa được bắt đầu ở lần tương tác gần nhất, dùng để biết Player có vừa nghe xong node CUỐI hay không
    private DialogueNode lastNodeStarted;
    // Đảm bảo cutscene chỉ phát đúng 1 lần, kể cả khi repeatLastNode = true và Player nói chuyện lại sau đó
    private bool cutscenePlayed;

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

        DialogueNode nodeToPlay = GetNodeForCurrentTalkCount();
        if (nodeToPlay == null) return;

        if (DialogueManager.Instance == null || DialogueManager.Instance.IsDialogueActive) return;
        if (Time.frameCount == DialogueManager.Instance.DialogueEndFrame) return;

        if (Input.GetKeyDown(interactKey))
        {
            npc?.SetTalking(true, playerTransform);
            lastNodeStarted = nodeToPlay;
            DialogueManager.Instance.StartDialogue(nodeToPlay, this);
            talkCount++;
        }
    }

    /// <summary>
    /// Trả về node hội thoại tương ứng với lần tương tác hiện tại (dựa trên talkCount).
    /// Trả về null nếu không có node nào để nói (ví dụ đã hết danh sách và repeatLastNode = false).
    /// </summary>
    private DialogueNode GetNodeForCurrentTalkCount()
    {
        // Fallback: chưa gán dialogueSequence nhưng vẫn còn dùng startNode kiểu cũ
        if ((dialogueSequence == null || dialogueSequence.Length == 0))
        {
            return talkCount == 0 ? startNode : (repeatLastNode ? startNode : null);
        }

        if (talkCount < dialogueSequence.Length)
        {
            return dialogueSequence[talkCount];
        }

        // Đã nói hết danh sách
        if (!repeatLastNode) return null;
        return dialogueSequence[dialogueSequence.Length - 1];
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

        TryTriggerEndCutscene();
    }

    /// <summary>
    /// Phát cutscene nếu: đã bật playCutsceneAfterFinalNode, có gán clip, node vừa nói là node CUỐI
    /// trong dialogueSequence, hội thoại kết thúc tự nhiên (playerInRange vẫn true), và chưa từng phát trước đó.
    /// </summary>
    private void TryTriggerEndCutscene()
    {
        if (!playCutsceneAfterFinalNode || cutscenePlayed) return;
        if (endCutsceneClip == null) return;
        if (dialogueSequence == null || dialogueSequence.Length == 0) return;

        // Chỉ trigger nếu node vừa nói chính là node cuối cùng trong danh sách
        if (lastNodeStarted != dialogueSequence[dialogueSequence.Length - 1]) return;

        // Chỉ trigger nếu hội thoại kết thúc TỰ NHIÊN (Player vẫn còn trong vùng tương tác).
        // Nếu Player rời vùng giữa chừng, OnTriggerExit2D đã set playerInRange = false
        // TRƯỚC KHI gọi ForceEndDialogue -> OnDialogueEnded, nên ở đây playerInRange sẽ là false.
        if (!playerInRange) return;

        cutscenePlayed = true;

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.PlayCutscene(endCutsceneClip);
        }
        else
        {
            Debug.LogWarning($"[{name}] Không tìm thấy CutsceneManager.Instance trong scene để phát cutscene.");
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        // Lọc theo layer trước cho nhanh, tránh gọi GetComponentInParent không cần thiết
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return false;

        // Dùng GetComponentInParent phòng trường hợp collider nằm trên object con của Player
        return other.GetComponentInParent<PlayerController>() != null;
    }
}