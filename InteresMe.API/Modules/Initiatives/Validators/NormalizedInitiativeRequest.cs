using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Initiatives.Validators;

public sealed record NormalizedInitiativeRequest(
    string Title,
    string ShortDescription,
    InitiativeGoalType GoalType,
    List<Guid> InterestIds,
    List<string> Roles,
    string? University,
    int? TeamSize,
    InitiativeStatus? Status,
    InitiativeVisibility? Visibility);
