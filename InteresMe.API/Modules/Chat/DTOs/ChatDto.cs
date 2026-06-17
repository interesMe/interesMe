namespace InteresMe.API.Modules.Chat.DTOs;

public sealed class ChatDto
{
    public Guid Id { get; set; }

    public Guid? InitiativeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public int ParticipantsCount { get; set; }

    public string? LastMessagePreview { get; set; }

    public List<ChatParticipantDto> Participants { get; set; } = [];
}
