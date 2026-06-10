using InteresMe.API.Modules.Auth.Models;

namespace InteresMe.API.Modules.Initiatives.Models;

public sealed class Initiative
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public InitiativeGoalType GoalType { get; set; }

    public string? University { get; set; }

    public int? TeamSize { get; set; }

    public InitiativeStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User OwnerUser { get; set; } = null!;

    public ICollection<InitiativeInterest> InitiativeInterests { get; set; } = [];

    public ICollection<InitiativeRole> Roles { get; set; } = [];

    public ICollection<InitiativeJoinRequest> JoinRequests { get; set; } = [];
}
