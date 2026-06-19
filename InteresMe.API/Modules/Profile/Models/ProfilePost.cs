using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Profile.Models;

public sealed class ProfilePost
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? InterestPath { get; set; }

    public string? MediaUrlsJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
