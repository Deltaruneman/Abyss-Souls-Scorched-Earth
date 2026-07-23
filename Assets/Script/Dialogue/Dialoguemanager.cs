using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton quản lý UI hội thoại và logic rẽ nhánh. Đặt trên 1 GameObject
/// trong scene (ví dụ chung với GameManager), chỉ cần 1 instance duy nhất.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel chứa toàn bộ UI hội thoại, sẽ bật/tắt khi bắt đầu/kết thúc hội thoại")]
    public GameObject dialoguePanel;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    [Tooltip("Transform cha chứa các nút lựa chọn được sinh ra động (nên có Vertical Layout Group)")]
    public Transform choicesContainer;
    [Tooltip("Prefab 1 nút lựa chọn (Button + TMP_Text con), kéo prefab từ Project window vào đây")]
    public Button choiceButtonPrefab;
    [Tooltip("UI gợi ý bấm phím để tiếp tục, chỉ hiện khi node hiện tại KHÔNG có lựa chọn nào")]
    public GameObject continuePrompt;

    [Header("Settings")]
    [Tooltip("Tạm dừng game (Time.timeScale = 0) trong lúc hội thoại đang diễn ra")]
    public bool pauseGameDuringDialogue = true;
    [Tooltip("Phím dùng để chuyển sang node tiếp theo khi node hiện tại không có lựa chọn")]
    public KeyCode continueKey = KeyCode.E;

    private DialogueNode currentNode;
    private int currentLineIndex;
    private NPCDialogue currentNPC;
    private readonly List<GameObject> spawnedChoiceButtons = new List<GameObject>();

    // Frame lúc StartDialogue() được gọi -> dùng để bỏ qua 1 lần đọc phím continueKey
    // ngay trong frame đó, tránh trường hợp phím E vừa dùng để MỞ hội thoại (từ NPCDialogue)
    // bị đọc thêm 1 lần nữa ở đây và nhảy luôn qua câu thoại đầu tiên.
    private int dialogueStartFrame = -1;

    // Frame lúc EndDialogue() vừa chạy -> NPCDialogue sẽ đọc field này để KHÔNG mở lại
    // hội thoại ngay trong cùng frame đó, tránh trường hợp continueKey trùng với interactKey
    // (mặc định cả 2 đều là E) khiến bấm 1 lần E ở câu cuối vừa kết thúc vừa mở lại hội thoại.
    public int DialogueEndFrame { get; private set; } = -1;

    public bool IsDialogueActive { get; private set; }
    public NPCDialogue CurrentNPC => currentNPC;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    private void Update()
    {
        if (!IsDialogueActive || currentNode == null) return;
        if (Time.frameCount == dialogueStartFrame) return;

        bool hasChoices = currentNode.choices != null && currentNode.choices.Count > 0;

        // Chỉ đọc phím continueKey khi KHÔNG phải trường hợp "đã ở câu cuối cùng và node có lựa chọn"
        // (trường hợp đó dùng nút bấm lựa chọn, xử lý qua SelectChoice, không dùng phím)
        if (Input.GetKeyDown(continueKey) && !(IsOnLastLine() && hasChoices))
        {
            Continue();
        }
    }

    /// <summary>Gọi từ NPCDialogue khi Player tương tác để bắt đầu 1 cuộc hội thoại.</summary>
    public void StartDialogue(DialogueNode startNode, NPCDialogue npc)
    {
        if (startNode == null || IsDialogueActive) return;

        currentNPC = npc;
        IsDialogueActive = true;
        dialogueStartFrame = Time.frameCount;

        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 0f;
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        ShowNode(startNode);
    }

    private void ShowNode(DialogueNode node)
    {
        currentNode = node;
        currentLineIndex = 0;
        DisplayCurrentLine();
    }

    /// <summary>
    /// Hiện câu thoại tại currentLineIndex của currentNode. Nếu chưa phải câu cuối cùng
    /// trong node -> chỉ hiện continuePrompt, CHƯA hiện choices (choices chỉ xuất hiện
    /// sau khi đã đọc hết các câu trong node).
    /// </summary>
    private void DisplayCurrentLine()
    {
        if (speakerNameText != null) speakerNameText.text = currentNode.speakerName;

        string line = (currentNode.lines != null && currentLineIndex < currentNode.lines.Count)
            ? currentNode.lines[currentLineIndex]
            : string.Empty;
        if (dialogueText != null) dialogueText.text = line;

        // onEnter chỉ gọi 1 lần khi vừa vào node (ở câu đầu tiên), không gọi lại mỗi lần lướt câu
        if (currentLineIndex == 0)
        {
            currentNode.onEnter?.Invoke();
        }

        ClearChoiceButtons();

        if (!IsOnLastLine())
        {
            if (continuePrompt != null) continuePrompt.SetActive(true);
            return;
        }

        // Đã ở câu cuối cùng của node -> tới lúc hiện lựa chọn (nếu có) hoặc chờ bấm tiếp tục
        bool hasChoices = currentNode.choices != null && currentNode.choices.Count > 0;

        if (continuePrompt != null) continuePrompt.SetActive(!hasChoices);

        if (hasChoices)
        {
            foreach (DialogueChoice choice in currentNode.choices)
            {
                SpawnChoiceButton(choice);
            }
        }
    }

    /// <summary>Số câu thoại thực tế của 1 node (tối thiểu 1, phòng trường hợp lines để trống).</summary>
    private int GetLineCount(DialogueNode node)
    {
        return (node.lines != null && node.lines.Count > 0) ? node.lines.Count : 1;
    }

    private bool IsOnLastLine()
    {
        return currentLineIndex >= GetLineCount(currentNode) - 1;
    }

    private void SpawnChoiceButton(DialogueChoice choice)
    {
        if (choiceButtonPrefab == null || choicesContainer == null) return;

        // Time.timeScale = 0 không ảnh hưởng Instantiate/UI, chỉ ảnh hưởng vật lý & Update thường
        Button button = Instantiate(choiceButtonPrefab, choicesContainer);
        button.gameObject.SetActive(true);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = choice.choiceText;

        button.onClick.AddListener(() => SelectChoice(choice));
        spawnedChoiceButtons.Add(button.gameObject);
    }

    private void ClearChoiceButtons()
    {
        foreach (GameObject btn in spawnedChoiceButtons)
        {
            Destroy(btn);
        }
        spawnedChoiceButtons.Clear();
    }

    /// <summary>Gọi khi người chơi bấm 1 nút lựa chọn (đăng ký qua SpawnChoiceButton).</summary>
    private void SelectChoice(DialogueChoice choice)
    {
        if (choice.nextNode != null)
        {
            ShowNode(choice.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>Gọi khi người chơi bấm continueKey (đang xem 1 câu chưa phải lựa chọn cuối).</summary>
    private void Continue()
    {
        if (!IsOnLastLine())
        {
            currentLineIndex++;
            DisplayCurrentLine();
            return;
        }

        // Đã hết câu trong node hiện tại và node không có lựa chọn -> chuyển node kế hoặc kết thúc
        if (currentNode.autoContinueNode != null)
        {
            ShowNode(currentNode.autoContinueNode);
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// Buộc kết thúc hội thoại từ bên ngoài (ví dụ NPCDialogue gọi khi Player rời khỏi vùng tương tác).
    /// Chỉ có tác dụng nếu npc truyền vào đúng là NPC đang hội thoại, tránh 1 NPC khác lỡ tay tắt hội thoại hộ.
    /// </summary>
    public void ForceEndDialogue(NPCDialogue npc)
    {
        if (currentNPC != npc) return;
        EndDialogue();
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;
        currentNode = null;
        currentLineIndex = 0;
        DialogueEndFrame = Time.frameCount;

        ClearChoiceButtons();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continuePrompt != null) continuePrompt.SetActive(false);

        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 1f;
        }

        // Báo cho NPC biết hội thoại đã kết thúc (để NPC.SetTalking(false), tiếp tục patrol),
        // gọi sau khi đã dừng game/UI để đảm bảo currentNPC vẫn còn hợp lệ lúc gọi
        currentNPC?.OnDialogueEnded();
        currentNPC = null;
    }
}