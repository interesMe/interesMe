namespace InteresMe.API.Modules.Auth.Services.Contracts;

public sealed class GithubAuthorizationStart
{
    public string AuthorizationUrl { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;
}
