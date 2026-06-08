using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Interests.DTOs;

namespace InteresMe.API.Modules.Interests.Services;

public interface IInterestService
{
    Task<List<InterestResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<InterestResponse>> CreateAsync(
        CreateInterestRequest request,
        CancellationToken cancellationToken = default);
}
