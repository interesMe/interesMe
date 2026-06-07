namespace InteresMe.API.Modules.Auth.DTOs;

public class GithubAuthRequest
{
    public string Code { get; set; } = string.Empty;

    public string? RedirectUri { get; set; }
}
