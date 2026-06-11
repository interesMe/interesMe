using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;

namespace InteresMe.API.Modules.Initiatives.Services.JoinRequests;

public interface IInitiativeJoinRequestService
{
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
