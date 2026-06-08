namespace InteresMe.API.Modules.Auth.Services;

public interface IOAuthRedirectService
{
    string BuildGithubCallbackUrl(HttpRequest request);

    string BuildFrontendCallbackUrl(string? sessionCode = null, string? error = null);

    void SetGithubStateCookie(HttpResponse response, HttpRequest request, string state);

    string GetGithubStateCookie(HttpRequest request);

    void DeleteGithubStateCookie(HttpResponse response);
}
