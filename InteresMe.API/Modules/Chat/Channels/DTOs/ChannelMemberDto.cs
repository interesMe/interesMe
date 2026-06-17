using InteresMe.API.Modules.Chat.Shared.Enums;

namespace InteresMe.API.Modules.Chat.Channels.DTOs;

public sealed class ChannelMemberDto
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public ChatParticipantRole Role { get; set; }

    public DateTime JoinedAt { get; set; }
}
