namespace InteresMe.API.Modules.Posts.Feed.Contracts.Responses;

public sealed class PostResponse
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }

    public string AuthorDisplayName { get; set; } = string.Empty;

    public string? AuthorAvatarUrl { get; set; }

    public string Body { get; set; } = string.Empty;

    public Guid? InitiativeId { get; set; }

    public string? InitiativeTitle { get; set; }

    public int LikesCount { get; set; }

    public bool IsLikedByCurrentUser { get; set; }

    public IReadOnlyList<PostAttachmentResponse> Attachments { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
