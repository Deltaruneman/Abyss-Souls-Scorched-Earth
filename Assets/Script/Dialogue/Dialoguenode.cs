using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 1 "nút" trong cây hội thoại: 1 câu thoại + (tuỳ chọn) các lựa chọn trả lời.
/// Mỗi đoạn hội thoại là 1 file asset riêng (Create > Dialogue > Dialogue Node),
/// các node nối với nhau bằng cách kéo-thả trực tiếp vào field "nextNode"/"autoContinueNode",
/// không cần quản lý ID thủ công.
/// </summary>
[CreateAssetMenu(fileName = "New Dialogue Node", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("Nội dung")]
    [Tooltip("Tên người nói MẶC ĐỊNH cho cả node. Dùng làm fallback cho các câu (DialogueLine) " +
             "không tự đặt speakerName riêng. Để trống nếu mỗi câu đều tự set tên riêng.")]
    public string defaultSpeakerName;

    [Tooltip("Ảnh chân dung MẶC ĐỊNH cho cả node. Dùng làm fallback cho các câu (DialogueLine) " +
             "không tự đặt portrait riêng. Để trống nếu mỗi câu đều tự set ảnh riêng, " +
             "hoặc node này không cần hiện portrait.")]
    public Sprite defaultSpeakerPortrait;

    [Tooltip("Ảnh nền (background) hiện ra khi node này được hiển thị. Tùy chọn - để trống nếu " +
             "không muốn đổi ảnh nền tại node này (giữ nguyên ảnh nền của node trước đó, hoặc ẩn đi " +
             "tùy theo setting hideBackgroundWhenEmpty ở DialogueManager).")]
    public Sprite backgroundImage;

    [Tooltip("Các câu thoại hiển thị LẦN LƯỢT trong node này (bấm phím tiếp tục để chuyển câu). " +
             "Mỗi câu có thể tự đặt tên người nói riêng (để trống thì dùng defaultSpeakerName ở trên). " +
             "Lựa chọn/autoContinueNode chỉ xuất hiện sau khi đã hiện xong câu cuối cùng trong list này.")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Lựa chọn (rẽ nhánh)")]
    [Tooltip("Các câu trả lời người chơi có thể chọn ở node này. " +
             "Để trống nếu node này chỉ là thoại thường (dùng autoContinueNode bên dưới thay vì lựa chọn).")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("Khi KHÔNG có lựa chọn")]
    [Tooltip("Chỉ áp dụng khi choices rỗng: node tiếp theo sẽ tự hiện ra khi người chơi bấm phím tiếp tục. " +
             "Để trống (None) nếu đây là câu thoại cuối cùng -> hội thoại kết thúc.")]
    public DialogueNode autoContinueNode;

    [Header("Sự kiện (tuỳ chọn)")]
    [Tooltip("Được gọi ngay khi node này hiện ra trên màn hình. Dùng để trigger quest, mở cửa, cộng item, v.v.")]
    public UnityEvent onEnter;
}