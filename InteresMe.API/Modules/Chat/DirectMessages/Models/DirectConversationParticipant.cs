using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Chat.DirectMessages.Models;

public sealed class DirectConversationParticipant
{
    public Guid Id { get; set; }

    public Guid DirectConversationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DirectConversation DirectConversation { get; set; } = null!;

    public User User { get; set; } = null!;
}
