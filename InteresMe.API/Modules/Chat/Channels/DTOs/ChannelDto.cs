namespace InteresMe.API.Modules.Chat.Channels.DTOs;

public sealed class ChannelDto
{
    public Guid Id { get; set; }

    public Guid? InitiativeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public int MembersCount { get; set; }

    public List<ChannelMemberDto> Members { get; set; } = [];
}
