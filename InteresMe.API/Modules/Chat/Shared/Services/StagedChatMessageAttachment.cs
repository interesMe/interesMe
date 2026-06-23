namespace InteresMe.API.Modules.Chat.Shared.Services;

public sealed record StagedChatMessageAttachment(
    Guid Id,
    string StagingPath,
    string FinalPath,
    string StoragePath,
    string ContentType,
    long SizeBytes,
    int SortOrder);
