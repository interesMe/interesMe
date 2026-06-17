using InteresMe.API.Modules.Chat.Shared.Enums;

namespace InteresMe.API.Modules.Chat.Groups.DTOs;

public sealed class GroupChatParticipantDto
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public ChatParticipantRole Role { get; set; }

    public DateTime JoinedAt { get; set; }
}
