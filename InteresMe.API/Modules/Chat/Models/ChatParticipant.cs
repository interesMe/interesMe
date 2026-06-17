using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Chat.Models;

public sealed class ChatParticipant
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public ChatParticipantRole Role { get; set; }

    public ChatRoom ChatRoom { get; set; } = null!;

    public User User { get; set; } = null!;
}
