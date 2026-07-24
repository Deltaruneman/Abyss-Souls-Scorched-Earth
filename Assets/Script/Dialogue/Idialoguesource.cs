/// <summary>
/// Bất kỳ đối tượng nào có thể khởi động 1 cuộc hội thoại qua DialogueManager
/// (NPC, vùng trigger tự động, rương đồ, cutscene, ...) đều implement interface này.
/// Giúp DialogueManager không cần biết cụ thể "ai" mở hội thoại, chỉ cần biết
/// cách báo lại cho người mở khi hội thoại kết thúc.
/// </summary>
public interface IDialogueSource
{
    /// <summary>Được DialogueManager gọi khi hội thoại do source này khởi động đã kết thúc,
    /// bất kể kết thúc bằng cách nào (hết node, Player rời vùng tương tác/khu vực, v.v.).</summary>
    void OnDialogueEnded();
}