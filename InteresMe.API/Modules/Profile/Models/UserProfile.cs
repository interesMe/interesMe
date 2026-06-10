using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Profile.Models;

public sealed class UserProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Headline { get; set; }

    public string? Bio { get; set; }

    public string? City { get; set; }

    public string? AvatarUrl { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
