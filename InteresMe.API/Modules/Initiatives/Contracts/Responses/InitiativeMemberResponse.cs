using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Initiatives.Contracts.Responses;

/// <summary>A participant whose join request was accepted by the initiative owner.</summary>
public sealed class InitiativeMemberResponse
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? RoleName { get; set; }

    public DateTime JoinedAt { get; set; }
}

/// <summary>The current viewer's latest join request for the initiative (null when there is none).</summary>
public sealed class ViewerJoinRequestResponse
{
    public Guid Id { get; set; }

    public InitiativeJoinRequestStatus Status { get; set; }

    public Guid? RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
