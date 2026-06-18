using InteresMe.API.Modules.Interests.DTOs;

namespace InteresMe.API.Modules.Interests.Services;

public interface IInterestCatalogService
{
    Task<InterestCatalogResponse> GetCatalogAsync(
        CancellationToken cancellationToken = default);
}
