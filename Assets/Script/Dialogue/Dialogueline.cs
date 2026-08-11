using System;
using UnityEngine;

/// <summary>
/// 1 câu thoại đơn lẻ trong DialogueNode, có thể có tên người nói RIÊNG cho câu đó.
/// Dùng khi trong cùng 1 node có nhiều người nói xen kẽ (ví dụ A nói, B đáp, A nói tiếp...).
/// </summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("Tên người nói câu này. Để trống nếu muốn dùng defaultSpeakerName của Node (người nói không đổi).")]
    public string speakerName;

    [Tooltip("Nội dung câu thoại")]
    [TextArea(2, 4)]
    public string text;

    [Tooltip("Ảnh chân dung người nói câu này. Để trống nếu muốn dùng defaultSpeakerPortrait của Node " +
             "(người nói không đổi), hoặc để trống cả 2 nếu không cần hiện portrait.")]
    public Sprite portrait;
}