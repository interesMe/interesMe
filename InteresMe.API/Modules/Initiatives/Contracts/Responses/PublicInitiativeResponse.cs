using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Initiatives.Contracts.Responses;

public sealed class PublicInitiativeResponse
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public InitiativeGoalType GoalType { get; set; }

    public InitiativeStatus Status { get; set; }

    public InitiativeVisibility Visibility { get; set; }

    public string? University { get; set; }

    public int? TeamSize { get; set; }

    public string? OwnerDisplayName { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<InitiativeInterestResponse> Interests { get; set; } = [];

    public List<InitiativeRoleResponse> Roles { get; set; } = [];
}
