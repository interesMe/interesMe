using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Chat.Shared.Enums;

namespace InteresMe.API.Modules.Chat.Groups.Models;

public sealed class GroupChatParticipant
{
    public Guid Id { get; set; }

    public Guid GroupChatId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public ChatParticipantRole Role { get; set; }

    public GroupChat GroupChat { get; set; } = null!;

    public User User { get; set; } = null!;
}
