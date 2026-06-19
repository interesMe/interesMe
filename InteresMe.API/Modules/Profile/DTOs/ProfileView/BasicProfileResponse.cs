namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class BasicProfileResponse
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? City { get; set; }

    public string? Bio { get; set; }

    public string ActivityStatus { get; set; } = "unknown";

    public string ProfileStatus { get; set; } = "active_member";

    public List<ProfileSocialLinkResponse> SocialLinks { get; set; } = [];

    public DateTime JoinedAt { get; set; }
}
