using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.DTOs;

namespace InteresMe.API.Modules.Initiatives.Services;

public interface IInitiativeService
{
    Task<List<InitiativeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<InitiativeResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

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

    Task<ApplicationResult<InitiativeJoinRequestResponse>> CreateJoinRequestAsync(
        Guid userId,
        Guid initiativeId,
        CreateInitiativeJoinRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<List<InitiativeJoinRequestResponse>>> GetJoinRequestsAsync(
        Guid ownerUserId,
        Guid initiativeId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<InitiativeJoinRequestResponse>> AcceptJoinRequestAsync(
        Guid ownerUserId,
        Guid initiativeId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<InitiativeJoinRequestResponse>> RejectJoinRequestAsync(
        Guid ownerUserId,
        Guid initiativeId,
        Guid requestId,
        CancellationToken cancellationToken = default);
}
