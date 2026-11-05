using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Discovery.DTOs;

namespace InteresMe.API.Modules.Discovery.Services;

public interface IPeopleSearchService
{
    Task<ApplicationResult<PeopleSearchResponse>> SearchByInterestsAsync(
        Guid viewerUserId,
        IReadOnlyCollection<Guid>? interestIds,
        int? limit,
        CancellationToken cancellationToken = default);
}
