using Google.Apis.Auth;

namespace InteresMe.API.Modules.Auth.Services;

public sealed class OAuthProviderService : IOAuthProviderService
{
    private readonly IConfiguration configuration;

    public OAuthProviderService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload> ValidateTokenAsync(
        string idToken)
    {
        return await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience =
                [
                    configuration["Google:ClientId"]!
                ]
            });
    }
}