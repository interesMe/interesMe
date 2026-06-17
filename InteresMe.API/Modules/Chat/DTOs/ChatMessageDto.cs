namespace InteresMe.API.Modules.Chat.DTOs;

public sealed class ChatMessageDto
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    public Guid SenderUserId { get; set; }

    public string? SenderDisplayName { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }
}
