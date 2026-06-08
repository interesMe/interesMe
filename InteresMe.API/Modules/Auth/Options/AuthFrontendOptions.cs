namespace InteresMe.API.Modules.Auth.Options;

public sealed class AuthFrontendOptions
{
    public const string SectionName = "Auth:Frontend";

    public string OAuthCallbackUrl { get; init; } = "http://localhost:4201/auth/callback";
}
