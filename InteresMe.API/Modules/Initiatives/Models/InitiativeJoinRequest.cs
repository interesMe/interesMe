using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Initiatives.Models;

public sealed class InitiativeJoinRequest
{
    public Guid Id { get; set; }

    public Guid InitiativeId { get; set; }

    public Guid UserId { get; set; }

    public string? Message { get; set; }

    public InitiativeJoinRequestStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Initiative Initiative { get; set; } = null!;

    public User User { get; set; } = null!;
}
