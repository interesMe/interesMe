using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.Interests.DTOs;

namespace InteresMe.API.Modules.Interests.Services;

public interface IInterestService
{
    Task<List<InterestResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<AuthResult<InterestResponse>> CreateAsync(
        CreateInterestRequest request,
        CancellationToken cancellationToken = default);
}
