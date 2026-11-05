using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Initiatives.Contracts.Responses;

public sealed class InitiativeResponse
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public InitiativeGoalType GoalType { get; set; }

    public string? University { get; set; }

    public int? TeamSize { get; set; }

    public InitiativeStatus Status { get; set; }

    public InitiativeVisibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<InitiativeInterestResponse> Interests { get; set; } = [];

    public List<InitiativeRoleResponse> Roles { get; set; } = [];

    /// <summary>Accepted participants (owner excluded). Filled for the details view.</summary>
    public List<InitiativeMemberResponse> Members { get; set; } = [];

    /// <summary>Number of accepted participants (owner excluded).</summary>
    public int MemberCount { get; set; }

    public ViewerJoinRequestResponse? MyJoinRequest { get; set; }
}
