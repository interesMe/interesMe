using Google.Apis.Auth;

namespace InteresMe.API.Modules.Auth.Services;

public interface IOAuthProviderService
{
    Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(
        string idToken);
}