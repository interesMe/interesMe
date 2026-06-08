using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Services.Contracts;

namespace InteresMe.API.Modules.Auth.Services;

public interface IAuthService
{
    Task<ApplicationResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<RefreshResponse>> RefreshTokenAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        RefreshRequest? request,
        CancellationToken cancellationToken = default);

    Task DeleteMyAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<AuthResponse>> GoogleLoginAsync(
        GoogleAuthRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResult<AuthResponse>> GithubLoginAsync(
        GithubAuthRequest request,
        CancellationToken cancellationToken = default);

    ApplicationResult<GithubAuthorizationStart> StartGithubLogin(
        string redirectUri);

    Task<ApplicationResult<string>> CompleteGithubCallbackAsync(
        GithubAuthRequest request,
        string expectedState,
        string actualState,
        CancellationToken cancellationToken = default);

    ApplicationResult<AuthResponse> CompleteGithubSession(
        GithubSessionRequest request);
}
