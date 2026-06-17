using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Chat.Models;

public sealed class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    public Guid SenderUserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }

    public ChatRoom ChatRoom { get; set; } = null!;

    public User SenderUser { get; set; } = null!;
}
