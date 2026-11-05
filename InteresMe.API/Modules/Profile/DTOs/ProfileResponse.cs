namespace InteresMe.API.Modules.Profile.DTOs;

public class ProfileResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Headline { get; set; }

    public string? Bio { get; set; }

    public string? City { get; set; }

    public string? AvatarUrl { get; set; }

    public DateOnly? BirthDate { get; set; }

    public IReadOnlyList<string> Skills { get; set; } = [];

    /// <summary>Selected interests (filled by GET /api/profile/me) so the edit form can pre-select them.</summary>
    public IReadOnlyList<Guid> InterestIds { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
