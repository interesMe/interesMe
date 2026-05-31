using Google.Apis.Auth;

namespace InteresMe.API.Modules.Auth.Services;

public sealed class OAuthProviderService : IOAuthProviderService
{
    private readonly IConfiguration configuration;

    public OAuthProviderService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException(
                "Google id token is required.",
                nameof(idToken));
        }

        var clientId = configuration["Google:ClientId"];

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Google ClientId is not configured.");
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            });

        if (payload is null)
        {
            throw new InvalidOperationException(
                "Failed to validate Google token.");
        }

        if (string.IsNullOrWhiteSpace(payload.Subject))
        {
            throw new InvalidOperationException(
                "Google token does not contain subject.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new InvalidOperationException(
                "Google token does not contain email.");
        }

        if (!payload.EmailVerified)
        {
            throw new InvalidOperationException(
                "Google email is not verified.");
        }

        return payload;
    }
}