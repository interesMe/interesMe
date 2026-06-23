namespace InteresMe.API.Modules.Chat.Shared.Models;

public sealed class ChatMessageAttachment
{
    public Guid Id { get; set; }

    public Guid ChatMessageId { get; set; }

    public string Url { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }

    public ChatMessage ChatMessage { get; set; } = null!;
}
