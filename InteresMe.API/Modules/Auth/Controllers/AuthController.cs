using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Auth.DTOs;
using InteresMe.API.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IOAuthRedirectService oauthRedirectService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.RegisterAsync(request, cancellationToken));

    [HttpPost("google")]
    [AllowAnonymous]
    public Task<IActionResult> GoogleLogin(
        [FromBody] GoogleAuthRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(
            authService.GoogleLoginAsync(
                request,
                cancellationToken));

    [HttpGet("github")]
    [AllowAnonymous]
    public IActionResult StartGithubLogin()
    {
        var callbackUrl = oauthRedirectService.BuildGithubCallbackUrl(Request);
        var result = authService.StartGithubLogin(callbackUrl);

        if (!result.IsSuccess || result.Response is null)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        oauthRedirectService.SetGithubStateCookie(
            Response,
            Request,
            result.Response.State);

        return Redirect(result.Response.AuthorizationUrl);
    }

    [HttpGet("github/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GithubCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect(oauthRedirectService.BuildFrontendCallbackUrl(error: "github_denied"));
        }

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(state))
        {
            return Redirect(oauthRedirectService.BuildFrontendCallbackUrl(error: "github_invalid_callback"));
        }

        var expectedState = oauthRedirectService.GetGithubStateCookie(Request);
        oauthRedirectService.DeleteGithubStateCookie(Response);

        var result = await authService.CompleteGithubCallbackAsync(
            new GithubAuthRequest
            {
                Code = code,
                RedirectUri = oauthRedirectService.BuildGithubCallbackUrl(Request)
            },
            expectedState,
            state,
            cancellationToken);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Response))
        {
            return Redirect(oauthRedirectService.BuildFrontendCallbackUrl(error: "github_auth_failed"));
        }

        return Redirect(oauthRedirectService.BuildFrontendCallbackUrl(sessionCode: result.Response));
    }

    [HttpPost("github")]
    [AllowAnonymous]
    public Task<IActionResult> GithubLogin(
        [FromBody] GithubAuthRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(
            authService.GithubLoginAsync(
                request,
                cancellationToken));

    [HttpPost("github/session")]
    [AllowAnonymous]
    public Task<IActionResult> CompleteGithubSession(
        [FromBody] GithubSessionRequest request) =>
        ToActionResult(Task.FromResult(authService.CompleteGithubSession(request)));

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(authService.RefreshTokenAsync(request, cancellationToken));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest? request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMyAccount(
        CancellationToken cancellationToken)
    {
        Guid userId;

        try
        {
            userId = currentUser.UserId;
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { message = "Invalid access token." });
        }

        await authService.DeleteMyAccountAsync(userId, cancellationToken);

        return NoContent();
    }

}
