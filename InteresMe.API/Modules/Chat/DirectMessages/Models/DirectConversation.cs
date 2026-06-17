using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Chat.Shared.Models;

namespace InteresMe.API.Modules.Chat.DirectMessages.Models;

public sealed class DirectConversation
{
    public Guid Id { get; set; }

    public Guid UserOneId { get; set; }

    public Guid UserTwoId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User UserOne { get; set; } = null!;

    public User UserTwo { get; set; } = null!;

    public ICollection<DirectConversationParticipant> Participants { get; set; } = [];

    public ICollection<ChatMessage> Messages { get; set; } = [];
}
