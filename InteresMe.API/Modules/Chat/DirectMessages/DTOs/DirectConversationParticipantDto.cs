namespace InteresMe.API.Modules.Chat.DirectMessages.DTOs;

public sealed class DirectConversationParticipantDto
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}
