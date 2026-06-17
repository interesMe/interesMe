using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Chat.Shared.Enums;

namespace InteresMe.API.Modules.Chat.Channels.Models;

public sealed class ChannelMember
{
    public Guid Id { get; set; }

    public Guid ChannelId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public ChatParticipantRole Role { get; set; }

    public Channel Channel { get; set; } = null!;

    public User User { get; set; } = null!;
}
