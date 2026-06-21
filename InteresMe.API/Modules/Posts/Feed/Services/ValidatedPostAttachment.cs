namespace InteresMe.API.Modules.Posts.Feed.Services;

public sealed record ValidatedPostAttachment(
    IFormFile File,
    string CanonicalExtension,
    string ContentType,
    long SizeBytes,
    int SortOrder);
