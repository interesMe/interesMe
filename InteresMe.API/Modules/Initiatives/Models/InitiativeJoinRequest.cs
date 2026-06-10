using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Initiatives.Models;

public sealed class InitiativeJoinRequest
{
    public Guid Id { get; set; }

    public Guid InitiativeId { get; set; }

    public Guid UserId { get; set; }

    public Guid? RoleId { get; set; }

    public string? Message { get; set; }

    public string? Motivation { get; set; }

    public string? Experience { get; set; }

    public string? Contribution { get; set; }

    public string? Availability { get; set; }

    public InitiativeJoinRequestStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Initiative Initiative { get; set; } = null!;

    public User User { get; set; } = null!;

    public InitiativeRole? Role { get; set; }
}
