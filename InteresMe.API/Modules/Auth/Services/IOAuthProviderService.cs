using Google.Apis.Auth;
using InteresMe.API.Modules.Auth.Services.Contracts;

namespace InteresMe.API.Modules.Auth.Services;

public interface IOAuthProviderService
{
    string BuildGithubAuthorizationUrl(
        string state,
        string redirectUri);

    Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default);

    Task<OAuthUserInfo> ValidateGithubCodeAsync(
        string code,
        string? redirectUri,
        CancellationToken cancellationToken = default);
}
