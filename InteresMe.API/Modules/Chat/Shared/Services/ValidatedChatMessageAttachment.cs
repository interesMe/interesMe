namespace InteresMe.API.Modules.Chat.Shared.Services;

public sealed record ValidatedChatMessageAttachment(
    IFormFile File,
    string CanonicalExtension,
    string FileName,
    string ContentType,
    long SizeBytes,
    int SortOrder);
