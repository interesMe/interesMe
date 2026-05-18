namespace InteresMe.API.Modules.Chat.DTOs;

public class MessageResponse
{
    public Guid Id { get; set; }

    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }
}
