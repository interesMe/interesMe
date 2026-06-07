using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Services.Contracts;

namespace InteresMe.API.Modules.Auth.Services;

public interface IAuthService
{
    Task<AuthResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<RefreshResponse>> RefreshTokenAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<AuthResponse>> GoogleLoginAsync(
        GoogleAuthRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResult<AuthResponse>> GithubLoginAsync(
        GithubAuthRequest request,
        CancellationToken cancellationToken = default);

    AuthResult<GithubAuthorizationStart> StartGithubLogin(
        string redirectUri);

    Task<AuthResult<string>> CompleteGithubCallbackAsync(
        GithubAuthRequest request,
        string expectedState,
        string actualState,
        CancellationToken cancellationToken = default);

    AuthResult<AuthResponse> CompleteGithubSession(
        GithubSessionRequest request);
}
