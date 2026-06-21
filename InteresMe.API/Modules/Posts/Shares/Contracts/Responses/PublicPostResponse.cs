using InteresMe.API.Modules.Posts.Feed.Contracts.Responses;

namespace InteresMe.API.Modules.Posts.Shares.Contracts.Responses;

public sealed class PublicPostResponse
{
    public string Body { get; set; } = string.Empty;

    public PublicPostAuthorResponse Author { get; set; } = new();

    public PublicPostInitiativeResponse? Initiative { get; set; }

    public IReadOnlyList<PostAttachmentResponse> Attachments { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
