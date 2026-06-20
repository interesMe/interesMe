namespace InteresMe.API.Modules.Posts.Feed.Contracts.Responses;

public sealed class PostPageResponse
{
    public IReadOnlyList<PostResponse> Items { get; set; } = [];

    public string? NextCursor { get; set; }
}
