namespace InteresMe.API.Modules.Posts.Shares.Contracts.Responses;

public sealed class PublicPostAuthorResponse
{
    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
}
