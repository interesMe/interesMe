using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Enums;

namespace InteresMe.API.Modules.Initiatives.Services.InitiativeQueries;

public interface IInitiativeQueryService
{
    /// <summary>Open initiatives visible to the viewer: public ones plus the viewer's own.</summary>
    Task<ApplicationResult<List<InitiativeResponse>>> GetAllAsync(
        Guid viewerUserId,
        string? search = null,
        InitiativeGoalType? goalType = null,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>All initiatives owned by the viewer, any status or visibility.</summary>
    Task<List<InitiativeResponse>> GetMineAsync(
        Guid viewerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Details with owner, team and the viewer's join request. Private initiatives are visible
    /// only to the owner and accepted members; otherwise NotFound is returned (no existence leak).</summary>
    Task<ApplicationResult<InitiativeResponse>> GetByIdAsync(
        Guid viewerUserId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<PublicInitiativeResponse>> GetPublicBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
