namespace InteresMe.API.Modules.Posts.Feed.Contracts.Responses;

public sealed class PostAttachmentResponse
{
    public Guid Id { get; set; }

    public string Kind { get; set; } = PostAttachmentKinds.Image;

    public string Url { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int Order { get; set; }
}
