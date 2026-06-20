namespace InteresMe.API.Modules.Posts.Comments.Contracts.Responses;

public sealed class CommentResponse
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public CommentAuthorResponse Author { get; set; } = new();

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
