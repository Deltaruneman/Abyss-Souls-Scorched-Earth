using System;
using UnityEngine;

/// <summary>
/// 1 lựa chọn trả lời trong hội thoại. Gắn trực tiếp trong list "choices" của DialogueNode.
/// </summary>
[Serializable]
public class DialogueChoice
{
    [Tooltip("Nội dung hiển thị trên nút để người chơi chọn")]
    public string choiceText;

    [Tooltip("Node hội thoại tiếp theo khi người chơi chọn câu này. " +
             "Để trống (None) nếu chọn xong thì kết thúc hội thoại luôn.")]
    public DialogueNode nextNode;
}