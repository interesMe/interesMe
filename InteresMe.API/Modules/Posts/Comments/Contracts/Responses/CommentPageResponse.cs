namespace InteresMe.API.Modules.Posts.Comments.Contracts.Responses;

public sealed class CommentPageResponse
{
    public IReadOnlyList<CommentResponse> Items { get; set; } = [];

    public string? NextCursor { get; set; }
}
