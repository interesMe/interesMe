using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Profile.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersProfileController(
    IProfileViewService profileViewService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me/profile-view")]
    public Task<IActionResult> GetMyProfileView(CancellationToken cancellationToken) =>
        ToActionResult(profileViewService.GetAsync(
            currentUser.UserId,
            currentUser.UserId,
            cancellationToken));

    [HttpGet("{userId:guid}/profile-view")]
    public Task<IActionResult> GetProfileView(Guid userId, CancellationToken cancellationToken) =>
        ToActionResult(profileViewService.GetAsync(
            currentUser.UserId,
            userId,
            cancellationToken));

    [HttpPost("{userId:guid}/follow")]
    public Task<IActionResult> Follow(Guid userId, CancellationToken cancellationToken) =>
        ToNoContentResult(profileViewService.FollowAsync(
            currentUser.UserId,
            userId,
            cancellationToken));

    [HttpDelete("{userId:guid}/follow")]
    public Task<IActionResult> Unfollow(Guid userId, CancellationToken cancellationToken) =>
        ToNoContentResult(profileViewService.UnfollowAsync(
            currentUser.UserId,
            userId,
            cancellationToken));

    private static async Task<IActionResult> ToNoContentResult(
        Task<ApplicationResult<bool>> resultTask)
    {
        var result = await resultTask;
        return result.IsSuccess ? new NoContentResult() : ToActionResult(result);
    }
}
