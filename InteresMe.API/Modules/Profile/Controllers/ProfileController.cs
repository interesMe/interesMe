using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Interests.DTOs;
using InteresMe.API.Modules.Profile.DTOs;
using InteresMe.API.Modules.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Profile.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController(
    IProfileService profileService,
    IProfileRequestReader profileRequestReader,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    public Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => profileService.GetMyProfileAsync(userId, cancellationToken));

    [HttpPost("me")]
    [Consumes("application/json", "multipart/form-data")]
    public async Task<IActionResult> CreateMyProfile(
        CancellationToken cancellationToken)
    {
        var request = await profileRequestReader.ReadAsync<CreateProfileRequest>(
            Request,
            cancellationToken);

        if (request.ErrorMessage is not null)
        {
            return BadRequest(new { message = request.ErrorMessage });
        }

        return await WithCurrentUserId(
            userId => profileService.CreateMyProfileWithAvatarAsync(
                userId,
                request.Value!,
                request.AvatarFile,
                cancellationToken));
    }

    [HttpPut("me")]
    [Consumes("application/json", "multipart/form-data")]
    public async Task<IActionResult> UpdateMyProfile(
        CancellationToken cancellationToken)
    {
        var request = await profileRequestReader.ReadAsync<UpdateProfileRequest>(
            Request,
            cancellationToken);

        if (request.ErrorMessage is not null)
        {
            return BadRequest(new { message = request.ErrorMessage });
        }

        return await WithCurrentUserId(
            userId => profileService.UpdateMyProfileWithAvatarAsync(
                userId,
                request.Value!,
                request.AvatarFile,
                cancellationToken));
    }

    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> UploadMyAvatar(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Task.FromResult<IActionResult>(
                BadRequest(new { message = "Avatar file is required." }));
        }

        return WithCurrentUserId(
            userId => profileService.UploadMyAvatarAsync(userId, file, cancellationToken));
    }

    [HttpPut("me/interests")]
    public Task<IActionResult> ReplaceMyInterests(
        [FromBody] UpdateUserInterestsRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => profileService.ReplaceMyInterestsAsync(userId, request, cancellationToken));

    private async Task<IActionResult> WithCurrentUserId<T>(
        Func<Guid, Task<ApplicationResult<T>>> action)
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

        return ToActionResult(await action(userId));
    }
}
