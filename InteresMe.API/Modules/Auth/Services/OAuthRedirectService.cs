using InteresMe.API.Modules.Auth.Options;
using Microsoft.Extensions.Options;

namespace InteresMe.API.Modules.Auth.Services;

public sealed class OAuthRedirectService(
    IOptions<AuthFrontendOptions> frontendOptions) : IOAuthRedirectService
{
    private const string GithubStateCookieName = "interesme.github.state";

    public string BuildGithubCallbackUrl(HttpRequest request) =>
        $"{request.Scheme}://{request.Host}{request.PathBase}/api/auth/github/callback";

    public string BuildFrontendCallbackUrl(
        string? sessionCode = null,
        string? error = null)
    {
        var callbackUrl = frontendOptions.Value.OAuthCallbackUrl;

        if (!string.IsNullOrWhiteSpace(sessionCode))
        {
            return AppendQueryParameter(
                callbackUrl,
                "githubSessionCode",
                sessionCode);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            return AppendQueryParameter(
                callbackUrl,
                "error",
                error);
        }

        return callbackUrl;
    }

    public void SetGithubStateCookie(
        HttpResponse response,
        HttpRequest request,
        string state)
    {
        response.Cookies.Append(
            GithubStateCookieName,
            state,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(10),
                SameSite = SameSiteMode.Lax,
                Secure = request.IsHttps
            });
    }

    public string GetGithubStateCookie(HttpRequest request) =>
        request.Cookies[GithubStateCookieName] ?? string.Empty;

    public void DeleteGithubStateCookie(HttpResponse response) =>
        response.Cookies.Delete(GithubStateCookieName);

    private static string AppendQueryParameter(
        string url,
        string name,
        string value)
    {
        var separator = url.Contains('?') ? '&' : '?';

        return $"{url}{separator}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
    }
}
