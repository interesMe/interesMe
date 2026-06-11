using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Initiatives.Contracts.Requests;

public sealed class CreateInitiativeRequest
{
    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public InitiativeGoalType GoalType { get; set; }

    public List<Guid> InterestIds { get; set; } = [];

    public List<string> Roles { get; set; } = [];

    public string? University { get; set; }

    public int? TeamSize { get; set; }

    public InitiativeStatus? Status { get; set; }

    public InitiativeVisibility? Visibility { get; set; }
}
