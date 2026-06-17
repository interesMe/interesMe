namespace InteresMe.API.Modules.Chat.Channels.DTOs;

public sealed class CreateChannelRequest
{
    public string Title { get; set; } = string.Empty;

    public Guid? InitiativeId { get; set; }
}
