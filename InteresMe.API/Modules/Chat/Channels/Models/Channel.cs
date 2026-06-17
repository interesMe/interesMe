using InteresMe.API.Modules.Chat.Shared.Models;
using InteresMe.API.Modules.Initiatives.Domain.Entities;

namespace InteresMe.API.Modules.Chat.Channels.Models;

public sealed class Channel
{
    public Guid Id { get; set; }

    public Guid? InitiativeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Initiative? Initiative { get; set; }

    public ICollection<ChannelMember> Members { get; set; } = [];

    public ICollection<ChatMessage> Messages { get; set; } = [];
}
