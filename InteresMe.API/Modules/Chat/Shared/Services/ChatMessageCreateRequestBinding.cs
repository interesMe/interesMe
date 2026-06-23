namespace InteresMe.API.Modules.Chat.Shared.Services;

public sealed record ChatMessageCreateRequestBinding(
    string? Text,
    IReadOnlyList<IFormFile> Attachments,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public bool IsValid => ErrorCode is null;
}
