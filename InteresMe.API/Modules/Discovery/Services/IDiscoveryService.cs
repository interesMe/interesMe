using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Discovery.DTOs;

namespace InteresMe.API.Modules.Discovery.Services;

public interface IDiscoveryService
{
    IReadOnlyList<DiscoveryGoalResponse> GetGoals();

    Task<ApplicationResult<UserDiscoveryPreferenceResponse>> GetMyPreferenceAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<UserDiscoveryPreferenceResponse>> UpdateMyGoalAsync(
        Guid userId,
        UpdateUserGoalRequest request,
        CancellationToken cancellationToken = default);
}
