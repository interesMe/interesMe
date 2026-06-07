using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InteresMe.API.Modules.Auth.Services;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Profile.DTOs;
using InteresMe.API.Modules.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteresMe.API.Modules.Profile.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet("me")]
    public Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => profileService.GetMyProfileAsync(userId, cancellationToken));

    [HttpPost("me")]
    public Task<IActionResult> CreateMyProfile(
        [FromBody] CreateProfileRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => profileService.CreateMyProfileAsync(userId, request, cancellationToken));

    [HttpPut("me")]
    public Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => profileService.UpdateMyProfileAsync(userId, request, cancellationToken));

    [HttpPut("me/interests")]
    public Task<IActionResult> ReplaceMyInterests(
        [FromBody] UpdateUserInterestsRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => profileService.ReplaceMyInterestsAsync(userId, request, cancellationToken));

    private async Task<IActionResult> WithCurrentUserId<T>(
        Func<Guid, Task<AuthResult<T>>> action)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new { message = "Invalid access token." });
        }

        return ToActionResult(await action(userId.Value));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static IActionResult ToActionResult<T>(
        AuthResult<T> result)
    {
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
                new NotFoundObjectResult(
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
