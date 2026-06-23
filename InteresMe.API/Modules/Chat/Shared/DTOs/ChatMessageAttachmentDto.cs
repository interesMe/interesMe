namespace InteresMe.API.Modules.Chat.Shared.DTOs;

public sealed class ChatMessageAttachmentDto
{
    public Guid Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }
}
