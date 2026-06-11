using InteresMe.API.BuildingBlocks.Results;
using InteresMe.API.BuildingBlocks.Security;
using InteresMe.API.Modules.Initiatives.Contracts.Requests;
using InteresMe.API.Modules.Initiatives.Services.InitiativeManagement;
using InteresMe.API.Modules.Initiatives.Services.InitiativeQueries;
using InteresMe.API.Modules.Initiatives.Services.JoinRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static InteresMe.API.BuildingBlocks.Results.ApplicationResultMapper;

namespace InteresMe.API.Modules.Initiatives.Controllers;

[ApiController]
[Authorize]
[Route("api/initiatives")]
public sealed class InitiativesController(
    IInitiativeQueryService initiativeQueryService,
    IInitiativeManagementService initiativeManagementService,
    IInitiativeJoinRequestService initiativeJoinRequestService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Ok(await initiativeQueryService.GetAllAsync(cancellationToken));

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] CreateInitiativeRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeManagementService.CreateAsync(userId, request, cancellationToken));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        ToActionResult(initiativeQueryService.GetByIdAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateInitiativeRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeManagementService.UpdateAsync(userId, id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId, out var unauthorizedResult))
        {
            return unauthorizedResult;
        }

        var result = await initiativeManagementService.DeleteAsync(userId, id, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : ToActionResult(result);
    }

    [HttpPost("{id:guid}/join-requests")]
    public Task<IActionResult> CreateJoinRequest(
        Guid id,
        [FromBody] CreateInitiativeJoinRequestRequest request,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeJoinRequestService.CreateJoinRequestAsync(
                userId,
                id,
                request,
                cancellationToken));

    [HttpGet("{id:guid}/join-requests")]
    public Task<IActionResult> GetJoinRequests(
        Guid id,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeJoinRequestService.GetJoinRequestsAsync(
                userId,
                id,
                cancellationToken));

    [HttpPost("{id:guid}/join-requests/{requestId:guid}/accept")]
    public Task<IActionResult> AcceptJoinRequest(
        Guid id,
        Guid requestId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeJoinRequestService.AcceptJoinRequestAsync(
                userId,
                id,
                requestId,
                cancellationToken));

    [HttpPost("{id:guid}/join-requests/{requestId:guid}/reject")]
    public Task<IActionResult> RejectJoinRequest(
        Guid id,
        Guid requestId,
        CancellationToken cancellationToken) =>
        WithCurrentUserId(
            userId => initiativeJoinRequestService.RejectJoinRequestAsync(
                userId,
                id,
                requestId,
                cancellationToken));

    private async Task<IActionResult> WithCurrentUserId<T>(
        Func<Guid, Task<ApplicationResult<T>>> action)
    {
        if (!TryGetCurrentUserId(out var userId, out var unauthorizedResult))
        {
            return unauthorizedResult;
        }

        return ToActionResult(await action(userId));
    }

    private bool TryGetCurrentUserId(
        out Guid userId,
        out IActionResult unauthorizedResult)
    {
        try
        {
            userId = currentUser.UserId;
            unauthorizedResult = default!;
            return true;
        }
        catch (InvalidOperationException)
        {
            userId = default;
            unauthorizedResult = Unauthorized(new { message = "Invalid access token." });
            return false;
        }
    }
}
