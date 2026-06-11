using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;

namespace InteresMe.API.Modules.Initiatives.Services.InitiativeManagement;

public interface IInitiativeManagementService
{
    Task<ApplicationResult<InitiativeResponse>> CreateAsync(
        Guid ownerUserId,
        CreateInitiativeRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<InitiativeResponse>> UpdateAsync(
        Guid ownerUserId,
        Guid id,
        UpdateInitiativeRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<bool>> DeleteAsync(
        Guid ownerUserId,
        Guid id,
        CancellationToken cancellationToken = default);
}
