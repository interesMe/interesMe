namespace InteresMe.API.Modules.Chat.DirectMessages.DTOs;

public sealed class DirectConversationDto
{
    public Guid Id { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int ParticipantsCount { get; set; }

    public string? LastMessagePreview { get; set; }

    public List<DirectConversationParticipantDto> Participants { get; set; } = [];
}
