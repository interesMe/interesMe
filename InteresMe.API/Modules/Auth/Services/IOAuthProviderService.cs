using Google.Apis.Auth;

namespace InteresMe.API.Modules.Auth.Services;

public interface IOAuthProviderService
{
    Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default);
}