using InteresMe.API.Modules.Chat.Models;

namespace InteresMe.API.Modules.Chat.DTOs;

public sealed class ChatParticipantDto
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public ChatParticipantRole Role { get; set; }

    public DateTime JoinedAt { get; set; }
}
