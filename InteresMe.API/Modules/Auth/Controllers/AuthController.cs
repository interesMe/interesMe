using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.Modules.Auth.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IConfiguration configuration) : ControllerBase
{
    private const string GithubStateCookieName = "interesme.github.state";

    [HttpPost("register")]
    public Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.RegisterAsync(request, cancellationToken));

    [HttpPost("google")]
    public Task<IActionResult> GoogleLogin(
        [FromBody] GoogleAuthRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(
            authService.GoogleLoginAsync(
                request,
                cancellationToken));

    [HttpGet("github")]
    public IActionResult StartGithubLogin()
    {
        var callbackUrl = BuildGithubCallbackUrl();
        var result = authService.StartGithubLogin(callbackUrl);

        if (!result.IsSuccess || result.Response is null)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        Response.Cookies.Append(
            GithubStateCookieName,
            result.Response.State,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(10),
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        return Redirect(result.Response.AuthorizationUrl);
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GithubCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect(BuildFrontendCallbackUrl(error: "github_denied"));
        }

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(state))
        {
            return Redirect(BuildFrontendCallbackUrl(error: "github_invalid_callback"));
        }

        var expectedState = Request.Cookies[GithubStateCookieName] ?? string.Empty;
        Response.Cookies.Delete(GithubStateCookieName);

        var result = await authService.CompleteGithubCallbackAsync(
            new GithubAuthRequest
            {
                Code = code,
                RedirectUri = BuildGithubCallbackUrl()
            },
            expectedState,
            state,
            cancellationToken);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Response))
        {
            return Redirect(BuildFrontendCallbackUrl(error: "github_auth_failed"));
        }

        return Redirect(BuildFrontendCallbackUrl(sessionCode: result.Response));
    }

    [HttpPost("github")]
    public Task<IActionResult> GithubLogin(
        [FromBody] GithubAuthRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(
            authService.GithubLoginAsync(
                request,
                cancellationToken));

    [HttpPost("github/session")]
    public Task<IActionResult> CompleteGithubSession(
        [FromBody] GithubSessionRequest request) =>
        ToActionResult(Task.FromResult(authService.CompleteGithubSession(request)));

    [HttpPost("login")]
    public Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    public Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.RefreshTokenAsync(request, cancellationToken));

    private string BuildGithubCallbackUrl() =>
        $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/auth/github/callback";

    private string BuildFrontendCallbackUrl(
        string? sessionCode = null,
        string? error = null)
    {
        var callbackUrl =
            configuration["Frontend:OAuthCallbackUrl"] ??
            configuration["FRONTEND_OAUTH_CALLBACK_URL"] ??
            "http://localhost:4201/auth/callback";

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

    private static string AppendQueryParameter(
        string url,
        string name,
        string value)
    {
        var separator = url.Contains('?') ? '&' : '?';

        return $"{url}{separator}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
    }

    private static async Task<IActionResult> ToActionResult<T>(
    Task<AuthResult<T>> resultTask)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Response);
        }

        return result.ErrorKind switch
        {
            AuthErrorKind.Validation =>
                new BadRequestObjectResult(
                    new { message = result.ErrorMessage }),

            AuthErrorKind.EmailAlreadyExists =>
                new ConflictObjectResult(
                    new { message = result.ErrorMessage }),

            AuthErrorKind.InvalidCredentials =>
                new UnauthorizedObjectResult(
                    new { message = result.ErrorMessage }),

            AuthErrorKind.NotImplemented =>
                new ObjectResult(
                    new { message = result.ErrorMessage })
                {
                    StatusCode = StatusCodes.Status501NotImplemented
                },

            _ =>
                new BadRequestObjectResult(
                    new { message = result.ErrorMessage })
        };
    }

}
