using InteresMe.API.Modules.Posts.Feed.Contracts.Responses;

namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class ProfilePostPreviewResponse
{
    public Guid Id { get; set; }

    public string Body { get; set; } = string.Empty;

    public Guid? InitiativeId { get; set; }

    public string? InitiativeTitle { get; set; }

    public int LikesCount { get; set; }

    public IReadOnlyList<PostAttachmentResponse> Attachments { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
