using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;

namespace InteresMe.API.Modules.Initiatives.Services.InitiativeQueries;

public interface IInitiativeQueryService
{
    Task<List<InitiativeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<InitiativeResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<PublicInitiativeResponse>> GetPublicBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
