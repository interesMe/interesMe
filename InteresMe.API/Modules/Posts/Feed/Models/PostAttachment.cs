namespace InteresMe.API.Modules.Posts.Feed.Models;

public sealed class PostAttachment
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public string Kind { get; set; } = PostAttachmentKinds.Image;

    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public Post Post { get; set; } = null!;
}
