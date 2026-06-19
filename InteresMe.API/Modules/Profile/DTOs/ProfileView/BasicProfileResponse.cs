namespace InteresMe.API.Modules.Profile.DTOs.ProfileView;

public sealed class BasicProfileResponse
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? City { get; set; }

    public DateTime JoinedAt { get; set; }
}
