using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Discovery.DTOs;
using InteresMe.API.Modules.Discovery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Discovery.Controllers;

[ApiController]
[Authorize]
[Route("api/discovery")]
public sealed class DiscoveryController(
    IDiscoveryService discoveryService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("goals")]
    [AllowAnonymous]
    public IActionResult GetGoals() => Ok(discoveryService.GetGoals());

    [HttpGet("me")]
    public Task<IActionResult> GetMyPreference(
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => discoveryService.GetMyPreferenceAsync(userId, cancellationToken));

    [HttpPut("me/goal")]
    public Task<IActionResult> UpdateMyGoal(
        [FromBody] UpdateUserGoalRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => discoveryService.UpdateMyGoalAsync(userId, request, cancellationToken));

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
