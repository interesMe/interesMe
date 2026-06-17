using InteresMe.API.Modules.Initiatives.Domain.Entities;

namespace InteresMe.API.Modules.Chat.Models;

public sealed class ChatRoom
{
    public Guid Id { get; set; }

    public Guid? InitiativeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Initiative? Initiative { get; set; }

    public ICollection<ChatParticipant> Participants { get; set; } = [];

    public ICollection<ChatMessage> Messages { get; set; } = [];
}
