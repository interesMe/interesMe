namespace InteresMe.API.Modules.Chat.DTOs;

public class SendMessageRequest
{
    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    public string Content { get; set; } = string.Empty;
}
