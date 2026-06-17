namespace InteresMe.API.Modules.Chat.Groups.DTOs;

public sealed class GroupChatDto
{
    public Guid Id { get; set; }

    public Guid? InitiativeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public int ParticipantsCount { get; set; }

    public string? LastMessagePreview { get; set; }

    public List<GroupChatParticipantDto> Participants { get; set; } = [];
}
