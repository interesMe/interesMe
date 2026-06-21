namespace InteresMe.API.Modules.Posts.Feed;

internal static class PostFeedRules
{
    public const int MaxBodyLength = 1200;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int MaxCursorLength = 128;
    public const int MaxAttachmentCount = 4;
    public const long MaxAttachmentSizeBytes = 5 * 1024 * 1024;
    public const long MaxTotalAttachmentSizeBytes = 20 * 1024 * 1024;
    public const long MaxMultipartRequestSizeBytes = MaxTotalAttachmentSizeBytes + (1024 * 1024);
}
