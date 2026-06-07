namespace InteresMe.API.Modules.Auth.Services.Contracts;

public sealed class OAuthUserInfo
{
    public string ProviderUserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
