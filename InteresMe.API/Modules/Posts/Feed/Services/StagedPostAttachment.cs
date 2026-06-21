namespace InteresMe.API.Modules.Posts.Feed.Services;

public sealed record StagedPostAttachment(
    Guid Id,
    string StagingPath,
    string FinalPath,
    string StoragePath,
    string ContentType,
    long SizeBytes,
    int SortOrder);
