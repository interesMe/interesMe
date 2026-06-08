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
        var request = await ReadProfileRequestAsync<CreateProfileRequest>(cancellationToken);

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
        var request = await ReadProfileRequestAsync<UpdateProfileRequest>(cancellationToken);

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

    private async Task<ProfileRequestBinding<TRequest>> ReadProfileRequestAsync<TRequest>(
        CancellationToken cancellationToken)
        where TRequest : class, new()
    {
        if (!Request.HasFormContentType)
        {
            var jsonRequest = await Request.ReadFromJsonAsync<TRequest>(
                cancellationToken: cancellationToken);

            return jsonRequest is null
                ? new ProfileRequestBinding<TRequest>(null, null, "Profile request body is required.")
                : new ProfileRequestBinding<TRequest>(jsonRequest, null, null);
        }

        var form = await Request.ReadFormAsync(cancellationToken);
        var birthDateValue = form["birthDate"].ToString();
        DateOnly? birthDate = null;

        if (!string.IsNullOrWhiteSpace(birthDateValue))
        {
            if (!DateOnly.TryParse(birthDateValue, out var parsedBirthDate))
            {
                return new ProfileRequestBinding<TRequest>(
                    null,
                    null,
                    "Birth date must be a valid date.");
            }

            birthDate = parsedBirthDate;
        }

        var request = new TRequest();

        switch (request)
        {
            case CreateProfileRequest createRequest:
                createRequest.DisplayName = form["displayName"].ToString();
                createRequest.City = NormalizeOptional(form["city"].ToString());
                createRequest.BirthDate = birthDate;
                break;

            case UpdateProfileRequest updateRequest:
                updateRequest.DisplayName = form["displayName"].ToString();
                updateRequest.City = NormalizeOptional(form["city"].ToString());
                updateRequest.BirthDate = birthDate;
                break;
        }

        return new ProfileRequestBinding<TRequest>(
            request,
            form.Files.GetFile("avatarFile"),
            null);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record ProfileRequestBinding<TRequest>(
        TRequest? Value,
        IFormFile? AvatarFile,
        string? ErrorMessage);
}
