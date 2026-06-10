using InteresMe.API.Modules.Initiatives.Models;

namespace InteresMe.API.Modules.Initiatives.DTOs;

public sealed class UpdateInitiativeRequest
{
    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public InitiativeGoalType GoalType { get; set; }

    public List<Guid> InterestIds { get; set; } = [];

    public List<string> Roles { get; set; } = [];

    public string? University { get; set; }

    public int? TeamSize { get; set; }

    public InitiativeStatus? Status { get; set; }
}
