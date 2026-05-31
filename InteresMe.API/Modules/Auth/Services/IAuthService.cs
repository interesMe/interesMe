using InteresMe.API.Modules.Auth.DTOs;

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
}