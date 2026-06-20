namespace InteresMe.API.Modules.Posts.Comments.Contracts.Responses;

public sealed class CommentAuthorResponse
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
}
