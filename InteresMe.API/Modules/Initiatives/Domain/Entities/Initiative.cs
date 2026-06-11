using InteresMe.API.Modules.Auth.Models;
using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Initiatives.Domain.Entities;

public sealed class Initiative
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public InitiativeGoalType GoalType { get; set; }

    public string? University { get; set; }

    public int? TeamSize { get; set; }

    public InitiativeStatus Status { get; set; }

    public InitiativeVisibility Visibility { get; set; } = InitiativeVisibility.Public;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User OwnerUser { get; set; } = null!;

    public ICollection<InitiativeInterest> InitiativeInterests { get; set; } = [];

    public ICollection<InitiativeRole> Roles { get; set; } = [];

    public ICollection<InitiativeJoinRequest> JoinRequests { get; set; } = [];
}
